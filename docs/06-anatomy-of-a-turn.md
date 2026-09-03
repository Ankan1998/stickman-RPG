# 6. Anatomy of a turn

**The most useful page here.** We follow one single turn from the moment you
click a button to the moment pixels change, naming every method along the way.

Once you have followed this once, the rest of the codebase is obvious.

---

## The scenario

Round 1. Turn order is decided by Speed, highest first:

| Order | Actor | Speed |
|---|---|---|
| 1st | Stick Archer | 16 |
| 2nd | Goblin A | 14 |
| 3rd | Goblin B | 14 |
| 4th | Stick Medic | 12 |
| **5th** | **Stick Warrior** | **10** |
| 6th | Goblin Brute | 7 |

Everyone above the Warrior has already gone. It is now his turn, and you are
about to pick **Heavy Blow**, then **Goblin A**.

Relevant numbers:

```
Stick Warrior   Attack 15   CritChance 10%
Heavy Blow      Power 180   Cooldown 2
Goblin A        Health 44   Defense 4
```

---

## The whole thing at a glance

```
   YOU CLICK
       │
       │  game/scripts/BattleView.cs
       ▼
  ┌─────────────────────────────────────────────┐
  │ 1  OnActionChosen(action)                   │
  │ 2  ClearActionMenu()                        │
  └─────────────────────────────────────────────┘
       │
       │  crossing into the rules  ─────────────────┐
       ▼                                            │
  ┌─────────────────────────────────────────────┐  │  src/Rpg.Core/
  │ 3  battle.TakeTurn(action)                  │  │
  │      3a  validate it is really their turn   │  │  EVERYTHING here
  │      3b  action.Execute(...)                │  │  happens in about
  │            roll crit, compute damage,       │  │  20 microseconds.
  │            apply it, record it              │  │  Nothing is drawn.
  │      3c  ReportDeaths()                     │  │
  │      3d  TickStatuses()   ← poison, timers  │  │
  │      3e  CheckForEnd()                      │  │
  │      3f  AdvanceToNextTurn()                │  │
  │                                             │  │
  │    returns List<GameEvent>  ────────────────┼──┘
  └─────────────────────────────────────────────┘
       │
       │  back in game/
       ▼
  ┌─────────────────────────────────────────────┐
  │ 4  await PlayEvents(events)                 │   ~1.4 seconds
  │      for each event:                        │   of animation
  │        Describe() → text into the log       │
  │        RefreshAll()  → redraw the fighters     │
  │        await 0.45s pause                    │
  └─────────────────────────────────────────────┘
       │
       ▼
  ┌─────────────────────────────────────────────┐
  │ 5  await ContinueBattle()                   │
  │      monster? → AI picks, loop again        │
  │      hero?    → show menu, stop and wait    │
  └─────────────────────────────────────────────┘
```

Now the same thing, slowly.

---

## Step 1 — The click

Back when it became the Warrior's turn, `ShowSkillMenu` asked the rules what he
could legally do, and **grouped that list by skill**:

```csharp
List<IAction> legal = _run.Battle.LegalActions(actor);

var bySkill = legal.OfType<SkillAction>()
    .GroupBy(a => a.Skill.Id)
    .ToList();
```

`LegalActions` returned ten options — Slash and Heavy Blow at each of the three
living monsters, Guard on self, and Wait. Ten buttons is a wall nobody reads, so
the grouping turns them into **three**:

```
Slash        (100% dmg)
Heavy Blow   (180% dmg)     <- you click this one
Guard        (guarding)
Wait
```

You click Heavy Blow. Because that skill has more than one legal target, the menu
does not act yet — it asks the second question:

```
Heavy Blow - pick a target
  Goblin A       (25 dmg)   <- and this one
  Goblin B       (25 dmg)
  Goblin Brute   (24 dmg)
  < Back
```

Those previews are not guesses; they call the very same `DamageCalculator` the
fight will use a millisecond later:

```csharp
string preview = $"{DamageCalculator.Compute(
    actor.CurrentStats, target.CurrentStats, skill.Power, false)} dmg";
```

You click. Godot fires the `Pressed` signal. The lambda runs.

---

## Step 2 — Clear the menu

```csharp
private async void OnActionChosen(IAction action)
{
    ClearMenu();
    await PlayEvents(_run.TakeTurn(action));
    await ContinueBattle();
}
```

> Note it calls `_run.TakeTurn`, not `_battle.TakeTurn`. `Run` forwards to the
> battle and *also* folds the resulting events into the run statistics. Same
> events, same rules - it just keeps score on the way past.

The buttons are removed immediately, so you cannot click twice while the
animation plays.

> **Why `async void`?** Normally bad practice — but this is an *event handler*,
> called by Godot's button signal, which has no way to await anything. Event
> handlers are the one accepted exception. See the
> [C# crash course](02-csharp-crash-course.md#async--await).

Look at the middle line carefully:

```csharp
await PlayEvents( _run.TakeTurn(action) );
       ▲                ▲
       │                └── runs FIRST, and finishes completely.
       │                    The entire turn is decided here.
       └── only then does anything appear on screen.
```

---

## Step 3 — The rules resolve the turn

We are now inside `Battle.TakeTurn` in
[`src/Rpg.Core/Combat/Battle.cs`](../src/Rpg.Core/Combat/Battle.cs). No drawing
happens anywhere in this step.

### 3a. Sanity checks

```csharp
if (IsOver)
    throw new InvalidOperationException("The battle is already over.");

if (!ReferenceEquals(action.Actor, Current))
    throw new InvalidOperationException(
        $"It is {Current?.Name}'s turn, but the action belongs to {action.Actor.Name}.");
```

Cheap, and they turn a whole class of subtle bugs into an immediate loud crash.
There is a test for the second one (`ActingOutOfTurnIsRejected`).

An empty log is created. Everything from here appends to it.

```csharp
var log = new List<GameEvent>();
```

### 3b. Execute the action

```csharp
if (actor.CanAct)
    action.Execute(State, log);
else
    log.Add(new TurnSkipped(actor.Id, actor.BlockedReason ?? "Unable to act"));
```

`CanAct` is false if any active status has `PreventsAction` — that is how stun
works. Our Warrior is fine, so `SkillAction.Execute` runs
([`SkillAction.cs`](../src/Rpg.Core/Combat/SkillAction.cs)):

```csharp
log.Add(new SkillUsed(Actor.Id, Skill.Id, Target.Id));
```

> **📝 EVENT 1** — `SkillUsed("hero_warrior", "heavy_blow", "monster_goblin")`

```csharp
Actor.PutOnCooldown(Skill);
```

Heavy Blow has `Cooldown: 2`, so the Warrior's cooldown dictionary now holds
`{"heavy_blow": 2}`. He cannot use it again until it ticks down to zero.

```csharp
bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);
```

`CritChance` is 10, so this is `NextInt(0, 100) < 10`. Say the seeded RNG returns
`63` — no critical this time.

> This line runs **before** the damage calculation, always. Consuming the random
> number generator in a fixed order is what makes a seed reproduce a battle
> exactly. Reorder these two lines and every saved replay breaks.

```csharp
int damage = DamageCalculator.Compute(
    Actor.CurrentStats, Target.CurrentStats, Skill.Power, isCritical);
```

Inside [`DamageCalculator`](../src/Rpg.Core/Combat/DamageCalculator.cs):

```
raw       = Attack × Power / 100  =  15 × 180 / 100  =  27
mitigated = raw − Defense / 2     =  27 − (4 / 2)    =  25
critical? = no
result    = max(1, 25)            =  25
```

(If the crit *had* landed, that would be `25 × 200 / 100 = 50` — more than the
Goblin's 44 HP, and the Goblin would die right here.)

```csharp
int applied = Target.TakeDamage(damage);
log.Add(new Damaged(Target.Id, applied, isCritical, SourceId: Actor.Id));
```

`TakeDamage` clamps: you can never lose more health than you have, so `applied`
is the *actual* amount lost. The Goblin drops from 44 to 19.

> **📝 EVENT 2** — `Damaged("monster_goblin", 25, IsCritical: false, SourceId: "hero_warrior")`

`SourceId` says who swung. It has to, because the screen replays this log *after*
the turn has already advanced — so asking the battle "whose turn is it?" would
name the wrong fighter, and every impact would play the wrong weapon's sound.

Heavy Blow has no `Healing` and no `AppliesStatus`, so the last two blocks are
skipped.

### 3c. Report deaths

```csharp
ReportDeaths(log);
```

Walks every actor, and logs `Died` for anyone at 0 HP who has not already been
reported. A `HashSet<string>` remembers who has been announced.

> **Why centralised?** An actor can die from a sword, from poison, or later from
> a reflected hit. If each of those logged its own `Died`, you would eventually
> double-report one and the UI would play the death animation twice.

The Goblin is at 19 HP. Nothing logged.

### 3d. Tick the acting actor's statuses

```csharp
TickStatuses(actor, log);
```

This is where poison damage lands and where durations count down — for the actor
whose turn just ended.

```csharp
foreach (StatusEffect status in actor.Statuses.ToList())
{
    if (status.Definition.DamagePerTurn > 0 && actor.IsAlive)
    {
        int applied = actor.TakeDamage(status.Definition.DamagePerTurn);
        log.Add(new Damaged(actor.Id, applied, IsCritical: false, StatusId: status.Id));
    }

    status.Tick();

    if (status.IsExpired)
    {
        actor.RemoveStatus(status);
        log.Add(new StatusExpired(actor.Id, status.Id));
    }
    else
    {
        log.Add(new StatusTicked(actor.Id, status.Id, status.RemainingTurns));
    }
}
```

Three things worth noticing:

- **`.ToList()`** takes a snapshot, because the loop removes expired statuses
  from the very collection it is iterating.
- **This happens *after* acting**, deliberately. A 1-turn poison still gets to
  deal its damage once before wearing off.
- **It ticks only the acting actor.** Poison on the Goblin ticks on the Goblin's
  turn, not the Warrior's. That is what makes durations feel fair.

Our Warrior has no statuses. Nothing logged.

Then `ReportDeaths(log)` runs again — because poison can kill.

### 3e. Is the battle over?

```csharp
if (CheckForEnd(log)) return log;
```

Checks whether either team has been wiped out. Both monsters are alive, so no.

### 3f. Move to the next actor

```csharp
AdvanceToNextTurn(log);
```

```csharp
while (!_queue.MoveNext())
{
    if (_queue.Round >= MaxRounds) { Finish(null, log); return; }   // draw
    _queue.BeginRound(State.Actors);
    log.Add(new RoundStarted(_queue.Round));
}

Actor next = _queue.Current!;
next.TickCooldowns();
log.Add(new TurnStarted(next.Id));
```

`MoveNext()` returns true — the Goblin Brute is 4th and still alive. The `while`
loop is for the case where the round has run out and a new one must start.

`next.TickCooldowns()` fires at the *start* of the Brute's turn, reducing each of
his cooldowns by one.

> **📝 EVENT 3** — `TurnStarted("monster_brute")`

### 3g. Return

```csharp
return log;
```

`TakeTurn` is done. Total elapsed: roughly 20 microseconds. Nothing has been
drawn. The returned list is:

```csharp
[
  SkillUsed("hero_warrior", "heavy_blow", "monster_goblin"),
  Damaged("monster_goblin", 25, IsCritical: false),
  TurnStarted("monster_brute"),
]
```

**The turn is over. The screen has not been touched.**

---

## Step 4 — The screen replays it

Now we are back in Godot, in `PlayEvents`:

```csharp
private async Task PlayEvents(IEnumerable<GameEvent> events)
{
    foreach (GameEvent gameEvent in events)
    {
        if (gameEvent is RoundStarted round)
            _displayedRound = round.Round;

        if (gameEvent is TurnStarted turn)
            _statusLabel.Text = $"Round {_displayedRound}  -  {ActorName(turn.ActorId)}'s turn";

        string? line = Describe(gameEvent);
        if (line is not null)
            Write(line);

        RefreshViews();

        if (IsWorthPausingFor(gameEvent))
            await ToSignal(GetTree().CreateTimer(0.45), SceneTreeTimer.SignalName.Timeout);
    }
}
```

### Event 1 — `SkillUsed`

`Describe` matches it and produces text:

```csharp
SkillUsed s => s.ActorId == s.TargetId
    ? $"{ActorName(s.ActorId)} uses [b]{SkillName(s.SkillId)}[/b]."
    : $"{ActorName(s.ActorId)} uses [b]{SkillName(s.SkillId)}[/b] on {ActorName(s.TargetId)}.",
```

The `[b]...[/b]` is **BBCode** — `RichTextLabel`'s markup for bold, colour, and so
on. The log now reads:

> Stick Warrior uses **Heavy Blow** on Goblin.

`RefreshViews()` calls `QueueRedraw()` on all four stickmen. Then `IsWorthPausingFor`
returns true, so we `await` for 0.45 seconds — the game keeps rendering frames,
this method just resumes later.

### Event 2 — `Damaged`

```csharp
Damaged d => d.IsCritical
    ? $"    [color=#cf5b5b]{d.Amount}[/color] damage to {ActorName(d.ActorId)} [b](critical!)[/b]"
    : $"    [color=#cf5b5b]{d.Amount}[/color] damage to {ActorName(d.ActorId)}",
```

> &nbsp;&nbsp;&nbsp;&nbsp;<span style="color:#cf5b5b">25</span> damage to Goblin

`RefreshViews()` again — and **this** is where the Goblin's health bar visibly
drops, because `ActorView.Refresh()` reads `_actor.Health` live:

```csharp
float fraction = (float)_actor.Health / _actor.MaxHealth;
_bar.Value = _actor.Health;
_bar.AddThemeStyleboxOverride("fill", UiTheme.Flat(UiTheme.HealthColour(fraction)));
```

19 / 44 = 0.43, so the bar shrinks to 43% and stays green (it turns red below
30%).

Another 0.45 second pause.

### Event 3 — `TurnStarted`

The status label updates to *"Round 1 - Goblin Brute's turn"*, and the Brute's
stickman gets its highlight plate:

```csharp
if (_highlighted && alive)
    DrawRect(new Rect2(4, 4, Size.X - 8, Size.Y - 8), new Color(1, 1, 1, 0.06f));
```

`Describe` returns `null` for `TurnStarted` (the status label already says it, and
a log line every turn would be noise), so nothing is written. `IsWorthPausingFor`
returns false, so **no pause** — the next turn begins immediately.

---

## Step 5 — Keep going

```csharp
await ContinueBattle();
```

```csharp
while (!_battle.IsOver)
{
    Actor current = _battle.Current!;

    if (current.Team == Team.Heroes)
    {
        ShowActionMenu(current);
        return;                       // stop. wait for a human.
    }

    IAction action = ScoringAi.ChooseAction(_battle, current);
    await PlayEvents(_battle.TakeTurn(action));
}

ShowResult();
```

It is the Goblin Brute's turn — a monster. So `ScoringAi.ChooseAction` scores
every legal action and picks the best, and we loop straight back through steps
3–4 without any human involvement.

The Brute's options score roughly:

| Action | Damage | Score | Why |
|---|---|---|---|
| Club → Stick Medic | 15 | 15 | `18 − 6/2 = 15` |
| **Headbutt → Stick Medic** | 9 | **25** | `18×70% − 6/2 = 9`, plus `StunValue` 16 |
| Club → Stick Warrior | 14 | 14 | `18 − 9/2 = 14` |
| Headbutt → Stick Warrior | 8 | 24 | `8`, plus `StunValue` 16 |
| Wait | — | 0.1 | `PassValue` |

Headbutt on the Medic wins at 25 — **the AI gives up 6 points of damage to deny a
turn.** That is `StunValue` doing its job, and it is the single number you would
change to make monsters more or less annoying. Drop it to 5 and the Brute would
just club people.

(Integer division everywhere: `9 / 2` is `4`, not `4.5`.)

Eventually the loop reaches the Warrior or the Medic again, `ShowActionMenu`
fires, `return` exits the loop, and the game sits idle until you click.

When one team is wiped out, `_battle.IsOver` becomes true, the loop exits, and
`ShowResult()` prints **Victory** or **Defeat** and offers a "Fight again (new
seed)" button.

---

## The point

Read that trace again and notice the shape of it:

- **Step 3 has no `await`, no drawing, no timing.** It is pure computation, which
  is why a test can run it ten thousand times per second.
- **Step 4 has no rules.** It cannot change the outcome. It cannot even
  accidentally change the outcome, because `Actor.Health` has a `private set` and
  the mutating methods are `internal`.
- **The list of events is the only thing crossing between them.**

That separation is the whole design. Everything else in this repository is a
consequence of it.

---

## Next

[Recipes](07-recipes.md) — how to actually change things.

# 8. Turns, actions and resolution

> **Where you are:** chapter 8 of 17 · [index](README.md) · previous: [Randomness and determinism](07-randomness-and-determinism.md) · next: [Numbers: damage and stat design](09-numbers-and-stat-design.md)

---

## The problem

"Turn-based" sounds like one thing. It is at least five, and choosing between
them is one of the earliest and most consequential design decisions you will
make — because it determines what your Speed stat *means*, and therefore how
half your game feels.

---

## Models of time

### 1. Real-time

Time advances on its own. Everybody acts whenever they want.

```
   ---------------------------------------------> time
   hero attacks    goblin attacks   hero dodges
```

*Doom, Hades, Street Fighter.* Reflexes matter. Hard to make deep tactically,
because the player cannot think.

### 2. Real-time with pause

Real-time, but the player can freeze it and issue orders.

*Baldur's Gate, Pillars of Eternity.* Tactical depth without losing momentum.
Complex to build.

### 3. Round-based (this project)

Everybody alive acts once per round, in an order decided by Speed. When everybody
has gone, a new round begins.

```
   ROUND 1:  Rogue(17) -> Cleric(11) -> Warrior(10) -> Goblin(8)
   ROUND 2:  Rogue(17) -> Cleric(11) -> Warrior(10) -> Goblin(8)
```

*XCOM, Darkest Dungeon, most tabletop RPGs.* Simple, predictable, easy to show
the player. Speed decides *when* you act within a round, but never *how often*.

### 4. ATB / action gauge

Every actor fills a gauge over time; when it is full, they act.

```
   Rogue    ####------  fills fast   -> acts often
   Warrior  ##--------  fills slow   -> acts rarely
```

*Final Fantasy IV–IX, Grandia.* Speed now grants **extra turns**, which is
enormously more powerful. Deeper, and much harder to balance.

### 5. Action points

Each turn you get a pool of points and spend them however you like. Move 1,
shoot 2, reload 1.

*XCOM 2, Divinity: Original Sin.* Excellent for tactical variety. A large amount
of design and UI work.

---

## Why this project chose round-based

From [`TurnQueue`](../../src/Rpg.Core/Combat/TurnQueue.cs):

> It is the simplest scheme that still makes Speed a meaningful stat, and it is
> trivial to show the player.

That is the whole argument, and it is a good one for a first game. Everything you
need is about sixty lines:

```csharp
public void BeginRound(IEnumerable<Actor> actors)
{
    Round++;
    _order.Clear();
    _order.AddRange(actors
        .Where(a => a.IsAlive)
        .OrderByDescending(a => a.CurrentStats.Speed)
        .ThenBy(a => a.Id, StringComparer.Ordinal));
    _index = -1;
}

public bool MoveNext()
{
    // Re-check IsAlive: an actor can be killed DURING the round, after the
    // order was already decided. A corpse must not get a turn.
    while (++_index < _order.Count)
        if (_order[_index].IsAlive) return true;

    return false;
}
```

Three details in there are worth stealing:

- **`CurrentStats.Speed`, not `BaseStats.Speed`** — so a Haste buff genuinely
  moves you up the order. There is a test called
  `SpeedBuffsActuallyChangeTheOrder` guarding exactly this.
- **The dead are re-checked in `MoveNext`**, not only when the round starts. Kill
  the goblin before its turn and it does not get one.
- **`ThenBy(a => a.Id, StringComparer.Ordinal)`** — deterministic tie-breaks, for
  the reasons in [chapter 7](07-randomness-and-determinism.md).

And note the upgrade path is *already open*, because turn order lives behind its
own type:

> When you want more depth, replace this with an ATB system: each actor
> accumulates Speed points every tick and acts when they cross a threshold.
> Nothing outside this class would need to change — which is exactly why turn
> order lives behind its own type instead of being a for-loop inside `Battle`.

That is the practical argument for small, single-purpose classes, stated as a
consequence rather than a principle.

---

## Actions: the Command pattern

A player clicks a button. An AI picks a move. Both then need to *do* the thing.

The naive approach is a method per move — `Attack()`, `Heal()`, `Defend()` — and
it falls apart immediately, because the AI now needs to evaluate options it
cannot hold in a variable.

**The fix: make a move into an object.**

```csharp
public interface IAction
{
    Actor Actor { get; }
    string Label { get; }
    void Execute(BattleState state, List<GameEvent> log);
}
```

Now a move is a **value**. You can store it, pass it, put it in a list, *score*
it, sort by that score, and later undo it. This is the classic **Command
pattern**, and it is probably the single most useful pattern in turn-based game
code.

There are three implementations in this project:

| Action | Does |
|---|---|
| [`SkillAction`](../../src/Rpg.Core/Combat/SkillAction.cs) | Use a skill on a target. **This one class executes all 55 skills.** |
| [`MoveAction`](../../src/Rpg.Core/Combat/MoveAction.cs) | Swap places with the ally beside you. |
| [`PassAction`](../../src/Rpg.Core/Combat/PassAction.cs) | Wait. |

### One list feeds both the menu and the AI

```csharp
public List<IAction> LegalActions(Actor actor)
```

This is the most architecturally important method in the combat system.

```
                  Battle.LegalActions(actor)
                            |
              +-------------+-------------+
              |                           |
              v                           v
        the button menu              ScoringAi
        (groups them)                (scores them, picks best)
```

Neither side can do anything the other could not, because **there is no other
source of moves**. The AI cannot ignore a cooldown or reach a rank it should not,
not because somebody was careful, but because no code path exists that would let
it.

### Why `Execute` takes the log as a parameter

This looks tidier:

```csharp
IEnumerable<GameEvent> Execute(BattleState state);     // NOT what this does
```

...and is a trap. In C#, a method that builds a sequence with `yield return`
**does nothing at all** until somebody enumerates the result. Forget to loop over
it and your attack silently deals no damage, with no error anywhere.

That is a genuinely nasty bug to find in combat code. So the list is passed in
and appended to. Boring, obvious, always runs.

### Why `PassAction` exists

It looks pointless. It is structural:

> It guarantees `LegalActions()` can never return an empty list.

Without it, an actor who is stunned, or whose every skill is on cooldown, would
have *no legal move*. The UI would have no buttons and the game would sit there
forever, with no error and nothing in a log to explain it.

> **The general lesson: make the degenerate case legal rather than impossible.**
> It is nearly free and removes a whole category of hangs.

---

## Anatomy of one turn

Here is [`Battle.TakeTurn`](../../src/Rpg.Core/Combat/Battle.cs) with the noise
removed. Read the order carefully — every line of it is a rules decision.

```csharp
public List<GameEvent> TakeTurn(IAction action)
{
    if (IsOver) throw new InvalidOperationException("The battle is already over.");
    if (!ReferenceEquals(action.Actor, Current))
        throw new InvalidOperationException(/* wrong actor */);

    var log = new List<GameEvent>();

    // 1. Act, unless something is stopping this actor.
    if (actor.CanAct) action.Execute(State, log);
    else log.Add(new TurnSkipped(actor.Id, actor.BlockedReason ?? "Unable to act"));

    ReportDeaths(log);

    // 2. End-of-turn statuses: poison damage, durations counting down.
    TickStatuses(actor, log);
    ReportDeaths(log);              // ...because poison can kill

    // 3. Is anyone left standing?
    if (CheckForEnd(log)) return log;

    // 4. On to the next actor.
    AdvanceToNextTurn(log);
    return log;
}
```

### The decisions hiding in that order

**Statuses tick *after* acting.** So a 1-turn poison still gets to deal its
damage once before wearing off. Tick first and a poison applied with 1 turn left
would expire without ever hurting anybody — technically defensible, and it feels
like a bug to a player.

**`ReportDeaths` is called twice.** Because you can die from a sword *or* from
poison, and both need to be announced. Which raises the obvious problem, solved
next.

**Deaths are announced exactly once, ever:**

```csharp
private void ReportDeaths(List<GameEvent> log)
{
    foreach (Actor actor in State.Actors)
    {
        // HashSet.Add returns false if it was already there, so this reads as
        // "if they are dead AND we have not said so yet".
        if (!actor.IsAlive && _deathsReported.Add(actor.Id))
            log.Add(new Died(actor.Id));
    }
}
```

Centralised on purpose. An actor can die from a blade, from poison, or later from
a reflected hit. If each of those logged its own death, you would eventually
double-report one — and the screen would play the death animation twice.

> That is worth pausing on. The *rules* were carefully built so a death is
> announced once. The bug in [chapter 6](06-events-and-replay.md) was that the
> *screen* found a second way to notice the same death. Both halves of the split
> need this discipline, independently.

**Cooldowns tick at the start of your turn**, in `AdvanceToNextTurn`:

```csharp
next.TickCooldowns();
```

So "cooldown 2" means "you miss two of *your own* turns", which is what a player
intuitively expects. Tick them at end-of-round instead and a fast character's
cooldowns effectively last longer, for no reason anybody could explain.

**There is a round limit:**

```csharp
public const int MaxRounds = 100;
```

Two actors who cannot meaningfully hurt each other must not loop forever. At 100
rounds it is a draw. This is a safety valve, not a mechanic — but without it, a
balance harness running 2,250 fights will eventually hang on one, and you will
lose an afternoon.

---

## Skills: one class, 55 behaviours

[`SkillAction.Execute`](../../src/Rpg.Core/Combat/SkillAction.cs) runs every
skill in the game. Slash, Heavy Blow, Healing Word, Poison Dart, Bloodthirst and
Soul Rip all go through these ~40 lines:

```csharp
log.Add(new SkillUsed(Actor.Id, Skill.Id, Target.Id));
Actor.PutOnCooldown(Skill);            // missing still costs you the turn

if (Skill.DealsDamage)
{
    bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);
    int damage = DamageCalculator.Compute(
        Actor.CurrentStats, Target.CurrentStats, Skill.Power, isCritical);

    int applied = Target.TakeDamage(damage);
    log.Add(new Damaged(Target.Id, applied, isCritical, SourceId: Actor.Id));

    if (Skill.Drains)                   // life steal
    {
        int drained = Actor.Heal(applied * Skill.LifestealPercent / 100);
        if (drained > 0) log.Add(new Healed(Actor.Id, drained));
    }
}

if (Skill.Heals)                        // separate "if", not "else if" -
{                                       // a skill can do both
    int healed = Target.Heal(Skill.Healing);
    if (healed > 0) log.Add(new Healed(Target.Id, healed));
}

if (Skill.AppliesStatus is { } status && Target.IsAlive)
{
    Target.ApplyStatus(status, Skill.StatusTurns);
    log.Add(new StatusApplied(Target.Id, status.Id, Skill.StatusTurns));
}
```

They differ **only in the numbers on their definition**. That is
[chapter 11](11-content-as-data.md), and it is the reason this game has 55 skills
instead of the 8 it would have if each needed a class.

Three small decisions worth noticing:

- **`if (Skill.Heals)` is a separate `if`, not an `else if`.** A skill can damage
  *and* heal. Life-drain sets both.
- **The status only lands if the target survived.** Poisoning a corpse produces
  baffling combat logs.
- **Life-drain uses `applied`, not `damage`.** Draining a nearly-dead target
  heals very little, because overkill was already thrown away by `TakeDamage`
  ([chapter 5](05-state-and-entities.md)).

> **The rule to internalise:** if you are about to write
> `class FireballAction : IAction`, stop and ask whether the skill *definition*
> needs one more data field instead. Nine times out of ten it does. The tenth
> time is when you are adding a genuinely new mechanic that every skill can then
> opt into — and then you add it *here*, once.

---

## What it costs you

**Round-based caps your design.** Speed can never grant an extra turn, so it will
always be a weaker stat than Attack. That is a real ceiling, and moving to ATB
later means rebalancing every character.

**One `Execute` for everything grows.** Right now it handles damage, drain, heal
and status in forty readable lines. Add stuns-on-hit, chains, area damage,
reflect and summons, and it becomes a 300-line method of special cases. At that
point you split it — but split it by *mechanic*, not by *skill*.

**The Command pattern allocates.** `LegalActions` builds a fresh list of objects
every time it is called, and the AI calls it for every actor on every turn.
Irrelevant at this scale. In a game with 200 units it would need pooling.

---

## Try it

**1. Change the model of time.** In `TurnQueue`, remove
`.OrderByDescending(a => a.CurrentStats.Speed)`. Now turn order is just list
order. Run the balance harness and watch the numbers move — you have just deleted
a whole stat.

**2. Tick statuses first.** In `TakeTurn`, move `TickStatuses` above
`action.Execute`. Run the tests. `PoisonDamagesAtTheEndOfEachTurnAndThenWearsOff`
fails, and a 1-turn poison now expires without ever dealing damage.

**3. Add an action.** Try `DefendAction` — an action that applies a `guard`
status to yourself and costs a turn. You will find you only need to touch
`LegalActions` and one new file, which is the point of the pattern.

---

**Next:** [Chapter 9 — Numbers: damage and stat design](09-numbers-and-stat-design.md)

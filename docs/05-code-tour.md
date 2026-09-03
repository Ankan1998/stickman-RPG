# 5. Code tour

Every file, what it does, and why it exists. Roughly in the order that makes
sense to read them.

Total: about 5,600 lines, and a lot of that is comments.

---

## Reading order

If you want to actually understand the codebase rather than skim it, read the
files in this order. Each one only depends on the ones above it.

```
 1. StatBlock.cs        the numbers
 2. Team.cs             which side you are on
 3. StatusDefinition.cs what poison IS
 4. StatusEffect.cs     poison ON someone
 5. SkillDefinition.cs  what a skill IS
 6. Actor.cs            a combatant
 7. IRandomSource.cs    dice
 8. GameEvent.cs        the vocabulary of "what happened"
 9. DamageCalculator.cs the one damage formula
10. IAction.cs          "something you can do"
11. SkillAction.cs      using a skill
12. TurnQueue.cs        who goes next
13. BattleState.cs      the fight's contents
14. Battle.cs           ★ the heart of everything
15. ScoringAi.cs        how monsters choose
16. ContentDatabase.cs  the actual game content
17. Statuses.cs         poison, burning, chill, curse, buffs
18. Skills.cs           every ability, hero and monster
19. Heroes.cs           the ten you can recruit
20. Monsters.cs         the twenty-two that want you dead
21. Weapons.cs          the forty-seven pieces of loot
22. ThreatModel.cs      how dangerous each hero looks to the enemy
23. RunStats.cs         the scoreboard, built from the event log
24. DungeonDefinition.cs  the three dungeons and their loot tables
25. Campaign.cs         ★ hub -> dungeon -> hub, wounds and all
26. UiTheme.cs          the look of every button and panel
27. SpriteAnimator.cs   plays a strip of frames
28. Audio.cs            the sound bank
29. EffectOverlay.cs    the impact animations
30. ActorView.cs        one fighter on screen
31. BattleView.cs       one encounter on screen
32. GameRoot.cs         title -> camp -> dungeons -> results
```

---

# `src/Rpg.Core/` — the rules

## `Entities/StatBlock.cs`

**The five numbers that describe how good someone is at fighting.**

```csharp
public readonly record struct StatBlock(
    int MaxHealth, int Attack, int Defense, int Speed, int CritChance)
```

| Stat | Effect |
|---|---|
| `MaxHealth` | How much damage you survive |
| `Attack` | How hard you hit |
| `Defense` | Reduces incoming damage (by half its value) |
| `Speed` | Decides turn order, highest first |
| `CritChance` | Percent chance to double your damage |

The clever bit is the `+` operator:

```csharp
public static StatBlock operator +(StatBlock a, StatBlock b) => new(...)
```

That is what lets buffs stack by simple addition. `Actor.CurrentStats` is
literally `BaseStats + every active status modifier`.

`Clamped()` stops debuffs pushing values negative, and keeps `CritChance` inside
0–100.

**To add a new stat** (Accuracy, Evasion, Resistance...): add one parameter here
and one line in the `+` operator. That is the entire cost, and it is deliberate.

---

## `Entities/Team.cs`

Twelve lines. `Heroes` or `Monsters`, plus an `Opposite()` helper so
`TargetsFor` can ask "who are this actor's enemies?" without an `if`.

---

## `Effects/StatusDefinition.cs`

**What a status effect *is* — the template.**

There is no `class Poison` and no `class Stun`. A status is data:

```csharp
public sealed record StatusDefinition(
    string Id, string Name, string Description,
    StatBlock Modifier,          // added to your stats while active
    int DamagePerTurn = 0,       // poison, burning, bleed
    bool PreventsAction = false, // stun, freeze, sleep
    string Icon = "*");
```

Three fields cover a surprising amount:

| Status | How it is expressed |
|---|---|
| Poison | `DamagePerTurn: 4` |
| Stun | `PreventsAction: true` |
| Guard (defence buff) | `Modifier: new StatBlock(0, 0, 6, 0, 0)` |
| Haste | `Modifier: new StatBlock(0, 0, 0, 20, 0)` |
| Curse (weaken) | `Modifier: new StatBlock(0, -5, 0, 0, 0)` |

This is the **Type Object** pattern. It will eventually run out of expressiveness
— around the time you want "on being hit, reflect 20% damage" — and the upgrade
then is to give the definition a list of triggers. Not before.

---

## `Effects/StatusEffect.cs`

**Poison *on a specific actor*, as opposed to the idea of poison.**

The distinction matters: `StatusDefinition` is shared and immutable; there is one
"poison" in the whole game. `StatusEffect` is per-actor and mutable, because it
tracks how many turns are left.

```csharp
public void Refresh(int turns) => RemainingTurns = Math.Max(RemainingTurns, turns);
public void Tick() => RemainingTurns--;
```

`Refresh` implements a design decision: **re-applying a status refreshes its
duration rather than stacking a second copy.** If you want stacking poison,
this is where you would add a `Stacks` field.

---

## `Content/SkillDefinition.cs`

**What a skill is.** Same idea as `StatusDefinition` — data, not a class per
skill.

```csharp
public sealed record SkillDefinition(
    string Id, string Name, string Description,
    TargetKind Target,               // enemy / ally / self
    int Power = 0,                   // damage, as % of Attack
    int Healing = 0,
    StatusDefinition? AppliesStatus = null,
    int StatusTurns = 0,
    int Cooldown = 0);
```

`Power` is a **percentage of the user's Attack**, not a flat number:

- `Power: 100` — a normal hit
- `Power: 180` — Heavy Blow, hits for 1.8× your Attack
- `Power: 40` — Poison Dart, weak hit but it applies poison
- `Power: 0` — deals no damage at all (Bandage, Guard)

Percentages rather than flat numbers mean a skill stays relevant as the character
levels up.

Also defines `TargetKind`: `SingleEnemy`, `SingleAlly`, `Self`.

---

## `Entities/Actor.cs`

**A combatant.** Heroes and monsters are the same class — the difference is
data.

The important parts:

```csharp
public int Health { get; private set; }
```

`private set` means nothing outside `Actor` can write to Health. Damage *must*
go through `TakeDamage()`, which clamps it so you can never go below 0. Healing
must go through `Heal()`, which cannot overheal and cannot revive the dead.

```csharp
public StatBlock CurrentStats
{
    get
    {
        StatBlock total = BaseStats;
        foreach (StatusEffect status in _statuses)
            total += status.Definition.Modifier;
        return total.Clamped();
    }
}
```

Recomputed on every single read. This is why buffs work everywhere with no code
remembering to recalculate. **Combat always reads `CurrentStats`, never
`BaseStats`** — reaching for `BaseStats` outside character creation is usually a
bug where a buff is being silently ignored.

Cooldowns live here too, in a small dictionary:

```csharp
private readonly Dictionary<string, int> _cooldowns = new();
```

`TickCooldowns()` is called when the actor's turn *begins*, and `PutOnCooldown()`
when they use a skill. Both are `internal` — the Godot layer literally cannot
call them.

---

## `Rng/IRandomSource.cs`, `SplitMix64Random.cs`, `FixedRandom.cs`

**All dice rolls, made repeatable.**

`IRandomSource` is deliberately tiny — two methods. Convenience helpers
(`Chance`, `Range`, `Pick`) are extension methods, so implementing the interface
stays trivial.

`SplitMix64Random` is the real one. Fifteen lines, seeded, identical on every
platform and every .NET version. Deliberately not `System.Random`, whose
algorithm Microsoft has changed between releases.

`FixedRandom` is the test double. You hand it the numbers you want:

```csharp
new FixedRandom(0)     // NextInt always returns the minimum
```

This is how a test can assert exact damage numbers without fighting randomness.
Note a subtlety used throughout the tests: `Chance(percent)` is written as

```csharp
percent > 0 && rng.NextInt(0, 100) < percent
```

so an actor with `CritChance: 0` never touches the RNG at all. Set crit to zero
in a test and every number becomes exact.

---

## `Combat/GameEvent.cs`

**The vocabulary for describing what happened.** This is the bridge between the
rules and the screen.

```csharp
public abstract record GameEvent;

public sealed record BattleStarted : GameEvent;
public sealed record RoundStarted(int Round) : GameEvent;
public sealed record TurnStarted(string ActorId) : GameEvent;
public sealed record SkillUsed(string ActorId, string SkillId, string TargetId) : GameEvent;
public sealed record Damaged(string ActorId, int Amount, bool IsCritical,
                             string? SourceId = null, string? StatusId = null) : GameEvent;
public sealed record Healed(string ActorId, int Amount) : GameEvent;
public sealed record StatusApplied(string ActorId, string StatusId, int Turns) : GameEvent;
public sealed record StatusTicked(string ActorId, string StatusId, int RemainingTurns) : GameEvent;
public sealed record StatusExpired(string ActorId, string StatusId) : GameEvent;
public sealed record TurnSkipped(string ActorId, string Reason) : GameEvent;
public sealed record Died(string ActorId) : GameEvent;
public sealed record BattleEnded(Team? Winner) : GameEvent;
```

Two design notes worth internalising:

**They carry ids, not object references.** `Damaged("monster_goblin", 25, true)`,
not `Damaged(goblinObject, 25, true)`. That keeps events serialisable — you can
write them to a file or send them over a network. It is the difference between
"we could add replays later" and "we could not".

**They are records**, so a list of them compares by value. That is what makes
this test possible:

```csharp
Assert.Equal(BattleRunner.Run(4242).Log, BattleRunner.Run(4242).Log);
```

---

## `Combat/DamageCalculator.cs`

**Every damage number in the game comes from this one method.**

```csharp
public static int Compute(StatBlock attacker, StatBlock defender, int power, bool isCritical)
{
    int raw = attacker.Attack * power / 100;
    int mitigated = raw - defender.Defense / 2;

    if (isCritical)
        mitigated = mitigated * CriticalMultiplierPercent / 100;

    return Math.Max(MinimumDamage, mitigated);
}
```

Worked example — Warrior (Attack 15) uses Heavy Blow (Power 180) on the Brute
(Defense 7):

```
raw       = 15 * 180 / 100  = 27
mitigated = 27 - (7 / 2)    = 27 - 3 = 24        (integer division: 7/2 = 3)
critical? = no
result    = 24
```

Two decisions baked in here:

**Defence subtracts, it does not divide.** Easy to reason about and easy to
explain to a player, but it scales badly — at very high numbers, defence either
trivialises damage or does nothing. If your endgame stats reach the thousands,
revisit this. That is a design decision, not a bug.

**`MinimumDamage = 1`.** However tanky the defender, a hit does at least 1. Without
this, a high-defence actor becomes literally unkillable and battles hang forever.
There is a test for it: `DamageNeverDropsBelowOne`.

---

## `Combat/IAction.cs`, `SkillAction.cs`, `PassAction.cs`

**"Something an actor chose to do."** The Command pattern.

```csharp
public interface IAction
{
    Actor Actor { get; }
    string Label { get; }                                  // for menus and logs
    void Execute(BattleState state, List<GameEvent> log);
}
```

The player picks one from a menu; the AI scores them and picks the best. Both
funnel into the same `Battle.TakeTurn()`.

**`SkillAction` executes *any* skill.** There is deliberately only one:

```csharp
public void Execute(BattleState state, List<GameEvent> log)
{
    log.Add(new SkillUsed(Actor.Id, Skill.Id, Target.Id));
    Actor.PutOnCooldown(Skill);

    if (Skill.DealsDamage)
    {
        bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);
        int damage = DamageCalculator.Compute(
            Actor.CurrentStats, Target.CurrentStats, Skill.Power, isCritical);
        int applied = Target.TakeDamage(damage);
        log.Add(new Damaged(Target.Id, applied, isCritical, SourceId: Actor.Id));
    }

    if (Skill.Heals) { ... }
    if (Skill.AppliesStatus is { } status && Target.IsAlive) { ... }
}
```

Note the comment about ordering: the crit is rolled *before* damage is computed,
and always in that order. Consuming the RNG in a fixed order is what makes a seed
reproduce a battle exactly. Reorder those lines and every saved replay breaks.

Also note `&& Target.IsAlive` on the status — you cannot poison a corpse. Small
detail; produces much saner combat logs.

**`PassAction` does nothing.** It exists so `LegalActions()` can never return an
empty list. An engine that deadlocks because every skill happened to be on
cooldown is an engine you debug at 2am.

---

## `Combat/TurnQueue.cs`

**Who acts, and in what order.**

Round-based: everyone alive acts once per round, fastest first.

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
    while (++_index < _order.Count)
        if (_order[_index].IsAlive) return true;
    return false;
}
```

`MoveNext()` re-checks `IsAlive` because an actor can die *during* the round,
after the order was decided. Returns `false` when the round is exhausted.

**Upgrade path:** replace this with an ATB / "action gauge" system, where each
actor accumulates Speed points per tick and acts on crossing a threshold. Fast
actors then genuinely get *extra* turns rather than merely acting first. Nothing
outside this class needs to change — which is exactly why turn order lives behind
its own type instead of being a `for` loop inside `Battle`.

---

## `Combat/BattleState.cs`

**A dumb container: who is in the fight, and the dice.**

Thirty lines. It holds the actor list and the `IRandomSource`, and offers three
queries (`GetActor`, `LivingMembersOf`, `IsTeamWipedOut`).

The rules live in `Battle`, not here. The one piece of logic it does have is a
constructor check that no two actors share an id — because events reference
actors by id, duplicate ids would silently corrupt everything downstream.

---

## `Combat/Battle.cs` ★

**The heart of the project.** Read this one properly.

Public surface:

```csharp
public List<GameEvent> Start();
public List<IAction>   LegalActions(Actor actor);
public List<GameEvent> TakeTurn(IAction action);

public Actor? Current { get; }
public Team?  Winner  { get; }
public bool   IsOver  { get; }
public int    Round   { get; }
```

`TakeTurn` is the whole game in five steps:

```csharp
// 1. Act - unless something is stopping this actor (stun, sleep...)
if (actor.CanAct) action.Execute(State, log);
else log.Add(new TurnSkipped(actor.Id, actor.BlockedReason ?? "Unable to act"));

ReportDeaths(log);

// 2. End-of-turn statuses: poison damage, durations counting down.
//    Deliberately AFTER acting, so a 1-turn poison still deals damage once.
TickStatuses(actor, log);
ReportDeaths(log);

// 3. Is anyone left?
if (CheckForEnd(log)) return log;

// 4. Next.
AdvanceToNextTurn(log);
return log;
```

Three details that took thought:

**`ReportDeaths` is centralised.** An actor can die from a sword, from poison, or
later from a reflected hit. If each of those logged its own `Died` event, you
would eventually double-report one and the UI would play the death animation
twice. A `HashSet<string>` remembers who has already been reported.

**`TickStatuses` iterates a copy** (`actor.Statuses.ToList()`), because expiring a
status removes it from the collection being iterated.

**`MaxRounds = 100`.** A safety valve. Two actors who cannot hurt each other must
not loop forever; at the limit the battle is declared a draw. There is a test
asserting real fights finish well inside it.

`LegalActions` is worth reading closely — it is the single source of truth that
both the menu and the AI consume:

```csharp
var actions = new List<IAction>();

if (actor.CanAct)
    foreach (SkillDefinition skill in actor.Skills)
        if (actor.IsSkillReady(skill.Id))
            foreach (Actor target in TargetsFor(actor, skill))
                actions.Add(new SkillAction(actor, skill, target));

actions.Add(new PassAction(actor));   // always legal
return actions;
```

---

## `Ai/ScoringAi.cs`

**How monsters decide.** One-ply scoring: look at every legal action, give it a
number, take the highest.

All the weights are in one block at the top, deliberately:

```csharp
public const double LethalBonus = 60.0;
public const double DamageWeight = 1.0;
public const double HealWeight = 1.3;
public const double EmergencyHealBonus = 25.0;
public const double EmergencyHealthFraction = 0.35;
public const double StunValue = 16.0;
public const double DamageOverTimeWeight = 0.6;
public const double BuffValue = 10.0;
public const double PassValue = 0.1;
```

**Treat these as game design values, not code.** As documented in
[the architecture page](04-architecture.md#2-measuring-instead-of-guessing),
changing `DamageOverTimeWeight` from `0.9` to `0.6` moved the encounter's win
rate further than a whole round of stat changes did.

The scoring itself:

- **Damage** — score the *useful* damage, `Math.Min(expected, target.Health)`.
  Overkill is wasted; 40 damage into a 6 HP goblin is worth 6, not 40.
- **Lethal** — a huge bonus for a killing blow, because removing an actor removes
  all their future turns.
- **No gambling on crits** — expected damage is computed with
  `isCritical: false`. Scoring the best case would make the AI chase
  low-probability crits, which reads as stupid.
- **Never re-apply an active status** — returns early if the target already has
  it.
- **Deterministic tie-breaking** by label, never by list order.

Two upgrade paths are written into the file's comments:

1. **Search.** Give `BattleState` a `Clone()`, apply the action to the clone, and
   score the resulting *position* instead of the action. That turns this into a
   real minimax and is the honest way to get a smart opponent.
2. **Personality.** Multiply the weights per enemy type — a berserker weights
   damage ×2 and healing ×0; a shaman weights statuses ×3. Cheap, and it makes
   encounters feel authored rather than generated.

---

## `Content/ContentDatabase.cs`

**The actual game.** Every skill, status and stat line lives here.

Three statuses:

| Id | Effect |
|---|---|
| `poison` | 4 damage per turn |
| `stun` | Cannot act |
| `guard` | +6 Defense |

Eight skills:

| Id | Target | Power | Notes |
|---|---|---|---|
| `slash` | Enemy | 100 | Plain attack |
| `heavy_blow` | Enemy | 180 | 2-turn cooldown |
| `poison_dart` | Enemy | 40 | Applies poison, 3 turns |
| `jab` | Enemy | 80 | Weak attack |
| `bandage` | Ally | — | Heals 14, 2-turn cooldown |
| `guard` | Self | — | +6 Defense for 2 turns |
| `club` | Enemy | 100 | Monster attack |
| `headbutt` | Enemy | 70 | Stuns for 1 turn, 3-turn cooldown |

And four actors:

| | HP | Atk | Def | Spd | Crit | Skills |
|---|---|---|---|---|---|---|
| Stick Warrior | 70 | 15 | 9 | 10 | 10% | slash, heavy_blow, guard |
| Stick Medic | 52 | 9 | 6 | 12 | 5% | jab, bandage, poison_dart |
| Goblin | 44 | 14 | 4 | 14 | 8% | club, poison_dart |
| Goblin Brute | 72 | 18 | 7 | 7 | 8% | club, headbutt |

Those numbers are **measured, not guessed**: 74.9% hero win rate over 1000
battles, averaging 6.7 rounds.

**Why content is C# right now:** it compiles, it is type-checked, and you can
Ctrl-click a skill to jump to its definition. That is genuinely valuable while
learning. It stops scaling around fifty skills, because at that point you should
not have to recompile to change a damage number — which is why moving this to
JSON is week 6 of [the roadmap](roadmap.md).

---

# `game/` — the screen

## `project.godot`

Godot's config file. Names the game, points at the startup scene, sets the window
size, and tells Godot the C# assembly is called `StickmanRpg.Game`.

Godot writes this itself; you rarely edit it by hand.

## `scenes/Battle.tscn`

Eleven lines. One `Control` node, filling the window, with `GameRoot.cs`
attached. Everything else is built in code — see
[the Godot crash course](03-godot-crash-course.md#why-is-our-scene-so-empty) for
why, and why you should eventually change that.

## `scripts/UiTheme.cs`

**The look of every button, panel and label.** Godot styles UI through a Theme -
a lookup table of "for a Button, the normal background is THIS image". Set it
once on the root node and every child inherits it.

It builds `StyleBoxTexture`s from the generated nine-slice PNGs, so a 24x24
button image stretches to any size with its 1px border intact. The palette is
deliberately the same as `tools/make_art.py`, so the interface and the world look
like one thing.

## `scripts/ActorView.cs`

**One fighter on screen** - sprite, name, health bar, status icons, plus the
reactions that make a turn-based game feel alive: a red flash and shake when hit,
a green pulse when healed, a glow on whoever is acting, and a topple to the
`_down` sprite on death.

This replaced an earlier `StickmanView` that drew a stick figure with `DrawLine`.
Notice how little else changed when the art arrived: the rules never knew what a
stickman looked like, so it was a one-file swap.

It holds an `Actor` and only ever **reads** it - it could not modify one if it
tried, because `Health` has a private setter and the mutating methods are
`internal`.

## `scripts/FloatingNumber.cs`

**The damage numbers that pop up and drift away.** About forty lines, changes
nothing about the game, and does more for how a hit *feels* than any amount of
rules work. Frees itself when the animation ends.

## `scripts/BattleView.cs`

**One wave, on screen.** Layout, the move menu, and replaying the event log with
animation and narration. It calculates nothing.

The menu is **two steps on purpose**: pick a skill, then pick a target. Three
heroes with three skills against three monsters is up to ten legal actions, and
ten buttons is a wall nobody reads. Target buttons show the exact consequence -
`Goblin A (24 dmg)` - because guessing is not strategy.

Both steps are still built from `Battle.LegalActions()`; they only *group* that
list, never add to it.

## `scripts/GameRoot.cs`

**The shell.** `TITLE -> wave 1 -> wave 2 -> wave 3 -> RESULTS`. Owns the `Run`,
swaps screens, and builds the title and results pages.

Also holds a small **screenshot harness** (`godot --path game -- --shots`) that
walks all three screens and saves a PNG of each. It reaches the results screen by
simulating a whole run in a few lines - possible only because the rules do not
need the engine.

---

## `Content/Statuses.cs`, `Skills.cs`, `Heroes.cs`, `Monsters.cs`, `Weapons.cs`

**All the game's content**, split by kind because one file holding ten heroes,
twenty-two monsters, fifty skills and forty-seven weapons is a file nobody can
find anything in.

`Weapons.cs` is worth a look for the pattern: each of the 47 weapons declares
only a **rarity** (how much power it gets) and an **archetype** (how it spends
it), and `WeaponDefinition` computes the stats from two small tables. So no
weapon can accidentally be weaker than a lower rarity, and rebalancing every
piece of loot in the game is a two-table edit.

## `Progression/DungeonDefinition.cs`

The three dungeons: name, mood, two-to-three encounters each, and a loot table
weighted by depth. Data, so adding a fourth is one entry and no code.

## `Progression/Campaign.cs` ★

**Hub → dungeon → hub → dungeon → results.** Owns the party, the loot, and the
two rules that make it a game: wounds carry between encounters, and only camp
clears them.

Worth reading alongside `Battle.cs` — the same "return a list of events, keep
score, decide nothing about the screen" shape, one level up.

## `Ai/ThreatModel.cs`

**"Which of these three should we kill first?"** Rates each hero on what they
*do* rather than how easy they are to hit, so the enemy goes for the healer and
finishes anyone already wounded.

About forty lines, and it completely changes how a fight reads — the party
suddenly has to protect somebody.

---

# `game/` — the screen

## `scripts/SpriteAnimator.cs`

Plays a horizontal strip of frames using an `AtlasTexture` window. Five clips per
character: idle, walk, attack, hurt, death.

## `scripts/Audio.cs`

The sound bank, plus `Sfx` — the table mapping weapon archetypes to impact
sounds, voice families to hurt cries, and statuses to magic effects. Every sound
ships as three takes and one is picked at random, so a mace landing three times
does not sound like a machine gun.

## `scripts/EffectOverlay.cs`

The 32x32 impact animations — a slash crescent, a splash of blood, a puff of
poison. Plays over the target, then frees itself.

## `scripts/DungeonBackdrop.cs`

Stitches 16x16 dungeon tiles into one 320x180 room image at load time, so each
dungeon has its own walls and floor without a hand-made background.

# `tools/` — the art

## `pixelart.py`

A PNG writer and a drawing canvas in about 250 lines, with no dependencies. PNG
turns out to be simple enough to write by hand with `zlib` and `struct`.

The three post-processing passes do most of the work: `outline()` adds the dark
border that makes pixel art read, `shade()` fakes volume, and
`desaturated().rotated_cw()` generates every defeated sprite automatically from
the living one.

## `make_art.py`

**One function per asset.** Characters are assembled from discs, lines and rects,
then finished with shading and an outline. Produces all 24 PNGs in
`game/assets/`.

```bash
python tools/make_art.py --preview
```

renders them as ASCII in the terminal, which is how the sprites were checked
without an image viewer. Full details in [the art pipeline](09-art-pipeline.md).

---

# `src/Rpg.Core.Tests/` — the checks

## `TestFixtures.cs`

Small builders so each test reads as one idea. Also contains `BattleRunner`,
which plays a complete battle with AI on both sides — five lines, and the reason
a thousand battles can be simulated in 95 milliseconds.

## `DamageCalculatorTests.cs`

Four tests on the damage formula in isolation. No actors, no battle, no engine.

## `CombatRulesTests.cs`

Nine tests on rules a player would complain about if you broke them: poison
ticking and expiring, stun losing a turn, cooldowns blocking reuse, the battle
ending when a team is wiped, acting out of turn being rejected, healing not
overhealing or reviving the dead, buffs affecting `CurrentStats` but not
`BaseStats`, and statuses refreshing rather than stacking.

## `TurnOrderTests.cs`

Four tests: descending speed, deterministic tie-breaks, speed buffs actually
changing the order (proving the queue reads `CurrentStats`), and an actor killed
mid-round being skipped.

## `BalanceHarnessTests.cs`

The payoff file. Five tests, including the 1000-battle harness and the
determinism checks. See
[architecture](04-architecture.md#2-measuring-instead-of-guessing).

---

## Next

[Anatomy of a turn](06-anatomy-of-a-turn.md) — one turn, traced end to end.

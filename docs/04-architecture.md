# 4. How it all fits together

One idea shapes every file in this repository. This page explains it, why it
exists, and what it buys you.

---

## The obvious way to build this (and why it hurts)

If you sat down to write a turn-based battle without thinking about structure,
you would probably write something like:

```csharp
void OnAttackButtonPressed()
{
    int damage = attacker.Attack - target.Defense / 2;
    target.Health -= damage;

    ShowDamageNumber(target, damage);          // draw
    await PlayHitAnimation(target);            // wait
    healthBar.Value = target.Health;           // draw

    if (target.Health <= 0)
    {
        await PlayDeathAnimation(target);      // wait
        actors.Remove(target);
    }

    NextTurn();
}
```

This works. It is also the version that quietly ruins the project around month
three. Here is why:

**You cannot test it.** To check whether poison correctly stops ticking when the
target dies, you must launch the game, start a fight, apply poison, and watch.
Every time. Forever.

**You cannot balance it.** "Is this boss too hard?" can only be answered by
playing the boss twenty times. With a hundred skills interacting, you will never
have enough hours.

**The rules leak everywhere.** Six months in, damage is calculated in four
different files, and nobody knows which one runs for a critical hit from a
poisoned attacker.

**The animation and the rules are welded together.** Want a fast-forward button?
A replay? An undo? Multiplayer? Each one now requires rewriting combat.

---

## What this project does instead

Split the game into two halves with a strict one-way relationship:

```
┌──────────────────────────────────────────────────────────────┐
│  game/           THE PRESENTATION                            │
│                                                              │
│  Draws stick figures. Shows buttons. Reads clicks.           │
│  Plays a list of events back slowly with pauses.             │
│                                                              │
│  Knows about: Godot, pixels, colours, timing, input          │
│  Knows about: Rpg.Core  ────────────────┐                    │
└─────────────────────────────────────────┼────────────────────┘
                                          │  allowed
                                          ▼
┌──────────────────────────────────────────────────────────────┐
│  src/Rpg.Core/   THE RULES                                   │
│                                                              │
│  Damage. Turn order. Poison. Death. Cooldowns. AI.           │
│  Decides everything. Renders nothing.                        │
│                                                              │
│  Knows about: numbers, actors, skills                        │
│  Knows about Godot: NOTHING. Not one line.        ✗ FORBIDDEN│
└──────────────────────────────────────────────────────────────┘
```

**The rule: `game/` may depend on `Rpg.Core`. `Rpg.Core` may never depend on
`game/`, or on Godot, or on anything visual.**

If you ever need to write `using Godot;` inside `src/Rpg.Core/`, something has
gone wrong — the code you are writing belongs on the other side of the line.

### This is enforced, not merely encouraged

Look at [`Rpg.Core.csproj`](../src/Rpg.Core/Rpg.Core.csproj). It is nine lines
and references nothing. There is no Godot package to accidentally use. The rules
project physically *cannot* draw a pixel — it does not have the ability.

The dependency is declared once, in
[`StickmanRpg.Game.csproj`](../game/StickmanRpg.Game.csproj):

```xml
<ProjectReference Include="..\src\Rpg.Core\Rpg.Core.csproj" />
```

One arrow, pointing one way.

---

## The bridge: a list of events

The two halves have to talk somehow. Here is the trick that makes it work.

When you click a button, the rules engine resolves the **entire** turn
immediately — damage, deaths, poison ticks, status expiry, whose turn is next —
in microseconds. It does not draw anything. Instead it returns a **list of things
that happened**:

```csharp
List<GameEvent> whatHappened = battle.TakeTurn(action);
```

That list might be:

```
SkillUsed("hero_warrior", "heavy_blow", "monster_goblin")
Damaged("monster_goblin", 25, IsCritical: true)
Died("monster_goblin")
TurnStarted("monster_brute")
```

The Godot layer then **replays that list**, one item at a time, with a pause
between each, drawing as it goes.

```
    time ──────────────────────────────────────────────►

    │
    │ battle.TakeTurn()        ← the entire turn resolves here.
    │ ▓                           ~20 microseconds.
    │
    │ then the screen replays it:
    │
    │   ░░░░░░░ "Warrior uses Heavy Blow"
    │           ░░░░░░░ "25 damage (critical!)"
    │                   ░░░░░░░ "Goblin goes down."
    │                           ░░░░░░░ next turn...
    │
    │ ◄──────── ~1.8 seconds of animation ────────►
```

**The fight is already decided before the first animation plays.** The screen is
a player of recordings. Nothing on screen can change the outcome.

This feels strange for about ten minutes, and then it feels obviously correct.

---

## What that one decision buys you

### 1. You can test the entire game

Because resolving a turn involves no drawing and no waiting, a test can play a
complete battle instantly:

```csharp
var battle = new Battle(content.CreateDemoBattle(seed: 42));
battle.Start();
while (!battle.IsOver)
    battle.TakeTurn(ScoringAi.ChooseAction(battle, battle.Current!));
```

That is the whole game loop, in five lines, with no engine running. It is the
core of [`BattleRunner`](../src/Rpg.Core.Tests/TestFixtures.cs), and it is why
this project has **21 tests that run in 0.14 seconds**.

### 2. Measuring instead of guessing

This is the payoff that matters most for a "deep" RPG.

[`BalanceHarnessTests`](../src/Rpg.Core.Tests/BalanceHarnessTests.cs) plays
**1000 complete battles** with the AI controlling both sides, and reports:

```
Battles simulated : 1000
Hero wins         : 749 (74.9%)
Monster wins      : 251
Draws (hit round limit) : 0
Average length    : 6.7 rounds
```

That takes about 95 milliseconds.

**This is not a demo. It is how the fight in this repo was actually balanced.**
The first version of the encounter measured:

| | Win rate | Rounds |
|---|---|---|
| First draft | **100.0%** | 3.8 |

100% is not a slightly-too-easy fight — it is a fight the player never has to
think about. The harness also revealed *why*: the goblin's AI was choosing a
Poison Dart that dealt **2** damage over a Club that dealt **10**, because its
`DamageOverTimeWeight` valued delayed damage almost at face value. In a fight
lasting six rounds, that is simply wrong.

Three measure-tune-remeasure passes later:

| Change | Win rate | Rounds |
|---|---|---|
| First draft | 100.0% | 3.8 |
| Raised everyone's stats | 99.9% | 5.7 |
| **Fixed the AI weights + nerfed healing** | 91.4% | 6.6 |
| Monster Attack +2 | 59.9% | 7.6 |
| Monster Attack −1 | **74.9%** | **6.7** |

Notice the third row. Fixing one AI weight moved the number **eight times
further** than a whole round of stat inflation did. You would never have found
that by playing the game. You would have concluded "the goblins need more damage"
and been wrong.

And now that intent is locked in as a test:

```csharp
Assert.InRange(winRate, 0.60, 0.85);
```

If a future change to any skill, stat, or AI weight breaks that promise, the
build fails and tells you.

### 3. The AI cannot cheat

Both the player's menu and the AI read the *same* list:

```csharp
List<IAction> options = battle.LegalActions(actor);
```

The UI turns each option into a button. The AI scores each option and picks one.
There is no separate "monster logic" that could accidentally ignore a cooldown or
target a dead actor. If the AI can do it, you can do it.

This is worth protecting. It is the difference between a game that feels hard and
a game that feels rigged.

### 4. Features you have not thought of yet become easy

Because the event list *is* a complete record of the battle:

| Feature | How it works |
|---|---|
| Combat log | Already done — that is what `Describe()` does |
| Replays | Save the event list to a file, play it back |
| Fast-forward | Replay with a shorter delay |
| Undo | Keep snapshots, re-run events up to point N |
| Networked play | Send the action, both sides run the same rules with the same seed |

None of these need combat to be rewritten, because combat does not know the
screen exists.

---

## Determinism: the same seed always plays out the same

Every dice roll in the game goes through
[`IRandomSource`](../src/Rpg.Core/Rng/IRandomSource.cs), which is handed to the
battle when it is created:

```csharp
new BattleState(actors, new SplitMix64Random(seed));
```

There is no global random anywhere. That gives you:

- **Reproducible tests.** Seed 4242 always produces the exact same battle, so a
  test can assert on it.
- **Reproducible bug reports.** A player sends you a seed; you see their fight.
- **Seeded runs**, roguelike-style, for free.
- **Replays and networking**, later.

Two details that look fussy but are load-bearing:

**We do not use `System.Random`.** Microsoft has changed its internal algorithm
between .NET versions. If your saved replays depend on the exact sequence of
numbers, you want an algorithm you control.
[`SplitMix64Random`](../src/Rpg.Core/Rng/SplitMix64Random.cs) is about fifteen
lines and produces identical output on every platform, forever.

**Ties break deterministically.** In
[`TurnQueue`](../src/Rpg.Core/Combat/TurnQueue.cs):

```csharp
.OrderByDescending(a => a.CurrentStats.Speed)
.ThenBy(a => a.Id, StringComparer.Ordinal)
```

Two actors with equal Speed are ordered by id, never by their position in a list.
If ties fell back to list order, then loading a save that rebuilt the list in a
different order would replay the battle differently — a horrible bug to track
down.

There is a test guarding exactly this:
`EqualSpeedsBreakOnIdSoTheOrderIsReproducible`.

---

## Content is data, not code

The last structural idea. A skill is not a class:

```csharp
new SkillDefinition("slash", "Slash", "A reliable swing.",
    TargetKind.SingleEnemy, Power: 100),

new SkillDefinition("heavy_blow", "Heavy Blow", "Slow, but it hurts.",
    TargetKind.SingleEnemy, Power: 180, Cooldown: 2),

new SkillDefinition("poison_dart", "Poison Dart", "Weak hit, lingering damage.",
    TargetKind.SingleEnemy, Power: 40, AppliesStatus: poison, StatusTurns: 3),
```

Slash, Heavy Blow and Poison Dart are the **same type** with different numbers.
There is exactly one piece of code that executes any of them
([`SkillAction`](../src/Rpg.Core/Combat/SkillAction.cs)), and one that executes
statuses ([`Battle.TickStatuses`](../src/Rpg.Core/Combat/Battle.cs)).

The same applies to actors — there is no `class Goblin`. A goblin is an `Actor`
with goblin numbers and a goblin skill list.

**Why it matters:** adding a skill costs you five numbers, not a new class plus a
new `switch` case plus a new test file. In a genre where the *content* is the
game, that ratio decides whether you finish.

**When to break the rule:** when a mechanic genuinely cannot be expressed as
data — "on being hit, reflect 20% of the damage" needs a new concept, not a new
number. Then you add a *generic* capability to the engine that all skills can
use. Not a special case for one skill.

---

## Summary

| Principle | Where you see it |
|---|---|
| Rules never touch the screen | `Rpg.Core` has zero Godot references |
| Turns resolve instantly, then replay | `TakeTurn()` returns `List<GameEvent>` |
| Randomness is injected and seeded | `IRandomSource`, `SplitMix64Random` |
| The AI plays by the player's rules | Both read `Battle.LegalActions()` |
| Balance is measured, not guessed | `BalanceHarnessTests` |
| Content is data | `SkillDefinition`, `StatusDefinition`, `ContentDatabase` |

---

## Next

[Code tour](05-code-tour.md) — every file, one at a time.

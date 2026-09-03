# 7. Recipes

Copy-paste answers to "how do I actually change this?"

Every recipe says which file to open, what to write, and what to check
afterwards.

---

## Contents

- [Add a new skill](#add-a-new-skill)
- [Add a new status effect](#add-a-new-status-effect)
- [Add a new hero or monster](#add-a-new-hero-or-monster)
- [Add a third monster to the fight](#add-a-third-monster-to-the-fight)
- [Add a new stat](#add-a-new-stat)
- [Change the damage formula](#change-the-damage-formula)
- [Tuning the difficulty](#tuning-the-difficulty)
- [Make the monsters smarter or dumber](#make-the-monsters-smarter-or-dumber)
- [Make healing statuses work (a real bug to fix)](#make-healing-statuses-work-a-real-bug-to-fix)
- [Change the colours and the look](#change-the-colours-and-the-look)
- [Speed up or slow down the animation](#speed-up-or-slow-down-the-animation)
- [Write a new test](#write-a-new-test)
- [Debugging](#debugging)

---

## Add a new skill

**File:** [`src/Rpg.Core/Content/ContentDatabase.cs`](../src/Rpg.Core/Content/ContentDatabase.cs)

**Step 1** — add it to the `skills` array inside `CreateDefault()`:

```csharp
new SkillDefinition("fire_bolt", "Fire Bolt", "A searing bolt of flame.",
    TargetKind.SingleEnemy, Power: 130, Cooldown: 1),
```

**Step 2** — give it to someone, in `CreateDemoBattle()`:

```csharp
var warrior = new Actor(
    id: "hero_warrior",
    name: "Stick Warrior",
    team: Team.Heroes,
    baseStats: new StatBlock(MaxHealth: 70, Attack: 15, Defense: 9, Speed: 10, CritChance: 10),
    skills: new[] { Skill("slash"), Skill("heavy_blow"), Skill("guard"), Skill("fire_bolt") });
    //                                                                   ^^^^^^^^^^^^^^^^^^^
```

**That is it.** You wrote no logic. The skill now:

- appears as a button in the menu, correctly labelled
- can be evaluated and used by the AI
- respects its cooldown
- shows up correctly in the combat log

**Check it:** run `dotnet test`. The balance harness win rate will have moved —
that is the point.

### The parameters

| Parameter | Meaning |
|---|---|
| `Id` | Unique string. Used internally, appears in events. |
| `Name` | Shown to the player. |
| `Description` | Not shown yet — hook it up to a tooltip when you build one. |
| `Target` | `SingleEnemy`, `SingleAlly`, or `Self`. |
| `Power` | Damage as **% of the user's Attack**. `100` = normal, `0` = no damage. |
| `Healing` | Flat healing. Not a percentage. |
| `AppliesStatus` | A `StatusDefinition`, or omit for none. |
| `StatusTurns` | How long that status lasts. |
| `Cooldown` | Turns before reuse. `0` = every turn. |

A skill can do several at once — damage *and* healing *and* a status.

---

## Add a new status effect

**File:** [`src/Rpg.Core/Content/ContentDatabase.cs`](../src/Rpg.Core/Content/ContentDatabase.cs)

**Step 1** — define it inside `CreateDefault()`, next to `poison` and `stun`:

```csharp
var weakened = new StatusDefinition(
    Id: "weakened",
    Name: "Weakened",
    Description: "Hits much more softly.",
    Modifier: new StatBlock(MaxHealth: 0, Attack: -6, Defense: 0, Speed: 0, CritChance: 0),
    Icon: "WEK");
```

**Step 2 — do not skip this.** Add it to the `statuses` array:

```csharp
var statuses = new[] { poison, stun, guard, weakened };
//                                          ^^^^^^^^
```

> ⚠️ **The gotcha.** If you forget step 2, everything compiles and the status
> works perfectly in combat — but the moment the UI tries to name it, you get a
> `KeyNotFoundException: No status with id 'weakened'`. That is
> `BattleView.StatusName()` looking it up in the database. The rules find the
> status through the skill's `AppliesStatus` reference; only the *display* needs
> the lookup table.

**Step 3** — attach it to a skill:

```csharp
new SkillDefinition("hex", "Hex", "Saps the target's strength.",
    TargetKind.SingleEnemy, Power: 20, AppliesStatus: weakened, StatusTurns: 3),
```

### What a status can do

| Field | Gives you |
|---|---|
| `Modifier` | Buffs and debuffs. Negative values weaken. |
| `DamagePerTurn` | Poison, burning, bleed. |
| `PreventsAction` | Stun, freeze, sleep. |

Combine them freely — a status that both weakens and burns is just two fields set.

---

## Add a new hero or monster

**File:** [`src/Rpg.Core/Content/ContentDatabase.cs`](../src/Rpg.Core/Content/ContentDatabase.cs),
in `CreateDemoBattle()`.

```csharp
var archer = new Actor(
    id: "hero_archer",                  // must be unique across the whole battle
    name: "Stick Archer",
    team: Team.Heroes,
    baseStats: new StatBlock(MaxHealth: 44, Attack: 13, Defense: 4, Speed: 16, CritChance: 20),
    skills: new[] { Skill("slash"), Skill("poison_dart") });
```

Then add them to the battle:

```csharp
return new BattleState(
    new[] { warrior, medic, archer, goblin, brute },
    //              ^^^^^^
    new SplitMix64Random(seed));
```

> **Ids must be unique.** `BattleState`'s constructor throws if two actors share
> one, because events reference actors by id and duplicates would silently
> corrupt everything downstream.

The UI adapts automatically — `BuildActorViews()` loops over
`_battle.State.Actors` and puts each one in the hero or monster row by team.

---

## Add a third monster to the fight

Exactly the recipe above with `team: Team.Monsters`. But expect this:

```
Assert.InRange() Failure: Value not in range
Hero wins : 412 (41.2%)
```

**That is the test working, not breaking.** You made the fight harder; the
harness noticed. Either rebalance until you are back in the band, or change the
band because you intended a harder fight. See
[Tuning the difficulty](#tuning-the-difficulty).

---

## Add a new stat

**File:** [`src/Rpg.Core/Entities/StatBlock.cs`](../src/Rpg.Core/Entities/StatBlock.cs)

Say you want `Evasion`.

**Step 1** — add the parameter:

```csharp
public readonly record struct StatBlock(
    int MaxHealth,
    int Attack,
    int Defense,
    int Speed,
    int CritChance,
    int Evasion)          // ← new
```

**Step 2** — add it to the `+` operator and to `Clamped()`:

```csharp
public static StatBlock operator +(StatBlock a, StatBlock b) => new(
    a.MaxHealth + b.MaxHealth,
    a.Attack + b.Attack,
    a.Defense + b.Defense,
    a.Speed + b.Speed,
    a.CritChance + b.CritChance,
    a.Evasion + b.Evasion);      // ← new
```

**Step 3** — the compiler now walks you through every place that constructs a
`StatBlock`. Fix each one. That is the type system doing your job for you.

**Step 4** — actually *use* it, in `SkillAction.Execute`:

```csharp
if (state.Random.Chance(Target.CurrentStats.Evasion))
{
    log.Add(new TurnSkipped(Actor.Id, "Missed"));
    return;
}
```

(You would probably add a proper `Missed` event rather than reusing
`TurnSkipped`.)

---

## Change the damage formula

**File:** [`src/Rpg.Core/Combat/DamageCalculator.cs`](../src/Rpg.Core/Combat/DamageCalculator.cs)

There is exactly one method. Every damage number in the game flows through it.

The current formula is **subtractive**:

```csharp
int mitigated = raw - defender.Defense / 2;
```

Easy to explain to a player, but it scales badly — at very high numbers, defence
either trivialises damage or does nothing.

A **multiplicative** alternative that scales smoothly forever:

```csharp
// Each point of Defense reduces damage by a diminishing percentage.
// Defense 10 → ~17% reduction. Defense 50 → ~50%. Never reaches 100%.
int mitigated = raw * 50 / (50 + defender.Defense);
```

**After changing it, run `dotnet test`.** Expect
[`DamageCalculatorTests`](../src/Rpg.Core.Tests/DamageCalculatorTests.cs) to fail
— it asserts specific numbers. Update those assertions to the new expected
values, then check what the balance harness says about the fight.

---

## Tuning the difficulty

This is the workflow the whole project is built around.

**Step 1 — measure what you have:**

```bash
dotnet test --logger "console;verbosity=detailed"
```

```
Battles simulated : 1000
Hero wins         : 749 (74.9%)
Monster wins      : 251
Draws (hit round limit) : 0
Average length    : 6.7 rounds
```

**Step 2 — change one thing** in `ContentDatabase`. One. Not three.

**Step 3 — measure again.** Repeat.

### What actually moves the needle

From the real tuning session that produced these numbers:

| Change | Win rate | Rounds |
|---|---|---|
| First draft | 100.0% | 3.8 |
| Raised everyone's stats | 99.9% | 5.7 |
| **Fixed one AI weight + nerfed one heal** | 91.4% | 6.6 |
| Monster Attack **+2** each | 59.9% | 7.6 |
| Monster Attack **−1** each | **74.9%** | 6.7 |

Lessons that generalise:

- **Attack is enormously leveraged.** ±1 Attack on two monsters moved the win
  rate 15 points. Because defence subtracts, a point of Attack is worth roughly
  two points of Defense.
- **Health barely matters on its own.** It lengthens the fight, which gives the
  healer more turns, which partly cancels it out.
- **Sustain is the strongest thing in the game.** Bandage at 18 HP with a 1-turn
  cooldown made the fight literally unloseable. At 14 HP with a 2-turn cooldown
  it is merely strong.
- **A bad AI is a bigger balance problem than bad stats.** See the next recipe.

### Locking in your intent

**File:** [`src/Rpg.Core.Tests/BalanceHarnessTests.cs`](../src/Rpg.Core.Tests/BalanceHarnessTests.cs)

```csharp
Assert.InRange(winRate, 0.60, 0.85);
```

Change the band to whatever you have designed for:

| Encounter | Sensible band |
|---|---|
| Tutorial fight | `0.85, 1.00` |
| Normal encounter | `0.60, 0.85` |
| Hard/elite | `0.40, 0.60` |
| Boss you should lose once | `0.25, 0.45` |

This turns a vague intention into a test that fails when a future change breaks
it.

---

## Make the monsters smarter or dumber

**File:** [`src/Rpg.Core/Ai/ScoringAi.cs`](../src/Rpg.Core/Ai/ScoringAi.cs)

Every weight is in one block at the top. **Treat these as game design values.**

```csharp
public const double LethalBonus = 60.0;              // finish off a target
public const double DamageWeight = 1.0;              // baseline
public const double HealWeight = 1.3;                // healing vs damage
public const double EmergencyHealBonus = 25.0;       // heal someone nearly dead
public const double EmergencyHealthFraction = 0.35;  // what "nearly dead" means
public const double StunValue = 16.0;                // worth of denying a turn
public const double DamageOverTimeWeight = 0.6;      // discount on delayed damage
public const double BuffValue = 10.0;
public const double PassValue = 0.1;                 // doing nothing
```

**The one that mattered most:** `DamageOverTimeWeight`. At `0.9` the goblin
preferred a Poison Dart dealing **2** direct damage over a Club dealing **10**,
because 4 damage × 3 turns × 0.9 = 10.8 outscored 10. In a six-round fight that
is simply wrong — delayed damage is worth less than immediate damage. Dropping it
to `0.6` fixed the AI, and moved the win rate eight times further than a whole
round of stat inflation.

### Giving monsters personality

Pass a multiplier set per actor instead of using the constants globally:

| Archetype | Damage | Heal | Status |
|---|---|---|---|
| Berserker | ×2.0 | ×0.0 | ×0.5 |
| Shaman | ×0.5 | ×1.5 | ×3.0 |
| Bodyguard | ×0.8 | ×2.0 | ×1.0 |

Cheap to build, and it makes encounters feel authored rather than generated.

### Making it genuinely smart

The real upgrade is **search**. Add a `Clone()` to `BattleState`, apply the
candidate action to the clone, and score the resulting *position* rather than the
action. That turns this into a proper minimax/expectimax and is the honest way to
get an opponent that plans ahead. It is week 7 of [the roadmap](roadmap.md).

---

## Make healing statuses work (a real bug to fix)

A genuinely good first engine change. Try this:

```csharp
var regeneration = new StatusDefinition(
    Id: "regeneration",
    Name: "Regenerating",
    Description: "Recovers health each turn.",
    Modifier: StatBlock.Zero,
    DamagePerTurn: -3,          // negative damage = healing, surely?
    Icon: "RGN");
```

Add it to the `statuses` array, attach it to a skill, use it in game — **and
watch nothing happen.**

**Why:** [`Battle.TickStatuses`](../src/Rpg.Core/Combat/Battle.cs) has this
guard:

```csharp
if (status.Definition.DamagePerTurn > 0 && actor.IsAlive)
```

`-3 > 0` is false, so the whole block is skipped.

**Your job:** make it work. A reasonable fix:

```csharp
if (status.Definition.DamagePerTurn != 0 && actor.IsAlive)
{
    if (status.Definition.DamagePerTurn > 0)
    {
        int applied = actor.TakeDamage(status.Definition.DamagePerTurn);
        log.Add(new Damaged(actor.Id, applied, IsCritical: false));
    }
    else
    {
        int applied = actor.Heal(-status.Definition.DamagePerTurn);
        log.Add(new Healed(actor.Id, applied));
    }
}
```

Then consider: should `ScoringAi` value it? Right now the AI's status scoring only
looks at `DamagePerTurn > 0`. And should the field be renamed to
`HealthChangePerTurn`? This is exactly the size of change worth practising on.

**Write a test first.** Something like:

```csharp
[Fact]
public void RegenerationRestoresHealthAtTheEndOfEachTurn()
{
    Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, spd: 20));
    Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));

    Battle battle = Duel(hero, monster);
    battle.Start();

    hero.TakeDamage(30);                                    // 70/100
    hero.ApplyStatus(Regeneration, turns: 2);

    battle.TakeTurn(new SkillAction(hero, Punch, monster));

    Assert.Equal(73, hero.Health);
}
```

---

## Change the colours and the look

**File:** [`tools/make_art.py`](../tools/make_art.py) and [`game/scripts/UiTheme.cs`](../game/scripts/UiTheme.cs)

The art and the UI share one palette, in two places that you should keep in step.

**The sprites** - edit the palette block at the top of `tools/make_art.py` and
re-run `python tools/make_art.py`. Every sprite, icon and panel regenerates.

```python
HERO_BLUE  = C("4d8fd6")
GOB_GREEN  = C("7dae4c")
```

**The interface** - the same colours live at the top of `UiTheme.cs`:

```csharp
public static readonly Color HeroBlue = new("6fb3d2");
public static readonly Color HealthGood = new("6dbf73");
```

Hex strings, no `#`. `new Color(1, 0, 0)` (floats 0-1) also works, as does
`new Color(1, 0, 0, 0.5f)` for alpha.

See [the art pipeline](09-art-pipeline.md) for the whole story, including how to
add a new sprite or throw the generator away and use real art.

The background colour is in
[`UiTheme.cs`](../game/scripts/UiTheme.cs):

```csharp
private static readonly Color Background = new("16161c");
```

---

## Speed up or slow down the animation

**File:** [`game/scripts/BattleView.cs`](../game/scripts/BattleView.cs)

```csharp
private const double EventDelaySeconds = 0.45;
```

Set it to `0.05` while testing, so fights fly past. Set it to `0` and the whole
battle resolves instantly — which is a decent demonstration that the animation
genuinely has no effect on the outcome.

To change *which* events pause at all:

```csharp
private static bool IsWorthPausingFor(GameEvent gameEvent) =>
    gameEvent is SkillUsed or Damaged or Healed or StatusApplied or TurnSkipped or Died;
```

---

## Write a new test

**File:** a new `.cs` file in `src/Rpg.Core.Tests/`.

```csharp
using Rpg.Core.Combat;
using Rpg.Core.Entities;
using Xunit;
using static Rpg.Core.Tests.TestFixtures;

namespace Rpg.Core.Tests;

public sealed class MyTests
{
    [Fact]                                    // marks it as a test
    public void DescribeWhatShouldHappen()
    {
        // Arrange
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, atk: 10, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));
        Battle battle = Duel(hero, monster);
        battle.Start();

        // Act
        battle.TakeTurn(new SkillAction(hero, Punch, monster));

        // Assert
        Assert.Equal(90, monster.Health);
    }
}
```

Helpers available from `TestFixtures`:

| Helper | Gives you |
|---|---|
| `Stats(hp:, atk:, def:, spd:, crit:)` | A `StatBlock` with sensible defaults |
| `MakeActor(id, team, stats, ...skills)` | An actor (defaults to knowing `Punch`) |
| `Duel(hero, monster)` | A 1v1 battle with **non-random** dice |
| `Punch` | A plain 100-power attack |
| `Poison`, `Stun` | Test status definitions |
| `BattleRunner.Run(seed)` | A complete AI-vs-AI battle |

> **Make your numbers exact.** Give actors `crit: 0` (the default) and
> `Chance()` short-circuits without touching the RNG at all — so damage is
> perfectly predictable and your test can never fail intermittently.

Common assertions:

```csharp
Assert.Equal(90, monster.Health);
Assert.True(hero.IsAlive);
Assert.Contains(log, e => e is Died { ActorId: "monster" });
Assert.Single(battle.LegalActions(hero));
Assert.Throws<InvalidOperationException>(() => battle.TakeTurn(badAction));
Assert.InRange(winRate, 0.60, 0.85);
```

---

## Debugging

### Debugging the rules — do this first

You almost never need the game running. Write a test, put a breakpoint in it, and
step through `Battle.TakeTurn`. It runs in milliseconds, with no window, and you
can re-run it as many times as you like.

```bash
dotnet test --filter "FullyQualifiedName~PoisonDamages"
```

`--filter` runs one test (or a group) instead of all of them.

### Printing from inside Godot

```csharp
GD.Print($"current = {_battle.Current?.Name}, round = {_battle.Round}");
```

Output appears in Godot's **Output** panel at the bottom of the editor.

> `GD.Print` only exists in the `game/` project. `Rpg.Core` cannot use it — that
> is the architecture rule holding. Use a test instead, or `Console.WriteLine`,
> or an `ITestOutputHelper`.

### When your C# changes seem to do nothing

**This is the most confusing Godot trap.** If your C# fails to compile, Godot
runs the **last successfully built version** — so the game launches fine and
simply ignores your change.

Always check the **Output** panel for build errors. Or, more reliably, build from
the terminal where errors cannot be missed:

```bash
dotnet build
```

### Breakpoints in the running game

Attaching a debugger to a Godot C# project works properly in **JetBrains Rider**
(free for non-commercial use) and in **VS Code** with the C# Dev Kit extension.
Rider's Godot support is noticeably better.

---

## Next

[Glossary](08-glossary.md) — every jargon word, defined.

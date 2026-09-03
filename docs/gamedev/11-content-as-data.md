# 11. Content as data

> **Where you are:** chapter 11 of 17 · [index](README.md) · previous: [Status effects and space](10-statuses-and-space.md) · next: [Enemy AI](12-enemy-ai.md)

---

## The problem

You are an experienced object-oriented programmer. You need a goblin, an orc and
a dragon. You know exactly what to write:

```csharp
abstract class Monster { }
class Goblin : Monster { }
class Orc : Monster { }
class GoblinShaman : Goblin { }
class ArmouredOrc : Orc { }
class Dragon : Monster { }
```

Five years of professional instinct says this is right. In game development it is
one of the most expensive mistakes you can make, and it is worth understanding
precisely why — because the instinct is *not stupid*, it is just being applied to
the wrong kind of thing.

### What goes wrong

**1. It does not scale.** This project has 22 monsters, 55 skills, 47 weapons,
10 heroes and 14 statuses. That is 148 classes to write "content". A shipped RPG
has thousands. Nobody can hold that codebase in their head.

**2. You cannot balance it.** "Is tier 2 too strong?" now requires opening
nineteen files and reading nineteen constructors. When the numbers live in one
table, you read one table.

**3. The hierarchy is always wrong.** Is a `GoblinShaman` a `Goblin` or a
`Healer`? Both. What about an `ArmouredGoblinShaman` that also flies? Single
inheritance cannot express it, and you end up with interfaces, mixins, and
eventually a component system — reinventing the answer badly.

**4. Only a programmer can add content.** Every new monster is a compile. A
designer cannot try fifty variations in an afternoon, which is exactly what
balancing requires.

**5. It cannot be serialised, modded, or hot-reloaded.** A class is code. You
cannot save it in a JSON file, ship it as DLC, or let players mod it.

---

## The idea

> **Content is data. Systems are code.**

Instead of a class per thing, you write **one generic system** and feed it
**rows of data**.

```
   INSTEAD OF:                        DO THIS:

   class Fireball : Skill             SkillDefinition("fireball", "Fireball",
   class IceLance : Skill                 Power: 110, AppliesStatus: Burning, ...)
   class Heal     : Skill             SkillDefinition("ice_lance", "Ice Lance",
   class Poison   : Skill                 Power: 100, AppliesStatus: Chilled, ...)
   ...48 more files                   ...52 more ROWS

   + a class each to execute them     + ONE SkillAction that executes them all
```

The test for whether you have done it right:

> **Could a non-programmer add a new monster?**

If yes, you have content-as-data. If it needs a compile and a new file, you have
content-as-code.

---

## In this project

### One definition type per kind of thing

[`SkillDefinition`](../../src/Rpg.Core/Content/SkillDefinition.cs) — every skill
in the game is one of these:

```csharp
public sealed record SkillDefinition(
    string Id,
    string Name,
    string Description,
    TargetKind Target,
    int Power = 0,
    int Healing = 0,
    StatusDefinition? AppliesStatus = null,
    int StatusTurns = 0,
    int Cooldown = 0,
    int LifestealPercent = 0,
    string LaunchPattern = "####",
    string TargetPattern = "####");
```

Every field has a default, so declaring a skill only mentions what makes it
*different*:

```csharp
public static readonly SkillDefinition Slash = new(
    "slash", "Slash", "A reliable swing.",
    TargetKind.SingleEnemy, Power: 100,
    LaunchPattern: "##--", TargetPattern: "##--");

public static readonly SkillDefinition Envenom = new(
    "envenom", "Envenom", "A thrown vial. Works from anywhere in the line.",
    TargetKind.SingleEnemy, Power: 50, AppliesStatus: Statuses.Poison, StatusTurns: 3,
    LaunchPattern: "####", TargetPattern: "###-");

public static readonly SkillDefinition Bloodthirst = new(
    "bloodthirst", "Bloodthirst", "Tears into them and drinks half of it back.",
    TargetKind.SingleEnemy, Power: 125, LifestealPercent: 50, Cooldown: 2,
    LaunchPattern: "##--", TargetPattern: "##--");
```

Those three skills behave completely differently. **None of them has any code.**
All three run through the same forty lines of
[`SkillAction.Execute`](../../src/Rpg.Core/Combat/SkillAction.cs).

### Computed properties turn data into behaviour

The definition exposes what the systems need to ask:

```csharp
public bool DealsDamage => Power > 0;
public bool Heals       => Healing > 0;
public bool Drains      => LifestealPercent > 0;
public Ranks LaunchRanks => Ranks.Parse(LaunchPattern);
public Ranks TargetRanks => Ranks.Parse(TargetPattern);
public bool IsPositional => LaunchRanks.Mask != Ranks.Any.Mask
                         || TargetRanks.Mask != Ranks.Any.Mask;
```

So `SkillAction` never asks "what kind of skill is this?" — it asks "does this
deal damage?", "does this heal?", "does it apply a status?". Every combination
works automatically. A skill that damages *and* heals *and* applies a status
needs no new code, because those are three independent `if`s.

> **That is the real payoff.** Not "less typing". **Combinatorial coverage.** A
> class hierarchy gives you the behaviours you wrote. Composed data gives you
> every combination of them.

### The same shape, five times

| Definition | Instance | Count |
|---|---|---|
| [`SkillDefinition`](../../src/Rpg.Core/Content/SkillDefinition.cs) | a cooldown entry on an Actor | 55 |
| [`StatusDefinition`](../../src/Rpg.Core/Effects/StatusDefinition.cs) | [`StatusEffect`](../../src/Rpg.Core/Effects/StatusEffect.cs) | 14 |
| [`HeroDefinition`](../../src/Rpg.Core/Content/Heroes.cs) | [`Actor`](../../src/Rpg.Core/Entities/Actor.cs) | 10 |
| [`MonsterTemplate`](../../src/Rpg.Core/Content/Monsters.cs) | [`Actor`](../../src/Rpg.Core/Entities/Actor.cs) | 22 |
| [`WeaponDefinition`](../../src/Rpg.Core/Content/WeaponDefinition.cs) | the weapon an Actor holds | 47 |

A hero and a monster are **the same `Actor` class**:

```csharp
public sealed record HeroDefinition(
    string Id, string Label, string Role, string Blurb,
    StatBlock Stats, string[] SkillIds,
    string SpriteName, string VoiceFamily, WeaponKind PreferredWeapon);
```

```csharp
public sealed record MonsterTemplate(
    string Id, string Label, int Tier,
    StatBlock Stats, string[] SkillIds,
    string SpriteName, string VoiceFamily, WeaponKind Weapon, string Blurb);
```

The difference between the Stick Warrior and a Demon Lord is **a row of
numbers**.

### The database

[`ContentDatabase`](../../src/Rpg.Core/Content/ContentDatabase.cs) loads all of
it into dictionaries and turns data into live objects:

```csharp
public Actor CreateHero(string id);
public Actor CreateMonster(string templateId, string actorId, string nameSuffix);
```

That is the factory. It is the *only* place that turns a definition into a thing
in a fight.

### Levels are data too

The idea does not stop at monsters. A whole dungeon is a row:

```csharp
new DungeonDefinition("warrens", "The Warrens",
    "Something has been breeding down here.",
    ThreatName: "Poison",
    ThreatBlurb: "...",
    FloorTile: "floor_dirt", WallTile: "wall_cave",
    Encounters: new[]
    {
        new EncounterDefinition("warrens_1", "The Entrance",
            "Three of them are already awake.",
            new[] { "goblin_grunt", "goblin_archer", "giant_rat" }),
        // ...
    },
    Loot: new LootTable(Common: 55, Uncommon: 30, Rare: 13, Epic: 2, Legendary: 0));
```

Nine encounters, three dungeons, three loot tables. No level-loading code, no
per-dungeon classes. Adding a fourth dungeon is adding an entry to an array.

---

## Where the data lives: C# vs files

There are two ways to store all this, and this project deliberately picked the
less obvious one.

| | **C# static readonly** (this project) | **JSON / CSV / Godot Resources** |
|---|---|---|
| Type safety | Compiler catches typos | Runtime errors |
| Refactoring | Rename works everywhere | Find-and-replace and hope |
| Hot reload | No — needs a rebuild | Yes |
| Modding / DLC | No | Yes |
| Designer-editable | Needs the SDK | Any text editor or spreadsheet |
| Merge conflicts | Manageable | Manageable |

**This project uses C#** because it is a teaching repository: everything is
visible on GitHub, the compiler catches every typo, and there is no loader to
explain.

> **For a real game, move it out to files.** The moment you have a designer — or
> the moment *you* want to tweak twenty numbers without a rebuild — external data
> wins decisively. The architecture is identical either way; only
> `ContentDatabase` changes, from "read a static array" to "parse a file". That
> is the point of putting a factory in the middle.

---

## The trap this project actually fell into

Worth including because it is a *real* C# gotcha that ate an hour.

`ContentDatabase` has properties named after the static content classes:

```csharp
public IReadOnlyList<HeroDefinition> Heroes => ...;    // property
// ...but there is also:
public static class Heroes { public static readonly HeroDefinition[] All; }
```

Inside the class, `Heroes.All` now resolves to *the property*, not the static
class, and the compiler produces `CS0119: 'Heroes' is a property but is used like
a type`. The fix is to fully qualify:

```csharp
global::Rpg.Core.Content.Heroes.All
```

**The general lesson:** when a name means two things in one scope, C# picks the
member, not the type. Either qualify it or — better — do not name a property the
same as a type you also need to reference.

---

## What it costs you

**You lose compile-time behaviour checking.** With a class per skill, `Fireball`
either implements its interface or does not compile. With data, a typo in
`"fierbolt"` is a runtime `KeyNotFoundException` at 11pm in encounter seven of a
playtest.

You buy that guarantee back with tests that walk every reference in every piece
of content — [`ContentIntegrityTests`](../../src/Rpg.Core.Tests/ContentIntegrityTests.cs):

```csharp
[Fact] public void EverySkillIdReferencedByAHeroExists()
[Fact] public void EverySkillIdReferencedByAMonsterExists()
[Fact] public void EveryMonsterIdInAnEncounterExists()
[Fact] public void EveryContentIdIsUnique()
[Fact] public void EveryHeroAndMonsterCanBeBuilt()
[Fact] public void EveryLootTableCanActuallyDropSomething()
```

They are fast, they never need updating as content grows, and they turn a
late-night runtime crash into a named build failure. **If you take the
content-as-data trade, take these too** — they are the other half of it.

**Everything becomes indirection.** "What does Envenom do?" is answered by
reading a row *and* reading the generic executor and mentally combining them.
With `class Envenom` you would read one file. That is a genuine loss of local
clarity, traded for global clarity.

**Genuinely unique mechanics fight the system.** A skill that "resurrects the
last hero who died, at half health, unless it is raining" does not fit any data
schema. At some point you need either a scripting hook or a small amount of
special-case code — and that is fine. The goal is that **95% of your content is
data**, not 100%.

**Data-driven systems are less discoverable.** A newcomer can find
`class Fireball` by name. Finding the row `"fireball"` in a 400-line array
requires knowing where to look.

---

## Try it

**1. Add a skill without writing any code.** In
[`Skills.cs`](../../src/Rpg.Core/Content/Skills.cs):

```csharp
public static readonly SkillDefinition PikeThrust =
    new("pike_thrust", "Pike Thrust", "Long reach, from the second rank.",
        TargetKind.SingleEnemy, Power: 110,
        LaunchPattern: "-##-", TargetPattern: "###-");
```

Add it to `All`, put `"pike_thrust"` in a hero's `SkillIds`, and run. It appears
in the menu, obeys reach rules, previews its damage, plays a weapon sound and is
scored by the AI. **You wrote no logic.**

**2. Add a monster.** Same exercise in
[`Monsters.cs`](../../src/Rpg.Core/Content/Monsters.cs). Drop its id into an
encounter and fight it.

**3. Feel the alternative.** Try sketching `class Fireball : Skill` for a skill
that damages, heals the caster, applies burning, has a cooldown *and* only
launches from the back rank. Then count how many classes you would need for every
*combination* of those five properties. That number is why this chapter exists.

---

**Next:** [Chapter 12 — Enemy AI](12-enemy-ai.md)

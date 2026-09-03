# 8. Glossary

Every jargon word used in this project or its docs, defined plainly. Grouped by
where it comes from.

---

## Words from this project

**Actor** — anyone who fights. Hero or monster; they are the same class, and the
difference is data. `Rpg.Core/Entities/Actor.cs`

**Balance harness** — code that plays thousands of complete battles automatically
and reports the win rate, so you can tune a fight by measuring rather than
guessing. `Rpg.Core.Tests/BalanceHarnessTests.cs`

**Content** — the actual skills, statuses and stat lines that make up your game,
as opposed to the engine that runs them. Lives in `ContentDatabase`.

**Content database** — the one place all content is defined.

**Event** (`GameEvent`) — a record of one thing that happened: "25 damage to the
goblin", "the goblin died". The rules produce a list of these; the screen replays
it. The single bridge between the two halves of the project.

**Legal actions** — every move an actor could make right now, given cooldowns,
stuns and living targets. Both the player's menu and the AI read the same list,
which is what stops the AI cheating. `Battle.LegalActions()`

**Presentation layer** — the `game/` folder. Everything that draws or reads
input. Contains no rules.

**Rules engine** — the `src/Rpg.Core/` folder. All the game logic, with zero
knowledge of graphics.

**Skill** — an attack, spell, heal or buff. Data, not a class:
`SkillDefinition`.

**Status effect** — poison, stun, a buff. Also data: `StatusDefinition` is the
template ("what poison is"); `StatusEffect` is one instance on one actor ("this
goblin has 2 turns of poison left").

**Turn queue** — decides who acts next. `TurnQueue.cs`

---

## Game development

**AI** — here, just "code that picks a move". No machine learning involved. Ours
scores every legal action and takes the highest.

**ATB (Active Time Battle)** — a turn system where actors fill a gauge over time
and act when it is full, so fast characters get genuinely *extra* turns. The
upgrade path from our simpler round-based system. Final Fantasy IV–IX use it.

**Buff / debuff** — a temporary improvement / worsening of someone's stats.

**Cooldown** — how many turns before a skill can be used again.

**Critical hit ("crit")** — a random chance to do extra damage. Ours doubles it.

**Deterministic** — given the same inputs, always produces the same output. Our
battles are deterministic given a seed, which is what makes replays, reproducible
tests and reproducible bug reports possible.

**DoT (damage over time)** — damage dealt each turn rather than all at once.
Poison, burning, bleed.

**Encounter** — one fight, with a specific set of enemies.

**Frame** — one image drawn to the screen. At 60 FPS, one frame is ~16
milliseconds. Turn-based games barely care; action games care enormously.

**Game loop** — the cycle a game repeats forever: read input, update state, draw.
In an engine like Godot, the engine owns the loop and calls *your* code
(`_Process`, `_Draw`) at the right moments.

**Headless** — running the game with no window and no graphics. Used here to test
that the Godot project loads and runs without a human watching.

**HUD** — heads-up display. The health bars, menus, and combat log.

**Mitigation** — reducing incoming damage, usually via Defense.

**Overkill** — damage beyond what was needed to kill. Our AI deliberately
discounts it: 40 damage into a 6 HP goblin is worth 6.

**Ply** — one level of lookahead in a decision. Our AI is "one-ply": it evaluates
each move without considering the opponent's reply. Chess engines search many
ply.

**Postmortem** — a public write-up after a game ships about what went right and
wrong. The *Slay the Spire* and *Into the Breach* ones are worth reading.

**RNG** — random number generator. Also used as a noun for randomness itself
("the RNG was kind to me").

**Roguelike** — a genre built on procedural generation and permadeath. The
`r/roguelikedev` community is unusually good on deep mechanics.

**Scope creep** — the project quietly growing until it can never be finished. The
main cause of unfinished games.

**Seed** — the starting number for a random generator. The same seed reproduces
the same sequence, and therefore the same battle.

**Sprite** — a 2D image used as a game object. This project has none — the stick
figures are drawn with lines.

**Stat** — a number describing a character. Ours: MaxHealth, Attack, Defense,
Speed, CritChance.

**Tick** — one step of a repeating process. "Poison ticks at the end of your
turn."

**Tile map** — a level built from a grid of reusable tiles. Not used here; you
will meet it if you build an overworld.

**Turn-based** — the game waits for you. Opposite of real-time.

---

## C# and .NET

**Assembly** — a compiled `.dll` or `.exe`. This project has three:
`Rpg.Core.dll`, `Rpg.Core.Tests.dll`, `StickmanRpg.Game.dll`.

**Attribute** — metadata attached to code, in square brackets. `[Fact]` tells the
test runner "this is a test".

**`csproj`** — an XML project file. Lists what a project targets and depends on.

**Expression-bodied member** — a method or property written with `=>` instead of
`{ return ...; }`.

**Extension method** — a method that appears to belong to a type you do not own.
Enabled by `this` on the first parameter. Only visible if you `using` its
namespace — a common source of "does not contain a definition for" errors.

**Immutable** — cannot be changed after creation. `StatBlock` and all our
`record` types are immutable.

**Implicit usings** — a project setting that auto-imports common namespaces so
you do not write them in every file.

**Interface** — a contract with no implementation. Conventionally prefixed `I`:
`IAction`, `IRandomSource`.

**LINQ** — the standard library for querying collections: `.Where()`,
`.Select()`, `.Any()`, `.OrderBy()`. Roughly JavaScript's array methods.

**Nullable reference types** — a compiler feature that tracks whether a reference
can be `null`. `string?` may be null; `string` should not be; `x!` means "trust
me, it is not".

**NuGet** — .NET's package manager. Used here only for the test libraries.

**Pattern matching** — testing shape and type in one step:
`e is Died { ActorId: "monster" }`.

**Property** — looks like a field, is really a getter/setter pair.
`{ get; private set; }` means "anyone can read, only I can write".

**Record** — a class or struct where the compiler generates the constructor,
properties, equality and `ToString()` for you. Used for every event and
definition here.

**SDK** — the developer toolkit (compiler + tools), as opposed to the *runtime*
which only runs programs. You need the SDK.

**`sealed`** — nobody may inherit from this class.

**`static`** — belongs to the type, not to an instance.
`DamageCalculator.Compute(...)`.

**Struct vs class** — a struct is a *value type* (copied on assignment, like
`int`); a class is a *reference type* (copied by reference). `StatBlock` is a
struct so it can never be mutated through a shared reference.

**Target-typed `new`** — writing `new()` when the type is already stated on the
left: `private readonly List<T> _x = new();`

**xUnit** — the testing library used here. `[Fact]` marks a test; `Assert.*`
checks things.

---

## Godot

**BBCode** — the markup `RichTextLabel` understands: `[b]bold[/b]`,
`[color=#ff0000]red[/color]`. Used for our combat log.

**Control** — the base class for all UI nodes. Understands position, size and
layout.

**GDScript** — Godot's own Python-like language. Most tutorials use it. Converting
GDScript to C# is mechanical: `snake_case` → `PascalCase`.

**Godot.NET.Sdk** — the build system that turns your C# into something Godot can
load. Its version must match your Godot version.

**Mono / .NET build** — the version of Godot that supports C#. The standard build
does not. Downloading the wrong one is the most common setup mistake.

**Node** — one object that does one job. Everything in a Godot game is a node.

**`partial`** — required on every Godot C# script, because Godot generates a
second file for the same class behind the scenes.

**`QueueRedraw()`** — "this node needs repainting next frame". You call this;
Godot then calls your `_Draw()`. Never call `_Draw()` yourself.

**`res://`** — a path relative to the project root (the folder holding
`project.godot`).

**Scene** — a saved branch of the node tree, in a `.tscn` file. Does **not** mean
"level" — a button, an enemy, or a whole game can each be a scene.

**Scene tree** — the live tree of all nodes currently in the game.

**Signal** — Godot's event system. In C#, `button.Pressed += MyHandler;`

**`.tscn`** — the scene file format. Plain text, so it diffs and merges in git.

**`_Ready()`** — called once when a node and its children are in the tree and
ready. Godot's equivalent of a constructor for anything touching the scene.

**Viewport** — the rectangle the game is drawn into.

---

## Software design patterns used here

**Command pattern** — wrapping "something to do" in an object so it can be
stored, queued, scored, or undone. Our `IAction`.

**Dependency injection** — passing a dependency in rather than creating it
inside. We inject `IRandomSource` into `BattleState`, which is the entire reason
tests can control the dice.

**Event sourcing** (loosely) — representing what happened as a sequence of events
rather than only keeping the final state. Our `List<GameEvent>`.

**Test double** — a stand-in used in tests. `FixedRandom` is ours.

**Type Object** — instead of one class per kind of thing, one class plus a data
object describing the kind. `SkillDefinition` and `StatusDefinition` are both
this. It is why adding a skill costs five numbers instead of a new class.

---

## Further reading

- **[Game Programming Patterns](https://gameprogrammingpatterns.com/)** — Robert
  Nystrom, free online. The Command, Type Object, Event Queue and State chapters
  describe this codebase almost exactly.
- **[Godot docs](https://docs.godotengine.org/)** — genuinely good, with GDScript
  and C# shown side by side.
- **[r/roguelikedev](https://reddit.com/r/roguelikedev)** — the most
  mechanics-literate game dev community online.

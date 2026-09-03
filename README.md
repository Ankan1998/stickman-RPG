# Stickman RPG

A complete turn-based dungeon crawler. Pick **three heroes from a roster of ten**,
descend into **three dungeons** of two-to-three encounters each, and fight your
way to the Frozen Crypt. Loot drops after every fight. Between dungeons you
return to camp, swap the party, and hand out weapons.

Combat is **positional, in the Darkest Dungeon sense**. Both sides stand in a
line facing each other, and where you stand decides what you can do: a sword
reaches the front two ranks, a bow cannot be fired from the front at all, and
their shaman hides at the back where your melee simply cannot go. Ranks close up
when somebody falls, so killing things changes the shape of the fight.

Wounds carry between encounters and only camp restores you, so a dungeon is
fought on one health bar. Each dungeon attacks you differently - **poison**,
then **burning**, then **chill and curse** - so the party that flattened the
first one stops working in the third.

Animated pixel-art sprites, 62 sound effects, and impact effects on every hit.
The rules underneath are a plain C# library with **no engine dependency at all**,
which is why the whole nine-encounter campaign can be simulated in a unit test.

---

## Start here if you have never done game development

You already know how to program. You do **not** need to know C#, Godot, or
anything about games.

There are two ways in, and they suit different goals:

### 🎓 Learn game development, using this project as the worked example

**→ [Game Development, From Zero](docs/gamedev/README.md)** — a twenty-chapter
course that teaches game development itself: the game loop, engines and scene
trees, separating rules from presentation, event logs, determinism, turn systems,
damage formulas, content-as-data, enemy AI, sprites, audio, UI, game feel and
balancing. Every concept is explained from nothing and then shown working in this
repository, including the bugs it shipped and how they were fixed.

Start there if your goal is **to learn game development**.

### 📘 The reference manual for this specific codebase

The numbered docs below explain *this project*, and are meant to be read in
order. Start here if your goal is **to work on this code**.

| #  | Document                                                | What it gives you                                                                        |
| -- | ------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| ▶ | [**How to play**](docs/how-to-play.md)             | What the screen shows, what the buttons do, and how to actually win. Read this first.    |
| 0  | [**How to run**](docs/00-how-to-run.md)            | Every command, the development loop, and troubleshooting.                                |
| 1  | [Getting started](docs/01-getting-started.md)            | A gentler walkthrough of installing and first launch. ~15 minutes.                       |
| 2  | [C# crash course](docs/02-csharp-crash-course.md)        | Every piece of C# syntax used in this repo, explained, with the real line it comes from. |
| 3  | [Godot crash course](docs/03-godot-crash-course.md)      | What a "node" and a "scene" are, and the five Godot ideas this project actually uses.    |
| 4  | [How it all fits together](docs/04-architecture.md)      | The one big design idea, in plain English, and why it matters.                           |
| 5  | [Code tour](docs/05-code-tour.md)                        | Every file, one at a time, what it does and why it exists.                               |
| 6  | [Anatomy of a turn](docs/06-anatomy-of-a-turn.md)        | One single turn traced from button click to pixels. The most useful page here.           |
| 7  | [Recipes](docs/07-recipes.md)                            | "How do I add a skill / a monster / a status effect?" Copy-paste answers.                |
| 8  | [Glossary](docs/08-glossary.md)                          | Every jargon word, defined.                                                              |
| 9  | [Art pipeline](docs/09-art-pipeline.md)                  | How the original placeholder art was generated from Python.                              |
| 10 | [Campaign plan](docs/10-campaign-implementation-plan.md) | The design and build plan for the dungeons, hub, loot, animation and audio.              |
| 11 | [**Positioning**](docs/11-positioning.md)          | Ranks, reach, and why your sword cannot hit their shaman.                                |
| 12 | [Roadmap](docs/roadmap.md)                               | What to build next, in what order.                                                       |
| ★ | [**Game dev course**](docs/gamedev/README.md)      | Twenty chapters teaching game development itself, worked through this codebase.       |

**If you read only one thing after getting it running, read
[Anatomy of a turn](docs/06-anatomy-of-a-turn.md).** Everything else makes sense
once you have followed one turn all the way through.

---

## What is in the box

```
stickman-RPG/
│
├── src/Rpg.Core/         THE RULES. Pure C#. Knows nothing about graphics.
│   ├── Entities/           Who is fighting: actors, teams, stats
│   ├── Effects/            Poison, stun, buffs
│   ├── Combat/             The turn loop, damage, actions, events
│   ├── Content/            Statuses, skills, 10 heroes, 22 monsters, 47 weapons
│   ├── Progression/        Dungeons, the campaign, loot, and the stats it records
│   ├── Ai/                 How monsters decide what to do
│   └── Rng/                Randomness (dice rolls), made repeatable
│
├── src/Rpg.Core.Tests/   Automated checks. 51 of them. Run in ~1 second.
│
├── game/                 THE SCREEN. A Godot project. Drawing and clicking only.
│   ├── project.godot       Godot's config file
│   ├── assets/             407 PNGs - animated characters, FX, weapons, tiles
│   ├── audio/              186 sounds - combat, voices, magic, UI
│   ├── scenes/             The one scene the game opens with
│   └── scripts/            Theme, animation, audio, effects, battle, hub, shell
│
├── stickman-rpg-assets/  The source art pack (1,457 PNGs + manifest)
├── stickman-rpg-audio/   The source audio pack (62 sounds x 3 takes)
│
├── tools/                The original placeholder art generator.
│
└── docs/                 The documents listed above
```

**The single most important rule in this repo:** `game/` is allowed to use
`Rpg.Core`, but `Rpg.Core` is *never* allowed to use anything from Godot. The
rules of your game must be runnable without a screen.
[Why this matters](docs/04-architecture.md).

---

## How to run it

Everything is already installed on this machine and verified working. Open a
terminal in this folder and use any of these three commands.

**Run the tests** — the whole combat system, checked in about a second. Needs no
Godot, opens no window:

```bash
dotnet test
```

You should see `Passed! - Failed: 0, Passed: 51`.

**Play the game:**

```bash
godot --path game
```

**Open the Godot editor** (then press **F5** to play):

```bash
godot --editor --path game
```

🎮 **[How to actually play it — screen layout, moves, and strategy →](docs/how-to-play.md)**

In VS Code you can also press **Ctrl+Shift+P → Tasks: Run Task** and pick
`test`, `play the game`, or `open Godot editor` — or open the **Testing** panel
(flask icon, left sidebar) to run and *debug* individual tests by clicking them.

📖 **[Full command reference, dev loop, and troubleshooting →](docs/00-how-to-run.md)**

<details>
<summary>What is installed, and setting this up elsewhere</summary>

| Software                | Version                     |
| ----------------------- | --------------------------- |
| .NET SDK                | 8.0.424                     |
| Godot (.NET/Mono build) | 4.7.2                       |
| Git                     | 2.54.0                      |
| VS Code                 | 1.135.0                     |
| VS Code extensions      | C#, C# Dev Kit, Godot Tools |

On another machine you need two free downloads —
[.NET SDK 8](https://dotnet.microsoft.com/download) and
[Godot 4](https://godotengine.org/download) (**take the .NET / Mono build**, not
the standard one — the standard build cannot run C#). Then `dotnet test` and
point Godot at `game/project.godot`. Details in
[How to run](docs/00-how-to-run.md#setting-this-up-on-a-different-machine).

</details>

---

## What you will see when you run it

A round of combat, narrated:

```
  [3] Mage   [2] Cleric   [1] Warrior    VS    [1] Goblin   [2] Rat   [3] Archer

-- round 1 --
Goblin Archer uses Rusty Bow on Cleric.
    11 damage to Cleric
Giant Rat swaps with Goblin Archer - now rank 2.
Goblin uses Club on Cleric.
    22 damage to Cleric - critical!
```

Animated fighters that lunge, flinch and fall over. Impact effects on every hit,
status icons above each head, damage numbers that pop and fade, and a two-step
move menu that previews exactly what each choice will do.

The enemy is not passive: it rates each of your heroes on how dangerous they are
and concentrates on whoever is worth killing - usually your healer.

The computer clears the full nine-encounter campaign about **18%** of the time -
a number *measured over 250 simulated campaigns*, not guessed. See
[the balance harness](docs/04-architecture.md#2-measuring-instead-of-guessing).

---

## The 30-second version of the design

```
   YOU CLICK A BUTTON
           │
           ▼
   game/  asks the rules: "what happened?"
           │
           ▼
   Rpg.Core  works out the ENTIRE result instantly
             (damage, deaths, poison, whose turn is next)
           │
           ▼
   returns a LIST of things that happened
           │
           ▼
   game/  plays that list back slowly, with pauses,
          so a human can follow it
```

The fight is already over before the first animation plays. The screen is just
replaying a recording. This sounds odd at first and is the reason the whole thing
is testable — [the full explanation is here](docs/04-architecture.md).

WEBSITE: [ankan1998.github.io/stickman-RPG](https://ankan1998.github.io/stickman-RPG/)

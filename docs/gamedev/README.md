# Game Development, From Zero

**A complete beginner's course in game development, taught through one real project.**

You have written software for years. You have never written a game. This course
is for exactly that person.

It does not teach you C#, and it does not teach you Godot — there are separate
crash courses for both, linked below. It teaches you **game development**: the
ideas, the vocabulary, the architecture, and the specific ways games are unlike
the software you already know how to write.

Every concept is explained from nothing, and then shown working in this
repository, with links to the actual file. Nothing here is hypothetical. When a
chapter says "this is what goes wrong", it is usually describing a bug that
genuinely happened in this codebase, and you can read the fix.

---

## How to read this

**Read it in order the first time.** The chapters build. Chapter 7 assumes you
understood chapter 5.

Each chapter has the same shape:

| Section | What it does |
|---|---|
| **The problem** | What goes wrong if you do not have this concept |
| **The idea** | The concept itself, in plain English |
| **In this project** | Where it lives, with links to real code |
| **What it costs you** | The honest trade-off. Every choice has one. |
| **Try it** | A small change you can make to feel it |

Chapters take 15–30 minutes each. There are twenty. You do not have to do it in
one sitting, and you should not.

---

## The chapters

### Part I — What a game actually is

| | Chapter | The one idea |
|---|---|---|
| 1 | [What makes games different](01-what-makes-games-different.md) | Games are simulations you steer, not requests you answer |
| 2 | [The game loop and time](02-the-game-loop-and-time.md) | Everything happens sixty times a second, forever |
| 3 | [Engines and the scene tree](03-engines-and-the-scene-tree.md) | What an engine gives you, what it charges, and why Y points down |
| 4 | [Input, signals and game flow](04-input-signals-and-game-flow.md) | A click is an intent; only the rules decide if it is legal |

### Part II — The architecture that makes a game tractable

| | Chapter | The one idea |
|---|---|---|
| 5 | [Rules vs presentation](05-rules-vs-presentation.md) | The most important line you will ever draw |
| 6 | [State and entities](06-state-and-entities.md) | What the game knows, and who is allowed to change it |
| 7 | [Events and replay](07-events-and-replay.md) | Don't draw the fight — record it, then play the recording |
| 8 | [Randomness and determinism](08-randomness-and-determinism.md) | Why `new Random()` will ruin your week |

### Part III — The rules of the game

| | Chapter | The one idea |
|---|---|---|
| 9 | [Turns, actions and resolution](09-turns-actions-and-resolution.md) | Time is a design decision, not a fact |
| 10 | [Numbers: damage and stat design](10-numbers-and-stat-design.md) | A formula is a personality |
| 11 | [Status effects and space](11-statuses-and-space.md) | Duration and position turn a fight into a puzzle |
| 12 | [Content as data](12-content-as-data.md) | Stop writing classes for content |
| 13 | [Enemy AI](13-enemy-ai.md) | Score every option, pick the best, keep it dumb |
| 14 | [Progression and the shape of a run](14-progression-and-the-shape-of-a-run.md) | The loop *around* the fights is where the game lives |

### Part IV — Making it felt

| | Chapter | The one idea |
|---|---|---|
| 15 | [Sprites and animation](15-sprites-and-animation.md) | A picture is a window onto a bigger picture |
| 16 | [Audio](16-audio.md) | Sound is half of impact, and nobody notices it until it's missing |
| 17 | [UI and game feel](17-ui-and-game-feel.md) | Juice, and a menu that explains itself |

### Part V — Doing this for real

| | Chapter | The one idea |
|---|---|---|
| 18 | [Debugging a game](18-debugging-a-game.md) | Seeds and event logs, not breakpoints |
| 19 | [Testing and balancing](19-testing-and-balancing.md) | Measure ten thousand fights instead of guessing about one |
| 20 | [Where to go next](20-where-to-go-next.md) | What to build, in what order, and what to avoid |

---

## What this project is

A turn-based tactical RPG in the style of *Darkest Dungeon*. Three heroes stand
in a line facing a line of monsters. Where you stand decides what you can do.
Wounds carry between fights. Three dungeons, nine encounters, and the computer
only finishes about one run in five.

```
        your party                                   the enemy
   [3]      [2]      [1]          VS          [1]      [2]      [3]
  Mage    Cleric   Warrior                  Goblin    Rat     Archer
  back  ---------> front                    front  ---------> back
```

It is built in **Godot 4** with **C#**, and it is split hard down the middle:

```
   src/Rpg.Core/          the rules.  Plain C#. Knows nothing about Godot.
   game/                  the screen. Godot. Knows everything about the rules.
```

That split is chapter 5, and it is the reason the rest of the project is
teachable at all.

---

## If you want the reference manual instead

This course teaches *concepts*. The numbered docs one directory up are the
*reference* for this specific codebase:

| Doc | For |
|---|---|
| [How to run it](../00-how-to-run.md) | Getting it on screen, right now |
| [Getting started](../01-getting-started.md) | The five-minute orientation |
| [C# crash course](../02-csharp-crash-course.md) | The language, if you don't know it |
| [Godot crash course](../03-godot-crash-course.md) | The engine, if you don't know it |
| [Architecture](../04-architecture.md) | The design decisions, argued |
| [Code tour](../05-code-tour.md) | Every file, what it does |
| [Anatomy of a turn](../06-anatomy-of-a-turn.md) | One turn, line by line |
| [Recipes](../07-recipes.md) | "How do I add a…" |
| [Glossary](../08-glossary.md) | Every term, defined |
| [How to play](../how-to-play.md) | The game itself |

---

## Before you start

Read [How to run it](../00-how-to-run.md) and get the game on screen. Play one
dungeon. Lose.

This course will make roughly twice as much sense if you have felt the thing it
is describing.

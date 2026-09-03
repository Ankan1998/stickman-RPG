# 20. Where to go next

> **Where you are:** chapter 20 of 20 · [index](README.md) · previous: [Testing and balancing](19-testing-and-balancing.md)

---

## What you now know

Nineteen chapters ago you had never written a game. Here is what you have picked
up, stated as things you can now *do*:

| | You can now |
|---|---|
| 1 | Explain why game code is structured differently from the software you already write |
| 2 | Write frame-rate-independent motion, and keep the rules clock separate from the program clock |
| 3 | Navigate a scene tree, read screen coordinates, and know when a container will overrule you |
| 4 | Turn input into intents, wire nodes together with signals, and see the state machine your screens already form |
| 5 | Separate rules from presentation, and enforce it with the build system |
| 6 | Design entities that defend their own invariants, and derive state rather than cache it |
| 7 | Build an event log you can replay, test, count and (later) serialise |
| 8 | Make a game deterministic, including the tie-breaks nobody thinks about |
| 9 | Choose a model of time, and implement turns with the Command pattern |
| 10 | Choose a damage formula on purpose, and balance items with budgets |
| 11 | Implement statuses and positioning, and avoid the dead ends they create |
| 12 | Express content as data instead of a class hierarchy |
| 13 | Write a utility AI, and tune it so it is interesting rather than merely optimal |
| 14 | Design the loop *around* the fights: attrition, reset points, escalation and reward cadence |
| 15 | Animate sprites from atlases, and make pixel art look correct |
| 16 | Build audio that does not sound cheap |
| 17 | Build a UI that teaches, and add the juice that makes it feel good |
| 18 | Debug a game with seeds, event logs and diagnostic tests instead of breakpoints |
| 19 | Measure your game instead of guessing about it |

That is a genuine foundation. It is not everything — there is nothing here about
3D, physics, shaders, networking, or platforms — but it is the part that
transfers to every 2D game you will ever build.

---

## The single most important thing left

**Ship something small.**

Not this project. Something *yours*, small enough to finish, finished all the way
to a download link on itch.io.

The reason is not motivational. It is that **the last 10% of a game is a
completely different skill from the first 90%**, and you cannot learn it by
starting more projects. Menus, settings, save files, an icon, a build for a
machine that is not yours, a description, a screenshot, the moment a stranger
plays it and it does not work. None of that appears in a tutorial and all of it
is the actual job.

A finished bad game teaches more than an unfinished good one. This is the most
commonly given advice in game development, and it is given constantly because it
is constantly ignored.

---

## What to build, in order

### 1. Dodge the Creeps (~3 hours)

Godot's official *Your First 2D Game*, in C#. Do it even though you have read
this whole course, because it teaches the **editor workflow** this project
deliberately skips: scenes, the inspector, signals, `.tscn` files, export
templates.

You now know *why* everything in it works, which makes it fast.

### 2. Something with no content (~1 weekend)

Pong, Snake, Breakout, Flappy Bird.

The point is that these have **almost no content**, so all you practise is the
loop, input, collision, state and shipping. Take it all the way to an itch.io
page with a screenshot. That is the exercise.

### 3. Something small with content (~2–4 weeks)

A single-screen roguelike. A card game with twenty cards. A puzzle game with
thirty levels.

Now you meet the real problem: **content is the cost**. Twenty cards means twenty
designs, twenty balance passes, twenty icons, twenty tooltips. You will
understand [chapter 12](12-content-as-data.md) in your bones after this.

### 4. Then your RPG

By now you will know what you actually want, and you will not build the wrong
thing for six months.

---

## Continuing in *this* project

If you want to keep learning here, the exercises are graded. The full plan is in
[the roadmap](../roadmap.md); these are the ones that teach the most per hour:

| | Exercise | Teaches | Difficulty |
|---|---|---|---|
| 1 | Add a skill, a monster and a weapon | The content pipeline | ★ |
| 2 | Fix the [regeneration bug](../07-recipes.md) — write the test first | Your first real engine change | ★★ |
| 3 | Add a **speed toggle** and a skip-animation key | The pacing problem from [ch 17](17-ui-and-game-feel.md) | ★★ |
| 4 | Add **hit pause** on criticals | The best juice technique you have not tried | ★★ |
| 5 | Add `TargetKind.AllEnemies` | Touches `TargetsFor` and `SkillAction` | ★★★ |
| 6 | Move content to JSON | Data-driven content, hot reload | ★★★ |
| 7 | Add **save/load** mid-battle | Serialisation — and why events carry ids | ★★★ |
| 8 | Give statuses **triggers** ("on being hit, reflect 20%") | The system that makes RPGs deep | ★★★★ |
| 9 | Give the AI **lookahead** — add `BattleState.Clone()` | Where "deep" is won or lost | ★★★★ |
| 10 | Rebuild the UI as `.tscn` scenes in the editor | The Godot workflow, properly | ★★★★ |

**Number 8 is the one to be strategic about.** Status *triggers* are the hardest
thing to retrofit, because every skill, every status and the AI's scoring all
have to learn about them. If you know you want reflect damage, on-death effects
or counter-attacks, design for it early.

---

## The traps

These are the ways beginner game projects die. All of them are avoidable and
none of them are avoided by being clever.

### 1. Scope

The number one killer, by a wide margin.

> "Deep turn-based RPG" at full scope is a multi-year project.

Your instinct will be an overworld, twelve dungeons, a story and forty heroes.
Build **one excellent twenty-minute gauntlet**, ship it, and grow it if people
like it.

Note that *this* project — three dungeons, nine fights, no story, no overworld,
placeholder art — is already substantial. Scope down from there, not up.

### 2. Building the engine instead of the game

Writing systems is comfortable. Designing content is hard, and it is where the
game actually lives. If you have spent three weeks on an inventory system and
have no fights, you are hiding.

### 3. Over-architecting before you understand the domain

> You do not yet know what a "skill" is in *your* game. Write the ugly version,
> play it, then abstract.

This course showed you a heavily architected codebase. That architecture is the
**result** of building the thing, not the starting point. If you begin your game
with `IActionResolutionStrategyFactory`, you will spend a month building a
framework for a game you have not designed.

Write it badly. Play it. *Then* apply chapter 5.

### 4. Not playing your own game

> Fun is empirical. It cannot be derived from a design document.

Play it for ten minutes every week. Not testing — *playing*. You will notice
things no test can express.

### 5. Perfectionism about art

Placeholder art is fine for years. This project used stick figures drawn with
`DrawLine` for its entire first version, and none of the mechanics knew or cared
([chapter 5](05-rules-vs-presentation.md)). *Into the Breach* and *Slay the
Spire* both have modest art and are both excellent.

### 6. Comparing your first game to shipped games

*Hades* had a hundred people and years. Your first game will be worse than
everything you admire. That is not a reason to stop; it is arithmetic.

---

## Reading and watching

**Start here:**

- **[Game Programming Patterns](https://gameprogrammingpatterns.com/)** — Robert
  Nystrom, free online. The Command, Component, Event Queue, State and Type
  Object chapters describe *exactly* what is in `Rpg.Core`. If you read one book,
  read this one.

**Then:**

- **[Godot documentation](https://docs.godotengine.org/)** — genuinely good, with
  GDScript and C# side by side.
- **Postmortems for *Slay the Spire* and *Into the Breach*** — the reference class
  for "deep mechanics, modest art, small team". Both talk candidly about
  balancing and iteration.
- **GDC talks on YouTube.** Search "juice it or lose it" for
  [chapter 17](17-ui-and-game-feel.md) in twenty minutes, and "Into the Breach
  design" for a masterclass in removing randomness.

**Communities:**

- **[r/roguelikedev](https://reddit.com/r/roguelikedev)** — the most
  mechanics-literate gamedev community online, and closest to what this project
  is.
- **[r/godot](https://reddit.com/r/godot)** — engine-specific help.
- **itch.io game jams** — a weekend jam will teach you more about scope and
  shipping than a month of reading. Ludum Dare and GMTK Jam are the well-known
  ones.

---

## The design decisions in this project, and when to revisit them

Every decision this course explained was a trade-off. Here is when each one stops
being right:

| Decision | Where | Revisit when |
|---|---|---|
| Round-based turn order | [`TurnQueue.cs`](../../src/Rpg.Core/Combat/TurnQueue.cs) | You want fast actors to get genuinely *extra* turns → ATB |
| Subtractive defence | [`DamageCalculator.cs`](../../src/Rpg.Core/Combat/DamageCalculator.cs) | Stats reach the hundreds and defence trivialises damage |
| Statuses are modifier + DoT + one flag | [`StatusDefinition.cs`](../../src/Rpg.Core/Effects/StatusDefinition.cs) | You want "on hit, reflect 20%" → add triggers |
| One-ply AI, no lookahead | [`ScoringAi.cs`](../../src/Rpg.Core/Ai/ScoringAi.cs) | Fights become winnable by rote |
| Statuses refresh, never stack | [`Actor.cs`](../../src/Rpg.Core/Entities/Actor.cs) | You want stacking poison → add `Stacks` |
| Content lives in C# | [`ContentDatabase.cs`](../../src/Rpg.Core/Content/ContentDatabase.cs) | You pass ~50 skills, or get a designer |
| UI built in code, not scenes | [`GameRoot.cs`](../../game/scripts/GameRoot.cs) | You want to iterate on layout visually — i.e. almost immediately |
| No music, no audio buses | [`Audio.cs`](../../game/scripts/Audio.cs) | Now, honestly. Buses are painful to retrofit. |
| No speed toggle | [`BattleView.cs`](../../game/scripts/BattleView.cs) | The first time you play it for the fortieth time |

---

## One last thing

The most useful habit in this entire codebase is not an architecture. It is that
**every non-obvious decision is written down next to the code, along with what it
cost.**

```csharp
/// This is the single biggest difficulty dial in the game. At 0 - which is
/// where this started - the first dungeon alone ended 52% of campaigns, and
/// nobody in 250 simulated runs ever saw the third.
public const int BreatherPercent = 28;
```

Six months from now you will not remember why that is 28. Neither will anybody
else. A comment that records the *measurement and the reasoning* is worth more
than any amount of clean code, because clean code tells you what it does and this
tells you **why it is not something else**.

Do that, measure instead of guessing, ship something small, and you will be fine.

---

## The whole course

| | Chapter |
|---|---|
| 1 | [What makes games different](01-what-makes-games-different.md) |
| 2 | [The game loop and time](02-the-game-loop-and-time.md) |
| 3 | [Engines and the scene tree](03-engines-and-the-scene-tree.md) |
| 4 | [Input, signals and game flow](04-input-signals-and-game-flow.md) |
| 5 | [Rules vs presentation](05-rules-vs-presentation.md) |
| 6 | [State and entities](06-state-and-entities.md) |
| 7 | [Events and replay](07-events-and-replay.md) |
| 8 | [Randomness and determinism](08-randomness-and-determinism.md) |
| 9 | [Turns, actions and resolution](09-turns-actions-and-resolution.md) |
| 10 | [Numbers: damage and stat design](10-numbers-and-stat-design.md) |
| 11 | [Status effects and space](11-statuses-and-space.md) |
| 12 | [Content as data](12-content-as-data.md) |
| 13 | [Enemy AI](13-enemy-ai.md) |
| 14 | [Progression and the shape of a run](14-progression-and-the-shape-of-a-run.md) |
| 15 | [Sprites and animation](15-sprites-and-animation.md) |
| 16 | [Audio](16-audio.md) |
| 17 | [UI and game feel](17-ui-and-game-feel.md) |
| 18 | [Debugging a game](18-debugging-a-game.md) |
| 19 | [Testing and balancing](19-testing-and-balancing.md) |
| 20 | Where to go next |

Now go and build something.

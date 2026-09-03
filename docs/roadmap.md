# 9. Roadmap

## Already built

Since this roadmap was first written the project grew a great deal, and several
of the items below are done:

- ✅ Ten hero classes, twenty-two monsters, fifty skills, forty-seven weapons
- ✅ Three dungeons with distinct status-effect themes, a hub, and loot
- ✅ Animated sprites, 62 sound effects, impact effects
- ✅ A smarter enemy that targets by threat and focuses the wounded
- ✅ A campaign balance harness measuring 250 complete campaigns

Still open: JSON content loading, an AI that searches rather than scores, saving
and loading, and rebuilding the UI as `.tscn` scenes in the editor.

## Where the scaffold leaves you

A lot of the plan below is already standing in this repo: a tested rules engine,
a turn loop, statuses, cooldowns, a scoring AI, and a playable Godot front end.
Reading it, breaking it, and fixing it *is* the first week's work.

| Week | Goal | What it teaches |
|---|---|---|
| 1 | Get it running ([Getting started](01-getting-started.md)), then read [Anatomy of a turn](06-anatomy-of-a-turn.md) and follow it in the real code. | How the whole thing hangs together. |
| 2 | Godot's official *Your First 2D Game* (Dodge the Creeps), in C#. | Nodes, scenes, signals, the editor. ~3 hours, and the only tutorial you need. |
| 3 | Work through [Recipes](07-recipes.md): add a skill, add a status, add a hero, tune the balance. | The seams of the codebase, hands-on. |
| 4 | Fix the [regeneration bug](07-recipes.md#make-healing-statuses-work-a-real-bug-to-fix). Write a test for it first. | Your first real engine change, well scoped. |
| 5 | Add multi-target skills (`TargetKind.AllEnemies`). | A bigger engine change. Touches `TargetsFor` and `SkillAction`. |
| 6 | Give statuses *triggers* — "on being hit, reflect damage". | The system that makes RPGs deep, and the hardest thing to retrofit later. |
| 7 | Move `ContentDatabase.CreateDefault` into JSON. | Data-driven content. Add a skill without recompiling. |
| 8 | Upgrade `ScoringAi` to search: add `BattleState.Clone()` and score *positions*. | Where "deep" is won or lost. |
| 9 | Rebuild the UI as a real `.tscn` scene in the Godot editor. | The editor, properly. See [why the scene is empty](03-godot-crash-course.md#why-is-our-scene-so-empty). |
| 10 | Save/load a battle mid-fight. Add hit sound effects. | Serialisation; the cheapest polish that exists. |

After that you have a real, tested, extensible combat game. *Then* decide about
overworlds, story and art.

---

## Design decisions already made (and where to revisit them)

| Decision | Where | Revisit when |
|---|---|---|
| Round-based turn order, sorted by Speed | `Combat/TurnQueue.cs` | You want fast actors to get genuinely *extra* turns. Replace with an ATB gauge. |
| Subtractive defence (`raw - Defense/2`) | `Combat/DamageCalculator.cs` | Endgame stats reach the hundreds and defence starts trivialising damage. |
| Statuses are data: modifier + DoT + one flag | `Effects/StatusDefinition.cs` | You want "on hit, reflect 20%". Add a trigger list. |
| One-ply heuristic AI, no lookahead | `Ai/ScoringAi.cs` | Fights feel winnable by rote. Add `Clone()` and search. |
| Re-applying a status refreshes, never stacks | `Entities/Actor.cs` | You want stacking poison. Add a `Stacks` field to `StatusEffect`. |
| Content lives in C# | `Content/ContentDatabase.cs` | Week 6, or once you pass ~50 skills. |

---

## The traps, restated

1. **Do not over-architect before you understand the domain.** You do not yet
   know what a "skill" is in *your* game. Write the ugly version, play it, then
   abstract.
2. **Content is the real cost.** 50 skills × (design + balance + test + icon +
   tooltip) dwarfs the systems that run them. Keep adding content boring.
3. **Play it for ten minutes every week.** Fun is empirical. It cannot be derived
   from a design document.
4. **Scope.** Target one excellent 20-minute combat gauntlet, ship it to itch.io,
   *then* grow it. "Deep turn-based RPG" at full scope is a multi-year project.

---

## Reading

- **[Game Programming Patterns](https://gameprogrammingpatterns.com/)** — Nystrom, free online.
  The Component, Event Queue, State and Type Object chapters describe exactly what
  is in `Rpg.Core`. Read this first.
- **[Godot docs](https://docs.godotengine.org/)** — GDScript and C# side by side.
- **Slay the Spire** and **Into the Breach** postmortems — the reference class for
  "deep mechanics, modest art".
- **[r/roguelikedev](https://reddit.com/r/roguelikedev)** — the most mechanics-literate
  gamedev community online.

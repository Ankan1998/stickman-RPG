# 1. What makes games different

> **Where you are:** chapter 1 of 17 · [index](README.md) · next: [The game loop and time](02-the-game-loop-and-time.md)

---

## The problem

You already know how to write software. So why does game code feel so strange
the first time you see it?

Here is the software you have probably been writing for five years:

```
   a request arrives
        |
   you do some work
        |
   you return an answer
        |
   you stop existing
```

A web request. A CLI command. A background job. Something asks, you answer, you
are done. Between requests your program is **not running**. It has no opinions.
It is not thinking about anything.

Now here is a game:

```
   +-----------------------------------------+
   |   what is the player pressing?          |
   |   how much time passed since last time? |
   |   move everything                       |
   |   resolve what happened                 |
   |   draw the entire screen                |
   +--------------+--------------------------+
                  |  60 times a second
                  +-------------> forever, until they quit
```

Nobody asked it anything. It just runs. Forever. And it has to finish all of
that in **sixteen milliseconds**, or the player sees a stutter.

That is not a small difference. It changes almost every instinct you have.

---

## The four things that are genuinely different

### 1. Your program never stops

There is no "request finished". The program is a **continuous simulation** that
happens to be showing itself to somebody.

Web code is *reactive* — it sleeps until poked. Game code is *active* — it runs
whether or not anything is happening, because "nothing is happening" still has
to be drawn, sixty times a second.

This is why the phrase **game loop** is the first thing anybody teaches you, and
it is chapter 2.

### 2. Time is a first-class input

In web code, "how long did that take?" is a metric you record. In a game it is
an **argument you are handed**. Every frame, your code is told how long the
previous frame took, and everything that moves must scale itself by that number.

```csharp
// Wrong. On a fast machine this is a blur; on a slow one it is a crawl.
position.X += 5;

// Right. Five units per SECOND, on every machine.
position.X += 5 * delta;
```

Godot passes you `delta` in every frame callback. You can see it doing real work
in [`SpriteAnimator._Process`](../../game/scripts/SpriteAnimator.cs), which is
the one place in this project that genuinely counts time by hand.

### 3. Correct is not the same as good

This is the one that catches experienced developers hardest.

Your login endpoint is correct or it is not. There is no category of "the login
is technically correct but it doesn't feel very good". Games have exactly that
category, and it is most of the work.

A hit that deals the right damage, updates the right health bar, and writes the
right log line can still be **wrong**, because:

- there was no pause before the number appeared, so it felt like nothing happened
- the sound played 100ms late, so it felt disconnected from the swing
- the health bar snapped instead of sliding, so the player never noticed it

None of that is a bug in any test you could write. All of it is the difference
between a game people play and a game people close. The industry word is **game
feel**, or **juice**, and it gets [its own chapter](15-ui-and-game-feel.md).

> **The real lesson:** in games the presentation is not a thin veneer over the
> logic. It is half the product. Which is exactly *why* chapter 4 insists on
> keeping it rigorously separate from the rules — the two halves are equally
> important, and equally deserve room to be done well.

### 4. Content dwarfs code

A web app might have fifty endpoints. This small RPG already has:

| Content | How many |
|---|---|
| Skills | 55 |
| Weapons | 47 |
| Monsters | 22 |
| Statuses | 14 |
| Heroes | 10 |
| Encounters | 9 |

That is **157 distinct things**, in a project with roughly 4,000 lines of actual
logic. A shipped RPG has tens of thousands.

If each of those had been a C# class, this project would be impossible to
maintain and impossible to balance. They are not classes. They are **data** —
rows of numbers fed into a handful of generic systems. That idea is
[chapter 11](11-content-as-data.md), and it is probably the single biggest
practical difference between hobby game code and professional game code.

---

## The vocabulary, all at once

You will meet these constantly. None of them are complicated; they are just
unfamiliar, and being unsure what a word means is exhausting.

| Word | What it actually means |
|---|---|
| **Frame** | One pass through the loop. One drawn image. |
| **Tick** | One pass through the *logic*, which may run at a different rate. |
| **Delta / dt** | Seconds since the previous frame. Usually about 0.016. |
| **FPS** | Frames per second. 60 is the usual target. |
| **Entity / Actor** | One "thing" in the world. A hero, a goblin, a bullet. |
| **Sprite** | A 2D image drawn on screen. |
| **Sprite sheet / atlas** | Many images packed into one big image file. |
| **Scene** | A reusable chunk of game: a level, a menu, a character. |
| **Node** | One element in Godot's tree. A sprite, a label, a sound player. |
| **Asset** | Any content file. Image, sound, font, level data. |
| **Juice** | Feedback that makes an action feel good. Shake, flash, sound. |
| **Game state** | Everything the game currently knows. |
| **Deterministic** | Same inputs produce the same outputs, every single time. |
| **Turn-based** | Time advances only when somebody acts. |
| **Real-time** | Time advances on its own, whether you act or not. |
| **Seed** | The number that makes "random" reproducible. |
| **Balance** | Tuning numbers until the game is fair and interesting. |
| **Encounter** | One fight. |
| **Run** | One playthrough, from the start to death or victory. |

There is a fuller [glossary](../08-glossary.md) covering this project's own
terms.

---

## "But mine is turn-based, so none of this applies"

It applies. This is worth being precise about, because it confuses almost
everybody once.

**Turn-based describes the rules, not the program.**

The *rules* of this game advance only when somebody acts. The *program* runs
sixty times a second the entire time, because it still has to draw the screen,
animate the idle sprites, run the tweens, and watch for a click.

```
   THE RULES                          THE PROGRAM
   ---------                          -----------
   frozen, waiting for you            running at 60fps
   nothing changes until              drawing, animating,
   you pick an action                 listening, tweening
```

Two different clocks. Confusing them is a classic beginner bug, and chapter 2
pulls them apart properly.

---

## In this project

Here is the entire idea of the codebase in one picture. It is worth memorising
now, because every later chapter refers back to it:

```
   +----------------------------------------------------------+
   |  src/Rpg.Core/        THE RULES                           |
   |                                                           |
   |  Plain C#. No Godot. No drawing. No waiting.              |
   |  Ask it to take a turn and it INSTANTLY hands back a      |
   |  list of everything that happened.                        |
   +-----------------------+-----------------------------------+
                           |  List<GameEvent>
                           v
   +----------------------------------------------------------+
   |  game/                THE SCREEN                          |
   |                                                           |
   |  Godot. Takes that list and acts it out slowly, with      |
   |  animation, sound, pauses and effects.                    |
   +----------------------------------------------------------+
```

The arrow points one way only. `Rpg.Core` has never heard of Godot and *cannot*
call into it — not by convention or good intentions, but because the compiler
will not let it. That is [chapter 4](04-rules-vs-presentation.md).

**The payoff, stated up front so you know why you are reading all this:** because
the rules need no screen, this project can simulate **250 complete campaigns —
2,250 fights — in about a second, inside a unit test.** That is how its
difficulty was tuned. Not by guessing. By measuring. See
[chapter 16](16-testing-and-balancing.md).

---

## What it costs you

Every chapter ends with the price, because honesty is more useful than
enthusiasm.

The separation above costs you **indirection**. When a hero swings a sword, the
damage is not calculated anywhere near the code that draws the swing. You have to
look in two places, and hold both in your head.

For a tiny game that is genuinely more work than putting the damage in the click
handler. It starts paying the moment your game needs balancing, replays, or
tests — which for an RPG is almost immediately, but for a two-week game jam entry
may be never. Jam games are frequently written as one glorious tangled file *on
purpose*, and that is correct engineering for that context.

Know which one you are building.

---

## Try it

Before the next chapter, do two things.

**1. Play the game, and lose.**

```bash
godot --path game
```

Take Warrior, Cleric, Mage into the Warrens. Notice that the Warrior cannot
reach the back rank, and that the Mage cannot cast from the front.

**2. Run the rules with no game attached.**

```bash
dotnet test --filter "FullyQualifiedName~CampaignHarness"
```

That just played hundreds of complete campaigns. No window opened. Nothing was
drawn. That is the whole architecture, demonstrated in about a second.

---

**Next:** [Chapter 2 — The game loop and time](02-the-game-loop-and-time.md)

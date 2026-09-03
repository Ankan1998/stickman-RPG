# 8. Randomness and determinism

> **Where you are:** chapter 8 of 20 · [index](README.md) · previous: [Events and replay](07-events-and-replay.md) · next: [Turns, actions and resolution](09-turns-actions-and-resolution.md)

---

## The problem

Games need randomness. Critical hits, loot drops, which monster the AI picks
when two are equally good. Without it, every playthrough is identical and there
is nothing to come back for.

So you write the obvious thing:

```csharp
if (Random.Shared.Next(100) < critChance)
    damage *= 2;
```

And you have just quietly destroyed four things you were going to need.

### What you lost

**1. Reproducible bugs.** A player reports "my Cleric died on the second
encounter and I don't know why". You cannot see what they saw. Ever. That fight
existed once and is gone.

**2. Testable combat.** You cannot write `Assert.Equal(25, damage)` when damage
sometimes doubles at random. So you write vaguer tests, which catch fewer bugs.

**3. Balance measurement.** You want to know "does the Warrens kill 11% of
parties?" You run the simulation twice and get 9% and 14%. Is that noise, or did
your change do something? You cannot tell.

**4. Replays, seeded runs and networked play.** All of them need the same
sequence of numbers to happen again. None of them are possible now.

That is a lot to give up for one line of convenience.

---

## The idea: seeded pseudo-randomness

The numbers were never really random. `Random` is a **pseudo**-random number
generator: it starts from a number called a **seed** and applies arithmetic to
produce a stream that *looks* random but is completely determined.

```
   seed 4242  -->  73, 12, 99, 4, 51, 88, ...     always exactly this
   seed 4243  -->  8, 61, 27, 90, 33, 2, ...      always exactly this
```

Same seed, same sequence. Every time. On every machine. Forever.

So the entire trick is: **stop letting the seed be chosen for you, and start
writing it down.**

That single change turns randomness from a source of chaos into a *feature*:

| With a known seed you get | How |
|---|---|
| Reproducible bug reports | Player sends you `seed 8815`. You see their exact fight. |
| Exact tests | Seed 42 always deals exactly 25 damage. Assert on it. |
| Meaningful balance runs | Seeds 1–250, same 250 campaigns every time. |
| Roguelike daily challenges | Everyone plays seed-of-the-day. Free. |
| Replays | The seed plus your inputs *is* the replay. |
| Lockstep multiplayer | Two machines, same seed, same rules, same result. |

---

## In this project

### The interface

Every die roll in the game goes through one small interface, in
[`IRandomSource.cs`](../../src/Rpg.Core/Rng/IRandomSource.cs):

```csharp
public interface IRandomSource
{
    int NextInt(int minInclusive, int maxExclusive);
    double NextDouble();
}
```

Two methods. That is deliberate — implementing a fake is trivial. The convenient
helpers are extension methods on top:

```csharp
public static bool Chance(this IRandomSource rng, int percent) =>
    percent > 0 && rng.NextInt(0, 100) < percent;

public static int Range(this IRandomSource rng, int min, int max) =>
    rng.NextInt(min, max + 1);

public static T Pick<T>(this IRandomSource rng, IReadOnlyList<T> items) =>
    items[rng.NextInt(0, items.Count)];
```

**The rule this project enforces:** never call `new Random()` anywhere in
`Rpg.Core`. The generator is always passed in.

> **A C# aside that cost this project its very first build error.** Extension
> methods are only visible if you `using` the namespace they live in. Forgetting
> `using Rpg.Core.Rng;` produces `'IRandomSource' does not contain a definition
> for 'Chance'` — which sounds like the interface is broken, and is not.

### Why not `System.Random`?

This project ships its own generator,
[`SplitMix64Random`](../../src/Rpg.Core/Rng/SplitMix64Random.cs), and the reason
is important:

**Microsoft has changed the algorithm inside `System.Random` between .NET
versions.** That is entirely reasonable of them — it is documented as an
implementation detail. It is also fatal if your saved replays, seeded runs and
balance baselines depend on the exact sequence.

So the project owns fifteen lines that will behave identically in ten years:

```csharp
private ulong NextUInt64()
{
    ulong z = (_state += 0x9E3779B97F4A7C15UL);
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
    return z ^ (z >> 31);
}
```

You do not need to understand the constants — they come from the published
SplitMix64 algorithm and are chosen to scramble bits well. You do need to know
it is **not cryptographically secure** and must never be used for passwords or
tokens. For dice, it is ideal: tiny, fast, and yours.

One detail worth stealing:

```csharp
public ulong State => _state;
```

Save that number alongside a game save and you can resume a fight mid-battle
with the randomness continuing exactly where it left off.

### The test double

[`FixedRandom`](../../src/Rpg.Core/Rng/FixedRandom.cs) returns numbers *you*
choose. So a test that needs a critical hit does not roll for one and hope:

```csharp
var rng = new FixedRandom(0);       // "always roll 0" -> crit chance 5% always fires
```

This is why `IRandomSource` is an interface rather than a class. Dependency
injection, applied to dice.

### One generator per campaign

From [`Campaign`](../../src/Rpg.Core/Progression/Campaign.cs):

```csharp
// ONE random source for the entire campaign, so the whole thing - every
// fight, every crit, every loot roll - replays from a single number.
_rng = new SplitMix64Random(seed);
```

Not one per battle. Not one per actor. **One.** Every fight, every critical hit,
every loot roll in nine encounters draws from the same stream. So a single
`ulong` reproduces an entire two-hour run.

The seed is shown to the player on the title and results screens, which costs
nothing and makes every bug report reproducible.

---

## The part that surprises everyone: consumption order

Here is the subtlety that turns "I used a seed" into "my game is actually
deterministic".

**It is not enough to use the same seed. You must consume the numbers in the same
order.**

The generator is a queue. Every call takes the next number. If your code takes
them in a different order — even once, even harmlessly — everything downstream
shifts, and the battle plays out completely differently.

From [`SkillAction.Execute`](../../src/Rpg.Core/Combat/SkillAction.cs):

```csharp
// Roll the crit BEFORE computing damage, and always in this order.
//
// Consuming the random number generator in a fixed order is what makes a
// seed reproduce a battle exactly. Swap these two lines and every saved
// replay and every determinism test breaks.
bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);

int damage = DamageCalculator.Compute(
    Actor.CurrentStats, Target.CurrentStats, Skill.Power, isCritical);
```

Reordering those two lines is not a refactor. It is a breaking change to every
replay that has ever existed.

### The traps

These are the ways determinism actually dies in practice:

| Trap | Why it breaks |
|---|---|
| **Rolling "just in case"** | Rolling a crit you then discard still advances the stream. |
| **Randomness in the UI** | A sparkle that calls the game RNG makes the *rules* depend on frame rate. |
| **Iterating a `Dictionary`** | .NET does not guarantee enumeration order. Order your keys. |
| **Parallelism** | Two threads drawing from one generator is a race. |
| **Floating-point across platforms** | The same double arithmetic can differ subtly between CPUs. |
| **Tie-breaking by list order** | See below — this is the sneaky one. |

That last one deserves its own section, because it is the one you will not
predict.

---

## Tie-breaking: the hidden determinism killer

Two actors have Speed 10. Who goes first?

The naive answer — "whoever happens to be first in the list" — is a landmine.
Because *list order is not stable*. Load a save that rebuilds the actor list in a
different order, add a hero, reorder a content file, and the tie now breaks the
other way. Your identical seed now plays a completely different battle, and the
cause is invisible.

[`TurnQueue.BeginRound`](../../src/Rpg.Core/Combat/TurnQueue.cs) refuses to have
that bug:

```csharp
_order.AddRange(actors
    .Where(a => a.IsAlive)
    .OrderByDescending(a => a.CurrentStats.Speed)

    // Tie-break by id, NEVER by position in the list.
    //
    // This looks fussy and is load-bearing. If ties fell back to list order,
    // then loading a save that happened to rebuild the actor list in a
    // different order would replay the whole battle differently - a genuinely
    // horrible bug to track down.
    .ThenBy(a => a.Id, StringComparer.Ordinal));
```

`StringComparer.Ordinal`, not the default — because the default string comparison
is **culture-sensitive**, and sorting can differ between a machine set to English
and one set to Turkish. Ordinal compares raw character values and is identical
everywhere.

The same discipline appears in the AI, in
[`ScoringAi.ChooseAction`](../../src/Rpg.Core/Ai/ScoringAi.cs):

```csharp
// Tie-break on the LABEL, never on position in the list.
bool better = score > bestScore
    || (score == bestScore && string.CompareOrdinal(option.Label, best.Label) < 0);
```

Without this, changing the order skills are *declared in* would silently change
every AI decision in the game.

> **The general rule:** anywhere you break a tie, break it on something
> **intrinsic and stable** — an id, a name — never on incidental ordering.

---

## Designing randomness that feels fair

A separate matter from determinism, but you will need it, so it belongs here.

**True randomness feels broken.** Flip a fair coin twenty times and you will
usually see a run of four or five heads. Players do not experience that as fair;
they experience it as the game cheating. A 25% miss chance will, reliably,
produce four misses in a row for somebody, and that somebody will post about it.

Techniques used in shipped games:

| Technique | What it does | Seen in |
|---|---|---|
| **Pseudo-random distribution** | Chance starts low and *rises* each failure, resetting on success | Dota 2 crits |
| **Bad-luck protection / pity** | Guarantee the drop after N failures | Most loot games |
| **Shuffled bag** | Put 1 hit and 3 misses in a bag, draw without replacement | Tetris piece order |
| **Fudging in the player's favour** | Silently boost odds when the player is losing | More games than admit it |
| **Removing randomness** | Make the number fixed and put the variance in *decisions* | Into the Breach |

**This project uses none of them**, and that is a deliberate design position
worth stating: its randomness is confined to critical hits and loot rarity, and
every other number is fixed. Damage is not a range. A skill that says 25 does 25.

The variance in a run comes from **decisions and attrition**, not from dice. This
is the *Into the Breach* school of design, and it makes the game much easier to
balance — because when a party dies, it died because of the choices, not because
the dice went cold.

---

## What it costs you

**You must pass the generator everywhere.** `BattleState` holds one, `Campaign`
holds one, every constructor takes one. `Random.Shared` is genuinely more
convenient, right up until the day you need any of the four things at the top of
this chapter.

**Determinism is fragile and easy to break silently.** Nothing crashes. A test
that asserts an exact damage number just... starts failing, and the cause is a
reordered line in a different file. The `TheSameSeedAlwaysReplaysTheIdenticalBattle`
test exists precisely to catch that.

**Fixed damage can feel flat.** Some players like the drama of a damage range.
This project trades that for balanceability. Both are valid; be aware you are
choosing.

---

## Try it

**1. Watch a seed reproduce itself.**

```csharp
[Fact]
public void SameSeedSameBattle()
{
    Assert.Equal(BattleRunner.Run(42).Log, BattleRunner.Run(42).Log);
}
```

It passes because events are records and compare by value
([chapter 7](07-events-and-replay.md)).

**2. Break determinism on purpose.** In
[`SkillAction.Execute`](../../src/Rpg.Core/Combat/SkillAction.cs), move the crit
roll to *after* the damage calculation. Run `dotnet test`. Watch the balance
harness numbers move — the same seeds now produce different campaigns, because
every subsequent draw shifted by one. Put it back.

**3. Break the tie-break.** In
[`TurnQueue.BeginRound`](../../src/Rpg.Core/Combat/TurnQueue.cs), delete the
`.ThenBy(a => a.Id, ...)` line. The tests may well still pass — which is exactly
the point. The bug it prevents only appears once something reorders the actor
list, months later, for an unrelated reason.

---

**Next:** [Chapter 9 — Turns, actions and resolution](09-turns-actions-and-resolution.md)

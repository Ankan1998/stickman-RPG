# 13. Enemy AI

> **Where you are:** chapter 13 of 20 · [index](README.md) · previous: [Content as data](12-content-as-data.md) · next: [Progression and the shape of a run](14-progression-and-the-shape-of-a-run.md)

---

## The problem

It is the goblin's turn. It has three skills and four possible targets. **What
does it do?**

The two obvious answers are both bad.

**Random.** Picks a skill, picks a target. Trivial to write, and it makes your
game feel *broken*. Players do not read random as "unpredictable"; they read it
as "the AI is stupid", and once they think that, the game stops being a contest.

**Hard-coded rules.**

```csharp
if (myHealth < 30) Flee();
else if (target.Health < 20) Attack(target);
else if (hasBuff) Buff();
else Attack(RandomTarget());
```

Works for one monster. By the fifth monster type it is a swamp of conditions
nobody dares edit, every new skill needs a new branch in every monster, and
"why did it do that?" is unanswerable.

---

## The idea: score every option, pick the best

This is called **utility AI**, and it is the workhorse of game AI. It is much
simpler than it sounds:

```
   1. Ask the game for every legal move.
   2. Give each one a number.
   3. Do the one with the biggest number.
```

That is it. That is the whole algorithm.

The intelligence is not in the algorithm — it is in **how you score**. And that
turns out to be exactly the right place for it, because scoring is *data you can
tune*, not *logic you have to rewrite*.

### Why this beats the alternatives

- **It handles new content for free.** Add a skill and the AI evaluates it
  immediately, with no new branches anywhere.
- **It is explainable.** "It healed because healing scored 47 and attacking
  scored 31." You can print the scores.
- **It is tunable by a designer.** The behaviour lives in about a dozen numbers.
- **It cannot cheat**, because the options come from the same `LegalActions` the
  player's menu is built from ([chapter 9](09-turns-actions-and-resolution.md)).

---

## In this project

[`ScoringAi.ChooseAction`](../../src/Rpg.Core/Ai/ScoringAi.cs), complete:

```csharp
public static IAction ChooseAction(Battle battle, Actor actor)
{
    List<IAction> options = battle.LegalActions(actor);

    IAction best = options[0];
    double bestScore = double.NegativeInfinity;

    foreach (IAction option in options)
    {
        double score = Score(option);

        // Tie-break on the LABEL, never on position in the list.
        bool better = score > bestScore
            || (score == bestScore && string.CompareOrdinal(option.Label, best.Label) < 0);

        if (better) { best = option; bestScore = score; }
    }

    return best;
}
```

Nineteen lines. Everything interesting is in `Score`.

### Every knob in one place

This is the part worth copying most:

```csharp
// ========================================================================
//  EVERY KNOB THE AI HAS, IN ONE PLACE.
//
//  Treat these as GAME DESIGN VALUES, not as code. You should be able to
//  change how the monsters behave without reading a single line below.
// ========================================================================

public const double LethalBonus              = 60.0;
public const double DamageWeight             = 1.0;
public const double HealWeight               = 1.3;
public const double EmergencyHealBonus       = 25.0;
public const double EmergencyHealthFraction  = 0.35;
public const double StunValue                = 16.0;
public const double DamageOverTimeWeight     = 0.6;
public const double BuffValue                = 10.0;
public const double PassValue                = 0.1;
public const double RepositionValue          = 0.4;
public const double ThreatWeight             = 0.30;
```

Eleven numbers define the entire personality of every enemy in the game. Somebody
who has never read the scoring function can retune the AI by editing this block.

### What the scoring actually says

```csharp
// Overkill is wasted. 40 damage into a 6 HP goblin is worth 6, not 40.
int useful = Math.Min(expected, target.Health);
score += useful * DamageWeight;

// Killing removes every future turn that actor would have had.
if (expected >= target.Health)
    score += LethalBonus;
```

Three ideas in there, each of which is a real lesson about turn-based games:

**Overkill is worthless.** Without this clamp, the AI fires its biggest attack at
the weakest target every time — which looks idiotic and wastes its best skill.

**Killing is worth far more than damage.** `LethalBonus` is 60, when a big hit is
about 25. Because removing an actor removes *all of their future turns*. This is
the action-economy insight from [chapter 10](10-numbers-and-stat-design.md),
expressed as a number.

**Damage over time is discounted:**

```csharp
// 12 damage spread across three turns is worth less than 12 damage now,
// because the fight may end first and the target may be healed in between.
//
// Set this to 1.0 and the AI starts preferring a 2-damage poison dart over
// a 10-damage club - exactly the kind of quietly terrible play that makes
// an opponent feel stupid without the player being able to say why.
public const double DamageOverTimeWeight = 0.6;
```

That comment describes a real class of AI bug: not *obviously* broken, just
persistently slightly wrong in a way players feel but cannot articulate.

### The threat model: who to hit

Damage alone tells you who is *easiest* to kill. It does not tell you who is
*worth* killing. That is
[`ThreatModel`](../../src/Rpg.Core/Ai/ThreatModel.cs):

```csharp
public static double ThreatOf(Actor target)
{
    double threat = 0;
    StatBlock stats = target.CurrentStats;

    threat += stats.Attack * PerPointOfAttack;
    threat += stats.Speed  * PerPointOfSpeed;

    foreach (SkillDefinition skill in target.Skills)
    {
        // Healing is the thing that most reliably beats a monster pack.
        if (skill.Heals)
            threat += HealerThreat + skill.Healing * PerPointOfHealing;

        // So is a buff aimed at somebody else.
        if (skill.Target == TargetKind.SingleAlly && skill.AppliesStatus is not null)
            threat += SupportThreat;

        if (skill.Drains)
            threat += skill.LifestealPercent * 0.25;
    }

    // ...and finish what somebody already started.
    double missing = 1.0 - (double)target.Health / Math.Max(1, target.MaxHealth);
    threat += missing * FocusOnWounded;

    return threat;
}
```

Notice it **reads the target's skill list** to decide they are a healer. Nobody
tagged the Cleric as "the healer". Add a new healing hero tomorrow and the
monsters will hunt them too, automatically. That is content-as-data
([chapter 12](12-content-as-data.md)) paying off in a system that was written
before that hero existed.

---

## The most important story in this chapter

Here is what happened when this AI was first turned on with `ThreatWeight` at
**0.55**.

**The campaign clear rate fell to 5%.**

Every monster in the room lasered the Cleric down inside two rounds. Every fight.
Without a healer, the party could not survive six more encounters, so essentially
every run ended in dungeon two.

The AI was working **perfectly**. It had correctly identified the most valuable
target and focused it. It was, by any technical measure, playing well.

It was also **not fun**, and — importantly — **not fair**, in a way worth being
precise about:

> A clever opponent is good; one that always removes your best piece first, with
> no taunt or guard mechanic available to stop it, is just unfair.

That is the crux. The player had **no counterplay**. There was no taunt, no
guard, no way to protect the Cleric. The AI was exploiting a hole in the *design*,
not outplaying the *player*.

Two fixes, and both are worth knowing:

1. **Turn the weight down**, 0.55 → 0.30. The monsters still prefer the healer;
   they no longer ignore everything else to reach her.
2. **Give the player counterplay.** [Positioning](11-statuses-and-space.md) means
   you can put the Cleric in rank 3, where melee cannot reach her at all.

> **The general lesson, and it is a big one: your AI's job is not to win. It is
> to make the player's decisions matter.** An unbeatable AI is trivial to write —
> let it see everything and always pick optimally. An AI that is *interesting to
> play against* is the actual craft.
>
> And when your AI feels unfair, ask first whether it is exploiting a hole in
> your design rather than whether it is too smart. The Cleric problem was a
> *missing mechanic*, not a *broken AI*.

---

## Deliberately dumb: one ply

This AI looks exactly one move ahead. It never asks "if I do this, what will they
do next?" No minimax, no tree search, no lookahead.

That is a choice, and the file argues it:

- **Chess-style search needs a reliable evaluation function.** In an RPG with
  statuses, cooldowns and positioning, "how good is this board?" is genuinely
  hard to answer — much harder than in chess.
- **It gets exponentially expensive.** Six actors with ten options each is a
  million positions at three plies.
- **It is not obviously more fun.** A monster that plays a perfect four-move
  combo is frequently just frustrating.

One-ply scoring produces monsters that look smart — they focus the healer, they
finish the wounded, they use their stun on the fast character — for a fraction of
the complexity.

> **Start here.** Add lookahead only when you can point at a specific decision
> that is visibly stupid *and* that lookahead would fix.

### The documented upgrade path

The file lists two next steps, and the second is excellent value:

**Personality.** Multiply the weights per monster type. A berserker weights
damage ×2 and healing ×0; a shaman weights statuses ×3. Cheap to implement, and
it makes encounters feel *authored* rather than generated — the same scoring
function producing recognisably different creatures.

---

## Making the AI legible

If a player cannot tell *why* an enemy did something, a smart AI and a random one
feel identical. Three things this game does:

- **The combat log names the skill and the target** — "Goblin Archer uses Rusty
  Bow on Cleric" — so the pattern of focus becomes visible over a few rounds.
- **The reach diagram is on every button**, so "why did it hit her and not him?"
  has an answer the player can see.
- **The strategy guide says it out loud.** From
  [how-to-play](../how-to-play.md): *"The monsters rate each of your heroes and go
  for whoever is most dangerous, which is usually the Cleric. Expect her to be
  focused."*

Telling the player how the AI thinks does not make the game easier. It turns
"unfair" into "a problem I can plan around", which is the whole difference.

---

## What it costs you

**Tuning eleven interacting numbers is genuinely hard.** They are not
independent. Raising `LethalBonus` implicitly lowers the relative value of
healing. The only honest way to tune them is to *measure*
([chapter 19](19-testing-and-balancing.md)) — which is exactly how
`ThreatWeight` got to 0.30.

**Scoring runs every turn for every actor.** `LegalActions` allocates a fresh
list, and each option is scored with a damage calculation. At six actors this is
free. At two hundred units it would need work.

**It cannot bluff, retreat, or plan.** One-ply AI has no concept of a multi-turn
strategy. It will never set up a combo, and it will never sacrifice a unit for
position. If your game *needs* that, utility scoring is the wrong tool.

**It is only as good as your threat model.** If `ThreatOf` misses something that
matters — a hero whose real value is a rare debuff — the AI will confidently
ignore the right target, and it will look stupid in a way that is hard to
diagnose.

---

## Try it

**1. Watch it think.** Add to `ChooseAction`:

```csharp
Console.WriteLine($"  {score,7:F1}  {option.Label}");
```

Run one battle from a test. You will see the entire decision, ranked. This is the
single best way to understand — and debug — a utility AI.

**2. Recreate the disaster.** Set `ThreatWeight = 0.55` and run:

```bash
dotnet test --filter "FullyQualifiedName~CampaignHarness"
```

Watch the clear rate collapse. Then set it to `0.0` and watch the monsters become
harmless idiots who hit whoever is nearest. The interesting range is narrow.

**3. Build a personality.** Add a `berserker` multiplier that doubles
`DamageWeight` and zeroes `HealWeight`, and apply it to the Orc Brute. Fight it
and see whether you can *feel* the difference — that is the bar an AI change has
to clear.

---

**Next:** [Chapter 14 — Progression and the shape of a run](14-progression-and-the-shape-of-a-run.md)

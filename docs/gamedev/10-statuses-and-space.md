# 10. Status effects and space

> **Where you are:** chapter 10 of 17 · [index](README.md) · previous: [Numbers: damage and stat design](09-numbers-and-stat-design.md) · next: [Content as data](11-content-as-data.md)

---

## The problem

A fight where everybody hits each other until somebody falls over is not a game.
It is arithmetic with a health bar.

Two mechanics turn arithmetic into tactics, and almost every turn-based game uses
both:

- **Duration** — things that persist across turns. Poison, buffs, stuns.
- **Space** — where you are standing changes what you can do.

They are in one chapter because they are the same idea: *a decision whose
consequences outlive the turn you made it in.*

---

# Part 1: Status effects

## The idea

A status effect is a temporary modification to an actor. Three flavours cover
almost everything:

| Kind | Does | Examples here |
|---|---|---|
| **Damage over time** | Chips health each turn | poison (4/turn), burning (9/turn), bleed |
| **Stat modifier** | Changes stats while active | blessed (+atk), cursed (−atk −def), chilled (−6 spd) |
| **Action denial** | Stops you acting at all | stun, webbed |

This game has fourteen. From
[`Statuses.cs`](../../src/Rpg.Core/Content/Statuses.cs).

## The template/instance split, again

You met this in [chapter 5](05-state-and-entities.md), and statuses are the
clearest example:

```csharp
// Shared, immutable, one per status TYPE in the entire game.
public sealed record StatusDefinition(
    string Id, string Name, string Description,
    StatBlock Modifier,
    int DamagePerTurn = 0,
    bool PreventsAction = false,
    string Icon = "");

// One per AFFECTED ACTOR. Mutable, because the countdown changes.
public sealed class StatusEffect
{
    public StatusDefinition Definition { get; }
    public int RemainingTurns { get; private set; }
    public void Tick() => RemainingTurns--;
}
```

Ten poisoned goblins means **one** `StatusDefinition` and **ten**
`StatusEffect`s.

## Three design decisions worth stealing

### 1. When do they tick?

From [`Battle.TakeTurn`](../../src/Rpg.Core/Combat/Battle.cs):

```csharp
// End-of-turn statuses. Deliberately AFTER acting, so a 1-turn poison
// still gets to deal its damage once before wearing off.
TickStatuses(actor, log);
```

**At the end of the bearer's own turn.** Not at end of round, not at start of
turn. This means every status ticks exactly once per round regardless of turn
order, and a 1-turn poison always deals damage at least once.

Tick at the *start* of a turn instead, and applying a 1-turn poison to somebody
who has already acted means it expires having done nothing — technically
defensible, and it reads as a bug to every player who sees it.

### 2. Stack or refresh?

Somebody poisons an already-poisoned goblin. What happens?

| Option | Consequence |
|---|---|
| **Stack** | Two poisons, 8 damage a turn. Spamming the cheap poison skill becomes optimal. |
| **Refresh** | One poison, duration reset. Poison is a *setup* move you use once. |

This game refreshes:

```csharp
public void Refresh(int turns) => RemainingTurns = Math.Max(RemainingTurns, turns);
```

Two things there. It is `Math.Max`, so a fresh 3-turn poison never *shortens* an
existing 5-turn one — a small correctness detail that is easy to get wrong. And
the decision is documented as a decision:

> This is a design decision, not a technical one. If you want stacking poison,
> add a `Stacks` field here and have `Battle.TickStatuses` multiply by it.

### 3. Status modifiers are just addition

Because `CurrentStats` sums the modifiers of every active status
([chapter 5](05-state-and-entities.md)), a debuff needs no special handling
anywhere:

```csharp
new StatusDefinition("cursed", "Cursed", "...",
    Modifier: new StatBlock(0, -4, -3, 0, 0),   // -4 Attack, -3 Defense
    ...)
```

That is the whole implementation of Cursed. No code. A row of numbers. Every
system that reads `CurrentStats` — damage, turn order, the AI's scoring, the UI's
preview — accounts for it automatically, and always will.

## The deliberate bug that is left in

There is a comment in `TickStatuses` that is worth reading carefully:

```csharp
// NOTE FOR LATER: this "> 0" is why a regeneration status with a
// negative DamagePerTurn silently does nothing. Fixing that is a
// deliberate exercise - see docs/07-recipes.md.
if (status.Definition.DamagePerTurn > 0 && actor.IsAlive)
```

A status with `DamagePerTurn: -5` would sensibly mean "heal 5 a turn". It does
nothing, because of that `> 0`.

It is left in on purpose as an exercise — but the general lesson is worth more
than the exercise. **When a guard clause silently swallows a whole category of
input, say so where somebody will read it.** Half the mysterious behaviour in
game code is a reasonable-looking condition quietly excluding a case nobody
thought about.

## Statuses as difficulty identity

The three dungeons here are differentiated almost entirely by status, not by
damage:

| Dungeon | Threat | What it does to you |
|---|---|---|
| The Warrens | **Poison** — 4/turn | Slow. Outrunnable if the fight is short. |
| The Ember Halls | **Burning** — 9/turn for 2 | No healing outruns it. Kill the caster or eat it. |
| The Frozen Crypt | **Chill / Curse / Sunder** | Takes your *stats*: −6 Speed, −4 Attack, −6 Defense |

The third one is the interesting design. The Crypt does not out-damage you — it
**takes away the thing you were relying on**. An all-damage party stops working
there because their damage is being cursed away, which forces a party change at
the hub.

That is how you make three dungeons feel different without inventing three combat
systems. Same rules, different statuses.

---

# Part 2: Space

## The problem

If everybody can hit everybody, position is decoration. Your Mage and your
Warrior have exactly the same tactical situation, which means your party has no
shape, and choosing it involves no thought.

## The idea: reach is a rule

Both sides stand in a line facing each other. **Position 1 is the front.**

```
        your party                                   the enemy
   [3]      [2]      [1]          VS          [1]      [2]      [3]
  Mage    Cleric   Warrior                  Goblin    Rat     Archer
  back  ---------> front                    front  ---------> back
```

Every skill declares **two** patterns, written front-first:

| Pattern | Means |
|---|---|
| **Launch** `##--` | You can only use it from ranks 1–2 |
| **Target** `--##` | It only reaches ranks 3–4 |

```csharp
public readonly record struct Ranks(int Mask)
{
    public const int Max = 4;
    public bool Includes(int rank) => (Mask & (1 << (rank - 1))) != 0;
    public string Diagram => /* "##--" */;
}
```

A bitmask, wrapped in a type that knows how to print itself. That `Diagram`
property matters more than it looks — it is shown directly to the player on every
skill button, so the notation the code uses and the notation the player learns
are literally the same string.

Some real skills:

| Skill | Launch | Reaches | In English |
|---|---|---|---|
| Slash | `##--` | `##--` | A sword. Front half only, both ends. |
| Arrow | `-###` | `####` | Useless jammed against the enemy; reaches anything. |
| Aimed Shot | `--##` | `--##` | A sniper shot. From the back, *at* the back. |
| Healing Word | `####` | `####` | Position-blind. |
| Soul Rip *(wraith)* | `####` | `####` | Ignores the formation. Nothing protects you. |

## Where it plugs in

The beautiful part is how *little* code this takes, because it slots into
`LegalActions` — the single source of moves from
[chapter 8](08-turns-actions-and-resolution.md):

```csharp
// In LegalActions - can I use this at all from where I stand?
if (!skill.LaunchRanks.Includes(myRank))
    continue;

// In TargetsFor - can I reach them?
return candidates.Where(t => skill.TargetRanks.Includes(State.RankOf(t)));
```

Two `if`s. That is the entire positioning system, and because both the menu and
the AI read that one list, **the monsters obey the same reach rules as you** with
no extra work.

## Ranks close up

This is the rule that makes killing things change the *shape* of a fight, not
just the numbers.

```
   before   [1] Goblin   [2] Rat   [3] Archer     your sword reaches 1-2
                                                   the Archer is safe

   Goblin dies

   after    [1] Rat      [2] Archer               the Archer just walked
                                                   into your reach
```

Implemented by computing rank on demand rather than storing it
([chapter 5](05-state-and-entities.md) again):

```csharp
public int RankOf(Actor actor)
{
    if (!actor.IsAlive) return 0;

    int rank = 0;
    foreach (Actor a in Actors)
    {
        if (a.Team != actor.Team || !a.IsAlive) continue;
        rank++;
        if (ReferenceEquals(a, actor)) return rank;
    }
    return 0;
}
```

It counts the living, every time. So it **cannot** drift out of step with who is
actually standing.

And it cuts both ways, which is where the tension comes from: **lose your
front-liner and your Mage is shoved towards rank 1**, where most of her spells
cannot be cast at all.

## Never build a dead end

Once position gates your abilities, an actor can end up somewhere *nothing*
works. That is not tension — it is a player watching a useless character for six
rounds.

This project addresses it three ways, and all three are worth copying.

**1. Everyone can shuffle.** [`MoveAction`](../../src/Rpg.Core/Combat/MoveAction.cs)
swaps you with a neighbour, and costs your whole turn:

> Free repositioning would make the formation meaningless; you would simply slide
> into the perfect spot every turn. Costing a turn is what keeps the marching
> order a decision you make BEFORE the fight.

**2. Every hero has *something* usable from every rank**, enforced by a test:

```csharp
[Fact]
public void EveryHeroHasSomethingUsableFromEveryRankTheyCanOccupy()
```

This caught a real dead end during development: the Rogue had no usable skill in
rank 3. The fix was to widen Envenom's launch pattern to `####` — a data change,
caught by a test, before any player ever saw it.

**3. The UI explains itself.** A skill you cannot use is not hidden; it is greyed
out **with the reason**:

```
   Slash   (needs rank 1-2)
   Arrow   (nothing in rank 3-4)
   Heavy Blow   (2 turns)
```

> A skill that just vanishes from the menu teaches the player nothing; one that
> says "needs rank 1-2" teaches them the whole positioning system.

## What positioning did to the difficulty

Measured over 250 campaigns, before and after adding it:

| | Before | After |
|---|---|---|
| Campaign clear rate | 13% | **34%** |
| Warrens lethality | 0% | 0% |
| Ember Halls lethality | 55% | **6%** |

**Positioning made the game much easier.** That was surprising, and the
explanation is obvious in hindsight: most enemy attacks are melee reaching
`##--`, so a squishy hero parked in rank 3 is simply *out of reach*. Protecting
your casters with your body became a real, working tactic.

Two things had to be fixed once that was visible:

1. **Two encounters had no enemy that could reach rank 3 at all.** A hero at the
   back was untouchable, which makes the formation a *solved problem* rather than
   a decision. Both now field something with reach.
2. Tiers 1 and 2 had lost their teeth entirely, and needed retuning — which is
   the story in [chapter 16](16-testing-and-balancing.md).

> **The general lesson: adding a mechanic changes the balance of everything that
> already existed.** You cannot add positioning and keep your old numbers. Budget
> for the retune.

---

## What it costs you

**Statuses multiply your test surface.** Fourteen statuses interacting with 55
skills is a large space, and "what happens if you stun a stunned actor who is
also poisoned and about to die" is a real question with a real answer you should
know.

**Positioning is a UI problem more than a rules problem.** The rules took two
`if`s. Making it *legible* — rank badges, reach diagrams, greyed-out reasons,
preferred-rank hints on hero cards, re-sorting the row when somebody dies — took
several times more code than the mechanic.

**Position makes some content obsolete.** Any enemy that can only reach `##--`
became far weaker overnight. Every mechanic you add re-tunes everything you
already had.

---

## Try it

**1. Add a status.** In `Statuses.cs`:

```csharp
public static readonly StatusDefinition Regen =
    new("regen", "Regenerating", "Heals over time.", StatBlock.Zero, DamagePerTurn: -3);
```

Attach it to a skill and watch it do **nothing** — the `> 0` guard. Now fix
`TickStatuses` to handle negatives. That is the deliberate exercise, and it is a
good one.

**2. Make a sword reach the back line.** In
[`Skills.cs`](../../src/Rpg.Core/Content/Skills.cs), change Slash's
`TargetPattern` from `"##--"` to `"####"`. Run the balance harness and watch the
clear rate jump. Positioning is *load-bearing* difficulty, and you just removed
some.

**3. Build a dead end on purpose.** Change every Mage skill's `LaunchPattern` to
`"--##"`. Run `dotnet test` and watch
`EveryHeroHasSomethingUsableFromEveryRankTheyCanOccupy` fail with a message
telling you exactly which rank strands her.

---

**Next:** [Chapter 11 — Content as data](11-content-as-data.md)

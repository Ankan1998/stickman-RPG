# 11. Positioning

Darkest Dungeon-style rank combat. The single rule is that **where you stand
decides what you can do**, and everything else follows from it.

---

## The battle line

Both sides stand in one horizontal line, facing each other. Position 1 is the
**front** — closest to the enemy.

```
        your party                                   the enemy
   [3]      [2]      [1]          VS          [1]      [2]      [3]
  Mage    Cleric   Warrior                  Goblin    Rat     Archer
  back  ---------> front                    front  ---------> back
```

Your back rank is furthest from the fighting. So is theirs. That is why their
shaman stands at the back, and why your sword cannot reach it.

---

## Two rules per skill

Every skill declares two patterns, written front-first:

| | Means |
|---|---|
| **Launch** `##--` | You can only use it from ranks 1–2 |
| **Target** `--##` | It only reaches ranks 3–4 |

So:

| Skill | Launch | Reaches | In English |
|---|---|---|---|
| Slash | `##--` | `##--` | A sword. Front half only, both ends. |
| Arrow | `-###` | `####` | Useless jammed against the enemy; reaches anything. |
| **Aimed Shot** | `--##` | `--##` | A sniper shot. From the back, *at* the back. It cannot hit what is in front of you. |
| Healing Word | `####` | `####` | Position-blind. |
| **Soul Rip** (wraith) | `####` | `####` | Ignores the formation entirely. No marching order saves you. |

A skill you cannot launch from your current rank is not offered. A target out of
reach is not offered. Both are **greyed out with the reason** — `Slash (needs
rank 1-2)` — because a button that silently vanishes teaches nothing.

---

## Ranks close up

Dead fighters do not hold a position. Kill the enemy's front rank and everyone
behind steps forward:

```
before   [1] Goblin   [2] Rat   [3] Archer      your sword reaches 1-2
                                                 the Archer is safe

Goblin dies ↓

after    [1] Rat      [2] Archer                 the Archer just walked
                                                 into your reach
```

This works in both directions, and the second one hurts: **lose your front-liner
and your Mage is shoved towards rank 1**, where most of her spells cannot be
cast at all.

`BattleState.RankOf` computes position on demand from the living, so it can never
drift out of step with who is actually standing.

---

## Repositioning

Because ranks shift underneath you, anyone can end up somewhere none of their
skills work. So every fighter can **step forward** or **step back**, swapping
with the ally beside them.

It costs the whole turn. That is deliberate — free repositioning would make the
formation meaningless, because you would simply slide into the perfect spot every
turn. Costing a turn keeps the marching order a decision you make *before* the
fight, and turns a bad position into a problem you play through rather than lose
to.

The AI uses it too, and only when it has nothing better: `RepositionValue` sits
just above `PassValue`, so a monster shuffles only when it genuinely cannot reach
anybody.

---

## Choosing the line

At camp you pick three heroes **and the order they stand in**. Each card tells
you where that hero belongs, worked out from their own skills rather than
hand-written:

```
   RANK 1 - FRONT        RANK 2               RANK 3
     [Warrior]           [Cleric]              [Mage]
   all skills usable   all skills usable   2 of 3 skills usable
```

If you put the Mage at the front it will say **NO SKILLS USABLE** in red, before
you have wasted a dungeon finding out.

Rough shape of the roster:

| Rank | Who belongs there |
|---|---|
| **1–2** | Warrior, Templar, Paladin, Berserker, Rogue, Monk |
| **2–3** | Ranger, Cleric |
| **3** | Mage, Necromancer |

Every hero has at least one skill usable from **every** rank, which is enforced
by a test (`EveryHeroHasSomethingUsableFromEveryRankTheyCanOccupy`). Nobody can
ever be pure dead weight — they will just be doing their worst thing.

---

## What it changed about the game

Measured over 250 simulated campaigns, before and after:

| | Before positioning | After | After retuning |
|---|---|---|---|
| Campaign clear rate | 13% | 34% | **18%** |
| Lethality: Warrens | 0% | 0% | **11%** |
| Lethality: Ember Halls | 55% | 6% | **26%** |
| Lethality: Frozen Crypt | 68% | 64% | **72%** |

Positioning made the game **easier**, which is the correct and slightly
counter-intuitive result: most enemy attacks are melee reaching `##--`, so a
squishy hero parked in rank 3 is simply out of reach. Protecting your casters
with your body is now a real, working tactic.

Two things had to be fixed once that became apparent:

1. **Two encounters had no enemy that could reach rank 3 at all** — the Warrens'
   opener and the Ember Halls' kennels were all melee. A hero at the back was
   untouchable, which makes the formation a solved problem rather than a
   decision. Both now field something with reach.

2. **Tiers 1 and 2 lost all their teeth** and needed +2 Attack across the board
   to threaten a properly positioned party.

### Retuning the curve afterwards

Positioning left the first two dungeons unloseable, so both were retuned - and
the interesting part is what the *diagnosis* turned out to be.

The obvious reading of "the Warrens kills nobody" is that its monsters do not
hit hard enough. Measuring said otherwise:

```
lowest party health seen, avg : 73%
party health leaving Warrens  : 89%
avg rounds per encounter      : 3.4
```

Three and a half rounds. The monsters were not failing to hurt anyone - they
were **dying before they got to swing**. So the fix was tier-1 HEALTH, not
tier-1 damage: +40% health across tier 1 took encounters to roughly six rounds
and the dungeon from 0% to 11% lethal.

Two other things that came out of the same session, both worth remembering:

- **Attack is a wrecking ball, health is a scalpel.** +3 Attack on tier 1 took
  the Warrens from 2% to **69%** lethal. +1 landed it on 11%. Because defence
  subtracts, a point of Attack is worth several points of anything else - it is
  the wrong dial for fine tuning.
- **A fourth monster is not a small change.** Adding one body to the opening
  encounter moved it from 2% to 41%. Action economy dominates everything at
  this party size.

The resulting curve, and the bands now asserted in `CampaignHarnessTests`:

| Dungeon | Lethal | Asserted |
|---|---|---|
| The Warrens | 11% | 5-20%, and softer than the Ember Halls |
| The Ember Halls | 26% | softer than the Crypt |
| The Frozen Crypt | 72% | - |
| **Full campaign** | **18% cleared** | 12-55% |

---

## Where it lives

| File | Does |
|---|---|
| [`Combat/Ranks.cs`](../src/Rpg.Core/Combat/Ranks.cs) | The `####` mask, and the words for it |
| [`Combat/BattleState.cs`](../src/Rpg.Core/Combat/BattleState.cs) | `RankOf`, `FormationOf`, `SwapPositions` |
| [`Combat/Battle.cs`](../src/Rpg.Core/Combat/Battle.cs) | Filters `LegalActions` and `TargetsFor` by rank |
| [`Combat/MoveAction.cs`](../src/Rpg.Core/Combat/MoveAction.cs) | Stepping forward and back |
| [`Content/Skills.cs`](../src/Rpg.Core/Content/Skills.cs) | Every skill's launch and target patterns |
| [`Tests/FormationTests.cs`](../src/Rpg.Core.Tests/FormationTests.cs) | Twelve tests covering all of it |

Adding a positional skill is still just data:

```csharp
new SkillDefinition("pike_thrust", "Pike Thrust", "Long reach.",
    TargetKind.SingleEnemy, Power: 110,
    LaunchPattern: "-##-", TargetPattern: "###-");
```

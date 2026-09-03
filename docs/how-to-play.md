# How to play

Three dungeons. Ten heroes, of whom you take three. Nine fights in total, and
the computer only finishes about one run in five.

---

## Launch it

```bash
godot --path game
```

Or press **F5** in the Godot editor. (Details in [How to run](00-how-to-run.md).)

---

## The shape of a run

```
     CAMP  ──►  THE WARRENS  ──►  CAMP  ──►  EMBER HALLS  ──►  CAMP  ──►  FROZEN CRYPT
      │          3 encounters              3 encounters                  3 encounters
      │          POISON                    BURNING                       CHILL + CURSE
      │
      └── pick 3 of 10 heroes, hand out weapons, rest completely
```

**Two rules do all the work:**

1. **Wounds carry between encounters, and only camp clears them.** You get a
   small breather (28% of your health) after each fight, never a full heal. So a
   dungeon is three fights on one health bar, and how cheaply you win the first
   one decides whether you survive the third.

2. **Each dungeon hurts you differently.** The party that flattened the Warrens
   is not the right party for the Crypt. That is the entire point of camp.

---

## Positioning is the whole game

Both sides stand in a line facing each other. **Position 1 is the front.**

```
        your party                                 the enemy
   [3]      [2]      [1]        VS        [1]      [2]      [3]
  Mage    Cleric   Warrior                Goblin    Rat    Archer
```

Every skill says where it can be **used from** and what it can **reach**, shown
as a diagram like `##--` (front two) or `--##` (back two):

- A **sword** works from ranks 1-2 and reaches ranks 1-2. It cannot touch their
  back line, ever.
- A **bow** cannot be fired from rank 1, but reaches anything.
- **Aimed Shot** is a sniper shot: from the back two, at the back two only.
- The **wraith's Soul Rip** ignores position entirely. Nothing protects you.

**Ranks close up when somebody dies.** Kill their front-liner and the archer
behind steps into your sword's reach. Lose *your* front-liner and your Mage gets
shoved forward, where most of her spells stop working - at which point you can
spend a turn to **step back** into position.

Full details in [Positioning](11-positioning.md).

---

## The screen

```
  The Warrens - The Entrance (1/3)     Round 1 - Cleric          party 82%
  ┌──────────────────────────────────────────────────────────────────────┐
  │   [Mage]   [Cleric]  [Warrior]  |  [Goblin]  [Rat]   [Archer]        │
  │   rank 3    rank 2    > FRONT <    > FRONT <  rank 2   rank 3        │
  │   42/42     35/68      78/78          42/42   28/28    34/34         │
  ├───────────────────────────────────┬──────────────────────────────────┤
  │ Goblin Archer uses Rusty Bow      │  Cleric                          │
  │     11 damage to Cleric           │  standing in rank 2              │
  │ Giant Rat swaps with Archer       │  ┌────────────────────────────┐  │
  │     - now rank 2.                 │  │ Healing Word  (heal 26)    │  │
  │ Goblin uses Club on Cleric.       │  │ Mace  (95%)  ##--          │  │
  │     22 damage - critical!         │  │ Step forward (swap Warrior)│  │
  │                                   │  │ Wait                       │  │
  └───────────────────────────────────┴──────────────────────────────────┘
```

The `##--` after a skill is its **reach**. `standing in rank 2` tells you where
you are. A skill you cannot use says why: `Slash (needs rank 1-2)`.

- Fighters **lunge** when they attack, **flinch** when hit, and **fall over**
  when killed. Criticals flash gold and shake harder.
- **Status icons** sit above each head with the turns remaining.
- Skills on **cooldown** are greyed out with a countdown, so you can plan.
- The move menu asks **twice**: pick a skill, then pick a target. Target buttons
  show the exact damage — `Goblin A (13 dmg)`.

---

## Your roster

You take **three**. Nobody is complete, which is why the choice matters.

| | HP | Atk | Def | Spd | Crit | Kit |
|---|---|---|---|---|---|---|
| **Warrior** | 78 | 15 | 10 | 10 | 8% | Slash, Heavy Blow, Guard |
| **Templar** | 76 | 17 | 9 | 7 | 10% | Greatcleave (210%), Slash, Shield Wall |
| **Paladin** | 72 | 14 | 11 | 8 | 8% | Smite (heals him), Bless, Guard |
| **Berserker** | 68 | 18 | 4 | 12 | 15% | Cleave, Bloodthirst (drains 50%), Rage |
| **Cleric** | 68 | 10 | 8 | 11 | 5% | **Healing Word (26)**, Mace, Bless |
| **Monk** | 56 | 14 | 7 | 16 | 20% | Palm Strike, **Stunning Palm**, Meditate |
| **Ranger** | 50 | 14 | 5 | 16 | 24% | Arrow, Aimed Shot (200%), Poison Dart |
| **Necromancer** | 48 | 14 | 4 | 10 | 10% | Drain Life (60%), Curse, Wither |
| **Rogue** | 46 | 15 | 4 | 17 | **28%** | Backstab, Eviscerate (bleed), Envenom |
| **Mage** | 42 | 17 | 3 | 11 | 12% | Firebolt (burn), Frostbolt (chill), Arcane Blast |

Roughly: **three walls** (Warrior, Templar, Paladin), **three damage**
(Rogue, Ranger, Berserker), **two casters** (Mage, Necromancer), **two support**
(Cleric, Monk).

Three walls survive everything and kill nothing. Three casters delete the first
fight and die in the second.

---

## The three dungeons

### 1. The Warrens — **Poison**

Goblins, rats, a slime, a skeleton. Individually feeble; there are simply more of
them than there are of you, and the **Goblin Shaman heals its friends**.

- **Kill the Shaman first.** 32 HP, and it undoes your damage otherwise.
- Poison is 4 a turn. Slow — you can out-pace it if the fight is short.

### 2. The Ember Halls — **Burning**

Imps, cultists, orcs, a gargoyle. Far heavier hitters, and **burning does 9 a
turn for 2 turns**.

- No healing you have outruns burning. You either kill the caster (imp, cultist)
  or accept two ticks.
- The Orc Brute hits for 18 and the Gargoyle has 70 HP. Fights here are long, and
  long fights are what the breather cannot pay for.

### 3. The Frozen Crypt — **Chill and Curse**

Skeleton knights, wraiths, a minotaur, a lich, a demon lord. These do **not**
simply out-damage you — they take your stats:

| | Effect |
|---|---|
| **Chilled** | −6 Speed. You lose the turn order itself. |
| **Cursed** | −4 Attack, −3 Defense. |
| **Sundered** | −6 Defense. The wraith's speciality. |

An all-damage party stops working here, because your damage is the thing being
taken away. Buffs (Bless, Rage) and healing matter more than big hits.

---

## Loot

A weapon drops after **every** encounter. Equip it immediately from the drop
screen, or later at camp.

| Rarity | Warrens | Ember | Crypt |
|---|---|---|---|
| Common | 55% | 25% | 8% |
| Uncommon | 30% | 38% | 25% |
| Rare | 13% | 27% | 37% |
| Epic | 2% | 9% | 23% |
| Legendary | — | 1% | **7%** |

A weapon's stats come from its **rarity** (how much power it gets) and its
**archetype** (how it spends it), so of two Rare weapons a dagger is crit-heavy
and a hammer is raw damage — but they are worth the same.

The archetype also decides **what it sounds like when it lands.**

---

## Strategy

### 1. Focus one target down

The biggest mistake is spreading damage. An enemy at 5 HP hits exactly as hard as
one at full. Damage only pays when it *kills*, because that removes a turn from
every future round.

### 2. Kill the healers and the casters first

The Goblin Shaman heals 14. The Imp and the Cultist set you on fire. All three
are fragile — 32 to 46 HP — and all three are worth more dead than the 78 HP orc
standing next to them.

### 3. Protect your own healer — the enemy is hunting her

The monsters rate each of your heroes and go for whoever is most dangerous, which
is usually the Cleric. Expect her to be focused, and heal her *before* she is
nearly dead rather than after.

### 4. Win the first encounter cheaply

You keep your wounds. A dungeon is not three fights; it is one health bar spread
over three fights. Spending a big cooldown to end fight one two rounds sooner is
almost always worth it.

### 5. Change the party for the dungeon

- **Warrens** — anything works. A good place to bring damage.
- **Ember Halls** — you need bulk. Warrior or Templar earns their slot.
- **Frozen Crypt** — bring the Cleric and a buffer. Your damage is being cursed
  away; sustain and Bless matter more than a bigger axe.

### 6. Use the cooldowns the moment they are up

Heavy Blow (180%), Aimed Shot (200%) and Greatcleave (210%) are roughly double a
normal hit. There is almost never a reason to hold them.

---

## Winning

Clear all three dungeons and you get a **rank**, based on heroes lost across the
whole campaign:

| Rank | Heroes lost |
|---|---|
| **S** | 0 |
| **A** | 1–2 |
| **B** | 3–5 |
| **C** | 6+ |
| **-** | you did not finish |

For scale: the computer loses about **six** heroes per campaign and finishes only
18% of the time. An A is genuinely hard.

Every campaign prints its **seed**, and the same seed always plays out
identically — [deliberately so](04-architecture.md#determinism-the-same-seed-always-plays-out-the-same).

---

## Then make it yours

- **Too hard or too easy?** [Tune it](07-recipes.md#tuning-the-difficulty), and
  *measure* over hundreds of simulated campaigns rather than guessing.
- **New skill, monster, dungeon or hero?** All of them are data —
  [Recipes](07-recipes.md).
- **How does a click become pixels?** [Anatomy of a turn](06-anatomy-of-a-turn.md).
- **How was all this built?** [The campaign plan](10-campaign-implementation-plan.md).

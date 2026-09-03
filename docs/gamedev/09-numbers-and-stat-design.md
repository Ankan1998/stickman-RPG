# 9. Numbers: damage and stat design

> **Where you are:** chapter 9 of 17 · [index](README.md) · previous: [Turns, actions and resolution](08-turns-actions-and-resolution.md) · next: [Status effects and space](10-statuses-and-space.md)

---

## The problem

An RPG is a machine made of numbers. A hero has Attack 15. A goblin has Defense
4. A skill has Power 180. Somebody takes damage.

**How much?**

There is no correct answer. There are only *consequences* — and the formula you
pick shapes what your entire game feels like, what stats are worth taking, and
whether you can balance it at all.

Beginners usually write a formula in ten seconds, never revisit it, and spend the
next six months fighting the consequences.

---

## Damage formulas and what they do to a game

### Subtractive: `damage = attack - defense`

The one everyone writes first.

| | |
|---|---|
| **Feels like** | Armour absorbs a fixed chunk. Intuitive and easy to explain. |
| **Great because** | A player can do the arithmetic in their head. |
| **Breaks when** | Numbers get big. At Attack 100 vs Defense 5, armour is meaningless. At Attack 10 vs Defense 10, you literally cannot hurt them. |

The failure mode is severe: as numbers scale, defence swings from *irrelevant* to
*invincible* with a very narrow band of interesting in between.

### Multiplicative: `damage = attack * (100 / (100 + defense))`

| | |
|---|---|
| **Feels like** | Armour is percentage reduction. |
| **Great because** | Scales forever. Defense 50 always means ~33% less damage, at any level. |
| **Breaks when** | You want a player to understand it. "Why did 30 Defense give me 23% reduction?" |

*This is what most large RPGs use*, because it survives level 1 to level 100.

### Ratio: `damage = attack * attack / (attack + defense)`

Smooth, self-balancing, and completely opaque to the player. Common in
auto-battlers where nobody is doing mental arithmetic anyway.

### Fixed: `damage = the number on the card`

No formula at all. *Into the Breach*, *Slay the Spire*.

| | |
|---|---|
| **Feels like** | Perfect information. You know exactly what happens. |
| **Great because** | Trivially balanceable, and puts all the tension in decisions. |
| **Breaks when** | You wanted gear progression to feel exciting. |

---

## What this project uses, and why

From [`DamageCalculator`](../../src/Rpg.Core/Combat/DamageCalculator.cs) — the
**entire** damage system, in three lines:

```csharp
int raw       = attacker.Attack * power / 100;
int mitigated = raw - defender.Defense / 2;
if (isCritical) mitigated = mitigated * CriticalMultiplierPercent / 100;
return Math.Max(MinimumDamage, mitigated);
```

Worked example — Warrior (Attack 15) uses Heavy Blow (Power 180) on a Goblin
(Defense 4):

```
   raw       = 15 * 180 / 100  =  27
   mitigated = 27 - (4 / 2)    =  25        <- integer division: 4/2 = 2
   critical? = no
   result    = max(1, 25)      =  25
```

Four decisions are packed into that, and each is worth understanding.

### Decision 1: Power is a percentage

`Power: 180` does not mean "180 damage". It means "180% of your Attack".

```
   Slash        Power 100    a normal hit
   Heavy Blow   Power 180    1.8x
   Greatcleave  Power 210    2.1x
   Poison Dart  Power 40     a scratch, but it applies poison
```

**Why percentages matter:** they keep a skill relevant forever. If Slash dealt a
flat 15 damage, it would be worthless the moment you found a better sword.
Because it is 100% of Attack, it scales with you automatically. Every skill in
the game stays meaningful for the whole run, with no per-level tables.

### Decision 2: Defence subtracts, and is halved

`raw - defender.Defense / 2`.

The comment in the file is refreshingly honest about it:

> Subtractive armour is easy to reason about and easy to explain to a player, at
> the cost of scaling badly at very high numbers. If your endgame stats get into
> the thousands, revisit this. **That is a design decision to make deliberately,
> not a bug.**

The `/ 2` keeps defence from dominating in a game where Attack sits between 10
and 21 and Defense between 3 and 11. Without it, a Defense 10 tank would take
nothing at all from a tier-1 goblin.

### Decision 3: crits multiply *after* mitigation

```csharp
if (isCritical) mitigated = mitigated * 200 / 100;
```

Order matters here. Multiply before mitigation and armour blunts crits, which
makes crit-based characters (Rogue, 28% crit) useless against armoured enemies.
Multiplying after keeps crits exciting against everything.

### Decision 4: the minimum-damage floor

```csharp
public const int MinimumDamage = 1;
```

This is not politeness. It is a **hang fix**.

Without it, a high-defence actor becomes literally unkillable, both sides chip at
zero forever, and the fight runs to the 100-round limit. In a balance harness
running 2,250 fights, that is a hung test suite.

There is a test called `DamageNeverDropsBelowOne`, and it exists because this
happened.

### Bonus: integer division everywhere

```csharp
int raw = attacker.Attack * power / 100;      // 7/2 is 3, not 3.5
```

Deliberate. Whole numbers are easier for a player to reason about, and they
sidestep floating-point differences between platforms — which matters for
[determinism](07-randomness-and-determinism.md).

Watch the ordering though: `attack * power / 100` and `attack * (power / 100)`
are *not* the same in integer maths. The second one is almost always zero.
Multiply first, divide last.

---

## Stat design: what each number is for

A stat is only worth having if it changes a decision. Five stats:

| Stat | Does | Range here | Feels like |
|---|---|---|---|
| **MaxHealth** | How long you survive | 42–104 | Staying power |
| **Attack** | Scales all your damage | 10–21 | Threat |
| **Defense** | Subtracts from incoming | 3–11 | Durability |
| **Speed** | Turn order | 5–28 | Initiative |
| **CritChance** | % chance of double damage | 0–28 | Excitement |

Notice how **narrow** those ranges are. The strongest Attack in the game (21) is
about twice the weakest (10). That is deliberate, and it is the opposite of what
most beginners do.

### Why narrow ranges

Because the *sensitivity* of your formula decides your range, and subtractive
armour is brutally sensitive.

Here is a real measurement from tuning this project. Tier-1 monsters, changing
only Attack:

| Change | Warrens lethality |
|---|---|
| baseline | 2% |
| **+1 Attack** | **11%** |
| **+3 Attack** | **69%** |

Three points of Attack took a tutorial dungeon from trivial to a meat grinder.

The reason is the formula. Because defence *subtracts*, a point of Attack is
worth several points of anything else — it multiplies through Power and then
survives mitigation intact.

> **The lesson, worth writing on a wall: Attack is a wrecking ball, not a dial.**
> When you need to nudge difficulty, reach for Health, or for the number of
> enemies. Reach for Attack only when you want a big move.

### The counter-intuitive one: Health is the fine-tuning stat

Also from tuning this project. The Warrens killed **0%** of parties. The obvious
diagnosis is "the monsters do not hit hard enough". Measuring said otherwise:

```
   lowest party health seen, avg : 73%
   party health leaving Warrens  : 89%
   avg rounds per encounter      : 3.4
```

Three and a half rounds. The goblins were not failing to hurt anyone — **they
were dying before they got to swing.**

The fix was tier-1 *Health*, not damage. +40% health took encounters to roughly
six rounds and the dungeon from 0% to 11% lethal.

> **Health buys ROUNDS, and rounds are when everything else happens.** More
> rounds means more enemy attacks, more poison ticks, more chances for a crit.
> It is the gentlest and most controllable difficulty dial you have.

### And the one that is never small: action economy

One more measurement:

| Change | Warrens lethality |
|---|---|
| baseline | 2% |
| **+1 monster** in the opening encounter | **41%** |

Adding a *single body* was a bigger change than +1 Attack on every monster in the
tier.

**Action economy** — how many actions each side gets per round — dominates
everything in turn-based combat. Three heroes against four monsters is not "33%
harder". Every extra enemy turn is another attack, and it compounds every round.

This is why "kill one enemy fast" beats "damage them all a bit", and it is the
core tactical insight the AI is built around ([chapter 12](12-enemy-ai.md)).

---

## Budgets: how to balance 47 weapons without going mad

You have 47 weapons across 5 rarities. How do you make a Rare dagger and a Rare
hammer *different* but *equally good*?

The naive way is to hand-write 47 stat blocks, and then discover a Rare dagger is
better than an Epic mace and have no idea why.

**The professional way is a budget.** Two tables, from
[`WeaponDefinition`](../../src/Rpg.Core/Content/WeaponDefinition.cs):

**Table 1 — how much power a rarity gets to spend:**

```csharp
Rarity.Common    => 4,
Rarity.Uncommon  => 8,
Rarity.Rare      => 13,
Rarity.Epic      => 19,
Rarity.Legendary => 27,
```

**Table 2 — how each archetype spends it** (as percentages, so the same table
works at every rarity):

```csharp
//                      (atk, def, spd, crit,  hp)
WeaponKind.Dagger     => ( 40,   0,  20,   80,   0),   // fast and crit-heavy
WeaponKind.Greataxe   => (120,   0, -15,   30,   0),   // huge, and it slows you
WeaponKind.Shield     => ( 10, 100,  -5,    0,  70),   // pure defence
WeaponKind.Tome       => ( 60,  25,   0,   20,  60),   // caster support
```

Then:

```csharp
public StatBlock Bonus => SpendOf(Kind, BudgetFor(Rarity));
```

**Every weapon in the game derives from those two tables.** Nothing is
hand-written.

What this buys you:

- A Rare dagger and a Rare hammer are **different in character and identical in
  value**, guaranteed by construction.
- Rebalancing all 47 weapons is a two-table edit.
- Adding a weapon means picking a name, an archetype and a rarity. It cannot be
  accidentally overpowered.
- Notice the negative numbers — `Greataxe` has `-15` Speed. **Budgets let you buy
  power by selling a weakness**, which is where interesting items come from.

Notice also that crit and health cost less per point:

```csharp
MaxHealth:  budget * mix.hp   * 3 / 100,     // health is cheapest
CritChance: budget * mix.crit * 2 / 100,     // crit is a chance, not a guarantee
```

Because a point of "28% chance of double damage" is worth less than a point of
guaranteed Attack. Your budget system needs an exchange rate, and getting it
roughly right is most of the work.

> **This idea generalises to everything.** Heroes, monsters, skills, enemy
> encounters — anywhere you have many things that should be varied but
> comparable, give them a budget and a spend profile. It is how professional
> games ship 300 items without 300 balance bugs.

---

## The one formula rule

> **Every damage number in the entire game comes out of one method.**

From the file header:

> KEEP IT THAT WAY. The moment damage maths is scattered across ten different
> skill classes, you can no longer answer "why did that hit for 43?" without
> archaeology, and you can no longer rebalance the game at all.

This is also what lets the *UI* show an honest preview. The target button says
`Goblin A (13 dmg)` because it asks the same calculator the fight will use:

```csharp
string preview = $"{DamageCalculator.Compute(
    actor.CurrentStats, target.CurrentStats, skill.Power, false)} dmg";
```

If the screen had its own copy of the maths, the two would drift and your UI
would start lying. A game whose UI lies is worse than a game with no UI.

---

## What it costs you

**Subtractive armour has a ceiling.** This formula works beautifully for a game
whose stats stay under ~30. If you add levels and endgame gear pushing Attack to
400, you will have to rewrite it, and rebalance everything.

**Budgets constrain design.** A weapon that is *supposed* to be strange and
overpowered — the fun kind of legendary — does not fit the table. You end up
adding exceptions, and every exception weakens the guarantee.

**Narrow stat ranges feel less exciting.** Some players love seeing damage go
from 12 to 4,000. This game will never give them that. That is the cost of being
balanceable by one person.

**Fixed damage removes a kind of drama.** No damage ranges means no lucky rolls.
The tension has to come from elsewhere — here, from attrition and positioning.

---

## Try it

**1. Feel the sensitivity.** In
[`Monsters.cs`](../../src/Rpg.Core/Content/Monsters.cs), give every tier-1
monster +2 Attack. Run:

```bash
dotnet test --filter "FullyQualifiedName~CampaignHarness"
```

Watch a tutorial dungeon become a wall. Then revert, and add +20 *Health*
instead. Compare how much gentler that dial is.

**2. Switch to multiplicative armour.** Replace the mitigation line with:

```csharp
int mitigated = raw * 100 / (100 + defender.Defense);
```

Run the harness. The whole game shifts — tanks get better, and glass cannons get
worse. One line changed the character of every fight.

**3. Break the floor.** Delete `Math.Max(MinimumDamage, ...)` and return
`mitigated`. Run the tests and watch `DamageNeverDropsBelowOne` fail — then note
how many battles now run to the 100-round draw limit.

---

**Next:** [Chapter 10 — Status effects and space](10-statuses-and-space.md)

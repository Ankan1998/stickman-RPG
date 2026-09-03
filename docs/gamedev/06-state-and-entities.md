# 6. State and entities

> **Where you are:** chapter 6 of 20 · [index](README.md) · previous: [Rules vs presentation](05-rules-vs-presentation.md) · next: [Events and replay](07-events-and-replay.md)

---

## The problem

**Game state** is everything the game currently knows. Who is alive, how hurt
they are, what is poisoned, whose turn it is, which dungeon you are in, what
loot you are carrying.

It sounds trivial. It is where most game bugs live.

The reason is that game state is **highly mutable, deeply interconnected, and
touched from everywhere**. A goblin's health is changed by swords, poison,
healing, life-drain, level transitions and the debug menu. If all six of those
can write to `goblin.Health` directly, then the day you find a goblin sitting on
**-12 health** you have six suspects and no evidence.

The bugs this produces are the worst kind: they appear far from their cause, they
depend on ordering, and they do not reproduce.

---

## The idea: entities own their state, and defend it

An **entity** (this project calls it an `Actor`) is one thing in the world with
its own state and its own rules about how that state may change.

The defence has three layers:

1. **Nobody writes a field directly.** Changes go through methods.
2. **The methods enforce the invariants.** Health cannot go negative, ever.
3. **The compiler enforces who may call what.** Not documentation. The compiler.

---

## In this project: `Actor`

[`Actor`](../../src/Rpg.Core/Entities/Actor.cs) is one combatant. A hero and a
goblin are **the same class** — the difference is entirely data.

### Layer 1: the field is not yours to write

```csharp
public int Health { get; private set; }
```

`private set` means: anyone may read this, only code inside `Actor` may write it.

So there is no way — anywhere in the entire codebase, including the Godot layer,
including a future contributor in a hurry — to do `actor.Health -= 10`. It does
not compile.

### Layer 2: the method enforces the rule

```csharp
public int TakeDamage(int amount)
{
    int applied = Math.Clamp(amount, 0, Health);
    Health -= applied;
    return applied;
}
```

Two things worth noticing, because both are more thoughtful than they look.

**The clamp makes negative health impossible.** Not unlikely. Impossible. There
is exactly one line in the program that can decrease health, and it cannot
produce a negative result. That entire bug category is now closed.

**It returns what was *actually* lost.** Hit an 8 HP goblin for 40 and this
returns `8`, not `40`. So the combat log says "8 damage" instead of "40 damage"
against a target that only had 8. Small detail; it is the difference between a
log a player trusts and one they do not.

That return value earns its keep elsewhere, too — life-drain heals you for
damage *actually* dealt, so draining a nearly-dead target heals very little.
Overkill is wasted consistently, and nobody had to write that rule twice.

Its sibling:

```csharp
public int Heal(int amount)
{
    if (!IsAlive) return 0;                              // no accidental resurrection
    int applied = Math.Clamp(amount, 0, MaxHealth - Health);   // no overhealing
    Health += applied;
    return applied;
}
```

That `if (!IsAlive) return 0;` is a **design decision expressed as code**.
Resurrection should be an explicit, deliberate mechanic — not something a stray
area-heal can do by accident. So `Heal` refuses, and a separate, deliberately
named method exists for the one place that is allowed to:

```csharp
internal void ReviveWith(int health)
```

### Layer 3: the compiler decides who may call it

That `internal` is doing real architectural work.

`internal` means "only code inside this project may call this". `ReviveWith`,
`RemoveStatus`, `PutOnCooldown`, `TickCooldowns` and `ResetForNextBattle` are all
internal to `Rpg.Core`.

**So the Godot layer physically cannot call them.** `ActorView` holds an `Actor`
and displays it, and could not modify one if it tried — the methods are invisible
from that project.

Look at what that combination gives you. From the presentation layer:

- `Health` — readable, not writable *(private set)*
- `Statuses` — enumerable, not addable *(exposed as `IReadOnlyList`)*
- `ReviveWith` — invisible *(internal)*

The rule "presentation only reads" from [chapter 5](05-rules-vs-presentation.md)
is not a convention anybody has to remember. It is the type system.

---

## Stored state vs derived state

Here is a question that comes up constantly, and getting it wrong is a rich
source of bugs.

A hero has 15 Attack. They pick up a sword worth +3. They are Blessed, worth +2.
What is their Attack?

**Option A — store it.** Keep an `Attack` field, and update it whenever anything
changes:

```csharp
actor.Attack = actor.BaseAttack + weapon.Attack + statuses.Sum(s => s.Attack);
```

Now you must remember to call that when: equipping, unequipping, a status
landing, a status expiring, a status being refreshed, a weapon being swapped
mid-fight, loading a save. Miss one and a hero silently keeps a buff forever.
This is the classic "stat drift" bug, and every RPG developer has shipped it at
least once.

**Option B — derive it.** Never store it. Compute it on every read:

```csharp
public StatBlock CurrentStats
{
    get
    {
        StatBlock total = BaseStats;
        if (Weapon is not null) total += Weapon.Bonus;
        foreach (StatusEffect status in _statuses) total += status.Definition.Modifier;
        return total.Clamped();
    }
}
```

**This project uses B**, and the reason is not performance — it is that **B
cannot drift.** There is no cached value to become stale, because there is no
cached value. Add a status and every read is correct immediately, with no code
anywhere remembering to recalculate anything.

The same idea appears again in
[`BattleState.RankOf`](../../src/Rpg.Core/Combat/BattleState.cs), which computes
a fighter's position in the line by counting the living, every time it is asked,
rather than storing a rank that would have to be updated on every death.

> **The rule of thumb:** derive by default; cache only when profiling proves you
> must. A stale cache is a far more expensive bug than a few extra additions.

And a consequence worth internalising:

```csharp
// Combat always reads CurrentStats, never BaseStats.
// Reaching for BaseStats outside character creation is usually a bug -
// it means some buff or debuff is being silently ignored.
```

### `StatBlock`: state as a value

The stats themselves are one immutable value:

```csharp
public readonly record struct StatBlock(
    int MaxHealth, int Attack, int Defense, int Speed, int CritChance);
```

`readonly record struct` means it is copied, not shared, and cannot be modified
after creation. So passing a `StatBlock` around can never let somebody
accidentally mutate a hero's base stats through a reference they were handed.

It also defines `+`:

```csharp
public static StatBlock operator +(StatBlock a, StatBlock b) => new(
    a.MaxHealth + b.MaxHealth,
    a.Attack    + b.Attack,
    /* ... */);
```

Which is why `CurrentStats` above reads as plainly as it does. Buffs, debuffs and
weapons are all just addition, and adding a sixth stat means adding one line
here and letting the compiler walk you through the rest.

---

## Definitions vs instances

This pattern shows up everywhere in games and it is worth learning by name,
because once you see it you will see it constantly.

Ten goblins are poisoned. How many "poison" objects exist?

**Eleven.** One describing *what poison is*, and ten describing *this goblin's
poison, two turns left*.

| | [`StatusDefinition`](../../src/Rpg.Core/Effects/StatusDefinition.cs) | [`StatusEffect`](../../src/Rpg.Core/Effects/StatusEffect.cs) |
|---|---|---|
| Means | "what poison is" | "this goblin is poisoned, 2 turns left" |
| How many | one, in the whole game | one per affected actor |
| Mutable? | never | yes — the countdown changes |
| Holds | damage per turn, stat modifier, name, icon | remaining turns |

```csharp
public sealed class StatusEffect
{
    public StatusDefinition Definition { get; }   // shared template
    public int RemainingTurns { get; private set; }
    public void Tick() => RemainingTurns--;
}
```

The same split appears three more times in this project:

| Template (shared, immutable) | Instance (per-thing, mutable) |
|---|---|
| `StatusDefinition` | `StatusEffect` |
| `SkillDefinition` | cooldown entry on an `Actor` |
| `MonsterTemplate` | `Actor` |
| `HeroDefinition` | `Actor` |
| `WeaponDefinition` | the `Weapon` an `Actor` holds |

You may know it as the **flyweight** pattern. Its practical value in games is
enormous: 500 goblins on screen share **one** `MonsterTemplate`, so the numbers
describing a goblin exist once in memory, not 500 times. And balancing goblins
means editing one row.

### The mistake this avoids

The instinct of an experienced OO developer is:

```csharp
class Goblin : Actor { }
class Orc : Actor { }
class GoblinShaman : Goblin { }
```

**Do not do this.** [Chapter 12](12-content-as-data.md) makes the full argument,
but the short version is that a 40-class inheritance tree cannot be balanced,
cannot be saved to disk, cannot be edited by a designer, and cannot be
hot-reloaded. This project has **22 monsters and one `Actor` class.** The
difference between a goblin and a demon lord is a row of numbers.

---

## Where the state actually lives

State should have **one owner**, and you should be able to say what it is.

```
   Campaign                                 owns the run
   |    dungeon index, encounter index, loot, party, RunStats, the RNG
   |
   +-- Battle                               owns one fight
   |     |  round number, who won, which deaths have been reported
   |     |
   |     +-- BattleState                    owns who is in it
   |     |     the actor list (which IS the marching order), the RNG
   |     |
   |     +-- TurnQueue                      owns the running order
   |           this round's order, whose turn it is
   |
   +-- Actor (x N)                          owns itself
         health, statuses, cooldowns, weapon
```

Read that top to bottom and it tells you where to look for anything. "Whose turn
is it?" is not a question you have to search for; `TurnQueue` owns it.

The one thing worth calling out: **the actor list is the formation.** Position in
that list *is* position in the battle line, which is why it is a `List` and not a
`HashSet`, and why the only thing allowed to reorder it is:

```csharp
internal void SwapPositions(Actor a, Actor b)
```

`internal`, because shuffling the battle line is a *rule* — it belongs to
[`MoveAction`](../../src/Rpg.Core/Combat/MoveAction.cs), which charges you a full
turn for it. The Godot layer cannot rearrange the battlefield.

---

## Fail loudly, at the earliest possible moment

One more habit worth stealing. From
[`BattleState`](../../src/Rpg.Core/Combat/BattleState.cs)'s constructor:

```csharp
var duplicate = Actors.GroupBy(a => a.Id).FirstOrDefault(g => g.Count() > 1);
if (duplicate is not null)
    throw new ArgumentException(
        $"Two actors share the id '{duplicate.Key}'. Ids must be unique - " +
        "events reference actors by id.");
```

Every event in this game refers to actors by id. Two actors sharing one would
silently corrupt the log, the UI, saves and replays — and it would show up as
"the wrong goblin played a death animation", three days later, in a completely
different file.

So it explodes **at construction**, with a message that tells you exactly what
you did.

> **The general lesson: turn a whole class of subtle bugs into one immediate,
> loud, obvious crash.** A crash on line one of the fight is a five-minute fix. A
> corrupted log noticed on Thursday is a five-hour one.

You will find the same instinct in `Battle.TakeTurn`:

```csharp
if (!ReferenceEquals(action.Actor, Current))
    throw new InvalidOperationException(
        $"It is {Current?.Name}'s turn, but the action belongs to {action.Actor.Name}.");
```

---

## What it costs you

**Verbosity.** `TakeDamage(10)` is more typing than `Health -= 10`. Every guard
is a line that does not obviously "do" anything. On a small project this feels
like ceremony.

**Some genuine inconvenience.** `internal` means you occasionally cannot do a
thing from the Godot layer that would have been convenient. That is the *point* —
but it will still annoy you at 1am, and you will be tempted to make it public.
Do not.

**Derived state costs a little CPU.** `CurrentStats` allocates and adds on every
read, and it is read a lot. For six actors, this is free. For 10,000 entities in
a real-time game, it would not be, and you would cache it — with an explicit
invalidation strategy, and the bugs that come with one.

---

## Try it

**1. Try to break an invariant.** In any test, write:

```csharp
Actor goblin = MakeActor("g", Team.Monsters, Stats(hp: 8));
goblin.TakeDamage(1000);
Assert.Equal(0, goblin.Health);      // not -992
```

Then try `goblin.Health = -5;` and read the compiler error.

**2. See the flyweight.** Add a `Console.WriteLine` in the `StatusDefinition`
constructor and run any battle. It fires **once per status type in the game**,
not once per poisoned goblin. Fourteen objects describe every status effect in
every fight you will ever play.

**3. Feel stat drift.** Temporarily cache `CurrentStats` in a field set only in
the constructor. Run the tests. `SpeedBuffsActuallyChangeTheOrder` in
[`TurnOrderTests`](../../src/Rpg.Core.Tests/TurnOrderTests.cs) fails, because a
Haste buff no longer moves anyone up the order. That is the bug this chapter is
about, caught by a test written for exactly that reason.

---

**Next:** [Chapter 7 — Events and replay](07-events-and-replay.md)

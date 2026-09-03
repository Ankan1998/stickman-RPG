# 2. C# crash course

You already know how to program. This page is **only** the C# that actually
appears in this repository, each item shown with the real line it comes from, so
you can read every file here without googling syntax.

If you know Java, C++, or TypeScript, most of this will feel familiar. Skim for
the unfamiliar bits.

---

## Contents

- [File layout: namespaces and usings](#file-layout-namespaces-and-usings)
- [Classes and their modifiers](#classes-and-their-modifiers)
- [Properties (the `{ get; set; }` thing)](#properties)
- [Expression bodies (`=>`)](#expression-bodies-)
- [Records — the biggest idea here](#records--the-biggest-idea-here)
- [`var`](#var)
- [Nullable reference types (`?`, `!`, `??`)](#nullable-reference-types)
- [Pattern matching and `switch` expressions](#pattern-matching-and-switch-expressions)
- [LINQ](#linq)
- [Collections](#collections)
- [Interfaces](#interfaces)
- [Enums](#enums)
- [Named and optional arguments](#named-and-optional-arguments)
- [String interpolation](#string-interpolation)
- [`const` and `readonly`](#const-and-readonly)
- [Extension methods](#extension-methods)
- [Access modifiers](#access-modifiers)
- [`params`](#params)
- [Operator overloading](#operator-overloading)
- [`async` / `await`](#async--await)
- [Things you will see and can ignore](#things-you-will-see-and-can-ignore)

---

## File layout: namespaces and usings

Every C# file in this repo starts the same way:

```csharp
using Rpg.Core.Content;
using Rpg.Core.Effects;

namespace Rpg.Core.Entities;
```

- **`namespace X;`** — "everything in this file belongs to group `X`". The
  semicolon version (called a *file-scoped namespace*) applies to the whole file
  and saves a level of indentation. The older style wrapped everything in `{ }`.
- **`using X;`** — "let me refer to things in group `X` by their short name".
  Exactly like Python's `from x import *` or Java's `import x.*`.

Namespaces here follow the folder structure: `src/Rpg.Core/Entities/Actor.cs`
declares `namespace Rpg.Core.Entities`. That is a convention, not a rule the
compiler enforces, but everyone follows it.

### Implicit usings

You will notice files use `List<T>` and `Math.Max` without importing
`System.Collections.Generic` or `System`. That is because of this line in
[`Rpg.Core.csproj`](../src/Rpg.Core/Rpg.Core.csproj):

```xml
<ImplicitUsings>enable</ImplicitUsings>
```

It silently adds the handful of near-universal imports (`System`,
`System.Collections.Generic`, `System.Linq`, and a few more) to every file.

> **A real bug this caused:** the first build of this project failed with
> `'IRandomSource' does not contain a definition for 'Chance'`. The `Chance`
> method lives in `Rpg.Core.Rng`, which is *not* one of the implicit usings, and
> [`SkillAction.cs`](../src/Rpg.Core/Combat/SkillAction.cs) was missing
> `using Rpg.Core.Rng;`. If you ever get "does not contain a definition for X"
> on something you are sure exists, a missing `using` is the first suspect.

---

## Classes and their modifiers

```csharp
public sealed class Actor          // Rpg.Core/Entities/Actor.cs
public static class DamageCalculator   // Rpg.Core/Combat/DamageCalculator.cs
public partial class GameRoot : Control      // game/scripts/GameRoot.cs
```

| Modifier | Meaning |
|---|---|
| `sealed` | Nobody can inherit from this class. Used liberally here — it documents "this is not a base class" and lets the compiler optimise. |
| `static` | No instances exist. Just a bag of functions. `DamageCalculator.Compute(...)` is called on the *type*, never on an object. |
| `partial` | This class is split across multiple files. **You only need this in Godot** — see the [Godot crash course](03-godot-crash-course.md#why-partial). |
| `: Control` | Inheritance. `GameRoot` *is a* `Control` (a Godot UI node). |

---

## Properties

C# properties look like fields but are really a pair of methods. This is
probably the biggest visual difference from Java.

```csharp
public string Id { get; }                  // read-only from outside AND inside
public int Health { get; private set; }    // anyone can read; only this class can write
public int Round { get; private set; }
```

- `{ get; }` — set once in the constructor, then permanently read-only.
- `{ get; private set; }` — the outside world can read `actor.Health`, but only
  code inside `Actor` can assign to it. This is deliberate: it means damage
  *must* go through `TakeDamage()`, which clamps it correctly. Nobody can write
  `actor.Health = -50` from outside.

You use them like fields:

```csharp
int hp = actor.Health;     // calls the getter
```

### Computed properties

A property can compute its value every time it is read:

```csharp
public bool IsAlive => Health > 0;
public int MaxHealth => CurrentStats.MaxHealth;
```

`IsAlive` is not stored anywhere. Each time you read `actor.IsAlive` it runs
`Health > 0`. Think of it as a zero-argument method with nicer syntax.

A bigger one, from [`Actor.cs`](../src/Rpg.Core/Entities/Actor.cs):

```csharp
public StatBlock CurrentStats
{
    get
    {
        StatBlock total = BaseStats;
        foreach (StatusEffect status in _statuses)
            total += status.Definition.Modifier;
        return total.Clamped();
    }
}
```

Every single time anything reads `actor.CurrentStats`, this loop runs and adds up
every active buff and debuff. That is why buffs "just work" everywhere without
any code remembering to recalculate.

---

## Expression bodies (`=>`)

When a method or property is a single expression, `=>` replaces `{ return ...; }`:

```csharp
// These two are identical:
public bool HasStatus(string statusId) => _statuses.Any(s => s.Id == statusId);

public bool HasStatus(string statusId)
{
    return _statuses.Any(s => s.Id == statusId);
}
```

Note `=>` is doing **two different jobs** in that line:

1. `HasStatus(...) => ...` — expression-bodied *method*.
2. `s => s.Id == statusId` — a *lambda* (anonymous function), exactly like
   JavaScript's `s => s.id === statusId` or Python's `lambda s: s.id == status_id`.

Same symbol, different meanings, decided by context. It is confusing for about a
day and then invisible.

---

## Records — the biggest idea here

A `record` is a class (or struct) where the compiler writes the boring parts for
you: constructor, properties, equality, and `ToString()`.

### Positional records

From [`SkillDefinition.cs`](../src/Rpg.Core/Content/SkillDefinition.cs):

```csharp
public sealed record SkillDefinition(
    string Id,
    string Name,
    string Description,
    TargetKind Target,
    int Power = 0,
    int Healing = 0,
    StatusDefinition? AppliesStatus = null,
    int StatusTurns = 0,
    int Cooldown = 0);
```

That single declaration gives you:

- a constructor taking all nine values
- nine read-only properties (`Id`, `Name`, `Power`, ...)
- **value equality** — two `SkillDefinition`s with identical contents are `==`
- a readable `ToString()` — `SkillDefinition { Id = slash, Name = Slash, ... }`

Writing that by hand is roughly 60 lines.

### Why value equality matters here

This is not decoration. [`BalanceHarnessTests`](../src/Rpg.Core.Tests/BalanceHarnessTests.cs)
contains:

```csharp
List<GameEvent> first = BattleRunner.Run(4242).Log;
List<GameEvent> second = BattleRunner.Run(4242).Log;
Assert.Equal(first, second);
```

That compares two lists of a few hundred events, element by element, by
*content*. Because `GameEvent` and its subtypes are records, it Just Works. With
normal classes, `Assert.Equal` would compare memory addresses and always fail.

### Record inheritance

[`GameEvent.cs`](../src/Rpg.Core/Combat/GameEvent.cs) uses a record hierarchy:

```csharp
public abstract record GameEvent;

public sealed record Damaged(
    string ActorId, int Amount, bool IsCritical,
    string? SourceId = null, string? StatusId = null) : GameEvent;
public sealed record Healed(string ActorId, int Amount) : GameEvent;
public sealed record Died(string ActorId) : GameEvent;
```

`abstract record GameEvent;` — note the semicolon, no body. It exists purely as
"the thing all events have in common", so you can write
`List<GameEvent>` holding a mix of all of them.

`Damaged` also shows **optional positional parameters**: `string? SourceId = null`
means callers may leave it out. Poison does, because poison has no attacker.

### `readonly record struct`

[`StatBlock.cs`](../src/Rpg.Core/Entities/StatBlock.cs):

```csharp
public readonly record struct StatBlock(
    int MaxHealth, int Attack, int Defense, int Speed, int CritChance)
```

`struct` (rather than `class`) means it is a **value type**: assigning it copies
it, like an `int`. `readonly` means it can never be modified after creation.

Together that means a `StatBlock` can never be secretly changed by code holding a
reference to it — which matters because `BaseStats` is shared and must never be
mutated by a temporary buff calculation.

---

## `var`

```csharp
var effect = new StatusEffect(definition, turns);
```

"Work out the type from the right-hand side." Identical to writing
`StatusEffect effect = ...`. It is **not** dynamic typing — the type is fixed at
compile time; you just did not type it twice.

This codebase writes types explicitly when the type is not obvious from the line
(`StatBlock total = BaseStats;`) and uses `var` when it is (`var effect = new
StatusEffect(...)`). Both are valid style.

---

## Nullable reference types

C# tracks whether a reference can be `null`, and warns you if you might have
missed a case. Turned on by `<Nullable>enable</Nullable>` in the `.csproj`.

| Syntax | Meaning |
|---|---|
| `string name` | Never null. The compiler warns if you might assign null. |
| `string? name` | Might be null. The compiler warns if you use it without checking. |
| `x?.Y` | If `x` is null, the whole expression is null; otherwise `x.Y`. |
| `a ?? b` | Use `a`, unless it is null, in which case `b`. |
| `x!` | "Trust me, this is not null." Silences the warning. |

Real examples:

```csharp
// Actor.cs - the "?" says this can legitimately be null
public string? BlockedReason =>
    _statuses.FirstOrDefault(s => s.Definition.PreventsAction)?.Definition.Name;
```

Read that right to left: find the first status that prevents acting.
`FirstOrDefault` returns `null` if there is none. `?.` then short-circuits — if
no such status exists, the whole thing is `null` instead of crashing.

```csharp
// Battle.cs - Winner is null both during the fight AND on a draw
public Team? Winner { get; private set; }
```

```csharp
// Battle.cs - we just checked MoveNext() returned true, so Current cannot be null
Actor next = _queue.Current!;
```

The `!` there is a promise to the compiler. It is safe *because of the line
immediately above it*. Use `!` sparingly and only when you can point at the
reason.

### `= null!` on fields

In [`GameRoot.cs`](../game/scripts/GameRoot.cs):

```csharp
private Battle _battle = null!;
```

This means: "it is null right now, but I promise it will be set before anything
reads it, so stop warning me." It is set in `_Ready()`, which Godot calls before
anything else. A slightly ugly but standard workaround for engine-managed
lifecycles.

---

## Pattern matching and `switch` expressions

C#'s most powerful modern feature, used heavily here.

### `is` with a type

```csharp
if (action is not SkillAction skill)
    return PassValue;
// from here on, `skill` exists and is a SkillAction
```

Test the type and bind a variable in one step. `is not` is the negation.

### Property patterns

```csharp
Assert.Contains(log, e => e is TurnSkipped { Reason: "Stunned" });
```

Reads as: "is this a `TurnSkipped` whose `Reason` property equals `"Stunned"`?"
No casting, no null checks.

### `is { } x`

```csharp
if (Skill.AppliesStatus is { } status && Target.IsAlive)
```

`{ }` is an empty property pattern, which matches **any non-null value**. So this
reads: "if `AppliesStatus` is not null, call it `status`, and also the target is
alive". It is the compact way to write "unwrap this optional".

### `switch` expressions

Not the old `switch` *statement* — this is an expression that produces a value:

```csharp
public IEnumerable<Actor> TargetsFor(Actor actor, SkillDefinition skill) => skill.Target switch
{
    TargetKind.SingleEnemy => State.LivingMembersOf(actor.Team.Opposite()),
    TargetKind.SingleAlly  => State.LivingMembersOf(actor.Team),
    TargetKind.Self        => new[] { actor },
    _                      => throw new NotSupportedException($"Unhandled: {skill.Target}"),
};
```

- `_` is the default case.
- Each arm is `pattern => value`.
- No `break`, no fallthrough.

The most impressive one in the repo is
[`BattleView.Describe`](../game/scripts/BattleView.cs), which turns any event
into a line of text by matching on its type:

```csharp
private string? Describe(GameEvent gameEvent) => gameEvent switch
{
    BattleStarted     => "[b]A fight breaks out.[/b]",
    RoundStarted r    => $"\n-- round {r.Round} --",
    Damaged d         => $"    {d.Amount} damage to {ActorName(d.ActorId)}",
    Died d            => $"[b]{ActorName(d.ActorId)} goes down.[/b]",
    _                 => null,
};
```

Note `RoundStarted r` — matches the type *and* names it `r` so you can read
`r.Round`. `BattleStarted` with no variable — matches the type, ignores the value.

---

## LINQ

LINQ is C#'s standard library for querying collections. Same ideas as JavaScript
array methods or Python comprehensions, different names.

| LINQ | JavaScript | Does |
|---|---|---|
| `.Where(x => ...)` | `.filter()` | Keep matching items |
| `.Select(x => ...)` | `.map()` | Transform each item |
| `.Any(x => ...)` | `.some()` | Is there at least one match? |
| `.FirstOrDefault(x => ...)` | `.find()` | First match, or `null` |
| `.OrderByDescending(x => ...)` | `.sort()` | Sort, biggest first |
| `.ThenBy(x => ...)` | — | Tie-breaker for the sort above |
| `.ToList()` / `.ToArray()` | `[...]` | Force it into a real collection |
| `.ToDictionary(x => x.Id)` | — | Build a lookup keyed by `x.Id` |

Real example from [`TurnQueue.cs`](../src/Rpg.Core/Combat/TurnQueue.cs):

```csharp
_order.AddRange(actors
    .Where(a => a.IsAlive)
    .OrderByDescending(a => a.CurrentStats.Speed)
    .ThenBy(a => a.Id, StringComparer.Ordinal));
```

"Take the actors, keep the living ones, sort by Speed highest-first, and break
ties alphabetically by id."

### The one gotcha: lazy evaluation

LINQ queries are lazy — they do not run until something consumes them. This is
usually fine, but it can bite. It is exactly why
[`IAction.Execute`](../src/Rpg.Core/Combat/IAction.cs) takes the log as a
parameter instead of returning a sequence:

```csharp
void Execute(BattleState state, List<GameEvent> log);   // what we do
IEnumerable<GameEvent> Execute(BattleState state);      // what we deliberately avoid
```

With the second version, an implementation using `yield return` would do
**nothing at all** unless the caller looped over the result. Silent no-op bugs in
combat code are miserable to find. `.ToList()` also appears in a few places for
this reason:

```csharp
foreach (StatusEffect status in actor.Statuses.ToList())
```

Here `.ToList()` takes a snapshot, because the loop body removes expired statuses
from the collection it is iterating — which would otherwise throw.

---

## Collections

| Type | What it is |
|---|---|
| `List<T>` | Growable array. `List<GameEvent>` |
| `Dictionary<K, V>` | Hash map. `Dictionary<string, int> _cooldowns` |
| `HashSet<T>` | Set of unique values. `HashSet<string> _deathsReported` |
| `T[]` | Fixed-size array. `new[] { warrior, medic }` |
| `IEnumerable<T>` | "Something you can foreach over." The most general. |
| `IReadOnlyList<T>` | "A list you can index and count, but not modify." |

That last one is a habit worth copying:

```csharp
private readonly List<StatusEffect> _statuses = new();
public IReadOnlyList<StatusEffect> Statuses => _statuses;
```

Internally it is a mutable `List`. Externally it is read-only. Outside code can
loop over `actor.Statuses` and count them, but cannot call `.Add()` — statuses
must go through `ApplyStatus()`, which handles the refresh-don't-stack rule.

Note `= new()` with nothing after it — a *target-typed new*. The type is already
written on the left, so you do not repeat it.

---

## Interfaces

```csharp
public interface IAction
{
    Actor Actor { get; }
    string Label { get; }
    void Execute(BattleState state, List<GameEvent> log);
}
```

A contract. `SkillAction` and `PassAction` both implement it, so anywhere the
code says `IAction` it can hold either. Same as Java interfaces or TypeScript
`interface`.

The `I` prefix is a near-universal C# convention: `IAction`, `IRandomSource`,
`IEnumerable`.

Implementing one needs no keyword — just list it after a colon:

```csharp
public sealed class PassAction : IAction
```

**Why this repo uses them:** `IRandomSource` is an interface so tests can swap in
`FixedRandom` (which returns numbers you choose) instead of real randomness. That
is the entire reason a test can assert "critical hits double damage" reliably
instead of rolling dice and hoping.

---

## Enums

```csharp
public enum Team { Heroes, Monsters }

public enum TargetKind { SingleEnemy, SingleAlly, Self }
```

A fixed set of named values. Used as `Team.Heroes`, `TargetKind.Self`.

`Team?` (with the question mark) means "a Team, or nothing" — used for
`Winner`, where "nobody won yet" and "it was a draw" both need to be
representable.

---

## Named and optional arguments

**Optional parameters** have defaults and can be skipped:

```csharp
public static StatBlock Stats(int hp = 100, int atk = 10, int def = 0, int spd = 10, int crit = 0)
```

**Named arguments** let you pass them out of order and, more importantly,
readably:

```csharp
Stats(hp: 100, atk: 10, spd: 20)
```

Compare those two lines:

```csharp
new StatBlock(70, 15, 9, 10, 10)
new StatBlock(MaxHealth: 70, Attack: 15, Defense: 9, Speed: 10, CritChance: 10)
```

The second is the one you can still read in six months. Game code is full of
numeric parameters, so this codebase uses named arguments heavily for content.

---

## String interpolation

```csharp
$"{Name} ({Health}/{MaxHealth} HP)"
```

The `$` prefix enables `{expression}` substitution — like a JavaScript template
literal, but with `{}` instead of `${}`. Anything can go inside:

```csharp
$"Hero win rate: {winRate:P1}"          // "74.9%"  — :P1 means percent, 1 decimal
$"Average: {total / (double)n:F1}"      // "6.7"    — :F1 means fixed, 1 decimal
```

---

## `const` and `readonly`

```csharp
public const int MaxRounds = 100;                       // baked in at compile time
public const double StunValue = 16.0;
private static readonly Color HeroColor = new("6fb3d2"); // set once, at startup
private readonly List<StatusEffect> _statuses = new();   // set once, per object
```

- `const` — compile-time constant. Only works for simple values (numbers,
  strings, bools).
- `readonly` — assigned once, in the constructor or at declaration, then frozen.
  Works for any type.

Note `readonly` on a `List` field freezes **the reference, not the contents** —
you can still `.Add()` to it. It only stops someone reassigning
`_statuses = someOtherList`.

---

## Extension methods

C# lets you add methods to types you do not own. From
[`IRandomSource.cs`](../src/Rpg.Core/Rng/IRandomSource.cs):

```csharp
public static class RandomSourceExtensions
{
    public static bool Chance(this IRandomSource rng, int percent) =>
        percent > 0 && rng.NextInt(0, 100) < percent;
}
```

The magic is `this` on the first parameter. It means you can call it as if it
were a method on `IRandomSource`:

```csharp
bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);
```

even though `IRandomSource` only actually declares `NextInt` and `NextDouble`.
This keeps the interface tiny (easy to implement — `FixedRandom` only needs two
methods) while still offering convenient helpers.

**The catch:** extension methods are only visible if you `using` the namespace
they are declared in. That is the exact bug mentioned at the top of this page.

---

## Access modifiers

| Modifier | Visible to |
|---|---|
| `public` | Everyone |
| `private` | Only this class (the default if you write nothing) |
| `internal` | Only this project/assembly |
| `protected` | This class and its subclasses |

`internal` is used deliberately here:

```csharp
internal void RemoveStatus(StatusEffect effect) => _statuses.Remove(effect);
internal void TickCooldowns() { ... }
```

`Battle` needs to call these, and `Battle` lives in the same project. But the
Godot layer, which is a *different* project, cannot — so the UI physically
cannot tick a cooldown or rip a status off an actor. The architecture rule is
enforced by the compiler, not by good intentions.

---

## `params`

```csharp
public static Actor MakeActor(string id, Team team, StatBlock stats, params SkillDefinition[] skills)
```

`params` means "any number of trailing arguments, collected into an array":

```csharp
MakeActor("hero", Team.Heroes, Stats(), heavy, Punch)   // skills = [heavy, Punch]
MakeActor("hero", Team.Heroes, Stats())                 // skills = []
```

Like `*args` in Python or `...rest` in JavaScript.

---

## Operator overloading

From [`StatBlock.cs`](../src/Rpg.Core/Entities/StatBlock.cs):

```csharp
public static StatBlock operator +(StatBlock a, StatBlock b) => new(
    a.MaxHealth + b.MaxHealth,
    a.Attack    + b.Attack,
    a.Defense   + b.Defense,
    a.Speed     + b.Speed,
    a.CritChance + b.CritChance);
```

This defines what `+` means for two `StatBlock`s. It is what makes this line
possible:

```csharp
total += status.Definition.Modifier;
```

Adding a buff to a stat sheet reads exactly like adding two numbers. Use this
sparingly — it is great when the operation is genuinely arithmetic-like, and
confusing otherwise.

---

## `async` / `await`

Only used in the Godot layer, and for one purpose: pausing between animations
without freezing the game.

```csharp
private async Task PlayEvents(IEnumerable<GameEvent> events)
{
    foreach (GameEvent gameEvent in events)
    {
        Write(Describe(gameEvent));
        RefreshViews();

        if (IsWorthPausingFor(gameEvent))
            await ToSignal(GetTree().CreateTimer(0.45), SceneTreeTimer.SignalName.Timeout);
    }
}
```

`await` here means: "stop running this method, let the game keep drawing frames
for 0.45 seconds, then resume from exactly this line." Without it, the entire
battle would resolve in one frame and the player would see only the final state.

If you have used `async`/`await` in JavaScript, Python, or Rust, it is the same
concept. Two C#-specific notes:

- `async Task` is the normal return type. `async void` is generally bad practice
  **except** for event handlers, which is exactly why
  `OnActionChosen` and `StartNewBattle` are `async void` — they are called by
  Godot's button signal and by `_Ready()`, neither of which can await.
- `ToSignal(...)` is Godot-specific: it converts "this engine signal will fire
  eventually" into something `await` understands.

---

## Things you will see and can ignore

| Thing | What it is |
|---|---|
| `/// <summary>` | XML documentation comments. Your IDE shows them as tooltips. Purely informational. |
| `<see cref="Battle"/>` | A link inside a doc comment. Ctrl-click navigates. |
| `#region` | Not used here. Good. |
| `[Fact]` | An **attribute** — metadata attached to code. This one tells the test runner "this method is a test". |
| `nameof(x)` | Turns a symbol into its own name as a string. Survives renames. |
| `!` at the end of `ToString()!` | Nullable suppression again — `ToString()` is declared as possibly-null, we know it is not. |

---

## Next

[Godot crash course](03-godot-crash-course.md) — the engine side.

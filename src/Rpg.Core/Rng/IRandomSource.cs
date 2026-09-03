// ============================================================================
//  IRANDOMSOURCE - all the dice in the game
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Every random decision in the game - every critical hit roll - goes through
//  this interface. Nothing anywhere calls "new Random()".
//
//  WHY BOTHER? WHY NOT JUST USE Random.Shared?
//  -------------------------------------------
//  Because a global random generator makes your game untestable and your bugs
//  unreproducible. Passing the source IN instead ("dependency injection") gives
//  you four things for free:
//
//    1. REPRODUCIBLE TESTS. Seed 4242 always plays out the identical battle, so
//       a test can assert on the exact damage numbers.
//
//    2. REPRODUCIBLE BUG REPORTS. A player sends you a seed; you see their exact
//       fight.
//
//    3. SEEDED RUNS, roguelike "daily challenge" style, at no extra cost.
//
//    4. REPLAYS AND NETWORKED PLAY, later. Two machines running the same rules
//       with the same seed stay in lockstep.
//
//  There are two implementations:
//
//      SplitMix64Random  - the real one, used by the game
//      FixedRandom       - a test double that returns numbers YOU choose
//
//  THE RULE: never call "new Random()" anywhere in Rpg.Core. Ever.
// ============================================================================

namespace Rpg.Core.Rng;

public interface IRandomSource
{
    /// <summary>A whole number from minInclusive up to but NOT including maxExclusive.</summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>A decimal from 0 up to but not including 1.</summary>
    double NextDouble();
}

// ============================================================================
//  Convenience helpers.
//
//  C# NOTE: these are EXTENSION METHODS. The "this" on the first parameter is
//  what makes the magic work - you can write
//
//      rng.Chance(25)
//
//  even though IRandomSource itself only declares NextInt and NextDouble.
//
//  Why do it this way? It keeps the interface tiny, so writing a new
//  implementation (like FixedRandom) means implementing two methods instead of
//  five - while callers still get the nice helpers.
//
//  THE CATCH: extension methods are only visible if you "using" the namespace
//  they live in. Forgetting "using Rpg.Core.Rng;" produces a confusing
//  "does not contain a definition for 'Chance'" error. That was, in fact, the
//  very first compile error this project ever had.
// ============================================================================
public static class RandomSourceExtensions
{
    /// <summary>
    /// True this percent of the time. 0 = never, 100 = always.
    ///
    /// Note the short-circuit: when percent is 0 the "&amp;&amp;" stops before
    /// NextInt is ever called, so the random generator is not advanced at all.
    /// That is deliberate and load-bearing - give a test actor CritChance 0 and
    /// every damage number becomes exactly predictable.
    /// </summary>
    public static bool Chance(this IRandomSource rng, int percent) =>
        percent > 0 && rng.NextInt(0, 100) < percent;

    /// <summary>An inclusive roll on both ends. Range(1, 6) is a six-sided die.</summary>
    public static int Range(this IRandomSource rng, int minInclusive, int maxInclusive) =>
        rng.NextInt(minInclusive, maxInclusive + 1);

    /// <summary>One random item from a list.</summary>
    public static T Pick<T>(this IRandomSource rng, IReadOnlyList<T> items) =>
        items[rng.NextInt(0, items.Count)];
}

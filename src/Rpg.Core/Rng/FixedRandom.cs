// ============================================================================
//  FIXEDRANDOM - fake dice, for tests
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A stand-in for the real random generator, where YOU decide what the "random"
//  numbers are.
//
//  This is how a test can assert "a critical hit does exactly double damage"
//  without rolling real dice ten thousand times and hoping to observe one. It is
//  the entire reason IRandomSource is an interface rather than a concrete class.
//
//  (The jargon for this is a "test double", or more specifically a "stub".)
//
//  HOW THE TESTS USE IT
//  --------------------
//  TestFixtures.Duel() creates battles with "new FixedRandom(0)". Combined with
//  actors that have CritChance 0, the Chance() helper short-circuits and never
//  touches the generator at all - so every damage number in those tests is
//  exact, and no test can ever fail intermittently.
//
//  To force a critical hit in a test:  give the actor CritChance 100.
//  To force no critical hit:           give the actor CritChance 0.
// ============================================================================

namespace Rpg.Core.Rng;

public sealed class FixedRandom : IRandomSource
{
    private readonly int[] _ints;
    private readonly double[] _doubles;
    private int _intIndex;
    private int _doubleIndex;

    // C# note: "params" lets callers write new FixedRandom(1, 2, 3) instead of
    // new FixedRandom(new[] { 1, 2, 3 }).
    public FixedRandom(params int[] ints) : this(ints, Array.Empty<double>()) { }

    public FixedRandom(int[] ints, double[] doubles)
    {
        _ints = ints;
        _doubles = doubles;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (_ints.Length == 0) return minInclusive;

        // "% _ints.Length" wraps back to the start when we run out, so a short
        // script of numbers can drive a long battle without the test having to
        // predict how many rolls will happen.
        int value = _ints[_intIndex++ % _ints.Length];

        // Clamp, so a scripted value can never fall outside the requested range
        // and produce a nonsense result.
        return Math.Clamp(value, minInclusive, maxExclusive - 1);
    }

    public double NextDouble()
    {
        if (_doubles.Length == 0) return 0.0;
        return _doubles[_doubleIndex++ % _doubles.Length];
    }
}

// ============================================================================
//  SPLITMIX64RANDOM - the real random number generator
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A small, fast, well-known algorithm for generating random-looking numbers
//  from a starting "seed". Same seed in, same sequence out, forever, on every
//  machine.
//
//  WHY NOT JUST USE System.Random?
//  -------------------------------
//  Because Microsoft has CHANGED the algorithm inside System.Random between .NET
//  versions. That is entirely reasonable of them - it is documented as an
//  implementation detail - but it is fatal for us.
//
//  If your saved replays, your seeded runs, and your balance tests all depend on
//  the exact sequence of numbers, then you need an algorithm you own. This one
//  is about fifteen lines and will produce identical output in ten years.
//
//  It is NOT cryptographically secure. It does not need to be. Do not use it for
//  passwords.
//
//  C# NOTES
//  --------
//  "ulong" is a 64-bit unsigned integer (0 to about 18 quintillion). The
//  algorithm relies on numbers wrapping around silently when they overflow,
//  which unsigned types do by default in C#.
//
//  ">>" shifts bits right. "^" is bitwise XOR. The specific constants are from
//  the published SplitMix64 algorithm - they are chosen to scramble bits well.
//  You do not need to understand them to use this.
// ============================================================================

namespace Rpg.Core.Rng;

public sealed class SplitMix64Random : IRandomSource
{
    private ulong _state;

    /// <param name="seed">The starting number. The same seed always produces the same sequence.</param>
    public SplitMix64Random(ulong seed) => _state = seed;

    /// <summary>
    /// The current internal state. Save this alongside a game save and you can
    /// resume a battle mid-fight with the randomness continuing exactly where it
    /// left off.
    /// </summary>
    public ulong State => _state;

    /// <summary>The core algorithm. Advances the state and scrambles it into a result.</summary>
    private ulong NextUInt64()
    {
        ulong z = (_state += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                $"maxExclusive ({maxExclusive}) must be greater than minInclusive ({minInclusive}).");

        // "% range" wraps the huge random number down into the range we want.
        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextUInt64() % range));
    }

    /// <summary>
    /// A decimal in [0, 1). The shift and multiply turn 53 random bits into an
    /// evenly spread double - the standard way to do this.
    /// </summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
}

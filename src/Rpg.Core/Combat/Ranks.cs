// ============================================================================
//  RANKS - which positions a skill reaches
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Both sides stand in a line. Position 1 is the FRONT - closest to the enemy -
//  and position 4 is the back.
//
//        your party                    the enemy
//     4    3    2    1        |     1    2    3    4
//    back ---------> front    |   front <--------- back
//
//  Every skill declares two things:
//
//      LaunchRanks   which positions you can USE it from
//      TargetRanks   which enemy positions it can REACH
//
//  A sword needs to be near the front and can only reach the front. A bow is
//  useless in the front rank but can pick off the enemy's back line. That one
//  rule turns "who do I hit?" into "who CAN I hit, and where is everybody
//  standing?" - which is the whole of Darkest Dungeon's combat, and it costs
//  about a hundred lines.
//
//  RANKS CLOSE UP
//  --------------
//  Dead fighters do not hold a position. Kill the enemy's front rank and the one
//  behind steps forward into reach - so killing things changes what your melee
//  can do next turn. See BattleState.RankOf.
//
//  HOW IT IS STORED
//  ----------------
//  A four-bit mask, bit 0 = rank 1. It is written as a string in the content
//  files because "##--" is instantly readable and 0b0011 is not.
// ============================================================================

namespace Rpg.Core.Combat;

public readonly record struct Ranks(int Mask)
{
    /// <summary>The most positions a side can occupy.</summary>
    public const int Max = 4;

    // ---- the common shapes, named ----------------------------------------

    /// <summary>Every position. Self-buffs, heals, and anything that ignores the line.</summary>
    public static readonly Ranks Any = new(0b1111);

    /// <summary>Front rank only. Reach-1 weapons.</summary>
    public static readonly Ranks Front = new(0b0001);

    /// <summary>The front two. Ordinary melee.</summary>
    public static readonly Ranks FrontTwo = new(0b0011);

    /// <summary>The front three. Long polearms, whips.</summary>
    public static readonly Ranks FrontThree = new(0b0111);

    /// <summary>The back two. Where casters and archers stand, and what snipers hit.</summary>
    public static readonly Ranks BackTwo = new(0b1100);

    /// <summary>The back three. Most ranged weapons can be used from here.</summary>
    public static readonly Ranks BackThree = new(0b1110);

    /// <summary>Positions two and three. The awkward middle.</summary>
    public static readonly Ranks Middle = new(0b0110);

    /// <summary>Builds a mask from 1-based positions: Of(1, 3) is ranks one and three.</summary>
    public static Ranks Of(params int[] positions)
    {
        int mask = 0;
        foreach (int p in positions)
            if (p is >= 1 and <= Max)
                mask |= 1 << (p - 1);
        return new Ranks(mask);
    }

    /// <summary>
    /// Parses the compact notation used in the content files, where '#' is a
    /// reachable position and '-' is not, front-first: "##--" is the front two.
    /// </summary>
    public static Ranks Parse(string pattern)
    {
        int mask = 0;
        for (int i = 0; i < pattern.Length && i < Max; i++)
            if (pattern[i] is '#' or 'X' or 'x' or '1')
                mask |= 1 << i;
        return new Ranks(mask);
    }

    // ---- asking questions ------------------------------------------------

    /// <summary>Is this 1-based position in the mask?</summary>
    public bool Includes(int rank) =>
        rank is >= 1 and <= Max && (Mask & (1 << (rank - 1))) != 0;

    public bool IsEmpty => Mask == 0;

    /// <summary>"##--", front-first. Shown on skill buttons so the rule is visible, not hidden.</summary>
    public string Diagram
    {
        get
        {
            Span<char> chars = stackalloc char[Max];
            for (int i = 0; i < Max; i++)
                chars[i] = (Mask & (1 << i)) != 0 ? '#' : '-';
            return new string(chars);
        }
    }

    public override string ToString() => Diagram;
}

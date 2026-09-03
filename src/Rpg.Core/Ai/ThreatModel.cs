// ============================================================================
//  THREATMODEL - how dangerous each hero looks to the enemy
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  "Which of these three should we kill first?"
//
//  Left to raw damage numbers, the AI attacks whoever it can hurt most - which
//  is always the squishiest hero, and which is often wrong. The Cleric with 7
//  Defense who heals 26 a turn is a far bigger problem than the Rogue with 4
//  Defense, because killing the Cleric ends the fight and killing the Rogue just
//  removes some damage.
//
//  So the enemy rates every hero on what they DO, not on how easy they are to
//  hit, and adds that to the score of any action aimed at them.
//
//  This is one of the cheapest ways to make an opponent feel intelligent. It is
//  about forty lines and it completely changes how a fight reads: the party
//  suddenly has to protect the healer, which is a decision they did not have to
//  make before.
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Entities;

namespace Rpg.Core.Ai;

public static class ThreatModel
{
    // ---- what makes a target worth killing --------------------------------

    /// <summary>A hero who can heal is the biggest problem on the field.</summary>
    public const double HealerThreat = 42.0;

    /// <summary>...and more so the more they heal for.</summary>
    public const double PerPointOfHealing = 0.8;

    /// <summary>A hero who buffs the others is nearly as bad.</summary>
    public const double SupportThreat = 18.0;

    /// <summary>Raw damage output still matters, just less than support does.</summary>
    public const double PerPointOfAttack = 1.1;

    /// <summary>Fast actors get more turns, so they are worth more dead.</summary>
    public const double PerPointOfSpeed = 0.7;

    /// <summary>
    /// FINISH THE WOUNDED. Scaled by how hurt they already are, so a pack
    /// concentrates instead of spreading damage across three healthy heroes.
    ///
    /// This is the single most important line in the file. Focused fire removes
    /// a turn from every future round; spread damage removes none.
    /// </summary>
    public const double FocusOnWounded = 46.0;

    /// <summary>How badly the enemy wants a target dead, before considering the specific attack.</summary>
    public static double ThreatOf(Actor target)
    {
        if (!target.IsAlive) return 0;

        double threat = 0;
        StatBlock stats = target.CurrentStats;

        threat += stats.Attack * PerPointOfAttack;
        threat += stats.Speed * PerPointOfSpeed;

        foreach (SkillDefinition skill in target.Skills)
        {
            // Healing is the thing that most reliably beats a monster pack.
            if (skill.Heals)
                threat += HealerThreat + skill.Healing * PerPointOfHealing;

            // So is a buff aimed at somebody else.
            if (skill.Target == TargetKind.SingleAlly && skill.AppliesStatus is not null)
                threat += SupportThreat;

            // Draining attacks make a target hard to grind down.
            if (skill.Drains)
                threat += skill.LifestealPercent * 0.25;
        }

        // ...and finish what somebody already started.
        double missing = 1.0 - (double)target.Health / Math.Max(1, target.MaxHealth);
        threat += missing * FocusOnWounded;

        return threat;
    }
}

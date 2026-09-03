// ============================================================================
//  SCORINGAI - how monsters decide what to do
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Look at every legal move. Give each one a number. Take the highest. That is
//  the entire algorithm.
//
//  There is no machine learning here and none is needed. "AI" in games usually
//  means exactly this: a scoring function plus some tuning.
//
//  WHY THIS FILE MATTERS MORE THAN IT LOOKS
//  ----------------------------------------
//  This is where "deep" is won or lost. A tactically competent opponent makes
//  mediocre mechanics feel great. A stupid opponent makes brilliant mechanics
//  feel pointless, because the player never has to engage with them.
//
//  A REAL EXAMPLE FROM THIS PROJECT: with DamageOverTimeWeight set to 0.9, the
//  goblin preferred a Poison Dart dealing 2 direct damage over a Club dealing
//  10 - because 4 damage x 3 turns x 0.9 = 10.8 beat 10. In a fight lasting six
//  rounds that is simply wrong. Changing that ONE NUMBER to 0.6 moved the
//  encounter's win rate eight times further than a whole round of stat changes
//  did. See docs/04-architecture.md#measuring-instead-of-guessing.
//
//  IT CANNOT CHEAT
//  ---------------
//  It only ever picks from Battle.LegalActions() - the same list that becomes
//  the player's buttons. So it is structurally incapable of ignoring a cooldown,
//  acting while stunned, or targeting a corpse.
//
//  IT IS SHORT-SIGHTED BY CONSTRUCTION
//  -----------------------------------
//  It scores each move on its own merits without considering what happens next
//  ("one-ply"). Two upgrade paths, both written up in docs/07-recipes.md:
//
//    1. SEARCH. Give BattleState a Clone(), apply the move to the clone, and
//       score the resulting POSITION instead of the move. That turns this into a
//       real minimax and is the honest way to get an opponent that plans ahead.
//
//    2. PERSONALITY. Multiply the weights per enemy type - a berserker weights
//       damage x2 and healing x0; a shaman weights statuses x3. Cheap, and it
//       makes encounters feel authored rather than generated.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Entities;

namespace Rpg.Core.Ai;

public static class ScoringAi
{
    // ========================================================================
    //  EVERY KNOB THE AI HAS, IN ONE PLACE.
    //
    //  Treat these as GAME DESIGN VALUES, not as code. You should be able to
    //  change how the monsters behave without reading a single line below.
    // ========================================================================

    /// <summary>Bonus for a killing blow. Removing an actor removes all their future turns.</summary>
    public const double LethalBonus = 60.0;

    /// <summary>Baseline: one point of useful damage is worth one point of score.</summary>
    public const double DamageWeight = 1.0;

    /// <summary>Healing is worth slightly more than damage, point for point.</summary>
    public const double HealWeight = 1.3;

    /// <summary>Extra urgency for healing someone nearly dead.</summary>
    public const double EmergencyHealBonus = 25.0;

    /// <summary>What counts as "nearly dead" - below 35% of maximum health.</summary>
    public const double EmergencyHealthFraction = 0.35;

    /// <summary>What denying an enemy one turn is worth. Raise it and monsters stun constantly.</summary>
    public const double StunValue = 16.0;

    // Damage over time is DISCOUNTED, not counted at face value. 12 damage
    // spread across three turns is worth less than 12 damage right now, because
    // the fight may end first and because the target may be healed in between.
    //
    // Set this to 1.0 and the AI starts preferring a 2-damage poison dart over a
    // 10-damage club - exactly the kind of quietly terrible play that makes an
    // opponent feel stupid without the player being able to say why.
    public const double DamageOverTimeWeight = 0.6;

    /// <summary>Flat value of applying a stat buff or debuff.</summary>
    public const double BuffValue = 10.0;

    /// <summary>Doing nothing. Deliberately tiny, so it only wins when nothing else can.</summary>
    public const double PassValue = 0.1;

    /// <summary>
    /// Swapping places. Just above passing, so it only wins when the actor has
    /// no attack available at all - which happens when they are stuck in a rank
    /// their skills cannot be used from.
    /// </summary>
    public const double RepositionValue = 0.4;

    /// <summary>
    /// How much the enemy weighs "is this target worth killing?" against "how
    /// much damage can I do to them?".
    ///
    /// At 0 the AI just hits whoever is squishiest. At 1.0 it plays like it has
    /// read the party sheet - it goes for the healer, and it finishes anyone
    /// already wounded. See ThreatModel.
    ///
    /// MEASURED, not guessed. The first version used 0.55 and the campaign clear
    /// rate was 5%: every monster in the room lasered the Cleric down inside two
    /// rounds, and a party with no healer does not survive six more encounters.
    /// A clever opponent is good; one that always removes your best piece first,
    /// with no taunt or guard mechanic available to stop it, is just unfair.
    /// </summary>
    public const double ThreatWeight = 0.30;

    // ========================================================================

    /// <summary>Picks the best move available to this actor right now.</summary>
    public static IAction ChooseAction(Battle battle, Actor actor)
    {
        List<IAction> options = battle.LegalActions(actor);

        // Defensive - LegalActions always includes PassAction, so this should be
        // impossible. Cheap insurance against a future change breaking that.
        if (options.Count == 0)
            return new PassAction(actor);

        IAction best = options[0];
        double bestScore = double.NegativeInfinity;

        foreach (IAction option in options)
        {
            double score = Score(option);

            // Tie-break on the LABEL, never on position in the list.
            //
            // Deterministic tie-breaking is what lets a seed reproduce a whole
            // battle. If two moves score identically and we just took whichever
            // came first, then any change to the order skills are declared in
            // would silently change every AI decision.
            bool better = score > bestScore
                || (score == bestScore && string.CompareOrdinal(option.Label, best.Label) < 0);

            if (better)
            {
                best = option;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>How good is this move? Higher is better. Public so you can test and tune it.</summary>
    public static double Score(IAction action)
    {
        // C# note: "is not ... skill" tests the type and, if it matches, gives us
        // a typed variable in one step. Anything that is not a skill (i.e. Wait)
        // scores the tiny PassValue.
        // Shuffling is normally a wasted turn - but when an actor is stranded in
        // a rank where none of their skills work, it is the only useful thing
        // they can do. ChooseAction below detects that case and scores it up.
        if (action is MoveAction)
            return RepositionValue;

        if (action is not SkillAction skill)
            return PassValue;

        Actor user = skill.Actor;
        Actor target = skill.Target;
        double score = 0;

        // Is this target worth killing at all? Applied to anything hostile, so
        // it steers BOTH the plain attacks and the debuffs onto the right hero.
        bool hostile = target.Team != user.Team;
        if (hostile)
            score += ThreatModel.ThreatOf(target) * ThreatWeight;

        // ---- Damage ----
        if (skill.Skill.DealsDamage)
        {
            // Score the AVERAGE case: assume no critical hit. Scoring the best
            // case would make the AI gamble on low-probability crits, which
            // reads to a player as stupid rather than bold.
            int expected = DamageCalculator.Compute(
                user.CurrentStats, target.CurrentStats, skill.Skill.Power, isCritical: false);

            // Overkill is wasted. 40 damage into a 6 HP goblin is worth 6, not
            // 40 - otherwise the AI would fire its biggest attack at the weakest
            // target every time.
            int useful = Math.Min(expected, target.Health);
            score += useful * DamageWeight;

            if (expected >= target.Health)
                score += LethalBonus;
        }

        // ---- Healing ----
        if (skill.Skill.Heals)
        {
            // Same idea: healing 14 onto someone missing 3 health is worth 3.
            // This is what stops the AI wasting turns topping up a full ally.
            int useful = Math.Min(skill.Skill.Healing, target.MaxHealth - target.Health);
            score += useful * HealWeight;

            if (target.Health < target.MaxHealth * EmergencyHealthFraction)
                score += EmergencyHealBonus;

            // Heal the ally who matters most, not just the one missing the most.
            score += ThreatModel.ThreatOf(target) * ThreatWeight * 0.5;
        }

        // ---- Status ----
        if (skill.Skill.AppliesStatus is { } status)
        {
            // Never waste a turn re-applying something already active. Note this
            // returns the damage score computed above, not zero - a poison dart
            // on an already-poisoned target is still a (weak) attack.
            if (target.HasStatus(status.Id))
                return score;

            if (status.PreventsAction)
                score += StunValue;

            if (status.DamagePerTurn > 0)
                score += status.DamagePerTurn * skill.Skill.StatusTurns * DamageOverTimeWeight;

            if (status.Modifier != StatBlock.Zero)
                score += BuffValue;
        }

        return score;
    }
}

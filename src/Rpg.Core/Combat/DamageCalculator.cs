// ============================================================================
//  DAMAGECALCULATOR - the one and only damage formula
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Every damage number in the entire game comes out of the one method below.
//
//  KEEP IT THAT WAY. The moment damage maths is scattered across ten different
//  skill classes, you can no longer answer "why did that hit for 43?" without
//  archaeology, and you can no longer rebalance the game at all. One formula,
//  one file, one place to later add armour penetration or fire resistance.
//
//  WORKED EXAMPLE
//  --------------
//  Stick Warrior (Attack 15) uses Heavy Blow (Power 180) on the Goblin (Def 4):
//
//      raw       = 15 * 180 / 100  =  27
//      mitigated = 27 - (4 / 2)    =  25      <- integer division: 4/2 = 2
//      critical? = no
//      result    = max(1, 25)      =  25
//
//  If the crit had landed:  25 * 200 / 100 = 50.
//
//  C# NOTE
//  -------
//  "static class" means no objects of this type ever exist. It is just a bag of
//  functions, called on the type itself: DamageCalculator.Compute(...).
//
//  Integer division truncates: 7 / 2 is 3, not 3.5. That is intentional here -
//  whole numbers are easier for a player to reason about than decimals.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public static class DamageCalculator
{
    /// <summary>
    /// A hit always does at least this much, however tanky the defender.
    ///
    /// Without this floor, a high-defence actor becomes literally unkillable and
    /// the battle runs until the round limit. There is a test for it:
    /// DamageNeverDropsBelowOne.
    /// </summary>
    public const int MinimumDamage = 1;

    /// <summary>200 = a critical hit does double damage.</summary>
    public const int CriticalMultiplierPercent = 200;

    public static int Compute(StatBlock attacker, StatBlock defender, int power, bool isCritical)
    {
        // Power is a PERCENTAGE, not a flat number: 100 means "a normal hit for
        // your Attack value", 180 means "1.8x your Attack". Percentages keep a
        // skill relevant as the character levels up.
        int raw = attacker.Attack * power / 100;

        // Defence SUBTRACTS rather than divides.
        //
        // Subtractive armour is easy to reason about and easy to explain to a
        // player, at the cost of scaling badly at very high numbers - eventually
        // defence either trivialises damage or does nothing at all. If your
        // endgame stats get into the thousands, revisit this. That is a design
        // decision to make deliberately, not a bug. docs/07-recipes.md has a
        // multiplicative alternative.
        int mitigated = raw - defender.Defense / 2;

        // Crits multiply AFTER mitigation, so armour does not blunt them.
        if (isCritical)
            mitigated = mitigated * CriticalMultiplierPercent / 100;

        return Math.Max(MinimumDamage, mitigated);
    }
}

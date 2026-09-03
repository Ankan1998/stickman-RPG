// ============================================================================
//  STATUSES - every condition a fighter can be under
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Poison, burning, bleed, stun, buffs, debuffs. All of them are DATA - there is
//  no "class Poison" anywhere in the codebase and no switch on status names in
//  the combat code.
//
//  EACH DUNGEON HAS A SIGNATURE STATUS
//  -----------------------------------
//  A harder dungeon that is just "the same fight with bigger numbers" is dull.
//  So each one attacks you differently:
//
//    The Warrens      POISON   4/turn for 3 turns. Slow, stacks up across a pack.
//                     BLEED    6/turn for 2 turns. Faster, from claws and fangs.
//
//    Ember Halls      BURNING  9/turn for 2 turns. Enormous burst - you have to
//                              heal through it or kill before the second tick.
//
//    Frozen Crypt     CHILLED  -6 Speed. You lose the turn order itself.
//                     CURSED   -4 Attack, -3 Defense. You lose your damage.
//
//  Notice how little machinery this needs. "Lose the turn order" is one negative
//  number in a StatBlock, because TurnQueue already sorts on CurrentStats.
// ============================================================================

using Rpg.Core.Effects;
using Rpg.Core.Entities;

namespace Rpg.Core.Content;

public static class Statuses
{
    // ---- damage over time -------------------------------------------------

    /// <summary>The Warrens' signature. Slow but relentless.</summary>
    public static readonly StatusDefinition Poison = new(
        Id: "poison", Name: "Poisoned",
        Description: "Loses 4 health at the end of each turn.",
        Modifier: StatBlock.Zero, DamagePerTurn: 4, Icon: "poison");

    /// <summary>Claws and fangs. Hurts more than poison, lasts less long.</summary>
    public static readonly StatusDefinition Bleed = new(
        Id: "bleed", Name: "Bleeding",
        Description: "Loses 6 health at the end of each turn.",
        Modifier: StatBlock.Zero, DamagePerTurn: 6, Icon: "blood");

    /// <summary>Ember Halls' signature. Brutal, but it burns out fast.</summary>
    public static readonly StatusDefinition Burning = new(
        Id: "burning", Name: "Burning",
        Description: "Loses 9 health at the end of each turn.",
        Modifier: StatBlock.Zero, DamagePerTurn: 9, Icon: "fire");

    // ---- control ----------------------------------------------------------

    public static readonly StatusDefinition Stun = new(
        Id: "stun", Name: "Stunned",
        Description: "Cannot act at all this turn.",
        Modifier: StatBlock.Zero, PreventsAction: true, Icon: "stun");

    /// <summary>Frozen Crypt. Costs you the turn order, which is worse than it sounds.</summary>
    public static readonly StatusDefinition Chilled = new(
        Id: "chilled", Name: "Chilled",
        Description: "Badly slowed - acts much later in the round.",
        Modifier: new StatBlock(0, 0, 0, -6, 0), Icon: "ice");

    /// <summary>Webbed. Same idea as chilled, from spiders rather than cold.</summary>
    public static readonly StatusDefinition Webbed = new(
        Id: "webbed", Name: "Webbed",
        Description: "Slowed and clumsy.",
        Modifier: new StatBlock(0, 0, 0, -4, -5), Icon: "debuff");

    // ---- debuffs ----------------------------------------------------------

    public static readonly StatusDefinition Weakened = new(
        Id: "weakened", Name: "Weakened",
        Description: "Hits much more softly.",
        Modifier: new StatBlock(0, -5, 0, 0, 0), Icon: "debuff");

    /// <summary>Frozen Crypt's second signature. Takes your damage AND your armour.</summary>
    public static readonly StatusDefinition Cursed = new(
        Id: "cursed", Name: "Cursed",
        Description: "Weaker and more fragile.",
        Modifier: new StatBlock(0, -4, -3, 0, 0), Icon: "debuff");

    /// <summary>Armour shredded - the wraith's speciality.</summary>
    public static readonly StatusDefinition Sundered = new(
        Id: "sundered", Name: "Sundered",
        Description: "Armour broken open.",
        Modifier: new StatBlock(0, 0, -6, 0, 0), Icon: "debuff");

    // ---- buffs ------------------------------------------------------------

    public static readonly StatusDefinition Guard = StatusDefinition.Buff(
        "guard", "Guarding", "Braced - greatly increased defence.",
        new StatBlock(0, 0, 7, 0, 0), icon: "guard");

    public static readonly StatusDefinition Blessed = StatusDefinition.Buff(
        "blessed", "Blessed", "Strikes harder and truer.",
        new StatBlock(0, 5, 0, 0, 8), icon: "buff");

    public static readonly StatusDefinition Enraged = StatusDefinition.Buff(
        "enraged", "Enraged", "Hits far harder, defends far worse.",
        new StatBlock(0, 8, -4, 0, 6), icon: "buff");

    public static readonly StatusDefinition Rallied = StatusDefinition.Buff(
        "rallied", "Rallied", "Fights with terrifying strength.",
        new StatBlock(0, 6, 0, 0, 5), icon: "buff");

    public static readonly StatusDefinition Focused = StatusDefinition.Buff(
        "focused", "Focused", "Moving fast, striking precisely.",
        new StatBlock(0, 0, 0, 4, 20), icon: "buff");

    /// <summary>
    /// Every status in the game.
    ///
    /// GOTCHA: a status missing from this list works perfectly in combat and then
    /// crashes the UI the moment it tries to print the status's name. The rules
    /// reach a status through the skill that applies it; only the DISPLAY needs
    /// this lookup table.
    /// </summary>
    public static readonly StatusDefinition[] All =
    {
        Poison, Bleed, Burning,
        Stun, Chilled, Webbed,
        Weakened, Cursed, Sundered,
        Guard, Blessed, Enraged, Rallied, Focused,
    };
}

// ============================================================================
//  STATUSDEFINITION - what a status effect IS
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The template for poison, stun, a defence buff. There is exactly ONE "poison"
//  in the whole game, and this is it.
//
//  (The thing tracking "this particular goblin has 2 turns of poison left" is
//  StatusEffect, in the file next door. Template vs instance.)
//
//  THE IMPORTANT PART
//  ------------------
//  There is no "class Poison" and no "class Stun" anywhere. There is no
//  "switch (statusId)" in the combat code. A status is just DATA - a stat
//  modifier, a per-turn damage number, and one flag:
//
//      Poison         ->  DamagePerTurn: 4
//      Stun           ->  PreventsAction: true
//      Guard (buff)   ->  Modifier: +6 Defense
//      Haste          ->  Modifier: +20 Speed
//      Curse          ->  Modifier: -5 Attack
//
//  That is the "Type Object" pattern, and it is why you can add Burning,
//  Regeneration or Enraged without touching the battle engine at all.
//
//  WHEN TO BREAK IT
//  ----------------
//  This will eventually stop being expressive enough - probably the first time
//  you want "on being hit, reflect 20% of the damage". The upgrade then is to
//  give the definition a list of TRIGGERS. Not before: premature generality here
//  costs more than it saves.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Effects;

public sealed record StatusDefinition(
    string Id,            // unique key, e.g. "poison". Referenced by events.
    string Name,          // shown to the player, e.g. "Poison"
    string Description,   // for a tooltip, once you build one

    // Added to the bearer's stats for as long as this is active.
    // Use NEGATIVE values for debuffs. StatBlock.Zero for statuses that change
    // no stats at all, like poison.
    StatBlock Modifier,

    // Health lost at the end of each of the bearer's turns.
    // Poison, burning, bleed.
    int DamagePerTurn = 0,

    // If true, the bearer loses their turn entirely.
    // Stun, freeze, sleep.
    bool PreventsAction = false,

    // Purely cosmetic: a short tag drawn above the actor, e.g. "PSN".
    string Icon = "*")
{
    /// <summary>
    /// Shorthand for a status whose only job is to change stats - no damage, no
    /// stun. Just saves repeating the defaults.
    /// </summary>
    public static StatusDefinition Buff(string id, string name, string description, StatBlock modifier, string icon = "+") =>
        new(id, name, description, modifier, Icon: icon);
}

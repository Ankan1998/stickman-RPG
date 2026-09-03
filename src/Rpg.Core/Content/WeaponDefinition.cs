// ============================================================================
//  WEAPONDEFINITION - a piece of loot
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A weapon is a stat bonus with a name and a look. Equip it and the wielder's
//  CurrentStats go up; that is the whole mechanic.
//
//  WHY THE STATS ARE COMPUTED, NOT TYPED OUT
//  -----------------------------------------
//  There are 47 weapons. Hand-authoring 47 stat blocks would mean 47 chances to
//  make a Rare weaker than a Common by accident, and no way to rebalance
//  "legendaries feel weak" without editing six entries.
//
//  Instead each weapon declares only two things - its RARITY (how much power it
//  gets to spend) and its ARCHETYPE (how it spends it) - and the numbers fall
//  out of that. A dagger and a hammer of the same rarity are equally strong;
//  they are just strong in different ways.
//
//  Rebalancing all loot is now a two-table edit. See BudgetFor and SpendOf.
//
//  THE ARCHETYPE ALSO DOES REAL WORK
//  ---------------------------------
//  It decides which sound plays when the weapon lands - a hammer goes "blunt",
//  a dagger goes "slash_light", a spear goes "pierce". So the audio varies with
//  your loot without a single special case anywhere in combat.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Content;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

/// <summary>What kind of weapon it is. Drives both the stat spread and the hit sound.</summary>
public enum WeaponKind
{
    // light blades - fast, crit-heavy
    Dagger, Sword, Katana, Whip,
    // heavy blades - raw damage
    Greatsword, Greataxe, Axe, Scythe,
    // blunt - damage plus a little toughness
    Club, Mace, Hammer, Flail,
    // piercing - reach and accuracy
    Spear, Trident,
    // ranged - crit specialists
    Bow, Crossbow,
    // casting - modest damage, some survivability
    Staff, Wand, Orb, Tome, Torch,
    // defensive
    Shield, Claw,
}

public sealed record WeaponDefinition(string Id, string Label, WeaponKind Kind, Rarity Rarity)
{
    /// <summary>The 24x24 icon in game/assets/weapons/.</summary>
    public string IconName => Id;

    /// <summary>The stat bonus this weapon grants, derived from rarity and kind.</summary>
    public StatBlock Bonus => SpendOf(Kind, BudgetFor(Rarity));

    /// <summary>Which combat sound family this weapon uses when it connects.</summary>
    public string HitSound => Kind switch
    {
        WeaponKind.Dagger or WeaponKind.Sword or WeaponKind.Katana or WeaponKind.Whip => "slash_light",
        WeaponKind.Greatsword or WeaponKind.Greataxe or WeaponKind.Axe or WeaponKind.Scythe => "slash_heavy",
        WeaponKind.Club or WeaponKind.Mace => "blunt_hit",
        WeaponKind.Hammer or WeaponKind.Flail => "blunt_heavy",
        WeaponKind.Spear or WeaponKind.Trident => "pierce",
        WeaponKind.Bow => "bow_release",
        WeaponKind.Crossbow => "crossbow_release",
        WeaponKind.Claw => "claw_hit",
        _ => "spell_cast",
    };

    public string RarityLabel => Rarity.ToString();

    /// <summary>A one-line summary for the loot screen, e.g. "+4 Attack, +6 Crit".</summary>
    public string Summary
    {
        get
        {
            StatBlock b = Bonus;
            var parts = new List<string>();
            if (b.Attack != 0) parts.Add($"{b.Attack:+#;-#;0} Attack");
            if (b.Defense != 0) parts.Add($"{b.Defense:+#;-#;0} Defense");
            if (b.Speed != 0) parts.Add($"{b.Speed:+#;-#;0} Speed");
            if (b.CritChance != 0) parts.Add($"{b.CritChance:+#;-#;0}% Crit");
            if (b.MaxHealth != 0) parts.Add($"{b.MaxHealth:+#;-#;0} Health");
            return parts.Count == 0 ? "No bonus" : string.Join(", ", parts);
        }
    }

    // ------------------------------------------------------------------
    //  The two tables that balance every weapon in the game
    // ------------------------------------------------------------------

    /// <summary>How many points of power a weapon of this rarity gets to spend.</summary>
    private static int BudgetFor(Rarity rarity) => rarity switch
    {
        Rarity.Common => 4,
        Rarity.Uncommon => 8,
        Rarity.Rare => 13,
        Rarity.Epic => 19,
        Rarity.Legendary => 27,
        _ => 0,
    };

    /// <summary>
    /// How an archetype spends its budget.
    ///
    /// Percentages of the budget, so the same table works at every rarity. Crit
    /// is deliberately cheap per point (it is a chance, not a guarantee) and
    /// Health is cheapest of all.
    /// </summary>
    private static StatBlock SpendOf(WeaponKind kind, int budget)
    {
        // (attack%, defense%, speed%, crit% at 2x, health% at 3x)
        (int atk, int def, int spd, int crit, int hp) mix = kind switch
        {
            WeaponKind.Dagger => (40, 0, 20, 80, 0),
            WeaponKind.Sword => (70, 10, 0, 40, 0),
            WeaponKind.Katana => (60, 0, 15, 70, 0),
            WeaponKind.Whip => (50, 0, 30, 50, 0),

            WeaponKind.Greatsword => (110, 0, -10, 20, 0),
            WeaponKind.Greataxe => (120, 0, -15, 30, 0),
            WeaponKind.Axe => (95, 0, 0, 30, 0),
            WeaponKind.Scythe => (100, 0, 0, 50, 0),

            WeaponKind.Club => (85, 15, 0, 0, 30),
            WeaponKind.Mace => (80, 25, 0, 0, 20),
            WeaponKind.Hammer => (105, 20, -10, 0, 30),
            WeaponKind.Flail => (90, 10, 0, 40, 0),

            WeaponKind.Spear => (80, 15, 10, 30, 0),
            WeaponKind.Trident => (85, 20, 5, 35, 0),

            WeaponKind.Bow => (65, 0, 25, 90, 0),
            WeaponKind.Crossbow => (85, 0, 0, 70, 0),

            WeaponKind.Staff => (70, 15, 0, 20, 40),
            WeaponKind.Wand => (75, 0, 15, 40, 0),
            WeaponKind.Orb => (65, 20, 0, 30, 50),
            WeaponKind.Tome => (60, 25, 0, 20, 60),
            WeaponKind.Torch => (70, 10, 10, 30, 20),

            WeaponKind.Shield => (10, 100, -5, 0, 70),
            WeaponKind.Claw => (75, 0, 25, 75, 0),

            _ => (75, 10, 0, 25, 0),
        };

        // Crit and health buy more per point, because a point of each is worth
        // less than a point of Attack. Rounding is deliberate: integers only.
        return new StatBlock(
            MaxHealth: budget * mix.hp * 3 / 100,
            Attack: budget * mix.atk / 100,
            Defense: budget * mix.def / 100,
            Speed: budget * mix.spd / 100,
            CritChance: budget * mix.crit * 2 / 100);
    }
}

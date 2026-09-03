// ============================================================================
//  WEAPONS - the loot table
// ============================================================================
//
//  Forty-seven weapons. Each one declares only its NAME, its ARCHETYPE and its
//  RARITY; the actual stats are computed from those two things by
//  WeaponDefinition, so no weapon can accidentally be weaker than a lower
//  rarity, and rebalancing all the loot in the game is a two-table edit.
//
//  Sorted by rarity, because that is how you will read it when tuning drops.
// ============================================================================

namespace Rpg.Core.Content;

public static class Weapons
{
    public static readonly WeaponDefinition[] All =
    {
        // ---- Common ------------------------------------------------------
        new("rusty_shortsword", "Rusty Shortsword", WeaponKind.Sword, Rarity.Common),
        new("iron_sword", "Iron Sword", WeaponKind.Sword, Rarity.Common),
        new("hand_axe", "Hand Axe", WeaponKind.Axe, Rarity.Common),
        new("oak_club", "Oak Club", WeaponKind.Club, Rarity.Common),
        new("iron_mace", "Iron Mace", WeaponKind.Mace, Rarity.Common),
        new("bandit_dirk", "Bandit's Dirk", WeaponKind.Dagger, Rarity.Common),
        new("short_bow", "Short Bow", WeaponKind.Bow, Rarity.Common),
        new("hunting_spear", "Hunting Spear", WeaponKind.Spear, Rarity.Common),
        new("iron_claws", "Iron Claws", WeaponKind.Claw, Rarity.Common),
        new("iron_greatsword", "Iron Greatsword", WeaponKind.Greatsword, Rarity.Common),
        new("apprentice_staff", "Apprentice Staff", WeaponKind.Staff, Rarity.Common),
        new("oak_wand", "Oak Wand", WeaponKind.Wand, Rarity.Common),
        new("wooden_shield", "Wooden Shield", WeaponKind.Shield, Rarity.Common),

        // ---- Uncommon ----------------------------------------------------
        new("steel_longsword", "Steel Longsword", WeaponKind.Sword, Rarity.Uncommon),
        new("battle_axe", "Battle Axe", WeaponKind.Axe, Rarity.Uncommon),
        new("spiked_club", "Spiked Club", WeaponKind.Club, Rarity.Uncommon),
        new("war_hammer", "War Hammer", WeaponKind.Hammer, Rarity.Uncommon),
        new("poison_fang", "Poison Fang", WeaponKind.Dagger, Rarity.Uncommon),
        new("tempered_katana", "Tempered Katana", WeaponKind.Katana, Rarity.Uncommon),
        new("hand_crossbow", "Hand Crossbow", WeaponKind.Crossbow, Rarity.Uncommon),
        new("grimoire", "Grimoire", WeaponKind.Tome, Rarity.Uncommon),
        new("ember_wand", "Ember Wand", WeaponKind.Wand, Rarity.Uncommon),
        new("everburning_torch", "Everburning Torch", WeaponKind.Torch, Rarity.Uncommon),

        // ---- Rare ---------------------------------------------------------
        new("flamebrand", "Flamebrand", WeaponKind.Sword, Rarity.Rare),
        new("frostbite", "Frostbite", WeaponKind.Sword, Rarity.Rare),
        new("skullsplitter", "Skullsplitter", WeaponKind.Axe, Rarity.Rare),
        new("morningstar", "Morningstar", WeaponKind.Flail, Rarity.Rare),
        new("reaper_scythe", "Reaper's Scythe", WeaponKind.Scythe, Rarity.Rare),
        new("elven_longbow", "Elven Longbow", WeaponKind.Bow, Rarity.Rare),
        new("halberd", "Halberd", WeaponKind.Spear, Rarity.Rare),
        new("arcane_staff", "Arcane Staff", WeaponKind.Staff, Rarity.Rare),
        new("serpent_whip", "Serpent Whip", WeaponKind.Whip, Rarity.Rare),
        new("tower_shield", "Tower Shield", WeaponKind.Shield, Rarity.Rare),

        // ---- Epic ---------------------------------------------------------
        new("shadowstep", "Shadowstep", WeaponKind.Dagger, Rarity.Epic),
        new("bloodfang", "Bloodfang", WeaponKind.Katana, Rarity.Epic),
        new("executioner", "Executioner", WeaponKind.Greataxe, Rarity.Epic),
        new("thunder_maul", "Thunder Maul", WeaponKind.Hammer, Rarity.Epic),
        new("dragon_talons", "Dragon Talons", WeaponKind.Claw, Rarity.Epic),
        new("siege_crossbow", "Siege Crossbow", WeaponKind.Crossbow, Rarity.Epic),
        new("tidecaller", "Tidecaller", WeaponKind.Trident, Rarity.Epic),
        new("codex_of_light", "Codex of Light", WeaponKind.Tome, Rarity.Epic),

        // ---- Legendary ----------------------------------------------------
        new("void_edge", "Void Edge", WeaponKind.Sword, Rarity.Legendary),
        new("dawnbreaker", "Dawnbreaker", WeaponKind.Greatsword, Rarity.Legendary),
        new("worldcleaver", "Worldcleaver", WeaponKind.Greataxe, Rarity.Legendary),
        new("soul_harvester", "Soul Harvester", WeaponKind.Scythe, Rarity.Legendary),
        new("lich_staff", "Staff of the Lich", WeaponKind.Staff, Rarity.Legendary),
        new("void_orb", "Void Orb", WeaponKind.Orb, Rarity.Legendary),
    };

    public static WeaponDefinition Get(string id) =>
        All.FirstOrDefault(w => w.Id == id)
        ?? throw new KeyNotFoundException($"No weapon with id '{id}'.");

    public static IEnumerable<WeaponDefinition> OfRarity(Rarity rarity) =>
        All.Where(w => w.Rarity == rarity);
}

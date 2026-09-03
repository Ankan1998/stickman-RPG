// ============================================================================
//  DUNGEONDEFINITION - one of the three places you go
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A dungeon is a name, a mood, two or three encounters, and a loot table.
//
//  Like everything else in Rpg.Core it is DATA. Adding a fourth dungeon means
//  adding one of these to Dungeons.All - no new code, no new class.
//
//  WOUNDS CARRY BETWEEN ENCOUNTERS, NOT BETWEEN DUNGEONS
//  ----------------------------------------------------
//  Inside a dungeon you fight two or three times on one health bar, so winning
//  the first fight cheaply is what buys you the last one. Coming out alive and
//  reaching the hub restores you completely.
//
//  That single rule is what makes the hub feel like relief rather than a menu,
//  and what makes "should I spend the Cleric's turn healing or attacking?" a
//  real question instead of an obvious one.
// ============================================================================

using Rpg.Core.Content;

namespace Rpg.Core.Progression;

/// <summary>One fight: which monsters, and what the screen calls it.</summary>
public sealed record EncounterDefinition(
    string Id,
    string Name,        // shown as the encounter title
    string Flavour,     // one line of scene-setting
    IReadOnlyList<string> MonsterIds);

/// <summary>
/// How likely each rarity is to drop here. Deeper dungeons roll better loot -
/// this is the entire progression curve for equipment.
/// </summary>
public sealed record LootTable(int Common, int Uncommon, int Rare, int Epic, int Legendary)
{
    public int Total => Common + Uncommon + Rare + Epic + Legendary;

    /// <summary>Picks a rarity from the weights. Roll must be in [0, Total).</summary>
    public Rarity RarityFor(int roll)
    {
        if ((roll -= Common) < 0) return Rarity.Common;
        if ((roll -= Uncommon) < 0) return Rarity.Uncommon;
        if ((roll -= Rare) < 0) return Rarity.Rare;
        if ((roll -= Epic) < 0) return Rarity.Epic;
        return Rarity.Legendary;
    }
}

public sealed record DungeonDefinition(
    string Id,
    string Label,
    string Blurb,
    /// <summary>The status this dungeon is built around. Shown to the player before they enter.</summary>
    string ThreatName,
    string ThreatBlurb,
    /// <summary>Which dungeon tile the background is built from, from the asset pack.</summary>
    string FloorTile,
    string WallTile,
    IReadOnlyList<EncounterDefinition> Encounters,
    LootTable Loot);

public static class Dungeons
{
    public static readonly DungeonDefinition[] All =
    {
        // ==================================================================
        //  1. THE WARRENS - numerous, fast, poisonous
        // ==================================================================
        new("warrens", "The Warrens",
            "Low tunnels that smell of wet fur and something worse.",
            "Poison",
            "They are individually feeble and they will still kill you, because "
            + "there are more of them than there are of you and the poison never stops.",
            "floor_mossy", "wall_mossy",
            new EncounterDefinition[]
            {
                new("warrens_1", "The Entrance",
                    "Three of them are already awake.",
                    new[] { "goblin_grunt", "goblin_archer", "giant_rat" }),

                new("warrens_2", "The Nest",
                    "Something in here is patching them up.",
                    new[] { "goblin_grunt", "goblin_archer", "goblin_shaman", "giant_rat" }),

                new("warrens_3", "The Deep Burrow",
                    "The tunnel opens out. It has been waiting.",
                    new[] { "skeleton", "goblin_archer", "slime", "goblin_shaman" }),
            },
            new LootTable(Common: 55, Uncommon: 30, Rare: 13, Epic: 2, Legendary: 0)),

        // ==================================================================
        //  2. EMBER HALLS - heavy hitters and fire
        // ==================================================================
        new("ember", "The Ember Halls",
            "Worked stone, and it is far too warm.",
            "Burning",
            "Burning does nine a turn. No healing you have outruns that - you "
            + "either kill the caster or you take it on the chin twice.",
            "floor_cracked", "wall_brick",
            new EncounterDefinition[]
            {
                new("ember_1", "The Forge Door",
                    "Two of them are stoking something.",
                    new[] { "imp", "cultist", "bandit" }),

                new("ember_2", "The Kennels",
                    "You hear it before you see it.",
                    new[] { "dire_wolf", "imp", "orc_brute" }),

                new("ember_3", "The Great Hall",
                    "Stone wings unfold from what you took for statues.",
                    new[] { "gargoyle", "harpy", "cultist" }),
            },
            new LootTable(Common: 25, Uncommon: 38, Rare: 27, Epic: 9, Legendary: 1)),

        // ==================================================================
        //  3. THE FROZEN CRYPT - they attack your stats
        // ==================================================================
        new("crypt", "The Frozen Crypt",
            "Ice on the inside of the walls. Nothing here has breathed in a long time.",
            "Chill and Curse",
            "These do not out-damage you - they take your Speed, your Attack and "
            + "your armour, and then the damage is enough. Buffs matter more than "
            + "big hits down here.",
            "floor_stone", "wall_rune",
            new EncounterDefinition[]
            {
                new("crypt_1", "The Antechamber",
                    "The cold gets into your hands first.",
                    new[] { "skeleton_knight", "wraith", "zombie" }),

                new("crypt_2", "The Ossuary",
                    "Something enormous is breathing in the dark.",
                    new[] { "minotaur", "wraith", "gargoyle" }),

                new("crypt_3", "The Throne",
                    "It has been expecting you for rather a long time.",
                    new[] { "lich", "demon_lord", "skeleton_knight" }),
            },
            new LootTable(Common: 8, Uncommon: 25, Rare: 37, Epic: 23, Legendary: 7)),
    };

    public static DungeonDefinition Get(string id) =>
        All.FirstOrDefault(d => d.Id == id)
        ?? throw new KeyNotFoundException($"No dungeon with id '{id}'.");
}

// ============================================================================
//  HEROES - the ten you can recruit
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Ten classes. You take THREE into each dungeon, chosen at the hub, and that
//  choice is the biggest decision in the game - bigger than any single move.
//
//  DESIGNED AS A TRIANGLE, NOT A LADDER
//  ------------------------------------
//  If one hero were simply best, picking a party would be a solved problem and
//  the hub would be a formality. So nobody is complete:
//
//    the walls      Warrior, Templar, Paladin    soak damage, low output
//    the damage     Rogue, Ranger, Berserker     enormous output, made of paper
//    the casters    Mage, Necromancer            burst and control, worst HP
//    the support    Cleric, Monk                 keep the others alive
//
//  A party of three walls survives everything and kills nothing. Three casters
//  delete wave one and die to wave two. The interesting parties are mixed, and
//  which mix is right depends on which dungeon you are about to enter - Frozen
//  Crypt curses your Attack, so bringing two damage dealers stops working there.
//
//  SPRITES AND VOICES
//  ------------------
//  SpriteName matches a folder in the asset pack, so the presentation layer
//  loads `warrior_idle_strip.png` and friends without being told about heroes.
//  VoiceFamily picks which set of hurt/death cries to play.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Content;

/// <summary>A recruitable hero. Data only - the Actor is built from this.</summary>
public sealed record HeroDefinition(
    string Id,
    string Label,
    string Role,
    string Blurb,
    StatBlock Stats,
    string[] SkillIds,
    string SpriteName,
    string VoiceFamily,
    WeaponKind PreferredWeapon);

public static class Heroes
{
    public static readonly HeroDefinition[] All =
    {
        // ---- the walls ----------------------------------------------------
        new("warrior", "Warrior", "Tank",
            "Sword and board. Soaks the front rank and hits hard on a timer.",
            new StatBlock(MaxHealth: 78, Attack: 15, Defense: 10, Speed: 10, CritChance: 8),
            new[] { "slash", "heavy_blow", "guard" },
            "warrior", "human", WeaponKind.Sword),

        new("templar", "Templar", "Heavy",
            "Two-handed greatsword. Slow, enormous, and very hard to move.",
            new StatBlock(MaxHealth: 76, Attack: 17, Defense: 9, Speed: 7, CritChance: 10),
            new[] { "greatcleave", "slash", "shield_wall" },
            "templar", "human", WeaponKind.Greatsword),

        new("paladin", "Paladin", "Tank / Support",
            "Holy warhammer. Heals himself as he smites, and blesses the party.",
            new StatBlock(MaxHealth: 72, Attack: 14, Defense: 11, Speed: 8, CritChance: 8),
            new[] { "smite", "bless", "guard" },
            "paladin", "human", WeaponKind.Hammer),

        // ---- the damage ---------------------------------------------------
        new("rogue", "Rogue", "Assassin",
            "Twin daggers and poison. Deletes one target, then dies to a stiff breeze.",
            new StatBlock(MaxHealth: 46, Attack: 15, Defense: 4, Speed: 17, CritChance: 28),
            new[] { "backstab", "eviscerate", "envenom" },
            "rogue", "human", WeaponKind.Dagger),

        new("ranger", "Ranger", "Marksman",
            "Longbow. Opens almost every round and crits constantly.",
            new StatBlock(MaxHealth: 50, Attack: 14, Defense: 5, Speed: 16, CritChance: 24),
            new[] { "arrow", "aimed_shot", "poison_dart" },
            "ranger", "human", WeaponKind.Bow),

        new("berserker", "Berserker", "Bruiser",
            "Greataxe, no armour. Trades his own safety for damage and drinks it back.",
            new StatBlock(MaxHealth: 68, Attack: 18, Defense: 4, Speed: 12, CritChance: 15),
            new[] { "cleave", "bloodthirst", "rage" },
            "berserker", "human", WeaponKind.Greataxe),

        // ---- the casters --------------------------------------------------
        new("mage", "Mage", "Elementalist",
            "Fire to burn, frost to slow. Folds instantly if anything reaches her.",
            new StatBlock(MaxHealth: 42, Attack: 17, Defense: 3, Speed: 11, CritChance: 12),
            new[] { "firebolt", "frostbolt", "arcane_blast" },
            "mage", "human", WeaponKind.Staff),

        new("necromancer", "Necromancer", "Curser",
            "Drains the living and curses the rest. Never quite dies himself.",
            new StatBlock(MaxHealth: 48, Attack: 14, Defense: 4, Speed: 10, CritChance: 10),
            new[] { "drain_life", "curse", "wither" },
            "necromancer", "human", WeaponKind.Scythe),

        // ---- the support --------------------------------------------------
        new("cleric", "Cleric", "Healer",
            "The reason anyone survives the third dungeon. Mace for emergencies.",
            new StatBlock(MaxHealth: 68, Attack: 10, Defense: 8, Speed: 11, CritChance: 5),
            new[] { "healing_word", "mace_strike", "bless" },
            "cleric", "human", WeaponKind.Mace),

        new("monk", "Monk", "Skirmisher",
            "Iron claws and footwork. Fast, and stops things happening.",
            new StatBlock(MaxHealth: 56, Attack: 14, Defense: 7, Speed: 16, CritChance: 20),
            new[] { "palm_strike", "stunning_palm", "meditate" },
            "monk", "human", WeaponKind.Claw),
    };

    /// <summary>The party you start the campaign with, before you have unlocked a choice.</summary>
    public static readonly string[] StartingParty = { "warrior", "cleric", "ranger" };

    public static HeroDefinition Get(string id) =>
        All.FirstOrDefault(h => h.Id == id)
        ?? throw new KeyNotFoundException($"No hero with id '{id}'.");
}

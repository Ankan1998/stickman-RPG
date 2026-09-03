// ============================================================================
//  MONSTERS - everything that wants you dead
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Twenty-two monsters across three tiers, one tier per dungeon. Like heroes,
//  they are data - there is no "class Goblin" anywhere.
//
//  THE TIERS ARE NOT JUST BIGGER NUMBERS
//  -------------------------------------
//  Each tier fights differently, so each dungeon has to be beaten differently:
//
//    TIER 1  The Warrens     Numerous and fast. Individually feeble, but they
//                            out-action you and stack POISON until it matters.
//
//    TIER 2  Ember Halls     Fewer, much heavier hitters, and BURNING - nine a
//                            turn, which outruns any healing you have.
//
//    TIER 3  Frozen Crypt    They attack your STATS. Chill takes the turn order
//                            away, curse takes your damage, sunder takes your
//                            armour. A party built purely for damage stops
//                            working here.
//
//  VoiceFamily picks the hurt/death cries from the audio pack. Weapon picks the
//  impact sound. Neither is gameplay - but both are why a skeleton and a slime
//  landing the same hit do not sound identical.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Content;

public sealed record MonsterTemplate(
    string Id,
    string Label,
    int Tier,
    StatBlock Stats,
    string[] SkillIds,
    string SpriteName,
    string VoiceFamily,
    WeaponKind Weapon,
    string Blurb);

public static class Monsters
{
    public static readonly MonsterTemplate[] All =
    {
        // ==================================================================
        //  TIER 1 - The Warrens.  Weak, fast, numerous, poisonous.
        // ==================================================================
        new("goblin_grunt", "Goblin", 1,
            new StatBlock(MaxHealth: 58, Attack: 16, Defense: 4, Speed: 13, CritChance: 8),
            new[] { "club" }, "goblin_grunt", "goblin", WeaponKind.Club,
            "Cowardly, numerous, hits in packs."),

        new("goblin_archer", "Goblin Archer", 1,
            new StatBlock(MaxHealth: 47, Attack: 16, Defense: 3, Speed: 15, CritChance: 12),
            new[] { "rusty_bow", "poison_dart" }, "goblin_archer", "goblin", WeaponKind.Bow,
            "Stays at the back and plinks."),

        new("goblin_shaman", "Goblin Shaman", 1,
            new StatBlock(MaxHealth: 45, Attack: 13, Defense: 3, Speed: 11, CritChance: 5),
            new[] { "hex", "mend", "poison_dart" }, "goblin_shaman", "goblin", WeaponKind.Staff,
            "Curses, and patches its friends up. Kill it first."),

        new("kobold", "Kobold", 1,
            new StatBlock(MaxHealth: 41, Attack: 15, Defense: 3, Speed: 16, CritChance: 14),
            new[] { "club", "web_shot" }, "kobold", "goblin", WeaponKind.Dagger,
            "Trap-setter. Fragile but very fast."),

        new("giant_rat", "Giant Rat", 1,
            new StatBlock(MaxHealth: 39, Attack: 14, Defense: 2, Speed: 14, CritChance: 8),
            new[] { "diseased_bite" }, "giant_rat", "beast", WeaponKind.Claw,
            "Diseased bite. Swarms."),

        new("bat", "Cave Bat", 1,
            new StatBlock(MaxHealth: 34, Attack: 13, Defense: 2, Speed: 18, CritChance: 10),
            new[] { "drain" }, "bat", "beast", WeaponKind.Claw,
            "Erratic flyer. Drains a little life with every bite."),

        new("slime", "Slime", 1,
            new StatBlock(MaxHealth: 62, Attack: 14, Defense: 6, Speed: 6, CritChance: 3),
            new[] { "corrode" }, "slime", "slime", WeaponKind.Claw,
            "Slow. Eats through armour."),

        new("skeleton", "Skeleton", 1,
            new StatBlock(MaxHealth: 56, Attack: 17, Defense: 6, Speed: 9, CritChance: 8),
            new[] { "bone_strike" }, "skeleton", "skeleton", WeaponKind.Sword,
            "Rusted blade, dead hands."),

        // ==================================================================
        //  TIER 2 - Ember Halls.  Heavy hitters and fire.
        // ==================================================================
        new("orc_brute", "Orc Brute", 2,
            new StatBlock(MaxHealth: 90, Attack: 20, Defense: 6, Speed: 7, CritChance: 8),
            new[] { "brute_swing", "headbutt" }, "orc_brute", "beast", WeaponKind.Greataxe,
            "Big swing, big opening."),

        new("imp", "Imp", 2,
            new StatBlock(MaxHealth: 44, Attack: 17, Defense: 4, Speed: 14, CritChance: 12),
            new[] { "firebomb" }, "imp", "demon", WeaponKind.Wand,
            "Flings fire from somewhere you cannot reach."),

        new("cultist", "Cultist", 2,
            new StatBlock(MaxHealth: 53, Attack: 17, Defense: 5, Speed: 11, CritChance: 10),
            new[] { "immolate", "sacrifice" }, "cultist", "human", WeaponKind.Torch,
            "Spends its own blood to burn yours."),

        new("bandit", "Bandit", 2,
            new StatBlock(MaxHealth: 62, Attack: 18, Defense: 6, Speed: 13, CritChance: 15),
            new[] { "rend", "brute_swing" }, "bandit", "human", WeaponKind.Sword,
            "Fast, mean, and after your gold."),

        new("dire_wolf", "Dire Wolf", 2,
            new StatBlock(MaxHealth: 60, Attack: 19, Defense: 5, Speed: 17, CritChance: 16),
            new[] { "rend" }, "dire_wolf", "beast", WeaponKind.Claw,
            "Pack hunter. Almost always strikes first."),

        new("giant_spider", "Giant Spider", 2,
            new StatBlock(MaxHealth: 55, Attack: 16, Defense: 5, Speed: 13, CritChance: 10),
            new[] { "web_shot", "venom_sting" }, "giant_spider", "beast", WeaponKind.Claw,
            "Webs a target, then poisons it at leisure."),

        new("scorpion", "Scorpion", 2,
            new StatBlock(MaxHealth: 58, Attack: 17, Defense: 6, Speed: 10, CritChance: 10),
            new[] { "venom_sting" }, "scorpion", "beast", WeaponKind.Spear,
            "Stinger applies stacking poison."),

        new("harpy", "Harpy", 2,
            new StatBlock(MaxHealth: 51, Attack: 16, Defense: 4, Speed: 16, CritChance: 14),
            new[] { "screech", "rend" }, "harpy", "beast", WeaponKind.Claw,
            "Screeches, stuns, and dives."),

        new("gargoyle", "Gargoyle", 2,
            new StatBlock(MaxHealth: 80, Attack: 18, Defense: 8, Speed: 6, CritChance: 6),
            new[] { "stone_fist" }, "gargoyle", "golem", WeaponKind.Hammer,
            "Stone skin. Slow, and extremely hard to dent."),

        new("zombie", "Zombie", 2,
            new StatBlock(MaxHealth: 76, Attack: 16, Defense: 5, Speed: 5, CritChance: 4),
            new[] { "diseased_bite" }, "zombie", "undead", WeaponKind.Claw,
            "Slow, relentless, infectious."),

        // ==================================================================
        //  TIER 3 - The Frozen Crypt.  They attack your STATS.
        // ==================================================================
        new("skeleton_knight", "Skeleton Knight", 3,
            new StatBlock(MaxHealth: 84, Attack: 19, Defense: 8, Speed: 10, CritChance: 12),
            new[] { "bone_strike", "parry" }, "skeleton_knight", "skeleton", WeaponKind.Greatsword,
            "Armoured undead. Parries."),

        new("wraith", "Wraith", 3,
            new StatBlock(MaxHealth: 62, Attack: 20, Defense: 5, Speed: 15, CritChance: 15),
            new[] { "soul_rip", "death_curse" }, "wraith", "undead", WeaponKind.Scythe,
            "Goes straight through armour."),

        new("minotaur", "Minotaur", 3,
            new StatBlock(MaxHealth: 96, Attack: 21, Defense: 7, Speed: 9, CritChance: 12),
            new[] { "charge", "brute_swing" }, "minotaur", "beast", WeaponKind.Greataxe,
            "Charges through the entire front rank."),

        new("lich", "Lich", 3,
            new StatBlock(MaxHealth: 80, Attack: 19, Defense: 6, Speed: 12, CritChance: 14),
            new[] { "frost_nova", "death_curse", "drain" }, "lich", "undead", WeaponKind.Staff,
            "Cold, curses, and centuries of patience."),

        new("demon_lord", "Demon Lord", 3,
            new StatBlock(MaxHealth: 104, Attack: 21, Defense: 7, Speed: 11, CritChance: 16),
            new[] { "hellfire", "brute_swing", "sacrifice" }, "demon_lord", "demon", WeaponKind.Greatsword,
            "Summons fire and burns the ground it stands on."),
    };

    public static MonsterTemplate Get(string id) =>
        All.FirstOrDefault(m => m.Id == id)
        ?? throw new KeyNotFoundException($"No monster with id '{id}'.");

    public static IEnumerable<MonsterTemplate> OfTier(int tier) => All.Where(m => m.Tier == tier);
}

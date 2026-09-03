// ============================================================================
//  SKILLS - every ability in the game
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Every skill any hero or monster can use, in one place. All of it is DATA -
//  there is exactly one piece of code (SkillAction) that executes all of them.
//
//  Adding a skill costs a handful of numbers. It needs no new class, no switch
//  case, no test file. That ratio is what makes a content-heavy game finishable.
//
//  READING A SKILL
//  ---------------
//    Power              damage as a PERCENTAGE of the user's Attack. 100 = normal.
//    Healing            flat health restored to the TARGET.
//    LifestealPercent   percentage of damage dealt returned to the USER.
//    AppliesStatus      what it inflicts, and StatusTurns for how long.
//    Cooldown           turns before it can be used again.
//
//  The Fx name at the end of each entry is not gameplay - it tells the
//  presentation layer which of the 16 effect animations to play.
// ============================================================================

namespace Rpg.Core.Content;

public static class Skills
{
    // ======================================================================
    //  HERO SKILLS
    // ======================================================================

    // ---- Warrior: soak damage, hit hard on a timer ------------------------
    public static readonly SkillDefinition Slash = new(
        "slash", "Slash", "A reliable swing.",
        TargetKind.SingleEnemy, Power: 100,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition HeavyBlow = new(
        "heavy_blow", "Heavy Blow", "Slow, but it hurts.",
        TargetKind.SingleEnemy, Power: 180, Cooldown: 2,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Guard = new(
        "guard", "Guard", "Brace. Greatly increased defence for two turns.",
        TargetKind.Self, AppliesStatus: Statuses.Guard, StatusTurns: 2,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- Paladin: a tank that pays you back -------------------------------
    public static readonly SkillDefinition Smite = new(
        "smite", "Smite", "Holy strike. Heals you for a third of the damage.",
        TargetKind.SingleEnemy, Power: 120, LifestealPercent: 35,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Bless = new(
        "bless", "Bless", "An ally hits harder and truer for three turns.",
        TargetKind.SingleAlly, AppliesStatus: Statuses.Blessed, StatusTurns: 3, Cooldown: 3,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- Ranger / Archer: opens the round, crits constantly ---------------
    public static readonly SkillDefinition Arrow = new(
        "arrow", "Arrow", "Quick and dependable.",
        TargetKind.SingleEnemy, Power: 110,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition AimedShot = new(
        "aimed_shot", "Aimed Shot", "Takes a moment. Worth it.",
        TargetKind.SingleEnemy, Power: 200, Cooldown: 3,
        LaunchPattern: "--##", TargetPattern: "--##");

    // ---- Rogue: burst and poison ------------------------------------------
    public static readonly SkillDefinition Backstab = new(
        "backstab", "Backstab", "Fast, mean, and usually a crit.",
        TargetKind.SingleEnemy, Power: 130,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Envenom = new(
        "envenom", "Envenom", "A thrown vial. Works from anywhere in the line.",
        TargetKind.SingleEnemy, Power: 50, AppliesStatus: Statuses.Poison, StatusTurns: 3,
        LaunchPattern: "####", TargetPattern: "###-");

    public static readonly SkillDefinition Eviscerate = new(
        "eviscerate", "Eviscerate", "Opens them up. They bleed out.",
        TargetKind.SingleEnemy, Power: 150, AppliesStatus: Statuses.Bleed, StatusTurns: 2, Cooldown: 3,
        LaunchPattern: "##--", TargetPattern: "###-");

    // ---- Mage: elemental burst --------------------------------------------
    public static readonly SkillDefinition Firebolt = new(
        "firebolt", "Firebolt", "Sets them alight. Burning hurts far more than poison.",
        TargetKind.SingleEnemy, Power: 90, AppliesStatus: Statuses.Burning, StatusTurns: 2,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Frostbolt = new(
        "frostbolt", "Frostbolt", "Slows them badly - they act much later.",
        TargetKind.SingleEnemy, Power: 85, AppliesStatus: Statuses.Chilled, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition ArcaneBlast = new(
        "arcane_blast", "Arcane Blast", "Raw force. Nothing clever about it.",
        TargetKind.SingleEnemy, Power: 195, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- Cleric: the reason anyone survives dungeon three ------------------
    public static readonly SkillDefinition MaceStrike = new(
        "mace_strike", "Mace", "For when nothing needs healing.",
        TargetKind.SingleEnemy, Power: 95,
        LaunchPattern: "###-", TargetPattern: "##--");

    public static readonly SkillDefinition HealingWord = new(
        "healing_word", "Healing Word", "Restores 26 health to an ally.",
        TargetKind.SingleAlly, Healing: 26, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition Bandage = new(
        "bandage", "Bandage", "Patch up an ally for 14.",
        TargetKind.SingleAlly, Healing: 14, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- Berserker: damage, and a price -----------------------------------
    public static readonly SkillDefinition Cleave = new(
        "cleave", "Cleave", "An enormous swing.",
        TargetKind.SingleEnemy, Power: 145, Cooldown: 1,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Rage = new(
        "rage", "Rage", "Far more damage, far less defence, for three turns.",
        TargetKind.Self, AppliesStatus: Statuses.Enraged, StatusTurns: 3, Cooldown: 4,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition Bloodthirst = new(
        "bloodthirst", "Bloodthirst", "Tears into them and drinks half of it back.",
        TargetKind.SingleEnemy, Power: 125, LifestealPercent: 50, Cooldown: 2,
        LaunchPattern: "##--", TargetPattern: "##--");

    // ---- Monk: control and evasion ----------------------------------------
    public static readonly SkillDefinition PalmStrike = new(
        "palm_strike", "Palm Strike", "Simple, fast, precise.",
        TargetKind.SingleEnemy, Power: 115,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition StunningPalm = new(
        "stunning_palm", "Stunning Palm", "They lose their next turn entirely.",
        TargetKind.SingleEnemy, Power: 70, AppliesStatus: Statuses.Stun, StatusTurns: 1, Cooldown: 3,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Meditate = new(
        "meditate", "Meditate", "Faster and deadlier for three turns.",
        TargetKind.Self, AppliesStatus: Statuses.Focused, StatusTurns: 3, Cooldown: 3,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- Necromancer: drain and curse -------------------------------------
    public static readonly SkillDefinition DrainLife = new(
        "drain_life", "Drain Life", "Takes their health and keeps most of it.",
        TargetKind.SingleEnemy, Power: 100, LifestealPercent: 60,
        LaunchPattern: "-###", TargetPattern: "###-");

    public static readonly SkillDefinition Curse = new(
        "curse", "Curse", "Weaker and more fragile for three turns.",
        TargetKind.SingleEnemy, Power: 40, AppliesStatus: Statuses.Cursed, StatusTurns: 3,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition Wither = new(
        "wither", "Wither", "Saps the strength out of them.",
        TargetKind.SingleEnemy, Power: 60, AppliesStatus: Statuses.Weakened, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    // ---- Templar: the biggest single hits in the game ----------------------
    public static readonly SkillDefinition Greatcleave = new(
        "greatcleave", "Greatcleave", "A two-handed overhead. Enormous.",
        TargetKind.SingleEnemy, Power: 210, Cooldown: 3,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition ShieldWall = new(
        "shield_wall", "Shield Wall", "Nothing gets through for two turns.",
        TargetKind.Self, AppliesStatus: Statuses.Guard, StatusTurns: 2, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    // ---- shared ------------------------------------------------------------
    public static readonly SkillDefinition Jab = new(
        "jab", "Jab", "A quick poke.",
        TargetKind.SingleEnemy, Power: 80,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition PoisonDart = new(
        "poison_dart", "Poison Dart", "Weak hit, lingering damage.",
        TargetKind.SingleEnemy, Power: 40, AppliesStatus: Statuses.Poison, StatusTurns: 3,
        LaunchPattern: "####", TargetPattern: "####");

    // ======================================================================
    //  MONSTER SKILLS
    // ======================================================================

    // -- tier 1: The Warrens
    public static readonly SkillDefinition Club = new(
        "club", "Club", "A crude bash.",
        TargetKind.SingleEnemy, Power: 100,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition RustyBow = new(
        "rusty_bow", "Rusty Bow", "Plinks from the back.",
        TargetKind.SingleEnemy, Power: 105,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Hex = new(
        "hex", "Hex", "Saps the target's strength.",
        TargetKind.SingleEnemy, Power: 30, AppliesStatus: Statuses.Weakened, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Mend = new(
        "mend", "Mend", "Knits an ally back together.",
        TargetKind.SingleAlly, Healing: 14, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition DiseasedBite = new(
        "diseased_bite", "Diseased Bite", "Filthy teeth.",
        TargetKind.SingleEnemy, Power: 75, AppliesStatus: Statuses.Poison, StatusTurns: 2,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Drain = new(
        "drain", "Drain", "Feeds on them.",
        TargetKind.SingleEnemy, Power: 70, LifestealPercent: 60,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Corrode = new(
        "corrode", "Corrode", "Eats through armour.",
        TargetKind.SingleEnemy, Power: 60, AppliesStatus: Statuses.Sundered, StatusTurns: 3,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition BoneStrike = new(
        "bone_strike", "Bone Strike", "A rusted blade in dead hands.",
        TargetKind.SingleEnemy, Power: 105,
        LaunchPattern: "##--", TargetPattern: "##--");

    // -- tier 2: Ember Halls
    public static readonly SkillDefinition BruteSwing = new(
        "brute_swing", "Brute Swing", "All shoulder, no technique.",
        TargetKind.SingleEnemy, Power: 125,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Headbutt = new(
        "headbutt", "Headbutt", "Weak, but it rattles them.",
        TargetKind.SingleEnemy, Power: 70, AppliesStatus: Statuses.Stun, StatusTurns: 1, Cooldown: 3,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Firebomb = new(
        "firebomb", "Firebomb", "Sets them alight from across the room.",
        TargetKind.SingleEnemy, Power: 80, AppliesStatus: Statuses.Burning, StatusTurns: 2,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Immolate = new(
        "immolate", "Immolate", "Wreathes them in flame.",
        TargetKind.SingleEnemy, Power: 100, AppliesStatus: Statuses.Burning, StatusTurns: 2, Cooldown: 2,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Rend = new(
        "rend", "Rend", "Claws that open you up.",
        TargetKind.SingleEnemy, Power: 95, AppliesStatus: Statuses.Bleed, StatusTurns: 2,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition WebShot = new(
        "web_shot", "Web", "Slowed and clumsy.",
        TargetKind.SingleEnemy, Power: 35, AppliesStatus: Statuses.Webbed, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition VenomSting = new(
        "venom_sting", "Venom Sting", "Deep, and it lingers.",
        TargetKind.SingleEnemy, Power: 85, AppliesStatus: Statuses.Poison, StatusTurns: 3,
        LaunchPattern: "##--", TargetPattern: "###-");

    public static readonly SkillDefinition Screech = new(
        "screech", "Screech", "A noise that stops you dead.",
        TargetKind.SingleEnemy, Power: 55, AppliesStatus: Statuses.Stun, StatusTurns: 1, Cooldown: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition StoneFist = new(
        "stone_fist", "Stone Fist", "Like being hit with a building.",
        TargetKind.SingleEnemy, Power: 130, Cooldown: 1,
        LaunchPattern: "##--", TargetPattern: "##--");

    public static readonly SkillDefinition Sacrifice = new(
        "sacrifice", "Dark Pact", "Spends its own blood for power.",
        TargetKind.Self, AppliesStatus: Statuses.Enraged, StatusTurns: 3, Cooldown: 4,
        LaunchPattern: "####", TargetPattern: "####");

    // -- tier 3: The Frozen Crypt
    public static readonly SkillDefinition Rally = new(
        "rally", "Rally", "Roars. Everything nearby hits harder.",
        TargetKind.SingleAlly, AppliesStatus: Statuses.Rallied, StatusTurns: 3, Cooldown: 3,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition Parry = new(
        "parry", "Parry", "Turns the next blow aside.",
        TargetKind.Self, AppliesStatus: Statuses.Guard, StatusTurns: 2, Cooldown: 2,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition SoulRip = new(
        "soul_rip", "Soul Rip", "Goes straight through armour.",
        TargetKind.SingleEnemy, Power: 130, AppliesStatus: Statuses.Sundered, StatusTurns: 3,
        LaunchPattern: "####", TargetPattern: "####");

    public static readonly SkillDefinition Charge = new(
        "charge", "Charge", "Runs them down.",
        TargetKind.SingleEnemy, Power: 190, Cooldown: 2,
        LaunchPattern: "##--", TargetPattern: "###-");

    public static readonly SkillDefinition FrostNova = new(
        "frost_nova", "Frost Nova", "The cold gets into everything.",
        TargetKind.SingleEnemy, Power: 95, AppliesStatus: Statuses.Chilled, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition DeathCurse = new(
        "death_curse", "Death Curse", "Weaker, softer, and running out of time.",
        TargetKind.SingleEnemy, Power: 85, AppliesStatus: Statuses.Cursed, StatusTurns: 3,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition Hellfire = new(
        "hellfire", "Hellfire", "The floor itself catches.",
        TargetKind.SingleEnemy, Power: 150, AppliesStatus: Statuses.Burning, StatusTurns: 2, Cooldown: 2,
        LaunchPattern: "-###", TargetPattern: "####");

    public static readonly SkillDefinition EyeRay = new(
        "eye_ray", "Eye Ray", "One of many, and never the same twice.",
        TargetKind.SingleEnemy, Power: 110, AppliesStatus: Statuses.Cursed, StatusTurns: 2,
        LaunchPattern: "####", TargetPattern: "####");

    /// <summary>Every skill in the game, for lookup by id.</summary>
    public static readonly SkillDefinition[] All =
    {
        // heroes
        Slash, HeavyBlow, Guard, Smite, Bless, Arrow, AimedShot,
        Backstab, Envenom, Eviscerate, Firebolt, Frostbolt, ArcaneBlast,
        MaceStrike, HealingWord, Bandage, Cleave, Rage, Bloodthirst,
        PalmStrike, StunningPalm, Meditate, DrainLife, Curse, Wither,
        Greatcleave, ShieldWall, Jab, PoisonDart,
        // monsters
        Club, RustyBow, Hex, Mend, DiseasedBite, Drain, Corrode, BoneStrike,
        BruteSwing, Headbutt, Firebomb, Immolate, Rend, WebShot, VenomSting,
        Screech, StoneFist, Sacrifice,
        Rally, Parry, SoulRip, Charge, FrostNova, DeathCurse, Hellfire, EyeRay,
    };
}

// ============================================================================
//  SKILLDEFINITION - what a skill IS
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Slash, Fireball, Bandage and Guard are all THIS SAME TYPE with different
//  numbers. There is exactly one piece of code that knows how to execute any of
//  them (SkillAction.cs).
//
//  This is the most consequential decision in the whole engine. Adding a skill
//  costs you five numbers - not a new class, plus a new switch case, plus a new
//  test file. In a genre where the CONTENT is the game, that ratio is what
//  decides whether you finish.
//
//  A FEW EXAMPLES FROM THE REAL GAME
//  ---------------------------------
//      Slash        Power 100                          plain attack
//      Heavy Blow   Power 180, Cooldown 2              big hit, sometimes
//      Poison Dart  Power 40, AppliesStatus: poison    weak hit, lingering damage
//      Bandage      Healing 14, Cooldown 2             heal an ally
//      Guard        Self, AppliesStatus: guard         buff yourself
//
//  Notice that a skill can do SEVERAL things at once - damage and healing and a
//  status. Life drain would set both Power and Healing.
//
//  ROADMAP
//  -------
//  Right now these are written in C# (see ContentDatabase.CreateDefault). That
//  is deliberate while you are learning: it compiles, it is type-checked, and
//  you can Ctrl-click to jump to a definition.
//
//  It stops scaling around fifty skills, because at that point you should not
//  have to recompile to change a damage number. Moving them to JSON is a later
//  step in the roadmap, and it touches exactly one class - every other file
//  already treats content as data.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Effects;

namespace Rpg.Core.Content;

/// <summary>Who a skill is allowed to be pointed at.</summary>
public enum TargetKind
{
    /// <summary>One living actor on the opposing team.</summary>
    SingleEnemy,

    /// <summary>One living actor on your own team, including yourself.</summary>
    SingleAlly,

    /// <summary>The user, and only the user.</summary>
    Self,
}

public sealed record SkillDefinition(
    string Id,            // unique key, e.g. "heavy_blow". Referenced by events.
    string Name,          // shown on the button, e.g. "Heavy Blow"
    string Description,   // for a tooltip, once you build one
    TargetKind Target,

    // Damage scaling, as a PERCENTAGE of the user's Attack.
    //   100 = a normal hit
    //   180 = hits for 1.8x your Attack
    //     0 = deals no damage at all (Bandage, Guard)
    //
    // Percentages rather than flat numbers keep a skill relevant as the
    // character levels up.
    int Power = 0,

    // Flat healing applied to the target. Not a percentage. 0 = heals nothing.
    int Healing = 0,

    // A status applied to the target on use, or null for none.
    // C# note: "StatusDefinition?" means this one is allowed to be null.
    StatusDefinition? AppliesStatus = null,

    // How many turns that status lasts.
    int StatusTurns = 0,

    // Turns before this skill can be used again. 0 = usable every turn.
    int Cooldown = 0,

    // Percentage of the damage dealt that is returned to the USER as health.
    // 0 = none. This is the whole of "life drain" and "smite heals you" - one
    // number, rather than a special case per skill.
    int LifestealPercent = 0,

    // WHERE YOU MUST STAND to use this, front-first. "##--" means the front two
    // positions only - a greatsword is no use from the back line.
    string LaunchPattern = "####",

    // WHICH POSITIONS IT REACHES, front-first. "--##" means it hits only the
    // enemy's back two - a sniper shot that cannot touch what is in front of it.
    string TargetPattern = "####")
{
    // Small readable helpers, so SkillAction can say "if (Skill.DealsDamage)"
    // rather than "if (Skill.Power > 0)".
    public bool DealsDamage => Power > 0;
    public bool Heals => Healing > 0;
    public bool Drains => LifestealPercent > 0;

    /// <summary>Positions this can be used FROM.</summary>
    public Ranks LaunchRanks => Ranks.Parse(LaunchPattern);

    /// <summary>Positions this can REACH.</summary>
    public Ranks TargetRanks => Ranks.Parse(TargetPattern);

    /// <summary>
    /// True if this skill cares about position at all. Self-buffs and party
    /// heals generally do not, and the UI should not clutter them with
    /// diagrams that say "anywhere".
    /// </summary>
    public bool IsPositional =>
        LaunchRanks.Mask != Ranks.Any.Mask || TargetRanks.Mask != Ranks.Any.Mask;
}

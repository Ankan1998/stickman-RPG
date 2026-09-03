// ============================================================================
//  STATBLOCK - the five numbers that describe how good someone is at fighting
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A character sheet. Every actor has one for their base stats, and every buff
//  or debuff is *also* one, holding the difference it makes.
//
//  Because both are the same type, applying a buff is literally addition:
//
//      CurrentStats = BaseStats + guardBuff + poisonDebuff
//
//  That is the whole trick, and it is why the + operator at the bottom of this
//  file earns its keep.
//
//  C# NOTES
//  --------
//  "readonly record struct" is three separate decisions:
//
//    record   - the compiler writes the constructor, the five properties,
//               equality, and ToString() for us. About 60 lines saved.
//
//    struct   - it is a VALUE type. Assigning it copies it, exactly like an int.
//               So handing a StatBlock to another method can never let that
//               method modify yours.
//
//    readonly - it can never be changed after it is created. Combined with
//               struct, that means a stray calculation cannot corrupt an
//               actor's BaseStats.
//
//  ADDING A NEW STAT (Accuracy, Evasion, Resistance...) costs you exactly two
//  lines: one parameter here, one line in the + operator. Then the compiler
//  walks you through every place that needs updating. See docs/07-recipes.md.
// ============================================================================

namespace Rpg.Core.Entities;

public readonly record struct StatBlock(
    int MaxHealth,    // how much damage you survive
    int Attack,       // how hard you hit
    int Defense,      // reduces incoming damage by half its value
    int Speed,        // decides turn order, highest first
    int CritChance)   // percent chance to double your damage
{
    /// <summary>An empty stat block. Used by statuses that change no stats, like poison.</summary>
    public static readonly StatBlock Zero = new(0, 0, 0, 0, 0);

    /// <summary>
    /// Adds two stat blocks together. This is what lets base stats, equipment
    /// and buffs stack in one readable expression.
    /// </summary>
    // C# note: this is "operator overloading" - defining what "+" means for our
    // own type. Use it sparingly; it is a good fit here because the operation
    // genuinely is arithmetic.
    public static StatBlock operator +(StatBlock a, StatBlock b) => new(
        a.MaxHealth + b.MaxHealth,
        a.Attack + b.Attack,
        a.Defense + b.Defense,
        a.Speed + b.Speed,
        a.CritChance + b.CritChance);

    /// <summary>
    /// Keeps the numbers sane after debuffs have been applied. Stats can be
    /// reduced to zero but never below it; MaxHealth floors at 1 (a maximum
    /// health of zero would mean permanently dead); CritChance stays a valid
    /// percentage.
    /// </summary>
    public StatBlock Clamped() => new(
        Math.Max(1, MaxHealth),
        Math.Max(0, Attack),
        Math.Max(0, Defense),
        Math.Max(0, Speed),
        Math.Clamp(CritChance, 0, 100));
}

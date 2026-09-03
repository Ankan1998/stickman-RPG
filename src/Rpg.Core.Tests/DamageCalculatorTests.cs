// ============================================================================
//  DAMAGECALCULATORTESTS - checking the damage formula on its own
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The damage formula is the single most important number generator in the game,
//  so it gets tested in complete isolation: no actors, no battle, no engine.
//  Just numbers in, numbers out.
//
//  These four tests are the fastest feedback loop in the project. Change the
//  formula and they tell you INSTANTLY and PRECISELY what you changed.
//
//  HOW TO READ AN xUNIT TEST
//  -------------------------
//    [Fact]                    marks the method as a test
//    Assert.Equal(a, b)        fails the test unless a == b (EXPECTED first)
//
//  If you change DamageCalculator, expect these to fail. That is not a problem -
//  it is the tests doing their job. Update the expected numbers to match your
//  new formula, deliberately, one at a time.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Entities;
using Xunit;

namespace Rpg.Core.Tests;

/// <summary>
/// The damage formula is the single most important number generator in the game,
/// so it gets tested in isolation - no actors, no battle, no engine. Change the
/// formula and these tests tell you instantly what you changed.
/// </summary>
public sealed class DamageCalculatorTests
{
    private static StatBlock Attacker(int attack) => new(100, attack, 0, 10, 0);
    private static StatBlock Defender(int defense) => new(100, 0, defense, 10, 0);

    [Fact]
    public void PowerScalesDamageAsAPercentageOfAttack()
    {
        // 20 Attack at 100% power, against no defence.
        Assert.Equal(20, DamageCalculator.Compute(Attacker(20), Defender(0), power: 100, isCritical: false));

        // Same attacker, a 180% power skill.
        Assert.Equal(36, DamageCalculator.Compute(Attacker(20), Defender(0), power: 180, isCritical: false));
    }

    [Fact]
    public void DefenceSubtractsHalfItsValue()
    {
        // raw 20, minus (10 / 2) = 15
        Assert.Equal(15, DamageCalculator.Compute(Attacker(20), Defender(10), power: 100, isCritical: false));
    }

    [Fact]
    public void CriticalHitsDoubleTheDamageAfterMitigation()
    {
        int normal = DamageCalculator.Compute(Attacker(20), Defender(10), power: 100, isCritical: false);
        int critical = DamageCalculator.Compute(Attacker(20), Defender(10), power: 100, isCritical: true);

        Assert.Equal(15, normal);
        Assert.Equal(30, critical);
    }

    [Fact]
    public void DamageNeverDropsBelowOne()
    {
        // A pebble against a fortress still chips it. Without this floor, a
        // high-defence actor becomes literally unkillable and battles hang.
        int damage = DamageCalculator.Compute(Attacker(1), Defender(500), power: 100, isCritical: false);

        Assert.Equal(DamageCalculator.MinimumDamage, damage);
    }
}

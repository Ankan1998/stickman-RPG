// ============================================================================
//  BALANCEHARNESSTESTS - the payoff file
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  These tests play a THOUSAND complete battles, with the AI controlling both
//  sides, and measure the result. They finish in about a tenth of a second,
//  because no pixel is ever drawn and no frame is ever waited for.
//
//  THIS IS HOW YOU BALANCE AN RPG AS ONE PERSON. You measure it. You do not
//  guess at it, and you certainly do not click through the UI a thousand times.
//
//  See the numbers yourself:
//
//      dotnet test --logger "console;verbosity=detailed"
//
//  A REAL EXAMPLE FROM THIS PROJECT
//  --------------------------------
//  The first version of the demo fight measured 100% hero wins in 3.8 rounds -
//  a fight the player never has to think about. The harness also revealed WHY:
//  the goblin's AI preferred a Poison Dart dealing 2 damage over a Club dealing
//  10, because DamageOverTimeWeight valued delayed damage almost at face value.
//
//  Three measure-tune-remeasure passes later it sits at 74.9% over 6.7 rounds.
//  Fixing that ONE AI weight moved the number eight times further than a whole
//  round of stat inflation did - and you would never have found it by playing.
//
//  The full story is in docs/04-architecture.md#measuring-instead-of-guessing.
//
//  C# NOTE
//  -------
//  ITestOutputHelper is xUnit's way of printing from inside a test. Plain
//  Console.WriteLine goes nowhere useful in a test runner.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Entities;
using Rpg.Core.Rng;
using Xunit;
using Xunit.Abstractions;

namespace Rpg.Core.Tests;

/// <summary>
/// The payoff file.
///
/// These tests play a THOUSAND complete battles, with AI on both sides, and
/// measure the result. They finish in about a second, because no pixel is ever
/// drawn. This is how you balance an RPG as a solo developer: you measure it,
/// you do not guess at it and you certainly do not click through the UI a
/// thousand times.
///
/// Run with detailed output to see the numbers:
///     dotnet test --logger "console;verbosity=detailed"
/// </summary>
public sealed class BalanceHarnessTests
{
    private const int Battles = 1000;

    private readonly ITestOutputHelper _output;

    public BalanceHarnessTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void TheTutorialFightIsHeroFavouredAndAlwaysTerminates()
    {
        int heroWins = 0;
        int monsterWins = 0;
        int draws = 0;
        long totalRounds = 0;

        for (ulong seed = 1; seed <= Battles; seed++)
        {
            BattleRunner.Result result = BattleRunner.Run(seed);

            totalRounds += result.Rounds;

            if (result.Winner == Team.Heroes) heroWins++;
            else if (result.Winner == Team.Monsters) monsterWins++;
            else draws++;
        }

        double winRate = heroWins / (double)Battles;

        _output.WriteLine($"Battles simulated : {Battles}");
        _output.WriteLine($"Hero wins         : {heroWins} ({winRate:P1})");
        _output.WriteLine($"Monster wins      : {monsterWins}");
        _output.WriteLine($"Draws (hit round limit) : {draws}");
        _output.WriteLine($"Average length    : {totalRounds / (double)Battles:F1} rounds");

        // A draw means the fight hit Battle.MaxRounds - a stalemate. In a real
        // game that is a bug, not a result.
        Assert.Equal(0, draws);

        // THIS IS THE ASSERTION YOU TUNE, and it is the most useful line in the
        // test suite. It encodes a DESIGN INTENT - "the opening fight should be
        // won most of the time, but losable if you play badly" - and it fails the
        // moment a balance change quietly breaks that promise.
        //
        // The band was picked by running the harness, reading the number, and
        // adjusting the stats in ContentDatabase until the fight felt right. That
        // loop - measure, tune, re-measure - is what balancing an RPG actually is.
        // A 100% win rate is not a hard fight made easy; it is a fight the player
        // never has to engage with.
        Assert.InRange(winRate, 0.60, 0.85);
    }

    [Fact]
    public void EveryBattleTerminatesWellInsideTheRoundLimit()
    {
        for (ulong seed = 1; seed <= 200; seed++)
        {
            BattleRunner.Result result = BattleRunner.Run(seed);
            Assert.True(result.Rounds < Battle.MaxRounds,
                $"Seed {seed} ran to the {Battle.MaxRounds} round limit - the fight can stalemate.");
        }
    }

    [Fact]
    public void TheSameSeedAlwaysReplaysTheIdenticalBattle()
    {
        // GameEvents are records, so this compares the full event stream by value.
        // If this ever fails, something in combat is reading ambient state -
        // wall-clock time, a static Random, or dictionary iteration order.
        List<GameEvent> first = BattleRunner.Run(4242).Log;
        List<GameEvent> second = BattleRunner.Run(4242).Log;

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeedsProduceDifferentBattles()
    {
        // The mirror of the test above: if randomness were not actually reaching
        // combat, every seed would replay the same fight and the test above
        // would pass for the wrong reason.
        var distinct = new HashSet<string>();

        for (ulong seed = 1; seed <= 40; seed++)
        {
            IEnumerable<string> events = BattleRunner.Run(seed).Log.Select(e => e.ToString()!);
            distinct.Add(string.Join("|", events));
        }

        Assert.True(distinct.Count > 1,
            "Every seed produced an identical battle - is the RNG actually wired into combat?");
    }

    [Fact]
    public void TheRandomSourceIsReproducibleFromItsSeed()
    {
        var a = new SplitMix64Random(99);
        var b = new SplitMix64Random(99);

        for (int i = 0; i < 100; i++)
            Assert.Equal(a.NextInt(0, 1000), b.NextInt(0, 1000));
    }
}

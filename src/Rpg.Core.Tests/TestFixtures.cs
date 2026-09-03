// ============================================================================
//  TESTFIXTURES - shared helpers for all the tests
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Small builders so each test reads as ONE IDEA rather than ten lines of setup.
//
//  Note that none of this needs Godot, a window, or a game loop. That is the
//  whole architecture paying off: the rules of the game are ordinary C# that you
//  can call from anywhere.
//
//  IF YOU HAVE NOT WRITTEN C# TESTS BEFORE
//  ---------------------------------------
//  The library is xUnit. A test is just a method marked [Fact]. You run them all
//  with:
//
//      dotnet test
//
//  ...or one at a time:
//
//      dotnet test --filter "FullyQualifiedName~PoisonDamages"
//
//  There is a worked example of writing your own in docs/07-recipes.md.
// ============================================================================

using Rpg.Core.Ai;
using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Effects;
using Rpg.Core.Entities;
using Rpg.Core.Progression;
using Rpg.Core.Rng;

namespace Rpg.Core.Tests;

// C# note: "internal" means "visible only inside this project". Test helpers
// have no business being part of a public API.
internal static class TestFixtures
{
    /// <summary>A plain 100-power attack. The default weapon for test actors.</summary>
    public static readonly SkillDefinition Punch =
        new("punch", "Punch", "Plain test attack.", TargetKind.SingleEnemy, Power: 100);

    /// <summary>Test poison: 4 damage per turn.</summary>
    public static readonly StatusDefinition Poison =
        new("poison", "Poison", "Test poison.", StatBlock.Zero, DamagePerTurn: 4);

    /// <summary>Test stun: lose your turn.</summary>
    public static readonly StatusDefinition Stun =
        new("stun", "Stunned", "Test stun.", StatBlock.Zero, PreventsAction: true);

    /// <summary>
    /// A stat block with sensible defaults, so a test only names the numbers it
    /// actually cares about: Stats(hp: 100, spd: 20).
    /// </summary>
    // C# note: these are OPTIONAL PARAMETERS. Callers can skip any of them, and
    // can pass them by name in any order.
    public static StatBlock Stats(int hp = 100, int atk = 10, int def = 0, int spd = 10, int crit = 0) =>
        new(hp, atk, def, spd, crit);

    /// <summary>Builds an actor, defaulting to knowing only Punch.</summary>
    // C# note: "params" means any number of trailing skill arguments get
    // collected into an array. Like *args in Python.
    public static Actor MakeActor(string id, Team team, StatBlock stats, params SkillDefinition[] skills) =>
        new(id, id, team, stats, skills.Length > 0 ? skills : new[] { Punch });

    /// <summary>
    /// A one-on-one fight with NON-RANDOM dice.
    ///
    /// Because Stats() defaults CritChance to 0, the Chance() helper
    /// short-circuits and never touches the random source at all - so every
    /// damage number in a test using this is exact, and the test can never fail
    /// intermittently. That reliability is exactly why IRandomSource is an
    /// interface.
    /// </summary>
    public static Battle Duel(Actor hero, Actor monster) =>
        new(new BattleState(new[] { hero, monster }, new FixedRandom(0)));
}

// ============================================================================
//  BATTLERUNNER - plays a whole battle, start to finish, with nobody watching
// ============================================================================
//
//  This tiny helper is the reason a "deep" RPG is tractable for one person: you
//  can run a THOUSAND complete fights in a unit test, in about a tenth of a
//  second, and actually MEASURE whether an encounter is fair.
//
//  Studios call this a balance harness. It exists only because Rpg.Core has no
//  dependency on the game engine - there is no window to open, no frame to wait
//  for, nothing to draw.
// ============================================================================
internal static class BattleRunner
{
    /// <summary>What came out of one simulated battle.</summary>
    internal sealed record Result(Team? Winner, List<GameEvent> Log, int Rounds);

    public static Result Run(ulong seed)
    {
        ContentDatabase content = ContentDatabase.CreateDefault();
        var battle = new Battle(content.CreateDemoBattle(seed));

        var log = new List<GameEvent>(battle.Start());

        // The entire game loop, with the AI playing BOTH sides. Compare this to
        // BattleScene.ContinueBattle() - same shape, minus the drawing and the
        // waiting.
        while (!battle.IsOver)
        {
            Actor current = battle.Current!;
            IAction action = ScoringAi.ChooseAction(battle, current);
            log.AddRange(battle.TakeTurn(action));
        }

        return new Result(battle.Winner, log, battle.Round);
    }
}

// ============================================================================
//  CAMPAIGNRUNNER - plays a COMPLETE campaign with nobody watching
// ============================================================================
//
//  The same idea as BattleRunner above, two levels up. Three dungeons, three
//  encounters each, wounds carrying between them, loot rolling as it goes - all
//  of it in a few milliseconds, because none of it needs an engine.
//
//  This is what makes a game this size balanceable by one person.
// ============================================================================
internal static class CampaignRunner
{
    internal sealed record Result(
        CampaignPhase Outcome,
        int DungeonsCleared,
        int EncountersCleared,
        RunStats Stats,
        string Grade,
        IReadOnlyList<LootDrop> Loot);

    /// <summary>Plays a whole campaign with the AI on both sides.</summary>
    public static Result Play(ulong seed, params string[] party)
    {
        ContentDatabase content = ContentDatabase.CreateDefault();
        var campaign = new Campaign(content, seed);

        if (party.Length > 0)
            campaign.SetParty(party);

        while (campaign.Phase is CampaignPhase.Hub or CampaignPhase.InDungeon)
        {
            if (campaign.Phase == CampaignPhase.Hub)
            {
                EquipBestLoot(campaign);
                campaign.EnterDungeon();
                continue;
            }

            campaign.BeginEncounter();

            while (!campaign.Battle.IsOver)
            {
                Actor current = campaign.Battle.Current!;
                campaign.TakeTurn(ScoringAi.ChooseAction(campaign.Battle, current));
            }

            campaign.CompleteEncounter();
        }

        return new Result(campaign.Phase, campaign.Stats.DungeonsCleared,
            campaign.Stats.EncountersCleared, campaign.Stats, campaign.Grade, campaign.Loot);
    }

    /// <summary>
    /// A stand-in for a player at the hub: give everyone the best weapon they
    /// are not already carrying. Crude, but it means the harness measures a
    /// party that actually uses its loot.
    /// </summary>
    private static void EquipBestLoot(Campaign campaign)
    {
        var pool = campaign.Loot
            .Select(l => l.Weapon)
            .OrderByDescending(w => (int)w.Rarity)
            .ToList();

        foreach (Actor hero in campaign.Party)
        {
            WeaponDefinition? best = pool.FirstOrDefault();
            if (best is null) break;

            int current = hero.Weapon?.Bonus.Attack ?? -1;
            if (best.Bonus.Attack > current)
            {
                campaign.EquipOn(hero.Id, best);
                pool.Remove(best);
            }
        }
    }
}

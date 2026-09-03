// ============================================================================
//  CAMPAIGNHARNESSTESTS - is the whole campaign fair?
// ============================================================================
//
//  Three dungeons, three encounters each, wounds carrying between them and loot
//  rolling as you go. This plays hundreds of complete campaigns with the AI on
//  both sides and reports where they die.
//
//  Why this cannot be eyeballed: nine encounters is far too many to reason about
//  by hand. Dungeon two can be perfectly fair on paper and still end 90% of runs
//  because the party arrives from dungeon one at 30% health. Only a harness that
//  plays the WHOLE thing can see that.
//
//      dotnet test --logger "console;verbosity=detailed"
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Progression;
using Xunit;
using Xunit.Abstractions;

namespace Rpg.Core.Tests;

public sealed class CampaignHarnessTests
{
    private const int Campaigns = 250;

    private readonly ITestOutputHelper _output;

    public CampaignHarnessTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void TheCampaignIsBeatableButNotAGiveaway()
    {
        var diedIn = new int[4];       // how many runs ended having cleared 0/1/2/3 dungeons
        var grades = new Dictionary<string, int>();
        long dealt = 0, taken = 0, rounds = 0, lost = 0, encounters = 0;

        for (ulong seed = 1; seed <= Campaigns; seed++)
        {
            CampaignRunner.Result r = CampaignRunner.Play(seed);

            diedIn[r.DungeonsCleared]++;
            grades[r.Grade] = grades.GetValueOrDefault(r.Grade) + 1;

            dealt += r.Stats.DamageDealt;
            taken += r.Stats.DamageTaken;
            rounds += r.Stats.RoundsFought;
            lost += r.Stats.HeroesLost;
            encounters += r.EncountersCleared;
        }

        double winRate = diedIn[3] / (double)Campaigns;

        _output.WriteLine($"Campaigns simulated : {Campaigns}");
        _output.WriteLine("");
        _output.WriteLine($"  fell in Warrens     : {diedIn[0],4}  ({diedIn[0] / (double)Campaigns:P0})");
        _output.WriteLine($"  fell in Ember Halls : {diedIn[1],4}  ({diedIn[1] / (double)Campaigns:P0})");
        _output.WriteLine($"  fell in Frozen Crypt: {diedIn[2],4}  ({diedIn[2] / (double)Campaigns:P0})");
        _output.WriteLine($"  CLEARED ALL THREE   : {diedIn[3],4}  ({winRate:P0})");
        _output.WriteLine("");
        _output.WriteLine($"Avg encounters cleared : {encounters / (double)Campaigns:F1} of 9");
        _output.WriteLine($"Avg damage dealt       : {dealt / (double)Campaigns:F0}");
        _output.WriteLine($"Avg damage taken       : {taken / (double)Campaigns:F0}");
        _output.WriteLine($"Avg rounds             : {rounds / (double)Campaigns:F1}");
        _output.WriteLine($"Avg heroes lost        : {lost / (double)Campaigns:F2}");
        _output.WriteLine("");
        foreach (var g in grades.OrderBy(g => g.Key))
            _output.WriteLine($"  grade {g.Key}             : {g.Value,4}");

        // DESIGN INTENT, as an assertion.
        //
        // The AI plays noticeably worse than a person - it has no plan beyond
        // this turn and it never retreats. So a ~25% AI clear rate is a campaign
        // an attentive human finishes perhaps half the time, which is the shape
        // we want for a nine-fight gauntlet.
        Assert.InRange(winRate, 0.12, 0.55);
    }

    [Fact]
    public void TheDifficultyRisesFromOneDungeonToTheNext()
    {
        // Each dungeon must kill more runs than the one before it - measured as a
        // share of the runs that actually REACHED it, otherwise dungeon three
        // looks safe purely because few parties ever see it.
        var reached = new int[3];
        var diedThere = new int[3];

        for (ulong seed = 1; seed <= Campaigns; seed++)
        {
            CampaignRunner.Result r = CampaignRunner.Play(seed);

            for (int d = 0; d <= r.DungeonsCleared && d < 3; d++) reached[d]++;
            if (r.Outcome == CampaignPhase.Lost) diedThere[r.DungeonsCleared]++;
        }

        double[] lethality = Enumerable.Range(0, 3)
            .Select(d => reached[d] == 0 ? 0 : diedThere[d] / (double)reached[d])
            .ToArray();

        for (int d = 0; d < 3; d++)
            _output.WriteLine($"  dungeon {d + 1}: reached {reached[d],4}, died {diedThere[d],4}  -> {lethality[d]:P0} lethal");

        // THE CURVE MUST RISE, dungeon by dungeon. A gauntlet that dips in the
        // middle is worse than a flat one - it teaches the player that progress
        // makes things easier, and then betrays them at the end.
        Assert.True(lethality[0] < lethality[1],
            $"the Warrens ({lethality[0]:P0}) should be softer than the Ember Halls ({lethality[1]:P0})");
        Assert.True(lethality[1] < lethality[2],
            $"the Ember Halls ({lethality[1]:P0}) should be softer than the Crypt ({lethality[2]:P0})");

        // The first dungeon is a tutorial, but a dungeon nobody can lose is not
        // a dungeon. It sat at 0% for a long time; this band is what stops it
        // drifting back there.
        //
        // Getting here took real measurement: the fights were ending in 3.5
        // rounds with the party at 73% health, which meant the monsters were not
        // failing to hurt anyone - they were DYING before they got to swing. The
        // fix was tier-1 health, not tier-1 damage.
        Assert.InRange(lethality[0], 0.05, 0.20);

        Assert.True(reached[2] > 0, "nobody ever reaches the third dungeon");
    }

    [Fact]
    public void EveryHeroCanCarryAParty()
    {
        // A hero nobody would ever pick is content that does not exist. Each of
        // the ten should get at least ONE campaign further than the first
        // encounter when paired with a standard supporting cast.
        var reached = new Dictionary<string, int>();

        foreach (HeroDefinition hero in Heroes.All)
        {
            // Marching order matters now, and the list is FRONT FIRST. A caster
            // in rank 1 cannot cast, so the hero under test goes where their kit
            // actually works - which is what any player would do.
            // Where does MOST of this kit work? Asking "do they have ANY skill
            // usable from rank 1?" is wrong, because every caster was
            // deliberately given one anywhere-launchable fallback so they can
            // never be stranded - and that fallback was putting the Mage in the
            // front rank, which is the worst place for her.
            var kit = hero.SkillIds.Select(id => Skills.All.First(sk => sk.Id == id)).ToList();
            int atFront = kit.Count(sk => sk.LaunchRanks.Includes(1));
            int atBack = kit.Count(sk => sk.LaunchRanks.Includes(3));
            bool frontliner = atFront > atBack;   // strict: a tie goes to the back

            // Give every party a HEALER and a TANK. The first version filled the
            // other two slots from a fixed list and ended up handing almost
            // everybody warrior+monk and no Cleric - so it was measuring "can
            // this hero carry a party with no healing?", which nobody can, and
            // it failed nine heroes out of ten for the wrong reason.
            string healer = hero.Id == "cleric" ? "paladin" : "cleric";
            string tank = hero.Id is "warrior" or "templar" ? "monk" : "warrior";

            string[] party = frontliner
                ? new[] { hero.Id, tank, healer }        // up front where they work
                : new[] { tank, healer, hero.Id };       // casters to the back

            int best = 0;
            for (ulong seed = 1; seed <= 20; seed++)
                best = Math.Max(best, CampaignRunner.Play(seed, party).EncountersCleared);

            reached[hero.Label] = best;
        }

        foreach (var (label, best) in reached.OrderByDescending(k => k.Value))
            _output.WriteLine($"  {label,-14} best run: {best} of 9 encounters");

        foreach (var (label, best) in reached)
            Assert.True(best >= 2, $"{label} never cleared more than {best} encounters - unusable hero");
    }

    [Fact]
    public void TheEnemyGoesForTheHealer()
    {
        // The threat model's whole job. Put a Cleric next to two tougher heroes
        // and the monsters should still concentrate on her.
        ContentDatabase content = ContentDatabase.CreateDefault();
        var campaign = new Campaign(content, 5);
        campaign.SetParty("cleric", "warrior", "templar");
        campaign.EnterDungeon();
        campaign.BeginEncounter();

        var damageTaken = new Dictionary<string, int>();
        while (!campaign.Battle.IsOver)
        {
            var log = campaign.TakeTurn(
                Rpg.Core.Ai.ScoringAi.ChooseAction(campaign.Battle, campaign.Battle.Current!));

            foreach (var e in log.OfType<Rpg.Core.Combat.Damaged>())
                if (campaign.Party.Any(h => h.Id == e.ActorId))
                    damageTaken[e.ActorId] = damageTaken.GetValueOrDefault(e.ActorId) + e.Amount;
        }

        foreach (var (id, dmg) in damageTaken.OrderByDescending(k => k.Value))
            _output.WriteLine($"  {id,-10} took {dmg}");

        int cleric = damageTaken.GetValueOrDefault("cleric");
        int warrior = damageTaken.GetValueOrDefault("warrior");

        Assert.True(cleric > warrior,
            $"the Cleric ({cleric}) should be focused over the Warrior ({warrior}) - "
            + "if this fails, ThreatWeight is too low to matter");
    }
}

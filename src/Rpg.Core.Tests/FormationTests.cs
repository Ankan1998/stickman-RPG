// ============================================================================
//  FORMATIONTESTS - the Darkest Dungeon positioning rules
// ============================================================================
//
//  Both sides stand in a line. Position 1 is the front, position 4 the back.
//  Every skill says where it can be USED from and what it can REACH.
//
//        your party                    the enemy
//     4    3    2    1        |     1    2    3    4
//    back ---------> front    |   front <--------- back
//
//  These tests cover the four rules that make that work:
//
//    1. A skill you cannot launch from your rank is not offered at all.
//    2. A target out of reach is not offered either.
//    3. Ranks CLOSE UP when somebody dies, changing what is reachable.
//    4. Nobody can ever be stranded with no useful move.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Progression;
using Xunit;
using Xunit.Abstractions;
using static Rpg.Core.Tests.TestFixtures;

namespace Rpg.Core.Tests;

public sealed class FormationTests
{
    private readonly ITestOutputHelper _output;
    public FormationTests(ITestOutputHelper output) => _output = output;

    /// <summary>Melee: front two only, reaches the front two.</summary>
    private static readonly SkillDefinition Sword =
        new("sword", "Sword", "Melee.", TargetKind.SingleEnemy, Power: 100,
            LaunchPattern: "##--", TargetPattern: "##--");

    /// <summary>An ordinary bow: anywhere but the front rank, reaches the whole line.</summary>
    private static readonly SkillDefinition Bow =
        new("bow", "Bow", "Ranged.", TargetKind.SingleEnemy, Power: 100,
            LaunchPattern: "-###", TargetPattern: "####");

    /// <summary>A sniper shot: back two only, and it reaches only the enemy's back two.</summary>
    private static readonly SkillDefinition Snipe =
        new("snipe", "Snipe", "Ranged.", TargetKind.SingleEnemy, Power: 100,
            LaunchPattern: "--##", TargetPattern: "--##");

    /// <summary>Builds a battle from two ordered lines, front first.</summary>
    private static Battle Formation(string[] heroes, string[] monsters,
        SkillDefinition? heroSkill = null, SkillDefinition? monsterSkill = null)
    {
        var actors = new List<Actor>();

        // Descending speed, so the first hero listed also acts first. Turn order
        // is decided by Speed and ties break alphabetically by id - without this
        // a hero called "friend" would act before one called "sniper", which has
        // nothing to do with the formation these tests are about.
        int speed = 40;
        foreach (string h in heroes)
            actors.Add(MakeActor(h, Team.Heroes, Stats(hp: 100, spd: speed--), heroSkill ?? Sword));
        foreach (string m in monsters)
            actors.Add(MakeActor(m, Team.Monsters, Stats(hp: 100, spd: 1), monsterSkill ?? Sword));

        return new Battle(new BattleState(actors, new Rpg.Core.Rng.FixedRandom(0)));
    }

    // ------------------------------------------------------------------

    [Fact]
    public void RankOneIsTheFrontAndTheOrderIsTheFormation()
    {
        Battle b = Formation(new[] { "front", "middle", "back" }, new[] { "m1", "m2" });
        BattleState s = b.State;

        Assert.Equal(1, s.RankOf(s.GetActor("front")));
        Assert.Equal(2, s.RankOf(s.GetActor("middle")));
        Assert.Equal(3, s.RankOf(s.GetActor("back")));

        // Each side is numbered independently - the enemy has its own rank 1.
        Assert.Equal(1, s.RankOf(s.GetActor("m1")));
        Assert.Equal(2, s.RankOf(s.GetActor("m2")));
    }

    [Fact]
    public void ASwordInTheBackRankIsNotOffered()
    {
        Battle b = Formation(new[] { "front", "middle", "back" }, new[] { "m1" });
        b.Start();

        Actor back = b.State.GetActor("back");          // rank 3, sword launches "##--"

        Assert.False(b.CanLaunch(back, Sword));
        Assert.DoesNotContain(b.LegalActions(back),
            a => a is SkillAction s && s.Skill.Id == "sword");

        // ...and the front-liner can use it perfectly well.
        Assert.True(b.CanLaunch(b.State.GetActor("front"), Sword));
    }

    [Fact]
    public void ASniperInTheFrontRankIsNotOfferedEither()
    {
        // The mirror image, and the reason a bow is not simply better than a sword.
        Battle b = Formation(new[] { "front", "middle", "back" }, new[] { "m1" }, heroSkill: Snipe);
        b.Start();

        Assert.False(b.CanLaunch(b.State.GetActor("front"), Snipe));   // rank 1
        Assert.True(b.CanLaunch(b.State.GetActor("back"), Snipe));     // rank 3
    }

    [Fact]
    public void MeleeCannotReachTheEnemyBackLine()
    {
        Battle b = Formation(new[] { "front" }, new[] { "m1", "m2", "m3" });
        b.Start();

        Actor front = b.State.GetActor("front");
        var reachable = b.TargetsFor(front, Sword).Select(t => t.Id).ToList();

        Assert.Equal(new[] { "m1", "m2" }, reachable);   // "##--"
        Assert.DoesNotContain("m3", reachable);
    }

    [Fact]
    public void ASniperReachesTheBackLineAndNothingElse()
    {
        Battle b = Formation(new[] { "a", "b", "c" }, new[] { "m1", "m2", "m3" }, heroSkill: Snipe);
        b.Start();

        Actor sniper = b.State.GetActor("c");            // rank 3
        var reachable = b.TargetsFor(sniper, Snipe).Select(t => t.Id).ToList();

        Assert.Equal(new[] { "m3" }, reachable);          // "--##", and m4 does not exist
        Assert.DoesNotContain("m1", reachable);
    }

    [Fact]
    public void RanksCloseUpWhenSomebodyDies()
    {
        // THE rule that makes killing things change the shape of the fight: drop
        // the enemy front rank and the one behind steps into your sword's reach.
        Battle b = Formation(new[] { "front" }, new[] { "m1", "m2", "m3" });
        b.Start();

        Actor front = b.State.GetActor("front");
        Actor m3 = b.State.GetActor("m3");

        Assert.Equal(3, b.State.RankOf(m3));
        Assert.DoesNotContain(m3, b.TargetsFor(front, Sword));

        b.State.GetActor("m1").TakeDamage(999);           // the front rank falls

        Assert.Equal(2, b.State.RankOf(m3));              // everyone shuffled forward
        Assert.Contains(m3, b.TargetsFor(front, Sword));  // and is now in reach
    }

    [Fact]
    public void LosingYourFrontLinerShovesTheBackRankForward()
    {
        Battle b = Formation(new[] { "tank", "mid", "caster" }, new[] { "m1" });
        b.Start();

        Actor caster = b.State.GetActor("caster");
        Assert.Equal(3, b.State.RankOf(caster));

        b.State.GetActor("tank").TakeDamage(999);

        Assert.Equal(2, b.State.RankOf(caster));   // pushed towards the danger
    }

    [Fact]
    public void AStrandedFighterCanAlwaysStepBackIntoPosition()
    {
        // Nobody may ever be dead weight. An archer shoved into rank 1 cannot
        // shoot, but must still have something better to do than pass - and one
        // step back has to actually fix it.
        Battle b = Formation(new[] { "archer", "friend" }, new[] { "m1" }, heroSkill: Bow);
        b.Start();

        Actor archer = b.State.GetActor("archer");        // rank 1, bow needs "-###"
        Assert.False(b.CanLaunch(archer, Bow));

        List<IAction> options = b.LegalActions(archer);
        Assert.Contains(options, a => a is MoveAction);

        // Take the move, and the formation actually changes.
        IAction move = options.First(a => a is MoveAction);
        b.TakeTurn(move);

        Assert.Equal(2, b.State.RankOf(archer));
        Assert.True(b.CanLaunch(archer, Bow));
    }

    [Fact]
    public void MovingCostsTheWholeTurnAndReportsItself()
    {
        Battle b = Formation(new[] { "a", "bb" }, new[] { "m1" });
        b.Start();

        Actor a = b.State.GetActor("a");
        IAction move = b.LegalActions(a).First(x => x is MoveAction);
        List<GameEvent> log = b.TakeTurn(move);

        Assert.Contains(log, e => e is Repositioned { ActorId: "a", NewRank: 2 });
        Assert.Equal(100, b.State.GetActor("m1").Health);   // no attack happened
        Assert.NotSame(a, b.Current);                       // and the turn passed on
    }

    [Fact]
    public void EveryHeroHasSomethingUsableFromEveryRankTheyCanOccupy()
    {
        // The dead-end check. With a party of three, every hero can end up in
        // ranks 1-3, and in every one of them they must have at least one skill
        // they can actually launch - otherwise their only move is to shuffle,
        // for the whole fight.
        var complaints = new List<string>();

        foreach (HeroDefinition hero in Heroes.All)
        {
            for (int rank = 1; rank <= Campaign.PartySize; rank++)
            {
                bool any = hero.SkillIds
                    .Select(sid => Skills.All.First(s => s.Id == sid))
                    .Any(s => s.LaunchRanks.Includes(rank));

                if (!any) complaints.Add($"{hero.Label} has nothing to do in rank {rank}");
            }
        }

        foreach (string c in complaints) _output.WriteLine("  " + c);
        Assert.Empty(complaints);
    }

    [Fact]
    public void TheWraithIgnoresTheFormationEntirely()
    {
        // Its whole identity: no marching order protects you from it.
        SkillDefinition soulRip = Skills.All.First(s => s.Id == "soul_rip");

        Assert.Equal(Ranks.Any.Mask, soulRip.LaunchRanks.Mask);
        Assert.Equal(Ranks.Any.Mask, soulRip.TargetRanks.Mask);
        Assert.False(soulRip.IsPositional);
    }

    [Fact]
    public void TheRankDiagramReadsFrontFirst()
    {
        // The notation is shown to the player, so it had better be right.
        Assert.Equal("##--", Ranks.FrontTwo.Diagram);
        Assert.Equal("--##", Ranks.BackTwo.Diagram);
        Assert.Equal("####", Ranks.Any.Diagram);

        Assert.True(Ranks.FrontTwo.Includes(1));
        Assert.False(Ranks.FrontTwo.Includes(3));
        Assert.True(Ranks.BackTwo.Includes(4));
    }
}

// ============================================================================
//  EVENTLOGTESTS - the log has to be trustworthy enough to replay
// ============================================================================
//
//  The screen never watches the fight happen. It gets a finished list of events
//  and acts it out afterwards. That only works if the log is honest about three
//  things, and each of these tests pins one of them down:
//
//    1. A fighter is announced dead EXACTLY ONCE, however they died.
//    2. Every point of damage says where it came from - who swung, or which
//       status burned them.
//    3. Nothing is reported that did not actually happen.
//
//  All three were broken in ways you could see and hear on screen. The comments
//  below say how.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Entities;
using Xunit;
using static Rpg.Core.Tests.TestFixtures;

namespace Rpg.Core.Tests;

public sealed class EventLogTests
{
    /// <summary>A duel the hero is about to lose to their own poison.</summary>
    private static (Battle Battle, Actor Hero, List<GameEvent> Log) PoisonedToDeath()
    {
        // 4 health, and the test poison does exactly 4 a turn. The hero is also
        // much faster, so they act first and the poison ticks at the end of
        // their own turn - which is the case that used to look worst on screen.
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 4, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));

        hero.ApplyStatus(Poison, turns: 3);

        Battle battle = Duel(hero, monster);
        var log = new List<GameEvent>(battle.Start());
        log.AddRange(battle.TakeTurn(battle.LegalActions(hero).First(a => a is SkillAction)));

        return (battle, hero, log);
    }

    // ------------------------------------------------------------------

    [Fact]
    public void AFighterKilledByPoisonIsAnnouncedDeadExactlyOnce()
    {
        (_, Actor hero, List<GameEvent> log) = PoisonedToDeath();

        Assert.False(hero.IsAlive);
        Assert.Equal(1, log.OfType<Died>().Count(d => d.ActorId == "hero"));
    }

    [Fact]
    public void TheModelReadsDeadBeforeTheDeathIsAnnounced()
    {
        // This is not a defect, it is the shape of the design - and it is the
        // reason the view has to LATCH a death rather than react to the moment a
        // health bar hits zero.
        //
        // A turn is resolved completely before anything is drawn, so when the
        // screen replays the poison damage the hero is already at zero health.
        // The view therefore sees the death one event EARLY, and then sees the
        // Died event too. Reacting to both is what made the corpse fall twice.
        (_, _, List<GameEvent> log) = PoisonedToDeath();

        int damage = log.FindIndex(e => e is Damaged { StatusId: "poison", ActorId: "hero" });
        int death = log.FindIndex(e => e is Died { ActorId: "hero" });

        Assert.True(damage >= 0, "the poison tick should be in the log");
        Assert.True(death > damage, "the death is announced after the damage that caused it");
    }

    [Fact]
    public void NobodyIsEverAnnouncedDeadTwiceInAWholeBattle()
    {
        // The broad version, over real fights rather than a rigged one. Poison,
        // blades and finishing blows all route through Battle.ReportDeaths, and
        // any one of them double-reporting would show up here.
        for (ulong seed = 1; seed <= 200; seed++)
        {
            List<GameEvent> log = BattleRunner.Run(seed).Log;

            var doubled = log.OfType<Died>()
                .GroupBy(d => d.ActorId)
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key} died {g.Count()} times")
                .ToList();

            Assert.True(doubled.Count == 0, $"seed {seed}: {string.Join(", ", doubled)}");
        }
    }

    // ------------------------------------------------------------------

    [Fact]
    public void DamageFromABlowNamesWhoSwung()
    {
        // Without this the screen had to guess, and the only thing it could
        // reach for - "whose turn is it?" - is already the NEXT fighter by the
        // time a log is replayed. Every impact played the wrong weapon's sound.
        (_, _, List<GameEvent> log) = PoisonedToDeath();

        Damaged blow = log.OfType<Damaged>().First(d => d.ActorId == "monster");

        Assert.Equal("hero", blow.SourceId);
        Assert.Null(blow.StatusId);
    }

    [Fact]
    public void ByReplayTimeTheBattleHasAlreadyMovedOnToTheNextFighter()
    {
        // The precise reason Damaged has to carry SourceId at all.
        //
        // TakeTurn resolves the turn AND advances the queue before it returns
        // the log. So when the screen replays that log, "whose turn is it?" -
        // the only attacker the view could previously reach for - names the
        // fighter who is about to go, not the one who just swung.
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();
        Assert.Equal("hero", battle.Current?.Id);

        List<GameEvent> log = battle.TakeTurn(battle.LegalActions(hero).First(a => a is SkillAction));

        Damaged blow = log.OfType<Damaged>().Single();

        Assert.Equal("hero", blow.SourceId);            // who actually swung
        Assert.Equal("monster", battle.Current?.Id);    // who the view used to blame
    }

    [Fact]
    public void DamageFromAStatusNamesTheStatusAndNobodySwinging()
    {
        (_, _, List<GameEvent> log) = PoisonedToDeath();

        Damaged tick = log.OfType<Damaged>().First(d => d.ActorId == "hero");

        Assert.Equal("poison", tick.StatusId);
        Assert.Null(tick.SourceId);      // poison has no attacker
    }

    // ------------------------------------------------------------------

    [Fact]
    public void HealingSomebodyAlreadyAtFullHealthIsNotReported()
    {
        // It restored nothing, so reporting it wrote "+0 health" into the combat
        // log and bought a pause in the replay for an event where visibly
        // nothing happened. The life-drain branch always guarded this; the
        // ordinary healing branch did not.
        var bandage = new Rpg.Core.Content.SkillDefinition(
            "bandage", "Bandage", "Test heal.",
            Rpg.Core.Content.TargetKind.Self, Healing: 20);

        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 50, spd: 20), bandage);
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();

        List<GameEvent> log = battle.TakeTurn(
            battle.LegalActions(hero).First(a => a is SkillAction s && s.Skill.Id == "bandage"));

        Assert.Contains(log, e => e is SkillUsed { SkillId: "bandage" });
        Assert.DoesNotContain(log, e => e is Healed);
    }
}

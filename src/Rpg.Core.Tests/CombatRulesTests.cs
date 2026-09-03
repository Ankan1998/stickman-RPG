// ============================================================================
//  COMBATRULESTESTS - the rules a player would complain about if you broke them
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Nine tests covering the promises the game makes: poison ticks then wears off,
//  stun costs you a turn, cooldowns actually block reuse, the fight ends when a
//  team is wiped, you cannot act out of turn, healing cannot overheal or revive
//  the dead, buffs change your current stats but never your base ones, and
//  re-applying a status refreshes rather than stacks.
//
//  Every one of these runs in well under a millisecond, because the battle
//  engine has no rendering and no timing. That is the whole argument for keeping
//  Rpg.Core free of Godot - see docs/04-architecture.md.
//
//  C# NOTE
//  -------
//  "using static Rpg.Core.Tests.TestFixtures;" at the top imports that class's
//  members directly, so we can write MakeActor(...) and Stats(...) rather than
//  TestFixtures.MakeActor(...). Handy for test helpers; use it sparingly
//  elsewhere.
//
//  Note also how each test reads: set up a situation, do ONE thing, then assert
//  what should have happened. That "arrange / act / assert" shape is worth
//  copying.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Effects;
using Rpg.Core.Entities;
using Xunit;
using static Rpg.Core.Tests.TestFixtures;

namespace Rpg.Core.Tests;

/// <summary>
/// The rules a player would complain about if you broke them.
///
/// Every one of these runs in well under a millisecond, because the battle
/// engine has no rendering and no timing. That is the whole argument for
/// keeping Rpg.Core free of Godot.
/// </summary>
public sealed class CombatRulesTests
{
    [Fact]
    public void PoisonDamagesAtTheEndOfEachTurnAndThenWearsOff()
    {
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, atk: 10, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, atk: 10, spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();
        Assert.Same(hero, battle.Current);

        hero.ApplyStatus(Poison, turns: 2);

        // Round 1, hero acts: poison ticks AFTER the action, so a 1-turn poison
        // still gets to deal its damage once.
        battle.TakeTurn(new SkillAction(hero, Punch, monster));
        Assert.Equal(96, hero.Health);
        Assert.True(hero.HasStatus("poison"));

        // Round 1, monster hits back for a flat 10.
        battle.TakeTurn(new SkillAction(monster, Punch, hero));
        Assert.Equal(86, hero.Health);

        // Round 2, hero acts: second and final poison tick.
        Assert.Same(hero, battle.Current);
        battle.TakeTurn(new SkillAction(hero, Punch, monster));
        Assert.Equal(82, hero.Health);
        Assert.False(hero.HasStatus("poison"));
    }

    [Fact]
    public void AStunnedActorLosesTheirTurnButStillHasALegalMove()
    {
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 100, spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();

        hero.ApplyStatus(Stun, turns: 1);

        // The menu must never be empty, or the game loop has nothing to offer
        // the player and simply hangs.
        List<IAction> options = battle.LegalActions(hero);
        Assert.Single(options);
        Assert.IsType<PassAction>(options[0]);

        List<GameEvent> log = battle.TakeTurn(options[0]);

        Assert.Contains(log, e => e is TurnSkipped { Reason: "Stunned" });
        Assert.Equal(100, monster.Health);     // no attack landed
        Assert.False(hero.HasStatus("stun"));  // and the stun has now expired
    }

    [Fact]
    public void ASkillOnCooldownCannotBeChosenUntilItRecharges()
    {
        var heavy = new SkillDefinition("heavy", "Heavy", "Two turn cooldown.",
            TargetKind.SingleEnemy, Power: 100, Cooldown: 2);

        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, atk: 10, spd: 20), heavy, Punch);
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 1000, atk: 1, spd: 1), Punch);

        Battle battle = Duel(hero, monster);
        battle.Start();

        battle.TakeTurn(new SkillAction(hero, heavy, monster));
        Assert.False(hero.IsSkillReady("heavy"));

        battle.TakeTurn(new SkillAction(monster, Punch, hero));

        // Round 2: the cooldown ticks 2 -> 1. Still not available.
        Assert.Same(hero, battle.Current);
        Assert.False(hero.IsSkillReady("heavy"));
        Assert.DoesNotContain(battle.LegalActions(hero),
            a => a is SkillAction s && s.Skill.Id == "heavy");

        battle.TakeTurn(new SkillAction(hero, Punch, monster));
        battle.TakeTurn(new SkillAction(monster, Punch, hero));

        // Round 3: the cooldown ticks 1 -> 0. Available again.
        Assert.Same(hero, battle.Current);
        Assert.True(hero.IsSkillReady("heavy"));
        Assert.Contains(battle.LegalActions(hero),
            a => a is SkillAction s && s.Skill.Id == "heavy");
    }

    [Fact]
    public void TheBattleEndsAsSoonAsOneTeamIsWipedOut()
    {
        Actor hero = MakeActor("hero", Team.Heroes, Stats(hp: 100, atk: 100, spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(hp: 10, atk: 5, spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();

        List<GameEvent> log = battle.TakeTurn(new SkillAction(hero, Punch, monster));

        Assert.True(battle.IsOver);
        Assert.Equal(Team.Heroes, battle.Winner);
        Assert.Null(battle.Current);
        Assert.Contains(log, e => e is Died { ActorId: "monster" });
        Assert.Contains(log, e => e is BattleEnded { Winner: Team.Heroes });
    }

    [Fact]
    public void ActingOutOfTurnIsRejected()
    {
        Actor hero = MakeActor("hero", Team.Heroes, Stats(spd: 20));
        Actor monster = MakeActor("monster", Team.Monsters, Stats(spd: 1));

        Battle battle = Duel(hero, monster);
        battle.Start();

        // It is the hero's turn; the monster must not be able to sneak a move in.
        Assert.Throws<InvalidOperationException>(
            () => battle.TakeTurn(new SkillAction(monster, Punch, hero)));
    }

    [Fact]
    public void HealingCannotOverhealAndCannotReviveTheDead()
    {
        Actor actor = MakeActor("a", Team.Heroes, Stats(hp: 50));

        actor.TakeDamage(20);
        Assert.Equal(10, actor.Heal(10));
        Assert.Equal(40, actor.Health);

        Assert.Equal(10, actor.Heal(999));   // clamped to the missing 10
        Assert.Equal(50, actor.Health);

        actor.TakeDamage(999);
        Assert.False(actor.IsAlive);
        Assert.Equal(0, actor.Heal(20));     // dead stays dead
    }

    [Fact]
    public void StatusModifiersChangeCurrentStatsButNeverBaseStats()
    {
        Actor actor = MakeActor("a", Team.Heroes, Stats(hp: 50, def: 2));

        actor.ApplyStatus(
            StatusDefinition.Buff("guard", "Guarding", "Braced.", new StatBlock(0, 0, 6, 0, 0)),
            turns: 2);

        Assert.Equal(8, actor.CurrentStats.Defense);
        Assert.Equal(2, actor.BaseStats.Defense);
    }

    [Fact]
    public void ReapplyingAStatusRefreshesItRatherThanStackingASecondCopy()
    {
        Actor actor = MakeActor("a", Team.Heroes, Stats());

        actor.ApplyStatus(Poison, turns: 1);
        actor.ApplyStatus(Poison, turns: 4);

        Assert.Single(actor.Statuses);
        Assert.Equal(4, actor.Statuses[0].RemainingTurns);
    }
}

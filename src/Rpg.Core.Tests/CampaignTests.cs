// ============================================================================
//  CAMPAIGNTESTS - the rules of the hub-and-dungeon game
// ============================================================================
//
//  A Battle is one fight. A DUNGEON is three fights on one health bar. A
//  CAMPAIGN is three dungeons with a camp between them.
//
//  These cover the rules that make that a game rather than nine unrelated
//  fights: wounds carry inside a dungeon, camp clears them, the party can only
//  change at camp, and loot gets better the deeper you go.
// ============================================================================

using Rpg.Core.Ai;
using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Progression;
using Xunit;

namespace Rpg.Core.Tests;

public sealed class CampaignTests
{
    private static Campaign New(ulong seed = 1) => new(ContentDatabase.CreateDefault(), seed);

    private static void AutoPlayEncounter(Campaign c)
    {
        while (!c.Battle.IsOver)
            c.TakeTurn(ScoringAi.ChooseAction(c.Battle, c.Battle.Current!));
    }

    [Fact]
    public void ACampaignStartsAtCampWithThreeHeroesAndThreeDungeonsAhead()
    {
        Campaign c = New();

        Assert.Equal(CampaignPhase.Hub, c.Phase);
        Assert.Equal(3, c.TotalDungeons);
        Assert.Equal(Campaign.PartySize, c.Party.Count);
        Assert.All(c.Party, h => Assert.Equal(h.MaxHealth, h.Health));
        Assert.Empty(c.Loot);
    }

    [Fact]
    public void ThePartyIsChosenAtCampAndLockedInsideADungeon()
    {
        Campaign c = New();

        c.SetParty("mage", "cleric", "templar");
        Assert.Equal(new[] { "mage", "cleric", "templar" }, c.Party.Select(h => h.Id));

        c.EnterDungeon();

        // Swapping heroes mid-dungeon would make "wounds carry over" meaningless.
        Assert.Throws<InvalidOperationException>(() => c.SetParty("rogue", "monk", "warrior"));
    }

    [Fact]
    public void APartyIsExactlyThreeDistinctHeroes()
    {
        Campaign c = New();

        Assert.Throws<ArgumentException>(() => c.SetParty("warrior", "cleric"));
        Assert.Throws<ArgumentException>(() => c.SetParty("warrior", "warrior", "warrior"));
    }

    [Fact]
    public void WoundsCarryBetweenEncountersAndOnlyCampClearsThem()
    {
        // THE rule that makes a dungeon a unit of tension rather than three
        // unrelated fights. Between encounters you get a BREATHER - a fraction of
        // your health back, never all of it. Only reaching camp restores you.
        Campaign c = New(3);
        c.EnterDungeon();

        c.BeginEncounter();
        AutoPlayEncounter(c);

        Actor hero = c.Party[0];
        hero.TakeDamage(hero.Health - 4);          // wound him badly, fight already decided
        c.CompleteEncounter();

        if (c.Phase != CampaignPhase.InDungeon)
            return;                                 // party wiped on encounter 1; nothing to assert

        // The breather helped, but nowhere near a full heal - the wound follows
        // him into the next fight.
        int expected = 4 + hero.MaxHealth * Campaign.BreatherPercent / 100;
        Assert.Equal(expected, hero.Health);
        Assert.True(hero.Health < hero.MaxHealth,
            "a breather must never fully restore a hero - that is what camp is for");

        c.BeginEncounter();
        Assert.Equal(expected, hero.Health);       // beginning a fight heals nobody
    }

    [Fact]
    public void ClearingADungeonReturnsYouToCampFullyRested()
    {
        Campaign c = FindCampaignThatClears(dungeons: 1);

        Assert.Equal(CampaignPhase.Hub, c.Phase);
        Assert.Equal(1, c.Stats.DungeonsCleared);
        Assert.Equal(2, c.DungeonNumber);           // now standing before dungeon two

        // Camp heals on the way OUT, so entering the next dungeon is full health.
        c.EnterDungeon();
        Assert.All(c.Party, h => Assert.Equal(h.MaxHealth, h.Health));
    }

    [Fact]
    public void EveryClearedEncounterDropsAWeapon()
    {
        Campaign c = FindCampaignThatClears(dungeons: 1);

        Assert.Equal(c.Stats.EncountersCleared, c.Loot.Count);
        Assert.All(c.Loot, drop => Assert.False(string.IsNullOrWhiteSpace(drop.Weapon.Label)));
    }

    [Fact]
    public void EquippingAWeaponRaisesTheWearersStats()
    {
        Campaign c = New();
        Actor hero = c.Party.First(h => h.Id == "warrior");

        int before = hero.CurrentStats.Attack;
        WeaponDefinition big = Weapons.Get("worldcleaver");   // legendary greataxe

        c.EquipOn(hero.Id, big);

        Assert.Same(big, hero.Weapon);
        Assert.True(hero.CurrentStats.Attack > before,
            $"attack should rise: was {before}, now {hero.CurrentStats.Attack}");
        Assert.Equal(hero.BaseStats.Attack + big.Bonus.Attack, hero.CurrentStats.Attack);
    }

    [Fact]
    public void ReChoosingTheSameHeroKeepsTheirWeapon()
    {
        // Otherwise fiddling with the party at camp silently disarms everyone.
        Campaign c = New();
        c.EquipOn("warrior", Weapons.Get("void_edge"));

        c.SetParty("warrior", "mage", "monk");

        Assert.Equal("void_edge", c.Party.First(h => h.Id == "warrior").Weapon?.Id);
    }

    [Fact]
    public void EquippingCannotLeaveAHeroAboveTheirMaximumHealth()
    {
        // Several weapons add MaxHealth. Taking one off must not leave the wearer
        // on more health than they can now hold.
        Campaign c = New();
        Actor hero = c.Party[0];

        c.EquipOn(hero.Id, Weapons.Get("tower_shield"));   // +health
        hero.Heal(999);
        int boosted = hero.Health;

        hero.Equip(null);

        Assert.True(hero.Health <= hero.MaxHealth,
            $"was {boosted} with the shield, {hero.Health} without, max {hero.MaxHealth}");
    }

    [Fact]
    public void DeeperDungeonsRollBetterLoot()
    {
        // The whole equipment progression is this one table. If it inverts, the
        // Frozen Crypt hands out rusty shortswords.
        LootTable warrens = Dungeons.Get("warrens").Loot;
        LootTable crypt = Dungeons.Get("crypt").Loot;

        Assert.True(crypt.Legendary > warrens.Legendary);
        Assert.True(crypt.Epic > warrens.Epic);
        Assert.True(crypt.Common < warrens.Common);
    }

    [Fact]
    public void EachDungeonFieldsHarderMonstersThanTheLast()
    {
        ContentDatabase content = ContentDatabase.CreateDefault();

        double[] averageTier = content.Dungeons
            .Select(d => d.Encounters
                .SelectMany(e => e.MonsterIds)
                .Average(id => content.Monster(id).Tier))
            .ToArray();

        Assert.True(averageTier[0] < averageTier[1],
            $"dungeon 1 ({averageTier[0]:F2}) should be softer than dungeon 2 ({averageTier[1]:F2})");
        Assert.True(averageTier[1] < averageTier[2],
            $"dungeon 2 ({averageTier[1]:F2}) should be softer than dungeon 3 ({averageTier[2]:F2})");
    }

    [Fact]
    public void EveryDungeonHasTwoOrThreeEncounters()
    {
        foreach (DungeonDefinition d in Dungeons.All)
            Assert.InRange(d.Encounters.Count, 2, 3);
    }

    [Fact]
    public void EverySkillAndMonsterReferencedByContentActuallyExists()
    {
        // Content is written by hand, so a typo in a skill id is the single most
        // likely mistake. This catches all of them at once.
        ContentDatabase content = ContentDatabase.CreateDefault();

        foreach (HeroDefinition h in Heroes.All)
            foreach (string skillId in h.SkillIds)
                content.Skill(skillId);                      // throws if missing

        foreach (MonsterTemplate m in Monsters.All)
            foreach (string skillId in m.SkillIds)
                content.Skill(skillId);

        foreach (DungeonDefinition d in Dungeons.All)
            foreach (EncounterDefinition e in d.Encounters)
                foreach (string monsterId in e.MonsterIds)
                    content.Monster(monsterId);

        // Every status a skill can apply must be in the display table too.
        foreach (SkillDefinition s in Skills.All)
            if (s.AppliesStatus is { } status)
                content.Status(status.Id);
    }

    [Fact]
    public void ACampaignIsFullyReproducibleFromItsSeed()
    {
        CampaignRunner.Result a = CampaignRunner.Play(777);
        CampaignRunner.Result b = CampaignRunner.Play(777);

        Assert.Equal(a.Outcome, b.Outcome);
        Assert.Equal(a.EncountersCleared, b.EncountersCleared);
        Assert.Equal(a.Stats.DamageDealt, b.Stats.DamageDealt);
        Assert.Equal(a.Grade, b.Grade);
        Assert.Equal(a.Loot.Select(l => l.Weapon.Id), b.Loot.Select(l => l.Weapon.Id));
    }

    // -- helpers ------------------------------------------------------------

    /// <summary>Finds a seed where the AI clears at least this many dungeons, and stops there.</summary>
    private static Campaign FindCampaignThatClears(int dungeons)
    {
        for (ulong seed = 1; seed <= 400; seed++)
        {
            Campaign c = New(seed);

            while (c.Phase is CampaignPhase.Hub or CampaignPhase.InDungeon)
            {
                if (c.Stats.DungeonsCleared >= dungeons && c.Phase == CampaignPhase.Hub)
                    return c;

                if (c.Phase == CampaignPhase.Hub) { c.EnterDungeon(); continue; }

                c.BeginEncounter();
                AutoPlayEncounter(c);
                c.CompleteEncounter();
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"No seed in 1..400 cleared {dungeons} dungeon(s). The balance is degenerate.");
    }
}

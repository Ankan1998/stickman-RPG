// ============================================================================
//  CONTENTINTEGRITYTESTS - the safety net that data-driven content needs
// ============================================================================
//
//  THE TRADE THESE TESTS PAY FOR
//  -----------------------------
//  Content in this project is DATA, not classes: 55 skills, 22 monsters, 10
//  heroes and 9 encounters are rows of numbers rather than types. That buys an
//  enormous amount (see docs/gamedev/11-content-as-data.md) and costs exactly
//  one thing:
//
//      the compiler can no longer check that "goblin_archer" is a real monster.
//
//  With a class per monster, a typo is a build error. With ids in an array, a
//  typo is a KeyNotFoundException at 11pm in encounter seven of a playtest.
//
//  These tests buy that guarantee back. They walk every reference in every piece
//  of content and check it resolves. They are fast, they never need updating,
//  and they turn a whole category of late runtime crash into an instant, named
//  build failure.
//
//  ADD CONTENT FREELY. If you fat-finger an id, one of these will tell you
//  exactly which row it is in.
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Progression;
using Xunit;
using Xunit.Abstractions;

namespace Rpg.Core.Tests;

public sealed class ContentIntegrityTests
{
    private readonly ITestOutputHelper _output;
    public ContentIntegrityTests(ITestOutputHelper output) => _output = output;

    private static readonly HashSet<string> SkillIds =
        Skills.All.Select(s => s.Id).ToHashSet();

    private static readonly HashSet<string> MonsterIds =
        Monsters.All.Select(m => m.Id).ToHashSet();

    /// <summary>Reports every complaint at once rather than dying on the first.</summary>
    private void AssertNone(List<string> complaints)
    {
        foreach (string c in complaints) _output.WriteLine("  " + c);
        Assert.Empty(complaints);
    }

    // ------------------------------------------------------------------
    //  Ids resolve
    // ------------------------------------------------------------------

    [Fact]
    public void EverySkillIdReferencedByAHeroExists()
    {
        var complaints = new List<string>();

        foreach (HeroDefinition hero in Heroes.All)
            foreach (string skillId in hero.SkillIds)
                if (!SkillIds.Contains(skillId))
                    complaints.Add($"{hero.Label} references skill '{skillId}', which does not exist");

        AssertNone(complaints);
    }

    [Fact]
    public void EverySkillIdReferencedByAMonsterExists()
    {
        var complaints = new List<string>();

        foreach (MonsterTemplate monster in Monsters.All)
            foreach (string skillId in monster.SkillIds)
                if (!SkillIds.Contains(skillId))
                    complaints.Add($"{monster.Label} references skill '{skillId}', which does not exist");

        AssertNone(complaints);
    }

    [Fact]
    public void EveryMonsterIdInAnEncounterExists()
    {
        var complaints = new List<string>();

        foreach (DungeonDefinition dungeon in Dungeons.All)
            foreach (EncounterDefinition encounter in dungeon.Encounters)
                foreach (string monsterId in encounter.MonsterIds)
                    if (!MonsterIds.Contains(monsterId))
                        complaints.Add(
                            $"{dungeon.Label} / {encounter.Name} references monster " +
                            $"'{monsterId}', which does not exist");

        AssertNone(complaints);
    }

    [Fact]
    public void EveryStartingPartyMemberIsARealHero()
    {
        var heroIds = Heroes.All.Select(h => h.Id).ToHashSet();

        foreach (string id in Heroes.StartingParty)
            Assert.True(heroIds.Contains(id), $"the starting party references '{id}', which does not exist");

        Assert.Equal(Campaign.PartySize, Heroes.StartingParty.Distinct().Count());
    }

    // ------------------------------------------------------------------
    //  Ids are unique
    //
    //  A duplicate id is worse than a missing one: lookups silently return
    //  whichever came first, so half your content quietly becomes the wrong
    //  thing with no error anywhere.
    // ------------------------------------------------------------------

    [Fact]
    public void EveryContentIdIsUnique()
    {
        var complaints = new List<string>();

        void CheckUnique<T>(string what, IEnumerable<T> items, Func<T, string> idOf)
        {
            foreach (var duplicate in items.GroupBy(idOf).Where(g => g.Count() > 1))
                complaints.Add($"{what} id '{duplicate.Key}' is used {duplicate.Count()} times");
        }

        CheckUnique("skill", Skills.All, s => s.Id);
        CheckUnique("status", Statuses.All, s => s.Id);
        CheckUnique("hero", Heroes.All, h => h.Id);
        CheckUnique("monster", Monsters.All, m => m.Id);
        CheckUnique("weapon", Weapons.All, w => w.Id);
        CheckUnique("dungeon", Dungeons.All, d => d.Id);
        CheckUnique("encounter", Dungeons.All.SelectMany(d => d.Encounters), e => e.Id);

        AssertNone(complaints);
    }

    // ------------------------------------------------------------------
    //  The content is actually playable
    // ------------------------------------------------------------------

    [Fact]
    public void EveryHeroAndMonsterCanBeBuilt()
    {
        // The end-to-end check: every definition survives the trip through
        // ContentDatabase into a live Actor. Catches anything the id checks
        // above miss, such as a skill list that is empty.
        ContentDatabase content = ContentDatabase.CreateDefault();

        foreach (HeroDefinition hero in Heroes.All)
        {
            var actor = content.CreateHero(hero.Id);
            Assert.True(actor.Skills.Count > 0, $"{hero.Label} has no skills");
            Assert.True(actor.MaxHealth > 0, $"{hero.Label} has no health");
        }

        foreach (MonsterTemplate monster in Monsters.All)
        {
            var actor = content.CreateMonster(monster.Id, "m0", string.Empty);
            Assert.True(actor.Skills.Count > 0, $"{monster.Label} has no skills");
            Assert.True(actor.MaxHealth > 0, $"{monster.Label} has no health");
        }
    }

    [Fact]
    public void EveryEncounterHasSomethingToFight()
    {
        var complaints = new List<string>();

        foreach (DungeonDefinition dungeon in Dungeons.All)
        {
            if (dungeon.Encounters.Count == 0)
                complaints.Add($"{dungeon.Label} has no encounters");

            foreach (EncounterDefinition encounter in dungeon.Encounters)
                if (encounter.MonsterIds.Count == 0)
                    complaints.Add($"{dungeon.Label} / {encounter.Name} has no monsters");
        }

        AssertNone(complaints);
    }

    [Fact]
    public void EveryLootTableCanActuallyDropSomething()
    {
        // A rarity with weight but no weapons in it would fall back down the
        // ladder at runtime. Better to know now.
        var complaints = new List<string>();

        foreach (DungeonDefinition dungeon in Dungeons.All)
        {
            LootTable loot = dungeon.Loot;
            if (loot.Total <= 0)
            {
                complaints.Add($"{dungeon.Label} has an empty loot table");
                continue;
            }

            foreach ((Rarity rarity, int weight) in new[]
                     {
                         (Rarity.Common, loot.Common),
                         (Rarity.Uncommon, loot.Uncommon),
                         (Rarity.Rare, loot.Rare),
                         (Rarity.Epic, loot.Epic),
                         (Rarity.Legendary, loot.Legendary),
                     })
            {
                if (weight > 0 && !Weapons.OfRarity(rarity).Any())
                    complaints.Add($"{dungeon.Label} can roll {rarity}, but no {rarity} weapon exists");
            }
        }

        AssertNone(complaints);
    }
}

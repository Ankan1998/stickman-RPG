// ============================================================================
//  CAMPAIGN - the whole game, hub and all
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  One Battle is a fight. One Dungeon is three fights on a single health bar.
//  A CAMPAIGN is three dungeons with a camp between them:
//
//      HUB -> Warrens -> HUB -> Ember Halls -> HUB -> Frozen Crypt -> VICTORY
//
//  At the hub you pick three heroes out of ten, hand out the loot you have
//  found, and rest. Inside a dungeon you are on your own: wounds carry from one
//  encounter to the next, and the only healing is what your party brought.
//
//  THE TWO RULES THAT MAKE IT A GAME
//  ---------------------------------
//  1. Wounds carry INSIDE a dungeon, and the hub clears them. So a dungeon is
//     the real unit of tension, and reaching camp is relief rather than a menu.
//
//  2. Your party is chosen per dungeon, and each dungeon hurts you differently.
//     Frozen Crypt curses your Attack, so the all-damage party that flattened
//     the Warrens stops working there. That is the whole point of the hub.
//
//  HOW YOU USE IT
//  --------------
//      var campaign = new Campaign(content, seed: 1);
//
//      campaign.SetParty("warrior", "cleric", "mage");   // at the hub
//      campaign.EnterDungeon();
//
//      while (campaign.Phase == CampaignPhase.InDungeon)
//      {
//          campaign.BeginEncounter();
//          while (!campaign.Battle.IsOver)
//              campaign.TakeTurn(chosen);
//          campaign.CompleteEncounter();      // rolls loot, or ends the run
//      }
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Rng;

namespace Rpg.Core.Progression;

public enum CampaignPhase
{
    /// <summary>At camp: choose a party, hand out loot, then descend.</summary>
    Hub,
    /// <summary>Between encounters inside a dungeon.</summary>
    InDungeon,
    /// <summary>All three dungeons cleared.</summary>
    Won,
    /// <summary>The party was wiped out.</summary>
    Lost,
}

/// <summary>Something that dropped after an encounter.</summary>
public sealed record LootDrop(WeaponDefinition Weapon, string FromEncounter);

public sealed class Campaign
{
    /// <summary>How many heroes go into a dungeon. Every encounter is built assuming this.</summary>
    public const int PartySize = 3;

    /// <summary>
    /// Health recovered by each surviving hero after clearing an encounter, as a
    /// percentage of their maximum.
    ///
    /// This is the single biggest difficulty dial in the game. At 0 - which is
    /// where this started - the first dungeon alone ended 52% of campaigns, and
    /// nobody in 250 simulated runs ever saw the third. Attrition needs to bite,
    /// but three fights on one health bar with no breather at all is not
    /// attrition, it is a coin flip.
    /// </summary>
    public const int BreatherPercent = 28;

    /// <summary>A hero who fell gets back up at this much health when the party moves on.</summary>
    public const int RevivePercent = 30;

    private readonly ContentDatabase _content;
    private readonly IRandomSource _rng;
    private readonly Dictionary<string, Team> _teams = new();
    private readonly List<LootDrop> _loot = new();
    private readonly List<Actor> _party = new();

    private int _dungeonIndex;
    private int _encounterIndex = -1;
    private Battle? _battle;

    public Campaign(ContentDatabase content, ulong seed)
    {
        _content = content;
        Seed = seed;

        // ONE random source for the entire campaign, so the whole thing - every
        // fight, every crit, every loot roll - replays from a single number.
        _rng = new SplitMix64Random(seed);

        SetParty(Heroes.StartingParty);
    }

    public ulong Seed { get; }
    public RunStats Stats { get; } = new();
    public CampaignPhase Phase { get; private set; } = CampaignPhase.Hub;

    /// <summary>The three heroes currently chosen. Fresh objects each time the party changes.</summary>
    public IReadOnlyList<Actor> Party => _party;

    /// <summary>Everything found so far, equipped or not.</summary>
    public IReadOnlyList<LootDrop> Loot => _loot;

    public IReadOnlyList<DungeonDefinition> Dungeons => _content.Dungeons;
    public int DungeonNumber => _dungeonIndex + 1;
    public int TotalDungeons => _content.Dungeons.Count;
    public bool HasMoreDungeons => _dungeonIndex < _content.Dungeons.Count;

    /// <summary>The dungeon you are in, or the one you are about to enter.</summary>
    public DungeonDefinition CurrentDungeon =>
        _content.Dungeons[Math.Min(_dungeonIndex, _content.Dungeons.Count - 1)];

    public int EncounterNumber => _encounterIndex + 1;
    public int TotalEncounters => CurrentDungeon.Encounters.Count;
    public EncounterDefinition CurrentEncounter => CurrentDungeon.Encounters[_encounterIndex];
    public bool HasMoreEncounters => _encounterIndex + 1 < CurrentDungeon.Encounters.Count;

    public Battle Battle => _battle
        ?? throw new InvalidOperationException("No encounter is running. Call BeginEncounter().");

    public double PartyHealthFraction
    {
        get
        {
            int max = _party.Sum(a => a.MaxHealth);
            return max == 0 ? 0 : _party.Sum(a => a.Health) / (double)max;
        }
    }

    // ==================================================================
    //  The hub
    // ==================================================================

    /// <summary>
    /// Chooses the party. Only legal at the hub - swapping heroes mid-dungeon
    /// would make the "wounds carry over" rule meaningless.
    /// </summary>
    public void SetParty(params string[] heroIds) => SetParty((IEnumerable<string>)heroIds);

    public void SetParty(IEnumerable<string> heroIds)
    {
        if (Phase is CampaignPhase.InDungeon)
            throw new InvalidOperationException("The party cannot be changed inside a dungeon.");

        string[] ids = heroIds.Distinct().Take(PartySize).ToArray();
        if (ids.Length != PartySize)
            throw new ArgumentException($"A party is exactly {PartySize} distinct heroes; got {ids.Length}.");

        // Remember what everyone was carrying, so re-picking the same hero does
        // not silently disarm them.
        var carried = _party.ToDictionary(a => a.Id, a => a.Weapon);

        _party.Clear();
        foreach (string id in ids)
        {
            Actor hero = _content.CreateHero(id);
            if (carried.TryGetValue(hero.Id, out WeaponDefinition? weapon))
                hero.Equip(weapon);
            _party.Add(hero);
            _teams[hero.Id] = hero.Team;
        }
    }

    /// <summary>Gives a found weapon to a hero, returning whatever they were holding.</summary>
    public WeaponDefinition? EquipOn(string heroId, WeaponDefinition weapon)
    {
        Actor hero = _party.FirstOrDefault(a => a.Id == heroId)
            ?? throw new KeyNotFoundException($"'{heroId}' is not in the party.");
        return hero.Equip(weapon);
    }

    /// <summary>Leaves the hub and starts the next dungeon. The party is fully restored first.</summary>
    public void EnterDungeon()
    {
        if (Phase != CampaignPhase.Hub)
            throw new InvalidOperationException($"Cannot enter a dungeon from {Phase}.");
        if (!HasMoreDungeons)
            throw new InvalidOperationException("There are no dungeons left.");

        // Resting at camp is the reward for getting out alive.
        foreach (Actor hero in _party)
        {
            hero.ResetForNextBattle();
            hero.ReviveWith(hero.MaxHealth);
            hero.Heal(hero.MaxHealth);
        }

        _encounterIndex = -1;
        Phase = CampaignPhase.InDungeon;
    }

    // ==================================================================
    //  Inside a dungeon
    // ==================================================================

    /// <summary>Sets up and starts the next encounter, returning its opening events.</summary>
    public List<GameEvent> BeginEncounter()
    {
        if (Phase != CampaignPhase.InDungeon)
            throw new InvalidOperationException($"Cannot begin an encounter from {Phase}.");
        if (!HasMoreEncounters)
            throw new InvalidOperationException("This dungeon has no encounters left.");

        _encounterIndex++;

        // Statuses and cooldowns do not survive between encounters. Wounds do.
        foreach (Actor hero in _party)
            hero.ResetForNextBattle();

        List<Actor> monsters = BuildMonsters(CurrentEncounter);
        foreach (Actor monster in monsters)
            _teams[monster.Id] = monster.Team;

        _battle = new Battle(new BattleState(_party.Concat(monsters), _rng));

        List<GameEvent> log = _battle.Start();
        Stats.Observe(log, TeamOf);
        return log;
    }

    /// <summary>Like Battle.TakeTurn, but also keeps score. Route every turn through here.</summary>
    public List<GameEvent> TakeTurn(IAction action)
    {
        List<GameEvent> log = Battle.TakeTurn(action);
        Stats.Observe(log, TeamOf);
        return log;
    }

    /// <summary>
    /// Call once the current battle is over. Rolls loot and advances, or ends the
    /// campaign. Returns what dropped, if anything.
    /// </summary>
    public LootDrop? CompleteEncounter()
    {
        if (_battle is null || !_battle.IsOver)
            throw new InvalidOperationException("The current encounter is not finished.");

        Stats.RoundsFought += _battle.Round;

        if (_battle.Winner != Team.Heroes)
        {
            Phase = CampaignPhase.Lost;
            return null;
        }

        Stats.EncountersCleared++;
        LootDrop drop = RollLoot();
        _loot.Add(drop);

        if (HasMoreEncounters)
        {
            CatchBreath();
            return drop;                       // press on, still wounded
        }

        // Dungeon cleared.
        Stats.DungeonsCleared++;
        _dungeonIndex++;
        Phase = HasMoreDungeons ? CampaignPhase.Hub : CampaignPhase.Won;
        return drop;
    }

    /// <summary>
    /// A letter grade for the campaign. Graded on heroes lost, because that is
    /// the clearest measure of playing well.
    /// </summary>
    public string Grade => Phase switch
    {
        CampaignPhase.Lost => "-",
        CampaignPhase.Won when Stats.HeroesLost == 0 => "S",
        CampaignPhase.Won when Stats.HeroesLost <= 2 => "A",
        CampaignPhase.Won when Stats.HeroesLost <= 5 => "B",
        CampaignPhase.Won => "C",
        _ => "?",
    };

    // ==================================================================

    /// <summary>
    /// Between encounters: bandages, not a night's sleep. Enough that a dungeon
    /// is survivable, not enough that the first fight stops mattering.
    /// </summary>
    private void CatchBreath()
    {
        foreach (Actor hero in _party)
        {
            if (hero.IsAlive)
                hero.Heal(hero.MaxHealth * BreatherPercent / 100);
            else
                hero.ReviveWith(hero.MaxHealth * RevivePercent / 100);
        }
    }

    /// <summary>Rolls one weapon from the current dungeon's loot table.</summary>
    private LootDrop RollLoot()
    {
        LootTable table = CurrentDungeon.Loot;
        Rarity rarity = table.RarityFor(_rng.NextInt(0, table.Total));

        // A rarity with no weapons in it would throw; fall back down the ladder
        // until we find one that does. Defensive, but a legendary-only table
        // with a typo in it should not crash the game.
        WeaponDefinition[] pool = Weapons.OfRarity(rarity).ToArray();
        while (pool.Length == 0 && rarity > Rarity.Common)
            pool = Weapons.OfRarity(--rarity).ToArray();

        return new LootDrop(pool[_rng.NextInt(0, pool.Length)], CurrentEncounter.Name);
    }

    /// <summary>
    /// Builds an encounter's monsters, giving duplicates distinct ids AND
    /// distinct display names - otherwise the player sees two identical buttons
    /// and cannot tell which one they are about to hit.
    /// </summary>
    private List<Actor> BuildMonsters(EncounterDefinition encounter)
    {
        var counts = encounter.MonsterIds.GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
        var seen = new Dictionary<string, int>();
        var monsters = new List<Actor>();

        for (int slot = 0; slot < encounter.MonsterIds.Count; slot++)
        {
            string templateId = encounter.MonsterIds[slot];
            seen[templateId] = seen.GetValueOrDefault(templateId) + 1;

            string suffix = counts[templateId] > 1
                ? " " + (char)('A' + seen[templateId] - 1)
                : string.Empty;

            monsters.Add(_content.CreateMonster(
                templateId, actorId: $"m{slot}_{templateId}", nameSuffix: suffix));
        }

        return monsters;
    }

    private Team TeamOf(string actorId) =>
        _teams.TryGetValue(actorId, out Team team) ? team : Team.Monsters;
}

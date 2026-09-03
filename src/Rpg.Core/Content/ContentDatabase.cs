// ============================================================================
//  CONTENTDATABASE - the front door to all the game's content
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Lookup tables. Ask it for a skill, a status, a hero or a monster by id and
//  it hands one over, or throws a clear error naming the id you got wrong.
//
//  The content ITSELF lives in five sibling files, because one file with ten
//  heroes, twenty-two monsters, fifty skills and forty-seven weapons in it is a
//  file nobody can find anything in:
//
//      Statuses.cs   poison, burning, chill, curse, buffs
//      Skills.cs     every ability, hero and monster
//      Heroes.cs     the ten you can recruit
//      Monsters.cs   the twenty-two that want you dead
//      Weapons.cs    the forty-seven pieces of loot
//
//  and the dungeons are in Progression/DungeonDefinition.cs, next to the code
//  that walks you through them.
//
//  START IN THOSE FILES if you want to change the game rather than the
//  machinery. Recipes are in docs/07-recipes.md.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Effects;
using Rpg.Core.Entities;
using Rpg.Core.Progression;
using Rpg.Core.Rng;

namespace Rpg.Core.Content;

public sealed class ContentDatabase
{
    private readonly Dictionary<string, SkillDefinition> _skills;
    private readonly Dictionary<string, StatusDefinition> _statuses;
    private readonly Dictionary<string, HeroDefinition> _heroes;
    private readonly Dictionary<string, MonsterTemplate> _monsters;

    private ContentDatabase()
    {
        // C# note: the "global::" prefix is needed because this class has
        // PROPERTIES called Skills, Statuses, Heroes and Monsters, which shadow
        // the static classes of the same name. Without it the compiler resolves
        // "Skills.All" to the property and then to LINQ's All() extension, and
        // the error message it gives is genuinely baffling.
        _skills = global::Rpg.Core.Content.Skills.All.ToDictionary(s => s.Id);
        _statuses = global::Rpg.Core.Content.Statuses.All.ToDictionary(s => s.Id);
        _heroes = global::Rpg.Core.Content.Heroes.All.ToDictionary(h => h.Id);
        _monsters = global::Rpg.Core.Content.Monsters.All.ToDictionary(m => m.Id);
    }

    public static ContentDatabase CreateDefault() => new();

    // ---- lookups ----------------------------------------------------------

    public IReadOnlyDictionary<string, SkillDefinition> Skills => _skills;
    public IReadOnlyDictionary<string, StatusDefinition> Statuses => _statuses;
    public IReadOnlyList<HeroDefinition> Heroes => global::Rpg.Core.Content.Heroes.All;
    public IReadOnlyList<MonsterTemplate> Monsters => global::Rpg.Core.Content.Monsters.All;
    public IReadOnlyList<WeaponDefinition> Weapons => global::Rpg.Core.Content.Weapons.All;
    public IReadOnlyList<DungeonDefinition> Dungeons => global::Rpg.Core.Progression.Dungeons.All;

    public SkillDefinition Skill(string id) =>
        _skills.TryGetValue(id, out SkillDefinition? s) ? s
        : throw new KeyNotFoundException($"No skill with id '{id}'.");

    /// <summary>
    /// Finds a status by id.
    ///
    /// GOTCHA: the UI uses this to print a status's name. Define a new status but
    /// forget to add it to Statuses.All and combat works perfectly while the UI
    /// throws right here. The rules reach a status through the skill that applies
    /// it; only the DISPLAY needs this table.
    /// </summary>
    public StatusDefinition Status(string id) =>
        _statuses.TryGetValue(id, out StatusDefinition? s) ? s
        : throw new KeyNotFoundException($"No status with id '{id}'.");

    public HeroDefinition Hero(string id) =>
        _heroes.TryGetValue(id, out HeroDefinition? h) ? h
        : throw new KeyNotFoundException($"No hero with id '{id}'.");

    public MonsterTemplate Monster(string id) =>
        _monsters.TryGetValue(id, out MonsterTemplate? m) ? m
        : throw new KeyNotFoundException($"No monster with id '{id}'.");

    // ---- factories --------------------------------------------------------

    /// <summary>Builds a fresh hero at full health from their definition.</summary>
    public Actor CreateHero(string heroId)
    {
        HeroDefinition h = Hero(heroId);
        return new Actor(h.Id, h.Label, Team.Heroes, h.Stats,
            h.SkillIds.Select(Skill), h.SpriteName, h.VoiceFamily);
    }

    /// <summary>
    /// Spawns one monster from a template.
    ///
    /// <paramref name="actorId"/> must be unique within the battle;
    /// <paramref name="nameSuffix"/> distinguishes duplicates on screen
    /// ("Goblin A" / "Goblin B").
    /// </summary>
    public Actor CreateMonster(string templateId, string actorId, string nameSuffix = "")
    {
        MonsterTemplate m = Monster(templateId);
        return new Actor(actorId, m.Label + nameSuffix, Team.Monsters, m.Stats,
            m.SkillIds.Select(Skill), m.SpriteName, m.VoiceFamily);
    }

    // ========================================================================
    //  The frozen teaching fixture
    // ========================================================================

    /// <summary>
    /// One stand-alone fight: two heroes against two monsters.
    ///
    /// DELIBERATELY SELF-CONTAINED. It builds its own actors with its own
    /// numbers rather than pulling from the rosters above, so that adding a
    /// hero or retuning a goblin cannot change it.
    ///
    /// Why bother? Because this exact encounter is measured at a 74.9% win rate
    /// throughout the documentation, and BalanceHarnessTests asserts on it. A
    /// fixture that drifts every time content changes teaches nothing and fails
    /// constantly. This one is frozen on purpose.
    ///
    /// The real game is <see cref="Campaign"/>.
    /// </summary>
    public BattleState CreateDemoBattle(ulong seed)
    {
        var warrior = new Actor("hero_warrior", "Stick Warrior", Team.Heroes,
            new StatBlock(MaxHealth: 70, Attack: 15, Defense: 9, Speed: 10, CritChance: 10),
            new[] { Skill("slash"), Skill("heavy_blow"), Skill("guard") },
            "warrior", "human");

        var medic = new Actor("hero_medic", "Stick Medic", Team.Heroes,
            new StatBlock(MaxHealth: 52, Attack: 9, Defense: 6, Speed: 12, CritChance: 5),
            new[] { Skill("jab"), Skill("bandage"), Skill("poison_dart") },
            "cleric", "human");

        var goblin = new Actor("monster_goblin", "Goblin", Team.Monsters,
            new StatBlock(MaxHealth: 44, Attack: 14, Defense: 4, Speed: 14, CritChance: 8),
            new[] { Skill("club"), Skill("poison_dart") },
            "goblin_grunt", "goblin");

        var brute = new Actor("monster_brute", "Goblin Brute", Team.Monsters,
            new StatBlock(MaxHealth: 72, Attack: 18, Defense: 7, Speed: 7, CritChance: 8),
            new[] { Skill("club"), Skill("headbutt") },
            "orc_brute", "beast");

        return new BattleState(
            new[] { warrior, medic, goblin, brute },
            new SplitMix64Random(seed));
    }
}

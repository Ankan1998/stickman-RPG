// ============================================================================
//  ACTOR - one combatant in a fight
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A hero or a monster. Both are THIS SAME CLASS - the difference between the
//  Stick Warrior and a Goblin is entirely data: different numbers, different
//  skill list, different team.
//
//  Resist the urge to write "class Goblin : Actor". Down that road is a
//  forty-class inheritance tree that you cannot balance, cannot save to disk,
//  and cannot reason about. See docs/04-architecture.md.
//
//  It holds:
//    - who it is        (Id, Name, Team)
//    - how good it is   (BaseStats, and CurrentStats once buffs are counted)
//    - how hurt it is   (Health)
//    - what is on it    (Statuses - poison, stun, buffs)
//    - what it can do   (Skills, and which are on cooldown)
//
//  DESIGN NOTE: notice how carefully the outside world is kept away from the
//  internals. Health has a "private set". The status list is exposed read-only.
//  Cooldown methods are "internal". None of that is ceremony - it means damage
//  MUST go through TakeDamage(), which clamps it, so no bug anywhere in the
//  codebase can leave an actor on -12 health.
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Effects;

namespace Rpg.Core.Entities;

public sealed class Actor
{
    // C# note: "readonly" on a field means the field cannot be pointed at a
    // DIFFERENT list later. The contents can still change - we add and remove
    // statuses all the time.
    private readonly List<StatusEffect> _statuses = new();

    // Skill id -> turns remaining before it can be used again.
    private readonly Dictionary<string, int> _cooldowns = new();

    public Actor(string id, string name, Team team, StatBlock baseStats,
        IEnumerable<SkillDefinition> skills,
        string spriteName = "goblin_grunt", string voiceFamily = "human")
    {
        Id = id;
        Name = name;
        Team = team;
        BaseStats = baseStats;
        Skills = skills.ToArray();
        SpriteName = spriteName;
        VoiceFamily = voiceFamily;
        Health = baseStats.MaxHealth;   // everyone starts at full health
    }

    // ---- Identity ---------------------------------------------------------

    /// <summary>Unique within a battle. Events refer to actors by this, never by object.</summary>
    public string Id { get; }

    /// <summary>What the player sees.</summary>
    public string Name { get; }

    public Team Team { get; }

    /// <summary>Stats before any buff or debuff. Character-creation data. Never changes.</summary>
    public StatBlock BaseStats { get; }

    public IReadOnlyList<SkillDefinition> Skills { get; }

    /// <summary>
    /// Which sprite folder to draw, e.g. "warrior" -> warrior_idle_strip.png.
    ///
    /// Presentation data living on a rules object is a small, deliberate
    /// compromise. The alternative - a lookup table in the Godot layer mapping
    /// ids to sprites - drifts out of sync the moment anyone adds a monster.
    /// This is a NAME, not a texture; Rpg.Core still has no idea what a texture
    /// is and still compiles without an engine.
    /// </summary>
    public string SpriteName { get; }

    /// <summary>Which set of hurt/death cries to use: human, goblin, undead, beast, demon, golem, slime, skeleton.</summary>
    public string VoiceFamily { get; }

    /// <summary>The weapon they carry, or null. Folded into CurrentStats.</summary>
    public WeaponDefinition? Weapon { get; private set; }

    /// <summary>Puts a weapon in their hands, returning whatever they were holding.</summary>
    public WeaponDefinition? Equip(WeaponDefinition? weapon)
    {
        WeaponDefinition? previous = Weapon;
        Weapon = weapon;

        // Some weapons add MaxHealth, so equipping one must not leave the wearer
        // above their new maximum or below their old current. Clamping here keeps
        // that invariant no matter what order things are equipped in.
        Health = Math.Clamp(Health, 0, MaxHealth);
        return previous;
    }

    // ---- Live state -------------------------------------------------------

    // C# note: "{ get; private set; }" = anyone may read Health, only code
    // inside this class may write it. This is the whole reason TakeDamage and
    // Heal below can guarantee their rules hold.
    public int Health { get; private set; }

    // C# note: the field is a mutable List, but we hand out an IReadOnlyList.
    // Outside code can loop over and count statuses but cannot .Add() one -
    // statuses must go through ApplyStatus(), which handles the refresh rule.
    public IReadOnlyList<StatusEffect> Statuses => _statuses;

    public bool IsAlive => Health > 0;

    /// <summary>
    /// Base stats PLUS every active status modifier, recalculated every time you
    /// read it.
    ///
    /// This is why buffs "just work" everywhere without any code remembering to
    /// recalculate anything. Combat always reads CurrentStats, never BaseStats.
    /// If you find yourself reaching for BaseStats outside character creation,
    /// that is usually a bug: it means some buff or debuff is being silently
    /// ignored.
    /// </summary>
    public StatBlock CurrentStats
    {
        get
        {
            StatBlock total = BaseStats;

            // Equipment first, then conditions. Order does not matter - addition
            // is addition - but reading it in this order matches how a player
            // thinks about it: "my gear, then whatever is happening to me".
            if (Weapon is not null)
                total += Weapon.Bonus;

            // "+=" works on StatBlock because StatBlock defines the + operator.
            // See StatBlock.cs.
            foreach (StatusEffect status in _statuses)
                total += status.Definition.Modifier;

            return total.Clamped();   // stop debuffs pushing anything negative
        }
    }

    public int MaxHealth => CurrentStats.MaxHealth;

    /// <summary>False if stunned, frozen, asleep - anything with PreventsAction.</summary>
    public bool CanAct => IsAlive && !_statuses.Any(s => s.Definition.PreventsAction);

    /// <summary>Why this actor cannot act, for the combat log. Null if they can act.</summary>
    // C# note: "?." means "if the left side is null, the whole thing is null".
    // FirstOrDefault returns null when nothing matches, so this reads as
    // "the name of the first blocking status, or nothing".
    public string? BlockedReason => _statuses.FirstOrDefault(s => s.Definition.PreventsAction)?.Definition.Name;

    // ---- Health -----------------------------------------------------------

    /// <summary>
    /// Applies damage and returns the amount ACTUALLY lost, which is never more
    /// than the health remaining. Returning the real number matters: the combat
    /// log should say "8 damage" when an 8 HP goblin is hit for 40, not "40".
    /// </summary>
    public int TakeDamage(int amount)
    {
        int applied = Math.Clamp(amount, 0, Health);
        Health -= applied;
        return applied;
    }

    /// <summary>
    /// Restores health, returning the amount actually healed. Cannot overheal,
    /// and cannot revive the dead - resurrection should be an explicit,
    /// deliberate mechanic, not an accident of a stray heal.
    /// </summary>
    public int Heal(int amount)
    {
        if (!IsAlive) return 0;

        int applied = Math.Clamp(amount, 0, MaxHealth - Health);
        Health += applied;
        return applied;
    }

    // ---- Statuses ---------------------------------------------------------

    /// <summary>
    /// Applies a status. If the actor already has it, the duration is REFRESHED
    /// rather than a second copy being stacked.
    ///
    /// That is a design decision, not a technical one. If you want stacking
    /// poison, add a Stacks field to StatusEffect and change this method.
    /// </summary>
    public StatusEffect ApplyStatus(StatusDefinition definition, int turns)
    {
        StatusEffect? existing = _statuses.FirstOrDefault(s => s.Id == definition.Id);

        // C# note: "is not null" is the modern way to write "!= null".
        if (existing is not null)
        {
            existing.Refresh(turns);
            return existing;
        }

        var effect = new StatusEffect(definition, turns);
        _statuses.Add(effect);
        return effect;
    }

    public bool HasStatus(string statusId) => _statuses.Any(s => s.Id == statusId);

    // C# note: "internal" means only code in THIS project (Rpg.Core) may call
    // it. Battle can; the Godot layer physically cannot. The architecture rule
    // is enforced by the compiler rather than by good intentions.
    internal void RemoveStatus(StatusEffect effect) => _statuses.Remove(effect);

    // ---- Cooldowns --------------------------------------------------------

    /// <summary>Can this skill be used right now?</summary>
    public bool IsSkillReady(string skillId) => TurnsUntilReady(skillId) == 0;

    /// <summary>Turns remaining before the skill is usable. 0 means ready.</summary>
    // C# note: "TryGetValue" is the standard dictionary lookup that does not
    // throw when the key is missing. Skills with no entry are simply ready.
    public int TurnsUntilReady(string skillId) =>
        _cooldowns.TryGetValue(skillId, out int turns) ? turns : 0;

    /// <summary>Called by SkillAction the moment a skill is used.</summary>
    internal void PutOnCooldown(SkillDefinition skill)
    {
        if (skill.Cooldown > 0)
            _cooldowns[skill.Id] = skill.Cooldown;
    }

    /// <summary>
    /// Called once at the START of this actor's turn, by Battle. Ticking at the
    /// start (rather than the end) is what makes "cooldown 2" mean "you miss
    /// two of your own turns".
    /// </summary>
    internal void TickCooldowns()
    {
        // ".ToList()" again snapshots the keys, because we remove entries while
        // looping over them.
        foreach (string id in _cooldowns.Keys.ToList())
        {
            if (--_cooldowns[id] <= 0)
                _cooldowns.Remove(id);
        }
    }

    // ---- between fights ---------------------------------------------------

    /// <summary>
    /// Clears statuses and cooldowns, ready for the next fight in a run.
    ///
    /// Deliberately does NOT restore health. Wounds carrying over between waves
    /// is the single rule that turns three separate fights into a game - see
    /// Progression/Run.cs.
    /// </summary>
    internal void ResetForNextBattle()
    {
        _statuses.Clear();
        _cooldowns.Clear();
    }

    /// <summary>
    /// Brings a fallen hero back with the given health, between waves of a run.
    ///
    /// Heal() deliberately refuses to revive the dead - resurrection should be an
    /// explicit, deliberate act rather than something a stray heal can do by
    /// accident. This is that explicit act.
    ///
    /// It also matters mechanically: an actor left dead inside the next battle
    /// would be re-announced as dying the moment that battle checked for deaths.
    /// </summary>
    internal void ReviveWith(int health)
    {
        if (IsAlive) return;
        Health = Math.Clamp(health, 1, MaxHealth);
    }

    /// <summary>Handy in the debugger and in test failure messages.</summary>
    public override string ToString() => $"{Name} ({Health}/{MaxHealth} HP)";
}

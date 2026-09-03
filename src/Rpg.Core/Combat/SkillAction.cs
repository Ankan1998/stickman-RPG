// ============================================================================
//  SKILLACTION - using a skill on a target
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  This one class executes EVERY skill in the game. Slash, Heavy Blow, Bandage,
//  Poison Dart and Guard all run through the code below; they differ only in the
//  numbers on their SkillDefinition.
//
//  If you ever find yourself about to write "class FireballAction : IAction",
//  stop and ask whether the skill DEFINITION needs one more data field instead.
//  Nine times out of ten it does. The tenth time is when you add a new generic
//  mechanic here that every skill can then opt into.
//
//  See docs/04-architecture.md#content-is-data-not-code.
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Rng;   // needed for the Chance() extension method - see below

namespace Rpg.Core.Combat;

public sealed class SkillAction : IAction
{
    public SkillAction(Actor actor, SkillDefinition skill, Actor target)
    {
        Actor = actor;
        Skill = skill;
        Target = target;
    }

    /// <summary>Who is using the skill.</summary>
    public Actor Actor { get; }

    /// <summary>Which skill. This is the DATA that decides what happens below.</summary>
    public SkillDefinition Skill { get; }

    /// <summary>Who it is pointed at. May be the user themselves.</summary>
    public Actor Target { get; }

    /// <summary>The text on the button, e.g. "Heavy Blow -&gt; Goblin".</summary>
    // C# note: "?:" is the ternary operator - condition ? ifTrue : ifFalse.
    // Self-targeted skills just show their name, with no arrow.
    public string Label => Target.Id == Actor.Id
        ? Skill.Name
        : Skill.Name + " -> " + Target.Name;

    /// <summary>
    /// Does the thing. Appends everything that happened to <paramref name="log"/>.
    /// </summary>
    public void Execute(BattleState state, List<GameEvent> log)
    {
        log.Add(new SkillUsed(Actor.Id, Skill.Id, Target.Id));

        // Start the cooldown immediately, whether or not the skill does anything
        // useful. Missing still costs you the turn.
        Actor.PutOnCooldown(Skill);

        // ---- Damage ----
        if (Skill.DealsDamage)
        {
            // Roll the crit BEFORE computing damage, and always in this order.
            //
            // Consuming the random number generator in a fixed order is what
            // makes a seed reproduce a battle exactly. Swap these two lines and
            // every saved replay and every determinism test breaks.
            //
            // C# note: Chance() is an EXTENSION METHOD defined in
            // Rpg.Core/Rng/IRandomSource.cs. It only compiles because of the
            // "using Rpg.Core.Rng;" at the top of this file - that exact missing
            // using was the first build error this project ever had.
            bool isCritical = state.Random.Chance(Actor.CurrentStats.CritChance);

            int damage = DamageCalculator.Compute(
                Actor.CurrentStats,     // note: CurrentStats, so buffs count
                Target.CurrentStats,
                Skill.Power,
                isCritical);

            // TakeDamage clamps and returns what was ACTUALLY lost, so hitting an
            // 8 HP goblin for 40 correctly reports 8 in the combat log.
            int applied = Target.TakeDamage(damage);
            log.Add(new Damaged(Target.Id, applied, isCritical, SourceId: Actor.Id));

            // Life drain: a slice of what we just dealt comes back as health.
            // Note it is based on damage ACTUALLY dealt, so draining a nearly
            // dead target heals you for very little - overkill is wasted here
            // too, which is a nice bit of consistency for free.
            if (Skill.Drains)
            {
                int drained = Actor.Heal(applied * Skill.LifestealPercent / 100);
                if (drained > 0)
                    log.Add(new Healed(Actor.Id, drained));
            }
        }

        // ---- Healing ----
        // A skill can both damage and heal - these are separate "if"s, not an
        // "else if". Life drain would set both Power and Healing.
        if (Skill.Heals)
        {
            int healed = Target.Heal(Skill.Healing);

            // Only report healing that actually happened. Healing a target who
            // is already full restores 0, and logging that produced a "+0
            // health" line and a pause in the replay for an event where nothing
            // observable occurred. The drain branch above already guarded this;
            // the two are now consistent.
            if (healed > 0)
                log.Add(new Healed(Target.Id, healed));
        }

        // ---- Status ----
        // A status only lands if the target survived the hit. Poisoning a corpse
        // is the kind of small detail that produces baffling combat logs later.
        //
        // C# note: "is { } status" is a pattern that matches ANY non-null value
        // and names it `status`. It is the compact way to write
        // "if this optional has something in it, unwrap it".
        if (Skill.AppliesStatus is { } status && Target.IsAlive)
        {
            Target.ApplyStatus(status, Skill.StatusTurns);
            log.Add(new StatusApplied(Target.Id, status.Id, Skill.StatusTurns));
        }
    }
}

// ============================================================================
//  RUNSTATS - the scoreboard for one complete run
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Everything the results screen shows: damage dealt, damage taken, crits
//  landed, enemies killed, turns lost to stun, and so on.
//
//  THE INTERESTING PART: none of this is tracked by the combat code. Battle.cs
//  has no idea these numbers exist. Instead this class READS THE EVENT LOG that
//  every turn already produces, and counts what it sees.
//
//  That is why adding a new statistic costs nothing and risks nothing - you add
//  a counter and one line in Observe(), and you cannot possibly break combat by
//  doing it. It is also why the numbers can never disagree with what actually
//  happened on screen: both are reading the same recording.
//
//  This pattern has a name - event sourcing - and this is the smallest useful
//  example of why people like it. See docs/04-architecture.md.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Entities;

namespace Rpg.Core.Progression;

public sealed class RunStats
{
    // ---- what you achieved ------------------------------------------------
    public int EncountersCleared { get; internal set; }
    public int DungeonsCleared { get; internal set; }
    public int EnemiesDefeated { get; internal set; }
    public int HeroesLost { get; internal set; }
    public int RoundsFought { get; internal set; }

    // ---- how the fighting went -------------------------------------------
    public int DamageDealt { get; internal set; }
    public int DamageTaken { get; internal set; }
    public int HealingDone { get; internal set; }
    public int BiggestHit { get; internal set; }
    public int CriticalHits { get; internal set; }

    // ---- how you played ---------------------------------------------------
    public int SkillsUsed { get; internal set; }
    public int StatusesApplied { get; internal set; }
    public int TurnsLostToStun { get; internal set; }

    /// <summary>
    /// Folds one battle's events into the running totals.
    ///
    /// <paramref name="teamOf"/> tells us which side an actor was on, which is
    /// the only thing an event id cannot tell us by itself.
    /// </summary>
    internal void Observe(IEnumerable<GameEvent> events, Func<string, Team> teamOf)
    {
        foreach (GameEvent gameEvent in events)
        {
            switch (gameEvent)
            {
                case Damaged d:
                    // "Dealt" vs "taken" is decided by whose health went down.
                    if (teamOf(d.ActorId) == Team.Heroes)
                        DamageTaken += d.Amount;
                    else
                    {
                        DamageDealt += d.Amount;
                        if (d.Amount > BiggestHit) BiggestHit = d.Amount;
                    }

                    if (d.IsCritical) CriticalHits++;
                    break;

                case Healed h when teamOf(h.ActorId) == Team.Heroes:
                    HealingDone += h.Amount;
                    break;

                case SkillUsed s when teamOf(s.ActorId) == Team.Heroes:
                    SkillsUsed++;
                    break;

                case StatusApplied s when teamOf(s.ActorId) == Team.Monsters:
                    StatusesApplied++;
                    break;

                case TurnSkipped t when teamOf(t.ActorId) == Team.Heroes && t.Reason != "Waiting":
                    TurnsLostToStun++;
                    break;

                case Died died:
                    if (teamOf(died.ActorId) == Team.Heroes) HeroesLost++;
                    else EnemiesDefeated++;
                    break;
            }
        }
    }
}

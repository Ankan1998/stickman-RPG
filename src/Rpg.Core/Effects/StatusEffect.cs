// ============================================================================
//  STATUSEFFECT - poison ON a specific actor
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The difference between this and StatusDefinition matters, and it is a pattern
//  you will use constantly in game code:
//
//      StatusDefinition   "what poison is"        shared, immutable, one in the
//                                                 whole game
//
//      StatusEffect       "this goblin is         one per affected actor,
//                          poisoned, 2 turns      mutable, because the countdown
//                          left"                  changes
//
//  Ten poisoned goblins means ten StatusEffect objects, all pointing at the same
//  single StatusDefinition. The template holds the rules; the instance holds the
//  bookkeeping.
// ============================================================================

namespace Rpg.Core.Effects;

public sealed class StatusEffect
{
    public StatusEffect(StatusDefinition definition, int turns)
    {
        Definition = definition;
        RemainingTurns = turns;
    }

    /// <summary>The shared template. What this status actually does.</summary>
    public StatusDefinition Definition { get; }

    /// <summary>Turns left before it wears off.</summary>
    public int RemainingTurns { get; private set; }

    /// <summary>Convenience shortcut to the definition's id.</summary>
    public string Id => Definition.Id;

    public bool IsExpired => RemainingTurns <= 0;

    /// <summary>
    /// Re-applying a status REFRESHES its duration rather than stacking a second
    /// copy. Math.Max means a fresh 3-turn poison will not shorten an existing
    /// 5-turn one.
    ///
    /// This is a design decision, not a technical one. If you want stacking
    /// poison, add a Stacks field here and have Battle.TickStatuses multiply by
    /// it.
    /// </summary>
    public void Refresh(int turns) => RemainingTurns = Math.Max(RemainingTurns, turns);

    /// <summary>Called once at the end of the bearer's turn, by Battle.TickStatuses.</summary>
    public void Tick() => RemainingTurns--;
}

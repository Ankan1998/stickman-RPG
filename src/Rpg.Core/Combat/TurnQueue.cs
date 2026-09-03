// ============================================================================
//  TURNQUEUE - who acts, and in what order
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Round-based: everyone still alive acts once per round, fastest first. When
//  everybody has had a turn, the round ends and a new one begins.
//
//  It is the simplest scheme that still makes Speed a meaningful stat, and it is
//  trivial to show the player.
//
//  UPGRADE PATH
//  ------------
//  When you want more depth, replace this with an ATB / "action gauge" system:
//  each actor accumulates Speed points every tick and acts when they cross a
//  threshold. Fast actors then genuinely get EXTRA turns instead of merely
//  acting first. (This is how Final Fantasy IV-IX work.)
//
//  Nothing outside this class would need to change - which is exactly why turn
//  order lives behind its own type instead of being a for-loop inside Battle.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public sealed class TurnQueue
{
    // This round's running order, decided once at the start of the round.
    private readonly List<Actor> _order = new();

    // Where we are in that list. -1 means "before the first actor".
    private int _index = -1;

    /// <summary>Which round we are on. Starts at 1 after the first BeginRound.</summary>
    public int Round { get; private set; }

    /// <summary>Whose turn it is, or null if the round has not started / has finished.</summary>
    public Actor? Current => _index >= 0 && _index < _order.Count ? _order[_index] : null;

    /// <summary>This round's full running order, for showing the player what is coming.</summary>
    public IReadOnlyList<Actor> Order => _order;

    /// <summary>
    /// Works out the running order for a new round: everyone alive, sorted by
    /// Speed, highest first.
    /// </summary>
    public void BeginRound(IEnumerable<Actor> actors)
    {
        Round++;
        _order.Clear();

        // C# note: this is LINQ. Read it top to bottom as a sentence -
        // "take the actors, keep the living ones, sort by speed descending,
        //  break ties alphabetically by id".
        _order.AddRange(actors
            .Where(a => a.IsAlive)

            // CurrentStats, not BaseStats - so a Haste buff genuinely moves you
            // up the order. There is a test for this: SpeedBuffsActuallyChangeTheOrder.
            .OrderByDescending(a => a.CurrentStats.Speed)

            // Tie-break by id, NEVER by position in the list.
            //
            // This looks fussy and is load-bearing. If ties fell back to list
            // order, then loading a save that happened to rebuild the actor list
            // in a different order would replay the whole battle differently -
            // a genuinely horrible bug to track down.
            .ThenBy(a => a.Id, StringComparer.Ordinal));

        _index = -1;
    }

    /// <summary>
    /// Advances to the next actor who is still alive. Returns false when the
    /// round is over.
    /// </summary>
    public bool MoveNext()
    {
        // We re-check IsAlive here as well as in BeginRound, because an actor
        // can be killed DURING the round, after the order was already decided.
        // A corpse must not get a turn.
        //
        // C# note: "++_index" increments first, then uses the new value.
        while (++_index < _order.Count)
        {
            if (_order[_index].IsAlive)
                return true;
        }

        return false;
    }
}

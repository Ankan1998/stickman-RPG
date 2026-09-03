// ============================================================================
//  BATTLESTATE - who is in this fight, and the dice
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A deliberately dumb container. It holds the list of actors and the random
//  number source, and answers three simple questions about them.
//
//  The RULES live in Battle.cs, not here. Keeping "the data" and "the rules that
//  operate on the data" apart is what stops this class quietly growing into a
//  three-thousand-line god object.
//
//  WHY THE RANDOM SOURCE LIVES HERE
//  --------------------------------
//  Because it is part of the fight's identity. Create a BattleState with seed
//  4242 and the battle will play out identically every single time - which
//  gives you reproducible tests, reproducible bug reports, replays, and
//  roguelike-style seeded runs, all for free. See Rng/IRandomSource.cs.
// ============================================================================

using Rpg.Core.Entities;
using Rpg.Core.Rng;

namespace Rpg.Core.Combat;

public sealed class BattleState
{
    private readonly List<Actor> _actors;

    public BattleState(IEnumerable<Actor> actors, IRandomSource rng)
    {
        _actors = actors.ToList();
        Random = rng;

        // Ids must be unique, because every GameEvent refers to actors by id.
        // Two actors sharing one would silently corrupt everything downstream -
        // the log, the UI, saves, replays. Far better to explode right here,
        // loudly, at construction time.
        var duplicate = Actors.GroupBy(a => a.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException(
                $"Two actors share the id '{duplicate.Key}'. Ids must be unique - events reference actors by id.");
    }

    /// <summary>
    /// Everyone in the fight, dead or alive, both teams - IN MARCHING ORDER.
    ///
    /// The order is the formation: earlier entries stand further forward. That
    /// is why it is a list and not a set, and why SwapPositions exists.
    /// </summary>
    public IReadOnlyList<Actor> Actors => _actors;

    /// <summary>Every dice roll in this battle comes from here.</summary>
    public IRandomSource Random { get; }

    /// <summary>Finds an actor by id. Throws if there is no such actor.</summary>
    public Actor GetActor(string id) =>
        Actors.FirstOrDefault(a => a.Id == id)
        ?? throw new KeyNotFoundException($"No actor with id '{id}' in this battle.");
        // C# note: "??" means "use the left side, unless it is null, then the
        // right side". FirstOrDefault returns null when nothing matches.

    /// <summary>Everyone on the given team who is still standing.</summary>
    public IEnumerable<Actor> LivingMembersOf(Team team) =>
        Actors.Where(a => a.Team == team && a.IsAlive);

    /// <summary>True if that whole team is dead. This is the lose condition.</summary>
    public bool IsTeamWipedOut(Team team) => !LivingMembersOf(team).Any();

    /// <summary>
    /// Which position this actor is standing in: 1 is the front rank, closest to
    /// the enemy. Returns 0 for the dead.
    ///
    /// RANKS CLOSE UP. The position is computed from the order the actors were
    /// added, skipping anyone who has fallen - so killing the enemy's front rank
    /// pulls the one behind it into reach of your melee, and losing your own
    /// front-liner shoves your Mage into the front line where half her spells
    /// stop working.
    ///
    /// Computing it on demand rather than storing it means it can never drift
    /// out of step with who is actually alive.
    /// </summary>
    public int RankOf(Actor actor)
    {
        if (!actor.IsAlive) return 0;

        int rank = 0;
        foreach (Actor a in Actors)
        {
            if (a.Team != actor.Team || !a.IsAlive) continue;
            rank++;
            if (ReferenceEquals(a, actor)) return rank;
        }
        return 0;
    }

    /// <summary>Everyone on a team, in marching order, front first.</summary>
    public IEnumerable<Actor> FormationOf(Team team) =>
        Actors.Where(a => a.Team == team && a.IsAlive);

    /// <summary>
    /// Swaps two fighters' places in the line. Used by MoveAction.
    ///
    /// Internal because shuffling the formation is a RULE, and rules belong to
    /// actions - the Godot layer must not be able to rearrange the battlefield.
    /// </summary>
    internal void SwapPositions(Actor a, Actor b)
    {
        int i = _actors.IndexOf(a), j = _actors.IndexOf(b);
        if (i < 0 || j < 0) return;
        (_actors[i], _actors[j]) = (_actors[j], _actors[i]);
    }

    /// <summary>
    /// The living ally standing directly in front of (offset -1) or behind
    /// (offset +1) this actor, or null at the end of the line.
    /// </summary>
    public Actor? NeighbourOf(Actor actor, int offset)
    {
        List<Actor> line = FormationOf(actor.Team).ToList();
        int index = line.IndexOf(actor);
        if (index < 0) return null;

        int target = index + offset;
        return target >= 0 && target < line.Count ? line[target] : null;
    }
}

// ============================================================================
//  MOVEACTION - swap places in the line
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Step forward, or step back, trading places with the ally next to you. It
//  costs your whole turn.
//
//  WHY THIS HAS TO EXIST
//  ---------------------
//  Once skills care about position, a fighter can end up somewhere none of their
//  skills work. Kill the party's front-liner and the Mage is shoved into rank
//  one, where a spell needing "-###" cannot be cast at all.
//
//  Without a way to move, that hero is simply dead weight for the rest of the
//  fight, and the player can do nothing about it. That is not tension, it is a
//  dead end. With it, the same situation becomes a decision: spend a turn
//  getting back where you belong, or squeeze out one bad attack from the wrong
//  rank?
//
//  Darkest Dungeon solves this the same way - shuffling is a real move, and
//  losing your formation is a crisis you play through rather than lose to.
//
//  COSTING A FULL TURN IS THE POINT
//  --------------------------------
//  Free repositioning would make the formation meaningless; you would simply
//  slide into the perfect spot every turn. Costing a turn is what keeps the
//  marching order a decision you make BEFORE the fight.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public sealed class MoveAction : IAction
{
    private readonly Actor _partner;
    private readonly bool _forward;

    public MoveAction(Actor actor, Actor partner, bool forward)
    {
        Actor = actor;
        _partner = partner;
        _forward = forward;
    }

    public Actor Actor { get; }

    public string Label => _forward
        ? $"Step forward (swap with {_partner.Name})"
        : $"Step back (swap with {_partner.Name})";

    public void Execute(BattleState state, List<GameEvent> log)
    {
        state.SwapPositions(Actor, _partner);
        log.Add(new Repositioned(Actor.Id, _partner.Id, state.RankOf(Actor)));
    }
}

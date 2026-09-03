// ============================================================================
//  PASSACTION - do nothing this turn
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The "Wait" button. It exists for one structural reason: it guarantees
//  Battle.LegalActions() can never return an empty list.
//
//  Without it, an actor who is stunned, or whose every skill happens to be on
//  cooldown, would have no legal move at all. The UI would then have no buttons
//  to show and the game would simply sit there forever, with no error message
//  and nothing in a log to explain it. That is an engine you debug at 2am.
//
//  The general lesson: make the degenerate case legal rather than impossible.
//  It is nearly free and it removes a whole category of hangs.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public sealed class PassAction : IAction
{
    public PassAction(Actor actor) => Actor = actor;

    public Actor Actor { get; }

    public string Label => "Wait";

    // The "state" parameter is unused - we need it because IAction requires this
    // exact signature, and every other action does need it.
    public void Execute(BattleState state, List<GameEvent> log) =>
        log.Add(new TurnSkipped(Actor.Id, "Waiting"));
}

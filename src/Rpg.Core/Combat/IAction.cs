// ============================================================================
//  IACTION - "something an actor chose to do on their turn"
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A move, wrapped in an object. There are two kinds in this project:
//  SkillAction (use a skill on someone) and PassAction (do nothing).
//
//  The player picks one from a menu of buttons. The AI scores them all and picks
//  the best. Both then hand it to the same Battle.TakeTurn(), which means the AI
//  can never do something the player could not. That property is worth
//  protecting - it is what makes a turn-based game feel fair rather than rigged.
//
//  This is the classic "Command pattern": bundling an operation into a value you
//  can store, pass around, score, sort, or later undo.
//
//  C# NOTE
//  -------
//  An "interface" is a contract with no implementation. Any class that says
//  ": IAction" promises to provide these three members. The "I" prefix is a
//  near-universal C# convention.
//
//  WHY EXECUTE TAKES THE LOG AS A PARAMETER
//  ----------------------------------------
//  The obvious alternative looks tidier:
//
//      IEnumerable<GameEvent> Execute(BattleState state);   // NOT what we do
//
//  ...but in C#, a method that builds a sequence with "yield return" does
//  NOTHING AT ALL until somebody loops over the result. Forget to enumerate it
//  and your attack silently does no damage, with no error anywhere. That is a
//  genuinely nasty bug to find in combat code, so we pass in a list and append
//  to it instead. Boring, obvious, always runs.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public interface IAction
{
    /// <summary>Who is doing it.</summary>
    Actor Actor { get; }

    /// <summary>Human-readable, for menu buttons and combat logs.</summary>
    string Label { get; }

    /// <summary>
    /// Carry it out, appending everything that happened to <paramref name="log"/>.
    /// </summary>
    void Execute(BattleState state, List<GameEvent> log);
}

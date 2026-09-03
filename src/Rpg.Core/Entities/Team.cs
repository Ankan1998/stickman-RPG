// ============================================================================
//  TEAM - which side you are on
// ============================================================================
//
//  Two sides, and a helper for "who are my enemies?".
//
//  C# NOTE
//  -------
//  An "enum" is a fixed set of named values. You write Team.Heroes, and the
//  compiler will reject Team.Bananas.
//
//  Elsewhere you will see "Team?" with a question mark, meaning "a Team, or
//  nothing" - used for Battle.Winner, where "nobody has won yet" and "it was a
//  draw" both need to be representable.
// ============================================================================

namespace Rpg.Core.Entities;

public enum Team
{
    Heroes,
    Monsters,
}

public static class TeamExtensions
{
    /// <summary>
    /// The other side. Lets targeting code ask "who are this actor's enemies?"
    /// without an if-statement, and means adding a third team later would touch
    /// one method rather than twenty.
    /// </summary>
    public static Team Opposite(this Team team) =>
        team == Team.Heroes ? Team.Monsters : Team.Heroes;
}

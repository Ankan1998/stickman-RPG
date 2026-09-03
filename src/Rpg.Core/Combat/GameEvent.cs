// ============================================================================
//  GAMEEVENT - the vocabulary for describing what happened
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  These are the ONLY things that cross the line between "the rules" and "the
//  screen". Combat never draws anything; it builds a list of these instead, and
//  the Godot layer replays that list slowly with pauses and animation.
//
//  A single turn might produce:
//
//      SkillUsed("hero_warrior", "heavy_blow", "monster_goblin")
//      Damaged("monster_goblin", 25, IsCritical: false)
//      Died("monster_goblin")
//      TurnStarted("monster_brute")
//
//  WHY THIS MATTERS MORE THAN IT LOOKS
//  -----------------------------------
//   - The simulation becomes testable, because it has no timing and no drawing.
//   - You can fast-forward, or run 10,000 battles headless to check balance.
//   - Replays, undo, combat logs and networked play all become easy LATER,
//     because this list already IS the replay format.
//
//  TWO DESIGN DECISIONS WORTH COPYING
//  ----------------------------------
//  1. Events carry actor IDs (strings), NOT Actor references. That keeps them
//     serialisable - you can write them to a file or send them over a socket.
//     It is the difference between "we could add replays later" and "we could
//     not".
//
//  2. They are records, so a list of them compares by VALUE. That is what makes
//     this test possible:
//
//         Assert.Equal(RunBattle(seed: 42).Log, RunBattle(seed: 42).Log);
//
//     With ordinary classes that would compare memory addresses and always fail.
//
//  C# NOTES
//  --------
//  "abstract record GameEvent;" - note the semicolon and no body. It exists
//  purely as "the thing all events have in common", so a List<GameEvent> can
//  hold a mix of all the types below.
//
//  "sealed record Damaged(string ActorId, int Amount, bool IsCritical)" declares
//  a whole class in one line: constructor, three read-only properties, equality,
//  and a readable ToString(). See docs/02-csharp-crash-course.md#records.
// ============================================================================

using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

/// <summary>Base type for everything that can happen in a battle.</summary>
public abstract record GameEvent;

// ---- Structure of the fight ------------------------------------------------

/// <summary>The fight has begun.</summary>
public sealed record BattleStarted : GameEvent;

/// <summary>A new round started. Everyone alive acts once per round.</summary>
public sealed record RoundStarted(int Round) : GameEvent;

/// <summary>It is now this actor's turn.</summary>
public sealed record TurnStarted(string ActorId) : GameEvent;

// ---- Things actors do ------------------------------------------------------

/// <summary>Someone used a skill. TargetId equals ActorId for self-targeted skills.</summary>
public sealed record SkillUsed(string ActorId, string SkillId, string TargetId) : GameEvent;

/// <summary>
/// Someone lost health, from a hit or from poison. Amount is the health ACTUALLY
/// lost, so hitting an 8 HP goblin for 40 reports 8.
///
/// WHERE IT CAME FROM
/// ------------------
/// Damage arrives from two completely different places, and anything replaying
/// the log has to tell them apart:
///
///     SourceId   who swung. Null when nobody did - poison has no attacker.
///     StatusId   which status burned them, for damage that TICKED rather than
///                landed. Null for an ordinary blow.
///
/// Exactly one of the two is set. They exist because the alternative - having
/// the screen ask the battle "whose turn is it?" - is quietly WRONG: by the time
/// a log is replayed the turn has already advanced, so that reads the next
/// fighter's weapon rather than the one that actually hit.
/// </summary>
public sealed record Damaged(
    string ActorId,
    int Amount,
    bool IsCritical,
    string? SourceId = null,
    string? StatusId = null) : GameEvent;

/// <summary>Someone gained health. Amount is what was actually restored, never overhealing.</summary>
public sealed record Healed(string ActorId, int Amount) : GameEvent;

// ---- Statuses --------------------------------------------------------------

/// <summary>A status landed on someone.</summary>
public sealed record StatusApplied(string ActorId, string StatusId, int Turns) : GameEvent;

/// <summary>A status counted down by one. Emitted every turn, so the UI ignores it as noise.</summary>
public sealed record StatusTicked(string ActorId, string StatusId, int RemainingTurns) : GameEvent;

/// <summary>A status ran out and was removed.</summary>
public sealed record StatusExpired(string ActorId, string StatusId) : GameEvent;

// ---- Endings ---------------------------------------------------------------

/// <summary>The actor could not act - stunned, asleep, or they chose to Wait.</summary>
public sealed record TurnSkipped(string ActorId, string Reason) : GameEvent;

/// <summary>Two allies swapped places in the line. Ranks changed.</summary>
public sealed record Repositioned(string ActorId, string SwappedWithId, int NewRank) : GameEvent;

/// <summary>Someone hit zero health. Emitted exactly once per actor, ever.</summary>
public sealed record Died(string ActorId) : GameEvent;

/// <summary>The fight is over. Winner is null on a draw (both sides wiped, or the round limit).</summary>
public sealed record BattleEnded(Team? Winner) : GameEvent;

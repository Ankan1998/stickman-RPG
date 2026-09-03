// ============================================================================
//  TURNORDERTESTS - who acts, and in what order
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Four tests on TurnQueue:
//
//    1. Faster actors go first.
//    2. Ties break on id, so the order is reproducible. This one looks trivial
//       and is not - see the comment inside it.
//    3. A Haste buff genuinely moves you up the order, proving the queue reads
//       CurrentStats rather than BaseStats.
//    4. An actor killed mid-round is skipped rather than acting from beyond the
//       grave.
//
//  Note these test TurnQueue DIRECTLY, without a Battle at all. Small classes
//  with one job are easy to test, which is a large part of why turn order lives
//  in its own class.
// ============================================================================

using Rpg.Core.Combat;
using Rpg.Core.Effects;
using Rpg.Core.Entities;
using Xunit;
using static Rpg.Core.Tests.TestFixtures;

namespace Rpg.Core.Tests;

public sealed class TurnOrderTests
{
    private static List<string> Drain(TurnQueue queue)
    {
        var ids = new List<string>();
        while (queue.MoveNext())
            ids.Add(queue.Current!.Id);
        return ids;
    }

    [Fact]
    public void ActorsActInDescendingSpeedOrder()
    {
        Actor slow = MakeActor("slow", Team.Heroes, Stats(spd: 5));
        Actor fast = MakeActor("fast", Team.Heroes, Stats(spd: 30));
        Actor mid = MakeActor("mid", Team.Monsters, Stats(spd: 15));

        var queue = new TurnQueue();
        queue.BeginRound(new[] { slow, fast, mid });

        Assert.Equal(new[] { "fast", "mid", "slow" }, Drain(queue));
    }

    [Fact]
    public void EqualSpeedsBreakOnIdSoTheOrderIsReproducible()
    {
        // Passed in deliberately out of order. If ties fell back to list
        // position, a save/load that rebuilt the list differently would replay
        // the battle differently - a genuinely horrible bug to track down.
        Actor b = MakeActor("b", Team.Heroes, Stats(spd: 10));
        Actor a = MakeActor("a", Team.Heroes, Stats(spd: 10));

        var queue = new TurnQueue();
        queue.BeginRound(new[] { b, a });

        Assert.Equal(new[] { "a", "b" }, Drain(queue));
    }

    [Fact]
    public void SpeedBuffsActuallyChangeTheOrder()
    {
        // Proof that the queue reads CurrentStats, not BaseStats.
        Actor slow = MakeActor("slow", Team.Heroes, Stats(spd: 5));
        Actor fast = MakeActor("fast", Team.Heroes, Stats(spd: 10));

        slow.ApplyStatus(
            StatusDefinition.Buff("haste", "Haste", "Quickened.", new StatBlock(0, 0, 0, 20, 0)),
            turns: 3);

        var queue = new TurnQueue();
        queue.BeginRound(new[] { slow, fast });

        Assert.Equal(new[] { "slow", "fast" }, Drain(queue));
    }

    [Fact]
    public void AnActorKilledMidRoundIsSkippedRatherThanActingFromBeyondTheGrave()
    {
        Actor first = MakeActor("a", Team.Heroes, Stats(spd: 30));
        Actor second = MakeActor("b", Team.Monsters, Stats(spd: 20));

        var queue = new TurnQueue();
        queue.BeginRound(new[] { first, second });

        Assert.True(queue.MoveNext());
        Assert.Same(first, queue.Current);

        second.TakeDamage(999);              // killed during the first actor's turn

        Assert.False(queue.MoveNext());      // skipped, and the round ends
    }
}

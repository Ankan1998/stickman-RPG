# 19. Testing and balancing

> **Where you are:** chapter 19 of 20 · [index](README.md) · previous: [Debugging a game](18-debugging-a-game.md) · next: [Where to go next](20-where-to-go-next.md)

---

## The problem

You have built an RPG. Is it any good?

Specifically: **is the first dungeon too hard?** How would you find out?

The usual answer is "play it". So you do, five times. You die twice. Is that a
50% death rate, or did you get unlucky, or are you just bad at your own game?
You change a goblin's health from 40 to 58 and play five more times. You die
once. **Did that help?**

You cannot tell. Five samples of a high-variance process tells you nothing, your
memory of the earlier runs is already unreliable, and you have been getting
steadily better at your own game the whole time — which is the single most
insidious bias in game development. *You are the worst possible judge of your
game's difficulty, and you get worse every day.*

---

## The idea: a balance harness

If your rules do not need a screen ([chapter 5](05-rules-vs-presentation.md)),
you can play thousands of games in a test.

```csharp
public static Result Play(ulong seed, params string[] party)
{
    var campaign = new Campaign(ContentDatabase.CreateDefault(), seed);
    campaign.SetParty(party);

    while (campaign.Phase is CampaignPhase.Hub or CampaignPhase.InDungeon)
    {
        if (campaign.Phase == CampaignPhase.Hub)
        {
            EquipBestLoot(campaign);
            campaign.EnterDungeon();
            continue;
        }

        campaign.BeginEncounter();
        while (!campaign.Battle.IsOver)
            campaign.TakeTurn(ScoringAi.ChooseAction(campaign.Battle, campaign.Battle.Current!));

        campaign.CompleteEncounter();
    }

    return new Result(campaign.Phase, /* ... */);
}
```

That is [`CampaignRunner`](../../src/Rpg.Core.Tests/TestFixtures.cs). Run it 250
times with different seeds and you get:

```
   fell in Warrens     :   28  (11%)
   fell in Ember Halls :   58  (23%)
   fell in Frozen Crypt:  118  (47%)
   CLEARED ALL THREE   :   46  (18%)

 Avg encounters cleared : 6.5 of 9
 Avg damage dealt       : 1377
 Avg damage taken       : 696
 Avg rounds             : 33.6
 Avg heroes lost        : 6.30
```

**In about one second.** Now "did that change help?" is a question with an
answer.

> Studios call this a balance harness. It exists only because `Rpg.Core` has no
> dependency on the game engine — there is no window to open, no frame to wait
> for, nothing to draw.

## The AI is your playtester

The harness plays with `ScoringAi` on **both** sides. That is important and worth
being clear-eyed about.

- It is **consistent** — it does not learn or get tired, so a difference between
  two runs is a difference in your *game*, not in your player.
- It is **repeatable** — same seed, same run ([chapter 8](08-randomness-and-determinism.md)).
- It is **not a human.** It plays at roughly a competent-but-unimaginative level.
  It never panics and never finds a clever combo.

So harness numbers are a **relative** instrument, not an absolute one. "Dungeon 3
is much harder than dungeon 1" is trustworthy. "18% of *human* players will
finish" is not — humans will do better once they learn the systems, and worse on
their first run.

Use it to compare, then playtest with humans to calibrate.

---

## What to assert

Do not assert exact numbers — they will change with every tweak and you will
delete the test in irritation. Assert **the properties you actually care about**.

### 1. It is winnable, and it is not a giveaway

```csharp
Assert.InRange(clearRate, 0.12, 0.55);
```

A wide band. It fails only when something is genuinely broken.

### 2. The difficulty curve rises

```csharp
Assert.True(lethality[0] < lethality[1]);
Assert.True(lethality[1] < lethality[2]);
Assert.InRange(lethality[0], 0.05, 0.20);   // the tutorial dungeon's band
```

This is the one that catches the most real regressions. A curve that *dips* in
the middle is worse than a flat one — dungeon 2 being easier than dungeon 1 makes
progression feel broken even if every individual number is reasonable.

### 3. Every hero is viable

```csharp
[Fact] public void EveryHeroCanCarryAParty()
```

Runs 20 seeds per hero and asserts each can clear at least two encounters. This
catches "the Necromancer is unplayable" before a player finds it.

### 4. The AI does the thing it is supposed to

```csharp
[Fact] public void TheEnemyGoesForTheHealer()
```

Behaviour, asserted.

---

## Three real stories from tuning this game

These are the whole reason this chapter exists. All three are cases where the
obvious answer was **wrong**, and only measurement found it.

### Story 1: measure the mechanism, not the symptom

**The symptom:** the Warrens (the tutorial dungeon) killed **0%** of parties in
250 campaigns. Too easy.

**The obvious diagnosis:** the monsters do not hit hard enough.

Three guesses were tried — more monsters, more attack, bigger encounters — and it
stayed at 0%. So the guessing stopped and a temporary diagnostic was added to
print what was *actually happening*:

```
   lowest party health seen, avg : 73%
   party health leaving Warrens  : 89%
   avg rounds per encounter      : 3.4
```

**Three and a half rounds.** The goblins were not failing to hurt anyone — *they
were dying before they got to swing.*

The fix was tier-1 **Health**, not damage. +40% health took encounters to roughly
six rounds and the dungeon from 0% to 11% lethal.

> **The lesson: when tuning does not respond, stop turning the dial and go
> measure the mechanism.** "Too easy" is a symptom. "Fights end in 3.4 rounds" is
> a cause. You cannot fix a symptom.

### Story 2: know which dials are dials

From the same session:

| Change | Warrens lethality |
|---|---|
| baseline | 2% |
| **+1 Attack** on tier 1 | **11%** |
| **+3 Attack** on tier 1 | **69%** |
| **+1 monster** in encounter 1 | **41%** |

Attack is a **wrecking ball**, not a dial — because defence subtracts, a point of
Attack is worth several points of anything else
([chapter 10](10-numbers-and-stat-design.md)). And a single extra body is never a
small change, because action economy compounds every round.

> **The lesson: learn the sensitivity of each of your knobs, by measuring.** Then
> reach for the gentle ones when tuning and the violent ones when redesigning.

### Story 3: fixing one thing breaks another

Raising tier-1 health fixed the Warrens — and made dungeon 2 *softer than dungeon
1* (11% vs 5%). The curve now dipped in the middle, which is worse than where it
started.

Tier-2 health +15% restored it. Final: **11% / 26% / 72%**.

> **The lesson: balance is a system, not a list of numbers.** Every change
> propagates. This is precisely why the monotonic-curve assertion exists — it
> turns "somebody will notice eventually" into "the build fails".

---

## Testing the rules

The harness is for *balance*. You also need ordinary tests for *correctness*, and
they are ordinary because the rules are ordinary C#.

**66 tests, about one second.**

| File | Covers |
|---|---|
| [`CombatRulesTests`](../../src/Rpg.Core.Tests/CombatRulesTests.cs) | Damage, statuses, death, cooldowns |
| [`ContentIntegrityTests`](../../src/Rpg.Core.Tests/ContentIntegrityTests.cs) | Every content id resolves, is unique, and builds |
| [`DamageCalculatorTests`](../../src/Rpg.Core.Tests/DamageCalculatorTests.cs) | The formula, including the floor |
| [`TurnOrderTests`](../../src/Rpg.Core.Tests/TurnOrderTests.cs) | Speed, ties, buffs, corpses |
| [`FormationTests`](../../src/Rpg.Core.Tests/FormationTests.cs) | Twelve positioning tests |
| [`CampaignTests`](../../src/Rpg.Core.Tests/CampaignTests.cs) | Hub rules, wounds carrying, loot |
| [`EventLogTests`](../../src/Rpg.Core.Tests/EventLogTests.cs) | The log is trustworthy enough to replay |
| [`BalanceHarnessTests`](../../src/Rpg.Core.Tests/BalanceHarnessTests.cs) | 1,000 battles |
| [`CampaignHarnessTests`](../../src/Rpg.Core.Tests/CampaignHarnessTests.cs) | 250 campaigns |

### Write tests that explain themselves

The best tests here document a *decision*, not just a behaviour. Compare:

```csharp
// Weak: restates the code
[Fact] public void TakeDamageReducesHealth()

// Strong: pins a decision somebody might innocently reverse
[Fact] public void PoisonDamagesAtTheEndOfEachTurnAndThenWearsOff()
[Fact] public void ASwordInTheBackRankIsNotOffered()
[Fact] public void EveryHeroHasSomethingUsableFromEveryRankTheyCanOccupy()
[Fact] public void TheModelReadsDeadBeforeTheDeathIsAnnounced()
```

That last one is unusual and worth copying. It asserts a property that is *not a
bug* — the model resolves ahead of the replay — because that property is
surprising, load-bearing, and the reason a piece of presentation code looks the
way it does. **A test can be documentation for a design constraint.**

### Regression tests come from real bugs

Every bug should leave a test behind. From the double-death fix:

```csharp
[Fact]
public void NobodyIsEverAnnouncedDeadTwiceInAWholeBattle()
{
    for (ulong seed = 1; seed <= 200; seed++)
    {
        var doubled = BattleRunner.Run(seed).Log.OfType<Died>()
            .GroupBy(d => d.ActorId)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} died {g.Count()} times")
            .ToList();

        Assert.True(doubled.Count == 0, $"seed {seed}: {string.Join(", ", doubled)}");
    }
}
```

Note the failure message names the seed and the actor. **A test that fails with
`Assert.True` and no message wastes the twenty minutes it was supposed to save.**

---

## What you cannot test

Be honest about the boundary. Automated tests cover the rules. They do not cover:

| Not testable | How you actually check it |
|---|---|
| Does it feel good? | Play it. Watch someone else play it. |
| Is the UI clear? | Watch someone play it **without helping them**. |
| Is it fun? | Playtesting. There is no substitute. |
| Do the sprites look right? | Screenshots — see below. |
| Is the audio mixed? | Listen with your eyes closed. |

For the visual half, this project has a screenshot harness:

```bash
godot --path game -- --shots
```

which plays a real encounter, simulates a whole campaign, and captures five PNGs.
Not automated verification — a human still looks — but it turns "check the game
still renders" from a five-minute manual chore into one command, which means it
actually gets done.

### Watching someone play is the highest-value activity in game development

Nothing else comes close. You will watch someone put the Mage in rank 1, and
discover that everything you thought was obvious about your formation UI is not.

**Do not help them.** The instinct to explain is overwhelming and it destroys the
data. If they need you to explain it, the game needs to explain it.

---

## What it costs you

**Harnesses take real time to build.** `CampaignRunner`, `BattleRunner`,
`EquipBestLoot` and the reporting are a few hundred lines that ship no features.
They paid for themselves in this project many times over, but the cost is real
and it is up front.

**Slow tests get skipped.** 250 campaigns is about a second, which is fine. 10,000
would be 40 seconds, and a 40-second test suite is one you stop running. Watch
that number.

**The AI is not a player.** Every harness number carries this asterisk. An AI that
never panics will find some content easier than a human does, and content
requiring a clever combo much harder.

**Tests can ossify a bad design.** Fifty tests asserting details of a system make
it expensive to redesign. Test *decisions and properties*, not implementation.

---

## Try it

**1. Break the balance and watch a test catch it.** In
[`Monsters.cs`](../../src/Rpg.Core/Content/Monsters.cs), give tier-1 monsters
+5 Attack:

```bash
dotnet test --filter "FullyQualifiedName~CampaignHarness"
```

`TheDifficultyRisesFromOneDungeonToTheNext` fails, because the tutorial dungeon
is now deadlier than the endgame.

**2. Add a metric.** `RunStats` counts fourteen things. Add
`TimesRepositioned` — a counter and one line in `Observe`
([chapter 7](07-events-and-replay.md)) — then print it from the harness to see
how often the AI actually shuffles. You cannot break combat by doing this.

**3. Find the real difficulty knob.** Change `BreatherPercent` in
[`Campaign.cs`](../../src/Rpg.Core/Progression/Campaign.cs) from 28 to 50, and
run the harness. Then try 10. This single number — how much health you recover
between encounters — moves the campaign clear rate more than any monster stat in
the game, which is why its comment calls it *"the single biggest difficulty dial
in the game"*.

---

**Next:** [Chapter 20 — Where to go next](20-where-to-go-next.md)

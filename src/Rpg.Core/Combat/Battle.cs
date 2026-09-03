// ============================================================================
//  BATTLE - the heart of the whole project
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  This class runs one fight from start to finish. It decides who acts, lets
//  them act, applies poison, notices who died, and works out when the fight is
//  over.
//
//  It draws NOTHING. It waits for NOTHING. Ask it to take a turn and it hands
//  back a list of everything that happened, having already finished. The screen
//  then replays that list slowly so a human can follow along.
//
//  If you only properly read one file in this repository, make it this one.
//  There is a line-by-line walkthrough in docs/06-anatomy-of-a-turn.md.
//
//  HOW YOU USE IT
//  --------------
//      var battle = new Battle(state);
//      List<GameEvent> log = battle.Start();
//
//      while (!battle.IsOver)
//      {
//          Actor whoseTurn = battle.Current!;
//
//          IAction chosen = whoseTurn.Team == Team.Heroes
//              ? /* whatever button the player clicked */
//              : ScoringAi.ChooseAction(battle, whoseTurn);
//
//          log = battle.TakeTurn(chosen);
//          // ...animate `log`, or in a test, ignore it completely
//      }
//
//  Every method returns the events it produced. The caller decides whether to
//  spend three seconds animating them or to throw them away and immediately run
//  the next of ten thousand simulated battles. That one decision is what makes
//  this codebase testable, and it is the thing most hobby RPG engines get wrong.
// ============================================================================

using Rpg.Core.Content;
using Rpg.Core.Effects;
using Rpg.Core.Entities;

namespace Rpg.Core.Combat;

public sealed class Battle
{
    /// <summary>
    /// Safety valve. Two actors who cannot meaningfully hurt each other must not
    /// loop forever, so at this many rounds we call it a draw.
    /// </summary>
    public const int MaxRounds = 100;

    private readonly TurnQueue _queue = new();

    // Remembers who we have already announced as dead. See ReportDeaths below
    // for why that matters.
    private readonly HashSet<string> _deathsReported = new();

    public Battle(BattleState state) => State = state;

    public BattleState State { get; }

    /// <summary>
    /// Whose turn it is. Null before <see cref="Start"/> has been called, and
    /// null once the battle is over.
    /// </summary>
    // C# note: "Actor?" means this can legitimately be null, and the compiler
    // will nag anyone who forgets to check.
    public Actor? Current => IsOver ? null : _queue.Current;

    public int Round => _queue.Round;

    /// <summary>
    /// Who won. Null while the battle is still running, AND null for a draw -
    /// so always check <see cref="IsOver"/> as well.
    /// </summary>
    public Team? Winner { get; private set; }

    public bool IsOver { get; private set; }

    // ========================================================================
    //  Starting
    // ========================================================================

    /// <summary>
    /// Begins the fight and works out who acts first. Call this once, before
    /// any call to <see cref="TakeTurn"/>.
    /// </summary>
    public List<GameEvent> Start()
    {
        var log = new List<GameEvent> { new BattleStarted() };
        AdvanceToNextTurn(log);
        return log;
    }

    // ========================================================================
    //  What can this actor do right now?
    // ========================================================================

    /// <summary>
    /// Every action the given actor could legally take this instant, taking
    /// cooldowns, stuns and living targets into account.
    ///
    /// This ONE list feeds both the player's button menu and the monster AI.
    /// That is deliberate and worth protecting: it means the AI is structurally
    /// incapable of doing something the player could not. It is the difference
    /// between a game that feels hard and a game that feels rigged.
    /// </summary>
    public List<IAction> LegalActions(Actor actor)
    {
        var actions = new List<IAction>();

        // A stunned actor skips this whole block and gets only "Wait".
        if (actor.CanAct)
        {
            int myRank = State.RankOf(actor);

            foreach (SkillDefinition skill in actor.Skills)
            {
                if (!actor.IsSkillReady(skill.Id))
                    continue;                       // still on cooldown

                // WHERE ARE YOU STANDING? A greatsword is no use from the back
                // line, and a longbow is no use jammed against the enemy. This
                // one check is what makes the marching order a decision.
                if (!skill.LaunchRanks.Includes(myRank))
                    continue;

                // One entry per (skill, reachable target) pair. Slash against two
                // living enemies in reach produces two separate buttons.
                foreach (Actor target in TargetsFor(actor, skill))
                    actions.Add(new SkillAction(actor, skill, target));
            }
        }

        // Shuffling the line. Offered whenever there is somebody to swap with,
        // so a hero stranded in the wrong rank always has something better to do
        // than pass. See MoveAction for why that matters.
        if (actor.CanAct)
        {
            if (State.NeighbourOf(actor, -1) is { } ahead)
                actions.Add(new MoveAction(actor, ahead, forward: true));
            if (State.NeighbourOf(actor, +1) is { } behind)
                actions.Add(new MoveAction(actor, behind, forward: false));
        }

        // Always legal, even when stunned or entirely on cooldown, so this list
        // is never empty and no caller ever has to handle "no options".
        actions.Add(new PassAction(actor));
        return actions;
    }

    /// <summary>
    /// Who a given skill can actually be pointed at, taking the formation into
    /// account.
    ///
    /// Two filters run here: the KIND of target (enemy, ally, self) and then the
    /// POSITION. A sword that reaches "##--" cannot touch the enemy shaman
    /// hiding in rank three, however much you would like it to.
    ///
    /// Ally-targeted and self-targeted skills are checked against the caster's
    /// own side, so a heal that only reaches "--##" can patch up the back line
    /// but not the front.
    /// </summary>
    public IEnumerable<Actor> TargetsFor(Actor actor, SkillDefinition skill)
    {
        IEnumerable<Actor> candidates = skill.Target switch
        {
            TargetKind.SingleEnemy => State.LivingMembersOf(actor.Team.Opposite()),
            TargetKind.SingleAlly => State.LivingMembersOf(actor.Team),
            TargetKind.Self => new[] { actor },
            _ => throw new NotSupportedException($"Unhandled target kind: {skill.Target}"),
        };

        // Self-targeting always works - you are, by definition, in reach.
        if (skill.Target == TargetKind.Self)
            return candidates;

        return candidates.Where(t => skill.TargetRanks.Includes(State.RankOf(t)));
    }

    /// <summary>
    /// Can this actor use this skill at all from where they are standing?
    /// Exposed so the UI can grey out a sword in the back rank and explain WHY,
    /// rather than silently omitting it.
    /// </summary>
    public bool CanLaunch(Actor actor, SkillDefinition skill) =>
        skill.LaunchRanks.Includes(State.RankOf(actor));

    // ========================================================================
    //  Taking a turn - the main event
    // ========================================================================

    /// <summary>
    /// Resolves one complete turn and advances to the next actor. Returns
    /// everything that happened, in order.
    ///
    /// By the time this method returns, the turn is entirely finished: damage
    /// applied, poison ticked, deaths recorded, next actor chosen. Nothing has
    /// been drawn and no time has passed.
    /// </summary>
    public List<GameEvent> TakeTurn(IAction action)
    {
        // --- Guard rails. Cheap, and they turn a whole class of subtle bugs
        //     into one immediate, loud, obvious crash.
        if (IsOver)
            throw new InvalidOperationException("The battle is already over.");

        if (!ReferenceEquals(action.Actor, Current))
            throw new InvalidOperationException(
                $"It is {Current?.Name ?? "nobody"}'s turn, but the action belongs to {action.Actor.Name}.");

        Actor actor = action.Actor;

        // Everything that happens from here gets appended to this list, and the
        // list is what we hand back to the caller.
        var log = new List<GameEvent>();

        // --- 1. Act, unless something is stopping this actor (stun, sleep...).
        if (actor.CanAct)
            action.Execute(State, log);
        else
            log.Add(new TurnSkipped(actor.Id, actor.BlockedReason ?? "Unable to act"));

        ReportDeaths(log);

        // --- 2. End-of-turn statuses: poison damage, durations counting down.
        //        Deliberately AFTER acting, so a 1-turn poison still gets to
        //        deal its damage once before wearing off.
        TickStatuses(actor, log);
        ReportDeaths(log);   // ...because poison can kill.

        // --- 3. Is anyone left standing?
        if (CheckForEnd(log))
            return log;

        // --- 4. On to the next actor.
        AdvanceToNextTurn(log);
        return log;
    }

    // ========================================================================
    //  The private machinery
    // ========================================================================

    /// <summary>
    /// Moves to the next living actor, starting a new round if this one is
    /// exhausted.
    /// </summary>
    private void AdvanceToNextTurn(List<GameEvent> log)
    {
        // MoveNext() returns false when the round has run out of actors. The
        // loop then starts a fresh round and tries again. It is a loop rather
        // than an "if" so that a round in which everyone is already dead cannot
        // wedge us.
        while (!_queue.MoveNext())
        {
            if (_queue.Round >= MaxRounds)
            {
                Finish(null, log);   // null winner == draw
                return;
            }

            _queue.BeginRound(State.Actors);
            log.Add(new RoundStarted(_queue.Round));
        }

        // C# note: "!" tells the compiler "I know this is not null." It is safe
        // here precisely because MoveNext() just returned true.
        Actor next = _queue.Current!;

        // Cooldowns tick down at the START of your turn, so a 2-turn cooldown
        // means "you miss two of your own turns".
        next.TickCooldowns();

        log.Add(new TurnStarted(next.Id));
    }

    /// <summary>
    /// Applies poison-style damage and counts down every status on the actor
    /// whose turn just ended.
    /// </summary>
    private static void TickStatuses(Actor actor, List<GameEvent> log)
    {
        // C# note: ".ToList()" takes a snapshot of the collection first. Without
        // it, removing an expired status below would modify the very list we are
        // looping over, and .NET throws for that.
        foreach (StatusEffect status in actor.Statuses.ToList())
        {
            // NOTE FOR LATER: this "> 0" is why a regeneration status with a
            // negative DamagePerTurn silently does nothing. Fixing that is a
            // deliberate exercise - see docs/07-recipes.md.
            if (status.Definition.DamagePerTurn > 0 && actor.IsAlive)
            {
                int applied = actor.TakeDamage(status.Definition.DamagePerTurn);
                log.Add(new Damaged(actor.Id, applied, IsCritical: false, StatusId: status.Id));
            }

            status.Tick();   // one turn closer to wearing off

            if (status.IsExpired)
            {
                actor.RemoveStatus(status);
                log.Add(new StatusExpired(actor.Id, status.Id));
            }
            else
            {
                log.Add(new StatusTicked(actor.Id, status.Id, status.RemainingTurns));
            }
        }
    }

    /// <summary>
    /// Emits a Died event for anyone who has just hit zero health.
    ///
    /// Centralised on purpose. An actor can die from a sword, from poison, or
    /// later from a reflected hit. If each of those logged its own death you
    /// would eventually double-report one, and the screen would play the death
    /// animation twice. The HashSet remembers who has already been announced.
    /// </summary>
    private void ReportDeaths(List<GameEvent> log)
    {
        foreach (Actor actor in State.Actors)
        {
            // C# note: HashSet.Add returns false if the item was already there,
            // so this is "if they are dead AND we have not said so yet".
            if (!actor.IsAlive && _deathsReported.Add(actor.Id))
                log.Add(new Died(actor.Id));
        }
    }

    /// <summary>Returns true if the battle just ended.</summary>
    private bool CheckForEnd(List<GameEvent> log)
    {
        bool heroesGone = State.IsTeamWipedOut(Team.Heroes);
        bool monstersGone = State.IsTeamWipedOut(Team.Monsters);

        if (!heroesGone && !monstersGone)
            return false;

        Team? winner = heroesGone && monstersGone
            ? null                                          // mutual destruction is a draw
            : heroesGone ? Team.Monsters : Team.Heroes;

        Finish(winner, log);
        return true;
    }

    private void Finish(Team? winner, List<GameEvent> log)
    {
        IsOver = true;
        Winner = winner;
        log.Add(new BattleEnded(winner));
    }
}

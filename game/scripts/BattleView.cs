// ============================================================================
//  BATTLEVIEW - one encounter, on screen
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Draws the fight, offers the player their moves, and replays what happened
//  with animation, sound and effects. It calculates nothing: every number it
//  shows came out of Rpg.Core.
//
//  WHERE THE PRESENTATION HOOKS IN
//  -------------------------------
//  Nothing about combat changed to add animation and audio. The event stream was
//  already there, already arriving one item at a time with pauses. Each event
//  simply grew a few more things to do:
//
//      SkillUsed      attacker plays `attack`, weapon swing sound
//      Damaged        target plays `hurt`, impact sound by weapon archetype,
//                     fx_slash/impact/pierce overlay, damage number,
//                     `critical_hit` instead if it crit
//      Healed         fx_heal, `heal` sound, green pulse
//      StatusApplied  fx_poison / fx_fire / fx_ice / fx_debuff + its sound
//      Died           `death` animation and the voice family's death cry
//      BattleEnded    `victory` or `defeat`
//
//  That is the payoff for having built combat as a recording rather than as a
//  series of direct screen updates.
//
//  THE MENU IS TWO STEPS, ON PURPOSE
//  ---------------------------------
//  Three heroes with three skills against three monsters is up to ten legal
//  actions, and ten buttons is a wall nobody reads. So the menu asks twice:
//  pick a skill, then pick a target. Both steps are still built from
//  Battle.LegalActions() - they only GROUP that list, never add to it.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Rpg.Core.Ai;
using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Progression;

namespace StickmanRpg.Game;

public partial class BattleView : Control
{
    private const double BeatSeconds = 0.30;

    private readonly Dictionary<string, ActorView> _views = new();

    private Campaign _campaign = null!;
    private ContentDatabase _content = null!;
    private Action _onEncounterFinished = null!;

    private HBoxContainer _monsterRow = null!;
    private HBoxContainer _heroRow = null!;
    private Label _whereLabel = null!;
    private Label _turnLabel = null!;
    private Label _partyLabel = null!;
    private RichTextLabel _log = null!;
    private VBoxContainer _menu = null!;
    private Control _fx = null!;

    private int _displayedRound = 1;

    // ------------------------------------------------------------------

    public void Begin(Campaign campaign, ContentDatabase content,
        IEnumerable<GameEvent> openingEvents, Action onEncounterFinished)
    {
        _campaign = campaign;
        _content = content;
        _onEncounterFinished = onEncounterFinished;

        BuildLayout();
        BuildActorViews();

        DungeonDefinition d = _campaign.CurrentDungeon;
        _whereLabel.Text =
            $"{d.Label}   -   {_campaign.CurrentEncounter.Name}  ({_campaign.EncounterNumber}/{_campaign.TotalEncounters})";
        Write($"[color=#5a5670]{_campaign.CurrentEncounter.Flavour}[/color]");

        Audio.Play("door_open", -4f);
        _ = Opening(openingEvents);
    }

    private async Task Opening(IEnumerable<GameEvent> events)
    {
        await PlayEvents(events);
        await ContinueBattle();
    }

    // ==================================================================
    //  Layout
    // ==================================================================

    private void BuildLayout()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        DungeonDefinition d = _campaign.CurrentDungeon;
        Texture2D? bg = DungeonBackdrop.Build(d.FloorTile, d.WallTile);

        if (bg is not null)
        {
            var back = new TextureRect
            {
                Texture = bg,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            back.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(back);
        }

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            margin.AddThemeConstantOverride(side, 14);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);

        // --- top bar
        var top = new PanelContainer();
        top.AddThemeStyleboxOverride("panel",
            UiTheme.Flat(new Color(0.05f, 0.04f, 0.08f, 0.78f), radius: 3, padding: 7));
        root.AddChild(top);

        var topRow = new HBoxContainer();
        topRow.AddThemeConstantOverride("separation", 16);
        top.AddChild(topRow);

        _whereLabel = UiTheme.MakeLabel("", 14, UiTheme.Gold);
        topRow.AddChild(_whereLabel);

        _turnLabel = UiTheme.MakeLabel("", 14, UiTheme.TextBright, HorizontalAlignment.Center);
        _turnLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topRow.AddChild(_turnLabel);

        _partyLabel = UiTheme.MakeLabel("", 13, UiTheme.TextDim, HorizontalAlignment.Right);
        topRow.AddChild(_partyLabel);

        // --- THE BATTLE LINE
        //
        //     your party                      the enemy
        //   [3] [2] [1]        VS        [1] [2] [3]
        //   back -> front                front -> back
        //
        // One row, both sides, facing each other. The whole point of the
        // formation is that you can SEE who is in front of whom, so the two
        // lines have to be laid out along the same axis - stacking them one
        // above the other hides the only thing that matters.
        var field = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        field.AddThemeConstantOverride("separation", 4);
        root.AddChild(field);

        // party: back rank leftmost, front rank against the divider
        _heroRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        _heroRow.AddThemeConstantOverride("separation", 4);
        _heroRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        field.AddChild(_heroRow);

        // The gap between the lines - where the fighting happens. A visible
        // no-man's-land makes the two formations read as two formations rather
        // than one row of six.
        var divider = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        divider.CustomMinimumSize = new Vector2(56, 0);
        divider.AddThemeConstantOverride("separation", 4);

        divider.AddChild(new ColorRect
        {
            Color = new Color(1, 1, 1, 0.10f),
            CustomMinimumSize = new Vector2(2, 120),
        });
        divider.AddChild(UiTheme.MakeLabel("VS", 16, UiTheme.Gold, HorizontalAlignment.Center));
        field.AddChild(divider);

        // enemy: front rank against the divider, back rank rightmost
        _monsterRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
        _monsterRow.AddThemeConstantOverride("separation", 4);
        _monsterRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        field.AddChild(_monsterRow);

        // --- log and menu
        var bottom = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        bottom.AddThemeConstantOverride("separation", 12);
        root.AddChild(bottom);

        var logPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 132),
        };
        bottom.AddChild(logPanel);

        var logMargin = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            logMargin.AddThemeConstantOverride(side, 4);
        logPanel.AddChild(logMargin);

        _log = new RichTextLabel { BbcodeEnabled = true, ScrollFollowing = true, FitContent = false };
        logMargin.AddChild(_log);

        var menuPanel = new PanelContainer { CustomMinimumSize = new Vector2(300, 0) };
        bottom.AddChild(menuPanel);

        var menuScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        menuPanel.AddChild(menuScroll);

        _menu = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _menu.AddThemeConstantOverride("separation", 4);
        menuScroll.AddChild(_menu);

        // --- effects layer, on top, click-through
        _fx = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _fx.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fx);
    }

    private static HBoxContainer CentredRow()
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        return row;
    }

    private void BuildActorViews()
    {
        _views.Clear();
        foreach (HBoxContainer row in new[] { _heroRow, _monsterRow })
            foreach (Node child in row.GetChildren())
            {
                row.RemoveChild(child);
                child.QueueFree();
            }

        BattleState state = _campaign.Battle.State;

        // The party is added in REVERSE formation order, so the back rank ends
        // up leftmost and the front rank sits against the divider.
        foreach (Actor actor in state.Actors.Where(a => a.Team == Team.Heroes).Reverse())
            AddView(actor, _heroRow);

        // The enemy is added front-first, so their rank 1 also sits against the
        // divider - the two front lines end up facing each other in the middle.
        foreach (Actor actor in state.Actors.Where(a => a.Team == Team.Monsters))
            AddView(actor, _monsterRow);
    }

    private void AddView(Actor actor, HBoxContainer row)
    {
        var view = new ActorView();
        row.AddChild(view);
        view.Bind(actor);
        _views[actor.Id] = view;
    }

    /// <summary>
    /// Re-sorts both lines so the screen matches the actual formation.
    ///
    /// Ranks close up when somebody falls, and if the display did not follow
    /// the player would have no way to see that the enemy back-liner has just
    /// stepped into their sword's reach.
    /// </summary>
    private void ReorderFormation()
    {
        BattleState state = _campaign.Battle.State;

        var heroes = state.Actors.Where(a => a.Team == Team.Heroes)
            .OrderByDescending(a => a.IsAlive ? state.RankOf(a) : int.MaxValue).ToList();
        var monsters = state.Actors.Where(a => a.Team == Team.Monsters)
            .OrderBy(a => a.IsAlive ? state.RankOf(a) : int.MaxValue).ToList();

        for (int i = 0; i < heroes.Count; i++)
            if (_views.TryGetValue(heroes[i].Id, out ActorView? v))
                _heroRow.MoveChild(v, i);

        for (int i = 0; i < monsters.Count; i++)
            if (_views.TryGetValue(monsters[i].Id, out ActorView? v))
                _monsterRow.MoveChild(v, i);
    }

    // ==================================================================
    //  Turn flow
    // ==================================================================

    private async Task ContinueBattle()
    {
        while (!_campaign.Battle.IsOver)
        {
            Actor current = _campaign.Battle.Current!;

            if (current.Team == Team.Heroes)
            {
                Audio.Play("turn_start", -14f);
                ShowSkillMenu(current);
                return;                            // stop, wait for a click
            }

            await ToSignal(GetTree().CreateTimer(0.20), SceneTreeTimer.SignalName.Timeout);
            IAction action = ScoringAi.ChooseAction(_campaign.Battle, current);
            await PlayEvents(_campaign.TakeTurn(action));
        }

        ClearMenu();
        Audio.Play(_campaign.Battle.Winner == Team.Heroes ? "victory" : "defeat", -3f);
        await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout);
        _onEncounterFinished();
    }

    // -- step one: which skill? -----------------------------------------

    private void ShowSkillMenu(Actor actor)
    {
        ClearMenu();
        AddMenuHeader(actor, "choose an action");

        List<IAction> legal = _campaign.Battle.LegalActions(actor);

        foreach (var group in legal.OfType<SkillAction>().GroupBy(a => a.Skill.Id))
        {
            SkillDefinition skill = group.First().Skill;
            List<SkillAction> options = group.ToList();

            var button = new Button
            {
                Text = SkillButtonText(skill),
                TooltipText = skill.Description,
                Alignment = HorizontalAlignment.Left,
            };
            button.Pressed += () =>
            {
                Audio.Play("ui_select", -10f);
                if (options.Count == 1) OnActionChosen(options[0]);
                else ShowTargetMenu(actor, skill, options);
            };
            _menu.AddChild(button);
        }

        // Everything unavailable, greyed out WITH THE REASON. A skill that just
        // vanishes from the menu teaches the player nothing; one that says
        // "needs rank 1-2" teaches them the whole positioning system.
        int myRank = _campaign.Battle.State.RankOf(actor);

        // Stunned, frozen, webbed: LegalActions drops every skill AND every
        // step at once, which left the player staring at a lone "Wait" button
        // with nothing anywhere saying why. A menu that explains itself has to
        // explain this case too - it is the one the player most wants explained.
        bool canAct = actor.CanAct;
        if (!canAct)
            _menu.AddChild(UiTheme.MakeLabel(
                $"{actor.BlockedReason ?? "Unable to act"} - loses this turn",
                12, UiTheme.HealthLow, HorizontalAlignment.Center));

        foreach (SkillDefinition skill in actor.Skills)
        {
            bool ready = actor.IsSkillReady(skill.Id);
            bool inPosition = skill.LaunchRanks.Includes(myRank);
            bool hasTarget = _campaign.Battle.TargetsFor(actor, skill).Any();

            if (canAct && ready && inPosition && hasTarget) continue;   // already offered above

            string why =
                !canAct ? (actor.BlockedReason ?? "cannot act").ToLowerInvariant()
                : !ready ? $"{actor.TurnsUntilReady(skill.Id)} turn{(actor.TurnsUntilReady(skill.Id) == 1 ? "" : "s")}"
                : !inPosition ? $"needs {RankWords(skill.LaunchRanks)}"
                : $"nothing in {RankWords(skill.TargetRanks)}";

            _menu.AddChild(new Button
            {
                Text = $"{skill.Name}   ({why})",
                Disabled = true,
                TooltipText = $"{skill.Description}"
                              + $"   [use from {skill.LaunchRanks.Diagram}"
                              + $" - reaches {skill.TargetRanks.Diagram}]",
                Alignment = HorizontalAlignment.Left,
            });
        }

        // --- shuffling the line. The way out of a bad position.
        foreach (MoveAction move in legal.OfType<MoveAction>())
        {
            MoveAction m = move;
            var b = new Button { Text = m.Label, Alignment = HorizontalAlignment.Left };
            b.AddThemeFontSizeOverride("font_size", 12);
            b.TooltipText = "Costs your whole turn.";
            b.Pressed += () => { Audio.Play("ui_move", -10f); OnActionChosen(m); };
            _menu.AddChild(b);
        }

        var wait = new Button { Text = "Wait", Alignment = HorizontalAlignment.Left };
        wait.Pressed += () => { Audio.Play("ui_select", -10f); OnActionChosen(legal.OfType<PassAction>().First()); };
        _menu.AddChild(wait);
    }

    private static string SkillButtonText(SkillDefinition skill)
    {
        var bits = new List<string>();
        if (skill.DealsDamage) bits.Add($"{skill.Power}%");
        if (skill.Heals) bits.Add($"heal {skill.Healing}");
        if (skill.Drains) bits.Add($"drain {skill.LifestealPercent}%");
        if (skill.AppliesStatus is not null) bits.Add(skill.AppliesStatus.Name.ToLowerInvariant());

        string detail = bits.Count == 0 ? skill.Name : $"{skill.Name}   ({string.Join(", ", bits)})";

        // The reach diagram, front-first. Showing it on every positional skill
        // is what turns "why can't I hit that?" from a mystery into a rule.
        return skill.IsPositional ? $"{detail}   {skill.TargetRanks.Diagram}" : detail;
    }

    /// <summary>Turns a rank mask into words, for a disabled button's explanation.</summary>
    private static string RankWords(Ranks ranks)
    {
        var positions = Enumerable.Range(1, Ranks.Max).Where(ranks.Includes).ToList();
        if (positions.Count == 0) return "nowhere";
        if (positions.Count == Ranks.Max) return "any rank";
        return positions.Count == 1
            ? $"rank {positions[0]}"
            : $"rank {positions.First()}-{positions.Last()}";
    }

    // -- step two: which target? ----------------------------------------

    private void ShowTargetMenu(Actor actor, SkillDefinition skill, List<SkillAction> options)
    {
        ClearMenu();
        AddMenuHeader(actor, $"{skill.Name} - pick a target");

        foreach (SkillAction option in options)
        {
            Actor target = option.Target;

            // Show the consequence up front. Guessing is not strategy.
            string preview = skill.DealsDamage
                ? $"{DamageCalculator.Compute(actor.CurrentStats, target.CurrentStats, skill.Power, false)} dmg"
                : skill.Heals
                    ? $"+{Math.Min(skill.Healing, target.MaxHealth - target.Health)} hp"
                    : $"{target.Health}/{target.MaxHealth}";

            var button = new Button
            {
                Text = $"[{_campaign.Battle.State.RankOf(target)}] {target.Name}   ({preview})",
                Alignment = HorizontalAlignment.Left,
            };
            button.Pressed += () => { Audio.Play("ui_select", -10f); OnActionChosen(option); };
            _menu.AddChild(button);
        }

        var back = new Button { Text = "< Back", Alignment = HorizontalAlignment.Left };
        back.Pressed += () => { Audio.Play("ui_back", -10f); ShowSkillMenu(actor); };
        _menu.AddChild(back);
    }

    private void AddMenuHeader(Actor actor, string subtitle)
    {
        _menu.AddChild(UiTheme.MakeLabel(actor.Name, 15, UiTheme.HeroBlue, HorizontalAlignment.Center));
        _menu.AddChild(UiTheme.MakeLabel(subtitle, 11, UiTheme.TextFaint, HorizontalAlignment.Center));

        int rank = _campaign.Battle.State.RankOf(actor);
        _menu.AddChild(UiTheme.MakeLabel(
            rank == 1 ? "standing in the FRONT rank" : $"standing in rank {rank}",
            11, rank == 1 ? UiTheme.Gold : UiTheme.TextDim, HorizontalAlignment.Center));

        if (actor.Weapon is { } w)
            _menu.AddChild(UiTheme.MakeLabel(w.Label, 11, UiTheme.Gold, HorizontalAlignment.Center));

        _menu.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
    }

    private async void OnActionChosen(IAction action)
    {
        ClearMenu();
        await PlayEvents(_campaign.TakeTurn(action));
        await ContinueBattle();
    }

    private void ClearMenu()
    {
        foreach (Node child in _menu.GetChildren())
        {
            _menu.RemoveChild(child);
            child.QueueFree();
        }
    }

    // ==================================================================
    //  Replaying the event log - where it all comes alive
    // ==================================================================

    public async Task PlayEvents(IEnumerable<GameEvent> events)
    {
        var log = events.ToList();
        HashSet<int> killingBlows = FindKillingBlows(log);

        for (int i = 0; i < log.Count; i++)
        {
            GameEvent e = log[i];

            switch (e)
            {
                case RoundStarted r:
                    _displayedRound = r.Round;
                    break;

                case TurnStarted t:
                    _turnLabel.Text = $"Round {_displayedRound}  -  {Name_(t.ActorId)}";
                    break;

                case SkillUsed s:
                    await ShowSkillUsed(s);
                    break;

                case Damaged d:
                    await ShowDamage(d, fatal: killingBlows.Contains(i));
                    break;

                case Healed h:
                    await ShowHeal(h);
                    break;

                case StatusApplied st:
                    ShowStatus(st);
                    break;

                case Died dead:
                    await ShowDeath(dead);
                    break;
            }

            string? line = Describe(e);
            if (line is not null) Write(line);

            RefreshAll();

            if (NeedsABeat(e))
                await ToSignal(GetTree().CreateTimer(BeatSeconds), SceneTreeTimer.SignalName.Timeout);
        }

        RefreshAll();
    }

    /// <summary>
    /// Which damage events were the blow that actually felled somebody: for each
    /// death, the last damage that fighter took before it.
    ///
    /// Worked out from the LOG rather than by asking whether the target is alive,
    /// because the model is already fully resolved before any of this is drawn.
    /// A fighter burning to death from two statuses at once reads as dead at the
    /// FIRST tick, which would wrongly mark both as fatal.
    /// </summary>
    private static HashSet<int> FindKillingBlows(List<GameEvent> log)
    {
        var fatal = new HashSet<int>();

        for (int i = 0; i < log.Count; i++)
        {
            if (log[i] is not Died death) continue;

            for (int j = i - 1; j >= 0; j--)
            {
                if (log[j] is Damaged d && d.ActorId == death.ActorId)
                {
                    fatal.Add(j);
                    break;
                }
            }
        }

        return fatal;
    }

    private async Task ShowSkillUsed(SkillUsed s)
    {
        if (!_views.TryGetValue(s.ActorId, out ActorView? view)) return;

        Actor actor = _campaign.Battle.State.GetActor(s.ActorId);
        SkillDefinition skill = _content.Skill(s.SkillId);

        // A caster winds up; a fighter swings.
        Audio.Play(skill.DealsDamage && actor.Weapon is null && skill.AppliesStatus is not null
            ? "spell_cast"
            : skill.Heals ? "spell_cast" : "miss_whoosh", -8f);

        await view.PlayAttack();
    }

    private async Task ShowDamage(Damaged d, bool fatal)
    {
        if (!_views.TryGetValue(d.ActorId, out ActorView? view)) return;

        Actor target = _campaign.Battle.State.GetActor(d.ActorId);

        // WHO SWUNG - taken from the event, which is the only thing that knows.
        //
        // This used to read Battle.Current, and that was simply wrong: TakeTurn
        // resolves the whole turn AND advances the queue before handing back the
        // log, so by replay time Current is the NEXT fighter. Every impact
        // therefore played the wrong weapon's sound - a goblin's club could land
        // with a bowstring - and poison ticks borrowed a weapon from whoever
        // happened to be up next.
        Actor? attacker = d.SourceId is null ? null : _campaign.Battle.State.GetActor(d.SourceId);

        if (d.StatusId is not null)
        {
            // Damage that ticked rather than landed. Poison burns; it does not
            // stab, so it gets its own sound and its own splash.
            string? sound = Sfx.ForStatus(d.StatusId);
            if (sound is not null) Audio.Play(sound, -6f);

            string? fx = Sfx.FxForStatus(d.StatusId);
            if (fx is not null) EffectOverlay.Spawn(_fx, ToFx(view.ImpactPoint), fx);
        }
        else
        {
            // The impact comes from the ATTACKER's weapon archetype, so a hammer
            // and a dagger landing on the same goblin sound different.
            Audio.Play(d.IsCritical ? "critical_hit" : Sfx.HitFor(attacker?.Weapon), -2f);

            EffectOverlay.Spawn(_fx, ToFx(view.ImpactPoint),
                d.IsCritical ? "fx_explosion" : Sfx.FxForWeapon(attacker?.Weapon));
        }

        Popup(view, d.IsCritical ? $"-{d.Amount}!" : $"-{d.Amount}",
            d.IsCritical ? UiTheme.CritGold : UiTheme.DamageRed, d.IsCritical);

        // A killing blow gets no hurt cry and no flinch. The death cry follows
        // immediately after, and playing both made a fighter sound like they
        // died twice; flinching first made the death animation start, abort and
        // start again.
        if (fatal) return;

        Audio.Play(Sfx.HurtFor(target.VoiceFamily), -6f);
        await view.PlayHit(d.IsCritical);
    }

    private async Task ShowHeal(Healed h)
    {
        if (!_views.TryGetValue(h.ActorId, out ActorView? view)) return;

        Audio.Play("heal", -5f);
        Popup(view, $"+{h.Amount}", UiTheme.HealGreen);
        EffectOverlay.Spawn(_fx, ToFx(view.ImpactPoint), "fx_heal");
        await view.PlayHeal();
    }

    private void ShowStatus(StatusApplied s)
    {
        if (!_views.TryGetValue(s.ActorId, out ActorView? view)) return;

        string? sound = Sfx.ForStatus(s.StatusId);
        if (sound is not null) Audio.Play(sound, -7f);

        string? fx = Sfx.FxForStatus(s.StatusId);
        if (fx is not null) EffectOverlay.Spawn(_fx, ToFx(view.ImpactPoint), fx);
    }

    private async Task ShowDeath(Died d)
    {
        if (!_views.TryGetValue(d.ActorId, out ActorView? view)) return;

        Actor actor = _campaign.Battle.State.GetActor(d.ActorId);
        Audio.Play(Sfx.DeathFor(actor.VoiceFamily), -3f);
        EffectOverlay.Spawn(_fx, ToFx(view.ImpactPoint), "fx_smoke");
        await view.PlayDeath();
    }

    /// <summary>Converts a global point into the effect layer's own coordinates.</summary>
    private Vector2 ToFx(Vector2 globalPoint) => globalPoint - _fx.GetGlobalRect().Position;

    private void Popup(ActorView view, string text, Color colour, bool emphasise = false)
    {
        FloatingNumber.Spawn(_fx, ToFx(view.ImpactPoint) - new Vector2(0, 30),
            text, colour, 22, emphasise);
    }

    private void RefreshAll()
    {
        BattleState state = _campaign.Battle.State;
        string? currentId = _campaign.Battle.Current?.Id;

        foreach ((string id, ActorView view) in _views)
            view.Refresh(id == currentId, state.RankOf(view.Actor));

        ReorderFormation();
        _partyLabel.Text = $"party {_campaign.PartyHealthFraction:P0}";
    }

    private string? Describe(GameEvent e) => e switch
    {
        RoundStarted r => $"\n[color=#4a4660]-- round {r.Round} --[/color]",

        SkillUsed s => s.ActorId == s.TargetId
            ? $"[color=#8d88a8]{Name_(s.ActorId)}[/color] uses [b]{SkillName(s.SkillId)}[/b]."
            : $"[color=#8d88a8]{Name_(s.ActorId)}[/color] uses [b]{SkillName(s.SkillId)}[/b] on {Name_(s.TargetId)}.",

        Damaged d => d.IsCritical
            ? $"    [color=#ffd97a]{d.Amount} damage[/color] to {Name_(d.ActorId)} [b]- critical![/b]"
            : $"    [color=#e8695c]{d.Amount} damage[/color] to {Name_(d.ActorId)}",

        Healed h => $"    [color=#7fd98a]+{h.Amount} health[/color] to {Name_(h.ActorId)}",

        StatusApplied s => $"    {Name_(s.ActorId)} is [b]{StatusName(s.StatusId)}[/b] ({s.Turns} turns)",

        StatusExpired s => $"    [color=#4a4660]{StatusName(s.StatusId)} fades from {Name_(s.ActorId)}[/color]",

        TurnSkipped t => $"[color=#8d88a8]{Name_(t.ActorId)}[/color] cannot act - {t.Reason.ToLowerInvariant()}.",

        Repositioned r => $"[color=#8d88a8]{Name_(r.ActorId)}[/color] swaps with {Name_(r.SwappedWithId)} - now rank {r.NewRank}.",

        Died d => $"[b][color=#cf5b5b]{Name_(d.ActorId)} falls.[/color][/b]",

        _ => null,
    };

    private static bool NeedsABeat(GameEvent e) =>
        e is Damaged or Healed or StatusApplied or TurnSkipped or Died or Repositioned;

    private void Write(string bbcode) => _log.AppendText(bbcode + "\n");

    private string Name_(string id) => _campaign.Battle.State.GetActor(id).Name;
    private string SkillName(string id) => _content.Skill(id).Name;
    private string StatusName(string id) => _content.Status(id).Name;
}

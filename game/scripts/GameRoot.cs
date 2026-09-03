// ============================================================================
//  GAMEROOT - the whole game, screen by screen
// ============================================================================
//
//      TITLE -> HUB -> dungeon (2-3 encounters, loot after each) -> HUB -> ...
//                                                                -> RESULTS
//
//  It owns the Campaign (which owns the rules) and swaps one screen for another.
//  Every screen is a plain Control built in code.
//
//  WHAT IT DOES NOT DO
//  -------------------
//  Not one rule lives here. It asks the Campaign to begin an encounter, hands
//  the resulting events to BattleView to animate, and asks how it went. All the
//  deciding happens in Rpg.Core - which is why the entire nine-encounter
//  campaign can be simulated in a unit test without any of this file existing.
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Rpg.Core.Combat;
using Rpg.Core.Content;
using Rpg.Core.Entities;
using Rpg.Core.Progression;

namespace StickmanRpg.Game;

public partial class GameRoot : Control
{
    private ContentDatabase _content = null!;
    private Campaign? _campaign;
    private ulong _nextSeed = 1;

    private Control _screen = null!;

    // The party being assembled at the hub.
    private readonly List<string> _picked = new();

    public override void _Ready()
    {
        Theme = UiTheme.Build();
        Audio.Attach(this);

        _content = ContentDatabase.CreateDefault();

        var backdrop = new ColorRect { Color = UiTheme.Backdrop, MouseFilter = MouseFilterEnum.Ignore };
        backdrop.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        _screen = new Control();
        _screen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_screen);

        ShowTitle();

        // A dev tool, not part of the game. See CaptureShots().
        if (OS.GetCmdlineUserArgs().Contains("--shots"))
            _ = CaptureShots();
    }

    private T SwapScreen<T>() where T : Control, new()
    {
        foreach (Node child in _screen.GetChildren())
        {
            _screen.RemoveChild(child);
            child.QueueFree();
        }

        var next = new T();
        next.SetAnchorsPreset(LayoutPreset.FullRect);
        _screen.AddChild(next);
        return next;
    }

    /// <summary>A scrollable, centred column - the skeleton of every menu screen.</summary>
    private static VBoxContainer MenuScreen(Control screen, int margin = 24)
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        screen.AddChild(scroll);

        var centre = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(centre);

        var wrap = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            wrap.AddThemeConstantOverride(side, margin);
        centre.AddChild(wrap);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 8);
        wrap.AddChild(column);
        return column;
    }

    // ==================================================================
    //  Title
    // ==================================================================

    private void ShowTitle()
    {
        VBoxContainer col = MenuScreen(SwapScreen<Control>());

        col.AddChild(UiTheme.MakeLabel("STICKMAN", 56, UiTheme.TextBright, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel("R P G", 28, UiTheme.Gold, HorizontalAlignment.Center));
        col.AddChild(Spacer(10));

        // A line-up of the enemy, as a threat rather than a cast photo.
        var cast = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cast.AddThemeConstantOverride("separation", 8);
        foreach (string id in new[] { "warrior", "cleric", "ranger", "mage", "rogue" })
            cast.AddChild(Portrait(_content.Hero(id).SpriteName, 3));
        col.AddChild(cast);

        col.AddChild(Spacer(8));
        col.AddChild(UiTheme.MakeLabel("Three dungeons. Ten heroes. Pick three.",
            17, UiTheme.TextDim, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel(
            "Wounds carry between encounters. Only camp restores you.",
            13, UiTheme.TextFaint, HorizontalAlignment.Center));
        col.AddChild(Spacer(16));

        col.AddChild(CentredButton("  Begin  ", 19, () =>
        {
            _campaign = new Campaign(_content, _nextSeed);
            _picked.Clear();
            _picked.AddRange(_campaign.Party.Select(p => p.Id));
            ShowHub();
        }));

        col.AddChild(Spacer(4));
        col.AddChild(UiTheme.MakeLabel($"seed {_nextSeed}", 11, UiTheme.TextFaint, HorizontalAlignment.Center));
    }

    // ==================================================================
    //  Hub - pick a party, hand out loot
    // ==================================================================

    private void ShowHub()
    {
        Campaign c = _campaign!;
        Control screen = SwapScreen<Control>();

        // Reserve the bottom strip for the footer, so the scrolling roster can
        // never hide the one button the player has to press.
        var body = new MarginContainer();
        body.SetAnchorsPreset(LayoutPreset.FullRect);
        body.AddThemeConstantOverride("margin_bottom", 62);
        screen.AddChild(body);

        VBoxContainer col = MenuScreen(body, margin: 14);

        DungeonDefinition next = c.CurrentDungeon;

        col.AddChild(UiTheme.MakeLabel("CAMP", 34, UiTheme.Gold, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel(
            c.Stats.DungeonsCleared == 0
                ? "Choose who goes in."
                : $"{c.Stats.DungeonsCleared} of {c.TotalDungeons} cleared. Everyone is rested.",
            13, UiTheme.TextDim, HorizontalAlignment.Center));
        col.AddChild(Spacer(10));

        // --- what is coming
        var brief = new PanelContainer();
        col.AddChild(brief);
        var briefBox = new MarginContainer();
        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            briefBox.AddThemeConstantOverride(side, 12);
        brief.AddChild(briefBox);

        var briefCol = new VBoxContainer();
        briefCol.AddThemeConstantOverride("separation", 3);
        briefBox.AddChild(briefCol);
        briefCol.AddChild(UiTheme.MakeLabel(
            $"NEXT:  {next.Label}   ({next.Encounters.Count} encounters)", 16, UiTheme.TextBright));
        briefCol.AddChild(UiTheme.MakeLabel(next.Blurb, 12, UiTheme.TextDim));
        briefCol.AddChild(UiTheme.MakeLabel($"Threat: {next.ThreatName}", 13, UiTheme.HealthLow));
        briefCol.AddChild(WrappedLabel(next.ThreatBlurb, 11, UiTheme.TextFaint, 620));

        col.AddChild(Spacer(10));
        col.AddChild(UiTheme.MakeLabel(
            $"MARCHING ORDER   ({_picked.Count}/{Campaign.PartySize})", 15, UiTheme.Gold, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel(
            "Rank 1 is the front line. Melee must be near the front; bows and spells cannot be cast from rank 1.",
            11, UiTheme.TextFaint, HorizontalAlignment.Center));

        col.AddChild(MarchingOrder());
        col.AddChild(Spacer(8));

        // --- the roster, as a grid of pickable cards
        var grid = new GridContainer { Columns = 5 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        col.AddChild(grid);

        foreach (HeroDefinition hero in _content.Heroes)
            grid.AddChild(HeroCard(hero));

        col.AddChild(Spacer(8));

        // --- loot
        if (c.Loot.Count > 0)
        {
            col.AddChild(UiTheme.MakeLabel("ARMOURY", 15, UiTheme.Gold, HorizontalAlignment.Center));
            col.AddChild(LootRack(c));
            col.AddChild(Spacer(8));
        }

        // --- fixed footer, pinned to the bottom of the screen
        bool ready = _picked.Count == Campaign.PartySize;

        var footer = new PanelContainer();
        footer.SetAnchorsPreset(LayoutPreset.BottomWide);
        footer.OffsetTop = -56;
        footer.OffsetBottom = 0;
        footer.AddThemeStyleboxOverride("panel",
            UiTheme.Flat(new Color(0.05f, 0.04f, 0.08f, 0.94f), padding: 8));
        screen.AddChild(footer);

        var footRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        footRow.AddThemeConstantOverride("separation", 16);
        footer.AddChild(footRow);

        footRow.AddChild(UiTheme.MakeLabel(
            ready ? string.Join("  -  ", _picked.Select(id => _content.Hero(id).Label))
                  : $"choose {Campaign.PartySize - _picked.Count} more",
            13, ready ? UiTheme.TextDim : UiTheme.TextFaint));

        var descend = new Button
        {
            Text = ready ? $"  Enter {next.Label}  " : "  Pick three heroes  ",
            Disabled = !ready,
        };
        descend.AddThemeFontSizeOverride("font_size", 17);
        descend.Pressed += () =>
        {
            Audio.Play("stairs", -4f);
            c.SetParty(_picked);
            c.EnterDungeon();
            BeginEncounter();
        };
        footRow.AddChild(descend);
    }

    /// <summary>
    /// The chosen three, in the order they will stand, with arrows to shuffle
    /// them.
    ///
    /// This is the decision the whole positioning system exists for, so it gets
    /// its own strip at the top rather than being implied by the order you
    /// happened to click things in.
    /// </summary>
    private Control MarchingOrder()
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 6);

        if (_picked.Count == 0)
        {
            row.AddChild(UiTheme.MakeLabel("- nobody chosen yet -", 12, UiTheme.TextFaint));
            return row;
        }

        // Rank 1 first, so the strip reads the same way round as the battle line.
        for (int i = 0; i < _picked.Count; i++)
        {
            int index = i;
            HeroDefinition hero = _content.Hero(_picked[i]);

            var card = new PanelContainer { CustomMinimumSize = new Vector2(150, 0) };
            card.AddThemeStyleboxOverride("panel", UiTheme.Flat(
                index == 0 ? new Color(0.24f, 0.20f, 0.10f) : new Color(0.11f, 0.10f, 0.15f),
                radius: 3, padding: 5));

            var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            box.AddThemeConstantOverride("separation", 1);
            card.AddChild(box);

            box.AddChild(UiTheme.MakeLabel(
                index == 0 ? "RANK 1 - FRONT" : $"RANK {index + 1}",
                11, index == 0 ? UiTheme.Gold : UiTheme.TextFaint, HorizontalAlignment.Center));
            box.AddChild(Portrait(hero.SpriteName, 2));
            box.AddChild(UiTheme.MakeLabel(hero.Label, 12, UiTheme.HeroBlue, HorizontalAlignment.Center));

            // What this hero can actually DO from this rank. The whole point.
            int usable = hero.SkillIds
                .Select(id => _content.Skill(id))
                .Count(sk => sk.LaunchRanks.Includes(index + 1));

            box.AddChild(UiTheme.MakeLabel(
                usable == hero.SkillIds.Length ? "all skills usable"
                : usable == 0 ? "NO SKILLS USABLE"
                : $"{usable} of {hero.SkillIds.Length} skills usable",
                10,
                usable == 0 ? UiTheme.HealthLow
                : usable < hero.SkillIds.Length ? UiTheme.HealthWarn
                : UiTheme.HealGreen,
                HorizontalAlignment.Center));

            var arrows = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            arrows.AddThemeConstantOverride("separation", 3);

            var up = new Button { Text = "<", Disabled = index == 0, TooltipText = "Move towards the front" };
            up.AddThemeFontSizeOverride("font_size", 11);
            up.Pressed += () => { Audio.Play("ui_move", -10f); Swap(index, index - 1); };
            arrows.AddChild(up);

            var down = new Button { Text = ">", Disabled = index == _picked.Count - 1, TooltipText = "Move towards the back" };
            down.AddThemeFontSizeOverride("font_size", 11);
            down.Pressed += () => { Audio.Play("ui_move", -10f); Swap(index, index + 1); };
            arrows.AddChild(down);

            box.AddChild(arrows);
            row.AddChild(card);
        }

        return row;
    }

    private void Swap(int a, int b)
    {
        if (a < 0 || b < 0 || a >= _picked.Count || b >= _picked.Count) return;
        (_picked[a], _picked[b]) = (_picked[b], _picked[a]);
        ShowHub();
    }

    /// <summary>
    /// A one-line summary of where a hero belongs in the line, worked out from
    /// the ranks their own skills can be launched from.
    ///
    /// Derived rather than hand-written, so it can never disagree with the
    /// actual skills - change a launch pattern and this updates itself.
    /// </summary>
    private string PreferredRanks(HeroDefinition hero)
    {
        // How many of this hero's skills work in each of the three ranks a
        // party of three can occupy.
        var usable = new int[Campaign.PartySize + 1];
        foreach (string skillId in hero.SkillIds)
        {
            Ranks launch = _content.Skill(skillId).LaunchRanks;
            for (int r = 1; r <= Campaign.PartySize; r++)
                if (launch.Includes(r)) usable[r]++;
        }

        int best = usable.Skip(1).Max();
        var good = Enumerable.Range(1, Campaign.PartySize).Where(r => usable[r] == best).ToList();

        if (good.Count == Campaign.PartySize) return "anywhere in the line";
        return good.Count == 1
            ? $"best in rank {good[0]}"
            : $"best in ranks {good.First()}-{good.Last()}";
    }

    /// <summary>One pickable hero in the roster grid.</summary>
    private Control HeroCard(HeroDefinition hero)
    {
        bool chosen = _picked.Contains(hero.Id);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(126, 0) };
        panel.AddThemeStyleboxOverride("panel", UiTheme.Flat(
            chosen ? new Color(0.20f, 0.26f, 0.20f) : new Color(0.11f, 0.10f, 0.15f),
            radius: 3, padding: 6));

        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", 1);
        panel.AddChild(col);

        col.AddChild(Portrait(hero.SpriteName, 2));
        col.AddChild(UiTheme.MakeLabel(hero.Label, 13,
            chosen ? UiTheme.HealGreen : UiTheme.HeroBlue, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel(hero.Role, 10, UiTheme.TextFaint, HorizontalAlignment.Center));

        var s = hero.Stats;
        col.AddChild(UiTheme.MakeLabel($"{s.MaxHealth}hp  {s.Attack}atk", 10, UiTheme.TextDim, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel($"{s.Defense}def  {s.Speed}spd", 10, UiTheme.TextDim, HorizontalAlignment.Center));

        // Where this hero's kit actually works. Reading ten of these is how you
        // learn to build a line without trial and error.
        col.AddChild(UiTheme.MakeLabel(PreferredRanks(hero), 10, UiTheme.Gold, HorizontalAlignment.Center));

        var pick = new Button { Text = chosen ? "Chosen" : "Pick", TooltipText = hero.Blurb };
        pick.AddThemeFontSizeOverride("font_size", 11);
        pick.Pressed += () =>
        {
            Audio.Play(chosen ? "ui_back" : "ui_select", -10f);
            if (chosen) _picked.Remove(hero.Id);
            else if (_picked.Count < Campaign.PartySize) _picked.Add(hero.Id);
            else Audio.Play("ui_error", -10f);
            ShowHub();                                  // rebuild with the new selection
        };
        col.AddChild(pick);

        return panel;
    }

    /// <summary>The found weapons, each assignable to a chosen hero.</summary>
    private Control LootRack(Campaign c)
    {
        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 4);

        // Best first, and only the most recent dozen - a full armoury after nine
        // encounters is a wall of buttons nobody reads.
        foreach (LootDrop drop in c.Loot
                     .OrderByDescending(l => (int)l.Weapon.Rarity)
                     .Take(12))
        {
            grid.AddChild(WeaponCard(c, drop.Weapon));
        }

        return grid;
    }

    private Control WeaponCard(Campaign c, WeaponDefinition weapon)
    {
        Actor? holder = c.Party.FirstOrDefault(h => h.Weapon?.Id == weapon.Id);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(168, 0) };
        panel.AddThemeStyleboxOverride("panel",
            UiTheme.Flat(new Color(0.10f, 0.09f, 0.14f), radius: 3, padding: 5));

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        panel.AddChild(row);

        Texture2D? icon = UiTheme.Texture($"weapons/{weapon.IconName}.png");
        if (icon is not null)
            row.AddChild(new TextureRect
            {
                Texture = icon,
                CustomMinimumSize = new Vector2(32, 32),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            });

        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 0);
        row.AddChild(col);

        col.AddChild(UiTheme.MakeLabel(weapon.Label, 12, RarityColour(weapon.Rarity)));
        col.AddChild(UiTheme.MakeLabel(weapon.Summary, 10, UiTheme.TextDim));

        if (holder is not null)
        {
            col.AddChild(UiTheme.MakeLabel($"held by {holder.Name}", 10, UiTheme.HealGreen));
        }
        else if (_picked.Count != Campaign.PartySize)
        {
            // CRASH GUARD. These buttons call SetParty, which insists on exactly
            // three heroes. Deselecting somebody while holding loot and then
            // clicking one used to throw straight out of the button handler.
            col.AddChild(UiTheme.MakeLabel("pick a full party to equip", 10, UiTheme.TextFaint));
        }
        else
        {
            var give = new HBoxContainer();
            give.AddThemeConstantOverride("separation", 2);
            foreach (string heroId in _picked)
            {
                HeroDefinition h = _content.Hero(heroId);
                var b = new Button { Text = h.Label[..Mathf.Min(3, h.Label.Length)], TooltipText = $"Give to {h.Label}" };
                b.AddThemeFontSizeOverride("font_size", 10);
                b.Pressed += () =>
                {
                    Audio.Play("item_pickup", -8f);
                    c.SetParty(_picked);            // make sure the party objects exist
                    c.EquipOn(heroId, weapon);
                    ShowHub();
                };
                give.AddChild(b);
            }
            col.AddChild(give);
        }

        return panel;
    }

    private static Color RarityColour(Rarity r) => r switch
    {
        Rarity.Legendary => UiTheme.Gold,
        Rarity.Epic => new Color("c07fe0"),
        Rarity.Rare => UiTheme.HeroBlue,
        Rarity.Uncommon => UiTheme.HealGreen,
        _ => UiTheme.TextDim,
    };

    // ==================================================================
    //  Dungeon
    // ==================================================================

    private void BeginEncounter()
    {
        List<GameEvent> opening = _campaign!.BeginEncounter();
        BattleView view = SwapScreen<BattleView>();
        view.Begin(_campaign, _content, opening, OnEncounterFinished);
    }

    private void OnEncounterFinished()
    {
        Campaign c = _campaign!;
        LootDrop? drop = c.CompleteEncounter();

        if (c.Phase is CampaignPhase.Won or CampaignPhase.Lost) { ShowResults(); return; }
        ShowLoot(drop);
    }

    /// <summary>What dropped, and where the party stands, between encounters.</summary>
    private void ShowLoot(LootDrop? drop)
    {
        Campaign c = _campaign!;
        VBoxContainer col = MenuScreen(SwapScreen<Control>());

        bool dungeonDone = c.Phase == CampaignPhase.Hub;
        Audio.Play(dungeonDone ? "level_up" : "chest_open", -4f);

        col.AddChild(UiTheme.MakeLabel(dungeonDone ? "DUNGEON CLEARED" : "ENCOUNTER CLEARED",
            30, UiTheme.HealGreen, HorizontalAlignment.Center));

        if (drop is not null)
        {
            col.AddChild(Spacer(8));
            col.AddChild(UiTheme.MakeLabel("You find:", 13, UiTheme.TextDim, HorizontalAlignment.Center));

            var lootRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            lootRow.AddThemeConstantOverride("separation", 8);

            Texture2D? icon = UiTheme.Texture($"weapons/{drop.Weapon.IconName}.png");
            if (icon is not null)
                lootRow.AddChild(new TextureRect
                {
                    Texture = icon,
                    CustomMinimumSize = new Vector2(64, 64),
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                });

            var info = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            info.AddChild(UiTheme.MakeLabel(drop.Weapon.Label, 20, RarityColour(drop.Weapon.Rarity)));
            info.AddChild(UiTheme.MakeLabel(drop.Weapon.RarityLabel, 12, UiTheme.TextFaint));
            info.AddChild(UiTheme.MakeLabel(drop.Weapon.Summary, 13, UiTheme.TextBright));
            lootRow.AddChild(info);

            col.AddChild(lootRow);

            // Hand it out straight away, without walking back to camp.
            var give = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            give.AddThemeConstantOverride("separation", 6);
            give.AddChild(UiTheme.MakeLabel("Equip on:", 12, UiTheme.TextDim));
            foreach (Actor hero in c.Party)
            {
                var b = new Button { Text = hero.Name };
                b.AddThemeFontSizeOverride("font_size", 12);
                b.Pressed += () =>
                {
                    Audio.Play("item_pickup", -6f);
                    c.EquipOn(hero.Id, drop.Weapon);
                    ShowLoot(drop);
                };
                give.AddChild(b);
            }
            col.AddChild(give);
        }

        col.AddChild(Spacer(12));
        col.AddChild(PartyStatus(c));
        col.AddChild(Spacer(14));

        col.AddChild(CentredButton(dungeonDone ? "  Return to camp  " : "  Press on  ", 17, () =>
        {
            if (dungeonDone) ShowHub();
            else BeginEncounter();
        }));
    }

    /// <summary>Everyone's health, which is the decision the player is really making.</summary>
    private static Control PartyStatus(Campaign c)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);

        foreach (Actor hero in c.Party)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);

            Label name = UiTheme.MakeLabel(hero.Name, 13,
                hero.IsAlive ? UiTheme.TextBright : UiTheme.TextFaint);
            name.CustomMinimumSize = new Vector2(140, 0);
            row.AddChild(name);

            float frac = (float)hero.Health / hero.MaxHealth;
            var bar = new ProgressBar
            {
                MinValue = 0, MaxValue = hero.MaxHealth, Value = hero.Health,
                ShowPercentage = false, CustomMinimumSize = new Vector2(230, 13),
            };
            bar.AddThemeStyleboxOverride("fill", UiTheme.Flat(UiTheme.HealthColour(frac)));
            row.AddChild(bar);

            Label hp = UiTheme.MakeLabel($"{hero.Health}/{hero.MaxHealth}", 12,
                UiTheme.TextDim, HorizontalAlignment.Right);
            hp.CustomMinimumSize = new Vector2(66, 0);
            row.AddChild(hp);

            row.AddChild(UiTheme.MakeLabel(hero.Weapon?.Label ?? "unarmed", 11, UiTheme.Gold));
            col.AddChild(row);
        }

        return col;
    }

    // ==================================================================
    //  Results
    // ==================================================================

    private void ShowResults()
    {
        Campaign c = _campaign!;
        bool won = c.Phase == CampaignPhase.Won;
        RunStats s = c.Stats;

        VBoxContainer col = MenuScreen(SwapScreen<Control>());

        // No sting here. BattleView already played victory or defeat the moment
        // the last fight ended, and this screen arrives less than a second
        // later - so sounding it again just played the same cue twice over
        // itself. The sting belongs to the fight, which is where the moment is.

        col.AddChild(UiTheme.MakeLabel(won ? "THE CRYPT IS SILENT" : "THE PARTY FALLS",
            32, won ? UiTheme.Gold : UiTheme.HealthLow, HorizontalAlignment.Center));
        col.AddChild(UiTheme.MakeLabel(
            won ? "All three dungeons cleared."
                : $"Fell in {c.CurrentDungeon.Label}, encounter {c.EncounterNumber} of {c.TotalEncounters}.",
            13, UiTheme.TextDim, HorizontalAlignment.Center));
        col.AddChild(Spacer(12));

        var gradeRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        gradeRow.AddThemeConstantOverride("separation", 12);
        gradeRow.AddChild(UiTheme.MakeLabel("RANK", 15, UiTheme.TextFaint));
        gradeRow.AddChild(UiTheme.MakeLabel(c.Grade, 48, GradeColour(c.Grade)));
        col.AddChild(gradeRow);
        col.AddChild(Spacer(12));

        AddStat(col, "Dungeons cleared", $"{s.DungeonsCleared} / {c.TotalDungeons}");
        AddStat(col, "Encounters cleared", s.EncountersCleared.ToString());
        AddStat(col, "Enemies defeated", s.EnemiesDefeated.ToString());
        AddStat(col, "Rounds fought", s.RoundsFought.ToString());
        col.AddChild(Divider());
        AddStat(col, "Damage dealt", s.DamageDealt.ToString());
        AddStat(col, "Damage taken", s.DamageTaken.ToString());
        AddStat(col, "Healing done", s.HealingDone.ToString());
        AddStat(col, "Biggest single hit", s.BiggestHit.ToString(), UiTheme.CritGold);
        col.AddChild(Divider());
        AddStat(col, "Critical hits", s.CriticalHits.ToString());
        AddStat(col, "Statuses inflicted", s.StatusesApplied.ToString());
        AddStat(col, "Turns lost to stun", s.TurnsLostToStun.ToString(),
            s.TurnsLostToStun > 0 ? UiTheme.HealthLow : UiTheme.TextDim);
        AddStat(col, "Heroes lost", s.HeroesLost.ToString(),
            s.HeroesLost == 0 ? UiTheme.HealGreen : UiTheme.HealthLow);
        AddStat(col, "Weapons found", c.Loot.Count.ToString(), UiTheme.Gold);

        col.AddChild(Spacer(16));

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 10);

        var again = new Button { Text = "  Try again  " };
        again.AddThemeFontSizeOverride("font_size", 16);
        again.Pressed += () =>
        {
            _nextSeed++;
            _campaign = new Campaign(_content, _nextSeed);
            _picked.Clear();
            _picked.AddRange(_campaign.Party.Select(p => p.Id));
            ShowHub();
        };
        buttons.AddChild(again);

        var title = new Button { Text = "  Title  " };
        title.Pressed += () => { _nextSeed++; ShowTitle(); };
        buttons.AddChild(title);
        col.AddChild(buttons);

        col.AddChild(Spacer(4));
        col.AddChild(UiTheme.MakeLabel($"seed {c.Seed}", 11, UiTheme.TextFaint, HorizontalAlignment.Center));
    }

    private static void AddStat(VBoxContainer column, string label, string value, Color? accent = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        row.CustomMinimumSize = new Vector2(420, 0);

        Label name = UiTheme.MakeLabel(label, 13, UiTheme.TextDim);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(name);
        row.AddChild(UiTheme.MakeLabel(value, 14, accent ?? UiTheme.TextBright, HorizontalAlignment.Right));
        column.AddChild(row);
    }

    private static Color GradeColour(string grade) => grade switch
    {
        "S" => UiTheme.Gold,
        "A" => UiTheme.HealGreen,
        "B" => UiTheme.HeroBlue,
        "C" => UiTheme.TextDim,
        _ => UiTheme.HealthLow,
    };

    // ==================================================================
    //  Small helpers
    // ==================================================================

    private static Control Portrait(string spriteName, int scale)
    {
        Texture2D? tex = UiTheme.Texture($"chars/{spriteName}.png");
        if (tex is null) return new Control();

        return new TextureRect
        {
            Texture = tex,
            CustomMinimumSize = new Vector2(32 * scale, 40 * scale),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        };
    }

    private static Control CentredButton(string text, int fontSize, System.Action onPressed)
    {
        var button = new Button { Text = text };
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.Pressed += () => { Audio.Play("ui_select", -8f); onPressed(); };

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddChild(button);
        return row;
    }

    private static Label WrappedLabel(string text, int size, Color colour, int width)
    {
        Label label = UiTheme.MakeLabel(text, size, colour);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(width, 0);
        return label;
    }

    private static Control Spacer(int height) => new() { CustomMinimumSize = new Vector2(0, height) };

    private static Control Divider()
    {
        var line = new ColorRect { Color = new Color(1, 1, 1, 0.07f), CustomMinimumSize = new Vector2(0, 1) };
        var wrap = new MarginContainer();
        wrap.AddThemeConstantOverride("margin_top", 4);
        wrap.AddThemeConstantOverride("margin_bottom", 4);
        wrap.AddChild(line);
        return wrap;
    }

    // ==================================================================
    //  Screenshot harness (dev tool):  godot --path game -- --shots
    // ==================================================================

    private async Task CaptureShots()
    {
        await Shot("01_title", 1.2);

        _campaign = new Campaign(_content, 4);
        _picked.Clear();
        _picked.AddRange(new[] { "warrior", "cleric", "mage" });
        ShowHub();
        await Shot("02_hub", 0.9);

        _campaign.SetParty(_picked);
        _campaign.EnterDungeon();
        BeginEncounter();
        await Shot("03_battle", 3.4);

        // Simulate to a loot screen and a finished campaign - possible in a few
        // lines only because the rules need no engine at all.
        var sim = new Campaign(_content, 4);
        sim.SetParty("warrior", "cleric", "mage");
        sim.EnterDungeon();
        sim.BeginEncounter();
        while (!sim.Battle.IsOver)
            sim.TakeTurn(Rpg.Core.Ai.ScoringAi.ChooseAction(sim.Battle, sim.Battle.Current!));
        LootDrop? drop = sim.CompleteEncounter();
        _campaign = sim;
        if (sim.Phase is CampaignPhase.InDungeon or CampaignPhase.Hub) ShowLoot(drop);
        await Shot("04_loot", 0.8);

        while (sim.Phase is CampaignPhase.Hub or CampaignPhase.InDungeon)
        {
            if (sim.Phase == CampaignPhase.Hub) { sim.EnterDungeon(); continue; }
            sim.BeginEncounter();
            while (!sim.Battle.IsOver)
                sim.TakeTurn(Rpg.Core.Ai.ScoringAi.ChooseAction(sim.Battle, sim.Battle.Current!));
            sim.CompleteEncounter();
        }
        ShowResults();
        await Shot("05_results", 0.8);

        GD.Print("SHOTS_DIR=" + ProjectSettings.GlobalizePath("user://"));
        GetTree().Quit();
    }

    private async Task Shot(string name, double waitSeconds)
    {
        await ToSignal(GetTree().CreateTimer(waitSeconds), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng($"user://{name}.png");
        GD.Print($"saved {name}.png");
    }
}

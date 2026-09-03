// ============================================================================
//  ACTORVIEW - one fighter on screen
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  An animated sprite, a name, a health bar, a row of status icons, and the
//  reactions that make a turn-based game feel alive: a flinch and a shake when
//  hit, a green pulse when healed, a glow on whoever is acting, and a death
//  animation that stays down.
//
//  It has now been through three versions, and the third one is the point:
//
//      v1  a stick figure drawn with DrawLine
//      v2  a static pixel-art PNG
//      v3  a five-animation sprite from the asset pack
//
//  Each of those was a change to THIS FILE ONLY. The rules never knew what a
//  fighter looked like, so the art could be replaced twice without a single
//  line of combat code moving. That is the whole argument of this project,
//  demonstrated rather than asserted.
//
//  IT ONLY READS
//  -------------
//  It holds an Actor and never modifies one - it could not even if it tried,
//  because Actor.Health has a private setter and the mutating methods are
//  internal, so they are invisible from this project.
// ============================================================================

using System.Linq;
using System.Threading.Tasks;
using Godot;
using Rpg.Core.Entities;

namespace StickmanRpg.Game;

public partial class ActorView : VBoxContainer
{
    private const int SpriteScale = 3;      // 32x40 art -> 96x120 on screen
    private const int StageWidth = 112;
    private const int StageHeight = 124;

    private Actor _actor = null!;

    private Panel _highlight = null!;
    private Control _stage = null!;
    private SpriteAnimator _sprite = null!;
    private Label _name = null!;
    private Label _rank = null!;
    private ProgressBar _bar = null!;
    private Label _hp = null!;
    private HBoxContainer _statusRow = null!;

    // A fighter falls exactly once. See BeginDeath.
    private bool _deathShown;

    // The tween currently driving `modulate`, so a new one can cancel it rather
    // than fight it frame by frame.
    private Tween? _colourTween;

    public Actor Actor => _actor;

    /// <summary>Where an effect or damage number should appear, in global coordinates.</summary>
    public Vector2 ImpactPoint => _stage.GetGlobalRect().GetCenter();

    // ------------------------------------------------------------------

    public void Bind(Actor actor)
    {
        _actor = actor;
        _deathShown = false;

        CustomMinimumSize = new Vector2(StageWidth + 14, 198);
        AddThemeConstantOverride("separation", 2);
        Alignment = AlignmentMode.End;

        // --- status icons, above the head
        _statusRow = new HBoxContainer { Alignment = AlignmentMode.Center };
        _statusRow.AddThemeConstantOverride("separation", 2);
        _statusRow.CustomMinimumSize = new Vector2(0, 18);
        AddChild(_statusRow);

        // --- the sprite sits in a fixed-size stage so it can shake without
        //     shoving the rest of the layout around
        _stage = new Control { CustomMinimumSize = new Vector2(StageWidth, StageHeight) };
        AddChild(_stage);

        _highlight = new Panel();
        _highlight.SetAnchorsPreset(LayoutPreset.FullRect);
        _highlight.AddThemeStyleboxOverride("panel",
            UiTheme.Flat(new Color(1, 1, 1, 0.06f), radius: 4));
        _highlight.Visible = false;
        _stage.AddChild(_highlight);

        _sprite = new SpriteAnimator();
        _stage.AddChild(_sprite);
        _sprite.Setup(actor.SpriteName, SpriteScale);

        // Sized and placed by hand rather than anchored, because the lunge and
        // shake below tween its position - and a VBoxContainer would fight any
        // tween applied to _stage itself.
        _sprite.Size = new Vector2(StageWidth, StageHeight);
        _sprite.Position = Vector2.Zero;

        // The party stands on the LEFT facing right; the enemy on the RIGHT
        // facing left. With the two lines side by side this is what makes the
        // formation legible - you can see who is in front of whom.
        if (actor.Team == Team.Monsters)
            _sprite.FlipH = true;

        // --- rank badge. The single most important number on screen once
        //     skills care about position.
        _rank = UiTheme.MakeLabel("", 11, UiTheme.TextFaint, HorizontalAlignment.Center);
        AddChild(_rank);

        // --- name
        _name = UiTheme.MakeLabel(actor.Name, 13,
            actor.Team == Team.Heroes ? UiTheme.HeroBlue : UiTheme.MonsterRed,
            HorizontalAlignment.Center);
        AddChild(_name);

        // --- health bar
        _bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = actor.MaxHealth,
            Value = actor.Health,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 9),
        };
        AddChild(_bar);

        _hp = UiTheme.MakeLabel("", 11, UiTheme.TextDim, HorizontalAlignment.Center);
        AddChild(_hp);

        Refresh(false, 1);
    }

    /// <summary>Re-reads everything from the Actor. Cheap; call after every event.</summary>
    public void Refresh(bool isCurrentTurn, int rank = 0)
    {
        bool alive = _actor.IsAlive;

        // Rank 1 is the front line. Marked, because "why can't my Warrior hit
        // that?" has to be answerable at a glance.
        _rank.Text = !alive ? "" : rank == 1 ? "> FRONT <" : $"rank {rank}";
        _rank.AddThemeColorOverride("font_color",
            rank == 1 ? UiTheme.Gold : UiTheme.TextDim);

        _highlight.Visible = isCurrentTurn && alive;

        _name.AddThemeColorOverride("font_color",
            !alive ? UiTheme.TextFaint
            : _actor.Team == Team.Heroes ? UiTheme.HeroBlue : UiTheme.MonsterRed);

        float fraction = _actor.MaxHealth == 0 ? 0 : (float)_actor.Health / _actor.MaxHealth;
        _bar.MaxValue = _actor.MaxHealth;
        _bar.Value = _actor.Health;
        _bar.AddThemeStyleboxOverride("fill", UiTheme.Flat(UiTheme.HealthColour(fraction)));
        _hp.Text = alive ? $"{_actor.Health} / {_actor.MaxHealth}" : "down";
        _hp.AddThemeColorOverride("font_color", alive ? UiTheme.TextDim : UiTheme.TextFaint);

        // The model is already fully resolved by the time the log is replayed,
        // so an actor reads as dead from the Damaged event onwards - before the
        // Died event that formally announces it arrives. Starting the animation
        // here keeps a death from ever going unnoticed; BeginDeath is what stops
        // it happening twice.
        if (!alive) BeginDeath();

        RebuildStatusIcons();
    }

    private void RebuildStatusIcons()
    {
        foreach (Node child in _statusRow.GetChildren())
        {
            _statusRow.RemoveChild(child);
            child.QueueFree();
        }

        if (!_actor.IsAlive) return;

        foreach (var status in _actor.Statuses)
        {
            var box = new HBoxContainer();
            box.AddThemeConstantOverride("separation", 0);

            // Status icons are frame 0 of the matching effect animation - so
            // poison's icon is literally the first frame of the poison splash.
            // Free consistency, and no extra art to keep in sync.
            string? fx = Sfx.FxForStatus(status.Id);
            Texture2D? sheet = fx is null ? null : UiTheme.Texture($"fx/{fx}_strip.png");

            if (sheet is not null)
            {
                box.AddChild(new TextureRect
                {
                    Texture = new AtlasTexture { Atlas = sheet, Region = new Rect2(0, 0, 32, 32) },
                    CustomMinimumSize = new Vector2(17, 17),
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    TooltipText = $"{status.Definition.Name}: {status.Definition.Description}",
                });
            }

            box.AddChild(UiTheme.MakeLabel($"{status.RemainingTurns}", 10, UiTheme.Gold));
            _statusRow.AddChild(box);
        }
    }

    // ------------------------------------------------------------------
    //  Reactions
    // ------------------------------------------------------------------

    /// <summary>Wind up and swing. Awaited so the caller can land the hit on the right frame.</summary>
    public async Task PlayAttack()
    {
        // A small lunge towards the enemy, on top of the attack animation.
        float lunge = _actor.Team == Team.Heroes ? 14f : -14f;
        Tween step = CreateTween();
        step.TweenProperty(_sprite, "position:x", lunge, 0.10)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        step.TweenProperty(_sprite, "position:x", 0f, 0.22)
            .SetTrans(Tween.TransitionType.Quad);

        await _sprite.PlayOnce("attack");
    }

    /// <summary>
    /// Runs a colour tween, cancelling any that is still going.
    ///
    /// Two tweens driving `modulate` at once fight for it frame by frame, which
    /// is exactly what happened to a fighter hit and killed in the same instant:
    /// the hit flash kept yanking the sprite back to opaque white while the
    /// death fade tried to dim it.
    /// </summary>
    private Tween NewColourTween()
    {
        _colourTween?.Kill();
        _colourTween = CreateTween();
        return _colourTween;
    }

    /// <summary>Flinch, flash and shake.</summary>
    public async Task PlayHit(bool critical)
    {
        _sprite.Play("hurt", restart: true);

        Tween flash = NewColourTween();
        flash.TweenProperty(_sprite, "modulate",
            critical ? new Color(2.4f, 1.5f, 0.6f) : new Color(2.0f, 0.7f, 0.7f), 0.04);
        flash.TweenProperty(_sprite, "modulate", Colors.White, 0.22);

        float power = critical ? 10f : 5f;
        Tween shake = CreateTween();
        shake.TweenProperty(_sprite, "position:x", power, 0.04);
        shake.TweenProperty(_sprite, "position:x", -power, 0.06);
        shake.TweenProperty(_sprite, "position:x", 0f, 0.06);

        await ToSignal(shake, Tween.SignalName.Finished);
    }

    /// <summary>A soft green pulse.</summary>
    public async Task PlayHeal()
    {
        Tween t = NewColourTween();
        t.TweenProperty(_sprite, "modulate", new Color(0.6f, 1.8f, 0.9f), 0.08);
        t.TweenProperty(_sprite, "modulate", Colors.White, 0.26);

        // Waits on a timer rather than the tween's own `finished` signal,
        // because a cancelled tween never emits it - and awaiting a signal that
        // can never arrive would freeze the whole replay.
        await ToSignal(GetTree().CreateTimer(0.34), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>
    /// Starts the death animation, at most once per fighter.
    ///
    /// THE BUG THIS EXISTS TO KILL. A death is noticed twice: once by Refresh,
    /// when the health bar first reads zero, and again when the Died event is
    /// replayed a beat later. Both used to call Play("death", restart: true), so
    /// the fighter dropped, snapped upright, and dropped again - most visibly
    /// when poison did the killing, because a status tick puts a longer gap
    /// between the two. Returns true only for the caller that actually started
    /// it.
    /// </summary>
    private bool BeginDeath()
    {
        if (_deathShown) return false;
        _deathShown = true;

        _sprite.Play("death", restart: true);

        Tween fade = NewColourTween();
        fade.TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 0.75f), 0.45);
        return true;
    }

    /// <summary>
    /// Fall over and stay there. Safe to call after Refresh already started the
    /// fall - it then simply waits for the rest of it.
    /// </summary>
    public async Task PlayDeath()
    {
        BeginDeath();
        await _sprite.WaitForCurrent();
    }
}

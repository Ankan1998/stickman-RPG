// ============================================================================
//  SPRITEANIMATOR - plays a strip of frames
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The asset pack stores animations as horizontal STRIPS: warrior_attack_strip.png
//  is 192x40, which is six 32x40 frames side by side.
//
//        +------+------+------+------+------+------+
//        |  0   |  1   |  2   |  3   |  4   |  5    |   192 x 40
//        +------+------+------+------+------+------+
//
//  This node shows one frame at a time out of that single image, using an
//  AtlasTexture - a "window" onto a region of a bigger texture. Changing the
//  window is free; there is no per-frame texture loading and no memory cost.
//
//  ANIMATIONS AVAILABLE ON EVERY CHARACTER
//  ---------------------------------------
//      idle    4 frames  @  6 fps   loops
//      walk    6 frames  @ 10 fps   loops
//      attack  6 frames  @ 12 fps   plays once, then back to idle
//      hurt    3 frames  @ 14 fps   plays once, then back to idle
//      death   6 frames  @  8 fps   plays once, then HOLDS on the last frame
//
//  WHY NOT AnimatedSprite2D?
//  -------------------------
//  Godot has one, and it is good. But it wants a SpriteFrames resource built in
//  the editor, and this project builds its UI in code. Forty lines here keeps
//  everything in one place and works for any character without an editor step.
// ============================================================================

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace StickmanRpg.Game;

public partial class SpriteAnimator : TextureRect
{
    /// <summary>One animation: how many frames, how fast, and whether it repeats.</summary>
    private readonly record struct Clip(string Name, int Frames, int Fps, bool Loops);

    // Straight from the asset pack's manifest.json.
    private static readonly Dictionary<string, Clip> Clips = new()
    {
        ["idle"] = new("idle", 4, 6, true),
        ["walk"] = new("walk", 6, 10, true),
        ["attack"] = new("attack", 6, 12, false),
        ["hurt"] = new("hurt", 3, 14, false),
        ["death"] = new("death", 6, 8, false),
    };

    private const int FrameWidth = 32;
    private const int FrameHeight = 40;

    private string _spriteName = "";
    private Clip _clip = Clips["idle"];
    private int _frame;
    private double _elapsed;
    private Texture2D? _sheet;

    /// <summary>True once a non-looping clip has reached its final frame.</summary>
    public bool Finished { get; private set; }

    public string Current { get; private set; } = "idle";

    // ------------------------------------------------------------------

    public void Setup(string spriteName, int scale = 3)
    {
        _spriteName = spriteName;
        StretchMode = StretchModeEnum.KeepAspectCentered;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        CustomMinimumSize = new Vector2(FrameWidth * scale, FrameHeight * scale);

        // restart: true is REQUIRED here. Current is initialised to "idle", so a
        // plain Play("idle") would hit the "already playing that" guard below and
        // return without ever loading a texture - leaving the sprite blank until
        // something happened to it.
        Play("idle", restart: true);
    }

    /// <summary>
    /// Switches animation. Restarting the clip you are already playing is a
    /// no-op unless <paramref name="restart"/> is set - otherwise a hero hit
    /// twice in a round never finishes their flinch.
    /// </summary>
    public void Play(string clipName, bool restart = false)
    {
        if (!Clips.TryGetValue(clipName, out Clip clip))
            clip = Clips["idle"];

        if (!restart && Current == clipName && !Finished)
            return;

        Texture2D? sheet = UiTheme.Texture($"chars/{_spriteName}_{clipName}_strip.png");

        // Not every sprite has every animation, and a missing strip should show
        // the still frame rather than nothing at all.
        if (sheet is null)
        {
            Texture = UiTheme.Texture($"chars/{_spriteName}.png");
            Current = clipName;
            Finished = true;
            return;
        }

        _sheet = sheet;
        _clip = clip;
        _frame = 0;
        _elapsed = 0;
        Current = clipName;
        Finished = false;
        ShowFrame(0);
    }

    /// <summary>Plays a one-shot clip and returns when it has finished.</summary>
    public async Task PlayOnce(string clipName)
    {
        Play(clipName, restart: true);
        if (Finished) return;                     // no strip; nothing to wait for

        double seconds = _clip.Frames / (double)_clip.Fps;
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>
    /// Waits for whatever is ALREADY playing to finish, without restarting it.
    ///
    /// The difference from PlayOnce is the whole point. A death can be noticed
    /// by two different pieces of code - the health bar refresh and the Died
    /// event - and only the first should start the animation. The second just
    /// needs to wait for it, and calling PlayOnce there would snap the corpse
    /// back to frame zero and drop it a second time.
    /// </summary>
    public async Task WaitForCurrent()
    {
        if (_sheet is null || Finished) return;

        double remaining = (_clip.Frames - _frame) / (double)_clip.Fps;
        if (remaining <= 0) return;

        await ToSignal(GetTree().CreateTimer(remaining), SceneTreeTimer.SignalName.Timeout);
    }

    public override void _Process(double delta)
    {
        if (_sheet is null || Finished) return;

        _elapsed += delta;
        double perFrame = 1.0 / _clip.Fps;
        if (_elapsed < perFrame) return;

        _elapsed -= perFrame;
        _frame++;

        if (_frame >= _clip.Frames)
        {
            if (_clip.Loops)
            {
                _frame = 0;
            }
            else
            {
                // Hold the last frame. Death in particular must NOT snap back to
                // standing, and a flinch that resets itself looks like a glitch.
                _frame = _clip.Frames - 1;
                Finished = true;

                // A flinch returns to standing; a death stays down.
                if (Current == "hurt" || Current == "attack")
                    Play("idle", restart: true);
                return;
            }
        }

        ShowFrame(_frame);
    }

    /// <summary>Points the atlas window at frame n of the strip.</summary>
    private void ShowFrame(int n)
    {
        if (_sheet is null) return;

        Texture = new AtlasTexture
        {
            Atlas = _sheet,
            Region = new Rect2(n * FrameWidth, 0, FrameWidth, FrameHeight),
        };
    }
}

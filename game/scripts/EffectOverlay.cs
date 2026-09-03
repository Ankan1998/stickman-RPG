// ============================================================================
//  EFFECTOVERLAY - the 32x32 impact animations
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A slash crescent, a splash of blood, a puff of poison, a burst of flame -
//  played over whoever was just hit, then deleted.
//
//  The asset pack has sixteen of these, each a 6-frame strip of 32x32 frames:
//
//      fx_slash   fx_impact  fx_pierce  fx_blood     fx_explosion  fx_fire
//      fx_ice     fx_lightning  fx_poison  fx_arcane  fx_heal      fx_buff
//      fx_debuff  fx_stun    fx_shockwave  fx_smoke
//
//  This is decoration. It changes nothing and it is worth every line: a hit
//  without an impact effect reads as "a number changed", and a hit with one
//  reads as something happening to somebody.
// ============================================================================

using Godot;

namespace StickmanRpg.Game;

public partial class EffectOverlay : TextureRect
{
    private const int FrameSize = 32;
    private const int Frames = 6;
    private const int Fps = 14;

    private Texture2D _sheet = null!;
    private int _frame;
    private double _elapsed;

    /// <summary>
    /// Plays one effect centred on a point in <paramref name="layer"/>'s
    /// coordinates, then frees itself. Does nothing if the effect is missing.
    /// </summary>
    public static void Spawn(Control layer, Vector2 centre, string effectName, int scale = 3)
    {
        Texture2D? sheet = UiTheme.Texture($"fx/{effectName}_strip.png");
        if (sheet is null) return;

        var fx = new EffectOverlay
        {
            _sheet = sheet,
            StretchMode = StretchModeEnum.KeepAspectCentered,
            ExpandMode = ExpandModeEnum.IgnoreSize,
            // Never eat a click - the action menu is underneath this.
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 90,
        };

        int size = FrameSize * scale;
        fx.CustomMinimumSize = new Vector2(size, size);
        fx.Size = new Vector2(size, size);
        fx.Position = centre - new Vector2(size / 2f, size / 2f);

        layer.AddChild(fx);
        fx.ShowFrame(0);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < 1.0 / Fps) return;

        _elapsed = 0;
        _frame++;

        if (_frame >= Frames)
        {
            QueueFree();          // done - remove ourselves, nobody tracks us
            return;
        }

        ShowFrame(_frame);
    }

    private void ShowFrame(int n) => Texture = new AtlasTexture
    {
        Atlas = _sheet,
        Region = new Rect2(n * FrameSize, 0, FrameSize, FrameSize),
    };
}

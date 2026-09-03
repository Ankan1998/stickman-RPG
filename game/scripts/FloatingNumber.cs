// ============================================================================
//  FLOATINGNUMBER - the damage numbers that pop up and drift away
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  A label that appears over a fighter, floats upward, fades out and deletes
//  itself. Pure decoration - it changes nothing about the game.
//
//  It is worth having anyway. In a turn-based game the combat log is the honest
//  record, but a number popping off the goblin you just hit is what makes the
//  hit feel connected to the click. This is about forty lines and it does more
//  for "feel" than any amount of rules work.
//
//  It frees itself when the animation ends, so nothing has to keep track of it.
// ============================================================================

using Godot;

namespace StickmanRpg.Game;

public partial class FloatingNumber : Label
{
    /// <summary>
    /// Spawns a number on <paramref name="layer"/> at a point in that layer's
    /// own coordinates, then animates and removes it.
    /// </summary>
    public static void Spawn(Control layer, Vector2 whereInLayer, string text, Color color,
        int fontSize = 22, bool emphasise = false)
    {
        // Note this is a FloatingNumber, not a plain Label - we need our own
        // Animate() on it below.
        var label = new FloatingNumber
        {
            Text = text,
            ZIndex = 100,
            // Let clicks pass straight through - this must never eat a button press.
            MouseFilter = MouseFilterEnum.Ignore,
        };

        label.AddThemeFontSizeOverride("font_size", emphasise ? fontSize + 8 : fontSize);
        label.AddThemeColorOverride("font_color", color);

        // A dark outline so the number stays readable over any sprite.
        label.AddThemeColorOverride("font_outline_color", UiTheme.Ink);
        label.AddThemeConstantOverride("outline_size", 5);

        layer.AddChild(label);

        // Centre it on the requested point. We can only do that once Godot has
        // worked out how wide the text is, which happens after it enters the tree.
        label.Position = whereInLayer - new Vector2(label.Size.X / 2f, 0);

        label.Animate(emphasise);
    }

    private void Animate(bool emphasise)
    {
        float rise = emphasise ? 62f : 44f;
        float seconds = emphasise ? 0.95f : 0.75f;

        Tween tween = CreateTween();
        tween.SetParallel(true);

        // Drift upward, easing out so it decelerates like it is losing momentum.
        tween.TweenProperty(this, "position:y", Position.Y - rise, seconds)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);

        // Fade out over the back half only, so it is fully readable at first.
        tween.TweenProperty(this, "modulate:a", 0.0f, seconds * 0.55f)
             .SetDelay(seconds * 0.45f);

        if (emphasise)
        {
            // Criticals get a quick punch-in scale.
            PivotOffset = Size / 2f;
            tween.TweenProperty(this, "scale", new Vector2(1.35f, 1.35f), 0.10f);
            tween.Chain().TweenProperty(this, "scale", Vector2.One, 0.18f);
        }

        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}

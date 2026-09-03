// ============================================================================
//  UITHEME - the look of every button, panel and label in the game
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Godot styles its UI through a Theme: a lookup table of "for a Button, the
//  normal background is THIS image, the text is THIS colour, the font is THIS
//  big". Set the theme once on the root node and every child inherits it.
//
//  This is the difference between "grey default Godot buttons" and something
//  that looks designed. It is also the cheapest visual upgrade available: one
//  file, and the entire game changes.
//
//  NINE-SLICE, THE IDEA THAT MAKES THIS WORK
//  -----------------------------------------
//  Our button image is 24x24 pixels, but buttons are all different sizes. A
//  StyleBoxTexture solves that by cutting the image into nine pieces:
//
//        +---+-------+---+     The four CORNERS are drawn as-is.
//        | 1 |   2   | 3 |     The EDGES (2,4,6,8) stretch along one axis.
//        +---+-------+---+     The MIDDLE (5) stretches both ways.
//        | 4 |   5   | 6 |
//        +---+-------+---+     So a 24x24 image can become a 300x40 button
//        | 7 |   8   | 9 |     with its border still exactly 1px thick.
//        +---+-------+---+
//
//  "TextureMargin" says how big the corners are in the SOURCE image.
//  "ContentMargin" says how much padding to leave inside for the text.
//
//  COLOURS
//  -------
//  The palette here matches tools/make_art.py, so the UI and the sprites belong
//  to the same world. If you change one, change the other.
// ============================================================================

using Godot;

namespace StickmanRpg.Game;

public static class UiTheme
{
    // -- the palette, shared with the pixel art ------------------------------
    public static readonly Color Ink = new("14131c");
    public static readonly Color Backdrop = new("0f0e16");
    public static readonly Color TextBright = new("ece9f5");
    public static readonly Color TextDim = new("8d88a8");
    public static readonly Color TextFaint = new("5a5670");
    public static readonly Color Gold = new("e0c46c");
    public static readonly Color HeroBlue = new("6fb3d2");
    public static readonly Color MonsterRed = new("d2795f");
    public static readonly Color HealthGood = new("6dbf73");
    public static readonly Color HealthWarn = new("e0b04a");
    public static readonly Color HealthLow = new("cf5b5b");
    public static readonly Color DamageRed = new("e8695c");
    public static readonly Color HealGreen = new("7fd98a");
    public static readonly Color CritGold = new("ffd97a");

    private const string Assets = "res://assets/";

    /// <summary>Loads a texture from assets/, or null if it is missing.</summary>
    public static Texture2D? Texture(string fileName) =>
        ResourceLoader.Exists(Assets + fileName)
            ? GD.Load<Texture2D>(Assets + fileName)
            : null;

    /// <summary>
    /// Builds the whole theme. Call once and assign to the root Control's
    /// Theme property - every descendant picks it up automatically.
    /// </summary>
    public static Theme Build()
    {
        var theme = new Theme();

        // ---- Buttons ------------------------------------------------------
        theme.SetStylebox("normal", "Button", NineSlice("ui_button.png"));
        theme.SetStylebox("hover", "Button", NineSlice("ui_button_hover.png"));
        theme.SetStylebox("pressed", "Button", NineSlice("ui_button_pressed.png"));
        theme.SetStylebox("disabled", "Button", NineSlice("ui_button_disabled.png"));
        theme.SetStylebox("focus", "Button", new StyleBoxEmpty());

        theme.SetColor("font_color", "Button", TextBright);
        theme.SetColor("font_hover_color", "Button", Gold);
        theme.SetColor("font_pressed_color", "Button", Gold);
        theme.SetColor("font_disabled_color", "Button", TextFaint);
        theme.SetFontSize("font_size", "Button", 15);

        // ---- Panels -------------------------------------------------------
        theme.SetStylebox("panel", "PanelContainer", NineSlice("ui_panel.png"));
        theme.SetStylebox("panel", "Panel", NineSlice("ui_panel.png"));

        // ---- Text ---------------------------------------------------------
        theme.SetColor("font_color", "Label", TextBright);
        theme.SetFontSize("font_size", "Label", 15);

        theme.SetColor("default_color", "RichTextLabel", TextDim);
        theme.SetFontSize("normal_font_size", "RichTextLabel", 14);
        theme.SetFontSize("bold_font_size", "RichTextLabel", 14);
        theme.SetStylebox("normal", "RichTextLabel", new StyleBoxEmpty());

        // ---- Progress bars (the health bars) --------------------------------
        var track = new StyleBoxFlat { BgColor = new Color("15141d") };
        track.SetBorderWidthAll(1);
        track.BorderColor = new Color("3b3750");
        theme.SetStylebox("background", "ProgressBar", track);

        var fill = new StyleBoxFlat { BgColor = HealthGood };
        theme.SetStylebox("fill", "ProgressBar", fill);

        return theme;
    }

    /// <summary>
    /// Turns one of our small bordered PNGs into a stretchable background.
    /// See the nine-slice diagram at the top of this file.
    /// </summary>
    private static StyleBox NineSlice(string fileName, int corner = 4, int padding = 7)
    {
        Texture2D? tex = Texture(fileName);

        // If the art is missing, fall back to a flat colour rather than crashing.
        // A game that still runs with no assets is much easier to work on.
        if (tex is null)
        {
            var flat = new StyleBoxFlat { BgColor = new Color("2a2739") };
            flat.SetCornerRadiusAll(3);
            flat.SetContentMarginAll(padding);
            return flat;
        }

        var box = new StyleBoxTexture { Texture = tex };

        foreach (Side side in new[] { Side.Left, Side.Top, Side.Right, Side.Bottom })
        {
            box.SetTextureMargin(side, corner);    // corner size in the SOURCE image
            box.SetContentMargin(side, padding);   // padding for the text inside
        }

        return box;
    }

    // ------------------------------------------------------------------
    //  Small helpers used all over the UI code
    // ------------------------------------------------------------------

    /// <summary>Health bar colour: green, amber below 55%, red below 30%.</summary>
    public static Color HealthColour(float fraction) =>
        fraction <= 0.30f ? HealthLow
        : fraction <= 0.55f ? HealthWarn
        : HealthGood;

    /// <summary>A plain coloured box, for dividers and backdrops.</summary>
    public static StyleBoxFlat Flat(Color color, int radius = 0, int padding = 0)
    {
        var box = new StyleBoxFlat { BgColor = color };
        if (radius > 0) box.SetCornerRadiusAll(radius);
        if (padding > 0) box.SetContentMarginAll(padding);
        return box;
    }

    /// <summary>A Label with the given text, size and colour. Saves four lines every time.</summary>
    public static Label MakeLabel(string text, int size, Color color,
        HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = align };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}

// ============================================================================
//  DUNGEONBACKDROP - builds a room out of 16x16 tiles
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  The asset pack has 43 dungeon tiles - floors, walls, torches, bones, pillars.
//  Each is 16x16. This stitches them into one 320x180 image at load time, which
//  Godot then scales 4x to fill the window.
//
//  Each dungeon names its own floor and wall tile, so the Warrens are mossy, the
//  Ember Halls are cracked brick and the Frozen Crypt is runed stone - without a
//  single hand-made background image.
//
//  WHY BUILD IT ONCE INTO A TEXTURE
//  --------------------------------
//  The alternative is a TileMap, or hundreds of TextureRect nodes. Both are
//  heavier than this needs to be: the background never changes during a fight,
//  so we can flatten it into one texture and hand the engine a single quad.
//
//  Image.BlitRect is the "paste this rectangle of pixels there" call. Godot
//  gives it to us on the CPU side, which is exactly right for something we do
//  once per encounter and never again.
// ============================================================================

using System.Collections.Generic;
using Godot;

namespace StickmanRpg.Game;

public static class DungeonBackdrop
{
    private const int Tile = 16;
    private const int Width = 320;
    private const int Height = 180;
    private const int FloorLine = 112;      // where the wall stops and the floor starts

    // Building the same room twice a second would be wasteful, so remember them.
    private static readonly Dictionary<string, ImageTexture> Cache = new();

    /// <summary>
    /// Builds (or returns a cached) backdrop for a dungeon. Falls back to the
    /// original hand-generated arena if the tiles are missing.
    /// </summary>
    public static Texture2D? Build(string floorTile, string wallTile, string propTile = "torch_wall")
    {
        string key = $"{floorTile}|{wallTile}|{propTile}";
        if (Cache.TryGetValue(key, out ImageTexture? cached)) return cached;

        Image? floor = LoadTile(floorTile);
        Image? wall = LoadTile(wallTile);
        if (floor is null || wall is null)
            return UiTheme.Texture("bg_arena.png");

        var canvas = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
        canvas.Fill(new Color("14131c"));

        var src = new Rect2I(0, 0, Tile, Tile);

        // --- wall, from the top down to the floor line
        for (int y = 0; y < FloorLine; y += Tile)
            for (int x = 0; x < Width; x += Tile)
                canvas.BlitRect(wall, src, new Vector2I(x, y));

        // --- floor, the rest
        for (int y = FloorLine; y < Height; y += Tile)
            for (int x = 0; x < Width; x += Tile)
                canvas.BlitRect(floor, src, new Vector2I(x, y));

        // --- a pair of props on the wall, for a bit of life
        Image? prop = LoadTile(propTile);
        if (prop is not null)
        {
            canvas.BlitRect(prop, src, new Vector2I(Tile * 3, Tile * 2));
            canvas.BlitRect(prop, src, new Vector2I(Width - Tile * 4, Tile * 2));
        }

        Darken(canvas);

        ImageTexture texture = ImageTexture.CreateFromImage(canvas);
        Cache[key] = texture;
        return texture;
    }

    private static Image? LoadTile(string name)
    {
        Texture2D? tex = UiTheme.Texture($"dungeon/{name}.png");
        return tex?.GetImage();
    }

    /// <summary>
    /// Dims the whole thing, and dims the edges further.
    ///
    /// Backgrounds built from tiles are always too busy and too bright: the
    /// fighters have to read against them, and a wall with as much contrast as a
    /// character is a wall you accidentally look at. Pushing it back is what
    /// makes the sprites pop.
    /// </summary>
    private static void Darken(Image image)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Color c = image.GetPixel(x, y);

                // flat dim
                c = c.Darkened(0.42f);

                // vignette
                float dx = Mathf.Abs(x - Width / 2f) / (Width / 2f);
                float dy = Mathf.Abs(y - Height / 2f) / (Height / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 0.55f)
                    c = c.Darkened(Mathf.Min(0.72f, (d - 0.55f) * 1.25f));

                image.SetPixel(x, y, c);
            }
        }
    }
}

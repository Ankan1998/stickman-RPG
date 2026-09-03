# 13. Sprites and animation

> **Where you are:** chapter 13 of 17 · [index](README.md) · previous: [Enemy AI](12-enemy-ai.md) · next: [Audio](14-audio.md)

---

## The problem

You have a PNG of a warrior. You want him to stand, walk, swing a sword, flinch
and die.

Naively that is five PNGs times six frames = thirty files per character, times
thirty characters = nine hundred files. And every one of them is a separate disk
read, a separate texture uploaded to the graphics card, and a separate thing to
get wrong.

There is a much better way, and it is the foundation of all 2D animation.

---

## The idea: a sprite sheet

Put all the frames in **one image**, side by side.

```
   warrior_attack_strip.png     192 x 40 pixels
   +------+------+------+------+------+------+
   |  0   |  1   |  2   |  3   |  4   |  5   |
   +------+------+------+------+------+------+
      32     32     32     32     32     32
```

Then, to animate, you do not load different images. You **move a window** over
this one image.

In Godot that window is an `AtlasTexture`:

```csharp
Texture = new AtlasTexture
{
    Atlas  = _sheet,                                  // the whole strip
    Region = new Rect2(n * 32, 0, 32, 40),            // just frame n
};
```

Changing the window is essentially free. There is no per-frame disk read and no
extra memory. One texture, six frames.

**Why this matters beyond convenience:** sending a texture to the GPU is
expensive, and switching between textures while drawing is expensive. One sheet
means one upload and one switch. This is why professional 2D games pack
*everything* — every character, every effect, every UI element — into a handful
of large atlases.

---

## In this project

Every character in the asset pack ships as five strips:

| Clip | Frames | FPS | Loops? |
|---|---|---|---|
| `idle` | 4 | 6 | yes |
| `walk` | 6 | 10 | yes |
| `attack` | 6 | 12 | no — returns to idle |
| `hurt` | 3 | 14 | no — returns to idle |
| `death` | 6 | 8 | **no — holds the last frame** |

Encoded as data in
[`SpriteAnimator`](../../game/scripts/SpriteAnimator.cs):

```csharp
private readonly record struct Clip(string Name, int Frames, int Fps, bool Loops);

private static readonly Dictionary<string, Clip> Clips = new()
{
    ["idle"]   = new("idle",   4,  6, true),
    ["walk"]   = new("walk",   6, 10, true),
    ["attack"] = new("attack", 6, 12, false),
    ["hurt"]   = new("hurt",   3, 14, false),
    ["death"]  = new("death",  6,  8, false),
};
```

### The frame clock

```csharp
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
        if (_clip.Loops) _frame = 0;
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
```

This is [chapter 2](02-the-game-loop-and-time.md)'s delta time doing real work.
The animation runs at 6fps or 14fps *regardless* of whether the game is drawing
at 30 or 144.

Note `_elapsed -= perFrame` rather than `_elapsed = 0`. Setting it to zero throws
away the leftover and makes animations run slightly slow; subtracting carries the
remainder forward and keeps the average exact.

### Why not `AnimatedSprite2D`?

Godot has a built-in animated sprite node. This project does not use it, and
says why:

> It wants a `SpriteFrames` resource built in the editor, and this project builds
> its UI in code. Forty lines here keeps everything in one place and works for any
> character without an editor step.

**For your own game, use `AnimatedSprite2D`.** It is well-tested and free. The
hand-rolled version here exists because this repository is a teaching artifact
where everything must be readable as text — the same trade-off as
[chapter 3](03-engines-and-the-scene-tree.md)'s code-built UI.

---

## Pixel art: the one setting that ruins everything

Here is the single most common "why does my pixel art look terrible" problem.

You draw a crisp 32×40 character. You scale it 3× to show it on a modern screen.
It comes out a **blurry smear**.

The cause: by default, GPUs use **bilinear filtering** when scaling — blending
neighbouring pixels to smooth the result. Excellent for photographs. Catastrophic
for pixel art, where the hard edges *are* the art.

The fix is one line in
[`project.godot`](../../game/project.godot):

```ini
[rendering]
; 0 = Nearest. Without this every sprite is bilinear-filtered into a blurry
; smear when scaled up, which is the classic "why does my pixel art look bad"
; problem. This one line is the whole fix.
textures/canvas_textures/default_texture_filter=0
```

**Nearest-neighbour** filtering picks the closest pixel and does not blend. Your
3× sprite becomes crisp 3×3 blocks.

```
   BILINEAR (default)          NEAREST (what you want)
   soft, blurry edges          hard, crunchy edges
   good for photos             good for pixel art
```

### Integer scaling

The other pixel-art rule: **scale by whole numbers only.** 2×, 3×, 4×. Scale by
2.5× and some source pixels become 2 screen pixels while others become 3, which
gives you visible uneven "wobble" along every edge.

This project uses `SpriteScale = 3` — 32×40 art becomes exactly 96×120 on screen.

---

## Tweens: animating things that are not sprites

A sprite strip animates a *character*. What about sliding a health bar, fading a
corpse, or shaking a sprite when it is hit?

That is a **tween** — short for "in-betweening": you name a start, an end and a
duration, and the engine fills in the frames.

```csharp
Tween shake = CreateTween();
shake.TweenProperty(_sprite, "position:x",  power, 0.04);
shake.TweenProperty(_sprite, "position:x", -power, 0.06);
shake.TweenProperty(_sprite, "position:x",      0, 0.06);
```

Three lines, and a hit now has weight. Tweens chain by default — each step waits
for the previous one.

The lunge in `PlayAttack` adds **easing**, which is what separates a tween that
feels mechanical from one that feels alive:

```csharp
step.TweenProperty(_sprite, "position:x", lunge, 0.10)
    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
```

`Quad` + `EaseOut` means "start fast, slow into the destination". Real things
accelerate and decelerate; linear motion reads as robotic. If you learn one thing
about tweens, learn that **easing is most of the effect**.

---

## Three real animation bugs from this project

These are all bugs that shipped and were fixed. Each teaches something general.

### 1. The blank sprite

```csharp
public void Setup(string spriteName, int scale = 3)
{
    // restart: true is REQUIRED here. Current is initialised to "idle", so a
    // plain Play("idle") would hit the "already playing that" guard below and
    // return without ever loading a texture - leaving the sprite blank until
    // something happened to it.
    Play("idle", restart: true);
}
```

`Play` has a sensible optimisation — do not restart a clip that is already
playing. But `Current` was *initialised* to `"idle"` before anything had loaded,
so the guard fired on the very first call and no texture was ever set.

> **The lesson:** an initial value that lies about the state is a bug waiting to
> happen. `Current` should arguably have started as `""`.

### 2. The container that ate the tween

Covered in [chapter 3](03-engines-and-the-scene-tree.md): a `VBoxContainer` sets
its children's positions every frame, so tweening a child's position does
nothing. The fix is a fixed-size stage the container owns, with the sprite
positioned by hand inside it.

> **The lesson:** in UI frameworks, layout systems and animation systems compete
> for the same properties. Know which one owns what.

### 3. The death that played twice

The full story is in [chapter 6](06-events-and-replay.md). The animation-specific
part is that `PlayDeath` did this:

```csharp
_sprite.Play("death", restart: true);     // start
// ...
await _sprite.PlayOnce("death");          // PlayOnce ALSO restarts it
```

`PlayOnce` restarts by design, so calling it after `Play` restarted the clip
twice back-to-back. The fix was a method that waits **without** restarting:

```csharp
public async Task WaitForCurrent()
{
    if (_sheet is null || Finished) return;
    double remaining = (_clip.Frames - _frame) / (double)_clip.Fps;
    if (remaining <= 0) return;
    await ToSignal(GetTree().CreateTimer(remaining), SceneTreeTimer.SignalName.Timeout);
}
```

> **The lesson:** "play this" and "wait for this" are different operations. If
> your only API is "play and wait", the second caller always restarts.

---

## One-shot effects

[`EffectOverlay`](../../game/scripts/EffectOverlay.cs) plays a 32×32 impact
animation over whoever was hit, then deletes itself:

```csharp
if (_frame >= Frames)
{
    QueueFree();          // done - remove ourselves, nobody tracks us
    return;
}
```

This is a genuinely nice pattern: a node that **owns its own lifetime**. Nothing
holds a reference, nothing has to remember to clean it up, and there is no
manager class. Spawn it and forget it.

Two details that matter:

```csharp
MouseFilter = MouseFilterEnum.Ignore,    // never eat a click
ZIndex = 90,                             // draw on top
```

An effect that swallows clicks makes your action menu randomly stop working, and
that bug is *miserable* to track down because it is intermittent by nature.

The file is also honest about what this is for:

> This is decoration. It changes nothing and it is worth every line: a hit
> without an impact effect reads as "a number changed", and a hit with one reads
> as something happening to somebody.

---

## What it costs you

**Sprite sheets must agree with your code.** `SpriteAnimator` hard-codes 32×40
frames and specific frame counts. Art that does not match those numbers renders
garbage, with no error. Real pipelines ship a manifest file describing each sheet
— this project's asset pack has one, and the numbers were copied out of it by
hand, which is exactly the sort of thing that drifts.

**Hand-rolled animation misses features.** No events on specific frames ("play
the sound on frame 3"), no blending, no per-frame timing. `AnimatedSprite2D` and
`AnimationPlayer` give you those.

**Tweens are fire-and-forget, and that bites.** Two tweens driving the same
property fight frame by frame. This project hit exactly that — a hit flash and a
death fade both animating `modulate` — and had to add:

```csharp
private Tween NewColourTween()
{
    _colourTween?.Kill();               // cancel whatever was running
    _colourTween = CreateTween();
    return _colourTween;
}
```

And note the second-order trap: a killed tween never emits its `finished` signal,
so anything `await`ing that signal hangs **forever**. `PlayHeal` had to be
changed to await a timer instead. *Cancelling something that another piece of
code is awaiting is a deadlock in disguise.*

---

## Try it

**1. See the atlas move.** In `SpriteAnimator.ShowFrame`, hard-code the region:

```csharp
Region = new Rect2(0, 0, FrameWidth, FrameHeight),   // always frame 0
```

Everyone freezes mid-pose. That single `Rect2` *is* the animation.

**2. Ruin your pixel art.** In `project.godot`, change
`default_texture_filter=0` to `=1`. Run the game. That blur is what every
first-time pixel-art game looks like before somebody finds this setting.

**3. Feel easing.** In `ActorView.PlayAttack`, delete the `.SetTrans(...)
.SetEase(...)`. The lunge becomes noticeably robotic. Then try
`TransitionType.Elastic` for something absurd, so you can feel the range.

**4. Make a new effect.** `EffectOverlay.Spawn(_fx, point, "fx_lightning")` —
sixteen effect strips ship with the pack and only about half are used.

---

**Next:** [Chapter 14 — Audio](14-audio.md)

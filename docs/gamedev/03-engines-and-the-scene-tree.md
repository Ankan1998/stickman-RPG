# 3. Engines and the scene tree

> **Where you are:** chapter 3 of 20 · [index](README.md) · previous: [The game loop and time](02-the-game-loop-and-time.md) · next: [Input, signals and game flow](04-input-signals-and-game-flow.md)

---

## The problem

Chapter 2 showed you the game loop, and said it was six lines long. So why is
Godot a 200MB download?

Because between "a loop that draws" and "a game" there is an enormous amount of
tedious, difficult, solved work:

- opening a window on Windows, macOS and Linux
- talking to the graphics card (Vulkan, Metal, DirectX, OpenGL, WebGL)
- decoding PNG, JPEG, OGG, WAV, TTF
- mixing audio without clicks and pops
- reading keyboards, mice, touchscreens and eight brands of gamepad
- laying out UI that survives being resized
- packaging all of it into something you can send to a stranger

An **engine** is a library that has done all of that already, plus an editor to
manage it. You could write your own. People do. It takes years and produces a
worse Godot.

---

## What an engine gives you

| Engine | Language | Best at | Cost |
|---|---|---|---|
| **Godot** | GDScript, C#, C++ | 2D, small teams, open source | Smaller ecosystem than Unity |
| **Unity** | C# | Huge ecosystem, mobile, 3D | Heavy, licence history has spooked people |
| **Unreal** | C++, Blueprints | High-end 3D, AAA | Overkill and complex for 2D |
| **MonoGame** | C# | Total control, no editor | You build everything yourself |
| **Bevy / LÖVE / Pygame** | Rust / Lua / Python | Code-first, learning | Fewer batteries included |

**This project uses Godot 4 with C#**, and the reasoning is in
[the architecture doc](../04-architecture.md). The short version: it is excellent
at 2D, it is genuinely free, its UI system is strong (which matters enormously
for a menu-driven RPG), and C# is a language you already know.

---

## The idea: a tree of nodes

Here is the core mental model of Godot, and of most engines in some form.

**Everything in your game is a node. Nodes are arranged in a tree.**

```
   GameRoot                     (a Control - the whole screen)
   |
   +-- backdrop                 (a ColorRect)
   |
   +-- screen                   (a Control - swapped per screen)
       |
       +-- BattleView
           |
           +-- background       (a TextureRect)
           +-- margin
               +-- root         (a VBoxContainer - stacks children vertically)
                   |
                   +-- top bar          (an HBoxContainer)
                   +-- field            (an HBoxContainer)
                   |   +-- heroRow      (heroes, back rank first)
                   |   +-- divider
                   |   +-- monsterRow   (monsters, front rank first)
                   +-- bottom
                       +-- log          (a RichTextLabel)
                       +-- menu         (a VBoxContainer of Buttons)
```

Each node does **one** thing. A `Label` shows text. A `TextureRect` shows an
image. An `AudioStreamPlayer` plays a sound. A `Timer` counts down. You compose
them into a tree, and the tree is your game.

### The tree is not just organisation

Three things flow down the tree automatically, and this is what makes it more
than a folder structure:

1. **Position.** Move a parent, and every child moves with it. Position is
   relative to the parent.
2. **Visibility.** Hide a parent and the whole subtree disappears.
3. **Lifetime.** Free a parent and the whole subtree is freed.

That third one is why this project can throw away an entire screen in four lines
in [`GameRoot.SwapScreen`](../../game/scripts/GameRoot.cs):

```csharp
foreach (Node child in _screen.GetChildren())
{
    _screen.RemoveChild(child);
    child.QueueFree();        // this frees the ENTIRE subtree below it
}
```

One call disposes of dozens of buttons, labels, sprites and sound players.

> **`QueueFree()` not `Free()`.** `QueueFree` deletes the node at the end of the
> current frame, once it is safe. `Free` deletes it *now*, which will crash you
> if the engine was in the middle of iterating over it. Use `QueueFree`.

---

## The node types you will actually use in 2D

Godot has hundreds. You need about eight.

| Node | What it is for |
|---|---|
| `Node` | Nothing visual. A container, or a place to hang a script. |
| `Node2D` | Anything positioned in the game world. Has x/y, rotation, scale. |
| `Sprite2D` | Draws a texture in the world. |
| `Control` | The base of all UI. Positioned by anchors and layout, not raw x/y. |
| `Label` / `RichTextLabel` | Text. `Rich` supports BBCode like `[b]bold[/b]`. |
| `TextureRect` | Draws a texture in the *UI* layer. |
| `Button` | A button. |
| `VBoxContainer` / `HBoxContainer` | Stacks children vertically / horizontally, automatically. |
| `AudioStreamPlayer` | Plays a sound. |

### The one distinction that trips everyone up: `Node2D` vs `Control`

They both draw things. They are not interchangeable.

|  | `Node2D` | `Control` |
|---|---|---|
| Think of it as | a thing in the world | a thing in the interface |
| Positioned by | x, y coordinates you set | anchors, margins and containers |
| Use for | characters, bullets, tiles, a camera | buttons, labels, panels, menus |

**This project is built almost entirely from `Control` nodes**, including the
fighters themselves. [`ActorView`](../../game/scripts/ActorView.cs) is a
`VBoxContainer`, not a `Node2D`.

That is a deliberate and slightly unusual choice, and it is correct here: the
fighters are laid out *as a row that must stay tidy when someone dies and the
ranks close up*. Letting a container handle that layout is far less work than
computing pixel positions by hand.

It also caused a real bug, which is worth knowing about because it is a classic:

> **Containers fight tweens.** A `VBoxContainer` sets its children's positions
> every frame. The first version animated the attack lunge by tweening the
> child's position — and the container immediately dragged it back. The fix, in
> `ActorView`, is a fixed-size `Control` "stage" that the container positions,
> with the sprite *inside* it positioned by hand, free to be tweened:
>
> ```csharp
> _sprite.Size = new Vector2(StageWidth, StageHeight);
> _sprite.Position = Vector2.Zero;      // manual, so tweens work
> ```
>
> If a tween on a UI node seems to do nothing, a container is overruling you.

---

## Coordinates: the Y axis points down

Here is the thing that trips up every programmer who learned graphs at school.

In maths, Y goes up. On a screen, **Y goes down.** The origin `(0, 0)` is the
**top-left** corner, X increases to the right, and Y increases *towards the
bottom of the screen*.

```
   (0,0) ---------------------------------> X
     |
     |        . (300, 100)
     |
     |                          . (600, 350)
     |
     v
     Y
```

So "move up" is `position.Y -= 5`, not `+= 5`. You will write it the wrong way
round exactly once, watch your sprite dive into the floor, and never forget it.

Look at the floating damage numbers in
[`FloatingNumber`](../../game/scripts/FloatingNumber.cs). They drift *upward*:

```csharp
tween.TweenProperty(this, "position:y", Position.Y - rise, seconds)   // minus = up
```

### `Vector2`: a position is two numbers, treated as one

Godot bundles x and y into a `Vector2`, and you can add, subtract and scale them
as single values. The pattern you will use most is **centring**: to put a thing
of size *s* over a point *p*, place its top-left corner at `p - s/2`.

```csharp
// EffectOverlay.Spawn - centre a 96x96 effect on the impact point
fx.Position = centre - new Vector2(size / 2f, size / 2f);
```

### Local vs global: every position is relative to a parent

This is the one that produces the "my effect appears in the wrong place" bug.

A node's `Position` is relative to its **parent**, not to the screen. Move the
parent and every child moves with it (that is [the tree](#the-tree-is-not-just-organisation)
doing its job). The screen-space position is the **global** one — all the parent
offsets summed up the tree.

Which means that to put a thing from one branch of the tree *over* a thing in
another branch, you have to **translate between spaces**. This project does it
in one place, and it is worth reading twice:

```csharp
// ActorView: where the sprite is, in SCREEN coordinates.
public Vector2 ImpactPoint => _stage.GetGlobalRect().GetCenter();

// BattleView: convert a screen point into the effect layer's OWN coordinates.
private Vector2 ToFx(Vector2 globalPoint) => globalPoint - _fx.GetGlobalRect().Position;
```

The effect layer `_fx` is a sibling of the battle line, not a parent of it. So an
effect positioned at the sprite's *global* point would be offset by exactly
wherever `_fx` itself sits. Subtracting `_fx`'s global origin fixes it.

> **The rule: when something appears offset by a suspiciously round amount,
> you have mixed local and global coordinates.** Every engine has this
> distinction; every beginner hits it.

### Anchors: how UI survives a resize

`Node2D` positions are pixels. `Control` positions are **anchors** — fractions of
the parent, from 0 to 1 — plus pixel offsets. Anchor a panel's right edge at 1.0
and it stays on the right edge whatever size the window becomes.

```csharp
next.SetAnchorsPreset(LayoutPreset.FullRect);     // fill the parent entirely

footer.SetAnchorsPreset(LayoutPreset.BottomWide); // stick to the bottom edge...
footer.OffsetTop = -56;                           // ...and be 56 px tall
```

That second one is the camp screen's pinned footer — the fix for the "Enter
dungeon" button that once scrolled off the bottom of the window
([chapter 17](17-ui-and-game-feel.md)).

---

## Scenes: the reusable chunk

A **scene** is a saved subtree, in a `.tscn` file. It is Godot's version of a
prefab or a component.

You build "a goblin" once — sprite, health bar, collision, sound — save it as
`Goblin.tscn`, and then create fifty of them. Edit the file, and all fifty
change. Scenes can contain other scenes, which is how a `Level` contains
`Goblin`s.

That is the normal Godot workflow: build visually in the editor, save scenes,
instantiate them from code.

### This project does almost none of that

Here is the *entire* scene file for this game:

```
[gd_scene format=3]

[ext_resource type="Script" path="res://scripts/GameRoot.cs" id="1_root"]

[node name="GameRoot" type="Control"]
anchors_preset = 15
script = ExtResource("1_root")
```

**One node, with one script on it.** Every other node in the game — every
button, every sprite, every health bar — is created in C# at runtime.

That is unusual, so it deserves a defence and an honest accounting.

**Why it was done that way here:**

- **The docs can be complete.** Every single thing on screen is explained by
  code you can read in a text file. A `.tscn` full of editor-set properties is
  invisible to someone reading the repository on GitHub — and this project is
  primarily a teaching artifact.
- **It diffs and merges.** Scene files are machine-generated and produce
  horrible merge conflicts. C# does not.
- **The UI is almost entirely dynamic anyway.** The roster grid, the loot rack,
  the skill menu and the battle line are all built from data whose *shape*
  changes at runtime. You cannot lay out "one card per hero" in an editor when
  the number of heroes is a list.

**What it costs:**

- **No visual iteration.** You cannot nudge a button four pixels and see it
  move. You change a number, rebuild, and relaunch. For a layout-heavy game this
  is genuinely slower.
- **You are re-implementing the editor.** [`UiTheme`](../../game/scripts/UiTheme.cs)
  exists because somebody had to write the styling that the editor's inspector
  would have given you for free.
- **It is not what you will see in tutorials**, which will confuse you when you
  follow one.

> **For your own first game: use the editor.** Build scenes visually. The
> code-only approach here is a defensible choice for a documented reference
> project and a bad default for someone learning the tool. Learn the normal way
> first; deviate once you know what you are giving up.

---

## Resources, and `res://`

The other half of Godot is **resources**: things that are loaded, not placed. A
texture, a sound, a font, a theme.

Paths start with `res://`, which means "the root of the project":

```csharp
Texture2D? sheet = GD.Load<Texture2D>("res://assets/chars/warrior_idle_strip.png");
```

Two things you must know:

**1. Godot re-encodes your assets on import.** Drop a PNG into the project and
Godot generates a `.import` file next to it and a converted copy in a hidden
`.godot/` folder. The game loads *that*, not your PNG. This is why the project
has 400-odd `.import` files, and why a freshly cloned copy has to be imported
once before it will run:

```bash
godot --headless --path game --import
```

**2. A missing resource must not crash you.** `GD.Load` throws if the path is
wrong. [`UiTheme.Texture`](../../game/scripts/UiTheme.cs) checks first and
returns `null` instead:

```csharp
public static Texture2D? Texture(string fileName) =>
    ResourceLoader.Exists(Assets + fileName)
        ? GD.Load<Texture2D>(Assets + fileName)
        : null;
```

Every caller handles `null` by drawing nothing, so a missing sprite is a hole on
screen rather than a crash — the "fail silently for decoration" rule from
[chapter 16](16-audio.md).

Godot's `ResourceLoader` keeps its own cache of loaded resources, so repeated
`GD.Load` calls for the same path do not re-read the disk. Where a cache *is*
hand-rolled — [`Audio.Load`](../../game/scripts/Audio.cs) — it caches **misses**
too, so a typo'd sound name costs one failed lookup rather than one per play.

---

## The alternative model: ECS

You will hear about **ECS** — Entity Component System — and should know roughly
what it is, because it is the other major way to organise a game.

| | Scene tree (Godot, Unity) | ECS (Bevy, Unity DOTS) |
|---|---|---|
| An entity is | a node with children and a script | a bare id number |
| Behaviour lives in | the node's script | systems that run over components |
| Data lives in | fields on the node | components, stored in flat arrays |
| Strength | intuitive, visual, fast to build | extremely fast at huge scale |
| Weakness | slower with 100,000 entities | more abstract; more upfront design |

ECS exists because iterating 100,000 objects scattered across the heap thrashes
your CPU cache, while iterating 100,000 entries in a flat array does not.

**You do not need ECS.** If your game has fewer than a few thousand active
entities — which is nearly every 2D game, and *certainly* a turn-based RPG with
six fighters — the scene tree is simpler, faster to write, and fast enough.
Reach for ECS when profiling tells you to, not before.

---

## In this project

| File | Node type | Job |
|---|---|---|
| [`GameRoot.cs`](../../game/scripts/GameRoot.cs) | `Control` | The whole game. Owns the Campaign, swaps screens. |
| [`BattleView.cs`](../../game/scripts/BattleView.cs) | `Control` | One encounter: the line, the log, the menu. |
| [`ActorView.cs`](../../game/scripts/ActorView.cs) | `VBoxContainer` | One fighter: sprite, name, health, statuses. |
| [`SpriteAnimator.cs`](../../game/scripts/SpriteAnimator.cs) | `TextureRect` | Plays a strip of frames. |
| [`UiTheme.cs`](../../game/scripts/UiTheme.cs) | static | Colours, fonts, textures, the theme. |
| [`Audio.cs`](../../game/scripts/Audio.cs) | static + `Node` | A pool of sound players. |
| [`EffectOverlay.cs`](../../game/scripts/EffectOverlay.cs) | static | Spawns one-shot effect animations. |
| [`FloatingNumber.cs`](../../game/scripts/FloatingNumber.cs) | static | Spawns the damage numbers. |
| [`DungeonBackdrop.cs`](../../game/scripts/DungeonBackdrop.cs) | static | Builds a backdrop image from tiles. |

Nine files. That is the entire presentation layer of the game, and it is worth
noticing how small it is — because all the *rules* are somewhere else entirely.

Which is the next chapter, and the most important one in this course.

---

## Try it

**1. Find the tree.** Run the game and press **F5** in the Godot editor, then
open the **Remote** tab of the Scene panel while it runs. You are looking at the
live node tree, rebuilt in real time as screens swap. Click any node and its
properties appear. This is the single best debugging tool Godot has.

**2. Break the container.** In [`ActorView.cs`](../../game/scripts/ActorView.cs),
find `PlayAttack` and change the tween target from `_sprite` to `_stage`:

```csharp
step.TweenProperty(_stage, "position:x", lunge, 0.10)
```

Run a fight. The lunge does nothing, because `_stage` is inside a
`VBoxContainer` that overrules it every frame. That is the bug described above,
reproduced on demand. Put it back.

---

**Next:** [Chapter 4 — Input, signals and game flow](04-input-signals-and-game-flow.md)

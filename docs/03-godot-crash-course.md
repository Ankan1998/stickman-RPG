# 3. Godot crash course

Everything you need to know about Godot to understand this project. Godot is
large; this project uses a small, deliberately boring corner of it.

---

## What a game engine actually does for you

Three things, mainly:

1. **Opens a window and draws in it**, 60 times a second.
2. **Tells you about input** — clicks, keys, touches.
3. **Gives you a way to organise your game objects** so you are not managing one
   giant array of everything.

Everything else — physics, animation, audio, particles, 3D — is optional extra.
This project uses (1), (2), (3), and nothing else at all.

---

## Nodes: the one core concept

A **node** is a single object that does one job. Godot ships with hundreds of
types, and they all inherit from `Node`.

The ones this project uses:

| Node type | Job |
|---|---|
| `Control` | Base class for all UI. Has a position and size, understands layout. |
| `ColorRect` | Fills a rectangle with a flat colour. Our background. |
| `MarginContainer` | Adds padding around its child. |
| `VBoxContainer` | Stacks its children **v**ertically. |
| `HBoxContainer` | Stacks its children **h**orizontally. |
| `PanelContainer` | Draws a background panel behind its child. |
| `Label` | Shows plain text. |
| `RichTextLabel` | Shows text with formatting and scrolling. Our combat log. |
| `Button` | Clickable. Emits a signal when pressed. |

`ActorView`, in this project, is a custom node - a `Control` subclass holding a
sprite, a health bar and a row of status icons.

### Nodes form a tree

Nodes have one parent and any number of children. The whole running game is one
tree, called the **scene tree**. Ours looks like this:

```
GameRoot                   (Control)         <- game/scripts/GameRoot.cs
├── ColorRect                                ← the dark background
└── MarginContainer                          ← 24px padding all round
    └── VBoxContainer                        ← stack everything vertically
        ├── HBoxContainer   (monster row)    <- the enemy this wave
        │   ├── ActorView
        │   ├── ActorView
        │   └── ActorView
        ├── HBoxContainer   (hero row)       <- Warrior, Medic, Archer
        │   ├── ActorView
        │   ├── ActorView
        │   └── ActorView
        ├── Label           (status line)    ← "Round 3 - Stick Medic's turn"
        └── HBoxContainer   (bottom half)
            ├── PanelContainer
            │   └── RichTextLabel            ← the combat log
            └── VBoxContainer                ← the action buttons
```

Children are positioned **relative to their parent**, and the container nodes
(`VBoxContainer` and friends) automatically lay their children out. You never
compute pixel positions for UI — you nest containers and let them do it. That is
why this project has essentially no layout maths.

You can see this exact tree while the game runs: press F5, then in the editor
switch to the **Remote** tab of the Scene dock.

---

## Scenes: reusable chunks of tree

A **scene** is a saved branch of the node tree, in a `.tscn` file. Confusingly,
"scene" does *not* mean "level" — a single button, an enemy, or your whole game
can each be a scene.

This project has exactly one, and it is nearly empty:

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/GameRoot.cs" id="1_battle"]

[node name="GameRoot" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
script = ExtResource("1_battle")
```

In English: *"one node, named `GameRoot`, of type `Control`, stretched to fill
the window, with `GameRoot.cs` attached to it."*

`.tscn` is a plain text format, which is deliberate — it means scenes diff and
merge in git like source code.

`project.godot` names this as the startup scene:

```ini
run/main_scene="res://scenes/Battle.tscn"
```

### Why is our scene so empty?

Normally you would build that whole UI tree by dragging nodes around in the Godot
editor, and the `.tscn` file would be hundreds of lines.

This project builds it **in C# instead**, in `BattleView.BuildLayout()`. That is
a deliberate teaching choice, not best practice:

- ✅ The entire UI is readable in one file, alongside the logic that uses it.
- ✅ It works without you knowing the editor yet.
- ❌ You cannot see or tweak the layout visually, which is the editor's whole point.

**Once you are comfortable in Godot, rebuilding this as a real `.tscn` is a
genuinely good exercise.** The logic barely changes — you would delete
`BuildLayout()` and replace the field assignments with `GetNode<Button>("...")`
lookups.

---

## `res://` paths

Godot paths start with `res://`, which means "the root of this project" — the
folder containing `project.godot`.

```
res://scenes/Battle.tscn   →   game/scenes/Battle.tscn
res://scripts/GameRoot.cs → game/scripts/GameRoot.cs
```

You will also occasionally see `user://`, which is a per-user writable folder for
save games. This project does not use it yet.

---

## Attaching a script to a node

A script *extends* a node. When you attach `GameRoot.cs` to a `Control` node,
that node **becomes** your class. That is why the class declaration is:

```csharp
public partial class GameRoot : Control
```

Inside that class, `this` **is** the node. You can call `AddChild(...)`,
`GetTree()`, `QueueRedraw()` directly, because they are inherited from `Control`
and `Node`.

### Why `partial`

Every Godot C# script must be `partial`. Godot's build step generates a second
file behind the scenes for each script, containing plumbing that connects your C#
to the C++ engine core. `partial` is what lets your file and the generated file
be the same class.

You will never see or edit the generated file. Just remember: **forget `partial`
and you get a confusing compile error.**

---

## The lifecycle methods

Godot calls specific methods on your node at specific times. You override the
ones you care about. This project uses two.

### `_Ready()`

Called **once**, when the node and all its children have entered the tree and are
ready to use. This is your constructor-equivalent for anything touching the
scene.

```csharp
public override void _Ready()
{
    BuildLayout();
    StartNewBattle();
}
```

> Do not use a real C# constructor for setup in Godot. When the constructor runs,
> the node is not in the tree yet, so `GetTree()` and friends will fail.

### `_Draw()`

Called when the node needs to repaint. This project no longer uses it (sprites
are `TextureRect` nodes now), but the earlier stick-figure version drew like this:

```csharp
public override void _Draw()
{
    DrawArc(new Vector2(midX, 46), 17, 0, Mathf.Tau, 24, body, 3f);   // head
    DrawLine(new Vector2(midX, 63), new Vector2(midX, 118), body, 3f); // spine
    ...
}
```

**Important:** you never call `_Draw()` yourself. You call `QueueRedraw()`, which
tells Godot "this needs repainting next frame", and Godot calls `_Draw()` when it
is ready. Calling it directly would draw at the wrong time, into nothing.

`ActorView.Refresh()` follows the same rule for its own repaints.

### Ones you will meet later but this project does not use

| Method | When |
|---|---|
| `_Process(double delta)` | Every frame. `delta` is seconds since the last one. For animation and real-time logic. |
| `_PhysicsProcess(double delta)` | Fixed timestep, for physics. |
| `_Input(InputEvent e)` | Raw input events. |
| `_ExitTree()` | Cleanup when the node is removed. |

A turn-based game needs none of these, which is a large part of why turn-based is
a good first genre.

---

## The drawing API

The earlier stick-figure version used Godot's immediate-mode 2D drawing, and the
same API still draws the health bars. Every call happens inside
`_Draw()` and is relative to the node's own top-left corner.

| Call | Draws |
|---|---|
| `DrawLine(from, to, colour, width)` | A line |
| `DrawArc(centre, radius, start, end, points, colour, width)` | An arc. Full circle = `0` to `Mathf.Tau` |
| `DrawRect(rect, colour)` | A filled rectangle — our health bars |
| `DrawString(font, pos, text, align, width, size, colour)` | Text |

Two helper types you will see constantly:

- `Vector2(x, y)` — a 2D point. Y increases **downwards**, as in most 2D graphics.
- `Rect2(x, y, width, height)` — a rectangle.
- `Mathf.Tau` — 2π. Godot prefers Tau for full circles because it reads better.

The whole health bar is two rectangles:

```csharp
DrawRect(new Rect2(12, 178, barWidth, 9), TrackColor);                    // dark track
DrawRect(new Rect2(12, 178, barWidth * fraction, 9), fill);              // coloured fill
```

That is the entire "art pipeline" of this game.

---

## Signals

Signals are Godot's built-in event system: a node announces something happened,
and anyone interested reacts. In C# they are plain events, so `+=` subscribes:

```csharp
var button = new Button { Text = action.Label };
button.Pressed += () => OnActionChosen(chosen);
```

"When this button is pressed, call `OnActionChosen` with this action."

If you have used GDScript tutorials, you will have seen
`button.pressed.connect(_on_pressed)`. The C# equivalent is `+=`.

> **The closure gotcha this code avoids.** Look closely:
>
> ```csharp
> foreach (IAction action in _battle.LegalActions(actor))
> {
>     IAction chosen = action;
>     button.Pressed += () => OnActionChosen(chosen);
> }
> ```
>
> In modern C#, `foreach` gives each iteration its own `action` variable, so
> capturing it directly is safe. The extra `chosen` local makes that guarantee
> explicit rather than something the reader has to remember. In older C# (and in
> a `for` loop, even today) every button would end up firing the *last* action.

---

## Themes and `AddThemeConstantOverride`

Godot styles UI through **themes**. You can override one value on one node:

```csharp
root.AddThemeConstantOverride("separation", 12);   // 12px gap between children
margin.AddThemeConstantOverride("margin_left", 24);
```

Those string names ("separation", "margin_left") are per-node-type and listed in
the Godot docs for each class. It is the least discoverable part of Godot; the
editor's Inspector panel is the practical way to find them.

---

## The `.godot/` folder

The first time you open the project, Godot creates `game/.godot/`. It holds
import caches, compiled shaders, and the built C# assemblies.

- It is **git-ignored** here, correctly.
- Deleting it is always safe. Godot regenerates it on next open.
- If Godot ever behaves bizarrely, deleting `.godot/` is the standard first
  troubleshooting step.

---

## How C# fits into Godot

Godot's core is C++. C# support works by hosting the .NET runtime alongside it.
Practical consequences:

1. **You need the .NET build of Godot.** The standard build has no C# at all.
2. **You need the .NET SDK installed** so Godot can compile your code.
3. **Godot builds your C# when you press play** — and if it fails to compile, it
   runs the *last successfully built* version. This is a genuinely confusing
   trap: your changes appear to do nothing. Always check the **Output** panel at
   the bottom of the editor for build errors.
4. **`Godot.NET.Sdk` version must match your Godot version.** That is the line at
   the top of [`StickmanRpg.Game.csproj`](../game/StickmanRpg.Game.csproj):

   ```xml
   <Project Sdk="Godot.NET.Sdk/4.7.2">
   ```

   Upgrade Godot, change that number.

### Naming conventions differ

Godot's own docs and 90% of tutorials use GDScript, which is snake_case. The C#
API is the same thing in PascalCase:

| GDScript | C# |
|---|---|
| `queue_redraw()` | `QueueRedraw()` |
| `get_tree()` | `GetTree()` |
| `_ready()` | `_Ready()` |
| `add_child(node)` | `AddChild(node)` |
| `draw_line(...)` | `DrawLine(...)` |

Translation is mechanical. When you find a GDScript tutorial, you can almost
always convert it on sight. The lifecycle methods keep their leading underscore
in C# (`_Ready`, `_Draw`) — a small inconsistency you get used to.

---

## Deliberately not used in this project

So you know what you are *not* missing:

- **Nodes for 2D game objects** (`Node2D`, `Sprite2D`, `AnimatedSprite2D`) - the
  fighters are UI `TextureRect` nodes, because this game is really a menu.
- **Physics** (`CharacterBody2D`, collision shapes) — turn-based games do not
  need it.
- **`Resource` / `.tres` files** — Godot's own data-asset system. It is a real
  alternative to the JSON plan in the roadmap, but it would put Godot types
  inside your rules, which this project's whole architecture forbids.
- **Autoloads / singletons** — global nodes. Easy to reach for, easy to regret.
- **The animation system** (`AnimationPlayer`, `Tween`) — worth learning when you
  want hits to feel good.

---

## Next

[How it all fits together](04-architecture.md) — the design idea that shapes
every file in this repo.

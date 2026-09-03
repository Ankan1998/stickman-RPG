# 15. UI and game feel

> **Where you are:** chapter 15 of 17 · [index](README.md) · previous: [Audio](14-audio.md) · next: [Testing and balancing](16-testing-and-balancing.md)

---

## The problem

In an action game the interface is a health bar in the corner. In a turn-based
RPG **the interface is the game**. Every decision a player makes, they make by
reading your UI and clicking a button.

So a confusing menu is not a polish issue. It is a *gameplay* issue — as
fundamental as a broken damage formula, and much more likely to make someone
stop playing.

---

# Part 1: UI

## Two ways to build UI

**Immediate mode.** Every frame, you say what should be on screen. Nothing is
stored.

```csharp
if (GUI.Button("Attack")) DoAttack();     // called EVERY frame
```

Dear ImGui works this way. Excellent for debug tools and editors: no state to
sync, no stale widgets. Poor for polished game UI — animation and transitions
are awkward when nothing persists.

**Retained mode.** You build a tree of widget objects once. They stay until you
change or destroy them.

Godot, HTML, and every OS toolkit work this way. More setup, but animation,
layout and styling all become natural.

**This project is retained**, like everything in Godot.

## Layout: stop computing pixel positions

The instinct is to place things by coordinates. Resist it — the moment your
window resizes or a name is longer than you expected, hand-placed layouts break.

Use **containers**, which position their children for you:

| Container | Does |
|---|---|
| `VBoxContainer` | Stacks children vertically |
| `HBoxContainer` | Stacks children horizontally |
| `GridContainer` | A grid of N columns |
| `CenterContainer` | Centres its child |
| `MarginContainer` | Adds padding |
| `ScrollContainer` | Scrolls if the content overflows |
| `PanelContainer` | Draws a background behind its child |

The battle line is one `HBoxContainer` containing three things — heroes, a
divider, monsters — and the heroes are themselves an `HBoxContainer`:

```csharp
var field = new HBoxContainer { Alignment = AlignmentMode.Center };

_heroRow = new HBoxContainer { Alignment = AlignmentMode.End };      // back rank leftmost
field.AddChild(_heroRow);
field.AddChild(divider);                                             // the "VS"
_monsterRow = new HBoxContainer { Alignment = AlignmentMode.Begin }; // front rank leftmost
field.AddChild(_monsterRow);
```

Nobody computes a single x-coordinate. When a fighter dies and ranks close up,
the row re-sorts itself:

```csharp
for (int i = 0; i < heroes.Count; i++)
    _heroRow.MoveChild(_views[heroes[i].Id], i);
```

### The layout bug you will hit

Two real ones from this project, both instructive:

**Content that overflows the screen.** The hub grew a roster grid *and* an
armoury, and the "Enter dungeon" button — the one button the player must press —
ended up below the bottom of the window. The fix was to reserve the bottom strip
and pin the footer:

```csharp
body.AddThemeConstantOverride("margin_bottom", 62);   // reserve space
footer.SetAnchorsPreset(LayoutPreset.BottomWide);     // pin the footer
```

> **The lesson: never let scrollable content own the critical control.** Pin the
> thing the player must click.

**Containers overruling tweens** — covered in
[chapter 3](03-engines-and-the-scene-tree.md) and worth repeating because it will
happen to you.

## Theming: define the look once

Fourteen colours define this entire game, from
[`UiTheme`](../../game/scripts/UiTheme.cs):

```csharp
public static readonly Color Ink        = new("14131c");
public static readonly Color TextBright = new("ece9f5");
public static readonly Color TextDim    = new("8d88a8");
public static readonly Color TextFaint  = new("5a5670");
public static readonly Color Gold       = new("e0c46c");
public static readonly Color HeroBlue   = new("6fb3d2");
public static readonly Color MonsterRed = new("d2795f");
public static readonly Color HealthGood = new("6dbf73");
// ...
```

**Three tiers of text** — bright, dim, faint — is a small idea that does a lot of
work. It gives you a visual hierarchy without needing different fonts or sizes:
important things are bright, context is dim, incidental detail is faint.

And colours carry **meaning** consistently: gold is "important/positive", blue is
"yours", red is "theirs". A player learns that in about thirty seconds and then
reads the screen faster forever.

Health bars use a function rather than a fixed colour:

```csharp
public static Color HealthColour(float fraction) => /* green -> amber -> red */
```

So "this hero is in trouble" is readable **peripherally**, without reading a
number. That is a real design principle: put the most urgent information in the
channel that needs the least attention.

---

## The three UI decisions that matter most here

### 1. Ask twice, not once

Three heroes × three skills × three targets is up to ten legal actions. Ten
buttons is a wall nobody reads.

So the menu asks twice: **pick a skill, then pick a target.**

```
   STEP 1                         STEP 2
   Healing Word  (heal 26)        [1] Warrior   (+26 hp)
   Mace  (95%)  ##--              [2] Cleric    (+18 hp)
   Bless  (blessed)               [3] Mage      (+26 hp)
   Step forward (swap Warrior)    < Back
   Wait
```

Crucially, this only **groups** `LegalActions`. It never adds to it:

```csharp
foreach (var group in legal.OfType<SkillAction>().GroupBy(a => a.Skill.Id))
```

The UI is a *view* of the legal moves, not a second source of them. This is the
same discipline as [chapter 4](04-rules-vs-presentation.md): presentation may
reorganise, never reimplement.

### 2. Show the consequence

Every target button shows what will happen:

```
   Goblin A  (13 dmg)
   Warrior   (+26 hp)
```

Computed by asking the *rules'* calculator, not by reimplementing the formula:

```csharp
string preview = skill.DealsDamage
    ? $"{DamageCalculator.Compute(actor.CurrentStats, target.CurrentStats, skill.Power, false)} dmg"
    : ...
```

> **Guessing is not strategy.** A tactical game where the player cannot predict
> the outcome of their own move is a slot machine. Show the numbers.

### 3. Explain every refusal

This is the best UI idea in the project. A skill you cannot use is **not hidden**
— it is greyed out **with the reason**:

```
   Slash        (needs rank 1-2)
   Aimed Shot   (nothing in rank 3-4)
   Heavy Blow   (2 turns)
```

The argument, from the source:

> A skill that just vanishes from the menu teaches the player nothing; one that
> says "needs rank 1-2" teaches them the whole positioning system.

That is a genuinely deep point. **Your UI is your tutorial.** Every refusal is a
chance to teach a rule at the exact moment the player is curious about it. A
disabled button with a reason does more teaching than a tutorial popup nobody
reads.

#### The bug that proved the point

There was a hole in this. A **stunned** hero got a menu with a single "Wait"
button and *no explanation at all*.

The cause: `LegalActions` drops every skill and every move at once when
`CanAct` is false. So the greyed-out loop, which only ran for skills that were
otherwise unavailable, skipped all of them — they were "available" in every sense
it checked.

The player saw an empty menu with no reason. In the one situation they most
wanted an explanation, the menu that explains everything explained nothing:

```csharp
// Stunned, frozen, webbed: LegalActions drops every skill AND every step at
// once, which left the player staring at a lone "Wait" button with nothing
// anywhere saying why. A menu that explains itself has to explain this case
// too - it is the one the player most wants explained.
bool canAct = actor.CanAct;
if (!canAct)
    _menu.AddChild(UiTheme.MakeLabel(
        $"{actor.BlockedReason ?? "Unable to act"} - loses this turn",
        12, UiTheme.HealthLow, HorizontalAlignment.Center));
```

> **The lesson: check that your "explain everything" system handles the case
> where *everything* is unavailable.** That is usually the case it was not written
> for.

---

# Part 2: Game feel

## The idea

**Game feel** (or *juice*) is the feedback that makes an action satisfying. It
changes nothing about the rules and it is not optional.

The test: take any two games with identical mechanics. The one that feels better
is the one people play.

### The toolkit

| Technique | What it does |
|---|---|
| **Screen shake** | Impact and weight |
| **Hit pause** | Freeze 2–3 frames on a big hit. Enormously effective. |
| **Flash** | Tint the sprite white on hit |
| **Squash and stretch** | Deform on impact |
| **Particles** | Sparks, blood, dust |
| **Floating numbers** | Makes damage legible and connected to the click |
| **Anticipation** | Wind up before the swing |
| **Follow-through** | Overshoot and settle |
| **Sound layering** | Impact + voice + effect on one hit |
| **Pacing** | Deliberate pauses between beats |

## What this project uses

**The lunge** — anticipation and follow-through:

```csharp
float lunge = _actor.Team == Team.Heroes ? 14f : -14f;
step.TweenProperty(_sprite, "position:x", lunge, 0.10)
    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
step.TweenProperty(_sprite, "position:x", 0f, 0.22);
```

Note the asymmetry: **out fast (0.10s), back slow (0.22s)**. Real strikes commit
quickly and recover slowly. Equal timings feel like a metronome.

**The flash, scaled to significance:**

```csharp
flash.TweenProperty(_sprite, "modulate",
    critical ? new Color(2.4f, 1.5f, 0.6f)     // gold, and brighter
             : new Color(2.0f, 0.7f, 0.7f),    // red
    0.04);
```

Values above 1.0 overbrighten. A crit flashes *gold and harder* — the same event
type, louder, so the player feels the difference before reading the number.

**The shake, also scaled:**

```csharp
float power = critical ? 10f : 5f;
```

**Floating numbers** — [`FloatingNumber`](../../game/scripts/FloatingNumber.cs)
is forty lines and, per its own header, *"does more for feel than any amount of
rules work"*. Three details worth stealing:

```csharp
// A dark outline so the number stays readable over any sprite.
label.AddThemeColorOverride("font_outline_color", UiTheme.Ink);
label.AddThemeConstantOverride("outline_size", 5);
```

An outline is what makes text legible over *arbitrary* backgrounds. Without it
your damage numbers vanish against half your sprites.

```csharp
// Drift upward, easing out so it decelerates like it is losing momentum.
.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out)
```

```csharp
MouseFilter = MouseFilterEnum.Ignore,     // must never eat a button press
```

**Pacing** — one constant controls the entire rhythm of combat:

```csharp
private const double BeatSeconds = 0.30;

if (NeedsABeat(e))
    await ToSignal(GetTree().CreateTimer(BeatSeconds), ...);
```

And crucially, **not every event gets a beat**:

```csharp
private static bool NeedsABeat(GameEvent e) =>
    e is Damaged or Healed or StatusApplied or TurnSkipped or Died or Repositioned;
```

`StatusTicked` fires every turn for every status and would drown the pacing in
meaningless pauses. **Pause on things that matter; let bookkeeping fly past.**
Uniform pacing is not good pacing.

## The most valuable one you have not tried

**Hit pause** (or *hitstop*): when a big hit lands, freeze *everything* for 2–3
frames.

It sounds wrong — you are removing motion to convey impact — and it is
astonishingly effective. Almost every action game you have enjoyed uses it. It is
the single highest value-per-line juice technique there is, and this project does
not have it. It would be about five lines in `ShowDamage`.

---

## What it costs you

**Juice can obscure the game.** Shake hard enough and the player cannot read the
screen. Accessibility matters here: some players get motion sick, and shake and
flash should be options.

**Pacing is a tax on every turn.** 0.30 seconds per event is *right* for a first
playthrough and *interminable* on your fortieth. Any turn-based game shipping for
real needs a speed toggle and a skip-animation key. This one does not have them,
and it would be the first thing to add.

**UI is where the code volume goes.** The positioning *rules* were two `if`s.
Making positioning *legible* — rank badges, reach diagrams, greyed-out reasons,
preferred-rank hints, re-sorting the row on death — was several times more code.
That ratio is normal, and it surprises people.

---

## Try it

**1. Delete the juice.** In `BattleView.ShowDamage`, comment out `Popup(...)`,
`EffectOverlay.Spawn(...)` and `await view.PlayHit(...)`. Play a fight. The rules
are *identical*. The game is dramatically worse. That gap is game feel.

**2. Add hit pause.** In `ShowDamage`, before the flinch:

```csharp
if (d.IsCritical)
{
    GetTree().Paused = true;
    await ToSignal(GetTree().CreateTimer(0.06, true, false, true),
                   SceneTreeTimer.SignalName.Timeout);
    GetTree().Paused = false;
}
```

**3. Break the pacing filter.** Make `NeedsABeat` return `true` for everything.
Combat becomes glacial, because every invisible `StatusTicked` now costs a third
of a second.

**4. Hide a refusal instead of explaining it.** Delete the greyed-out skill loop
in `ShowSkillMenu`. Put the Mage in rank 1 and look at her menu. You now have no
idea why her spells are gone — which is what every game that hides unavailable
options feels like to a new player.

---

**Next:** [Chapter 16 — Testing and balancing](16-testing-and-balancing.md)

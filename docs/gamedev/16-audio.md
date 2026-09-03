# 16. Audio

> **Where you are:** chapter 16 of 20 · [index](README.md) · previous: [Sprites and animation](15-sprites-and-animation.md) · next: [UI and game feel](17-ui-and-game-feel.md)

---

## The problem

Audio is the most neglected part of hobby game development, and it is the one
that most reliably separates games that feel professional from games that feel
cheap.

Here is the uncomfortable fact: **players cannot tell you what is wrong with your
audio, but they can feel it.** Mute a good game and it becomes noticeably worse.
Play a game with bad audio and you will describe it as "janky" or "unpolished"
without ever consciously noticing the sound.

So it earns real attention, and the good news is that most of the benefit comes
from about four techniques.

---

## Technique 1: never play the same sample twice

This is the big one.

Play the identical mace sample three times in a row and the human ear instantly
recognises it as **a machine gun**, not three separate impacts. Our hearing is
extraordinarily good at detecting exact repetition — it is what tells us a sound
is artificial.

Two cheap fixes, used together:

**Multiple takes.** The audio pack for this project ships three variants of every
sound — `slash_light_1`, `_2`, `_3` — which differ slightly in pitch, length and
level, the way real recorded takes do.

```csharp
int take = _rng.RandiRange(1, TakesPerSound);
```

**Pitch jitter.** On top of that, vary the playback pitch slightly on every play:

```csharp
// A touch of pitch variation on top of the three takes. Cheap, and it
// stops repeated hits sounding mechanical.
player.PitchScale = 1f + _rng.RandfRange(-pitchJitter, pitchJitter);
```

The default jitter here is `0.06` — ±6%. Small enough that nobody notices the
variation, large enough that the ear stops hearing a loop.

Three takes × continuous pitch variation means a player will effectively never
hear the same sound twice. That is most of the perceived quality difference,
for about eight lines of code.

> **This is the highest-value audio technique there is.** If you do nothing else
> from this chapter, do this.

---

## Technique 2: a pool of players

One `AudioStreamPlayer` plays one thing at a time. Assign it a new stream and the
current sound **stops dead**.

So a hit landing while a spell is still ringing would cut the spell off
mid-note — which sounds obviously broken.

The fix is a **pool**, rotated round-robin:

```csharp
private const int Voices = 8;          // how many sounds can overlap

public override void _Ready()
{
    for (int i = 0; i < Voices; i++)
    {
        var player = new AudioStreamPlayer { Bus = "Master" };
        AddChild(player);
        _players.Add(player);
    }
}

private void PlayInternal(...)
{
    AudioStreamPlayer player = _players[_next];
    _next = (_next + 1) % _players.Count;
    // ...
    player.Play();
}
```

Eight voices means eight sounds can overlap. The ninth simultaneous sound steals
the oldest player — which is the correct behaviour, since the oldest is the one
most likely to have finished.

**Object pooling** is a pattern you will meet again for bullets, particles and
damage numbers: pre-create a fixed set of expensive things and reuse them,
instead of allocating and freeing constantly.

---

## Technique 3: map sounds to *categories*, not to events

The naive approach hard-codes a sound at each call site:

```csharp
Audio.Play("sword_hit");     // ...and every weapon sounds like a sword
```

Better: derive the sound from **what kind of thing it is**. From
[`Sfx`](../../game/scripts/Audio.cs):

```csharp
// weapon archetype -> impact sound     (a hammer and a dagger differ)
public static string HitFor(WeaponDefinition? weapon) => weapon?.HitSound ?? "hit_flesh";

// voice family -> hurt/death cry       (a skeleton and a slime differ)
public static string HurtFor(string voiceFamily)  => $"{voiceFamily}_hurt";
public static string DeathFor(string voiceFamily) => $"{voiceFamily}_death";

// status -> magic sound
public static string? ForStatus(string statusId) => statusId switch
{
    "poison"  => "poison",
    "burning" => "fire",
    "chilled" => "ice",
    "stun"    => "stun",
    // ...
    _ => null,
};
```

Three mapping functions cover every sound in the game. Notice:

- **`HitSound` lives on the weapon definition**, so adding a weapon automatically
  gives it the right impact sound. Content-as-data
  ([chapter 12](12-content-as-data.md)) paying off again.
- **`VoiceFamily` lives on the actor** — `human`, `goblin`, `undead`, `beast`,
  `demon`, `golem`, `slime`, `skeleton`. A skeleton dies with a clatter and a
  slime with a splat, and nobody wrote a `switch` at the call site.
- **`ForStatus` returns `string?`.** Some statuses make no noise, and `null` is
  the honest answer.

---

## Technique 4: mix by ear, in decibels

Every `Play` call takes a volume:

```csharp
Audio.Play("critical_hit", -2f);            // loud - it should land
Audio.Play(Sfx.HurtFor(voice), -6f);        // quieter, it is a reaction
Audio.Play("ui_select", -10f);              // background, do not intrude
Audio.Play("turn_start", -14f);             // nearly subliminal
```

Decibels are **logarithmic**: −6dB is roughly half as loud, −12dB roughly a
quarter. Negative numbers are the norm; 0 is "as recorded".

The mix is a **hierarchy of importance**. A critical hit should punch through; a
UI click should never compete with it. Getting this ordering right matters more
than the exact numbers.

> **The practical method:** play the game with your eyes closed. Anything you
> notice that is not important gets turned down.

---

## The bugs audio produces

Audio has its own failure modes, and this project shipped two of them.

### The doubled cue

When the party was wiped out, the "defeat" sting played **twice, overlapping
itself**:

```csharp
// In BattleView.ContinueBattle - when the fight ends
Audio.Play(_campaign.Battle.Winner == Team.Heroes ? "victory" : "defeat", -3f);

// ...and 0.9 seconds later, in GameRoot.ShowResults
Audio.Play(won ? "victory" : "defeat", -2f);
```

Two pieces of code independently decided they owned the same moment. The fix was
to decide **who owns it** and say so:

> No sting here. `BattleView` already played victory or defeat the moment the
> last fight ended, and this screen arrives less than a second later — so
> sounding it again just played the same cue twice over itself. The sting belongs
> to the fight, which is where the moment is.

### The doubled cry

More subtle. On a killing blow, the fighter played their **hurt** cry and then,
immediately, their **death** cry. Which sounds exactly like what it is: somebody
dying twice.

```csharp
// A killing blow gets no hurt cry and no flinch. The death cry follows
// immediately after, and playing both made a fighter sound like they died
// twice; flinching first made the death animation start, abort and start again.
if (fatal) return;

Audio.Play(Sfx.HurtFor(target.VoiceFamily), -6f);
await view.PlayHit(d.IsCritical);
```

> **The general lesson: sound reveals sequencing bugs that graphics hide.** Two
> overlapping animations can look like one slightly odd animation. Two
> overlapping sounds are unmistakable. If something sounds wrong, your event
> ordering probably *is* wrong.

### Playing the wrong weapon's sound

Also worth revisiting here, because it was found *by* thinking about audio.
`ShowDamage` used to determine the attacker with:

```csharp
Actor? attacker = _campaign.Battle.Current;      // whose turn is it?
```

Which is always the **next** fighter, because the turn already advanced
([chapter 7](07-events-and-replay.md)). So a goblin's club could land with a
bowstring. The fix was to put the attacker in the event.

---

## Fail silently

```csharp
/// Silently does nothing if the file is missing - a game that crashes
/// because an optional sound effect is absent is a badly built game.
public static void Play(string soundName, float volumeDb = 0f, float pitchJitter = 0.06f)
```

This is a deliberate philosophy and worth thinking about. Audio is
**decoration**. A missing sound file should never take down a game.

Contrast that with `BattleState`'s constructor from
[chapter 6](06-state-and-entities.md), which explodes loudly on a duplicate actor
id. Both are correct:

> **Crash loudly when the *rules* are wrong. Fail silently when the *decoration*
> is missing.**

A missing sound is a cosmetic bug you fix on Tuesday. A corrupted battle state is
a bug that eats a weekend.

The same instinct appears in the loading cache, which caches **failures** too:

```csharp
/// Finds a sound across the audio folders and caches the result, hit or miss.
```

A typo'd sound name costs one failed disk lookup, not one per frame forever.

---

## What this project does not do

Being honest about scope, because you will need these eventually:

**No music.** No looping tracks, no crossfades between the hub and a dungeon.
Music is the single largest remaining audio gap, and it is what most changes how
a game *feels*.

**No audio buses.** Godot supports named buses — Master, SFX, Music, UI — with
independent volume and effects. This project puts everything on Master, which
means no "SFX volume" slider is possible without refactoring. **For a real game,
set up buses on day one**; retrofitting them is annoying.

**No spatial audio.** Fine for a menu-driven game; essential the moment things
have positions that matter.

**No ducking.** Professional mixes automatically lower music when a big sound
effect plays. Noticeably good, and not hard.

---

## Try it

**1. Hear why jitter matters.** In `PlayInternal`, force it off:

```csharp
player.PitchScale = 1f;
```

...and force a single take with `int take = 1;`. Fight a battle. Every hit is
now identical, and it sounds like a machine gun. This is the clearest
demonstration in the whole project of a technique whose absence you notice
without being able to name.

**2. Break the pool.** Set `Voices = 1`. Now every sound cuts off the previous
one. Attack a goblin and hear the impact murder the swing.

**3. Fix the mix.** Set every `volumeDb` to `0f`. The result is a wall of noise
where nothing is more important than anything else — which is exactly what an
unmixed game sounds like.

---

**Next:** [Chapter 17 — UI and game feel](17-ui-and-game-feel.md)

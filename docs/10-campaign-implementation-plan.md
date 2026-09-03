# 10. Campaign implementation plan

Turning the three-wave gauntlet into a full campaign: three dungeons, a hub
between them, animated sprites, sound, and loot.

This is the plan I worked to. It is kept in the repo because the *reasoning*
behind a build is usually more useful later than the build itself.

---

## What the asset packs give us

`stickman-rpg-assets/` (1,457 PNGs) and `stickman-rpg-audio/` (62 sounds × 3
takes, WAV and OGG). Both are generated from Python, same as the art already in
`tools/`, and both ship a `manifest.json` describing every file.

| | Count | Format |
|---|---|---|
| Hero classes | 10 | 32×40, 5 animations each |
| Enemies | 29, tiered 1–3 | 32×40, 5 animations each |
| Weapons | 47, 5 rarities | 24×24 icons |
| Dungeon tiles | 43 | 16×16 |
| Effects | 16 | 32×32, 6 frames |
| Sounds | 62 | mono OGG/WAV, 3 variants each |

Animations are **horizontal strips**: `warrior_attack_strip.png` is 192×40, six
32×40 frames. Frame counts and fps come from the manifest — idle 4f/6fps, walk
6f/10fps, attack 6f/12fps, hurt 3f/14fps, death 6f/8fps.

Critically, the pack already names its stills `warrior.png` / `warrior_down.png`
— exactly what `ActorView` loads today. So the sprites drop in with no code
change, and animation is an upgrade on top rather than a rewrite.

---

## The shape of the game

```
                    ┌──────────────────────────┐
                    │   HUB - Camp             │
                    │   pick 3 of 10 heroes    │
                    │   equip the loot         │
                    │   rest (full heal)       │
                    └───────────┬──────────────┘
       ┌────────────────────────┼────────────────────────┐
       ▼                        ▼                        ▼
┌──────────────┐        ┌──────────────┐         ┌──────────────┐
│ THE WARRENS  │        │ EMBER HALLS  │         │ FROZEN CRYPT │
│ 3 encounters │  ───►  │ 3 encounters │  ───►   │ 3 encounters │
│ tier 1 foes  │  hub   │ tier 2 foes  │   hub   │ tier 3 + boss│
│ POISON       │        │ BURNING      │         │ CHILL + CURSE│
└──────────────┘        └──────────────┘         └──────────────┘
       │ loot after each encounter                        │
       └──────────────────────────────────────────────────┘
                                                          ▼
                                                      VICTORY
```

**Wounds carry between encounters inside a dungeon.** The hub restores you
fully. That makes a dungeon the real unit of tension — three fights on one health
bar — and makes reaching the hub feel like an achievement rather than a menu.

### Why each dungeon has a signature status

A harder dungeon that is only "the same fight with bigger numbers" is boring. So
each one attacks you differently and demands a different answer:

| Dungeon | Signature | What it does | The counter-play |
|---|---|---|---|
| **The Warrens** | `poison` | 4/turn for 3 turns, stacks up across a pack | Kill the appliers fast; poison is slow, so pressure beats patience |
| **Ember Halls** | `burning` | 8/turn but only 2 turns | Huge burst - you must heal *through* it or kill before the second tick |
| **Frozen Crypt** | `chilled` + `cursed` | −6 Speed, −4 Attack | You lose the turn order and your damage. Buffs and dispels matter |

Plus `bleed` (Warrens), `weakened` (Ember), and `stun` everywhere.

---

## Rpg.Core changes

Everything below is engine-free and therefore testable.

### New files

| File | Purpose |
|---|---|
| `Content/WeaponDefinition.cs` | A weapon: stat bonus, rarity, archetype (drives the hit sound) |
| `Content/HeroDefinition.cs` | A recruitable hero: stats, skills, sprite, voice family |
| `Content/MonsterTemplate.cs` | Moved out of ContentDatabase; now also carries tier, voice, weapon |
| `Progression/DungeonDefinition.cs` | Name, theme, encounters, loot table |
| `Progression/Campaign.cs` | Hub ↔ dungeon state machine; replaces `Run` as the top level |
| `Progression/LootRoll.cs` | What dropped, and the seeded roll that produced it |
| `Ai/ThreatModel.cs` | How dangerous each hero looks to the enemy |

### Changed files

- **`Actor`** — gains an equipped `WeaponDefinition?` folded into `CurrentStats`,
  plus `VoiceFamily` and `SpriteName` so the presentation layer stops guessing.
- **`ScoringAi`** — see below.
- **`ContentDatabase`** — grows the full rosters; `Run` stays for the existing
  tests and becomes "one dungeon".

### Smarter enemies

The current AI scores each action in isolation. Three additions make it play like
it wants you dead:

1. **Finish the wounded.** `score += (1 − hpFraction) × FocusWeight`. A pack that
   concentrates on one hero removes a turn from every future round; three enemies
   each hitting a different hero removes none.
2. **Threat assessment.** Healers and high-damage heroes are worth killing first.
   `ThreatModel` rates each hero from their skills and stats, and the AI adds
   that to any action targeting them.
3. **Do not waste overkill.** Already present, but now also *prefers* the target
   it can actually finish this round over a bigger but survivable hit.

This is still one-ply scoring — no search. It just scores the right things.

---

## Godot changes

| File | Purpose |
|---|---|
| `AnimatedSprite.cs` | Plays a strip as frames: `AtlasTexture` per frame over one `Texture2D` |
| `Audio.cs` | Sound bank; picks one of three takes at random per play |
| `EffectOverlay.cs` | Plays an `fx_*` strip over a target, then frees itself |
| `HubScreen.cs` | Roster, party picker, equipment |
| `DungeonScreen.cs` | Encounter progress, the run-so-far, "descend" |
| `LootScreen.cs` | What dropped, equip or discard |
| `ActorView.cs` | Rewritten around `AnimatedSprite` |
| `BattleView.cs` | Triggers animation, sound and FX off the event stream |

### Where sound and animation hook in

They hook into the **event replay**, which already exists and already happens one
event at a time with pauses. Nothing about combat changes:

```
SkillUsed      -> attacker plays `attack`; on frame 2, weapon's swing sound
Damaged        -> target plays `hurt` + hit sound by weapon archetype
                  + fx_slash / fx_impact / fx_pierce overlay
                  + `critical_hit` sound instead, if it crit
Healed         -> fx_heal + `heal`
StatusApplied  -> fx_poison / fx_fire / fx_ice / fx_debuff + matching sound
Died           -> target plays `death` + voice-family death cry
BattleEnded    -> `victory` or `defeat`
```

The audio pack's README maps sounds to sprite frames, and its voice families
(`human`, `goblin`, `undead`, `beast`, `demon`, `golem`, `slime`) map onto the
enemy roster.

---

## Loot

Weapons drop after every encounter, rolled from a seeded table weighted by
dungeon depth:

| Dungeon | common | uncommon | rare | epic | legendary |
|---|---|---|---|---|---|
| Warrens | 55% | 30% | 13% | 2% | – |
| Ember Halls | 25% | 38% | 27% | 9% | 1% |
| Frozen Crypt | 8% | 25% | 37% | 23% | 7% |

A weapon is a `StatBlock` bonus and an archetype. The archetype does real work:
it decides which sound plays when you hit, so a hammer and a dagger *sound*
different without any special-casing in combat.

Equipping happens at the hub, or immediately from the drop screen.

---

## Balance

Same method as before: measure, do not guess.

`CampaignHarnessTests` plays hundreds of complete campaigns with the AI on both
sides and reports the clear rate per dungeon. Targets:

| | AI clear rate |
|---|---|
| Warrens | ~85% |
| Ember Halls | ~65% |
| Frozen Crypt | ~40% |
| Full campaign | 20–35% |

The AI plays worse than a person, so a ~25% AI clear rate is a campaign a careful
human finishes maybe half the time. Those bands become assertions, so a future
content change that quietly breaks the curve fails the build.

---

## Order of work

1. **Assets in** — copy the sprites, FX and OGG audio into `game/`, wire nearest
   filtering, confirm the existing game still runs.
2. **Core: statuses, weapons, heroes, monsters** — content only, no new systems.
3. **Core: dungeons and campaign** — the state machine, plus loot.
4. **Core: smarter AI** — threat and focus fire.
5. **Tests and balance** — harness, then tune until the curve is right.
6. **Godot: animation, audio, FX** — the battle screen comes alive.
7. **Godot: hub, dungeon and loot screens** — the campaign wrapper.
8. **Verify** — build, tests, headless run, screenshots of every screen.

Each step ends green: `dotnet build`, `dotnet test`, and a headless Godot run.

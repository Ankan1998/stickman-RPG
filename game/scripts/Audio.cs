// ============================================================================
//  AUDIO - the sound bank
// ============================================================================
//
//  IN PLAIN ENGLISH
//  ----------------
//  Play("slash_light") and a sword noise happens. That is the whole interface.
//
//  THREE TAKES OF EVERYTHING
//  -------------------------
//  Every sound in the pack ships as three variants - slash_light_1/2/3 - which
//  differ slightly in pitch, length and level, the way real takes do. We pick
//  one at random on each play.
//
//  This matters far more than it sounds. A mace landing three times in a row
//  with the identical sample is instantly recognisable as a machine gun, and it
//  makes a game feel cheap in a way players notice but cannot name. Three takes
//  plus a little pitch jitter is the difference.
//
//  A POOL OF PLAYERS
//  -----------------
//  One AudioStreamPlayer can only play one thing at a time, so a hit landing
//  while a spell is still ringing would cut the spell off. We keep a small pool
//  and use whichever is free.
//
//  WHICH SOUND FOR WHICH EVENT
//  ---------------------------
//  The audio pack's README maps this out, and Sfx below encodes it:
//    weapon archetype -> impact sound     (a hammer and a dagger differ)
//    voice family     -> hurt/death cry   (a skeleton and a slime differ)
//    status/effect    -> magic sound      (fire, ice, poison...)
// ============================================================================

using System.Collections.Generic;
using Godot;

namespace StickmanRpg.Game;

public partial class Audio : Node
{
    private const int Voices = 8;          // how many sounds can overlap
    private const int TakesPerSound = 3;

    private static Audio? _instance;

    private readonly List<AudioStreamPlayer> _players = new();
    private readonly Dictionary<string, AudioStream?> _cache = new();
    private readonly RandomNumberGenerator _rng = new();
    private int _next;

    /// <summary>Attaches the sound bank to the scene tree. Call once, from the root.</summary>
    public static Audio Attach(Node parent)
    {
        if (_instance is not null && IsInstanceValid(_instance)) return _instance;

        var audio = new Audio { Name = "Audio" };
        parent.AddChild(audio);
        _instance = audio;
        return audio;
    }

    public static Audio? Instance => _instance is not null && IsInstanceValid(_instance) ? _instance : null;

    public override void _Ready()
    {
        _rng.Randomize();
        for (int i = 0; i < Voices; i++)
        {
            var player = new AudioStreamPlayer { Bus = "Master" };
            AddChild(player);
            _players.Add(player);
        }
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Plays one of the three takes of a sound.
    ///
    /// Silently does nothing if the file is missing - a game that crashes
    /// because an optional sound effect is absent is a badly built game.
    /// </summary>
    public static void Play(string soundName, float volumeDb = 0f, float pitchJitter = 0.06f)
    {
        Instance?.PlayInternal(soundName, volumeDb, pitchJitter);
    }

    private void PlayInternal(string soundName, float volumeDb, float pitchJitter)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        int take = _rng.RandiRange(1, TakesPerSound);
        AudioStream? stream = Load(soundName, take);
        if (stream is null) return;

        AudioStreamPlayer player = _players[_next];
        _next = (_next + 1) % _players.Count;

        player.Stream = stream;
        player.VolumeDb = volumeDb;

        // A touch of pitch variation on top of the three takes. Cheap, and it
        // stops repeated hits sounding mechanical.
        player.PitchScale = 1f + _rng.RandfRange(-pitchJitter, pitchJitter);
        player.Play();
    }

    /// <summary>Finds a sound across the audio folders and caches the result, hit or miss.</summary>
    private AudioStream? Load(string soundName, int take)
    {
        string key = $"{soundName}_{take}";
        if (_cache.TryGetValue(key, out AudioStream? cached)) return cached;

        AudioStream? found = null;
        foreach (string folder in new[] { "combat", "voices", "magic", "ui" })
        {
            string path = $"res://audio/{folder}/{key}.ogg";
            if (ResourceLoader.Exists(path))
            {
                found = GD.Load<AudioStream>(path);
                break;
            }
        }

        _cache[key] = found;      // cache misses too, so we only look once
        return found;
    }
}

// ============================================================================
//  SFX - which sound belongs to which game event
// ============================================================================
//
//  Kept separate from the player above so that "what noise does a hammer make?"
//  is a data question with one obvious place to look, rather than a series of
//  if-statements scattered through the battle code.
// ============================================================================
public static class Sfx
{
    /// <summary>The impact sound for a weapon archetype. Falls back to a fist.</summary>
    public static string HitFor(Rpg.Core.Content.WeaponDefinition? weapon) =>
        weapon?.HitSound ?? "hit_flesh";

    /// <summary>The "ow" for a voice family: human, goblin, undead, beast, demon, golem, slime, skeleton.</summary>
    public static string HurtFor(string voiceFamily) => $"{voiceFamily}_hurt";

    /// <summary>The death cry for a voice family.</summary>
    public static string DeathFor(string voiceFamily) => $"{voiceFamily}_death";

    /// <summary>
    /// The magic sound matching a status effect, or null for statuses that make
    /// no noise of their own.
    /// </summary>
    public static string? ForStatus(string statusId) => statusId switch
    {
        "poison" => "poison",
        "burning" => "fire",
        "chilled" => "ice",
        "bleed" => "hit_flesh",
        "stun" => "stun",
        "guard" or "blessed" or "rallied" or "focused" or "enraged" => "buff",
        "weakened" or "cursed" or "sundered" or "webbed" => "debuff",
        _ => null,
    };

    /// <summary>
    /// The 32x32 effect animation matching a status - one of the 16 fx_* strips.
    /// </summary>
    public static string? FxForStatus(string statusId) => statusId switch
    {
        "poison" => "fx_poison",
        "burning" => "fx_fire",
        "chilled" => "fx_ice",
        "bleed" => "fx_blood",
        "stun" => "fx_stun",
        "guard" or "blessed" or "rallied" or "focused" or "enraged" => "fx_buff",
        "weakened" or "cursed" or "sundered" or "webbed" => "fx_debuff",
        _ => null,
    };

    /// <summary>The effect animation for a weapon connecting.</summary>
    public static string FxForWeapon(Rpg.Core.Content.WeaponDefinition? weapon) =>
        weapon?.Kind switch
        {
            Rpg.Core.Content.WeaponKind.Spear or Rpg.Core.Content.WeaponKind.Trident
                or Rpg.Core.Content.WeaponKind.Bow or Rpg.Core.Content.WeaponKind.Crossbow => "fx_pierce",
            Rpg.Core.Content.WeaponKind.Club or Rpg.Core.Content.WeaponKind.Mace
                or Rpg.Core.Content.WeaponKind.Hammer or Rpg.Core.Content.WeaponKind.Flail => "fx_impact",
            Rpg.Core.Content.WeaponKind.Staff or Rpg.Core.Content.WeaponKind.Wand
                or Rpg.Core.Content.WeaponKind.Orb or Rpg.Core.Content.WeaponKind.Tome => "fx_arcane",
            null => "fx_impact",
            _ => "fx_slash",
        };
}

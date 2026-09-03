"""Menus, stings and dungeon interactions. The retro fingerprint is
strongest here: square and triangle waves, tight envelopes, no reverb on
the menu blips so they stay snappy under everything else."""

import numpy as np
import synth as S
from sfx_voices import v
from sfx_combat import thump, click, clang, whoosh


def blip(f, dur=0.05, wave=S.square, width=0.5, k=5):
    w = wave(f, dur, width) if wave is S.square else wave(f, dur)
    return w * S.env_decay(dur, k) * 0.6


def ui_move(rng):
    return blip(v(rng, 880, 0.02), 0.045, S.tri, k=6)


def ui_select(rng):
    return S.seq(blip(659.3, 0.05, S.square, 0.25), blip(987.8, 0.09, S.square, 0.25))


def ui_back(rng):
    return S.seq(blip(659.3, 0.05, S.square, 0.25), blip(440, 0.09, S.square, 0.25))


def ui_error(rng):
    return S.mix(S.square(110, 0.18, 0.5), S.square(116.5, 0.18, 0.5)) * S.env_adsr(0.18, 0.005, 0.02, 0.8, 0.05) * 0.5


def turn_start(rng):
    return S.mix(S.sine(1046.5, 0.3) * S.env_decay(0.3, 5), S.sine(2093, 0.3) * S.env_decay(0.3, 8) * 0.3,
                 S.sine(1568, 0.25) * S.env_decay(0.25, 6) * 0.4) * 0.7


def _melody(notes, step, dur, wave=S.square, width=0.25, k=4, gain=0.5):
    out = np.zeros(S.n(step * len(notes) + dur), dtype=np.float32)
    for i, f in enumerate(notes):
        tone = (wave(f, dur, width) if wave is S.square else wave(f, dur)) * S.env_decay(dur, k) * gain
        out = S.mix(out, S.at(tone, i * step))
    return out


def level_up(rng):
    lead = _melody([523.25, 659.25, 783.99, 1046.5, 1318.5, 1568], 0.085, 0.3, S.square, 0.25, 4, 0.45)
    bass = _melody([130.8, 130.8, 196, 196, 261.6, 261.6], 0.085, 0.25, S.tri, k=3, gain=0.5)
    return S.mix(lead, bass, S.at(S.sine(2093, 0.5) * S.env_decay(0.5, 5) * 0.3, 0.43))


def victory(rng):
    lead = _melody([783.99, 783.99, 783.99, 1046.5], 0.12, 0.28, S.square, 0.3, 3, 0.45)
    harm = _melody([659.25, 659.25, 659.25, 783.99], 0.12, 0.28, S.square, 0.5, 3, 0.25)
    bass = _melody([261.6, 261.6, 261.6, 261.6], 0.12, 0.25, S.tri, k=3, gain=0.5)
    tail = S.at(S.mix(S.square(1046.5, 0.6, 0.3), S.square(1318.5, 0.6, 0.3) * 0.7, S.tri(261.6, 0.6)) * S.env_decay(0.6, 3) * 0.35, 0.36)
    return S.mix(lead, harm, bass, tail)


def defeat(rng):
    lead = _melody([440, 415.3, 392, 349.2], 0.22, 0.42, S.square, 0.5, 3, 0.35)
    bass = _melody([220, 207.7, 196, 174.6], 0.22, 0.42, S.tri, k=3, gain=0.5)
    end = S.at(S.mix(S.tri(146.8, 1.0), S.tri(174.6, 1.0) * 0.6) * S.env_decay(1.0, 3) * 0.5, 0.88)
    return S.mix(lead, bass, end)


def chest_open(rng):
    creak = S.saw(S.sweep(v(rng, 90, 0.2), v(rng, 140, 0.2), 0.35, 1.3), 0.35)
    creak = S.bp(creak, 1100, 1.4) * S.tremolo(np.ones(S.n(0.35), np.float32), 38, 0.75) * S.env_points(0.35, [(0, 0), (0.2, 1), (1, 0)]) * 0.7
    latch = S.mix(click(rng, 0.01, 1500), thump(rng, 400, 200, 0.05, 16) * 0.5)
    sparkle = _melody([1568, 2093, 2637, 3136], 0.05, 0.25, S.sine, k=6, gain=0.35)
    return S.mix(latch, S.at(creak, 0.03), S.at(sparkle, 0.32))


def door_open(rng):
    creak = S.saw(S.sweep(v(rng, 70, 0.2), v(rng, 115, 0.2), 0.6, 1.2), 0.6)
    creak = S.bp(creak, 800, 1.4) * S.tremolo(np.ones(S.n(0.6), np.float32), 24, 0.7) * S.env_points(0.6, [(0, 0), (0.15, 1), (0.85, 0.8), (1, 0)]) * 0.8
    return S.mix(S.at(creak, 0.0), S.at(thump(rng, 120, 50, 0.2, 9) * 0.6, 0.55), click(rng, 0.008, 1200) * 0.5)


def door_close(rng):
    return S.mix(thump(rng, 110, 40, 0.3, 7), S.lp(S.noise(0.08, rng), 700) * S.env_decay(0.08, 10) * 2, click(rng, 0.008, 1500) * 0.6,
                 S.at(click(rng, 0.006, 3000) * 0.4, 0.06))


def coins(rng):
    out = np.zeros(S.n(0.5), dtype=np.float32)
    for i in range(rng.integers(4, 7)):
        f = v(rng, 4200, 0.25)
        c_ = S.mix(clang(rng, f, 0.18, (1.0, 1.5, 2.4), 14) * 0.5, S.sine(f * 1.02, 0.15) * S.env_decay(0.15, 12) * 0.3)
        out = S.mix(out, S.at(c_, i * v(rng, 0.055, 0.25)))
    return out


def potion(rng):
    out = np.zeros(S.n(0.7), dtype=np.float32)
    for i in range(4):
        f = v(rng, 240, 0.2)
        g = S.sine(S.sweep(f, f * 2.4, 0.08, 0.5), 0.08) * S.env_decay(0.08, 7, 0.004) * 0.6
        out = S.mix(out, S.at(S.lp(g, 2500), i * 0.09))
    gulp = S.lp_sweep(S.noise(0.12, rng), S.sweep(1500, 300, 0.12)) * S.env_decay(0.12, 8) * 1.2
    sparkle = _melody([1318.5, 1760, 2637], 0.06, 0.25, S.sine, k=6, gain=0.3)
    return S.mix(out, S.at(gulp, 0.36), S.at(sparkle, 0.45))


def footstep_stone(rng):
    d = v(rng, 0.09, 0.2)
    return S.mix(thump(rng, v(rng, 140, 0.2), 60, d, 12) * 0.8,
                 S.lp(S.noise(0.05, rng), v(rng, 1400, 0.3)) * S.env_decay(0.05, 12) * 1.8,
                 S.hp(S.noise(0.02, rng), 3000) * S.env_decay(0.02, 12) * 0.4)


def trap_trigger(rng):
    plate = S.mix(click(rng, 0.01, 1200), thump(rng, 300, 150, 0.06, 14) * 0.6)
    spring = S.sine(S.sweep(600, 1800, 0.12, 0.7), 0.12) * S.env_decay(0.12, 6) * 0.35
    shing = S.mix(S.bp(S.noise(0.25, rng), 5500, 1.2) * S.env_decay(0.25, 6) * 1.2, clang(rng, 3400, 0.3, k=8) * 0.4)
    return S.mix(plate, S.at(spring, 0.04), S.at(shing, 0.14))


def stairs(rng):
    out = np.zeros(S.n(0.9), dtype=np.float32)
    for i in range(5):
        out = S.mix(out, S.at(footstep_stone(rng) * (1 - i * 0.12), i * v(rng, 0.16, 0.1)))
    return S.mix(out, S.at(whoosh(rng, 0.9, 300, 900, 0.8, 0.9) * 0.25, 0.0))


def item_pickup(rng):
    return S.seq(blip(1046.5, 0.05, S.square, 0.25, 5), blip(1568, 0.11, S.square, 0.25, 5))


def gold_pickup(rng):
    return S.mix(S.seq(blip(1318.5, 0.045, S.square, 0.25, 5), blip(2093, 0.1, S.square, 0.25, 5)),
                 S.at(coins(rng) * 0.35, 0.02))


NOROOM = dict(crush_bits=9, room=0.0)
SOUNDS = [
    dict(name="ui_move", fn=ui_move, blurb="Cursor moves.", finish=NOROOM),
    dict(name="ui_select", fn=ui_select, blurb="Confirm.", finish=NOROOM),
    dict(name="ui_back", fn=ui_back, blurb="Cancel / back.", finish=NOROOM),
    dict(name="ui_error", fn=ui_error, blurb="Invalid action.", finish=NOROOM),
    dict(name="turn_start", fn=turn_start, blurb="Player's turn begins.", finish=dict(crush_bits=10, room=0.15)),
    dict(name="level_up", fn=level_up, blurb="Rising fanfare.", finish=dict(crush_bits=9, room=0.12)),
    dict(name="victory", fn=victory, blurb="Battle won.", finish=dict(crush_bits=9, room=0.14)),
    dict(name="defeat", fn=defeat, blurb="Party wiped.", finish=dict(crush_bits=9, room=0.2)),
    dict(name="chest_open", fn=chest_open, blurb="Latch, creak, sparkle.", finish=dict(crush_bits=10, room=0.14)),
    dict(name="door_open", fn=door_open, blurb="Heavy wooden creak.", finish=dict(crush_bits=10, room=0.18)),
    dict(name="door_close", fn=door_close, blurb="Slam.", finish=dict(crush_bits=10, room=0.2)),
    dict(name="coins", fn=coins, blurb="Handful of coins.", finish=dict(crush_bits=10, room=0.1)),
    dict(name="potion", fn=potion, blurb="Glug, gulp, sparkle.", finish=dict(crush_bits=10, room=0.08)),
    dict(name="footstep_stone", fn=footstep_stone, blurb="One step on flagstones.", finish=dict(crush_bits=10, room=0.16)),
    dict(name="trap_trigger", fn=trap_trigger, blurb="Plate, spring, spikes.", finish=dict(crush_bits=10, room=0.14)),
    dict(name="stairs", fn=stairs, blurb="Descending to the next floor.", finish=dict(crush_bits=10, room=0.2)),
    dict(name="item_pickup", fn=item_pickup, blurb="Picked something up.", finish=NOROOM),
    dict(name="gold_pickup", fn=gold_pickup, blurb="Picked up gold.", finish=dict(crush_bits=9, room=0.05)),
]
for s_ in SOUNDS:
    s_["category"] = "ui" if s_["name"].startswith("ui_") or s_["name"] in ("turn_start", "level_up", "victory", "defeat", "item_pickup", "gold_pickup") else "dungeon"

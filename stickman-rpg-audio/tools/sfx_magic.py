"""One sound per effect in the art pack, so fx_fire.png and fire.wav land
together. Named after the effect, not the spell."""

import numpy as np
import synth as S
from sfx_voices import v
from sfx_combat import thump, click, whoosh


def chord(freqs, dur, wave=S.sine, k=4, stagger=0.0):
    out = np.zeros(S.n(dur) + S.n(stagger) * len(freqs), dtype=np.float32)
    for i, f in enumerate(freqs):
        out = S.mix(out, S.at(wave(f, dur) * S.env_decay(dur, k) / (1 + i * 0.3), stagger * i))
    return out


def fire(rng):
    d = v(rng, 0.7, 0.12)
    roar = S.lp_sweep(S.noise(d, rng, "pink"), S.sweep(400, 2200, d, 0.5) * S.env_points(d, [(0, 1), (0.5, 1), (1, 0.3)]) + 200) \
        * S.env_points(d, [(0, 0), (0.25, 1), (0.7, 0.8), (1, 0)]) * 2.2
    out = roar
    for _ in range(rng.integers(14, 22)):                       # crackles
        t0 = rng.uniform(0.05, d * 0.9)
        out = S.mix(out, S.at(click(rng, v(rng, 0.006, 0.4), 2500) * rng.uniform(0.3, 0.8), t0))
    return S.mix(out, S.at(whoosh(rng, 0.25, 300, 1600, 0.9) * 0.7, 0.0))


def ice(rng):
    d = 0.6
    base = v(rng, 1900, 0.1)
    glass = np.zeros(S.n(d), dtype=np.float32)
    for i, r in enumerate((1.0, 1.31, 1.62, 2.11, 2.73, 3.4)):
        glass = S.mix(glass, S.at(S.sine(base * r, 0.45) * S.env_decay(0.45, 5 + i), i * 0.025) / (1 + i * 0.4))
    shatter = np.zeros(S.n(d), dtype=np.float32)
    for _ in range(rng.integers(8, 14)):
        f = v(rng, 5000, 0.4)
        shatter = S.mix(shatter, S.at(S.sine(f, 0.08) * S.env_decay(0.08, 10) * 0.35, rng.uniform(0.15, 0.5)))
    crack = click(rng, 0.01, 4000) * 1.2
    return S.mix(glass * 1.2, shatter, crack, S.hp(S.noise(0.12, rng), 6000) * S.env_decay(0.12, 10) * 0.6)


def lightning(rng):
    d = 0.9
    crack = S.hp(S.noise(0.05, rng), 1500) * S.env_decay(0.05, 20, 0.0002) * 3.5
    zap = S.square(S.sweep(v(rng, 3800, 0.15), 180, 0.16, 0.5), 0.16, 0.2) * S.env_decay(0.16, 7) * 0.5
    rumble = S.lp(S.noise(d, rng, "brown"), 140) * 2.5 * S.env_points(d, [(0, 0), (0.08, 1), (1, 0)])
    sizzle = S.bp(S.noise(0.5, rng), 6500, 1.0) * S.tremolo(np.ones(S.n(0.5), np.float32), 45, 0.9) * S.env_decay(0.5, 6) * 0.6
    return S.mix(crack, S.at(zap, 0.004), S.at(rumble, 0.03), S.at(sizzle, 0.02))


def poison(rng):
    d = v(rng, 0.8, 0.12)
    hiss = S.bp_sweep(S.noise(d, rng, "pink"), S.sweep(1200, 500, d), 1.0) * S.env_points(d, [(0, 0), (0.2, 1), (1, 0)]) * 1.6
    out = hiss
    for _ in range(rng.integers(8, 13)):
        f = v(rng, 320, 0.5)
        b = S.sine(S.sweep(f, f * 2.2, 0.09, 0.6), 0.09) * S.env_decay(0.09, 8, 0.003) * 0.6
        out = S.mix(out, S.at(S.lp(b, 1800), rng.uniform(0.05, d * 0.85)))
    return out


def heal(rng):
    notes = [523.25, 659.25, 783.99, 1046.5, 1318.5]        # C E G C E - rising pentatonic
    out = np.zeros(S.n(0.9), dtype=np.float32)
    for i, f in enumerate(notes):
        f *= v(rng, 1.0, 0.004)
        tone = S.mix(S.sine(f, 0.5), S.sine(f * 2, 0.5) * 0.3, S.tri(f * 3, 0.5) * 0.08) * S.env_decay(0.5, 4)
        out = S.mix(out, S.at(tone, i * 0.075))
    shimmer = S.bp(S.noise(0.9, rng), 7000, 1.0) * S.env_points(0.9, [(0, 0), (0.3, 0.5), (1, 0)]) * 0.35
    return S.mix(out, shimmer)


def arcane(rng):
    d = 0.7
    car = S.sweep(v(rng, 180, 0.1), v(rng, 720, 0.1), d, 0.8)
    mod = S.sine(car * 2.01, d) * S.env_points(d, [(0, 4), (1, 0.5)])
    fm = np.sin(S.TAU * np.cumsum(car) / S.SR + mod).astype(np.float32)
    swell = S.env_points(d, [(0, 0), (0.35, 1), (0.6, 0.7), (1, 0)])
    air = S.bp_sweep(S.noise(d, rng), S.sweep(600, 3600, d), 1.2) * swell * 0.7
    return S.mix(fm * swell, air, S.at(chord([880, 1320, 1760], 0.4, k=6) * 0.35, 0.3))


def buff(rng):
    d = 0.55
    rise = S.mix(S.sine(S.sweep(330, 1320, d, 0.9), d), S.tri(S.sweep(330, 1320, d, 0.9) * 1.5, d) * 0.3) \
        * S.env_points(d, [(0, 0), (0.15, 1), (0.8, 0.7), (1, 0)])
    return S.mix(rise * 0.8, S.at(chord([1046.5, 1318.5, 1568], 0.45, k=5, stagger=0.03) * 0.6, 0.3))


def debuff(rng):
    d = 0.6
    fall = S.mix(S.saw(S.sweep(660, 165, d, 1.2), d) * 0.5, S.saw(S.sweep(660 * 1.06, 165 * 1.06, d, 1.2), d) * 0.5)
    fall = S.lp(fall, 2200) * S.env_points(d, [(0, 0), (0.1, 1), (1, 0)])
    return S.mix(fall, S.at(chord([233.1, 277.2], 0.5, wave=S.tri, k=5) * 0.5, 0.15))


def stun(rng):
    out = np.zeros(S.n(0.8), dtype=np.float32)
    for i, f in enumerate((1568, 1318.5, 1046.5, 1318.5, 1568, 1318.5)):
        tone = S.sine(S.vibrato(f * v(rng, 1.0, 0.005), 0.2, 12, 0.03), 0.2) * S.env_decay(0.2, 7) * 0.7
        out = S.mix(out, S.at(tone, i * 0.11))
    return out


def explosion(rng):
    d = 1.0
    boom = S.lp(S.noise(0.5, rng), 500) * S.env_decay(0.5, 6) * 3
    sub = thump(rng, 90, 28, 0.6, 5) * 1.5
    crack = S.hp(S.noise(0.03, rng), 2000) * S.env_decay(0.03, 14, 0.0003) * 2.5
    tail = S.lp(S.noise(d, rng, "brown"), 300) * 2.0 * S.env_decay(d, 5)
    out = S.mix(crack, boom, sub, S.at(tail, 0.05))
    for _ in range(rng.integers(6, 10)):                                # debris
        out = S.mix(out, S.at(click(rng, 0.008, 1800) * rng.uniform(0.2, 0.6), rng.uniform(0.25, 0.9)))
    return out


def shockwave(rng):
    d = 0.7
    sub = S.sine(S.sweep(110, 25, d, 0.5), d) * S.env_points(d, [(0, 0), (0.05, 1), (1, 0)]) * 1.4
    rumble = S.lp(S.noise(d, rng, "brown"), 200) * 2.2 * S.env_points(d, [(0, 0), (0.1, 1), (1, 0)])
    gravel = S.bp(S.noise(0.5, rng), 900, 1.0) * S.env_decay(0.5, 6) * 0.9
    return S.mix(sub, rumble, S.at(gravel, 0.03), thump(rng, 160, 50, 0.15, 10))


def smoke(rng):
    d = v(rng, 0.55, 0.15)
    puff = S.lp_sweep(S.noise(d, rng, "pink"), S.sweep(1800, 400, d, 0.8)) * S.env_points(d, [(0, 0), (0.12, 1), (1, 0)]) * 2.0
    return puff


SOUNDS = [
    dict(name="fire", fn=fire, blurb="Roar with crackles; pairs with fx_fire.", finish=dict(crush_bits=10, room=0.1)),
    dict(name="ice", fn=ice, blurb="Glassy partials and a shatter; fx_ice.", finish=dict(crush_bits=11, room=0.2)),
    dict(name="lightning", fn=lightning, blurb="Crack, zap, rumble; fx_lightning.", finish=dict(crush_bits=10, room=0.18)),
    dict(name="poison", fn=poison, blurb="Hiss with bubbles; fx_poison.", finish=dict(crush_bits=10, room=0.08)),
    dict(name="heal", fn=heal, blurb="Rising pentatonic shimmer; fx_heal.", finish=dict(crush_bits=11, room=0.22)),
    dict(name="arcane", fn=arcane, blurb="FM swell into a chord; fx_arcane.", finish=dict(crush_bits=10, room=0.2)),
    dict(name="buff", fn=buff, blurb="Rising sweep with a chime; fx_buff.", finish=dict(crush_bits=10, room=0.16)),
    dict(name="debuff", fn=debuff, blurb="Detuned fall; fx_debuff.", finish=dict(crush_bits=9, room=0.14)),
    dict(name="stun", fn=stun, blurb="Wobbly circling bells; fx_stun.", finish=dict(crush_bits=10, room=0.14)),
    dict(name="explosion", fn=explosion, blurb="Boom, sub, debris; fx_explosion.", finish=dict(crush_bits=9, room=0.18)),
    dict(name="shockwave", fn=shockwave, blurb="Sub sweep and gravel; fx_shockwave.", finish=dict(crush_bits=10, room=0.16)),
    dict(name="smoke", fn=smoke, blurb="Soft airy puff; fx_smoke.", finish=dict(crush_bits=11, room=0.1)),
]
for s_ in SOUNDS:
    s_["category"] = "magic"

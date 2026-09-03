"""Combat impacts. Every hit is layers: a transient (the click), a body
(the thump or ring) and a tail (the whoosh or debris)."""

import numpy as np
import synth as S
from sfx_voices import v


def whoosh(rng, dur=0.22, f0=600, f1=3200, q=1.2, curve=0.8):
    x = S.noise(dur, rng)
    x = S.bp_sweep(x, S.sweep(f0, f1, dur, curve), q)
    return x * S.env_points(dur, [(0, 0), (0.55, 1), (1, 0)]) * 2.2


def thump(rng, f0=160, f1=45, dur=0.22, k=8):
    return S.sine(S.sweep(f0, f1, dur, 0.6), dur) * S.env_decay(dur, k)


def click(rng, dur=0.012, hpf=2500):
    return S.hp(S.noise(dur, rng), hpf) * S.env_decay(dur, 12, 0.0002) * 2.5


def clang(rng, f=1900, dur=0.35, partials=(1.0, 1.42, 2.13, 2.87, 3.71), k=9):
    """Inharmonic partials = metal. Harmonic ones would sound like a bell."""
    out = np.zeros(S.n(dur), dtype=np.float32)
    for i, r in enumerate(partials):
        out += S.sine(f * r * v(rng, 1.0, 0.01), dur) * S.env_decay(dur, k + i * 2) / (i + 1)
    return out


def slash_light(rng):
    d = v(rng, 0.2, 0.15)
    w = whoosh(rng, d, v(rng, 900, 0.15), v(rng, 5200, 0.15), 1.0)
    imp = S.at(S.mix(click(rng, 0.01, 4000) * 1.6,
                     S.bp(S.noise(0.07, rng), 3200, 1.5) * S.env_decay(0.07, 12) * 3.2,
                     thump(rng, 260, 120, 0.05, 16) * 0.5), d * 0.55)
    return S.mix(w * 0.8, imp)


def slash_heavy(rng):
    d = v(rng, 0.34, 0.15)
    w = whoosh(rng, d, v(rng, 350, 0.15), v(rng, 2400, 0.15), 0.9, 0.7)
    hit = S.mix(thump(rng, 140, 50, 0.18) * 1.4,
                S.bp(S.noise(0.1, rng), 1800, 1.2) * S.env_decay(0.1, 10) * 3.0, click(rng) * 1.5)
    return S.mix(w * 0.75, S.at(hit, d * 0.6))


def blunt_hit(rng):
    return S.mix(thump(rng, v(rng, 170, 0.15), 48, 0.2, 9),
                 S.lp(S.noise(0.08, rng), 900) * S.env_decay(0.08, 12) * 2.5,
                 click(rng, 0.008, 1800) * 0.7)


def blunt_heavy(rng):
    return S.mix(thump(rng, v(rng, 120, 0.15), 35, 0.4, 6) * 1.2,
                 S.lp(S.noise(0.14, rng), 600) * S.env_decay(0.14, 9) * 2.6,
                 S.lp(S.noise(0.3, rng, "brown"), 200) * S.env_decay(0.3, 5) * 3,
                 click(rng, 0.01, 1500))


def pierce(rng):
    d = v(rng, 0.09, 0.2)
    return S.mix(click(rng, 0.006, 5000) * 1.4,
                 S.bp(S.noise(d, rng), v(rng, 2600, 0.15), 2.5) * S.env_decay(d, 16) * 2.2,
                 thump(rng, 220, 90, 0.07, 14) * 0.6)


def hit_flesh(rng):
    d = v(rng, 0.14, 0.15)
    return S.mix(S.lp_sweep(S.noise(d, rng), S.sweep(1800, 250, d, 0.6)) * S.env_decay(d, 10) * 2.2,
                 thump(rng, 150, 60, 0.12, 10) * 0.8)


def hit_bone(rng):
    x = S.noise(0.07, rng) * S.env_decay(0.07, 18, 0.0003)
    return S.mix(S.resonator(x, v(rng, 1300, 0.25), 14) * 1.3, S.hp(x, 3000) * 1.2,
                 thump(rng, 200, 90, 0.06, 16) * 0.5)


def hit_armor(rng):
    return S.mix(clang(rng, v(rng, 2200, 0.15), 0.25, k=12) * 0.6,
                 thump(rng, 160, 55, 0.16, 10) * 0.9,
                 S.lp(S.noise(0.06, rng), 1200) * S.env_decay(0.06, 12) * 1.6)


def claw_hit(rng):
    out = np.zeros(S.n(0.3), dtype=np.float32)
    for i in range(3):
        d = 0.08
        w = whoosh(rng, d, 1200, 4800, 1.4) * 0.8
        imp = S.at(S.bp(S.noise(0.04, rng), 2800, 1.5) * S.env_decay(0.04, 14) * 1.5, d * 0.5)
        out = S.mix(out, S.at(S.mix(w, imp), i * v(rng, 0.055, 0.15)))
    return out


def bow_release(rng):
    """Karplus-Strong string: a burst of noise fed through a short delay."""
    f = v(rng, 95, 0.12)
    d = int(S.SR / f)
    N = S.n(0.35)
    buf = rng.uniform(-1, 1, d).astype(np.float32)
    out = np.zeros(N, dtype=np.float32)
    for i in range(N):
        out[i] = buf[i % d]
        buf[i % d] = 0.5 * (buf[i % d] + buf[(i + 1) % d]) * 0.992
    twang = S.hp(out, 120) * S.env_decay(0.35, 5) * 1.4
    fly = S.at(whoosh(rng, 0.28, 1500, 5000, 1.6) * 0.5, 0.02)
    return S.mix(twang, fly, click(rng, 0.006, 3000) * 0.5)


def crossbow_release(rng):
    thk = S.mix(thump(rng, 200, 70, 0.08, 14), S.lp(S.noise(0.04, rng), 2500) * S.env_decay(0.04, 14) * 2, click(rng, 0.008, 2000))
    f = v(rng, 140, 0.12)
    d = int(S.SR / f)
    N = S.n(0.22)
    buf = rng.uniform(-1, 1, d).astype(np.float32)
    out = np.zeros(N, dtype=np.float32)
    for i in range(N):
        out[i] = buf[i % d]
        buf[i % d] = 0.5 * (buf[i % d] + buf[(i + 1) % d]) * 0.985
    return S.mix(thk, S.at(S.hp(out, 150) * S.env_decay(0.22, 6), 0.01),
                 S.at(whoosh(rng, 0.2, 2000, 6000, 1.8) * 0.45, 0.02))


def shield_block(rng):
    x = S.noise(0.12, rng) * S.env_decay(0.12, 10, 0.0005)
    body = S.resonator(x, v(rng, 420, 0.2), 8) * 1.5 + S.resonator(x, v(rng, 780, 0.2), 10) * 0.8
    return S.mix(body, thump(rng, 180, 70, 0.15, 10) * 1.0, S.lp(x, 1500) * 1.2)


def parry_clang(rng):
    return S.mix(clang(rng, v(rng, 2600, 0.12), 0.45, k=7) * 0.9, click(rng, 0.008, 5000) * 1.2,
                 S.bp(S.noise(0.05, rng), 4000, 1.5) * S.env_decay(0.05, 12))


def miss_whoosh(rng):
    return whoosh(rng, v(rng, 0.26, 0.15), v(rng, 500, 0.15), v(rng, 2600, 0.15), 1.0, 0.7)


def critical_hit(rng):
    hit = S.mix(thump(rng, 180, 40, 0.3, 7) * 1.2,
                S.lp(S.noise(0.12, rng), 1400) * S.env_decay(0.12, 9) * 2.4, click(rng))
    sting = S.square(S.sweep(600, 2400, 0.18, 0.6), 0.18, 0.3) * S.env_decay(0.18, 5) * 0.35
    shimmer = S.mix(*[S.sine(v(rng, 3000, 0.02) * r, 0.3) * S.env_decay(0.3, 6 + i * 3) / (i + 1.5)
                      for i, r in enumerate((1.0, 1.5, 2.0, 2.5))]) * 0.5
    return S.mix(hit, S.at(sting, 0.01), S.at(shimmer, 0.03))


def spell_cast_generic(rng):
    """The caster's wind-up before any spell lands."""
    d = 0.4
    tone = S.sine(S.sweep(220, 880, d, 1.4), d) * 0.5 + S.tri(S.sweep(330, 1320, d, 1.4), d) * 0.3
    air = S.bp_sweep(S.noise(d, rng), S.sweep(800, 4000, d), 1.0) * 0.8
    return S.mix(tone, air) * S.env_points(d, [(0, 0), (0.7, 1), (1, 0)])


SOUNDS = [
    dict(name="slash_light", fn=slash_light, blurb="Dagger, sword, katana."),
    dict(name="slash_heavy", fn=slash_heavy, blurb="Greatsword, axe, scythe."),
    dict(name="blunt_hit", fn=blunt_hit, blurb="Mace, club, fist."),
    dict(name="blunt_heavy", fn=blunt_heavy, blurb="Hammer, maul, ogre club."),
    dict(name="pierce", fn=pierce, blurb="Arrow, bolt, spear, trident landing."),
    dict(name="hit_flesh", fn=hit_flesh, blurb="Impact on an organic target."),
    dict(name="hit_bone", fn=hit_bone, blurb="Impact on a skeleton."),
    dict(name="hit_armor", fn=hit_armor, blurb="Impact on plate or a golem."),
    dict(name="claw_hit", fn=claw_hit, blurb="Beast or monk triple-rake."),
    dict(name="bow_release", fn=bow_release, blurb="String twang plus arrow flight."),
    dict(name="crossbow_release", fn=crossbow_release, blurb="Trigger thunk, twang, bolt."),
    dict(name="shield_block", fn=shield_block, blurb="Wooden shield absorbs a hit."),
    dict(name="parry_clang", fn=parry_clang, blurb="Steel on steel."),
    dict(name="miss_whoosh", fn=miss_whoosh, blurb="A swing that finds nothing."),
    dict(name="critical_hit", fn=critical_hit, blurb="Big hit plus a bright sting."),
    dict(name="spell_cast", fn=spell_cast_generic, blurb="Caster wind-up before any spell."),
]
for s_ in SOUNDS:
    s_.setdefault("category", "combat")
    s_.setdefault("finish", dict(crush_bits=10, room=0.12))

"""Creature hurt and death voices.

A voice is a glottal pulse train, pitched by a contour, pushed through a
few resonators at vowel formant frequencies, with breath noise mixed in.
Change the pitch, the formant scale and the roughness and the same engine
gives you a goblin, a bear, or a demon. Skeletons, slimes and golems have
no larynx, so they get bone, water and stone instead.
"""

import numpy as np
import synth as S

VOWELS = {                # F1, F2, F3 (Hz)
    "a": (730, 1090, 2440), "o": (570, 840, 2410), "u": (300, 870, 2240),
    "e": (530, 1840, 2480), "i": (270, 2290, 3010), "uh": (620, 1200, 2400),
    "ae": (660, 1720, 2410),
}


def v(rng, base, pct=0.1):
    return base * (1 + rng.uniform(-pct, pct))


def voice(rng, dur, f0, vowels, breath=0.15, rough=0.0, fscale=1.0,
          width=0.1, drive_amt=1.6, hp_cut=60, lp_cut=6200):
    """f0: frequency array. vowels: list of (time_frac, vowel) breakpoints."""
    N = S.n(dur)
    src = S.pulse_train(f0, dur, width)
    src = src - np.mean(src)
    if rough > 0:                                   # growl: amplitude flutter
        tt = S.t(dur)
        rate = v(rng, 26, 0.3)
        src = src * (1 - rough * 0.5 * (1 + np.sign(np.sin(S.TAU * rate * tt))))
    src = src + S.noise(dur, rng, "pink") * breath
    outs = []
    for _, vw in vowels:
        fs = [f * fscale for f in VOWELS[vw]]
        outs.append(S.formants(src, fs, qs=[9, 11, 13], gains=[1.0, 0.7, 0.35]))
    if len(outs) == 1:
        y = outs[0]
    else:                                           # crossfade along the breakpoints
        y = np.zeros(N, dtype=np.float32)
        x = np.linspace(0, 1, N)
        ts = [p[0] for p in vowels]
        for i in range(len(outs)):
            w = np.interp(x, ts, [1.0 if j == i else 0.0 for j in range(len(outs))])
            y += outs[i] * w
    y = S.hp(y, hp_cut)
    y = S.lp(y, lp_cut)                             # real throats roll off up top
    y = S.drive(y, drive_amt)
    return y


# ---------------------------------------------------------------------------
#  families
# ---------------------------------------------------------------------------

def human_hurt(rng):
    dur = v(rng, 0.30, 0.15)
    f0 = S.sweep(v(rng, 175, 0.08), v(rng, 120, 0.1), dur, 0.7)
    f0 = S.jitter(f0, dur, rng, 0.02)
    y = voice(rng, dur, f0, [(0, "uh"), (0.6, "u")], breath=0.2, width=0.12)
    return y * S.env_adsr(dur, 0.01, 0.08, 0.5, 0.12)


def human_death(rng):
    dur = v(rng, 0.85, 0.12)
    f0 = S.sweep(v(rng, 165, 0.08), v(rng, 70, 0.1), dur, 0.5)
    f0 = S.jitter(f0, dur, rng, 0.035, 12)
    y = voice(rng, dur, f0, [(0, "a"), (0.4, "o"), (1, "u")], breath=0.3, width=0.14)
    return y * S.env_adsr(dur, 0.02, 0.2, 0.55, 0.35, 1.5)


def goblin_hurt(rng):
    dur = v(rng, 0.22, 0.2)
    f0 = S.sweep(v(rng, 330, 0.1), v(rng, 240, 0.1), dur, 0.6)
    f0 = S.jitter(f0, dur, rng, 0.05, 40)
    y = voice(rng, dur, f0, [(0, "ae"), (1, "e")], breath=0.12, fscale=1.25, width=0.07)
    return y * S.env_adsr(dur, 0.004, 0.05, 0.6, 0.08)


def goblin_death(rng):
    dur = v(rng, 0.6, 0.15)
    f0 = S.sweep(v(rng, 420, 0.1), v(rng, 150, 0.15), dur, 0.35)
    f0 = S.vibrato(f0, dur, 9, 0.06)
    y = voice(rng, dur, f0, [(0, "i"), (0.5, "ae"), (1, "a")], breath=0.18, fscale=1.25, width=0.07)
    return y * S.env_adsr(dur, 0.01, 0.1, 0.6, 0.25, 1.4)


def beast_hurt(rng):
    dur = v(rng, 0.32, 0.15)
    f0 = S.sweep(v(rng, 120, 0.1), v(rng, 90, 0.1), dur, 0.8)
    f0 = S.jitter(f0, dur, rng, 0.04, 25)
    y = voice(rng, dur, f0, [(0, "a"), (1, "o")], breath=0.25, rough=0.7, fscale=0.9,
              width=0.16, drive_amt=2.4)
    return y * S.env_adsr(dur, 0.01, 0.06, 0.7, 0.12)


def beast_death(rng):
    dur = v(rng, 0.9, 0.12)
    f0 = S.sweep(v(rng, 220, 0.1), v(rng, 60, 0.15), dur, 0.45)
    f0 = S.vibrato(f0, dur, 7, 0.05)
    y = voice(rng, dur, f0, [(0, "i"), (0.3, "a"), (1, "u")], breath=0.3, rough=0.4,
              fscale=0.95, width=0.14, drive_amt=2.0)
    return y * S.env_adsr(dur, 0.02, 0.15, 0.55, 0.4, 1.5)


def undead_hurt(rng):
    dur = v(rng, 0.45, 0.15)
    f0 = S.sweep(v(rng, 105, 0.1), v(rng, 85, 0.1), dur, 1.0)
    f0 = S.jitter(f0, dur, rng, 0.06, 8)
    y = voice(rng, dur, f0, [(0, "o"), (1, "u")], breath=0.5, rough=0.25, fscale=0.85,
              width=0.2, drive_amt=2.0)
    return y * S.env_adsr(dur, 0.05, 0.1, 0.7, 0.2)


def undead_death(rng):
    dur = v(rng, 1.3, 0.12)
    f0 = S.sweep(v(rng, 120, 0.1), v(rng, 45, 0.15), dur, 0.7)
    f0 = S.jitter(f0, dur, rng, 0.07, 6)
    y = voice(rng, dur, f0, [(0, "a"), (0.5, "o"), (1, "u")], breath=0.55, rough=0.3,
              fscale=0.85, width=0.2, drive_amt=2.2)
    rattle = S.bp(S.noise(dur, rng), 2600, 1.2) * S.tremolo(np.ones(S.n(dur), np.float32), 22, 0.9) \
        * S.env_points(dur, [(0, 0), (0.55, 0), (0.7, 0.5), (1, 0)])
    return S.mix(y * S.env_adsr(dur, 0.04, 0.2, 0.6, 0.5, 1.4), rattle * 0.5)


def demon_hurt(rng):
    dur = v(rng, 0.4, 0.15)
    f0 = S.sweep(v(rng, 95, 0.1), v(rng, 70, 0.1), dur, 0.8)
    f0 = S.jitter(f0, dur, rng, 0.03, 20)
    y = voice(rng, dur, f0, [(0, "a"), (1, "o")], breath=0.2, rough=0.6, fscale=0.8,
              width=0.18, drive_amt=3.5)
    sub = S.sine(f0 * 0.5, dur) * 0.6
    y = S.mix(y, sub)
    return y * S.env_adsr(dur, 0.01, 0.08, 0.7, 0.15)


def demon_death(rng):
    dur = v(rng, 1.4, 0.12)
    f0 = S.sweep(v(rng, 140, 0.1), v(rng, 35, 0.15), dur, 0.5)
    f0 = S.vibrato(f0, dur, 5, 0.04)
    y = voice(rng, dur, f0, [(0, "a"), (0.4, "o"), (1, "u")], breath=0.25, rough=0.5,
              fscale=0.8, width=0.18, drive_amt=3.5)
    sub = S.sine(f0 * 0.5, dur) * 0.7
    rumble = S.lp(S.noise(dur, rng, "brown"), 120) * 2.0 * S.env_points(dur, [(0, 0.2), (0.6, 1), (1, 0)])
    return S.mix(y * S.env_adsr(dur, 0.02, 0.25, 0.6, 0.5, 1.4), sub * S.env_decay(dur, 2), rumble)


# ---- voiceless families ----------------------------------------------------

def _knock(rng, f, dur=0.06, q=18):
    x = S.noise(dur, rng) * S.env_decay(dur, 14, 0.0005)
    return S.resonator(x, f, q) * 1.2 + S.hp(x, 3000) * 0.25 * S.env_decay(dur, 30)


def skeleton_hurt(rng):
    total = v(rng, 0.3, 0.15)
    out = np.zeros(S.n(total), dtype=np.float32)
    k = rng.integers(4, 7)
    for i in range(k):
        t0 = (i / k) * total * 0.8 + rng.uniform(0, 0.02)
        out = S.mix(out, S.at(_knock(rng, v(rng, 1400, 0.35)), t0))
    return out


def skeleton_death(rng):
    total = v(rng, 0.9, 0.12)
    out = np.zeros(S.n(total), dtype=np.float32)
    k = rng.integers(12, 18)
    for i in range(k):
        t0 = (i / k) ** 0.7 * total * 0.9 + rng.uniform(0, 0.03)
        f = v(rng, 1100, 0.45) * (1 - 0.3 * i / k)
        out = S.mix(out, S.at(_knock(rng, f, 0.08) * (1 - 0.4 * i / k), t0))
    thud = S.sine(S.sweep(120, 45, 0.25), 0.25) * S.env_decay(0.25, 8)
    return S.mix(out, S.at(thud * 0.8, total * 0.55))


def _bubble(rng, f, dur=0.07):
    return S.sine(S.sweep(f, f * 1.8, dur, 0.6), dur) * S.env_decay(dur, 9, 0.003)


def slime_hurt(rng):
    dur = v(rng, 0.3, 0.15)
    squelch = S.lp_sweep(S.noise(dur, rng), S.sweep(2200, 300, dur, 0.7)) * S.env_decay(dur, 7)
    out = squelch * 1.6
    for i in range(rng.integers(3, 6)):
        out = S.mix(out, S.at(_bubble(rng, v(rng, 500, 0.4)) * 0.5, rng.uniform(0.02, dur * 0.7)))
    return out


def slime_death(rng):
    dur = v(rng, 0.8, 0.12)
    splat = S.lp_sweep(S.noise(dur, rng), S.sweep(3500, 150, dur, 0.5)) * S.env_decay(dur, 5) * 2.0
    thump = S.sine(S.sweep(160, 50, 0.3), 0.3) * S.env_decay(0.3, 7) * 0.8
    out = S.mix(splat, thump)
    for i in range(rng.integers(6, 10)):
        out = S.mix(out, S.at(_bubble(rng, v(rng, 380, 0.5), 0.09) * 0.55,
                              rng.uniform(0.1, dur * 0.9)))
    return out


def _crack(rng, dur=0.05):
    x = S.noise(dur, rng) * S.env_decay(dur, 22, 0.0003)
    return S.hp(x, v(rng, 1800, 0.3)) * 1.5 + S.resonator(x, v(rng, 900, 0.3), 10)


def golem_hurt(rng):
    dur = v(rng, 0.35, 0.15)
    grind = S.bp_sweep(S.noise(dur, rng, "pink"), S.sweep(900, 400, dur), 1.5) * S.env_adsr(dur, 0.01, 0.1, 0.6, 0.15) * 2.0
    out = grind
    for i in range(rng.integers(2, 4)):
        out = S.mix(out, S.at(_crack(rng), rng.uniform(0, dur * 0.6)))
    thud = S.sine(S.sweep(90, 50, 0.2), 0.2) * S.env_decay(0.2, 8)
    return S.mix(out, thud)


def golem_death(rng):
    dur = v(rng, 1.3, 0.12)
    rumble = S.lp(S.noise(dur, rng, "brown"), 160) * 2.5 * S.env_points(dur, [(0, 0.3), (0.4, 1), (1, 0)])
    grind = S.bp_sweep(S.noise(dur, rng, "pink"), S.sweep(1200, 250, dur, 0.8), 1.3) \
        * S.env_points(dur, [(0, 1), (0.5, 0.6), (1, 0)]) * 1.5
    out = S.mix(rumble, grind)
    k = rng.integers(8, 13)
    for i in range(k):
        t0 = (i / k) ** 0.8 * dur * 0.85
        out = S.mix(out, S.at(_crack(rng, 0.07) * (1.2 - 0.5 * i / k), t0))
        out = S.mix(out, S.at(S.sine(S.sweep(110, 40, 0.25), 0.25) * S.env_decay(0.25, 9) * 0.6, t0))
    return out


FAMILIES = ["human", "goblin", "beast", "skeleton", "slime", "undead", "demon", "golem"]
FINISH = dict(human=dict(crush_bits=10, room=0.10), goblin=dict(crush_bits=9, room=0.08),
              beast=dict(crush_bits=10, room=0.12), skeleton=dict(crush_bits=10, room=0.14),
              slime=dict(crush_bits=10, room=0.06), undead=dict(crush_bits=9, room=0.18),
              demon=dict(crush_bits=10, room=0.16), golem=dict(crush_bits=10, room=0.2))

SOUNDS = []
for fam in FAMILIES:
    for kind in ("hurt", "death"):
        SOUNDS.append(dict(name=f"{fam}_{kind}", fn=globals()[f"{fam}_{kind}"],
                           category="voice", family=fam, kind=kind,
                           finish=FINISH[fam],
                           blurb=f"{fam.title()} {'takes a hit' if kind == 'hurt' else 'dies'}."))

"""Impact and spell effects, 32x32, 6 frames each.

Effects are drawn as a function of t (0..1 through the animation) rather
than as hand-drawn frames, so retiming one is a single number. They are
deliberately drawn OPAQUE at the core: translucent fire blended into a dark
background just reads as grey smudge - a lesson already recorded in this
project's art notes.
"""

import math
import random
from pixelart import Canvas
from rig import C, shift

S = 32
FRAMES = 6


def _r(seed):
    return random.Random(seed)


def glow(c, cx, cy, rad, color, peak=150):
    for j in range(max(0, int(cy - rad - 1)), min(S, int(cy + rad + 2))):
        for i in range(max(0, int(cx - rad - 1)), min(S, int(cx + rad + 2))):
            d = math.hypot(i - cx, j - cy)
            if d <= rad:
                a = (1 - d / rad) ** 2
                c.blend(i, j, (color[0], color[1], color[2], int(peak * a)))


def ring(c, cx, cy, rad, color, thick=1.6):
    for j in range(S):
        for i in range(S):
            d = math.hypot(i - cx, j - cy)
            if abs(d - rad) <= thick:
                a = 1 - abs(d - rad) / thick
                c.blend(i, j, (color[0], color[1], color[2], int(230 * a)))


def spark(c, cx, cy, ang, d0, d1, color, t=1):
    a = math.radians(ang)
    c.line(int(cx + math.cos(a) * d0), int(cy + math.sin(a) * d0),
           int(cx + math.cos(a) * d1), int(cy + math.sin(a) * d1), color, t)


# ---------------------------------------------------------------------------

WHITE = C("ffffff"); PALE = C("e8f0ff")
STEELY = C("cfd8e6")
RED = C("c4453a"); RED_D = C("8a2f28"); RED_L = C("ff8a7a")
ORANGE = C("e08a3c"); YELL = C("fae296"); GOLD = C("e0c46c")
BLUE = C("4d8fd6"); CYAN = C("8fd4e8"); ICE = C("d6f2ff")
GREEN = C("8ad148"); GREEN_D = C("4f7530")
PURPLE = C("9b6fd6"); PURPLE_L = C("d0b0f0")
SMOKE = C("6b6f7a")


def fx_slash(c, t):
    """A crescent sweeping across the target, with two fading echoes behind."""
    cx, cy = 15, 16
    lead = -100 + t * 165
    for layer, (lag, fade, col) in enumerate(
            ((0, 1.0, WHITE), (16, 0.62, PALE), (30, 0.34, STEELY))):
        a0 = lead - lag
        for k in range(34):
            u = k / 33.0
            ang = math.radians(a0 + u * 105)
            rad = 14 - abs(u - 0.5) * 4
            w = (math.sin(u * math.pi) ** 0.55) * 3.4 * fade * (1 - t * 0.35)
            x = cx + math.cos(ang) * rad
            y = cy + math.sin(ang) * rad
            if w >= 0.5:
                c.disc(int(round(x)), int(round(y)), int(w), col)
    if t < 0.55:
        glow(c, cx, cy, 15, PALE, int(60 * (1 - t * 1.8)))


def fx_impact(c, t):
    cx, cy = 16, 16
    r = 3 + t * 11
    for i in range(8):
        spark(c, cx, cy, i * 45 + 10, r * 0.35, r, YELL if t < 0.5 else ORANGE,
              2 if t < 0.4 else 1)
    c.disc(cx, cy, max(0, int(6 * (1 - t))), WHITE)
    glow(c, cx, cy, 4 + t * 10, YELL, int(160 * (1 - t)))


def fx_blood(c, t):
    rr = _r(11)
    cx, cy = 16, 15
    c.ellipse(cx, cy, max(1, int(7 * (1 - t * 0.5))),
              max(1, int(5 * (1 - t * 0.5))), RED_D if t > 0.5 else RED)
    for i in range(11):
        ang = rr.uniform(0, 360)
        d = 4 + t * rr.uniform(6, 15)
        x = int(cx + math.cos(math.radians(ang)) * d)
        y = int(cy + math.sin(math.radians(ang)) * d + t * t * 7)
        c.disc(x, y, 1 if rr.random() < 0.6 else 0, RED)
    if t < 0.4:
        c.ellipse(cx, cy - 1, 4, 3, RED_L)


def fx_fire(c, t):
    cx = 16
    base = 27 - t * 4
    h = 6 + t * 15
    for k in range(20):
        u = k / 19.0
        y = base - u * h
        w = (1 - u) * (6 - t * 2) + 1
        wob = math.sin(u * 7 + t * 6) * 2 * u
        c.ellipse(int(cx + wob), int(y), max(1, int(w)), 2, RED_D)
        c.ellipse(int(cx + wob), int(y), max(1, int(w * 0.7)), 2, ORANGE)
        if u < 0.7:
            c.ellipse(int(cx + wob), int(y), max(1, int(w * 0.35)), 1, YELL)
    glow(c, cx, int(base - h * 0.4), 12, ORANGE, int(90 * (1 - t * 0.4)))


def fx_explosion(c, t):
    cx, cy = 16, 16
    r = 2 + t * 14
    rr = _r(21)
    for i in range(14):
        ang = i * 26 + rr.uniform(-8, 8)
        d = r * rr.uniform(0.55, 1.0)
        x = int(cx + math.cos(math.radians(ang)) * d)
        y = int(cy + math.sin(math.radians(ang)) * d)
        c.disc(x, y, max(0, int(4 * (1 - t) + 1)), RED_D if t > 0.6 else ORANGE)
    c.disc(cx, cy, max(0, int(9 * (1 - t))), YELL)
    c.disc(cx, cy, max(0, int(5 * (1 - t))), WHITE)
    glow(c, cx, cy, r + 3, ORANGE, int(150 * (1 - t)))


def fx_ice(c, t):
    cx, cy = 16, 16
    for i in range(6):
        ang = i * 60 + t * 25
        d = 4 + t * 10
        a = math.radians(ang)
        x, y = cx + math.cos(a) * d, cy + math.sin(a) * d
        L = 6 * (1 - t * 0.4)
        spark(c, x, y, ang, -L * 0.5, L * 0.5, ICE, 2)
        spark(c, x, y, ang + 90, -2.5, 2.5, CYAN, 1)
        c.disc(int(x), int(y), 1, WHITE)
    c.disc(cx, cy, max(0, int(5 * (1 - t))), ICE)
    glow(c, cx, cy, 8 + t * 6, CYAN, int(110 * (1 - t)))


def fx_lightning(c, t):
    rr = _r(int(t * 100) + 3)
    x = 16
    y = 0
    seg = []
    while y < 30:
        seg.append((x, y))
        y += rr.randrange(3, 6)
        x += rr.randrange(-4, 5)
        x = max(4, min(27, x))
    col = WHITE if t < 0.45 else CYAN
    for i in range(len(seg) - 1):
        c.line(seg[i][0], seg[i][1], seg[i + 1][0], seg[i + 1][1], col,
               3 if t < 0.3 else 2)
        c.line(seg[i][0], seg[i][1], seg[i + 1][0], seg[i + 1][1], WHITE, 1)
    if t > 0.3:
        bx, by = seg[len(seg) // 2]
        for i in range(3):
            spark(c, bx, by, 40 + i * 110, 0, 5 * (1 - t), CYAN, 1)
    for (px_, py_) in seg:
        glow(c, px_, py_, 5, BLUE, int(70 * (1 - t)))


def fx_poison(c, t):
    rr = _r(31)
    for i in range(9):
        ang = rr.uniform(0, 360)
        d = t * rr.uniform(3, 12)
        x = int(16 + math.cos(math.radians(ang)) * d)
        y = int(18 + math.sin(math.radians(ang)) * d - t * 8)
        rad = int((5 - t * 2) * rr.uniform(0.6, 1.0))
        c.ellipse(x, y, max(1, rad), max(1, rad - 1), GREEN_D)
        c.ellipse(x, y - 1, max(1, rad - 2), max(1, rad - 2), GREEN)
    for i in range(4):
        x = 8 + i * 5
        c.set(x, int(24 - t * 12), GREEN)


def fx_heal(c, t):
    cx = 16
    for i in range(7):
        ph = (t + i / 7.0) % 1.0
        y = int(28 - ph * 24)
        x = int(cx + math.sin(ph * 6 + i) * 8)
        s_ = 2 if ph < 0.6 else 1
        c.rect(x - s_, y, s_ * 2 + 1, 1, C("a8f0c0"))
        c.rect(x, y - s_, 1, s_ * 2 + 1, C("a8f0c0"))
        c.set(x, y, WHITE)
    ring(c, cx, 26, 3 + t * 9, C("53c1a6"), 1.4)
    glow(c, cx, 20, 12, C("53c1a6"), int(60 * (1 - t * 0.5)))


def fx_arcane(c, t):
    cx, cy = 16, 16
    ring(c, cx, cy, 4 + t * 10, PURPLE, 1.6)
    ring(c, cx, cy, max(1, 10 - t * 8), PURPLE_L, 1.2)
    for i in range(6):
        ang = i * 60 + t * 120
        a = math.radians(ang)
        d = 6 + t * 7
        x, y = int(cx + math.cos(a) * d), int(cy + math.sin(a) * d)
        c.disc(x, y, 1, PURPLE_L)
        c.set(x, y, WHITE)
    c.disc(cx, cy, max(0, int(4 * (1 - t))), WHITE)
    glow(c, cx, cy, 12, PURPLE, int(120 * (1 - t * 0.6)))


def fx_shockwave(c, t):
    cx, cy = 16, 22
    for k in range(3):
        r = (t * 15) - k * 3
        if r > 0:
            ring(c, cx, cy, r, STEELY if k == 0 else SMOKE, 1.5 - k * 0.4)
    for i in range(6):
        x = int(cx + math.cos(math.radians(i * 60)) * t * 13)
        c.rect(x, cy - int(t * 4), 1, max(1, int(4 - t * 3)), SMOKE)


def fx_smoke(c, t):
    rr = _r(41)
    for i in range(8):
        ang = rr.uniform(0, 360)
        d = t * rr.uniform(2, 10)
        x = int(16 + math.cos(math.radians(ang)) * d)
        y = int(22 + math.sin(math.radians(ang)) * d - t * 10)
        rad = int((3 + t * 4) * rr.uniform(0.7, 1.1))
        col = shift(SMOKE, 0.15 - t * 0.3)
        c.ellipse(x, y, max(1, rad), max(1, rad - 1), col)


def fx_pierce(c, t):
    """An arrow or thrust landing: a spike of force plus a short spray."""
    cx, cy = 16, 16
    L = 6 + t * 12
    spark(c, cx, cy, 0, -L, L * 0.3, PALE, 2)
    spark(c, cx, cy, 0, -L * 0.6, L * 0.15, WHITE, 1)
    for i in (-32, 32, -55, 55):
        spark(c, cx, cy, i, 2, 3 + t * 9, STEELY, 1)
    c.disc(cx, cy, max(0, int(4 * (1 - t))), WHITE)
    glow(c, cx, cy, 7, PALE, int(120 * (1 - t)))


def fx_buff(c, t):
    cx, cy = 16, 24
    ring(c, cx, int(cy - t * 16), 9 - int(t * 3), GOLD, 1.5)
    for i in range(6):
        ph = (t + i / 6.0) % 1.0
        ang = i * 60 + t * 90
        a = math.radians(ang)
        d = 9 - ph * 3
        x, y = int(cx + math.cos(a) * d), int(cy - ph * 18 + math.sin(a) * 2)
        c.set(x, y, YELL)
        c.set(x, y - 1, GOLD)
    glow(c, cx, int(cy - t * 12), 11, GOLD, int(80 * (1 - t * 0.5)))


def fx_debuff(c, t):
    cx, cy = 16, 10
    ring(c, cx, int(cy + t * 14), 9 - int(t * 3), PURPLE, 1.5)
    for i in range(6):
        ph = (t + i / 6.0) % 1.0
        a = math.radians(i * 60 - t * 90)
        d = 9 - ph * 3
        x, y = int(cx + math.cos(a) * d), int(cy + ph * 18 + math.sin(a) * 2)
        c.set(x, y, PURPLE_L)
        c.set(x, y + 1, PURPLE)
    glow(c, cx, int(cy + t * 12), 11, PURPLE, int(80 * (1 - t * 0.5)))


def fx_stun(c, t):
    cx, cy = 16, 12
    for i in range(4):
        ang = i * 90 + t * 360
        a = math.radians(ang)
        x, y = int(cx + math.cos(a) * 9), int(cy + math.sin(a) * 4)
        c.disc(x, y, 2, C("f2d05a"))
        c.disc(x, y, 1, WHITE)
    ring(c, cx, cy, 9, C("f2d05a"), 1.0)


EFFECTS = [
    ("fx_slash", fx_slash, "A melee crescent. Pair with any bladed weapon."),
    ("fx_impact", fx_impact, "Generic blunt hit spark."),
    ("fx_pierce", fx_pierce, "Arrow or thrust landing."),
    ("fx_blood", fx_blood, "Damage splatter for organic enemies."),
    ("fx_explosion", fx_explosion, "Bomb or big finisher."),
    ("fx_fire", fx_fire, "Burning column; also a status loop."),
    ("fx_ice", fx_ice, "Frost burst; pairs with a slow."),
    ("fx_lightning", fx_lightning, "Chain-lightning strike from above."),
    ("fx_poison", fx_poison, "Rising toxic cloud."),
    ("fx_arcane", fx_arcane, "Neutral magic burst."),
    ("fx_heal", fx_heal, "Restorative motes rising."),
    ("fx_buff", fx_buff, "Rising ring for a positive status."),
    ("fx_debuff", fx_debuff, "Falling ring for a negative status."),
    ("fx_stun", fx_stun, "Orbiting stars over a stunned actor."),
    ("fx_shockwave", fx_shockwave, "Ground slam ripple."),
    ("fx_smoke", fx_smoke, "Dissipating puff; use for retreats."),
]


def render_fx(entry, frames=FRAMES):
    name, fn, blurb = entry
    out = []
    for i in range(frames):
        c = Canvas(S, S)
        fn(c, i / float(frames - 1))
        out.append(c)
    return out

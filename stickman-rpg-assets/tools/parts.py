"""
Reusable sprite parts: heads, helmets, torsos, weapons, extras.

The interesting one is `Shaft`. A weapon is described in weapon-local
coordinates - u = distance along the shaft from the grip, v = sideways -
and Shaft maps that onto the canvas at any angle. So ONE description of a
sword renders as the inventory icon (angle 180) and as every frame of a
swing (angle 100..250) with no extra art.
"""

import math
from pixelart import Canvas
from rig import (INK, LIGHT, DARK, SKIN, SKIN_DK, STEEL, STEEL_DK, LEATHER,
                 LEATHER_DK, WOOD, WOOD_DK, CLOTH_W, GOLD, BLOOD, POISON,
                 STUN_Y, BONE, BONE_DK, shift, polar, lerp)


# ---------------------------------------------------------------------------
#  Shaft - weapon-local coordinate system
# ---------------------------------------------------------------------------

class Shaft:
    def __init__(self, canvas, gx, gy, angle, scale=1.0):
        self.c = canvas
        self.gx, self.gy = float(gx), float(gy)
        self.angle = angle
        self.s = scale
        a = math.radians(angle)
        self.ax, self.ay = math.sin(a), math.cos(a)
        self.px, self.py = math.cos(a), -math.sin(a)

    def point(self, u, v=0.0):
        u *= self.s
        v *= self.s
        return (self.gx + self.ax * u + self.px * v,
                self.gy + self.ay * u + self.py * v)

    def _span(self, u, hw, color):
        p0 = self.point(u, -hw)
        p1 = self.point(u, hw)
        self.c.line(int(round(p0[0])), int(round(p0[1])),
                    int(round(p1[0])), int(round(p1[1])), color)

    def seg(self, u0, u1, hw, color):
        """Constant-width band from u0 to u1."""
        self.taper(u0, u1, hw, hw, color)

    def taper(self, u0, u1, hw0, hw1, color):
        """Band whose half-width goes hw0 -> hw1. Blades, points, horns."""
        steps = max(2, int(abs(u1 - u0) * self.s * 3) + 1)
        for i in range(steps + 1):
            t = i / steps
            self._span(lerp(u0, u1, t), max(0.0, lerp(hw0, hw1, t)), color)

    def dot(self, u, v, r, color):
        p = self.point(u, v)
        self.c.disc(int(round(p[0])), int(round(p[1])), max(0, int(round(r * self.s))), color)

    def quad(self, u0, u1, v0, v1, color):
        """Axis-aligned-in-weapon-space block (axe heads, boxes)."""
        steps = max(2, int(abs(u1 - u0) * self.s * 3) + 1)
        for i in range(steps + 1):
            u = lerp(u0, u1, i / steps)
            p0 = self.point(u, v0)
            p1 = self.point(u, v1)
            self.c.line(int(round(p0[0])), int(round(p0[1])),
                        int(round(p1[0])), int(round(p1[1])), color)

    def blob(self, u, v, rx, ry, color):
        p = self.point(u, v)
        self.c.ellipse(int(round(p[0])), int(round(p[1])),
                       max(1, int(round(rx * self.s))),
                       max(1, int(round(ry * self.s))), color)

    def profile(self, u0, u1, fv0, fv1, color):
        """Band whose two edges are functions of t in 0..1. Crescents, bits."""
        steps = max(2, int(abs(u1 - u0) * self.s * 4) + 1)
        for i in range(steps + 1):
            t = i / steps
            u = lerp(u0, u1, t)
            p0 = self.point(u, fv0(t))
            p1 = self.point(u, fv1(t))
            self.c.line(int(round(p0[0])), int(round(p0[1])),
                        int(round(p1[0])), int(round(p1[1])), color)

    def stroke(self, u0, v0, u1, v1, color, t=1):
        p0 = self.point(u0, v0)
        p1 = self.point(u1, v1)
        self.c.line(int(round(p0[0])), int(round(p0[1])),
                    int(round(p1[0])), int(round(p1[1])), color, t)


# ---------------------------------------------------------------------------
#  WEAPONS   grip at u=0, weapon extends toward +u
# ---------------------------------------------------------------------------

def _grip(s, u0, u1, hw, metal):
    s.seg(u0, u1, hw, LEATHER)
    s.dot(u0, 0, hw + 0.4, shift(metal, -0.2))


def w_sword(s, metal=STEEL, trim=GOLD):
    _grip(s, -3, 0, 1.0, metal)
    s.quad(0, 1.3, -2.6, 2.6, trim)              # crossguard
    s.taper(1.3, 11, 1.3, 1.0, metal)            # blade
    s.taper(11, 13.5, 1.0, 0.0, metal)           # point
    s.taper(2, 11.5, 0.4, 0.3, shift(metal, 0.4))  # fuller highlight


def w_greatsword(s, metal=STEEL, trim=GOLD):
    _grip(s, -5, 0, 1.2, metal)
    s.quad(0, 2.0, -4.0, 4.0, trim)
    s.taper(2.0, 16, 2.4, 1.8, metal)
    s.taper(16, 20, 1.8, 0.0, metal)
    s.taper(3, 17, 0.8, 0.6, shift(metal, 0.35))


def w_katana(s, metal=STEEL, trim=BLOOD):
    _grip(s, -4, 0, 1.0, metal)
    s.quad(0, 0.9, -2.2, 2.2, shift(metal, -0.4))
    s.taper(0.9, 14, 1.3, 1.0, metal)
    s.taper(14, 17, 1.0, 0.0, metal)
    s.taper(1.5, 15, 0.4, 0.3, shift(metal, 0.4))
    s.seg(-4, 0, 0.4, trim)


def w_dagger(s, metal=STEEL, trim=LEATHER):
    _grip(s, -2.5, 0, 0.9, metal)
    s.quad(0, 1.0, -2.0, 2.0, trim)
    s.taper(1.0, 6, 1.3, 0.9, metal)
    s.taper(6, 8.5, 0.9, 0.0, metal)


def w_axe(s, metal=STEEL, wood=WOOD):
    s.seg(-4, 12, 1.0, wood)
    s.profile(7.0, 14.0, lambda t: 0.6,
              lambda t: 2.0 + 4.0 * math.sin(math.pi * t) ** 0.65, metal)
    s.profile(8.0, 13.0, lambda t: 1.4,
              lambda t: 1.8 + 2.2 * math.sin(math.pi * t) ** 0.65,
              shift(metal, 0.3))
    s.profile(7.0, 14.0, lambda t: 1.6 + 4.0 * math.sin(math.pi * t) ** 0.65,
              lambda t: 2.0 + 4.0 * math.sin(math.pi * t) ** 0.65,
              shift(metal, -0.28))
    s.taper(12, 14.5, 1.6, 0.0, metal)


def w_greataxe(s, metal=STEEL, wood=WOOD_DK):
    s.seg(-5, 14, 1.3, wood)
    for sgn in (1, -1):
        s.profile(7.5, 15.5, lambda t: sgn * 0.7,
                  lambda t: sgn * (2.2 + 4.6 * math.sin(math.pi * t) ** 0.6), metal)
        s.profile(8.5, 14.5, lambda t: sgn * 1.6,
                  lambda t: sgn * (2.0 + 2.4 * math.sin(math.pi * t) ** 0.6),
                  shift(metal, 0.28))
    s.taper(15, 18, 1.3, 0.0, metal)


def w_mace(s, metal=STEEL, wood=LEATHER):
    s.seg(-3, 9, 1.0, wood)
    s.blob(11, 0, 3.0, 3.0, metal)
    for ang in (-90, -30, 30, 90, 150, 210):
        r = math.radians(ang)
        s.dot(11 + math.cos(r) * 3.4, math.sin(r) * 3.4, 0.9, shift(metal, -0.2))
    s.blob(10.2, -1.0, 1.2, 1.2, shift(metal, 0.35))


def w_hammer(s, metal=STEEL, wood=WOOD):
    s.seg(-4, 10, 1.1, wood)
    s.quad(9, 14, -4.2, 4.2, metal)
    s.quad(10, 13, -2.6, 2.6, shift(metal, 0.25))
    s.quad(9, 14, 3.4, 4.2, shift(metal, -0.3))


def w_spear(s, metal=STEEL, wood=WOOD):
    s.seg(-6, 14, 0.9, wood)
    s.quad(13.5, 14.5, -2.2, 2.2, shift(metal, -0.2))
    s.taper(14.5, 20, 1.8, 0.0, metal)
    s.taper(15, 19, 0.6, 0.0, shift(metal, 0.35))


def w_trident(s, metal=STEEL, wood=WOOD_DK):
    s.seg(-6, 13, 0.9, wood)
    s.quad(13, 14, -3.4, 3.4, metal)
    for v in (-3.0, 0.0, 3.0):
        s.taper(14, 19, 0.9, 0.0, metal)
        s.stroke(14, v, 18.5, v, metal, 1)
        s.dot(18.6, v, 0.4, metal)


def w_scythe(s, metal=STEEL, wood=WOOD_DK):
    s.seg(-6, 14, 1.0, wood)
    s.seg(12.5, 14.5, 1.7, shift(wood, -0.35))
    cu, cv, R = 13.0, -1.0, 10.5
    pts = []
    for i in range(15):
        a = math.radians(-6 + (i / 14.0) * 96)
        pts.append((cu - R * math.sin(a) * 0.55 + 1.0, cv - R * math.sin(a)))
    pts = [(cu + R * math.cos(math.radians(88 - (i / 14.0) * 82)) * 0.42,
            cv - R * math.sin(math.radians(88 - (i / 14.0) * 82)))
           for i in range(15)]
    for i in range(len(pts) - 1):
        w = 2 if i < 9 else 1
        s.stroke(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], metal, w)
    for i in range(len(pts) - 1):
        s.stroke(pts[i][0] + 0.9, pts[i][1], pts[i + 1][0] + 0.9,
                 pts[i + 1][1], shift(metal, 0.35), 1)
    s.dot(13, -1, 1.2, shift(metal, -0.3))


def w_staff(s, wood=WOOD, gem=STUN_Y):
    s.seg(-6, 14, 1.0, wood)
    s.seg(12, 14.5, 1.6, shift(wood, -0.3))
    s.blob(16.5, 0, 2.6, 2.6, gem)
    s.blob(16.0, -0.6, 1.2, 1.2, shift(gem, 0.5))


def w_wand(s, wood=WOOD_DK, gem=STUN_Y):
    s.seg(-2, 7, 0.8, wood)
    s.blob(8.6, 0, 1.8, 1.8, gem)
    s.dot(8.2, -0.5, 0.5, shift(gem, 0.55))


def w_club(s, wood=WOOD_DK, spikes=False):
    s.taper(-3, 13.5, 1.2, 3.2, shift(wood, -0.4))    # dark silhouette
    s.dot(13.5, 0, 2.8, shift(wood, -0.4))
    s.taper(-3, 13, 0.8, 2.5, wood)                   # body
    s.dot(13, -0.2, 2.2, wood)
    s.taper(-1, 12, 0.4, 1.1, shift(wood, 0.28))      # lit side
    for u, v in ((7.5, 1.6), (10.0, -1.8), (12.5, 1.2)):
        s.dot(u, v, 0, BONE)                          # studs, 1px each
    if spikes:
        for u, v in ((7.0, 2.6), (10.0, -2.9), (12.8, 2.4), (14.0, -1.0)):
            sgn = 1 if v > 0 else -1
            s.stroke(u, v, u + 0.6, v + sgn * 2.6, BONE, 1)
            s.dot(u + 0.8, v + sgn * 3.0, 0, BONE)


def w_flail(s, metal=STEEL, wood=LEATHER):
    s.seg(-3, 7, 1.0, wood)
    s.stroke(7, 0, 11, 3, shift(metal, -0.35), 1)
    s.blob(12.5, 4.4, 2.6, 2.6, metal)
    for ang in (0, 72, 144, 216, 288):
        r = math.radians(ang)
        s.dot(12.5 + math.cos(r) * 3.0, 4.4 + math.sin(r) * 3.0, 0.7, shift(metal, -0.2))


def w_bow(s, wood=WOOD, string=CLOTH_W):
    prev = None
    for i in range(15):
        t = i / 14.0
        v = lerp(-9.0, 9.0, t)
        u = 6.0 - (v * v) / 11.0
        if prev:
            s.stroke(prev[0], prev[1], u, v, wood, 2)
        prev = (u, v)
    s.stroke(-1.4, -9.0, -1.4, 9.0, string, 1)
    s.stroke(-1.4, 0, 2.0, 0, string, 1)


def w_crossbow(s, wood=WOOD_DK, metal=STEEL):
    s.seg(-3, 9, 1.2, wood)
    s.quad(7, 9, -7.0, 7.0, metal)
    s.stroke(7.5, -7.0, 4.0, 0, CLOTH_W, 1)
    s.stroke(7.5, 7.0, 4.0, 0, CLOTH_W, 1)
    s.taper(4, 12, 0.8, 0.0, shift(metal, 0.2))


def w_claw(s, metal=BONE, glove=None):
    """Talons at the hand. Tiny on purpose - at 32x40 a detailed gauntlet
    just merges into a white mitten once the outline pass runs."""
    if glove:
        s.seg(-1.0, 0.8, 1.4, glove)
    for v in (-1.7, 0.0, 1.7):
        s.stroke(0.4, v * 0.5, 3.0, v, metal, 1)
        s.dot(3.6, v * 1.2, 0, metal)


def w_whip(s, leather=LEATHER):
    _grip(s, -3, 1, 1.0, STEEL_DK)
    prev = (1, 0)
    for i in range(1, 13):
        t = i / 12.0
        u = 1 + t * 13
        v = math.sin(t * 5.0) * (3.0 + t * 3.0)
        s.stroke(prev[0], prev[1], u, v, leather, 2 if t < 0.6 else 1)
        prev = (u, v)


def w_tome(s, cover=BLOOD, page=CLOTH_W):
    s.quad(-1, 7, -5.0, 5.0, cover)
    s.quad(0, 6, -4.0, 4.0, page)
    s.quad(-1, 7, -0.7, 0.7, shift(cover, -0.35))
    s.quad(1.5, 4.5, -3.0, -1.2, shift(page, -0.12))
    s.quad(1.5, 4.5, 1.2, 3.0, shift(page, -0.12))


def w_orb(s, glow=STUN_Y):
    s.blob(3, 0, 3.4, 3.4, glow)
    s.blob(2.4, -0.8, 1.5, 1.5, shift(glow, 0.5))
    s.dot(1.8, -1.4, 0.5, (255, 255, 255, 255))


def w_torch(s, wood=WOOD_DK):
    s.seg(-3, 6, 1.1, wood)
    s.seg(5, 7, 1.7, LEATHER)
    s.blob(9.5, 0, 2.2, 3.0, (196, 69, 58, 255))
    s.blob(9.0, 0, 1.4, 2.0, (224, 138, 60, 255))
    s.blob(8.6, 0, 0.7, 1.1, (250, 226, 150, 255))


def w_shield_r(s, face=WOOD, boss=STEEL):
    s.blob(1.0, 0, 4.0, 5.2, face)
    s.blob(1.0, 0, 2.4, 3.4, shift(face, -0.25))
    s.blob(1.0, 0, 1.3, 1.6, boss)


WEAPONS = {
    "sword": w_sword, "greatsword": w_greatsword, "katana": w_katana,
    "dagger": w_dagger, "axe": w_axe, "greataxe": w_greataxe,
    "mace": w_mace, "hammer": w_hammer, "spear": w_spear,
    "trident": w_trident, "scythe": w_scythe, "staff": w_staff,
    "wand": w_wand, "club": w_club, "flail": w_flail, "bow": w_bow,
    "crossbow": w_crossbow, "claw": w_claw, "whip": w_whip,
    "tome": w_tome, "orb": w_orb, "torch": w_torch, "shield": w_shield_r,
}

# how far the weapon naturally rests from vertical, per archetype
WEAPON_REST = {
    "bow": 90, "crossbow": 60, "tome": 40, "orb": 40, "claw": 60,
    "whip": 120, "shield": 90,
}


def draw_weapon(canvas, kind, gx, gy, angle, scale=1.0, **kw):
    if kind not in WEAPONS:
        return
    WEAPONS[kind](Shaft(canvas, gx, gy, angle, scale), **kw)


# ---------------------------------------------------------------------------
#  HEADS
# ---------------------------------------------------------------------------

def _eyes(c, cx, cy, r, color, style="dots", spread=None):
    sp = spread if spread is not None else max(2, r - 2)
    ey = cy + max(1, r // 3)
    if style == "dots":
        c.rect(cx - sp, ey, 1, 2, color)
        c.rect(cx + sp, ey, 1, 2, color)
    elif style == "wide":
        c.rect(cx - sp - 1, ey, 2, 2, color)
        c.rect(cx + sp - 1, ey, 2, 2, color)
    elif style == "glow":
        c.rect(cx - sp - 1, ey, 2, 2, color)
        c.rect(cx + sp - 1, ey, 2, 2, color)
        c.blend(cx - sp - 1, ey - 1, (color[0], color[1], color[2], 110))
        c.blend(cx + sp, ey - 1, (color[0], color[1], color[2], 110))
    elif style == "slit":
        c.rect(cx - sp - 1, ey, 3, 1, color)
        c.rect(cx + sp - 1, ey, 3, 1, color)
    elif style == "single":
        c.disc(cx, ey - 1, max(1, r // 2), color)


def h_human(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    c.rect(cx - r, cy - r, r * 2 + 1, max(1, r - 2), shift(b.skin, -0.3))  # hair
    _eyes(c, cx, cy, r, b.eyes or INK)


def h_goblin(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    for sgn in (-1, 1):                                     # pointed ears
        c.line(cx + sgn * r, cy - 1, cx + sgn * (r + 3), cy - 4, b.skin, 2)
    c.rect(cx - 1, cy + 1, 2, 3, shift(b.skin, -0.28))      # nose
    _eyes(c, cx, cy, r, b.eyes or INK, "wide")
    c.rect(cx - 2, cy + r - 1, 5, 1, shift(b.skin, -0.45))  # grin


def h_skull(c, cx, cy, r, b):
    c.disc(cx, cy, r, BONE)
    c.rect(cx - r + 1, cy + r - 2, r * 2 - 1, 3, BONE)      # jaw
    _eyes(c, cx, cy, r, b.eyes or INK, "wide")
    for i in range(-2, 3):
        c.set(cx + i, cy + r, BONE_DK)
    c.rect(cx, cy + 1, 1, 2, INK)                           # nose hole


def h_beast(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    c.ellipse(cx + r - 1, cy + 2, r - 1, max(2, r - 3), shift(b.skin, -0.15))  # muzzle
    c.set(cx + r + 1, cy + 1, INK)                          # nose
    for sgn in (-1, 1):                                     # ears
        c.line(cx + sgn * (r - 2), cy - r + 1, cx + sgn * (r - 1), cy - r - 3, b.skin, 2)
    _eyes(c, cx - 1, cy, r, b.eyes or INK, "slit")


def h_orb(c, cx, cy, r, b):
    col = b.accent
    c.disc(cx, cy, r, col)
    c.disc(cx - 1, cy - 1, max(1, r - 2), shift(col, 0.4))
    c.disc(cx - 1, cy - 1, max(1, r - 4), (255, 255, 255, 235))


def h_slime(c, cx, cy, r, b):
    c.ellipse(cx, cy + 1, r + 1, r - 1, b.skin)
    c.ellipse(cx - 1, cy - 1, max(1, r - 3), max(1, r - 4), shift(b.skin, 0.45))
    _eyes(c, cx, cy, r, b.eyes or INK, "wide")


def h_eye(c, cx, cy, r, b):
    c.disc(cx, cy, r, CLOTH_W)
    c.disc(cx, cy, max(1, r - 2), b.accent)
    c.disc(cx, cy, max(1, r - 4), INK)
    c.set(cx - 1, cy - 1, (255, 255, 255, 255))


def h_insect(c, cx, cy, r, b):
    c.ellipse(cx, cy, r, r - 1, b.skin)
    c.disc(cx - r + 1, cy - 1, max(1, r - 3), shift(b.skin, -0.4))   # compound eyes
    c.disc(cx + r - 1, cy - 1, max(1, r - 3), shift(b.skin, -0.4))
    for sgn in (-1, 1):                                              # antennae
        c.line(cx + sgn * 2, cy - r, cx + sgn * 4, cy - r - 4, shift(b.skin, -0.3), 1)
    c.rect(cx - 2, cy + r - 1, 5, 2, shift(b.skin, -0.5))            # mandibles


def h_undead(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    c.rect(cx - r, cy - r, r * 2 + 1, max(1, r - 3), shift(b.skin, -0.4))
    _eyes(c, cx, cy, r, b.eyes or POISON, "glow")
    c.rect(cx - 2, cy + r - 1, 5, 1, shift(b.skin, -0.5))
    c.set(cx + r - 1, cy - 1, shift(b.skin, -0.45))


def h_demon(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    for sgn in (-1, 1):                                     # horns
        c.line(cx + sgn * (r - 1), cy - r + 1, cx + sgn * (r + 1), cy - r - 4, BONE, 2)
        c.set(cx + sgn * (r + 1), cy - r - 5, BONE)
    _eyes(c, cx, cy, r, b.eyes or BLOOD, "glow")
    c.rect(cx - 2, cy + r - 1, 5, 1, shift(b.skin, -0.5))


def h_bird(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    c.line(cx + r, cy + 1, cx + r + 4, cy + 2, GOLD, 2)     # beak
    c.line(cx - 2, cy - r, cx - 4, cy - r - 3, b.accent, 2)  # crest
    c.line(cx, cy - r, cx - 2, cy - r - 4, b.accent, 2)
    _eyes(c, cx, cy, r, b.eyes or INK, "dots", spread=2)


def h_plant(c, cx, cy, r, b):
    c.disc(cx, cy, r, b.skin)
    for ang in (-140, -110, -70, -40):                      # petals / leaves
        p = polar(cx, cy, ang, r + 3)
        c.ellipse(int(p[0]), int(p[1]), 2, 3, b.accent)
    _eyes(c, cx, cy, r, b.eyes or INK, "dots")


def h_construct(c, cx, cy, r, b):
    c.rect(cx - r, cy - r, r * 2, r * 2, b.skin)
    c.rect(cx - r, cy - r, r * 2, 2, shift(b.skin, 0.25))
    c.rect(cx - r + 1, cy, r * 2 - 2, 2, INK)
    c.rect(cx - r + 2, cy, 2, 2, b.eyes or STUN_Y)
    c.rect(cx + r - 4, cy, 2, 2, b.eyes or STUN_Y)


HEADS = {"human": h_human, "goblin": h_goblin, "skull": h_skull,
         "beast": h_beast, "orb": h_orb, "slime": h_slime, "eye": h_eye,
         "insect": h_insect, "undead": h_undead, "demon": h_demon,
         "bird": h_bird, "plant": h_plant, "construct": h_construct}


def draw_head(c, kind, cx, cy, r, b):
    HEADS.get(kind, h_human)(c, int(cx), int(cy), int(r), b)


# ---------------------------------------------------------------------------
#  HELMETS / HEADGEAR   drawn after the head
# ---------------------------------------------------------------------------

def _dome(c, cx, cy, r, col):
    c.disc(cx, cy - 1, r, col)
    c.rect(cx - r, cy, r * 2 + 1, r + 2, (0, 0, 0, 0))   # clear below (no-op on alpha)


def hm_dome(c, cx, cy, r, b, col):
    for j in range(cy - r - 1, cy + 1):
        for i in range(cx - r, cx + r + 1):
            if (i - cx) ** 2 + (j - (cy - 1)) ** 2 <= r * r + r:
                c.set(i, j, col)
    c.rect(cx - r, cy - 2, r * 2 + 1, 2, shift(col, -0.28))
    c.rect(cx - 1, cy - 1, 1, r - 1, shift(col, -0.28))  # nose guard


def hm_horned(c, cx, cy, r, b, col):
    hm_dome(c, cx, cy, r, b, col)
    for sgn in (-1, 1):
        c.line(cx + sgn * r, cy - 2, cx + sgn * (r + 3), cy - 6, BONE, 2)
        c.set(cx + sgn * (r + 3), cy - 7, BONE)


def hm_hood(c, cx, cy, r, b, col):
    """A shell around the head, not a blob over it - the face stays visible."""
    R = r + 1
    inner = (r - 1) * (r - 1)
    for j in range(cy - R - 1, cy + R + 2):
        for i in range(cx - R - 1, cx + R + 2):
            d2 = (i - cx) ** 2 + (j - cy) ** 2
            if d2 > R * R + R:
                continue
            if j < cy - 1 or d2 > inner:
                c.set(i, j, col)
    for j in range(cy - 1, cy + 2):          # shadow under the brow
        for i in range(cx - r + 1, cx + r):
            if (i - cx) ** 2 + (j - cy) ** 2 <= inner and j == cy - 1:
                c.set(i, j, shift(col, -0.45))
    c.rect(cx - R, cy + r - 3, 2, 5, col)    # drape
    c.rect(cx + R - 1, cy + r - 3, 2, 4, col)


def hm_crown(c, cx, cy, r, b, col):
    c.rect(cx - r, cy - r - 1, r * 2 + 1, 2, col)
    for i, dx in enumerate(range(-r, r + 1, max(1, r))):
        c.rect(cx + dx, cy - r - 4, 1, 3, col)
        c.set(cx + dx, cy - r - 5, BLOOD if i % 2 == 0 else col)


def hm_wizard(c, cx, cy, r, b, col):
    for i in range(7):
        w = max(0, int((r + 1) * (1 - i / 7.0)))
        c.rect(cx - w - 1, cy - r - i, w * 2 + 2, 1, col)
    c.rect(cx - r - 2, cy - r - 1, (r + 2) * 2 + 1, 2, col)        # brim
    c.rect(cx - r - 2, cy - r + 1, (r + 2) * 2 + 1, 1, shift(col, -0.35))
    c.rect(cx - r + 1, cy - r - 3, (r - 1) * 2, 1, b.accent)       # band
    c.set(cx - 1, cy - r - 7, b.accent)


def hm_plume(c, cx, cy, r, b, col):
    hm_dome(c, cx, cy, r, b, col)
    for i in range(5):
        c.rect(cx - 1 + i // 2, cy - r - 2 - i, 2, 2, b.accent)


def hm_visor(c, cx, cy, r, b, col):
    for j in range(cy - r - 1, cy + r):
        for i in range(cx - r, cx + r + 1):
            if (i - cx) ** 2 + (j - cy) ** 2 <= r * r + r:
                c.set(i, j, col)
    c.rect(cx - r + 1, cy, r * 2 - 1, 2, INK)
    c.rect(cx - r + 2, cy, 2, 2, b.eyes or STUN_Y)
    c.rect(cx + r - 3, cy, 2, 2, b.eyes or STUN_Y)


def hm_band(c, cx, cy, r, b, col):
    c.rect(cx - r, cy - 2, r * 2 + 1, 2, col)
    c.line(cx - r, cy - 1, cx - r - 3, cy + 3, col, 1)


def hm_antlers(c, cx, cy, r, b, col):
    for sgn in (-1, 1):
        c.line(cx + sgn * 2, cy - r, cx + sgn * 4, cy - r - 6, col, 1)
        c.line(cx + sgn * 3, cy - r - 3, cx + sgn * 6, cy - r - 4, col, 1)
        c.line(cx + sgn * 4, cy - r - 5, cx + sgn * 6, cy - r - 8, col, 1)


def hm_skullcap(c, cx, cy, r, b, col):
    for j in range(cy - r - 1, cy - 1):
        for i in range(cx - r, cx + r + 1):
            if (i - cx) ** 2 + (j - cy) ** 2 <= r * r + r:
                c.set(i, j, col)


HELMETS = {"dome": hm_dome, "horned": hm_horned, "hood": hm_hood,
           "crown": hm_crown, "wizard": hm_wizard, "plume": hm_plume,
           "visor": hm_visor, "band": hm_band, "antlers": hm_antlers,
           "skullcap": hm_skullcap}


def draw_helmet(c, kind, cx, cy, r, b, col=None):
    if kind in HELMETS:
        HELMETS[kind](c, int(cx), int(cy), int(r), b, col or STEEL)


# ---------------------------------------------------------------------------
#  TORSOS
# ---------------------------------------------------------------------------

def _band(c, tl, tr, bl, br, color):
    """Fill the quad (top-left, top-right, bottom-left, bottom-right)."""
    y0, y1 = int(round(min(tl[1], tr[1]))), int(round(max(bl[1], br[1])))
    if y1 <= y0:
        y1 = y0 + 1
    for y in range(y0, y1 + 1):
        t = (y - y0) / max(1, (y1 - y0))
        xl = lerp(tl[0], bl[0], t)
        xr = lerp(tr[0], br[0], t)
        c.line(int(round(xl)), y, int(round(xr)), y, color)


def torso_shape(c, j, rig, color):
    half = rig.torso_w / 2.0
    sh, hip = j["sh"], j["hip"]
    tl = (sh[0] - half, sh[1] - 1)
    tr = (sh[0] + half, sh[1] - 1)
    bl = (hip[0] - half * 0.82, hip[1] + 1)
    br = (hip[0] + half * 0.82, hip[1] + 1)
    _band(c, tl, tr, bl, br, color)
    return tl, tr, bl, br


def t_tunic(c, j, rig, b):
    tl, tr, bl, br = torso_shape(c, j, rig, b.cloth)
    _band(c, tl, tr, (tl[0], tl[1] + 3), (tr[0], tr[1] + 3), b.cloth_dk)
    y = int(round(lerp(tl[1], bl[1], 0.72)))
    c.line(int(tl[0]) - 1, y, int(tr[0]) + 1, y, LEATHER)
    c.rect(int(j["hip"][0]) - 1, y, 2, 2, b.accent)


def t_plate(c, j, rig, b):
    tl, tr, bl, br = torso_shape(c, j, rig, STEEL)
    _band(c, tl, tr, (tl[0] - 1, tl[1] + 3), (tr[0] + 1, tr[1] + 3), STEEL_DK)
    for k in (0.35, 0.55, 0.75):
        y = int(round(lerp(tl[1], bl[1], k)))
        c.line(int(tl[0]) + 1, y, int(tr[0]) - 1, y, STEEL_DK)
    c.rect(int(j["sh"][0]) - 1, int(tl[1]) + 4, 2, 3, b.cloth)


def t_robe(c, j, rig, b):
    half = rig.torso_w / 2.0
    sh, hip = j["sh"], j["hip"]
    tl = (sh[0] - half * 0.9, sh[1] - 1)
    tr = (sh[0] + half * 0.9, sh[1] - 1)
    bl = (hip[0] - half * 1.7, hip[1] + 6)
    br = (hip[0] + half * 1.7, hip[1] + 6)
    _band(c, tl, tr, bl, br, b.cloth)
    _band(c, (tl[0] + 1, tl[1] + 2), (tr[0] - 1, tr[1] + 2),
          (bl[0] + 3, bl[1]), (br[0] - 3, br[1]), b.cloth_dk)
    c.line(int(sh[0]), int(sh[1]), int(hip[0]), int(hip[1]) + 5, b.accent)


def t_bare(c, j, rig, b):
    torso_shape(c, j, rig, b.skin)
    sh, hip = j["sh"], j["hip"]
    mid = int(round(lerp(sh[1], hip[1], 0.5)))
    c.line(int(sh[0]) - 2, mid, int(sh[0]) + 2, mid, shift(b.skin, -0.25))
    c.line(int(sh[0]), int(sh[1]) + 2, int(sh[0]), mid, shift(b.skin, -0.18))


def t_ribs(c, j, rig, b):
    half = rig.torso_w / 2.0
    sh, hip = j["sh"], j["hip"]
    c.line(int(sh[0]), int(sh[1]), int(hip[0]), int(hip[1]), BONE, 2)
    n = 4
    for i in range(n):
        t = 0.12 + i * 0.22
        y = int(round(lerp(sh[1], hip[1], t)))
        x = int(round(lerp(sh[0], hip[0], t)))
        w = int(half * (1.0 - i * 0.14))
        c.line(x - w, y, x + w, y, BONE)
        c.set(x - w - 1, y + 1, BONE_DK)
        c.set(x + w + 1, y + 1, BONE_DK)


def t_carapace(c, j, rig, b):
    tl, tr, bl, br = torso_shape(c, j, rig, b.cloth)
    for k in (0.25, 0.5, 0.75):
        y = int(round(lerp(tl[1], bl[1], k)))
        c.line(int(tl[0]), y, int(tr[0]), y, shift(b.cloth, -0.4))
    _band(c, tl, tr, (tl[0], tl[1] + 2), (tr[0], tr[1] + 2), shift(b.cloth, 0.25))


def t_fur(c, j, rig, b):
    tl, tr, bl, br = torso_shape(c, j, rig, b.cloth)
    for y in range(int(tl[1]), int(bl[1]), 2):
        c.set(int(tl[0]) - 1, y, b.cloth)
        c.set(int(tr[0]) + 1, y + 1, b.cloth)
    _band(c, (tl[0] + 1, tl[1] + 2), (tr[0] - 1, tr[1] + 2),
          (bl[0] + 1, bl[1] - 2), (br[0] - 1, br[1] - 2), shift(b.cloth, 0.22))


def t_scales(c, j, rig, b):
    tl, tr, bl, br = torso_shape(c, j, rig, b.cloth)
    for y in range(int(tl[1]) + 1, int(bl[1]), 2):
        off = (y % 4) // 2
        for x in range(int(tl[0]) + off, int(tr[0]), 2):
            c.set(x, y, shift(b.cloth, -0.3))


TORSOS = {"tunic": t_tunic, "plate": t_plate, "robe": t_robe, "bare": t_bare,
          "ribs": t_ribs, "carapace": t_carapace, "fur": t_fur,
          "scales": t_scales}


def draw_torso(c, kind, j, rig, b):
    TORSOS.get(kind, t_tunic)(c, j, rig, b)


# ---------------------------------------------------------------------------
#  EXTRAS   wings, tails, auras - drawn behind or in front of the body
# ---------------------------------------------------------------------------

def x_wings(c, j, rig, b, back=True):
    if not back:
        return
    sh = j["sh"]
    col = b.accent if b.accent else shift(b.cloth, -0.15)
    edge = shift(col, -0.35)
    for sgn in (-1, 1):
        ax, ay = sh[0] + sgn * 2, sh[1] - 1
        for i in range(5):
            span = 10 - i * 1.4
            ang = sgn * (118 + i * 15)
            px_, py_ = polar(ax, ay + i * 1.3, ang, span)
            c.line(int(ax), int(ay + i * 1.3), int(px_), int(py_),
                   col if i % 2 == 0 else edge, 2)
        c.line(int(ax), int(ay), int(ax + sgn * 9), int(ay - 6), edge, 2)


def x_batwings(c, j, rig, b, back=True):
    if not back:
        return
    sh = j["sh"]
    col = shift(b.cloth, -0.22)
    edge = shift(col, -0.4)
    for sgn in (-1, 1):
        for k in range(1, 12):
            t = k / 11.0
            x = int(round(sh[0] + sgn * (2 + t * 9)))
            top = int(round(sh[1] - 7 + t * t * 5))
            bot = int(round(sh[1] + 3 - t * 4))
            if bot < top:
                bot = top
            c.line(x, top, x, bot, col)
            c.set(x, top, edge)
        for i in range(3):                      # finger struts
            ang = sgn * (104 + i * 20)
            px_, py_ = polar(sh[0] + sgn * 2, sh[1] - 1, ang, 9 - i)
            c.line(int(sh[0] + sgn * 2), int(sh[1] - 1),
                   int(px_), int(py_), edge, 1)


def x_tail(c, j, rig, b, back=True):
    if not back:
        return
    hip = j["hip"]
    col = shift(b.cloth if b.cloth else b.skin, -0.1)
    prev = (hip[0] - 2, hip[1])
    for i in range(1, 7):
        t = i / 6.0
        p = (hip[0] - 2 - t * 9, hip[1] - math.sin(t * 3.0) * 5 + t * 2)
        c.line(int(prev[0]), int(prev[1]), int(p[0]), int(p[1]), col, max(1, 3 - i // 3))
        prev = p


def x_aura(c, j, rig, b, back=True):
    if not back:
        return
    col = b.accent
    cx, cy = j["sh"][0], (j["sh"][1] + j["hip"][1]) / 2
    for i in range(9):
        a = i * 40 + 10
        p = polar(cx, cy, a, 12)
        c.blend(int(p[0]), int(p[1]), (col[0], col[1], col[2], 100))
        p2 = polar(cx, cy, a + 18, 9)
        c.blend(int(p2[0]), int(p2[1]), (col[0], col[1], col[2], 70))


def x_spikes(c, j, rig, b, back=True):
    if back:
        return
    sh, hip = j["sh"], j["hip"]
    for i in range(4):
        t = i / 3.0
        x = int(round(lerp(sh[0], hip[0], t))) - rig.torso_w // 2 - 1
        y = int(round(lerp(sh[1], hip[1], t)))
        c.line(x, y, x - 3, y - 1, BONE, 1)


def x_shoulderpads(c, j, rig, b, back=True):
    if back:
        return
    for key, sgn in (("sh_b", -1), ("sh_f", 1)):
        p = j[key]
        c.ellipse(int(p[0]), int(p[1]), 3, 2, STEEL)
        c.ellipse(int(p[0]), int(p[1] - 1), 2, 1, shift(STEEL, 0.3))


def x_flame(c, j, rig, b, back=True):
    if not back:
        return
    sh = j["sh"]
    for i in range(6):
        t = i / 5.0
        y = int(sh[1] - 4 - i * 2)
        w = int(4 - t * 3)
        c.rect(int(sh[0]) - w, y, w * 2, 2, (196, 69, 58, 255))
        c.rect(int(sh[0]) - max(1, w - 1), y, max(2, (w - 1) * 2), 2, (224, 138, 60, 255))


def x_cloak(c, j, rig, b, back=True):
    if not back:
        return
    sh, hip = j["sh"], j["hip"]
    col = b.cape or shift(b.cloth, -0.3)
    _band(c, (sh[0] - rig.torso_w / 2 - 1, sh[1] - 1),
          (sh[0] + rig.torso_w / 2 + 1, sh[1] - 1),
          (hip[0] - rig.torso_w / 2 - 3, hip[1] + 7),
          (hip[0] + rig.torso_w / 2 + 1, hip[1] + 5), col)


EXTRAS = {"wings": x_wings, "batwings": x_batwings, "tail": x_tail,
          "aura": x_aura, "spikes": x_spikes, "shoulderpads": x_shoulderpads,
          "flame": x_flame, "cloak": x_cloak}


def draw_extras(c, names, j, rig, b, back=True):
    for n in names:
        if n in EXTRAS:
            EXTRAS[n](c, j, rig, b, back)

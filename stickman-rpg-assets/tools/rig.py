"""
Parametric stick-figure rig.

The whole point: a character is a SKELETON (joint angles) plus a LOOK
(colours + part choices). Animation is then just interpolating angles, so
5 animation cycles x 35 characters costs no more authoring than one pose.

Angle convention: degrees, 0 = straight down, positive = clockwise on
screen (toward +x). So 90 = right, -90 = left, 180 = up.
"""

import math
from dataclasses import dataclass, field, replace

from pixelart import Canvas, hex_rgba as C

# ---------------------------------------------------------------------------
#  Shared palette - extends the game's existing one so new art matches old
# ---------------------------------------------------------------------------

INK        = C("14131c")
LIGHT      = C("ffffff", 60)
DARK       = C("000000", 70)

SKIN       = C("e8b796")
SKIN_DK    = C("c08a69")
STEEL      = C("b9c4d4")
STEEL_DK   = C("7d8798")
LEATHER    = C("6b4a32")
LEATHER_DK = C("4a3222")
WOOD       = C("8a5f38")
WOOD_DK    = C("5e3f24")
CLOTH_W    = C("e8e4dc")
GOLD       = C("e0c46c")
BLOOD      = C("c4453a")
POISON     = C("8ad148")
STUN_Y     = C("f2d05a")
BONE       = C("ded6c0")
BONE_DK    = C("a99f86")


def shift(color, amount):
    """Lighten (amount>0) or darken (amount<0) an rgba tuple. amount in -1..1."""
    r, g, b, a = color
    if amount >= 0:
        f = amount
        return (int(r + (255 - r) * f), int(g + (255 - g) * f),
                int(b + (255 - b) * f), a)
    f = 1 + amount
    return (int(r * f), int(g * f), int(b * f), a)


def polar(x, y, angle_deg, length):
    a = math.radians(angle_deg)
    return (x + math.sin(a) * length, y + math.cos(a) * length)


def lerp(a, b, t):
    return a + (b - a) * t


# ---------------------------------------------------------------------------
#  Pose - one frame of a skeleton
# ---------------------------------------------------------------------------

@dataclass
class Pose:
    dx: float = 0.0          # whole-body offset
    dy: float = 0.0
    lean: float = 0.0        # torso tilt, degrees
    head_dx: float = 0.0
    head_dy: float = 0.0
    head_tilt: float = 0.0

    # (upper, lower) absolute angles
    arm_b: tuple = (-18.0, -26.0)   # back arm  (screen left)
    arm_f: tuple = (18.0, 26.0)     # front arm (screen right, holds weapon)
    leg_b: tuple = (-8.0, -4.0)
    leg_f: tuple = (10.0, 6.0)

    weapon: float = 0.0      # weapon angle offset from the hand
    weapon_dx: float = 0.0
    weapon_dy: float = 0.0
    prone: float = 0.0       # 0..1, topples the skeleton flat onto the ground
    squash: float = 0.0      # 0..1, collapses the figure toward the ground
    fade: float = 0.0        # 0..1, desaturate + darken (death)

    def blend(self, other, t):
        """Interpolate every numeric field toward `other`."""
        out = {}
        for k, v in self.__dict__.items():
            o = getattr(other, k)
            if isinstance(v, tuple):
                out[k] = tuple(lerp(a, b, t) for a, b in zip(v, o))
            else:
                out[k] = lerp(v, o, t)
        return Pose(**out)


REST = Pose()

# A weapon drawn at icon size sticks out of a 32x40 cell mid-swing, so the
# held version is scaled down - more for the long two-handers.
INHAND = {
    "greatsword": 0.58, "greataxe": 0.56, "scythe": 0.50, "spear": 0.60,
    "trident": 0.60, "staff": 0.82, "bow": 0.80, "crossbow": 0.78,
    "whip": 0.60, "hammer": 0.72, "axe": 0.72, "flail": 0.68,
    "club": 0.76, "sword": 0.80, "katana": 0.78, "dagger": 0.95,
    "wand": 1.0, "tome": 0.85, "orb": 1.0, "torch": 0.85, "mace": 0.78,
    "claw": 0.8, "shield": 0.82,
}


# ---------------------------------------------------------------------------
#  Body - the "look" half of a character
# ---------------------------------------------------------------------------

@dataclass
class Body:
    skin: tuple = SKIN
    cloth: tuple = C("4d8fd6")
    cloth_dk: tuple = None
    accent: tuple = GOLD
    limb: tuple = None            # limb colour (defaults to skin)

    head: str = "human"           # human|goblin|skull|beast|orb|horned|slime|eye
    helmet: str = None            # dome|horned|hood|crown|wizard|plume|visor|band
    torso: str = "tunic"          # tunic|plate|robe|bare|ribs|carapace
    cape: tuple = None
    weapon: str = None            # archetype key -> parts.draw_weapon
    offhand: str = None           # shield|buckler|dagger|tome|orb|torch|lantern
    extras: tuple = ()            # wings|tail|aura|horns|spikes|shoulderpads|flame

    scale: float = 1.0            # 1.0 = normal; 0.75 tiny, 1.25 big
    weapon_scale: float = None    # in-hand size; auto from archetype
    build: float = 1.0            # limb / torso thickness
    eyes: tuple = None            # eye colour; None = INK

    def __post_init__(self):
        if self.cloth_dk is None:
            self.cloth_dk = shift(self.cloth, -0.35)
        if self.limb is None:
            self.limb = self.skin
        if self.weapon_scale is None:
            self.weapon_scale = INHAND.get(self.weapon, 0.78)


# ---------------------------------------------------------------------------
#  Rig - resolves a Body + Pose into joint positions, then draws
# ---------------------------------------------------------------------------

W, H = 32, 40
GROUND = 38


class Rig:
    def __init__(self, body, pad=0):
        self.b = body
        self.pad = pad
        s = body.scale
        self.head_r = max(3, round(4.6 * s))
        self.thigh = 6.0 * s
        self.shin = 6.2 * s
        self.upper_arm = 5.0 * s
        self.fore_arm = 5.2 * s
        self.torso_h = 10.0 * s
        self.torso_w = max(5, round(8 * s * body.build))
        self.limb_t = max(2, round(2 * s * body.build))
        self.base_x = (15.0 if body.offhand else 13.5) + pad
        self.foot_y = GROUND + pad - 1
        self.hip_y = self.foot_y - (self.thigh + self.shin)
        self.sh_y = self.hip_y - self.torso_h + 2
        self.head_y = self.sh_y - self.head_r - 1

    def joints(self, pose):
        """Resolve a pose into concrete pixel positions."""
        b = self.b
        cx = self.base_x + pose.dx
        sq = pose.squash
        # squash pulls everything toward the ground and tips it over
        ground = GROUND + self.pad

        def y(v):
            return v + pose.dy + (ground - v) * sq * 0.85

        hip = (cx, y(self.hip_y))
        lean_r = math.radians(pose.lean)
        sh = (cx + math.sin(lean_r) * self.torso_h * 0.8,
              y(self.sh_y) - math.cos(lean_r) * 0 )
        sh = (sh[0], y(self.sh_y))
        head = (sh[0] + pose.head_dx + math.sin(lean_r) * 3,
                y(self.head_y) + pose.head_dy)

        half = self.torso_w / 2.0
        sh_b = (sh[0] - half * 0.75, sh[1] + 1)
        sh_f = (sh[0] + half * 0.75, sh[1] + 1)
        hip_b = (hip[0] - half * 0.55, hip[1])
        hip_f = (hip[0] + half * 0.55, hip[1])

        def chain(root, angles, l1, l2):
            mid = polar(root[0], root[1], angles[0], l1)
            end = polar(mid[0], mid[1], angles[1], l2)
            return mid, end

        elbow_b, hand_b = chain(sh_b, pose.arm_b, self.upper_arm, self.fore_arm)
        elbow_f, hand_f = chain(sh_f, pose.arm_f, self.upper_arm, self.fore_arm)
        knee_b, foot_b = chain(hip_b, pose.leg_b, self.thigh, self.shin)
        knee_f, foot_f = chain(hip_f, pose.leg_f, self.thigh, self.shin)

        out = dict(head=head, sh=sh, hip=hip, sh_b=sh_b, sh_f=sh_f,
                   hip_b=hip_b, hip_f=hip_f,
                   elbow_b=elbow_b, hand_b=hand_b,
                   elbow_f=elbow_f, hand_f=hand_f,
                   knee_b=knee_b, foot_b=foot_b,
                   knee_f=knee_f, foot_f=foot_f)
        if pose.prone > 0:
            out = self._topple(out, pose.prone)
        return out

    # A defeated body lies down; it does not shrink. PRONE is where every
    # joint ends up, and `t` is how far along the fall we are.
    PRONE = dict(head=(10, 33), sh=(15, 35), hip=(20, 35),
                 sh_b=(15, 34), sh_f=(15, 36), hip_b=(20, 34), hip_f=(20, 36),
                 elbow_b=(17, 32), hand_b=(20, 31),
                 elbow_f=(17, 37), hand_f=(20, 37),
                 knee_b=(24, 34), foot_b=(27, 33),
                 knee_f=(24, 36), foot_f=(27, 37))

    def _topple(self, j, t):
        r = self.head_r
        lift = (4.6 - r) * 0.8          # small bodies lie a little lower
        out = {}
        for k, v in j.items():
            tx, ty = self.PRONE[k]
            out[k] = (lerp(v[0], tx + self.pad, t),
                      lerp(v[1], ty + lift + self.pad, t))
        return out


def _i(p):
    return (int(round(p[0])), int(round(p[1])))


# ---------------------------------------------------------------------------
#  Drawing a character
# ---------------------------------------------------------------------------

def _wkw(b, kind):
    """Per-body weapon tinting. A beast's claws should be its own colour."""
    if kind == "claw":
        return {"metal": shift(b.skin, 0.42)}
    return {}


def _limb(c, root, mid, end, color, t):
    r, m, e = _i(root), _i(mid), _i(end)
    c.line(r[0], r[1], m[0], m[1], color, t)
    c.line(m[0], m[1], e[0], e[1], color, max(1, t - 1) if t > 2 else t)


def draw_character(body, pose, flip=False, canvas=None, pad=0):
    import parts
    c = canvas or Canvas(W + pad * 2, H + pad * 2)
    rig = Rig(body, pad)
    j = rig.joints(pose)
    b = body
    t = rig.limb_t
    back_col = shift(b.limb, -0.42)

    parts.draw_extras(c, b.extras, j, rig, b, back=True)

    # back arm + back leg
    _limb(c, j["sh_b"], j["elbow_b"], j["hand_b"], back_col, t)
    _limb(c, j["hip_b"], j["knee_b"], j["foot_b"], shift(b.cloth_dk, -0.42), t + 1)
    if pose.prone < 0.5:
        fb = _i(j["foot_b"])
        c.rect(fb[0] - 2, fb[1], 5, 2, shift(LEATHER_DK, -0.2))

    # offhand item in the back hand
    if b.offhand:
        hb = j["hand_b"]
        parts.draw_weapon(c, b.offhand, hb[0], hb[1],
                          pose.weapon - 180 if b.offhand != "shield" else 260,
                          scale=b.scale * INHAND.get(b.offhand, 0.8),
                          **_wkw(b, b.offhand))

    parts.draw_torso(c, b.torso, j, rig, b)

    # front leg
    _limb(c, j["hip_f"], j["knee_f"], j["foot_f"], b.cloth_dk, t + 1)
    if pose.prone < 0.5:
        ff = _i(j["foot_f"])
        c.rect(ff[0] - 2, ff[1], 5, 2, LEATHER_DK)

    # head
    hd = j["head"]
    parts.draw_head(c, b.head, hd[0], hd[1], rig.head_r, b)
    if b.helmet:
        parts.draw_helmet(c, b.helmet, hd[0], hd[1], rig.head_r, b,
                          b.cloth if b.helmet in ("hood", "wizard")
                          else (b.accent if b.helmet == "crown" else None))

    # front arm, then the weapon in that hand
    _limb(c, j["sh_f"], j["elbow_f"], j["hand_f"], b.limb, t)
    if b.weapon:
        hf = j["hand_f"]
        parts.draw_weapon(c, b.weapon, hf[0] + pose.weapon_dx,
                          hf[1] + pose.weapon_dy, pose.weapon,
                          scale=b.scale * b.weapon_scale, **_wkw(b, b.weapon))

    parts.draw_extras(c, b.extras, j, rig, b, back=False)

    c.shade(LIGHT, DARK)
    c.outline(INK)

    if pose.fade > 0:
        f = pose.fade
        c = c.desaturated(amount=0.9 * f, darken=1 - 0.45 * f)
    if flip:
        c = c.flip_h()
    return c


# ---------------------------------------------------------------------------
#  Animation
# ---------------------------------------------------------------------------

MELEE = {"sword", "greatsword", "katana", "dagger", "axe", "greataxe", "mace",
         "hammer", "spear", "trident", "scythe", "club", "flail", "claw",
         "whip"}
RANGED = {"bow", "crossbow"}
CASTER = {"staff", "wand", "tome", "orb", "torch"}


def rest_angle(weapon):
    if weapon in RANGED:
        return 100
    if weapon in ("tome", "orb"):
        return 60
    if weapon in ("claw",):
        return 60
    if weapon in ("whip",):
        return 120
    return 168


def _keys(frames, keyframes):
    """keyframes: [(t0, poseA), (t1, poseB), ...] with t in 0..1."""
    out = []
    for i in range(frames):
        t = i / float(frames)
        for k in range(len(keyframes) - 1):
            t0, p0 = keyframes[k]
            t1, p1 = keyframes[k + 1]
            if t0 <= t <= t1:
                local = 0 if t1 == t0 else (t - t0) / (t1 - t0)
                out.append(p0.blend(p1, local))
                break
        else:
            out.append(keyframes[-1][1])
    return out


def anim_idle(body, n=4):
    wa = rest_angle(body.weapon)
    base = Pose(weapon=wa)
    up = Pose(dy=-1, head_dy=-1, weapon=wa - 3,
              arm_b=(-20, -30), arm_f=(20, 30), leg_b=(-8, -4), leg_f=(10, 6))
    return _keys(n, [(0.0, base), (0.5, up), (1.0, base)])


def anim_walk(body, n=6):
    wa = rest_angle(body.weapon)
    swing_b = 8 if body.offhand else 14      # a held shield barely swings
    out = []
    for i in range(n):
        p = i / float(n)
        s = math.sin(p * math.tau)
        s2 = math.sin(p * math.tau + math.pi)
        out.append(Pose(
            dy=-abs(math.sin(p * math.tau * 2)) * 1.2,
            lean=4,
            arm_b=(-18 + s2 * swing_b, -26 + s2 * swing_b * 0.8),
            arm_f=(18 + s * 12, 26 + s * 9),
            leg_b=(-6 + s * 26, -6 + max(0, s) * 26),
            leg_f=(8 + s2 * 26, 4 + max(0, s2) * 26),
            weapon=wa - s * 4,
        ))
    return out


def anim_attack(body, n=6):
    w = body.weapon
    wa = rest_angle(w)
    if w in RANGED:
        ready = Pose(lean=-4, arm_b=(64, 76), arm_f=(78, 82), weapon=96,
                     leg_b=(-16, -10), leg_f=(18, 10))
        draw = Pose(lean=-6, arm_b=(48, 14), arm_f=(80, 84), weapon=98,
                    leg_b=(-18, -12), leg_f=(20, 12), dx=-1)
        loose = Pose(lean=6, arm_b=(74, 88), arm_f=(78, 82), weapon=96,
                     leg_b=(-14, -8), leg_f=(18, 10), dx=1)
        return _keys(n, [(0.0, ready), (0.45, draw), (0.62, draw),
                         (0.78, loose), (1.0, ready)])
    if w in CASTER or w is None and "aura" in body.extras:
        low = Pose(weapon=wa, arm_f=(20, 28), arm_b=(-20, -30))
        raise_ = Pose(weapon=186, arm_f=(48, 10), arm_b=(-40, -74),
                      dy=-1, head_dy=-1, lean=-6)
        cast = Pose(weapon=150, arm_f=(56, 32), arm_b=(-26, -48),
                    dx=-1, lean=8, head_dy=1)
        return _keys(n, [(0.0, low), (0.4, raise_), (0.58, raise_),
                         (0.76, cast), (1.0, low)])
    # melee: wind up behind, then swing through
    idle = Pose(weapon=wa)
    wind = Pose(lean=-10, dx=-1, arm_f=(-112, -142), arm_b=(-26, -40),
                weapon=206, leg_b=(-16, -10), leg_f=(14, 8), head_dy=-1)
    strike = Pose(lean=14, dx=0, arm_f=(30, 40), arm_b=(-6, 10),
                  weapon=36, leg_b=(-24, -15), leg_f=(24, 13), head_dy=1)
    follow = Pose(lean=8, dx=0, arm_f=(22, 24), arm_b=(-12, -6),
                  weapon=14, leg_b=(-16, -10), leg_f=(16, 9))
    return _keys(n, [(0.0, idle), (0.32, wind), (0.46, wind),
                     (0.60, strike), (0.78, follow), (1.0, idle)])


def anim_hurt(body, n=3):
    wa = rest_angle(body.weapon)
    idle = Pose(weapon=wa)
    hit = Pose(dx=-1, lean=-16, head_dx=-1, head_dy=1, weapon=wa + 22,
               arm_b=(-34, -46), arm_f=(-10, -26),
               leg_b=(-18, -11), leg_f=(4, 18))
    ease = Pose(dx=0, lean=-7, weapon=wa + 10,
                arm_b=(-26, -36), arm_f=(2, -4))
    return _keys(n, [(0.0, hit), (0.6, ease), (1.0, idle)])


def anim_death(body, n=6):
    wa = rest_angle(body.weapon)
    idle = Pose(weapon=wa)
    stagger = Pose(dx=-1, lean=-20, head_dy=1, weapon=wa + 26,
                   arm_b=(-44, -58), arm_f=(-20, -42), leg_b=(-20, -13))
    buckle = Pose(dx=-1, lean=-36, prone=0.40, fade=0.3, weapon=wa + 58,
                  arm_b=(-56, -68), arm_f=(-40, -58),
                  leg_b=(-28, -40), leg_f=(-6, -28))
    fall = Pose(dx=0, lean=-56, prone=0.80, fade=0.7, weapon=118)
    down = Pose(prone=1.0, fade=1.0, weapon=96)
    return _keys(n, [(0.0, idle), (0.18, stagger), (0.42, buckle),
                     (0.66, fall), (0.86, down), (1.0, down)])


ANIMS = {"idle": (anim_idle, 4), "walk": (anim_walk, 6),
         "attack": (anim_attack, 6), "hurt": (anim_hurt, 3),
         "death": (anim_death, 6)}


def _tame_offhand(body, poses):
    """A shield stays tucked against the body; a free hand may flail."""
    if not body.offhand:
        return poses
    rest = (-18.0, -26.0)
    for p in poses:
        p.arm_b = tuple(rest[i] + (p.arm_b[i] - rest[i]) * 0.45 for i in (0, 1))
    return poses


def render_anim(body, name, flip=False, pad=0):
    fn, n = ANIMS[name]
    poses = _tame_offhand(body, fn(body, n))
    return [draw_character(body, p, flip=flip, pad=pad) for p in poses]


# ---------------------------------------------------------------------------
#  Auto-fit
#
#  Hand-tuning every pose so no weapon pokes out of a 32x40 cell does not
#  converge - there are too many weapon lengths. Instead: draw each frame
#  with generous padding, measure the union of every frame of every animation
#  for that character, then crop all of them with ONE shared window. Shared,
#  because a per-frame window would make the sprite jitter. If the union
#  still will not fit, the character is drawn slightly smaller and remeasured.
# ---------------------------------------------------------------------------

PAD = 14


def _bbox(c):
    x0, y0, x1, y1 = c.w, c.h, -1, -1
    for y in range(c.h):
        row = y * c.w
        for x in range(c.w):
            if c.px[row + x][3]:
                if x < x0: x0 = x
                if x > x1: x1 = x
                if y < y0: y0 = y
                if y > y1: y1 = y
    return (x0, y0, x1, y1)


def _crop(c, ox, oy):
    out = Canvas(W, H)
    for y in range(H):
        for x in range(W):
            sx, sy = x + ox, y + oy
            if 0 <= sx < c.w and 0 <= sy < c.h:
                out.px[y * W + x] = c.px[sy * c.w + sx]
    return out


def build_character(body, anims=None, flip=False):
    """Render every animation for `body`, auto-fitted into 32x40 cells."""
    anims = anims or list(ANIMS)
    for attempt in range(5):
        raw = {a: render_anim(body, a, flip=flip, pad=PAD) for a in anims}
        boxes = [_bbox(f) for fr in raw.values() for f in fr]
        boxes = [b for b in boxes if b[2] >= 0]
        x0 = min(b[0] for b in boxes); x1 = max(b[2] for b in boxes)
        y0 = min(b[1] for b in boxes); y1 = max(b[3] for b in boxes)
        if (x1 - x0) < W and (y1 - y0) < H:
            break
        body = replace(body, scale=body.scale * 0.9,
                       weapon_scale=body.weapon_scale * 0.88)
    cw, chh = raw[anims[0]][0].w, raw[anims[0]][0].h
    ox = int(round((x0 + x1) / 2.0)) - W // 2          # centre horizontally
    ox = max(x1 - W + 1, min(ox, x0))                  # ...but never cut art off
    oy = PAD                                            # keep feet on the floor
    oy = max(y1 - H + 1, min(oy, y0))
    ox = max(0, min(ox, cw - W))
    oy = max(0, min(oy, chh - H))
    return {a: [_crop(f, ox, oy) for f in fr] for a, fr in raw.items()}


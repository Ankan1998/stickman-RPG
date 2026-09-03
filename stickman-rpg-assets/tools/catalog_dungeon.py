"""Dungeon tileset, 16x16, built for procedural generation.

Floors and walls are drawn so their edges line up when repeated, so a
generator can stamp them on a grid without seams. Props sit on a
transparent background and are meant to be layered over a floor.
"""

import math
import random
from pixelart import Canvas
from rig import INK, LIGHT, DARK, WOOD, WOOD_DK, GOLD, BLOOD, POISON, BONE, \
                BONE_DK, STEEL, STEEL_DK, LEATHER, LEATHER_DK, CLOTH_W, \
                C, shift

T = 16

STONE   = C("6b6f7a")
STONE_D = C("4a4e57")
STONE_L = C("878d99")
MORTAR  = C("34373f")
DIRT    = C("55483a")
MOSS    = C("5c8f4a")
SAND    = C("c9b083")
WATER   = C("3f7fa8")
LAVA    = C("d95f2b")
RUNE    = C("6fa8dc")


def _rng(seed):
    return random.Random(seed)


def _noise(c, seed, color, n=18):
    r = _rng(seed)
    for _ in range(n):
        c.set(r.randrange(T), r.randrange(T), color)


# ---------------------------------------------------------------------------
#  FLOORS   opaque, tile seamlessly
# ---------------------------------------------------------------------------

def floor_stone(c):
    c.rect(0, 0, T, T, shift(STONE, -0.18))
    for y in (0, 8):
        c.rect(0, y, T, 1, MORTAR)
    c.rect(0, 0, 1, 8, MORTAR)
    c.rect(8, 8, 1, 8, MORTAR)
    for bx, by in ((1, 1), (9, 9)):
        c.rect(bx, by, 7, 1, shift(STONE, 0.05))
    for bx, by in ((9, 1), (1, 9)):
        c.rect(bx, by, 7, 7, shift(STONE, -0.26))
    _noise(c, 1, shift(STONE_D, -0.15), 18)


def floor_cracked(c):
    floor_stone(c)
    r = _rng(2)
    x, y = 3, 2
    for _ in range(12):
        c.set(x, y, STONE_D)
        x = max(0, min(T - 1, x + r.choice((0, 1, 1))))
        y = max(0, min(T - 1, y + r.choice((-1, 0, 1, 1))))
    c.rect(10, 11, 3, 1, STONE_D)


def floor_mossy(c):
    floor_stone(c)
    r = _rng(3)
    for _ in range(26):
        x, y = r.randrange(T), r.randrange(T)
        c.set(x, y, MOSS)
        if r.random() < 0.5:
            c.set(x + 1, y, shift(MOSS, -0.25))


def floor_wood(c):
    c.rect(0, 0, T, T, WOOD)
    for y in range(0, T, 4):
        c.rect(0, y, T, 1, WOOD_DK)
        c.rect(0, y + 1, T, 1, shift(WOOD, 0.12))
    for x, y in ((5, 0), (11, 4), (2, 8), (13, 12)):
        c.rect(x, y, 1, 4, WOOD_DK)
    _noise(c, 4, shift(WOOD, -0.2), 10)


def floor_sand(c):
    c.rect(0, 0, T, T, SAND)
    _noise(c, 5, shift(SAND, -0.15), 30)
    _noise(c, 6, shift(SAND, 0.18), 18)


def floor_dirt(c):
    c.rect(0, 0, T, T, DIRT)
    _noise(c, 7, shift(DIRT, -0.22), 26)
    _noise(c, 8, shift(DIRT, 0.16), 14)


def floor_blood(c):
    floor_stone(c)
    r = _rng(9)
    c.ellipse(7, 8, 5, 4, shift(BLOOD, -0.25))
    c.ellipse(6, 7, 3, 2, BLOOD)
    for _ in range(7):
        c.set(r.randrange(T), r.randrange(T), shift(BLOOD, -0.35))


def floor_water(c):
    c.rect(0, 0, T, T, WATER)
    for y in range(0, T, 3):
        c.rect(0, y, T, 1, shift(WATER, 0.16))
    c.rect(2, 5, 5, 1, shift(WATER, 0.4))
    c.rect(9, 11, 4, 1, shift(WATER, 0.4))


def floor_lava(c):
    c.rect(0, 0, T, T, shift(LAVA, -0.35))
    for y in range(T):
        for x in range(T):
            v = math.sin(x * 0.7) + math.cos(y * 0.9)
            if v > 0.6:
                c.set(x, y, LAVA)
            elif v > 1.4:
                c.set(x, y, C("f7d14a"))
    c.rect(4, 3, 3, 1, C("f7d14a"))
    c.rect(10, 10, 3, 1, C("f7d14a"))


# ---------------------------------------------------------------------------
#  WALLS
# ---------------------------------------------------------------------------

def _brick(c, base, mortar, light):
    c.rect(0, 0, T, T, base)
    for y in range(0, T, 4):
        c.rect(0, y, T, 1, mortar)
        off = 0 if (y // 4) % 2 == 0 else 4
        for x in range(off, T + 8, 8):
            c.rect(x % T, y + 1, 1, 3, mortar)
        c.rect(0, y + 1, T, 1, light)


def wall_stone(c):
    _brick(c, STONE, MORTAR, STONE_L)
    _noise(c, 11, STONE_D, 16)


def wall_stone_top(c):
    """Top edge of a wall run - the lit cap a generator puts on row 0."""
    wall_stone(c)
    c.rect(0, 0, T, 3, STONE_L)
    c.rect(0, 3, T, 1, shift(STONE_L, -0.2))
    c.rect(0, 0, T, 1, shift(STONE_L, 0.25))


def wall_cracked(c):
    wall_stone(c)
    r = _rng(12)
    x, y = 4, 1
    for _ in range(14):
        c.set(x, y, STONE_D)
        c.set(x + 1, y, shift(STONE_D, -0.3))
        x = max(0, min(T - 1, x + r.choice((0, 1))))
        y = min(T - 1, y + 1)


def wall_mossy(c):
    wall_stone(c)
    r = _rng(13)
    for _ in range(22):
        x, y = r.randrange(T), r.randrange(T)
        c.set(x, y, MOSS)
    for x in range(T):
        if r.random() < 0.5:
            c.set(x, T - 1, shift(MOSS, -0.2))


def wall_brick(c):
    _brick(c, C("8a5a4a"), C("4a2f28"), C("a8705c"))


def wall_rune(c):
    wall_stone(c)
    c.rect(4, 3, 8, 10, STONE_D)
    for x, y in ((6, 5), (7, 5), (8, 5), (6, 6), (6, 7), (7, 7),
                 (9, 8), (9, 9), (8, 10), (7, 10)):
        c.set(x, y, RUNE)
    for x, y in ((6, 5), (7, 7), (9, 9)):
        c.blend(x, y - 1, (RUNE[0], RUNE[1], RUNE[2], 110))


# ---------------------------------------------------------------------------
#  STRUCTURES
# ---------------------------------------------------------------------------

def door_closed(c):
    c.rect(0, 0, T, T, STONE_D)
    c.rect(2, 1, 12, 15, WOOD_DK)
    c.rect(3, 2, 10, 13, WOOD)
    for x in (5, 8, 11):
        c.rect(x, 2, 1, 13, WOOD_DK)
    c.rect(3, 6, 10, 1, STEEL_DK)
    c.rect(3, 11, 10, 1, STEEL_DK)
    c.disc(11, 9, 1, GOLD)


def door_open(c):
    c.rect(0, 0, T, T, STONE_D)
    c.rect(0, 0, T, 2, STONE)
    c.rect(2, 1, 3, 15, WOOD)
    c.rect(2, 1, 3, 1, WOOD_DK)
    c.rect(11, 1, 3, 15, WOOD)
    c.rect(5, 2, 6, 14, C("1a1820"))


def portcullis(c):
    c.rect(0, 0, T, T, C("1a1820"))
    for x in range(1, T, 4):
        c.rect(x, 0, 2, T, STEEL_DK)
        c.rect(x, 0, 1, T, STEEL)
    for y in range(2, T, 6):
        c.rect(0, y, T, 2, STEEL_DK)
        c.rect(0, y, T, 1, STEEL)


def stairs_down(c):
    c.rect(0, 0, T, T, STONE_D)
    for i in range(4):
        y = i * 4
        w = T - i * 2
        c.rect(i, y, w, 3, shift(STONE, -0.12 * i))
        c.rect(i, y, w, 1, shift(STONE_L, -0.12 * i))
    c.rect(6, 13, 4, 3, C("14131c"))


def stairs_up(c):
    c.rect(0, 0, T, T, STONE_D)
    for i in range(4):
        y = T - 4 - i * 4
        w = T - i * 2
        c.rect(i, y, w, 3, shift(STONE, 0.06 * i))
        c.rect(i, y, w, 1, shift(STONE_L, 0.06 * i))


def pillar(c):
    c.rect(4, 0, 8, T, STONE)
    c.rect(4, 0, 8, 2, STONE_L)
    c.rect(3, 1, 10, 2, STONE_L)
    c.rect(3, 13, 10, 3, STONE_L)
    c.rect(5, 3, 1, 10, STONE_L)
    c.rect(10, 3, 1, 10, STONE_D)
    c.rect(4, 0, 8, 1, shift(STONE_L, 0.2))


def arch(c):
    c.rect(0, 0, T, T, C("1a1820"))
    c.rect(0, 0, 4, T, STONE)
    c.rect(12, 0, 4, T, STONE)
    for x in range(T):
        d = abs(x - 7.5) / 7.5
        h = int(5 - d * d * 4)
        c.rect(x, 0, 1, max(1, h), STONE)
        c.set(x, max(1, h), STONE_L)
    c.rect(0, 0, T, 1, STONE_L)


# ---------------------------------------------------------------------------
#  PROPS   transparent background, layered over a floor
# ---------------------------------------------------------------------------

def _finish(c):
    c.shade(LIGHT, DARK)
    c.outline(INK)
    return c


def chest_closed(c):
    c.rect(2, 7, 12, 7, WOOD)
    c.rect(2, 4, 12, 4, WOOD_DK)
    c.ellipse(8, 5, 6, 2, WOOD)
    c.rect(2, 7, 12, 1, shift(WOOD, -0.35))
    for x in (4, 11):
        c.rect(x, 4, 1, 10, GOLD)
    c.rect(7, 8, 2, 3, GOLD)
    c.set(8, 9, C("14131c"))
    _finish(c)


def chest_open(c):
    c.rect(2, 8, 12, 6, WOOD)
    c.rect(2, 8, 12, 1, shift(WOOD, 0.2))
    c.rect(3, 2, 11, 4, WOOD_DK)
    c.rect(4, 3, 9, 2, shift(WOOD_DK, -0.25))
    c.rect(4, 9, 9, 4, GOLD)
    for x, y in ((5, 10), (8, 9), (11, 11)):
        c.set(x, y, C("fff2b0"))
    for x in (4, 11):
        c.rect(x, 8, 1, 6, shift(GOLD, -0.3))
    _finish(c)


def barrel(c):
    c.ellipse(8, 4, 5, 2, WOOD)
    c.rect(3, 4, 11, 10, WOOD)
    c.ellipse(8, 14, 5, 2, WOOD_DK)
    for y in (6, 11):
        c.rect(3, y, 11, 1, shift(WOOD_DK, -0.15))
    c.rect(5, 5, 1, 9, shift(WOOD, 0.22))
    _finish(c)


def crate(c):
    c.rect(2, 4, 12, 11, WOOD)
    c.frame(2, 4, 12, 11, WOOD_DK)
    c.line(2, 4, 13, 14, WOOD_DK)
    c.line(13, 4, 2, 14, WOOD_DK)
    c.rect(2, 4, 12, 1, shift(WOOD, 0.25))
    _finish(c)


def urn(c):
    c.ellipse(8, 9, 5, 5, C("b07a4a"))
    c.rect(6, 3, 5, 4, C("96643c"))
    c.ellipse(8, 3, 4, 1, C("b07a4a"))
    c.rect(4, 12, 9, 2, C("96643c"))
    c.ellipse(6, 7, 1, 2, shift(C("b07a4a"), 0.3))
    _finish(c)


def torch_wall(c):
    c.rect(7, 8, 2, 7, WOOD_DK)
    c.rect(6, 7, 4, 2, STEEL_DK)
    c.ellipse(8, 4, 3, 4, C("c4453a"))
    c.ellipse(8, 4, 2, 3, C("e08a3c"))
    c.ellipse(8, 4, 1, 2, C("fae296"))
    _finish(c)
    for j in range(0, 10):
        for i in range(T):
            d = ((i - 8) ** 2 + (j - 4) ** 2) ** 0.5
            a = max(0, 1 - d / 8.0)
            if a > 0:
                c.blend(i, j, (240, 170, 90, int(60 * a * a)))


def brazier(c):
    c.rect(6, 11, 4, 4, STEEL_DK)
    c.rect(4, 14, 8, 2, STEEL_DK)
    c.rect(3, 8, 10, 4, STEEL)
    c.rect(3, 8, 10, 1, shift(STEEL, 0.25))
    c.ellipse(8, 6, 4, 3, C("c4453a"))
    c.ellipse(8, 5, 3, 3, C("e08a3c"))
    c.ellipse(8, 4, 1, 2, C("fae296"))
    _finish(c)


def bones(c):
    c.rect(3, 11, 9, 2, BONE)
    c.disc(3, 12, 1, BONE)
    c.disc(12, 12, 1, BONE)
    c.rect(5, 8, 7, 2, BONE_DK)
    c.disc(5, 8, 1, BONE_DK)
    _finish(c)


def skull_pile(c):
    c.disc(5, 10, 3, BONE)
    c.rect(3, 12, 5, 2, BONE)
    c.set(4, 10, INK); c.set(6, 10, INK)
    c.disc(11, 11, 3, BONE_DK)
    c.set(10, 11, INK); c.set(12, 11, INK)
    c.rect(2, 14, 12, 2, shift(BONE_DK, -0.25))
    _finish(c)


def cobweb(c):
    for a in range(0, 91, 15):
        r = math.radians(a)
        c.line(0, 0, int(math.cos(r) * 15), int(math.sin(r) * 15), CLOTH_W)
    for rad in (5, 9, 13):
        prev = None
        for a in range(0, 95, 10):
            r = math.radians(a)
            p = (int(math.cos(r) * rad), int(math.sin(r) * rad))
            if prev:
                c.line(prev[0], prev[1], p[0], p[1], shift(CLOTH_W, -0.2))
            prev = p


def spike_trap(c):
    c.rect(0, 12, T, 4, STONE_D)
    for x in range(1, T, 3):
        c.line(x, 12, x + 1, 4, STEEL, 1)
        c.line(x + 1, 4, x + 2, 12, STEEL_DK, 1)
        c.set(x + 1, 3, shift(STEEL, 0.35))
    _finish(c)


def pressure_plate(c):
    c.rect(0, 0, T, T, STONE_D)
    c.rect(2, 2, 12, 12, STONE)
    c.frame(2, 2, 12, 12, STONE_D)
    c.rect(3, 3, 10, 1, STONE_L)
    c.rect(6, 6, 4, 4, shift(STONE_D, -0.2))


def altar(c):
    c.rect(2, 6, 12, 8, STONE)
    c.rect(1, 4, 14, 3, STONE_L)
    c.rect(1, 4, 14, 1, shift(STONE_L, 0.22))
    c.rect(3, 13, 10, 3, STONE_D)
    c.disc(8, 3, 2, C("9b6fd6"))
    c.disc(8, 3, 1, C("d0b0f0"))
    _finish(c)
    for j in range(0, 9):
        for i in range(T):
            d = ((i - 8) ** 2 + (j - 3) ** 2) ** 0.5
            a = max(0, 1 - d / 7.0)
            if a > 0:
                c.blend(i, j, (155, 111, 214, int(70 * a * a)))


def rubble(c):
    r = _rng(21)
    spots = ((2, 9, 4), (7, 11, 5), (12, 10, 3), (5, 6, 3), (10, 7, 4),
             (3, 13, 3), (9, 14, 4), (13, 13, 2))
    for x, y, s_ in spots:
        col = STONE if r.random() < 0.55 else STONE_D
        c.rect(x, y, s_, s_ - 1, col)
        c.rect(x, y, s_, 1, STONE_L)
        c.set(x + s_ - 1, y + s_ - 2, shift(col, -0.3))
    _finish(c)


def banner(c):
    c.rect(2, 0, 12, 2, WOOD_DK)
    c.rect(3, 2, 10, 11, C("9e2f2f"))
    c.rect(3, 2, 10, 1, shift(C("9e2f2f"), 0.25))
    for x in (3, 12):
        c.rect(x, 2, 1, 11, shift(C("9e2f2f"), -0.3))
    c.rect(6, 5, 4, 4, GOLD)
    c.rect(7, 6, 2, 2, C("9e2f2f"))
    for i, x in enumerate(range(3, 13, 3)):
        c.rect(x, 13, 3, 2 if i % 2 else 1, C("9e2f2f"))
    _finish(c)


def chain(c):
    for y in range(0, T, 4):
        c.ellipse(8, y + 2, 2, 2, STEEL_DK)
        c.ellipse(8, y + 2, 1, 1, C("1a1820"))
        c.set(7, y + 1, STEEL)


def mushrooms(c):
    for cx, cy, rad, col in ((5, 10, 3, C("c4453a")), (10, 12, 2, C("8e5bc4")),
                             (12, 8, 2, C("c4453a"))):
        c.rect(cx - 1, cy, 2, 16 - cy - 2, CLOTH_W)
        c.ellipse(cx, cy, rad, rad - 1, col)
        c.set(cx - 1, cy - 1, shift(col, 0.45))
    _finish(c)


def pit(c):
    c.rect(0, 0, T, T, C("14131c"))
    for i in range(4):
        c.frame(i, i, T - i * 2, T - i * 2, shift(STONE_D, -0.18 * i))
    c.rect(0, 0, T, 1, STONE_L)


def key_item(c):
    c.disc(5, 5, 3, GOLD)
    c.disc(5, 5, 1, C("14131c"))
    c.rect(6, 7, 2, 8, GOLD)
    c.rect(8, 12, 3, 2, GOLD)
    c.rect(8, 9, 2, 2, GOLD)
    _finish(c)


def coin_pile(c):
    for x, y in ((4, 12), (7, 13), (10, 12), (6, 10), (9, 10), (7, 8)):
        c.ellipse(x, y, 2, 1, GOLD)
        c.set(x - 1, y - 1, C("fff2b0"))
    _finish(c)


def potion(c):
    c.rect(6, 2, 4, 3, STEEL_DK)
    c.rect(5, 5, 6, 2, C("8a8f9c"))
    c.ellipse(8, 10, 5, 5, C("c4453a"))
    c.ellipse(8, 11, 4, 3, shift(C("c4453a"), -0.2))
    c.ellipse(6, 9, 1, 2, C("ff9a90"))
    _finish(c)


TILES = [
    # (name, fn, category, tiles-seamlessly, blurb)
    ("floor_stone", floor_stone, "floor", True, "Base dungeon floor."),
    ("floor_cracked", floor_cracked, "floor", True, "Damaged floor variant."),
    ("floor_mossy", floor_mossy, "floor", True, "Overgrown, for damp rooms."),
    ("floor_wood", floor_wood, "floor", True, "Planking for inhabited rooms."),
    ("floor_sand", floor_sand, "floor", True, "Desert or crypt floor."),
    ("floor_dirt", floor_dirt, "floor", True, "Cave and tunnel floor."),
    ("floor_blood", floor_blood, "floor", True, "Marks a past encounter."),
    ("floor_water", floor_water, "floor", True, "Shallow water; slows movement."),
    ("floor_lava", floor_lava, "floor", True, "Damages anything standing on it."),
    ("wall_stone", wall_stone, "wall", True, "Standard wall block."),
    ("wall_stone_top", wall_stone_top, "wall", True, "Lit cap for the top wall row."),
    ("wall_cracked", wall_cracked, "wall", True, "Breakable wall variant."),
    ("wall_mossy", wall_mossy, "wall", True, "Damp-room wall."),
    ("wall_brick", wall_brick, "wall", True, "Red brick, for a different biome."),
    ("wall_rune", wall_rune, "wall", False, "Marks a secret or a boss door."),
    ("door_closed", door_closed, "structure", False, "Blocks a room exit."),
    ("door_open", door_open, "structure", False, "Passable exit."),
    ("portcullis", portcullis, "structure", False, "Gate; opens on a trigger."),
    ("stairs_down", stairs_down, "structure", False, "Descend to the next floor."),
    ("stairs_up", stairs_up, "structure", False, "Return to the previous floor."),
    ("pillar", pillar, "structure", False, "Room decoration and cover."),
    ("arch", arch, "structure", False, "Corridor entrance."),
    ("pit", pit, "structure", False, "Impassable hole."),
    ("chest_closed", chest_closed, "prop", False, "Unopened loot."),
    ("chest_open", chest_open, "prop", False, "Looted state for a chest."),
    ("barrel", barrel, "prop", False, "Breakable container."),
    ("crate", crate, "prop", False, "Breakable container."),
    ("urn", urn, "prop", False, "Breakable container."),
    ("torch_wall", torch_wall, "prop", False, "Wall light source."),
    ("brazier", brazier, "prop", False, "Floor light source."),
    ("bones", bones, "prop", False, "Scatter decor."),
    ("skull_pile", skull_pile, "prop", False, "Scatter decor; crypt rooms."),
    ("cobweb", cobweb, "prop", False, "Corner overlay for stale rooms."),
    ("spike_trap", spike_trap, "prop", False, "Damages anything that steps on it."),
    ("pressure_plate", pressure_plate, "prop", False, "Trap and door trigger."),
    ("altar", altar, "prop", False, "Shrine; buffs or curses."),
    ("rubble", rubble, "prop", False, "Scatter decor."),
    ("banner", banner, "prop", False, "Wall decor; marks a faction."),
    ("chain", chain, "prop", False, "Hanging wall decor."),
    ("mushrooms", mushrooms, "prop", False, "Cave decor; harvestable."),
    ("key_item", key_item, "pickup", False, "Opens a locked door."),
    ("coin_pile", coin_pile, "pickup", False, "Currency drop."),
    ("potion", potion, "pickup", False, "Consumable drop."),
]


def render_tile(entry):
    name, fn, cat, seamless, blurb = entry
    c = Canvas(T, T)
    fn(c)
    return c

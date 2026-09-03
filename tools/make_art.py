"""
Generates every image in game/assets/.

Run it with:      python tools/make_art.py
Preview in text:  python tools/make_art.py --preview

The PNGs are OUTPUT, not source. If you want to change the art, change this
file and re-run it. That keeps the art diffable in git and means nobody needs
a paint program to tweak a colour.

Everything is drawn from primitives (discs, lines, rects) rather than
hand-placed pixels, then given a dark outline and a light/dark shading pass -
which is what makes flat colour read as pixel art rather than as blobs.
"""

import os
import sys

from pixelart import Canvas, hex_rgba as C

OUT = os.path.join(os.path.dirname(__file__), "..", "game", "assets")

# ---------------------------------------------------------------------------
#  PALETTE  -  one place, so the whole game stays colour-coherent
# ---------------------------------------------------------------------------

INK        = C("14131c")      # near-black outline used on everything
INK_SOFT   = C("14131c", 120)

SKIN       = C("e8b796")
SKIN_DK    = C("c08a69")

STEEL      = C("b9c4d4")
STEEL_DK   = C("7d8798")
LEATHER    = C("6b4a32")
LEATHER_DK = C("4a3222")

HERO_BLUE  = C("4d8fd6")
HERO_BLUE_D= C("2f5f96")
HERO_TEAL  = C("53c1a6")
HERO_TEAL_D= C("2f8570")

GOB_GREEN  = C("7dae4c")
GOB_GREEN_D= C("4f7530")
BRUTE_GRN  = C("5c8f4a")
BRUTE_GRN_D= C("3a5f30")
BRUTE_RED  = C("b3543f")

WOOD       = C("8a5f38")
WOOD_DK    = C("5e3f24")
CLOTH_W    = C("e8e4dc")
GOLD       = C("e0c46c")
BLOOD      = C("c4453a")
POISON     = C("8ad148")
STUN_Y     = C("f2d05a")
SHIELD_B   = C("6fa8dc")

LIGHT      = C("ffffff", 60)   # shading: top highlight
DARK       = C("000000", 70)   # shading: bottom shadow


# ---------------------------------------------------------------------------
#  CHARACTERS   32 x 40, feet at the bottom
# ---------------------------------------------------------------------------

W, H = 32, 40


def _finish(c):
    c.shade(LIGHT, DARK)
    c.outline(INK)
    return c


def warrior():
    """Stick Warrior - blue tabard, sword, round shield."""
    c = Canvas(W, H)

    # sword (behind the arm, drawn first so the hand overlaps it)
    c.rect(23, 6, 2, 15, STEEL)          # blade
    c.rect(21, 20, 6, 2, GOLD)           # crossguard
    c.rect(23, 22, 2, 4, LEATHER)        # grip

    # head
    c.disc(14, 10, 5, SKIN)
    c.rect(9, 4, 11, 4, STEEL)           # helmet dome
    c.rect(9, 8, 11, 2, STEEL_DK)        # helmet brim
    c.rect(13, 8, 1, 4, STEEL_DK)        # nose guard
    c.set(11, 12, INK)                   # eyes
    c.set(17, 12, INK)

    # torso
    c.rect(11, 16, 8, 10, HERO_BLUE)
    c.rect(11, 16, 8, 3, HERO_BLUE_D)    # shoulder yoke
    c.rect(10, 22, 10, 2, LEATHER)       # belt
    c.rect(14, 22, 2, 2, GOLD)           # buckle

    # arms
    c.line(11, 18, 7, 23, SKIN, 2)       # shield arm
    c.line(19, 18, 24, 22, SKIN, 2)      # sword arm

    # shield
    c.ellipse(6, 25, 4, 5, WOOD)
    c.ellipse(6, 25, 2, 3, STEEL)

    # legs
    c.line(13, 26, 12, 35, HERO_BLUE_D, 3)
    c.line(17, 26, 19, 35, HERO_BLUE_D, 3)
    c.rect(9, 35, 6, 3, LEATHER_DK)      # boots
    c.rect(17, 35, 6, 3, LEATHER_DK)

    return _finish(c)


def medic():
    """Stick Medic - white and teal, satchel, healing staff."""
    c = Canvas(W, H)

    # staff
    c.rect(24, 8, 2, 20, WOOD)
    c.rect(22, 6, 6, 2, HERO_TEAL)       # crossbar of the healing symbol
    c.rect(24, 4, 2, 6, HERO_TEAL)

    # head + hood
    c.disc(14, 10, 5, SKIN)
    c.rect(9, 4, 11, 5, CLOTH_W)         # hood
    c.rect(9, 9, 3, 3, CLOTH_W)          # hood sides
    c.rect(17, 9, 3, 3, CLOTH_W)
    c.set(12, 12, INK)
    c.set(17, 12, INK)

    # robe
    c.rect(11, 16, 8, 9, CLOTH_W)
    c.rect(11, 25, 10, 6, CLOTH_W)       # flared skirt
    c.rect(10, 28, 12, 3, CLOTH_W)
    c.rect(14, 16, 2, 9, HERO_TEAL)      # centre stripe
    c.rect(10, 21, 10, 2, HERO_TEAL_D)   # sash

    # arms
    c.line(11, 18, 8, 23, SKIN, 2)
    c.line(19, 18, 24, 21, SKIN, 2)

    # satchel with a cross
    c.rect(5, 22, 6, 6, LEATHER)
    c.rect(7, 23, 2, 4, CLOTH_W)
    c.rect(6, 24, 4, 2, CLOTH_W)

    # feet
    c.rect(11, 31, 4, 3, LEATHER_DK)
    c.rect(17, 31, 4, 3, LEATHER_DK)

    return _finish(c)


def archer():
    """Stick Archer - green hood, longbow, quiver. Fast and crit-happy."""
    c = Canvas(W, H)

    GREEN = C("4f8f52")
    GREEN_D = C("336036")

    # longbow, drawn behind the body
    for y in range(6, 31):
        t = (y - 18) / 12.0
        x = int(25 - 3 * (1 - t * t))
        c.set(x, y, WOOD)
        c.set(x + 1, y, WOOD_DK)
    c.line(25, 6, 25, 30, C("ddd6c0"), 1)   # bowstring

    # quiver of arrows over the shoulder
    c.rect(5, 16, 5, 10, LEATHER_DK)
    for ax in (6, 8):
        c.line(ax, 10, ax, 17, C("ddd6c0"), 1)
        c.set(ax, 10, GOLD)

    # hooded head
    c.disc(14, 10, 5, SKIN)
    c.rect(9, 4, 11, 5, GREEN)               # hood
    c.rect(8, 8, 4, 4, GREEN)                # side flaps
    c.rect(17, 8, 4, 4, GREEN)
    c.rect(9, 9, 11, 1, GREEN_D)
    c.set(12, 12, INK)
    c.set(17, 12, INK)

    # tunic with a belt
    c.rect(11, 16, 8, 9, GREEN)
    c.rect(11, 16, 8, 2, GREEN_D)
    c.rect(10, 21, 10, 2, LEATHER)
    c.rect(13, 19, 4, 4, C("6fae72"))        # chest lacing

    # arms: one holding the bow, one drawing the string
    c.line(11, 18, 8, 21, SKIN, 2)
    c.line(19, 18, 24, 19, SKIN, 2)

    # legs
    c.line(13, 25, 12, 34, GREEN_D, 3)
    c.line(17, 25, 19, 34, GREEN_D, 3)
    c.rect(9, 34, 6, 3, LEATHER_DK)
    c.rect(17, 34, 6, 3, LEATHER_DK)

    return _finish(c)


def goblin():
    """Goblin - small, fast, hunched, wooden club."""
    c = Canvas(W, H)

    # club
    c.rect(23, 14, 3, 12, WOOD)
    c.rect(22, 12, 5, 4, WOOD_DK)

    # ears
    c.line(9, 14, 4, 10, GOB_GREEN, 2)
    c.line(20, 14, 25, 10, GOB_GREEN, 2)

    # head (large relative to body - reads as "small creature")
    c.disc(14, 15, 6, GOB_GREEN)
    c.rect(10, 12, 9, 2, GOB_GREEN_D)    # brow
    c.rect(11, 14, 2, 2, C("f2d05a"))     # yellow eyes...
    c.rect(17, 14, 2, 2, C("f2d05a"))
    c.set(12, 15, INK)                    # ...with dark slit pupils
    c.set(17, 15, INK)
    c.rect(13, 19, 4, 1, CLOTH_W)        # teeth

    # hunched body
    c.rect(11, 22, 8, 7, GOB_GREEN)
    c.rect(11, 25, 8, 2, LEATHER)        # loincloth

    # arms
    c.line(11, 23, 8, 27, GOB_GREEN, 2)
    c.line(19, 23, 23, 25, GOB_GREEN, 2)

    # stubby legs
    c.line(13, 29, 12, 34, GOB_GREEN_D, 3)
    c.line(17, 29, 19, 34, GOB_GREEN_D, 3)
    c.rect(9, 34, 6, 3, GOB_GREEN_D)
    c.rect(17, 34, 6, 3, GOB_GREEN_D)

    return _finish(c)


def brute():
    """Goblin Brute - big, slow, armoured shoulder, huge club."""
    c = Canvas(W, H)

    # huge club
    c.rect(24, 10, 4, 18, WOOD)
    c.rect(22, 6, 8, 7, WOOD_DK)
    c.set(23, 8, STEEL)                  # studs
    c.set(28, 9, STEEL)
    c.set(25, 11, STEEL)

    # small head on a big body
    c.disc(14, 10, 4, BRUTE_GRN)
    c.line(10, 9, 7, 6, BRUTE_GRN, 2)    # ears
    c.line(19, 9, 22, 6, BRUTE_GRN, 2)
    c.set(12, 10, C("d8452f"))           # angry red eyes
    c.set(17, 10, C("d8452f"))
    c.rect(12, 13, 6, 1, CLOTH_W)        # tusks

    # bulky torso
    c.rect(8, 15, 15, 12, BRUTE_GRN)
    c.rect(8, 15, 15, 3, BRUTE_GRN_D)
    c.rect(7, 15, 6, 5, STEEL_DK)        # pauldron
    c.rect(8, 16, 4, 2, STEEL)
    c.rect(8, 23, 15, 3, BRUTE_RED)      # belt

    # thick arms
    c.line(8, 19, 5, 25, BRUTE_GRN, 3)
    c.line(22, 19, 25, 24, BRUTE_GRN, 3)

    # stumpy legs
    c.line(12, 27, 11, 34, BRUTE_GRN_D, 4)
    c.line(19, 27, 20, 34, BRUTE_GRN_D, 4)
    c.rect(7, 34, 8, 4, LEATHER_DK)
    c.rect(17, 34, 8, 4, LEATHER_DK)

    return _finish(c)


def shaman():
    """Goblin Shaman - frail, hooded, bone staff. Heals its friends."""
    c = Canvas(W, H)

    PURPLE = C("6b4b8a")
    PURPLE_D = C("46305c")
    BONE = C("ddd6c0")

    # bone staff with a skull on top
    c.rect(24, 12, 2, 16, WOOD_DK)
    c.disc(25, 9, 3, BONE)               # skull
    c.set(24, 9, INK)
    c.set(26, 9, INK)
    c.rect(24, 11, 3, 1, INK)
    c.line(21, 14, 29, 12, BONE, 1)      # hanging charms

    # hood, worn low over the face
    c.disc(14, 14, 6, PURPLE)
    c.rect(8, 10, 13, 6, PURPLE)
    c.rect(8, 8, 13, 3, PURPLE_D)
    c.ellipse(14, 16, 4, 3, C("2a1f38"))  # shadowed face
    c.set(12, 16, C("8ad148"))            # glowing green eyes
    c.set(16, 16, C("8ad148"))

    # ears poking through
    c.line(8, 13, 5, 10, GOB_GREEN, 2)
    c.line(20, 13, 23, 10, GOB_GREEN, 2)

    # robe
    c.rect(10, 21, 9, 8, PURPLE)
    c.rect(9, 27, 12, 5, PURPLE_D)
    c.rect(13, 21, 3, 8, C("8a68ab"))     # centre panel
    c.rect(9, 24, 11, 2, GOLD)            # cord belt

    # arms
    c.line(10, 22, 7, 26, GOB_GREEN, 2)
    c.line(19, 22, 24, 24, GOB_GREEN, 2)

    c.rect(10, 32, 4, 2, LEATHER_DK)
    c.rect(17, 32, 4, 2, LEATHER_DK)

    return _finish(c)


def warchief():
    """Goblin Warchief - the boss. Horned helm, cape, enormous axe."""
    c = Canvas(W, H)

    CAPE = C("8f2f34")
    CAPE_D = C("5e1e22")
    IRON = C("9aa3b5")
    IRON_D = C("636b7d")

    # cape behind everything
    c.rect(4, 16, 24, 14, CAPE_D)
    c.rect(6, 15, 20, 10, CAPE)

    # great axe
    c.rect(25, 8, 3, 22, WOOD_DK)
    c.ellipse(24, 11, 6, 7, IRON)         # axe head
    c.ellipse(27, 11, 4, 6, C("14131c", 0))
    c.rect(23, 6, 6, 2, IRON_D)

    # horned helmet
    c.disc(14, 12, 5, BRUTE_GRN)
    c.rect(9, 6, 11, 5, IRON_D)
    c.rect(9, 10, 11, 2, IRON)
    c.line(9, 8, 4, 4, IRON, 2)           # horns
    c.line(20, 8, 25, 4, IRON, 2)
    c.set(11, 13, C("ff5a3c"))            # burning eyes
    c.set(12, 13, C("ff5a3c"))
    c.set(17, 13, C("ff5a3c"))
    c.set(16, 13, C("ff5a3c"))
    c.rect(11, 16, 8, 1, CLOTH_W)         # tusks

    # armoured torso
    c.rect(7, 18, 16, 12, BRUTE_GRN)
    c.rect(7, 18, 16, 4, IRON_D)          # breastplate
    c.rect(9, 19, 12, 2, IRON)
    c.rect(13, 21, 4, 4, GOLD)            # chieftain medallion
    c.rect(7, 26, 16, 3, C("3a2a20"))     # belt

    # heavy arms
    c.line(7, 22, 4, 28, BRUTE_GRN, 3)
    c.line(23, 22, 26, 26, BRUTE_GRN, 3)

    c.line(12, 30, 11, 35, BRUTE_GRN_D, 4)
    c.line(19, 30, 20, 35, BRUTE_GRN_D, 4)
    c.rect(7, 35, 8, 3, LEATHER_DK)
    c.rect(17, 35, 8, 3, LEATHER_DK)

    return _finish(c)


# ---------------------------------------------------------------------------
#  STATUS ICONS   16 x 16
# ---------------------------------------------------------------------------

def icon_weak():
    """Weakened - a snapped-off blade."""
    c = Canvas(16, 16)
    c.line(4, 12, 8, 7, C("9aa3b5"), 2)   # lower half of a broken sword
    c.rect(3, 11, 4, 2, LEATHER)          # grip
    c.line(10, 5, 12, 3, C("9aa3b5"), 2)  # snapped tip, floating away
    c.set(9, 6, C("c4453a"))
    c.set(10, 7, C("c4453a"))
    return _finish(c)


def icon_rally():
    """Rallied - an upward arrow."""
    c = Canvas(16, 16)
    c.line(8, 3, 3, 8, C("e8763c"), 2)
    c.line(8, 3, 13, 8, C("e8763c"), 2)
    c.rect(7, 3, 3, 10, C("e8763c"))
    c.set(8, 4, C("ffc48a"))
    return _finish(c)



def icon_poison():
    c = Canvas(16, 16)
    c.disc(8, 10, 4, POISON)             # droplet body
    c.line(8, 2, 8, 7, POISON, 3)        # droplet tip
    c.line(8, 3, 8, 5, POISON, 1)
    c.set(6, 9, C("d8f5b0"))             # highlight
    c.set(6, 8, C("d8f5b0"))
    return _finish(c)


def icon_stun():
    c = Canvas(16, 16)
    for (x, y, r) in ((4, 5, 2), (11, 4, 2), (8, 11, 2)):
        c.disc(x, y, r, STUN_Y)
        c.set(x, y, C("fff4c2"))
    c.line(4, 5, 11, 4, C("f2d05a", 160), 1)
    c.line(11, 4, 8, 11, C("f2d05a", 160), 1)
    return _finish(c)


def icon_guard():
    c = Canvas(16, 16)
    c.rect(3, 2, 10, 7, SHIELD_B)
    c.ellipse(8, 9, 5, 5, SHIELD_B)
    c.rect(7, 4, 2, 8, CLOTH_W)          # cross
    c.rect(5, 6, 6, 2, CLOTH_W)
    return _finish(c)


def icon_crit():
    c = Canvas(16, 16)
    for a in range(8):
        import math
        r = 6 if a % 2 == 0 else 3
        x = int(8 + r * math.cos(a * math.pi / 4))
        y = int(8 + r * math.sin(a * math.pi / 4))
        c.line(8, 8, x, y, GOLD, 2)
    c.disc(8, 8, 2, C("fff4c2"))
    return _finish(c)


# ---------------------------------------------------------------------------
#  UI  -  9-slice panels and buttons
# ---------------------------------------------------------------------------

def nine_slice(fill, border, highlight=None, size=24, corner_dark=None):
    """A bordered box designed to be stretched by Godot's NinePatchRect."""
    c = Canvas(size, size)
    c.rect(0, 0, size, size, fill)
    c.frame(0, 0, size, size, border)
    c.frame(1, 1, size - 2, size - 2, corner_dark or border)
    if highlight:
        c.rect(2, 2, size - 4, 1, highlight)      # inner top highlight
    return c


def panel():
    return nine_slice(C("1e1d2a"), C("3d3a52"), C("55516e"))


def panel_dark():
    return nine_slice(C("14131c"), C("2c2a3c"), None)


def button_normal():
    return nine_slice(C("343150"), C("57527e"), C("6f68a0"))


def button_hover():
    return nine_slice(C("454170"), C("7d76ad"), C("9b93c9"))


def button_pressed():
    return nine_slice(C("262341"), C("47426a"), None)


def button_disabled():
    return nine_slice(C("22212c"), C("343141"), None)


def bar_frame():
    c = Canvas(16, 12)
    c.rect(0, 0, 16, 12, C("0d0c12"))
    c.frame(0, 0, 16, 12, C("46425e"))
    return c


# ---------------------------------------------------------------------------
#  BACKGROUND   320 x 180  (scales to 1280x720 at 4x, nearest-neighbour)
# ---------------------------------------------------------------------------

def arena():
    c = Canvas(320, 180, C("1a1826"))

    # --- back wall: stone brickwork, darker towards the top
    brick_h, brick_w = 12, 30
    for row, y in enumerate(range(0, 118, brick_h)):
        shade = 0.55 + 0.45 * (y / 118.0)
        base = (int(58 * shade), int(54 * shade), int(76 * shade), 255)
        mortar = (int(30 * shade), int(28 * shade), int(42 * shade), 255)
        offset = 0 if row % 2 == 0 else brick_w // 2
        for x in range(-brick_w, 320 + brick_w, brick_w):
            c.rect(x + offset, y, brick_w - 2, brick_h - 2, base)
            c.rect(x + offset, y + brick_h - 2, brick_w, 2, mortar)
            c.rect(x + offset + brick_w - 2, y, 2, brick_h, mortar)

    # --- floor
    c.rect(0, 118, 320, 62, C("2e2a3d"))
    c.rect(0, 118, 320, 3, C("454059"))          # lip where wall meets floor
    for i, y in enumerate(range(124, 180, 11)):  # receding flagstones
        c.rect(0, y, 320, 1, C("241f31"))
        step = 26 + i * 6
        for x in range(-10 + (i % 2) * (step // 2), 330, step):
            c.rect(x, y, 1, 11, C("241f31"))

    # --- two torches
    #
    # The light spill is a SMOOTH radial falloff computed per pixel, not a stack
    # of translucent discs. Discs were the first attempt and they looked awful:
    # at 4x scale each one became a 160px circle with a hard visible edge, so the
    # wall had two enormous brown blobs painted on it. Gradients need to actually
    # be gradients.
    for tx in (46, 274):
        radius = 38
        for y in range(max(0, 30 - radius), min(118, 30 + radius)):
            for x in range(max(0, tx - radius), min(320, tx + radius)):
                d = (((x - tx) ** 2 + (y - 32) ** 2) ** 0.5) / radius
                if d >= 1.0:
                    continue
                falloff = (1.0 - d) ** 2.2        # quadratic: bright core, soft edge
                c.blend(x, y, (255, 176, 96, int(44 * falloff)))

        # short iron bracket and bowl
        c.rect(tx - 1, 40, 3, 12, C("3a3446"))
        c.rect(tx - 4, 36, 9, 4, C("55506b"))
        c.rect(tx - 3, 35, 7, 1, C("6d6788"))

        # the flame itself, drawn OPAQUE and in layers so it actually reads as
        # fire. The previous version used translucent discs and washed out into
        # a grey smudge at a distance.
        c.ellipse(tx, 30, 4, 6, C("e0561f"))      # outer
        c.ellipse(tx, 30, 3, 5, C("ff9a3c"))      # mid
        c.ellipse(tx, 29, 2, 3, C("ffd47a"))      # inner
        c.rect(tx, 24, 1, 4, C("ffe9b0"))         # tip licking upward
        c.set(tx, 28, C("fff6dc"))

    # --- vignette: darken the edges so the fighters pop
    for y in range(180):
        for x in range(320):
            dx = abs(x - 160) / 160.0
            dy = abs(y - 90) / 90.0
            d = (dx * dx + dy * dy) ** 0.5
            if d > 0.62:
                c.blend(x, y, (10, 8, 18, min(150, int((d - 0.62) * 260))))

    return c


# ---------------------------------------------------------------------------
#  Preview  -  render to the terminal so the art can be checked without a viewer
# ---------------------------------------------------------------------------

RAMP = " .:-=+*#%@"


def preview(c, name, max_w=64):
    step = max(1, c.w // max_w)
    print(f"\n--- {name}  ({c.w}x{c.h}) ---")
    for y in range(0, c.h, step * 2):
        line = ""
        for x in range(0, c.w, step):
            r, g, b, a = c.get(x, y)
            if a < 40:
                line += " "
            else:
                lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0
                line += RAMP[min(len(RAMP) - 1, int(lum * (len(RAMP) - 1)) + 1)]
        print("  |" + line + "|")


# ---------------------------------------------------------------------------

SPRITES = {
    "warrior": warrior,
    "medic": medic,
    "archer": archer,
    "goblin": goblin,
    "brute": brute,
    "shaman": shaman,
    "warchief": warchief,
}

MISC = {
    "icon_poison": icon_poison,
    "icon_stun": icon_stun,
    "icon_guard": icon_guard,
    "icon_weak": icon_weak,
    "icon_rally": icon_rally,
    "icon_crit": icon_crit,
    "ui_panel": panel,
    "ui_panel_dark": panel_dark,
    "ui_button": button_normal,
    "ui_button_hover": button_hover,
    "ui_button_pressed": button_pressed,
    "ui_button_disabled": button_disabled,
    "ui_bar_frame": bar_frame,
    "bg_arena": arena,
}


def main():
    show = "--preview" in sys.argv
    os.makedirs(OUT, exist_ok=True)
    written = []

    for name, fn in SPRITES.items():
        c = fn()
        written.append(c.save(os.path.join(OUT, f"{name}.png")))
        # defeated variant: toppled over and drained of colour
        written.append(c.desaturated().rotated_cw().save(
            os.path.join(OUT, f"{name}_down.png")))
        if show:
            preview(c, name, max_w=32)

    for name, fn in MISC.items():
        c = fn()
        written.append(c.save(os.path.join(OUT, f"{name}.png")))
        if show and name.startswith(("icon", "bg")):
            preview(c, name, max_w=64 if name.startswith("bg") else 16)

    print(f"\nWrote {len(written)} PNGs to {os.path.normpath(OUT)}")
    for p in sorted(written):
        print("   ", os.path.basename(p))


if __name__ == "__main__":
    main()

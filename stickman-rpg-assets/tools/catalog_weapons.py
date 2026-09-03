"""Weapon item icons.

Same drawing code as the in-hand weapons - only the angle and the palette
change - so an item icon can never disagree with what the hero is holding.
"""

from pixelart import Canvas
import parts
from rig import (INK, LIGHT, DARK, STEEL, STEEL_DK, WOOD, WOOD_DK, LEATHER,
                 LEATHER_DK, GOLD, BLOOD, POISON, STUN_Y, BONE, CLOTH_W,
                 C, shift)

ICON = 24

RARITY = {
    "common":    dict(tint=None,          glow=None,          order=0),
    "uncommon":  dict(tint=C("7dae4c"),   glow=C("7dae4c"),   order=1),
    "rare":      dict(tint=C("4d8fd6"),   glow=C("4d8fd6"),   order=2),
    "epic":      dict(tint=C("a86fd6"),   glow=C("a86fd6"),   order=3),
    "legendary": dict(tint=C("e0a13c"),   glow=C("e0a13c"),   order=4),
}

# metal / wood palettes reused across items
IRON    = C("9aa3b0"); IRON_D  = C("6d7482")
STEELY  = STEEL
DARKSTL = C("6a6f80")
GOLDM   = C("e0c46c")
EMBER   = C("e07a3c")
FROST   = C("8fd4e8")
VOID    = C("9b6fd6")
VERDANT = C("8ad148")
CRIMSON = C("c4453a")
BONEM   = BONE


def W(name, label, kind, rarity, slot, blurb, **kw):
    return dict(name=name, label=label, kind=kind, rarity=rarity, slot=slot,
                blurb=blurb, kw=kw)


WEAPONS = [
    # -- swords ------------------------------------------------------------
    W("rusty_shortsword", "Rusty Shortsword", "sword", "common", "1h",
      "Chipped, pitted, and technically still a sword.",
      metal=C("8a7f6a"), trim=C("6b5f4a")),
    W("iron_sword", "Iron Sword", "sword", "common", "1h",
      "The default. Reliable and unremarkable.", metal=IRON, trim=IRON_D),
    W("steel_longsword", "Steel Longsword", "sword", "uncommon", "1h",
      "Properly forged. Holds an edge through a whole floor.",
      metal=STEELY, trim=GOLDM),
    W("flamebrand", "Flamebrand", "sword", "rare", "1h",
      "Sets the struck target alight for three turns.",
      metal=C("f0a05a"), trim=CRIMSON),
    W("frostbite", "Frostbite", "sword", "rare", "1h",
      "Slows anything it cuts. Never needs cleaning.",
      metal=FROST, trim=C("4d8fd6")),
    W("void_edge", "Void Edge", "sword", "legendary", "1h",
      "Ignores armour. Costs a little HP every swing.",
      metal=VOID, trim=C("2e2440")),
    # -- katana ------------------------------------------------------------
    W("tempered_katana", "Tempered Katana", "katana", "uncommon", "1h",
      "Folded steel. High crit, low damage floor.",
      metal=C("c9d2de"), trim=C("2e3444")),
    W("bloodfang", "Bloodfang", "katana", "epic", "1h",
      "Heals the wielder for a share of the damage dealt.",
      metal=C("e0a0a0"), trim=CRIMSON),
    # -- greatswords -------------------------------------------------------
    W("iron_greatsword", "Iron Greatsword", "greatsword", "common", "2h",
      "Two hands, one target, no subtlety.", metal=IRON, trim=IRON_D),
    W("dawnbreaker", "Dawnbreaker", "greatsword", "legendary", "2h",
      "Cleaves the front rank and blinds the undead.",
      metal=C("f7e6b0"), trim=GOLDM),
    # -- daggers -----------------------------------------------------------
    W("bandit_dirk", "Bandit's Dirk", "dagger", "common", "1h",
      "Quick, cheap, and easy to hide.", metal=IRON, trim=LEATHER),
    W("poison_fang", "Poison Fang", "dagger", "uncommon", "1h",
      "Applies a stack of poison on every hit.",
      metal=VERDANT, trim=C("4f7530")),
    W("shadowstep", "Shadowstep", "dagger", "epic", "1h",
      "Guarantees a critical hit from stealth.",
      metal=C("8a86a8"), trim=C("2e2440")),
    # -- axes --------------------------------------------------------------
    W("hand_axe", "Hand Axe", "axe", "common", "1h",
      "Doubles as a tool. Chops doors as well as orcs.",
      metal=IRON, wood=WOOD),
    W("battle_axe", "Battle Axe", "axe", "uncommon", "1h",
      "Heavier head, deeper bite.", metal=STEELY, wood=WOOD_DK),
    W("skullsplitter", "Skullsplitter", "axe", "rare", "1h",
      "Bonus damage against anything with a skull.",
      metal=BONEM, wood=C("5e3f24")),
    W("executioner", "Executioner", "greataxe", "epic", "2h",
      "Instantly finishes an enemy below a quarter health.",
      metal=C("d0d6e0"), wood=C("3d2a1c")),
    W("worldcleaver", "Worldcleaver", "greataxe", "legendary", "2h",
      "Hits every enemy. Takes a turn to wind up.",
      metal=C("e8c060"), wood=C("4a2f20")),
    # -- blunt -------------------------------------------------------------
    W("oak_club", "Oak Club", "club", "common", "1h",
      "A heavy stick. Surprisingly effective.", wood=WOOD_DK),
    W("spiked_club", "Spiked Club", "club", "uncommon", "1h",
      "A heavy stick with opinions.", wood=C("5e3f24"), spikes=True),
    W("iron_mace", "Iron Mace", "mace", "common", "1h",
      "Crushes armour instead of cutting it.", metal=IRON, wood=LEATHER),
    W("morningstar", "Morningstar", "flail", "rare", "1h",
      "Swings around shields. Cannot be parried.",
      metal=STEELY, wood=LEATHER_DK),
    W("war_hammer", "War Hammer", "hammer", "uncommon", "1h",
      "Stuns on a critical hit.", metal=STEELY, wood=WOOD),
    W("thunder_maul", "Thunder Maul", "hammer", "epic", "2h",
      "Shockwave hits the two enemies beside the target.",
      metal=C("bcd8f0"), wood=C("3d3a52")),
    # -- polearms ----------------------------------------------------------
    W("hunting_spear", "Hunting Spear", "spear", "common", "2h",
      "Reach. Strikes from the second rank.", metal=IRON, wood=WOOD),
    W("halberd", "Halberd", "spear", "rare", "2h",
      "Stops a charge dead.", metal=STEELY, wood=WOOD_DK),
    W("tidecaller", "Tidecaller", "trident", "epic", "2h",
      "Three prongs, three chances to crit.",
      metal=C("7fd0d8"), wood=C("2f6f7a")),
    W("reaper_scythe", "Reaper's Scythe", "scythe", "rare", "2h",
      "Damage scales with how many enemies have died.",
      metal=C("cfd6e0"), wood=C("3a2f28")),
    W("soul_harvester", "Soul Harvester", "scythe", "legendary", "2h",
      "Every kill grants a free extra turn.",
      metal=C("a8f0c0"), wood=C("22301f")),
    # -- magic -------------------------------------------------------------
    W("apprentice_staff", "Apprentice Staff", "staff", "common", "2h",
      "A stick with a rock on it. It does work.",
      wood=WOOD, gem=C("6fa8dc")),
    W("arcane_staff", "Arcane Staff", "staff", "rare", "2h",
      "Spells cost one less mana.", wood=C("5a4a63"), gem=VOID),
    W("lich_staff", "Staff of the Lich", "staff", "legendary", "2h",
      "Raises the last enemy you killed as an ally.",
      wood=C("2f4038"), gem=VERDANT),
    W("oak_wand", "Oak Wand", "wand", "common", "1h",
      "Beginner's focus. Cheap to replace.", wood=WOOD_DK, gem=C("6fa8dc")),
    W("ember_wand", "Ember Wand", "wand", "uncommon", "1h",
      "Small, fast fireballs.", wood=C("4a3222"), gem=EMBER),
    W("grimoire", "Grimoire", "tome", "uncommon", "offhand",
      "Holds one prepared spell you can cast for free.",
      cover=C("6b2f4a"), page=C("e8e4dc")),
    W("codex_of_light", "Codex of Light", "tome", "epic", "offhand",
      "Heals the whole party at the end of each turn.",
      cover=C("e8e4dc"), page=C("f7e6b0")),
    W("void_orb", "Void Orb", "orb", "legendary", "offhand",
      "Reflects a share of magic damage back at the caster.",
      glow=VOID),
    # -- ranged ------------------------------------------------------------
    W("short_bow", "Short Bow", "bow", "common", "2h",
      "Fast draw, modest damage.", wood=WOOD, string=CLOTH_W),
    W("elven_longbow", "Elven Longbow", "bow", "rare", "2h",
      "Always strikes first in the round.",
      wood=C("b9a07a"), string=C("d8f0c0")),
    W("hand_crossbow", "Hand Crossbow", "crossbow", "uncommon", "1h",
      "Punches through shields.", wood=WOOD_DK, metal=IRON),
    W("siege_crossbow", "Siege Crossbow", "crossbow", "epic", "2h",
      "Enormous damage, fires every other turn.",
      wood=C("3d2a1c"), metal=C("d0d6e0")),
    # -- exotic ------------------------------------------------------------
    W("iron_claws", "Iron Claws", "claw", "common", "1h",
      "Two attacks per turn at half damage each.",
      metal=IRON, glove=LEATHER_DK),
    W("dragon_talons", "Dragon Talons", "claw", "epic", "1h",
      "Each hit shreds one point of armour, permanently.",
      metal=C("e0a13c"), glove=C("6b2f28")),
    W("serpent_whip", "Serpent Whip", "whip", "rare", "1h",
      "Hits the back rank without moving.", leather=C("6aa834")),
    W("everburning_torch", "Everburning Torch", "torch", "uncommon", "offhand",
      "Lights the dungeon and frightens the undead.", wood=WOOD_DK),
    W("wooden_shield", "Wooden Shield", "shield", "common", "offhand",
      "Blocks the first hit of every fight.", face=WOOD, boss=IRON),
    W("tower_shield", "Tower Shield", "shield", "rare", "offhand",
      "Covers an adjacent ally as well as yourself.",
      face=C("6b7078"), boss=GOLDM),
]


# Some shapes read better at a different angle or size as an inventory icon
# than they do in a fist: a bow is drawn across the aim line, so upright in
# the hand means sideways in the panel.
ICON_ANGLE = {"bow": 90, "crossbow": 90, "whip": 200, "claw": 140}
ICON_SCALE = {"claw": 2.1, "wand": 1.55, "orb": 1.7, "dagger": 1.3,
              "shield": 1.25, "tome": 1.3, "torch": 1.25, "club": 0.95,
              "sword": 1.05, "katana": 1.05}


def _bbox(c):
    xs = [x for y in range(c.h) for x in range(c.w) if c.get(x, y)[3]]
    ys = [y for y in range(c.h) for x in range(c.w) if c.get(x, y)[3]]
    return (min(xs), min(ys), max(xs), max(ys)) if xs else (0, 0, 0, 0)


def render_weapon(entry, size=ICON, glow=True):
    """Draw big, measure, then centre-crop - so every icon is optically centred
    whatever its shape, without a per-weapon offset table."""
    big = 64
    c = Canvas(big, big)
    k = entry["kind"]
    parts.draw_weapon(c, k, big // 2, big // 2 + 14, ICON_ANGLE.get(k, 180),
                      scale=ICON_SCALE.get(k, 1.0), **entry["kw"])
    c.shade(LIGHT, DARK)
    c.outline(INK)
    x0, y0, x1, y1 = _bbox(c)
    cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
    ox, oy = cx - size // 2, cy - size // 2
    out = Canvas(size, size)
    g = RARITY[entry["rarity"]]["glow"]
    if glow and g:
        for j in range(size):
            for i in range(size):
                d = ((i - size / 2) ** 2 + (j - size / 2) ** 2) ** 0.5
                a = max(0, 1 - d / (size * 0.52))
                if a > 0:
                    out.blend(i, j, (g[0], g[1], g[2], int(95 * a * a)))
    for y in range(size):
        for x in range(size):
            sx, sy = x + ox, y + oy
            if 0 <= sx < big and 0 <= sy < big:
                p = c.px[sy * big + sx]
                if p[3]:
                    out.px[y * size + x] = p
    return out

"""Build the whole asset pack: PNGs, strips, atlases, manifest.

    python tools/build_assets.py [outdir]

Everything here is OUTPUT. To change the art, change the catalogues and
re-run - that is the point of generating it.
"""

import json
import os
import sys

from pixelart import Canvas
import rig
import catalog_chars as chars
import catalog_weapons as cwep
import catalog_dungeon as cdun
import catalog_fx as cfx
import sheet as sh

FPS = {"idle": 6, "walk": 10, "attack": 12, "hurt": 14, "death": 8}
ANIM_ORDER = ["idle", "walk", "attack", "hurt", "death"]


def ensure(*parts):
    p = os.path.join(*parts)
    os.makedirs(p, exist_ok=True)
    return p


def atlas(canvases, cols, path, cell=None):
    """Pack canvases into a grid atlas. Returns (path, cols, cw, ch)."""
    cw = cell[0] if cell else max(c.w for c in canvases)
    ch = cell[1] if cell else max(c.h for c in canvases)
    rows = (len(canvases) + cols - 1) // cols
    out = Canvas(cols * cw, rows * ch)
    for i, c in enumerate(canvases):
        gx, gy = (i % cols) * cw, (i // cols) * ch
        ox, oy = (cw - c.w) // 2, (ch - c.h) // 2
        for y in range(c.h):
            for x in range(c.w):
                p = c.px[y * c.w + x]
                if p[3]:
                    out.px[(gy + oy + y) * out.w + gx + ox + x] = p
    out.save(path)
    return dict(path=os.path.basename(path), cols=cols, rows=rows,
                cell_w=cw, cell_h=ch, count=len(canvases))


def build(root):
    man = dict(
        name="Stickman RPG Asset Pack",
        version="1.0.0",
        generated_by="Python generators in tools/ - re-run to regenerate",
        license="Made for this project; use freely.",
        conventions=dict(
            character_size=[32, 40],
            weapon_icon_size=[24, 24],
            tile_size=[16, 16],
            fx_size=[32, 32],
            filtering="nearest-neighbour - do not smooth when scaling",
            recommended_scale="characters 3x, tiles 3-4x, integer scales only",
            anchor="characters stand on the bottom edge, horizontally centred",
            animation_fps=FPS,
        ),
        heroes=[], enemies=[], weapons=[], dungeon=[], fx=[], atlases={},
    )

    # ---- characters ------------------------------------------------------
    for group, entries in (("heroes", chars.HEROES), ("enemies", chars.ENEMIES)):
        gdir = ensure(root, "characters", group)
        idle_pool, idle_names = [], []
        for e in entries:
            name = e["name"]
            cdir = ensure(gdir, name)
            fdir = ensure(cdir, "frames")
            sets = rig.build_character(e["body"])
            anims = {}
            for a in ANIM_ORDER:
                fr = sets[a]
                for i, c in enumerate(fr):
                    c.save(os.path.join(fdir, f"{name}_{a}_{i:02d}.png"))
                sh.strip(fr, os.path.join(cdir, f"{name}_{a}_strip.png"))
                anims[a] = dict(frames=len(fr), fps=FPS[a],
                                strip=f"{name}_{a}_strip.png",
                                frame_w=fr[0].w, frame_h=fr[0].h)
            # drop-in singles matching this repo's existing asset names
            sets["idle"][0].save(os.path.join(cdir, f"{name}.png"))
            sets["death"][-1].save(os.path.join(cdir, f"{name}_down.png"))
            idle_pool.append(sets["idle"][0]); idle_names.append(name)

            rec = dict(name=name, label=e.get("label", name),
                       dir=f"characters/{group}/{name}",
                       blurb=e.get("blurb", ""), animations=anims,
                       still=f"{name}.png", defeated=f"{name}_down.png")
            if group == "heroes":
                rec["role"] = e.get("role", "")
                rec["weapon"] = e["body"].weapon
                rec["offhand"] = e["body"].offhand
            else:
                rec["tier"] = e.get("tier", 1)
                rec["tier_name"] = chars.TIER_NAMES.get(e.get("tier", 1), "")
                rec["weapon"] = e["body"].weapon
            man[group].append(rec)
        adir = ensure(root, "atlases")
        man["atlases"][f"{group}_idle"] = dict(
            **atlas(idle_pool, 10, os.path.join(adir, f"{group}_idle.png"),
                    cell=(32, 40)), order=idle_names)

    # every animation frame of every character, one atlas per group
    for group, entries in (("heroes", chars.HEROES), ("enemies", chars.ENEMIES)):
        pool, order = [], []
        for e in entries:
            sets = rig.build_character(e["body"])
            for a in ANIM_ORDER:
                for i, c in enumerate(sets[a]):
                    pool.append(c)
                    order.append(f"{e['name']}:{a}:{i}")
        adir = ensure(root, "atlases")
        man["atlases"][f"{group}_all"] = dict(
            **atlas(pool, 25, os.path.join(adir, f"{group}_all.png"),
                    cell=(32, 40)), order=order)

    # ---- weapons ---------------------------------------------------------
    wdir = ensure(root, "weapons")
    pool, order = [], []
    for w in cwep.WEAPONS:
        c = cwep.render_weapon(w)
        c.save(os.path.join(wdir, w["name"] + ".png"))
        pool.append(c); order.append(w["name"])
        man["weapons"].append(dict(
            name=w["name"], label=w["label"], archetype=w["kind"],
            rarity=w["rarity"], slot=w["slot"], blurb=w["blurb"],
            file=f"weapons/{w['name']}.png",
            rarity_order=cwep.RARITY[w["rarity"]]["order"]))
    man["atlases"]["weapons"] = dict(
        **atlas(pool, 12, os.path.join(root, "atlases", "weapons.png"),
                cell=(24, 24)), order=order)

    # ---- dungeon ---------------------------------------------------------
    ddir = ensure(root, "dungeon")
    pool, order = [], []
    for t in cdun.TILES:
        c = cdun.render_tile(t)
        c.save(os.path.join(ddir, t[0] + ".png"))
        pool.append(c); order.append(t[0])
        man["dungeon"].append(dict(name=t[0], category=t[2], seamless=t[3],
                                   blurb=t[4], file=f"dungeon/{t[0]}.png",
                                   size=[16, 16]))
    man["atlases"]["dungeon"] = dict(
        **atlas(pool, 11, os.path.join(root, "atlases", "dungeon.png"),
                cell=(16, 16)), order=order)

    # ---- fx --------------------------------------------------------------
    xdir = ensure(root, "fx")
    pool, order = [], []
    for e in cfx.EFFECTS:
        name = e[0]
        edir = ensure(xdir, name)
        fr = cfx.render_fx(e)
        for i, c in enumerate(fr):
            c.save(os.path.join(edir, f"{name}_{i:02d}.png"))
        sh.strip(fr, os.path.join(xdir, f"{name}_strip.png"))
        pool += fr; order += [f"{name}:{i}" for i in range(len(fr))]
        man["fx"].append(dict(name=name, blurb=e[2], frames=len(fr), fps=14,
                              strip=f"fx/{name}_strip.png",
                              dir=f"fx/{name}", size=[32, 32]))
    man["atlases"]["fx"] = dict(
        **atlas(pool, cfx.FRAMES, os.path.join(root, "atlases", "fx.png"),
                cell=(32, 32)), order=order)

    man["totals"] = dict(
        heroes=len(man["heroes"]), enemies=len(man["enemies"]),
        weapons=len(man["weapons"]), dungeon=len(man["dungeon"]),
        fx=len(man["fx"]),
        character_frames=sum(sum(a["frames"] for a in h["animations"].values())
                             for h in man["heroes"] + man["enemies"]),
        fx_frames=sum(f["frames"] for f in man["fx"]))
    with open(os.path.join(root, "manifest.json"), "w") as f:
        json.dump(man, f, indent=2)
    return man


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "out/stickman-rpg-assets"
    os.makedirs(out, exist_ok=True)
    m = build(out)
    print(json.dumps(m["totals"], indent=2))

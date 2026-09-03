"""Automated sprite QA. Cheap checks that catch the failures that actually
happen: art running off the edge of its cell, and empty frames."""


def edges(c):
    top = sum(1 for x in range(c.w) if c.get(x, 0)[3])
    bot = sum(1 for x in range(c.w) if c.get(x, c.h - 1)[3])
    left = sum(1 for y in range(c.h) if c.get(0, y)[3])
    right = sum(1 for y in range(c.h) if c.get(c.w - 1, y)[3])
    return dict(top=top, bottom=bot, left=left, right=right)


def opaque(c):
    return sum(1 for p in c.px if p[3])


def check(name, frames, allow_bottom=True, limit=1):
    """Report frames whose art is cut off by the cell edge."""
    bad = []
    for i, c in enumerate(frames):
        e = edges(c)
        if opaque(c) < 40:
            bad.append((name, i, "nearly empty"))
            continue
        for side in ("top", "left", "right"):
            if e[side] > limit:
                bad.append((name, i, f"clipped {side} ({e[side]}px)"))
        if not allow_bottom and e["bottom"] > limit:
            bad.append((name, i, f"clipped bottom ({e['bottom']}px)"))
    return bad

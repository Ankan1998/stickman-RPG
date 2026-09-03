"""Contact sheets + strip export. Uses Pillow only for previews, never for
the shipped assets - those stay dependency-free via pixelart.write_png."""
from PIL import Image
from pixelart import Canvas


def canvas_to_pil(c):
    img = Image.new("RGBA", (c.w, c.h))
    img.putdata([tuple(p) for p in c.px])
    return img


def strip(canvases, path=None):
    """Horizontal sprite sheet, 1 row."""
    if not canvases:
        return None
    w, h = canvases[0].w, canvases[0].h
    out = Canvas(w * len(canvases), h)
    for i, c in enumerate(canvases):
        for y in range(c.h):
            for x in range(c.w):
                out.px[y * out.w + i * w + x] = c.px[y * c.w + x]
    if path:
        out.save(path)
    return out


def contact(rows, path, scale=4, pad=4, bg=(38, 36, 48, 255), labels=None):
    """rows: list of lists of Canvas. Writes a scaled preview grid."""
    cols = max(len(r) for r in rows)
    cw = max(c.w for r in rows for c in r)
    ch = max(c.h for r in rows for c in r)
    W = cols * (cw * scale + pad) + pad
    H = len(rows) * (ch * scale + pad) + pad + (14 if labels else 0) * len(rows)
    img = Image.new("RGBA", (W, H), bg)
    y = pad
    for ri, row in enumerate(rows):
        x = pad
        for c in row:
            p = canvas_to_pil(c).resize((c.w * scale, c.h * scale), Image.NEAREST)
            img.alpha_composite(p, (x, y))
            x += cw * scale + pad
        y += ch * scale + pad + (14 if labels else 0)
    img.save(path)
    return path

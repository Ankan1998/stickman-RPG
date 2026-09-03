"""
Tiny pixel-art toolkit: a PNG writer and a drawing canvas.

No third-party libraries. PNG is a simple enough format to write by hand with
zlib and struct, which keeps this repo dependency-free.

Used by make_art.py, which is the actual source of truth for every image in
game/assets/. The PNGs are generated, not hand-drawn - so if you want to change
the art, change the script and re-run it.
"""

import struct
import zlib

TRANSPARENT = (0, 0, 0, 0)


# ---------------------------------------------------------------------------
#  PNG writing
# ---------------------------------------------------------------------------

def write_png(path, width, height, pixels):
    """pixels: flat list of (r,g,b,a) tuples, row-major, length width*height."""
    rows = []
    for y in range(height):
        row = bytearray()
        row.append(0)  # filter type 0 (None) for this scanline
        for x in range(width):
            r, g, b, a = pixels[y * width + x]
            row += bytes((r, g, b, a))
        rows.append(bytes(row))
    raw = b"".join(rows)

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")

    with open(path, "wb") as f:
        f.write(png)


def hex_rgba(code, alpha=255):
    """'6fb3d2' or '#6fb3d2' -> (r, g, b, a)."""
    code = code.lstrip("#")
    return (int(code[0:2], 16), int(code[2:4], 16), int(code[4:6], 16), alpha)


# ---------------------------------------------------------------------------
#  Canvas
# ---------------------------------------------------------------------------

class Canvas:
    def __init__(self, width, height, fill=TRANSPARENT):
        self.w = width
        self.h = height
        self.px = [fill] * (width * height)

    # -- basics ------------------------------------------------------------

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y * self.w + x]
        return TRANSPARENT

    def set(self, x, y, color):
        if 0 <= x < self.w and 0 <= y < self.h and color[3] > 0:
            self.px[y * self.w + x] = color

    def blend(self, x, y, color):
        """Alpha-composite `color` over whatever is already there."""
        if not (0 <= x < self.w and 0 <= y < self.h):
            return
        sr, sg, sb, sa = color
        if sa == 0:
            return
        if sa == 255:
            self.px[y * self.w + x] = color
            return
        dr, dg, db, da = self.px[y * self.w + x]
        a = sa / 255.0
        self.px[y * self.w + x] = (
            int(sr * a + dr * (1 - a)),
            int(sg * a + dg * (1 - a)),
            int(sb * a + db * (1 - a)),
            max(da, sa),
        )

    # -- shapes ------------------------------------------------------------

    def rect(self, x, y, w, h, color):
        for j in range(y, y + h):
            for i in range(x, x + w):
                self.set(i, j, color)

    def rect_blend(self, x, y, w, h, color):
        for j in range(y, y + h):
            for i in range(x, x + w):
                self.blend(i, j, color)

    def frame(self, x, y, w, h, color):
        self.rect(x, y, w, 1, color)
        self.rect(x, y + h - 1, w, 1, color)
        self.rect(x, y, 1, h, color)
        self.rect(x + w - 1, y, 1, h, color)

    def line(self, x0, y0, x1, y1, color, thickness=1):
        """Bresenham. thickness>1 draws a square brush."""
        dx, dy = abs(x1 - x0), abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx - dy
        while True:
            if thickness == 1:
                self.set(x0, y0, color)
            else:
                o = thickness // 2
                self.rect(x0 - o, y0 - o, thickness, thickness, color)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x0 += sx
            if e2 < dx:
                err += dx
                y0 += sy

    def disc(self, cx, cy, r, color):
        """Filled circle. Uses <= r*r + r for a rounder small-radius look."""
        limit = r * r + r
        for j in range(cy - r, cy + r + 1):
            for i in range(cx - r, cx + r + 1):
                if (i - cx) ** 2 + (j - cy) ** 2 <= limit:
                    self.set(i, j, color)

    def ring(self, cx, cy, r, color):
        inner = (r - 1) * (r - 1) + (r - 1)
        limit = r * r + r
        for j in range(cy - r, cy + r + 1):
            for i in range(cx - r, cx + r + 1):
                d = (i - cx) ** 2 + (j - cy) ** 2
                if inner < d <= limit:
                    self.set(i, j, color)

    def ellipse(self, cx, cy, rx, ry, color):
        for j in range(cy - ry, cy + ry + 1):
            for i in range(cx - rx, cx + rx + 1):
                if ((i - cx) / rx) ** 2 + ((j - cy) / ry) ** 2 <= 1.15:
                    self.set(i, j, color)

    # -- post-processing ---------------------------------------------------

    def outline(self, color, diagonal=False):
        """
        Add a 1px border around every opaque shape.

        This single step is what makes pixel art read clearly against any
        background, and it is why these sprites look deliberate rather than
        like coloured blobs.
        """
        neighbours = [(-1, 0), (1, 0), (0, -1), (0, 1)]
        if diagonal:
            neighbours += [(-1, -1), (1, -1), (-1, 1), (1, 1)]

        additions = []
        for y in range(self.h):
            for x in range(self.w):
                if self.get(x, y)[3] != 0:
                    continue
                for dx, dy in neighbours:
                    if self.get(x + dx, y + dy)[3] > 0:
                        additions.append((x, y))
                        break
        for x, y in additions:
            self.px[y * self.w + x] = color

    def shade(self, light, dark):
        """
        Cheap directional shading: brighten the top edge of each shape and
        darken the bottom edge. Gives flat colour a sense of volume for free.
        """
        edits = []
        for y in range(self.h):
            for x in range(self.w):
                c = self.get(x, y)
                if c[3] == 0:
                    continue
                above = self.get(x, y - 1)
                below = self.get(x, y + 1)
                if above[3] == 0 and below[3] != 0:
                    edits.append((x, y, light))
                elif below[3] == 0 and above[3] != 0:
                    edits.append((x, y, dark))
        for x, y, tint in edits:
            r, g, b, a = self.get(x, y)
            tr, tg, tb, ta = tint
            k = ta / 255.0
            self.px[y * self.w + x] = (
                min(255, int(r + (tr - r) * k)),
                min(255, int(g + (tg - g) * k)),
                min(255, int(b + (tb - b) * k)),
                a,
            )

    def flip_h(self):
        out = Canvas(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                out.px[y * self.w + (self.w - 1 - x)] = self.px[y * self.w + x]
        return out

    def rotated_cw(self):
        out = Canvas(self.h, self.w)
        for y in range(self.h):
            for x in range(self.w):
                out.px[x * out.w + (out.w - 1 - y)] = self.px[y * self.w + x]
        return out

    def desaturated(self, amount=0.85, darken=0.55):
        out = Canvas(self.w, self.h)
        for i, (r, g, b, a) in enumerate(self.px):
            if a == 0:
                continue
            grey = int(0.299 * r + 0.587 * g + 0.114 * b)
            out.px[i] = (
                int((r + (grey - r) * amount) * darken),
                int((g + (grey - g) * amount) * darken),
                int((b + (grey - b) * amount) * darken),
                a,
            )
        return out

    def save(self, path):
        write_png(path, self.w, self.h, self.px)
        return path

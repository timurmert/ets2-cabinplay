"""Writes app/CabinPlay.ico: the same picture the app draws for its tray icon
(MainForm.CreateIcon), at the sizes Windows asks for. Used for the app, the setup and the
uninstaller. Pure Python, no imaging library: each pixel is sampled 4x4 times.

    python tools/make_icon.py                 # the icon only
    python tools/make_icon.py --wizard DIR    # also the setup wizard's pictures, into DIR
"""
import math
import os
import struct
import sys
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "app", "CabinPlay.ico")
SIZES = (16, 20, 24, 32, 40, 48, 64, 256)
SAMPLES = 4

# In the 64x64 design space of MainForm.CreateIcon.
BODY = (2, 12, 60, 40, 9)
SCREEN = (6, 16, 52, 32, 6)
PLAY = ((27, 24), (27, 40), (41, 32))
BODY_RGB = (24, 24, 28)
GRADIENT = ((30, 60, 150), (150, 60, 170), 25.0)


def in_rounded(x, y, rect):
    left, top, w, h, r = rect
    if x < left or y < top or x > left + w or y > top + h:
        return False
    cx = min(max(x, left + r), left + w - r)
    cy = min(max(y, top + r), top + h - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def in_triangle(x, y, tri):
    (x1, y1), (x2, y2), (x3, y3) = tri

    def side(ax, ay, bx, by):
        return (x - bx) * (ay - by) - (ax - bx) * (y - by)

    d1, d2, d3 = side(x1, y1, x2, y2), side(x2, y2, x3, y3), side(x3, y3, x1, y1)
    return not ((d1 < 0 or d2 < 0 or d3 < 0) and (d1 > 0 or d2 > 0 or d3 > 0))


def gradient(x, y):
    # GDI+ LinearGradientBrush with an angle: 0 at one corner of the rectangle, 1 at the
    # opposite one, measured along the angle.
    left, top, w, h, _ = SCREEN
    a = math.radians(GRADIENT[2])
    t = ((x - left) * math.cos(a) + (y - top) * math.sin(a)) / (w * math.cos(a) + h * math.sin(a))
    t = min(max(t, 0.0), 1.0)
    c0, c1 = GRADIENT[0], GRADIENT[1]
    return tuple(c0[i] + (c1[i] - c0[i]) * t for i in range(3))


def icon_colour(x, y):
    """Colour of the icon at (x, y) in its 64x64 design space, or None outside it."""
    if in_triangle(x, y, PLAY):
        return (255, 255, 255)
    if in_rounded(x, y, SCREEN):
        return gradient(x, y)
    if in_rounded(x, y, BODY):
        return BODY_RGB
    return None


def render(size):
    """Returns rows of (r, g, b, a) from top to bottom, colours not premultiplied."""
    scale = 64.0 / size
    rows = []
    for py in range(size):
        row = []
        for px in range(size):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    rgb = icon_colour((px + (sx + 0.5) / SAMPLES) * scale, (py + (sy + 0.5) / SAMPLES) * scale)
                    if rgb is None:
                        continue
                    acc[0] += rgb[0]
                    acc[1] += rgb[1]
                    acc[2] += rgb[2]
                    acc[3] += 1
            n = SAMPLES * SAMPLES
            if acc[3]:
                row.append(tuple(round(c / acc[3]) for c in acc[:3]) + (round(255 * acc[3] / n),))
            else:
                row.append((0, 0, 0, 0))
        rows.append(row)
    return rows


def render_banner(width, height):
    """The tall picture on the setup's last page: the icon on dark blue. The setup fills
    the space around it with the same colour (WizardImageBackColor in packaging/setup.iss)."""
    top = bottom = (16, 22, 48)
    icon = width * 0.62
    left, upper = (width - icon) / 2, height * 0.38 - icon / 2
    scale = 64.0 / icon
    rows = []
    for py in range(height):
        t = py / (height - 1)
        back = tuple(top[i] + (bottom[i] - top[i]) * t for i in range(3))
        row = []
        for px in range(width):
            if not (left - 1 <= px <= left + icon + 1 and upper - 1 <= py <= upper + icon + 1):
                row.append(tuple(round(c) for c in back) + (255,))
                continue
            acc = [0.0, 0.0, 0.0]
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    rgb = icon_colour((px + (sx + 0.5) / SAMPLES - left) * scale,
                                      (py + (sy + 0.5) / SAMPLES - upper) * scale) or back
                    for i in range(3):
                        acc[i] += rgb[i]
            row.append(tuple(round(c / (SAMPLES * SAMPLES)) for c in acc) + (255,))
        rows.append(row)
    return rows


def bmp(rows):
    """A 32-bit bottom-up BMP file with straight alpha (Inno Setup: WizardImageAlphaFormat=defined)."""
    height, width = len(rows), len(rows[0])
    pixels = b"".join(bytes((b, g, r, a)) for row in reversed(rows) for (r, g, b, a) in row)
    info = struct.pack("<IiiHHIIiiII", 40, width, height, 1, 32, 0, len(pixels), 2835, 2835, 0, 0)
    return struct.pack("<2sIHHI", b"BM", 14 + len(info) + len(pixels), 0, 0, 14 + len(info)) + info + pixels


def write_wizard(folder):
    os.makedirs(folder, exist_ok=True)
    # Inno Setup picks the size closest to the display scaling: 100% and 200% here.
    for factor in (1, 2):
        for name, rows in (("wizard-small-%d.bmp" % (100 * factor), render(55 * factor)),
                           ("wizard-large-%d.bmp" % (100 * factor), render_banner(164 * factor, 314 * factor))):
            with open(os.path.join(folder, name), "wb") as f:
                f.write(bmp(rows))
    print("wrote the setup pictures to", folder)


def png(rows):
    size = len(rows)
    raw = b"".join(b"\0" + bytes(c for px in row for c in px) for row in rows)

    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def dib(rows):
    """32-bit BMP as icons store it: bottom-up BGRA, doubled height, then the AND mask."""
    size = len(rows)
    header = struct.pack("<IiiHHIIiiII", 40, size, size * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    pixels = b"".join(bytes((b, g, r, a)) for row in reversed(rows) for (r, g, b, a) in row)
    stride = ((size + 31) // 32) * 4
    mask = bytearray()
    for row in reversed(rows):
        bits = bytearray(stride)
        for x, px in enumerate(row):
            if px[3] == 0:
                bits[x // 8] |= 0x80 >> (x % 8)
        mask += bits
    return header + pixels + bytes(mask)


def main():
    images = []
    for size in SIZES:
        rows = render(size)
        images.append((size, png(rows) if size >= 256 else dib(rows)))
    out = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    for size, data in images:
        dim = 0 if size >= 256 else size
        out += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
    out += b"".join(data for _, data in images)
    with open(OUT, "wb") as f:
        f.write(out)
    print("wrote", OUT, len(out), "bytes")
    if len(sys.argv) == 3 and sys.argv[1] == "--wizard":
        write_wizard(sys.argv[2])


if __name__ == "__main__":
    main()

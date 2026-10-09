"""Software-renders a PMG to a PNG contact sheet (front / side / back views).

Used to eyeball generated geometry without launching the game. Triangles are
culled with the game's winding rule, so wrongly wound faces show up as holes.
"""
import math
import os
import struct
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scsmodel

MAT_COLORS = {0: (0.42, 0.43, 0.46), 1: (0.10, 0.10, 0.12), 2: (0.15, 0.45, 0.95), 3: (1.0, 0.0, 1.0)}


def png(path, w, h, rgb):
    raw = b"".join(b"\0" + bytes(rgb[y * w * 3:(y + 1) * w * 3]) for y in range(h))

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b""))


def render(model, yaw, pitch, size, scale):
    cy, sy, cp, sp = math.cos(yaw), math.sin(yaw), math.cos(pitch), math.sin(pitch)

    def view(p):
        x, y, z = p[0] + 0.02, p[1] - 0.04, p[2] - 0.1
        x, z = x * cy + z * sy, -x * sy + z * cy
        y, z = y * cp - z * sp, y * sp + z * cp
        return x, y, z

    def viewdir(n):
        x, y, z = n
        x, z = x * cy + z * sy, -x * sy + z * cy
        y, z = y * cp - z * sp, y * sp + z * cp
        return x, y, z
    w = h = size
    color = bytearray([235, 238, 242] * (w * h))
    depth = [-1e9] * (w * h)
    light = scsmodel_norm((0.35, 0.55, 0.75))
    for piece in model["pieces"]:
        base = MAT_COLORS.get(piece["material"], (0.8, 0.2, 0.8))
        pts = [view(p) for p in piece["position"]]
        nrm = [viewdir(n) for n in piece["normal"]]
        col = piece["color"]
        idx = piece["indices"]
        for t in range(0, len(idx), 3):
            a, b, c = idx[t:t + 3]
            (x0, y0, z0), (x1, y1, z1), (x2, y2, z2) = pts[a], pts[b], pts[c]
            area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
            if area >= 0:      # game rule: clockwise seen from the front is front-facing
                continue
            sx = [w / 2 + v * scale for v in (x0, x1, x2)]
            sy_ = [h / 2 - v * scale for v in (y0, y1, y2)]
            minx, maxx = max(int(min(sx)), 0), min(int(max(sx)) + 1, w - 1)
            miny, maxy = max(int(min(sy_)), 0), min(int(max(sy_)) + 1, h - 1)
            den = (sy_[1] - sy_[2]) * (sx[0] - sx[2]) + (sx[2] - sx[1]) * (sy_[0] - sy_[2])
            if abs(den) < 1e-12:
                continue
            for py in range(miny, maxy + 1):
                for px in range(minx, maxx + 1):
                    fx, fy = px + 0.5, py + 0.5
                    l0 = ((sy_[1] - sy_[2]) * (fx - sx[2]) + (sx[2] - sx[1]) * (fy - sy_[2])) / den
                    l1 = ((sy_[2] - sy_[0]) * (fx - sx[2]) + (sx[0] - sx[2]) * (fy - sy_[2])) / den
                    l2 = 1 - l0 - l1
                    if l0 < 0 or l1 < 0 or l2 < 0:
                        continue
                    z = l0 * z0 + l1 * z1 + l2 * z2
                    o = py * w + px
                    if z <= depth[o]:
                        continue
                    depth[o] = z
                    n = [l0 * nrm[a][i] + l1 * nrm[b][i] + l2 * nrm[c][i] for i in range(3)]
                    ln = math.sqrt(sum(v * v for v in n)) or 1
                    d = max(sum(n[i] / ln * light[i] for i in range(3)), 0.0)
                    shade = (0.35 + 0.75 * d) * (col[a][0] / 128.0)
                    if piece["material"] == 2 and piece["texcoord"]:
                        u = l0 * piece["texcoord"][a][0] + l1 * piece["texcoord"][b][0] + l2 * piece["texcoord"][c][0]
                        v = l0 * piece["texcoord"][a][1] + l1 * piece["texcoord"][b][1] + l2 * piece["texcoord"][c][1]
                        # u grows red, v grows green: checks the screen mapping orientation
                        rgbc = (0.15 + 0.85 * u, 0.15 + 0.85 * v, 0.6)
                        shade = 1.0
                    else:
                        rgbc = base
                    for i in range(3):
                        color[o * 3 + i] = max(0, min(255, int(rgbc[i] * shade * 255)))
    return color


def scsmodel_norm(v):
    l = math.sqrt(sum(x * x for x in v))
    return tuple(x / l for x in v)


def main():
    model = scsmodel.read_pmg(sys.argv[1])
    size, scale = 420, 1100
    views = [(0.0, 0.0), (math.radians(-55), math.radians(12)), (math.radians(90), 0.0),
             (math.radians(180), math.radians(10))]
    tiles = [render(model, yaw, pitch, size, scale) for yaw, pitch in views]
    sheet = bytearray()
    for y in range(size):
        for t in tiles:
            sheet += t[y * size * 3:(y + 1) * size * 3]
    png(sys.argv[2], size * len(tiles), size, sheet)
    print("wrote", sys.argv[2])


if __name__ == "__main__":
    main()

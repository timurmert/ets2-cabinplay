"""Reader/writer for SCS binary models (PMG v0x15 geometry + PMD v4 descriptor).

Only the subset needed for static (boneless) accessory models is supported.
"""
import math
import struct

TOKEN_CHARS = "\0" + "0123456789" + "abcdefghijklmnopqrstuvwxyz" + "_"


def token(s):
    v = 0
    for i, c in enumerate(s):
        v += TOKEN_CHARS.index(c) * (38 ** i)
    return v


def untoken(v):
    out = []
    while v:
        out.append(TOKEN_CHARS[v % 38])
        v //= 38
    return "".join(out)


# --------------------------------------------------------------------------- PMG

PMG_HEADER = struct.Struct("<B3s5iQ3ff6f10i")  # 112 bytes
PMG_PIECE = struct.Struct("<iiIii3ff6f10i")  # 100 bytes
PMG_PART = struct.Struct("<Q4i")  # 24 bytes
PMG_LOCATOR = struct.Struct("<Q3ff4fi")  # 44 bytes


def read_pmg(path):
    d = open(path, "rb").read()
    h = PMG_HEADER.unpack_from(d, 0)
    (ver, sig, n_piece, n_part, n_bone, weight_w, n_loc, skel_hash) = h[:8]
    bb_center, bb_diag, bb = h[8:11], h[11], h[12:18]
    (skel_off, parts_off, loc_off, pieces_off, sp_off, sp_size,
     vp_off, vp_size, ip_off, ip_size) = h[18:28]
    assert ver == 0x15 and sig == b"gmP", (ver, sig)
    m = dict(version=ver, bones=n_bone, weight_width=weight_w, skeleton_hash=skel_hash,
             bb_center=bb_center, bb_diag=bb_diag, bb=bb,
             offsets=dict(skeleton=skel_off, parts=parts_off, locators=loc_off,
                          pieces=pieces_off, strings=(sp_off, sp_size),
                          vertices=(vp_off, vp_size), indices=(ip_off, ip_size)),
             size=len(d), parts=[], locators=[], pieces=[])
    for i in range(n_part):
        name, pc, pi, lc, li = PMG_PART.unpack_from(d, parts_off + i * PMG_PART.size)
        m["parts"].append(dict(name=untoken(name), piece_count=pc, pieces_idx=pi,
                               locator_count=lc, locators_idx=li))
    for i in range(n_loc):
        v = PMG_LOCATOR.unpack_from(d, loc_off + i * PMG_LOCATOR.size)
        hookup = None
        if v[9] != -1:
            s = sp_off + v[9]
            hookup = d[s:d.index(b"\0", s)].decode()
        m["locators"].append(dict(name=untoken(v[0]), pos=v[1:4], scale=v[4],
                                  rot_wxyz=v[5:9], hookup=hookup))
    for i in range(n_piece):
        v = PMG_PIECE.unpack_from(d, pieces_off + i * PMG_PIECE.size)
        p = dict(edges=v[0], verts=v[1], texcoord_mask=v[2], texcoord_width=v[3],
                 material=v[4], bb_center=v[5:8], bb_diag=v[8], bb=v[9:15])
        names = ["stride", "position", "normal", "texcoord", "color", "factor",
                 "tangent", "bone_index", "bone_weight", "index"]
        p["offsets"] = dict(zip(names, v[15:25]))
        o, n = p["offsets"], p["verts"]
        # Attributes are interleaved; the stride is the gap between consecutive vertices.
        present = sorted(x for k, x in o.items()
                         if k not in ("stride", "index") and x != -1)
        p["present"] = [k for k in names if k not in ("stride", "index") and o[k] != -1]

        def col(off, fmt, stride):
            s = struct.Struct(fmt)
            return [s.unpack_from(d, off + j * stride) for j in range(n)]

        p["_first_attr_offset"] = present[0]
        m["pieces"].append(p)
    # Stride per piece = distance to next piece's first attribute (or pool end).
    starts = sorted(p["_first_attr_offset"] for p in m["pieces"])
    for p in m["pieces"]:
        idx = starts.index(p["_first_attr_offset"])
        end = starts[idx + 1] if idx + 1 < len(starts) else vp_off + vp_size
        p["stride"] = (end - p["_first_attr_offset"]) // p["verts"]
        o, n, st = p["offsets"], p["verts"], p["stride"]

        def col(off, fmt):
            s = struct.Struct(fmt)
            return [s.unpack_from(d, off + j * st) for j in range(n)]

        p["position"] = col(o["position"], "<3f")
        p["normal"] = col(o["normal"], "<3f")
        p["tangent"] = col(o["tangent"], "<4f") if o["tangent"] != -1 else None
        p["color"] = col(o["color"], "<4B") if o["color"] != -1 else None
        p["factor"] = col(o["factor"], "<4B") if o["factor"] != -1 else None
        p["texcoord"] = (col(o["texcoord"], "<%df" % (2 * p["texcoord_width"]))
                         if o["texcoord"] != -1 else None)
        p["indices"] = list(struct.unpack_from("<%dH" % p["edges"], d, o["index"]))
    return m


def _bbox(points):
    xs, ys, zs = zip(*points)
    lo = (min(xs), min(ys), min(zs))
    hi = (max(xs), max(ys), max(zs))
    center = tuple((a + b) / 2 for a, b in zip(lo, hi))
    diag = math.sqrt(sum((b - a) ** 2 for a, b in zip(lo, hi)))
    return center, diag, lo + hi


def write_pmg(path, pieces, locators=(), part_name="defaultpart"):
    """pieces: list of dict(material=int, position=[(x,y,z)], normal=[...],
    uv=[(u,v)], color=[(r,g,b,a)] bytes per vertex, indices=[...]).

    Layout mirrors the game's own static accessory models: header, parts, locators,
    pieces, string pool, interleaved vertex pool (position, normal, color, uv), indices.
    """
    n_piece, n_loc = len(pieces), len(locators)
    parts_off = PMG_HEADER.size
    loc_off = parts_off + PMG_PART.size
    pieces_off = loc_off + n_loc * PMG_LOCATOR.size
    sp_off = pieces_off + n_piece * PMG_PIECE.size

    strings = b""
    loc_recs = []
    for l in locators:
        hook = -1
        if l.get("hookup"):
            hook = len(strings)
            strings += l["hookup"].encode() + b"\0"
        loc_recs.append(PMG_LOCATOR.pack(token(l["name"]), *l["pos"], l.get("scale", 1.0),
                                         *l["rot_wxyz"], hook))
    vp_off = sp_off + len(strings)

    stride = 36
    vpool = b""
    layouts = []
    for p in pieces:
        base = vp_off + len(vpool)
        buf = bytearray()
        for i in range(len(p["position"])):
            buf += struct.pack("<3f", *p["position"][i])
            buf += struct.pack("<3f", *p["normal"][i])
            buf += struct.pack("<4B", *p["color"][i])
            buf += struct.pack("<2f", *p["uv"][i])
        assert len(buf) == stride * len(p["position"])
        vpool += bytes(buf)
        layouts.append(dict(position=base, normal=base + 12, color=base + 24,
                            texcoord=base + 28))

    ip_off = vp_off + len(vpool)
    ipool = b""
    for p, off in zip(pieces, layouts):
        assert max(p["indices"]) < len(p["position"]) < 65536
        off["index"] = ip_off + len(ipool)
        ipool += struct.pack("<%dH" % len(p["indices"]), *p["indices"])

    piece_recs = b""
    allpts = []
    for p, off in zip(pieces, layouts):
        c, dg, bb = _bbox(p["position"])
        allpts += p["position"]
        piece_recs += PMG_PIECE.pack(
            len(p["indices"]), len(p["position"]), 0xFFFFFFF0, 1, p["material"],
            *c, dg / 2, *bb,
            stride, off["position"], off["normal"], off["texcoord"], off["color"], -1,
            -1, -1, -1, off["index"])
    c, dg, bb = _bbox(allpts)
    header = PMG_HEADER.pack(
        0x15, b"gmP", n_piece, 1, 0, 0, n_loc, 0, *c, dg / 2, *bb,
        parts_off, parts_off, loc_off, pieces_off, sp_off, len(strings),
        vp_off, len(vpool), ip_off, len(ipool))
    part = PMG_PART.pack(token(part_name), n_piece, 0, n_loc, 0)
    with open(path, "wb") as f:
        f.write(header + part + b"".join(loc_recs) + piece_recs + strings + vpool + ipool)

# --------------------------------------------------------------------------- PMD

PMD_HEADER = struct.Struct("<16I")


def read_pmd(path):
    d = open(path, "rb").read()
    h = PMD_HEADER.unpack_from(d, 0)
    (ver, n_mat, n_look, n_piece, n_var, n_part, n_attr, attr_val_size, mat_block,
     look_off, var_off, part_attr_off, attr_val_off, attr_off, mat_off, mat_data_off) = h
    assert ver == 4
    m = dict(header=dict(materials=n_mat, looks=n_look, pieces=n_piece, variants=n_var,
                         parts=n_part, attribs=n_attr, attr_val_size=attr_val_size,
                         mat_block=mat_block, look_off=look_off, var_off=var_off,
                         part_attr_off=part_attr_off, attr_val_off=attr_val_off,
                         attr_off=attr_off, mat_off=mat_off, mat_data_off=mat_data_off),
             size=len(d))
    m["looks"] = [untoken(x) for x in struct.unpack_from("<%dQ" % n_look, d, look_off)]
    m["variants"] = [untoken(x) for x in struct.unpack_from("<%dQ" % n_var, d, var_off)]
    m["part_attribs"] = [struct.unpack_from("<2i", d, part_attr_off + 8 * i)
                         for i in range(n_part)]
    m["attribs"] = []
    for i in range(n_attr):
        name, typ, off = struct.unpack_from("<Qii", d, attr_off + 16 * i)
        m["attribs"].append((untoken(name), typ, off))
    m["attrib_values"] = d[attr_val_off:attr_val_off + n_var * attr_val_size].hex()
    m["materials"] = []
    for off in struct.unpack_from("<%dI" % (n_look * n_mat), d, mat_off):
        m["materials"].append(d[off:d.index(b"\0", off)].decode())
    return m


def write_pmd(path, materials, look="default", variant="default"):
    """Single look, single variant, single part with a 'visible' attribute."""
    n_mat = len(materials)
    look_off = PMD_HEADER.size
    var_off = look_off + 8
    part_attr_off = var_off + 8
    attr_off = part_attr_off + 8
    attr_val_off = attr_off + 16
    mat_off = attr_val_off + 4
    mat_data_off = mat_off + 4 * n_mat
    blob = b""
    offs = []
    for mpath in materials:
        offs.append(mat_data_off + len(blob))
        blob += mpath.encode() + b"\0"
    header = PMD_HEADER.pack(4, n_mat, 1, 0, 1, 1, 1, 4, len(blob),
                             look_off, var_off, part_attr_off, attr_val_off, attr_off,
                             mat_off, mat_data_off)
    body = struct.pack("<Q", token(look)) + struct.pack("<Q", token(variant))
    body += struct.pack("<2i", 0, 1)
    body += struct.pack("<Qii", token("visible"), 0, 0)
    body += struct.pack("<i", 1)
    body += struct.pack("<%dI" % n_mat, *offs)
    with open(path, "wb") as f:
        f.write(header + body + blob)


if __name__ == "__main__":
    import sys

    for path in sys.argv[1:]:
        print("=" * 100)
        print(path)
        if path.endswith(".pmd"):
            m = read_pmd(path)
            for k, v in m.items():
                print("  %-14s %s" % (k, v))
            continue
        m = read_pmg(path)
        print("  size", m["size"], "bones", m["bones"], "weight_width", m["weight_width"],
              "skel_hash", m["skeleton_hash"])
        print("  bb_center", m["bb_center"], "diag", m["bb_diag"])
        print("  bb", m["bb"])
        print("  offsets", m["offsets"])
        for p in m["parts"]:
            print("  part", p)
        for l in m["locators"]:
            print("  locator", l)
        for i, p in enumerate(m["pieces"]):
            print("  piece %d: verts=%d edges=%d mat=%d tcmask=%#x tcw=%d stride=%d present=%s"
                  % (i, p["verts"], p["edges"], p["material"], p["texcoord_mask"],
                     p["texcoord_width"], p["stride"], p["present"]))
            print("     offsets", p["offsets"])
            print("     bb", tuple(round(x, 4) for x in p["bb"]))
            cols = set(p["color"]) if p["color"] else None
            print("     colors", list(cols)[:6] if cols else None,
                  "factor", list(set(p["factor"]))[:4] if p["factor"] else None)
            if p["verts"] <= 8:
                for j in range(p["verts"]):
                    print("     v%d pos=%s n=%s uv=%s tan=%s" % (
                        j, tuple(round(x, 4) for x in p["position"][j]),
                        tuple(round(x, 3) for x in p["normal"][j]),
                        tuple(round(x, 4) for x in p["texcoord"][j]) if p["texcoord"] else None,
                        tuple(round(x, 3) for x in p["tangent"][j]) if p["tangent"] else None))
                print("     idx", p["indices"])

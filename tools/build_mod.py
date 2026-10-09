"""Builds dist/cabinplay.scs: the in-cabin CabinPlay screen accessory.

Everything in the package is generated here (model, textures, materials, defs),
so the mod has no source assets besides this script.
"""
import math
import os
import shutil
import struct
import sys
import zipfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scsmodel

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TREE = os.path.join(ROOT, "build", "mod_tree")
DIST = os.path.join(ROOT, "dist")
STATIC = os.path.join(ROOT, "mod_static")

MOD_VERSION = os.environ.get("CABINPLAY_VERSION", "0.0.0")  # set by build.ps1 from the VERSION file
GAME_VERSION = "1.61.*"
MODEL_DIR = "/vehicle/truck/upgrade/cabinplay"

# Trucks whose interiors have the left-windshield accessory slot (the one the
# game's own portable navigator uses).
TRUCKS = [
    "daf.2021", "daf.xd", "daf.xf", "daf.xf_euro6",
    "iveco.hiway", "iveco.stralis", "iveco.sway",
    "man.tgx", "man.tgx_2020", "man.tgx_euro6",
    "mercedes.actros", "mercedes.actros2014",
    "renault.magnum", "renault.premium", "renault.t",
    "scania.r", "scania.r_2016", "scania.s_2016", "scania.streamline",
    "volvo.fh16", "volvo.fh16_2012", "volvo.fh_2021", "volvo.fh_2024",
]
SLOT = "set_lglass"

# The screen texture is a flat fill of this exact colour. The plugin recognises the
# texture by it, at any mip level, and replaces its contents with live frames.
# Keep in sync with plugin/cabinplay_plugin.c.
MARKER_BGRA = (0x06, 0x02, 0x04, 0xFF)
SCREEN_TEX_W, SCREEN_TEX_H = 1024, 512

# The game draws its navigation map into a texture of this size for the accessory
# (ui_drawable_size). The map takes the part right of the CabinPlay dock; the strip that
# is left over carries the colours the plugin recognises the texture by.
# Keep in sync with plugin/frame_protocol.h.
NAV_W, NAV_H = 1024, 512
DOCK_W = 88

# ---- Device dimensions (metres). Frame: origin = mount point on the glass,
# +x right, +y up, +z towards the driver. The mount point is low in the left corner of
# the windscreen, so the screen hangs on a short gooseneck arm that lifts it and brings
# it a little closer to the driver.
#
# The mount point's own axes are not the cabin's: measured in game (Scania S), +z is
# turned about 25 degrees towards the driver and dips about 9 degrees. So moving the
# screen along +z also carries it to the right, and it already faces the driver without
# any yaw of its own.
#
# The screen is sold in two versions; the player picks one in the truck workshop.
#   unit     accessory name in the definitions (at most 12 characters)
#   screen   half width, half height of the visible picture (2:1)
#   body     half width, half height, thickness, corner radius of the tablet
#   center   position of the screen centre
#   tilt     degrees the screen leans back
#   yaw      degrees it turns right (negative: left)
VARIANTS = [
    # 11 inch on the gooseneck arm: raised, clear of the A-pillar, squarely facing the driver.
    dict(unit="cabinplay", model="cabinplay", name="CabinPlay Screen (Large)", price=1500,
         screen=(0.1260, 0.0630), screen_r=0.0055, body=(0.1330, 0.0700, 0.0095, 0.0130),
         center=(0.039, 0.092, 0.236), tilt=12.0, yaw=-12.0),
    # 10 inch right at the glass, where the stock navigator sits.
    dict(unit="cabinplay_s", model="cabinplay_compact", name="CabinPlay Screen (Compact)", price=1200,
         screen=(0.1150, 0.0575), screen_r=0.0050, body=(0.1220, 0.0645, 0.0090, 0.0120),
         center=(0.0, 0.025, 0.105), tilt=10.0, yaw=0.0),
]


# --------------------------------------------------------------------------- mesh

def vsub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def vadd(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def vmul(a, s): return (a[0] * s, a[1] * s, a[2] * s)
def vdot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def vcross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def vnorm(a):
    l = math.sqrt(vdot(a, a)) or 1.0
    return (a[0] / l, a[1] / l, a[2] / l)


class Mesh:
    def __init__(self, material):
        self.material = material
        self.position, self.normal, self.uv, self.color, self.indices = [], [], [], [], []

    def vert(self, p, n, uv=(0.5, 0.5), shade=128):
        self.position.append(p)
        self.normal.append(vnorm(n))
        self.uv.append(uv)
        self.color.append((shade, shade, shade, 255))
        return len(self.position) - 1

    def tri(self, a, b, c):
        # The game treats triangles that are clockwise when seen from the front as
        # front-facing, so orient every triangle against its vertex normals.
        pa, pb, pc = self.position[a], self.position[b], self.position[c]
        face = vcross(vsub(pb, pa), vsub(pc, pa))
        n = vadd(vadd(self.normal[a], self.normal[b]), self.normal[c])
        if vdot(face, n) > 0:
            b, c = c, b
        self.indices += [a, b, c]

    def quad(self, a, b, c, d):
        self.tri(a, b, c)
        self.tri(a, c, d)

    def transform(self, fn_point, fn_dir):
        self.position = [fn_point(p) for p in self.position]
        self.normal = [vnorm(fn_dir(n)) for n in self.normal]

    def piece(self):
        return dict(material=self.material, position=self.position, normal=self.normal,
                    uv=self.uv, color=self.color, indices=self.indices)


def rounded_rect(hw, hh, r, seg=8):
    """Outline points, counter-clockwise, with their outward 2D normals."""
    pts = []
    corners = [(hw - r, hh - r, 0), (-hw + r, hh - r, 90), (-hw + r, -hh + r, 180), (hw - r, -hh + r, 270)]
    for cx, cy, a0 in corners:
        for i in range(seg + 1):
            a = math.radians(a0 + 90.0 * i / seg)
            pts.append(((cx + r * math.cos(a), cy + r * math.sin(a)), (math.cos(a), math.sin(a))))
    return pts


def add_cap(mesh, outline, z, nz, shade=128, uv_fn=None):
    """Flat rounded-rect face at height z facing nz (+1/-1)."""
    uv_fn = uv_fn or (lambda x, y: (0.5, 0.5))
    centre = mesh.vert((0, 0, z), (0, 0, nz), uv_fn(0, 0), shade)
    ring = [mesh.vert((x, y, z), (0, 0, nz), uv_fn(x, y), shade) for (x, y), _ in outline]
    for i in range(len(ring)):
        mesh.tri(centre, ring[i], ring[(i + 1) % len(ring)])


def add_band(mesh, outline_a, za, outline_b, zb, shade=128, smooth_z=0.0):
    """Wall between two outlines with outward-facing smooth normals."""
    n = len(outline_a)
    ra = [mesh.vert((p[0], p[1], za), (nrm[0], nrm[1], smooth_z), shade=shade) for p, nrm in outline_a]
    rb = [mesh.vert((p[0], p[1], zb), (nrm[0], nrm[1], smooth_z), shade=shade) for p, nrm in outline_b]
    for i in range(n):
        j = (i + 1) % n
        mesh.quad(ra[i], ra[j], rb[j], rb[i])


def frame_for_axis(axis):
    axis = vnorm(axis)
    helper = (1, 0, 0) if abs(axis[0]) < 0.9 else (0, 1, 0)
    u = vnorm(vcross(axis, helper))
    v = vcross(axis, u)
    return axis, u, v


def add_tube(mesh, rings, seg=16, shade=128, cap_start=False, cap_end=False):
    """Surface of revolution through rings: list of (centre, radius) along one axis."""
    axis, u, v = frame_for_axis(vsub(rings[-1][0], rings[0][0]))
    loops = []
    for k, (c, r) in enumerate(rings):
        # Slope of the profile gives the normal its axial component.
        k0, k1 = max(k - 1, 0), min(k + 1, len(rings) - 1)
        dr = rings[k1][1] - rings[k0][1]
        dl = math.sqrt(vdot(vsub(rings[k1][0], rings[k0][0]), vsub(rings[k1][0], rings[k0][0]))) or 1e-9
        loop = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            radial = vadd(vmul(u, math.cos(a)), vmul(v, math.sin(a)))
            normal = vadd(radial, vmul(axis, -dr / dl))
            loop.append(mesh.vert(vadd(c, vmul(radial, r)), normal, shade=shade))
        loops.append(loop)
    for a, b in zip(loops, loops[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            mesh.quad(a[i], a[j], b[j], b[i])
    for flag, (c, r), sign in ((cap_start, rings[0], -1), (cap_end, rings[-1], 1)):
        if not flag:
            continue
        nrm = vmul(axis, sign)
        centre = mesh.vert(c, nrm, shade=shade)
        ring = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            radial = vadd(vmul(u, math.cos(a)), vmul(v, math.sin(a)))
            ring.append(mesh.vert(vadd(c, vmul(radial, r)), nrm, shade=shade))
        for i in range(seg):
            mesh.tri(centre, ring[i], ring[(i + 1) % seg])


def add_path_tube(mesh, points, radius, seg=12, shade=128):
    """Tube of constant radius along a curved path."""
    tangents = []
    for i in range(len(points)):
        tangents.append(vnorm(vsub(points[min(i + 1, len(points) - 1)], points[max(i - 1, 0)])))
    _, u, _ = frame_for_axis(tangents[0])
    loops = []
    for p, t in zip(points, tangents):
        # Carry the frame along the path so the tube does not twist.
        u = vnorm(vsub(u, vmul(t, vdot(u, t))))
        v = vcross(t, u)
        loop = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            radial = vadd(vmul(u, math.cos(a)), vmul(v, math.sin(a)))
            loop.append(mesh.vert(vadd(p, vmul(radial, radius)), radial, shade=shade))
        loops.append(loop)
    for a, b in zip(loops, loops[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            mesh.quad(a[i], a[j], b[j], b[i])


def bezier(p0, p1, p2, p3, steps):
    out = []
    for i in range(steps + 1):
        t = i / steps
        s = 1 - t
        out.append(tuple(s ** 3 * p0[k] + 3 * s * s * t * p1[k] + 3 * s * t * t * p2[k] + t ** 3 * p3[k]
                         for k in range(3)))
    return out


def add_sphere(mesh, centre, radius, seg=14, rings=8, shade=128):
    rows = []
    for k in range(rings + 1):
        phi = math.pi * k / rings
        row = []
        for i in range(seg):
            th = 2 * math.pi * i / seg
            n = (math.sin(phi) * math.cos(th), math.cos(phi), math.sin(phi) * math.sin(th))
            row.append(mesh.vert(vadd(centre, vmul(n, radius)), n, shade=shade))
        rows.append(row)
    for a, b in zip(rows, rows[1:]):
        for i in range(seg):
            j = (i + 1) % seg
            mesh.quad(a[i], a[j], b[j], b[i])


def build_model(variant):
    SCREEN_HW, SCREEN_HH = variant["screen"]
    SCREEN_R = variant["screen_r"]
    BODY_HW, BODY_HH, BODY_T, BODY_R = variant["body"]
    CENTER, TILT_DEG, YAW_DEG = variant["center"], variant["tilt"], variant["yaw"]
    body, glass, screen, nav = Mesh(0), Mesh(1), Mesh(2), Mesh(3)

    # --- tablet, built facing +z with its front glass at z = 0
    bevel = 0.0012
    outer = rounded_rect(BODY_HW, BODY_HH, BODY_R)
    front = rounded_rect(BODY_HW - bevel, BODY_HH - bevel, BODY_R - bevel)
    back = rounded_rect(BODY_HW - 0.003, BODY_HH - 0.003, BODY_R - 0.003)
    add_cap(glass, front, 0.0, 1)
    add_band(body, front, 0.0, outer, -bevel, shade=150, smooth_z=1.0)     # polished chamfer
    add_band(body, outer, -bevel, outer, -BODY_T + 0.003, shade=112)
    add_band(body, outer, -BODY_T + 0.003, back, -BODY_T, shade=96, smooth_z=-1.0)
    add_cap(body, back, -BODY_T, -1, shade=88)

    def screen_uv(x, y):
        return ((x + SCREEN_HW) / (2 * SCREEN_HW), (SCREEN_HH - y) / (2 * SCREEN_HH))
    add_cap(screen, rounded_rect(SCREEN_HW, SCREEN_HH, SCREEN_R, seg=6), 0.0004, 1, uv_fn=screen_uv)

    # The game only draws the navigation map if a material on the model uses its
    # texture, so a sliver of it sits out of sight inside the body.
    s, zn = 0.002, -BODY_T / 2
    q = [nav.vert((x, y, zn), (0, 0, 1), uv) for x, y, uv in
         ((-s, -s, (0, 1)), (s, -s, (1, 1)), (s, s, (1, 0)), (-s, s, (0, 0)))]
    nav.quad(*q)

    # mounting boss and ball joint on the back of the tablet
    boss_z = -BODY_T - 0.006
    ball_local = (0.0, -0.010, -BODY_T - 0.017)
    add_tube(body, [((0, -0.010, -BODY_T), 0.028), ((0, -0.010, boss_z + 0.002), 0.026),
                    ((0, -0.010, boss_z), 0.017)], seg=20, shade=100)
    add_sphere(body, ball_local, 0.0135, shade=120)

    t, yaw = math.radians(TILT_DEG), math.radians(YAW_DEG)

    def rot(p):
        x, y, z = p[0], p[1] * math.cos(t) + p[2] * math.sin(t), -p[1] * math.sin(t) + p[2] * math.cos(t)
        return (x * math.cos(yaw) + z * math.sin(yaw), y, -x * math.sin(yaw) + z * math.cos(yaw))

    def place(p):
        return vadd(rot(p), CENTER)

    for m in (body, glass, screen, nav):
        m.transform(place, rot)
    ball = place(ball_local)

    # --- suction cup on the glass. The glass leans back about 5 degrees here.
    g_axis = vnorm((0.0, -0.088, 1.0))
    base = (0.0, 0.0, 0.0035)

    def along(d):
        return vadd(base, vmul(g_axis, d))
    add_tube(body, [(along(-0.0015), 0.0370), (along(0.0005), 0.0360), (along(0.0045), 0.0300),
                    (along(0.0100), 0.0170), (along(0.0150), 0.0125), (along(0.0210), 0.0125)],
             seg=24, shade=92, cap_end=True)
    neck = along(0.0210)
    add_sphere(body, neck, 0.0105, shade=110)
    # gooseneck arm: leaves the cup square to the glass, meets the tablet square to its back
    back_dir = rot((0.0, 0.0, -1.0))
    reach = math.sqrt(vdot(vsub(ball, neck), vsub(ball, neck)))
    path = bezier(neck, vadd(neck, vmul(g_axis, reach * 0.45)), vadd(ball, vmul(back_dir, reach * 0.40)), ball, 18)
    add_path_tube(body, path, 0.0078, seg=12, shade=106)
    for end, direction in ((neck, g_axis), (ball, back_dir)):
        add_tube(body, [(vadd(end, vmul(direction, 0.004)), 0.0105), (vadd(end, vmul(direction, 0.022)), 0.0105),
                        (vadd(end, vmul(direction, 0.027)), 0.0080)], seg=14, shade=84)

    return [body.piece(), glass.piece(), screen.piece(), nav.piece()]


# --------------------------------------------------------------------------- textures

def dds_bytes(width, height, mips, pixel_fn):
    """Uncompressed B8G8R8A8_UNORM_SRGB DDS with a DX10 header, as the game ships them."""
    mip_count = len(mips)
    flags = 0x00021007
    caps = 0x00401008 if mip_count > 1 else 0x00001000
    hdr = b"DDS " + struct.pack("<7I", 124, flags, height, width, 0, 0, mip_count)
    hdr += b"\0" * 44
    hdr += struct.pack("<2I4s5I", 32, 4, b"DX10", 0, 0, 0, 0, 0)
    hdr += struct.pack("<5I", caps, 0, 0, 0, 0)
    hdr += struct.pack("<5I", 91, 3, 0, 1, 0)
    return hdr + b"".join(pixel_fn(w, h) for w, h in mips)


def mip_chain(w, h):
    out = [(w, h)]
    while w > 1 or h > 1:
        w, h = max(1, w // 2), max(1, h // 2)
        out.append((w, h))
    return out


def tobj_bytes(dds_path, mips=True, clamp=True):
    addr = 2 if clamp else 0
    hdr = struct.pack("<I", 0x70B10A01) + b"\0" * 16
    hdr += struct.pack("<HBB", 1, 0, 0)
    hdr += bytes([2, 0, 3, 3, 3 if mips else 2, 0, addr, addr, 0, 0, 0, 0, 0, 1, 0, 0])
    hdr += struct.pack("<II", len(dds_path), 0)
    return hdr + dds_path.encode()


def render_icon(width=256, height=64, ss=3):
    """Accessory list icon: a small tablet with a home screen, on transparency."""
    def rr(px, py, cx, cy, hw, hh, r):
        qx, qy = abs(px - cx) - (hw - r), abs(py - cy) - (hh - r)
        return math.hypot(max(qx, 0), max(qy, 0)) + min(max(qx, qy), 0) - r

    apps = [(255, 59, 48), (52, 199, 89), (10, 132, 255), (255, 159, 10),
            (191, 90, 242), (255, 55, 95), (100, 210, 255), (255, 214, 10)]
    cx, cy = width / 2, height / 2
    out = bytearray()
    for y in range(height):
        for x in range(width):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(ss):
                for sx in range(ss):
                    px, py = x + (sx + 0.5) / ss, y + (sy + 0.5) / ss
                    col = None
                    if rr(px, py, cx, cy, 52, 28, 5) <= 0:
                        col = (28, 28, 30)
                        if rr(px, py, cx, cy, 49, 25, 3) <= 0:
                            k = (px - (cx - 49)) / 98.0
                            col = (int(18 + 30 * k), int(24 + 10 * k), int(56 + 60 * k))
                            if px < cx - 38:
                                col = (12, 12, 16)
                            for i, a in enumerate(apps):
                                ax = cx - 26 + (i % 4) * 19
                                ay = cy - 10 + (i // 4) * 20
                                if rr(px, py, ax, ay, 6.5, 6.5, 2.2) <= 0:
                                    col = a
                    if col:
                        acc[0] += col[0]; acc[1] += col[1]; acc[2] += col[2]; acc[3] += 1
            n = ss * ss
            a = acc[3] / n
            if acc[3]:
                r, g, b = (int(acc[i] / acc[3]) for i in range(3))
            else:
                r = g = b = 0
            out += bytes((b, g, r, int(a * 255)))
    return bytes(out)


# --------------------------------------------------------------------------- package

def write(rel, data):
    path = os.path.join(TREE, rel.lstrip("/").replace("/", os.sep))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    mode = "wb" if isinstance(data, bytes) else "w"
    with open(path, mode, **({} if mode == "wb" else {"encoding": "utf-8", "newline": "\n"})) as f:
        f.write(data)
    return path


def main():
    shutil.rmtree(TREE, ignore_errors=True)
    os.makedirs(DIST, exist_ok=True)

    # models: one per version, sharing materials and textures
    materials = [MODEL_DIR + "/body.mat", MODEL_DIR + "/glass.mat", MODEL_DIR + "/screen.mat",
                 MODEL_DIR + "/nav.mat"]
    for variant in VARIANTS:
        assert len(variant["unit"]) <= 12, "the game limits each part of a unit name to 12 characters"
        pieces = build_model(variant)
        pmg = write("%s/%s.pmg" % (MODEL_DIR, variant["model"]), b"")
        scsmodel.write_pmg(pmg, pieces)
        scsmodel.write_pmd(pmg[:-4] + ".pmd", materials)
        print("  %-16s %d verts" % (variant["model"], sum(len(p["position"]) for p in pieces)))

    # textures
    marker = bytes(MARKER_BGRA)
    write(MODEL_DIR + "/screen.dds", dds_bytes(SCREEN_TEX_W, SCREEN_TEX_H, mip_chain(SCREEN_TEX_W, SCREEN_TEX_H),
                                               lambda w, h: marker * (w * h)))
    write(MODEL_DIR + "/screen.tobj", tobj_bytes(MODEL_DIR + "/screen.dds"))
    white = bytes((255, 255, 255, 255))
    write(MODEL_DIR + "/white.dds", dds_bytes(16, 16, mip_chain(16, 16), lambda w, h: white * (w * h)))
    write(MODEL_DIR + "/white.tobj", tobj_bytes(MODEL_DIR + "/white.dds", clamp=False))
    # placeholder the game swaps for its navigation render target
    black = bytes((0, 0, 0, 255))
    write(MODEL_DIR + "/nav_ui.dds", dds_bytes(4, 4, mip_chain(4, 4), lambda w, h: black * (w * h)))
    write(MODEL_DIR + "/nav_ui.tobj", tobj_bytes(MODEL_DIR + "/nav_ui.dds"))

    # materials (values for the screen are the ones the stock GPS screens use)
    def lit(diffuse, specular, shininess):
        return ('effect : "eut2.dif.spec.rfx" {\n'
                '\tadditional_ambient : 0.000000\n'
                '\tdiffuse : { %f , %f , %f }\n'
                '\treflection : 0.000000\n'
                '\tshininess : %f\n'
                '\tspecular : { %f , %f , %f }\n'
                '\ttexture : "texture_base" {\n'
                '\t\tsource : "%s/white.tobj"\n'
                '\t\tsampler : default\n'
                '\t}\n}\n') % (diffuse, diffuse, diffuse, shininess, specular, specular, specular, MODEL_DIR)
    write(MODEL_DIR + "/body.mat", lit(0.016, 0.10, 30.0))
    write(MODEL_DIR + "/glass.mat", lit(0.004, 0.55, 110.0))
    def lit_screen(tobj):
        return ('effect : "eut2.dif.lum.spec.lvcol.rfx" {\n'
                '\tadditional_ambient : 0.000000\n'
                '\tdiffuse : { 0.132868 , 0.132868 , 0.132868 }\n'
                '\tluminance_night : 3.100000\n'
                '\tluminance_output : 2000.000000\n'
                '\treflection : 0.000000\n'
                '\tshininess : 40.000000\n'
                '\tspecular : { 0.014123 , 0.014123 , 0.014123 }\n'
                '\ttexture : "texture_base" {\n'
                '\t\tsource : "%s/%s"\n'
                '\t\tsampler : default\n'
                '\t}\n}\n') % (MODEL_DIR, tobj)
    write(MODEL_DIR + "/screen.mat", lit_screen("screen.tobj"))
    write(MODEL_DIR + "/nav.mat", lit_screen("nav_ui.tobj"))

    # Navigation screen the game renders for the accessory: its own GPS map, laid out
    # like the stock navigator's UI. UI coordinates have y pointing up.
    def ui_fill(name, layer, l, r, t, b, text):
        assert len(name) <= 12, "the game limits each part of a unit name to 12 characters"
        return ('ui::text : _nameless._.%s {\n'
                ' text: "%s"\n'
                ' coords_l: %d\n coords_r: %d\n coords_t: %d\n coords_b: %d\n'
                ' area_l: 1\n area_r: 0\n area_t: 0\n area_b: 1\n'
                ' id: 0\n layer: %d\n tab: -1\n pointer: -1\n my_parent: _nameless.wnd\n}\n\n') % (
                    name, text, l, r, t, b, layer)
    map_w = NAV_W - DOCK_W
    # bare_map.mat is square; show a centred slice with the screen's aspect ratio.
    u_span = 0.84
    v_span = u_span * NAV_H / map_w
    fill = "<img src=/material/ui/white.mat color=%s xscale=stretch yscale=stretch>"
    write("/ui/dashboard/cabinplay_nav.sii",
          'SiiNunit\n{\n'
          'ui::window : _nameless.wnd {\n'
          ' window_handler: null\n clip_children: true\n keep_aspect: none\n user_string_data: ""\n'
          ' first_direction_focus_id: 0\n fitting: false\n'
          ' my_children: 4\n'
          ' my_children[0]: _nameless._.map\n'
          ' my_children[1]: _nameless._.mark_top\n'
          ' my_children[2]: _nameless._.mark_bottom\n'
          ' my_children[3]: _nameless._.background\n'
          ' coords_l: 0\n coords_r: %d\n coords_t: %d\n coords_b: 0\n'
          ' area_l: 0\n area_r: %d\n area_t: %d\n area_b: 0\n'
          ' id: 0\n layer: 0\n tab: -1\n pointer: -1\n my_parent: null\n}\n\n' % (NAV_W, NAV_H, NAV_W, NAV_H)
          + ui_fill("map", 1, 0, map_w, NAV_H, 0,
                    "<img src=/material/ui/dashboard/bare_map.mat xscale=stretch yscale=stretch "
                    "left=%.4f right=%.4f bottom=%.4f top=%.4f>" % (
                        0.5 - u_span / 2, 0.5 + u_span / 2, 0.5 - v_span / 2, 0.5 + v_span / 2))
          # The plugin tells the texture apart, and which way up it is stored, by this strip:
          # green over magenta. Both read the same whichever way the colour bytes are ordered.
          + ui_fill("mark_top", 2, map_w, NAV_W, NAV_H, NAV_H // 2, fill % "ff10c010")
          + ui_fill("mark_bottom", 2, map_w, NAV_W, NAV_H // 2, 0, fill % "ffc010c0")
          + ui_fill("background", 0, 0, NAV_W, NAV_H, 0, fill % "ff191919")
          + '}\n')

    # accessory icon
    write("/material/ui/accessory/cabinplay_screen.dds", dds_bytes(256, 64, [(256, 64)], lambda w, h: render_icon(w, h)))
    write("/material/ui/accessory/cabinplay_screen.tobj",
          tobj_bytes("/material/ui/accessory/cabinplay_screen.dds", mips=False))
    write("/material/ui/accessory/cabinplay_screen.mat",
          'effect : "ui.rfx" {\n\ttexture : "texture" {\n\t\tsource : "cabinplay_screen.tobj"\n'
          '\t\tu_address : clamp\n\t\tv_address : clamp\n\t\tmip_filter : none\n\t}\n}\n')

    # one accessory definition per truck and version
    for truck in TRUCKS:
        for variant in VARIANTS:
            write("/def/vehicle/truck/%s/accessory/%s/%s.sii" % (truck, SLOT, variant["unit"]),
                  'SiiNunit\n{\n'
                  'accessory_addon_int_ui_data : %s.%s.%s\n{\n'
                  '\tname: "%s"\n'
                  '\tprice: %d\n'
                  '\tunlock: 0\n'
                  '\ticon: "cabinplay_screen"\n'
                  '\tpart_type: aftermarket\n\n'
                  '\tinterior_model: "%s/%s.pmd"\n\n'
                  '\tui_path: "/ui/dashboard/cabinplay_nav.sii"\n'
                  '\tui_drawable_texture_path: "%s/nav_ui.tobj"\n'
                  '\tui_drawable_size: (%d, %d)\n'
                  '}\n}\n' % (variant["unit"], truck, SLOT, variant["name"], variant["price"],
                              MODEL_DIR, variant["model"], MODEL_DIR, NAV_W, NAV_H))

    # manifest
    has_icon = os.path.exists(os.path.join(STATIC, "mod_icon.jpg"))
    if has_icon:
        shutil.copy(os.path.join(STATIC, "mod_icon.jpg"), os.path.join(TREE, "mod_icon.jpg"))
    write("/manifest.sii",
          'SiiNunit\n{\nmod_package : .package_name\n{\n'
          '\tpackage_version: "%s"\n'
          '\tdisplay_name: "CabinPlay Screen"\n'
          '\tauthor: "HydRaboN"\n'
          '\tcategory[]: "interior"\n'
          '%s'
          '\tdescription_file: "mod_description.txt"\n'
          '\tcompatible_versions[]: "%s"\n'
          '}\n}\n' % (MOD_VERSION, '\ticon: "mod_icon.jpg"\n' if has_icon else '', GAME_VERSION))
    write("/mod_description.txt",
          "CabinPlay Screen\n\n"
          "Adds an in-cabin display to the left windshield accessory slot of every truck, in two\n"
          "versions: Large (11 inch, on an arm) and Compact (10 inch, at the glass).\n"
          "The picture comes from the CabinPlay companion app through the cabinplay plugin;\n"
          "without them the screen stays off.\n")

    out = os.path.join(DIST, "cabinplay.scs")
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for base, _, files in os.walk(TREE):
            for name in sorted(files):
                full = os.path.join(base, name)
                z.write(full, os.path.relpath(full, TREE).replace(os.sep, "/"))
    print("built", out, os.path.getsize(out), "bytes")


if __name__ == "__main__":
    main()

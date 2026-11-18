"""Measures where the accessory slots sit in each truck's interior and writes them to
tools/cabin_slots.py, which build_mod.py places the screens by, along with the interior
cameras build_mod.py adds the pillow view to.

Reads the interior models extracted from the game into tools/extracted/probe_int_all2 and
the camera units into tools/extracted/cam (git-ignored); the Coaches DLC buses from
tools/extracted/bus/x (dlc_bus.scs: /vehicle/bus/*/interior/model/*_int.pmg and
/def/camera/units/interior_*.sui). Run it again after a game update moves things:

    python -I tools/measure_slots.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import scsmodel

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROBE = os.path.join(ROOT, "tools", "extracted", "probe_int_all2", "vehicle", "truck")
OUT = os.path.join(ROOT, "tools", "cabin_slots.py")
# /def/camera/units extracted from def.scs
CAMERA_DIR = os.path.join(ROOT, "tools", "extracted", "cam", "def", "camera", "units")
# Trucks whose interior camera gets the pillow view, and the file the game defines it in.
# scania.s_2024e drives with the S 2016's camera, so it gets that one's pillow view.
CAMERAS = {
    "daf.2021": "interior_daf_2021", "daf.xd": "interior_daf_xd", "daf.xf": "interior_daf_xf",
    "daf.xf_electric": "interior_daf_xf_electric", "daf.xf_euro6": "interior_daf_xf_euro6",
    "iveco.hiway": "interior_iveco_hiway", "iveco.stralis": "interior_iveco_stralis", "iveco.sway": "interior_iveco_sway",
    "man.tgx": "interior_man_tgx", "man.tgx_2020": "interior_man_tgx_2020", "man.tgx_euro6": "interior_man_tgx_euro6",
    "mercedes.actros": "interior_mercedes_actros_2009", "mercedes.actros2014": "interior_mercedes_actros_2014",
    "renault.magnum": "interior_renault_magnum_2009", "renault.premium": "interior_renault_premium",
    "renault.t": "interior_renault_t",
    "scania.r": "interior_scania_rcab_2009", "scania.r_2016": "interior_scania_r_2016",
    "scania.s_2016": "interior_scania_s_2016", "scania.streamline": "interior_scania_streamline",
    "volvo.fh16": "interior_volvo_fh16_2009", "volvo.fh16_2012": "interior_volvo_fh16_2012",
    "volvo.fh_2021": "interior_volvo_fh_2021", "volvo.fh_2024": "interior_volvo_fh_2024",
}

# The Coaches DLC buses (dlc_bus.scs): the driver's part of each interior model, with the
# driver's eyes at the origin like a truck's, and the interior camera.
BUS_PROBE = os.path.join(ROOT, "tools", "extracted", "bus", "x", "vehicle", "bus")
BUS_CAMERA_DIR = os.path.join(ROOT, "tools", "extracted", "bus", "x", "def", "camera", "units")
BUS_MODELS = {
    "man.lions_24": "man_lions_24/interior/model/standard_121h4_int",
    "scania.touring_23": "scania_touring_23/interior/model/standard_129h4_int",
    "volvo.9700_23": "volvo_9700_23/interior/model/standard_143d6_int",
}
BUS_CAMERAS = {
    "man.lions_24": "interior_man_lions_24", "scania.touring_23": "interior_scania_touring_23",
    "volvo.9700_23": "interior_volvo_9700_23",
}

# Each truck's left-hand-drive interior model, relative to vehicle/truck/. Where a truck has
# several cabins the first one stands for all: the slots sit in the same places in each.
MODELS = {
    "daf.2021": "daf_2021/interior/model/xf", "daf.xd": "daf_xd/interior/model/sl",
    "daf.xf": "daf_xf/interior/model", "daf.xf_electric": "daf_2021/interior/model/xf",
    "daf.xf_euro6": "daf_xf_euro6/interior/model",
    "iveco.hiway": "iveco_hiway/interior/model", "iveco.stralis": "iveco_stralis/interior/model",
    "iveco.sway": "iveco_sway/interior/model/as",
    "man.tgx": "man_tgx/interior/model", "man.tgx_2020": "man_tgx_2020/interior/model/gx",
    "man.tgx_euro6": "man_tgx_euro6/interior/cab_xl",
    "mercedes.actros": "mercedes_actros_2009/interior/model",
    "mercedes.actros2014": "mercedes_actros_2014/interior/model",
    "renault.magnum": "renault_magnum_2009/interior/model", "renault.premium": "renault_premium/interior/model",
    "renault.t": "renault_t/interior/model",
    "scania.r": "scania_rcab_2009/interior/model", "scania.r_2016": "scania_2016/interior/model",
    "scania.s_2016": "scania_2016/interior/model", "scania.s_2024e": "scania_s_2024e/interior/model_e",
    "scania.streamline": "scania_rcab_2009/interior/model",
    "volvo.fh16": "volvo_fh16_2009/interior/model/cab_ab", "volvo.fh16_2012": "volvo_fh16_2012/interior/model/gt",
    "volvo.fh_2021": "volvo_fh_2021/interior/model/gt", "volvo.fh_2024": "volvo_fh_2024/interior/model/gt",
}
# The slots, by the name of their folder in the accessory definitions. A slot is the locator
# of the same name, or the first numbered one when the model has only those.
SLOTS = ["set_lglass", "set_glass", "set_dashbrd", "toyac", "toyhang", "toyseat", "toybed", "toystand",
         "cup_holder", "intlight_bck", "l_pillow"]
# The MAN bus calls its cup holder slot this.
BUS_SLOTS = SLOTS + ["set_cuphold"]


def main():
    lines = ['"""Where the accessory slots sit in each truck\'s interior: position (metres; the driver\'s',
             "eyes are at the origin, +x right, +y up, -z forward) and rotation (w, x, y, z).",
             "Generated by tools/measure_slots.py from the game's interior models; do not edit.",
             '"""', "", "SLOTS = {"]
    models = [(truck, os.path.join(PROBE, *rel.split("/")), SLOTS) for truck, rel in MODELS.items()]
    models += [(bus, os.path.join(BUS_PROBE, *rel.split("/")), BUS_SLOTS) for bus, rel in BUS_MODELS.items()]
    for truck, path, slots in models:
        m = scsmodel.read_pmg(path + ".pmg")
        # The first locator of a name: some models carry their right-hand-drive cabin's after it.
        found = {}
        for l in m["locators"]:
            found.setdefault(l["name"], l)
        lines.append("    %r: {" % truck)
        for slot in slots:
            loc = found.get(slot) or found.get(slot + "_1")
            if loc:
                lines.append("        %r: ((%.4f, %.4f, %.4f), (%.4f, %.4f, %.4f, %.4f))," % (
                    slot, *loc["pos"], *loc["rot_wxyz"]))
        lines.append("    },")
    lines.append("}")
    # The interior cameras the pillow view is added to, as the game defines them.
    lines += ["", "# The game's interior camera of each truck the pillow view is added to.", "CAMERAS = {"]
    cameras = [(truck, name, CAMERA_DIR) for truck, name in CAMERAS.items()]
    cameras += [(bus, name, BUS_CAMERA_DIR) for bus, name in BUS_CAMERAS.items()]
    for truck, name, folder in cameras:
        with open(os.path.join(folder, name + ".sui"), encoding="utf-8") as f:
            lines.append("    %r: (%r, %r)," % (truck, name, f.read()))
    lines.append("}")
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print("wrote", OUT)


if __name__ == "__main__":
    main()

"""
Legenda Desa Eldara - KERBAU (Eldara Water Buffalo, soft stylized) + ANIMASI untuk Blender 3.x / 4.x
Cara pakai: Blender > tab Scripting > New > paste > Run Script (Alt+P). Lalu tekan Spasi di Timeline.

Model: kerbau menghadap -Y (Numpad 1 = depan, Numpad 3 = samping), origin di tanah di bawah badan.
Animasi (satu timeline, 24 fps, tiap klip ditandai Marker):
    Idle   : frame   1 -  48   (napas, mengunyah, telinga & ekor bergerak)   -> loop
    Walk   : frame  49 -  96   (jalan berat diagonal 4 kaki, 2 siklus)       -> loop
    Graze  : frame  97 - 144   (menunduk makan rumput sambil mengunyah)
    Moo    : frame 145 - 216   (melenguh 2x, mulut membuka, dada mengembang)
    Charge : frame 217 - 252   (menyeruduk: mengais tanah lalu menerjang dengan tanduk)
Untuk Unity/Godot: export FBX/GLB lalu potong klip berdasarkan rentang frame di atas.
"""
import bpy
import math
from math import radians, sin, cos, pi

# ------------------------------------------------------------------
# PENGATURAN
# ------------------------------------------------------------------
CLEAR_SCENE = True
ADD_STUDIO = True
ANIMATE = True
PACK = True           # True = kantong pelana kulit di sisi badan (seperti di lembar aset)
SIZE = 1.0            # skala keseluruhan (1.0 = panjang badan sekitar 2 m, tinggi bahu sekitar 1.4 m)

ANIMS = [("Idle", 1, 48), ("Walk", 49, 96), ("Graze", 97, 144), ("Moo", 145, 216), ("Charge", 217, 252)]
KEY_STEP = 2

# ------------------------------------------------------------------
# UTILITAS
# ------------------------------------------------------------------
def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete()
    for block in (bpy.data.meshes, bpy.data.materials, bpy.data.actions):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def set_input(node, names, value):
    for n in names:
        if n in node.inputs:
            node.inputs[n].default_value = value
            return


def make_mat(name, color, metallic=0.0, roughness=0.75):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1)
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    set_input(bsdf, ["Base Color"], (*color, 1))
    set_input(bsdf, ["Metallic"], metallic)
    set_input(bsdf, ["Roughness"], roughness)
    return mat


M = {}
def build_materials():
    M["hide"] = make_mat("Kulit_AbuHijau", (0.060, 0.085, 0.060), 0.0, 0.8)
    M["hide_l"] = make_mat("Kulit_Terang", (0.105, 0.130, 0.100), 0.0, 0.8)
    M["hide_d"] = make_mat("Kulit_Gelap", (0.030, 0.042, 0.032), 0.0, 0.85)
    M["muzzle"] = make_mat("Moncong", (0.22, 0.17, 0.17), 0.0, 0.5)
    M["nostril"] = make_mat("Lubang_Hidung", (0.02, 0.012, 0.012), 0.0, 0.5)
    M["horn"] = make_mat("Tanduk", (0.42, 0.38, 0.30), 0.0, 0.45)
    M["horn_tip"] = make_mat("Ujung_Tanduk", (0.045, 0.04, 0.035), 0.0, 0.4)
    M["ear_in"] = make_mat("Telinga_Dalam", (0.32, 0.14, 0.14), 0.0, 0.6)
    M["hoof"] = make_mat("Kuku", (0.03, 0.026, 0.024), 0.1, 0.5)
    M["eye"] = make_mat("Mata", (0.004, 0.004, 0.005), 0.0, 0.25)
    M["mouth"] = make_mat("Mulut", (0.30, 0.03, 0.04), 0.0, 0.5)
    M["leather"] = make_mat("Kulit_Pelana", (0.13, 0.045, 0.014), 0.0, 0.65)
    M["cloth"] = make_mat("Kain_Pelana", (0.16, 0.06, 0.02), 0.0, 0.9)
    M["floor"] = make_mat("Lantai", (0.20, 0.21, 0.25), 0.0, 0.9)


def prim(kind, name, loc, scale, mat, rot=(0, 0, 0), parent=None):
    """kind: sphere(r=1) | cyl(r=1, setengah-tinggi 1) | cone(alas r=1, ujung +Z) | cube"""
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=24, ring_count=12, location=loc)
    elif kind == "cyl":
        bpy.ops.mesh.primitive_cylinder_add(radius=1, depth=2, vertices=20, location=loc)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(radius1=1, depth=2, vertices=16, location=loc)
    else:
        bpy.ops.mesh.primitive_cube_add(size=2, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler = [radians(r) for r in rot]
    bpy.ops.object.transform_apply(scale=True)
    obj.data.materials.append(mat)
    bpy.ops.object.shade_smooth()
    if parent:
        obj.parent = parent
    return obj


PIV = {}
REST = {}

def piv(name, parent, loc, rot=(0, 0, 0), scale=(1, 1, 1)):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = [radians(r) for r in rot]
    o.scale = scale
    if parent:
        o.parent = parent
    PIV[name] = o
    REST[name] = (loc, rot, scale)
    return o


# ------------------------------------------------------------------
# MODEL
# ------------------------------------------------------------------
def build_leg(root, sx, sy, tag):
    """sx: +1 kiri / -1 kanan ; sy: -1 depan / +1 belakang"""
    y = -0.50 if sy < 0 else 0.52
    hip = piv(f"Hip{tag}", root, (0.22 * sx, y, 0.62))
    prim("sphere", f"ThighTop_{tag}", (0, 0, -0.02), (0.15, 0.19, 0.20), M["hide"], parent=hip)
    prim("cyl", f"Thigh_{tag}", (0, 0, -0.15), (0.115, 0.115, 0.15), M["hide"], parent=hip)
    hock = piv(f"Hock{tag}", hip, (0, 0, -0.30))
    prim("sphere", f"Knee_{tag}", (0, 0, 0.0), (0.095, 0.095, 0.095), M["hide"], parent=hock)
    prim("cyl", f"Lower_{tag}", (0, 0, -0.16), (0.075, 0.075, 0.16), M["hide_l"], parent=hock)
    foot = piv(f"Foot{tag}", hock, (0, 0, -0.32))
    prim("cyl", f"Hoof_{tag}", (0, -0.01, 0.04), (0.092, 0.098, 0.04), M["hoof"], parent=foot)
    prim("cyl", f"Fetlock_{tag}", (0, 0, 0.10), (0.085, 0.085, 0.03), M["hide_d"], parent=foot)


def build_horn(head, sx, s):
    """Tanduk melengkung lebar ke samping-belakang lalu menekuk ke atas."""
    def P(u):
        return (sx * (0.16 + 0.62 * u), 0.18 * u * u, 0.12 + 0.05 * u + 0.34 * u ** 3)
    n = 8
    for i in range(n):
        a, b = P(i / n), P((i + 1) / n)
        d = [b[k] - a[k] for k in range(3)]
        L = math.sqrt(sum(c * c for c in d))
        dn = [c / L for c in d]
        ax = math.degrees(math.asin(-dn[1]))           # arahkan sumbu +Z silinder ke vektor d
        by = math.degrees(math.atan2(dn[0], dn[2]))
        mid = [(a[k] + b[k]) / 2 for k in range(3)]
        ra = 0.060 * (1 - i / n) + 0.010
        rb = 0.060 * (1 - (i + 1) / n) + 0.010
        mat = M["horn_tip"] if i >= n - 2 else M["horn"]
        prim("cyl", f"Horn_{s}{i}", mid, ((ra + rb) / 2, (ra + rb) / 2, L / 2 * 1.02), mat, rot=(ax, by, 0), parent=head)
        prim("sphere", f"HornJoint_{s}{i}", b, (rb, rb, rb), mat, parent=head)


def build_head(body):
    neck = piv("Neck", body, (0, -0.70, 0.06))
    prim("sphere", "NeckMain", (0, -0.08, -0.04), (0.30, 0.32, 0.32), M["hide"], parent=neck)
    head = piv("Head", neck, (0, -0.24, -0.06), rot=(22, 0, 0))      # kepala kerbau dibawa agak rendah
    prim("sphere", "HeadMain", (0, -0.16, -0.03), (0.19, 0.30, 0.19), M["hide"], parent=head)
    prim("sphere", "Forehead", (0, 0.0, 0.14), (0.17, 0.11, 0.07), M["hide_d"], parent=head)
    prim("sphere", "Muzzle", (0, -0.40, -0.10), (0.15, 0.14, 0.13), M["muzzle"], parent=head)
    for sx, s in ((1, "L"), (-1, "R")):
        prim("sphere", f"Eye_{s}", (0.175 * sx, -0.10, 0.07), (0.028, 0.034, 0.034), M["eye"], parent=head)
        prim("sphere", f"Nostril_{s}", (0.055 * sx, -0.525, -0.085), (0.026, 0.02, 0.032), M["nostril"], parent=head)
        build_horn(head, sx, s)
        ear = piv(f"Ear{s}", head, (0.17 * sx, -0.03, 0.10), rot=(0, 15 * sx, 0))
        prim("sphere", f"EarOuter_{s}", (0.10 * sx, 0.02, 0.0), (0.13, 0.06, 0.032), M["hide"], parent=ear)
        prim("sphere", f"EarInner_{s}", (0.10 * sx, 0.02, 0.012), (0.09, 0.04, 0.02), M["ear_in"], parent=ear)
    jaw = piv("Jaw", head, (0, -0.32, -0.14))
    prim("sphere", "JawMain", (0, -0.08, 0.0), (0.085, 0.13, 0.04), M["hide_l"], parent=jaw)
    prim("sphere", "JawInner", (0, -0.08, 0.026), (0.065, 0.11, 0.016), M["mouth"], parent=jaw)


def build_tail(body):
    t = piv("Tail", body, (0, 0.80, 0.12))
    prim("cyl", "TailStem", (0, 0.03, -0.24), (0.03, 0.03, 0.24), M["hide"], rot=(-8, 0, 0), parent=t)
    prim("sphere", "TailTuft", (0, 0.075, -0.52), (0.06, 0.06, 0.13), M["hide_d"], parent=t)


def build_pack(body):
    prim("sphere", "Saddle", (0, 0.08, 0.335), (0.33, 0.28, 0.06), M["cloth"], parent=body)
    for sx, s in ((1, "L"), (-1, "R")):
        prim("sphere", f"Bag_{s}", (0.43 * sx, 0.06, -0.02), (0.075, 0.23, 0.18), M["leather"], parent=body)
        prim("cube", f"BagStrap_{s}", (0.36 * sx, 0.06, 0.20), (0.012, 0.04, 0.16), M["leather"],
             rot=(0, -28 * sx, 0), parent=body)


def build_buffalo():
    root = piv("Kerbau_Root", None, (0, 0, 0), scale=(SIZE, SIZE, SIZE))
    for tag, sx, sy in (("FL", 1, -1), ("FR", -1, -1), ("BL", 1, 1), ("BR", -1, 1)):
        build_leg(root, sx, sy, tag)
    body = piv("Body", root, (0, 0, 0.95))
    prim("sphere", "BodyMain", (0, 0, 0), (0.40, 0.72, 0.38), M["hide"], parent=body)
    prim("sphere", "Belly", (0, 0.0, -0.13), (0.36, 0.60, 0.27), M["hide_l"], parent=body)
    prim("sphere", "Shoulders", (0, -0.40, 0.05), (0.40, 0.38, 0.40), M["hide"], parent=body)
    prim("sphere", "Rump", (0, 0.46, -0.02), (0.38, 0.36, 0.36), M["hide"], parent=body)
    if PACK:
        build_pack(body)
    build_tail(body)
    build_head(body)
    return root


# ------------------------------------------------------------------
# ANIMASI (prosedural -> keyframe)
# ------------------------------------------------------------------
def sstep(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def legs(P, hip, hock, tags=("FL", "FR", "BL", "BR")):
    for g in tags:
        h = hip[g] if isinstance(hip, dict) else hip
        k = hock[g] if isinstance(hock, dict) else hock
        P["Hip" + g] = dict(rot=(h, 0, 0))
        P["Hock" + g] = dict(rot=(k, 0, 0))
        P["Foot" + g] = dict(rot=(-(h + k), 0, 0))


def pose_idle(t):
    w = 2 * pi * t
    return {
        "Body": dict(loc=(0, 0, 0.005 * sin(w)), scale=1 + 0.01 * sin(w)),
        "Neck": dict(rot=(2 * sin(w + 0.4), 0, 0)),
        "Head": dict(rot=(2 * sin(2 * w), 0, 8 * sin(w))),
        "Jaw": dict(rot=(6 * abs(sin(4 * w)), 0, 0)),               # mengunyah
        "EarL": dict(rot=(0, 0, 6 * max(0, sin(3 * w)))), "EarR": dict(rot=(0, 0, -6 * max(0, sin(3 * w + 2)))),
        "Tail": dict(rot=(6 * sin(2 * w), 0, 10 * sin(2 * w + 1))),
    }


def pose_walk(t):
    phi = 2 * pi * 2 * t
    P = {}
    for g, off in (("FL", 0.0), ("BR", 0.0), ("FR", pi), ("BL", pi)):
        ph = phi + off
        hip = -14 * sin(ph) + 2
        hock = 24 * max(0, cos(ph)) + 3 * max(0, -cos(ph))
        legs(P, {g: hip}, {g: hock}, tags=(g,))
    P["Body"] = dict(loc=(0, 0, 0.012 * cos(2 * phi)), rot=(1.0 * sin(2 * phi), 2.5 * sin(phi), 0))
    P["Neck"] = dict(rot=(4 * sin(2 * phi + 0.5), 0, 0))
    P["Head"] = dict(rot=(3 * sin(2 * phi + 1.0), 0, 3 * sin(phi)))
    P["Jaw"] = dict(rot=(4 * abs(sin(2 * phi)), 0, 0))
    P["EarL"] = dict(rot=(0, 0, 8 * sin(2 * phi + 1.0)))
    P["EarR"] = dict(rot=(0, 0, -8 * sin(2 * phi + 1.0)))
    P["Tail"] = dict(rot=(5 * sin(2 * phi), 0, 14 * sin(phi)))
    return P


def pose_graze(t):
    e = sstep(0.0, 0.15, t) * (1 - sstep(0.85, 1.0, t))
    chew = sin(2 * pi * 8 * t)
    P = {
        "Body": dict(rot=(12 * e, 0, 0), loc=(0, 0, -0.12 * e)),
        "Neck": dict(rot=(36 * e, 0, 0)),
        "Head": dict(rot=(28 * e, 0, 5 * sin(2 * pi * 2 * t) * e)),
        "Jaw": dict(rot=(12 * max(0, chew) * e, 0, 0)),
        "EarL": dict(rot=(0, 0, 8 * max(0, sin(2 * pi * 3 * t)))), "EarR": dict(rot=(0, 0, -8 * max(0, sin(2 * pi * 3 * t + 2)))),
        "Tail": dict(rot=(6 * sin(2 * pi * 3 * t), 0, 18 * sin(2 * pi * 3 * t + 1))),
    }
    legs(P, {"FL": 8 * e, "FR": 8 * e, "BL": -3 * e, "BR": -3 * e},
         {"FL": 10 * e, "FR": 10 * e, "BL": 4 * e, "BR": 4 * e})
    return P


def pose_moo(t):
    e = sstep(0.0, 0.2, t) * (1 - sstep(0.85, 1.0, t))
    pulse = max(0.0, sin(2 * pi * 2 * t)) ** 0.6
    P = {
        "Body": dict(scale=1 + 0.025 * e + 0.02 * e * pulse, rot=(-2 * e, 0, 0)),
        "Neck": dict(rot=(-16 * e, 0, 0)),
        "Head": dict(rot=((-24 - 10 * pulse) * e, 0, 0)),
        "Jaw": dict(rot=(26 * e * pulse, 0, 0)),
        "EarL": dict(rot=(0, -14 * e, 6 * e)), "EarR": dict(rot=(0, 14 * e, -6 * e)),
        "Tail": dict(rot=(-14 * e, 0, 16 * sin(2 * pi * 3 * t) * e)),
    }
    legs(P, {"FL": -3 * e * pulse, "FR": -3 * e * pulse, "BL": 2 * e, "BR": 2 * e}, 0)
    return P


def pose_charge(t):
    wind = sstep(0.0, 0.3, t) - sstep(0.3, 0.38, t)
    strike = sstep(0.28, 0.42, t) - sstep(0.55, 0.9, t)
    scrape = sin(2 * pi * 3 * t) * wind
    P = {
        "Body": dict(rot=(5 * wind + 5 * strike, 0, 0), loc=(0, -0.24 * strike, -0.04 * wind - 0.02 * strike)),
        "Neck": dict(rot=(20 * wind + 14 * strike, 0, 0)),
        "Head": dict(rot=(14 * wind + 12 * strike, 0, 5 * scrape)),
        "Jaw": dict(rot=(10 * strike, 0, 0)),
        "EarL": dict(rot=(0, 18 * (wind + strike), 0)), "EarR": dict(rot=(0, -18 * (wind + strike), 0)),
        "Tail": dict(rot=(-10 * (wind + strike), 0, 12 * sin(2 * pi * 5 * t) * (wind + strike))),
    }
    legs(P, {"FL": -20 * scrape + 5 * wind - 20 * strike, "FR": 5 * wind - 20 * strike,
             "BL": -5 * wind + 14 * strike, "BR": -5 * wind + 14 * strike},
         {"FL": 14 * max(0, scrape) + 5 * wind, "FR": 5 * wind + 6 * strike,
          "BL": 8 * wind + 6 * strike, "BR": 8 * wind + 6 * strike})
    return P


POSES = {"Idle": pose_idle, "Walk": pose_walk, "Graze": pose_graze, "Moo": pose_moo, "Charge": pose_charge}
LOC_KEYED = {"Body", "Head"}
SCALE_KEYED = {"Body"}


def apply_pose(pose):
    for name, obj in PIV.items():
        if name == "Kerbau_Root":
            continue
        rl, rr, rs = REST[name]
        d = pose.get(name, {})
        rot = d.get("rot", (0, 0, 0))
        loc = d.get("loc", (0, 0, 0))
        sc = d.get("scale", 1.0)
        obj.location = tuple(a + b for a, b in zip(rl, loc))
        obj.rotation_euler = [radians(a + b) for a, b in zip(rr, rot)]
        obj.scale = (rs[0] * sc, rs[1] * sc, rs[2] * sc)


def build_animation():
    scene = bpy.context.scene
    scene.render.fps = 24
    scene.frame_start = ANIMS[0][1]
    scene.frame_end = ANIMS[-1][2]
    for name, start, end in ANIMS:
        scene.timeline_markers.new(name, frame=start)
        fn = POSES[name]
        frames = list(range(start, end + 1, KEY_STEP))
        if frames[-1] != end:
            frames.append(end)
        for f in frames:
            t = (f - start) / float(end - start)
            apply_pose(fn(t))
            for pname, obj in PIV.items():
                if pname == "Kerbau_Root":
                    continue
                obj.keyframe_insert(data_path="rotation_euler", frame=f)
                if pname in LOC_KEYED:
                    obj.keyframe_insert(data_path="location", frame=f)
                if pname in SCALE_KEYED:
                    obj.keyframe_insert(data_path="scale", frame=f)
    scene.frame_set(1)


# ------------------------------------------------------------------
# STUDIO
# ------------------------------------------------------------------
def build_studio():
    prim("cube", "Floor", (0, 0, -0.01), (5, 5, 0.01), M["floor"])
    bpy.ops.object.camera_add(location=(-6.0, -0.2, 0.9), rotation=(radians(90), 0, radians(-90)))
    cam = bpy.context.active_object
    cam.data.lens = 50
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type='AREA', location=(-3, -4, 5))
    key = bpy.context.active_object
    key.data.energy = 900
    key.data.size = 4
    key.rotation_euler = (radians(50), 0, radians(-35))
    bpy.ops.object.light_add(type='AREA', location=(-4, 4, 3))
    fill = bpy.context.active_object
    fill.data.energy = 400
    fill.data.size = 4
    fill.rotation_euler = (radians(60), 0, radians(-140))


def main():
    if CLEAR_SCENE:
        clear_scene()
    build_materials()
    build_buffalo()
    if ANIMATE:
        build_animation()
    if ADD_STUDIO:
        build_studio()
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if bpy.app.version >= (4, 2, 0) else 'BLENDER_EEVEE'
    try:
        scene.view_settings.view_transform = 'Standard'
    except Exception:
        pass
    world = scene.world
    if world is None:
        world = bpy.data.worlds.new("World")
        scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.20, 0.21, 0.25, 1)
    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type == 'VIEW_3D':
                for space in area.spaces:
                    if space.type == 'VIEW_3D':
                        space.shading.type = 'MATERIAL'
    print("Kerbau Eldara + animasi selesai dibuat! Tekan Spasi untuk memutar.")


main()

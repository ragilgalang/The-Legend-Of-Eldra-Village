"""
Legenda Desa Eldara - BABI DESA (Eldara Pig, soft stylized) + ANIMASI untuk Blender 3.x / 4.x
Cara pakai: Blender > tab Scripting > New > paste > Run Script (Alt+P). Lalu tekan Spasi di Timeline.

Model: babi menghadap -Y (Numpad 1 = depan, Numpad 3 = samping), origin di tanah di bawah badan.
Animasi (satu timeline, 24 fps, tiap klip ditandai Marker):
    Idle    : frame   1 -  48   (napas, menoleh, telinga & ekor bergerak)  -> loop
    Walk    : frame  49 -  96   (jalan diagonal 4 kaki, 2 siklus)          -> loop
    Forage  : frame  97 - 144   (mengendus & menggali tanah dengan hidung)
    Oink    : frame 145 - 216   (menguik 3x, mulut membuka)
    Charge  : frame 217 - 252   (menyeruduk: mengais tanah lalu menerjang)
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
SPOTTED = True        # True = ada bercak gelap di punggung
SIZE = 1.0            # skala keseluruhan (1.0 = panjang badan sekitar 1 m)

ANIMS = [("Idle", 1, 48), ("Walk", 49, 96), ("Forage", 97, 144), ("Oink", 145, 216), ("Charge", 217, 252)]
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


def make_mat(name, color, metallic=0.0, roughness=0.7):
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
    M["pink"] = make_mat("Kulit_Pink", (0.74, 0.33, 0.28), 0.0, 0.65)
    M["pink_l"] = make_mat("Kulit_PinkTerang", (0.84, 0.45, 0.40), 0.0, 0.65)
    M["pink_d"] = make_mat("Kulit_PinkGelap", (0.55, 0.21, 0.19), 0.0, 0.65)
    M["snout"] = make_mat("Moncong", (0.80, 0.38, 0.36), 0.0, 0.5)
    M["nostril"] = make_mat("Lubang_Hidung", (0.12, 0.03, 0.03), 0.0, 0.5)
    M["spot"] = make_mat("Bercak", (0.045, 0.03, 0.03), 0.0, 0.7)
    M["bristle"] = make_mat("Bulu_Kasar", (0.45, 0.17, 0.14), 0.0, 0.8)
    M["hoof"] = make_mat("Kuku", (0.04, 0.03, 0.03), 0.1, 0.5)
    M["eye"] = make_mat("Mata", (0.005, 0.005, 0.006), 0.0, 0.25)
    M["mouth"] = make_mat("Mulut", (0.30, 0.03, 0.04), 0.0, 0.5)
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
    y = 0.27 * sy if sy < 0 else 0.29
    hip = piv(f"Hip{tag}", root, (0.125 * sx, y, 0.32))
    prim("sphere", f"ThighTop_{tag}", (0, 0.0, -0.01), (0.09, 0.105, 0.10), M["pink"], parent=hip)
    prim("cyl", f"Thigh_{tag}", (0, 0, -0.08), (0.066, 0.066, 0.08), M["pink"], parent=hip)
    hock = piv(f"Hock{tag}", hip, (0, 0, -0.16))
    prim("sphere", f"Knee_{tag}", (0, 0, 0.0), (0.058, 0.058, 0.058), M["pink"], parent=hock)
    prim("cyl", f"Lower_{tag}", (0, 0, -0.07), (0.046, 0.046, 0.07), M["pink_l"], parent=hock)
    foot = piv(f"Foot{tag}", hock, (0, 0, -0.14))
    prim("cyl", f"Hoof_{tag}", (0, -0.004, 0.0), (0.053, 0.056, 0.022), M["hoof"], parent=foot)
    prim("cyl", f"Fetlock_{tag}", (0, 0, 0.03), (0.05, 0.05, 0.015), M["pink_d"], parent=foot)


def build_tail(body):
    t = piv("Tail", body, (0, 0.45, 0.08))
    for k in range(8):                       # ekor keriting (spiral kecil)
        a = radians(k * 35)
        r = 0.020 - k * 0.0012
        prim("sphere", f"TailCurl_{k}", (0, 0.065 * math.sin(a), 0.05 * (1 - math.cos(a))), (r, r, r), M["pink_d"], parent=t)


def build_head(body):
    neck = piv("Neck", body, (0, -0.34, 0.05))
    prim("sphere", "NeckMain", (0, -0.04, 0.0), (0.17, 0.16, 0.165), M["pink"], parent=neck)
    head = piv("Head", neck, (0, -0.08, 0.0), scale=(1.22, 1.22, 1.22))   # kepala babi relatif besar
    prim("sphere", "HeadMain", (0, -0.10, 0.0), (0.165, 0.17, 0.155), M["pink"], parent=head)
    prim("sphere", "Forehead", (0, -0.07, 0.07), (0.12, 0.12, 0.09), M["pink_l"], parent=head)
    for sx, s in ((1, "L"), (-1, "R")):
        prim("sphere", f"Cheek_{s}", (0.10 * sx, -0.14, -0.04), (0.09, 0.10, 0.09), M["pink"], parent=head)
        prim("sphere", f"Eye_{s}", (0.128 * sx, -0.155, 0.05), (0.018, 0.02, 0.022), M["eye"], parent=head)
        prim("sphere", f"Brow_{s}", (0.122 * sx, -0.15, 0.078), (0.036, 0.034, 0.014), M["pink_d"], rot=(0, 18 * sx, 0), parent=head)
        ear = piv(f"Ear{s}", head, (0.10 * sx, -0.04, 0.13), rot=(20, 35 * sx, 0))
        prim("sphere", f"EarOuter_{s}", (0.04 * sx, -0.045, 0.0), (0.07, 0.092, 0.014), M["pink"], parent=ear)
        prim("sphere", f"EarInner_{s}", (0.04 * sx, -0.045, 0.008), (0.045, 0.062, 0.010), M["pink_d"], parent=ear)
    snout = piv("Snout", head, (0, -0.24, -0.03))
    prim("cyl", "SnoutMain", (0, -0.045, 0.0), (0.078, 0.07, 0.056), M["snout"], rot=(90, 0, 0), parent=snout)
    for sx, s in ((1, "L"), (-1, "R")):
        prim("sphere", f"Nostril_{s}", (0.03 * sx, -0.099, 0.0), (0.016, 0.012, 0.024), M["nostril"], parent=snout)
    jaw = piv("Jaw", head, (0, -0.13, -0.10))
    prim("sphere", "JawMain", (0, -0.06, 0.0), (0.075, 0.10, 0.036), M["pink_d"], parent=jaw)
    prim("sphere", "JawInner", (0, -0.06, 0.026), (0.056, 0.085, 0.014), M["mouth"], parent=jaw)


def build_pig():
    root = piv("Babi_Root", None, (0, 0, 0), scale=(SIZE, SIZE, SIZE))
    for tag, sx, sy in (("FL", 1, -1), ("FR", -1, -1), ("BL", 1, 1), ("BR", -1, 1)):
        build_leg(root, sx, sy, tag)
    body = piv("Body", root, (0, 0, 0.42))
    prim("sphere", "BodyMain", (0, 0, 0), (0.22, 0.42, 0.22), M["pink"], parent=body)
    prim("sphere", "Belly", (0, 0.0, -0.07), (0.20, 0.36, 0.16), M["pink_l"], parent=body)
    prim("sphere", "Shoulders", (0, -0.24, 0.01), (0.22, 0.20, 0.22), M["pink"], parent=body)
    prim("sphere", "Rump", (0, 0.25, 0.0), (0.225, 0.22, 0.225), M["pink"], parent=body)
    if SPOTTED:
        prim("sphere", "Spot_Back", (0.10, 0.10, 0.168), (0.085, 0.115, 0.045), M["spot"], rot=(0, 10, 0), parent=body)
        prim("sphere", "Spot_Rump", (-0.12, 0.26, 0.17), (0.08, 0.09, 0.04), M["spot"], rot=(0, -12, 0), parent=body)
        prim("sphere", "Spot_Side", (-0.20, -0.05, 0.03), (0.04, 0.10, 0.08), M["spot"], parent=body)
    for i in range(9):                       # bulu kasar sepanjang tulang punggung
        y = -0.32 + i * 0.075
        z = 0.215 * math.sqrt(max(1 - (y / 0.46) ** 2, 0.05)) + 0.005
        prim("cone", f"Bristle_{i}", (0, y, z), (0.011, 0.011, 0.032), M["bristle"], parent=body)
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
        "Body": dict(loc=(0, 0, 0.004 * sin(w)), scale=1 + 0.012 * sin(w)),
        "Neck": dict(rot=(2 * sin(w + 0.4), 0, 0)),
        "Head": dict(rot=(3 * sin(2 * w), 0, 12 * sin(w) + 4 * sin(3 * w))),
        "Snout": dict(scale=1 + 0.035 * sin(4 * w)),
        "EarL": dict(rot=(0, 0, 0 + 5 * sin(2 * w))), "EarR": dict(rot=(0, 0, 5 * sin(2 * w + 1.5))),
        "Tail": dict(rot=(0, 0, 8 * sin(2 * w))),
    }


def pose_walk(t):
    phi = 2 * pi * 2 * t
    P = {}
    for g, off in (("FL", 0.0), ("BR", 0.0), ("FR", pi), ("BL", pi)):
        ph = phi + off
        hip = -18 * sin(ph) + 2
        hock = 30 * max(0, cos(ph)) + 4 * max(0, -cos(ph))
        legs(P, {g: hip}, {g: hock}, tags=(g,))
    P["Body"] = dict(loc=(0, 0, 0.010 * cos(2 * phi)), rot=(1.5 * sin(2 * phi), 2 * sin(phi), 0))
    P["Neck"] = dict(rot=(3 * sin(2 * phi + 0.5), 0, 0))
    P["Head"] = dict(rot=(3 * sin(2 * phi + 1.0), 0, 4 * sin(phi)))
    P["Snout"] = dict(scale=1 + 0.03 * sin(4 * phi))
    P["EarL"] = dict(rot=(8 * sin(2 * phi + 1.0), 0, 6 * sin(2 * phi)))
    P["EarR"] = dict(rot=(8 * sin(2 * phi + 1.0), 0, -6 * sin(2 * phi)))
    P["Tail"] = dict(rot=(0, 0, 14 * sin(2 * phi)))
    return P


def pose_forage(t):
    e = sstep(0.0, 0.15, t) * (1 - sstep(0.85, 1.0, t))
    sweep = sin(2 * pi * 4 * t)
    nudge = sin(2 * pi * 8 * t)
    P = {
        "Body": dict(rot=(8 * e, 0, 0), loc=(0, 0, -0.03 * e)),
        "Neck": dict(rot=(22 * e, 0, 0)),
        "Head": dict(rot=((20 + 4 * nudge) * e, 0, 14 * sweep * e)),
        "Snout": dict(scale=1 + 0.07 * nudge * e),
        "EarL": dict(rot=(10 * e, 0, 8 * sweep * e)), "EarR": dict(rot=(10 * e, 0, 8 * sweep * e)),
        "Tail": dict(rot=(0, 0, 16 * sin(2 * pi * 3 * t))),
    }
    legs(P, {"FL": 12 * e, "FR": 12 * e, "BL": -4 * e, "BR": -4 * e},
         {"FL": 16 * e, "FR": 16 * e, "BL": 6 * e, "BR": 6 * e})
    return P


def pose_oink(t):
    e = sstep(0.0, 0.2, t) * (1 - sstep(0.85, 1.0, t))
    pulse = max(0.0, sin(2 * pi * 3 * t)) ** 0.7
    P = {
        "Body": dict(scale=1 + 0.03 * e + 0.025 * e * pulse, loc=(0, 0, 0.012 * e * pulse), rot=(-3 * e, 0, 0)),
        "Neck": dict(rot=(-10 * e, 0, 0)),
        "Head": dict(rot=((-14 - 8 * pulse) * e, 0, 0)),
        "Jaw": dict(rot=(30 * e * pulse, 0, 0)),
        "Snout": dict(scale=1 + 0.05 * e * pulse),
        "EarL": dict(rot=(-18 * e, 0, 4 * e)), "EarR": dict(rot=(-18 * e, 0, -4 * e)),
        "Tail": dict(rot=(0, 0, 20 * sin(2 * pi * 4 * t) * e)),
    }
    legs(P, {"FL": -3 * e * pulse, "FR": -3 * e * pulse, "BL": 2 * e, "BR": 2 * e}, 0)
    return P


def pose_charge(t):
    wind = sstep(0.0, 0.3, t) - sstep(0.3, 0.38, t)
    strike = sstep(0.28, 0.42, t) - sstep(0.55, 0.9, t)
    scrape = sin(2 * pi * 3 * t) * wind
    P = {
        "Body": dict(rot=(7 * wind + 6 * strike, 0, 0), loc=(0, -0.14 * strike, -0.035 * wind - 0.01 * strike)),
        "Neck": dict(rot=(22 * wind + 16 * strike, 0, 0)),
        "Head": dict(rot=(18 * wind + 14 * strike, 0, 6 * scrape)),
        "Jaw": dict(rot=(18 * strike, 0, 0)),
        "Snout": dict(scale=1 + 0.06 * wind),
        "EarL": dict(rot=(-22 * strike, 0, 0)), "EarR": dict(rot=(-22 * strike, 0, 0)),
        "Tail": dict(rot=(0, 0, 10 * sin(2 * pi * 5 * t) * (wind + strike))),
    }
    legs(P, {"FL": -22 * scrape * 0.9 + 6 * wind - 24 * strike, "FR": 6 * wind - 24 * strike,
             "BL": -6 * wind + 14 * strike, "BR": -6 * wind + 14 * strike},
         {"FL": 14 * max(0, scrape) + 6 * wind, "FR": 6 * wind + 6 * strike,
          "BL": 10 * wind + 6 * strike, "BR": 10 * wind + 6 * strike})
    return P


POSES = {"Idle": pose_idle, "Walk": pose_walk, "Forage": pose_forage, "Oink": pose_oink, "Charge": pose_charge}
LOC_KEYED = {"Body", "Head"}
SCALE_KEYED = {"Body", "Snout"}


def apply_pose(pose):
    for name, obj in PIV.items():
        if name == "Babi_Root":
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
                if pname == "Babi_Root":
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
    prim("cube", "Floor", (0, 0, -0.01), (3, 3, 0.01), M["floor"])
    bpy.ops.object.camera_add(location=(-3.2, 0.0, 0.5), rotation=(radians(90), 0, radians(-90)))
    cam = bpy.context.active_object
    cam.data.lens = 50
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type='AREA', location=(-2, -2.5, 3))
    key = bpy.context.active_object
    key.data.energy = 350
    key.data.size = 3
    key.rotation_euler = (radians(50), 0, radians(-35))
    bpy.ops.object.light_add(type='AREA', location=(-2.5, 2.5, 2))
    fill = bpy.context.active_object
    fill.data.energy = 150
    fill.data.size = 3
    fill.rotation_euler = (radians(60), 0, radians(-140))


def main():
    if CLEAR_SCENE:
        clear_scene()
    build_materials()
    build_pig()
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
    print("Babi Eldara + animasi selesai dibuat! Tekan Spasi untuk memutar.")


main()

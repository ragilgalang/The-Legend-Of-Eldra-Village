"""
Legenda Desa Eldara - BUAYA RAWA (Swamp Croc, Ambush Predator, soft stylized) + ANIMASI untuk Blender 3.x / 4.x
Cara pakai: Blender > tab Scripting > New > paste > Run Script (Alt+P). Lalu tekan Spasi di Timeline.

Model: buaya menghadap -Y (Numpad 1 = depan, Numpad 3 = samping, Numpad 7 = atas), origin di tanah di bawah badan.
Animasi (satu timeline, 24 fps, tiap klip ditandai Marker):
    Idle      : frame   1 -  48   (napas, rahang sedikit terbuka, ekor mengayun)   -> loop
    Walk      : frame  49 -  96   (merayap, kaki menyamping, badan meliuk)         -> loop
    Swim      : frame  97 - 144   (berenang, kaki dilipat, ekor bergelombang)      -> loop
    Snap      : frame 145 - 192   (menerkam: menerjang, rahang terbuka lalu mengatup, mengibas kepala)
    DeathRoll : frame 193 - 252   (death roll: badan berputar 2x pada porosnya sambil mencengkeram)
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
SIZE = 1.0            # skala keseluruhan (1.0 = panjang total sekitar 3.9 m)
LOW_POLY = False      # True = segmen lebih sedikit (lebih ringan untuk game, tampilan sedikit kasar)

ANIMS = [("Idle", 1, 48), ("Walk", 49, 96), ("Swim", 97, 144), ("Snap", 145, 192), ("DeathRoll", 193, 252)]
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


def make_mat(name, color, metallic=0.0, roughness=0.6):
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
    M["scale"] = make_mat("Sisik_HijauRawa", (0.040, 0.075, 0.030), 0.0, 0.55)
    M["scale_l"] = make_mat("Sisik_Terang", (0.075, 0.115, 0.040), 0.0, 0.55)
    M["scale_d"] = make_mat("Sisik_Gelap", (0.018, 0.032, 0.016), 0.0, 0.5)
    M["belly"] = make_mat("Perut_Kuning", (0.50, 0.42, 0.18), 0.0, 0.6)
    M["mouth"] = make_mat("Mulut", (0.34, 0.05, 0.06), 0.0, 0.45)
    M["teeth"] = make_mat("Gigi", (0.85, 0.82, 0.68), 0.0, 0.35)
    M["eye"] = make_mat("Mata_Kuning", (0.90, 0.62, 0.03), 0.0, 0.25)
    M["pupil"] = make_mat("Pupil", (0.004, 0.004, 0.004), 0.0, 0.25)
    M["claw"] = make_mat("Cakar", (0.03, 0.028, 0.022), 0.0, 0.5)
    M["floor"] = make_mat("Lantai", (0.20, 0.21, 0.25), 0.0, 0.9)


def prim(kind, name, loc, scale, mat, rot=(0, 0, 0), parent=None):
    """kind: sphere(r=1) | cyl(r=1, setengah-tinggi 1) | cone(alas r=1, ujung +Z) | cube"""
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=10 if LOW_POLY else 20, ring_count=6 if LOW_POLY else 10, location=loc)
    elif kind == "cyl":
        bpy.ops.mesh.primitive_cylinder_add(radius=1, depth=2, vertices=8 if LOW_POLY else 16, location=loc)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(radius1=1, depth=2, vertices=6 if LOW_POLY else 12, location=loc)
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
    """Kaki menyamping (sprawling): sx +1 kiri / -1 kanan ; sy -1 depan / +1 belakang"""
    y = -0.40 if sy < 0 else 0.42
    hip = piv(f"Hip{tag}", root, (0.27 * sx, y, 0.255), rot=(0, -55 * sx, 0))
    prim("sphere", f"Shoulder_{tag}", (0, 0, 0), (0.085, 0.10, 0.085), M["scale"], parent=hip)
    prim("cyl", f"Upper_{tag}", (0, 0, -0.085), (0.055, 0.065, 0.085), M["scale"], parent=hip)
    hock = piv(f"Hock{tag}", hip, (0, 0, -0.17), rot=(0, 45 * sx, 0))
    prim("sphere", f"Elbow_{tag}", (0, 0, 0), (0.055, 0.06, 0.055), M["scale_l"], parent=hock)
    prim("cyl", f"Lower_{tag}", (0, 0, -0.08), (0.04, 0.046, 0.08), M["scale_l"], parent=hock)
    foot = piv(f"Foot{tag}", hock, (0, 0, -0.16), rot=(0, 10 * sx, 0))
    prim("sphere", f"Pad_{tag}", (0, -0.03, 0.016), (0.07, 0.09, 0.018), M["scale_l"], parent=foot)
    for i, phi in enumerate((-30, 0, 30)):
        a = radians(phi)
        prim("cone", f"Claw_{tag}{i}", (0.07 * math.sin(a), -0.09 * math.cos(a), 0.014), (0.013, 0.013, 0.045),
             M["claw"], rot=(90, 0, phi), parent=foot)


def build_head(body):
    head = piv("Head", body, (0, -0.62, 0.02))
    prim("sphere", "Skull", (0, -0.12, 0.0), (0.165, 0.22, 0.11), M["scale"], parent=head)
    prim("sphere", "SnoutUpper", (0, -0.46, -0.012), (0.098, 0.34, 0.058), M["scale"], parent=head)
    prim("sphere", "SnoutUnder", (0, -0.45, -0.05), (0.085, 0.32, 0.03), M["belly"], parent=head)
    prim("sphere", "Palate", (0, -0.45, -0.066), (0.07, 0.30, 0.012), M["mouth"], parent=head)
    prim("sphere", "NoseBump", (0, -0.78, 0.035), (0.052, 0.05, 0.04), M["scale_d"], parent=head)
    for sx, s in ((1, "L"), (-1, "R")):
        prim("sphere", f"Nostril_{s}", (0.022 * sx, -0.822, 0.055), (0.012, 0.012, 0.01), M["pupil"], parent=head)
        prim("sphere", f"EyeMound_{s}", (0.10 * sx, -0.14, 0.085), (0.05, 0.07, 0.045), M["scale_l"], parent=head)
        prim("sphere", f"Eye_{s}", (0.108 * sx, -0.165, 0.115), (0.03, 0.036, 0.036), M["eye"], parent=head)
        prim("sphere", f"Pupil_{s}", (0.134 * sx, -0.165, 0.115), (0.006, 0.014, 0.026), M["pupil"], parent=head)
        for k in range(7):                   # gigi atas menghadap ke bawah
            prim("cone", f"ToothUp_{s}{k}", (sx * (0.093 - 0.006 * k), -0.30 - 0.075 * k, -0.075),
                 (0.0085, 0.0085, 0.028), M["teeth"], rot=(180, 0, 0), parent=head)
    for k in range(5):                       # sisik kasar di atas kepala
        prim("sphere", f"HeadScute_{k}", (0, -0.10 - 0.07 * k, 0.105 - 0.01 * k), (0.03, 0.03, 0.02), M["scale_d"], parent=head)
    jaw = piv("Jaw", head, (0, -0.22, -0.075))
    prim("sphere", "LowerJaw", (0, -0.30, 0.0), (0.088, 0.34, 0.036), M["belly"], parent=jaw)
    prim("sphere", "LowerJawBack", (0, -0.30, 0.012), (0.090, 0.34, 0.025), M["scale"], parent=jaw)
    prim("sphere", "Tongue", (0, -0.30, 0.03), (0.06, 0.28, 0.010), M["mouth"], parent=jaw)
    for sx, s in ((1, "L"), (-1, "R")):
        for k in range(6):                   # gigi bawah menghadap ke atas
            prim("cone", f"ToothLow_{s}{k}", (sx * (0.082 - 0.006 * k), -0.18 - 0.07 * k, 0.045),
                 (0.0075, 0.0075, 0.025), M["teeth"], parent=jaw)


def build_tail(body):
    parent = body
    spacing = (0.60, 0.36, 0.35, 0.34, 0.33)
    widths = (0.17, 0.14, 0.11, 0.085, 0.06)
    heights = (0.14, 0.115, 0.09, 0.07, 0.05)
    for i in range(5):
        seg = piv(f"T{i + 1}", parent, (0, spacing[i], 0))
        prim("sphere", f"TailSeg_{i + 1}", (0, 0.18, 0), (widths[i], 0.21, heights[i]), M["scale"], parent=seg)
        prim("sphere", f"TailUnder_{i + 1}", (0, 0.18, -heights[i] * 0.55), (widths[i] * 0.8, 0.2, heights[i] * 0.5),
             M["belly"], parent=seg)
        for k, y in enumerate((0.06, 0.22)):  # sisik punggung ekor (dua baris)
            for sx in (1, -1):
                prim("cone", f"TailScute_{i + 1}_{k}_{'L' if sx > 0 else 'R'}", (0.035 * sx * (1 - i * 0.12), y, heights[i] * 0.92),
                     (0.016, 0.02, 0.03 - i * 0.003), M["scale_d"], parent=seg)
        parent = seg


def build_body(root):
    body = piv("Body", root, (0, 0, 0.30))
    prim("sphere", "BodyMain", (0, 0, 0), (0.26, 0.62, 0.17), M["scale"], parent=body)
    prim("sphere", "Belly", (0, 0, -0.075), (0.225, 0.56, 0.10), M["belly"], parent=body)
    prim("sphere", "Rump", (0, 0.48, 0.0), (0.20, 0.22, 0.15), M["scale"], parent=body)
    prim("sphere", "Chest", (0, -0.40, 0.0), (0.24, 0.24, 0.16), M["scale"], parent=body)
    for i in range(10):                       # sisik punggung (osteoderm) dua baris + tengah
        y = -0.50 + 0.105 * i
        z = 0.17 * math.sqrt(max(1 - (y / 0.64) ** 2, 0.05)) + 0.008
        for sx in (1, -1):
            prim("cube", f"Scute_{i}_{'L' if sx > 0 else 'R'}", (0.07 * sx, y, z), (0.04, 0.045, 0.024), M["scale_d"], parent=body)
        prim("cone", f"Ridge_{i}", (0, y, z + 0.012), (0.02, 0.03, 0.035), M["scale_d"], parent=body)
    build_head(body)
    build_tail(body)
    return body


def build_croc():
    root = piv("Buaya_Root", None, (0, 0, 0), scale=(SIZE, SIZE, SIZE))
    for tag, sx, sy in (("FL", 1, -1), ("FR", -1, -1), ("BL", 1, 1), ("BR", -1, 1)):
        build_leg(root, sx, sy, tag)
    build_body(root)
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


def tail_wave(P, amp, phase_t, lag, mult=1.0, base=0.0):
    """Gelombang ekor: tiap ruas berotasi Z dengan fase tertunda (amplitudo membesar ke ujung)."""
    for i in range(5):
        P["T%d" % (i + 1)] = dict(rot=(0, 0, base + amp * (0.55 + 0.25 * i) * mult * sin(phase_t - lag * i)))


def pose_idle(t):
    w = 2 * pi * t
    P = {
        "Body": dict(scale=1 + 0.012 * sin(w), loc=(0, 0, 0.003 * sin(w)), rot=(0, 0, 1.5 * sin(w))),
        "Head": dict(rot=(1.5 * sin(2 * w), 0, 5 * sin(w + 0.6))),
        "Jaw": dict(rot=(5 + 3 * sin(w), 0, 0)),
    }
    tail_wave(P, 5.0, w, 0.7)
    return P


def pose_walk(t):
    phi = 2 * pi * 2 * t
    P = {}
    for g, off in (("FL", 0.0), ("BR", 0.0), ("FR", pi), ("BL", pi)):
        ph = phi + off
        hip = -20 * sin(ph)
        hock = 26 * max(0, cos(ph)) + 3 * max(0, -cos(ph))
        legs(P, {g: hip}, {g: hock}, tags=(g,))
    P["Body"] = dict(loc=(0, 0, 0.012 * cos(2 * phi)), rot=(0, 0, 7 * sin(phi)))
    P["Head"] = dict(rot=(0, 0, -9 * sin(phi + 0.3)))
    P["Jaw"] = dict(rot=(4, 0, 0))
    tail_wave(P, 9.0, phi, 0.8)
    return P


def pose_swim(t):
    w = 2 * pi * 2 * t
    P = {
        "Body": dict(loc=(0, 0, -0.04 + 0.015 * sin(w)), rot=(0, 0, 5 * sin(w + 0.5))),
        "Head": dict(rot=(0, 0, -6 * sin(w + 0.9))),
        "Jaw": dict(rot=(3, 0, 0)),
    }
    tail_wave(P, 14.0, w, 0.95)
    legs(P, 48, 38)                           # kaki dilipat ke belakang
    return P


def pose_snap(t):
    coil = sstep(0.0, 0.2, t) - sstep(0.2, 0.26, t)
    lunge = sstep(0.2, 0.36, t) - sstep(0.55, 0.95, t)
    jaw = 55 * (sstep(0.10, 0.34, t) - sstep(0.38, 0.44, t))          # buka lebar lalu mengatup cepat
    shake = sin(2 * pi * 6 * t) * (sstep(0.44, 0.5, t) - sstep(0.72, 0.82, t))
    P = {
        "Body": dict(loc=(0, -0.38 * lunge, 0.02 * lunge - 0.02 * coil), rot=(0, 0, 12 * coil * sin(1.0))),
        "Head": dict(rot=(-14 * lunge + 6 * coil, 0, 14 * shake)),
        "Jaw": dict(rot=(jaw + 4, 0, 0)),
    }
    tail_wave(P, 12.0, 2 * pi * 3 * t, 0.8, mult=lunge + 0.3 * coil)
    legs(P, {"FL": 10 * lunge, "FR": 10 * lunge, "BL": -14 * lunge, "BR": -14 * lunge}, 6 * lunge)
    return P


def pose_deathroll(t):
    roll = 720 * sstep(0.10, 0.90, t)                               # 2 putaran penuh pada poros badan
    arc = sin(pi * sstep(0.05, 0.95, t))
    P = {
        "Body": dict(loc=(0, 0, 0.10 * arc), rot=(0, roll, 6 * sin(2 * pi * 3 * t) * arc)),
        "Head": dict(rot=(0, 0, 10 * sin(2 * pi * 4 * t) * arc)),
        "Jaw": dict(rot=(2, 0, 0)),
    }
    tail_wave(P, 16.0, 2 * pi * 4 * t, 0.7, mult=arc)
    for g, off in (("FL", 0.0), ("BR", 0.0), ("FR", 2.0), ("BL", 2.0)):   # kaki menggapai-gapai
        legs(P, {g: 28 * sin(2 * pi * 4 * t + off) * arc}, {g: 20 * max(0, sin(2 * pi * 4 * t + off)) * arc}, tags=(g,))
    return P


POSES = {"Idle": pose_idle, "Walk": pose_walk, "Swim": pose_swim, "Snap": pose_snap, "DeathRoll": pose_deathroll}
LOC_KEYED = {"Body", "Head"}
SCALE_KEYED = {"Body"}


def apply_pose(pose):
    for name, obj in PIV.items():
        if name == "Buaya_Root":
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
                if pname == "Buaya_Root":
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
    prim("cube", "Floor", (0, 0, -0.01), (6, 6, 0.01), M["floor"])
    bpy.ops.object.camera_add(location=(-8.0, 0.5, 0.8), rotation=(radians(90), 0, radians(-90)))
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
    build_croc()
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
    print("Buaya rawa Eldara + animasi selesai dibuat! Tekan Spasi untuk memutar.")


main()

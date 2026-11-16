"""
Anjing realistis dengan rig tulang, bulu (hair particles) dan siklus jalan.

Pakai di Blender 4.x / 5.x:
  GUI : tab Scripting > Open dog_walk.py > Alt+P, lalu tekan Spasi untuk memutar.
  CLI : blender -b -P dog_walk.py -- --render      (hasil: dog_walk.mp4)
        blender -b -P dog_walk.py -- --still       (hasil: dog_still.png)
"""
import bpy, math, sys

FPS = 24
CYCLE = 48            # frame untuk satu siklus langkah (2 detik)
CYCLES = 2            # jumlah siklus pada timeline
FUR_DENSITY = 6000    # jumlah helai bulu badan (turunkan bila render berat)
RES = (1280, 720)

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

# ---------------------------------------------------------------- scene reset
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start, scene.frame_end = 1, CYCLE * CYCLES
scene.render.resolution_x, scene.render.resolution_y = RES

for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
    try:
        scene.render.engine = eng
        break
    except TypeError:
        continue

# ---------------------------------------------------------------- materials
def mat(name, color, rough=0.7, spec=0.2):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Roughness"].default_value = rough
    for k in ("Specular IOR Level", "Specular"):
        if k in b.inputs:
            b.inputs[k].default_value = spec
            break
    return m

M_FUR = mat("Fur", (0.34, 0.19, 0.08), 0.85)
M_DARK = mat("DarkFur", (0.12, 0.06, 0.03), 0.85)
M_LIGHT = mat("LightFur", (0.62, 0.45, 0.27), 0.85)
M_NOSE = mat("Nose", (0.01, 0.01, 0.01), 0.25, 0.6)
M_EYE = mat("Eye", (0.02, 0.01, 0.005), 0.1, 0.8)
M_GROUND = mat("Ground", (0.16, 0.19, 0.14), 0.95)

# ---------------------------------------------------------------- armature
# Anjing menghadap +Y, tinggi di +Z. Kaki: kepala tulang di atas, ekor tulang di bawah.
arm_data = bpy.data.armatures.new("DogRig")
arm = bpy.data.objects.new("Dog", arm_data)
scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

def bone(name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head, b.tail = head, tail
    if parent:
        b.parent = arm_data.edit_bones[parent]
    return b

bone("root", (0, 0, 0.62), (0, 0.12, 0.62))
bone("spine", (0, -0.52, 0.64), (0, 0.32, 0.66), "root")
bone("neck", (0, 0.32, 0.66), (0, 0.6, 0.86), "spine")
bone("head", (0, 0.6, 0.86), (0, 0.9, 0.84), "neck")
bone("tail1", (0, -0.52, 0.68), (0, -0.7, 0.82), "spine")
bone("tail2", (0, -0.7, 0.82), (0, -0.86, 0.8), "tail1")
for side, x in (("L", 0.12), ("R", -0.12)):
    for pre, y in (("F", 0.38), ("B", -0.4)):
        bone(f"{pre}{side}_up", (x, y, 0.6), (x, y, 0.32), "spine")
        bone(f"{pre}{side}_lo", (x, y, 0.32), (x, y, 0.05), f"{pre}{side}_up")
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------- body parts
def part(name, bone_name, center, radii, material, rot=(0, 0, 0), fur=0, subdiv=2):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=center)
    o = bpy.context.active_object
    o.name = name
    o.scale = radii
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    o.data.materials.append(material)
    vg = o.vertex_groups.new(name=bone_name)
    vg.add([v.index for v in o.data.vertices], 1.0, "REPLACE")
    o.parent = arm
    am = o.modifiers.new("Armature", "ARMATURE")
    am.object = arm
    sub = o.modifiers.new("Subsurf", "SUBSURF")
    sub.levels = subdiv
    if fur:
        add_fur(o, fur, material)
    return o

def add_fur(obj, count, material, length=0.035):
    obj.data.materials.append(material) if material not in obj.data.materials[:] else None
    ps_mod = obj.modifiers.new("Fur", "PARTICLE_SYSTEM")
    ps = ps_mod.particle_system.settings
    ps.type = "HAIR"
    ps.count = count
    ps.hair_length = length
    ps.use_advanced_hair = True
    ps.child_type = "INTERPOLATED"
    ps.child_nbr = 6
    ps.rendered_child_count = 20
    ps.child_radius = 0.02
    ps.child_length = 0.9
    ps.clump_factor = 0.4
    ps.roughness_endpoint = 0.15
    ps.render_type = "PATH"
    ps.material = 1
    ps.root_radius = 0.002
    ps.tip_radius = 0.0

# torso
part("Chest", "spine", (0, 0.24, 0.62), (0.21, 0.3, 0.23), M_FUR, fur=FUR_DENSITY)
part("Belly", "spine", (0, -0.04, 0.58), (0.18, 0.3, 0.18), M_FUR, fur=FUR_DENSITY // 2)
part("Rump", "spine", (0, -0.32, 0.62), (0.2, 0.26, 0.21), M_FUR, fur=FUR_DENSITY // 2)
part("Neck", "neck", (0, 0.5, 0.78), (0.11, 0.17, 0.16), M_LIGHT, rot=(math.radians(-40), 0, 0), fur=2500)
# head
part("Skull", "head", (0, 0.72, 0.88), (0.12, 0.14, 0.12), M_FUR, fur=2500)
part("Muzzle", "head", (0, 0.88, 0.83), (0.065, 0.12, 0.06), M_LIGHT, fur=1200)
part("Nose", "head", (0, 0.995, 0.835), (0.03, 0.02, 0.022), M_NOSE, subdiv=1)
for s in (1, -1):
    part(f"Eye{s}", "head", (s * 0.075, 0.82, 0.915), (0.016, 0.014, 0.016), M_EYE, subdiv=1)
    part(f"Ear{s}", "head", (s * 0.105, 0.68, 0.97), (0.025, 0.065, 0.11), M_DARK,
         rot=(0, math.radians(s * 14), 0), fur=500, subdiv=1)
# tail
part("Tail1", "tail1", (0, -0.6, 0.75), (0.045, 0.09, 0.05), M_FUR, rot=(math.radians(-35), 0, 0), fur=600, subdiv=1)
part("Tail2", "tail2", (0, -0.78, 0.81), (0.04, 0.09, 0.04), M_DARK, rot=(math.radians(10), 0, 0), fur=600, subdiv=1)
# legs
for side, x in (("L", 0.12), ("R", -0.12)):
    s = 1 if side == "L" else -1
    part(f"FUp{side}", f"F{side}_up", (x * 1.1, 0.38, 0.45), (0.065, 0.075, 0.17), M_FUR, fur=900)
    part(f"FLo{side}", f"F{side}_lo", (x * 1.05, 0.38, 0.19), (0.04, 0.045, 0.15), M_LIGHT, fur=500, subdiv=1)
    part(f"FPaw{side}", f"F{side}_lo", (x * 1.05, 0.405, 0.035), (0.052, 0.085, 0.037), M_LIGHT, subdiv=1)
    part(f"BUp{side}", f"B{side}_up", (x * 1.15, -0.4, 0.46), (0.08, 0.11, 0.19), M_FUR, fur=1100)
    part(f"BLo{side}", f"B{side}_lo", (x * 1.05, -0.4, 0.19), (0.038, 0.045, 0.15), M_LIGHT, fur=500, subdiv=1)
    part(f"BPaw{side}", f"B{side}_lo", (x * 1.05, -0.375, 0.035), (0.05, 0.085, 0.037), M_LIGHT, subdiv=1)

# ---------------------------------------------------------------- walk cycle
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode="POSE")
for pb in arm.pose.bones:
    pb.rotation_mode = "XYZ"

TAU = 2 * math.pi
PHASE = {"BL": 0.0, "FL": 0.25, "BR": 0.5, "FR": 0.75}   # urutan jejak kaki anjing berjalan
SWING = {"F": math.radians(26), "B": math.radians(22)}
FLEX = {"F": math.radians(55), "B": math.radians(45)}

def key(pb, frame, axis, value):
    pb.rotation_euler[axis] = value
    pb.keyframe_insert("rotation_euler", index=axis, frame=frame)

for f in range(1, CYCLE * CYCLES + 1):
    t = ((f - 1) % CYCLE) / CYCLE
    for leg, ph in PHASE.items():
        p = TAU * (t - ph)
        pre = leg[0]
        up, lo = arm.pose.bones[f"{leg}_up"], arm.pose.bones[f"{leg}_lo"]
        # +X memutar ujung kaki ke depan (+Y). Tahap ayun = kaki bergerak maju (cos > 0).
        key(up, f, 0, SWING[pre] * math.sin(p))
        lift = max(0.0, math.cos(p))
        key(lo, f, 0, -FLEX[pre] * lift * lift)
    root = arm.pose.bones["root"]
    root.location.z = 0.012 * math.cos(TAU * 2 * t)
    root.keyframe_insert("location", index=2, frame=f)
    key(arm.pose.bones["spine"], f, 2, math.radians(2.5) * math.sin(TAU * t))
    key(arm.pose.bones["neck"], f, 0, math.radians(3) * math.sin(TAU * 2 * t + 0.6))
    key(arm.pose.bones["head"], f, 0, math.radians(-3) * math.sin(TAU * 2 * t + 0.6))
    key(arm.pose.bones["tail1"], f, 2, math.radians(22) * math.sin(TAU * 2 * t))
    key(arm.pose.bones["tail2"], f, 2, math.radians(18) * math.sin(TAU * 2 * t - 0.8))
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------------------------------------------------------- stage
bpy.ops.mesh.primitive_plane_add(size=30)
ground = bpy.context.active_object
ground.data.materials.append(M_GROUND)

def light(name, kind, loc, energy, rot):
    d = bpy.data.lights.new(name, kind)
    d.energy = energy
    o = bpy.data.objects.new(name, d)
    o.location, o.rotation_euler = loc, rot
    scene.collection.objects.link(o)
    return d

sun = light("Sun", "SUN", (3, -3, 5), 3.5, (math.radians(50), 0, math.radians(35)))
sun.angle = math.radians(4)
light("Fill", "AREA", (-3, 2, 2), 120, (math.radians(70), 0, math.radians(-120))).size = 4

cam_d = bpy.data.cameras.new("Cam")
cam_d.lens = 55
cam = bpy.data.objects.new("Cam", cam_d)
cam.location = (2.6, 1.9, 0.95)
scene.collection.objects.link(cam)
target = bpy.data.objects.new("Target", None)
target.location = (0, 0, 0.5)
scene.collection.objects.link(target)
tc = cam.constraints.new("TRACK_TO")
tc.target, tc.track_axis, tc.up_axis = target, "TRACK_NEGATIVE_Z", "UP_Y"
scene.camera = cam

world = bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs["Color"].default_value = (0.55, 0.65, 0.78, 1)
bg.inputs["Strength"].default_value = 0.8

# ---------------------------------------------------------------- render (opsional)
if "--render" in argv:
    scene.render.image_settings.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.filepath = "//dog_walk.mp4"
    bpy.ops.render.render(animation=True)
elif "--still" in argv:
    scene.frame_set(12)
    scene.render.filepath = "//dog_still.png"
    bpy.ops.render.render(write_still=True)
else:
    scene.frame_set(1)


"""
Elang botak realistis: badan berlekuk, bulu berlapis, kaki bercakar, kepakan sayap, melayang,
dan terbang melingkar. Rig memakai hirarki pivot (Empty), sama persis dengan pratinjau 3D.

Blender 4.x / 5.x:
  GUI : tab Scripting > Open eagle_fly.py > Alt+P, lalu tekan Spasi untuk memutar.
  CLI : blender -b -P eagle_fly.py -- --render   (hasil: eagle_fly.mp4)
        blender -b -P eagle_fly.py -- --still    (hasil: eagle_still.png)
"""
import bpy, math, sys

FPS = 24
FRAMES = 240          # satu putaran penuh (10 detik)
FLAP = 30             # frame per kepakan (1,25 detik)
RADIUS = 5.0          # jari-jari lingkaran terbang (m)
HEIGHT = 10.0         # ketinggian terbang (m)
BANK = 18             # kemiringan badan saat berbelok (derajat)
RES = (1280, 720)

r = math.radians
TAU = 2 * math.pi
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

# ---------------------------------------------------------------- scene reset
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start, scene.frame_end = 1, FRAMES
scene.render.resolution_x, scene.render.resolution_y = RES
for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
    try:
        scene.render.engine = eng
        break
    except TypeError:
        continue
try:
    scene.view_settings.view_transform = "Standard"
except TypeError:
    pass

# ---------------------------------------------------------------- material
def lin(h):
    """Warna hex sRGB -> RGB linear."""
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return tuple(out)

def mat(name, color, rough=0.8, spec=0.2):
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

def feather_tex():
    """Tekstur abu-abu halus: tulang bulu di tengah dan garis helai miring."""
    w, h = 32, 128
    px = []
    for y in range(h):
        v = y / (h - 1)
        for x in range(w):
            u = x / (w - 1)
            d = abs(u - 0.5) * 2
            rachis = math.exp(-((u - 0.5) / 0.05) ** 2)
            barb = 0.06 * math.sin((v * 90 + d * 12) * math.pi)
            g = max(0.0, min(1.0, 0.78 + 0.16 * rachis + barb + 0.12 * v ** 3))
            px += [g, g, g, 1.0]
    return w, h, px

_img = []
def get_img():
    if not _img:
        w, h, px = feather_tex()
        img = bpy.data.images.new("FeatherTex", w, h)
        img.pixels.foreach_set(px)
        try:
            img.colorspace_settings.name = "Non-Color"
        except Exception:
            pass
        _img.append(img)
    return _img[0]

def fmat(name, h):
    """Material bulu: warna dikalikan tekstur helai. Jika gagal, tetap berwarna polos."""
    m = mat(name, lin(h), 0.8)
    try:
        nt = m.node_tree
        bsdf = nt.nodes["Principled BSDF"]
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = get_img()
        mix = nt.nodes.new("ShaderNodeMixRGB")
        mix.blend_type = "MULTIPLY"
        mix.inputs["Fac"].default_value = 1.0
        mix.inputs["Color1"].default_value = (*lin(h), 1)
        nt.links.new(tex.outputs["Color"], mix.inputs["Color2"])
        nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
    except Exception as ex:
        print("Tekstur bulu dilewati:", ex)
    return m

M = {
    "body": mat("Body", lin("3a2412"), 0.85),
    "pants": mat("Pants", lin("5a3b1f"), 0.9),
    "white": mat("White", lin("f0efe9"), 0.85),
    "yellow": mat("Yellow", lin("f0b514"), 0.5, 0.4),
    "black": mat("Black", lin("0a0a0a"), 0.35),
    "eye": mat("Eye", lin("f2d255"), 0.2, 0.6),
    "lead": mat("Lead", lin("4a3019"), 0.85),
    "ground": mat("Ground", lin("5d7a42"), 0.95),
    "sec": fmat("Secondary", "2b1a0e"),
    "prim": fmat("Primary", "241509"),
    "gc": fmat("GreaterCoverts", "3d2814"),
    "mc": fmat("MedianCoverts", "4d321b"),
    "lc": fmat("LesserCoverts", "5e4025"),
    "tail": fmat("Tail", "f7f6f0"),
}

# ---------------------------------------------------------------- pembangun bentuk
def v3sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def v3cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def v3norm(a):
    l = math.sqrt(a[0] ** 2 + a[1] ** 2 + a[2] ** 2) or 1.0
    return (a[0] / l, a[1] / l, a[2] / l)

def loft(secs, seg=16):
    """Badan berpenampang elips sepanjang sumbu Y. secs: (y, setengah lebar, setengah tinggi, tinggi pusat)."""
    verts, faces = [], []
    for y, rx, rz, cz in secs:
        for j in range(seg):
            a = TAU * j / seg
            verts.append((math.cos(a) * rx, y, cz + math.sin(a) * rz))
    for i in range(len(secs) - 1):
        for j in range(seg):
            a0, a1 = i * seg + j, i * seg + (j + 1) % seg
            b0, b1 = (i + 1) * seg + j, (i + 1) * seg + (j + 1) % seg
            faces.append((a0, b0, b1, a1))
    return verts, faces

def tube(pts, radii, seg=10):
    """Pipa mengikuti titik-titik lintasan dengan jari-jari berubah (kaki, jari, cakar, tulang sayap)."""
    verts, faces = [], []
    n = len(pts)
    for i in range(n):
        p = pts[i]
        t = v3norm(v3sub(pts[min(i + 1, n - 1)], pts[max(i - 1, 0)]))
        ref = (1, 0, 0) if abs(t[2]) > 0.9 else (0, 0, 1)
        nn = v3norm(v3cross(t, ref))
        bn = v3cross(t, nn)
        for j in range(seg):
            a = TAU * j / seg
            c, s = math.cos(a) * radii[i], math.sin(a) * radii[i]
            verts.append((p[0] + nn[0] * c + bn[0] * s, p[1] + nn[1] * c + bn[1] * s, p[2] + nn[2] * c + bn[2] * s))
    for i in range(n - 1):
        for j in range(seg):
            a0, a1 = i * seg + j, i * seg + (j + 1) % seg
            b0, b1 = (i + 1) * seg + j, (i + 1) * seg + (j + 1) % seg
            faces.append((a0, a1, b1, b0))
    return verts, faces

def feather_mesh(L, W, droop):
    """Satu helai bulu: pipih, ujung membulat, tulang tengah menonjol, melengkung ke bawah."""
    nu, nv = 8, 4
    verts, uvs, faces = [], [], []
    for i in range(nu + 1):
        u = i / nu
        w = W * math.sin(math.pi * max(u, 1e-4) ** 0.75) ** 0.6
        for j in range(nv + 1):
            v = -1 + 2 * j / nv
            verts.append((v * w, u * L, -droop * L * u * u + 0.003 * (1 - abs(v))))
            uvs.append((j / nv, u))
    for i in range(nu):
        for j in range(nv):
            a = i * (nv + 1) + j
            faces.append((a, a + 1, a + nv + 2, a + nv + 1))
    return verts, faces, uvs

def make_mesh(name, verts, faces, uvs=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    try:
        me.shade_smooth()
    except AttributeError:
        for p in me.polygons:
            p.use_smooth = True
    if uvs:
        uvl = me.uv_layers.new(name="UVMap")
        for li, lp in enumerate(me.loops):
            uvl.data[li].uv = uvs[lp.vertex_index]
    return me

# bola dasar untuk mata
bpy.ops.mesh.primitive_uv_sphere_add(radius=1, segments=16, ring_count=10)
_tmp = bpy.context.active_object
bpy.ops.object.shade_smooth()
base = _tmp.data
bpy.data.objects.remove(_tmp)

piv = {}   # nama -> (empty, posisi dunia saat pose diam)

def pivot(name, parent, pos):
    e = bpy.data.objects.new(name, None)
    scene.collection.objects.link(e)
    e.empty_display_size = 0.04
    pp = (0.0, 0.0, 0.0)
    if parent:
        e.parent = piv[parent][0]
        pp = piv[parent][1]
    e.location = (pos[0] - pp[0], pos[1] - pp[1], pos[2] - pp[2])
    piv[name] = (e, pos)
    return e

def add(parent, me, material, pos=(0.0, 0.0, 0.0), rx=0.0, ry=0.0, rz=0.0):
    me.materials.append(material)
    o = bpy.data.objects.new(me.name, me)
    scene.collection.objects.link(o)
    o.parent = piv[parent][0]
    pp = piv[parent][1]
    o.location = (pos[0] - pp[0], pos[1] - pp[1], pos[2] - pp[2])
    o.rotation_euler = (rx, ry, rz)
    return o

def ell(parent, center, radii, material, rz=0.0, rx=0.0):
    o = add(parent, base.copy(), material, center, rx=rx, rz=rz)
    o.scale = radii
    return o

def lofted(parent, secs, material, seg=16):
    v, f = loft(secs, seg)
    add(parent, make_mesh("loft", v, f), material)      # titik sudah dalam koordinat dunia

def piped(parent, pts, radii, material, seg=10):
    v, f = tube(pts, radii, seg)
    add(parent, make_mesh("tube", v, f), material)

def fea(parent, root, d, L, W, material, droop=0.1):
    """Bulu dari titik pangkal 'root' menuju arah d (bidang XY), panjang L, setengah lebar W."""
    n = math.hypot(d[0], d[1])
    dx, dy = d[0] / n, d[1] / n
    v, f, uv = feather_mesh(L, W, droop)
    add(parent, make_mesh("feather", v, f, uv), material, root, rz=math.atan2(-dx, dy))

# ---------------------------------------------------------------- hirarki pivot
pivot("path", None, (0, 0, 0))
pivot("bank", "path", (0, 0, 0))
pivot("body", "bank", (0, 0, 0))
pivot("neck", "body", (0, 0.37, 0.10))
pivot("head", "neck", (0, 0.50, 0.16))
pivot("tail", "body", (0, -0.34, 0.0))

# ---------------------------------------------------------------- badan, leher, kepala
lofted("body", [(-0.40, 0.002, 0.002, 0.02), (-0.34, 0.06, 0.055, 0.02), (-0.26, 0.11, 0.095, 0.02),
                (-0.12, 0.15, 0.13, 0.012), (0.04, 0.17, 0.15, 0.005), (0.18, 0.17, 0.155, 0.012),
                (0.29, 0.15, 0.15, 0.035), (0.37, 0.11, 0.12, 0.07), (0.42, 0.002, 0.002, 0.09)], M["body"])
lofted("neck", [(0.26, 0.10, 0.10, 0.04), (0.33, 0.12, 0.12, 0.06), (0.40, 0.108, 0.113, 0.10),
                (0.47, 0.096, 0.10, 0.135), (0.52, 0.093, 0.097, 0.152)], M["white"])
lofted("head", [(0.46, 0.095, 0.10, 0.14), (0.50, 0.092, 0.098, 0.15), (0.545, 0.09, 0.096, 0.165),
                (0.585, 0.08, 0.088, 0.17), (0.62, 0.06, 0.066, 0.163), (0.645, 0.04, 0.046, 0.155),
                (0.66, 0.002, 0.002, 0.15)], M["white"])
# paruh kuning melengkung ke bawah di ujung
lofted("head", [(0.60, 0.042, 0.05, 0.15), (0.65, 0.04, 0.052, 0.145), (0.69, 0.033, 0.048, 0.137),
                (0.725, 0.022, 0.038, 0.122), (0.745, 0.012, 0.026, 0.105), (0.755, 0.006, 0.018, 0.088),
                (0.76, 0.002, 0.004, 0.078)], M["yellow"])
lofted("head", [(0.62, 0.03, 0.02, 0.118), (0.66, 0.028, 0.018, 0.115), (0.70, 0.02, 0.014, 0.113),
                (0.73, 0.002, 0.002, 0.11)], M["yellow"])
for s in (1, -1):
    ell("head", (s * 0.06, 0.575, 0.21), (0.03, 0.04, 0.012), M["white"])        # alis
    ell("head", (s * 0.068, 0.583, 0.19), (0.016, 0.016, 0.016), M["eye"])
    ell("head", (s * 0.0745, 0.595, 0.19), (0.008, 0.008, 0.008), M["black"])

# ---------------------------------------------------------------- kaki: paha berbulu, tarsus, jari, cakar
K = 0.85
for s in (1, -1):
    x = s * 0.095
    piped("body", [(s * 0.09, 0.03, -0.07), (x, 0.0, -0.13), (x, -0.05, -0.17)], [0.06, 0.05, 0.032], M["pants"], 12)
    piped("body", [(x, -0.05, -0.165), (x, -0.085, -0.185), (x, -0.115, -0.20)], [0.022, 0.02, 0.017], M["yellow"])
    A = (x, -0.115, -0.20)
    ell("body", A, (0.02, 0.02, 0.02), M["yellow"])
    def at(o, A=A):
        return (A[0] + o[0] * K, A[1] + o[1] * K, A[2] + o[2] * K)
    for psi in (-0.35, 0.0, 0.35):                       # tiga jari depan, menekuk ke bawah
        lx = math.sin(psi)
        toe = [at(o) for o in ((0, 0, 0), (lx * 0.035, 0.04, -0.01), (lx * 0.06, 0.07, -0.035), (lx * 0.075, 0.082, -0.072))]
        piped("body", toe, [0.014, 0.012, 0.0105, 0.009], M["yellow"], 8)
        claw = [at(o) for o in ((lx * 0.075, 0.082, -0.072), (lx * 0.082, 0.09, -0.10), (lx * 0.082, 0.088, -0.125))]
        piped("body", claw, [0.009, 0.006, 0.001], M["black"], 8)
    toe = [at(o) for o in ((0, 0, 0), (0, -0.03, -0.01), (0, -0.05, -0.035), (0, -0.055, -0.065))]   # jari belakang
    piped("body", toe, [0.014, 0.012, 0.0105, 0.009], M["yellow"], 8)
    claw = [at(o) for o in ((0, -0.055, -0.065), (0, -0.05, -0.095), (0, -0.035, -0.12))]
    piped("body", claw, [0.011, 0.007, 0.001], M["black"], 8)

# ---------------------------------------------------------------- ekor putih: 12 bulu mengipas
for i in range(12):
    b = r(-26 + 4.73 * i)
    fea("tail", (0, -0.30, 0.0015 * (6 - abs(i - 5.5))), (math.sin(b), -math.cos(b)), 0.42, 0.05, M["tail"], 0.04)

# ---------------------------------------------------------------- sayap: tulang depan + bulu berlapis
def build_wing(s):
    nm = "R" if s > 0 else "L"
    sh, el, wr = "sh" + nm, "el" + nm, "wr" + nm
    pivot(sh, "body", (s * 0.12, 0.08, 0.06))
    pivot(el, sh, (s * 0.50, 0.06, 0.07))
    pivot(wr, el, (s * 0.85, 0.07, 0.06))

    # tepi depan sayap (otot dan tulang)
    for par, pts, rad in (
        (sh, [(s * 0.12, 0.08, 0.06), (s * 0.31, 0.085, 0.065), (s * 0.50, 0.08, 0.065)], [0.05, 0.045, 0.04]),
        (el, [(s * 0.50, 0.08, 0.065), (s * 0.68, 0.09, 0.062), (s * 0.85, 0.09, 0.06)], [0.04, 0.034, 0.028]),
        (wr, [(s * 0.85, 0.09, 0.06), (s * 0.92, 0.095, 0.058), (s * 0.98, 0.09, 0.055)], [0.028, 0.02, 0.011]),
    ):
        piped(par, pts, rad, M["lead"])

    # lengan atas
    for i in range(5):
        x, z = 0.16 + 0.075 * i, 0.0008 * i
        d = (s * 0.04, -1)
        fea(sh, (s * x, 0.0, 0.050 + z), d, 0.30, 0.055, M["sec"])
        fea(sh, (s * x, 0.06, 0.0555 + z), d, 0.14, 0.05, M["gc"])
        fea(sh, (s * (x + 0.01), 0.085, 0.061 + z), d, 0.10, 0.04, M["mc"])
        fea(sh, (s * (x + 0.02), 0.095, 0.0665 + z), d, 0.07, 0.035, M["lc"])

    # lengan bawah
    for i in range(7):
        x, z = 0.52 + 0.05 * i, 0.0008 * i
        d = (s * (0.05 + 0.05 * i), -1)
        fea(el, (s * x, 0.0, 0.050 + z), d, 0.32 + 0.012 * i, 0.045, M["sec"])
        fea(el, (s * x, 0.07, 0.0555 + z), d, 0.15, 0.045, M["gc"])
        fea(el, (s * (x + 0.01), 0.088, 0.061 + z), d, 0.10, 0.04, M["mc"])
        fea(el, (s * (x + 0.02), 0.098, 0.0665 + z), d, 0.07, 0.035, M["lc"])

    # tangan: 10 bulu primer mengipas seperti jari
    for i in range(10):
        a = r(-62 + 8 * i)
        d = (s * math.cos(a), math.sin(a))
        ln = (0.30 + 0.17 * math.sin(math.pi * (i + 0.8) / 10.4)) * 0.92
        root = (s * (0.86 + 0.012 * i), 0.05 - 0.004 * i, 0.048 + 0.0008 * i)
        fea(wr, root, d, ln, 0.032, M["prim"])
        fea(wr, (root[0], root[1] + 0.03, 0.054 + 0.0008 * i), d, 0.16, 0.03, M["gc"])
    for i in range(5):
        a = r(-40 + 12 * i)
        fea(wr, (s * (0.84 + 0.03 * i), 0.098, 0.067 + 0.0008 * i), (s * math.cos(a), math.sin(a)), 0.08, 0.03, M["lc"])

build_wing(1)     # sayap kanan (+X)
build_wing(-1)    # sayap kiri (-X)

# ---------------------------------------------------------------- animasi
def smooth(x, a, b):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def env(t):
    """1 = mengepak penuh, 0 = melayang. Pola: mengepak ~2,4 kepak lalu melayang."""
    u = (t % 120) / 120.0
    return smooth(u, 0.02, 0.10) * (1 - smooth(u, 0.60, 0.72))

def rot(name, f, x=0.0, y=0.0, z=0.0):
    e = piv[name][0]
    e.rotation_euler = (x, y, z)
    e.keyframe_insert("rotation_euler", frame=f)

def loc(name, f, v):
    e = piv[name][0]
    e.location = v
    e.keyframe_insert("location", frame=f)

body_rest = piv["body"][0].location.copy()

for f in range(1, FRAMES + 1):
    t = f - 1
    th = TAU * t / FRAMES         # sudut pada lingkaran
    ph = TAU * t / FLAP           # fase kepakan (0 = sayap di puncak)
    e = env(t)

    # lintasan melingkar berlawanan jarum jam, badan miring ke dalam
    loc("path", f, (RADIUS * math.cos(th), RADIUS * math.sin(th), HEIGHT + 0.25 * math.sin(2 * th)))
    rot("path", f, z=th)
    rot("bank", f, y=-r(BANK))

    # sayap. Sudut positif = ujung sayap ke atas.
    flap = r(6) + e * (r(2) + r(38) * math.cos(ph))
    wrist = r(6) + e * r(24) * math.sin(ph - 0.35)      # ujung sayap tertinggal
    elbow = e * r(10) * math.sin(ph - 0.2)
    fold = r(3) + e * r(22) * max(0.0, -math.sin(ph))   # sayap menekuk ke belakang saat naik
    for s, nm in ((1, "R"), (-1, "L")):
        rot("sh" + nm, f, y=-s * flap, z=-s * r(3))
        rot("el" + nm, f, y=-s * elbow, z=-s * fold)
        rot("wr" + nm, f, y=-s * wrist, z=-s * 0.6 * fold)

    # badan naik saat sayap turun, kepala tetap stabil, ekor ikut menyeimbangkan
    loc("body", f, (body_rest.x, body_rest.y, body_rest.z + 0.05 * e * math.sin(ph)))
    rot("body", f, x=e * r(3) * math.sin(ph + 0.4))
    rot("neck", f, x=-r(4))
    rot("head", f, x=-e * r(3) * math.sin(ph + 0.4), z=r(12) + r(6) * math.sin(3 * th))
    rot("tail", f, x=r(4) + e * r(6) * math.sin(ph + 0.9))

# ---------------------------------------------------------------- panggung
bpy.ops.mesh.primitive_plane_add(size=300)
bpy.context.active_object.data.materials.append(M["ground"])

sun_d = bpy.data.lights.new("Sun", "SUN")
sun_d.energy = 4.0
sun_d.angle = r(3)
sun = bpy.data.objects.new("Sun", sun_d)
sun.rotation_euler = (r(50), 0, r(30))
scene.collection.objects.link(sun)

cam_d = bpy.data.cameras.new("Cam")
cam_d.lens = 35
cam = bpy.data.objects.new("Cam", cam_d)
cam.location = (0, -13, HEIGHT + 2.5)
scene.collection.objects.link(cam)
target = bpy.data.objects.new("Target", None)
target.location = (0, 0, HEIGHT)
scene.collection.objects.link(target)
tc = cam.constraints.new("TRACK_TO")
tc.target, tc.track_axis, tc.up_axis = target, "TRACK_NEGATIVE_Z", "UP_Y"
scene.camera = cam

world = bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs["Color"].default_value = (0.30, 0.50, 0.85, 1)
bg.inputs["Strength"].default_value = 1.0

# ---------------------------------------------------------------- render (opsional)
if "--render" in argv:
    scene.render.image_settings.file_format = "FFMPEG"
    scene.render.ffmpeg.format = "MPEG4"
    scene.render.ffmpeg.codec = "H264"
    scene.render.filepath = "//eagle_fly.mp4"
    bpy.ops.render.render(animation=True)
elif "--still" in argv:
    scene.frame_set(20)
    scene.render.filepath = "//eagle_still.png"
    bpy.ops.render.render(write_still=True)
else:
    scene.frame_set(1)

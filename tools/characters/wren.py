"""Wren, built and animated in Blender (CHR-02, CHR-03): a small round bird in an ink-blue cowl with the
needle-quill, rendered side-on to frames the packer turns into sprite sheets.

Run from the repo root, then pack:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/wren.py
    python tools/characters/pack.py wren
    ... -P tools/characters/wren.py -- idle run          (only those clips)
    ... -P tools/characters/wren.py -- turnaround        (the model sheet only)

The model is parts on a hierarchy of empties (root, hips, body, head, wings, tail, quill, legs), so every
clip is a function of time that sets a few rotations and offsets: no armature, nothing baked. Frames are
rendered at twice the game's density (art-direction 4: authored at 2x, 96 px per unit in game) with the
same flat washes and Freestyle ink line as the paper kits, so she and the world are one drawing.
"""
import bpy, bmesh, json, math, os, sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES = os.path.join(ROOT, "tools", "characters", ".frames", "wren")   # intermediate, not imported by Unity, not committed
os.makedirs(FRAMES, exist_ok=True)

PPU = 96          # in game
SCALE = 2         # authored at 2x
CELL = 2.0        # units per frame cell: room for the quill and the jumps
RES = int(CELL * PPU * SCALE)   # 384 px

# Wren's colours (art-direction 4)
BROWN = (0.42, 0.38, 0.34)
BROWN_DARK = (0.33, 0.29, 0.26)
CREAM = (0.93, 0.88, 0.76)
COWL = (0.16, 0.22, 0.40)
BRASS = (0.78, 0.62, 0.30)
INK = (0.08, 0.10, 0.16)
BEAK = (0.55, 0.42, 0.22)
PAPER = (0.93, 0.89, 0.80)

D = math.radians


# ---------------------------------------------------------------- scene plumbing

def reset_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)


def flat_material(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nodes = m.node_tree.nodes
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new("ShaderNodeOutputMaterial")
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (*rgb, 1.0)
    emit.inputs["Strength"].default_value = 1.0
    m.node_tree.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    return m


MATS = {}


def mat(name, rgb):
    if name not in MATS:
        MATS[name] = flat_material(name, rgb)
    return MATS[name]


def link(ob, parent=None):
    bpy.context.scene.collection.objects.link(ob)
    if parent is not None:
        ob.parent = parent
    return ob


def empty(name, parent=None, loc=(0, 0, 0)):
    ob = bpy.data.objects.new(name, None)
    ob.location = loc
    return link(ob, parent)


def from_bmesh(name, bm, material, parent, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), smooth=True):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    ob.location = loc
    ob.rotation_euler = rot
    ob.scale = scale
    ob.data.materials.append(material)
    return link(ob, parent)


def sphere(name, r, material, parent, loc=(0, 0, 0), scale=(1, 1, 1), rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=r)
    return from_bmesh(name, bm, material, parent, loc, rot, scale)


def cone(name, r1, r2, depth, material, parent, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=r1, radius2=r2, depth=depth)
    return from_bmesh(name, bm, material, parent, loc, rot, scale)


def cube(name, size, material, parent, loc=(0, 0, 0), rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    return from_bmesh(name, bm, material, parent, loc, rot, size, smooth=False)


# ---------------------------------------------------------------- Wren

class Wren:
    """Her parts, with their rest transforms, so a pose is a small set of changes from rest."""

    def __init__(self):
        self.root = empty("root")
        self.hips = empty("hips", self.root, (0, 0, 0.32))
        self.body = empty("body", self.hips, (0, 0, 0.30))
        self.head = empty("head", self.body, (0.14, 0, 0.42))
        self.wing_near = empty("wing_near", self.body, (0.0, -0.27, 0.12))
        self.wing_far = empty("wing_far", self.body, (0.0, 0.27, 0.12))
        self.quill = empty("quill", self.wing_near, (0.16, -0.1, -0.14))
        self.tail = empty("tail", self.body, (-0.30, 0, -0.02))
        self.leg_l = empty("leg_l", self.root, (0.05, -0.1, 0.32))
        self.leg_r = empty("leg_r", self.root, (0.05, 0.1, 0.32))

        brown, dark, cream, cowl = mat("brown", BROWN), mat("dark", BROWN_DARK), mat("cream", CREAM), mat("cowl", COWL)
        brass, ink, beak = mat("brass", BRASS), mat("ink", INK), mat("beak", BEAK)
        sphere("body_mesh", 0.33, brown, self.body, scale=(1.0, 0.85, 1.05))
        sphere("breast", 0.25, cream, self.body, loc=(0.14, 0, -0.09), scale=(0.9, 0.8, 1.0))
        cone("cowl", 0.41, 0.19, 0.44, cowl, self.body, loc=(0.0, 0, 0.14))
        sphere("clasp", 0.045, brass, self.body, loc=(0.20, -0.24, 0.28))
        sphere("head_mesh", 0.21, brown, self.head, scale=(1.05, 0.95, 1.0))
        cone("beak", 0.07, 0.006, 0.2, beak, self.head, loc=(0.27, 0, -0.02), rot=(0, D(90), 0))
        sphere("eye", 0.035, ink, self.head, loc=(0.10, -0.19, 0.05))
        sphere("glint", 0.011, mat("paper", PAPER), self.head, loc=(0.115, -0.222, 0.062))
        cone("crest", 0.05, 0.005, 0.16, dark, self.head, loc=(-0.08, 0, 0.19), rot=(0, D(-35), 0))
        sphere("wing_near_mesh", 0.2, dark, self.wing_near, loc=(-0.05, -0.04, -0.08), scale=(1.3, 0.35, 0.7), rot=(0, D(-15), 0))
        sphere("wing_far_mesh", 0.2, dark, self.wing_far, loc=(-0.05, 0.04, -0.08), scale=(1.3, 0.35, 0.7), rot=(0, D(-15), 0))
        cube("tail_mesh", (0.36, 0.16, 0.06), dark, self.tail, loc=(-0.14, 0, 0.04), rot=(0, D(-25), 0))
        cone("quill_mesh", 0.022, 0.016, 1.1, ink, self.quill, loc=(0, 0, 0.25))
        cone("nib", 0.02, 0.0, 0.1, brass, self.quill, loc=(0, 0, 0.85))
        sphere("quill_end", 0.03, brass, self.quill, loc=(0, 0, -0.30))
        for leg, side in ((self.leg_l, -1), (self.leg_r, 1)):
            cone("shin" + leg.name[-2:], 0.026, 0.022, 0.32, dark, leg, loc=(0, 0, -0.16))
            cube("foot" + leg.name[-2:], (0.17, 0.06, 0.03), dark, leg, loc=(0.05, 0, -0.32))
        self.parts = [self.root, self.hips, self.body, self.head, self.wing_near, self.wing_far, self.quill, self.tail, self.leg_l, self.leg_r]
        self.rest = {p.name: (tuple(p.location), tuple(p.rotation_euler), tuple(p.scale)) for p in self.parts}
        self.quill.rotation_euler = (0, D(30), 0)   # held up and forward
        self.rest["quill"] = (tuple(self.quill.location), tuple(self.quill.rotation_euler), tuple(self.quill.scale))

    def reset(self):
        for p in self.parts:
            l, r, s = self.rest[p.name]
            p.location, p.rotation_euler, p.scale = l, r, s

    # helpers: degrees, offsets from rest
    def rot(self, part, x=0.0, y=0.0, z=0.0):
        r = self.rest[part.name][1]
        part.rotation_euler = (r[0] + D(x), r[1] + D(y), r[2] + D(z))

    def move(self, part, x=0.0, y=0.0, z=0.0):
        l = self.rest[part.name][0]
        part.location = (l[0] + x, l[1] + y, l[2] + z)

    def scale(self, part, x=1.0, y=1.0, z=1.0):
        part.scale = (x, y, z)

    def flap(self, a):
        """Both wings up by a degrees (about the body's forward axis)."""
        self.rot(self.wing_near, x=a)
        self.rot(self.wing_far, x=-a)

    def legs(self, l, r):
        """Legs swung forward by l and r degrees."""
        self.rot(self.leg_l, y=l)
        self.rot(self.leg_r, y=r)


# ---------------------------------------------------------------- clips
# Each clip: (fps, frames, loop, pose(w, i, n)). Rotations about Y lean forward for positive angles.

def idle(w, i, n):
    b = math.sin(2 * math.pi * i / n)
    w.move(w.body, z=0.02 * b)
    w.rot(w.head, y=-3 * b)
    w.rot(w.tail, y=4 * b)
    w.rot(w.wing_near, y=3 * b)
    w.rot(w.quill, y=2 * b)


def run(w, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.legs(38 * s, -38 * s)
    w.move(w.body, z=0.05 * abs(math.sin(2 * math.pi * t)))
    w.rot(w.body, y=10)
    w.rot(w.head, y=-6 + 2 * s)
    w.rot(w.wing_near, y=-25)
    w.rot(w.wing_far, y=-25)
    w.rot(w.tail, y=15)
    w.rot(w.quill, y=12)


def jump(w, i, n):
    if i == 0:
        w.move(w.body, z=-0.07)
        w.legs(20, 20)
        w.scale(w.body, 1.1, 1, 0.9)
        w.flap(-10)
        return
    t = (i - 1) / max(1, n - 2)
    w.legs(-35, -45)
    w.flap(50 - 15 * t)
    w.rot(w.body, y=5)
    w.rot(w.head, y=-8)
    w.rot(w.tail, y=-10)
    w.scale(w.body, 0.96, 1, 1.06)


def fall(w, i, n):
    t = i / n
    w.flap(22 + 10 * math.sin(2 * math.pi * t))
    w.legs(15, 10)
    w.rot(w.head, y=8)
    w.rot(w.tail, y=8)
    w.rot(w.quill, y=-10)


def glide(w, i, n):
    t = i / n
    w.flap(72 + 8 * math.sin(2 * math.pi * t))
    w.legs(-30, -30)
    w.rot(w.body, y=-5)
    w.rot(w.tail, y=-12)


def land(w, i, n):
    k = (1.15, 1.06, 0.98)[i] if i < 3 else 1.0
    w.scale(w.body, k, 1, 2.0 - k)
    w.move(w.body, z=-0.08 * (1 - i / 3))
    w.legs(25 * (1 - i / 3), 25 * (1 - i / 3))
    w.flap(20 * (1 - i / 3))


def cling(w, i, n):
    w.rot(w.body, y=-12)
    w.flap(40 + 5 * i)
    w.legs(30, 25)
    w.rot(w.quill, y=-25)
    w.rot(w.head, y=-10)


def dash(w, i, n):
    w.rot(w.body, y=22)
    w.scale(w.body, 1.25, 1, 0.85)
    w.legs(-45, -50)
    w.flap(-8)
    w.rot(w.quill, y=55)
    w.rot(w.tail, y=25)


def thread(w, i, n):
    w.rot(w.body, y=28)
    w.scale(w.body, 1.15, 1, 0.9)
    w.legs(-40, -45)
    w.flap(35)
    w.rot(w.quill, y=45)
    w.move(w.quill, x=0.1, z=0.1)


def strike1(w, i, n):   # the forward thrust
    q = (-25, 60, 68, 50, 30, 12)[i]
    lean = (-5, 12, 14, 8, 4, 0)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=(0, 0.12, 0.16, 0.08, 0, 0)[i])
    w.rot(w.body, y=lean)
    w.rot(w.head, y=-lean * 0.5)
    w.flap((5, -10, -10, -5, 0, 0)[i])


def strike2(w, i, n):   # the rising slash
    q = (75, 40, -5, -25, -15, 0)[i]
    w.rot(w.quill, y=q)
    w.rot(w.body, y=(8, 4, -4, -8, -4, 0)[i])
    w.rot(w.head, y=(4, 0, -8, -10, -6, 0)[i])
    w.flap((-5, 5, 20, 25, 15, 5)[i])


def strike3(w, i, n):   # the overhead stab
    q = (-40, 10, 75, 90, 70, 45)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=(0, 0.05, 0.14, 0.18, 0.1, 0.02)[i], z=(0.08, 0.06, 0, -0.04, 0, 0)[i])
    w.rot(w.body, y=(-8, 0, 12, 16, 10, 4)[i])
    w.rot(w.head, y=(-6, -2, 6, 8, 4, 0)[i])
    w.flap((15, 5, -10, -12, -5, 0)[i])


def strike_up(w, i, n):
    q = (30, -10, -35, -30, -15, 0)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, z=(0, 0.08, 0.14, 0.12, 0.06, 0)[i])
    w.rot(w.body, y=(4, -4, -8, -6, -2, 0)[i])
    w.rot(w.head, y=(0, -8, -14, -12, -6, 0)[i])
    w.flap((0, 10, 20, 15, 8, 0)[i])


def pogo(w, i, n):   # the down-strike: the quill under her
    q = (90, 140, 152, 140)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=-0.05, z=(0, -0.1, -0.16, -0.1)[i])
    w.legs(-30, -30)
    w.scale(w.body, 0.95, 1, 1.06)
    w.flap((10, 30, 35, 30)[i])
    w.rot(w.head, y=(6, 12, 14, 12)[i])


def bind(w, i, n):   # the quill circles her: she redraws her own outline
    t = i / n
    w.move(w.quill, x=-0.16, z=0.14)
    w.rot(w.quill, y=-30 + 360 * t)
    w.rot(w.head, y=6)
    w.move(w.body, z=0.01 * math.sin(2 * math.pi * t))
    w.flap(5)


def survey(w, i, n):   # the quill held high; she looks out
    t = i / n
    w.rot(w.quill, y=-32 + 2 * math.sin(2 * math.pi * t))
    w.move(w.quill, z=0.12)
    w.rot(w.head, y=-16)
    w.rot(w.body, y=-4)
    w.rot(w.tail, y=-6)
    w.flap(6)


def hurt(w, i, n):
    k = (1.0, 0.7, 0.35)[i]
    w.rot(w.body, y=-22 * k)
    w.rot(w.head, y=-14 * k)
    w.flap(45 * k)
    w.legs(20 * k, 15 * k)
    w.rot(w.quill, y=70 * k)
    w.move(w.body, x=-0.06 * k)


def death(w, i, n):
    t = min(1.0, i / 5)
    w.rot(w.body, y=-85 * t)
    w.move(w.body, z=-0.28 * t, x=-0.15 * t)
    w.rot(w.head, y=-30 * t)
    w.flap(60 * t)
    w.legs(40 * t, 30 * t)
    w.rot(w.quill, y=120 * t)
    w.move(w.quill, x=0.1 * t, z=-0.2 * t)
    w.rot(w.hips, y=-40 * t)


CLIPS = [
    ("idle", 12, 8, True, idle),
    ("run", 12, 8, True, run),
    ("jump", 12, 4, False, jump),
    ("fall", 12, 4, True, fall),
    ("glide", 12, 4, True, glide),
    ("land", 12, 3, False, land),
    ("cling", 12, 2, True, cling),
    ("dash", 24, 3, False, dash),
    ("thread", 24, 2, True, thread),
    ("strike1", 24, 6, False, strike1),
    ("strike2", 24, 6, False, strike2),
    ("strike3", 24, 6, False, strike3),
    ("strike_up", 24, 6, False, strike_up),
    ("pogo", 24, 4, False, pogo),
    ("bind", 12, 8, True, bind),
    ("survey", 12, 6, True, survey),
    ("hurt", 12, 3, False, hurt),
    ("death", 12, 8, False, death),
]


# ---------------------------------------------------------------- rendering

def setup_render(size_px, ortho_units, look_at_z, yaw=0.0):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 16
    sc.cycles.use_denoising = False
    sc.render.film_transparent = True
    sc.render.resolution_x = sc.render.resolution_y = size_px
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    try:
        sc.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    sc.view_settings.look = "None"
    cam = bpy.data.objects.get("Cam")
    if cam is None:
        cam_data = bpy.data.cameras.new("Cam")
        cam_data.type = "ORTHO"
        cam = bpy.data.objects.new("Cam", cam_data)
        sc.collection.objects.link(cam)
    cam.data.ortho_scale = ortho_units
    cam.location = (10 * math.sin(yaw), -10 * math.cos(yaw), look_at_z)
    cam.rotation_euler = (D(90), 0, yaw)
    sc.camera = cam
    sc.render.use_freestyle = True
    sc.render.line_thickness_mode = "ABSOLUTE"
    line = 3.2
    sc.render.line_thickness = line
    fs = sc.view_layers[0].freestyle_settings
    fs.crease_angle = D(110)
    while fs.linesets:
        fs.linesets.remove(fs.linesets[0])
    ls = fs.linesets.new("Ink")
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = True
    ls.select_contour = False
    st = ls.linestyle
    st.color = INK
    st.thickness = line
    st.caps = "ROUND"
    st.use_chaining = True
    st.chaining = "PLAIN"
    noise = st.geometry_modifiers.new("Pen", "PERLIN_NOISE_1D")
    noise.frequency = 10.0
    noise.amplitude = 0.5
    noise.octaves = 2
    cal = st.thickness_modifiers.new("Nib", "CALLIGRAPHY")
    cal.orientation = D(35)
    cal.thickness_min = line * 0.55
    cal.thickness_max = line * 1.35
    along = st.thickness_modifiers.new("Taper", "ALONG_STROKE")
    along.mapping = "CURVE"
    along.influence = 0.4
    return sc


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return os.path.getsize(path)


def main():
    only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    reset_scene()
    MATS.clear()
    w = Wren()
    meta = {"character": "Wren", "ppu": PPU, "scale": SCALE, "cell": CELL, "clips": []}
    if not only or any(c[0] in only for c in CLIPS):
        setup_render(RES, CELL, CELL / 2)
        for name, fps, n, loop, pose in CLIPS:
            if only and name not in only:
                continue
            for i in range(n):
                w.reset()
                pose(w, i, n)
                render(os.path.join(FRAMES, "%s_%02d.png" % (name, i)))
            print("[wren] %s: %d frames at %d fps" % (name, n, fps))
        for name, fps, n, loop, pose in CLIPS:
            meta["clips"].append({"name": name, "fps": fps, "frames": n, "loop": loop})
        with open(os.path.join(FRAMES, "clips.json"), "w") as f:
            json.dump(meta, f, indent=2)
    if not only or "turnaround" in only:
        w.reset()
        for label, yaw in (("side", 0.0), ("three_quarter", D(-45)), ("front", D(-90)), ("back", D(90))):
            setup_render(RES * 2, CELL, CELL / 2, yaw=yaw)
            render(os.path.join(FRAMES, "turnaround_%s.png" % label))
        print("[wren] turnaround: side, three-quarter, front, back")


if __name__ == "__main__":
    main()

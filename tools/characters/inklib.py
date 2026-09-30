"""Shared plumbing for the characters drawn in Blender (CHR-02, CHR-03, CHR-06): flat washes, the Freestyle ink line
the paper kits use, primitives from bmesh, a rig of empties whose pose is a few rotations and offsets from rest,
and the render loop that turns a list of clips into frames for pack.py.

Imported by wren.py and saltmarrow_enemies.py; not run on its own.
"""
import bpy, bmesh, json, math, os, sys

D = math.radians
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES_ROOT = os.path.join(ROOT, "tools", "characters", ".frames")   # intermediate, not imported by Unity, not committed

PPU = 96          # in game
SCALE = 2         # authored at 2x (art-direction 4)
INK = (0.08, 0.10, 0.16)
PAPER = (0.93, 0.89, 0.80)


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


# ---------------------------------------------------------------- scene plumbing

def reset_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    MATS.clear()


def flat_material(name, rgb):
    """An unlit wash: exact palette colour, no shading, so the drawing stays a drawing."""
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
    """A cone or cylinder along its local Z, centred."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=r1, radius2=r2, depth=depth)
    return from_bmesh(name, bm, material, parent, loc, rot, scale)


def cube(name, size, material, parent, loc=(0, 0, 0), rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    return from_bmesh(name, bm, material, parent, loc, rot, size, smooth=False)


def slab(name, points_xz, thickness, material, parent, loc=(0, 0, 0), rot=(0, 0, 0)):
    """A flat cut-out with a little depth: a closed outline in (x, z), extruded along y."""
    bm = bmesh.new()
    verts = [bm.verts.new((x, -thickness / 2, z)) for x, z in points_xz]
    face = bm.faces.new(verts)
    res = bmesh.ops.extrude_face_region(bm, geom=[face])
    moved = [e for e in res["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, thickness, 0), verts=moved)
    bm.normal_update()
    return from_bmesh(name, bm, material, parent, loc, rot, (1, 1, 1), smooth=False)


# ---------------------------------------------------------------- rig

class Rig:
    """Parts on a hierarchy of empties. A pose is a few rotations and offsets from the rest pose recorded by
    snapshot(); reset() puts everything back. Parts are attributes as well as names."""

    def __init__(self):
        self.parts = []
        self.rest = {}
        self.props = {}

    def add(self, name, parent=None, loc=(0, 0, 0), rot=(0, 0, 0)):
        e = empty(name, parent, loc)
        e.rotation_euler = rot
        self.parts.append(e)
        setattr(self, name, e)
        return e

    def snapshot(self):
        self.rest = {p.name: (tuple(p.location), tuple(p.rotation_euler), tuple(p.scale)) for p in self.parts}

    def reset(self):
        for p in self.parts:
            l, r, s = self.rest[p.name]
            p.location, p.rotation_euler, p.scale = l, r, s
        for name in self.props:
            self.show(name, False)

    def prop(self, name, root):
        """A part drawn only in the clips that ask for it (a net, a ledger): hidden at rest, with everything under it."""
        self.props[name] = root
        self.show(name, False)

    def show(self, name, on=True):
        root = self.props[name]
        for ob in [root] + list(root.children_recursive):
            ob.hide_render = not on

    def _part(self, part):
        return getattr(self, part) if isinstance(part, str) else part

    def rot(self, part, x=0.0, y=0.0, z=0.0):
        """Degrees from rest. About Y, a positive angle leans the top toward +X (forward)."""
        p = self._part(part)
        r = self.rest[p.name][1]
        p.rotation_euler = (r[0] + D(x), r[1] + D(y), r[2] + D(z))

    def move(self, part, x=0.0, y=0.0, z=0.0):
        p = self._part(part)
        l = self.rest[p.name][0]
        p.location = (l[0] + x, l[1] + y, l[2] + z)

    def scale(self, part, x=1.0, y=1.0, z=1.0):
        self._part(part).scale = (x, y, z)


# ---------------------------------------------------------------- rendering

def setup_render(size_px, ortho_units, centre_z, yaw=0.0, line=3.2, ink=INK):
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
    cam.location = (10 * math.sin(yaw), -10 * math.cos(yaw), centre_z)
    cam.rotation_euler = (D(90), 0, yaw)
    sc.camera = cam
    sc.render.use_freestyle = True
    sc.render.line_thickness_mode = "ABSOLUTE"
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
    st.color = ink
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


def run(character, cell, clips, build, centre_z, only=(), turnaround=True, line=3.2, ink=INK, ppu=PPU):
    """Render a character's clips (and its turnaround) to FRAMES_ROOT/<character lower>/.

    clips: (name, fps, frames, loop, pose) with pose(rig, i, n). build() returns the Rig, already snapshotted.
    cell: units per frame cell. centre_z: the world z the cell is centred on (feet at 0 → cell / 2)."""
    frames = os.path.join(FRAMES_ROOT, character.lower())
    os.makedirs(frames, exist_ok=True)
    reset_scene()
    rig = build()
    res = int(cell * ppu * SCALE)
    meta = {"character": character, "ppu": ppu, "scale": SCALE, "cell": cell, "clips": []}
    if not only or any(c[0] in only for c in clips):
        setup_render(res, cell, centre_z, line=line, ink=ink)
        for name, fps, n, loop, pose in clips:
            if only and name not in only:
                continue
            for i in range(n):
                rig.reset()
                pose(rig, i, n)
                render(os.path.join(frames, "%s_%02d.png" % (name, i)))
            print("[%s] %s: %d frames at %d fps" % (character.lower(), name, n, fps))
        for name, fps, n, loop, pose in clips:
            meta["clips"].append({"name": name, "fps": fps, "frames": n, "loop": loop})
        with open(os.path.join(frames, "clips.json"), "w") as f:
            json.dump(meta, f, indent=2)
    if turnaround and (not only or "turnaround" in only):
        rig.reset()
        for label, yaw in (("side", 0.0), ("three_quarter", D(-45)), ("front", D(-90)), ("back", D(90))):
            setup_render(res * 2, cell, centre_z, yaw=yaw, line=line, ink=ink)
            render(os.path.join(frames, "turnaround_%s.png" % label))
        print("[%s] turnaround: side, three-quarter, front, back" % character.lower())
    return rig


def argv_after_dashes():
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

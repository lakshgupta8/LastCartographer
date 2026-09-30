"""Saltmarrow paper kit (ENV-01, ENV-02): every backdrop strip and ground tile the coast's rooms use, rendered
from cut-out geometry with Freestyle ink lines in headless Blender.

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/saltmarrow.py
    ... -P tools/paperkit/saltmarrow.py -- Paper_Far_Tower Ground_Stone      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Saltmarrow/.
Every layer is a flat wash in the region's palette (art-direction 5) with a single ink line around each
cut-out; farther layers are washed toward the paper and drawn thinner (aerial perspective as thinning ink).
A "_Faded" layer is the same drawing with the geometry seeded the same way, washed further and drawn
thinner: the faded third lighthouse's look from the start. The geometry here is deliberately simple: an
artist reworks a layer by editing or replacing its shapes here, or by painting over the PNG at the same
size. Unity reads the PNG, not this file.
"""
import bpy, json, math, os, random, sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Saltmarrow")
os.makedirs(OUT, exist_ok=True)

# Saltmarrow palette (art-direction 5): paper, wash 1, wash 2, accent, ink.
PAPER = (0.93, 0.89, 0.80)
SILVER = (0.64, 0.66, 0.64)
OLIVE = (0.50, 0.54, 0.36)
RUST = (0.60, 0.36, 0.24)
INK = (0.08, 0.10, 0.16)

PPU = 40          # pixels per unit for parallax strips (background: half the sprite density)
TILE_PPU = 96     # pixels per unit for ground tiles (they sit on the gameplay plane)
STRIP_WIDTH = 80  # the quads MakePaperLayer makes are 80 units wide
FADED_WASH = 0.3  # how much further a _Faded layer is washed toward paper
FADED_LINE = 0.6  # and how much thinner its line is


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


class Palette:
    """Materials for one layer: every colour washed toward paper by the layer's depth (and more if faded)."""

    def __init__(self, depth, faded):
        self.depth = min(0.92, depth + (FADED_WASH if faded else 0.0))
        self._cache = {}

    def wash(self, rgb, extra=0.0):
        return lerp(rgb, PAPER, min(0.95, self.depth + extra))

    def ink(self):
        return self.wash(INK, 0.0)

    def __call__(self, name, rgb, extra=0.0):
        key = (name, extra)
        if key not in self._cache:
            self._cache[key] = flat_material(name, self.wash(rgb, extra))
        return self._cache[key]


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


def mesh_object(name, verts, faces, material, location=(0, 0, 0)):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    ob.location = location
    ob.data.materials.append(material)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def polygon(name, points_xz, material, y=0.0):
    """A filled cut-out from a closed 2D outline (x, z). Nearer the camera is more negative y."""
    verts = [(x, y, z) for x, z in points_xz]
    return mesh_object(name, verts, [list(range(len(verts)))], material)


def box(name, cx, cz, w, h, material, y=0.0, d=0.4):
    verts = [(cx - w / 2, y - d / 2, cz - h / 2), (cx + w / 2, y - d / 2, cz - h / 2),
             (cx + w / 2, y - d / 2, cz + h / 2), (cx - w / 2, y - d / 2, cz + h / 2),
             (cx - w / 2, y + d / 2, cz - h / 2), (cx + w / 2, y + d / 2, cz - h / 2),
             (cx + w / 2, y + d / 2, cz + h / 2), (cx - w / 2, y + d / 2, cz + h / 2)]
    faces = [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    return mesh_object(name, verts, faces, material)


def setup_render(width_units, height_units, ppu, bottom, line_thickness, ink_rgb, seed):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 16
    sc.cycles.use_denoising = False
    sc.render.film_transparent = True
    sc.render.resolution_x = int(round(width_units * ppu))
    sc.render.resolution_y = int(round(height_units * ppu))
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.image_settings.color_depth = "8"
    try:
        sc.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    sc.view_settings.look = "None"
    # Orthographic camera looking down +Y, framing exactly [-w/2, w/2] x [bottom, bottom + h].
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = max(width_units, height_units)
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    cam.location = (0.0, -20.0, bottom + height_units / 2)
    cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    # Freestyle ink: one line around every silhouette and crease, a little noise so it reads as a pen.
    sc.render.use_freestyle = True
    sc.render.line_thickness_mode = "ABSOLUTE"
    sc.render.line_thickness = line_thickness
    fs = sc.view_layers[0].freestyle_settings
    fs.crease_angle = math.radians(120)
    while fs.linesets:
        fs.linesets.remove(fs.linesets[0])
    ls = fs.linesets.new("Ink")
    ls.select_silhouette = True
    ls.select_border = True
    ls.select_crease = True
    ls.select_contour = False
    st = ls.linestyle
    st.color = ink_rgb
    st.thickness = line_thickness
    st.caps = "ROUND"
    st.use_chaining = True
    st.chaining = "PLAIN"
    noise = st.geometry_modifiers.new("Pen", "PERLIN_NOISE_1D")
    noise.frequency = 8.0
    noise.amplitude = 0.6
    noise.octaves = 2
    noise.seed = seed
    cal = st.thickness_modifiers.new("Nib", "CALLIGRAPHY")
    cal.orientation = math.radians(35)
    cal.thickness_min = line_thickness * 0.5
    cal.thickness_max = line_thickness * 1.4
    along = st.thickness_modifiers.new("Taper", "ALONG_STROKE")
    along.mapping = "CURVE"
    along.influence = 0.5
    return sc


def render(path):
    sc = bpy.context.scene
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return os.path.getsize(path)


# ---------------------------------------------------------------- shapes

def reed(name, x, base_z, height, lean, width, material, seed_head=None):
    """A reed blade: a thin cut-out tapering to a point, leaning a little, with an optional seed head."""
    top_x = x + lean
    pts = [(x - width / 2, base_z), (x + width / 2, base_z), (top_x + width * 0.03, base_z + height), (top_x - width * 0.03, base_z + height)]
    ob = polygon(name, pts, material)
    if seed_head is not None:
        hw, hh = width * 1.4, height * 0.22
        polygon(name + "_head", [(top_x - hw / 2, base_z + height - hh * 0.3), (top_x + hw / 2, base_z + height - hh * 0.3),
                                 (top_x + hw * 0.15, base_z + height + hh), (top_x - hw * 0.15, base_z + height + hh)], seed_head, y=-0.01)
    return ob


def reed_bank(rng, x0, x1, base_z, count, h_range, blade, head, spacing_jitter=0.6, width=0.10, head_chance=0.55):
    step = (x1 - x0) / count
    for i in range(count):
        x = x0 + step * (i + 0.5) + rng.uniform(-spacing_jitter, spacing_jitter) * step
        h = rng.uniform(*h_range)
        reed("reed_%d_%d" % (int(base_z * 10), i), x, base_z, h, rng.uniform(-0.35, 0.35) * h * 0.35, width * rng.uniform(0.8, 1.3), blade,
             head if (head is not None and rng.random() < head_chance) else None)


def ridge(name, rng, x0, x1, base_z, mean_h, amp, segments, material, y=0.0):
    """A hill, dune or crust line: a filled outline with a wandering top edge."""
    pts = [(x0, base_z)]
    for i in range(segments + 1):
        t = i / segments
        x = x0 + (x1 - x0) * t
        z = base_z + mean_h + amp * (math.sin(t * math.pi * rng.uniform(1.5, 3.5) + rng.uniform(0, 6.28)) * 0.6 + rng.uniform(-0.4, 0.4))
        pts.append((x, z))
    pts.append((x1, base_z))
    return polygon(name, pts, material, y=y)


def roost(name, x, ground_z, w, h, stilt_h, walls, roof, stilt, y=0.0):
    """A stilt-roost: a hut on two posts with a peaked roof and a dark doorway."""
    z0 = ground_z + stilt_h
    box(name + "_body", x, z0 + h / 2, w, h, walls, y=y)
    peak = h * 0.55 + w * 0.25
    polygon(name + "_roof", [(x - w * 0.62, z0 + h), (x + w * 0.62, z0 + h), (x, z0 + h + peak)], roof, y=y - 0.05)
    polygon(name + "_door", [(x - w * 0.12, z0), (x + w * 0.12, z0), (x + w * 0.12, z0 + h * 0.55), (x - w * 0.12, z0 + h * 0.55)], stilt, y=y - 0.1)
    for sx in (-w * 0.36, w * 0.36):
        box(name + "_stilt", x + sx, ground_z + stilt_h / 2, 0.14, stilt_h, stilt, y=y + 0.05)
    box(name + "_beam", x, z0 - 0.05, w * 1.05, 0.12, stilt, y=y + 0.02)


def lighthouse(name, x, ground_z, base_w, height, walls, lamp, dark, y=0.0):
    """A tapered tower with a gallery, a lamp room and a cone roof."""
    top_w = base_w * 0.62
    polygon(name + "_tower", [(x - base_w / 2, ground_z), (x + base_w / 2, ground_z), (x + top_w / 2, ground_z + height), (x - top_w / 2, ground_z + height)], walls, y=y)
    box(name + "_gallery", x, ground_z + height + 0.08, top_w * 1.5, 0.16, dark, y=y - 0.05)
    lamp_h = base_w * 0.55
    box(name + "_lamp", x, ground_z + height + 0.16 + lamp_h / 2, top_w * 0.9, lamp_h, lamp, y=y - 0.05)
    polygon(name + "_roof", [(x - top_w * 0.7, ground_z + height + 0.16 + lamp_h), (x + top_w * 0.7, ground_z + height + 0.16 + lamp_h),
                             (x, ground_z + height + 0.16 + lamp_h + top_w * 0.6)], dark, y=y - 0.08)
    for i in range(1, 4):
        box(name + "_band%d" % i, x, ground_z + height * i / 4, base_w - (base_w - top_w) * i / 4 + 0.02, 0.08, dark, y=y - 0.02)
    box(name + "_door", x, ground_z + base_w * 0.35, base_w * 0.22, base_w * 0.7, dark, y=y - 0.1)


def chapel(name, x, ground_z, width, walls, roof, dark, salt, rng, y=0.0):
    """The Salt Chapel: a nave with a gable, a bell tower, buttresses, its edges eaten by salt."""
    nave_h = width * 0.6
    box(name + "_nave", x, ground_z + nave_h / 2, width, nave_h, walls, y=y)
    polygon(name + "_gable", [(x - width * 0.55, ground_z + nave_h), (x + width * 0.55, ground_z + nave_h), (x, ground_z + nave_h + width * 0.42)], roof, y=y - 0.05)
    tw = width * 0.22
    tower_x = x + width * 0.42
    tower_h = nave_h * 1.7
    box(name + "_tower", tower_x, ground_z + tower_h / 2, tw, tower_h, walls, y=y - 0.1)
    box(name + "_belfry", tower_x, ground_z + tower_h * 0.86, tw * 0.5, tw * 0.6, dark, y=y - 0.15)
    polygon(name + "_spire", [(tower_x - tw * 0.6, ground_z + tower_h), (tower_x + tw * 0.6, ground_z + tower_h), (tower_x, ground_z + tower_h + tw * 1.4)], roof, y=y - 0.12)
    for i, bx in enumerate((-0.42, -0.14, 0.14)):
        polygon(name + "_buttress%d" % i, [(x + bx * width - 0.25, ground_z), (x + bx * width + 0.25, ground_z), (x + bx * width + 0.1, ground_z + nave_h * 0.7),
                                           (x + bx * width - 0.1, ground_z + nave_h * 0.7)], walls, y=y - 0.15)
    box(name + "_door", x - width * 0.28, ground_z + nave_h * 0.25, width * 0.09, nave_h * 0.5, dark, y=y - 0.2)
    for i in range(3):
        polygon(name + "_window%d" % i, [(x + (i - 1) * width * 0.18 - 0.12, ground_z + nave_h * 0.45), (x + (i - 1) * width * 0.18 + 0.12, ground_z + nave_h * 0.45),
                                         (x + (i - 1) * width * 0.18 + 0.12, ground_z + nave_h * 0.75), (x + (i - 1) * width * 0.18, ground_z + nave_h * 0.85),
                                         (x + (i - 1) * width * 0.18 - 0.12, ground_z + nave_h * 0.75)], dark, y=y - 0.2)
    # salt crust climbing the walls
    for i in range(14):
        sx = x + rng.uniform(-0.5, 0.5) * width
        box(name + "_salt%d" % i, sx, ground_z + rng.uniform(0.1, nave_h * 0.35), rng.uniform(0.3, 0.9), rng.uniform(0.15, 0.4), salt, y=y - 0.25)


# ---------------------------------------------------------------- layers

def layer_fore_reeds(rng, faded):
    """Paper_Fore_Reeds: 80 x 1.6 units at z = -4, the reeds in front of the walkway. Dark, thick line."""
    p = Palette(0.0, faded)
    blade = p("blade", lerp(OLIVE, INK, 0.45))
    head = p("head", lerp(RUST, INK, 0.35))
    reed_bank(rng, -40, 40, -0.3, 150, (0.9, 1.7), blade, head, width=0.16)
    return (STRIP_WIDTH, 1.6, PPU, -0.3, 3.0, p.ink())


def layer_mid_reeds(rng, faded):
    """Paper_Mid_Reeds: 80 x 6 units at z = 3, the reed bank behind the quay with a salt-flat foot."""
    p = Palette(0.15, faded)
    flat = p("flat", SILVER, 0.2)
    blade = p("blade", OLIVE)
    blade2 = p("blade2", lerp(OLIVE, SILVER, 0.5))
    head = p("head", RUST, 0.1)
    ridge("saltflat", rng, -41, 41, 0.0, 0.7, 0.25, 24, flat, y=0.3)
    reed_bank(rng, -40, 40, 0.3, 90, (2.4, 4.6), blade2, head, width=0.12)
    reed_bank(rng, -40, 40, 0.5, 130, (1.6, 3.6), blade, head, width=0.12)
    return (STRIP_WIDTH, 6, PPU, 0.0, 2.0, p.ink())


def layer_far_roosts(rng, faded):
    """Paper_Far_Roosts: 80 x 10 units at z = 8, the stilt-roosts of the quay, a jetty and its ropes."""
    p = Palette(0.45, faded)
    walls = p("walls", SILVER)
    roof = p("roof", RUST, 0.1)
    stilt = p("stilt", lerp(INK, SILVER, 0.5))
    water = p("water", lerp(SILVER, PAPER, 0.5))
    ridge("shallows", rng, -41, 41, 2.0, 0.5, 0.15, 30, water, y=0.5)
    xs = [-34, -27, -21, -13, -6, 2, 9, 15, 22, 29, 36]
    for i, x in enumerate(xs):
        w = rng.uniform(1.6, 2.8)
        roost("roost_%d" % i, x + rng.uniform(-1, 1), 2.2, w, rng.uniform(1.2, 2.0), rng.uniform(1.4, 3.2), walls, roof, stilt)
    box("jetty", 0, 3.05, 60, 0.18, stilt, y=0.2)
    for x in range(-29, 30, 4):
        box("post_%d" % x, x, 2.55, 0.14, 1.0, stilt, y=0.25)
    return (STRIP_WIDTH, 10, PPU, 2.0, 1.4, p.ink())


def layer_farther_cliffs(rng, faded):
    """Paper_Farther_Cliffs: 80 x 16 units at z = 16, the dunes and the sea line, almost paper."""
    p = Palette(0.6, faded)
    back = p("back", SILVER, 0.15)
    front = p("front", lerp(SILVER, OLIVE, 0.4))
    sea = p("sea", lerp(SILVER, PAPER, 0.3))
    ridge("sea", rng, -41, 41, 6.0, 1.2, 0.05, 8, sea, y=1.0)
    ridge("dunes_back", rng, -41, 41, 6.0, 7.5, 2.2, 14, back, y=0.6)
    ridge("dunes_front", rng, -41, 41, 6.0, 4.6, 1.6, 18, front, y=0.2)
    return (STRIP_WIDTH, 16, PPU, 6.0, 1.3, p.wash(INK, -0.1))


def layer_mid_salt(rng, faded):
    """Paper_Mid_Salt: 80 x 6 units at z = 3, the salt flats before the chapel: crust, cracks, dead reeds, posts."""
    p = Palette(0.15, faded)
    crust = p("crust", lerp(SILVER, PAPER, 0.6))
    crust2 = p("crust2", lerp(SILVER, PAPER, 0.35))
    crack = p("crack", lerp(INK, SILVER, 0.4))
    dead = p("dead", lerp(OLIVE, SILVER, 0.7))
    post = p("post", lerp(INK, RUST, 0.4))
    ridge("crust_back", rng, -41, 41, 0.0, 1.3, 0.35, 30, crust2, y=0.4)
    ridge("crust", rng, -41, 41, 0.0, 0.8, 0.25, 40, crust, y=0.2)
    for i in range(60):
        x = rng.uniform(-40, 40)
        box("crack_%d" % i, x, rng.uniform(0.1, 0.7), rng.uniform(0.3, 1.2), 0.03, crack, y=0.1)
    reed_bank(rng, -40, 40, 0.5, 70, (0.8, 2.2), dead, None, width=0.09)
    for i, x in enumerate(range(-36, 40, 9)):
        h = rng.uniform(1.6, 2.6)
        box("post_%d" % i, x + rng.uniform(-1.5, 1.5), 0.4 + h / 2, 0.18, h, post, y=0.0)
        box("post_cap_%d" % i, x + rng.uniform(-1.5, 1.5), 0.4 + h, 0.5, 0.12, post, y=0.0)
    return (STRIP_WIDTH, 6, PPU, 0.0, 2.0, p.ink())


def layer_far_chapel(rng, faded):
    """Paper_Far_Chapel: 80 x 14 units at z = 8, the Salt Chapel at the east end over a salt shore and a few roosts."""
    p = Palette(0.45, faded)
    walls = p("walls", SILVER)
    roof = p("roof", lerp(RUST, SILVER, 0.3), 0.1)
    dark = p("dark", lerp(INK, SILVER, 0.45))
    salt = p("salt", PAPER, 0.2)
    shore = p("shore", lerp(SILVER, PAPER, 0.5))
    ridge("shore", rng, -41, 41, 4.0, 0.6, 0.2, 30, shore, y=0.5)
    chapel("chapel", 13, 4.3, 12, walls, roof, dark, salt, rng)
    for i, x in enumerate((-30, -19, -8)):
        roost("roost_%d" % i, x + rng.uniform(-1, 1), 4.2, rng.uniform(1.6, 2.4), rng.uniform(1.2, 1.8), rng.uniform(1.0, 2.2), walls, roof, dark, y=0.3)
    for i in range(6):
        x = -36 + i * 6 + rng.uniform(-1, 1)
        box("post_%d" % i, x, 4.9, 0.14, 1.6, dark, y=0.35)
    return (STRIP_WIDTH, 14, PPU, 4.0, 1.4, p.ink())


def layer_far_tower(rng, faded):
    """Paper_Far_Tower: 80 x 14 units at z = 8, the fourth lighthouse over the lamp room, the chain receding both ways."""
    p = Palette(0.45, faded)
    walls = p("walls", SILVER, 0.1)
    lamp = p("lamp", lerp(RUST, PAPER, 0.4))
    dark = p("dark", lerp(INK, SILVER, 0.45))
    shore = p("shore", lerp(SILVER, PAPER, 0.5))
    ridge("shore", rng, -41, 41, 4.0, 0.7, 0.2, 30, shore, y=0.5)
    lighthouse("fourth", 3, 4.6, 3.2, 10.5, walls, lamp, dark)
    lighthouse("third", -24, 4.5, 2.0, 6.5, p("walls_far", SILVER, 0.3), p("lamp_far", lerp(RUST, PAPER, 0.4), 0.2), p("dark_far", lerp(INK, SILVER, 0.45), 0.2), y=0.3)
    lighthouse("fifth", 30, 4.5, 1.8, 6.0, p("walls_far", SILVER, 0.3), p("lamp_far", lerp(RUST, PAPER, 0.4), 0.2), p("dark_far", lerp(INK, SILVER, 0.45), 0.2), y=0.3)
    box("keeper_hut", 7.5, 5.4, 3.0, 1.6, walls, y=0.1)
    polygon("keeper_roof", [(5.6, 6.2), (9.4, 6.2), (7.5, 7.3)], p("roof", lerp(RUST, SILVER, 0.3), 0.1), y=0.05)
    box("jetty", -12, 4.75, 16, 0.16, dark, y=0.2)
    for x in range(-19, -4, 3):
        box("post_%d" % x, x, 4.35, 0.12, 0.8, dark, y=0.25)
    return (STRIP_WIDTH, 14, PPU, 4.0, 1.4, p.ink())


def layer_farther_sea(rng, faded):
    """Paper_Farther_Sea: 80 x 16 units at z = 16, the open sea to the horizon, wave lines, an island far off."""
    p = Palette(0.6, faded)
    sea = p("sea", lerp(SILVER, PAPER, 0.25))
    sea2 = p("sea2", lerp(SILVER, PAPER, 0.45))
    crest = p("crest", PAPER, 0.3)
    island = p("island", lerp(SILVER, OLIVE, 0.3), 0.1)
    ridge("sea_far", rng, -41, 41, 6.0, 4.2, 0.04, 6, sea2, y=1.0)
    ridge("sea_near", rng, -41, 41, 6.0, 2.4, 0.12, 20, sea, y=0.6)
    for i in range(90):
        x = rng.uniform(-40, 40)
        z = 6.2 + rng.uniform(0, 3.6)
        box("crest_%d" % i, x, z, rng.uniform(0.6, 2.2) * (1.0 - (z - 6.2) / 6), 0.05, crest, y=0.3)
    ridge("island", rng, -34, -26, 9.9, 0.9, 0.5, 8, island, y=0.8)
    box("island_tower", -30.5, 11.0, 0.3, 1.2, island, y=0.7)
    return (STRIP_WIDTH, 16, PPU, 6.0, 1.3, p.wash(INK, -0.1))


def tile_boardwalk(rng, faded):
    """Ground_Boardwalk: a 4 x 1 unit tile of quay planks (tiled in world space by the ink shader)."""
    p = Palette(0.0, faded)
    plank = p("plank", lerp(RUST, SILVER, 0.55))
    plank2 = p("plank2", lerp(RUST, SILVER, 0.42))
    nail = p("nail", INK)
    grain = p("grain", lerp(RUST, INK, 0.45))
    gap = 0.06
    x = -2.0
    i = 0
    while x < 2.0 - 1e-6:
        w = 2.0 if i % 3 == 1 else 1.0
        if x + w > 2.0:
            w = 2.0 - x
        box("plank_%d" % i, x + w / 2, 0.5, w - gap, 0.86, plank if i % 2 == 0 else plank2)
        nx = x + 0.12 if i % 2 == 0 else x + w - 0.12
        box("nail_%d" % i, nx, 0.5 + rng.uniform(-0.2, 0.2), 0.04, 0.04, nail, y=-0.2)
        for g in range(2):
            gz = 0.5 + rng.uniform(-0.3, 0.3)
            gw = (w - gap) * rng.uniform(0.35, 0.7)
            box("grain_%d_%d" % (i, g), x + w / 2 + rng.uniform(-0.15, 0.15) * w, gz, gw, 0.018, grain, y=-0.15)
        x += w
        i += 1
    box("beam", 0, 0.05, 4.2, 0.12, plank2, y=0.3)
    return (4, 1, TILE_PPU, 0.0, 2.2, p.ink())


def tile_shallows(rng, faded):
    """Ground_Shallows: a 4 x 1 unit tile of the tide over the mud: pale water, wave lines, a darker bed."""
    p = Palette(0.0, faded)
    water = p("water", lerp(SILVER, PAPER, 0.35))
    deep = p("deep", lerp(SILVER, OLIVE, 0.35))
    crest = p("crest", PAPER)
    box("bed", 0, 0.2, 4.2, 0.5, deep, y=0.4)
    box("water", 0, 0.62, 4.2, 0.5, water, y=0.2)
    for i in range(9):
        x = -1.9 + i * 0.45 + rng.uniform(-0.1, 0.1)
        box("crest_%d" % i, x, 0.7 + rng.uniform(-0.12, 0.12), rng.uniform(0.25, 0.5), 0.035, crest, y=0.0)
    for i in range(5):
        box("weed_%d" % i, -1.7 + i * 0.85 + rng.uniform(-0.2, 0.2), 0.12, 0.05, rng.uniform(0.15, 0.3), deep, y=0.1)
    return (4, 1, TILE_PPU, 0.0, 1.8, p.ink())


def tile_stone(rng, faded):
    """Ground_Stone: a 4 x 1 unit tile of salt-crusted stone blocks in two staggered courses."""
    p = Palette(0.0, faded)
    stone = p("stone", lerp(SILVER, PAPER, 0.25))
    stone2 = p("stone2", lerp(SILVER, PAPER, 0.1))
    salt = p("salt", PAPER)
    gap = 0.05
    for course in range(2):
        z = 0.25 + course * 0.5
        offset = 0.0 if course == 0 else 1.0
        x = -2.0 - offset
        i = 0
        while x < 2.0 - 1e-6:
            w = 2.0
            x0, x1 = max(x, -2.0), min(x + w, 2.0)
            if x1 > x0:
                box("block_%d_%d" % (course, i), (x0 + x1) / 2, z, x1 - x0 - gap, 0.5 - gap, stone if (i + course) % 2 == 0 else stone2)
            x += w
            i += 1
    for i in range(12):
        box("salt_%d" % i, rng.uniform(-1.9, 1.9), rng.uniform(0.05, 0.4), rng.uniform(0.12, 0.4), rng.uniform(0.04, 0.1), salt, y=-0.2)
    return (4, 1, TILE_PPU, 0.0, 2.2, p.ink())


# ---------------------------------------------------------------- the kit

LAYERS = [
    # name, kind, builder, faded
    ("Paper_Fore_Reeds", "strip", layer_fore_reeds, False),
    ("Paper_Mid_Reeds", "strip", layer_mid_reeds, False),
    ("Paper_Far_Roosts", "strip", layer_far_roosts, False),
    ("Paper_Farther_Cliffs", "strip", layer_farther_cliffs, False),
    ("Paper_Mid_Reeds_Faded", "strip", layer_mid_reeds, True),
    ("Paper_Far_Roosts_Faded", "strip", layer_far_roosts, True),
    ("Paper_Farther_Cliffs_Faded", "strip", layer_farther_cliffs, True),
    ("Paper_Mid_Salt", "strip", layer_mid_salt, False),
    ("Paper_Far_Chapel", "strip", layer_far_chapel, False),
    ("Paper_Far_Tower", "strip", layer_far_tower, False),
    ("Paper_Farther_Sea", "strip", layer_farther_sea, False),
    ("Ground_Boardwalk", "tile", tile_boardwalk, False),
    ("Ground_Boardwalk_Faded", "tile", tile_boardwalk, True),
    ("Ground_Shallows", "tile", tile_shallows, False),
    ("Ground_Stone", "tile", tile_stone, False),
]


def build(name, kind, fn, faded):
    reset_scene()
    seed = sum(ord(c) for c in name.replace("_Faded", ""))   # a faded layer draws the same shapes as its original
    rng = random.Random(seed)
    w, h, ppu, bottom, line, ink = fn(rng, faded)
    if faded:
        line *= FADED_LINE
    setup_render(w, h, ppu, bottom, line, ink, seed % 1000)
    size = render(os.path.join(OUT, name + ".png"))
    return w, h, ppu, size


def main():
    only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    manifest_path = os.path.join(OUT, "kit.json")
    kit = {"region": "Saltmarrow", "ppu": PPU, "tilePpu": TILE_PPU, "paper": PAPER, "layers": []}
    if only and os.path.exists(manifest_path):
        with open(manifest_path) as f:
            kit = json.load(f)
    for name, kind, fn, faded in LAYERS:
        if only and name not in only:
            continue
        w, h, ppu, size = build(name, kind, fn, faded)
        print("[paperkit] %s %dx%d px (%s) %d bytes" % (name, w * ppu, h * ppu, kind, size))
        entry = {"name": name, "kind": kind, "widthUnits": w, "heightUnits": h, "ppu": ppu,
                 "widthPx": int(round(w * ppu)), "heightPx": int(round(h * ppu)), "file": name + ".png", "faded": faded}
        kit["layers"] = [l for l in kit["layers"] if l["name"] != name] + [entry]
    order = [l[0] for l in LAYERS]
    kit["layers"].sort(key=lambda l: order.index(l["name"]) if l["name"] in order else 99)
    with open(manifest_path, "w") as f:
        json.dump(kit, f, indent=2)
    print("[paperkit] wrote kit.json with %d layers" % len(kit["layers"]))


if __name__ == "__main__":
    main()

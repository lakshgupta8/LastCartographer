"""Shared plumbing for the paper kits (ENV-01..03): the flat-wash materials, the cut-out geometry helpers, the
orthographic Freestyle render, and the build loop that writes a region's PNGs and kit.json. A region's script
(saltmarrow.py, emberdown.py) owns its palette and its shapes and calls run_kit with its layer table.
"""
import bpy, json, math, os, random, sys


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
    """Materials for one layer: every colour washed toward the region's paper by the layer's depth (and more if faded)."""

    def __init__(self, paper, ink, depth, faded, faded_wash=0.3):
        self.paper = paper
        self.ink_rgb = ink
        self.depth = min(0.92, depth + (faded_wash if faded else 0.0))
        self._cache = {}

    def wash(self, rgb, extra=0.0):
        return lerp(rgb, self.paper, min(0.95, self.depth + extra))

    def ink(self):
        return self.wash(self.ink_rgb, 0.0)

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


def disc(name, cx, cz, r, material, y=0.0, n=16):
    pts = [(cx + r * math.cos(2 * math.pi * i / n), cz + r * math.sin(2 * math.pi * i / n)) for i in range(n)]
    return polygon(name, pts, material, y=y)


def blob(name, rng, cx, cz, rx, rz, material, y=0.0, n=14, wobble=0.25):
    """A soft irregular round: smoke, steam, a drift of ash."""
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        k = 1.0 + rng.uniform(-wobble, wobble)
        pts.append((cx + rx * k * math.cos(a), cz + rz * k * math.sin(a)))
    return polygon(name, pts, material, y=y)


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


# ---------------------------------------------------------------- the build loop

def build_layer(out, name, kind, fn, faded, faded_line):
    reset_scene()
    seed = sum(ord(c) for c in name.replace("_Faded", ""))   # a faded layer draws the same shapes as its original
    rng = random.Random(seed)
    w, h, ppu, bottom, line, ink = fn(rng, faded)
    if faded:
        line *= faded_line
    setup_render(w, h, ppu, bottom, line, ink, seed % 1000)
    size = render(os.path.join(out, name + ".png"))
    return w, h, ppu, size


def run_kit(out, region, ppu, tile_ppu, paper, layers, faded_line=0.6):
    """Render every layer in the table (or only those named after `--`) and keep the region's kit.json in step."""
    os.makedirs(out, exist_ok=True)
    only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    manifest_path = os.path.join(out, "kit.json")
    kit = {"region": region, "ppu": ppu, "tilePpu": tile_ppu, "paper": paper, "layers": []}
    if os.path.exists(manifest_path):
        with open(manifest_path) as f:
            kit = json.load(f)
    for name, kind, fn, faded in layers:
        if only and name not in only:
            continue
        w, h, ppu_l, size = build_layer(out, name, kind, fn, faded, faded_line)
        print("[paperkit] %s %dx%d px (%s) %d bytes" % (name, w * ppu_l, h * ppu_l, kind, size))
        entry = {"name": name, "kind": kind, "widthUnits": w, "heightUnits": h, "ppu": ppu_l,
                 "widthPx": int(round(w * ppu_l)), "heightPx": int(round(h * ppu_l)), "file": name + ".png", "faded": faded}
        kit["layers"] = [l for l in kit["layers"] if l["name"] != name] + [entry]
    order = [l[0] for l in layers]
    kit["layers"].sort(key=lambda l: (order.index(l["name"]) if l["name"] in order else 99, l["name"]))
    with open(manifest_path, "w") as f:
        json.dump(kit, f, indent=2)
    print("[paperkit] wrote kit.json with %d layers" % len(kit["layers"]))

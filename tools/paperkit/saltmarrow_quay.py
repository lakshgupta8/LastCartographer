"""Saltmarrow paper kit (ENV-01): the Quay's parallax layers and ground tiles, rendered from cut-out
geometry with Freestyle ink lines.

Run headless from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/saltmarrow_quay.py

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Saltmarrow/.
Every layer is a flat wash in the region's palette (art-direction 5) with a single ink line around each
cut-out; farther layers are washed toward the paper and drawn thinner (aerial perspective as thinning ink).
The geometry here is deliberately simple: an artist reworks a layer by editing or replacing the shapes in
this file, or by painting over the PNG at the same size. Unity reads the PNG, not this file.
"""
import bpy, bmesh, json, math, os, random, sys

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


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def wash(color, depth):
    """Aerial perspective: farther layers are thinner ink on the same paper."""
    return lerp(color, PAPER, depth)


# ---------------------------------------------------------------- scene plumbing

def reset_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)


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
    """A filled cut-out from a closed 2D outline (x, z)."""
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
    # Orthographic camera looking down +Y, framing exactly [−w/2, w/2] × [bottom, bottom + h].
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
    """A reed blade: a thin tapered cut-out leaning a little, with an optional seed head."""
    top_x = x + lean
    pts = [(x - width / 2, base_z), (x + width / 2, base_z), (top_x + width * 0.03, base_z + height), (top_x - width * 0.03, base_z + height)]
    ob = polygon(name, pts, material)
    if seed_head is not None:
        hw, hh = width * 1.4, height * 0.22
        head = polygon(name + "_head", [(top_x - hw / 2, base_z + height - hh * 0.3), (top_x + hw / 2, base_z + height - hh * 0.3),
                                        (top_x + hw * 0.15, base_z + height + hh), (top_x - hw * 0.15, base_z + height + hh)], seed_head, y=-0.01)
        return ob, head
    return ob


def reed_bank(rng, x0, x1, base_z, count, h_range, blade, head, spacing_jitter=0.6, width=0.10):
    step = (x1 - x0) / count
    for i in range(count):
        x = x0 + step * (i + 0.5) + rng.uniform(-spacing_jitter, spacing_jitter) * step
        h = rng.uniform(*h_range)
        reed("reed_%d_%d" % (int(base_z * 10), i), x, base_z, h, rng.uniform(-0.35, 0.35) * h * 0.35, width * rng.uniform(0.8, 1.3), blade,
             head if rng.random() < 0.55 else None)


def ridge(name, rng, x0, x1, base_z, mean_h, amp, segments, material, y=0.0):
    """A hill or cliff line: a filled outline with a wandering top edge."""
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


# ---------------------------------------------------------------- layers

def layer_fore_reeds(rng):
    """Paper_Fore_Reeds: 80 × 1.6 units at z = −4, the reeds in front of the walkway. Dark, thick line."""
    reset_scene()
    blade = flat_material("fore_blade", lerp(OLIVE, INK, 0.45))
    head = flat_material("fore_head", lerp(RUST, INK, 0.35))
    reed_bank(rng, -40, 40, -0.3, 150, (0.9, 1.7), blade, head, width=0.16)
    setup_render(STRIP_WIDTH, 1.6, PPU, -0.3, 3.0, INK, 1)
    return render(os.path.join(OUT, "Paper_Fore_Reeds.png"))


def layer_mid_reeds(rng):
    """Paper_Mid_Reeds: 80 × 6 units at z = 3, the reed bank behind the quay with a salt-flat foot."""
    reset_scene()
    d = 0.15
    flat = flat_material("mid_flat", wash(SILVER, d + 0.2))
    blade = flat_material("mid_blade", wash(OLIVE, d))
    blade2 = flat_material("mid_blade2", wash(lerp(OLIVE, SILVER, 0.5), d))
    head = flat_material("mid_head", wash(RUST, d + 0.1))
    ridge("saltflat", rng, -41, 41, 0.0, 0.7, 0.25, 24, flat, y=0.3)
    reed_bank(rng, -40, 40, 0.3, 90, (2.4, 4.6), blade2, head, width=0.12)
    reed_bank(rng, -40, 40, 0.5, 130, (1.6, 3.6), blade, head, width=0.12)
    setup_render(STRIP_WIDTH, 6, PPU, 0.0, 2.0, wash(INK, d), 2)
    return render(os.path.join(OUT, "Paper_Mid_Reeds.png"))


def layer_far_roosts(rng):
    """Paper_Far_Roosts: 80 × 10 units at z = 8, the stilt-roosts of the quay, a jetty and its ropes."""
    reset_scene()
    d = 0.45
    walls = flat_material("far_walls", wash(SILVER, d))
    roof = flat_material("far_roof", wash(RUST, d + 0.1))
    stilt = flat_material("far_stilt", wash(lerp(INK, SILVER, 0.5), d))
    water = flat_material("far_water", wash(lerp(SILVER, PAPER, 0.5), d))
    ridge("shallows", rng, -41, 41, 2.0, 0.5, 0.15, 30, water, y=0.5)
    xs = [-34, -27, -21, -13, -6, 2, 9, 15, 22, 29, 36]
    for i, x in enumerate(xs):
        w = rng.uniform(1.6, 2.8)
        roost("roost_%d" % i, x + rng.uniform(-1, 1), 2.2, w, rng.uniform(1.2, 2.0), rng.uniform(1.4, 3.2), walls, roof, stilt)
    # a jetty: a long plank line on posts
    box("jetty", 0, 3.05, 60, 0.18, stilt, y=0.2)
    for x in range(-29, 30, 4):
        box("post_%d" % x, x, 2.55, 0.14, 1.0, stilt, y=0.25)
    setup_render(STRIP_WIDTH, 10, PPU, 2.0, 1.4, wash(INK, d), 3)
    return render(os.path.join(OUT, "Paper_Far_Roosts.png"))


def layer_farther_cliffs(rng):
    """Paper_Farther_Cliffs: 80 × 16 units at z = 16, the dunes and the sea line, almost paper."""
    reset_scene()
    d = 0.6
    back = flat_material("cliff_back", wash(SILVER, d + 0.15))
    front = flat_material("cliff_front", wash(lerp(SILVER, OLIVE, 0.4), d))
    sea = flat_material("sea", wash(lerp(SILVER, PAPER, 0.3), d))
    ridge("sea", rng, -41, 41, 6.0, 1.2, 0.05, 8, sea, y=1.0)
    ridge("dunes_back", rng, -41, 41, 6.0, 7.5, 2.2, 14, back, y=0.6)
    ridge("dunes_front", rng, -41, 41, 6.0, 4.6, 1.6, 18, front, y=0.2)
    setup_render(STRIP_WIDTH, 16, PPU, 6.0, 1.3, wash(INK, d - 0.1), 4)
    return render(os.path.join(OUT, "Paper_Farther_Cliffs.png"))


def tile_boardwalk(rng):
    """Ground_Boardwalk: a 4 × 1 unit tile of quay planks (world-space tiled by the InkSprite shader)."""
    reset_scene()
    plank = flat_material("plank", lerp(RUST, SILVER, 0.55))
    plank2 = flat_material("plank2", lerp(RUST, SILVER, 0.42))
    nail = flat_material("nail", INK)
    grain = flat_material("grain", lerp(RUST, INK, 0.45))
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
    setup_render(4, 1, TILE_PPU, 0.0, 2.2, INK, 5)
    return render(os.path.join(OUT, "Ground_Boardwalk.png"))


def main():
    rng = random.Random(7)
    kit = {"region": "Saltmarrow", "ppu": PPU, "tilePpu": TILE_PPU, "paper": PAPER, "layers": []}
    jobs = [
        ("Paper_Fore_Reeds", "strip", STRIP_WIDTH, 1.6, PPU, layer_fore_reeds),
        ("Paper_Mid_Reeds", "strip", STRIP_WIDTH, 6, PPU, layer_mid_reeds),
        ("Paper_Far_Roosts", "strip", STRIP_WIDTH, 10, PPU, layer_far_roosts),
        ("Paper_Farther_Cliffs", "strip", STRIP_WIDTH, 16, PPU, layer_farther_cliffs),
        ("Ground_Boardwalk", "tile", 4, 1, TILE_PPU, tile_boardwalk),
    ]
    only = [a for a in sys.argv[sys.argv.index("--") + 1:]] if "--" in sys.argv else []
    for name, kind, w, h, ppu, fn in jobs:
        if only and name not in only:
            continue
        size = fn(rng)
        print("[paperkit] %s %dx%d px (%s) %d bytes" % (name, w * ppu, h * ppu, kind, size))
        kit["layers"].append({"name": name, "kind": kind, "widthUnits": w, "heightUnits": h, "ppu": ppu,
                              "widthPx": int(w * ppu), "heightPx": int(h * ppu), "file": name + ".png"})
    if not only:
        with open(os.path.join(OUT, "kit.json"), "w") as f:
            json.dump(kit, f, indent=2)
        print("[paperkit] wrote kit.json with %d layers" % len(kit["layers"]))


if __name__ == "__main__":
    main()

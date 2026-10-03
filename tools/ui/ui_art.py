"""The atlas UI's drawings, rendered in Blender (ENV-11): the paper the pages are made of, the masks as inked
feathers, the Inkwell as a bottle that fills, the boss bar as a brush stroke, and the glyphs the pages point with
(the quill marker, the Charters' cowls, the Instruments, the compass rose, the vantage marks, a lamp and a desk).
Every piece is a cut-out drawn with the same ink line and palette as the characters and the kits, so the UI is
drawn by the same pen as the world.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/ui/ui_art.py
    ... -P tools/ui/ui_art.py -- UI_Page UI_MaskFull          (only those)
    python tools/ui/pack.py

Renders at 2x into tools/ui/.frames/ (not committed) with ui.json beside them: each piece's size at 1x, its
nine-slice insets where it stretches, and where the Inkwell's fill runs. pack.py downsamples them into
Assets/_Project/Art/UI/Resources/UI/ for InkArt to load.

Units: 1 unit is 100 px at 1x. The camera looks along +Y; smaller y is nearer, so a thing drawn "on top" sits at a
more negative y. Objects in the NoInk collection get no Freestyle line: washes, rules and highlights.
"""
import json, math, os, random, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "characters"))
import bpy
from inklib import D, mat, lerp, link, slab, reset_scene, argv_after_dashes, INK, PAPER, SCALE

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES = os.path.join(ROOT, "tools", "ui", ".frames")
UPX = 100.0   # px per unit at 1x

# The UI's palette (InkTheme.Warm) in the drawings' terms, and the world's colours the glyphs borrow.
PAPER_DARK = (0.88, 0.84, 0.74)
WASH = (0.20, 0.27, 0.45)
DIM = (0.35, 0.35, 0.40)
OCHRE = (0.86, 0.70, 0.30)
BRASS = (0.78, 0.62, 0.30)
STEEL = (0.30, 0.32, 0.36)
WOOD = (0.55, 0.42, 0.28)
ROPE = (0.52, 0.40, 0.26)
BROWN = (0.42, 0.38, 0.34)
CREAM = (0.93, 0.88, 0.76)
BEAK = (0.55, 0.42, 0.22)
COWL = (0.16, 0.22, 0.40)
WAX = (0.60, 0.18, 0.14)
IRIS = (0.45, 0.30, 0.55)
CHARTER_COWLS = {
    "Surveyor": COWL, "Warden": (0.28, 0.34, 0.44), "Drifter": (0.62, 0.42, 0.22),
    "Ferryman": (0.28, 0.38, 0.36), "Unwriter": (0.88, 0.86, 0.80), "Remnant": (0.46, 0.46, 0.47),
}


def m(rgb):
    return mat("ui_%.3f_%.3f_%.3f" % tuple(rgb), rgb)


# ---------------------------------------------------------------- scene

def noink_collection():
    coll = bpy.data.collections.get("NoInk")
    if coll is None:
        coll = bpy.data.collections.new("NoInk")
    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)
    return coll


def setup(w_px, h_px, line=3.2, ink=INK, seed=0):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 16
    sc.cycles.use_denoising = False
    sc.render.film_transparent = True
    sc.render.resolution_x = int(round(w_px * SCALE))
    sc.render.resolution_y = int(round(h_px * SCALE))
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    try:
        sc.view_settings.view_transform = "Standard"
    except TypeError:
        pass
    sc.view_settings.look = "None"
    w_u, h_u = w_px / UPX, h_px / UPX
    cam = bpy.data.objects.get("Cam")
    if cam is None:
        cam_data = bpy.data.cameras.new("Cam")
        cam_data.type = "ORTHO"
        cam = bpy.data.objects.new("Cam", cam_data)
        sc.collection.objects.link(cam)
    cam.data.ortho_scale = max(w_u, h_u)
    cam.location = (0.0, -10.0, 0.0)
    cam.rotation_euler = (D(90), 0, 0)
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
    ls.select_by_collection = True
    ls.collection = noink_collection()
    ls.collection_negation = "EXCLUSIVE"
    st = ls.linestyle
    st.color = ink
    st.thickness = line
    st.caps = "ROUND"
    st.use_chaining = True
    st.chaining = "PLAIN"
    noise = st.geometry_modifiers.new("Pen", "PERLIN_NOISE_1D")
    noise.frequency = 10.0
    noise.amplitude = 0.4
    noise.octaves = 2
    noise.seed = seed
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


# ---------------------------------------------------------------- shapes (all in the x-z plane)

def finish(ob, ink):
    if not ink:
        coll = noink_collection()
        coll.objects.link(ob)
        bpy.context.scene.collection.objects.unlink(ob)
    return ob


def poly(name, pts, rgb, y=0.0, ink=True, depth=0.05):
    clean = []
    for x, z in pts:
        if not clean or abs(clean[-1][0] - x) > 1e-5 or abs(clean[-1][1] - z) > 1e-5:
            clean.append((x, z))
    if len(clean) > 2 and abs(clean[0][0] - clean[-1][0]) < 1e-5 and abs(clean[0][1] - clean[-1][1]) < 1e-5:
        clean.pop()
    return finish(slab(name, clean, depth, m(rgb), None, loc=(0, y, 0)), ink)


def rect(name, cx, cz, w, h, rgb, y=0.0, ink=True):
    return poly(name, [(cx - w / 2, cz - h / 2), (cx + w / 2, cz - h / 2), (cx + w / 2, cz + h / 2), (cx - w / 2, cz + h / 2)], rgb, y, ink)


def rrect_points(cx, cz, w, h, r, n=6):
    pts = []
    for k, (sx, sz) in enumerate(((1, -1), (1, 1), (-1, 1), (-1, -1))):
        ccx, ccz = cx + sx * (w / 2 - r), cz + sz * (h / 2 - r)
        a0 = (-90 + 90 * k) * math.pi / 180
        for i in range(n + 1):
            a = a0 + (math.pi / 2) * i / n
            pts.append((ccx + r * math.cos(a), ccz + r * math.sin(a)))
    return pts


def rrect(name, cx, cz, w, h, r, rgb, y=0.0, ink=True):
    return poly(name, rrect_points(cx, cz, w, h, r), rgb, y, ink)


def ellipse_points(cx, cz, rx, rz, n=40, a0=0.0, a1=2 * math.pi, rot=0.0):
    pts = []
    for i in range(n):
        a = a0 + (a1 - a0) * i / (n if a1 - a0 >= 2 * math.pi - 1e-6 else n - 1)
        x, z = rx * math.cos(a), rz * math.sin(a)
        c, s = math.cos(rot), math.sin(rot)
        pts.append((cx + x * c - z * s, cz + x * s + z * c))
    return pts


def ellipse(name, cx, cz, rx, rz, rgb, y=0.0, ink=True, n=40, rot=0.0):
    return poly(name, ellipse_points(cx, cz, rx, rz, n, rot=rot), rgb, y, ink)


def line(name, x0, z0, x1, z1, w, rgb, y=0.0, ink=False):
    """A straight stroke of width w, square-ended."""
    dx, dz = x1 - x0, z1 - z0
    L = math.hypot(dx, dz) or 1e-6
    nx, nz = -dz / L * w / 2, dx / L * w / 2
    return poly(name, [(x0 + nx, z0 + nz), (x1 + nx, z1 + nz), (x1 - nx, z1 - nz), (x0 - nx, z0 - nz)], rgb, y, ink)


def stroke(name, pts, w, rgb, y=0.0, ink=False, taper=1.0):
    """A polyline drawn as one ribbon of width w (w * taper at the end)."""
    left, right = [], []
    n = len(pts)
    for i, (x, z) in enumerate(pts):
        if i == 0:
            dx, dz = pts[1][0] - x, pts[1][1] - z
        elif i == n - 1:
            dx, dz = x - pts[i - 1][0], z - pts[i - 1][1]
        else:
            dx, dz = pts[i + 1][0] - pts[i - 1][0], pts[i + 1][1] - pts[i - 1][1]
        L = math.hypot(dx, dz) or 1e-6
        ww = w * (1.0 + (taper - 1.0) * i / max(1, n - 1)) / 2
        nx, nz = -dz / L * ww, dx / L * ww
        left.append((x + nx, z + nz))
        right.append((x - nx, z - nz))
    return poly(name, left + right[::-1], rgb, y, ink)


def rotated(pts, deg, cx=0.0, cz=0.0):
    c, s = math.cos(D(deg)), math.sin(D(deg))
    return [(cx + (x - cx) * c - (z - cz) * s, cz + (x - cx) * s + (z - cz) * c) for x, z in pts]


def deckle_points(cx, cz, w, h, rng, amp, per_unit=6.0):
    """A rectangle whose edges wander a little: cut paper."""
    pts = []
    corners = [(cx - w / 2, cz - h / 2), (cx + w / 2, cz - h / 2), (cx + w / 2, cz + h / 2), (cx - w / 2, cz + h / 2)]
    for i in range(4):
        (x0, z0), (x1, z1) = corners[i], corners[(i + 1) % 4]
        L = math.hypot(x1 - x0, z1 - z0)
        n = max(2, int(L * per_unit))
        nx, nz = -(z1 - z0) / L, (x1 - x0) / L
        for k in range(n):
            t = k / n
            off = 0.0 if k == 0 else rng.uniform(-amp, amp)
            pts.append((x0 + (x1 - x0) * t + nx * off, z0 + (z1 - z0) * t + nz * off))
    return pts


def deckle(name, cx, cz, w, h, rgb, rng, amp=0.03, y=0.0, ink=True):
    return poly(name, deckle_points(cx, cz, w, h, rng, amp), rgb, y, ink)


def rule(name, cx, cz, w, h, width, rgb, y=-0.1):
    """A rectangular ink rule (four strokes), no Freestyle line."""
    x0, x1, z0, z1 = cx - w / 2, cx + w / 2, cz - h / 2, cz + h / 2
    line(name + "_b", x0, z0, x1, z0, width, rgb, y)
    line(name + "_t", x0, z1, x1, z1, width, rgb, y)
    line(name + "_l", x0, z0 - width / 2, x0, z1 + width / 2, width, rgb, y)
    line(name + "_r", x1, z0 - width / 2, x1, z1 + width / 2, width, rgb, y)


def corner_ticks(name, cx, cz, w, h, length, width, rgb, y=-0.1):
    """Small diagonal pen ticks off each corner of a rule, the way a draughtsman marks a sheet."""
    for k, (sx, sz) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        x, z = cx + sx * w / 2, cz + sz * h / 2
        line("%s_%d" % (name, k), x + sx * 0.02, z + sz * 0.02, x + sx * (0.02 + length), z + sz * (0.02 + length), width, rgb, y)


def ring(name, cx, cz, r, width, rgb, inner_rgb, y=0.0, n=48):
    """A drawn ring: an ink disc with a paper disc on it, so both edges carry a line."""
    ellipse(name + "_o", cx, cz, r, r, rgb, y, True, n)
    ellipse(name + "_i", cx, cz, r - width, r - width, inner_rgb, y - 0.05, True, n)


def arc(name, cx, cz, r, a0_deg, a1_deg, w, rgb, y=0.0, ink=False, n=16):
    return stroke(name, ellipse_points(cx, cz, r, r, n, D(a0_deg), D(a1_deg)), w, rgb, y, ink)


# ---------------------------------------------------------------- the paper

def paper_page(rng):
    """The page every panel is made of: a deckled sheet, an ink rule set in from the edge, a fainter one inside it."""
    W, H = 10.24, 5.12
    deckle("sheet", 0, 0, W - 0.10, H - 0.10, PAPER, rng, 0.025)
    rule("rule", 0, 0, W - 0.52, H - 0.52, 0.016, INK)
    rule("rule2", 0, 0, W - 0.66, H - 0.66, 0.010, lerp(INK, PAPER, 0.62))
    corner_ticks("tick", 0, 0, W - 0.52, H - 0.52, 0.07, 0.014, INK)


def paper_strip(rng):
    """A strip torn off the sheet for a caption: the long edges torn, a short rule at each end."""
    W, H = 7.68, 1.28
    pts = []
    n = 60
    for k in range(n):   # bottom, left to right
        t = k / n
        pts.append((-W / 2 + 0.05 + (W - 0.10) * t, -H / 2 + 0.12 + rng.uniform(-0.045, 0.045)))
    pts.append((W / 2 - 0.05, -H / 2 + 0.12))
    for k in range(n):   # top, right to left
        t = k / n
        pts.append((W / 2 - 0.05 - (W - 0.10) * t, H / 2 - 0.12 + rng.uniform(-0.045, 0.045)))
    poly("strip", pts, PAPER)
    for sx in (-1, 1):
        line("end_%d" % sx, sx * (W / 2 - 0.34), -H / 2 + 0.30, sx * (W / 2 - 0.34), H / 2 - 0.30, 0.016, INK, -0.1)
        line("end2_%d" % sx, sx * (W / 2 - 0.42), -H / 2 + 0.34, sx * (W / 2 - 0.42), H / 2 - 0.34, 0.010, lerp(INK, PAPER, 0.62), -0.1)


def paper_spread(rng):
    """The atlas: two pages open on a sewn spine."""
    W, H = 15.0, 9.0
    gutter = 0.14
    pw = W / 2 - gutter - 0.10
    rect("gutter", 0, 0, 2 * gutter + 0.3, H - 0.3, lerp(PAPER_DARK, INK, 0.55), 0.1, False)
    for sx, name in ((-1, "left"), (1, "right")):
        cx = sx * (gutter + pw / 2)
        deckle(name, cx, 0, pw, H - 0.14, PAPER, rng, 0.025)
        # the page curls into the spine: a band of shade along the gutter edge
        rect(name + "_shade", sx * (gutter + 0.16), 0, 0.32, H - 0.22, lerp(PAPER, INK, 0.10), -0.05, False)
        rect(name + "_shade2", sx * (gutter + 0.06), 0, 0.12, H - 0.22, lerp(PAPER, INK, 0.18), -0.06, False)
        # the rule, set further in on the gutter side
        inner = 0.46
        rx0 = sx * (gutter + inner) if sx > 0 else -(W / 2 - 0.40)
        rx1 = (W / 2 - 0.40) if sx > 0 else -(gutter + inner)
        rw = rx1 - rx0
        rule(name + "_rule", (rx0 + rx1) / 2, 0, rw, H - 0.80, 0.016, INK)
        rule(name + "_rule2", (rx0 + rx1) / 2, 0, rw - 0.14, H - 0.94, 0.010, lerp(INK, PAPER, 0.62))
        corner_ticks(name + "_tick", (rx0 + rx1) / 2, 0, rw, H - 0.80, 0.07, 0.014, INK)
    # the sewing: thread across the spine, a stitch every so often
    for k in range(9):
        z = -H / 2 + 0.75 + k * (H - 1.5) / 8 + rng.uniform(-0.04, 0.04)
        line("stitch_%d" % k, -0.30, z, 0.30, z + rng.uniform(-0.02, 0.02), 0.035, lerp(CREAM, INK, 0.15), -0.12)
        line("stitchv_%d" % k, 0.0, z - 0.14, 0.0, z + 0.14, 0.03, lerp(CREAM, INK, 0.15), -0.12)


def paper_portrait(rng):
    """The square the speaker's face sits in: a sheet with a darker inner square and a rule."""
    S = 2.56
    deckle("sheet", 0, 0, S - 0.08, S - 0.08, PAPER, rng, 0.02)
    rrect("inner", 0, 0, S - 0.40, S - 0.40, 0.08, PAPER_DARK, -0.05, False)
    rule("rule", 0, 0, S - 0.34, S - 0.34, 0.016, INK)
    corner_ticks("tick", 0, 0, S - 0.34, S - 0.34, 0.06, 0.014, INK)


# ---------------------------------------------------------------- the HUD

def feather(full):
    """A mask: one inked feather. Full is ink with paper barbs; empty is paper with ink barbs."""
    tilt = -22.0
    vane = []
    n = 26
    for i in range(n + 1):
        t = i / n
        z = -0.12 + 0.56 * t
        vane.append((-0.17 * math.sin(math.pi * t) ** 0.85 - 0.02 * t, z))
    for i in range(n, -1, -1):
        t = i / n
        z = -0.12 + 0.56 * t
        vane.append((0.12 * math.sin(math.pi * t) ** 1.1 + 0.03 * t, z))
    vane_rgb = lerp(INK, PAPER, 0.08) if full else PAPER
    poly("vane", rotated(vane, tilt), vane_rgb)
    line_rgb = lerp(PAPER, INK, 0.25) if full else INK
    barb_rgb = lerp(PAPER, INK, 0.35) if full else lerp(INK, PAPER, 0.55)
    (x0, z0), (x1, z1) = rotated([(0, -0.44), (0.03, 0.42)], tilt)
    line("rachis", x0, z0, x1, z1, 0.028, line_rgb, -0.1)
    for k in range(5):
        t = 0.2 + 0.14 * k
        z = -0.12 + 0.56 * t
        for side, reach in ((-1, 0.15), (1, 0.10)):
            p0, p1 = rotated([(0.0, z), (side * reach * math.sin(math.pi * t) ** 0.9, z + 0.07)], tilt)
            line("barb_%d_%d" % (k, side), p0[0], p0[1], p1[0], p1[1], 0.014, barb_rgb, -0.1)


def bottle_body_points(inset=0.0):
    """The Inkwell's outline as (x, z) points: a squat flask, the shoulder, the neck, the lip. Counter-clockwise
    from the bottom-left corner; inset draws the same shape a little inside itself (the ink in the glass)."""
    i = inset
    return [(-0.44 + i, -0.62 + i), (0.44 - i, -0.62 + i), (0.44 - i, 0.02 - i), (0.40 - i, 0.10 - i), (0.30 - i, 0.16 - i),
            (0.17 - i, 0.30), (0.17 - i, 0.44), (0.21 - i, 0.44), (0.21 - i, 0.52 - i), (-0.21 + i, 0.52 - i), (-0.21 + i, 0.44),
            (-0.17 + i, 0.44), (-0.17 + i, 0.30), (-0.30 + i, 0.16 - i), (-0.40 + i, 0.10 - i), (-0.44 + i, 0.02 - i)]


def inkwell(rng):
    """The bottle: pale glass with an ink line, a cork, two highlights."""
    poly("glass", bottle_body_points(), lerp(PAPER, WASH, 0.14))
    rect("cork", 0, 0.60, 0.30, 0.18, WOOD, -0.02)
    line("cork_grain", -0.08, 0.53, -0.06, 0.67, 0.012, lerp(WOOD, INK, 0.4), -0.1)
    line("cork_grain2", 0.06, 0.53, 0.08, 0.67, 0.012, lerp(WOOD, INK, 0.4), -0.1)
    line("shine", -0.33, -0.48, -0.33, 0.00, 0.04, PAPER, -0.1)
    line("shine2", -0.26, -0.52, -0.26, -0.30, 0.025, PAPER, -0.1)


def inkwell_fill(rng):
    """The ink in the bottle, drawn to the brim: the game shows as much of it, from the bottom, as she has."""
    pts = bottle_body_points(0.05)
    pts = [(x, min(z, 0.44)) for x, z in pts]
    poly("ink", pts, lerp(WASH, INK, 0.30), 0.0, False)
    line("sheen", -0.28, -0.40, -0.28, 0.0, 0.03, lerp(WASH, PAPER, 0.25), -0.1)


# the fill, as fractions of the texture's height from the bottom (the bottle is 1.44 units tall, centred)
FILL_BOTTOM = (-0.57 + 0.72) / 1.44
FILL_TOP = (0.44 + 0.72) / 1.44


def lantern(scale, cx=0.0, cz=0.0):
    """Wren's lantern: a glass body, a cap, a handle, the flame."""
    s = scale
    rrect("body", cx, cz - 0.02 * s, 0.26 * s, 0.30 * s, 0.05 * s, lerp(OCHRE, PAPER, 0.55))
    rect("cap", cx, cz + 0.16 * s, 0.18 * s, 0.06 * s, STEEL, -0.02)
    rect("foot", cx, cz - 0.20 * s, 0.20 * s, 0.05 * s, STEEL, -0.02)
    arc("handle", cx, cz + 0.19 * s, 0.10 * s, 10, 170, 0.03 * s, STEEL, 0.02, True, 10)
    ellipse("flame", cx, cz - 0.03 * s, 0.05 * s, 0.08 * s, OCHRE, -0.1, False, 20)
    ellipse("flame_core", cx, cz - 0.05 * s, 0.025 * s, 0.04 * s, lerp(OCHRE, PAPER, 0.6), -0.12, False, 16)


def boss_bar(rng):
    """The trough the boss's ink sits in: a long brush stroke, rounded at the left, brushed out at the right."""
    W, H = 10.24, 0.48
    pts = ellipse_points(-W / 2 + 0.30, 0, 0.22, 0.18, 14, D(90), D(270))
    pts += [(W / 2 - 0.9, -0.18), (W / 2 - 0.3, -0.13), (W / 2 - 0.08, -0.03), (W / 2 - 0.3, 0.12), (W / 2 - 0.9, 0.18)]
    poly("trough", pts, PAPER)


def boss_fill(rng):
    """The ink in the bar: one stroke, dry-brushed at its end."""
    W = 10.24
    pts = ellipse_points(-W / 2 + 0.30, 0, 0.18, 0.14, 14, D(90), D(270))
    pts += [(W / 2 - 1.0, -0.14), (W / 2 - 0.5, -0.10), (W / 2 - 0.26, -0.02), (W / 2 - 0.5, 0.09), (W / 2 - 1.0, 0.14)]
    poly("ink", pts, INK, 0.0, False)
    for k, (z, l) in enumerate(((0.07, 0.5), (0.0, 0.62), (-0.08, 0.42))):
        line("dry_%d" % k, W / 2 - 0.9, z, W / 2 - 0.9 + l, z * 0.6, 0.03 - 0.005 * k, INK, -0.05)


def tick(rng):
    line("tick", -0.015, -0.2, 0.015, 0.2, 0.04, INK, 0.0)


def marker(rng):
    """The row marker: a pen nib pointing at what is chosen."""
    pts = ellipse_points(-0.12, 0, 0.10, 0.15, 10, D(90), D(270))
    pts += [(-0.04, -0.15), (0.24, 0.0), (-0.04, 0.15)]
    poly("nib", pts, INK)
    line("slit", 0.22, 0.0, 0.02, 0.0, 0.022, PAPER, -0.1)
    ellipse("hole", -0.02, 0, 0.035, 0.035, PAPER, -0.1, False, 16)


def seed(rng):
    """An iris seed: three lobes on a stem."""
    ellipse("pod", 0, 0.03, 0.095, 0.16, OCHRE, 0.0, True, 24)
    ellipse("pod_l", -0.085, -0.01, 0.075, 0.13, OCHRE, 0.02, True, 24, rot=D(28))
    ellipse("pod_r", 0.085, -0.01, 0.075, 0.13, OCHRE, 0.02, True, 24, rot=D(-28))
    line("stem", 0.0, -0.13, -0.02, -0.19, 0.02, INK, -0.1)
    line("seam", 0.0, 0.14, 0.0, -0.06, 0.012, lerp(OCHRE, INK, 0.5), -0.1)


def rose(rng, scale=1.0, cx=0.0, cz=0.0, with_ring=True, y=0.0):
    """A compass rose, Wren's clasp: four long points, four short, a ring, brass at the heart."""
    s = scale
    if with_ring:
        ring("ring", cx, cz, 0.60 * s, 0.045 * s, INK, PAPER, y + 0.05)
    for k in range(4):
        a = D(90 * k)
        for half, rgb in ((1, INK), (-1, PAPER)):
            pts = [(cx, cz), (cx + 0.52 * s * math.cos(a), cz + 0.52 * s * math.sin(a)),
                   (cx + 0.11 * s * math.cos(a) - half * 0.08 * s * math.sin(a), cz + 0.11 * s * math.sin(a) + half * 0.08 * s * math.cos(a))]
            poly("pt_%d_%d" % (k, half), pts, rgb, y + (-0.02 if half > 0 else -0.01))
    for k in range(4):
        a = D(45 + 90 * k)
        for half, rgb in ((1, INK), (-1, PAPER)):
            pts = [(cx, cz), (cx + 0.30 * s * math.cos(a), cz + 0.30 * s * math.sin(a)),
                   (cx + 0.08 * s * math.cos(a) - half * 0.05 * s * math.sin(a), cz + 0.08 * s * math.sin(a) + half * 0.05 * s * math.cos(a))]
            poly("sp_%d_%d" % (k, half), pts, rgb, y)
    ellipse("heart", cx, cz, 0.07 * s, 0.07 * s, BRASS, y - 0.08, True, 20)


def vantage(kind):
    if kind == "Drawn":
        ellipse("dot", 0, 0, 0.13, 0.13, INK, 0.0, True, 28)
        line("flick", 0.09, 0.09, 0.17, 0.17, 0.03, INK, -0.05)
    elif kind == "Blank":
        ring("ring", 0, 0, 0.13, 0.03, lerp(INK, PAPER, 0.3), PAPER, 0.0, 28)
    else:
        line("x1", -0.14, -0.14, 0.14, 0.14, 0.045, INK, 0.0)
        line("x2", -0.14, 0.14, 0.14, -0.14, 0.045, INK, -0.05)
        ellipse("ghost", 0, 0, 0.11, 0.11, lerp(INK, PAPER, 0.8), 0.05, False, 24)


def lamp(rng):
    """A lit lamp on its post, as the atlas marks a travel point."""
    ellipse("glow", 0.0, 0.08, 0.17, 0.17, lerp(OCHRE, PAPER, 0.7), 0.1, False, 24)
    line("post", 0.0, -0.19, 0.0, -0.02, 0.035, STEEL, 0.0, True)
    rect("foot", 0.0, -0.18, 0.14, 0.03, STEEL, -0.01)
    rrect("head", 0.0, 0.08, 0.16, 0.18, 0.03, lerp(OCHRE, PAPER, 0.5), -0.02)
    rect("cap", 0.0, 0.18, 0.12, 0.035, STEEL, -0.03)
    ellipse("flame", 0.0, 0.07, 0.03, 0.05, OCHRE, -0.1, False, 16)


def desk(rng):
    """A drafting desk: the slanted board on its legs."""
    poly("board", [(-0.18, -0.02), (0.18, 0.06), (0.18, 0.12), (-0.18, 0.04)], WOOD)
    line("leg_l", -0.13, -0.02, -0.13, -0.19, 0.035, WOOD, 0.0, True)
    line("leg_r", 0.13, 0.06, 0.13, -0.19, 0.035, WOOD, 0.0, True)
    line("sheet", -0.10, 0.02, 0.10, 0.065, 0.025, PAPER, -0.1)
    line("quill", 0.04, 0.09, 0.12, 0.20, 0.015, INK, -0.1)


# ---------------------------------------------------------------- the Charters' cowls

def wren_head(rgb_head=BROWN, y=-0.1, cx=0.02, cz=0.06):
    ellipse("head", cx, cz, 0.20, 0.19, rgb_head, y, True, 32)
    poly("beak", [(cx + 0.17, cz + 0.02), (cx + 0.30, cz - 0.03), (cx + 0.17, cz - 0.06)], BEAK, y - 0.02)
    ellipse("eye", cx + 0.08, cz + 0.05, 0.028, 0.028, INK, y - 0.03, False, 12)


def charter(name):
    cowl = CHARTER_COWLS[name]
    if name == "Surveyor":
        poly("cowl", [(-0.46, -0.46), (-0.38, -0.10), (-0.22, 0.26), (0.0, 0.36), (0.22, 0.26), (0.36, -0.06), (0.44, -0.46)], cowl)
        wren_head()
        ellipse("clasp", 0.0, -0.18, 0.055, 0.055, BRASS, -0.15, True, 16)
        rose(None, 0.07, 0.0, -0.18, with_ring=False, y=-0.22)
    elif name == "Warden":
        poly("collar", [(-0.30, -0.10), (-0.28, 0.20), (0.28, 0.20), (0.30, -0.10)], lerp(cowl, INK, 0.2), 0.02)
        poly("mantle", [(-0.48, -0.46), (-0.46, -0.08), (-0.30, 0.00), (0.30, 0.00), (0.46, -0.08), (0.48, -0.46)], cowl)
        wren_head(cz=0.08)
        ellipse("rivet", -0.36, -0.12, 0.03, 0.03, STEEL, -0.12, True, 12)
        ellipse("rivet2", 0.36, -0.12, 0.03, 0.03, STEEL, -0.12, True, 12)
    elif name == "Drifter":
        stroke("tail1", [(-0.26, -0.08), (-0.36, -0.20), (-0.46, -0.38)], 0.07, cowl, 0.03, True, 0.3)
        stroke("tail2", [(-0.24, -0.14), (-0.38, -0.30), (-0.44, -0.46)], 0.06, cowl, 0.04, True, 0.3)
        poly("hood", [(-0.34, -0.46), (-0.30, -0.06), (-0.20, 0.24), (0.02, 0.32), (0.24, 0.22), (0.30, -0.06), (0.34, -0.46)], cowl)
        wren_head()
    elif name == "Ferryman":
        poly("cowl", [(-0.42, -0.46), (-0.34, -0.08), (-0.20, 0.22), (0.0, 0.30), (0.20, 0.22), (0.34, -0.08), (0.42, -0.46)], cowl)
        wren_head()
        ellipse("brim", 0.02, 0.24, 0.46, 0.065, lerp(cowl, INK, 0.3), -0.2, True, 36)
        rrect("crown", 0.02, 0.33, 0.30, 0.16, 0.05, lerp(cowl, INK, 0.3), -0.18)
    elif name == "Unwriter":
        pts = [(-0.46, -0.46)]
        for k in range(15):
            a = D(180 - 180 * k / 14)
            r = 0.44 + 0.035 * ((k % 2) * 2 - 1)
            pts.append((r * math.cos(a) * 1.05, -0.12 + r * math.sin(a) * 1.05))
        pts.append((0.46, -0.46))
        poly("wool", pts, cowl)
        wren_head()
        ellipse("crown", 0.0, 0.30, 0.22, 0.10, cowl, -0.14, True, 24)
        ellipse("crown2", -0.12, 0.24, 0.12, 0.08, cowl, -0.15, True, 20)
    else:   # Remnant
        hem = []
        for k in range(10):
            hem.append((-0.44 + 0.88 * k / 9, -0.46 + (0.08 if k % 2 else 0.0)))
        poly("hood", hem + [(0.36, -0.06), (0.22, 0.26), (0.0, 0.36), (-0.16, 0.34), (-0.34, 0.20), (-0.44, 0.04), (-0.36, -0.04)], cowl)
        wren_head(lerp(BROWN, cowl, 0.6))
        stroke("point", [(-0.10, 0.34), (-0.26, 0.40), (-0.40, 0.30), (-0.44, 0.14)], 0.10, cowl, -0.02, True, 0.35)


# ---------------------------------------------------------------- the Instruments

def instrument(name):
    if name == "CompassDart":
        line("shaft", -0.34, -0.30, 0.22, 0.26, 0.05, STEEL, 0.0, True)
        poly("head", rotated([(0.22, 0.26), (0.30, 0.20), (0.44, 0.44), (0.20, 0.36)], 0), BRASS, -0.05)
        poly("fletch", [(-0.34, -0.30), (-0.44, -0.26), (-0.40, -0.38)], PAPER, -0.02)
        poly("fletch2", [(-0.30, -0.26), (-0.42, -0.16), (-0.36, -0.30)], INK, -0.03)
    elif name == "PlumbWeight":
        line("string", 0.0, 0.44, 0.0, 0.08, 0.018, INK, -0.05)
        ring("loop", 0.0, 0.10, 0.06, 0.025, BRASS, PAPER, -0.02, 16)
        poly("weight", [(-0.17, 0.04), (0.17, 0.04), (0.14, -0.10), (0.0, -0.42), (-0.14, -0.10)], BRASS)
        line("shine", -0.08, 0.0, -0.04, -0.20, 0.025, lerp(BRASS, PAPER, 0.5), -0.1)
    elif name == "SightingLens":
        ring("rim", 0.08, 0.10, 0.30, 0.05, BRASS, lerp(PAPER, WASH, 0.25), 0.0, 40)
        line("shine", -0.04, 0.22, 0.14, 0.26, 0.03, PAPER, -0.12)
        line("handle", -0.12, -0.12, -0.36, -0.40, 0.08, WOOD, 0.02, True)
        rect("ferrule", -0.14, -0.14, 0.10, 0.06, BRASS, -0.02)
    elif name == "FieldLantern":
        lantern(1.7, 0.0, -0.02)
    elif name == "TetherHook":
        stroke("hook", [(0.10, 0.20), (0.10, -0.10)] + ellipse_points(-0.04, -0.14, 0.15, 0.16, 12, D(0), D(-200))[1:], 0.07, STEEL, 0.0, True, 0.5)
        for k in range(3):
            ellipse("coil_%d" % k, 0.10, 0.26 + 0.07 * k, 0.16, 0.05, ROPE, -0.02 - 0.01 * k, True, 20)
    elif name == "IrisTincture":
        poly("vial", rrect_points(0, -0.08, 0.34, 0.50, 0.10, 5)[0:], lerp(PAPER, IRIS, 0.5))
        rect("neck", 0, 0.22, 0.14, 0.12, lerp(PAPER, IRIS, 0.25), 0.01)
        rect("stopper", 0, 0.33, 0.16, 0.10, WOOD, -0.02)
        poly("fill", rrect_points(0, -0.15, 0.26, 0.30, 0.08, 5), IRIS, -0.05, False)
        line("shine", -0.10, -0.26, -0.10, 0.02, 0.03, PAPER, -0.1)
        poly("drop", [(0.26, -0.22), (0.34, -0.34), (0.26, -0.42), (0.18, -0.34)], IRIS, -0.06)
    else:   # WaxSeal
        rng = random.Random(7)
        pts = [(0.30 * (1 + rng.uniform(-0.06, 0.06)) * math.cos(a), 0.04 + 0.30 * (1 + rng.uniform(-0.06, 0.06)) * math.sin(a)) for a in [2 * math.pi * k / 28 for k in range(28)]]
        poly("ribbon", [(-0.10, -0.10), (0.0, -0.14), (-0.06, -0.44), (-0.18, -0.40)], PAPER_DARK, 0.05)
        poly("ribbon2", [(0.02, -0.10), (0.12, -0.12), (0.22, -0.42), (0.10, -0.44)], PAPER_DARK, 0.05)
        poly("wax", pts, WAX)
        ring("impress", 0.0, 0.04, 0.20, 0.025, lerp(WAX, INK, 0.5), WAX, -0.05, 32)
        rose(None, 0.26, 0.0, 0.04, with_ring=False, y=-0.16)


# ---------------------------------------------------------------- the table: name -> (w px, h px, line, slices, build)

def S(l, t=None, r=None, b=None):
    t = l if t is None else t
    r = l if r is None else r
    b = t if b is None else b
    return dict(l=l, t=t, r=r, b=b)


PIECES = [
    ("UI_Page", 1024, 512, 3.2, S(96), paper_page),
    ("UI_Strip", 768, 128, 3.0, S(64, 24), paper_strip),
    ("UI_Spread", 1500, 900, 3.2, S(120, 100), paper_spread),
    ("UI_Portrait", 256, 256, 3.0, S(40), paper_portrait),
    ("UI_MaskFull", 96, 96, 2.6, None, lambda rng: feather(True)),
    ("UI_MaskEmpty", 96, 96, 2.6, None, lambda rng: feather(False)),
    ("UI_Inkwell", 112, 144, 2.8, None, inkwell),
    ("UI_InkwellFill", 112, 144, 2.8, None, inkwell_fill),
    ("UI_Lantern", 48, 48, 2.0, None, lambda rng: lantern(1.3)),
    ("UI_BossBar", 1024, 48, 3.0, None, boss_bar),
    ("UI_BossFill", 1024, 48, 3.0, None, boss_fill),
    ("UI_Tick", 12, 48, 2.0, None, tick),
    ("UI_Marker", 48, 48, 2.2, None, marker),
    ("UI_Seed", 40, 40, 2.0, None, seed),
    ("UI_Rose", 128, 128, 2.6, None, lambda rng: rose(rng)),
    ("UI_VantageDrawn", 40, 40, 2.0, None, lambda rng: vantage("Drawn")),
    ("UI_VantageBlank", 40, 40, 2.0, None, lambda rng: vantage("Blank")),
    ("UI_VantageErased", 40, 40, 2.0, None, lambda rng: vantage("Erased")),
    ("UI_Lamp", 40, 40, 2.0, None, lamp),
    ("UI_Desk", 40, 40, 2.0, None, desk),
]
for _c in CHARTER_COWLS:
    PIECES.append(("UI_Charter_" + _c, 96, 96, 2.6, None, (lambda c: lambda rng: charter(c))(_c)))
for _i in ("CompassDart", "PlumbWeight", "SightingLens", "FieldLantern", "TetherHook", "IrisTincture", "WaxSeal"):
    PIECES.append(("UI_Instrument_" + _i, 96, 96, 2.6, None, (lambda i: lambda rng: instrument(i))(_i)))


def main():
    only = argv_after_dashes()
    os.makedirs(FRAMES, exist_ok=True)
    manifest_path = os.path.join(FRAMES, "ui.json")
    manifest = {"pieces": []}
    if os.path.exists(manifest_path):
        with open(manifest_path) as f:
            manifest = json.load(f)
    by_name = {p["name"]: p for p in manifest["pieces"]}
    for name, w, h, line_px, slices, build in PIECES:
        if only and not any(name == o or (o.endswith("*") and name.startswith(o[:-1])) for o in only):
            continue
        reset_scene()
        seed = sum(ord(c) for c in name)
        rng = random.Random(seed)
        build(rng)
        setup(w, h, line_px, seed=seed % 1000)
        size = render(os.path.join(FRAMES, name + ".png"))
        entry = {"name": name, "w": w, "h": h}
        if slices:
            entry["slices"] = slices
        if name == "UI_InkwellFill":
            entry["fill"] = {"bottom": round(FILL_BOTTOM, 4), "top": round(FILL_TOP, 4)}
        by_name[name] = entry
        print("[ui] %s: %dx%d at 1x (%d bytes at 2x)" % (name, w, h, size))
    order = [p[0] for p in PIECES]
    manifest["pieces"] = [by_name[n] for n in order if n in by_name]
    with open(manifest_path, "w") as f:
        json.dump(manifest, f, indent=2)


if __name__ == "__main__":
    main()

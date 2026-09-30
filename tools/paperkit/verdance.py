"""Verdance paper kit (ENV-04): every backdrop strip and ground tile the forest's rooms use, rendered from cut-out
geometry with Freestyle ink lines in headless Blender, on the plumbing all the kits share (kitlib.py).

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/verdance.py
    ... -P tools/paperkit/verdance.py -- Paper_Mid_Trunks Ground_Moss      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Verdance/.
The bible (4.3): trees eighty wingspans tall, light in shafts, near silence; a monastery grown into one tree's
roots; lanterns hung in the canopy by birds who could reach them; a library the forest floor swallowed; a village
on its last day; a gate for flyers with roots for a path. Pale gold paper, deep green, moss, bone white, sepia ink
(art-direction 5). Farther layers wash toward the paper and draw thinner, as on the coast.
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Verdance")

# Verdance palette (art-direction 5): paper, wash 1, wash 2, accent, ink. The paper is ProjectSetup.RegionPaper's.
PAPER = (0.94, 0.90, 0.72)
GREEN = (0.16, 0.30, 0.20)
MOSS = (0.48, 0.56, 0.30)
BONE = (0.93, 0.90, 0.82)
INK = (0.26, 0.17, 0.10)
BARK = lerp(INK, MOSS, 0.35)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


# ---------------------------------------------------------------- shapes

def trunk(name, rng, x, base_z, top_z, w, bark, dark, y=0.0, buttress=True):
    """A tree eighty wingspans tall: a trunk that leaves the top of the frame, buttress roots flaring at its foot,
    bark lines up it. The top is never drawn: the tree goes on."""
    pts = [(x - w / 2, base_z)]
    if buttress:
        pts = [(x - w * 1.4, base_z), (x - w * 0.9, base_z + 0.35), (x - w * 0.55, base_z + 1.2)]
    pts += [(x - w * 0.5 + rng.uniform(-0.05, 0.05), base_z + (top_z - base_z) * 0.5), (x - w * 0.42, top_z), (x + w * 0.42, top_z),
            (x + w * 0.5 + rng.uniform(-0.05, 0.05), base_z + (top_z - base_z) * 0.5)]
    if buttress:
        pts += [(x + w * 0.55, base_z + 1.2), (x + w * 0.9, base_z + 0.35), (x + w * 1.4, base_z)]
    else:
        pts += [(x + w / 2, base_z)]
    polygon(name, pts, bark, y=y)
    for i in range(int((top_z - base_z) / 1.4)):
        z = base_z + 1.0 + i * 1.4 + rng.uniform(-0.3, 0.3)
        bx = x + rng.uniform(-w * 0.3, w * 0.3)
        box(name + "_bark%d" % i, bx, z, 0.06, rng.uniform(0.5, 1.3), dark, y=y - 0.03)


def root(name, rng, x0, z0, x1, z1, thick, material, y=0.0, bow=0.6, n=10):
    """A root: a thick band along a bowed line from one point to another, tapering toward the far end."""
    length = max(0.01, math.hypot(x1 - x0, z1 - z0))
    nx, nz = -(z1 - z0) / length, (x1 - x0) / length   # the band's width lies across its direction, so a vertical root keeps its thickness
    pts_a, pts_b = [], []
    for i in range(n + 1):
        t = i / n
        s = bow * math.sin(t * math.pi) + rng.uniform(-0.04, 0.04)
        cx = x0 + (x1 - x0) * t + nx * s
        cz = z0 + (z1 - z0) * t + nz * s
        th = thick * (1.0 - 0.6 * t)
        pts_a.append((cx + nx * th / 2, cz + nz * th / 2))
        pts_b.append((cx - nx * th / 2, cz - nz * th / 2))
    return polygon(name, pts_a + pts_b[::-1], material, y=y)


def lantern(name, x, z, size, body, glow, dark, cord=0.0, y=0.0, lit=True):
    """A grove lantern: a paper body on a cord, its light on the paper behind it when it is lit."""
    if cord > 0:
        box(name + "_cord", x, z + size * 0.6 + cord / 2, 0.03, cord, dark, y=y + 0.02)
    if lit:
        disc(name + "_glow", x, z, size * 1.5, glow, y=y + 0.12, n=20)
    polygon(name, [(x - size * 0.35, z - size * 0.6), (x + size * 0.35, z - size * 0.6), (x + size * 0.5, z - size * 0.2), (x + size * 0.5, z + size * 0.25),
                   (x + size * 0.3, z + size * 0.6), (x - size * 0.3, z + size * 0.6), (x - size * 0.5, z + size * 0.25), (x - size * 0.5, z - size * 0.2)], body, y=y)
    box(name + "_cap", x, z + size * 0.62, size * 0.5, size * 0.1, dark, y=y - 0.02)
    box(name + "_foot", x, z - size * 0.62, size * 0.4, size * 0.08, dark, y=y - 0.02)
    for i in range(2):
        box(name + "_rib%d" % i, x, z - size * 0.15 + i * size * 0.35, size * 0.9, 0.025, dark, y=y - 0.03)


def light_shaft(name, x, z0, z1, w, material, lean=0.35, y=0.0):
    """Light in shafts: a pale slanted band through the trees."""
    return polygon(name, [(x - w / 2, z0), (x + w / 2, z0), (x + w / 2 + (z1 - z0) * lean, z1), (x - w / 2 + (z1 - z0) * lean, z1)], material, y=y)


def house(name, rng, x, base_z, w, h, walls, roof, dark, y=0.0):
    """A village house: a low wall, a deep thatched roof, a door and a window, bread on the sill."""
    box(name, x, base_z + h / 2, w, h, walls, y=y)
    polygon(name + "_roof", [(x - w / 2 - 0.35, base_z + h), (x + w / 2 + 0.35, base_z + h), (x + w * 0.25, base_z + h + w * 0.42), (x - w * 0.25, base_z + h + w * 0.42)], roof, y=y - 0.05)
    polygon(name + "_door", [(x - w * 0.15, base_z), (x + w * 0.15, base_z), (x + w * 0.15, base_z + h * 0.7), (x, base_z + h * 0.82), (x - w * 0.15, base_z + h * 0.7)], dark, y=y - 0.08)
    wx = x + w * 0.3 * (1 if rng.random() < 0.5 else -1)
    box(name + "_win", wx, base_z + h * 0.55, w * 0.18, h * 0.3, dark, y=y - 0.08)
    box(name + "_sill", wx, base_z + h * 0.38, w * 0.24, 0.06, roof, y=y - 0.1)


def bunting(name, rng, x0, x1, z, sag, cord, flag_a, flag_b, y=0.0, n=None):
    """Bunting: a sagging cord with small flags hung from it."""
    if n is None:
        n = max(3, int((x1 - x0) / 0.55))
    prev = None
    for i in range(n + 1):
        t = i / n
        px = x0 + (x1 - x0) * t
        pz = z - sag * math.sin(t * math.pi)
        if prev is not None:
            dx, dz = px - prev[0], pz - prev[1]
            length = math.hypot(dx, dz)
            ob = box("%s_cord%d" % (name, i), 0, 0, length, 0.03, cord, y=y, d=0.1)
            ob.rotation_euler = (0, -math.atan2(dz, dx), 0)
            ob.location = ((px + prev[0]) / 2, ob.location[1], (pz + prev[1]) / 2)
        if 0 < i < n:
            fw = 0.22
            polygon("%s_flag%d" % (name, i), [(px - fw / 2, pz), (px + fw / 2, pz), (px + rng.uniform(-0.05, 0.05), pz - fw * 1.4)], flag_a if i % 2 == 0 else flag_b, y=y - 0.02)
        prev = (px, pz)


def milestone(name, x, base_z, h, stone, dark, y=0.0, letters=3):
    """A flying-age milestone: a tall slab with a rounded top, its letters cut large and high."""
    w = h * 0.34
    pts = [(x - w / 2, base_z), (x + w / 2, base_z), (x + w / 2, base_z + h - w * 0.5)]
    for i in range(1, 6):
        a = math.pi * i / 6
        pts.append((x + w / 2 * math.cos(a), base_z + h - w * 0.5 + w / 2 * math.sin(a)))
    pts.append((x - w / 2, base_z + h - w * 0.5))
    polygon(name, pts, stone, y=y)
    for i in range(letters):
        box(name + "_cut%d" % i, x, base_z + h * 0.78 - i * h * 0.13, w * 0.5, h * 0.05, dark, y=y - 0.05)


def shelf(name, rng, x, base_z, w, h, wood, spines, dark, y=0.0, rows=3):
    """A bookcase: uprights and boards, the books' spines between them in the palette's few colours."""
    box(name + "_l", x - w / 2, base_z + h / 2, 0.12, h, wood, y=y)
    box(name + "_r", x + w / 2, base_z + h / 2, 0.12, h, wood, y=y)
    for r in range(rows + 1):
        box(name + "_board%d" % r, x, base_z + h * r / rows, w, 0.09, wood, y=y - 0.02)
    for r in range(rows):
        bx = x - w / 2 + 0.12
        k = 0
        while bx < x + w / 2 - 0.2:
            bw = rng.uniform(0.09, 0.2)
            bh = rng.uniform(0.45, 0.78) * (h / rows - 0.12)
            box("%s_book%d_%d" % (name, r, k), bx + bw / 2, base_z + h * r / rows + 0.05 + bh / 2, bw - 0.02, bh, rng.choice(spines), y=y - 0.04)
            bx += bw
            k += 1
        if rng.random() < 0.5:
            box("%s_gap%d" % (name, r), bx + 0.1, base_z + h * r / rows + 0.3, 0.2, 0.5, dark, y=y - 0.03)


def fern(name, rng, x, base_z, h, lean, frond, y=0.0, pinnae=7):
    """A fern frond: a leaning stem with pairs of leaflets shrinking toward the tip."""
    tip = (x + lean, base_z + h)
    box(name + "_stem", x + lean / 2, base_z + h / 2, 0.035, h, frond, y=y)
    for i in range(pinnae):
        t = (i + 1) / (pinnae + 1)
        px, pz = x + lean * t, base_z + h * t
        pl = h * 0.28 * (1 - t * 0.7)
        for s in (-1, 1):
            polygon("%s_p%d%s" % (name, i, "a" if s < 0 else "b"), [(px, pz - 0.03), (px + s * pl, pz + pl * 0.35), (px + s * pl * 0.6, pz + pl * 0.55), (px, pz + 0.05)], frond, y=y - 0.01)


def dust(rng, n, x0, x1, z0, z1, mat, y=0.0):
    """Dust that hangs where it is: small pale flecks."""
    for i in range(n):
        disc("dust_%d" % i, rng.uniform(x0, x1), rng.uniform(z0, z1), rng.uniform(0.03, 0.07), mat, y=y, n=6)


# ---------------------------------------------------------------- layers

def layer_fore_ferns(rng, faded):
    """Paper_Fore_Ferns: the undergrowth in front of the walk: ferns and a few tall grasses, dark, a thick line."""
    p = Palette(0.0, faded)
    frond, dark = p("frond", lerp(GREEN, INK, 0.25)), p("dark", lerp(GREEN, INK, 0.5))
    for i in range(70):
        x = -40 + i * 1.15 + rng.uniform(-0.4, 0.4)
        fern("fern_%d" % i, rng, x, -0.8, rng.uniform(1.2, 2.1), rng.uniform(-0.7, 0.7), frond if i % 3 else dark, y=-0.3 - 0.03 * (i % 4))
    for i in range(40):
        box("blade_%d" % i, rng.uniform(-40, 40), -0.2, 0.05, rng.uniform(0.6, 1.4), dark, y=-0.35)
    return STRIP_WIDTH, 1.6, PPU, -0.8, 3.0, p.ink()


def layer_mid_trunks(rng, faded):
    """Paper_Mid_Trunks: the first trees: trunks eighty wingspans tall leaving the top of the strip, roots at their
    feet, moss on the north side, ferns between."""
    p = Palette(0.15, faded)
    bark, dark, moss, frond, floor = p("bark", BARK), p("dark", lerp(BARK, INK, 0.5)), p("moss", MOSS), p("frond", GREEN), p("floor", lerp(MOSS, PAPER, 0.35))
    ridge("floor", rng, -40, 40, 0.0, 0.6, 0.2, 36, floor, y=0.4)
    xs = [-36, -25, -14, -4, 7, 18, 29, 38]
    for k, x0 in enumerate(xs):
        x = x0 + rng.uniform(-1.5, 1.5)
        w = rng.uniform(1.6, 2.8)
        trunk("trunk_%d" % k, rng, x, 0.2, 6.2, w, bark, dark, y=0.1 + 0.02 * (k % 3))
        polygon("moss_%d" % k, [(x - w * 0.5, 0.3), (x - w * 0.15, 0.3), (x - w * 0.2, 2.6), (x - w * 0.45, 3.4)], moss, y=0.05)
    for i in range(30):
        fern("fern_%d" % i, rng, rng.uniform(-40, 40), 0.45, rng.uniform(0.7, 1.3), rng.uniform(-0.4, 0.4), frond, y=-0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_roots(rng, faded):
    """Paper_Mid_Roots: the Quiet House: a wall of one tree's roots, a door cut in them, small windows, lanterns
    hung from the roots, moss in the joins."""
    p = Palette(0.15, faded)
    bark, dark, moss, glow, body, wall = p("bark", BARK), p("dark", lerp(BARK, INK, 0.55)), p("moss", MOSS), p("glow", lerp(BONE, PAPER, 0.4)), p("body", BONE), p("wall", lerp(BARK, PAPER, 0.3))
    polygon("wall", [(-40, 0), (40, 0), (40, 6.2), (-40, 6.2)], wall, y=0.5)
    k = 0
    for row in range(5):
        z = 0.6 + row * 1.25
        x = -41
        while x < 40:
            length = rng.uniform(4, 9)
            root("root_%d" % k, rng, x, z + rng.uniform(-0.3, 0.3), x + length, z + rng.uniform(-0.5, 0.5), rng.uniform(0.35, 0.7), bark if k % 3 else dark, y=0.3 - 0.02 * (row % 2), bow=rng.uniform(-0.5, 0.5), n=8)
            x += length - 0.8
            k += 1
    for i in range(24):
        blob("moss_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(0.3, 5.5), rng.uniform(0.3, 0.7), rng.uniform(0.15, 0.3), moss, y=0.2, wobble=0.35)
    for j, x in enumerate((-26, 0, 27)):
        polygon("door_%d" % j, [(x - 0.9, 0), (x + 0.9, 0), (x + 0.9, 2.2), (x + 0.4, 3.0), (x - 0.4, 3.0), (x - 0.9, 2.2)], dark, y=0.0)
        box("lintel_%d" % j, x, 3.05, 2.4, 0.2, bark, y=-0.02)
    for j, x in enumerate((-33, -18, -9, 8, 17, 35)):
        box("win_%d" % j, x, rng.uniform(2.0, 3.6), 0.6, 0.8, dark, y=0.0)
    for j in range(7):
        x = -36 + j * 12 + rng.uniform(-2, 2)
        lantern("lantern_%d" % j, x, rng.uniform(3.6, 4.8), 0.6, body, glow, dark, cord=rng.uniform(0.6, 1.4), y=-0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_branches(rng, faded):
    """Paper_Mid_Branches: the Lantern Grove at eye height: branches crossing the strip with lanterns hung from them
    and the knots the threads catch, leaves in clusters."""
    p = Palette(0.15, faded)
    bark, dark, leaf, glow, body, bone = p("bark", BARK), p("dark", lerp(BARK, INK, 0.5)), p("leaf", GREEN), p("glow", lerp(BONE, PAPER, 0.4)), p("body", BONE), p("bone", BONE)
    for k in range(6):
        x = -42 + k * 15 + rng.uniform(-2, 2)
        z = rng.uniform(1.5, 4.5)
        root("branch_%d" % k, rng, x, z, x + rng.uniform(14, 20), z + rng.uniform(-1.5, 1.5), rng.uniform(0.35, 0.6), bark, y=0.3, bow=rng.uniform(-0.8, 0.8), n=10)
        for i in range(3):
            bx = x + rng.uniform(2, 12)
            root("twig_%d_%d" % (k, i), rng, bx, z + rng.uniform(-0.3, 0.3), bx + rng.uniform(1.5, 4), z + rng.uniform(1.0, 2.5), 0.15, dark, y=0.25, bow=0.2, n=5)
    for i in range(40):
        blob("leaves_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(1.0, 6.0), rng.uniform(0.5, 1.1), rng.uniform(0.3, 0.6), leaf, y=0.15 + 0.01 * (i % 3), wobble=0.45)
    for j in range(9):
        x = -38 + j * 9.5 + rng.uniform(-2, 2)
        lantern("lantern_%d" % j, x, rng.uniform(1.2, 3.4), rng.uniform(0.5, 0.75), body, glow, dark, cord=rng.uniform(0.8, 1.8), y=-0.1, lit=(j % 3 != 1))
    for j in range(5):
        x = -30 + j * 15 + rng.uniform(-3, 3)
        z = rng.uniform(2.5, 5.0)
        disc("knot_%d" % j, x, z, 0.32, dark, y=-0.05, n=12)
        disc("ring_%d" % j, x, z, 0.16, bone, y=-0.1, n=12)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_shelves(rng, faded):
    """Paper_Mid_Shelves: the Sunken Library: bookcases the forest floor swallowed, roots through them, dust that
    does not fall, a lectern's shadow."""
    p = Palette(0.15, faded)
    wood, dark, earth, bark, dustm = p("wood", lerp(BARK, PAPER, 0.15)), p("dark", lerp(INK, BARK, 0.3)), p("earth", lerp(BARK, PAPER, 0.45)), p("bark", BARK), p("dust", lerp(BONE, PAPER, 0.3))
    spines = [p("spine_a", GREEN), p("spine_b", lerp(BARK, PAPER, 0.3)), p("spine_c", BONE), p("spine_d", MOSS)]
    polygon("earth", [(-40, 0), (40, 0), (40, 6.2), (-40, 6.2)], earth, y=0.5)
    x = -39
    k = 0
    while x < 39:
        w = rng.uniform(3.0, 5.5)
        h = rng.uniform(3.8, 5.6)
        shelf("case_%d" % k, rng, x + w / 2, 0.0, w, h, wood, spines, dark, y=0.2, rows=rng.choice((3, 4)))
        x += w + rng.uniform(0.4, 1.6)
        k += 1
    for i in range(10):
        rx = rng.uniform(-40, 36)
        root("root_%d" % i, rng, rx, 6.2, rx + rng.uniform(2, 6), rng.uniform(2.0, 4.5), rng.uniform(0.25, 0.5), bark, y=0.05, bow=rng.uniform(-0.6, 0.6), n=7)
    dust(rng, 80, -40, 40, 0.5, 6.0, dustm, y=-0.15)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_village(rng, faded):
    """Paper_Mid_Village: Aldermere on its last day: low houses under deep roofs, bunting between them, bread on the
    sills, the trees behind."""
    p = Palette(0.15, faded)
    walls, roof, dark, cord, flag_a, flag_b, floor = p("walls", lerp(BONE, PAPER, 0.3)), p("roof", lerp(BARK, MOSS, 0.4)), p("dark", lerp(BARK, INK, 0.5)), p("cord", BARK), p("flag_a", GREEN), p("flag_b", BONE), p("floor", lerp(MOSS, PAPER, 0.4))
    ridge("floor", rng, -40, 40, 0.0, 0.5, 0.15, 36, floor, y=0.4)
    x = -39
    k = 0
    poles = []
    while x < 38:
        w = rng.uniform(2.6, 4.4)
        h = rng.uniform(1.4, 2.2)
        house("house_%d" % k, rng, x + w / 2, 0.4, w, h, walls, roof, dark, y=0.1)
        poles.append((x + w / 2, 0.4 + h + w * 0.42))
        x += w + rng.uniform(1.0, 2.6)
        k += 1
    for i in range(len(poles) - 1):
        (x0, z0), (x1, z1) = poles[i], poles[i + 1]
        bunting("bunting_%d" % i, rng, x0, x1, min(z0, z1) - 0.1, 0.5, cord, flag_a, flag_b, y=-0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_ash(rng, faded):
    """Paper_Mid_Ash: the ash field: where the village is already paper: the same houses drawn in outline only, bone
    and paper, the bunting still up, ash on the ground."""
    p = Palette(0.35, faded)
    walls, roof, dark, cord, flag, ash = p("walls", lerp(BONE, PAPER, 0.6)), p("roof", lerp(BONE, PAPER, 0.4)), p("dark", lerp(BONE, PAPER, 0.2)), p("cord", lerp(BARK, PAPER, 0.5)), p("flag", lerp(BONE, PAPER, 0.5)), p("ash", BONE)
    ridge("ash", rng, -40, 40, 0.0, 0.5, 0.15, 36, ash, y=0.4)
    x = -39
    k = 0
    poles = []
    while x < 38:
        w = rng.uniform(2.6, 4.4)
        h = rng.uniform(1.4, 2.2)
        house("house_%d" % k, rng, x + w / 2, 0.4, w, h, walls, roof, dark, y=0.1)
        poles.append((x + w / 2, 0.4 + h + w * 0.42))
        x += w + rng.uniform(1.0, 2.6)
        k += 1
    for i in range(len(poles) - 1):
        (x0, z0), (x1, z1) = poles[i], poles[i + 1]
        bunting("bunting_%d" % i, rng, x0, x1, min(z0, z1) - 0.1, 0.5, cord, flag, flag, y=-0.1)
    for i in range(50):
        disc("fleck_%d" % i, rng.uniform(-40, 40), rng.uniform(0.3, 5.5), rng.uniform(0.04, 0.09), ash, y=-0.2, n=6)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_gate(rng, faded):
    """Paper_Mid_Gate: the Overgrown Gate: two stone gateposts with a landing ledge between them, high, for the
    winged; roots up and over the stone; the inscription's band."""
    p = Palette(0.15, faded)
    stone, dark, bark, moss, band = p("stone", lerp(BONE, MOSS, 0.25)), p("dark", lerp(INK, MOSS, 0.4)), p("bark", BARK), p("moss", MOSS), p("band", lerp(BONE, PAPER, 0.2))
    ridge("floor", rng, -40, 40, 0.0, 0.5, 0.15, 36, p("floor", lerp(MOSS, PAPER, 0.35)), y=0.4)
    for j, x in enumerate((-12, 12)):
        polygon("post_%d" % j, [(x - 2.2, 0), (x + 2.2, 0), (x + 1.9, 6.2), (x - 1.9, 6.2)], stone, y=0.2)
        for i in range(6):
            box("course_%d_%d" % (j, i), x, 0.5 + i * 1.0, 3.8 - i * 0.05, 0.06, dark, y=0.15)
    box("ledge", 0, 4.6, 20.4, 0.5, stone, y=0.25)
    box("ledge_lip", 0, 4.9, 20.8, 0.12, dark, y=0.2)
    box("band", 0, 5.5, 19.0, 0.6, band, y=0.22)
    for i in range(9):
        box("letter_%d" % i, -8 + i * 2.0, 5.5, 1.1, 0.12, dark, y=0.18)
    for i in range(12):
        x0 = rng.uniform(-16, 16)
        root("root_%d" % i, rng, x0, 0.0, x0 + rng.uniform(-3, 3), rng.uniform(2.5, 6.0), rng.uniform(0.25, 0.5), bark, y=0.0 - 0.01 * i, bow=rng.uniform(-0.8, 0.8), n=8)
    for i in range(16):
        blob("moss_%d" % i, rng, rng.uniform(-15, 15), rng.uniform(0.3, 5.8), rng.uniform(0.3, 0.7), rng.uniform(0.15, 0.3), moss, y=-0.05, wobble=0.35)
    for k, x in enumerate((-32, -24, 24, 33)):
        trunk("trunk_%d" % k, rng, x + rng.uniform(-1, 1), 0.2, 6.2, rng.uniform(1.6, 2.4), bark, dark, y=0.3)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_canopy(rng, faded):
    """Paper_Far_Canopy: the trunks going on up behind everything, leaves in masses, light in shafts between them."""
    p = Palette(0.45, faded)
    bark, dark, leaf, leaf2, light = p("bark", BARK), p("dark", lerp(BARK, INK, 0.4)), p("leaf", GREEN), p("leaf2", MOSS), p("light", BONE, 0.2)
    for k in range(9):
        x = -38 + k * 9.5 + rng.uniform(-2, 2)
        trunk("trunk_%d" % k, rng, x, 2.0, 12.2, rng.uniform(1.0, 1.8), bark, dark, y=0.3, buttress=False)
    for i in range(7):
        light_shaft("shaft_%d" % i, -34 + i * 11 + rng.uniform(-2, 2), 2.0, 12.0, rng.uniform(1.2, 2.6), light, lean=rng.uniform(0.25, 0.45), y=0.6)
    for i in range(50):
        blob("leaves_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(6.5, 12.0), rng.uniform(1.0, 2.4), rng.uniform(0.5, 1.1), leaf if i % 3 else leaf2, y=0.1 + 0.01 * (i % 4), wobble=0.4)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_far_lanterns(rng, faded):
    """Paper_Far_Lanterns: lanterns hung in the canopy by birds who could reach it, some still lit; branches and
    leaves around them."""
    p = Palette(0.45, faded)
    bark, dark, leaf, glow, body = p("bark", BARK), p("dark", lerp(BARK, INK, 0.4)), p("leaf", GREEN), p("glow", BONE, 0.1), p("body", BONE)
    for k in range(7):
        x = -38 + k * 12 + rng.uniform(-2, 2)
        trunk("trunk_%d" % k, rng, x, 2.0, 12.2, rng.uniform(0.9, 1.6), bark, dark, y=0.3, buttress=False)
    for k in range(8):
        x = -42 + k * 11 + rng.uniform(-2, 2)
        z = rng.uniform(5, 11)
        root("branch_%d" % k, rng, x, z, x + rng.uniform(8, 14), z + rng.uniform(-1, 1), 0.3, bark, y=0.25, bow=rng.uniform(-0.6, 0.6), n=8)
    for i in range(36):
        blob("leaves_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(4.5, 12.0), rng.uniform(0.8, 2.0), rng.uniform(0.4, 0.9), leaf, y=0.15, wobble=0.4)
    for j in range(16):
        x = -39 + j * 5.2 + rng.uniform(-1.5, 1.5)
        lantern("lantern_%d" % j, x, rng.uniform(3.0, 10.5), rng.uniform(0.4, 0.7), body, glow, dark, cord=rng.uniform(0.6, 2.2), y=-0.1, lit=(j % 4 != 2))
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_farther_forest(rng, faded):
    """Paper_Farther_Forest: the forest behind the forest, trunks nearly paper, the light in shafts through them."""
    p = Palette(0.6, faded)
    bark, far, light = p("bark", BARK), p("far", BARK, 0.15), p("light", BONE, 0.1)
    for k in range(14):
        x = -40 + k * 6 + rng.uniform(-1.5, 1.5)
        trunk("far_%d" % k, rng, x, 6.0, 22.2, rng.uniform(0.7, 1.3), far, far, y=0.6, buttress=False)
    for k in range(8):
        x = -37 + k * 10.5 + rng.uniform(-2, 2)
        trunk("near_%d" % k, rng, x, 6.0, 22.2, rng.uniform(1.0, 1.8), bark, bark, y=0.3, buttress=False)
    for i in range(6):
        light_shaft("shaft_%d" % i, -30 + i * 12 + rng.uniform(-3, 3), 6.0, 22.0, rng.uniform(2.0, 4.0), light, lean=rng.uniform(0.2, 0.4), y=0.2)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.3, p.ink()


def tile_root(rng, faded):
    """Ground_Root: the House's floor: bark and root, roots running along it in bands, moss in the joins."""
    p = Palette(0.0, faded)
    bark, dark, moss = p("bark", BARK), p("dark", lerp(BARK, INK, 0.5)), p("moss", MOSS)
    box("bed", 0, 0.5, 4.0, 1.0, bark, y=0.2)
    for i in range(4):
        root("root_%d" % i, rng, -2.1, 0.15 + i * 0.24 + rng.uniform(-0.04, 0.04), 2.1, 0.15 + i * 0.24 + rng.uniform(-0.04, 0.04), 0.12, dark if i % 2 else moss, y=0.0, bow=rng.uniform(-0.05, 0.05), n=12)
    for i in range(3):
        blob("moss_%d" % i, rng, rng.uniform(-1.8, 1.8), rng.uniform(0.7, 0.92), rng.uniform(0.15, 0.3), 0.06, moss, y=-0.05, wobble=0.3)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_moss(rng, faded):
    """Ground_Moss: the road and the grove floor: packed earth with moss lying along the top, a pebble or two."""
    p = Palette(0.0, faded)
    earth, moss, pebble, dark = p("earth", lerp(BARK, PAPER, 0.3)), p("moss", MOSS), p("pebble", lerp(BONE, MOSS, 0.4)), p("dark", lerp(BARK, INK, 0.4))
    box("bed", 0, 0.45, 4.0, 0.9, earth, y=0.2)
    ridge("moss", rng, -2.0, 2.0, 0.72, 0.22, 0.08, 14, moss, y=-0.02)
    for i in range(5):
        disc("pebble_%d" % i, rng.uniform(-1.9, 1.9), rng.uniform(0.15, 0.6), rng.uniform(0.04, 0.08), pebble, y=-0.05, n=8)
    for i in range(3):
        box("crack_%d" % i, rng.uniform(-1.9, 1.9), rng.uniform(0.2, 0.6), 0.03, rng.uniform(0.15, 0.4), dark, y=-0.06)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_flag(rng, faded):
    """Ground_Flag: the library's and the gate's flagstones: pale stone in long slabs, joints, a fern in one crack."""
    p = Palette(0.0, faded)
    stone, joint, frond = p("stone", lerp(BONE, MOSS, 0.2)), p("joint", lerp(INK, MOSS, 0.4)), p("frond", GREEN)
    box("bed", 0, 0.5, 4.0, 1.0, stone, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.8, 1.5)
        box("slab_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.5, w - 0.05, 0.92, stone, y=0.0)
        x += w
        i += 1
    box("course", 0, 0.5, 4.0, 0.03, joint, y=-0.05)
    fern("fern", rng, rng.uniform(-1.5, 1.5), 0.55, 0.4, 0.15, frond, y=-0.1, pinnae=4)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_lane(rng, faded):
    """Ground_Lane: Aldermere's lane: packed earth with cart ruts, and patches already paper."""
    p = Palette(0.0, faded)
    earth, rut, paper, moss = p("earth", lerp(BARK, PAPER, 0.4)), p("rut", lerp(BARK, INK, 0.3)), p("paper", PAPER), p("moss", MOSS)
    box("bed", 0, 0.5, 4.0, 1.0, earth, y=0.2)
    for z in (0.3, 0.62):
        box("rut_%d" % int(z * 100), 0, z, 4.0, 0.05, rut, y=-0.02)
    for i in range(3):
        blob("paper_%d" % i, rng, rng.uniform(-1.7, 1.7), rng.uniform(0.2, 0.8), rng.uniform(0.2, 0.45), rng.uniform(0.08, 0.16), paper, y=-0.05, wobble=0.4)
    ridge("moss", rng, -2.0, -1.0, 0.85, 0.1, 0.04, 6, moss, y=-0.06)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Fore_Ferns", "strip", layer_fore_ferns, False),
    ("Paper_Mid_Trunks", "strip", layer_mid_trunks, False),
    ("Paper_Mid_Roots", "strip", layer_mid_roots, False),
    ("Paper_Mid_Branches", "strip", layer_mid_branches, False),
    ("Paper_Mid_Shelves", "strip", layer_mid_shelves, False),
    ("Paper_Mid_Village", "strip", layer_mid_village, False),
    ("Paper_Mid_Ash", "strip", layer_mid_ash, False),
    ("Paper_Mid_Gate", "strip", layer_mid_gate, False),
    ("Paper_Far_Canopy", "strip", layer_far_canopy, False),
    ("Paper_Far_Lanterns", "strip", layer_far_lanterns, False),
    ("Paper_Farther_Forest", "strip", layer_farther_forest, False),
    ("Ground_Root", "tile", tile_root, False),
    ("Ground_Moss", "tile", tile_moss, False),
    ("Ground_Flag", "tile", tile_flag, False),
    ("Ground_Lane", "tile", tile_lane, False),
]


def main():
    run_kit(OUT, "Verdance", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

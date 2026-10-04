"""Halden paper kit (ENV-05): every backdrop strip and ground tile the Plateau's rooms use, rendered from cut-out
geometry with Freestyle ink lines in headless Blender, on the plumbing all the kits share (kitlib.py).

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/halden.py
    ... -P tools/paperkit/halden.py -- Paper_Mid_Bridges Ground_Granite      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Halden/.
The bible (4.4): the Citadel on its plateau, stone and copper gone green, always late afternoon; seven bridges
over the drop, the seventh under repair for forty years; the mills with wet paper in the air; Lowmarket below the
walls with its paint thinner; the Hall as she left it; the only fallen leaves in Halden; flyer-towers with no
stairs; the brass dome. Cool cream paper, slate, verdigris, brass, blue-black ink (art-direction 5). Farther layers
wash toward the paper and draw thinner, as everywhere.
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Halden")

# Halden palette (art-direction 5): paper, wash 1, wash 2, accent, ink. The paper is ProjectSetup.RegionPaper's.
PAPER = (0.92, 0.92, 0.87)
SLATE = (0.36, 0.40, 0.46)
VERDIGRIS = (0.42, 0.62, 0.56)
BRASS = (0.76, 0.60, 0.30)
INK = (0.08, 0.10, 0.18)
STONE = lerp(SLATE, PAPER, 0.5)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


# ---------------------------------------------------------------- shapes

def rbox(name, cx, cz, w, h, material, angle, y=0.0):
    """A box turned about its own centre (a box built in place would turn about the world origin)."""
    ob = box(name, 0.0, 0.0, w, h, material, y=y)
    ob.rotation_euler = (0, angle, 0)
    ob.location = (cx, ob.location[1], cz)
    return ob


def arch_bridge(name, x0, x1, deck_z, depth, stone, dark, y=0.0, piers=True):
    """A bridge span: a deck, a round arch under it, a pier at each end going down out of the strip."""
    w = x1 - x0
    box(name + "_deck", (x0 + x1) / 2, deck_z, w, 0.5, stone, y=y)
    box(name + "_lip", (x0 + x1) / 2, deck_z + 0.3, w, 0.12, dark, y=y - 0.03)
    pts = [(x0, deck_z - 0.25), (x1, deck_z - 0.25), (x1, deck_z - depth)]
    n = 12
    for i in range(n + 1):
        a = math.pi * i / n
        pts.append(((x0 + x1) / 2 + (w / 2 - 0.4) * math.cos(a), deck_z - depth + (depth - 0.6) * math.sin(a)))
    pts.append((x0, deck_z - depth))
    polygon(name + "_arch", pts, stone, y=y + 0.05)
    if piers:
        for px in (x0, x1):
            box(name + "_pier%d" % int(px), px, deck_z - depth - 1.5, 1.2, 3.2, dark, y=y + 0.1)


def balusters(name, x0, x1, base_z, h, stone, brass, y=0.0, step=0.8):
    """A balustrade: posts along a rail, a brass finial on every fourth."""
    box(name + "_rail", (x0 + x1) / 2, base_z + h, x1 - x0, 0.12, stone, y=y - 0.02)
    k = 0
    x = x0
    while x <= x1:
        box("%s_post%d" % (name, k), x, base_z + h / 2, 0.14, h, stone, y=y)
        if k % 4 == 0:
            disc("%s_fin%d" % (name, k), x, base_z + h + 0.18, 0.1, brass, y=y - 0.04, n=10)
        x += step
        k += 1


def building(name, rng, x, base_z, w, h, walls, roof, dark, y=0.0, copper=None, windows=2):
    """A Citadel building: a stone block, a copper roof gone green, tall windows."""
    box(name, x, base_z + h / 2, w, h, walls, y=y)
    polygon(name + "_roof", [(x - w / 2 - 0.25, base_z + h), (x + w / 2 + 0.25, base_z + h), (x + w * 0.3, base_z + h + w * 0.35), (x - w * 0.3, base_z + h + w * 0.35)], copper or roof, y=y - 0.05)
    for i in range(windows):
        wx = x - w / 2 + w * (i + 1) / (windows + 1)
        polygon("%s_win%d" % (name, i), [(wx - w * 0.07, base_z + h * 0.25), (wx + w * 0.07, base_z + h * 0.25), (wx + w * 0.07, base_z + h * 0.65), (wx, base_z + h * 0.75), (wx - w * 0.07, base_z + h * 0.65)], dark, y=y - 0.08)


def dome(name, x, base_z, r, copper, brass, dark, y=0.0):
    """The Observatory's brass dome gone green: a half-round on a drum, ribs, a lantern on top."""
    pts = []
    n = 20
    for i in range(n + 1):
        a = math.pi * i / n
        pts.append((x + r * math.cos(a), base_z + r * math.sin(a)))
    polygon(name, pts, copper, y=y)
    for i in range(1, 6):
        a = math.pi * i / 6
        rbox("%s_rib%d" % (name, i), x + r * math.cos(a) * 0.5, base_z + r * math.sin(a) * 0.5, 0.08, r * 0.95, dark, a - math.pi / 2, y=y - 0.03)
    box(name + "_lantern", x, base_z + r + 0.35, 0.5, 0.7, brass, y=y - 0.04)
    disc(name + "_ball", x, base_z + r + 0.85, 0.14, brass, y=y - 0.05, n=10)


def mill_wheel(name, x, z, r, wood, dark, y=0.0, spokes=8):
    """A mill wheel: a ring, spokes, paddles round the rim."""
    disc(name + "_rim", x, z, r, wood, y=y)
    disc(name + "_in", x, z, r * 0.78, PAPER_MAT[0], y=y - 0.03)
    for i in range(spokes):
        a = 2 * math.pi * i / spokes
        rbox("%s_sp%d" % (name, i), x, z, 0.08, r * 1.9, dark, a, y=y - 0.06)
        rbox("%s_pad%d" % (name, i), x + r * math.cos(a + math.pi / spokes), z + r * math.sin(a + math.pi / spokes), 0.12, 0.5, dark, a + math.pi / spokes, y=y - 0.07)
    disc(name + "_hub", x, z, r * 0.12, dark, y=y - 0.09, n=10)


PAPER_MAT = [None]


def sheets(name, rng, x0, x1, z, n, sheet, cord, dark, y=0.0):
    """Vellum hung to dry: a line and rectangles pegged along it, every one the same."""
    box(name + "_line", (x0 + x1) / 2, z, x1 - x0, 0.03, cord, y=y)
    for i in range(n):
        x = x0 + (x1 - x0) * (i + 0.5) / n
        box("%s_s%d" % (name, i), x + rng.uniform(-0.05, 0.05), z - 0.75, 1.0, 1.4, sheet, y=y - 0.03)
        disc("%s_peg%d" % (name, i), x - 0.4, z, 0.04, dark, y=y - 0.06, n=6)
        disc("%s_peg2%d" % (name, i), x + 0.4, z, 0.04, dark, y=y - 0.06, n=6)
        disc("%s_print%d" % (name, i), x + 0.35, z - 1.3, 0.06, dark, y=y - 0.06, n=8)   # the same thumbprint in the same corner


def tree(name, rng, x, base_z, h, trunk, leaf, bare=0.5, y=0.0):
    """An orchard tree, old: a short trunk, a few branches, sparse leaves (the only ones that fall in Halden)."""
    polygon(name, [(x - 0.45, base_z), (x + 0.45, base_z), (x + 0.2, base_z + h * 0.55), (x - 0.2, base_z + h * 0.55)], trunk, y=y)
    for i in range(4):
        a = math.radians(40 + i * 35) + rng.uniform(-0.2, 0.2)
        L = h * rng.uniform(0.35, 0.5)
        rbox("%s_br%d" % (name, i), x + math.cos(a) * L / 2, base_z + h * 0.5 + math.sin(a) * L / 2, L, 0.09, trunk, -a, y=y - 0.02)
        if rng.random() > bare:
            blob("%s_lf%d" % (name, i), rng, x + math.cos(a) * L, base_z + h * 0.5 + math.sin(a) * L, 0.55, 0.35, leaf, y=y - 0.05, wobble=0.4)


def leaves(rng, n, x0, x1, z0, z1, mat, y=0.0):
    for i in range(n):
        ob = polygon("leaf_%d" % i, [(-0.08, 0), (0.08, 0), (0.1, 0.12), (0, 0.2), (-0.1, 0.12)], mat, y=y)
        ob.location = (rng.uniform(x0, x1), ob.location[1], rng.uniform(z0, z1))
        ob.rotation_euler = (0, rng.uniform(0, 6.28), 0)


def stall(name, x, base_z, w, h, cloth, wood, dark, y=0.0):
    """A market stall: a counter, two posts, a canvas awning, thin paint."""
    box(name + "_counter", x, base_z + 0.45, w, 0.9, wood, y=y)
    for dx in (-w / 2 + 0.1, w / 2 - 0.1):
        box(name + "_post%d" % int(dx * 10), x + dx, base_z + h / 2, 0.1, h, wood, y=y + 0.02)
    polygon(name + "_awn", [(x - w / 2 - 0.2, base_z + h), (x + w / 2 + 0.2, base_z + h), (x + w / 2, base_z + h + 0.5), (x - w / 2, base_z + h + 0.5)], cloth, y=y - 0.04)
    for i in range(3):
        box("%s_sc%d" % (name, i), x - w / 3 + i * w / 3, base_z + h - 0.03, w / 3 - 0.08, 0.1, dark, y=y - 0.05)


def tower_wall(name, rng, x0, x1, base_z, top_z, stone, dark, groove, y=0.0):
    """A flyer-tower's inside wall: stone courses, talon grooves worn in them, and no stairs."""
    polygon(name, [(x0, base_z), (x1, base_z), (x1, top_z), (x0, top_z)], stone, y=y)
    k = 0
    z = base_z + 0.5
    while z < top_z:
        box("%s_c%d" % (name, k), (x0 + x1) / 2, z, x1 - x0, 0.05, dark, y=y - 0.02)
        z += 0.9
        k += 1
    for i in range(int((x1 - x0) / 2.5)):
        gx = x0 + rng.uniform(0.5, x1 - x0 - 0.5)
        gz = rng.uniform(base_z + 0.5, top_z - 0.5)
        for j in range(3):
            box("%s_g%d_%d" % (name, i, j), gx + j * 0.12 - 0.12, gz, 0.04, rng.uniform(0.25, 0.5), groove, y=y - 0.04)


# ---------------------------------------------------------------- layers

def layer_fore_balustrade(rng, faded):
    """Paper_Fore_Balustrade: a stone balustrade in front of the walk, brass finials, dark, a thick line."""
    p = Palette(0.0, faded)
    stone, brass = p("stone", lerp(SLATE, INK, 0.35)), p("brass", lerp(BRASS, INK, 0.3))
    box("plinth", 0, -0.6, 80, 0.4, stone, y=-0.3)
    balusters("bal", -40, 40, -0.4, 0.95, stone, brass, y=-0.35)
    return STRIP_WIDTH, 1.6, PPU, -0.8, 3.0, p.ink()


def layer_mid_bridges(rng, faded):
    """Paper_Mid_Bridges: the Seven Bridges behind the walk: spans over the drop, piers going down, the seventh's
    scaffolding, the far parapet."""
    p = Palette(0.15, faded)
    stone, dark, scaf, brass = p("stone", STONE), p("dark", lerp(SLATE, INK, 0.3)), p("scaf", lerp(BRASS, SLATE, 0.5)), p("brass", BRASS)
    x = -41
    k = 0
    while x < 40:
        w = rng.uniform(8, 12)
        arch_bridge("span_%d" % k, x, x + w, 2.4, 2.2, stone, dark, y=0.3)
        if k == 3:
            for i in range(6):
                box("scaf_v%d" % i, x + 1 + i * (w - 2) / 5, 3.6, 0.1, 2.4, scaf, y=0.0)
            for z in (3.4, 4.6):
                box("scaf_h%d" % int(z * 10), x + w / 2, z, w - 2, 0.1, scaf, y=0.0)
        x += w
        k += 1
    balusters("par", -40, 40, 2.65, 0.8, stone, brass, y=0.2, step=1.0)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_drop(rng, faded):
    """Paper_Far_Drop: the drop under the bridges: cliff faces, the mills' roofs and chimneys far below, mist."""
    p = Palette(0.45, faded)
    cliff, dark, mist, roof = p("cliff", SLATE), p("dark", lerp(SLATE, INK, 0.3)), p("mist", PAPER, 0.1), p("roof", VERDIGRIS)
    for k in range(5):
        x = -36 + k * 18 + rng.uniform(-3, 3)
        polygon("cliff_%d" % k, [(x - 5, 2.0), (x + 5, 2.0), (x + 4 + rng.uniform(-1, 1), 7.5 + rng.uniform(-1, 1)), (x - 4 + rng.uniform(-1, 1), 8.0 + rng.uniform(-1, 1))], cliff, y=0.4)
        for i in range(5):
            box("crack_%d_%d" % (k, i), x + rng.uniform(-4, 4), rng.uniform(2.5, 7.0), 0.07, rng.uniform(0.5, 1.5), dark, y=0.35)
    for i in range(8):
        bx = rng.uniform(-38, 38)
        building("mill_%d" % i, rng, bx, 2.0, rng.uniform(2, 3.5), rng.uniform(1.0, 1.8), p("walls", STONE), roof, dark, y=0.5, windows=1)
        box("stack_%d" % i, bx + 1.0, 4.4, 0.3, 1.6, dark, y=0.45)
    for i in range(20):
        blob("mist_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(2.0, 5.0), rng.uniform(2, 5), rng.uniform(0.5, 1.2), mist, y=0.2, wobble=0.3)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_mid_mills(rng, faded):
    """Paper_Mid_Mills: the Paper Mills: mill houses on the race, wheels, vellum hung to dry on lines between them."""
    p = Palette(0.15, faded)
    walls, roof, dark, wood, sheet, cord, water = p("walls", STONE), p("roof", VERDIGRIS), p("dark", lerp(SLATE, INK, 0.3)), p("wood", lerp(SLATE, BRASS, 0.3)), p("sheet", PAPER, 0.1), p("cord", lerp(SLATE, INK, 0.1)), p("water", lerp(VERDIGRIS, PAPER, 0.5))
    PAPER_MAT[0] = p("wheel_in", lerp(STONE, PAPER, 0.3))
    polygon("race", [(-40, 0), (40, 0), (40, 0.9), (-40, 0.9)], water, y=0.4)
    for i in range(14):
        box("ripple_%d" % i, rng.uniform(-40, 40), rng.uniform(0.2, 0.8), rng.uniform(1, 3), 0.04, p("ripple", PAPER, 0.2), y=0.3)
    xs = [-34, -18, -3, 12, 28]
    for k, x in enumerate(xs):
        w = rng.uniform(6, 9)
        building("mill_%d" % k, rng, x, 0.9, w, rng.uniform(3.0, 4.2), walls, roof, dark, y=0.2, windows=3)
        mill_wheel("wheel_%d" % k, x + w / 2 + 0.2, 1.4, 1.3, wood, dark, y=0.0)
    for k in range(4):
        x0 = xs[k] + 4
        sheets("line_%d" % k, rng, x0, xs[k + 1] - 4, 4.6, 5, sheet, cord, dark, y=-0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_lowmarket(rng, faded):
    """Paper_Mid_Lowmarket: Lowmarket below the walls: stalls and low houses, the paint thinner, the wall above with
    its battlements, a notice board."""
    p = Palette(0.3, faded)   # the paint is thinner here: the mid layer washes like a far one
    walls, roof, dark, cloth, wood, wall = p("walls", STONE), p("roof", lerp(VERDIGRIS, PAPER, 0.3)), p("dark", lerp(SLATE, INK, 0.2)), p("cloth", lerp(BRASS, PAPER, 0.4)), p("wood", lerp(SLATE, BRASS, 0.3)), p("wall", lerp(SLATE, PAPER, 0.3))
    polygon("wall", [(-40, 3.6), (40, 3.6), (40, 6.2), (-40, 6.2)], wall, y=0.6)
    for i in range(40):
        box("merlon_%d" % i, -39 + i * 2.0, 6.0, 1.0, 0.5, wall, y=0.6)
    x = -39
    k = 0
    while x < 39:
        if k % 3 == 2:
            stall("stall_%d" % k, x + 1.5, 0.3, 3.0, 2.2, cloth, wood, dark, y=0.0)
            x += 4.2
        else:
            w = rng.uniform(3, 5)
            building("house_%d" % k, rng, x + w / 2, 0.3, w, rng.uniform(1.6, 2.6), walls, roof, dark, y=0.2, windows=2)
            x += w + rng.uniform(0.4, 1.2)
        k += 1
    box("board", 0.0, 2.0, 1.6, 1.2, wood, y=-0.05)
    for i in range(4):
        box("paste_%d" % i, rng.uniform(-0.5, 0.5), rng.uniform(1.6, 2.4), 0.5, 0.35, p("paste", PAPER, 0.05 * i), y=-0.1 - 0.01 * i)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_hall(rng, faded):
    """Paper_Mid_Hall: the Journeyman's Hall inside: panelled walls, pilasters, framed charts, the roll of names,
    brass lamps."""
    p = Palette(0.15, faded)
    panel, dark, frame, chart, brass, stone = p("panel", lerp(SLATE, BRASS, 0.35)), p("dark", lerp(SLATE, INK, 0.4)), p("frame", BRASS), p("chart", PAPER, 0.05), p("brass", BRASS), p("stone", STONE)
    polygon("wall", [(-40, 0), (40, 0), (40, 6.2), (-40, 6.2)], stone, y=0.5)
    box("dado", 0, 1.3, 80, 2.6, panel, y=0.4)
    box("rail", 0, 2.65, 80, 0.12, dark, y=0.35)
    for i in range(17):
        x = -40 + i * 5
        box("pil_%d" % i, x, 3.1, 0.6, 6.2, stone, y=0.3)
        box("pil_cap_%d" % i, x, 5.9, 0.9, 0.2, dark, y=0.28)
    for i in range(16):
        x = -37.5 + i * 5
        if i % 3 == 0:
            box("frame_%d" % i, x, 4.3, 2.6, 1.8, frame, y=0.2)
            box("chart_%d" % i, x, 4.3, 2.3, 1.5, chart, y=0.15)
            for j in range(3):
                box("line_%d_%d" % (i, j), x + rng.uniform(-0.6, 0.6), 4.0 + j * 0.3, rng.uniform(0.5, 1.4), 0.03, dark, y=0.1)
        elif i % 3 == 1:
            box("lamp_%d" % i, x, 4.0, 0.14, 0.7, brass, y=0.2)
            disc("glow_%d" % i, x, 4.5, 0.5, p("glow", PAPER, 0.0), y=0.25, n=16)
        else:
            box("roll_%d" % i, x, 4.4, 1.6, 2.2, p("roll", lerp(STONE, PAPER, 0.3)), y=0.2)
            for j in range(6):
                box("name_%d_%d" % (i, j), x, 5.2 - j * 0.3, rng.uniform(0.6, 1.1), 0.05, dark, y=0.15)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_orchard(rng, faded):
    """Paper_Mid_Orchard: the Old Orchard: the orchard wall, old trees with sparse leaves, leaves raked into one pile,
    the rake against the wall, the flyer-tower's foot at the east."""
    p = Palette(0.15, faded)
    wall, dark, trunk, leaf, fallen, tower = p("wall", STONE), p("dark", lerp(SLATE, INK, 0.3)), p("trunk", lerp(SLATE, INK, 0.1)), p("leaf", lerp(VERDIGRIS, PAPER, 0.2)), p("fallen", lerp(BRASS, PAPER, 0.2)), p("tower", lerp(STONE, SLATE, 0.3))
    polygon("wall", [(-40, 0), (40, 0), (40, 2.6), (-40, 2.6)], wall, y=0.5)
    for i in range(80):
        box("course_%d" % i, rng.uniform(-40, 40), rng.uniform(0.2, 2.4), rng.uniform(0.6, 1.4), 0.04, dark, y=0.45)
    box("coping", 0, 2.7, 80, 0.2, dark, y=0.45)
    for k in range(9):
        x = -36 + k * 9 + rng.uniform(-2, 2)
        tree("tree_%d" % k, rng, x, 0.0, rng.uniform(4.0, 5.5), trunk, leaf, bare=0.45, y=0.2 + 0.01 * k)
    leaves(rng, 60, -40, 40, 0.05, 0.5, fallen, y=-0.1)
    ridge("pile", rng, 6, 10, 0.0, 0.9, 0.2, 8, fallen, y=-0.15)
    rbox("rake", 11.2, 1.0, 0.06, 2.0, dark, 0.25, y=-0.2)
    box("rake_head", 11.6, 0.1, 0.7, 0.1, dark, y=-0.2)
    polygon("tower", [(30, 0), (40, 0), (40, 6.2), (31, 6.2)], tower, y=0.3)
    for i in range(6):
        box("tower_c%d" % i, 35.5, 0.5 + i * 1.0, 9, 0.05, dark, y=0.28)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_tower(rng, faded):
    """Paper_Mid_Tower: inside a flyer-tower: stone courses with talon grooves, the old landing doors high up with no
    stairs to them, a brass rail, a door cut crudely at the foot."""
    p = Palette(0.15, faded)
    stone, dark, groove, brass = p("stone", STONE), p("dark", lerp(SLATE, INK, 0.3)), p("groove", lerp(SLATE, INK, 0.1)), p("brass", BRASS)
    tower_wall("wall", rng, -40, 40, 0.0, 6.2, stone, dark, groove, y=0.4)
    for k in range(5):
        x = -32 + k * 16 + rng.uniform(-3, 3)
        z = rng.uniform(3.2, 4.6)
        polygon("door_%d" % k, [(x - 0.7, z), (x + 0.7, z), (x + 0.7, z + 1.6), (x, z + 2.1), (x - 0.7, z + 1.6)], dark, y=0.2)
        box("ledge_%d" % k, x, z - 0.1, 2.2, 0.2, stone, y=0.15)
        box("rail_%d" % k, x, z + 0.4, 2.2, 0.05, brass, y=0.1)
    polygon("cut", [(-0.9, 0), (0.9, 0), (1.0, 1.9), (-0.8, 2.1)], dark, y=0.2)
    box("plaque", 1.8, 1.4, 0.7, 0.45, brass, y=0.15)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_dome(rng, faded):
    """Paper_Mid_Dome: inside the Observatory: the dome's ribs rising out of the strip, the brass instruments, the
    frame of the Atlas with its seven sockets on the back wall, a stair."""
    p = Palette(0.15, faded)
    stone, dark, brass, rib, socket = p("stone", STONE), p("dark", lerp(SLATE, INK, 0.3)), p("brass", BRASS), p("rib", lerp(VERDIGRIS, SLATE, 0.4)), p("socket", lerp(INK, SLATE, 0.3))
    polygon("wall", [(-40, 0), (40, 0), (40, 6.2), (-40, 6.2)], stone, y=0.5)
    for k in range(9):
        x = -40 + k * 10
        polygon("rib_%d" % k, [(x - 0.4, 0), (x + 0.4, 0), (x + 0.25 + (x * 0.01), 6.2), (x - 0.25 + (x * 0.01), 6.2)], rib, y=0.3)
    box("ring", 0, 5.8, 80, 0.3, rib, y=0.28)
    # the frame: a brass ring with seven sockets, one full
    disc("frame", 0, 3.0, 2.6, brass, y=0.1, n=28)
    disc("frame_in", 0, 3.0, 2.2, stone, y=0.05, n=28)
    for i in range(7):
        a = math.pi / 2 + 2 * math.pi * i / 7
        disc("socket_%d" % i, 1.75 * math.cos(a), 3.0 + 1.75 * math.sin(a), 0.32, socket, y=0.0, n=14)
        if i == 0:
            disc("stone_%d" % i, 1.75 * math.cos(a), 3.0 + 1.75 * math.sin(a), 0.24, p("keystone", lerp(BRASS, PAPER, 0.3)), y=-0.05, n=12)
    for k, x in enumerate((-24, 24)):
        box("stand_%d" % k, x, 1.0, 0.2, 2.0, brass, y=0.1)
        disc("lens_%d" % k, x, 2.4, 0.5, p("lens", PAPER, 0.1), y=0.05, n=16)
        rbox("tube_%d" % k, x + 0.5, 2.6, 1.8, 0.3, brass, -0.5, y=0.08)
    for i in range(10):
        box("step_%d" % i, -36 + i * 0.7, 0.2 + i * 0.3, 0.7, 0.3, stone, y=0.2)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_citadel(rng, faded):
    """Paper_Far_Citadel: the city behind: roofs of copper gone green, the walls, the Hall's front, the dome, towers."""
    p = Palette(0.45, faded)
    walls, roof, dark, wall, brass = p("walls", STONE), p("roof", VERDIGRIS), p("dark", lerp(SLATE, INK, 0.3)), p("wall", lerp(SLATE, PAPER, 0.3)), p("brass", BRASS)
    polygon("wall", [(-40, 2), (40, 2), (40, 4.2), (-40, 4.2)], wall, y=0.6)
    for i in range(40):
        box("merlon_%d" % i, -39 + i * 2.0, 4.4, 1.0, 0.4, wall, y=0.6)
    x = -40
    k = 0
    while x < 40:
        w = rng.uniform(3, 7)
        building("b_%d" % k, rng, x + w / 2, 4.2, w, rng.uniform(2, 5), walls, roof, dark, y=0.3 + 0.01 * (k % 3), windows=2)
        x += w + rng.uniform(0.2, 1.0)
        k += 1
    for k, x in enumerate((-30, 22)):
        polygon("tower_%d" % k, [(x - 1.4, 4.2), (x + 1.4, 4.2), (x + 1.1, 11.4), (x - 1.1, 11.4)], walls, y=0.2)
        box("tower_top_%d" % k, x, 11.5, 2.8, 0.3, dark, y=0.18)
    dome("dome", 4, 8.6, 3.2, roof, brass, dark, y=0.1)
    box("drum", 4, 7.4, 6.8, 2.4, walls, y=0.25)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_farther_sky(rng, faded):
    """Paper_Farther_Sky: always late afternoon: a long light low across the plateau, the far towers nearly paper,
    the dome a shape in it."""
    p = Palette(0.6, faded)
    far, light, cloud = p("far", SLATE, 0.1), p("light", lerp(BRASS, PAPER, 0.75)), p("cloud", PAPER, 0.05)
    polygon("light", [(-40, 6), (40, 6), (40, 11.5), (-40, 13.5)], light, y=0.7)
    for k in range(7):
        x = -36 + k * 12 + rng.uniform(-2, 2)
        polygon("far_%d" % k, [(x - 1.2, 6), (x + 1.2, 6), (x + 0.9, 6 + rng.uniform(5, 10)), (x - 0.9, 6 + rng.uniform(5, 10))], far, y=0.5)
    for i in range(8):
        blob("cloud_%d" % i, rng, rng.uniform(-40, 40), rng.uniform(14, 21), rng.uniform(3, 7), rng.uniform(0.5, 1.0), cloud, y=0.3, wobble=0.3)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.3, p.ink()


def tile_granite(rng, faded):
    """Ground_Granite: the bridges' and the tower's stone: long slabs, close joints, a copper strip gone green."""
    p = Palette(0.0, faded)
    stone, joint, copper = p("stone", STONE), p("joint", lerp(SLATE, INK, 0.3)), p("copper", VERDIGRIS)
    box("bed", 0, 0.5, 4.0, 1.0, stone, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.9, 1.6)
        box("slab_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.5, w - 0.04, 0.92, stone, y=0.0)
        x += w
        i += 1
    box("strip", 0, 0.9, 4.0, 0.06, copper, y=-0.05)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_boards(rng, faded):
    """Ground_Boards: the mills' floor: pale boards, wet along the top, a nail here and there."""
    p = Palette(0.0, faded)
    board, gap, wet, nail = p("board", lerp(STONE, BRASS, 0.3)), p("gap", lerp(SLATE, INK, 0.4)), p("wet", lerp(VERDIGRIS, PAPER, 0.6)), p("nail", lerp(SLATE, INK, 0.2))
    box("beam", 0, 0.2, 4.0, 0.4, gap, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.4, 0.7)
        box("board_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.66, w - 0.05, 0.66, board, y=0.0)
        if rng.random() < 0.4:
            disc("nail_%d" % i, x + w / 2, 0.86, 0.03, nail, y=-0.05, n=6)
        x += w
        i += 1
    for i in range(2):
        blob("wet_%d" % i, rng, rng.uniform(-1.6, 1.6), 0.9, rng.uniform(0.3, 0.6), 0.08, wet, y=-0.04, wobble=0.3)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_parquet(rng, faded):
    """Ground_Parquet: the Hall's floor: herringbone blocks, waxed, a brass inlay line."""
    p = Palette(0.0, faded)
    wood, wood2, dark, brass = p("wood", lerp(SLATE, BRASS, 0.45)), p("wood2", lerp(SLATE, BRASS, 0.3)), p("dark", lerp(SLATE, INK, 0.4)), p("brass", BRASS)
    box("bed", 0, 0.5, 4.0, 1.0, wood2, y=0.2)
    k = 0
    for r in range(3):
        for c in range(9):
            x = -2.0 + c * 0.5 + (0.25 if r % 2 else 0)
            z = 0.2 + r * 0.3
            ob = box("blk_%d" % k, 0, 0, 0.5, 0.16, wood if (r + c) % 2 else wood2, y=0.0)
            ob.rotation_euler = (0, 0.6 if (r + c) % 2 else -0.6, 0)
            ob.location = (max(-1.85, min(1.85, x)), ob.location[1], z)
            k += 1
    box("inlay", 0, 0.93, 4.0, 0.04, brass, y=-0.05)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_cobble(rng, faded):
    """Ground_Cobble: Lowmarket's and the orchard's cobbles: rounds in rows, the paint thinner, a leaf."""
    p = Palette(0.0, faded)
    bed, cob, cob2, leaf = p("bed", lerp(SLATE, PAPER, 0.3)), p("cob", STONE), p("cob2", lerp(STONE, PAPER, 0.3)), p("leaf", lerp(BRASS, PAPER, 0.2))
    box("bed", 0, 0.5, 4.0, 1.0, bed, y=0.2)
    k = 0
    for r in range(3):
        for c in range(10):
            x = -1.8 + c * 0.4 + (0.2 if r % 2 else 0)
            z = 0.2 + r * 0.32
            disc("cob_%d" % k, max(-1.9, min(1.9, x)), z, rng.uniform(0.14, 0.19), cob if (r + c) % 3 else cob2, y=0.0, n=10)
            k += 1
    ob = polygon("leaf", [(-0.08, 0), (0.08, 0), (0.1, 0.12), (0, 0.2), (-0.1, 0.12)], leaf, y=-0.05)
    ob.location = (rng.uniform(-1.5, 1.5), ob.location[1], 0.8)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Fore_Balustrade", "strip", layer_fore_balustrade, False),
    ("Paper_Mid_Bridges", "strip", layer_mid_bridges, False),
    ("Paper_Mid_Mills", "strip", layer_mid_mills, False),
    ("Paper_Mid_Lowmarket", "strip", layer_mid_lowmarket, False),
    ("Paper_Mid_Hall", "strip", layer_mid_hall, False),
    ("Paper_Mid_Orchard", "strip", layer_mid_orchard, False),
    ("Paper_Mid_Tower", "strip", layer_mid_tower, False),
    ("Paper_Mid_Dome", "strip", layer_mid_dome, False),
    ("Paper_Far_Citadel", "strip", layer_far_citadel, False),
    ("Paper_Far_Drop", "strip", layer_far_drop, False),
    ("Paper_Farther_Sky", "strip", layer_farther_sky, False),
    ("Ground_Granite", "tile", tile_granite, False),
    ("Ground_Boards", "tile", tile_boards, False),
    ("Ground_Parquet", "tile", tile_parquet, False),
    ("Ground_Cobble", "tile", tile_cobble, False),
]


def main():
    run_kit(OUT, "Halden", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

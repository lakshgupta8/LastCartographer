"""Greyfold paper kit (ENV-08): every backdrop strip and ground tile the Greyfold's rooms use, rendered from cut-out
geometry with Freestyle ink lines in headless Blender, on the plumbing all the kits share (kitlib.py).

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/greyfold.py
    ... -P tools/paperkit/greyfold.py -- Paper_Mid_Nave Ground_Chalk      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Greyfold/.
The bible (4.6): the threshold, half a cathedral, a road that stops; white paper, no wash, outlines only at the edge
of the eye, Wren's blue and lantern gold the only colours, ghost-grey ink (art-direction 5). Every strip is drawn
nearly in paper: the ink line carries it, and the east of most strips is eaten by the white (`white_eat`), since the
Greyfold runs out eastward. Farther layers wash toward the paper and draw thinner, as everywhere. Layer names are
unique across the kits (a material is named after its layer).
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Greyfold")

# Greyfold palette (art-direction 5): white paper, no wash, Wren's blue and lantern gold, ghost-grey ink.
# The paper is ProjectSetup.RegionPaper's; the greys are the ink thinned, never a colour of their own.
PAPER = (0.98, 0.98, 0.97)
INK = (0.70, 0.70, 0.68)
GREY = (0.80, 0.80, 0.78)
PALE = (0.90, 0.90, 0.88)
DARK = (0.58, 0.58, 0.56)
BLUE = (0.24, 0.32, 0.50)
GOLD = (0.90, 0.76, 0.40)
WOOD = (0.68, 0.64, 0.58)
ROPE = (0.76, 0.72, 0.64)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


PAPER_MAT = [None]   # the paper's wash for this layer, for the shapes that cut back to it


# ---------------------------------------------------------------- shapes

def rbox(name, cx, cz, w, h, material, angle, y=0.0):
    """A box turned about its own centre (a box built in place would turn about the world origin)."""
    ob = box(name, 0.0, 0.0, w, h, material, y=y)
    ob.rotation_euler = (0, angle, 0)
    ob.location = (cx, ob.location[1], cz)
    return ob


def stroke(name, x0, z0, x1, z1, thickness, material, y=0.0):
    """A pen stroke between two points, as a thin turned box."""
    dx, dz = x1 - x0, z1 - z0
    return rbox(name, (x0 + x1) / 2, (z0 + z1) / 2, math.hypot(dx, dz), thickness, material, -math.atan2(dz, dx), y=y)


def white_eat(rng, name, x0, x1, z0, z1, y=-0.5, step=0.08):
    """The white eating a strip from the east: one sheet of paper laid over everything, its west edge wandering
    (further west the higher it is), so what stands there shows only at the edge of the eye and one ghost line marks
    where the white begins. One shape, not bands: every edge Freestyle finds becomes a line."""
    pts = [(x1 + 2, z0 - 1), (x1 + 2, z1 + 1)]
    z = z1 + 1
    k = 0
    while z > z0 - 1:
        reach = (x1 - x0) * (0.55 + 0.45 * math.sin(z * 1.3 + k * 0.1)) * rng.uniform(0.85, 1.0)
        pts.append((x1 - reach, z))
        z -= step
        k += 1
    return polygon(name, pts, PAPER_MAT[0], y=y)


def outline_grass(rng, name, x0, x1, base_z, h, n, material, y=0.0):
    """Grass that has gone to outline: single thin blades, no clump, nearly paper."""
    for i in range(n):
        x = x0 + (x1 - x0) * (i + rng.uniform(0.2, 0.8)) / n
        bh = h * rng.uniform(0.5, 1.0)
        polygon("%s_%d" % (name, i), [(x - 0.03, base_z), (x + 0.03, base_z), (x + rng.uniform(-0.1, 0.15), base_z + bh)], material, y=y - 0.001 * (i % 7))


def tether_post(name, x, base_z, h, wood, rope, dark, y=0.0, rope_to=None):
    """A Guild tether-post: a squared post with an iron ring, the rope running up and away into the white."""
    box(name, x, base_z + h / 2, 0.22, h, wood, y=y)
    box(name + "_cap", x, base_z + h, 0.3, 0.08, dark, y=y - 0.02)
    disc(name + "_ring", x + 0.14, base_z + h * 0.8, 0.07, dark, y=y - 0.03, n=10)
    if rope_to is not None:
        stroke(name + "_rope", x + 0.14, base_z + h * 0.8, rope_to[0], rope_to[1], 0.03, rope, y=y - 0.01)


def stake(name, x, base_z, h, material, rope, y=0.0, rope_to=None):
    """A survey stake driven into the white, a tether tied off on it and run taut."""
    polygon(name, [(x - 0.08, base_z), (x + 0.08, base_z), (x + 0.05, base_z + h), (x - 0.05, base_z + h)], material, y=y)
    if rope_to is not None:
        stroke(name + "_rope", x, base_z + h * 0.85, rope_to[0], rope_to[1], 0.025, rope, y=y - 0.01)


def tent(name, x, base_z, w, h, cloth, dark, y=0.0, open_side=1):
    """A Guild tent: a ridge-pole, the cloth down either side, one side pegged open."""
    polygon(name, [(x - w / 2, base_z), (x + w / 2, base_z), (x, base_z + h)], cloth, y=y)
    stroke(name + "_pole", x, base_z, x, base_z + h, 0.04, dark, y=y - 0.02)
    polygon(name + "_door", [(x + open_side * w * 0.05, base_z), (x + open_side * w * 0.42, base_z), (x + open_side * w * 0.1, base_z + h * 0.55)], PAPER_MAT[0], y=y - 0.03)
    for k in range(3):
        stroke("%s_peg%d" % (name, k), x - w / 2 + k * w / 2, base_z, x - w / 2 + k * w / 2 + 0.1, base_z - 0.12, 0.03, dark, y=y - 0.02)


def column(name, x, base_z, w, h, material, dark, y=0.0):
    """A nave column: a shaft with a base and a capital."""
    box(name, x, base_z + h / 2, w, h, material, y=y)
    box(name + "_base", x, base_z + 0.12, w * 1.5, 0.24, dark, y=y - 0.01)
    box(name + "_cap", x, base_z + h - 0.12, w * 1.5, 0.24, dark, y=y - 0.01)
    for k in range(3):
        stroke("%s_f%d" % (name, k), x - w * 0.3 + k * w * 0.3, base_z + 0.3, x - w * 0.3 + k * w * 0.3, base_z + h - 0.3, 0.015, dark, y=y - 0.02)


def arch(name, x, z, w, h, material, y=0.0, n=12):
    """A pointed arch's outline as a thin band: two curves meeting at the top."""
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((x - w / 2 + w * t, z + h * math.sin(t * math.pi) ** 0.7))
    for i in range(n, -1, -1):
        t = i / n
        pts.append((x - w / 2 + w * t, z + (h - 0.14) * math.sin(t * math.pi) ** 0.7 - 0.02))
    return polygon(name, pts, material, y=y)


def bell(name, x, z, r, material, dark, y=0.0):
    """A bell hung in the nave: the body, the lip, the clapper, the rope coming down."""
    polygon(name, [(x - r * 0.3, z + r * 1.6), (x + r * 0.3, z + r * 1.6), (x + r * 0.5, z + r * 0.7), (x + r, z), (x - r, z), (x - r * 0.5, z + r * 0.7)], material, y=y)
    box(name + "_lip", x, z - 0.03, r * 2.05, 0.06, dark, y=y - 0.02)
    disc(name + "_cl", x, z - 0.12, r * 0.14, dark, y=y - 0.03, n=8)


def milepost(name, x, base_z, h, material, dark, y=0.0, marks=2):
    """A milepost: a squared stone with a rounded top, the count cut in it (or nothing)."""
    polygon(name, [(x - 0.3, base_z), (x + 0.3, base_z), (x + 0.3, base_z + h * 0.8), (x, base_z + h), (x - 0.3, base_z + h * 0.8)], material, y=y)
    for k in range(marks):
        stroke("%s_m%d" % (name, k), x - 0.12, base_z + h * 0.4 - k * 0.18, x + 0.12, base_z + h * 0.4 - k * 0.18, 0.03, dark, y=y - 0.02)


def cobbles(rng, name, x0, x1, z, n, material, dark, y=0.0):
    """A line of cobbles, each a rounded stone, drawn fainter the further east they go."""
    for i in range(n):
        t = i / max(1, n - 1)
        x = x0 + (x1 - x0) * t + rng.uniform(-0.05, 0.05)
        w = rng.uniform(0.35, 0.6)
        blob("%s_%d" % (name, i), rng, x, z + rng.uniform(-0.03, 0.03), w / 2, 0.14, material, y=y - 0.001 * i, n=10, wobble=0.15)


def footprints(name, x0, x1, z, n, material, y=0.0):
    """A line of footprints: three toes each, left and right, stopping mid-stride."""
    for i in range(n):
        x = x0 + (x1 - x0) * i / max(1, n - 1)
        zz = z + (0.12 if i % 2 else 0.0)
        for k in range(3):
            stroke("%s_%d_%d" % (name, i, k), x, zz, x + 0.18, zz + (k - 1) * 0.1, 0.03, material, y=y)


def lamp(name, x, base_z, h, iron, gold, glow, y=0.0):
    """A lamp on an iron stand, lit: the one colour in the room."""
    stroke(name + "_stand", x, base_z, x, base_z + h, 0.05, iron, y=y)
    box(name + "_foot", x, base_z + 0.04, 0.5, 0.08, iron, y=y - 0.01)
    box(name + "_box", x, base_z + h + 0.25, 0.34, 0.5, iron, y=y - 0.02)
    box(name + "_flame", x, base_z + h + 0.25, 0.22, 0.34, gold, y=y - 0.04)
    disc(name + "_glow", x, base_z + h + 0.25, 0.8, glow, y=y + 0.3, n=20)


def chick(name, x, base_z, material, dark, y=0.0):
    """A grey chick, small, round, at the edge of the eye."""
    disc(name, x, base_z + 0.28, 0.26, material, y=y, n=14)
    disc(name + "_head", x + 0.12, base_z + 0.56, 0.17, material, y=y - 0.01, n=12)
    disc(name + "_eye", x + 0.18, base_z + 0.6, 0.02, dark, y=y - 0.03, n=6)
    stroke(name + "_beak", x + 0.28, base_z + 0.56, x + 0.38, base_z + 0.54, 0.03, dark, y=y - 0.02)


def reflection(rng, name, x0, x1, z0, z1, water, line, y=0.0):
    """Water lying flat: a pale sheet with ripple lines."""
    polygon(name, [(x0, z0), (x1, z0), (x1, z1), (x0, z1)], water, y=y)
    for i in range(int((x1 - x0) / 1.4)):
        x = rng.uniform(x0, x1 - 1.0)
        z = rng.uniform(z0 + 0.1, z1 - 0.1)
        stroke("%s_r%d" % (name, i), x, z, x + rng.uniform(0.5, 1.2), z + rng.uniform(-0.02, 0.02), 0.015, line, y=y - 0.02)


def far_city(rng, name, x0, x1, base_z, mat, n, y=0.0):
    """Buildings that show only at the edge of the eye: roof-lines in near-paper, no walls."""
    for i in range(n):
        x = x0 + (x1 - x0) * (i + rng.uniform(0.1, 0.9)) / n
        w, h = rng.uniform(1.5, 4.0), rng.uniform(1.2, 4.5)
        kind = rng.random()
        if kind < 0.5:
            polygon("%s_%d" % (name, i), [(x - w / 2, base_z), (x + w / 2, base_z), (x + w / 2, base_z + h * 0.7), (x, base_z + h), (x - w / 2, base_z + h * 0.7)], mat, y=y - 0.001 * i)
        else:
            polygon("%s_%d" % (name, i), [(x - w / 2, base_z), (x + w / 2, base_z), (x + w / 2, base_z + h), (x - w / 2, base_z + h)], mat, y=y - 0.001 * i)
            stroke("%s_%dt" % (name, i), x - w * 0.2, base_z + h, x - w * 0.2, base_z + h + rng.uniform(0.6, 1.4), 0.08, mat, y=y - 0.002 * i)


# ---------------------------------------------------------------- layers

def layer_mid_edge(rng, faded):
    """Paper_Mid_Edge: the Edge's plain (the prologue's room): a chalk flat, the Guild's last tether-posts with their
    ropes running east into nothing, grass gone to outline, and the white eating everything past the middle."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, grey, dark, wood, rope = p("flat", PALE), p("grey", GREY), p("dark", DARK), p("wood", WOOD), p("rope", ROPE)
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.12, 40, flat, y=0.5)
    for k, x in enumerate((-34, -22, -8, 6)):
        tether_post("post_%d" % k, x, 0.4, 2.2, wood, rope, dark, y=0.2 + 0.01 * k, rope_to=(x + 16, 4.8))
    outline_grass(rng, "g", -41, 41, 0.3, 0.7, 60, grey, y=0.1)
    for k in range(3):
        stroke("chalk_%d" % k, -40, 0.9 + k * 0.1, 40, 1.05 + k * 0.1, 0.02, grey, y=0.3)
    white_eat(rng, "eat", 10, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_far_cathedral(rng, faded):
    """Paper_Far_Cathedral: half a cathedral far off: the west half standing (a nave's roof, a broken tower, the rose
    window's ring), the east half already the white."""
    p = Palette(0.45, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    wall, dark, roof = p("wall", PALE), p("dark", GREY), p("roof", lerp(GREY, PAPER, 0.4))
    polygon("nave", [(-22, 2), (6, 2), (6, 8.5), (-22, 8.5)], wall, y=0.5)
    polygon("roof", [(-23, 8.4), (7, 8.4), (-8, 12.2)], roof, y=0.45)
    polygon("tower", [(-20, 2), (-15, 2), (-15, 12.5), (-16.5, 13.4), (-18.2, 12.0), (-20, 12.8)], wall, y=0.4)
    for k in range(6):
        arch("win_%d" % k, -18 + k * 4.2, 3.0, 1.4, 3.2, dark, y=0.3)
    disc("rose", -1.5, 7.0, 1.4, dark, y=0.3, n=20)
    disc("rose_in", -1.5, 7.0, 1.2, wall, y=0.28, n=20)
    for k in range(8):
        stroke("spoke_%d" % k, -1.5, 7.0, -1.5 + 1.2 * math.cos(k * math.pi / 4), 7.0 + 1.2 * math.sin(k * math.pi / 4), 0.03, dark, y=0.26)
    for k in range(5):
        stroke("butt_%d" % k, -21 + k * 5.5, 2, -21.5 + k * 5.5, 8.0, 0.25, dark, y=0.35)
    far_city(rng, "city", 8, 40, 2.0, p("faint", PAPER, 0.02), 10, y=0.6)
    white_eat(rng, "eat", -2, 40, 2, 13, y=-0.5)
    return STRIP_WIDTH, 11.0, PPU, 2.0, 1.1, p.ink()


def layer_farther_edge(rng, faded):
    """Paper_Farther_Edge: the white itself with buildings at the edge of the eye: a horizon that is nearly nothing,
    roof-lines in near-paper, a flight of nothing."""
    p = Palette(0.6, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    faint = p("faint", GREY, 0.08)
    polygon("sky", [(-40, 6), (40, 6), (40, 22), (-40, 22)], PAPER_MAT[0], y=0.9)
    stroke("horizon", -40, 7.2, 40, 7.0, 0.03, faint, y=0.7)
    far_city(rng, "city", -40, 40, 7.0, p("roof", PAPER, 0.03), 18, y=0.6)
    white_eat(rng, "eat", -10, 40, 6, 22, y=-0.5, step=0.1)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.0, p.ink()


def layer_mid_fence(rng, faded):
    """Paper_Mid_Fence: the orchard road's end: a Guild fence with no gate, posts and two rails, a notice nailed to
    it, the road's last stones, and the white beyond."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, wood, dark, grey, sheet = p("flat", PALE), p("wood", WOOD), p("dark", DARK), p("grey", GREY), p("sheet", PAPER)
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.12, 40, flat, y=0.5)
    cobbles(rng, "road", -40, 2, 0.35, 70, grey, dark, y=0.3)
    for k in range(9):
        x = -16 + k * 4.5
        box("post_%d" % k, x, 1.5, 0.22, 2.2, wood, y=0.2)
    box("rail_a", 2, 1.6, 36, 0.12, wood, y=0.25)
    box("rail_b", 2, 2.3, 36, 0.12, wood, y=0.25)
    box("notice", -11.5, 1.95, 0.9, 0.7, sheet, y=0.1)
    for k in range(3):
        stroke("line_%d" % k, -11.85, 2.15 - k * 0.14, -11.85 + rng.uniform(0.4, 0.7), 2.15 - k * 0.14, 0.02, dark, y=0.05)
    outline_grass(rng, "g", -41, 41, 0.3, 0.6, 50, grey, y=0.1)
    white_eat(rng, "eat", 6, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_outpost(rng, faded):
    """Paper_Mid_Outpost: the Edge Camp: Guild tents gone grey, tether-posts with their ropes coiled and never used,
    a ledger board nobody posts to, a beam with initials, the white a stride past the last tent."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, cloth, dark, wood, rope, grey = p("flat", PALE), p("cloth", lerp(GREY, PAPER, 0.5)), p("dark", DARK), p("wood", WOOD), p("rope", ROPE), p("grey", GREY)
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.12, 40, flat, y=0.5)
    for k, x in enumerate((-30, -19, 3)):
        tent("tent_%d" % k, x, 0.45, rng.uniform(4.5, 6.0), rng.uniform(2.6, 3.2), cloth, dark, y=0.2 + 0.01 * k, open_side=1 if k % 2 else -1)
    for k, x in enumerate((-10, -6, 10, 14)):
        tether_post("post_%d" % k, x, 0.4, 2.0, wood, rope, dark, y=0.15 + 0.01 * k)
        for r in range(3):
            disc("coil_%d_%d" % (k, r), x, 0.9 + r * 0.14, 0.26, rope, y=0.12, n=12)
            disc("coilin_%d_%d" % (k, r), x, 0.9 + r * 0.14, 0.2, PAPER_MAT[0], y=0.11, n=12)
    box("board", -2, 1.9, 1.6, 1.1, dark, y=0.2)
    box("board_post", -2, 0.9, 0.14, 1.2, wood, y=0.22)
    box("beam", 6.5, 1.3, 0.3, 2.8, wood, y=0.2)
    stroke("init_i", 6.42, 1.9, 6.42, 2.2, 0.03, dark, y=0.1)
    stroke("init_m", 6.5, 1.9, 6.6, 2.2, 0.03, dark, y=0.1)
    outline_grass(rng, "g", -41, 41, 0.3, 0.6, 50, grey, y=0.1)
    white_eat(rng, "eat", 16, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_nave(rng, faded):
    """Paper_Mid_Nave: inside the half-cathedral: columns and pointed arches down the nave, the bells hung high, the
    road's cobbles running down the middle, the east side fading to outline and then to nothing."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    wall, dark, grey, bronze = p("wall", PALE), p("dark", DARK), p("grey", GREY), p("bronze", lerp(GOLD, GREY, 0.6))
    polygon("wall", [(-40, 0), (40, 0), (40, 12), (-40, 12)], wall, y=0.6)
    for k in range(13):
        x = -36 + k * 6
        column("col_%d" % k, x, 0.0, 0.9, 8.5, grey, dark, y=0.3 + 0.005 * k)
    for k in range(12):
        x = -33 + k * 6
        arch("arch_%d" % k, x, 8.3, 6.0, 3.2, dark, y=0.28)
    for k, x in enumerate((-30, -12, 6, 24)):
        bell("bell_%d" % k, x, 9.2, 0.9, bronze, dark, y=0.2)
        stroke("rope_%d" % k, x, 9.0, x, 1.0, 0.03, dark, y=0.19)
    cobbles(rng, "road", -40, 40, 0.3, 120, grey, dark, y=0.25)
    chick("chick", 1.0, 0.0, grey, dark, y=0.15)
    white_eat(rng, "eat", 12, 40, 0, 12, y=-0.5)
    return STRIP_WIDTH, 12.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_road(rng, faded):
    """Paper_Mid_Road: the Road That Stops: cobbles that fade a stride at a time, mileposts counting down to the
    capital (the last blank), outlines of a hedge, and the white where it stops."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, grey, dark, stone = p("flat", PALE), p("grey", GREY), p("dark", DARK), p("stone", lerp(GREY, PAPER, 0.3))
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.1, 40, flat, y=0.5)
    cobbles(rng, "road", -40, 30, 0.35, 110, grey, dark, y=0.3)
    cobbles(rng, "road2", -40, 24, 0.7, 90, stone, dark, y=0.32)
    for k, x in enumerate((-30, -10, 10, 28)):
        milepost("post_%d" % k, x, 0.4, 1.8, stone, dark, y=0.2 + 0.01 * k, marks=max(0, 3 - k))
    for i in range(30):
        x = rng.uniform(-40, 40)
        blob("hedge_%d" % i, rng, x, 1.4, rng.uniform(0.8, 1.6), rng.uniform(0.3, 0.6), p("hedge", PAPER, 0.02), y=0.4, n=10, wobble=0.3)
    outline_grass(rng, "g", -41, 41, 0.3, 0.5, 40, grey, y=0.1)
    white_eat(rng, "eat", 4, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_shore(rng, faded):
    """Paper_Mid_Shore: the white shore: a beach of paper, the pool's near edge lying flat, reeds gone to outline,
    a boat that was never drawn finished."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    sand, grey, dark, water = p("sand", PAPER, 0.02), p("grey", GREY), p("dark", DARK), p("water", lerp(BLUE, PAPER, 0.88))
    ridge("sand", rng, -40, 40, 0.0, 0.6, 0.1, 40, sand, y=0.5)
    reflection(rng, "pool", -6, 40, 0.3, 1.5, water, grey, y=0.4)
    for i in range(24):
        x = rng.uniform(-40, -8)
        stroke("reed_%d" % i, x, 0.5, x + rng.uniform(-0.2, 0.2), 0.5 + rng.uniform(1.0, 2.0), 0.03, grey, y=0.2)
    polygon("boat", [(-26, 0.5), (-21, 0.5), (-21.6, 1.3), (-25.4, 1.3)], p("hull", PALE), y=0.15)
    stroke("boat_gone", -21, 0.5, -17, 0.9, 0.03, grey, y=0.14)
    outline_grass(rng, "g", -41, -6, 0.4, 0.5, 30, grey, y=0.1)
    white_eat(rng, "eat", 10, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_pool(rng, faded):
    """Paper_Mid_Pool: the Mirror Pool: water that shows what is not on the bank: the bank empty, and in the
    reflection a grey chick, upside down."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    sand, grey, dark, water = p("sand", PAPER, 0.02), p("grey", GREY), p("dark", DARK), p("water", lerp(BLUE, PAPER, 0.85))
    ridge("sand", rng, -40, 40, 0.0, 0.5, 0.1, 40, sand, y=0.5)
    reflection(rng, "pool", -40, 40, 0.0, 2.2, water, grey, y=0.4)
    # The chick's reflection: drawn upside down under the bank, and nothing above it.
    disc("ref_body", 4.0, 1.3, 0.3, grey, y=0.3, n=14)
    disc("ref_head", 4.14, 0.98, 0.19, grey, y=0.29, n=12)
    disc("ref_eye", 4.2, 0.94, 0.025, dark, y=0.27, n=6)
    stroke("ref_beak", 4.32, 0.98, 4.44, 1.0, 0.03, dark, y=0.28)
    for i in range(12):
        x = rng.uniform(-40, 40)
        stroke("reed_%d" % i, x, 2.2, x + rng.uniform(-0.2, 0.2), 2.2 + rng.uniform(0.8, 1.6), 0.03, grey, y=0.2)
    white_eat(rng, "eat", 14, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_line(rng, faded):
    """Paper_Mid_Line: the Threshold: the Guild's stakes in a line across the white, every tether run taut into it,
    a field desk, the Guild's flag in no wind, one old Ferrymen's tether among the new."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, grey, dark, wood, rope, old, blue = p("flat", PALE), p("grey", GREY), p("dark", DARK), p("wood", WOOD), p("rope", ROPE), p("old", lerp(ROPE, DARK, 0.5)), p("blue", lerp(BLUE, PAPER, 0.5))
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.1, 40, flat, y=0.5)
    for k in range(14):
        x = -34 + k * 5.2 + rng.uniform(-0.6, 0.6)
        stake("stake_%d" % k, x, 0.4, rng.uniform(1.3, 1.8), wood, old if k == 6 else rope, y=0.2 + 0.005 * k, rope_to=(x + 18, 3.6 + rng.uniform(-0.4, 0.4)))
    box("desk_top", -24, 1.35, 2.2, 0.12, wood, y=0.15)
    for dx in (-0.9, 0.9):
        stroke("desk_leg%s" % dx, -24 + dx, 0.4, -24 + dx * 0.8, 1.3, 0.06, wood, y=0.16)
    box("sheet", -24.2, 1.45, 1.1, 0.08, PAPER_MAT[0], y=0.1)
    stroke("flagpole", -29, 0.4, -29, 4.6, 0.05, dark, y=0.15)
    polygon("flag", [(-29, 4.6), (-27.4, 4.3), (-29, 3.9)], blue, y=0.12)
    outline_grass(rng, "g", -41, 41, 0.3, 0.5, 40, grey, y=0.1)
    white_eat(rng, "eat", 2, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_lastcamp(rng, faded):
    """Paper_Mid_LastCamp: Isolde's Last Camp just across the line: her tent, her lamp still lit (the one gold in the
    Greyfold), her atlas open on a stone, a kettle, the white a pace from the fire."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    flat, cloth, dark, wood, iron, gold, glow, grey = (p("flat", PALE), p("cloth", lerp(GREY, PAPER, 0.4)), p("dark", DARK), p("wood", WOOD),
                                                       p("iron", lerp(DARK, INK, 0.3)), p("gold", GOLD, 0.0), p("glow", lerp(GOLD, PAPER, 0.75), 0.0), p("grey", GREY))
    ridge("flat", rng, -40, 40, 0.0, 0.5, 0.1, 40, flat, y=0.5)
    tent("tent", -4, 0.45, 5.5, 3.0, cloth, dark, y=0.2, open_side=1)
    lamp("lamp", 2.0, 0.45, 1.6, iron, gold, glow, y=0.1)
    box("stone", 6.5, 0.75, 1.8, 0.7, grey, y=0.25)
    polygon("atlas_l", [(5.7, 1.1), (6.5, 1.1), (6.5, 1.22), (5.7, 1.3)], PAPER_MAT[0], y=0.12)
    polygon("atlas_r", [(6.5, 1.1), (7.3, 1.1), (7.3, 1.3), (6.5, 1.22)], PAPER_MAT[0], y=0.12)
    for k in range(4):
        stroke("page_%d" % k, 5.85 + 0.03 * k, 1.17 + 0.025 * k, 6.35, 1.16 + 0.025 * k, 0.012, dark, y=0.08)
    disc("kettle", -9, 0.75, 0.3, iron, y=0.2, n=12)
    for k in range(5):
        disc("ring_%d" % k, -0.6 + k * 0.3, 0.5, 0.08, grey, y=0.22, n=8)
    outline_grass(rng, "g", -41, 41, 0.3, 0.5, 40, grey, y=0.1)
    white_eat(rng, "eat", 12, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_far_white(rng, faded):
    """Paper_Far_White: the far white behind the road: the capital's roof-lines at the edge of the eye, a tower, and
    the road's last mileposts going away small."""
    p = Palette(0.45, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    faint, grey = p("faint", PAPER, 0.03), p("grey", GREY, 0.1)
    polygon("ground", [(-40, 2), (40, 2), (40, 4.5), (-40, 4.2)], PAPER_MAT[0], y=0.6)
    far_city(rng, "city", -40, 40, 3.8, faint, 16, y=0.5)
    stroke("tower", 12, 3.8, 12, 9.5, 0.9, faint, y=0.48)
    for k in range(6):
        milepost("post_%d" % k, -30 + k * 10, 2.6, 0.9 - k * 0.08, grey, grey, y=0.4, marks=0)
    white_eat(rng, "eat", -20, 40, 2, 12, y=-0.5, step=0.08)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.1, p.ink()


def layer_farther_blank(rng, faded):
    """Paper_Farther_Blank: nearly nothing: paper to the top of the screen and one horizon line that gives up."""
    p = Palette(0.6, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    polygon("sky", [(-40, 6), (40, 6), (40, 22), (-40, 22)], PAPER_MAT[0], y=0.9)
    stroke("horizon", -40, 7.4, 2, 7.2, 0.03, p("faint", GREY, 0.1), y=0.7)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.0, p.ink()


# ---------------------------------------------------------------- tiles

def tile_chalk(rng, faded):
    """Ground_Chalk: the Greyfold's ground: chalk-pale earth, a line of pebbles, grass gone to outline along the top."""
    p = Palette(0.0, faded)
    earth, dark, grey = p("earth", PALE), p("dark", DARK), p("grey", GREY)
    box("bed", 0, 0.5, 4.0, 1.0, earth, y=0.2)
    for i in range(8):
        disc("pebble_%d" % i, rng.uniform(-2, 2), rng.uniform(0.15, 0.6), rng.uniform(0.03, 0.06), grey, y=-0.1, n=8)
    stroke("top", -2, 0.96, 2, 0.96, 0.03, grey, y=0.05)
    for i in range(10):
        x = -2 + 4 * (i + rng.random()) / 10
        polygon("b_%d" % i, [(x - 0.02, 0.98), (x + 0.02, 0.98), (x + rng.uniform(-0.05, 0.05), 0.98 + rng.uniform(0.06, 0.12))], dark, y=-0.12)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_cobbles(rng, faded):
    """Ground_Cobbles: the Road That Stops: cobbles in two courses, every third one fainter than the last."""
    p = Palette(0.0, faded)
    bed, stone, pale, dark = p("bed", PALE), p("stone", GREY), p("pale", lerp(GREY, PAPER, 0.6)), p("dark", DARK)
    box("bed", 0, 0.5, 4.0, 1.0, bed, y=0.2)
    for row in range(2):
        x = -2.0 + (0.3 if row else 0.0)
        i = 0
        while x < 2.0:
            w = rng.uniform(0.5, 0.7)
            blob("c_%d_%d" % (row, i), rng, min(x + w / 2, 2.0 - w / 2), 0.3 + row * 0.42, w / 2, 0.18, pale if (i + row) % 3 == 2 else stone, y=-0.1 - 0.001 * i, n=10, wobble=0.12)
            x += w + 0.04
            i += 1
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_whitesand(rng, faded):
    """Ground_WhiteSand: the white shore: paper sand, a tide-line of grey, a shell."""
    p = Palette(0.0, faded)
    sand, grey, dark = p("sand", PAPER, 0.02), p("grey", GREY), p("dark", DARK)
    box("bed", 0, 0.5, 4.0, 1.0, sand, y=0.2)
    stroke("tide", -2, 0.82, 2, 0.86, 0.02, grey, y=0.05)
    for i in range(6):
        stroke("grain_%d" % i, rng.uniform(-2, 2), rng.uniform(0.2, 0.7), rng.uniform(-2, 2), rng.uniform(0.2, 0.7), 0.012, grey, y=0.04)
    disc("shell", rng.uniform(-1.5, 1.5), 0.5, 0.07, dark, y=-0.1, n=8)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_line(rng, faded):
    """Ground_Line: the Threshold's ground: trodden white, a chalk line drawn across it, stake-holes."""
    p = Palette(0.0, faded)
    bed, grey, dark, chalk = p("bed", PALE), p("grey", GREY), p("dark", DARK), p("chalk", PAPER)
    box("bed", 0, 0.5, 4.0, 1.0, bed, y=0.2)
    box("chalk", 0, 0.9, 4.0, 0.08, chalk, y=0.05)
    for i in range(3):
        disc("hole_%d" % i, -1.4 + i * 1.4 + rng.uniform(-0.2, 0.2), 0.75, 0.06, dark, y=-0.1, n=8)
    for i in range(10):
        stroke("tread_%d" % i, rng.uniform(-2, 2), rng.uniform(0.15, 0.65), rng.uniform(-2, 2), rng.uniform(0.15, 0.65), 0.012, grey, y=0.04)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Mid_Edge", "strip", layer_mid_edge, False),
    ("Paper_Far_Cathedral", "strip", layer_far_cathedral, False),
    ("Paper_Farther_Edge", "strip", layer_farther_edge, False),
    ("Paper_Mid_Fence", "strip", layer_mid_fence, False),
    ("Paper_Mid_Outpost", "strip", layer_mid_outpost, False),
    ("Paper_Mid_Nave", "strip", layer_mid_nave, False),
    ("Paper_Mid_Road", "strip", layer_mid_road, False),
    ("Paper_Mid_Shore", "strip", layer_mid_shore, False),
    ("Paper_Mid_Pool", "strip", layer_mid_pool, False),
    ("Paper_Mid_Line", "strip", layer_mid_line, False),
    ("Paper_Mid_LastCamp", "strip", layer_mid_lastcamp, False),
    ("Paper_Far_White", "strip", layer_far_white, False),
    ("Paper_Farther_Blank", "strip", layer_farther_blank, False),
    ("Ground_Chalk", "tile", tile_chalk, False),
    ("Ground_Cobbles", "tile", tile_cobbles, False),
    ("Ground_WhiteSand", "tile", tile_whitesand, False),
    ("Ground_Line", "tile", tile_line, False),
]


def main():
    run_kit(OUT, "Greyfold", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

"""Blank paper kit (ENV-08): every backdrop strip and ground tile the Blank's fixed islands use, rendered from cut-out
geometry with Freestyle ink lines in headless Blender, on the plumbing all the kits share (kitlib.py).

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/blank.py
    ... -P tools/paperkit/blank.py -- Paper_Mid_Hollow Ground_Grey      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Blank/.
The bible (4.7): inside the white; Thessaly Hollow grey, the old capital half-drawn and then drawn backwards, Aury's
light still turning; islands drifting past. White paper, no wash, Wren's blue and lantern gold the only colours,
ghost-grey ink (art-direction 5), a shade darker than the Greyfold's since here the grey is the people. Where the
Greyfold's strips are eaten from the east, the Blank's are drawn half: a thing stops where the drawing stopped
(`half_drawn`). Layer names are unique across the kits (a material is named after its layer).
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)
from greyfold import (rbox, stroke, outline_grass, lamp, chick, arch, column, PAPER_MAT)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Blank")

# The Blank's palette (art-direction 5): white paper, the greys the ink thinned, Wren's blue, lantern gold, a child's
# crayon in Corra's room (ochre, not a colour of the world's), ghost-grey ink.
PAPER = (0.98, 0.98, 0.97)
INK = (0.62, 0.62, 0.60)
GREY = (0.74, 0.74, 0.72)
PALE = (0.88, 0.88, 0.86)
DARK = (0.50, 0.50, 0.48)
BLUE = (0.24, 0.32, 0.50)
GOLD = (0.90, 0.76, 0.40)
CRAYON = (0.72, 0.56, 0.30)
STONE = (0.80, 0.80, 0.78)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


# ---------------------------------------------------------------- shapes

def half_drawn(rng, name, x0, x1, z0, z1, y=-0.5, step=0.12, from_east=True, amp=0.5):
    """Paper laid over the part of a thing the drawing never reached: one sheet whose edge jumps in and out at
    random, so the thing ends mid-line. From the east unless told otherwise. One shape, not bands: every edge
    Freestyle finds becomes a line."""
    far, near = (x1 + 2, x1) if from_east else (x0 - 2, x0)
    pts = [(far, z0 - 1), (far, z1 + 1)]
    z = z1 + 1
    while z > z0 - 1:
        reach = (x1 - x0) * (1.0 - amp + amp * rng.uniform(0.0, 1.0) ** 2)   # the edge wanders over the last `amp` of the span
        pts.append((near - reach if from_east else near + reach, z))
        z -= step
    return polygon(name, pts, PAPER_MAT[0], y=y)


def house(name, rng, x, base_z, w, h, wall, roof, dark, y=0.0, door=True):
    """A Hollow house: low walls under a deep roof, a door, a window, a chimney."""
    box(name, x, base_z + h * 0.3, w, h * 0.6, wall, y=y)
    polygon(name + "_roof", [(x - w * 0.6, base_z + h * 0.58), (x + w * 0.6, base_z + h * 0.58), (x + w * 0.15, base_z + h), (x - w * 0.15, base_z + h)], roof, y=y - 0.02)
    if door:
        box(name + "_door", x - w * 0.22, base_z + h * 0.2, w * 0.16, h * 0.4, dark, y=y - 0.03)
    box(name + "_win", x + w * 0.22, base_z + h * 0.35, w * 0.16, h * 0.16, dark, y=y - 0.03)
    box(name + "_winx", x + w * 0.22, base_z + h * 0.35, w * 0.12, 0.02, wall, y=y - 0.04)
    box(name + "_chim", x + w * 0.35, base_z + h * 0.85, w * 0.08, h * 0.3, dark, y=y - 0.01)


def island(name, rng, x, z, w, h, top, under, dark, y=0.0):
    """A drifting island: a slab of ground with a ragged underside, grass along its top."""
    pts = [(x - w / 2, z), (x + w / 2, z)]
    n = 9
    for i in range(n, -1, -1):
        t = i / n
        pts.append((x - w / 2 + w * t, z - h * (0.3 + 0.7 * math.sin(t * math.pi)) * rng.uniform(0.7, 1.0)))
    polygon(name + "_under", pts, under, y=y + 0.01)
    box(name, x, z + 0.12, w, 0.24, top, y=y)
    for k in range(int(w * 1.5)):
        bx = x - w / 2 + w * (k + rng.random()) / int(w * 1.5)
        stroke("%s_g%d" % (name, k), bx, z + 0.24, bx + rng.uniform(-0.05, 0.05), z + 0.24 + rng.uniform(0.1, 0.25), 0.03, dark, y=y - 0.01)


def facade(name, rng, x, base_z, w, h, wall, dark, y=0.0, floors=2):
    """A capital street front: a tall house with rows of windows, a doorway, a cornice."""
    box(name, x, base_z + h / 2, w, h, wall, y=y)
    box(name + "_cornice", x, base_z + h - 0.08, w + 0.2, 0.16, dark, y=y - 0.01)
    for f in range(floors):
        for k in range(max(1, int(w / 1.1))):
            wx = x - w / 2 + (k + 0.5) * w / max(1, int(w / 1.1))
            box("%s_w%d_%d" % (name, f, k), wx, base_z + 1.6 + f * (h - 1.6) / floors, 0.4, 0.7, dark, y=y - 0.02)
    box(name + "_door", x, base_z + 0.7, 0.6, 1.4, dark, y=y - 0.02)


def dome_half(name, x, base_z, r, mat, dark, y=0.0, n=16):
    """The mirror-Observatory's dome, the half the capital kept: a quarter-circle standing on a drum."""
    box(name + "_drum", x + r / 2, base_z + 1.0, r, 2.0, mat, y=y)
    pts = [(x, base_z + 2.0), (x + r, base_z + 2.0)]
    for i in range(n, -1, -1):
        a = (math.pi / 2) * i / n
        pts.append((x + r * math.cos(a), base_z + 2.0 + r * math.sin(a)))
    polygon(name, pts, mat, y=y - 0.01)
    for k in range(4):
        a = (math.pi / 2) * (k + 0.5) / 4
        stroke("%s_rib%d" % (name, k), x, base_z + 2.0, x + r * math.cos(a), base_z + 2.0 + r * math.sin(a), 0.03, dark, y=y - 0.03)


def crayon_heron(name, rng, x, base_z, h, crayon, y=0.0):
    """A child's drawing of a tall heron: scribbled strokes, a compass in its wing, no face."""
    t = 0.09
    stroke(name + "_leg1", x - 0.2, base_z, x - 0.1, base_z + h * 0.45, t, crayon, y=y)
    stroke(name + "_leg2", x + 0.25, base_z, x + 0.1, base_z + h * 0.45, t, crayon, y=y)
    for k in range(6):
        a = k / 6
        stroke("%s_body%d" % (name, k), x - 0.6 + rng.uniform(-0.1, 0.1), base_z + h * (0.45 + 0.08 * k) + rng.uniform(-0.05, 0.05), x + 0.6 + rng.uniform(-0.1, 0.1), base_z + h * (0.5 + 0.08 * k), t, crayon, y=y - 0.001 * k)
    stroke(name + "_neck", x + 0.3, base_z + h * 0.85, x + 0.55, base_z + h * 1.0, t, crayon, y=y)
    disc(name + "_head", x + 0.6, base_z + h * 1.02, 0.22, crayon, y=y - 0.01, n=12)
    stroke(name + "_bill", x + 0.75, base_z + h * 1.0, x + 1.2, base_z + h * 0.96, t * 0.7, crayon, y=y)
    disc(name + "_comp", x - 0.2, base_z + h * 0.68, 0.18, crayon, y=y - 0.02, n=10)
    disc(name + "_compin", x - 0.2, base_z + h * 0.68, 0.12, PAPER_MAT[0], y=y - 0.03, n=10)
    stroke(name + "_needle", x - 0.3, base_z + h * 0.62, x - 0.1, base_z + h * 0.74, 0.03, crayon, y=y - 0.04)


def beacon(name, x, base_z, h, iron, gold, glow, y=0.0):
    """A lighthouse lamp turning: the lens on its stand, the beam going out one way, the glow around it."""
    box(name + "_stand", x, base_z + h * 0.25, 0.5, h * 0.5, iron, y=y)
    disc(name + "_lens", x, base_z + h * 0.75, h * 0.25, gold, y=y - 0.02, n=16)
    polygon(name + "_beam", [(x + h * 0.2, base_z + h * 0.85), (x + h * 0.2, base_z + h * 0.65), (x + 9, base_z + h * 0.1), (x + 9, base_z + h * 1.4)], glow, y=y + 0.2)
    disc(name + "_glow", x, base_z + h * 0.75, h * 0.6, glow, y=y + 0.3, n=20)


# ---------------------------------------------------------------- layers

def layer_mid_lantern(rng, faded):
    """Paper_Mid_Lantern: inside the white: nothing, then colour blooming where she walked: a few strokes of Wren's
    blue and the lantern's gold in the middle of the strip and outlines fading off either side; the first island's
    edge under it."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grey, blue, gold, under = p("grey", GREY), p("blue", lerp(BLUE, PAPER, 0.35), 0.0), p("gold", lerp(GOLD, PAPER, 0.3), 0.0), p("under", PALE)
    island("isle", rng, 4, 0.0, 44, 1.6, p("top", STONE), under, p("dark", DARK), y=0.5)
    for k in range(10):
        a = k / 10
        x = -4 + 8 * a
        blob("bloom_%d" % k, rng, x, 0.6 + rng.uniform(0, 1.6), 0.9 - abs(a - 0.5), 0.5, blue if k % 3 else gold, y=0.2 - 0.005 * k, n=12, wobble=0.4)
    outline_grass(rng, "g", -18, 26, 0.24, 0.6, 50, grey, y=0.1)
    half_drawn(rng, "half", -8, 40, 0, 6, y=-0.5, amp=0.35)
    half_drawn(rng, "halfw", -40, 8, 0, 6, y=-0.5, from_east=False, amp=0.35)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_hollow(rng, faded):
    """Paper_Mid_Hollow: Thessaly Hollow: low houses under deep roofs, grey; a well; height marks in a doorframe;
    the Remnant's chicks along the roofs."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    ground, wall, roof, dark, grey = p("ground", PALE), p("wall", STONE), p("roof", GREY), p("dark", DARK), p("grey", GREY)
    ridge("ground", rng, -40, 40, 0.0, 0.5, 0.1, 40, ground, y=0.5)
    for k, x in enumerate((-33, -24, -13, -2, 9, 19, 30)):
        house("house_%d" % k, rng, x, 0.4, rng.uniform(4.0, 5.5), rng.uniform(3.0, 3.8), wall, roof, dark, y=0.2 + 0.01 * k)
    for k, (x, z) in enumerate(((-31, 3.9), (-11, 3.6), (11, 3.7))):
        chick("chick_%d" % k, x, z, grey, dark, y=0.1)
    disc("well", 4, 0.9, 0.7, wall, y=0.15, n=14)
    disc("well_in", 4, 0.9, 0.5, dark, y=0.14, n=14)
    box("well_post", 4, 1.9, 0.08, 1.4, dark, y=0.13)
    for k in range(5):
        stroke("mark_%d" % k, -3.0, 0.9 + k * 0.28, -2.75, 0.9 + k * 0.28, 0.025, dark, y=0.05)
    outline_grass(rng, "g", -41, 41, 0.3, 0.5, 40, grey, y=0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_drift(rng, faded):
    """Paper_Mid_Drift: the Hollow's far edge: the village's ground breaking off, its underside ragged over the
    white, the undersides of islands going past."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    top, under, dark, grey = p("top", STONE), p("under", PALE), p("dark", DARK), p("grey", GREY)
    island("edge", rng, -22, 0.0, 36, 3.0, top, under, dark, y=0.5)
    for k, (x, z, w) in enumerate(((4, 3.2, 7), (16, 1.6, 9), (30, 4.0, 6))):
        island("isle_%d" % k, rng, x, z, w, rng.uniform(1.0, 1.8), top, under, dark, y=0.3 + 0.01 * k)
    house("far_house", rng, 17, 1.84, 2.4, 1.8, p("wall", STONE), grey, dark, y=0.25)
    outline_grass(rng, "g", -40, -4, 0.24, 0.5, 30, grey, y=0.1)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_capital(rng, faded):
    """Paper_Mid_Capital: streets of the old capital, half-drawn: tall fronts with rows of windows, a Guild office
    door with a nameplate, lamps unlit, every second building stopping where the drawing stopped."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    street, wall, dark, iron, brass = p("street", PALE), p("wall", STONE), p("dark", DARK), p("iron", lerp(DARK, INK, 0.3)), p("brass", lerp(GOLD, PAPER, 0.5))
    polygon("street", [(-40, 0), (40, 0), (40, 0.6), (-40, 0.55)], street, y=0.5)
    x = -38
    k = 0
    while x < 40:
        w, h = rng.uniform(4.0, 7.0), rng.uniform(5.0, 7.6)
        facade("front_%d" % k, rng, x + w / 2, 0.5, w, h, wall, dark, y=0.3 + 0.005 * k, floors=rng.randint(2, 3))
        x += w + 0.3
        k += 1
    box("plate", -9.0, 1.9, 0.5, 0.18, brass, y=0.05)
    for k, lx in enumerate((-20, 0, 20)):
        stroke("lamp_%d" % k, lx, 0.5, lx, 3.2, 0.06, iron, y=0.1)
        box("lampbox_%d" % k, lx, 3.4, 0.36, 0.5, iron, y=0.08)
    half_drawn(rng, "half", -4, 40, 0, 8, y=-0.5, step=0.08)
    return STRIP_WIDTH, 8.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_crayon(rng, faded):
    """Paper_Mid_Crayon: Corra's room: a white room with a wainscot, the same tall heron drawn over and over in
    crayon, each with a compass, none with a face."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    wall, rail, crayon, dark = p("wall", PAPER, 0.0), p("rail", PALE), p("crayon", CRAYON, 0.0), p("dark", DARK)
    polygon("wall", [(-40, 0), (40, 0), (40, 6), (-40, 6)], wall, y=0.6)
    box("rail", 0, 1.1, 80, 0.1, rail, y=0.4)
    box("skirting", 0, 0.1, 80, 0.2, rail, y=0.4)
    for k in range(9):
        x = -36 + k * 9 + rng.uniform(-1, 1)
        crayon_heron("heron_%d" % k, rng, x, 0.3 + rng.uniform(0, 0.6), rng.uniform(2.6, 4.2), crayon, y=0.2 + 0.005 * k)
    box("door", 30, 1.6, 1.4, 3.2, rail, y=0.3)
    disc("knob", 30.5, 1.6, 0.06, dark, y=0.25, n=8)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_mirror(rng, faded):
    """Paper_Mid_Mirror: the capital's streets reversed: the same fronts drawn right to left, their doors on the wrong
    side, and the mirror-Observatory's half dome at their end."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    street, wall, dark, brass = p("street", PALE), p("wall", STONE), p("dark", DARK), p("brass", lerp(GOLD, PAPER, 0.5))
    polygon("street", [(-40, 0), (40, 0), (40, 0.6), (-40, 0.55)], street, y=0.5)
    x = 40
    k = 0
    while x > -14:
        w, h = rng.uniform(4.0, 7.0), rng.uniform(5.0, 7.6)
        facade("front_%d" % k, rng, x - w / 2, 0.5, w, h, wall, dark, y=0.3 + 0.005 * k, floors=rng.randint(2, 3))
        box("front_%d_door2" % k, x - w / 2 + w * 0.3, 1.2, 0.6, 1.4, dark, y=0.28)
        x -= w + 0.3
        k += 1
    dome_half("dome", -14, 0.5, 7.0, wall, dark, y=0.35)
    for k in range(4):
        stroke("step_%d" % k, -14 - k * 0.6, 0.5 + k * 0.25, -10, 0.5 + k * 0.25, 0.06, dark, y=0.3)
    disc("socket", -8.5, 4.2, 0.4, brass, y=0.28, n=12)
    half_drawn(rng, "half", -40, -16, 0, 8, y=-0.5, step=0.08)
    return STRIP_WIDTH, 8.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_causeway(rng, faded):
    """Paper_Mid_Causeway: the tether's end: a stone causeway running into the white, the sea gone to paper either
    side, the third lighthouse's foot ahead, a tether-post with Sable's rope on it."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    stone, dark, water, grey, rope = p("stone", STONE), p("dark", DARK), p("water", lerp(BLUE, PAPER, 0.9)), p("grey", GREY), p("rope", lerp(GOLD, GREY, 0.5))
    polygon("sea", [(-40, 0), (40, 0), (40, 1.4), (-40, 1.2)], water, y=0.6)
    for i in range(24):
        x = rng.uniform(-40, 40)
        stroke("wave_%d" % i, x, rng.uniform(0.2, 1.2), x + rng.uniform(0.6, 1.6), rng.uniform(0.2, 1.2), 0.015, grey, y=0.55)
    polygon("causeway", [(-40, 0.9), (40, 0.9), (40, 1.7), (-40, 1.5)], stone, y=0.4)
    for k in range(40):
        stroke("joint_%d" % k, -40 + k * 2 + rng.uniform(-0.3, 0.3), 0.9, -40 + k * 2 + rng.uniform(-0.3, 0.3), 1.6, 0.02, dark, y=0.35)
    polygon("foot", [(24, 1.5), (34, 1.5), (33, 6), (25, 6)], stone, y=0.3)
    for k in range(5):
        stroke("course_%d" % k, 24.4 + k * 0.15, 2.2 + k * 0.8, 33.6 - k * 0.15, 2.2 + k * 0.8, 0.03, dark, y=0.28)
    box("post", -30, 2.5, 0.22, 1.8, dark, y=0.2)
    stroke("rope", -29.9, 3.2, -40, 3.8, 0.03, rope, y=0.18)
    half_drawn(rng, "half", 30, 40, 0, 6, y=-0.5)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_mid_lamproom(rng, faded):
    """Paper_Mid_LampRoom: Aury's lamp room: the lamp turning (the one gold in the Blank), the rail, the glass in
    its iron frame, a kettle, two chairs."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    wall, iron, gold, glow, dark = p("wall", PALE), p("iron", lerp(DARK, INK, 0.3)), p("gold", GOLD, 0.0), p("glow", lerp(GOLD, PAPER, 0.7), 0.0), p("dark", DARK)
    polygon("wall", [(-40, 0), (40, 0), (40, 6), (-40, 6)], wall, y=0.6)
    for k in range(17):
        stroke("mullion_%d" % k, -40 + k * 5, 1.2, -40 + k * 5, 6, 0.08, iron, y=0.5)
    box("sill", 0, 1.15, 80, 0.12, iron, y=0.5)
    box("rail", 0, 1.9, 80, 0.06, iron, y=0.45)
    beacon("lamp", 2, 0.5, 3.2, iron, gold, glow, y=0.2)
    disc("kettle", -8, 0.75, 0.3, iron, y=0.25, n=12)
    for k, x in enumerate((-12, 10)):
        box("chair_%d" % k, x, 0.85, 0.9, 0.1, dark, y=0.3)
        box("chairback_%d" % k, x - 0.4, 1.3, 0.1, 1.0, dark, y=0.3)
        for dx in (-0.4, 0.4):
            stroke("chairleg_%d_%s" % (k, dx), x + dx, 0.0, x + dx, 0.85, 0.06, dark, y=0.3)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 1.6, p.ink()


def layer_far_islands(rng, faded):
    """Paper_Far_Islands: islands drifting far off: slabs with houses on them, undersides ragged, each paler than
    the last."""
    p = Palette(0.45, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    top, under, dark, wall, roof = p("top", STONE), p("under", PALE), p("dark", GREY), p("wall", STONE, 0.1), p("roof", GREY, 0.1)
    for k, (x, z, w) in enumerate(((-30, 4.5, 9), (-12, 7.5, 7), (6, 3.5, 11), (24, 6.5, 8), (36, 9.5, 5))):
        island("isle_%d" % k, rng, x, z, w, rng.uniform(1.2, 2.2), top, under, dark, y=0.4 + 0.01 * k)
        for h in range(rng.randint(1, 3)):
            house("h_%d_%d" % (k, h), rng, x - w / 2 + 1.5 + h * (w - 3) / 2, z + 0.24, rng.uniform(1.2, 2.0), rng.uniform(1.0, 1.5), wall, roof, dark, y=0.3 + 0.01 * k, door=False)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.1, p.ink()


def layer_farther_grey(rng, faded):
    """Paper_Farther_Grey: nothing at all: the white, and the grey of a horizon that is not one."""
    p = Palette(0.6, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    polygon("sky", [(-40, 6), (40, 6), (40, 22), (-40, 22)], PAPER_MAT[0], y=0.9)
    for k in range(3):
        stroke("band_%d" % k, -40, 8 + k * 2.5, 40, 8.2 + k * 2.5, 0.02, p("faint", GREY, 0.12), y=0.7)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.0, p.ink()


# ---------------------------------------------------------------- tiles

def tile_grey(rng, faded):
    """Ground_Grey: an island's ground: grey slab, a crack, grass gone to outline along the top."""
    p = Palette(0.0, faded)
    slab, dark, grey = p("slab", STONE), p("dark", DARK), p("grey", GREY)
    box("bed", 0, 0.5, 4.0, 1.0, slab, y=0.2)
    stroke("crack", rng.uniform(-1.5, 0), rng.uniform(0.2, 0.5), rng.uniform(0, 1.5), rng.uniform(0.5, 0.8), 0.02, dark, y=0.05)
    stroke("top", -2, 0.96, 2, 0.96, 0.03, dark, y=0.05)
    for i in range(10):
        x = -2 + 4 * (i + rng.random()) / 10
        polygon("b_%d" % i, [(x - 0.02, 0.98), (x + 0.02, 0.98), (x + rng.uniform(-0.05, 0.05), 0.98 + rng.uniform(0.06, 0.12))], grey, y=-0.12)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_street(rng, faded):
    """Ground_Street: the capital's paving: long flags, half of them never finished."""
    p = Palette(0.0, faded)
    bed, flag, pale, dark = p("bed", PALE), p("flag", STONE), p("pale", lerp(STONE, PAPER, 0.6)), p("dark", DARK)
    box("bed", 0, 0.5, 4.0, 1.0, bed, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.9, 1.5)
        box("flag_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.5, w - 0.05, 0.92, pale if i % 2 else flag, y=0.0)
        x += w
        i += 1
    stroke("kerb", -2, 0.94, 2, 0.94, 0.03, dark, y=-0.05)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_crayon(rng, faded):
    """Ground_Crayon: Corra's floor: white boards with crayon scribble across them."""
    p = Palette(0.0, faded)
    board, line, crayon = p("board", PAPER, 0.0), p("line", PALE), p("crayon", CRAYON, 0.0)
    box("bed", 0, 0.5, 4.0, 1.0, board, y=0.2)
    for k in range(3):
        stroke("seam_%d" % k, -2, 0.25 + k * 0.25, 2, 0.25 + k * 0.25, 0.015, line, y=0.05)
    for i in range(6):
        x, z = rng.uniform(-1.8, 1.8), rng.uniform(0.2, 0.8)
        stroke("scribble_%d" % i, x, z, x + rng.uniform(-0.5, 0.5), z + rng.uniform(-0.2, 0.2), 0.05, crayon, y=0.0)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


def tile_causeway(rng, faded):
    """Ground_Causeway: the causeway's stone, wet along the top, weed in a joint."""
    p = Palette(0.0, faded)
    stone, dark, wet, weed = p("stone", STONE), p("dark", DARK), p("wet", lerp(BLUE, PAPER, 0.8)), p("weed", lerp(GREY, DARK, 0.5))
    box("bed", 0, 0.5, 4.0, 1.0, stone, y=0.2)
    box("wet", 0, 0.92, 4.0, 0.14, wet, y=0.05)
    for k in range(3):
        stroke("joint_%d" % k, -1.5 + k * 1.4, 0.05, -1.5 + k * 1.4 + rng.uniform(-0.1, 0.1), 0.9, 0.02, dark, y=0.0)
    stroke("weed", -0.1, 0.3, 0.2, 0.8, 0.03, weed, y=-0.02)
    return 4.0, 1.0, TILE_PPU, 0.0, 1.6, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Mid_Lantern", "strip", layer_mid_lantern, False),
    ("Paper_Mid_Hollow", "strip", layer_mid_hollow, False),
    ("Paper_Mid_Drift", "strip", layer_mid_drift, False),
    ("Paper_Mid_Capital", "strip", layer_mid_capital, False),
    ("Paper_Mid_Crayon", "strip", layer_mid_crayon, False),
    ("Paper_Mid_Mirror", "strip", layer_mid_mirror, False),
    ("Paper_Mid_Causeway", "strip", layer_mid_causeway, False),
    ("Paper_Mid_LampRoom", "strip", layer_mid_lamproom, False),
    ("Paper_Far_Islands", "strip", layer_far_islands, False),
    ("Paper_Farther_Grey", "strip", layer_farther_grey, False),
    ("Ground_Grey", "tile", tile_grey, False),
    ("Ground_Street", "tile", tile_street, False),
    ("Ground_Crayon", "tile", tile_crayon, False),
    ("Ground_Causeway", "tile", tile_causeway, False),
]


def main():
    run_kit(OUT, "Blank", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

"""Windreach paper kit (ENV-07): every backdrop strip and ground tile the Steppe's rooms use, rendered from cut-out
geometry with Freestyle ink lines in headless Blender, on the plumbing all the kits share (kitlib.py).

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/windreach.py
    ... -P tools/paperkit/windreach.py -- Paper_Mid_Stones Ground_Turf      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Windreach/.
The bible (4.5): endless wind-bent grass, long shadows, sky that is most of the screen, standing stones, the clans'
walking-wagons; updrafts drawn as visible ink-swirls. Straw paper, sky blue, storm violet, gold, grey-brown ink
(art-direction 5). The wind is from the west: every blade in every layer leans east. Farther layers wash toward the
paper and draw thinner, as everywhere. Layer names are unique across the kits (a material is named after its layer).
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Windreach")

# Windreach palette (art-direction 5): paper, wash 1, wash 2, accent, ink. The paper is ProjectSetup.RegionPaper's.
PAPER = (0.93, 0.88, 0.70)
STRAW = (0.78, 0.64, 0.32)
SKY = (0.54, 0.70, 0.86)
VIOLET = (0.40, 0.34, 0.54)
GOLD = (0.88, 0.70, 0.28)
INK = (0.30, 0.24, 0.18)
STONE = lerp(VIOLET, PAPER, 0.5)
LICHEN = lerp(GOLD, STRAW, 0.5)
WOOD = lerp(INK, STRAW, 0.35)
IRON = lerp(VIOLET, INK, 0.55)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6
LEAN = 0.35   # the wind: how far east a blade's tip leans per unit of height


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


# ---------------------------------------------------------------- shapes

def disc(name, cx, cz, r, material, y=0.0, n=16, squash=1.0):
    """A round, squashed if asked (a pot seen from the side)."""
    pts = [(cx + r * math.cos(2 * math.pi * i / n), cz + r * squash * math.sin(2 * math.pi * i / n)) for i in range(n)]
    return polygon(name, pts, material, y=y)


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


def blade(name, x, base_z, h, material, lean=LEAN, y=0.0, w=0.07):
    """One grass blade: a tapering triangle whose tip leans east with the wind."""
    return polygon(name, [(x - w / 2, base_z), (x + w / 2, base_z), (x + lean * h, base_z + h)], material, y=y)


def tussock(name, rng, x, base_z, w, h, material, lean=LEAN, y=0.0, n=9):
    """A clump of blades fanning out of one root, all leaning the same way."""
    for i in range(n):
        t = (i + 0.5) / n - 0.5
        blade("%s_%d" % (name, i), x + t * w, base_z, h * rng.uniform(0.6, 1.0), material, lean=lean + t * 0.5, y=y - 0.002 * i, w=0.05 + 0.03 * abs(t))


def grass_band(rng, name, x0, x1, base_z, h, amp, n, material, lean=LEAN, y=0.0):
    """A band of tussocks across a strip."""
    for i in range(n):
        x = x0 + (x1 - x0) * (i + rng.uniform(0.2, 0.8)) / n
        tussock("%s_%d" % (name, i), rng, x, base_z, rng.uniform(0.4, 0.9), h + rng.uniform(-amp, amp), material, lean=lean, y=y - 0.01 * (i % 5))


def standing_stone(name, rng, x, base_z, w, h, stone, lichen, dark, y=0.0, tilt=0.0, notches=0):
    """A standing stone: a tapering slab with a worn top, lichen on its north (west) face, notches cut in it."""
    pts = [(x - w / 2, base_z), (x + w / 2, base_z), (x + w * 0.42 + tilt, base_z + h * 0.92), (x + w * 0.1 + tilt, base_z + h), (x - w * 0.38 + tilt, base_z + h * 0.95)]
    polygon(name, pts, stone, y=y)
    for i in range(rng.randint(2, 4)):
        blob("%s_l%d" % (name, i), rng, x - w * 0.28 + tilt * 0.5, base_z + h * rng.uniform(0.35, 0.85), w * 0.18, h * 0.07, lichen, y=y - 0.03, n=10, wobble=0.35)
    for i in range(notches):
        box("%s_n%d" % (name, i), x + w * 0.15 + tilt * 0.6, base_z + h * 0.25 + i * 0.16, 0.18, 0.035, dark, y=y - 0.04)
    stroke("%s_c" % name, x - w * 0.1, base_z + h * 0.3, x + w * 0.05, base_z + h * 0.7, 0.025, dark, y=y - 0.04)


def shadow(name, x, base_z, w, length, material, y=0.0):
    """A long shadow lying east on the grass, as the low light draws it."""
    return polygon(name, [(x, base_z), (x + w, base_z), (x + w + length, base_z + 0.08), (x + length, base_z + 0.12)], material, y=y)


def wagon(name, rng, x, base_z, w, h, wood, cloth, dark, y=0.0, cloth_band=None):
    """A walking-wagon: a plank body on two tall wheels, a canvas hoop over it, the shafts resting on the grass."""
    r = h * 0.3
    for k, wx in enumerate((x - w * 0.3, x + w * 0.3)):
        disc("%s_w%d" % (name, k), wx, base_z + r, r, wood, y=y + 0.02)
        disc("%s_wi%d" % (name, k), wx, base_z + r, r * 0.8, PAPER_MAT[0], y=y + 0.01)
        for s in range(6):
            rbox("%s_s%d_%d" % (name, k, s), wx, base_z + r, 0.05, r * 1.7, dark, math.pi * s / 6, y=y - 0.01)
        disc("%s_h%d" % (name, k), wx, base_z + r, r * 0.16, dark, y=y - 0.02, n=10)
    box(name + "_body", x, base_z + r + h * 0.22, w, h * 0.36, wood, y=y - 0.03)
    for i in range(4):
        box("%s_p%d" % (name, i), x, base_z + r + h * 0.1 + i * h * 0.09, w, 0.02, dark, y=y - 0.04)
    top = base_z + r + h * 0.4
    pts = [(x - w / 2, top), (x + w / 2, top)]
    n = 10
    for i in range(n, -1, -1):
        a = math.pi * i / n
        pts.append((x + w / 2 * math.cos(a), top + h * 0.5 * math.sin(a)))
    polygon(name + "_hoop", pts, cloth, y=y - 0.05)
    for i in range(1, 4):
        a = math.pi * i / 4
        stroke("%s_rib%d" % (name, i), x + w / 2 * math.cos(a) * 0.98, top, x + w / 2 * math.cos(a) * 0.98, top + h * 0.5 * math.sin(a) * 0.98, 0.02, dark, y=y - 0.06)
    if cloth_band is not None:
        box(name + "_band", x, base_z + r + h * 0.25, w * 0.6, h * 0.16, cloth_band, y=y - 0.07)
        for i in range(6):
            stroke("%s_z%d" % (name, i), x - w * 0.28 + i * w * 0.1, base_z + r + h * 0.19, x - w * 0.23 + i * w * 0.1, base_z + r + h * 0.31, 0.015, dark, y=y - 0.08)
    stroke(name + "_shaft", x + w * 0.5, base_z + r + h * 0.1, x + w * 0.85, base_z + 0.1, 0.06, wood, y=y - 0.02)


def fire(name, rng, x, base_z, w, stone, flame, glow, dark, y=0.0):
    """A fire in a ring of stones, with smoke."""
    for i in range(7):
        blob("%s_r%d" % (name, i), rng, x - w / 2 + w * i / 6, base_z + 0.08, 0.14, 0.09, stone, y=y, n=8, wobble=0.3)
    polygon(name + "_glow", [(x - w * 0.3, base_z + 0.1), (x + w * 0.3, base_z + 0.1), (x + w * 0.12, base_z + 0.7), (x - w * 0.1, base_z + 0.6)], glow, y=y - 0.02)
    polygon(name + "_flame", [(x - w * 0.18, base_z + 0.1), (x + w * 0.18, base_z + 0.1), (x + w * 0.05, base_z + 0.45), (x - w * 0.02, base_z + 0.38)], flame, y=y - 0.04)
    for i in range(3):
        box("%s_log%d" % (name, i), x + (i - 1) * 0.18, base_z + 0.12, 0.5, 0.06, dark, y=y - 0.03)


def smoke(rng, name, x, z0, n, material, y=0.0):
    """Smoke that drifts east as it rises."""
    for i in range(n):
        t = i / max(1, n - 1)
        blob("%s_%d" % (name, i), rng, x + t * 2.2 + rng.uniform(-0.2, 0.2), z0 + t * 2.4, 0.25 + t * 0.5, 0.18 + t * 0.3, material, y=y + 0.01 * i, n=10, wobble=0.35)


def hull(name, rng, x, base_z, w, h, wood, dark, paper, y=0.0, tilt=0.5):
    """A boat on its side in the mud: the hull as a lens, planks across it, the name on the bow."""
    pts = []
    n = 16
    for i in range(n):
        a = 2 * math.pi * i / n
        pts.append((x + w / 2 * math.cos(a), base_z + h / 2 + h / 2 * math.sin(a) * (1.0 if math.sin(a) > 0 else 0.5)))
    ob = polygon(name, pts, wood, y=y)
    for i in range(4):
        stroke("%s_p%d" % (name, i), x - w * 0.45, base_z + h * 0.2 + i * h * 0.18, x + w * 0.45, base_z + h * 0.25 + i * h * 0.17, 0.02, dark, y=y - 0.03)
    box(name + "_keel", x, base_z + h * 0.62, w * 0.9, 0.05, dark, y=y - 0.04)
    for i in range(3):
        stroke("%s_t%d" % (name, i), x + w * 0.25 + i * 0.12, base_z + h * 0.3, x + w * 0.3 + i * 0.12, base_z + h * 0.5, 0.015, paper, y=y - 0.05)
    return ob


def reeds(rng, name, x0, x1, base_z, n, stalk, head, y=0.0):
    """Dead reeds: thin stalks leaning east, a seed-head on each."""
    for i in range(n):
        x = x0 + (x1 - x0) * (i + rng.uniform(0.2, 0.8)) / n
        h = rng.uniform(1.2, 2.2)
        tip = (x + LEAN * h * 0.8, base_z + h)
        stroke("%s_s%d" % (name, i), x, base_z, tip[0], tip[1], 0.03, stalk, y=y)
        disc("%s_h%d" % (name, i), tip[0], tip[1], rng.uniform(0.06, 0.1), head, y=y - 0.02, n=8)


def cracks(rng, name, x0, x1, z0, z1, n, material, y=0.0):
    """Cracked mud: short dark strokes meeting at angles."""
    for i in range(n):
        x, z = rng.uniform(x0, x1), rng.uniform(z0, z1)
        for k in range(3):
            a = rng.uniform(0, 2 * math.pi)
            l = rng.uniform(0.2, 0.6)
            stroke("%s_%d_%d" % (name, i, k), x, z, x + l * math.cos(a), z + l * math.sin(a) * 0.4, 0.02, material, y=y)


def swirl(name, x, z0, z1, w, material, y=0.0, turns=3.0, thickness=0.09, phase=0.0):
    """An updraft drawn as ink: a ribbon winding up a column, as the clans draw the wind."""
    n = int(24 * turns)
    prev = None
    for i in range(n + 1):
        t = i / n
        pt = (x + w / 2 * math.sin(phase + t * turns * 2 * math.pi), z0 + t * (z1 - z0))
        if prev is not None:
            stroke("%s_%d" % (name, i), prev[0], prev[1], pt[0], pt[1], thickness * (0.6 + 0.4 * abs(math.cos(phase + t * turns * 2 * math.pi))), material, y=y)
        prev = pt


def cliff(rng, name, x0, x1, base_z, top_z, stone, dark, groove, y=0.0):
    """A cut bank's face: strata lines, talon grooves worn into it, a lip of turf at the top."""
    polygon(name, [(x0, base_z), (x1, base_z), (x1 + 0.4, top_z), (x0 - 0.4, top_z)], stone, y=y)
    z = base_z + 0.6
    k = 0
    while z < top_z - 0.3:
        stroke("%s_st%d" % (name, k), x0 + rng.uniform(0, 1.5), z, x1 - rng.uniform(0, 1.5), z + rng.uniform(-0.15, 0.15), 0.04, dark, y=y - 0.02)
        z += rng.uniform(0.9, 1.6)
        k += 1
    for i in range(int((x1 - x0) / 1.6)):
        gx, gz = rng.uniform(x0 + 0.5, x1 - 0.5), rng.uniform(base_z + 0.8, top_z - 0.8)
        for j in range(3):
            box("%s_g%d_%d" % (name, i, j), gx + j * 0.14 - 0.14, gz, 0.04, rng.uniform(0.3, 0.6), groove, y=y - 0.04)


def cloud(rng, name, x, z, w, h, material, y=0.0):
    for i in range(4):
        blob("%s_%d" % (name, i), rng, x + (i - 1.5) * w * 0.22, z + rng.uniform(-h * 0.2, h * 0.2), w * 0.3, h * rng.uniform(0.5, 1.0), material, y=y - 0.001 * i, wobble=0.3)


def birds(rng, name, x, z, n, material, y=0.0):
    """A flight: small chevrons strung across the sky."""
    for i in range(n):
        bx, bz = x + i * 0.9 + rng.uniform(-0.2, 0.2), z + abs(i - n / 2) * 0.25 + rng.uniform(-0.1, 0.1)
        stroke("%s_%da" % (name, i), bx - 0.25, bz - 0.08, bx, bz, 0.03, material, y=y)
        stroke("%s_%db" % (name, i), bx, bz, bx + 0.25, bz - 0.08, 0.03, material, y=y)


PAPER_MAT = [None]   # the paper's wash for this layer, set by each layer for the shapes that cut back to it


# ---------------------------------------------------------------- layers

def layer_fore_grass(rng, faded):
    """Paper_Fore_Grass: long grass in front of the walk: dark, close, a thick line, leaning east."""
    p = Palette(0.0, faded)
    dark, darker = p("dark", lerp(STRAW, INK, 0.45)), p("darker", lerp(STRAW, INK, 0.65))
    grass_band(rng, "g", -41, 41, -0.8, 1.3, 0.5, 90, dark, y=-0.3)
    grass_band(rng, "h", -41, 41, -0.8, 0.8, 0.3, 60, darker, y=-0.4)
    return STRIP_WIDTH, 2.0, PPU, -0.8, 3.0, p.ink()


def layer_mid_stones(rng, faded):
    """Paper_Mid_Stones: the Nine Stones' walk: standing stones in a line across the grass, lichen on their north
    faces, the clans' notches, long shadows east of each."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grass, stone, lichen, dark, shade = p("grass", STRAW), p("stone", STONE), p("lichen", LICHEN), p("dark", lerp(VIOLET, INK, 0.4)), p("shade", lerp(VIOLET, PAPER, 0.6))
    ridge("turf", rng, -40, 40, 0.0, 0.5, 0.15, 40, grass, y=0.5)
    for k, x in enumerate((-31, -15, 1, 17, 33)):
        h = rng.uniform(2.8, 4.0)
        shadow("sh_%d" % k, x + 0.4, 0.45, 0.9, 5.0, shade, y=0.45)
        standing_stone("stone_%d" % k, rng, x + rng.uniform(-1, 1), 0.4, rng.uniform(0.9, 1.3), h, stone, lichen, dark, y=0.2 + 0.01 * k, tilt=rng.uniform(-0.15, 0.15), notches=rng.randint(3, 7))
    grass_band(rng, "g", -41, 41, 0.3, 1.1, 0.4, 70, p("blade", lerp(STRAW, INK, 0.2)), y=0.0)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_camp(rng, faded):
    """Paper_Mid_Camp: the Long Grass Camp: walking-wagons in a ring, the fire in the middle, the route woven on a
    wagon's cloth, smoke going east."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grass, wood, cloth, dark, stone, flame, glow, smk, band = (p("grass", STRAW), p("wood", WOOD), p("cloth", lerp(PAPER, STRAW, 0.25)), p("dark", lerp(WOOD, INK, 0.5)),
                                                             p("stone", STONE), p("flame", GOLD), p("glow", lerp(GOLD, PAPER, 0.5)), p("smoke", lerp(VIOLET, PAPER, 0.7)), p("band", lerp(GOLD, STRAW, 0.4)))
    ridge("turf", rng, -40, 40, 0.0, 0.5, 0.15, 40, grass, y=0.5)
    for k, x in enumerate((-30, -14, 9, 24)):
        wagon("wagon_%d" % k, rng, x, 0.4, rng.uniform(4.5, 5.5), 3.2, wood, cloth, dark, y=0.2 + 0.01 * k, cloth_band=band if k == 1 else None)
    fire("fire", rng, -2, 0.45, 1.6, stone, flame, glow, dark, y=0.1)
    smoke(rng, "smoke", -1.8, 1.2, 7, smk, y=0.3)
    grass_band(rng, "g", -41, 41, 0.3, 1.0, 0.3, 60, p("blade", lerp(STRAW, INK, 0.2)), y=0.0)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_river(rng, faded):
    """Paper_Mid_River: the Dry River: the far bank of cracked mud, boats on their sides with names on their bows,
    dead reeds, the cut bank rising at the east."""
    p = Palette(0.15, faded)
    mud, crack, wood, dark, stalk, head, bank, grass = (p("mud", lerp(STRAW, PAPER, 0.4)), p("crack", lerp(STRAW, INK, 0.5)), p("wood", WOOD), p("dark", lerp(WOOD, INK, 0.5)),
                                                        p("stalk", lerp(STRAW, INK, 0.3)), p("head", lerp(STRAW, INK, 0.5)), p("bank", lerp(STONE, STRAW, 0.4)), p("grass", STRAW))
    polygon("bed", [(-40, 0), (40, 0), (40, 1.4), (-40, 1.2)], mud, y=0.5)
    cracks(rng, "cr", -40, 40, 0.1, 1.2, 70, crack, y=0.45)
    ridge("farbank", rng, -40, 40, 1.2, 0.6, 0.2, 40, grass, y=0.4)
    for k, x in enumerate((-26, -6, 16)):
        hull("boat_%d" % k, rng, x, 0.3, rng.uniform(3.5, 5.0), rng.uniform(1.2, 1.7), wood, dark, p("paper", PAPER), y=0.2 + 0.01 * k)
    reeds(rng, "reeds_a", -40, -32, 0.6, 12, stalk, head, y=0.1)
    reeds(rng, "reeds_b", 2, 10, 0.6, 10, stalk, head, y=0.1)
    polygon("cut", [(28, 0), (40, 0), (40, 6), (31, 6)], bank, y=0.3)
    for i in range(6):
        stroke("cut_s%d" % i, 29 + i * 0.3, 0.8 + i * 0.85, 40, 1.0 + i * 0.85, 0.04, crack, y=0.28)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_cliff(rng, faded):
    """Paper_Mid_Cliff: the cut bank's face, twelve units of it: strata, talon grooves, the lip's grass at the top."""
    p = Palette(0.15, faded)
    stone, dark, groove, grass = p("stone", lerp(STONE, STRAW, 0.3)), p("dark", lerp(VIOLET, INK, 0.4)), p("groove", lerp(VIOLET, INK, 0.2)), p("grass", STRAW)
    cliff(rng, "face", -40, 40, 0.0, 11.2, stone, dark, groove, y=0.4)
    ridge("lip", rng, -40, 40, 11.1, 0.5, 0.15, 40, grass, y=0.3)
    grass_band(rng, "g", -41, 41, 11.3, 0.6, 0.2, 50, p("blade", lerp(STRAW, INK, 0.2)), y=0.2)
    return STRIP_WIDTH, 12.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_windgate(rng, faded):
    """Paper_Mid_WindGate: the Wind Gate: the lip of the cliff with its flat carved stones, two tall stones leaning
    toward each other, and the wind itself drawn as ink-swirls between and beyond them."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grass, stone, lichen, dark, flat, ink, swl = (p("grass", STRAW), p("stone", STONE), p("lichen", LICHEN), p("dark", lerp(VIOLET, INK, 0.4)),
                                                 p("flat", lerp(STONE, PAPER, 0.3)), p("ink", INK), p("swirl", lerp(VIOLET, INK, 0.2)))
    ridge("turf", rng, -40, 40, 0.0, 0.5, 0.15, 40, grass, y=0.5)
    for i in range(9):
        x = -14 + i * 3.4
        box("flat_%d" % i, x, 0.55, 2.0, 0.3, flat, y=0.3)
        for k in range(3):
            stroke("carve_%d_%d" % (i, k), x - 0.6 + k * 0.45, 0.5, x - 0.3 + k * 0.45, 0.62, 0.02, dark, y=0.25)
    standing_stone("gate_w", rng, -5.5, 0.4, 1.6, 6.4, stone, lichen, dark, y=0.2, tilt=0.6, notches=5)
    standing_stone("gate_e", rng, 5.5, 0.4, 1.6, 6.0, stone, lichen, dark, y=0.21, tilt=-0.6, notches=4)
    for k, x in enumerate((0, 14, 26, -26, -36, 36)):
        swirl("wind_%d" % k, x, 0.8, rng.uniform(5.0, 7.5), rng.uniform(1.4, 2.2), swl, y=0.1 + 0.01 * k, turns=rng.uniform(2.5, 3.5), phase=rng.uniform(0, 6))
    grass_band(rng, "g", -41, 41, 0.3, 1.0, 0.3, 60, p("blade", lerp(STRAW, INK, 0.2)), y=0.0)
    return STRIP_WIDTH, 8.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_highgrass(rng, faded):
    """Paper_Mid_HighGrass: the high grass past the Gate: grass over anyone's head, seed-heads, a trampled way
    through the middle where the clan walked her in."""
    p = Palette(0.15, faded)
    grass, dark, head = p("grass", STRAW), p("dark", lerp(STRAW, INK, 0.3)), p("head", lerp(GOLD, STRAW, 0.4))
    ridge("turf", rng, -40, 40, 0.0, 0.5, 0.15, 40, grass, y=0.5)
    for i in range(140):
        x = -41 + i * 0.59 + rng.uniform(-0.2, 0.2)
        trampled = -6 < x < 6
        h = rng.uniform(1.6, 2.4) if trampled else rng.uniform(3.6, 5.6)
        tussock("t_%d" % i, rng, x, 0.3, rng.uniform(0.5, 1.0), h, dark if i % 3 == 0 else grass, lean=LEAN * (0.6 if trampled else 1.0), y=0.3 - 0.002 * i, n=7)
        if not trampled and rng.random() < 0.5:
            disc("head_%d" % i, x + LEAN * h * 0.95, 0.3 + h * 0.98, rng.uniform(0.08, 0.14), head, y=0.1 - 0.002 * i, n=8)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_hearth(rng, faded):
    """Paper_Mid_Hearth: Idrenne's Fire: the hearth in its ring of stones, the cooking-stone flat on the fire, a pot
    on a tripod, the clan's wagons either side, smoke."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grass, wood, cloth, dark, stone, flame, glow, smk, key, iron = (p("grass", STRAW), p("wood", WOOD), p("cloth", lerp(PAPER, STRAW, 0.25)), p("dark", lerp(WOOD, INK, 0.5)),
                                                                   p("stone", STONE), p("flame", GOLD), p("glow", lerp(GOLD, PAPER, 0.5)), p("smoke", lerp(VIOLET, PAPER, 0.7)), p("key", lerp(VIOLET, INK, 0.3)), p("iron", IRON))
    ridge("turf", rng, -40, 40, 0.0, 0.5, 0.15, 40, grass, y=0.5)
    for k, x in enumerate((-32, -18, 16, 30)):
        wagon("wagon_%d" % k, rng, x, 0.4, rng.uniform(4.5, 5.5), 3.2, wood, cloth, dark, y=0.2 + 0.01 * k)
    fire("hearth", rng, -2, 0.45, 2.6, stone, flame, glow, dark, y=0.1)
    box("keystone", -2, 0.95, 1.2, 0.22, key, y=0.0)
    for i in range(3):
        a = -0.5 + i * 0.5
        stroke("leg_%d" % i, -2 + a * 1.4, 0.5, -2 + a * 0.2, 2.6, 0.05, iron, y=0.05)
    stroke("chain", -2, 2.5, -2, 1.9, 0.03, iron, y=0.0)
    disc("pot", -2, 1.6, 0.45, iron, y=-0.02, n=14, squash=0.7)
    smoke(rng, "smoke", -1.6, 1.8, 7, smk, y=0.3)
    grass_band(rng, "g", -41, 41, 0.3, 1.0, 0.3, 60, p("blade", lerp(STRAW, INK, 0.2)), y=0.0)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_crater(rng, faded):
    """Paper_Mid_Crater: the anvil-crater: the iron wall the Star threw up, the Star itself at the middle, forty years
    of hammer-marks, the smiths' wagon, sparks."""
    p = Palette(0.15, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    iron, dark, wall, wood, cloth, spark, cinder, heat = (p("iron", IRON), p("dark", lerp(IRON, INK, 0.5)), p("wall", lerp(IRON, STONE, 0.5)), p("wood", WOOD), p("cloth", lerp(PAPER, STRAW, 0.25)),
                                                          p("spark", GOLD), p("cinder", lerp(VIOLET, INK, 0.3)), p("heat", lerp(GOLD, PAPER, 0.75)))
    polygon("wall", [(-40, 0), (40, 0), (40, 5.5), (26, 3.2), (12, 2.4), (-12, 2.4), (-26, 3.2), (-40, 5.5)], wall, y=0.5)
    for i in range(24):
        x = rng.uniform(-40, 40)
        stroke("seam_%d" % i, x, rng.uniform(0.3, 2.0), x + rng.uniform(1, 4), rng.uniform(0.3, 2.0), 0.03, dark, y=0.45)
    ridge("cinders", rng, -40, 40, 0.0, 0.5, 0.2, 40, cinder, y=0.4)
    blob("star", rng, 0, 1.4, 2.6, 1.3, iron, y=0.2, n=18, wobble=0.18)
    box("anvil_top", 0, 2.55, 2.0, 0.25, dark, y=0.15)
    for i in range(14):
        stroke("mark_%d" % i, rng.uniform(-2, 2), rng.uniform(0.6, 2.3), rng.uniform(-2, 2), rng.uniform(0.6, 2.3), 0.025, dark, y=0.1)
    for i in range(5):
        blob("heat_%d" % i, rng, rng.uniform(-3, 3), 2.8 + i * 0.5, 1.2 + i * 0.3, 0.3, heat, y=0.3 + 0.01 * i, wobble=0.4)
    for i in range(12):
        disc("spark_%d" % i, rng.uniform(-3.5, 3.5), rng.uniform(2.6, 5.0), rng.uniform(0.04, 0.08), spark, y=0.05, n=6)
    wagon("smiths", rng, -24, 0.4, 5.5, 3.2, wood, cloth, dark, y=0.2)
    for i in range(4):
        box("tool_%d" % i, -19 + i * 0.5, 1.2, 0.08, rng.uniform(1.0, 1.6), dark, y=0.1)
    return STRIP_WIDTH, 8.0, PPU, 0.0, 2.0, p.ink()


def layer_far_steppe(rng, faded):
    """Paper_Far_Steppe: grass to the horizon: pale bands of it, the stones' line going away small, far wagons on the
    walk, every shadow long."""
    p = Palette(0.45, faded)
    PAPER_MAT[0] = p("paper", PAPER)
    grass, pale, stone, shade, wood, cloth, dark = (p("grass", STRAW), p("pale", STRAW, 0.15), p("stone", STONE), p("shade", lerp(VIOLET, PAPER, 0.5)), p("wood", WOOD), p("cloth", lerp(PAPER, STRAW, 0.25)), p("dark", lerp(WOOD, INK, 0.5)))
    polygon("horizon", [(-40, 2), (40, 2), (40, 6.4), (-40, 6.2)], pale, y=0.6)
    for k in range(5):
        ridge("band_%d" % k, rng, -40, 40, 2.0 + k * 0.8, 0.6, 0.2, 30, grass if k % 2 else pale, y=0.55 - 0.01 * k)
    for k in range(9):
        x = -34 + k * 8.5 + rng.uniform(-1.5, 1.5)
        s = 1.0 - k * 0.07
        shadow("sh_%d" % k, x + 0.2, 2.6 + k * 0.25, 0.4 * s, 2.5 * s, shade, y=0.5)
        standing_stone("st_%d" % k, rng, x, 2.6 + k * 0.25, 0.5 * s, 1.6 * s, stone, p("lichen", LICHEN), p("sdark", lerp(VIOLET, INK, 0.3)), y=0.4 - 0.01 * k, tilt=rng.uniform(-0.05, 0.05))
    for k, x in enumerate((-20, 14)):
        wagon("wagon_%d" % k, rng, x, 4.6, 2.2, 1.4, wood, cloth, dark, y=0.3 + 0.01 * k)
    grass_band(rng, "g", -41, 41, 2.0, 0.5, 0.2, 60, grass, y=0.35)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_far_rim(rng, faded):
    """Paper_Far_Rim: the crater's far rim and the cut bank's heights, pale violet, grass along their tops."""
    p = Palette(0.45, faded)
    rock, dark, grass = p("rock", lerp(STONE, VIOLET, 0.3)), p("dark", lerp(VIOLET, INK, 0.2)), p("grass", STRAW)
    pts = [(-40, 2), (40, 2)]
    for i in range(16, -1, -1):
        x = -40 + i * 5
        pts.append((x, 5.5 + 2.5 * math.sin(i * 0.9) + rng.uniform(-0.6, 0.6)))
    polygon("rim", pts, rock, y=0.5)
    for i in range(30):
        x = rng.uniform(-40, 40)
        stroke("st_%d" % i, x, rng.uniform(2.5, 5.5), x + rng.uniform(2, 5), rng.uniform(2.5, 5.8), 0.04, dark, y=0.45)
    grass_band(rng, "g", -41, 41, 2.0, 0.6, 0.2, 50, grass, y=0.35)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_farther_storm(rng, faded):
    """Paper_Farther_Storm: sky that is most of the screen: storm violet banked low on the horizon, a long gold light
    under the cloud, high clouds nearly paper, a flight going east."""
    p = Palette(0.6, faded)
    sky, storm, light, cld, bird = p("sky", SKY, 0.0), p("storm", VIOLET, 0.05), p("light", lerp(GOLD, PAPER, 0.6)), p("cloud", PAPER, 0.05), p("bird", INK, 0.1)
    polygon("sky", [(-40, 6), (40, 6), (40, 22), (-40, 22)], sky, y=0.9)
    polygon("light", [(-40, 6), (40, 6), (40, 8.5), (-40, 9.5)], light, y=0.8)
    for i in range(6):
        blob("storm_%d" % i, rng, -40 + i * 16 + rng.uniform(-4, 4), rng.uniform(8.5, 11.5), rng.uniform(8, 14), rng.uniform(1.0, 2.0), storm, y=0.7 - 0.01 * i, wobble=0.25)
    for i in range(7):
        cloud(rng, "cloud_%d" % i, rng.uniform(-40, 40), rng.uniform(14, 20), rng.uniform(5, 9), rng.uniform(0.8, 1.4), cld, y=0.5)
    birds(rng, "flight", -12, 16.5, 9, bird, y=0.3)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.3, p.ink()


def tile_turf(rng, faded):
    """Ground_Turf: the Steppe's ground: packed earth with grass along the top, every blade leaning east."""
    p = Palette(0.0, faded)
    earth, dark, grass = p("earth", lerp(STRAW, INK, 0.25)), p("dark", lerp(STRAW, INK, 0.5)), p("grass", STRAW)
    box("bed", 0, 0.5, 4.0, 1.0, earth, y=0.2)
    for i in range(10):
        disc("pebble_%d" % i, rng.uniform(-2, 2), rng.uniform(0.1, 0.6), rng.uniform(0.03, 0.07), dark, y=0.1, n=8)
    ridge("sod", rng, -2, 2, 0.78, 0.18, 0.05, 12, grass, y=0.05)
    for i in range(14):
        blade("b_%d" % i, -2 + 4 * (i + rng.random()) / 14, 0.9, rng.uniform(0.08, 0.14), dark, y=0.0, w=0.04)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_cracked(rng, faded):
    """Ground_Cracked: the Dry River's bed: mud cracked into plates, a dead reed root here and there."""
    p = Palette(0.0, faded)
    mud, crack, root = p("mud", lerp(STRAW, PAPER, 0.35)), p("crack", lerp(STRAW, INK, 0.55)), p("root", lerp(STRAW, INK, 0.35))
    box("bed", 0, 0.5, 4.0, 1.0, mud, y=0.2)
    cracks(rng, "cr", -2, 2, 0.15, 0.95, 9, crack, y=0.05)
    for i in range(3):
        stroke("root_%d" % i, rng.uniform(-2, 2), 1.0, rng.uniform(-2, 2), 0.7, 0.03, root, y=0.0)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_lip(rng, faded):
    """Ground_Lip: the Wind Gate's lip: flat stones laid close, carved, lichen in the joints."""
    p = Palette(0.0, faded)
    stone, joint, lichen, carve = p("stone", lerp(STONE, PAPER, 0.2)), p("joint", lerp(VIOLET, INK, 0.3)), p("lichen", LICHEN), p("carve", lerp(VIOLET, INK, 0.1))
    box("bed", 0, 0.5, 4.0, 1.0, joint, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.7, 1.3)
        box("flag_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.5, w - 0.05, 0.9, stone, y=0.0)
        for k in range(2):
            stroke("carve_%d_%d" % (i, k), x + 0.15 + k * 0.25, 0.75, x + 0.3 + k * 0.25, 0.85, 0.02, carve, y=-0.05)
        blob("lichen_%d" % i, rng, x + w, 0.3, 0.06, 0.1, lichen, y=-0.05, n=8)
        x += w
        i += 1
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_cinder(rng, faded):
    """Ground_Cinder: the crater's floor: cinders and iron flecks, a glow in the cracks."""
    p = Palette(0.0, faded)
    cinder, iron, glow = p("cinder", lerp(VIOLET, INK, 0.35)), p("iron", IRON), p("glow", lerp(GOLD, PAPER, 0.2))
    box("bed", 0, 0.5, 4.0, 1.0, cinder, y=0.2)
    for i in range(12):
        blob("fleck_%d" % i, rng, rng.uniform(-2, 2), rng.uniform(0.1, 0.9), rng.uniform(0.06, 0.16), rng.uniform(0.04, 0.1), iron, y=0.05, n=8, wobble=0.4)
    for i in range(4):
        stroke("glow_%d" % i, rng.uniform(-2, 2), rng.uniform(0.2, 0.8), rng.uniform(-2, 2), rng.uniform(0.2, 0.8), 0.025, glow, y=0.0)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Fore_Grass", "strip", layer_fore_grass, False),
    ("Paper_Mid_Stones", "strip", layer_mid_stones, False),
    ("Paper_Mid_Camp", "strip", layer_mid_camp, False),
    ("Paper_Mid_River", "strip", layer_mid_river, False),
    ("Paper_Mid_Cliff", "strip", layer_mid_cliff, False),
    ("Paper_Mid_WindGate", "strip", layer_mid_windgate, False),
    ("Paper_Mid_HighGrass", "strip", layer_mid_highgrass, False),
    ("Paper_Mid_Hearth", "strip", layer_mid_hearth, False),
    ("Paper_Mid_Crater", "strip", layer_mid_crater, False),
    ("Paper_Far_Steppe", "strip", layer_far_steppe, False),
    ("Paper_Far_Rim", "strip", layer_far_rim, False),
    ("Paper_Farther_Storm", "strip", layer_farther_storm, False),
    ("Ground_Turf", "tile", tile_turf, False),
    ("Ground_Cracked", "tile", tile_cracked, False),
    ("Ground_Lip", "tile", tile_lip, False),
    ("Ground_Cinder", "tile", tile_cinder, False),
]


def main():
    run_kit(OUT, "Windreach", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

"""Emberdown paper kit (ENV-03): every backdrop strip and ground tile the highland's rooms use, rendered from
cut-out geometry with Freestyle ink lines in headless Blender, on the shared plumbing in kitlib.py.

Run from the repo root:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/emberdown.py
    ... -P tools/paperkit/emberdown.py -- Paper_Far_Chimneys Ground_Basalt      (only those layers)

Writes PNGs (straight alpha) and kit.json to LastCartographer/Assets/_Project/Art/Environment/Emberdown/.
The bible (4.2): black basalt highland, red-lit mine mouths, hot springs steaming in cold air, ash falling like
slow snow, roosts cut into cliff faces. Charcoal, ember orange, sulphur, on smoke-grey paper, in black ink
(art-direction 5). Farther layers wash toward the paper and draw thinner, as on the coast.
"""
import bpy, json, math, os, random, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitlib import (lerp, reset_scene, polygon, box, ridge, disc, blob, run_kit, Palette as _Palette)

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Environment", "Emberdown")

# Emberdown palette (art-direction 5): paper, wash 1, wash 2, accent, ink. The paper is ProjectSetup.RegionPaper's.
PAPER = (0.82, 0.80, 0.78)
CHARCOAL = (0.24, 0.23, 0.24)
SULPHUR = (0.76, 0.66, 0.30)
EMBER = (0.88, 0.44, 0.16)
INK = (0.04, 0.04, 0.05)
BASALT = (0.15, 0.14, 0.15)

PPU = 40
TILE_PPU = 96
STRIP_WIDTH = 80
FADED_WASH = 0.3
FADED_LINE = 0.6


class Palette(_Palette):
    def __init__(self, depth, faded):
        super().__init__(PAPER, INK, depth, faded, FADED_WASH)


# ---------------------------------------------------------------- shapes

def cliff(name, rng, x0, x1, base_z, top_z, material, y=0.0, jag=0.5):
    """A basalt face: a filled wall with a broken top edge and a foot that steps."""
    pts = [(x0, base_z)]
    n = int((x1 - x0) / 1.6)
    for i in range(n + 1):
        t = i / max(1, n)
        x = x0 + (x1 - x0) * t
        pts.append((x, top_z + rng.uniform(-jag, jag) + 0.3 * math.sin(t * 9.0)))
    pts.append((x1, base_z))
    return polygon(name, pts, material, y=y)


def roost_cut(name, x, z, w, h, dark, ash, ladder=None, y=0.0):
    """A roost cut into the cliff: a square-topped doorway, a lintel, ash on its sill; a ladder if asked."""
    polygon(name + "_door", [(x - w / 2, z), (x + w / 2, z), (x + w / 2, z + h * 0.8), (x + w * 0.3, z + h), (x - w * 0.3, z + h), (x - w / 2, z + h * 0.8)], dark, y=y - 0.1)
    box(name + "_lintel", x, z + h + 0.08, w * 1.3, 0.16, dark, y=y - 0.05)
    box(name + "_sill", x, z - 0.06, w * 1.2, 0.12, ash, y=y - 0.12)
    if ladder is not None:
        lx = x + w * 0.75
        for sz in (0.05, 0.35):
            box(name + "_rail%d" % int(sz * 10), lx + (sz - 0.2) * 1.0, z - ladder / 2, 0.06, ladder, dark, y=y - 0.08)
        for i in range(int(ladder / 0.35)):
            box(name + "_rung%d" % i, lx, z - ladder + 0.2 + i * 0.35, 0.36, 0.05, dark, y=y - 0.09)


def stack(name, x, base_z, w, h, walls, dark, y=0.0):
    """A mine chimney: a tapered stack with a cap and iron bands."""
    top_w = w * 0.7
    polygon(name, [(x - w / 2, base_z), (x + w / 2, base_z), (x + top_w / 2, base_z + h), (x - top_w / 2, base_z + h)], walls, y=y)
    box(name + "_cap", x, base_z + h + 0.1, top_w * 1.25, 0.2, dark, y=y - 0.05)
    for i in range(1, 4):
        box(name + "_band%d" % i, x, base_z + h * i / 4, w - (w - top_w) * i / 4 + 0.02, 0.07, dark, y=y - 0.02)


def smoke(name, rng, x, z, n, mat, y=0.0):
    """A plume: rounds drifting up and east, growing and thinning."""
    for i in range(n):
        t = i / max(1, n - 1)
        blob(name + "_%d" % i, rng, x + t * 2.6 + rng.uniform(-0.2, 0.2), z + t * 3.2, 0.5 + t * 0.9, 0.35 + t * 0.6, mat, y=y + 0.02 * i, wobble=0.3)


def headframe(name, x, base_z, w, h, dark, y=0.0):
    """A mine head-frame: two legs, a crossbeam, the wheel."""
    polygon(name + "_l", [(x - w / 2, base_z), (x - w / 2 + 0.18, base_z), (x - 0.15, base_z + h), (x - 0.3, base_z + h)], dark, y=y)
    polygon(name + "_r", [(x + w / 2 - 0.18, base_z), (x + w / 2, base_z), (x + 0.3, base_z + h), (x + 0.15, base_z + h)], dark, y=y)
    box(name + "_beam", x, base_z + h * 0.62, w * 0.8, 0.14, dark, y=y - 0.02)
    disc(name + "_wheel", x, base_z + h + 0.15, 0.45, dark, y=y - 0.05, n=18)
    disc(name + "_hub", x, base_z + h + 0.15, 0.14, dark, y=y - 0.08, n=10)


def mouth(name, x, z, w, h, dark, glow, y=0.0):
    """A mine mouth or furnace door: a dark arch with the ember glow inside it."""
    polygon(name + "_arch", [(x - w / 2, z), (x + w / 2, z), (x + w / 2, z + h * 0.6), (x + w * 0.25, z + h), (x - w * 0.25, z + h), (x - w / 2, z + h * 0.6)], dark, y=y)
    polygon(name + "_glow", [(x - w * 0.3, z), (x + w * 0.3, z), (x + w * 0.3, z + h * 0.45), (x + w * 0.12, z + h * 0.72), (x - w * 0.12, z + h * 0.72), (x - w * 0.3, z + h * 0.45)], glow, y=y - 0.08)


def roof_row(name, rng, x0, x1, base_z, walls, roof, ash, y=0.0):
    """Roofs of a highland town: low houses with steep gables, ash drifted along the ridges."""
    x = x0
    i = 0
    while x < x1:
        w = rng.uniform(1.8, 3.2)
        h = rng.uniform(1.0, 1.7)
        box("%s_h%d" % (name, i), x + w / 2, base_z + h / 2, w, h, walls, y=y)
        polygon("%s_r%d" % (name, i), [(x - 0.2, base_z + h), (x + w + 0.2, base_z + h), (x + w / 2, base_z + h + w * 0.45)], roof, y=y - 0.05)
        box("%s_a%d" % (name, i), x + w / 2, base_z + h + w * 0.45 + 0.04, w * 0.35, 0.09, ash, y=y - 0.1)
        if rng.random() < 0.5:
            box("%s_c%d" % (name, i), x + w * rng.uniform(0.3, 0.7), base_z + h + w * 0.3, 0.22, 0.6, walls, y=y - 0.07)
        x += w + rng.uniform(0.3, 1.2)
        i += 1


def ash_fall(rng, n, x0, x1, z0, z1, mat, y=0.0):
    """Ash falling like slow snow: small pale flecks."""
    for i in range(n):
        disc("ash_%d" % i, rng.uniform(x0, x1), rng.uniform(z0, z1), rng.uniform(0.04, 0.1), mat, y=y, n=6)


def bell_tower(name, x, base_z, h, dark, walls, brass, y=0.0):
    """The Roll-Call Bell's tower: an open timber frame on a stone foot, the bell hung in it, a peaked cap."""
    box(name + "_foot", x, base_z + 0.6, 2.2, 1.2, walls, y=y)
    for dx in (-0.8, 0.8):
        polygon(name + "_leg%d" % int(dx * 10), [(x + dx - 0.12, base_z + 1.2), (x + dx + 0.12, base_z + 1.2), (x + dx * 0.7 + 0.1, base_z + h), (x + dx * 0.7 - 0.1, base_z + h)], dark, y=y - 0.02)
    box(name + "_beam", x, base_z + h, 1.9, 0.16, dark, y=y - 0.04)
    polygon(name + "_cap", [(x - 1.3, base_z + h + 0.08), (x + 1.3, base_z + h + 0.08), (x, base_z + h + 1.1)], dark, y=y - 0.06)
    bz = base_z + h - 0.3
    polygon(name + "_bell", [(x - 0.16, bz), (x + 0.16, bz), (x + 0.26, bz - 0.5), (x + 0.5, bz - 0.95), (x - 0.5, bz - 0.95), (x - 0.26, bz - 0.5)], brass, y=y - 0.1)
    disc(name + "_clapper", x, bz - 1.05, 0.09, dark, y=y - 0.12, n=8)


# ---------------------------------------------------------------- layers

def layer_fore_slag(rng, faded):
    """Paper_Fore_Slag: cinder and slag heaps in front of the walkway, ember flecks still in them."""
    p = Palette(0.0, faded)
    slag, cinder, ember = p("slag", BASALT), p("cinder", CHARCOAL), p("ember", EMBER)
    for k, x in enumerate(range(-40, 40, 5)):
        ridge("heap_%d" % k, rng, x - 0.5, x + 4.5 + rng.uniform(-1, 1), -0.8, 0.9, 0.35, 6, slag if k % 2 == 0 else cinder, y=-0.3 - 0.05 * (k % 3))
    for i in range(40):
        disc("ember_%d" % i, rng.uniform(-40, 40), rng.uniform(-0.6, 0.3), rng.uniform(0.04, 0.09), ember, y=-0.5, n=6)
    return STRIP_WIDTH, 1.6, PPU, -0.8, 3.0, p.ink()


def layer_mid_roosts(rng, faded):
    """Paper_Mid_Roosts: the cliff behind the town, roosts cut into it on ledges, ladders between, ash on every sill."""
    p = Palette(0.15, faded)
    face, dark, ash, ledge = p("face", CHARCOAL), p("dark", BASALT), p("ash", lerp(PAPER, SULPHUR, 0.2)), p("ledge", lerp(CHARCOAL, BASALT, 0.5))
    cliff("cliff", rng, -40, 40, 0.0, 5.6, face, y=0.3, jag=0.4)
    for k in range(9):
        x = -36 + k * 9 + rng.uniform(-1.5, 1.5)
        z = rng.choice((0.6, 2.2, 3.6))
        roost_cut("roost_%d" % k, x, z, rng.uniform(0.9, 1.3), rng.uniform(1.2, 1.6), dark, ash, ladder=(z if z > 1 and rng.random() < 0.6 else None), y=0.0)
        box("ledge_%d" % k, x + 0.4, z - 0.16, rng.uniform(2.5, 4.5), 0.22, ledge, y=0.05)
    for i in range(24):
        box("crack_%d" % i, rng.uniform(-40, 40), rng.uniform(0.5, 5.0), 0.08, rng.uniform(0.4, 1.4), dark, y=0.1)
    ash_fall(rng, 60, -40, 40, 0.2, 6.0, ash, y=-0.2)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_chimneys(rng, faded):
    """Paper_Far_Chimneys: the nine chimneys on the skyline, head-frames between them, mine mouths lit red, smoke going east."""
    p = Palette(0.45, faded)
    walls, dark, glow, plume, ground = p("walls", CHARCOAL), p("dark", BASALT), p("glow", EMBER), p("plume", lerp(PAPER, CHARCOAL, 0.25)), p("ground", lerp(CHARCOAL, PAPER, 0.2))
    ridge("ground", rng, -40, 40, 2.0, 1.4, 0.4, 30, ground, y=0.4)
    xs = sorted(rng.uniform(-37, 37) for _ in range(9))
    for k, x in enumerate(xs):
        h = rng.uniform(3.5, 6.5)
        stack("stack_%d" % k, x, 3.0, rng.uniform(0.9, 1.4), h, walls, dark, y=0.1)
        if k != 8:
            smoke("smoke_%d" % k, rng, x, 3.0 + h + 0.3, 5, plume, y=0.5)
    for k in range(3):
        x = rng.uniform(-30, 30)
        headframe("frame_%d" % k, x, 3.0, 2.4, 3.2, dark, y=0.0)
        mouth("mouth_%d" % k, x + rng.uniform(-3, 3), 3.0, 1.6, 1.4, dark, glow, y=-0.05)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_farther_ridge(rng, faded):
    """Paper_Farther_Ridge: the basalt highland behind everything, two ridges nearly paper, ash in the air."""
    p = Palette(0.6, faded)
    near, far, ash = p("near", CHARCOAL), p("far", CHARCOAL, 0.15), p("ash", PAPER, 0.05)
    ridge("far", rng, -40, 40, 6.0, 9.5, 1.6, 24, far, y=0.6)
    ridge("near", rng, -40, 40, 6.0, 6.0, 1.8, 30, near, y=0.3)
    ash_fall(rng, 50, -40, 40, 8.0, 21.0, ash, y=-0.2)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.3, p.ink()


def layer_mid_furnaces(rng, faded):
    """Paper_Mid_Furnaces: the furnace stair's furnaces: iron fronts, glowing doors, pipes up the wall, a landing rail."""
    p = Palette(0.15, faded)
    wall, iron, glow, rail = p("wall", CHARCOAL), p("iron", BASALT), p("glow", EMBER), p("rail", lerp(BASALT, PAPER, 0.15))
    cliff("wall", rng, -40, 40, 0.0, 5.8, wall, y=0.3, jag=0.2)
    for k in range(7):
        x = -36 + k * 12 + rng.uniform(-1, 1)
        box("front_%d" % k, x, 1.6, 4.2, 3.2, iron, y=0.05)
        mouth("door_%d" % k, x, 0.4, 1.8, 1.6, iron, glow, y=-0.35)   # in front of the iron, or the box hides it
        for i in range(3):
            box("rivet_%d_%d" % (k, i), x - 1.6 + i * 1.6, 3.0, 0.14, 0.14, rail, y=-0.08)
        box("pipe_%d" % k, x + 1.5, 4.4, 0.35, 2.6, iron, y=0.0)
        box("elbow_%d" % k, x + 1.5, 5.6, 1.2, 0.35, iron, y=0.0)
    box("rail", 0, 0.9, 80, 0.08, rail, y=-0.15)
    for i in range(41):
        box("post_%d" % i, -40 + i * 2.0, 0.45, 0.08, 0.9, rail, y=-0.15)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_mid_springs(rng, faded):
    """Paper_Mid_Springs: the cinder baths: pools of sulphur water in the rock, steam standing over them, boardwalk posts."""
    p = Palette(0.15, faded)
    rock, pool, steam, post = p("rock", CHARCOAL), p("pool", SULPHUR), p("steam", PAPER, 0.08), p("post", BASALT)
    ridge("rock", rng, -40, 40, 0.0, 1.6, 0.5, 40, rock, y=0.3)
    for k in range(6):
        x = -33 + k * 13 + rng.uniform(-2, 2)
        w = rng.uniform(4, 7)
        polygon("pool_%d" % k, [(x - w / 2, 0.5), (x + w / 2, 0.5), (x + w / 2 - 0.4, 0.95), (x - w / 2 + 0.4, 0.95)], pool, y=0.1)
        for i in range(4):
            blob("steam_%d_%d" % (k, i), rng, x + rng.uniform(-w / 3, w / 3), 1.6 + i * 1.1, 0.8 + i * 0.35, 0.6 + i * 0.3, steam, y=-0.1 - 0.02 * i, wobble=0.35)
    for i in range(20):
        box("post_%d" % i, -38 + i * 4 + rng.uniform(-0.5, 0.5), 0.9, 0.16, 1.2, post, y=-0.2)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_bell(rng, faded):
    """Paper_Far_Bell: Kettil's Rest from below: a row of ashed roofs, the bell tower over them, chimneys smoking."""
    p = Palette(0.45, faded)
    walls, roof, ash, dark, brass, plume = p("walls", CHARCOAL), p("roof", BASALT), p("ash", lerp(PAPER, SULPHUR, 0.15)), p("dark", BASALT), p("brass", lerp(SULPHUR, EMBER, 0.3)), p("plume", lerp(PAPER, CHARCOAL, 0.25))
    ridge("ground", rng, -40, 40, 2.0, 1.2, 0.3, 30, p("ground", lerp(CHARCOAL, PAPER, 0.25)), y=0.4)
    roof_row("west", rng, -38, -4, 3.0, walls, roof, ash, y=0.1)
    roof_row("east", rng, 4, 38, 3.0, walls, roof, ash, y=0.1)
    bell_tower("bell", 0.0, 3.0, 5.2, dark, walls, brass, y=0.0)
    smoke("smoke_a", rng, -22, 6.0, 4, plume, y=0.5)
    smoke("smoke_b", rng, 18, 6.2, 4, plume, y=0.5)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.4, p.ink()


def layer_farther_white(rng, faded):
    """Paper_Farther_White: from the Overlook: the Greyfold, a white mass on the horizon with the plateau's edge before it, bigger than it looks."""
    p = Palette(0.6, faded)
    edge, white, grey = p("edge", CHARCOAL, 0.1), p("white", (0.97, 0.96, 0.94)), p("grey", (0.86, 0.85, 0.84))
    ridge("white", rng, -40, 40, 6.0, 11.0, 0.6, 20, white, y=0.7)
    ridge("grey", rng, -40, 40, 6.0, 8.6, 0.9, 20, grey, y=0.5)
    ridge("edge", rng, -40, 40, 6.0, 4.0, 1.2, 30, edge, y=0.3)
    return STRIP_WIDTH, 16.0, PPU, 6.0, 1.0, p.ink()


def layer_mid_gallery(rng, faded):
    """Paper_Mid_Gallery: Hollowvein's gallery wall: timber props and lintels, a lamp for each name, rubble at the foot."""
    p = Palette(0.15, faded)
    wall, timber, glow, rubble = p("wall", BASALT), p("timber", lerp(CHARCOAL, SULPHUR, 0.2)), p("glow", lerp(SULPHUR, EMBER, 0.4)), p("rubble", CHARCOAL)
    cliff("wall", rng, -40, 40, 0.0, 5.9, wall, y=0.3, jag=0.15)
    for k in range(17):
        x = -40 + k * 5 + rng.uniform(-0.4, 0.4)
        box("prop_%d" % k, x, 2.6, 0.32, 5.2, timber, y=0.05)
        box("lintel_%d" % k, x + 2.5, 5.1, 5.2, 0.3, timber, y=0.06)
        if k % 2 == 0:
            box("lamp_%d" % k, x + 0.5, 3.4, 0.28, 0.4, timber, y=-0.05)
            disc("flame_%d" % k, x + 0.5, 3.4, 0.11, glow, y=-0.12, n=8)
    for i in range(30):
        ridge("rubble_%d" % i, rng, rng.uniform(-40, 38), 0, 0.0, rng.uniform(0.2, 0.6), 0.15, 4, rubble, y=-0.2)
    return STRIP_WIDTH, 6.0, PPU, 0.0, 2.0, p.ink()


def layer_far_dark(rng, faded):
    """Paper_Far_Dark: the mine's dark behind the gallery: a charcoal wash, faint beams, a rope going down."""
    p = Palette(0.2, faded)   # the mine's dark is dark: less wash than a far layer usually gets
    dark, beam = p("dark", BASALT), p("beam", CHARCOAL)
    polygon("dark", [(-40, 2.0), (40, 2.0), (40, 12.0), (-40, 12.0)], dark, y=0.6)
    for k in range(6):
        x = -34 + k * 13 + rng.uniform(-2, 2)
        box("beam_%d" % k, x, 7.0, 0.4, 10.0, beam, y=0.3)
    box("cross", 0, 9.5, 80, 0.5, beam, y=0.35)
    return STRIP_WIDTH, 10.0, PPU, 2.0, 1.2, p.ink()


def tile_basalt(rng, faded):
    """Ground_Basalt: black basalt in broken blocks, the cracks lit faintly from below where the heat is."""
    p = Palette(0.0, faded)
    stone, crack, glow = p("stone", BASALT), p("crack", INK), p("glow", lerp(EMBER, BASALT, 0.55))
    box("bed", 0, 0.5, 4.0, 1.0, stone, y=0.2)
    x = -2.0
    while x < 2.0:
        w = rng.uniform(0.5, 1.1)
        box("block_%d" % int((x + 2) * 100), min(x + w / 2, 2.0 - w / 2 + 0.01), 0.55, w - 0.05, 0.85, stone, y=0.0)
        x += w
    for i in range(6):
        box("crack_%d" % i, rng.uniform(-1.9, 1.9), rng.uniform(0.15, 0.85), 0.04, rng.uniform(0.2, 0.5), crack, y=-0.1)
    for i in range(3):
        box("glow_%d" % i, rng.uniform(-1.9, 1.9), 0.08, rng.uniform(0.3, 0.7), 0.05, glow, y=-0.05)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_iron(rng, faded):
    """Ground_Iron: the stair's landings: riveted iron plates, a rust bloom, an edge lip."""
    p = Palette(0.0, faded)
    iron, rivet, rust, lip = p("iron", CHARCOAL), p("rivet", BASALT), p("rust", lerp(EMBER, CHARCOAL, 0.6)), p("lip", BASALT)
    box("plate_a", -1.0, 0.5, 1.96, 1.0, iron, y=0.1)
    box("plate_b", 1.0, 0.5, 1.96, 1.0, iron, y=0.1)
    box("lip", 0, 0.95, 4.0, 0.1, lip, y=0.0)
    for i in range(8):
        disc("rivet_%d" % i, -1.75 + i * 0.5, 0.82, 0.05, rivet, y=-0.05, n=8)
        disc("rivet_lo_%d" % i, -1.75 + i * 0.5, 0.18, 0.05, rivet, y=-0.05, n=8)
    for i in range(3):
        blob("rust_%d" % i, rng, rng.uniform(-1.6, 1.6), rng.uniform(0.3, 0.7), rng.uniform(0.15, 0.35), rng.uniform(0.1, 0.2), rust, y=-0.03, wobble=0.4)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_timber(rng, faded):
    """Ground_Timber: the mine's boards: rough planks across a beam, gaps between, a nail here and there."""
    p = Palette(0.0, faded)
    plank, gap, beam, nail = p("plank", lerp(CHARCOAL, SULPHUR, 0.25)), p("gap", INK), p("beam", CHARCOAL), p("nail", BASALT)
    box("beam", 0, 0.2, 4.0, 0.4, beam, y=0.2)
    x = -2.0
    i = 0
    while x < 2.0:
        w = rng.uniform(0.45, 0.8)
        box("plank_%d" % i, min(x + w / 2, 2.0 - w / 2 + 0.01), 0.68, w - 0.06, 0.62, plank, y=0.0)
        if rng.random() < 0.5:
            disc("nail_%d" % i, x + w / 2, 0.85, 0.03, nail, y=-0.05, n=6)
        x += w
        i += 1
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


def tile_ash(rng, faded):
    """Ground_Ash: the town's stone under a drift of ash: grey blocks, the ash lying pale along the top."""
    p = Palette(0.0, faded)
    stone, ash, joint = p("stone", CHARCOAL), p("ash", lerp(PAPER, SULPHUR, 0.15)), p("joint", BASALT)
    box("bed", 0, 0.45, 4.0, 0.9, stone, y=0.2)
    for i in range(5):
        box("joint_%d" % i, -1.6 + i * 0.8 + rng.uniform(-0.1, 0.1), 0.45, 0.04, 0.9, joint, y=0.0)
    ridge("ash", rng, -2.0, 2.0, 0.78, 0.18, 0.06, 12, ash, y=-0.05)
    return 4.0, 1.0, TILE_PPU, 0.0, 2.0, p.ink()


LAYERS = [
    # name, kind, builder, faded
    ("Paper_Fore_Slag", "strip", layer_fore_slag, False),
    ("Paper_Mid_Roosts", "strip", layer_mid_roosts, False),
    ("Paper_Mid_Furnaces", "strip", layer_mid_furnaces, False),
    ("Paper_Mid_Springs", "strip", layer_mid_springs, False),
    ("Paper_Mid_Gallery", "strip", layer_mid_gallery, False),
    ("Paper_Far_Chimneys", "strip", layer_far_chimneys, False),
    ("Paper_Far_Bell", "strip", layer_far_bell, False),
    ("Paper_Far_Dark", "strip", layer_far_dark, False),
    ("Paper_Farther_Ridge", "strip", layer_farther_ridge, False),
    ("Paper_Farther_White", "strip", layer_farther_white, False),
    ("Ground_Basalt", "tile", tile_basalt, False),
    ("Ground_Iron", "tile", tile_iron, False),
    ("Ground_Timber", "tile", tile_timber, False),
    ("Ground_Ash", "tile", tile_ash, False),
]


def main():
    run_kit(OUT, "Emberdown", PPU, TILE_PPU, PAPER, LAYERS, FADED_LINE)


if __name__ == "__main__":
    main()

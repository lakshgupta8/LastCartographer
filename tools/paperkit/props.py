"""Saltmarrow hub props (ENV-09): the drafting desk, the commissions ledger, the training dummy, Sable's tether stall,
the survey stake, the lamp and its glow, iris seeds, a bound stake, the net rack, Dotha's stoop, a beached boat and a
tether-post, each a single cut-out at the sprite density, drawn with the same ink and palette as the kit's layers.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/paperkit/props.py
    ... -P tools/paperkit/props.py -- Prop_Desk Prop_Lamp      (only those)

Writes Prop_*.png next to the kit's layers and adds them to kit.json as kind "prop" (width and height in units, feet
at the bottom edge, centred). The greybox builder's MakeProp stands one on a quad wherever a helper asks for it and
keeps the greybox block when the drawing is missing. Nothing here is a behaviour: a desk is still the trigger it was.
"""
import json, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from saltmarrow import (OUT, PAPER, SILVER, OLIVE, RUST, INK, PPU, TILE_PPU, Palette, lerp, reset_scene, box, polygon,
                        setup_render, render)

KITS = os.path.dirname(OUT)   # Art/Environment: one folder and kit.json per region

BRASS = (0.78, 0.62, 0.30)
ROPE = (0.66, 0.56, 0.38)
GLOW = (0.96, 0.82, 0.42)
WET = (0.80, 0.78, 0.74)
STEEL = (0.30, 0.32, 0.36)


def ring(name, cx, cz, r_out, r_in, material, y=0.0, n=24):
    """A flat ring: an outer disc with the inner cut by drawing the inside in paper on top."""
    pts = [(cx + r_out * math.cos(2 * math.pi * i / n), cz + r_out * math.sin(2 * math.pi * i / n)) for i in range(n)]
    polygon(name, pts, material, y=y)
    return pts


def disc(name, cx, cz, r, material, y=0.0, n=24, squash=1.0):
    pts = [(cx + r * math.cos(2 * math.pi * i / n), cz + r * squash * math.sin(2 * math.pi * i / n)) for i in range(n)]
    return polygon(name, pts, material, y=y)


def line(name, x0, z0, x1, z1, thickness, material, y=0.0):
    """A stroke as a thin box between two points."""
    dx, dz = x1 - x0, z1 - z0
    length = math.hypot(dx, dz)
    ang = math.atan2(dz, dx)
    ob = box(name, 0, 0, length, thickness, material, y=y, d=0.1)
    ob.rotation_euler = (0, -ang, 0)
    ob.location = ((x0 + x1) / 2, ob.location[1], (z0 + z1) / 2)
    return ob


# ---------------------------------------------------------------- the props (bottom at z 0, centred on x)

def prop_desk(rng, p):
    """Prop_Desk: a drafting desk on trestles, the board slanted to the player, a sheet, an inkwell, a rolled chart."""
    wood, dark, paper, brass, ink = p("wood", lerp(RUST, SILVER, 0.35)), p("dark", lerp(RUST, INK, 0.5)), p("paper", PAPER), p("brass", BRASS), p("ink", INK)
    for x in (-0.62, 0.62):
        polygon("trestle_%s" % x, [(x - 0.28, 0.0), (x + 0.28, 0.0), (x + 0.08, 0.95), (x - 0.08, 0.95)], wood, y=0.1)
        box("rung_%s" % x, x, 0.32, 0.36, 0.05, dark, y=0.05)
    polygon("board", [(-0.95, 0.92), (0.95, 0.92), (0.95, 1.04), (-0.95, 1.16)], wood)
    polygon("sheet", [(-0.70, 1.00), (0.55, 1.00), (0.55, 1.07), (-0.70, 1.15)], paper, y=-0.05)
    for i in range(3):
        line("mark_%d" % i, -0.55 + 0.1 * i, 1.03 + 0.03 * i, -0.15 + 0.25 * rng.random(), 1.03 + 0.03 * i, 0.012, ink, y=-0.1)
    disc("coast", -0.3, 1.06, 0.06, ink, y=-0.1, n=10, squash=0.5)
    box("inkwell", 0.72, 1.10, 0.14, 0.14, dark, y=-0.05)
    line("quill", 0.72, 1.14, 0.86, 1.42, 0.02, ink, y=-0.1)
    box("chart", 0.0, 0.14, 0.9, 0.12, paper, y=-0.05)
    box("chart_band", 0.25, 0.14, 0.06, 0.13, brass, y=-0.1)
    return 2.0, 1.5, 2.2


def prop_ledger(rng, p):
    """Prop_Ledger: the commissions board: a post, a board, pinned slips with their scribbles."""
    wood, dark, paper, ink, brass = p("wood", lerp(RUST, SILVER, 0.35)), p("dark", lerp(RUST, INK, 0.5)), p("paper", PAPER), p("ink", INK), p("brass", BRASS)
    box("post", 0.0, 0.95, 0.14, 1.9, wood, y=0.1)
    box("board", 0.0, 1.45, 1.4, 0.95, dark)
    box("frame", 0.0, 1.45, 1.44, 0.06, wood, y=-0.02)
    box("frame2", 0.0, 1.92, 1.44, 0.06, wood, y=-0.02)
    slips = [(-0.42, 1.55, 0.36, 0.42, 6), (0.08, 1.6, 0.34, 0.36, -4), (0.46, 1.4, 0.3, 0.4, 3), (-0.2, 1.18, 0.4, 0.28, -7)]
    for i, (x, z, w, h, ang) in enumerate(slips):
        ob = box("slip_%d" % i, x, z, w, h, paper, y=-0.06)
        ob.rotation_euler = (0, math.radians(ang), 0)
        disc("pin_%d" % i, x, z + h / 2 - 0.03, 0.025, brass, y=-0.12, n=8)
        for k in range(3):
            line("scribble_%d_%d" % (i, k), x - w * 0.35, z + h * 0.25 - 0.08 * k, x - w * 0.35 + w * rng.uniform(0.4, 0.7), z + h * 0.25 - 0.08 * k, 0.012, ink, y=-0.1)
    return 1.5, 2.0, 2.2


def prop_dummy(rng, p):
    """Prop_Dummy: a training target: a post, a sacking body bound with rope, a painted ring."""
    wood, sack, rope, red, paper, ink = p("wood", lerp(RUST, SILVER, 0.35)), p("sack", lerp(OLIVE, PAPER, 0.35)), p("rope", ROPE), p("red", RUST), p("paper", PAPER), p("ink", INK)
    box("post", 0.0, 0.5, 0.12, 1.0, wood, y=0.1)
    polygon("body", [(-0.34, 0.45), (0.34, 0.45), (0.40, 0.95), (0.30, 1.30), (-0.30, 1.30), (-0.40, 0.95)], sack)
    disc("head", 0.0, 1.45, 0.16, sack, n=16)
    for i, z in enumerate((0.58, 0.78, 1.16)):
        box("wrap_%d" % i, 0.0, z, 0.74, 0.05, rope, y=-0.05)
    disc("ring_out", 0.0, 0.95, 0.20, red, y=-0.06)
    disc("ring_mid", 0.0, 0.95, 0.13, paper, y=-0.08)
    disc("ring_in", 0.0, 0.95, 0.06, red, y=-0.1)
    line("stitch", -0.08, 1.45, 0.08, 1.45, 0.015, ink, y=-0.1)
    return 1.0, 1.625, 2.2


def prop_stall(rng, p):
    """Prop_Stall: where the Ferrymen sell tethers: crates, coils of rope, a hanging board under an awning."""
    wood, dark, rope, canvas, paper, ink, brass = p("wood", lerp(RUST, SILVER, 0.35)), p("dark", lerp(RUST, INK, 0.5)), p("rope", ROPE), p("canvas", lerp(SILVER, PAPER, 0.4)), p("paper", PAPER), p("ink", INK), p("brass", BRASS)
    box("crate_a", -0.7, 0.3, 0.6, 0.6, wood)
    box("crate_b", -0.7, 0.9, 0.55, 0.55, wood, y=0.02)
    box("crate_c", -0.05, 0.28, 0.7, 0.56, wood)
    for i, (x, z) in enumerate(((-0.7, 0.3), (-0.7, 0.9), (-0.05, 0.28))):
        line("slat_%d" % i, x - 0.25, z - 0.2, x + 0.25, z + 0.2, 0.03, dark, y=-0.05)
    for i, (x, z, r) in enumerate(((0.55, 0.22, 0.22), (0.95, 0.2, 0.2), (0.75, 0.55, 0.2))):
        disc("coil_%d" % i, x, z, r, rope, y=-0.02)
        disc("coil_hole_%d" % i, x, z, r * 0.4, dark, y=-0.06)
    for x in (-1.1, 1.1):
        box("pole_%s" % x, x, 1.0, 0.08, 2.0, wood, y=0.15)
    polygon("awning", [(-1.25, 1.85), (1.25, 1.85), (1.15, 2.0), (-1.15, 2.0)], canvas, y=0.1)
    for i in range(4):
        box("scallop_%d" % i, -0.9 + 0.6 * i, 1.83, 0.5, 0.06, dark, y=0.05)
    box("sign", 0.0, 1.5, 0.9, 0.34, paper, y=-0.05)
    line("sign_cord_a", -0.3, 1.67, -0.5, 1.85, 0.015, ink, y=-0.05)
    line("sign_cord_b", 0.3, 1.67, 0.5, 1.85, 0.015, ink, y=-0.05)
    line("sign_word", -0.3, 1.5, 0.3, 1.5, 0.03, ink, y=-0.1)
    disc("sign_coin", 0.0, 1.4, 0.04, brass, y=-0.1, n=8)
    return 2.5, 2.0, 2.2


def prop_vantage(rng, p):
    """Prop_Vantage: a survey stake: a tall stake, a brass plate, a ribbon at the top."""
    wood, brass, ribbon, ink = p("wood", lerp(RUST, INK, 0.3)), p("brass", BRASS), p("ribbon", lerp(RUST, PAPER, 0.2)), p("ink", INK)
    box("stake", 0.0, 0.95, 0.1, 1.9, wood, y=0.05)
    box("plate", 0.0, 1.1, 0.2, 0.26, brass, y=-0.05)
    disc("rose", 0.0, 1.1, 0.06, ink, y=-0.1, n=8)
    polygon("ribbon", [(0.05, 1.9), (0.36, 1.78), (0.30, 1.66), (0.05, 1.74)], ribbon, y=-0.05)
    return 0.75, 2.0, 2.0


def prop_lamp(rng, p):
    """Prop_Lamp: a lantern on an iron stand: base, post, the housing with its glass, a cap."""
    iron, glass, brass, ink = p("iron", STEEL), p("glass", lerp(GLOW, PAPER, 0.55)), p("brass", BRASS), p("ink", INK)
    polygon("base", [(-0.5, 0.0), (0.5, 0.0), (0.3, 0.14), (-0.3, 0.14)], iron, y=0.05)
    box("post", 0.0, 0.8, 0.1, 1.4, iron, y=0.05)
    box("housing", 0.0, 1.85, 0.7, 0.9, iron)
    box("glass", 0.0, 1.85, 0.5, 0.7, glass, y=-0.05)
    box("bar", 0.0, 1.85, 0.03, 0.7, iron, y=-0.1)
    polygon("cap", [(-0.42, 2.3), (0.42, 2.3), (0.15, 2.48), (-0.15, 2.48)], iron, y=-0.02)
    disc("finial", 0.0, 2.48, 0.05, brass, y=-0.02, n=10)
    disc("flame", 0.0, 1.8, 0.09, brass, y=-0.12, n=10, squash=1.4)
    return 1.5, 2.5, 2.2


def prop_lampglow(rng, p):
    """Prop_LampGlow: the lit lamp's light on the paper: soft discs, no line (Freestyle off)."""
    for i, (r, k) in enumerate(((1.0, 0.82), (0.72, 0.66), (0.42, 0.45))):
        disc("glow_%d" % i, 0.0, 1.0, r, p("glow_%d" % i, lerp(GLOW, PAPER, k)), y=0.1 - 0.05 * i, n=36)
    return 2.0, 2.0, 0.0


def prop_seeds(rng, p):
    """Prop_Seeds: iris seed pods split on a stem, the seeds showing."""
    stem, pod, seed, ink = p("stem", OLIVE), p("pod", lerp(RUST, INK, 0.4)), p("seed", lerp(BRASS, PAPER, 0.25)), p("ink", INK)
    line("stem", 0.0, 0.0, 0.05, 0.45, 0.03, stem)
    for i, (x, z, ang) in enumerate(((-0.14, 0.5, 20), (0.16, 0.48, -25), (0.02, 0.62, 0))):
        ob = polygon("pod_%d" % i, [(x - 0.09, z - 0.12), (x + 0.09, z - 0.12), (x + 0.11, z + 0.1), (x, z + 0.16), (x - 0.11, z + 0.1)], pod, y=-0.02 * i)
        ob.rotation_euler = (0, math.radians(ang), 0)
        for k in range(3):
            disc("seed_%d_%d" % (i, k), x - 0.04 + 0.04 * k, z - 0.02 + 0.05 * (k % 2), 0.028, seed, y=-0.08 - 0.02 * i, n=8)
    return 0.75, 0.75, 1.8


def prop_bound(rng, p):
    """Prop_Bound: a bound stake: a short post with a knotted cord, blue-grey."""
    wood, cord, ink = p("wood", lerp((0.20, 0.27, 0.45), SILVER, 0.3)), p("cord", ROPE), p("ink", INK)
    box("stake", 0.0, 0.55, 0.12, 1.1, wood, y=0.05)
    polygon("top", [(-0.06, 1.1), (0.06, 1.1), (0.0, 1.22)], wood, y=0.05)
    for i, z in enumerate((0.78, 0.86)):
        box("cord_%d" % i, 0.0, z, 0.2, 0.05, cord, y=-0.05)
    disc("knot", 0.12, 0.82, 0.05, cord, y=-0.08, n=8)
    line("tail", 0.15, 0.8, 0.22, 0.6, 0.02, cord, y=-0.08)
    return 0.5, 1.25, 2.0


def prop_nets(rng, p):
    """Prop_Nets: a drying rack with a net draped over it, floats along the top rope."""
    wood, net, float_, ink = p("wood", lerp(RUST, SILVER, 0.35)), p("net", lerp(OLIVE, INK, 0.3)), p("float", lerp(RUST, PAPER, 0.3)), p("ink", INK)
    for x in (-0.8, 0.8):
        box("upright_%s" % x, x, 0.65, 0.08, 1.3, wood, y=0.1)
    box("rail", 0.0, 1.3, 1.75, 0.07, wood, y=0.05)
    n = 7
    for i in range(n):
        x = -0.72 + 1.44 * i / (n - 1)
        line("mesh_v_%d" % i, x, 1.28, x + rng.uniform(-0.05, 0.05), 0.35 + 0.12 * math.sin(i), 0.014, net, y=-0.02)
    for k in range(5):
        z = 1.2 - 0.18 * k
        line("mesh_h_%d" % k, -0.74, z + 0.03 * math.sin(k), 0.74, z - 0.03 * math.sin(k), 0.014, net, y=-0.02)
    for i in range(4):
        disc("float_%d" % i, -0.55 + 0.37 * i, 1.3, 0.05, float_, y=-0.06, n=8)
    return 1.75, 1.5, 2.0


def prop_stoop(rng, p):
    """Prop_Stoop: Dotha's doorstep: two stone steps, a bench, a pot with a dead reed in it."""
    stone, dark, wood, pot, reed = p("stone", lerp(SILVER, PAPER, 0.25)), p("dark", lerp(SILVER, INK, 0.5)), p("wood", lerp(RUST, SILVER, 0.35)), p("pot", lerp(RUST, PAPER, 0.15)), p("reed", lerp(OLIVE, PAPER, 0.5))
    box("step_a", -0.3, 0.14, 1.4, 0.28, stone, y=0.1)
    box("step_b", -0.4, 0.4, 1.0, 0.24, stone, y=0.05)
    line("crack", -0.6, 0.06, -0.3, 0.2, 0.012, dark, y=-0.05)
    box("bench_seat", 0.55, 0.5, 0.8, 0.08, wood, y=-0.02)
    for x in (0.25, 0.85):
        box("bench_leg_%s" % x, x, 0.24, 0.07, 0.48, wood)
    polygon("pot", [(-0.86, 0.0), (-0.6, 0.0), (-0.56, 0.42), (-0.9, 0.42)], pot, y=-0.05)
    box("pot_rim", -0.73, 0.44, 0.38, 0.06, pot, y=-0.08)
    line("reed_a", -0.73, 0.45, -0.66, 1.1, 0.025, reed, y=-0.1)
    line("reed_b", -0.75, 0.45, -0.86, 0.95, 0.02, reed, y=-0.1)
    return 2.0, 1.25, 2.0


def prop_boat(rng, p):
    """Prop_Boat: a beached rowing boat, planked, an oar across the thwart."""
    hull, dark, wood, rope = p("hull", lerp(RUST, INK, 0.25)), p("dark", lerp(RUST, INK, 0.55)), p("wood", lerp(RUST, SILVER, 0.35)), p("rope", ROPE)
    polygon("hull", [(-1.5, 0.55), (-1.3, 0.15), (-0.8, 0.02), (0.9, 0.02), (1.35, 0.2), (1.5, 0.7), (1.2, 0.62), (-1.1, 0.5)], hull)
    for i in range(3):
        z0 = 0.16 + 0.13 * i
        line("plank_%d" % i, -1.28 + 0.1 * i, z0 + 0.1 * i * 0.5, 1.3, z0 + 0.08 * i, 0.015, dark, y=-0.05)
    box("thwart", 0.1, 0.56, 0.9, 0.07, wood, y=-0.06)
    line("oar", -1.1, 0.9, 1.2, 0.5, 0.035, wood, y=-0.1)
    polygon("blade", [(-1.1, 0.98), (-1.35, 1.0), (-1.4, 0.88), (-1.15, 0.84)], wood, y=-0.1)
    line("painter", 1.45, 0.66, 1.5, 0.25, 0.02, rope, y=-0.08)
    return 3.0, 1.0, 2.2


def prop_tether(rng, p):
    """Prop_Tether: a tether-post of Merrow's End: a post with its rope running up and away into the white."""
    wood, iron, rope, paper = p("wood", lerp(RUST, INK, 0.3)), p("iron", STEEL), p("rope", ROPE), p("rope_far", lerp(ROPE, PAPER, 0.5))
    box("post", 0.0, 0.8, 0.22, 1.6, wood, y=0.1)
    box("band_a", 0.0, 1.35, 0.26, 0.07, iron, y=0.05)
    box("band_b", 0.0, 0.5, 0.26, 0.07, iron, y=0.05)
    disc("eye", 0.0, 1.5, 0.08, iron, y=-0.02, n=10)
    line("rope_near", 0.0, 1.5, 0.3, 2.3, 0.035, rope, y=-0.05)
    line("rope_far", 0.3, 2.3, 0.48, 2.95, 0.028, paper, y=-0.05)
    for i in range(3):
        box("wrap_%d" % i, 0.0, 0.9 + 0.09 * i, 0.3, 0.04, rope, y=-0.02)
    return 1.0, 3.0, 2.0


def prop_wetedge(rng, p):
    """Prop_WetEdge: where the paper is wet before it is white: a fibrous damp band, the white bleeding in from
    the right, drawn tall so the Edge room can stretch it over the whole height. No ink line: it is not a thing."""
    wet, damp, white = p("wet", WET), p("damp", lerp(WET, PAPER, 0.45)), p("white", (0.97, 0.96, 0.93))
    box("white", 1.6, 6.0, 1.6, 12.4, white, y=0.2)
    for k in range(240):
        z = 0.025 + 11.95 * k / 239
        reach = 0.9 + 0.7 * math.sin(z * 1.7) * math.sin(z * 0.6 + 1.0) + 0.18 * math.sin(z * 9.0) + rng.uniform(-0.12, 0.12)
        box("damp_%d" % k, 0.8 - reach * 0.5, z, reach + 0.6, 0.06, damp, y=0.1)
        box("wet_%d" % k, 0.9 - reach * 0.2, z, reach * 0.5 + rng.uniform(0.1, 0.5), 0.04, wet, y=0.0)
    for k in range(40):
        z = rng.uniform(0.0, 12.0)
        x = 0.9 + rng.uniform(-1.4, 0.2)
        box("fibre_%d" % k, x, z, rng.uniform(0.15, 0.5), 0.02, wet, y=-0.05)
    return 4.0, 12.0, 0.0


PROPS = [
    ("Prop_Desk", prop_desk), ("Prop_Ledger", prop_ledger), ("Prop_Dummy", prop_dummy), ("Prop_Stall", prop_stall),
    ("Prop_Vantage", prop_vantage), ("Prop_Lamp", prop_lamp), ("Prop_LampGlow", prop_lampglow), ("Prop_Seeds", prop_seeds),
    ("Prop_Bound", prop_bound), ("Prop_Nets", prop_nets), ("Prop_Stoop", prop_stoop), ("Prop_Boat", prop_boat),
    ("Prop_Tether", prop_tether),
    ("Prop_WetEdge", prop_wetedge, "Greyfold"),   # the Edge room is Greyfold's: its own kit, this its first layer
]


def build(name, fn, region="Saltmarrow"):
    out = os.path.join(KITS, region)
    os.makedirs(out, exist_ok=True)
    reset_scene()
    seed = sum(ord(c) for c in name)
    rng = random.Random(seed)
    p = Palette(0.0, False)
    w, h, line_px = fn(rng, p)
    sc = setup_render(w, h, TILE_PPU, 0.0, max(line_px, 0.1), p.ink(), seed % 1000)
    sc.render.use_freestyle = line_px > 0
    size = render(os.path.join(out, name + ".png"))
    return w, h, size


def main():
    only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    kits = {}
    for entry in PROPS:
        name, fn = entry[0], entry[1]
        region = entry[2] if len(entry) > 2 else "Saltmarrow"
        if only and name not in only:
            continue
        if region not in kits:
            path = os.path.join(KITS, region, "kit.json")
            if os.path.exists(path):
                with open(path) as f:
                    kits[region] = json.load(f)
            else:
                kits[region] = {"region": region, "ppu": PPU, "tilePpu": TILE_PPU, "paper": PAPER, "layers": []}
        w, h, size = build(name, fn, region)
        print("[props] %s %dx%d px %d bytes (%s)" % (name, w * TILE_PPU, h * TILE_PPU, size, region))
        layer = {"name": name, "kind": "prop", "widthUnits": w, "heightUnits": h, "ppu": TILE_PPU,
                 "widthPx": int(round(w * TILE_PPU)), "heightPx": int(round(h * TILE_PPU)), "file": name + ".png", "faded": False}
        kits[region]["layers"] = [l for l in kits[region]["layers"] if l["name"] != name] + [layer]
    for region, kit in kits.items():
        with open(os.path.join(KITS, region, "kit.json"), "w") as f:
            json.dump(kit, f, indent=2)
        print("[props] wrote %s/kit.json with %d layers" % (region, len(kit["layers"])))


if __name__ == "__main__":
    main()

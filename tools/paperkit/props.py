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
from saltmarrow import (OUT, PAPER, SILVER, OLIVE, RUST, INK, PPU, TILE_PPU, lerp, reset_scene, box, polygon,
                        setup_render, render)
from kitlib import Palette as _Palette, ridge, blob
import emberdown as _ember
import verdance as _verd
import halden as _hald
import windreach as _wind
import greyfold as _grey
import blank as _blank

KITS = os.path.dirname(OUT)   # Art/Environment: one folder and kit.json per region

BRASS = (0.78, 0.62, 0.30)
ROPE = (0.66, 0.56, 0.38)
GLOW = (0.96, 0.82, 0.42)
WET = (0.80, 0.78, 0.74)
STEEL = (0.30, 0.32, 0.36)

# The same drawings in another region's palette (ENV-03): the colour names above rebound before each render.
REGIONS = {
    "Saltmarrow": dict(paper=PAPER, ink=INK, colours=dict(PAPER=PAPER, SILVER=SILVER, OLIVE=OLIVE, RUST=RUST, INK=INK, BRASS=BRASS, ROPE=ROPE, GLOW=GLOW, WET=WET, STEEL=STEEL)),
    # The Greyfold (ENV-08): white paper, the greys the ink thinned, Wren's blue and lantern gold, ghost-grey ink; the furniture is grey wood and rope.
    "Greyfold": dict(paper=_grey.PAPER, ink=_grey.INK, colours=dict(
        PAPER=_grey.PAPER, SILVER=_grey.GREY, OLIVE=_grey.PALE, RUST=_grey.WOOD, INK=_grey.INK,
        BRASS=lerp(_grey.GOLD, _grey.PAPER, 0.3), ROPE=_grey.ROPE, GLOW=lerp(_grey.GOLD, _grey.PAPER, 0.5), WET=lerp(_grey.GREY, _grey.PAPER, 0.5), STEEL=_grey.DARK)),
    # The Blank (ENV-08): the same white, a shade darker in its greys (the grey is the people); the furniture is stone and grey wood.
    "Blank": dict(paper=_blank.PAPER, ink=_blank.INK, colours=dict(
        PAPER=_blank.PAPER, SILVER=_blank.GREY, OLIVE=_blank.PALE, RUST=_blank.STONE, INK=_blank.INK,
        BRASS=lerp(_blank.GOLD, _blank.PAPER, 0.3), ROPE=lerp(_blank.GREY, _blank.PAPER, 0.3), GLOW=lerp(_blank.GOLD, _blank.PAPER, 0.5), WET=lerp(_blank.BLUE, _blank.PAPER, 0.85), STEEL=_blank.DARK)),
    "Emberdown": dict(paper=_ember.PAPER, ink=_ember.INK, colours=dict(
        PAPER=_ember.PAPER, SILVER=_ember.CHARCOAL, OLIVE=_ember.SULPHUR, RUST=(0.36, 0.26, 0.20), INK=_ember.INK,
        BRASS=(0.58, 0.46, 0.22), ROPE=(0.46, 0.40, 0.32), GLOW=_ember.EMBER, WET=(0.55, 0.53, 0.52), STEEL=(0.20, 0.20, 0.22))),
    # The Verdance (ENV-04): pale gold paper, deep green, moss, bone white, sepia ink; the furniture is wood and bone.
    "Verdance": dict(paper=_verd.PAPER, ink=_verd.INK, colours=dict(
        PAPER=_verd.PAPER, SILVER=lerp(_verd.BONE, _verd.MOSS, 0.3), OLIVE=_verd.MOSS, RUST=_verd.BARK, INK=_verd.INK,
        BRASS=(0.72, 0.60, 0.34), ROPE=lerp(_verd.BARK, _verd.PAPER, 0.4), GLOW=(0.96, 0.86, 0.52), WET=lerp(_verd.MOSS, _verd.PAPER, 0.5), STEEL=lerp(_verd.INK, _verd.MOSS, 0.4))),
    # Halden (ENV-05): cool cream paper, slate, verdigris, brass, blue-black ink; the furniture is stone, dark wood and brass.
    "Halden": dict(paper=_hald.PAPER, ink=_hald.INK, colours=dict(
        PAPER=_hald.PAPER, SILVER=_hald.STONE, OLIVE=_hald.VERDIGRIS, RUST=lerp(_hald.SLATE, _hald.BRASS, 0.35), INK=_hald.INK,
        BRASS=_hald.BRASS, ROPE=lerp(_hald.SLATE, _hald.PAPER, 0.4), GLOW=(0.97, 0.88, 0.58), WET=lerp(_hald.VERDIGRIS, _hald.PAPER, 0.5), STEEL=_hald.SLATE)),
    # Windreach (ENV-07): straw paper, sky blue, storm violet, gold, grey-brown ink; the furniture is weathered wood and grass.
    "Windreach": dict(paper=_wind.PAPER, ink=_wind.INK, colours=dict(
        PAPER=_wind.PAPER, SILVER=_wind.STONE, OLIVE=_wind.STRAW, RUST=_wind.WOOD, INK=_wind.INK,
        BRASS=_wind.GOLD, ROPE=lerp(_wind.STRAW, _wind.PAPER, 0.3), GLOW=(0.98, 0.86, 0.50), WET=lerp(_wind.SKY, _wind.PAPER, 0.5), STEEL=_wind.IRON)),
}


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


def prop_bell(rng, p):
    """Prop_Bell: the Roll-Call Bell: a timber frame on a stone foot, the bell hung in it, the rope down to a cleat."""
    timber, dark, brass, stone, rope = p("timber", lerp(RUST, INK, 0.2)), p("dark", lerp(INK, RUST, 0.2)), p("brass", BRASS), p("stone", SILVER), p("rope", ROPE)
    box("foot", 0.0, 0.3, 1.8, 0.6, stone, y=0.1)
    for dx in (-0.85, 0.85):
        polygon("leg_%s" % dx, [(dx - 0.13, 0.6), (dx + 0.13, 0.6), (dx * 0.65 + 0.1, 3.1), (dx * 0.65 - 0.1, 3.1)], timber, y=0.0)
    box("beam", 0.0, 3.1, 1.7, 0.18, timber, y=-0.02)
    box("brace", 0.0, 1.9, 1.5, 0.12, timber, y=0.03)
    polygon("cap", [(-1.15, 3.19), (1.15, 3.19), (0.0, 3.5)], dark, y=-0.05)
    polygon("bell", [(-0.17, 2.98), (0.17, 2.98), (0.28, 2.5), (0.55, 2.05), (-0.55, 2.05), (-0.28, 2.5)], brass, y=-0.1)
    box("lip", 0.0, 2.02, 1.12, 0.08, dark, y=-0.12)
    disc("clapper", 0.0, 1.92, 0.09, dark, y=-0.14, n=8)
    line("rope", 0.0, 1.9, 0.62, 0.7, 0.035, rope, y=-0.06)
    box("cleat", 0.62, 0.66, 0.16, 0.1, dark, y=-0.05)
    return 2.5, 3.5, 2.0


def prop_anvil(rng, p):
    """Prop_Anvil: the smith's: an anvil on a stump, a hammer leaning, a quench bucket, tongs on a hook."""
    iron, stump, wood, water = p("iron", STEEL), p("stump", lerp(RUST, INK, 0.3)), p("wood", RUST), p("water", lerp(WET, INK, 0.3))
    box("stump", -0.3, 0.3, 0.7, 0.6, stump, y=0.05)
    polygon("anvil", [(-0.85, 0.6), (0.25, 0.6), (0.25, 0.78), (0.6, 0.78), (0.72, 0.95), (0.3, 1.0), (-0.65, 1.0), (-0.95, 0.9), (-0.95, 0.72)], iron, y=0.0)
    box("waist", -0.3, 0.68, 0.36, 0.16, iron, y=0.02)
    line("hammer_shaft", 0.45, 0.02, 0.62, 0.7, 0.05, wood, y=-0.05)
    box("hammer_head", 0.64, 0.74, 0.26, 0.14, iron, y=-0.08)
    box("bucket", 0.75, 0.24, 0.42, 0.48, wood, y=0.02)
    box("water", 0.75, 0.46, 0.34, 0.05, water, y=-0.02)
    box("band", 0.75, 0.12, 0.44, 0.05, iron, y=-0.03)
    return 2.0, 1.5, 2.0


def prop_boards(rng, p):
    """Prop_Boards: the pit-head's mine mouth, boarded: a dark arch in the rock with planks nailed across at angles."""
    rock, dark, plank, nail = p("rock", SILVER), p("dark", INK), p("plank", lerp(RUST, OLIVE, 0.3)), p("nail", STEEL)
    polygon("rock", [(-1.5, 0.0), (1.5, 0.0), (1.5, 1.9), (1.0, 2.5), (-1.0, 2.5), (-1.5, 1.9)], rock, y=0.1)
    polygon("arch", [(-0.95, 0.0), (0.95, 0.0), (0.95, 1.4), (0.55, 2.05), (-0.55, 2.05), (-0.95, 1.4)], dark, y=0.0)
    for i, (z0, z1, t) in enumerate(((0.35, 0.55, 0.22), (0.95, 0.8, 0.2), (1.5, 1.7, 0.2), (0.15, 1.95, 0.16))):
        line("plank_%d" % i, -1.15, z0, 1.15, z1, t, plank, y=-0.05 - 0.01 * i)
        for x in (-0.95, 0.9):
            zz = z0 + (z1 - z0) * (x + 1.15) / 2.3
            disc("nail_%d_%s" % (i, x), x, zz, 0.03, nail, y=-0.12, n=6)
    return 3.0, 2.5, 2.0


def prop_porch(rng, p):
    """Prop_Porch: Kettil's porch: a step, two posts, a lean-to roof with ash on it, a stool, a lantern hook."""
    wood, dark, ash, stone = p("wood", lerp(RUST, INK, 0.2)), p("dark", lerp(INK, RUST, 0.15)), p("ash", lerp(PAPER, OLIVE, 0.15)), p("stone", SILVER)
    box("step", 0.0, 0.12, 2.8, 0.24, stone, y=0.05)
    for x in (-1.2, 1.2):
        box("post_%s" % x, x, 0.24 + 0.85, 0.16, 1.7, wood, y=0.0)
    polygon("roof", [(-1.5, 1.94), (1.5, 1.94), (1.5, 1.7), (-1.5, 1.4)], dark, y=-0.05)
    box("ash", 0.0, 1.98, 2.2, 0.06, ash, y=-0.1)
    box("stool", 0.55, 0.5, 0.5, 0.08, wood, y=-0.02)
    for x in (0.38, 0.72):
        box("stool_leg_%s" % x, x, 0.36, 0.06, 0.26, wood, y=-0.01)
    box("hook", -1.0, 1.35, 0.06, 0.2, dark, y=-0.04)
    return 3.0, 2.0, 2.0


def prop_milestone(rng, p):
    """Prop_Milestone: a flying-age milestone: a tall slab, rounded, its letters cut large and high, chalk low down."""
    stone, dark, chalk, moss = p("stone", lerp(SILVER, PAPER, 0.15)), p("dark", lerp(INK, SILVER, 0.3)), p("chalk", PAPER), p("moss", OLIVE)
    w = 0.9
    pts = [(-w / 2, 0.0), (w / 2, 0.0), (w / 2, 2.05)]
    for i in range(1, 6):
        a = math.pi * i / 6
        pts.append((w / 2 * math.cos(a), 2.05 + w / 2 * math.sin(a)))
    pts.append((-w / 2, 2.05))
    polygon("slab", pts, stone, y=0.05)
    for i in range(3):
        box("cut_%d" % i, 0.0, 1.95 - i * 0.32, rng.uniform(0.3, 0.5), 0.1, dark, y=-0.05)
    for i in range(2):
        line("chalk_%d" % i, -0.3, 0.55 - i * 0.14, -0.3 + rng.uniform(0.3, 0.55), 0.55 - i * 0.14, 0.03, chalk, y=-0.08)
    ridge("moss", rng, -0.5, 0.5, 0.0, 0.12, 0.05, 6, moss, y=-0.1)
    return 1.5, 2.5, 2.0


def prop_lantern(rng, p):
    """Prop_Lantern: a grove lantern on its cord: a paper body ribbed with cane, a cap, the light behind it."""
    body, dark, glow = p("body", lerp(PAPER, SILVER, 0.15)), p("dark", lerp(INK, RUST, 0.3)), p("glow", lerp(GLOW, PAPER, 0.5))
    box("cord", 0.0, 2.05, 0.03, 0.9, dark, y=0.05)
    disc("glow", 0.0, 0.85, 0.75, glow, y=0.12, n=24)
    polygon("body", [(-0.22, 0.2), (0.22, 0.2), (0.34, 0.55), (0.34, 1.15), (0.2, 1.5), (-0.2, 1.5), (-0.34, 1.15), (-0.34, 0.55)], body, y=0.0)
    box("cap", 0.0, 1.53, 0.34, 0.08, dark, y=-0.02)
    box("foot", 0.0, 0.17, 0.28, 0.07, dark, y=-0.02)
    for i in range(3):
        box("rib_%d" % i, 0.0, 0.45 + i * 0.32, 0.64, 0.025, dark, y=-0.03)
    disc("flame", 0.0, 0.85, 0.1, p("flame", GLOW), y=-0.06, n=10, squash=1.4)
    return 1.0, 2.5, 2.0


def prop_lectern(rng, p):
    """Prop_Lectern: Brother Ansel's lectern: a post on a foot, a slanted board, a book open on page 214, dust on it."""
    wood, dark, page, ink, dustm = p("wood", lerp(RUST, SILVER, 0.2)), p("dark", lerp(RUST, INK, 0.5)), p("page", PAPER), p("ink", INK), p("dust", lerp(SILVER, PAPER, 0.5))
    polygon("foot", [(-0.4, 0.0), (0.4, 0.0), (0.25, 0.12), (-0.25, 0.12)], wood, y=0.05)
    box("post", 0.0, 0.6, 0.12, 1.0, wood, y=0.05)
    polygon("board", [(-0.55, 1.05), (0.55, 1.05), (0.55, 1.2), (-0.55, 1.35)], wood, y=0.0)
    polygon("book_l", [(-0.5, 1.12), (-0.02, 1.12), (-0.02, 1.25), (-0.5, 1.4)], page, y=-0.05)
    polygon("book_r", [(0.02, 1.12), (0.5, 1.12), (0.5, 1.18), (0.02, 1.25)], page, y=-0.05)
    for i in range(4):
        line("text_%d" % i, -0.44, 1.2 + i * 0.045, -0.1, 1.17 + i * 0.045, 0.012, ink, y=-0.1)
        line("text_r%d" % i, 0.08, 1.2 + i * 0.03, 0.42, 1.14 + i * 0.03, 0.012, ink, y=-0.1)
    for i in range(6):
        disc("dust_%d" % i, rng.uniform(-0.5, 0.5), rng.uniform(1.25, 1.5), 0.025, dustm, y=-0.12, n=6)
    return 1.5, 1.5, 2.0


def prop_bunting(rng, p):
    """Prop_Bunting: Aldermere's last day: two poles and a sagging cord of small flags between them."""
    wood, cord, flag_a, flag_b = p("wood", lerp(RUST, INK, 0.2)), p("cord", RUST), p("flag_a", lerp(OLIVE, INK, 0.3)), p("flag_b", lerp(SILVER, PAPER, 0.3))
    for x in (-1.85, 1.85):
        box("pole_%s" % x, x, 1.2, 0.1, 2.4, wood, y=0.05)
    n = 9
    prev = None
    for i in range(n + 1):
        t = i / n
        px = -1.85 + 3.7 * t
        pz = 2.3 - 0.55 * math.sin(t * math.pi)
        if prev is not None:
            line("cord_%d" % i, prev[0], prev[1], px, pz, 0.025, cord, y=0.0)
        if 0 < i < n:
            polygon("flag_%d" % i, [(px - 0.14, pz), (px + 0.14, pz), (px + rng.uniform(-0.04, 0.04), pz - 0.4)], flag_a if i % 2 else flag_b, y=-0.03)
        prev = (px, pz)
    return 4.0, 2.5, 2.0


def prop_anchor(rng, p):
    """Prop_Anchor: an anchor-point in the branches: a knot of root round a bone ring, which is what the thread
    catches; drawn at the point itself (feet half a unit below it)."""
    bark, dark, bone = p("bark", RUST), p("dark", lerp(RUST, INK, 0.5)), p("bone", lerp(SILVER, PAPER, 0.5))
    for i in range(4):
        a = math.pi / 2 * i + 0.4
        ob = box("knot_%d" % i, 0.0, 0.5, 0.75, 0.16, bark if i % 2 else dark, y=0.02 * i)
        ob.rotation_euler = (0, a, 0)
    disc("ring", 0.0, 0.5, 0.26, bone, y=-0.1, n=16)
    disc("hole", 0.0, 0.5, 0.13, p("hole", PAPER), y=-0.14, n=12)
    return 1.0, 1.0, 2.0


def prop_gravestone(rng, p):
    """Prop_Gravestone: the orchard's stone: a slab with a rounded top, a crest cut in it (an owl's eye), leaves at its foot."""
    stone, dark, leaf = p("stone", SILVER), p("dark", lerp(INK, SILVER, 0.3)), p("leaf", lerp(BRASS, PAPER, 0.2))
    w = 1.0
    pts = [(-w / 2, 0.0), (w / 2, 0.0), (w / 2, 1.3)]
    for i in range(1, 8):
        a = math.pi * i / 8
        pts.append((w / 2 * math.cos(a), 1.3 + w / 2 * math.sin(a)))
    pts.append((-w / 2, 1.3))
    polygon("slab", pts, stone, y=0.05)
    disc("crest", 0.0, 1.35, 0.22, dark, y=-0.05, n=16)
    disc("eye", 0.0, 1.35, 0.1, stone, y=-0.08, n=12)
    for i in range(3):
        line("cut_%d" % i, -0.3, 0.95 - i * 0.2, -0.3 + rng.uniform(0.35, 0.6), 0.95 - i * 0.2, 0.03, dark, y=-0.06)
    for i in range(4):
        ob = polygon("leaf_%d" % i, [(-0.08, 0), (0.08, 0), (0.1, 0.12), (0, 0.2), (-0.1, 0.12)], leaf, y=-0.1)
        ob.location = (rng.uniform(-0.7, 0.7), ob.location[1], rng.uniform(0.0, 0.15))
        ob.rotation_euler = (0, rng.uniform(0, 6.28), 0)
    return 1.5, 2.0, 2.0


def prop_wheel(rng, p):
    """Prop_Wheel: a mill wheel on its axle housing, paddles round the rim, water at its foot."""
    wood, dark, water = p("wood", lerp(RUST, SILVER, 0.2)), p("dark", lerp(RUST, INK, 0.5)), p("water", WET)
    r = 1.1
    disc("rim", 0.0, 1.25, r, wood, y=0.0)
    disc("inner", 0.0, 1.25, r * 0.78, p("inner", lerp(SILVER, PAPER, 0.3)), y=-0.03)
    for i in range(8):
        a = 2 * math.pi * i / 8
        ob = box("spoke_%d" % i, 0.0, 0.0, 0.07, r * 1.9, dark, y=-0.06)
        ob.rotation_euler = (0, a, 0)
        ob.location = (0.0, ob.location[1], 1.25)
        pb = box("pad_%d" % i, 0.0, 0.0, 0.12, 0.45, dark, y=-0.07)
        pb.rotation_euler = (0, a + math.pi / 8, 0)
        pb.location = (r * math.cos(a + math.pi / 8), pb.location[1], 1.25 + r * math.sin(a + math.pi / 8))
    disc("hub", 0.0, 1.25, 0.14, dark, y=-0.09, n=10)
    box("housing", 0.0, 1.25, 0.3, 0.6, wood, y=0.1)
    for i in range(3):
        blob("water_%d" % i, rng, rng.uniform(-1.0, 1.0), 0.1, rng.uniform(0.3, 0.6), 0.1, water, y=-0.1, wobble=0.3)
    return 2.5, 2.5, 2.0


def prop_scaffold(rng, p):
    """Prop_Scaffold: the seventh bridge's repair: poles lashed, two planks, a bucket; the same for forty years."""
    pole, plank, rope, bucket = p("pole", lerp(RUST, INK, 0.3)), p("plank", lerp(RUST, SILVER, 0.3)), p("rope", ROPE), p("bucket", STEEL)
    for x in (-1.2, 0.0, 1.2):
        box("pole_%s" % x, x, 1.5, 0.08, 3.0, pole, y=0.05)
    for z in (1.1, 2.3):
        box("plank_%d" % int(z * 10), 0.0, z, 2.8, 0.12, plank, y=0.0)
        for x in (-1.2, 0.0, 1.2):
            disc("lash_%d_%s" % (int(z * 10), x), x, z, 0.09, rope, y=-0.04, n=8)
    line("brace", -1.2, 0.2, 1.2, 2.2, 0.06, pole, y=0.08)
    box("bucket", 0.7, 1.4, 0.3, 0.3, bucket, y=-0.06)
    return 3.0, 3.0, 2.0


def prop_frame(rng, p):
    """Prop_Frame: the frame of the Great Atlas: a brass ring on a stand, seven sockets with a compass rose each,
    one stone in it (the Observatory's own, forty-one years)."""
    brass, dark, stone, keystone = p("brass", BRASS), p("dark", lerp(INK, SILVER, 0.2)), p("stone", SILVER), p("keystone", lerp(BRASS, PAPER, 0.35))
    polygon("foot", [(-0.9, 0.0), (0.9, 0.0), (0.5, 0.25), (-0.5, 0.25)], stone, y=0.1)
    box("stem", 0.0, 0.6, 0.3, 0.8, brass, y=0.08)
    disc("ring", 0.0, 2.3, 1.55, brass, y=0.0, n=32)
    disc("ring_in", 0.0, 2.3, 1.3, p("page", PAPER), y=-0.03, n=32)
    for i in range(7):
        a = math.pi / 2 + 2 * math.pi * i / 7
        cx, cz = 1.05 * math.cos(a), 2.3 + 1.05 * math.sin(a)
        disc("socket_%d" % i, cx, cz, 0.2, dark, y=-0.06, n=12)
        for k in range(4):
            ob = box("rose_%d_%d" % (i, k), 0.0, 0.0, 0.02, 0.26, brass, y=-0.08)
            ob.rotation_euler = (0, k * math.pi / 4, 0)
            ob.location = (cx, ob.location[1], cz)
        if i == 0:
            disc("stone", cx, cz, 0.15, keystone, y=-0.1, n=10)
    return 3.5, 4.0, 2.0


def prop_slots(rng, p):
    """Prop_Slots: the Vault's wall: seven niches in a row under a brass rail, six with a stone in them, one empty
    and its dust younger."""
    stone, dark, brass, keystone, dust = p("stone", SILVER), p("dark", lerp(INK, SILVER, 0.25)), p("brass", BRASS), p("keystone", lerp(BRASS, PAPER, 0.35)), p("dust", lerp(SILVER, PAPER, 0.5))
    box("wall", 0.0, 1.25, 5.0, 2.5, stone, y=0.1)
    box("rail", 0.0, 2.3, 4.8, 0.06, brass, y=0.0)
    for i in range(7):
        x = -2.1 + i * 0.7
        polygon("niche_%d" % i, [(x - 0.22, 0.6), (x + 0.22, 0.6), (x + 0.22, 1.5), (x, 1.75), (x - 0.22, 1.5)], dark, y=0.0)
        if i != 4:
            disc("stone_%d" % i, x, 1.05, 0.16, keystone, y=-0.05, n=10)
        else:
            disc("dust", x, 0.68, 0.14, dust, y=-0.05, n=8, squash=0.4)
        line("label_%d" % i, x - 0.15, 0.45, x + 0.15, 0.45, 0.025, dark, y=-0.05)
    return 5.0, 2.5, 2.0


def prop_examdesk(rng, p):
    """Prop_ExamDesk: an exam desk with its paper on it, the same paper on every desk, and a stool."""
    wood, dark, paper, ink = p("wood", lerp(RUST, SILVER, 0.2)), p("dark", lerp(RUST, INK, 0.5)), p("paper", PAPER), p("ink", INK)
    box("top", 0.0, 0.95, 1.3, 0.08, wood, y=0.0)
    for x in (-0.55, 0.55):
        box("leg_%s" % x, x, 0.46, 0.07, 0.9, dark, y=0.05)
    box("modesty", 0.0, 0.55, 1.1, 0.4, wood, y=0.08)
    polygon("paper", [(-0.4, 0.99), (0.3, 0.99), (0.32, 1.06), (-0.38, 1.1)], paper, y=-0.05)
    for i in range(3):
        line("line_%d" % i, -0.3, 1.02 + i * 0.022, -0.3 + rng.uniform(0.3, 0.5), 1.02 + i * 0.022, 0.012, ink, y=-0.08)
    box("stool", -0.9, 0.5, 0.4, 0.06, wood, y=0.02)
    for x in (-1.05, -0.75):
        box("stool_leg_%s" % x, x, 0.24, 0.05, 0.48, dark, y=0.04)
    return 2.5, 1.25, 2.0


def prop_notice(rng, p):
    """Prop_Notice: Lowmarket's notice board: a post, a board, the notice pasted over older copies of itself."""
    wood, dark, paper = p("wood", lerp(RUST, INK, 0.3)), p("dark", lerp(RUST, INK, 0.5)), p("paper", PAPER)
    box("post", 0.0, 1.0, 0.12, 2.0, wood, y=0.1)
    box("board", 0.0, 1.55, 1.4, 1.0, dark, y=0.0)
    for i in range(4):
        box("old_%d" % i, rng.uniform(-0.25, 0.25), 1.45 + rng.uniform(-0.15, 0.15), 0.8, 0.55, p("old_%d" % i, lerp(PAPER, RUST, 0.12 * (4 - i))), y=-0.03 - 0.01 * i)
    box("notice", 0.0, 1.55, 0.8, 0.55, paper, y=-0.09)
    line("head", -0.3, 1.72, 0.3, 1.72, 0.04, p("ink", INK), y=-0.12)
    for i in range(3):
        line("text_%d" % i, -0.3, 1.6 - i * 0.08, -0.3 + rng.uniform(0.3, 0.55), 1.6 - i * 0.08, 0.015, p("ink", INK), y=-0.12)
    return 1.5, 2.0, 2.0


CHALK = (0.94, 0.93, 0.88)
SLATE = (0.30, 0.31, 0.33)


def prop_drillrack(rng, p):
    """Prop_DrillRack: the drill-yard's rack: two uprights and a rail, practice lances leaning in it, nibs blunted
    with wrapped cloth; one slot empty (Oriel's)."""
    wood, dark, shaft, cloth = p("wood", lerp(RUST, SILVER, 0.25)), p("dark", lerp(RUST, INK, 0.5)), p("shaft", lerp(RUST, PAPER, 0.35)), p("cloth", lerp(PAPER, SILVER, 0.3))
    box("upright_l", -1.0, 0.8, 0.12, 1.6, wood, y=0.1)
    box("upright_r", 1.0, 0.8, 0.12, 1.6, wood, y=0.1)
    box("rail_top", 0.0, 1.45, 2.2, 0.1, dark, y=0.05)
    box("rail_low", 0.0, 0.35, 2.2, 0.08, dark, y=0.05)
    for i, x in enumerate((-0.7, -0.35, 0.35, 0.7)):   # the slot at the middle is hers, and empty
        lean = rng.uniform(-0.08, 0.08)
        ob = box("lance_%d" % i, x, 1.05, 0.05, 2.1, shaft, y=0.0)
        ob.rotation_euler = (0, lean, 0)
        blob("wrap_%d" % i, rng, x + lean * 1.05, 2.08, 0.07, 0.11, cloth, y=-0.02, n=8, wobble=0.2)
    box("tag", 0.0, 1.62, 0.3, 0.16, p("paper", PAPER), y=-0.03)
    line("tag_mark", -0.08, 1.62, 0.08, 1.62, 0.02, p("ink", INK), y=-0.05)
    return 2.5, 2.3, 2.0


def prop_chalkboard(rng, p):
    """Prop_ChalkBoard: a slate on an easel with the morning's drill chalked on it: three figures, overhead, shove,
    sweep, each with its feet and an arrow; the Charter's combo, the way the Captain reads it back."""
    wood, slate, chalk = p("wood", lerp(RUST, INK, 0.3)), p("slate", SLATE), p("chalk", CHALK)
    for x, lean in ((-0.55, 0.12), (0.55, -0.12)):
        ob = box("leg_%s" % x, x, 0.9, 0.08, 1.9, wood, y=0.1)
        ob.rotation_euler = (0, lean, 0)
    box("tray", 0.0, 0.62, 1.4, 0.06, wood, y=-0.02)
    box("slate", 0.0, 1.25, 1.5, 1.05, slate, y=0.0)
    for k, (dx, dy) in enumerate(((0.0, 0.25), (0.25, 0.0), (0.2, -0.2))):   # overhead, shove, sweep: the stroke's way
        cx = -0.48 + 0.48 * k
        disc("head_%d" % k, cx, 1.55, 0.06, chalk, y=-0.28, n=10)
        line("body_%d" % k, cx, 1.49, cx, 1.27, 0.045, chalk, y=-0.28)
        line("foot_a_%d" % k, cx, 1.27, cx - 0.08, 1.12, 0.04, chalk, y=-0.28)
        line("foot_b_%d" % k, cx, 1.27, cx + 0.1, 1.12, 0.04, chalk, y=-0.28)
        line("lance_%d" % k, cx, 1.42, cx + 0.12 + dx * 0.4, 1.42 + dy * 0.6, 0.035, chalk, y=-0.28)
        line("arrow_%d" % k, cx - 0.1, 0.98, cx + 0.12, 0.98, 0.035, chalk, y=-0.28)
        line("arrow_h_%d" % k, cx + 0.12, 0.98, cx + 0.06, 1.02, 0.035, chalk, y=-0.28)
    box("chalk_stub", 0.4, 0.67, 0.12, 0.05, chalk, y=-0.28)
    return 1.75, 2.0, 2.0


def prop_paces(rng, p):
    """Prop_Paces: the yard's paces chalked along the edge of the flags, a tick a pace and every fourth numbered
    (Halvard counts in fours); stood in front of the floor's face, so it reads from the side."""
    chalk = p("chalk", CHALK)
    for i in range(17):
        x = -4.0 + 0.5 * i
        h = 0.22 if i % 4 == 0 else 0.12
        line("tick_%d" % i, x, 0.3, x + rng.uniform(-0.02, 0.02), 0.3 - h, 0.045, chalk, y=0.0)
    for k in range(5):
        x = -4.0 + 2.0 * k
        for j in range(k + 1):   # a tally, not a figure: the yard was chalked by birds who count
            line("tally_%d_%d" % (k, j), x + 0.08 + 0.05 * j, 0.06, x + 0.08 + 0.05 * j, 0.0, 0.035, chalk, y=0.0)
    line("edge", -4.1, 0.3, 4.1, 0.3, 0.035, chalk, y=0.0)
    return 8.5, 0.35, 2.0


# ---------------------------------------------------------------- Windreach (ENV-07): the Steppe's own

def prop_stone(rng, p):
    """Prop_Stone: a standing stone of the Nine: a tapering slab, lichen on its north face, the clans' notches."""
    stone, lichen, dark = p("stone", SILVER), p("lichen", lerp(BRASS, OLIVE, 0.5)), p("dark", lerp(INK, SILVER, 0.3))
    w, h = 1.1, 2.9
    polygon("slab", [(-w / 2, 0.0), (w / 2, 0.0), (w * 0.4, h * 0.92), (w * 0.08, h), (-w * 0.38, h * 0.95)], stone, y=0.05)
    for i in range(3):
        blob("lichen_%d" % i, rng, -w * 0.28, h * rng.uniform(0.35, 0.85), w * 0.16, h * 0.06, lichen, y=-0.02, n=10, wobble=0.35)
    for i in range(rng.randint(4, 7)):
        box("notch_%d" % i, w * 0.16, h * 0.22 + i * 0.17, 0.2, 0.035, dark, y=-0.04)
    line("crack", -w * 0.1, h * 0.3, w * 0.05, h * 0.7, 0.025, dark, y=-0.04)
    return 1.5, 3.0, 2.0


def prop_wagon(rng, p):
    """Prop_Wagon: a walking-wagon: a plank body on two tall wheels, a canvas hoop, the shafts down on the grass."""
    wood, dark, cloth, paper = p("wood", RUST), p("dark", lerp(RUST, INK, 0.5)), p("cloth", lerp(PAPER, OLIVE, 0.2)), p("paper", PAPER)
    w, h, r = 3.2, 2.6, 0.75
    for k, wx in enumerate((-w * 0.3, w * 0.3)):
        disc("wheel_%d" % k, wx, r, r, wood, y=0.02)
        disc("wheel_in_%d" % k, wx, r, r * 0.8, paper, y=0.01)
        for s in range(6):
            ob = box("spoke_%d_%d" % (k, s), 0.0, 0.0, 0.05, r * 1.7, dark, y=-0.01)
            ob.rotation_euler = (0, math.pi * s / 6, 0)
            ob.location = (wx, ob.location[1], r)
        disc("hub_%d" % k, wx, r, r * 0.16, dark, y=-0.02, n=10)
    box("body", 0.0, r + h * 0.22, w, h * 0.36, wood, y=-0.03)
    for i in range(4):
        box("plank_%d" % i, 0.0, r + h * 0.1 + i * h * 0.09, w, 0.02, dark, y=-0.04)
    top = r + h * 0.4
    pts = [(-w / 2, top), (w / 2, top)]
    for i in range(10, -1, -1):
        a = math.pi * i / 10
        pts.append((w / 2 * math.cos(a), top + h * 0.5 * math.sin(a)))
    polygon("hoop", pts, cloth, y=-0.05)
    for i in range(1, 4):
        a = math.pi * i / 4
        line("rib_%d" % i, w / 2 * math.cos(a) * 0.98, top, w / 2 * math.cos(a) * 0.98, top + h * 0.5 * math.sin(a) * 0.98, 0.02, dark, y=-0.06)
    line("shaft", w * 0.5, r + h * 0.1, w * 0.62, 0.1, 0.06, wood, y=-0.02)
    return 4.0, 3.0, 2.0


def prop_fire(rng, p):
    """Prop_Fire: the camp's fire: a ring of stones, logs, a flame, the glow over it."""
    stone, dark, flame, glow = p("stone", SILVER), p("dark", lerp(RUST, INK, 0.5)), p("flame", BRASS), p("glow", GLOW)
    for i in range(7):
        blob("ring_%d" % i, rng, -0.6 + 1.2 * i / 6, 0.09, 0.13, 0.08, stone, y=0.0, n=8, wobble=0.3)
    polygon("glow", [(-0.4, 0.1), (0.4, 0.1), (0.15, 1.05), (-0.12, 0.9)], glow, y=-0.02)
    polygon("flame", [(-0.22, 0.1), (0.22, 0.1), (0.06, 0.62), (-0.03, 0.52)], flame, y=-0.04)
    for i in range(3):
        box("log_%d" % i, (i - 1) * 0.16, 0.13, 0.5, 0.06, dark, y=-0.03)
    return 1.5, 1.25, 2.0


def prop_ashes(rng, p):
    """Prop_Ashes: where the camp isn't: the ring of stones round old ashes."""
    stone, ash = p("stone", SILVER), p("ash", lerp(SILVER, INK, 0.5))
    blob("ash", rng, 0.0, 0.1, 0.45, 0.09, ash, y=-0.02, n=12, wobble=0.3)
    for i in range(7):
        blob("ring_%d" % i, rng, -0.6 + 1.2 * i / 6, 0.09, 0.13, 0.08, stone, y=0.0, n=8, wobble=0.3)
    for i in range(3):
        box("char_%d" % i, rng.uniform(-0.3, 0.3), 0.1, rng.uniform(0.2, 0.4), 0.05, ash, y=-0.03)
    return 1.5, 0.5, 2.0


def prop_bedroll(rng, p):
    """Prop_Bedroll: a clan bedroll rolled tight and tied, for anyone walking with them."""
    cloth, dark, rope = p("cloth", lerp(RUST, PAPER, 0.3)), p("dark", lerp(RUST, INK, 0.5)), p("rope", ROPE)
    disc("roll", 0.0, 0.22, 0.22, cloth, y=0.0, n=16, squash=1.0)
    box("body", 0.0, 0.22, 1.1, 0.44, cloth, y=0.02)
    disc("end", 0.55, 0.22, 0.22, dark, y=-0.02, n=16)
    disc("end_in", 0.55, 0.22, 0.12, cloth, y=-0.04, n=12)
    for x in (-0.3, 0.2):
        box("tie_%s" % x, x, 0.22, 0.05, 0.48, rope, y=-0.03)
    return 1.5, 0.5, 2.0


def prop_hull(rng, p):
    """Prop_Hull: a boat on its side in the dry river's mud, planks, a name on the bow, a smudge's shadow in it."""
    wood, dark, paper, mud = p("wood", RUST), p("dark", lerp(RUST, INK, 0.55)), p("paper", PAPER), p("mud", lerp(OLIVE, PAPER, 0.5))
    w, h = 2.8, 1.4
    pts = []
    for i in range(16):
        a = 2 * math.pi * i / 16
        pts.append((w / 2 * math.cos(a), h / 2 + h / 2 * math.sin(a) * (1.0 if math.sin(a) > 0 else 0.5)))
    polygon("hull", pts, wood, y=0.0)
    for i in range(4):
        line("plank_%d" % i, -w * 0.45, h * 0.2 + i * h * 0.18, w * 0.45, h * 0.25 + i * h * 0.17, 0.02, dark, y=-0.03)
    box("keel", 0.0, h * 0.62, w * 0.9, 0.05, dark, y=-0.04)
    for i in range(3):
        line("name_%d" % i, w * 0.25 + i * 0.12, h * 0.3, w * 0.3 + i * 0.12, h * 0.5, 0.015, paper, y=-0.05)
    blob("mud", rng, 0.0, 0.05, w * 0.5, 0.06, mud, y=0.05, n=12, wobble=0.3)
    return 3.0, 1.5, 2.0


def prop_reeds(rng, p):
    """Prop_Reeds: dead reeds on the bank, every head leaning east."""
    stalk, head = p("stalk", lerp(OLIVE, INK, 0.3)), p("head", lerp(OLIVE, INK, 0.5))
    for i in range(9):
        x = -0.8 + 1.6 * (i + rng.uniform(0.2, 0.8)) / 9
        h = rng.uniform(1.0, 1.9)
        tx, tz = x + 0.3 * h, h
        line("stalk_%d" % i, x, 0.0, tx, tz, 0.03, stalk, y=0.0 - 0.002 * i)
        disc("head_%d" % i, tx, tz, rng.uniform(0.05, 0.09), head, y=-0.02, n=8)
    return 2.0, 2.0, 2.0


def prop_lipstone(rng, p):
    """Prop_LipStone: a flat stone on the Gate's lip, a place carved in it: what the clan sings."""
    stone, dark, lichen = p("stone", lerp(SILVER, PAPER, 0.2)), p("dark", lerp(INK, SILVER, 0.2)), p("lichen", lerp(BRASS, OLIVE, 0.5))
    polygon("flat", [(-0.7, 0.0), (0.7, 0.0), (0.62, 0.4), (-0.66, 0.42)], stone, y=0.0)
    for k in range(4):
        line("carve_%d" % k, -0.45 + k * 0.25, 0.14, -0.3 + k * 0.25, 0.3, 0.025, dark, y=-0.04)
    blob("lichen", rng, 0.5, 0.35, 0.1, 0.05, lichen, y=-0.03, n=8)
    return 1.5, 0.5, 2.0


def prop_swirl(rng, p):
    """Prop_Swirl: one turn of an updraft: an ink ribbon winding up the column, tileable top to bottom (the room
    stacks and scrolls it)."""
    ink, pale = p("swirl", lerp(STEEL, INK, 0.3)), p("pale", lerp(STEEL, PAPER, 0.6))
    n = 28
    prev = None
    for i in range(n + 1):
        t = i / n
        pt = (0.78 * math.sin(t * 2 * math.pi), t * 2.0)
        if prev is not None:
            th = 0.09 * (0.55 + 0.45 * abs(math.cos(t * 2 * math.pi)))
            line("rib_%d" % i, prev[0], prev[1], pt[0], pt[1], th, ink, y=0.0)
        prev = pt
    prev = None
    for i in range(n + 1):
        t = i / n
        pt = (0.5 * math.sin(t * 2 * math.pi + 2.2), t * 2.0)
        if prev is not None:
            line("thin_%d" % i, prev[0], prev[1], pt[0], pt[1], 0.035, pale, y=0.05)
        prev = pt
    return 2.0, 2.0, 1.6


def prop_hearth(rng, p):
    """Prop_Hearth: Idrenne's hearth: the ring of stones, the fire, the cooking-stone flat over it, a pot on a tripod."""
    stone, dark, flame, glow, key, iron = p("stone", SILVER), p("dark", lerp(RUST, INK, 0.5)), p("flame", BRASS), p("glow", GLOW), p("key", lerp(STEEL, INK, 0.3)), p("iron", STEEL)
    for i in range(9):
        blob("ring_%d" % i, rng, -1.0 + 2.0 * i / 8, 0.1, 0.15, 0.09, stone, y=0.0, n=8, wobble=0.3)
    polygon("glow", [(-0.5, 0.12), (0.5, 0.12), (0.2, 0.75), (-0.15, 0.7)], glow, y=-0.02)
    polygon("flame", [(-0.28, 0.12), (0.28, 0.12), (0.08, 0.5), (-0.04, 0.44)], flame, y=-0.04)
    box("keystone", 0.0, 0.58, 1.0, 0.18, key, y=-0.06)
    for i in range(3):
        a = -0.5 + i * 0.5
        line("leg_%d" % i, a * 1.2, 0.15, a * 0.15, 1.9, 0.05, iron, y=0.03)
    line("chain", 0.0, 1.85, 0.0, 1.35, 0.03, iron, y=-0.01)
    disc("pot", 0.0, 1.1, 0.38, iron, y=-0.03, n=14, squash=0.7)
    return 2.5, 2.0, 2.0


def _grass_tuft(rng, p, n, h, w):
    grass, dark = p("grass", lerp(OLIVE, PAPER, 0.15)), p("dark", lerp(OLIVE, INK, 0.3))
    for i in range(n):
        t = (i + 0.5) / n - 0.5
        bh = h * rng.uniform(0.55, 1.0)
        lean = 0.3 + t * 0.6
        x = t * w
        polygon("blade_%d" % i, [(x - 0.035, 0.0), (x + 0.035, 0.0), (x + lean * bh, bh)], dark if i % 3 == 0 else grass, y=-0.003 * i)


def prop_grass_a(rng, p):
    """Prop_Grass_A: a tuft of long grass, leaning east; the room's grass is rows of these, and they part for her."""
    _grass_tuft(rng, p, 7, 1.1, 0.5)
    return 1.0, 1.25, 1.6


def prop_grass_b(rng, p):
    """Prop_Grass_B: a thinner tuft."""
    _grass_tuft(rng, p, 5, 1.2, 0.35)
    return 1.0, 1.25, 1.6


def prop_grass_c(rng, p):
    """Prop_Grass_C: a broad low tuft."""
    _grass_tuft(rng, p, 9, 0.9, 0.7)
    return 1.0, 1.25, 1.6


def prop_grass_tall(rng, p):
    """Prop_Grass_Tall: the high grass past the Gate: a tuft over anyone's head, seed-heads on it."""
    _grass_tuft(rng, p, 9, 2.3, 0.8)
    head = p("head", lerp(BRASS, OLIVE, 0.4))
    for i in range(4):
        disc("head_%d" % i, rng.uniform(0.2, 0.9), rng.uniform(1.9, 2.4), rng.uniform(0.06, 0.1), head, y=-0.05, n=8)
    return 1.5, 2.5, 1.6


# ---------------------------------------------------------------- the Greyfold (ENV-08): the threshold's own

def prop_fence(rng, p):
    """Prop_Fence: the Guild fence with no gate at the orchard road's end: posts, two rails, a notice nailed on."""
    wood, dark, sheet, ink = p("wood", RUST), p("dark", lerp(RUST, INK, 0.5)), p("sheet", PAPER), p("ink", INK)
    for x in (-1.7, -0.6, 0.6, 1.7):
        box("post_%s" % x, x, 1.1, 0.18, 2.2, wood, y=0.05)
    box("rail_a", 0.0, 0.9, 3.8, 0.12, wood, y=0.0)
    box("rail_b", 0.0, 1.6, 3.8, 0.12, wood, y=0.0)
    box("notice", 0.0, 1.55, 0.8, 0.6, sheet, y=-0.05)
    for k in range(3):
        line("line_%d" % k, -0.3, 1.72 - k * 0.14, -0.3 + rng.uniform(0.35, 0.6), 1.72 - k * 0.14, 0.02, ink, y=-0.1)
    return 4.0, 2.5, 1.6


def prop_milepost(rng, p):
    """Prop_Milepost: a milepost for a road nobody finished: a squared stone with a rounded top, the count cut in it."""
    stone, dark = p("stone", SILVER), p("dark", lerp(INK, SILVER, 0.3))
    polygon("post", [(-0.3, 0.0), (0.3, 0.0), (0.3, 1.6), (0.0, 2.0), (-0.3, 1.6)], stone, y=0.0)
    for k in range(rng.randint(0, 3)):
        box("mark_%d" % k, 0.0, 0.9 - k * 0.2, 0.26, 0.035, dark, y=-0.04)
    return 0.75, 2.0, 1.6


def prop_tent(rng, p):
    """Prop_Tent: a Guild tent: a ridge-pole, the cloth down either side, one side pegged open."""
    cloth, dark, paper = p("cloth", lerp(SILVER, PAPER, 0.5)), p("dark", lerp(INK, SILVER, 0.3)), p("paper", PAPER)
    polygon("tent", [(-1.5, 0.0), (1.5, 0.0), (0.0, 2.4)], cloth, y=0.0)
    line("pole", 0.0, 0.0, 0.0, 2.4, 0.04, dark, y=-0.02)
    polygon("door", [(0.15, 0.0), (1.25, 0.0), (0.3, 1.3)], paper, y=-0.03)
    for x in (-1.5, 0.0, 1.5):
        line("peg_%s" % x, x, 0.0, x + 0.1, 0.1, 0.03, dark, y=-0.02)
    return 3.0, 2.5, 1.6


def prop_stake(rng, p):
    """Prop_Stake: a Guild survey stake driven into the white, a tether tied off on it and run up and away."""
    wood, rope = p("wood", RUST), p("rope", ROPE)
    polygon("stake", [(-0.08, 0.0), (0.08, 0.0), (0.05, 1.5), (-0.05, 1.5)], wood, y=0.0)
    line("rope", 0.0, 1.3, 0.25, 2.0, 0.025, rope, y=-0.02)
    return 0.5, 2.0, 1.6


def prop_cobble(rng, p):
    """Prop_Cobble: one stride of the Road That Stops: cobbles in a row, the kind her lantern draws."""
    stone, dark, pale = p("stone", SILVER), p("dark", lerp(INK, SILVER, 0.3)), p("pale", lerp(SILVER, PAPER, 0.6))
    box("bed", 0.0, 0.2, 2.4, 0.4, pale, y=0.05)
    x = -1.2
    i = 0
    while x < 1.2:
        w = rng.uniform(0.4, 0.6)
        blob("c_%d" % i, rng, min(x + w / 2, 1.2 - w / 2), 0.26, w / 2, 0.18, stone if i % 3 else pale, y=0.0, n=10, wobble=0.12)
        x += w + 0.03
        i += 1
    return 2.5, 0.5, 1.6


def prop_atlas(rng, p):
    """Prop_Atlas: Isolde's complete atlas open on a stone, every page drawn."""
    stone, paper, ink = p("stone", SILVER), p("paper", PAPER), p("ink", INK)
    box("stone", 0.0, 0.3, 1.4, 0.6, stone, y=0.05)
    polygon("page_l", [(-0.7, 0.58), (0.0, 0.58), (0.0, 0.72), (-0.7, 0.82)], paper, y=-0.02)
    polygon("page_r", [(0.0, 0.58), (0.7, 0.58), (0.7, 0.82), (0.0, 0.72)], paper, y=-0.02)
    for k in range(4):
        line("l_%d" % k, -0.6, 0.66 + 0.03 * k, -0.1, 0.65 + 0.03 * k, 0.012, ink, y=-0.06)
        line("r_%d" % k, 0.1, 0.65 + 0.03 * k, 0.6, 0.66 + 0.03 * k, 0.012, ink, y=-0.06)
    return 1.5, 1.0, 1.6


def prop_footprints(rng, p):
    """Prop_Footprints: a line of footprints in the road's last dust, stopping mid-stride where the road does."""
    dark = p("dark", lerp(INK, SILVER, 0.4))
    for i in range(6):
        x = -1.3 + i * 0.5
        z = 0.1 + (0.15 if i % 2 else 0.0)
        for k in range(3):
            line("f_%d_%d" % (i, k), x, z, x + 0.18, z + (k - 1) * 0.1, 0.03, dark, y=0.0)
    return 3.0, 0.5, 1.6


def prop_beam(rng, p):
    """Prop_Beam: a tent-post of the Edge Camp with "I. M." cut in it and a wren in four strokes under."""
    wood, dark = p("wood", RUST), p("dark", lerp(INK, RUST, 0.4))
    box("post", 0.0, 1.5, 0.3, 3.0, wood, y=0.0)
    line("i", -0.06, 2.2, -0.06, 2.5, 0.03, dark, y=-0.04)
    line("m1", 0.02, 2.2, 0.02, 2.5, 0.03, dark, y=-0.04)
    line("m2", 0.02, 2.5, 0.1, 2.3, 0.025, dark, y=-0.04)
    line("w1", -0.1, 1.9, 0.0, 1.8, 0.025, dark, y=-0.04)
    line("w2", 0.0, 1.8, 0.1, 1.9, 0.025, dark, y=-0.04)
    line("w3", -0.02, 1.8, 0.04, 2.0, 0.02, dark, y=-0.04)
    line("w4", 0.04, 1.92, 0.12, 1.93, 0.02, dark, y=-0.04)
    return 0.5, 3.0, 1.6


# ---------------------------------------------------------------- the Blank (ENV-08): the fixed islands' own

def prop_house(rng, p):
    """Prop_House: a Hollow house: low walls under a deep roof, a door, a window, a chimney, grey."""
    wall, roof, dark = p("wall", RUST), p("roof", SILVER), p("dark", lerp(INK, SILVER, 0.2))
    box("wall", 0.0, 0.75, 2.6, 1.5, wall, y=0.0)
    polygon("roof", [(-1.5, 1.45), (1.5, 1.45), (0.4, 2.5), (-0.4, 2.5)], roof, y=-0.02)
    box("door", -0.6, 0.5, 0.44, 1.0, dark, y=-0.03)
    box("win", 0.6, 0.85, 0.44, 0.4, dark, y=-0.03)
    box("winx", 0.6, 0.85, 0.36, 0.03, wall, y=-0.04)
    box("chim", 0.95, 2.15, 0.22, 0.7, dark, y=-0.01)
    return 3.0, 2.5, 1.6


def prop_island(rng, p):
    """Prop_Island: an island going past the drift: a slab of grey ground, a ragged underside, a roof or two on it."""
    top, under, dark, wall = p("top", RUST), p("under", lerp(SILVER, PAPER, 0.5)), p("dark", lerp(INK, SILVER, 0.2)), p("wall", SILVER)
    pts = [(-2.5, 1.2), (2.5, 1.2)]
    for i in range(9, -1, -1):
        t = i / 9
        pts.append((-2.5 + 5.0 * t, 1.2 - 1.1 * (0.3 + 0.7 * math.sin(t * math.pi)) * rng.uniform(0.7, 1.0)))
    polygon("under", pts, under, y=0.02)
    box("top", 0.0, 1.3, 5.0, 0.2, top, y=0.0)
    for k in range(2):
        hx = -1.2 + k * 2.0
        box("h_%d" % k, hx, 1.6, 0.9, 0.4, wall, y=-0.02)
        polygon("r_%d" % k, [(hx - 0.55, 1.78), (hx + 0.55, 1.78), (hx, 2.0)], dark, y=-0.03)
    for k in range(6):
        bx = -2.3 + k * 0.9
        line("g_%d" % k, bx, 1.4, bx + 0.03, 1.6, 0.03, dark, y=-0.02)
    return 5.0, 2.0, 1.6


def prop_chair(rng, p):
    """Prop_Chair: the chair Corvin drew for her, a little too tall."""
    wood, dark = p("wood", RUST), p("dark", lerp(INK, SILVER, 0.3))
    box("seat", 0.0, 0.7, 0.8, 0.08, wood, y=0.0)
    box("back", -0.34, 1.1, 0.08, 0.9, wood, y=0.0)
    box("rail", -0.34, 1.45, 0.08, 0.08, dark, y=-0.02)
    for x in (-0.34, 0.34):
        line("leg_%s" % x, x, 0.0, x, 0.7, 0.06, wood, y=0.0)
    return 1.0, 1.5, 1.6


def prop_crayon(rng, p):
    """Prop_Crayon: Corra's drawing on the wall: her father, tall, a compass in his wing, no face."""
    crayon = p("crayon", (0.72, 0.56, 0.30))
    t = 0.08
    line("leg1", -0.2, 0.0, -0.1, 1.0, t, crayon, y=0.0)
    line("leg2", 0.25, 0.0, 0.1, 1.0, t, crayon, y=0.0)
    for k in range(5):
        line("body_%d" % k, -0.5 + rng.uniform(-0.08, 0.08), 1.0 + 0.16 * k, 0.5 + rng.uniform(-0.08, 0.08), 1.1 + 0.16 * k, t, crayon, y=-0.001 * k)
    line("neck", 0.3, 1.8, 0.5, 2.15, t, crayon, y=0.0)
    disc("head", 0.55, 2.2, 0.18, crayon, y=-0.01, n=12)
    line("bill", 0.68, 2.18, 0.95, 2.12, t * 0.7, crayon, y=0.0)
    disc("comp", -0.15, 1.5, 0.16, crayon, y=-0.02, n=10)
    disc("compin", -0.15, 1.5, 0.1, p("paper", PAPER), y=-0.03, n=10)
    return 2.0, 2.5, 1.6


def prop_beacon(rng, p):
    """Prop_Beacon: Aury's lamp: the lens on its stand, lit, the glow round it."""
    iron, gold, glow = p("iron", STEEL), p("gold", BRASS), p("glow", GLOW)
    box("stand", 0.0, 0.6, 0.5, 1.2, iron, y=0.0)
    box("foot", 0.0, 0.06, 0.9, 0.12, iron, y=-0.01)
    disc("glow", 0.0, 1.8, 0.7, glow, y=0.3, n=20)
    disc("lens", 0.0, 1.8, 0.45, gold, y=-0.02, n=16)
    box("cap", 0.0, 2.3, 0.6, 0.1, iron, y=-0.03)
    return 1.5, 2.5, 1.6


def prop_well(rng, p):
    """Prop_Well: the Hollow's well: a stone ring, a post, a bucket on its rope."""
    stone, dark, rope = p("stone", RUST), p("dark", lerp(INK, SILVER, 0.2)), p("rope", ROPE)
    box("ring", 0.0, 0.35, 1.3, 0.7, stone, y=0.0)
    box("mouth", 0.0, 0.62, 1.0, 0.12, dark, y=-0.02)
    line("post", 0.5, 0.7, 0.5, 1.5, 0.06, dark, y=-0.01)
    line("arm", 0.5, 1.45, -0.3, 1.45, 0.06, dark, y=-0.01)
    line("rope", -0.3, 1.45, -0.3, 0.95, 0.02, rope, y=-0.02)
    box("bucket", -0.3, 0.85, 0.24, 0.22, dark, y=-0.03)
    return 1.5, 1.5, 1.6


def prop_door(rng, p):
    """Prop_Door: a Guild office door in the grey capital, its nameplate polished bright at a chick's height."""
    frame, door, brass, dark = p("frame", RUST), p("door", SILVER), p("brass", BRASS), p("dark", lerp(INK, SILVER, 0.2))
    box("frame", 0.0, 1.25, 1.4, 2.5, frame, y=0.05)
    box("door", 0.0, 1.2, 1.1, 2.4, door, y=0.0)
    box("panel_a", 0.0, 1.8, 0.7, 0.7, dark, y=-0.02)
    box("panel_b", 0.0, 0.7, 0.7, 0.9, dark, y=-0.02)
    box("plate", 0.0, 1.3, 0.5, 0.14, brass, y=-0.04)
    disc("knob", 0.4, 1.2, 0.05, dark, y=-0.04, n=8)
    return 1.5, 2.5, 1.6


# ---------------------------------------------------------------- the dressing (ENV-06): what the rooms say without anyone saying it
# One drawing per piece of environment.md, in its region's palette; a piece that changes with its place has a second
# drawing (_Chalk, _Up, _Lit, _Swept, _Open, _Complete) and DressingProp shows one or the other. The thing speaks, so the
# words on it are marks, not letters: the hand pass letters them.

def marks(prefix, x0, z, count, material, step=0.11, h=0.07, t=0.022, y=-0.31, gap_every=0):
    """A row of cut or chalked characters as short upright strokes."""
    for i in range(count):
        x = x0 + i * step + (0.04 if gap_every and i % gap_every == 0 and i else 0.0)
        line("%s_%d" % (prefix, i), x, z - h / 2, x + 0.01, z + h / 2, t, material, y=y)


def text_rows(prefix, x0, z0, rows, width, material, dz=0.1, t=0.016, y=-0.33, rng=None, jitter=0.0):
    """Rows of script as thin horizontal strokes."""
    for i in range(rows):
        w = width if rng is None else width * rng.uniform(1 - jitter, 1)
        line("%s_%d" % (prefix, i), x0, z0 - i * dz, x0 + w, z0 - i * dz, t, material, y=y)


def tally(prefix, x0, z, count, material, step=0.06, h=0.14, t=0.02, y=-0.31):
    """Tally strokes, the fifth across the four."""
    for i in range(count):
        g, k = divmod(i, 5)
        x = x0 + g * (step * 5 + 0.05) + (k if k < 4 else 1.5) * step
        if k < 4:
            line("%s_%d" % (prefix, i), x, z - h / 2, x + 0.01, z + h / 2, t, material, y=y)
        else:
            line("%s_%d" % (prefix, i), x0 + g * (step * 5 + 0.05) - 0.02, z - h / 2, x0 + g * (step * 5 + 0.05) + 3 * step + 0.03, z + h / 2, t, material, y=y - 0.01)


# ---- Saltmarrow

def prop_tetherposts(rng, p):
    """Prop_TetherPosts: the Shore's tether-posts along the tideline, their ropes cut a wingspan out; eleven notches on
    one, a bracelet knotted on another, and nothing after the eleventh."""
    wood, iron, rope, frayed, dark, brass = p("wood", lerp(RUST, INK, 0.3)), p("iron", STEEL), p("rope", ROPE), p("frayed", lerp(ROPE, PAPER, 0.4)), p("dark", lerp(INK, RUST, 0.3)), p("brass", BRASS)
    for i, (x, h) in enumerate(((-1.1, 1.3), (0.0, 1.5), (1.1, 1.2))):
        box("post_%d" % i, x, h / 2, 0.2, h, wood, y=0.1 - 0.02 * i)
        box("band_%d" % i, x, h - 0.2, 0.24, 0.06, iron, y=0.05)
        disc("eye_%d" % i, x, h - 0.05, 0.06, iron, y=-0.27, n=8)
        line("rope_%d" % i, x, h - 0.05, x - 0.55, h + 0.35, 0.03, rope, y=-0.30)
        for k in range(3):   # the cut end, frayed
            line("fray_%d_%d" % (i, k), x - 0.55, h + 0.35, x - 0.68 - 0.04 * k, h + 0.42 - 0.08 * k, 0.015, frayed, y=-0.31)
    for k in range(11):   # Brin's notches
        line("notch_%d" % k, -1.19, 0.25 + k * 0.08, -1.03, 0.25 + k * 0.08, 0.018, dark, y=-0.31)
    for k in range(5):    # the bracelet on the third
        disc("bead_%d" % k, 1.0 + 0.05 * k, 0.72 + 0.03 * (k % 2), 0.025, brass, y=-0.33, n=6)
    return 3.0, 2.0, 2.0


def prop_priceboard(rng, p):
    """Prop_PriceBoard: the Ferrymen's price board by the desk: slate on a post, chalk rows, the return price a blank,
    and under it, in another hand, two chalked words."""
    wood, slate, chalk, faint = p("wood", lerp(RUST, INK, 0.3)), p("slate", lerp(INK, SILVER, 0.35)), p("chalk", lerp(PAPER, SILVER, 0.15)), p("faint", lerp(PAPER, SILVER, 0.45))
    box("post", 0.0, 0.9, 0.12, 1.8, wood, y=0.1)
    box("board", 0.0, 1.4, 1.3, 1.0, slate, y=0.0)
    box("frame", 0.0, 1.4, 1.36, 1.06, wood, y=0.03)
    for i in range(3):
        text_rows("row%d" % i, -0.5, 1.72 - i * 0.2, 1, 0.55, chalk, t=0.05)
        line("price_%d" % i, 0.35, 1.72 - i * 0.2, 0.5, 1.72 - i * 0.2, 0.05, chalk, y=-0.33)
    text_rows("ask", -0.5, 1.12, 1, 0.7, chalk, t=0.05)   # "ask", and no price after it
    text_rows("nobody", -0.3, 0.98, 1, 0.5, faint, t=0.045)
    return 1.5, 2.0, 1.2


def _lintels(rng, p, chalk):
    wood, frame, dark, white, cut, chalkm = p("wood", lerp(RUST, SILVER, 0.3)), p("frame", lerp(RUST, INK, 0.4)), p("dark", lerp(INK, RUST, 0.2)), p("white", lerp(PAPER, (1, 1, 1), 0.5)), p("cut", lerp(INK, RUST, 0.4)), p("chalk", lerp(PAPER, (1, 1, 1), 0.8))
    for i, x in enumerate((-0.8, 0.8)):
        box("jamb_l_%d" % i, x - 0.5, 0.95, 0.12, 1.9, frame, y=0.05)
        box("jamb_r_%d" % i, x + 0.5, 0.95, 0.12, 1.9, frame, y=0.05)
        box("door_%d" % i, x, 0.9, 0.9, 1.8, white if i == 0 else p("door_wood", lerp(RUST, SILVER, 0.5)), y=0.02)
        box("lintel_%d" % i, x, 2.05, 1.3, 0.3, wood, y=0.0)
        marks("name_%d" % i, x - 0.45, 2.05, 8, cut, step=0.12, h=0.12)
        if chalk:
            marks("chalk_%d" % i, x - 0.45, 2.05, 8, chalkm, step=0.12, h=0.12, t=0.06, y=-0.34)
    return 3.0, 2.5, 2.0


def prop_lintels(rng, p):
    """Prop_Lintels: Merrow's End: a song's name carved over every door; one door already white paper."""
    return _lintels(rng, p, False)


def prop_lintels_chalk(rng, p):
    """Prop_Lintels_Chalk: the same lintels with fresh chalk in every letter (the place held or anchored)."""
    return _lintels(rng, p, True)


def prop_log(rng, p):
    """Prop_Log: the first lighthouse's log, open on the lamp-room sill, weighted with a shell."""
    page, ink, shell, cover = p("page", PAPER), p("ink", INK), p("shell", lerp(SILVER, PAPER, 0.5)), p("cover", lerp(RUST, INK, 0.5))
    polygon("cover", [(-0.5, 0.0), (0.5, 0.0), (0.5, 0.06), (-0.5, 0.06)], cover, y=0.05)
    polygon("page_l", [(-0.48, 0.04), (-0.02, 0.04), (-0.02, 0.4), (-0.48, 0.34)], page, y=0.0)
    polygon("page_r", [(0.02, 0.04), (0.48, 0.04), (0.48, 0.34), (0.02, 0.4)], page, y=0.0)
    for i in range(5):
        line("entry_l%d" % i, -0.42, 0.3 - i * 0.05, -0.1, 0.31 - i * 0.05, 0.01, ink, y=-0.30)
        line("entry_r%d" % i, 0.08, 0.31 - i * 0.05, 0.4, 0.3 - i * 0.05, 0.01, ink, y=-0.30)
    disc("shell", 0.3, 0.3, 0.09, shell, y=-0.33, n=12, squash=0.7)
    return 1.0, 0.5, 2.0


def prop_tapestry(rng, p):
    """Prop_Tapestry: the Salt Chapel's tapestry: a flock over the sea, the birds eaten out by salt so only their holes
    remain; salt climbing the cloth from the hem."""
    rod, sky, sea, hole, salt, fringe = p("rod", lerp(RUST, INK, 0.4)), p("sky", lerp(WET, PAPER, 0.3)), p("sea", lerp(WET, INK, 0.25)), p("hole", lerp(PAPER, (1, 1, 1), 0.6)), p("salt", lerp(PAPER, (1, 1, 1), 0.3)), p("fringe", lerp(ROPE, PAPER, 0.3))
    box("rod", 0.0, 2.9, 2.5, 0.08, rod, y=0.05)
    box("cloth", 0.0, 1.65, 2.2, 2.4, sky, y=0.0)
    polygon("sea", [(-1.1, 0.45), (1.1, 0.45), (1.1, 1.0), (0.6, 0.92), (0.0, 1.02), (-0.6, 0.9), (-1.1, 0.98)], sea, y=-0.27)
    for i in range(7):
        x, z = -0.8 + 0.27 * i + 0.05 * (i % 2), 1.6 + 0.35 * ((i * 3) % 4) / 2
        polygon("hole_%d" % i, [(x - 0.14, z), (x - 0.04, z + 0.07), (x, z + 0.02), (x + 0.04, z + 0.07), (x + 0.14, z), (x, z - 0.03)], hole, y=-0.29)
    for k in range(9):   # salt from the hem
        blob("salt_%d" % k, rng, -1.0 + 0.25 * k, 0.5 + rng.uniform(0.0, 0.25), 0.16, 0.1, salt, y=-0.28, n=10)
    for k in range(11):
        line("fringe_%d" % k, -1.0 + 0.2 * k, 0.45, -1.0 + 0.2 * k + 0.02, 0.3, 0.02, fringe, y=-0.27)
    return 2.5, 3.0, 1.8


def _chapeldoor(rng, p, open_):
    stone, salt, paper, ink, gap, scrap = p("stone", SILVER), p("salt", lerp(PAPER, (1, 1, 1), 0.4)), p("paper", lerp(PAPER, (1, 1, 1), 0.2)), p("ink", INK), p("gap", lerp(INK, SILVER, 0.15)), p("scrap", lerp(PAPER, BRASS, 0.2))
    box("jamb_l", -0.6, 1.2, 0.2, 2.4, stone, y=0.05)
    box("jamb_r", 0.6, 1.2, 0.2, 2.4, stone, y=0.05)
    box("head", 0.0, 2.38, 1.4, 0.24, stone, y=0.05)
    if not open_:
        box("door", 0.0, 1.15, 1.0, 2.3, paper, y=0.0)
        line("cut", -0.3, 1.55, 0.3, 1.55, 0.025, ink, y=-0.29)   # "Sing me in."
    else:
        box("gap", 0.0, 1.15, 1.0, 2.3, gap, y=0.02)
        polygon("door", [(-0.5, 0.0), (-0.1, 0.12), (-0.1, 2.18), (-0.5, 2.3)], paper, y=-0.28)
        box("shelf", 0.1, 1.0, 0.6, 0.05, stone, y=-0.27)
        box("scrap_a", -0.02, 1.08, 0.18, 0.12, scrap, y=-0.29)
        box("scrap_b", 0.22, 1.09, 0.18, 0.12, scrap, y=-0.30)
    for k in range(4):
        blob("salt_%d" % k, rng, -0.45 + 0.3 * k, 0.1 + rng.uniform(0, 0.15), 0.18, 0.1, salt, y=-0.31, n=10)
    return 1.5, 2.5, 2.0


def prop_chapeldoor(rng, p):
    """Prop_ChapelDoor: the chapel's door of salt-eaten paper behind the altar, cut with one line."""
    return _chapeldoor(rng, p, False)


def prop_chapeldoor_open(rng, p):
    """Prop_ChapelDoor_Open: the same door open on the reliquary, its two scraps on the shelf (saltmarrow.chapel.door_open)."""
    return _chapeldoor(rng, p, True)


def prop_ladders(rng, p):
    """Prop_Ladders: a stilt-roost's door with a landing ledge and no steps, a rope ladder lashed on later in another
    colour, and a line of wing-bindings drying beside it."""
    stilt, wall, dark, ledge, rope, rung, cord, binding = p("stilt", lerp(RUST, INK, 0.35)), p("wall", lerp(OLIVE, PAPER, 0.5)), p("dark", lerp(INK, RUST, 0.2)), p("ledge", lerp(RUST, SILVER, 0.3)), p("rope", lerp(ROPE, OLIVE, 0.4)), p("rung", lerp(RUST, PAPER, 0.3)), p("cord", ROPE), p("binding", lerp(PAPER, (1, 1, 1), 0.3))
    box("stilt_a", -0.55, 1.5, 0.14, 3.0, stilt, y=0.1)
    box("stilt_b", 0.0, 1.5, 0.14, 3.0, stilt, y=0.1)
    box("wall", -0.28, 3.4, 1.0, 1.2, wall, y=0.05)
    box("door", -0.28, 3.3, 0.5, 0.9, dark, y=0.0)
    box("ledge", -0.28, 2.82, 0.9, 0.1, ledge, y=-0.27)
    for x in (-0.18, 0.12):
        line("rail_%s" % x, x, 0.1, x + 0.1, 2.8, 0.03, rope, y=-0.29)
    for k in range(8):
        line("rung_%d" % k, -0.2 + 0.004 * k, 0.35 + 0.32 * k, 0.16 + 0.004 * k, 0.35 + 0.32 * k, 0.04, rung, y=-0.31)
    line("cord", 0.08, 2.3, 0.95, 2.45, 0.015, cord, y=-0.30)
    for k in range(4):
        x = 0.3 + 0.18 * k
        polygon("binding_%d" % k, [(x - 0.04, 2.33 + 0.03 * k), (x + 0.04, 2.33 + 0.03 * k), (x + 0.05, 1.95 + 0.03 * k), (x - 0.05, 1.98 + 0.03 * k)], binding, y=-0.32)
    return 2.0, 4.0, 2.0


def prop_moorings(rng, p):
    """Prop_Moorings: two boat-shaped patches of paper on the landing's water where the boats were moored, their painted
    names still floating on the white."""
    water, patch, name = p("water", lerp(WET, PAPER, 0.3)), p("patch", lerp(PAPER, (1, 1, 1), 0.6)), p("name", lerp(INK, RUST, 0.3))
    for k in range(4):
        line("wave_%d" % k, -1.4 + 0.1 * k, 0.12 + 0.14 * k, -0.6 + 0.3 * k, 0.12 + 0.14 * k, 0.012, water, y=0.05)
        line("wave_r%d" % k, 0.4 + 0.1 * k, 0.1 + 0.15 * k, 1.3, 0.1 + 0.15 * k, 0.012, water, y=0.05)
    for i, x in enumerate((-0.75, 0.75)):
        polygon("patch_%d" % i, [(x - 0.7, 0.3), (x - 0.5, 0.18), (x + 0.5, 0.18), (x + 0.7, 0.34), (x + 0.4, 0.44), (x - 0.4, 0.44)], patch, y=0.0)
        marks("name_%d" % i, x - 0.25, 0.31, 5, name, step=0.09, h=0.05, t=0.016, y=-0.29)
    return 3.0, 0.75, 1.4


def prop_keeper(rng, p):
    """Prop_Keeper: the faded third light's lamp room from outside: a grey keeper's silhouette at the lamp, never seen
    from within."""
    frame, glass, lamp, glow, grey = p("frame", lerp(SILVER, PAPER, 0.5)), p("glass", lerp(PAPER, (1, 1, 1), 0.3)), p("lamp", lerp(SILVER, PAPER, 0.3)), p("glow", lerp(GLOW, PAPER, 0.7)), p("grey", lerp(SILVER, PAPER, 0.45))
    box("sill", 0.0, 0.1, 2.0, 0.2, frame, y=0.05)
    box("glass", 0.0, 1.5, 1.7, 2.6, glass, y=0.1)
    for x in (-0.85, 0.85):
        box("mullion_%s" % x, x, 1.5, 0.1, 2.8, frame, y=0.0)
    box("head", 0.0, 2.85, 2.0, 0.1, frame, y=0.0)
    disc("glow", -0.45, 1.5, 0.55, glow, y=-0.25, n=24)
    box("stand", -0.45, 0.7, 0.3, 0.9, lamp, y=-0.15)
    disc("lens", -0.45, 1.5, 0.3, lamp, y=-0.3, n=16)
    polygon("body", [(0.2, 0.2), (0.75, 0.2), (0.8, 1.1), (0.6, 1.6), (0.35, 1.6), (0.15, 1.0)], grey, y=-0.27)
    disc("head_k", 0.5, 1.8, 0.22, grey, y=-0.27, n=14)
    polygon("beak", [(0.3, 1.8), (0.08, 1.74), (0.3, 1.7)], grey, y=-0.28)
    return 2.0, 3.0, 1.2


# ---- Emberdown

def prop_lintel(rng, p):
    """Prop_Lintel: a roost cut into the cliff over Kettil's gate: the door high up for flyers, words cut over it, and
    an iron ladder bolted up the basalt after, its rungs worn pale."""
    rock, dark, cut, iron, worn, ledge = p("rock", SILVER), p("dark", INK), p("cut", lerp(PAPER, SILVER, 0.3)), p("iron", STEEL), p("worn", lerp(PAPER, STEEL, 0.4)), p("ledge", lerp(SILVER, PAPER, 0.2))
    polygon("rock", [(-1.0, 0.0), (1.0, 0.0), (0.95, 2.0), (1.0, 4.0), (-1.0, 4.0), (-0.9, 2.0)], rock, y=0.1)
    polygon("door", [(-0.35, 2.5), (0.35, 2.5), (0.35, 3.3), (0.0, 3.55), (-0.35, 3.3)], dark, y=0.0)
    box("ledge", 0.0, 2.45, 0.9, 0.1, ledge, y=-0.27)
    marks("words", -0.45, 3.78, 9, cut, step=0.1, h=0.1, gap_every=4)   # "Land soft. Leave light."
    for x in (-0.15, 0.15):
        line("rail_%s" % x, x, 0.1, x, 2.4, 0.035, iron, y=-0.29)
    for k in range(7):
        line("rung_%d" % k, -0.16, 0.3 + 0.3 * k, 0.16, 0.3 + 0.3 * k, 0.04, worn if 1 < k < 5 else iron, y=-0.31)
        disc("bolt_%d" % k, 0.26, 0.3 + 0.3 * k, 0.025, iron, y=-0.30, n=6)
    return 2.0, 4.0, 2.0


def prop_tallywall(rng, p):
    """Prop_TallyWall: Kettil's tally wall: a year and a count cut for every year since the town began, every number
    different, and one year that falls by thirty-one."""
    stone, cut, year = p("stone", lerp(SILVER, PAPER, 0.15)), p("cut", lerp(INK, SILVER, 0.2)), p("year", lerp(INK, SILVER, 0.4))
    box("wall", 0.0, 1.0, 2.5, 2.0, stone, y=0.1)
    counts = (14, 16, 17, 19, 21, 23, 24, 26, 28, 30, 31, 33, 35, 36)
    for i, c in enumerate(counts):
        z = 1.85 - i * 0.125
        marks("year_%d" % i, -1.15, z, 3, year, step=0.07, h=0.07, t=0.018)
        tally("count_%d" % i, -0.85, z, (c // 2) if i != 7 else 2, cut, step=0.045, h=0.09)
    return 2.5, 2.0, 2.0


def _cups(rng, p, up):
    wood, leg, cup, glaze, mouth, name = p("wood", lerp(RUST, SILVER, 0.3)), p("leg", lerp(RUST, INK, 0.4)), p("cup", lerp(SILVER, PAPER, 0.35)), p("glaze", lerp(PAPER, (1, 1, 1), 0.5)), p("mouth", lerp(INK, SILVER, 0.3)), p("name", lerp(INK, SILVER, 0.3))
    for x in (-1.0, 1.0):
        polygon("leg_%s" % x, [(x - 0.18, 0.0), (x + 0.18, 0.0), (x + 0.05, 0.55), (x - 0.05, 0.55)], leg, y=0.1)
    box("top", 0.0, 0.58, 2.4, 0.08, wood, y=0.05)
    n = 0
    for row, (count, z, y) in enumerate(((11, 0.72, 0.0), (10, 0.88, 0.04), (10, 1.04, 0.08))):
        for i in range(count):
            x = -1.0 + i * 0.2 + (0.1 if row else 0.0)
            if up:
                polygon("cup_%d" % n, [(x - 0.07, z - 0.1), (x + 0.07, z - 0.1), (x + 0.09, z + 0.08), (x - 0.09, z + 0.08)], glaze, y=y)
                disc("mouth_%d" % n, x, z + 0.08, 0.09, mouth, y=y - 0.30, n=10, squash=0.35)
            else:
                polygon("cup_%d" % n, [(x - 0.09, z - 0.1), (x + 0.09, z - 0.1), (x + 0.07, z + 0.08), (x - 0.07, z + 0.08)], cup, y=y)
                line("name_%d" % n, x - 0.03, z - 0.02, x + 0.03, z - 0.02, 0.012, name, y=y - 0.31)
            n += 1
    return 2.5, 1.25, 2.0


def prop_cups(rng, p):
    """Prop_Cups: thirty-one cups on a trestle by the boarded mine mouth, turned down, a name scratched in each."""
    return _cups(rng, p, False)


def prop_cups_up(rng, p):
    """Prop_Cups_Up: the same cups right side up and washed (emberdown.hollowvein.walked)."""
    return _cups(rng, p, True)


def prop_chimneyfoot(rng, p):
    """Prop_ChimneyFoot: the ninth chimney's foot: a bare stone where the other eight have their builders' names, and fresh
    soot on it."""
    stone, named, cut, soot = p("stone", lerp(SILVER, INK, 0.3)), p("named", lerp(SILVER, INK, 0.15)), p("cut", lerp(PAPER, SILVER, 0.4)), p("soot", INK)
    box("foot", 0.0, 0.75, 2.0, 1.5, stone, y=0.1)
    box("named_a", -0.7, 0.4, 0.5, 0.3, named, y=0.0)
    marks("name_a", -0.9, 0.4, 4, cut, step=0.1, h=0.08)
    box("bare", 0.25, 0.4, 0.7, 0.3, named, y=0.0)
    blob("soot", rng, 0.3, 0.55, 0.3, 0.18, soot, y=-0.29, n=12)
    blob("soot_b", rng, 0.05, 0.3, 0.18, 0.1, soot, y=-0.29, n=10)
    for k in range(3):
        line("course_%d" % k, -1.0, 0.65 + 0.3 * k, 1.0, 0.65 + 0.3 * k, 0.012, p("mortar", lerp(SILVER, INK, 0.5)), y=-0.27)
    return 2.0, 1.5, 2.0


def _ninthdoor(rng, p, open_):
    rock, dark, plank, iron, gap, satchel, soot = p("rock", lerp(SILVER, INK, 0.3)), p("dark", INK), p("plank", lerp(RUST, OLIVE, 0.3)), p("iron", STEEL), p("gap", INK), p("satchel", lerp(RUST, INK, 0.3)), p("soot", lerp(INK, SILVER, 0.2))
    polygon("rock", [(-0.75, 0.0), (0.75, 0.0), (0.75, 2.5), (-0.75, 2.5)], rock, y=0.1)
    polygon("arch", [(-0.5, 0.0), (0.5, 0.0), (0.5, 1.7), (0.0, 2.0), (-0.5, 1.7)], dark, y=0.05)
    blob("soot", rng, 0.0, 2.25, 0.4, 0.15, soot, y=0.0, n=12)   # no name over it, soot instead
    if not open_:
        for i, x in enumerate((-0.33, -0.11, 0.11, 0.33)):
            box("plank_%d" % i, x, 0.85, 0.2, 1.7, plank, y=0.0)
        for z in (0.4, 1.3):
            box("band_%s" % z, 0.0, z, 0.9, 0.07, iron, y=-0.28)
    else:
        box("gap", 0.0, 0.85, 0.9, 1.7, gap, y=0.02)
        polygon("door", [(-0.48, 0.0), (-0.18, 0.1), (-0.18, 1.6), (-0.48, 1.7)], plank, y=-0.28)
        box("satchel", 0.2, 0.25, 0.4, 0.3, satchel, y=-0.29)
        line("strap", 0.05, 0.4, 0.35, 0.55, 0.02, iron, y=-0.30)
    return 1.5, 2.5, 2.0


def prop_ninthdoor(rng, p):
    """Prop_NinthDoor: the door at the ninth chimney with no builder's name over it, fresh soot on the stone."""
    return _ninthdoor(rng, p, False)


def prop_ninthdoor_open(rng, p):
    """Prop_NinthDoor_Open: the same door open on the builder's satchel (emberdown.ninth.door_open)."""
    return _ninthdoor(rng, p, True)


def _hooklamps(rng, p, lit):
    beam, hook, lamp, glass, glow, plate = p("beam", lerp(RUST, INK, 0.4)), p("hook", STEEL), p("lamp", STEEL), p("glass", lerp(GLOW, PAPER, 0.2) if lit else lerp(INK, SILVER, 0.4)), p("glow", lerp(GLOW, PAPER, 0.55)), p("plate", BRASS)
    box("beam", 0.0, 1.9, 3.0, 0.16, beam, y=0.1)
    for i in range(8):
        x = -1.3 + i * 0.37
        line("hook_%d" % i, x, 1.82, x, 1.62, 0.025, hook, y=0.0)
        if lit:
            disc("glow_%d" % i, x, 1.3, 0.3, glow, y=0.2, n=18)
        box("lamp_%d" % i, x, 1.3, 0.22, 0.4, lamp, y=-0.27)
        box("glass_%d" % i, x, 1.3, 0.14, 0.26, glass, y=-0.30)
        box("plate_%d" % i, x, 0.9, 0.24, 0.1, plate, y=-0.28)
        line("name_%d" % i, x - 0.08, 0.9, x + 0.08, 0.9, 0.012, p("ink", INK), y=-0.31)
    return 3.0, 2.0, 2.0


def prop_hooklamps(rng, p):
    """Prop_HookLamps: a miner's lamp on a hook for each name down the gallery wall, a brass plate under each, dark."""
    return _hooklamps(rng, p, False)


def prop_hooklamps_lit(rng, p):
    """Prop_HookLamps_Lit: every lamp lit, the families having come down (emberdown.hollowvein.walked)."""
    return _hooklamps(rng, p, True)


def _bottom(rng, p, swept):
    rubble, wall, boot, lamp, haft, head, chalk, swept_m = p("rubble", lerp(SILVER, INK, 0.3)), p("wall", lerp(INK, SILVER, 0.2)), p("boot", lerp(RUST, INK, 0.4)), p("lamp", STEEL), p("haft", lerp(RUST, SILVER, 0.3)), p("head", STEEL), p("chalk", lerp(PAPER, (1, 1, 1), 0.5)), p("swept", lerp(SILVER, PAPER, 0.2))
    box("wall", 0.0, 0.9, 2.5, 1.2, wall, y=0.15)
    if not swept:
        for k in range(7):
            blob("rubble_%d" % k, rng, -1.0 + 0.33 * k, 0.12 + rng.uniform(0, 0.12), 0.22, 0.12, rubble, y=0.05 - 0.01 * k, n=9)
        polygon("boot", [(-0.75, 0.05), (-0.4, 0.05), (-0.38, 0.22), (-0.55, 0.26), (-0.58, 0.45), (-0.72, 0.45)], boot, y=-0.27)
        box("lamp", 0.0, 0.25, 0.2, 0.34, lamp, y=-0.27)
        line("haft", 0.3, 0.1, 0.95, 0.5, 0.04, haft, y=-0.28)
        polygon("head", [(0.85, 0.42), (1.1, 0.62), (1.0, 0.68), (0.78, 0.5)], head, y=-0.29)
        line("name", 0.5, 0.3, 0.62, 0.37, 0.012, p("ink", INK), y=-0.31)
    else:
        box("swept", 0.0, 0.08, 2.4, 0.16, swept_m, y=0.05)
        for row in range(3):
            marks("names_%d" % row, -1.1, 1.3 - row * 0.22, 11 if row == 0 else 10, chalk, step=0.2, h=0.08, t=0.028, y=-0.27)
    return 2.5, 1.5, 2.0


def prop_bottom(rng, p):
    """Prop_Bottom: under the rubble where the Collapse lay: a boot, a lamp, a pick with a name on the haft."""
    return _bottom(rng, p, False)


def prop_bottom_swept(rng, p):
    """Prop_Bottom_Swept: the rubble cleared, the stone swept, thirty-one names chalked on the wall (emberdown.hollowvein.walked)."""
    return _bottom(rng, p, True)


# ---- the Verdance

def _woolmap(rng, p, released):
    rod, wool, river, knot, thread = p("rod", RUST), p("wool", lerp(PAPER, SILVER, 0.25)), p("river", lerp(WET, INK, 0.2)), p("knot", lerp(INK, RUST, 0.4)), p("thread", lerp(RUST, PAPER, 0.3))
    box("rod", 0.0, 2.42, 2.5, 0.08, rod, y=0.05)
    box("cloth", 0.0, 1.3, 2.3, 2.2, wool, y=0.0)
    pts = [(-1.1, 1.0), (-0.6, 1.3), (-0.1, 1.1), (0.4, 1.5), (0.9, 1.3), (1.15, 1.6)]
    for i in range(len(pts) - 1):
        line("river_%d" % i, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], 0.03, river, y=-0.27)
    villages = [(-0.9, 1.9), (-0.5, 0.8), (-0.2, 1.6), (0.1, 0.6), (0.3, 2.0), (0.6, 1.0), (0.8, 1.9), (-0.7, 0.45), (0.95, 0.7), (-0.05, 1.05), (0.55, 1.65)]
    unpicked = {1, 7} | ({8} if released else set())   # Aldermere's is the east one
    for i, (x, z) in enumerate(villages):
        if i in unpicked:
            line("thread_%d" % i, x, z, x + 0.05, z - 0.5, 0.02, thread, y=-0.29)
            line("thread2_%d" % i, x, z, x - 0.06, z - 0.42, 0.015, thread, y=-0.29)
        else:
            disc("knot_%d" % i, x, z, 0.055, knot, y=-0.29, n=8)
    return 2.5, 2.5, 1.8


def prop_woolmap(rng, p):
    """Prop_WoolMap: the Quiet House's map of the Verdance in undyed wool, villages as knots, some unpicked, none cut."""
    return _woolmap(rng, p, False)


def prop_woolmap_open(rng, p):
    """Prop_WoolMap_Open: Aldermere's knot unpicked with the others (Verdance_Aldermere_2 released)."""
    return _woolmap(rng, p, True)


def prop_dust(rng, p):
    """Prop_Dust: the reading stair's dust, hanging where it is and not falling: flecks only, no line."""
    for i in range(170):
        k = rng.random()
        disc("fleck_%d" % i, rng.uniform(-1.9, 1.9), rng.uniform(0.3, 2.9), 0.012 + 0.022 * k, p("fleck_%d" % (i % 4), lerp(SILVER, PAPER, 0.45 + 0.1 * (i % 4))), y=0.1 - 0.1 * k, n=6)
    return 4.0, 3.0, 0.0


def prop_ledge(rng, p):
    """Prop_Ledge: the Overgrown Gate's landing ledge, worn into grooves by talons, none newer than forty years."""
    stone, groove, moss = p("stone", lerp(SILVER, PAPER, 0.2)), p("groove", lerp(INK, SILVER, 0.4)), p("moss", OLIVE)
    polygon("ledge", [(-1.0, 0.0), (1.0, 0.0), (1.0, 0.7), (0.9, 0.85), (-0.9, 0.85), (-1.0, 0.7)], stone, y=0.05)
    for k in range(9):
        x = -0.8 + 0.2 * k
        line("groove_%d" % k, x, 0.82, x + 0.08 * (1 if k % 2 else -1), 0.55, 0.02, groove, y=-0.28)
        line("groove2_%d" % k, x + 0.06, 0.8, x + 0.1 * (1 if k % 2 else -1), 0.6, 0.015, groove, y=-0.28)
    blob("moss_a", rng, -0.9, 0.2, 0.2, 0.15, moss, y=-0.27, n=10)
    blob("moss_b", rng, 0.85, 0.3, 0.18, 0.2, moss, y=-0.27, n=10)
    return 2.0, 1.0, 1.8


def prop_inscription(rng, p):
    """Prop_Inscription: the gate's inscription: a band of letters cut in the gatepost for eyes above the trees, roots
    up the stone."""
    stone, cut, root = p("stone", lerp(SILVER, PAPER, 0.15)), p("cut", lerp(INK, SILVER, 0.3)), p("root", lerp(RUST, OLIVE, 0.4))
    polygon("post", [(-0.8, 0.0), (0.8, 0.0), (0.75, 3.0), (-0.75, 3.0)], stone, y=0.1)
    box("band", 0.0, 2.2, 1.4, 0.5, p("band", lerp(SILVER, PAPER, 0.3)), y=0.02)
    marks("line1", -0.6, 2.32, 10, cut, step=0.12, h=0.14, t=0.03, gap_every=5)
    marks("line2", -0.6, 2.1, 8, cut, step=0.12, h=0.12, t=0.028, gap_every=4)
    for k in range(3):
        line("root_%d" % k, -0.7 + 0.5 * k, 0.0, -0.5 + 0.5 * k + 0.1 * (k % 2), 1.3 + 0.3 * k, 0.05 - 0.01 * k, root, y=-0.28)
    return 2.0, 3.0, 1.8


# ---- Halden

def prop_tollboard(rng, p):
    """Prop_TollBoard: the toll board at the first bridge: "Revised each spring", and forty springs of revisions painted
    one under another, every figure matching the one above."""
    post, board, head, row = p("post", lerp(RUST, INK, 0.3)), p("board", lerp(SILVER, PAPER, 0.4)), p("head", INK), p("row", lerp(INK, SILVER, 0.25))
    box("post", 0.0, 1.25, 0.12, 2.5, post, y=0.1)
    box("board", 0.0, 1.6, 1.3, 1.7, board, y=0.0)
    line("head", -0.5, 2.35, 0.5, 2.35, 0.03, head, y=-0.31)
    for i in range(13):   # the same figures, every row
        z = 2.2 - i * 0.11
        line("year_%d" % i, -0.55, z, -0.3, z, 0.016, row, y=-0.31)
        marks("fig_%d" % i, -0.1, z, 4, row, step=0.12, h=0.05, t=0.016)
    return 1.5, 2.5, 2.0


def prop_sheets(rng, p):
    """Prop_Sheets: the drying lofts' vellum hung on a line, every sheet with the same thumbprint in the same corner."""
    cord, peg, sheet, thumb = p("cord", ROPE), p("peg", lerp(RUST, INK, 0.3)), p("sheet", lerp(PAPER, (1, 1, 1), 0.2)), p("thumb", lerp(INK, SILVER, 0.3))
    line("cord", -1.5, 2.3, 1.5, 2.3, 0.02, cord, y=0.05)
    for i in range(5):
        x = -1.2 + 0.6 * i
        polygon("sheet_%d" % i, [(x - 0.26, 0.9), (x + 0.26, 0.9), (x + 0.24, 2.28), (x - 0.24, 2.28)], sheet, y=0.0 - 0.01 * i)
        box("peg_%d" % i, x, 2.3, 0.06, 0.14, peg, y=-0.28)
        disc("thumb_%d" % i, x + 0.14, 1.05, 0.06, thumb, y=-0.29, n=10, squash=1.3)
        disc("thumb_in_%d" % i, x + 0.14, 1.05, 0.03, sheet, y=-0.30, n=8, squash=1.3)
    return 3.0, 2.5, 1.8


def prop_order(rng, p):
    """Prop_Order: the Guild's standing order pinned inside the Hall's doors: the paper brown, the pins bright."""
    panel, paper, pin, head, text = p("panel", lerp(RUST, INK, 0.4)), p("paper", lerp(PAPER, RUST, 0.3)), p("pin", BRASS), p("head", INK), p("text", lerp(INK, RUST, 0.3))
    box("panel", 0.0, 0.8, 1.0, 1.6, panel, y=0.1)
    box("paper", 0.0, 1.0, 0.7, 0.9, paper, y=0.0)
    for x, z in ((-0.3, 1.4), (0.3, 1.4), (-0.3, 0.6), (0.3, 0.6)):
        disc("pin_%s_%s" % (x, z), x, z, 0.03, pin, y=-0.31, n=8)
    line("head", -0.25, 1.3, 0.25, 1.3, 0.03, head, y=-0.30)
    text_rows("text", -0.28, 1.15, 5, 0.55, text, dz=0.09, rng=rng, jitter=0.3)
    return 1.0, 1.75, 2.0


def prop_roll(rng, p):
    """Prop_Roll: the roll of Guildmasters cut in the Hall's wall: nine names and a tenth, the ninth chiselled out, its
    owl's-eye crest left beside it, Voss's name on smooth stone."""
    wall, cut, chisel, smooth, crest = p("wall", lerp(SILVER, PAPER, 0.2)), p("cut", lerp(INK, SILVER, 0.2)), p("chisel", lerp(SILVER, PAPER, 0.5)), p("smooth", lerp(SILVER, PAPER, 0.45)), p("crest", lerp(INK, SILVER, 0.2))
    box("wall", 0.0, 1.25, 2.0, 2.5, wall, y=0.1)
    line("head", -0.6, 2.3, 0.6, 2.3, 0.03, cut, y=-0.30)
    for i in range(10):
        z = 2.1 - i * 0.17
        if i == 8:
            blob("chisel", rng, 0.05, z, 0.5, 0.08, chisel, y=-0.28, n=12, wobble=0.3)
            disc("crest", -0.7, z, 0.06, crest, y=-0.30, n=10)
            disc("crest_in", -0.7, z, 0.025, wall, y=-0.31, n=8)
        else:
            if i == 9:
                box("smooth", 0.05, z, 0.9, 0.14, smooth, y=-0.27)
            marks("name_%d" % i, -0.4, z, 7 + (i % 3), cut, step=0.1, h=0.08, t=0.02)
    return 2.0, 2.5, 2.0


def prop_plaque(rng, p):
    """Prop_Plaque: the flyer-tower's foot: a door cut crudely into the stone the year after the fall, a brass plaque
    beside it, the old landing door far above and no stairs between."""
    stone, dark, plaque, cut, mortar = p("stone", SILVER), p("dark", INK), p("plaque", BRASS), p("cut", lerp(INK, SILVER, 0.3)), p("mortar", lerp(SILVER, INK, 0.3))
    box("tower", 0.0, 1.25, 2.0, 2.5, stone, y=0.1)
    for k in range(5):
        line("course_%d" % k, -1.0, 0.45 + 0.45 * k, 1.0, 0.45 + 0.45 * k, 0.012, mortar, y=0.0)
    polygon("door", [(-0.75, 0.0), (-0.15, 0.0), (-0.1, 0.9), (-0.25, 1.5), (-0.6, 1.55), (-0.8, 1.0)], dark, y=-0.3)
    box("plaque", 0.4, 1.1, 0.6, 0.4, plaque, y=-0.28)
    text_rows("text", 0.15, 1.22, 3, 0.5, cut, dz=0.1, t=0.018)
    box("landing", 0.3, 2.42, 0.4, 0.16, dark, y=-0.1)   # the old door's sill, at the top edge
    return 2.0, 2.5, 2.0


def prop_drawing(rng, p):
    """Prop_Drawing: a child's crayon drawing in a Guild frame on Voss's wall, dusted: a tall heron with a compass, a
    city, a small heron waving from a high window."""
    frame, sheet, crayon, red, dust = p("frame", lerp(RUST, INK, 0.4)), p("sheet", lerp(PAPER, (1, 1, 1), 0.4)), p("crayon", lerp(RUST, GLOW, 0.3)), p("red", RUST), p("dust", lerp(SILVER, PAPER, 0.5))
    box("frame", 0.0, 0.95, 1.2, 1.5, frame, y=0.05)
    box("sheet", 0.0, 0.95, 1.0, 1.3, sheet, y=0.0)
    t = 0.035
    line("leg_a", -0.3, 0.4, -0.3, 0.9, t, crayon, y=-0.30)
    line("leg_b", -0.22, 0.4, -0.24, 0.9, t, crayon, y=-0.30)
    polygon("body", [(-0.45, 0.9), (-0.1, 0.9), (-0.12, 1.3), (-0.4, 1.3)], crayon, y=-0.29)
    line("neck", -0.25, 1.3, -0.2, 1.52, t, crayon, y=-0.30)
    disc("head", -0.18, 1.55, 0.07, crayon, y=-0.30, n=10)
    line("bill", -0.12, 1.55, 0.02, 1.52, t * 0.7, crayon, y=-0.31)
    disc("compass", -0.05, 1.05, 0.09, red, y=-0.31, n=10)
    disc("compass_in", -0.05, 1.05, 0.05, sheet, y=-0.32, n=8)
    for k in range(3):   # the city
        box("house_%d" % k, 0.12 + 0.14 * k, 0.5 + 0.1 * k, 0.12, 0.2 + 0.2 * k, crayon, y=-0.29)
    box("window", 0.4, 1.0, 0.07, 0.08, sheet, y=-0.30)
    line("wave", 0.4, 1.0, 0.47, 1.1, t * 0.6, crayon, y=-0.31)
    for k in range(4):
        disc("dust_%d" % k, -0.5 + 0.3 * k, 1.7, 0.02, dust, y=-0.31, n=6)
    return 1.25, 1.75, 1.6


def prop_leaves(rng, p):
    """Prop_Leaves: the only fallen leaves in Halden, raked into one neat pile against the orchard wall, the rake beside."""
    wall, leaf_a, leaf_b, leaf_c, handle, tine = p("wall", lerp(SILVER, PAPER, 0.3)), p("leaf_a", lerp(RUST, GLOW, 0.4)), p("leaf_b", lerp(RUST, INK, 0.2)), p("leaf_c", lerp(OLIVE, RUST, 0.5)), p("handle", lerp(RUST, INK, 0.3)), p("tine", STEEL)
    box("wall", 0.0, 0.5, 2.0, 1.0, wall, y=0.15)
    blob("pile", rng, -0.3, 0.25, 0.6, 0.3, leaf_b, y=0.05, n=16)
    for i in range(28):
        m = (leaf_a, leaf_b, leaf_c)[i % 3]
        disc("leaf_%d" % i, -0.3 + rng.uniform(-0.55, 0.55), 0.08 + rng.uniform(0, 0.45), 0.05, m, y=-0.27 - 0.002 * i, n=7, squash=0.6)
    line("handle", 0.75, 0.1, 0.5, 1.25, 0.035, handle, y=-0.28)
    box("rake", 0.78, 0.12, 0.4, 0.06, handle, y=-0.29)
    for k in range(5):
        line("tine_%d" % k, 0.6 + 0.09 * k, 0.1, 0.6 + 0.09 * k, 0.0, 0.015, tine, y=-0.30)
    return 2.0, 1.25, 1.8


def prop_notice_complete(rng, p):
    """Prop_Notice_Complete: Lowmarket's board after the strike: a fresh SURVEY COMPLETE pasted over them all."""
    w, h, px = prop_notice(rng, p)
    fresh, ink = p("fresh", lerp(PAPER, (1, 1, 1), 0.6)), p("ink", INK)
    box("complete", 0.0, 1.5, 0.9, 0.6, fresh, y=-0.39)
    line("head_c", -0.33, 1.7, 0.33, 1.7, 0.05, ink, y=-0.42)
    line("head_c2", -0.3, 1.58, 0.25, 1.58, 0.04, ink, y=-0.42)
    text_rows("text_c", -0.33, 1.45, 2, 0.5, ink, dz=0.08, y=-0.42)
    return w, h, px


# ---- the Greyfold

def prop_cuttether(rng, p):
    """Prop_CutTether: the outpost's tether pegs: coiled tethers never used, one peg empty, its tether run out under
    the fence into the white and cut."""
    peg, rope, rail, cut = p("peg", RUST), p("rope", ROPE), p("rail", lerp(RUST, PAPER, 0.3)), p("cut", lerp(ROPE, PAPER, 0.5))
    for i, x in enumerate((-0.8, -0.4, 0.0)):
        polygon("peg_%d" % i, [(x - 0.05, 0.0), (x + 0.05, 0.0), (x + 0.03, 0.5), (x - 0.03, 0.5)], peg, y=0.05)
        if i < 2:
            for k in range(4):
                disc("coil_%d_%d" % (i, k), x, 0.42 - 0.06 * k, 0.11, rope, y=-0.27 - 0.01 * k, n=12, squash=0.5)
                disc("coil_in_%d_%d" % (i, k), x, 0.42 - 0.06 * k, 0.05, p("paper", PAPER), y=-0.28 - 0.01 * k, n=8, squash=0.5)
    line("run", 0.0, 0.45, 0.6, 0.08, 0.025, rope, y=-0.28)
    line("run2", 0.6, 0.08, 1.0, 0.06, 0.025, cut, y=-0.28)
    box("rail", 0.65, 0.5, 0.1, 1.0, rail, y=0.0)
    box("rail_bar", 0.82, 0.75, 0.36, 0.06, rail, y=0.0)
    return 2.0, 1.25, 1.6


def prop_oldtether(rng, p):
    """Prop_OldTether: among the Guild's new stakes, one old Ferrymen's tether-post staked long before, its rope running
    taut into the white."""
    wood, iron, rope = p("wood", lerp(RUST, PAPER, 0.35)), p("iron", lerp(STEEL, RUST, 0.5)), p("rope", lerp(ROPE, PAPER, 0.25))
    ob = box("post", 0.0, 0.8, 0.22, 1.6, wood, y=0.1)
    ob.rotation_euler = (0, math.radians(-7), 0)
    box("band_a", 0.05, 1.3, 0.28, 0.07, iron, y=0.05)
    box("band_b", -0.03, 0.5, 0.28, 0.07, iron, y=0.05)
    disc("eye", 0.1, 1.5, 0.08, iron, y=-0.27, n=10)
    line("rope", 0.1, 1.5, 0.72, 2.45, 0.03, rope, y=-0.30)
    for i in range(3):
        box("wrap_%d" % i, 0.02, 0.9 + 0.09 * i, 0.3, 0.04, rope, y=-0.27)
    return 1.5, 2.5, 1.6


# ---- the Blank

def prop_doorframe(rng, p):
    """Prop_Doorframe: Ilse's doorframe with the height marks knifed into it, stopping at six."""
    frame, dark, mark = p("frame", RUST), p("dark", lerp(INK, SILVER, 0.2)), p("mark", lerp(INK, SILVER, 0.1))
    box("jamb_l", -0.45, 1.2, 0.16, 2.4, frame, y=0.05)
    box("jamb_r", 0.45, 1.2, 0.16, 2.4, frame, y=0.05)
    box("head", 0.0, 2.42, 1.1, 0.16, frame, y=0.05)
    box("way", 0.0, 1.17, 0.74, 2.34, dark, y=0.1)
    for k, z in enumerate((0.55, 0.68, 0.8, 0.93, 1.05, 1.16)):
        line("mark_%d" % k, 0.38, z, 0.52, z, 0.02, mark, y=-0.29)
        line("age_%d" % k, 0.54, z - 0.02, 0.56, z + 0.04, 0.015, mark, y=-0.29)
    return 1.25, 2.5, 1.6


PROPS = [
    ("Prop_Desk", prop_desk), ("Prop_Ledger", prop_ledger), ("Prop_Dummy", prop_dummy), ("Prop_Stall", prop_stall),
    ("Prop_Vantage", prop_vantage), ("Prop_Lamp", prop_lamp), ("Prop_LampGlow", prop_lampglow), ("Prop_Seeds", prop_seeds),
    ("Prop_Bound", prop_bound), ("Prop_Nets", prop_nets), ("Prop_Stoop", prop_stoop), ("Prop_Boat", prop_boat),
    ("Prop_Tether", prop_tether),
    ("Prop_WetEdge", prop_wetedge, "Greyfold"),   # the Edge room is Greyfold's: its own kit, this its first layer
    # Emberdown (ENV-03): the coast's furniture in the highland's palette, and the town's own.
    ("Prop_Desk", prop_desk, "Emberdown"), ("Prop_Ledger", prop_ledger, "Emberdown"), ("Prop_Vantage", prop_vantage, "Emberdown"),
    ("Prop_Lamp", prop_lamp, "Emberdown"), ("Prop_LampGlow", prop_lampglow, "Emberdown"), ("Prop_Seeds", prop_seeds, "Emberdown"),
    ("Prop_Bound", prop_bound, "Emberdown"),
    ("Prop_Bell", prop_bell, "Emberdown"), ("Prop_Anvil", prop_anvil, "Emberdown"), ("Prop_Boards", prop_boards, "Emberdown"),
    ("Prop_Porch", prop_porch, "Emberdown"),
    # The Verdance (ENV-04): the shared furniture in the forest's palette, and the forest's own.
    ("Prop_Desk", prop_desk, "Verdance"), ("Prop_Ledger", prop_ledger, "Verdance"), ("Prop_Vantage", prop_vantage, "Verdance"),
    ("Prop_Lamp", prop_lamp, "Verdance"), ("Prop_LampGlow", prop_lampglow, "Verdance"), ("Prop_Seeds", prop_seeds, "Verdance"),
    ("Prop_Bound", prop_bound, "Verdance"),
    ("Prop_Milestone", prop_milestone, "Verdance"), ("Prop_Lantern", prop_lantern, "Verdance"), ("Prop_Lectern", prop_lectern, "Verdance"),
    ("Prop_Bunting", prop_bunting, "Verdance"), ("Prop_Anchor", prop_anchor, "Verdance"),
    # Halden (ENV-05): the shared furniture in the Citadel's palette, and the Plateau's own.
    ("Prop_Desk", prop_desk, "Halden"), ("Prop_Ledger", prop_ledger, "Halden"), ("Prop_Vantage", prop_vantage, "Halden"),
    ("Prop_Lamp", prop_lamp, "Halden"), ("Prop_LampGlow", prop_lampglow, "Halden"), ("Prop_Seeds", prop_seeds, "Halden"),
    ("Prop_Bound", prop_bound, "Halden"), ("Prop_Anchor", prop_anchor, "Halden"),
    ("Prop_Gravestone", prop_gravestone, "Halden"), ("Prop_Wheel", prop_wheel, "Halden"), ("Prop_Scaffold", prop_scaffold, "Halden"),
    ("Prop_Frame", prop_frame, "Halden"), ("Prop_Slots", prop_slots, "Halden"), ("Prop_ExamDesk", prop_examdesk, "Halden"),
    ("Prop_Notice", prop_notice, "Halden"),
    ("Prop_DrillRack", prop_drillrack, "Halden"), ("Prop_ChalkBoard", prop_chalkboard, "Halden"), ("Prop_Paces", prop_paces, "Halden"),
    # Windreach (ENV-07): the shared furniture in the Steppe's palette, and the Steppe's own.
    ("Prop_Desk", prop_desk, "Windreach"), ("Prop_Ledger", prop_ledger, "Windreach"), ("Prop_Vantage", prop_vantage, "Windreach"),
    ("Prop_Lamp", prop_lamp, "Windreach"), ("Prop_LampGlow", prop_lampglow, "Windreach"), ("Prop_Seeds", prop_seeds, "Windreach"),
    ("Prop_Bound", prop_bound, "Windreach"),
    ("Prop_Stone", prop_stone, "Windreach"), ("Prop_Wagon", prop_wagon, "Windreach"), ("Prop_Fire", prop_fire, "Windreach"),
    ("Prop_Ashes", prop_ashes, "Windreach"), ("Prop_Bedroll", prop_bedroll, "Windreach"), ("Prop_Hull", prop_hull, "Windreach"),
    ("Prop_Reeds", prop_reeds, "Windreach"), ("Prop_LipStone", prop_lipstone, "Windreach"), ("Prop_Swirl", prop_swirl, "Windreach"),
    ("Prop_Hearth", prop_hearth, "Windreach"), ("Prop_Grass_A", prop_grass_a, "Windreach"), ("Prop_Grass_B", prop_grass_b, "Windreach"),
    ("Prop_Grass_C", prop_grass_c, "Windreach"), ("Prop_Grass_Tall", prop_grass_tall, "Windreach"),
    # The Greyfold (ENV-08): the shared furniture in grey and rope, and the threshold's own.
    ("Prop_Desk", prop_desk, "Greyfold"), ("Prop_Ledger", prop_ledger, "Greyfold"), ("Prop_Vantage", prop_vantage, "Greyfold"),
    ("Prop_Lamp", prop_lamp, "Greyfold"), ("Prop_LampGlow", prop_lampglow, "Greyfold"), ("Prop_Seeds", prop_seeds, "Greyfold"),
    ("Prop_Bound", prop_bound, "Greyfold"), ("Prop_Tether", prop_tether, "Greyfold"),
    ("Prop_Fence", prop_fence, "Greyfold"), ("Prop_Milepost", prop_milepost, "Greyfold"), ("Prop_Tent", prop_tent, "Greyfold"),
    ("Prop_Stake", prop_stake, "Greyfold"), ("Prop_Cobble", prop_cobble, "Greyfold"), ("Prop_Atlas", prop_atlas, "Greyfold"),
    ("Prop_Footprints", prop_footprints, "Greyfold"), ("Prop_Beam", prop_beam, "Greyfold"),
    # The Blank (ENV-08): the desk and the lamp in the Blank's greys, and the fixed islands' own.
    ("Prop_Desk", prop_desk, "Blank"), ("Prop_Lamp", prop_lamp, "Blank"), ("Prop_LampGlow", prop_lampglow, "Blank"),
    ("Prop_Seeds", prop_seeds, "Blank"), ("Prop_Bound", prop_bound, "Blank"),
    ("Prop_House", prop_house, "Blank"), ("Prop_Island", prop_island, "Blank"), ("Prop_Chair", prop_chair, "Blank"),
    ("Prop_Crayon", prop_crayon, "Blank"), ("Prop_Beacon", prop_beacon, "Blank"), ("Prop_Well", prop_well, "Blank"),
    ("Prop_Door", prop_door, "Blank"),
    # The dressing (ENV-06): what the rooms say, one drawing a piece, a second where the piece changes with its place.
    ("Prop_TetherPosts", prop_tetherposts), ("Prop_PriceBoard", prop_priceboard), ("Prop_Lintels", prop_lintels),
    ("Prop_Lintels_Chalk", prop_lintels_chalk), ("Prop_Log", prop_log), ("Prop_Tapestry", prop_tapestry),
    ("Prop_ChapelDoor", prop_chapeldoor), ("Prop_ChapelDoor_Open", prop_chapeldoor_open), ("Prop_Ladders", prop_ladders),
    ("Prop_Moorings", prop_moorings), ("Prop_Keeper", prop_keeper),
    ("Prop_Lintel", prop_lintel, "Emberdown"), ("Prop_TallyWall", prop_tallywall, "Emberdown"), ("Prop_Cups", prop_cups, "Emberdown"),
    ("Prop_Cups_Up", prop_cups_up, "Emberdown"), ("Prop_ChimneyFoot", prop_chimneyfoot, "Emberdown"), ("Prop_NinthDoor", prop_ninthdoor, "Emberdown"),
    ("Prop_NinthDoor_Open", prop_ninthdoor_open, "Emberdown"), ("Prop_HookLamps", prop_hooklamps, "Emberdown"), ("Prop_HookLamps_Lit", prop_hooklamps_lit, "Emberdown"),
    ("Prop_Bottom", prop_bottom, "Emberdown"), ("Prop_Bottom_Swept", prop_bottom_swept, "Emberdown"),
    ("Prop_WoolMap", prop_woolmap, "Verdance"), ("Prop_WoolMap_Open", prop_woolmap_open, "Verdance"), ("Prop_Dust", prop_dust, "Verdance"),
    ("Prop_Ledge", prop_ledge, "Verdance"), ("Prop_Inscription", prop_inscription, "Verdance"),
    ("Prop_TollBoard", prop_tollboard, "Halden"), ("Prop_Sheets", prop_sheets, "Halden"), ("Prop_Order", prop_order, "Halden"),
    ("Prop_Roll", prop_roll, "Halden"), ("Prop_Plaque", prop_plaque, "Halden"), ("Prop_Drawing", prop_drawing, "Halden"),
    ("Prop_Leaves", prop_leaves, "Halden"), ("Prop_Notice_Complete", prop_notice_complete, "Halden"),
    ("Prop_CutTether", prop_cuttether, "Greyfold"), ("Prop_OldTether", prop_oldtether, "Greyfold"),
    ("Prop_Doorframe", prop_doorframe, "Blank"),
]


def build(name, fn, region="Saltmarrow"):
    out = os.path.join(KITS, region)
    os.makedirs(out, exist_ok=True)
    reset_scene()
    seed = sum(ord(c) for c in name)
    rng = random.Random(seed)
    spec = REGIONS.get(region, REGIONS["Saltmarrow"])
    globals().update(spec["colours"])   # the prop functions name their colours; each region rebinds them
    p = _Palette(spec["paper"], spec["ink"], 0.0, False)
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
        if only and name not in only and (region + ":" + name) not in only:   # a bare name, or Region:Name for a shared drawing
            continue
        if region not in kits:
            path = os.path.join(KITS, region, "kit.json")
            if os.path.exists(path):
                with open(path) as f:
                    kits[region] = json.load(f)
            else:
                kits[region] = {"region": region, "ppu": PPU, "tilePpu": TILE_PPU, "paper": REGIONS.get(region, REGIONS["Saltmarrow"])["paper"], "layers": []}
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

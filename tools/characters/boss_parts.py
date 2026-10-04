"""The pieces the late fights make at run time, drawn in Blender (CHR-09, CHR-10): the Collapse's rubble, surge and
chorus lamps; the Gatekeeper's stone feathers; Hale's stones and their strikes; the Fallen Star's fist; Voss's
seals; the Bells' ropes; Corra's small drawing; the Archivist's quill hand; the Complete Survey's ink pools. Each is
packed as its own character and a boss carries the sheets it needs (Boss.PartSkinNames) so BossPart can wear them.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/boss_parts.py
    ... -- BellRope StarFist             (only those)
    python tools/characters/pack.py rubble surge choruslamp feather stone stonestrike starfist vossseal bellrope crayonsmall quillhand inkpool

A part is centred on its collider's centre like an enemy. Parts have no turnaround: they are not characters.
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER
from saltmarrow_enemies import BRASS, GREY_INK, SMUDGE, SMUDGE_MID
from bosses import (GOLD, STONE, STONE_DARK, IRON, IRON_LIGHT, LAMP, CrayonVoss, crayon_idle, CRAYON_INK, FADED_INK, FADED_BRASS,
                    OWL_INK, smoothstep)


# ---------------------------------------------------------------- the Collapse's

class Rubble(Rig):
    """A block of the mine come down: pogo it. 1.4 x 0.8."""
    def __init__(self):
        super().__init__()
        rng = random.Random(21)
        root = self.add("root")
        body = self.add("body", root)
        stone, dark, dust = mat("stone", (0.36, 0.33, 0.30)), mat("stone_dark", (0.26, 0.24, 0.22)), mat("dust", (0.55, 0.52, 0.48))
        cube("block", (1.2, 0.7, 0.6), stone, body, loc=(0, 0, -0.05), rot=(0, D(-4), 0))
        for k in range(5):
            s = rng.uniform(0.15, 0.32)
            cube("chip%d" % k, (s, s, s * 0.8), dark if k % 2 else stone, body, loc=(rng.uniform(-0.65, 0.65), rng.uniform(-0.3, -0.1), rng.uniform(-0.35, 0.3)),
                 rot=(0, D(rng.uniform(0, 40)), 0))
        for k in range(3):
            sphere("dust%d" % k, 0.08, dust, body, loc=(-0.5 + 0.5 * k, -0.4, 0.4), scale=(1.4, 0.5, 0.8))
        self.snapshot()


def rubble_idle(r, i, n):
    t = i / n
    for k in range(3):
        r.move("body", z=0.0)
    r.scale("body", 1, 1, 1 + 0.01 * math.sin(2 * math.pi * t))


RUBBLE_CLIPS = [("idle", 12, 4, True, rubble_idle)]


class Surge(Rig):
    """Ink along the floor from the far end: a wave with a crest. 1.6 x 0.7, moving +X."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        ink, mid = mat("ink", INK), mat("mid", SMUDGE_MID)
        slab("wave", [(-0.8, -0.35), (-0.4, -0.35), (0.0, -0.1), (0.35, 0.2), (0.6, 0.35), (0.8, 0.15), (0.8, -0.35)], 0.4, ink, body)
        slab("crest", [(0.3, 0.1), (0.55, 0.32), (0.75, 0.3), (0.62, 0.05)], 0.3, mid, body, loc=(0, -0.05, 0))
        for k in range(3):
            d = self.add("drop%d" % k, body, (0.4 + 0.15 * k, -0.2, 0.4 + 0.08 * k))
            sphere("drop%d_m" % k, 0.06, ink, d, scale=(1, 0.5, 1.3))
        self.snapshot()


def surge_move(s, i, n):
    t = i / n
    s.scale("body", 1 + 0.08 * math.sin(2 * math.pi * t), 1, 1 - 0.06 * math.sin(2 * math.pi * t))
    for k in range(3):
        s.move("drop%d" % k, z=0.08 * math.sin(2 * math.pi * t + k), x=0.05 * k * math.sin(2 * math.pi * t))


SURGE_CLIPS = [("idle", 12, 4, True, surge_move), ("move", 12, 4, True, surge_move)]


class ChorusLamp(Rig):
    """A miner's lamp over its section of the floor: dark, or lit on the beat. 0.5 x 0.7."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        flame = self.add("flame", body, (0, -0.05, -0.05))
        brass, ink = mat("brass", BRASS), mat("ink", INK)
        self.glass_dark = mat("glass_dark", (0.22, 0.20, 0.18))
        self.glass_lit = mat("glass_lit", LAMP)
        cone("cap", 0.17, 0.08, 0.1, brass, body, loc=(0, 0, 0.27))
        cube("ring", (0.06, 0.06, 0.1), ink, body, loc=(0, 0, 0.36))
        self.glass = cone("glass", 0.14, 0.15, 0.36, self.glass_dark, body, loc=(0, 0, 0.0))
        for k in range(3):
            cube("bar%d" % k, (0.025, 0.025, 0.4), ink, body, loc=(-0.13 + 0.13 * k, -0.14, 0.0))
        cone("base", 0.17, 0.15, 0.08, brass, body, loc=(0, 0, -0.24))
        cone("flame_m", 0.05, 0.0, 0.16, mat("flame", (1.0, 0.92, 0.62)), flame, loc=(0, 0, 0.0))
        self.prop("lit", flame)
        self.snapshot()

    def light(self, on):
        self.glass.data.materials[0] = self.glass_lit if on else self.glass_dark
        if on:
            self.show("lit")


def lamp_dark(l, i, n):
    l.light(False)


def lamp_lit(l, i, n):
    t = i / n
    l.light(True)
    l.scale("flame", 1 + 0.15 * math.sin(2 * math.pi * t), 1, 1 + 0.25 * math.sin(2 * math.pi * t + 1))


CHORUSLAMP_CLIPS = [("dark", 12, 2, True, lamp_dark), ("idle", 12, 2, True, lamp_dark), ("lit", 12, 4, True, lamp_lit)]


# ---------------------------------------------------------------- the Gatekeeper's

class Feather(Rig):
    """A stone feather, falling where its shadow was: pogo it. 0.5 x 0.9."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        stone, dark, ink = mat("stone", STONE), mat("stone_dark", STONE_DARK), mat("ink", INK)
        slab("vane", [(-0.2, 0.45), (0.2, 0.3), (0.22, -0.2), (0.0, -0.45), (-0.22, -0.2)], 0.2, stone, body)
        cube("shaft", (0.04, 0.26, 0.9), dark, body, loc=(0, -0.02, -0.02))
        for k in range(4):
            cube("barb%d" % k, (0.3, 0.02, 0.025), ink, body, loc=(0, -0.12, 0.3 - 0.18 * k), rot=(0, D(-25), 0))
        self.snapshot()


def feather_idle(f, i, n):
    t = i / n
    f.rot("body", y=8 * math.sin(2 * math.pi * t))


FEATHER_CLIPS = [("idle", 12, 3, True, feather_idle)]


# ---------------------------------------------------------------- Hale's

class Stone(Rig):
    """One of the Nine Stones: bare, his (a brass sighting mark), or hers (an ink mark). 0.6 x 1.2."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        stone, dark, brass, ink = mat("stone", (0.58, 0.58, 0.54)), mat("stone_dark", (0.46, 0.46, 0.42)), mat("brass", BRASS), mat("ink", INK)
        slab("stone_m", [(-0.3, -0.6), (0.3, -0.6), (0.26, 0.2), (0.12, 0.6), (-0.18, 0.55), (-0.3, 0.1)], 0.5, stone, body)
        cube("lichen", (0.2, 0.06, 0.1), dark, body, loc=(-0.08, -0.26, -0.2), rot=(0, D(20), 0))
        hale = self.add("hale_mark", body, (0.02, -0.27, 0.15))
        cone("sight_disc", 0.12, 0.12, 0.03, brass, hale, rot=(D(90), 0, 0))
        cube("sight_line", (0.02, 0.02, 0.4), brass, hale, loc=(0, 0, -0.3))
        wren = self.add("wren_mark", body, (0.0, -0.27, 0.1))
        for k in range(3):
            cube("scribble%d" % k, (0.3, 0.02, 0.03), ink, wren, loc=(0, 0, 0.1 - 0.1 * k), rot=(0, D(-15 + 15 * k), 0))
        self.prop("hale", hale)
        self.prop("wren", wren)
        self.snapshot()


def stone_bare(s, i, n):
    pass


def stone_hale(s, i, n):
    s.show("hale")


def stone_wren(s, i, n):
    s.show("wren")


STONE_CLIPS = [("idle", 12, 2, True, stone_bare), ("bare", 12, 2, True, stone_bare), ("hale", 12, 2, True, stone_hale), ("wren", 12, 2, True, stone_wren)]


class StoneStrike(Rig):
    """The count called: a column of light over one of his stones, solid enough to pogo. 1 x 3."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, -1.5))
        light, pale = mat("light", (0.96, 0.84, 0.50)), mat("pale", (0.99, 0.95, 0.80))
        cone("column", 0.42, 0.3, 3.0, light, body, loc=(0, 0, 1.5))
        cone("core", 0.2, 0.12, 3.0, pale, body, loc=(0, -0.1, 1.5))
        for k in range(4):
            cube("ray%d" % k, (0.04, 0.04, 0.8), pale, body, loc=(-0.45 + 0.3 * k, -0.3, 2.4 + 0.2 * (k % 2)))
        self.snapshot()


def strike_erupt(s, i, n):
    k = (0.35, 0.8, 1.0)[i]
    s.scale("body", 1, 1, k)


STONESTRIKE_CLIPS = [("idle", 24, 3, False, strike_erupt), ("erupt", 24, 3, False, strike_erupt)]


# ---------------------------------------------------------------- the Fallen Star's

class StarFist(Rig):
    """The fist, down on the mark and staying a moment: iron to pogo from. 1.6 x 1."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        iron, light = mat("iron", IRON), mat("iron_light", IRON_LIGHT)
        sphere("fist", 0.5, light, body, loc=(0.1, 0, -0.05), scale=(1.4, 0.9, 0.9))
        for k in range(4):
            cube("knuckle%d" % k, (0.22, 0.2, 0.22), iron, body, loc=(-0.45 + 0.3 * k, -0.35, 0.18))
        cube("wrist", (0.5, 0.5, 0.4), iron, body, loc=(-0.65, 0, 0.1))
        self.snapshot()


def fist_idle(f, i, n):
    t = i / n
    f.move("body", z=0.015 * math.sin(2 * math.pi * t))


STARFIST_CLIPS = [("idle", 12, 3, True, fist_idle)]


# ---------------------------------------------------------------- Voss's

class VossSeal(Rig):
    """A brass seal over a section of the floor, the compass rose on it: it holds her for a beat. SectionWidth x 5."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        rose = self.add("rose", body, (0, -0.06, 0))
        brass, gold, ink, pale = mat("brass", (0.72, 0.66, 0.46)), mat("gold", GOLD), mat("ink", INK), mat("pale", (0.86, 0.82, 0.68))
        cone("disc", 1.3, 1.3, 0.04, brass, body, rot=(D(90), 0, 0))
        cone("rim", 1.34, 1.34, 0.02, ink, body, rot=(D(90), 0, 0), loc=(0, 0.02, 0))
        cone("inner", 0.9, 0.9, 0.02, pale, body, rot=(D(90), 0, 0), loc=(0, -0.03, 0))
        cone("boss", 0.18, 0.18, 0.06, gold, rose, rot=(D(90), 0, 0))
        for k in range(8):
            a = k * 45
            cube("point%d" % k, (0.06, 0.02, 1.0 if k % 2 == 0 else 0.6), ink if k % 2 == 0 else gold, rose, loc=(0.5 * math.sin(D(a)) * (1.0 if k % 2 == 0 else 0.6), 0, 0.5 * math.cos(D(a)) * (1.0 if k % 2 == 0 else 0.6)), rot=(0, D(a), 0))
        for k in range(24):
            a = k * 15
            cube("tick%d" % k, (0.03, 0.02, 0.14), ink, body, loc=(1.15 * math.sin(D(a)), -0.04, 1.15 * math.cos(D(a))), rot=(0, D(a), 0))
        self.snapshot()


def seal_idle(s, i, n):
    s.rot("rose", y=2.5 * i)


VOSSSEAL_CLIPS = [("idle", 12, 4, True, seal_idle)]


# ---------------------------------------------------------------- the Bells'

class BellRope(Rig):
    """A bell-rope in the nave, its bell at the top: cut it when you can see it. 0.3 x 6, centred three units up."""
    def __init__(self, great=False):
        super().__init__()
        root = self.add("root")
        bell = self.add("bell", root, (0, 0, 2.75))
        rope = self.add("rope", bell, (0, 0, -0.55))
        brass, ink, rope_m, wood = mat("f_brass", FADED_BRASS), mat("f_ink", FADED_INK), mat("rope", (0.70, 0.66, 0.58)), mat("f_wood", (0.74, 0.68, 0.58))
        r = 0.42
        cube("hanger", (0.06, 0.06, 0.3), ink, bell, loc=(0, 0, 0.3))
        cone("bell_m", r, r * 0.45, r * 1.5, brass, bell, loc=(0, 0, -0.05))
        cone("lip", r * 1.05, r, 0.06, ink, bell, loc=(0, 0, -0.38))
        sphere("clapper", 0.07, ink, bell, loc=(0, 0, -0.42))
        cone("rope_m", 0.05, 0.045, 5.3, rope_m, rope, loc=(0, 0, -2.65))
        for k in range(6):
            cube("twist%d" % k, (0.1, 0.02, 0.02), ink, rope, loc=(0, -0.05, -0.6 - 0.8 * k), rot=(0, D(30), 0))
        sphere("knot", 0.11, rope_m, rope, loc=(0, 0, -5.3), scale=(1, 1, 1.4))
        cube("handle", (0.3, 0.1, 0.1), wood, rope, loc=(0, 0, -5.55))
        self.snapshot()


def rope_idle(r, i, n):
    t = i / n
    r.rot("rope", y=1.5 * math.sin(2 * math.pi * t))


def rope_ring(r, i, n):
    """The bell swings through the ring (sought by the ring's progress)."""
    k = i / (n - 1)
    r.rot("bell", y=32 * math.sin(k * math.pi * 3))
    r.rot("rope", y=-10 * math.sin(k * math.pi * 3))
    r.move("rope", z=-0.1 * abs(math.sin(k * math.pi * 3)))


BELLROPE_CLIPS = [("idle", 12, 2, True, rope_idle), ("ring", 12, 6, False, rope_ring)]


# ---------------------------------------------------------------- Corra's

def crayon_small():
    return CrayonVoss(small=True)


def small_idle(c, i, n):
    crayon_idle(c, i, n)


CRAYONSMALL_CLIPS = [("idle", 12, 4, True, small_idle)]


# ---------------------------------------------------------------- the Archivist's

class QuillHand(Rig):
    """His quill hand: an owl's talon round a quill. Only a hand while he draws. 0.7 x 0.7."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        quill = self.add("quill", body, (0.05, -0.05, 0.0), rot=(0, D(-25), 0))
        grey, dark, ink, vane = mat("owl_grey", (0.50, 0.48, 0.46)), mat("owl_dark", (0.36, 0.34, 0.32)), mat("ink_grey", OWL_INK), mat("vane", (0.84, 0.82, 0.78))
        sphere("palm", 0.17, grey, body, scale=(1.2, 0.8, 0.9))
        for k, a in enumerate((-50, 0, 50)):
            cone("claw%d" % k, 0.05, 0.0, 0.3, dark, body, loc=(0.18 * math.cos(D(a)), -0.05, -0.18 * math.sin(D(a)) - 0.05), rot=(0, D(90 - a + 30), 0))
        cone("quill_m", 0.025, 0.01, 0.8, ink, quill, loc=(0, 0, 0.1))
        cone("quill_vane", 0.07, 0.02, 0.5, vane, quill, loc=(0.02, 0, 0.3))
        cone("nib", 0.02, 0.0, 0.12, ink, quill, loc=(0, 0, -0.36), rot=(0, D(180), 0))
        self.snapshot()


def hand_idle(h, i, n):
    t = i / n
    h.move("body", z=0.02 * math.sin(2 * math.pi * t))


def hand_draw(h, i, n):
    t = i / n
    h.rot("quill", y=12 * math.sin(4 * math.pi * t))
    h.move("body", x=0.08 * math.sin(2 * math.pi * t), z=0.04 * math.cos(4 * math.pi * t))


QUILLHAND_CLIPS = [("idle", 12, 2, True, hand_idle), ("draw", 12, 4, True, hand_draw)]


# ---------------------------------------------------------------- the Complete Survey's

class InkPool(Rig):
    """Ink pooled where the named ground was: strike it to wound the Atlas. 1.4 x 0.3."""
    def __init__(self):
        super().__init__()
        rng = random.Random(3)
        root = self.add("root")
        body = self.add("body", root)
        ink, mid = mat("ink", INK), mat("mid", SMUDGE_MID)
        sphere("pool", 0.7, ink, body, loc=(0, 0, -0.08), scale=(1.0, 0.6, 0.18))
        sphere("ripple", 0.45, mid, body, loc=(-0.1, -0.25, -0.05), scale=(1.0, 0.5, 0.1))
        for k in range(4):
            d = self.add("drop%d" % k, body, (rng.uniform(-0.5, 0.5), -0.2, rng.uniform(0.1, 0.4)))
            sphere("drop%d_m" % k, rng.uniform(0.04, 0.07), ink, d, scale=(1, 0.5, 1.4))
        self.snapshot()


def pool_idle(p, i, n):
    t = i / n
    p.scale("body", 1 + 0.04 * math.sin(2 * math.pi * t), 1, 1)
    for k in range(4):
        p.move("drop%d" % k, z=0.12 * math.sin(2 * math.pi * t + k * 1.3))


INKPOOL_CLIPS = [("idle", 12, 4, True, pool_idle)]


# ================================================================ all of them

# name, cell, builder, clips, line thickness at 2x, ink colour
# ---------------------------------------------------------------- Halvard's later hunts (CMB-12)

CORD = (0.52, 0.40, 0.26)
GRANITE = (0.58, 0.58, 0.55)
GRANITE_DARK = (0.44, 0.44, 0.42)


class CordLance(Rig):
    """Halvard's second lance in flight, point forward along +X, the cord trailing behind it. 1.6 x 0.4."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        ink, brass, cord = mat("ink", INK), mat("brass", BRASS), mat("cord", CORD)
        cone("shaft", 0.015, 0.011, 1.1, ink, body, loc=(0.05, 0, 0), rot=(0, D(90), 0))
        cone("tip", 0.0, 0.022, 0.14, brass, body, loc=(0.68, 0, 0), rot=(0, D(90), 0))
        cone("butt", 0.022, 0.022, 0.07, brass, body, loc=(-0.52, 0, 0), rot=(0, D(90), 0))
        for k in range(4):
            self.add("cord%d" % k, body, (-0.6 - 0.12 * k, 0, 0))
            cone("cord%d_m" % k, 0.008, 0.008, 0.13, cord, getattr(self, "cord%d" % k), rot=(0, D(90), 0))
        self.snapshot()


def cordlance_fly(c, i, n):
    """The cord ripples behind the lance as it flies."""
    for k in range(4):
        c.move("cord%d" % k, z=0.03 * math.sin(math.pi * (k + i)))
        c.rot("cord%d" % k, y=12 * math.cos(math.pi * (k + i)))


CORDLANCE_CLIPS = [("fly", 12, 2, True, cordlance_fly)]


class BridgeSpan(Rig):
    """One span of the Seven Bridges: granite blocks on an arch rib, 3 wide. The walking surface is a unit above the
    cell's centre (the game stands the drawing a unit under the floor), so the span has the cell's lower part to fall
    out of; the balustrade stands behind her, 0.3 over the deck."""
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 1.0))   # the walking surface
        rib = self.add("rib", body)
        granite, dark, ink = mat("granite", GRANITE), mat("granite_dark", GRANITE_DARK), mat("ink", INK)
        for k in range(5):
            cube("block%d" % k, (0.56, 0.6, 0.42), granite if k % 2 == 0 else dark, body, loc=(-1.2 + 0.6 * k, 0, -0.21))
        cube("deck", (3.0, 0.6, 0.06), dark, body, loc=(0, 0, -0.03))
        cube("rib_m", (2.9, 0.5, 0.18), dark, rib, loc=(0, 0, -0.52))
        for k in range(3):
            cube("rail%d" % k, (0.06, 0.06, 0.3), ink, body, loc=(-1.0 + 1.0 * k, 0.24, 0.15))
        cube("rail_top", (2.2, 0.05, 0.05), ink, body, loc=(0, 0.24, 0.3))
        for k in range(3):
            crack = self.add("crack%d" % k, body, (-0.9 + 0.9 * k, -0.32, -0.2))
            cube("crack%d_m" % k, (0.03, 0.02, 0.4), ink, crack, rot=(0, D(-20 + 20 * k), 0))
            self.prop("crack%d" % k, crack)
        # grit trickling from under the rib into the drop: the bridge is old, and it is high
        for k in range(3):
            g = self.add("grit%d" % k, body, (-0.8 + 0.8 * k, -0.1, -0.7))
            cube("grit%d_m" % k, (0.05, 0.05, 0.05), dark, g, rot=(0, D(30 * k), 0))
        self.snapshot()


def bridgespan_idle(b, i, n):
    """At rest: grit falling from under the rib, each grain a frame behind the last."""
    for k in range(3):
        f = ((i + k) % n) / n
        b.move("grit%d" % k, z=-0.8 * f, x=0.04 * f)


def bridgespan_fall(b, i, n):
    """The count: the span cracks, tips off its rib and drops out of the cell."""
    t = i / (n - 1)
    for k in range(3):
        b.show("crack%d" % k)
    drop = smoothstep(max(0.0, (t - 0.3) / 0.7))
    b.move("body", z=-3.2 * drop * drop, x=0.1 * drop)
    b.rot("body", y=-14 * drop)
    b.move("rib", z=-0.05 * min(1.0, t * 3))


BRIDGESPAN_CLIPS = [("idle", 12, 4, True, bridgespan_idle), ("fall", 12, 6, False, bridgespan_fall)]


PARTS = [
    ("Rubble", 2.0, Rubble, RUBBLE_CLIPS, 3.0, INK),
    ("Surge", 2.0, Surge, SURGE_CLIPS, 3.0, INK),
    ("ChorusLamp", 1.0, ChorusLamp, CHORUSLAMP_CLIPS, 2.6, INK),
    ("Feather", 1.2, Feather, FEATHER_CLIPS, 2.6, INK),
    ("Stone", 1.6, Stone, STONE_CLIPS, 3.0, INK),
    ("StoneStrike", 3.5, StoneStrike, STONESTRIKE_CLIPS, 2.6, (0.80, 0.66, 0.30)),
    ("StarFist", 2.0, StarFist, STARFIST_CLIPS, 3.2, INK),
    ("VossSeal", 5.5, VossSeal, VOSSSEAL_CLIPS, 3.0, INK),
    ("BellRope", 6.5, BellRope, BELLROPE_CLIPS, 2.8, FADED_INK),
    ("CrayonSmall", 1.6, crayon_small, CRAYONSMALL_CLIPS, 2.6, CRAYON_INK),
    ("QuillHand", 1.2, QuillHand, QUILLHAND_CLIPS, 2.6, OWL_INK),
    ("InkPool", 1.6, InkPool, INKPOOL_CLIPS, 2.8, INK),
    ("CordLance", 2.0, CordLance, CORDLANCE_CLIPS, 2.6, INK),
    ("BridgeSpan", 3.0, BridgeSpan, BRIDGESPAN_CLIPS, 3.0, INK),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in PARTS:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink, turnaround=False)


if __name__ == "__main__":
    main()

"""The late bosses, built and animated in Blender (CHR-08, CHR-09, CHR-10): the Collapse, the Gatekeeper, Surveyor
Hale, Guildmaster Voss, the Fallen Star, Corra's Drawing, the Archivist, the Complete Survey and the Half-Cathedral
Bells, each rendered side-on to frames for pack.py. The Choir has no body (it is the song; its doves are in
families.py), and the pieces a fight makes at run time (doves, rubble, feathers, stones, seals, ropes, the fist, the
quill hand, the pools) are in boss_parts.py.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/bosses.py
    ... -- Voss Archivist                (only those)
    python tools/characters/pack.py collapse gatekeeper hale voss fallenstar corrasdrawing archivist completesurvey halfcathedralbells

Every drawing is centred on its collider's centre, facing +X (Enemy.Face flips it). Clip names are what each boss's
Clip property asks for (Collapse.Clip, Gatekeeper.Clip, ...). Hale and Voss are Guild birds on the Warden rig of
saltmarrow_enemies.py; the rest are rigs of their own. Corra's Drawing has every clip twice: drawn in crayon, and in
outline for when the crayon runs out.
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER
from saltmarrow_enemies import (Warden, BRASS, GREY_INK, SMUDGE, SMUDGE_MID, warden_idle, warden_move, warden_telegraph,
                                warden_thrust, warden_recover, warden_hurt, warden_death)
from wardens import recolour, part, halvard_lunge, halvard_survey, halvard_call, halvard_count, halvard_withdraw

GOLD = (0.90, 0.76, 0.40)
STONE = (0.60, 0.60, 0.55)
STONE_DARK = (0.48, 0.48, 0.44)
IRON = (0.26, 0.24, 0.26)
IRON_LIGHT = (0.38, 0.36, 0.38)
EMBER = (0.86, 0.42, 0.14)
LAMP = (0.98, 0.80, 0.42)


def smoothstep(k):
    k = max(0.0, min(1.0, k))
    return k * k * (3 - 2 * k)


# ================================================================ The Collapse (cell 5.0; a smudge-beast as wide as its section)

class Collapse(Rig):
    """The mine collapse itself: a mass of ink strokes with rubble in it and lamp-light for eyes. The reaching arm is
    drawn only in the reach."""
    STROKES = 36

    def __init__(self):
        super().__init__()
        rng = random.Random(7)
        root = self.add("root")
        body = self.add("body", root)
        arm = self.add("arm", body, (0.7, -0.1, 0.5))
        dark, mid, glow, stone = mat("dark", SMUDGE), mat("mid", SMUDGE_MID), mat("lampglow", LAMP), mat("stone", (0.36, 0.33, 0.30))
        sphere("mass", 1.0, dark, body, loc=(0, 0, -0.4), scale=(2.0, 0.8, 0.9))
        sphere("mass2", 0.8, mid, body, loc=(-0.9, 0.1, 0.3), scale=(1.4, 0.8, 1.0))
        sphere("mass3", 0.9, dark, body, loc=(0.8, 0.05, 0.2), scale=(1.3, 0.8, 1.1))
        for k in range(7):
            s = rng.uniform(0.25, 0.5)
            cube("rock%d" % k, (s, s * 0.8, s * 0.7), stone, body, loc=(rng.uniform(-1.8, 1.8), rng.uniform(-0.25, 0.1), rng.uniform(-1.1, 0.6)),
                 rot=(0, D(rng.uniform(0, 45)), 0))
        for k in range(self.STROKES):
            st = self.add("stroke%d" % k, body, (rng.uniform(-2.2, 2.2), rng.uniform(-0.3, 0.2), rng.uniform(-1.3, 1.4)), rot=(0, D(rng.uniform(-80, 80)), 0))
            cube("stroke%d_m" % k, (rng.uniform(0.4, 1.2), 0.04, 0.04), dark if k % 3 else mid, st)
        for side in (-1, 1):
            sphere("eye%d" % side, 0.09, glow, body, loc=(0.5 + 0.35 * side, -0.6, 0.45), scale=(1, 0.5, 0.8))
        for k in range(6):
            cube("arm%d" % k, (0.55, 0.05, 0.05), dark if k % 2 else mid, arm, loc=(0.25 + 0.3 * k, 0, (k % 3 - 1) * 0.06), rot=(0, D((k % 2) * 16 - 8), 0))
        sphere("arm_hand", 0.16, dark, arm, loc=(2.05, 0, 0), scale=(1.2, 0.6, 1.0))
        self.prop("reach", arm)
        self.snapshot()

    def jitter(self, seed, amount):
        rng = random.Random(seed)
        for k in range(self.STROKES):
            self.move("stroke%d" % k, x=rng.uniform(-amount, amount), z=rng.uniform(-amount, amount))
            self.rot("stroke%d" % k, y=rng.uniform(-25, 25) * amount * 10)


def collapse_idle(c, i, n):
    t = i / n
    c.scale("body", 1 + 0.04 * math.sin(2 * math.pi * t), 1, 1 - 0.04 * math.sin(2 * math.pi * t))
    c.jitter(i, 0.03)


def collapse_rumble(c, i, n):
    """The dust rises: the mass shivers and lifts."""
    c.move("body", x=0.06 * (1 if i % 2 else -1), z=0.04 * i)
    c.jitter(10 + i, 0.08)


def collapse_shake(c, i, n):
    """The block comes down: the mass drops with it."""
    c.move("body", z=(0.15, -0.12, 0.0)[i])
    c.scale("body", 1 + (0.0, 0.12, 0.04)[i], 1, 1 - (0.0, 0.12, 0.04)[i])
    c.jitter(20 + i, 0.12)


def collapse_surge(c, i, n):
    """Ink streaming along the floor: it leans the way the surge goes."""
    t = i / n
    c.rot("body", y=12)
    c.scale("body", 1.3, 1, 0.8)
    c.move("body", x=0.1 * math.sin(2 * math.pi * t), z=-0.1)
    c.jitter(30 + i, 0.08)


def collapse_reach(c, i, n):
    """An arm of ink reaches up for the next lamp (sought by the reach's progress)."""
    k = smoothstep(i / (n - 1))
    c.show("reach")
    c.scale("arm", 0.3 + 1.1 * k, 1, 0.6 + 0.6 * k)
    c.rot("arm", y=-38 * k)
    c.rot("body", y=8 * k)
    c.move("body", z=0.1 * k)
    c.jitter(40 + i, 0.05)


def collapse_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    c.scale("body", 1 - 0.15 * k, 1, 1 + 0.2 * k)
    c.jitter(50 + i, 0.12 * k)


def collapse_death(c, i, n):
    t = min(1.0, i / 4)
    c.scale("body", 1 + 0.5 * t, 1, 1 - 0.6 * t)
    c.jitter(60 + i, 0.3 * t)


COLLAPSE_CLIPS = [
    ("idle", 12, 6, True, collapse_idle),
    ("rumble", 12, 4, False, collapse_rumble),
    ("shake", 12, 3, False, collapse_shake),
    ("surge", 12, 4, True, collapse_surge),
    ("reach", 12, 6, False, collapse_reach),
    ("hurt", 12, 2, False, collapse_hurt),
    ("death", 12, 6, False, collapse_death),
]


# ================================================================ The Gatekeeper (cell 5.0; a flying-age statue of an eagle, roots for wings)

ROOT_WOOD = (0.30, 0.26, 0.18)
MOSS = (0.42, 0.50, 0.30)
WING_REST = 200   # the wing cone points along its local x; this angle about y lays it back along the body


class Gatekeeper(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.1))
        neck = self.add("neck", body, (0.35, 0, 0.9))
        head = self.add("head", neck, (0.1, 0, 0.3))
        wn = self.add("wing_near", body, (-0.1, -0.5, 0.6), rot=(0, D(WING_REST), 0))
        wf = self.add("wing_far", body, (-0.1, 0.5, 0.6), rot=(0, D(WING_REST), 0))
        ln = self.add("leg_near", body, (0.1, -0.3, -0.7))
        lf = self.add("leg_far", body, (0.1, 0.3, -0.7))
        stone, dark, wood, moss, ink = mat("stone", STONE), mat("stone_dark", STONE_DARK), mat("root", ROOT_WOOD), mat("moss", MOSS), mat("ink", INK)
        sphere("torso", 0.9, stone, body, loc=(0, 0, 0.1), scale=(1.0, 0.8, 1.15))
        sphere("breast", 0.6, dark, body, loc=(0.35, -0.1, -0.2), scale=(0.9, 0.7, 1.0))
        cone("tail", 0.35, 0.08, 1.1, stone, body, loc=(-1.0, 0, -0.3), rot=(0, D(-105), 0))
        sphere("head_m", 0.42, stone, head, scale=(1.1, 0.9, 1.0))
        cone("beak", 0.16, 0.02, 0.5, dark, head, loc=(0.5, 0, -0.02), rot=(0, D(90), 0))
        cone("beak_hook", 0.07, 0.0, 0.18, dark, head, loc=(0.72, 0, -0.12), rot=(0, D(150), 0))
        sphere("eye", 0.07, ink, head, loc=(0.26, -0.3, 0.1))
        sphere("brow", 0.12, dark, head, loc=(0.2, -0.28, 0.2), scale=(1.4, 0.5, 0.4))
        for wing, side in ((wn, -1), (wf, 1)):
            cone(wing.name + "_root", 0.22, 0.05, 2.0, wood, wing, loc=(1.0, 0, 0), rot=(0, D(90), 0))
            for k, (x, ang, length) in enumerate(((0.6, 40, 0.8), (1.1, -35, 0.9), (1.5, 30, 0.7))):
                cone("%s_branch%d" % (wing.name, k), 0.08, 0.01, length, wood, wing, loc=(x, side * 0.02, 0), rot=(0, D(90 + ang), 0))
            sphere(wing.name + "_moss", 0.16, moss, wing, loc=(0.5, side * 0.1, 0.1), scale=(1.3, 0.6, 0.6))
        for leg in (ln, lf):
            cone(leg.name + "_m", 0.16, 0.12, 0.7, dark, leg, loc=(0, 0, -0.35))
            cube(leg.name + "_talon", (0.5, 0.14, 0.1), dark, leg, loc=(0.12, 0, -0.75))
            cone(leg.name + "_claw", 0.05, 0.0, 0.2, ink, leg, loc=(0.42, 0, -0.78), rot=(0, D(100), 0))
        self.snapshot()

    def wings(self, a_near, a_far=None):
        """Both wings to an angle about y: 200 lies them back along the body, 260 half open, 280 up, 10 forward, 30 forward and down."""
        if a_far is None:
            a_far = a_near
        self.rot("wing_near", y=a_near - WING_REST)
        self.rot("wing_far", y=a_far - WING_REST)


def gate_idle(g, i, n):
    t = i / n
    g.move("body", z=0.02 * math.sin(2 * math.pi * t))
    g.rot("head", y=3 * math.sin(2 * math.pi * t + 1))
    g.wings(200 + 3 * math.sin(2 * math.pi * t))


def gate_perch(g, i, n):
    """At the top of the gate, hanging from the roots: wings half open."""
    t = i / n
    g.wings(255 + 5 * math.sin(2 * math.pi * t), 250 - 5 * math.sin(2 * math.pi * t))
    g.move("body", z=0.1 + 0.03 * math.sin(2 * math.pi * t))
    g.rot("leg_near", y=25)
    g.rot("leg_far", y=25)


def gate_fly(g, i, n):
    """Flying, badly, for the first time since the Grounding."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    g.wings(265 + 40 * s, 262 + 40 * math.sin(2 * math.pi * t + 0.4))
    g.move("body", z=0.1 * s)
    g.rot("body", y=18)
    g.rot("leg_near", y=55)
    g.rot("leg_far", y=55)
    g.rot("neck", y=-10)


def gate_rise(g, i, n):
    k = smoothstep((i + 1) / n)
    g.wings(200 + 65 * k)
    g.move("body", z=0.15 * k)
    g.rot("leg_near", y=40 * k)
    g.rot("leg_far", y=40 * k)
    g.rot("neck", y=-12 * k)


def gate_telegraph(g, i, n):
    """The wings drawn back and up before the sweep."""
    k = min(1.0, (i + 1) / 3)
    g.wings(200 + 85 * k)
    g.rot("body", y=-10 * k)
    g.rot("neck", y=15 * k)
    g.rot("head", y=-5 * k)


def gate_sweep(g, i, n):
    """The wings sweep the floor."""
    k = i / (n - 1)
    g.wings(285 - 255 * k)
    g.rot("body", y=-10 + 22 * k)
    g.rot("neck", y=15 - 20 * k)
    g.move("body", x=0.15 * k)


def gate_shake(g, i, n):
    """It shakes the stone feathers loose."""
    g.move("body", x=0.07 * (1 if i % 2 else -1), z=0.03 * (i % 2))
    g.wings(215 + 12 * (i % 2), 225 - 12 * (i % 2))
    g.rot("head", y=6 * (1 if i % 2 else -1))


def gate_pass(g, i, n):
    """Across the gate at speed, heavy and flat."""
    t = i / n
    g.wings(185 + 10 * math.sin(2 * math.pi * t), 175 - 10 * math.sin(2 * math.pi * t))
    g.rot("body", y=25)
    g.rot("neck", y=-20)
    g.rot("leg_near", y=70)
    g.rot("leg_far", y=70)
    g.move("body", z=0.05 * math.sin(2 * math.pi * t))


def gate_land(g, i, n):
    """The heavy landing: wings up, the floor jumps."""
    g.wings((280, 270, 250)[i])
    g.move("body", z=(0.1, -0.15, -0.05)[i])
    g.rot("leg_near", y=(20, -5, 0)[i])
    g.rot("leg_far", y=(20, -5, 0)[i])
    g.rot("neck", y=(-10, 12, 5)[i])


def gate_recover(g, i, n):
    k = 1 - (i + 1) / (n + 1)
    g.wings(200 + 40 * k)
    g.rot("body", y=8 * k)


def gate_hurt(g, i, n):
    k = (1.0, 0.5)[i]
    g.rot("body", y=-10 * k)
    g.wings(200 + 35 * k)
    g.rot("head", y=10 * k)


def gate_death(g, i, n):
    """It lies with its wings open."""
    t = smoothstep(i / (n - 1))
    g.rot("body", y=75 * t)
    g.move("body", z=-0.5 * t, x=0.3 * t)
    g.wings(200 + 70 * t, 200 - 50 * t)
    g.rot("neck", y=-30 * t)
    g.rot("leg_near", y=-40 * t)
    g.rot("leg_far", y=-40 * t)


GATEKEEPER_CLIPS = [
    ("idle", 12, 4, True, gate_idle),
    ("perch", 12, 4, True, gate_perch),
    ("fly", 12, 6, True, gate_fly),
    ("rise", 12, 4, False, gate_rise),
    ("telegraph", 12, 3, False, gate_telegraph),
    ("sweep", 24, 4, False, gate_sweep),
    ("shake", 12, 4, False, gate_shake),
    ("pass", 12, 4, True, gate_pass),
    ("land", 24, 3, False, gate_land),
    ("recover", 12, 3, False, gate_recover),
    ("hurt", 12, 2, False, gate_hurt),
    ("death", 12, 6, False, gate_death),
]


# ================================================================ Surveyor Hale (the Warden rig; a bittern in a road-coat, a quill, a lens on a strap)

COAT = (0.42, 0.36, 0.30)
BITTERN = (0.70, 0.62, 0.48)
GLASS = (0.55, 0.70, 0.72)


class Hale(Warden):
    def __init__(self):
        super().__init__()
        recolour(self, "blue", "coat", COAT)
        recolour(self, "pale", "bittern", BITTERN)
        brass, ink, glass, paper, leather = mat("brass", BRASS), mat("ink", INK), mat("glass", GLASS), mat("paper", PAPER), mat("leather", (0.30, 0.24, 0.18))
        part(self, "gorget").hide_render = True            # not a Warden: no gorget
        part(self, "sight").hide_render = True
        self.lance.scale = (1, 1, 0.55)                    # a quill, not a lance
        part(self, "lance_tip").data.materials[0] = ink
        cone("quill_vane", 0.05, 0.02, 0.5, paper, self.lance, loc=(0.03, 0, 0.35), rot=(0, D(6), 0))
        lens = self.add("lens", self.body, (0.14, -0.2, 0.12))
        cone("lens_m", 0.075, 0.075, 0.02, brass, lens, rot=(D(90), 0, 0))
        cone("lens_glass", 0.055, 0.055, 0.026, glass, lens, rot=(D(90), 0, 0))
        cube("strap", (0.03, 0.03, 0.34), leather, self.body, loc=(0.1, -0.18, 0.28), rot=(0, D(-20), 0))
        cube("satchel", (0.2, 0.08, 0.24), leather, self.body, loc=(-0.2, -0.22, -0.16))
        cube("pages", (0.15, 0.02, 0.18), paper, self.body, loc=(-0.2, -0.27, -0.12))
        cone("coat_hem", 0.3, 0.12, 0.5, mat("coat", COAT), self.body, loc=(-0.06, 0, -0.05))
        self.snapshot()


def hale_sight(w, i, n):
    """The lens up at the stone (sought by the sighting's progress)."""
    k = smoothstep((i + 1) / n)
    w.move("lens", x=0.12 * k, z=0.5 * k)
    w.rot("lens", y=-10 * k)
    w.rot("body", y=6 * k)
    w.rot("neck", y=8 * k)
    w.rot("head", y=-6 * k)
    w.rot("lance", y=55 * k)
    w.move("lance", z=-0.1 * k)


HALE_CLIPS = [
    ("idle", 12, 4, True, warden_idle),
    ("move", 12, 8, True, warden_move),
    ("sight", 12, 4, False, hale_sight),
    ("call", 12, 4, False, halvard_call),
    ("count", 24, 3, False, halvard_count),
    ("telegraph", 12, 3, False, warden_telegraph),
    ("quill", 24, 3, False, warden_thrust),
    ("recover", 12, 3, False, warden_recover),
    ("hurt", 12, 2, False, warden_hurt),
    ("death", 12, 6, False, warden_death),
]


# ================================================================ Guildmaster Voss (the Warden rig, taller; a great grey heron in full brass, the compass-rose shield)

GREY_HERON = (0.46, 0.48, 0.52)
HERON_PALE = (0.74, 0.75, 0.76)
VOSS_SCALE = 1.15


class Voss(Warden):
    def __init__(self):
        super().__init__()
        recolour(self, "blue", "grey_heron", GREY_HERON)
        recolour(self, "pale", "heron_pale", HERON_PALE)
        brass, gold, ink = mat("brass", BRASS), mat("gold", GOLD), mat("ink", INK)
        part(self, "gorget").scale = (1.4, 1.4, 1.5)
        part(self, "crest").scale = (1, 1, 1.9)
        for i, z in enumerate((0.22, 0.02, -0.18)):
            cube("plate_%d" % i, (0.10, 0.38, 0.06), brass, self.body, loc=(0.17, 0, z), rot=(0, D(-10), 0))
        shield = self.add("shield", self.body, (0.0, 0.24, 0.05))
        cone("shield_m", 0.36, 0.36, 0.03, brass, shield, rot=(D(90), 0, 0))
        cone("shield_rim", 0.37, 0.37, 0.02, ink, shield, rot=(D(90), 0, 0), loc=(0, 0.01, 0))
        cone("shield_boss", 0.1, 0.1, 0.05, gold, shield, rot=(D(90), 0, 0), loc=(0, -0.02, 0))
        cube("rose_ns", (0.04, 0.01, 0.56), ink, shield, loc=(0, -0.03, 0))
        cube("rose_ew", (0.56, 0.01, 0.04), ink, shield, loc=(0, -0.03, 0))
        cube("rose_ne", (0.03, 0.01, 0.4), ink, shield, loc=(0, -0.03, 0), rot=(0, D(45), 0))
        cube("rose_nw", (0.03, 0.01, 0.4), ink, shield, loc=(0, -0.03, 0), rot=(0, D(-45), 0))
        self.root.scale = (VOSS_SCALE, VOSS_SCALE, VOSS_SCALE)
        self.snapshot()

    def guard(self, k):
        """The shield brought forward and turned to meet her."""
        self.move("shield", x=0.5 * k, y=-0.5 * k, z=0.15 * k)
        self.rot("shield", z=65 * k)


def voss_idle(w, i, n):
    warden_idle(w, i, n)


def voss_guard(w, i, n):
    """The compass-rose shield up, walking forward behind it."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.guard(1.0)
    w.rot("body", y=8)
    w.rot("lance", y=-30)
    w.move("lance", x=-0.1, z=0.1)
    w.legs(16 * s, -16 * s)
    w.move("body", z=0.02 * abs(s))


def voss_anchor(w, i, n):
    """The lance planted on the section: the rose is drawn there."""
    halvard_survey(w, i, n)
    k = min(1.0, (i + 1) / 3)
    w.guard(0.5 * k)
    w.rot("wing", x=20 * k)


def voss_telegraph(w, i, n):
    warden_telegraph(w, i, n)
    k = min(1.0, (i + 1) / 3)
    w.guard(0.25 * k)


def voss_withdraw(w, i, n):
    """He does not fall: he lowers the lance and the shield, and steps aside. Go."""
    halvard_withdraw(w, i, n)
    k = min(1.0, i / (n - 1))
    w.move("shield", z=-0.2 * k)


VOSS_CLIPS = [
    ("idle", 12, 4, True, voss_idle),
    ("move", 12, 8, True, warden_move),
    ("telegraph", 12, 3, False, voss_telegraph),
    ("thrust", 24, 3, False, warden_thrust),
    ("lunge", 24, 4, False, halvard_lunge),
    ("guard", 12, 6, True, voss_guard),
    ("anchor", 12, 4, False, voss_anchor),
    ("recover", 12, 3, False, warden_recover),
    ("hurt", 12, 2, False, warden_hurt),
    ("death", 12, 6, False, voss_withdraw),
]


# ================================================================ The Fallen Star (cell 4.5; an iron meteorite-golem, the seam on top)

class FallenStar(Rig):
    def __init__(self):
        super().__init__()
        rng = random.Random(11)
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.3))
        an = self.add("arm_near", body, (-0.1, -0.95, 0.5))
        af = self.add("arm_far", body, (-0.1, 0.95, 0.5))
        ln = self.add("leg_near", body, (0.3, -0.45, -0.75))
        lf = self.add("leg_far", body, (0.3, 0.45, -0.75))
        cracks = self.add("cracks", body)
        iron, light, seam, ember, ink = mat("iron", IRON), mat("iron_light", IRON_LIGHT), mat("seam", GOLD), mat("ember", EMBER), mat("ink", INK)
        sphere("core", 1.05, iron, body, scale=(1.1, 0.9, 0.95))
        for k in range(6):
            r = rng.uniform(0.3, 0.55)
            a = rng.uniform(0, 2 * math.pi)
            sphere("lump%d" % k, r, light if k % 2 else iron, body, loc=(1.0 * math.cos(a), rng.uniform(-0.2, 0.2), 0.9 * math.sin(a)), scale=(1.2, 0.8, 1.0))
        cube("seam", (0.7, 0.14, 0.07), seam, body, loc=(0.05, -0.02, 1.0))
        cube("seam_lip", (0.9, 0.3, 0.05), light, body, loc=(0.05, 0, 0.96))
        for arm, side in ((an, -1), (af, 1)):
            cone(arm.name + "_upper", 0.3, 0.24, 0.9, iron, arm, loc=(0, 0, -0.45))
            sphere(arm.name + "_fist", 0.42, light, arm, loc=(0, 0, -1.05), scale=(1.1, 0.9, 0.9))
            for k in range(3):
                cube("%s_knuckle%d" % (arm.name, k), (0.14, 0.12, 0.12), iron, arm, loc=(0.3, side * (0.14 - 0.14 * k), -1.0 - 0.02 * k))
        for leg in (ln, lf):
            cone(leg.name + "_m", 0.36, 0.3, 0.8, iron, leg, loc=(0, 0, -0.4))
            cube(leg.name + "_foot", (0.7, 0.4, 0.14), light, leg, loc=(0.1, 0, -0.82))
        for k in range(7):
            a = rng.uniform(0, 2 * math.pi)
            cube("crack%d" % k, (rng.uniform(0.3, 0.7), 0.05, 0.05), ember, cracks, loc=(1.05 * math.cos(a), -0.95 * abs(math.sin(a)) * 0.5 - 0.3, 0.9 * math.sin(a)),
                 rot=(0, D(rng.uniform(-60, 60)), 0))
        self.prop("burning", cracks)
        self.snapshot()


def star_idle(s, i, n):
    t = i / n
    s.move("body", z=0.02 * math.sin(2 * math.pi * t))
    s.rot("arm_near", y=3 * math.sin(2 * math.pi * t))
    s.rot("arm_far", y=-3 * math.sin(2 * math.pi * t))


def star_walk(s, i, n):
    t = i / n
    v = math.sin(2 * math.pi * t)
    s.rot("leg_near", y=22 * v)
    s.rot("leg_far", y=-22 * v)
    s.rot("arm_near", y=-14 * v)
    s.rot("arm_far", y=14 * v)
    s.move("body", z=0.06 * abs(v))
    s.rot("body", y=4)


def star_telegraph(s, i, n):
    """The fist raised high over the mark."""
    k = smoothstep((i + 1) / n)
    s.rot("arm_near", y=-150 * k)
    s.rot("body", y=-12 * k)
    s.move("body", z=0.08 * k)
    s.rot("leg_near", y=-10 * k)


def star_slam(s, i, n):
    """The fist comes down."""
    k = (0.6, 1.0, 1.0)[i]
    s.rot("arm_near", y=-150 + 200 * k)
    s.move("arm_near", x=0.3 * k, z=-0.15 * k)
    s.rot("body", y=-12 + 30 * k)
    s.move("body", z=0.08 - 0.22 * k)
    s.rot("leg_near", y=-10 + 30 * k)
    s.rot("leg_far", y=-15 * k)


def star_raise(s, i, n):
    """Iron drawn up from the ground: both arms rise to the sides."""
    k = smoothstep((i + 1) / n)
    s.rot("arm_near", y=-85 * k)
    s.rot("arm_far", y=-85 * k)
    s.move("arm_near", x=-0.2 * k)
    s.move("arm_far", x=-0.2 * k)
    s.move("body", z=0.12 * k)
    s.rot("body", y=-6 * k)


def star_recover(s, i, n):
    k = 1 - (i + 1) / (n + 1)
    s.rot("arm_near", y=50 * k)
    s.move("arm_near", x=0.3 * k, z=-0.15 * k)
    s.rot("body", y=18 * k)
    s.move("body", z=-0.14 * k)


def star_burn(s, i, n):
    """It burns: the cracks glow, the heat shimmers."""
    t = i / n
    s.show("burning")
    s.move("body", z=0.02 * math.sin(2 * math.pi * t))
    s.scale("body", 1 + 0.02 * math.sin(2 * math.pi * t), 1, 1 + 0.02 * math.cos(2 * math.pi * t))
    s.rot("arm_near", y=4 * math.sin(2 * math.pi * t))
    s.rot("arm_far", y=-4 * math.sin(2 * math.pi * t))


def star_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.rot("body", y=-7 * k)
    s.move("body", z=0.06 * k)
    s.rot("arm_near", y=12 * k)


def star_death(s, i, n):
    """It sinks back into its crater and goes cold."""
    t = smoothstep(i / (n - 1))
    s.move("body", z=-0.55 * t)
    s.rot("body", y=22 * t)
    s.rot("arm_near", y=-60 * t)
    s.rot("arm_far", y=60 * t)
    s.rot("leg_near", y=35 * t)
    s.rot("leg_far", y=-25 * t)


FALLENSTAR_CLIPS = [
    ("idle", 12, 4, True, star_idle),
    ("walk", 12, 6, True, star_walk),
    ("telegraph", 12, 3, False, star_telegraph),
    ("slam", 24, 3, False, star_slam),
    ("raise", 12, 4, False, star_raise),
    ("recover", 12, 3, False, star_recover),
    ("burn", 12, 4, True, star_burn),
    ("hurt", 12, 2, False, star_hurt),
    ("death", 12, 6, False, star_death),
]


# ================================================================ Corra's Drawing (cell 5.0; a child's crayon drawing of her father, huge, wrong)

CRAYON_INK = (0.30, 0.28, 0.34)
CRAYON_BLUE = (0.36, 0.44, 0.70)
CRAYON_RED = (0.86, 0.30, 0.24)
CRAYON_YELLOW = (0.92, 0.78, 0.30)
CRAYON_SKIN = (0.95, 0.86, 0.76)
CRAYON_GREY = (0.62, 0.64, 0.70)
PAPER_WHITE = (0.98, 0.98, 0.97)


class CrayonVoss(Rig):
    """A stick-and-circle father with a heron's beak and a lance, drawn in crayon. outline(True) takes every wash to
    paper and leaves the lines: the crayon has run out. small=True is the second one, drawn beside the first."""

    def __init__(self, small=False):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.2))
        head = self.add("head", body, (0.05, 0, 1.0))
        an = self.add("arm_near", body, (0.15, -0.45, 0.45))
        af = self.add("arm_far", body, (0.15, 0.45, 0.45))
        ln = self.add("leg_near", body, (0.25, -0.25, -0.55))
        lf = self.add("leg_far", body, (-0.05, 0.25, -0.55))
        blue, red, yellow, skin, grey, ink = (mat("c_blue", CRAYON_BLUE), mat("c_red", CRAYON_RED), mat("c_yellow", CRAYON_YELLOW),
                                             mat("c_skin", CRAYON_SKIN), mat("c_grey", CRAYON_GREY), mat("c_ink", CRAYON_INK))
        sphere("head_m", 0.55, skin, head, scale=(1.0, 0.8, 1.05))
        cone("beak", 0.16, 0.0, 0.7, yellow, head, loc=(0.75, 0, -0.08), rot=(0, D(90), 0))
        for side in (-1, 1):
            sphere("eye%d" % side, 0.07, ink, head, loc=(0.32, -0.42, 0.1 + 0.08 * side), scale=(1, 0.4, 1))
        for k in range(3):
            cube("crest%d" % k, (0.06, 0.06, 0.4), ink, head, loc=(-0.25 + 0.1 * k, 0, 0.55), rot=(0, D(-30 + 25 * k), 0))
        cube("coat", (0.9, 0.5, 1.1), blue, body, loc=(0, 0, 0.0))
        cube("gorget", (0.95, 0.52, 0.18), yellow, body, loc=(0, 0, 0.5))
        cube("buttons", (0.08, 0.1, 0.9), ink, body, loc=(0.42, -0.27, -0.05))
        for arm, side in ((an, -1), (af, 1)):
            cube(arm.name + "_m", (0.11, 0.11, 0.95), skin, arm, loc=(0, 0, -0.45))
            sphere(arm.name + "_hand", 0.14, skin, arm, loc=(0, 0, -0.95))
        lance = self.add("lance", an, (0.05, -0.05, -0.95), rot=(0, D(75), 0))
        cube("lance_m", (0.09, 0.09, 1.9), grey, lance, loc=(0, 0, 0.5))
        cone("lance_tip", 0.1, 0.0, 0.3, yellow, lance, loc=(0, 0, 1.55))
        for leg in (ln, lf):
            cube(leg.name + "_m", (0.13, 0.13, 1.15), red, leg, loc=(0, 0, -0.55))
            cube(leg.name + "_foot", (0.45, 0.2, 0.12), ink, leg, loc=(0.12, 0, -1.15))
        self.originals = {}
        for ob in root.children_recursive:
            if ob.type == "MESH":
                self.originals[ob.name] = list(ob.data.materials)
        if small:
            root.scale = (0.3, 0.3, 0.3)
        self.snapshot()

    def outline(self, on):
        paper = mat("c_paper", PAPER_WHITE)
        for ob in self.root.children_recursive:
            if ob.type != "MESH":
                continue
            for i, m in enumerate(self.originals[ob.name]):
                ob.data.materials[i] = paper if (on and m.name != "c_ink") else m

    def wobble(self, seed, amount=0.03):
        """A child's drawing never quite holds still."""
        rng = random.Random(seed)
        for name in ("head", "arm_near", "arm_far", "leg_near", "leg_far", "lance"):
            self.move(name, x=rng.uniform(-amount, amount), z=rng.uniform(-amount, amount))
        self.rot("root", y=rng.uniform(-1.5, 1.5))


def crayon_idle(c, i, n):
    t = i / n
    c.wobble(i)
    c.scale("body", 1 + 0.02 * math.sin(2 * math.pi * t), 1, 1 - 0.02 * math.sin(2 * math.pi * t))
    c.rot("head", z=3 * math.sin(2 * math.pi * t))


def crayon_move(c, i, n):
    t = i / n
    v = math.sin(2 * math.pi * t)
    c.wobble(10 + i)
    c.rot("leg_near", y=28 * v)
    c.rot("leg_far", y=-28 * v)
    c.rot("arm_far", y=20 * v)
    c.move("body", z=0.06 * abs(v))


def crayon_telegraph(c, i, n):
    """The crayon-arm drawn back for the swipe."""
    k = smoothstep((i + 1) / n)
    c.wobble(20 + i)
    c.rot("arm_near", y=-120 * k)
    c.rot("body", y=-10 * k)
    c.rot("head", y=-6 * k)


def crayon_swipe(c, i, n):
    k = (0.5, 1.0, 1.0)[i]
    c.wobble(30 + i)
    c.rot("arm_near", y=-120 + 190 * k)
    c.rot("body", y=-10 + 24 * k)
    c.move("body", x=0.15 * k)
    c.rot("head", y=-6 + 12 * k)


def crayon_lift(c, i, n):
    """The foot raised over the red mark."""
    k = smoothstep((i + 1) / n)
    c.wobble(40 + i)
    c.rot("leg_near", y=-70 * k)
    c.move("leg_near", z=0.2 * k)
    c.rot("body", y=-8 * k)
    c.move("body", z=0.1 * k)
    c.rot("arm_near", y=20 * k)
    c.rot("arm_far", y=-20 * k)


def crayon_stomp(c, i, n):
    k = (1.0, 1.0)[i]
    c.wobble(50 + i, 0.05)
    c.rot("leg_near", y=0)
    c.move("body", z=(-0.15, -0.08)[i])
    c.rot("body", y=(8, 4)[i])
    c.scale("body", 1.05, 1, 0.92)


def crayon_recover(c, i, n):
    k = 1 - (i + 1) / (n + 1)
    c.wobble(60 + i)
    c.rot("arm_near", y=40 * k)
    c.rot("body", y=8 * k)


def crayon_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    c.wobble(70 + i, 0.06 * k)
    c.rot("head", z=18 * k)
    c.rot("body", y=-10 * k)


def crayon_death(c, i, n):
    """The drawing comes apart: the pieces drift from each other."""
    t = smoothstep(i / (n - 1))
    c.wobble(80 + i, 0.04 + 0.1 * t)
    c.move("head", z=0.35 * t, x=-0.1 * t)
    c.rot("arm_near", y=-50 * t)
    c.rot("arm_far", y=50 * t)
    c.rot("body", y=12 * t)
    c.move("body", z=-0.2 * t)
    c.rot("leg_near", y=25 * t)
    c.rot("leg_far", y=-25 * t)


def drawn(fn):
    def pose(c, i, n):
        c.outline(False)
        fn(c, i, n)
    return pose


def outlined(fn):
    def pose(c, i, n):
        c.outline(True)
        fn(c, i, n)
    return pose


_CRAYON_BASE = [
    ("idle", 12, 4, True, crayon_idle),
    ("move", 12, 4, True, crayon_move),
    ("telegraph", 12, 3, False, crayon_telegraph),
    ("swipe", 24, 3, False, crayon_swipe),
    ("lift", 12, 3, False, crayon_lift),
    ("stomp", 24, 2, False, crayon_stomp),
    ("recover", 12, 3, False, crayon_recover),
    ("hurt", 12, 2, False, crayon_hurt),
    ("death", 12, 6, False, crayon_death),
]
# The outline clips first, so the drawn ones are rendered last and the turnaround (rendered after) is in crayon.
CORRASDRAWING_CLIPS = [(name + "_outline", fps, n, loop, outlined(fn)) for name, fps, n, loop, fn in _CRAYON_BASE] + \
                      [(name, fps, n, loop, drawn(fn)) for name, fps, n, loop, fn in _CRAYON_BASE]


# ================================================================ The Archivist (cell 4.0; an enormous half-drawn owl)

OWL = (0.58, 0.56, 0.52)
OWL_DARK = (0.40, 0.38, 0.37)
OWL_FACE = (0.84, 0.82, 0.78)
OWL_EYE = (0.90, 0.76, 0.40)
OWL_INK = lerp(INK, PAPER, 0.3)   # half-drawn: the line a little gone, not most of the way
OWL_WING_REST = 190


class Archivist(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, -0.1))
        head = self.add("head", body, (0.1, 0, 1.0))
        wn = self.add("wing_near", body, (-0.1, -0.6, 0.45), rot=(0, D(OWL_WING_REST), 0))
        wf = self.add("wing_far", body, (-0.1, 0.6, 0.45), rot=(0, D(OWL_WING_REST), 0))
        ln = self.add("leg_near", body, (0.15, -0.3, -0.85))
        lf = self.add("leg_far", body, (0.15, 0.3, -0.85))
        owl, dark, face, gold, ink, grey = mat("owl", OWL), mat("owl_dark", OWL_DARK), mat("owl_face", OWL_FACE), mat("owl_eye", OWL_EYE), mat("ink_grey", OWL_INK), mat("owl_grey", lerp(OWL_DARK, PAPER, 0.3))
        sphere("torso", 0.78, owl, body, scale=(1.0, 0.8, 1.2))
        sphere("belly", 0.5, face, body, loc=(0.3, -0.15, -0.2), scale=(0.9, 0.6, 1.0))
        cone("tail", 0.3, 0.1, 0.8, dark, body, loc=(-0.7, 0, -0.7), rot=(0, D(-140), 0))
        sphere("head_m", 0.52, owl, head, scale=(1.15, 0.95, 1.0))
        cone("disc", 0.46, 0.46, 0.06, face, head, loc=(0.3, 0, 0.0), rot=(0, D(90), 0))
        for side in (-1, 1):
            e = self.add("eye%d" % side, head, (0.42, side * 0.19, 0.06))
            sphere("eye%d_m" % side, 0.17, gold, e, scale=(0.5, 1, 1))
            sphere("pupil%d" % side, 0.09, ink, e, loc=(0.08, 0, 0), scale=(0.5, 1, 1))
            cone("tuft%d" % side, 0.12, 0.0, 0.5, dark, head, loc=(-0.05, side * 0.3, 0.6), rot=(side * D(-25), D(-20), 0))
        cone("beak", 0.07, 0.0, 0.2, ink, head, loc=(0.55, 0, -0.18), rot=(0, D(130), 0))
        for wing, side in ((wn, -1), (wf, 1)):
            sphere(wing.name + "_m", 0.55, dark, wing, loc=(0.7, 0, 0), scale=(1.4, 0.35, 0.8))
            for k in range(3):
                cone("%s_tip%d" % (wing.name, k), 0.1, 0.0, 0.6, grey, wing, loc=(1.35, side * 0.02, -0.15 + 0.14 * k), rot=(0, D(90 + 12 * (k - 1)), 0))
        quill = self.add("quill", wn, (1.5, -0.05, 0.1), rot=(0, D(-20), 0))
        cone("quill_m", 0.03, 0.01, 1.0, ink, quill, loc=(0, 0, 0.45))
        cone("quill_vane", 0.07, 0.02, 0.6, face, quill, loc=(0.03, 0, 0.6))
        for leg in (ln, lf):
            cone(leg.name + "_m", 0.14, 0.1, 0.45, grey, leg, loc=(0, 0, -0.22))
            cube(leg.name + "_talon", (0.42, 0.14, 0.08), dark, leg, loc=(0.1, 0, -0.48))
        cube("keystone", (0.26, 0.2, 0.26), gold, lf, loc=(0.25, 0, -0.35), rot=(0, D(45), 0))
        self.snapshot()

    def wings(self, a_near, a_far=None):
        if a_far is None:
            a_far = a_near
        self.rot("wing_near", y=a_near - OWL_WING_REST)
        self.rot("wing_far", y=a_far - OWL_WING_REST)

    def eyes(self, k):
        """Lids: 1 open, 0 closed."""
        for side in (-1, 1):
            self.scale("eye%d" % side, 1, 1, max(0.08, k))


def owl_idle(o, i, n):
    t = i / n
    o.move("body", z=0.025 * math.sin(2 * math.pi * t))
    o.rot("head", y=4 * math.sin(2 * math.pi * t + 1))
    o.wings(190 + 2 * math.sin(2 * math.pi * t))
    o.eyes(0.4 if i == n - 1 else 1.0)


def owl_telegraph(o, i, n):
    """The quill lifted to the page."""
    k = smoothstep((i + 1) / n)
    o.wings(190 - 85 * k, 190)
    o.rot("quill", y=-30 * k)
    o.rot("head", y=-8 * k)
    o.move("body", z=0.05 * k)


def owl_draw(o, i, n):
    """The quill on the page: the wing scribbles."""
    t = i / n
    o.wings(105 + 8 * math.sin(4 * math.pi * t), 192)
    o.move("wing_near", x=0.08 * math.sin(2 * math.pi * t), z=0.04 * math.cos(4 * math.pi * t))
    o.rot("quill", y=-30 + 12 * math.sin(4 * math.pi * t))
    o.rot("head", y=-10 + 4 * math.sin(2 * math.pi * t))
    o.move("body", z=0.05)


def owl_swoop(o, i, n):
    """Low across the room, the wings flat."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    o.wings(275 + 25 * s, 265 - 25 * s)
    o.rot("body", y=28)
    o.rot("head", y=-18)
    o.rot("leg_near", y=55)
    o.rot("leg_far", y=55)
    o.move("body", z=0.06 * s)


def owl_recover(o, i, n):
    k = 1 - (i + 1) / (n + 1)
    o.wings(190 - 40 * k, 190)
    o.rot("body", y=10 * k)


def owl_hold(o, i, n):
    """He stops drawing, and holds: wings tight, the eyes nearly shut."""
    t = i / n
    o.wings(186, 186)
    o.move("body", z=0.01 * math.sin(2 * math.pi * t))
    o.eyes(0.25)
    o.rot("head", y=6)


def owl_hurt(o, i, n):
    k = (1.0, 0.5)[i]
    o.rot("body", y=-9 * k)
    o.wings(190 + 35 * k)
    o.rot("head", y=12 * k)
    o.eyes(1.0)


def owl_death(o, i, n):
    """He slumps on his perch; the keystone stays in his talon."""
    t = smoothstep(i / (n - 1))
    o.rot("body", y=32 * t)
    o.wings(190 + 55 * t, 190 - 40 * t)
    o.move("head", z=-0.25 * t)
    o.rot("head", y=25 * t)
    o.eyes(1 - 0.9 * t)


ARCHIVIST_CLIPS = [
    ("idle", 12, 6, True, owl_idle),
    ("telegraph", 12, 3, False, owl_telegraph),
    ("draw", 12, 6, True, owl_draw),
    ("swoop", 12, 4, True, owl_swoop),
    ("recover", 12, 3, False, owl_recover),
    ("hold", 12, 4, True, owl_hold),
    ("hurt", 12, 2, False, owl_hurt),
    ("death", 12, 6, False, owl_death),
]


# ================================================================ The Complete Survey (cell 4.0; the Great Atlas, open on the floor, its ink rising)

PAGE = (0.93, 0.92, 0.88)
COVER = (0.36, 0.28, 0.20)


class CompleteSurvey(Rig):
    def __init__(self):
        super().__init__()
        rng = random.Random(5)
        root = self.add("root")
        book = self.add("book", root, (0, 0, -0.35), rot=(D(-55), 0, 0))
        pn = self.add("page_near", book, (-0.05, 0, 0.0))
        pf = self.add("page_far", book, (0.05, 0, 0.0))
        plume = self.add("plume", root, (0, -0.1, 0.1))
        page, cover, gilt, ink = mat("page", PAGE), mat("cover", COVER), mat("gilt", GOLD), mat("ink", INK)
        cube("spine", (0.14, 1.6, 0.16), cover, book, loc=(0, 0, -0.02))
        cube("page_near_m", (1.6, 1.5, 0.12), page, pn, loc=(-0.85, 0, 0.02), rot=(0, D(6), 0))
        cube("page_near_cover", (1.7, 1.6, 0.05), cover, pn, loc=(-0.88, 0, -0.07), rot=(0, D(6), 0))
        cube("page_far_m", (1.6, 1.5, 0.12), page, pf, loc=(0.85, 0, 0.02), rot=(0, D(-6), 0))
        cube("page_far_cover", (1.7, 1.6, 0.05), cover, pf, loc=(0.88, 0, -0.07), rot=(0, D(-6), 0))
        for k in range(5):
            cube("line_n%d" % k, (1.0, 0.03, 0.03), ink, pn, loc=(-0.85, -0.5 + 0.25 * k, 0.09), rot=(0, D(6), 0))
            cube("line_f%d" % k, (1.0, 0.03, 0.03), ink, pf, loc=(0.85, -0.5 + 0.25 * k, 0.09), rot=(0, D(-6), 0))
        cube("gilt_edge", (0.1, 1.5, 0.06), gilt, book, loc=(-1.65, 0, 0.0), rot=(0, D(6), 0))
        for k in range(16):
            st = self.add("ink%d" % k, plume, (rng.uniform(-0.5, 0.5), rng.uniform(-0.15, 0.15), rng.uniform(0.0, 1.7)), rot=(0, D(rng.uniform(-30, 30) + 90), 0))
            cube("ink%d_m" % k, (rng.uniform(0.2, 0.6), 0.04, 0.04), ink, st)
        for k in range(5):
            d = self.add("drop%d" % k, plume, (rng.uniform(-0.6, 0.6), -0.15, rng.uniform(0.3, 1.9)))
            sphere("drop%d_m" % k, rng.uniform(0.04, 0.08), ink, d, scale=(1, 0.5, 1.3))
        self.snapshot()

    def seethe(self, seed, amount):
        rng = random.Random(seed)
        for k in range(16):
            self.move("ink%d" % k, x=rng.uniform(-amount, amount), z=rng.uniform(-amount, amount) + 0.1 * amount)
            self.rot("ink%d" % k, y=rng.uniform(-20, 20) * amount * 10)
        for k in range(5):
            self.move("drop%d" % k, z=rng.uniform(-amount, amount) * 2)


def atlas_idle(a, i, n):
    t = i / n
    a.seethe(i, 0.05)
    a.rot("page_near", y=4 * math.sin(2 * math.pi * t))
    a.rot("page_far", y=-4 * math.sin(2 * math.pi * t + 0.5))


def atlas_hurt(a, i, n):
    k = (1.0, 0.5)[i]
    a.seethe(10 + i, 0.14 * k)
    a.rot("page_near", y=30 * k)
    a.rot("page_far", y=-30 * k)
    a.move("book", z=-0.05 * k)


def atlas_death(a, i, n):
    """The Atlas closes."""
    t = smoothstep(i / (n - 1))
    a.seethe(20 + i, 0.08 * (1 - t))
    a.scale("plume", 1, 1, 1 - 0.95 * t)
    a.rot("page_near", y=85 * t)
    a.rot("page_far", y=-85 * t)


COMPLETESURVEY_CLIPS = [
    ("idle", 12, 6, True, atlas_idle),
    ("hurt", 12, 2, False, atlas_hurt),
    ("death", 12, 6, False, atlas_death),
]


# ================================================================ The Half-Cathedral Bells (cell 5.0; the beam over the nave and its four bells, faded)

FADED_INK = (0.55, 0.55, 0.53)
FADED_BRASS = (0.80, 0.72, 0.52)
FADED_STONE = (0.90, 0.90, 0.88)


class HalfCathedralBells(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        beam = self.add("beam", root, (0, 0, 0.9))
        stone, brass, wood, ink = mat("f_stone", FADED_STONE), mat("f_brass", FADED_BRASS), mat("f_wood", (0.74, 0.68, 0.58)), mat("f_ink", FADED_INK)
        cube("beam_m", (4.8, 0.36, 0.34), wood, beam)
        cube("beam_cap", (5.0, 0.4, 0.1), stone, beam, loc=(0, 0, 0.22))
        for k, (x, r) in enumerate(((-1.8, 0.3), (-0.6, 0.3), (0.6, 0.3), (1.8, 0.45))):
            b = self.add("bell%d" % k, beam, (x, 0, -0.2))
            cube("hanger%d" % k, (0.05, 0.05, 0.25), ink, b, loc=(0, 0, -0.1))
            cone("bell%d_m" % k, r, r * 0.45, r * 1.6, brass, b, loc=(0, 0, -0.25 - r * 0.8))
            cone("bell%d_lip" % k, r * 1.05, r * 1.0, 0.06, ink, b, loc=(0, 0, -0.25 - r * 1.55))
            sphere("clapper%d" % k, r * 0.18, ink, b, loc=(0, 0, -0.25 - r * 1.6))
        self.snapshot()


def bells_idle(b, i, n):
    t = i / n
    for k in range(4):
        b.rot("bell%d" % k, y=2 * math.sin(2 * math.pi * t + k))


def bells_hurt(b, i, n):
    k = (1.0, 0.5)[i]
    b.rot("bell1", y=25 * k)
    b.rot("bell2", y=-20 * k)
    b.move("beam", z=-0.03 * k)


def bells_death(b, i, n):
    t = i / (n - 1)
    for k in range(4):
        b.rot("bell%d" % k, y=4 * math.sin(2 * math.pi * t + k) * (1 - t))


HALFCATHEDRALBELLS_CLIPS = [
    ("idle", 12, 4, True, bells_idle),
    ("hurt", 12, 2, False, bells_hurt),
    ("death", 12, 4, False, bells_death),
]


# ================================================================ all of them

# name, cell, builder, clips, line thickness at 2x, ink colour
BOSSES = [
    ("Collapse", 5.0, Collapse, COLLAPSE_CLIPS, 3.2, INK),
    ("Gatekeeper", 5.0, Gatekeeper, GATEKEEPER_CLIPS, 3.4, INK),
    ("Hale", 2.8, Hale, HALE_CLIPS, 3.0, INK),
    ("Voss", 3.2, Voss, VOSS_CLIPS, 3.0, INK),
    ("FallenStar", 4.5, FallenStar, FALLENSTAR_CLIPS, 3.4, INK),
    ("CorrasDrawing", 5.0, CrayonVoss, CORRASDRAWING_CLIPS, 3.8, CRAYON_INK),
    ("Archivist", 4.0, Archivist, ARCHIVIST_CLIPS, 3.0, OWL_INK),
    ("CompleteSurvey", 4.0, CompleteSurvey, COMPLETESURVEY_CLIPS, 3.0, INK),
    ("HalfCathedralBells", 5.0, HalfCathedralBells, HALFCATHEDRALBELLS_CLIPS, 2.8, FADED_INK),
]


# The Guild birds stand in rooms as NPCs too (Voss at the Threshold, Hale at the Stones), drawn on their colliders'
# centres like every enemy, so their manifests say where their feet are, as Halvard's does (wardens.py FEET).
FEET = {"Hale": -0.82, "Voss": -0.82 * VOSS_SCALE}


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in BOSSES:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink, feet=FEET.get(name))


if __name__ == "__main__":
    main()

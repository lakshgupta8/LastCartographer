"""The coast's creatures, built and animated in Blender (CHR-06): the marsh crab, the reed skimmer, the smudge,
the Cantor, the Warden, the lost Remnant and the Lamp-Keeper, each rendered side-on to frames for pack.py.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/saltmarrow_enemies.py
    ... -- MarshCrab Warden            (only those creatures)
    python tools/characters/pack.py marshcrab reedskimmer smudge cantor warden lostremnant lampkeeper

Each creature is centred on its collider's centre (the enemy's transform), facing +X; Enemy.Face flips the
quad. Silhouette families follow art-direction 4: Wardens are tall vertical lines (a heron with a lance),
the marsh fauna are rounds, Cantors are teardrops with a bell, Smudges are scribbles, the Remnant are the same
shapes with the ink removed. Clip names are what EnemyAnimator asks for through Enemy.Clip.
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER

GREY_INK = lerp(INK, PAPER, 0.55)   # the Remnant's line: half gone


# ================================================================ Marsh crab (0.9 x 0.7, pogo it)

CRAB = (0.62, 0.34, 0.26)
CRAB_PALE = (0.80, 0.62, 0.50)


class MarshCrab(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        claw_f = self.add("claw_f", body, (0.30, -0.12, -0.05))
        claw_b = self.add("claw_b", body, (0.30, 0.12, -0.05))
        eyes = self.add("eyes", body, (0.16, 0, 0.16))
        shell, pale, ink = mat("shell", CRAB), mat("pale", CRAB_PALE), mat("ink", INK)
        sphere("carapace", 0.36, shell, body, loc=(0, 0, 0.02), scale=(1.15, 0.9, 0.62))
        sphere("belly", 0.3, pale, body, loc=(0.02, 0, -0.1), scale=(1.05, 0.8, 0.4))
        for i, (x, side) in enumerate([(-0.22, -1), (-0.08, -1), (0.06, -1), (0.18, -1), (-0.22, 1), (-0.08, 1), (0.06, 1), (0.18, 1)]):
            leg = self.add("leg%d" % i, body, (x, side * 0.28, -0.08))
            cone("leg%d_a" % i, 0.03, 0.02, 0.26, shell, leg, loc=(0, side * 0.05, -0.1), rot=(D(-side * 35), 0, 0))
            cone("leg%d_b" % i, 0.02, 0.008, 0.2, shell, leg, loc=(0, side * 0.16, -0.26), rot=(D(side * 25), 0, 0))
        for claw, side in ((claw_f, -1), (claw_b, 1)):
            cone("arm_" + claw.name, 0.035, 0.03, 0.22, shell, claw, loc=(0.08, 0, 0), rot=(0, D(90), 0))
            sphere("pincer_" + claw.name, 0.09, shell, claw, loc=(0.24, 0, 0.02), scale=(1.3, 0.8, 0.9))
            cone("tip_" + claw.name, 0.05, 0.005, 0.14, shell, claw, loc=(0.36, 0, 0.06), rot=(0, D(80), 0))
        for side in (-1, 1):
            cone("stalk%d" % side, 0.018, 0.014, 0.16, shell, eyes, loc=(0, side * 0.07, 0.06))
            sphere("eye%d" % side, 0.035, ink, eyes, loc=(0.01, side * 0.07, 0.15))
        self.snapshot()

    def legs_phase(self, t, amp=25.0):
        for i in range(8):
            ph = t * 2 * math.pi + (i % 4) * math.pi / 2 + (0 if i < 4 else math.pi)
            self.rot("leg%d" % i, y=amp * math.sin(ph))


def crab_idle(c, i, n):
    b = math.sin(2 * math.pi * i / n)
    c.move("eyes", z=0.01 * b)
    c.rot("claw_f", y=6 * b)
    c.rot("claw_b", y=-5 * b)
    c.move("body", z=0.005 * b)


def crab_move(c, i, n):
    t = i / n
    c.legs_phase(t)
    c.move("body", z=0.02 * abs(math.sin(2 * math.pi * t)))
    c.rot("claw_f", y=4 * math.sin(2 * math.pi * t))


def crab_hop(c, i, n):
    k = (0.0, 0.7, 1.0)[i]
    c.scale("body", 1.0 + 0.1 * (1 - k), 1, 0.85 + 0.25 * k)
    for j in range(8):
        c.rot("leg%d" % j, y=-30 * k)
    c.rot("claw_f", y=-35 * k)
    c.rot("claw_b", y=-35 * k)
    c.move("claw_f", z=0.1 * k)
    c.move("claw_b", z=0.1 * k)


def crab_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    c.scale("body", 1.1 * k + (1 - k), 1, 0.9 * k + (1 - k))
    c.rot("claw_f", y=40 * k)
    c.rot("claw_b", y=40 * k)
    c.move("eyes", z=-0.05 * k)


def crab_death(c, i, n):
    t = min(1.0, i / 3)
    c.rot("body", x=170 * t)
    c.move("body", z=0.1 * math.sin(math.pi * t))
    for j in range(8):
        c.rot("leg%d" % j, y=35 * math.sin(2 * math.pi * t + j))


CRAB_CLIPS = [
    ("idle", 12, 4, True, crab_idle),
    ("move", 12, 6, True, crab_move),
    ("hop", 12, 3, False, crab_hop),
    ("hurt", 12, 2, False, crab_hurt),
    ("death", 12, 4, False, crab_death),
]


# ================================================================ Reed skimmer (0.9 x 0.5, fodder on the wing)

SKIM = (0.35, 0.55, 0.50)
SKIM_PALE = (0.70, 0.80, 0.74)
FLARE = (0.95, 0.85, 0.35)


class ReedSkimmer(Rig):
    def __init__(self, flare=False):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        head = self.add("head", body, (0.30, 0, 0.04))
        wf = self.add("wing_front", body, (0.02, 0, 0.06))
        wb = self.add("wing_back", body, (-0.10, 0, 0.06))
        tail = self.add("tail", body, (-0.30, 0, 0))
        base = FLARE if flare else SKIM
        skin, pale, ink = mat("skin", base), mat("pale", lerp(SKIM_PALE, FLARE, 0.5) if flare else SKIM_PALE), mat("ink", INK)
        sphere("thorax", 0.14, skin, body, scale=(1.8, 0.7, 0.8))
        sphere("head_mesh", 0.10, skin, head, scale=(1.0, 0.9, 0.9))
        cone("beak", 0.03, 0.003, 0.22, ink, head, loc=(0.16, 0, -0.01), rot=(0, D(90), 0))
        sphere("eye", 0.03, ink, head, loc=(0.04, -0.08, 0.03))
        for w, side in ((wf, -1), (wf, 1), (wb, -1), (wb, 1)):
            sphere("%s_%d" % (w.name, side), 0.1, pale, w, loc=(0, side * 0.32, 0.04), scale=(2.2, 3.4, 0.25))
        cone("tail_mesh", 0.05, 0.005, 0.4, skin, tail, loc=(-0.18, 0, 0), rot=(0, D(-90), 0))
        self.snapshot()

    def wings(self, a):
        self.rot("wing_front", x=a)
        self.rot("wing_back", x=-a * 0.8)


def skim_idle(s, i, n):
    t = i / n
    s.wings(14 * math.sin(2 * math.pi * t))
    s.move("body", z=0.02 * math.sin(2 * math.pi * t))


def skim_rise(s, i, n):
    k = (0.3, 0.7, 1.0)[i]
    s.rot("body", y=-40 * k)
    s.wings(45 * k)
    s.move("body", z=0.06 * k)


def skim_dive(s, i, n):
    s.rot("body", y=(50, 62)[i])
    s.wings(-20)
    s.scale("body", 1.15, 1, 0.9)


def skim_move(s, i, n):
    t = i / n
    s.wings(30 * math.sin(2 * math.pi * t))
    s.rot("body", y=8)


def skim_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.rot("body", y=-25 * k, z=15 * k)
    s.wings(50 * k)


def skim_death(s, i, n):
    t = min(1.0, i / 3)
    s.rot("body", y=90 * t)
    s.move("body", z=-0.25 * t)
    s.wings(-40 * t)


def skimmer_clips():
    return [
        ("idle", 12, 4, True, skim_idle),
        ("move", 12, 4, True, skim_move),
        ("dive", 24, 2, True, skim_dive),
        ("hurt", 12, 2, False, skim_hurt),
        ("death", 12, 4, False, skim_death),
    ]


# ================================================================ Smudge (1.1 x 1.1, a scribble)

SMUDGE = (0.12, 0.12, 0.16)
SMUDGE_MID = (0.30, 0.30, 0.36)


class Smudge(Rig):
    def __init__(self):
        super().__init__()
        rng = random.Random(3)
        root = self.add("root")
        body = self.add("body", root)
        dark, mid, paper = mat("dark", SMUDGE), mat("mid", SMUDGE_MID), mat("paper", PAPER)
        sphere("blob", 0.34, dark, body, scale=(1.2, 0.8, 1.0))
        sphere("blob2", 0.26, mid, body, loc=(-0.12, 0.05, 0.14), scale=(1.1, 0.8, 0.9))
        sphere("blob3", 0.2, dark, body, loc=(0.2, -0.02, -0.12), scale=(1.1, 0.8, 0.8))
        for k in range(14):
            st = self.add("stroke%d" % k, body, (rng.uniform(-0.35, 0.35), rng.uniform(-0.15, 0.15), rng.uniform(-0.35, 0.4)),
                          rot=(0, D(rng.uniform(-70, 70)), 0))
            cube("stroke%d_m" % k, (rng.uniform(0.25, 0.6), 0.03, 0.03), dark if k % 3 else mid, st)
        for side in (-1, 1):
            sphere("eye%d" % side, 0.045, paper, body, loc=(0.18, -0.2, 0.08 + 0.02 * side), scale=(1, 0.5, 1))
        self.snapshot()

    def jitter(self, seed, amount):
        rng = random.Random(seed)
        for k in range(14):
            self.move("stroke%d" % k, x=rng.uniform(-amount, amount), z=rng.uniform(-amount, amount))
            self.rot("stroke%d" % k, y=rng.uniform(-25, 25) * amount * 10)


def smudge_idle(s, i, n):
    t = i / n
    s.scale("body", 1 + 0.06 * math.sin(2 * math.pi * t), 1, 1 - 0.06 * math.sin(2 * math.pi * t))
    s.jitter(i, 0.03)


def smudge_move(s, i, n):
    t = i / n
    s.scale("body", 1.25, 1, 0.85)
    s.rot("body", y=8 * math.sin(2 * math.pi * t))
    s.jitter(10 + i, 0.05)


def smudge_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.scale("body", 1 - 0.2 * k, 1, 1 + 0.25 * k)
    s.jitter(20 + i, 0.12 * k)


def smudge_death(s, i, n):
    t = min(1.0, i / 3)
    s.scale("body", 1 + 0.5 * t, 1, 1 - 0.6 * t)
    s.jitter(30 + i, 0.25 * t)


SMUDGE_CLIPS = [
    ("idle", 12, 6, True, smudge_idle),
    ("move", 12, 4, True, smudge_move),
    ("hurt", 12, 2, False, smudge_hurt),
    ("death", 12, 4, False, smudge_death),
]


# ================================================================ Cantor (0.8 x 0.9, a dove with a bell)

DOVE = (0.88, 0.86, 0.82)
DOVE_DARK = (0.66, 0.64, 0.62)
BRASS = (0.78, 0.62, 0.30)
GLASS = (0.55, 0.70, 0.72)


class Cantor(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.05))
        head = self.add("head", body, (0.12, 0, 0.30))
        wn = self.add("wing_near", body, (-0.02, -0.22, 0.10))
        wfar = self.add("wing_far", body, (-0.02, 0.22, 0.10))
        bell = self.add("bell", body, (0.06, 0, -0.30))
        flask = self.add("flask", body, (-0.22, -0.16, -0.12))
        dove, dark, brass, glass, ink = mat("dove", DOVE), mat("dove_dark", DOVE_DARK), mat("brass", BRASS), mat("glass", GLASS), mat("ink", INK)
        # a teardrop: a sphere over a cone
        sphere("breast", 0.26, dove, body, loc=(0.02, 0, 0.02), scale=(1.0, 0.85, 1.0))
        cone("tail_drop", 0.24, 0.05, 0.36, dove, body, loc=(-0.06, 0, -0.16), rot=(0, D(-10), 0))
        sphere("head_mesh", 0.15, dove, head)
        cone("beak", 0.045, 0.004, 0.14, brass, head, loc=(0.18, 0, -0.02), rot=(0, D(90), 0))
        sphere("eye", 0.028, ink, head, loc=(0.07, -0.13, 0.04))
        sphere("wing_near_m", 0.16, dark, wn, loc=(-0.06, -0.03, -0.04), scale=(1.4, 0.3, 0.7), rot=(0, D(-20), 0))
        sphere("wing_far_m", 0.16, dark, wfar, loc=(-0.06, 0.03, -0.04), scale=(1.4, 0.3, 0.7), rot=(0, D(-20), 0))
        cone("bell_cord", 0.012, 0.012, 0.2, ink, bell, loc=(0, 0, 0.14))
        cone("bell_mesh", 0.12, 0.05, 0.18, brass, bell, loc=(0, 0, 0.0))
        sphere("clapper", 0.03, ink, bell, loc=(0, 0, -0.1))
        cone("flask_mesh", 0.06, 0.05, 0.16, glass, flask)
        cone("flask_neck", 0.025, 0.03, 0.06, glass, flask, loc=(0, 0, 0.11))
        self.snapshot()

    def flap(self, a):
        self.rot("wing_near", x=a)
        self.rot("wing_far", x=-a)


def cantor_idle(c, i, n):
    t = i / n
    c.move("body", z=0.03 * math.sin(2 * math.pi * t))
    c.flap(12 + 8 * math.sin(2 * math.pi * t))
    c.rot("bell", y=6 * math.sin(2 * math.pi * t))


def cantor_move(c, i, n):
    t = i / n
    c.flap(35 * math.sin(2 * math.pi * t))
    c.rot("body", y=10)
    c.rot("bell", y=-15)


def cantor_ring(c, i, n):
    k = i / (n - 1)
    c.move("bell", z=0.9 * k, x=0.1 * k)
    c.rot("bell", y=25 * math.sin(k * math.pi * 3))
    c.flap(60 * k)
    c.rot("head", y=-25 * k)
    c.move("body", z=0.05 * k)


def cantor_recover(c, i, n):
    k = 1 - i / n
    c.move("bell", z=0.9 * k * 0.5)
    c.flap(20 * k)
    c.rot("head", y=15 * (1 - k))
    c.move("body", z=-0.04 * (1 - k))


def cantor_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    c.rot("body", y=-25 * k)
    c.flap(50 * k)
    c.rot("bell", y=40 * k)


def cantor_death(c, i, n):
    t = min(1.0, i / 3)
    c.rot("body", y=-80 * t)
    c.move("body", z=-0.3 * t)
    c.flap(70 * t)
    c.move("bell", z=-0.2 * t, x=0.3 * t)


CANTOR_CLIPS = [
    ("idle", 12, 4, True, cantor_idle),
    ("move", 12, 4, True, cantor_move),
    ("ring", 12, 6, False, cantor_ring),
    ("recover", 12, 3, False, cantor_recover),
    ("hurt", 12, 2, False, cantor_hurt),
    ("death", 12, 4, False, cantor_death),
]


# ================================================================ Warden (0.7 x 1.6, a heron with a lance)

WARDEN = (0.16, 0.22, 0.42)
WARDEN_PALE = (0.62, 0.66, 0.74)


class Warden(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        hips = self.add("hips", root, (0, 0, -0.05))
        body = self.add("body", hips, (0, 0, 0.25))
        neck = self.add("neck", body, (0.08, 0, 0.30))
        head = self.add("head", neck, (0.06, 0, 0.30))
        lance = self.add("lance", body, (0.20, -0.16, 0.05), rot=(0, D(80), 0))
        leg_l = self.add("leg_l", hips, (0.0, -0.08, 0))
        leg_r = self.add("leg_r", hips, (0.0, 0.08, 0))
        wing = self.add("wing", body, (-0.05, -0.2, 0.1))
        blue, pale, brass, ink = mat("blue", WARDEN), mat("pale", WARDEN_PALE), mat("brass", BRASS), mat("ink", INK)
        sphere("torso", 0.24, blue, body, loc=(0, 0, 0.05), scale=(1.0, 0.75, 1.3))
        sphere("wing_m", 0.18, blue, wing, loc=(-0.06, -0.02, -0.02), scale=(1.2, 0.3, 1.0), rot=(0, D(-30), 0))
        cone("neck_m", 0.06, 0.045, 0.42, pale, neck, loc=(0.03, 0, 0.15), rot=(0, D(12), 0))
        cone("gorget", 0.13, 0.11, 0.08, brass, neck, loc=(0, 0, 0.0))
        sphere("head_m", 0.11, pale, head, scale=(1.3, 0.9, 0.9))
        cone("bill", 0.035, 0.003, 0.34, ink, head, loc=(0.26, 0, -0.02), rot=(0, D(90), 0))
        sphere("eye", 0.025, ink, head, loc=(0.06, -0.09, 0.03))
        cone("crest", 0.03, 0.003, 0.18, ink, head, loc=(-0.12, 0, 0.06), rot=(0, D(-70), 0))
        cone("lance_m", 0.016, 0.012, 1.3, ink, lance, loc=(0, 0, 0.35))
        cone("lance_tip", 0.02, 0.0, 0.14, brass, lance, loc=(0, 0, 1.05))
        cone("sight", 0.06, 0.06, 0.02, brass, lance, loc=(0, -0.04, 0.55), rot=(D(90), 0, 0))
        for leg in (leg_l, leg_r):
            cone("thigh" + leg.name[-2:], 0.03, 0.025, 0.36, pale, leg, loc=(0.02, 0, -0.18), rot=(0, D(6), 0))
            knee = self.add("knee" + leg.name[-2:], leg, (0.04, 0, -0.36))
            cone("shin" + leg.name[-2:], 0.024, 0.018, 0.4, pale, knee, loc=(-0.02, 0, -0.2), rot=(0, D(-6), 0))
            cube("foot" + leg.name[-2:], (0.18, 0.05, 0.02), ink, knee, loc=(0.04, 0, -0.4))
        self.snapshot()

    def legs(self, l, r):
        self.rot("leg_l", y=l)
        self.rot("leg_r", y=r)
        self.rot("knee_l", y=-l * 0.6)
        self.rot("knee_r", y=-r * 0.6)


def warden_idle(w, i, n):
    b = math.sin(2 * math.pi * i / n)
    w.move("body", z=0.015 * b)
    w.rot("head", y=3 * b)
    w.rot("lance", y=2 * b)


def warden_move(w, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.legs(30 * s, -30 * s)
    w.move("body", z=0.03 * abs(s))
    w.rot("neck", y=6 * s)
    w.rot("head", y=-6 * s)
    w.rot("lance", y=4 * s)


def warden_measure(w, i, n):
    k = min(1.0, i / 2)
    w.rot("lance", y=-70 * k)            # the sight to the eye
    w.move("lance", x=-0.1 * k, z=0.35 * k)
    w.rot("neck", y=-12 * k)
    w.rot("head", y=10 * k)
    w.rot("body", y=-4 * k)


def warden_telegraph(w, i, n):
    k = (0.4, 0.8, 1.0)[i]
    w.rot("body", y=-14 * k)
    w.move("lance", x=-0.25 * k)
    w.rot("lance", y=-8 * k)
    w.rot("neck", y=-15 * k)
    w.rot("head", y=12 * k)
    w.legs(-8 * k, 10 * k)


def warden_thrust(w, i, n):
    k = (0.6, 1.0, 0.9)[i]
    w.rot("body", y=22 * k)
    w.move("lance", x=0.45 * k, z=-0.1 * k)
    w.rot("lance", y=6 * k)
    w.rot("neck", y=20 * k)
    w.rot("head", y=-4 * k)
    w.legs(35 * k, -25 * k)
    w.rot("wing", y=-30 * k)


def warden_recover(w, i, n):
    k = 1 - (i + 1) / (n + 1)
    w.rot("body", y=22 * k)
    w.move("lance", x=0.45 * k, z=-0.1 * k)
    w.rot("neck", y=20 * k)
    w.legs(35 * k, -25 * k)


def warden_hurt(w, i, n):
    k = (1.0, 0.5)[i]
    w.rot("body", y=-18 * k)
    w.rot("neck", y=-20 * k)
    w.rot("wing", x=40 * k)
    w.rot("lance", y=25 * k)


def warden_death(w, i, n):
    t = min(1.0, i / 4)
    w.rot("hips", y=-70 * t)
    w.move("hips", z=-0.55 * t, x=-0.2 * t)
    w.rot("neck", y=-40 * t)
    w.rot("head", y=30 * t)
    w.rot("lance", y=60 * t)
    w.move("lance", z=-0.3 * t)
    w.legs(30 * t, 50 * t)
    w.rot("wing", x=50 * t)


WARDEN_CLIPS = [
    ("idle", 12, 4, True, warden_idle),
    ("move", 12, 8, True, warden_move),
    ("measure", 12, 4, False, warden_measure),
    ("telegraph", 12, 3, False, warden_telegraph),
    ("thrust", 24, 3, False, warden_thrust),
    ("recover", 12, 3, False, warden_recover),
    ("hurt", 12, 2, False, warden_hurt),
    ("death", 12, 6, False, warden_death),
]


# ================================================================ Lost Remnant (0.8 x 1.2, a townsfolk oval with the ink gone)

REMNANT = lerp((0.66, 0.66, 0.64), PAPER, 0.5)
REMNANT_DARK = lerp((0.50, 0.50, 0.50), PAPER, 0.45)


class LostRemnant(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, -0.05))
        head = self.add("head", body, (0.06, 0, 0.44))
        wn = self.add("wing_near", body, (0, -0.24, 0.12))
        wf = self.add("wing_far", body, (0, 0.24, 0.12))
        grey, dark, ink = mat("grey", REMNANT), mat("grey_dark", REMNANT_DARK), mat("ink_grey", GREY_INK)
        sphere("oval", 0.32, grey, body, scale=(0.85, 0.75, 1.25))
        sphere("head_m", 0.16, grey, head)
        cone("beak", 0.04, 0.004, 0.12, dark, head, loc=(0.17, 0, -0.03), rot=(0, D(90), 0))
        sphere("eye", 0.025, ink, head, loc=(0.06, -0.14, 0.03))
        sphere("wing_near_m", 0.14, dark, wn, loc=(-0.02, -0.03, -0.14), scale=(0.9, 0.3, 1.5))
        sphere("wing_far_m", 0.14, dark, wf, loc=(-0.02, 0.03, -0.14), scale=(0.9, 0.3, 1.5))
        cone("feet", 0.06, 0.04, 0.16, dark, body, loc=(0, 0, -0.45))
        self.snapshot()


def remnant_idle(r, i, n):
    t = i / n
    r.move("body", z=0.04 * math.sin(2 * math.pi * t))
    r.rot("head", y=5 * math.sin(2 * math.pi * t + 1))
    r.rot("wing_near", x=8 * math.sin(2 * math.pi * t))
    r.rot("wing_far", x=-8 * math.sin(2 * math.pi * t))


def remnant_move(r, i, n):
    t = i / n
    r.rot("body", y=10)
    r.move("body", z=0.05 * math.sin(2 * math.pi * t))
    r.rot("wing_near", x=25 * math.sin(2 * math.pi * t))
    r.rot("wing_far", x=-25 * math.sin(2 * math.pi * t))
    r.rot("head", y=-6)


def remnant_hurt(r, i, n):
    k = (1.0, 0.5)[i]
    r.rot("body", y=-20 * k)
    r.rot("wing_near", x=45 * k)
    r.rot("wing_far", x=-45 * k)


def remnant_death(r, i, n):
    t = min(1.0, i / 3)
    r.scale("body", 1 + 0.2 * t, 1, 1 - 0.5 * t)
    r.move("body", z=-0.25 * t)
    r.rot("head", y=40 * t)


REMNANT_CLIPS = [
    ("idle", 12, 4, True, remnant_idle),
    ("move", 12, 4, True, remnant_move),
    ("hurt", 12, 2, False, remnant_hurt),
    ("death", 12, 4, False, remnant_death),
]


# ================================================================ The Lamp-Keeper (1.6 x 1.2; a Remnant gannet fused to the lamp)

KEEPER = lerp((0.62, 0.62, 0.60), PAPER, 0.35)
KEEPER_DARK = lerp((0.40, 0.40, 0.42), PAPER, 0.3)
LAMP = (0.96, 0.84, 0.50)


class LampKeeper(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.05))
        neck = self.add("neck", body, (0.30, 0, 0.22))
        head = self.add("head", neck, (0.14, 0, 0.34))
        sh_near = self.add("shutter_near", body, (-0.05, -0.30, 0.18))
        sh_far = self.add("shutter_far", body, (-0.05, 0.30, 0.18))
        lamp = self.add("lamp", body, (0.0, 0, -0.30))
        grey, dark, brass, glow, ink = mat("grey", KEEPER), mat("grey_dark", KEEPER_DARK), mat("brass", BRASS), mat("glow", LAMP), mat("ink_grey", GREY_INK)
        sphere("torso", 0.42, grey, body, scale=(1.5, 0.8, 0.9))
        cone("neck_m", 0.11, 0.08, 0.5, grey, neck, loc=(0.05, 0, 0.16), rot=(0, D(18), 0))
        sphere("head_m", 0.16, grey, head, scale=(1.4, 0.9, 0.9))
        cone("bill", 0.06, 0.004, 0.44, dark, head, loc=(0.34, 0, -0.03), rot=(0, D(90), 0))
        sphere("eye", 0.035, ink, head, loc=(0.1, -0.13, 0.04))
        for sh, side in ((sh_near, -1), (sh_far, 1)):
            # a shutter: a slatted panel hinged at the shoulder, folded flat along the body at rest
            for s in range(4):
                cube("%s_slat%d" % (sh.name, s), (0.9, 0.05, 0.16), dark if s % 2 else grey, sh,
                     loc=(-0.45, side * (0.05 + 0.06 * s), -0.02 - 0.05 * s))
            cube("%s_rib" % sh.name, (0.95, 0.03, 0.04), brass, sh, loc=(-0.45, side * 0.02, 0.08))
        cone("lamp_ring", 0.34, 0.30, 0.10, brass, lamp)
        cone("lamp_glass", 0.24, 0.20, 0.16, glow, lamp, loc=(0, 0, -0.1))
        cone("lamp_gear", 0.12, 0.12, 0.06, brass, lamp, loc=(0.2, -0.26, 0.04), rot=(D(90), 0, 0))
        cone("tail_m", 0.10, 0.01, 0.5, grey, body, loc=(-0.72, 0, 0.02), rot=(0, D(-95), 0))
        self.snapshot()

    def shutters(self, a):
        """Open both shutters by a degrees (the wings)."""
        self.rot("shutter_near", x=a)
        self.rot("shutter_far", x=-a)


def keeper_idle(k, i, n):
    t = i / n
    k.move("body", z=0.02 * math.sin(2 * math.pi * t))
    k.rot("head", z=25 * math.sin(2 * math.pi * t))
    k.shutters(6 + 4 * math.sin(2 * math.pi * t))


def keeper_telegraph(k, i, n):
    q = (i + 1) / n
    k.rot("neck", y=-30 * q)
    k.rot("head", y=-10 * q)
    k.shutters(45 * q)
    k.move("body", z=0.08 * q)


def keeper_beam(k, i, n):
    t = i / n
    k.shutters(95 + 6 * math.sin(2 * math.pi * t))
    k.rot("neck", y=-20)
    k.move("body", z=0.1)
    k.scale("lamp", 1.15, 1.15, 1.15)


def keeper_dive(k, i, n):
    q = (0.5, 0.8, 1.0)[i]
    k.rot("body", y=55 * q)
    k.rot("neck", y=25 * q)
    k.shutters(-15 * q)
    k.scale("body", 1.1, 1, 0.9)


def keeper_grounded(k, i, n):
    t = i / n
    k.move("body", z=-0.25)
    k.rot("neck", y=45)
    k.rot("head", y=20)
    k.shutters(80 + 5 * math.sin(2 * math.pi * t))
    k.rot("shutter_near", y=-25)
    k.rot("shutter_far", y=-25)
    k.scale("body", 1.15, 1, 0.8)


def keeper_return(k, i, n):
    t = i / n
    k.rot("body", y=-15)
    k.shutters(60 + 30 * math.sin(2 * math.pi * t))
    k.rot("neck", y=-10)


def keeper_hurt(k, i, n):
    q = (1.0, 0.5)[i]
    k.rot("body", y=-15 * q)
    k.rot("neck", y=-30 * q)
    k.shutters(100 * q)


def keeper_death(k, i, n):
    t = min(1.0, i / 6)
    k.rot("body", y=40 * t)
    k.move("body", z=-0.35 * t)
    k.rot("neck", y=60 * t)
    k.rot("head", y=30 * t)
    k.shutters(20 * (1 - t))
    k.scale("lamp", 1 - 0.3 * t, 1 - 0.3 * t, 1 - 0.3 * t)


KEEPER_CLIPS = [
    ("idle", 12, 6, True, keeper_idle),
    ("telegraph", 12, 4, False, keeper_telegraph),
    ("beam", 12, 4, True, keeper_beam),
    ("dive", 24, 3, False, keeper_dive),
    ("grounded", 12, 6, True, keeper_grounded),
    ("return", 12, 3, True, keeper_return),
    ("hurt", 12, 2, False, keeper_hurt),
    ("death", 12, 8, False, keeper_death),
]


# ================================================================ all of them

# name, cell (units), builder, clips, line thickness at 2x, ink colour
CREATURES = [
    ("MarshCrab", 1.6, MarshCrab, CRAB_CLIPS, 3.0, INK),
    ("ReedSkimmer", 1.6, ReedSkimmer, skimmer_clips(), 2.6, INK),
    ("Smudge", 2.0, Smudge, SMUDGE_CLIPS, 3.0, INK),
    ("Cantor", 2.4, Cantor, CANTOR_CLIPS, 3.0, INK),
    ("Warden", 2.8, Warden, WARDEN_CLIPS, 3.0, INK),
    ("LostRemnant", 2.0, LostRemnant, REMNANT_CLIPS, 2.4, GREY_INK),
    ("LampKeeper", 3.6, LampKeeper, KEEPER_CLIPS, 3.0, GREY_INK),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in CREATURES:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink)
        if name == "ReedSkimmer":
            # the rise: the same bird flaring yellow (the telegraph is a colour, combat doc 7)
            import inklib
            frames = os.path.join(inklib.FRAMES_ROOT, "reedskimmer")
            inklib.reset_scene()
            rig = ReedSkimmer(flare=True)
            inklib.setup_render(int(cell * inklib.PPU * inklib.SCALE), cell, 0.0, line=line, ink=ink)
            for i in range(3):
                rig.reset()
                skim_rise(rig, i, 3)
                inklib.render(os.path.join(frames, "rise_%02d.png" % i))
            import json
            with open(os.path.join(frames, "clips.json")) as f:
                meta = json.load(f)
            meta["clips"].append({"name": "rise", "fps": 12, "frames": 3, "loop": False})
            with open(os.path.join(frames, "clips.json"), "w") as f:
                json.dump(meta, f, indent=2)
            print("[reedskimmer] rise: 3 frames at 12 fps (flared)")


if __name__ == "__main__":
    main()

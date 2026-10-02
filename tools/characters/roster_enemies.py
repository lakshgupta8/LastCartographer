"""The roster's later families, built and animated in Blender (CMB-09, docs/design/enemy-animation.md §2d): the
lantern-moth cloud of the Verdance (a swarm: Blot it), the pulp-wasp of Halden's mills (a spitter in a line:
Longstroke reaches it) and the Sketch of the Greyfold's road (drawn only inside her lantern-radius: hit it there).

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/roster_enemies.py
    ... -- Mothcloud                   (only that creature)
    python tools/characters/pack.py mothcloud pulpwasp sketch

Each creature is centred on its collider's centre (the enemy's transform), facing +X; Enemy.Face flips the quad.
Silhouettes (art-direction 4): the fauna are rounds (the wasp a round with a stripe, the cloud a ring of small
rounds), the Sketch is a townsfolk oval drawn thin, as if the hand stopped. Clip names are what EnemyAnimator asks
for through Enemy.Clip.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER

GREY_INK = lerp(INK, PAPER, 0.45)   # the Sketch's line: a hand that stopped pressing

# The forest's palette (art-direction 5): moss, lantern amber, cream.
MOTH = (0.86, 0.80, 0.62)
MOTH_WING = (0.92, 0.88, 0.74)
MOTH_SPOT = (0.95, 0.66, 0.28)
MOTH_DARK = (0.46, 0.38, 0.30)
# The plateau's (copper gone green, paper, ink): the wasp in paper-buff and ink-brown bands.
WASP = (0.80, 0.68, 0.46)
WASP_BAND = (0.30, 0.22, 0.16)
WASP_WING = (0.82, 0.84, 0.80)
PULP = (0.90, 0.86, 0.74)
# The Greyfold's: paper, with the thinnest grey.
SKETCH = (0.90, 0.88, 0.83)
SKETCH_SHADE = (0.76, 0.75, 0.72)


# ================================================================ Lantern-moth cloud (1.4 x 1.2, Blot it)

class Mothcloud(Rig):
    """Five moths on a ring round the cloud's centre, each on its own empty so the cloud can breathe, scatter and
    gather. A moth: a furred body, two wings as slabs that flap about the body's axis, an amber eye-spot on each."""

    WING = [(0.0, 0.0), (0.05, 0.10), (0.12, 0.13), (0.17, 0.08), (0.15, 0.0), (0.12, -0.08), (0.06, -0.10), (0.0, -0.04)]
    RING = [(0.00, 0.42), (0.40, 0.14), (0.26, -0.36), (-0.26, -0.36), (-0.40, 0.14)]

    def __init__(self):
        super().__init__()
        root = self.add("root")
        cloud = self.add("cloud", root)
        fur, wing, spot, dark = mat("moth", MOTH), mat("wing", MOTH_WING), mat("spot", MOTH_SPOT), mat("dark", MOTH_DARK)
        for k, (x, z) in enumerate(self.RING):
            seat = self.add("seat%d" % k, cloud, (x, 0, z))
            moth = self.add("moth%d" % k, seat, rot=(0, D(-20 + 15 * (k % 3)), 0))
            sphere("body%d" % k, 0.045, fur, moth, scale=(1.9, 1.0, 1.0))
            sphere("head%d" % k, 0.03, dark, moth, loc=(0.085, 0, 0.01))
            for side in (-1, 1):
                cone("ant%d_%d" % (k, side), 0.004, 0.001, 0.09, dark, moth, loc=(0.10, side * 0.015, 0.045), rot=(D(side * 20), D(35), 0))
                w = self.add("wing%d_%d" % (k, side), moth, (0.0, side * 0.03, 0.01), rot=(D(side * 25), 0, 0))
                slab("wing%d_%d_m" % (k, side), self.WING, 0.004, wing, w, rot=(D(90), 0, 0))
                sphere("spot%d_%d" % (k, side), 0.022, spot, w, loc=(0.11, side * 0.004, 0.02), scale=(1, 0.3, 1))
        self.snapshot()

    def flap(self, k, a):
        for side in (-1, 1):
            self.rot("wing%d_%d" % (k, side), x=side * a)

    def breathe(self, t, amount=0.06):
        for k, (x, z) in enumerate(self.RING):
            ph = 2 * math.pi * t + k * 1.3
            self.move("seat%d" % k, x=amount * math.sin(ph), z=amount * math.cos(ph * 0.7))

    def gather(self, k):
        """The cloud pulled toward its centre by k (0 loose, 1 tight)."""
        for i, (x, z) in enumerate(self.RING):
            self.move("seat%d" % i, x=-x * 0.65 * k, z=-z * 0.65 * k)


def cloud_idle(c, i, n):
    t = i / n
    c.breathe(t)
    for k in range(5):
        c.flap(k, 40 * math.sin(2 * math.pi * t * 2 + k))


def cloud_move(c, i, n):
    t = i / n
    c.breathe(t, 0.04)
    c.rot("cloud", y=8)
    for k in range(5):
        c.flap(k, 50 * math.sin(2 * math.pi * t * 2 + k))
        c.rot("moth%d" % k, y=10)


def cloud_flare(c, i, n):
    k = (0.3, 0.7, 1.0)[i]
    c.breathe(0.0, 0.0)
    for m in range(5):
        c.flap(m, -70 * k)                      # wings thrown wide: the eye-spots shown
        c.scale("moth%d" % m, 1 + 0.25 * k, 1, 1 + 0.25 * k)
        x, z = Mothcloud.RING[m]
        c.move("seat%d" % m, x=x * 0.25 * k, z=z * 0.25 * k)


def cloud_dart(c, i, n):
    for m in range(5):
        c.flap(m, (60, 20)[i])
        c.rot("moth%d" % m, y=25)
        c.scale("moth%d" % m, 1.3, 1, 0.8)
        x, z = Mothcloud.RING[m]
        c.move("seat%d" % m, x=0.12 - x * 0.3, z=-z * 0.3)


def cloud_gather(c, i, n):
    t = i / n
    c.gather(0.9)
    for k in range(5):
        c.flap(k, 25 * math.sin(2 * math.pi * t + k))
        c.rot("moth%d" % k, y=-15 + 8 * math.sin(2 * math.pi * t + k))


def cloud_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    for m in range(5):
        x, z = Mothcloud.RING[m]
        c.move("seat%d" % m, x=x * 0.5 * k, z=z * 0.5 * k)
        c.flap(m, 65 * k)
        c.rot("moth%d" % m, z=30 * k * (1 if m % 2 else -1))


def cloud_death(c, i, n):
    t = min(1.0, i / 3)
    for m in range(5):
        x, z = Mothcloud.RING[m]
        c.move("seat%d" % m, x=x * 0.3 * t, z=-0.45 * t - abs(x) * 0.2 * t)
        c.rot("moth%d" % m, y=80 * t * (1 if m % 2 else -1))
        c.flap(m, 10)
        c.scale("moth%d" % m, 1 - 0.5 * t, 1, 1 - 0.5 * t)


CLOUD_CLIPS = [
    ("idle", 12, 6, True, cloud_idle),
    ("move", 12, 6, True, cloud_move),
    ("flare", 12, 3, False, cloud_flare),
    ("dart", 24, 2, True, cloud_dart),
    ("gather", 12, 4, True, cloud_gather),
    ("hurt", 12, 2, False, cloud_hurt),
    ("death", 12, 4, False, cloud_death),
]


# ================================================================ Pulp-wasp (0.9 x 0.7, Longstroke reaches it)

class Pulpwasp(Rig):
    """A round in paper-buff with ink-brown bands, hanging legs, two wings that blur; under the jaw the pulp sac it
    chews, which swells for the spit."""

    WING = [(0.0, 0.0), (0.10, 0.16), (0.22, 0.26), (0.34, 0.24), (0.36, 0.14), (0.26, 0.04), (0.12, -0.02)]

    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        head = self.add("head", body, (0.22, 0, 0.04))
        sac = self.add("sac", head, (0.08, 0, -0.10))
        buff, band, wing, pulp, ink = mat("buff", WASP), mat("band", WASP_BAND), mat("wing", WASP_WING), mat("pulp", PULP), mat("ink", INK)
        sphere("thorax", 0.15, buff, body, loc=(0.04, 0, 0), scale=(1.1, 0.9, 0.9))
        abdomen = self.add("abdomen", body, (-0.16, 0, -0.02), rot=(0, D(-8), 0))
        sphere("abdomen_m", 0.16, buff, abdomen, scale=(1.5, 0.85, 0.85))
        for k, x in enumerate((-0.04, -0.14, -0.22)):
            sphere("band%d" % k, 0.158, band, abdomen, loc=(x, 0, 0), scale=(0.28, 0.86, 0.86))
        cone("sting", 0.03, 0.003, 0.14, ink, abdomen, loc=(-0.30, 0, -0.01), rot=(0, D(-90), 0))
        sphere("head_m", 0.10, buff, head, scale=(1.0, 0.95, 0.95))
        for side in (-1, 1):
            sphere("eye%d" % side, 0.04, ink, head, loc=(0.05, side * 0.07, 0.03), scale=(1, 0.8, 1.2))
            cone("ant%d" % side, 0.006, 0.002, 0.16, ink, head, loc=(0.06, side * 0.03, 0.10), rot=(D(side * 25), D(40), 0))
            w = self.add("wing%d" % side, body, (0.0, side * 0.08, 0.12), rot=(D(side * 30), 0, 0))
            slab("wing%d_m" % side, self.WING, 0.004, wing, w, rot=(D(90), 0, 0))
            for k, x in enumerate((0.12, 0.02, -0.08)):
                leg = self.add("leg%d_%d" % (k, side), body, (x, side * 0.10, -0.08))
                cone("leg%d_%d_m" % (k, side), 0.012, 0.004, 0.22, band, leg, loc=(0, side * 0.03, -0.10), rot=(D(-side * 25), D(-20 + 15 * k), 0))
        sphere("sac_m", 0.045, pulp, sac)
        self.snapshot()

    def wings(self, a):
        for side in (-1, 1):
            self.rot("wing%d" % side, x=side * a)


def wasp_idle(w, i, n):
    t = i / n
    w.wings(45 * math.sin(2 * math.pi * t * 2))
    w.move("body", z=0.03 * math.sin(2 * math.pi * t))
    w.rot("abdomen", y=-4 * math.sin(2 * math.pi * t))


def wasp_move(w, i, n):
    t = i / n
    w.wings(55 * math.sin(2 * math.pi * t * 2))
    w.rot("body", y=14)
    w.move("body", z=0.02 * math.sin(2 * math.pi * t))
    for side in (-1, 1):
        for k in range(3):
            w.rot("leg%d_%d" % (k, side), y=-25)


def wasp_spit(w, i, n):
    k = (0.5, 1.0, 0.0)[i]
    w.wings((30, 30, 60)[i])
    w.rot("head", y=-30 * k + (35 if i == 2 else 0))       # back, back, then forward: the pellet leaves
    w.scale("sac", 1 + 1.6 * k, 1 + 1.6 * k, 1 + 1.6 * k)
    w.rot("abdomen", y=18 * k)
    w.move("body", x=-0.04 * k + (0.08 if i == 2 else 0))


def wasp_hurt(w, i, n):
    k = (1.0, 0.5)[i]
    w.rot("body", y=-25 * k, z=15 * k)
    w.wings(70 * k)
    w.scale("body", 0.9, 1, 1.12)


def wasp_death(w, i, n):
    t = min(1.0, i / 3)
    w.rot("body", y=-110 * t)
    w.move("body", z=-0.25 * t)
    w.wings(15)
    for side in (-1, 1):
        for k in range(3):
            w.rot("leg%d_%d" % (k, side), y=50 * t)
    w.scale("sac", 1 - 0.6 * t, 1 - 0.6 * t, 1 - 0.6 * t)


WASP_CLIPS = [
    ("idle", 12, 4, True, wasp_idle),
    ("move", 12, 4, True, wasp_move),
    ("spit", 12, 3, False, wasp_spit),
    ("hurt", 12, 2, False, wasp_hurt),
    ("death", 12, 4, False, wasp_death),
]


# ================================================================ Sketch (1.0 x 1.2, hit it inside the lantern-radius)

class Sketch(Rig):
    """A bird nobody finished: a townsfolk oval with the fill left out, a long neck carried low, legs too long, a
    crest of loose strokes. Built standing, feet at z = -0.6 (its collider's centre is the drawing's)."""

    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.0))
        neck = self.add("neck", body, (0.16, 0, 0.12))
        head = self.add("head", neck, (0.30, 0, 0.14))
        crest = self.add("crest", head, (-0.04, 0, 0.08))
        paper, shade, ink = mat("sketch", SKETCH), mat("shade", SKETCH_SHADE), mat("ink", INK)
        sphere("torso", 0.26, paper, body, scale=(1.25, 0.8, 1.0))
        sphere("shade_m", 0.22, shade, body, loc=(-0.06, 0, -0.08), scale=(1.1, 0.7, 0.6))
        cone("tail", 0.07, 0.01, 0.34, paper, body, loc=(-0.36, 0, 0.02), rot=(0, D(-100), 0))
        cone("neck_m", 0.07, 0.05, 0.40, paper, neck, loc=(0.15, 0, 0.07), rot=(0, D(62), 0))
        sphere("head_m", 0.09, paper, head, scale=(1.25, 0.85, 0.9))
        cone("bill", 0.03, 0.004, 0.18, ink, head, loc=(0.16, 0, -0.01), rot=(0, D(90), 0))
        sphere("eye", 0.02, ink, head, loc=(0.05, -0.07, 0.03))
        for k in range(4):
            cone("crest%d" % k, 0.008, 0.001, 0.16 + 0.03 * k, ink, crest, loc=(-0.02 * k, 0, 0.02), rot=(0, D(-30 - 22 * k), 0))
        for side, name in ((-1, "leg_near"), (1, "leg_far")):
            hip = self.add(name, body, (-0.02, side * 0.08, -0.16))
            cone(name + "_thigh", 0.035, 0.022, 0.30, shade, hip, loc=(0.0, 0, -0.14), rot=(0, D(-10), 0))
            shin = self.add(name + "_shin", hip, (0.02, 0, -0.28))
            cone(name + "_shin_m", 0.02, 0.012, 0.30, ink, shin, loc=(0.0, 0, -0.15), rot=(0, D(6), 0))
            cone(name + "_foot", 0.02, 0.003, 0.16, ink, shin, loc=(0.06, 0, -0.30), rot=(0, D(90), 0))
        self.snapshot()

    def legs(self, t, amp=35.0):
        for name, ph in (("leg_near", 0.0), ("leg_far", math.pi)):
            a = amp * math.sin(2 * math.pi * t + ph)
            self.rot(name, y=a)
            self.rot(name + "_shin", y=max(0.0, -a * 1.2))

    def low(self, k):
        """The neck carried low by k (its rest), raised toward 0."""
        self.rot("neck", y=-45 * k)
        self.rot("head", y=50 * k)


def sketch_idle(s, i, n):
    t = i / n
    b = math.sin(2 * math.pi * t)
    s.low(1.0)
    s.move("body", z=0.01 * b)
    s.rot("head", y=4 * b)
    s.rot("crest", y=6 * b)


def sketch_move(s, i, n):
    t = i / n
    s.low(1.0)
    s.legs(t)
    s.rot("body", y=10)
    s.move("body", z=0.03 * abs(math.sin(2 * math.pi * t)))
    s.rot("neck", y=4 * math.sin(4 * math.pi * t))


def sketch_fill(s, i, n):
    k = (0.35, 0.7, 1.0)[i]
    s.low(1.0 - k)                      # the neck comes up: the bird the hand meant
    s.rot("crest", y=-40 * k)
    s.scale("crest", 1 + 0.4 * k, 1, 1 + 0.4 * k)   # the loose strokes stand
    s.rot("body", y=-8 * k)
    s.move("body", z=0.06 * k)


def sketch_lunge(s, i, n):
    s.low(1.0)
    s.rot("body", y=(28, 36)[i])
    s.rot("neck", y=(40, 48)[i])
    s.move("body", x=0.1, z=(-0.08, -0.12)[i])
    s.rot("leg_near", y=(50, -40)[i])
    s.rot("leg_far", y=(-40, 50)[i])
    s.rot("crest", y=35)


def sketch_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.low(1.0)
    s.rot("body", y=-22 * k)
    s.rot("neck", y=30 * k)
    s.rot("crest", y=30 * k)
    s.scale("body", 0.92, 1, 1.08)


def sketch_death(s, i, n):
    t = min(1.0, i / 3)
    s.low(1.0)
    s.rot("body", y=-60 * t)
    s.move("body", z=-0.42 * t)
    s.rot("leg_near", y=-60 * t)
    s.rot("leg_far", y=60 * t)
    s.rot("neck", y=-40 * t)
    s.scale("crest", 1 - 0.7 * t, 1, 1 - 0.7 * t)


SKETCH_CLIPS = [
    ("idle", 12, 4, True, sketch_idle),
    ("move", 12, 6, True, sketch_move),
    ("fill", 12, 3, False, sketch_fill),
    ("lunge", 24, 2, True, sketch_lunge),
    ("hurt", 12, 2, False, sketch_hurt),
    ("death", 12, 4, False, sketch_death),
]


# ================================================================ Tussock (1.0 x 0.9, pogo it: shelled, and under the grass)

# The steppe's palette: dry grass, turf, a shell the colour of a stone.
TUSSOCK_GRASS = (0.72, 0.64, 0.38)
TUSSOCK_GRASS_DARK = (0.52, 0.46, 0.26)
TUSSOCK_SHELL = (0.50, 0.46, 0.40)
TUSSOCK_TURF = (0.46, 0.38, 0.26)


class Tussock(Rig):
    """A shelled burrower that passes for a clump of the long grass: a domed shell under a fringe of blades, two
    eyes under the fringe, stub claws. Built sitting, feet at z = -0.45 (its collider's centre is the drawing's).
    The shell and the mound are props: a clip shows the shell above ground, the mound of turf it travels under,
    or both as it heaves up or sinks."""

    def __init__(self):
        super().__init__()
        root = self.add("root")
        shell = self.add("shell", root)
        mound = self.add("mound", root, (0, 0, -0.40))
        grass, dark, shell_m, turf, ink = mat("grass", TUSSOCK_GRASS), mat("grass_dark", TUSSOCK_GRASS_DARK), mat("shell", TUSSOCK_SHELL), mat("turf", TUSSOCK_TURF), mat("ink", INK)
        sphere("dome", 0.34, shell_m, shell, loc=(0, 0, -0.12), scale=(1.2, 1.0, 0.75))
        sphere("rim", 0.35, turf, shell, loc=(0, 0, -0.30), scale=(1.25, 1.05, 0.25))
        blades = self.add("blades", shell, (0, 0, 0.08))
        for k in range(12):
            a = -150 + 25 * k
            r = 0.26 if k % 2 else 0.18
            x, y = r * math.cos(math.radians(a)), r * math.sin(math.radians(a)) * 0.6
            b = self.add("blade%d" % k, blades, (x, y, 0.0))
            cone("blade%d_m" % k, 0.022, 0.002, 0.34 + 0.06 * (k % 3), grass if k % 3 else dark, b, rot=(D(-y * 60), D(x * 70), 0))
        eyes = self.add("eyes", shell, (0.28, 0, -0.16))
        for side in (-1, 1):
            sphere("eye%d" % side, 0.028, ink, eyes, loc=(0, side * 0.10, 0))
        for side in (-1, 1):
            cone("claw%d" % side, 0.03, 0.004, 0.16, ink, shell, loc=(0.30, side * 0.16, -0.38), rot=(0, D(80), 0))
        sphere("mound_m", 0.40, turf, mound, scale=(1.5, 1.0, 0.32))
        for k in range(5):
            x = -0.3 + 0.15 * k
            cone("mound_blade%d" % k, 0.018, 0.002, 0.22, grass, mound, loc=(x, 0, 0.06), rot=(0, D(-35 + 18 * k), 0))
        soil = self.add("soil", root)
        for k in range(6):
            sphere("soil%d" % k, 0.03, turf, soil, loc=(-0.3 + 0.12 * k, 0, 0.1 + 0.1 * (k % 3)))
        self.prop("mound", mound)
        self.prop("shell", shell)
        self.prop("soil", soil)
        self.snapshot()

    def reset(self):
        """At rest it sits on the surface (the turnaround): the shell shown, the mound and the soil not."""
        super().reset()
        self.show("shell")

    def sway(self, t, amp=8.0):
        for k in range(12):
            self.rot("blade%d" % k, y=amp * math.sin(2 * math.pi * t + k * 0.7))


def tus_idle(s, i, n):
    t = i / n
    s.show("shell")
    s.sway(t)
    s.move("shell", z=0.008 * math.sin(2 * math.pi * t))
    s.scale("eyes", 1, 1, 0.3 + 0.7 * abs(math.cos(math.pi * t)))


def tus_ridge(s, i, n):
    t = i / n
    s.show("shell", False)
    s.show("mound")
    s.move("mound", x=0.04 * math.sin(2 * math.pi * t), z=0.03 * abs(math.sin(2 * math.pi * t)))
    s.scale("mound", 1 + 0.08 * math.sin(2 * math.pi * t), 1, 1 + 0.15 * abs(math.sin(2 * math.pi * t)))


def tus_heave(s, i, n):
    k = (0.3, 0.65, 1.0)[i]
    s.show("mound"); s.show("shell"); s.show("soil")
    s.scale("mound", 1 + 0.3 * k, 1, 1 + 1.2 * k)
    s.move("mound", z=0.08 * k)
    s.move("shell", z=-0.55 + 0.35 * k)
    s.scale("shell", 1, 1, 0.5 + 0.4 * k)
    s.scale("soil", 0.3 + 0.7 * k, 1, 0.3 + 0.7 * k)
    s.sway(k * 0.5, 25)


def tus_breach(s, i, n):
    s.show("shell"); s.show("soil")
    s.move("shell", z=(0.22, 0.12)[i])
    s.scale("shell", 0.95, 1, 1.15)
    s.move("soil", z=(0.25, 0.45)[i])
    s.scale("soil", (1.2, 1.6)[i], 1, (1.2, 1.6)[i])
    s.sway(0.0, 35)
    s.scale("eyes", 1.3, 1, 1.3)


def tus_burrow(s, i, n):
    k = (0.3, 0.65, 1.0)[i]
    s.show("mound"); s.show("shell")
    s.move("shell", z=-0.5 * k)
    s.scale("shell", 1, 1, 1 - 0.55 * k)
    s.scale("mound", 0.6 + 0.4 * k, 1, 0.4 + 0.6 * k)
    s.sway(0.0, -30 * k)


def tus_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.show("shell")
    s.rot("shell", y=-18 * k)
    s.move("shell", z=0.06 * k)
    s.sway(0.0, 40 * k)


def tus_death(s, i, n):
    t = min(1.0, i / 3)
    s.show("shell")
    s.rot("shell", y=150 * t)
    s.move("shell", z=0.2 * math.sin(math.pi * t) - 0.1 * t)
    s.sway(0.0, -45 * t)
    s.scale("eyes", 1 - 0.8 * t, 1, 1 - 0.8 * t)


TUSSOCK_CLIPS = [
    ("idle", 12, 4, True, tus_idle),
    ("ridge", 12, 4, True, tus_ridge),
    ("heave", 12, 3, False, tus_heave),
    ("breach", 24, 2, True, tus_breach),
    ("burrow", 12, 3, False, tus_burrow),
    ("hurt", 12, 2, False, tus_hurt),
    ("death", 12, 4, False, tus_death),
]


# ================================================================ Reedling (0.6 x 0.7, Blot the clutch)

REEDLING = (0.90, 0.86, 0.72)
REEDLING_CAP = (0.62, 0.54, 0.40)
REEDLING_BILL = (0.80, 0.62, 0.36)


class Reedling(Rig):
    """A half-drawn marsh chick of the Reedmother's brood (bible 6.2): a ball of fluff on stick legs, a wide bill, the
    wing-nubs of a bird that will not fly. Built standing, feet at z = -0.35 (its collider's centre is the drawing's).
    Drawn in a lighter ink: the hand that drew it did not finish."""

    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, 0.02))
        head = self.add("head", body, (0.12, 0, 0.20))
        fluff, cap, bill, ink = mat("fluff", REEDLING), mat("cap", REEDLING_CAP), mat("bill", REEDLING_BILL), mat("ink", INK)
        sphere("body_m", 0.21, fluff, body, scale=(1.15, 0.95, 1.0))
        sphere("cap_m", 0.19, cap, body, loc=(-0.04, 0, 0.08), scale=(1.0, 0.9, 0.55))
        sphere("head_m", 0.13, fluff, head)
        sphere("crown", 0.11, cap, head, loc=(-0.02, 0, 0.06), scale=(1.0, 0.9, 0.6))
        cone("bill_m", 0.05, 0.004, 0.14, bill, head, loc=(0.13, 0, -0.02), rot=(0, D(90), 0))
        for side in (-1, 1):
            sphere("eye%d" % side, 0.025, ink, head, loc=(0.07, side * 0.08, 0.03))
            w = self.add("wing%d" % side, body, (-0.02, side * 0.18, 0.02))
            sphere("wing%d_m" % side, 0.07, cap, w, scale=(1.3, 0.5, 0.8))
            leg = self.add("leg%d" % side, body, (0.0, side * 0.07, -0.18))
            cone("leg%d_m" % side, 0.016, 0.01, 0.2, ink, leg, loc=(0, 0, -0.1))
            cone("foot%d" % side, 0.014, 0.002, 0.12, ink, leg, loc=(0.05, 0, -0.2), rot=(0, D(90), 0))
        self.snapshot()

    def legs(self, t, amp=35.0):
        self.rot("leg-1", y=amp * math.sin(2 * math.pi * t))
        self.rot("leg1", y=amp * math.sin(2 * math.pi * t + math.pi))


def chick_idle(c, i, n):
    t = i / n
    b = math.sin(2 * math.pi * t)
    c.move("body", z=0.01 * b)
    c.rot("head", y=5 * b)
    for side in (-1, 1):
        c.rot("wing%d" % side, x=side * 6 * b)


def chick_move(c, i, n):
    t = i / n
    c.legs(t)
    c.rot("body", y=12)
    c.move("body", z=0.05 * abs(math.sin(2 * math.pi * t)))
    for side in (-1, 1):
        c.rot("wing%d" % side, x=side * 30 * math.sin(2 * math.pi * t * 2))
    c.rot("head", y=-6 * math.sin(4 * math.pi * t))


def chick_peck(c, i, n):
    k = (0.4, 0.8, 1.0)[i]
    c.rot("body", y=-18 * k)
    c.rot("head", y=-30 * k)
    c.move("body", z=0.03 * k)
    for side in (-1, 1):
        c.rot("wing%d" % side, x=side * 45 * k)


def chick_lunge(c, i, n):
    c.rot("body", y=(30, 38)[i])
    c.rot("head", y=(35, 42)[i])
    c.move("body", x=0.08, z=(-0.04, -0.07)[i])
    c.rot("leg-1", y=(45, -40)[i])
    c.rot("leg1", y=(-40, 45)[i])
    for side in (-1, 1):
        c.rot("wing%d" % side, x=side * -20)


def chick_hurt(c, i, n):
    k = (1.0, 0.5)[i]
    c.rot("body", y=-25 * k)
    c.rot("head", y=20 * k)
    c.scale("body", 0.9, 1, 1.1)
    for side in (-1, 1):
        c.rot("wing%d" % side, x=side * 60 * k)


def chick_death(c, i, n):
    t = min(1.0, i / 3)
    c.rot("body", y=-80 * t)
    c.move("body", z=-0.22 * t)
    c.rot("leg-1", y=-50 * t)
    c.rot("leg1", y=50 * t)
    c.rot("head", y=-30 * t)


REEDLING_CLIPS = [
    ("idle", 12, 4, True, chick_idle),
    ("move", 12, 6, True, chick_move),
    ("peck", 12, 3, False, chick_peck),
    ("lunge", 24, 2, True, chick_lunge),
    ("hurt", 12, 2, False, chick_hurt),
    ("death", 12, 4, False, chick_death),
]


# ================================================================ all of them

# name, cell (units), builder, clips, line thickness at 2x, ink colour
CREATURES = [
    ("Mothcloud", 1.8, Mothcloud, CLOUD_CLIPS, 2.4, INK),
    ("Pulpwasp", 1.6, Pulpwasp, WASP_CLIPS, 2.8, INK),
    ("Sketch", 2.0, Sketch, SKETCH_CLIPS, 2.6, GREY_INK),
    ("Tussock", 1.6, Tussock, TUSSOCK_CLIPS, 2.8, INK),
    ("Reedling", 1.2, Reedling, REEDLING_CLIPS, 2.6, lerp(INK, PAPER, 0.35)),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in CREATURES:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink)


if __name__ == "__main__":
    main()

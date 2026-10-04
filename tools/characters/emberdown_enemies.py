"""The highland's creatures, built and animated in Blender (CMB-09, docs/design/enemy-animation.md §2b): the cave-bat
and the salamander of the mine country, each rendered side-on to frames for pack.py.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/emberdown_enemies.py
    ... -- CaveBat                     (only that creature)
    python tools/characters/pack.py cavebat salamander

Each creature is centred on its collider's centre (the enemy's transform), facing +X; Enemy.Face flips the quad.
Both are rounds (art-direction 4: the fauna are rounds): the bat a hung cloak that opens into a scalloped wing, the
salamander a low black length with embers down its back. Clip names are what EnemyAnimator asks for through
Enemy.Clip.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK

# The highland's palette (art-direction 5): charcoal fur, ash, sulphur, ember.
FUR = (0.30, 0.25, 0.26)
FUR_PALE = (0.56, 0.50, 0.50)
MEMBRANE = (0.42, 0.34, 0.36)
EMBER = (0.96, 0.46, 0.12)
EMBER_HOT = (1.0, 0.78, 0.30)
BASALT = (0.11, 0.10, 0.11)
BELLY = (0.34, 0.30, 0.30)


# ================================================================ Cave-bat (0.8 x 0.6, strike it as it swoops)

class CaveBat(Rig):
    """Built in its flying pose, head to +X, back to +Z; the roost pose hangs it head-down with the wings folded
    along the body, so idle is a closed cloak and the unfurl opens it."""

    WING = [(0.08, 0.0), (0.03, 0.50), (-0.02, 0.40), (-0.09, 0.44), (-0.11, 0.32), (-0.17, 0.34), (-0.19, 0.20), (-0.24, 0.18), (-0.16, 0.06), (-0.07, 0.0)]
    FINGERS = [(0.03, 0.50), (-0.09, 0.44), (-0.17, 0.34), (-0.24, 0.18)]

    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root)
        head = self.add("head", body, (0.17, 0, 0.03))
        wing_far = self.add("wing_far", body, (0.0, 0.09, 0.05))
        wing_near = self.add("wing_near", body, (0.0, -0.09, 0.05), rot=(D(180), 0, 0))
        eyes = self.add("eyes", head, (0.06, -0.06, 0.02))
        fur, pale, membrane, ember, ink = mat("fur", FUR), mat("pale", FUR_PALE), mat("membrane", MEMBRANE), mat("ember", EMBER), mat("ink", INK)
        sphere("thorax", 0.13, fur, body, scale=(1.5, 0.75, 0.85))
        sphere("belly", 0.10, pale, body, loc=(0.02, 0, -0.05), scale=(1.4, 0.6, 0.6))
        sphere("head_mesh", 0.09, fur, head, scale=(1.05, 0.9, 0.95))
        for side in (-1, 1):
            cone("ear%d" % side, 0.035, 0.004, 0.14, fur, head, loc=(-0.01, side * 0.05, 0.11), rot=(D(side * 12), D(-15), 0))
        cone("snout", 0.035, 0.015, 0.07, pale, head, loc=(0.11, 0, -0.01), rot=(0, D(90), 0))
        sphere("eye", 0.022, ember, eyes)
        for w, side in ((wing_far, 1), (wing_near, -1)):
            slab(w.name + "_m", self.WING, 0.02, membrane, w)
            for k, (x, z) in enumerate(self.FINGERS):
                # the fingers: an ink line from the shoulder to each scallop's point
                length = math.hypot(x, z)
                ang = math.degrees(math.atan2(x, z))
                cone("%s_finger%d" % (w.name, k), 0.010, 0.004, length, fur, w, loc=(x * 0.5, side * 0.012, z * 0.5), rot=(0, D(ang), 0))
        for side in (-1, 1):
            cone("claw%d" % side, 0.014, 0.003, 0.09, ink, body, loc=(-0.18, side * 0.04, 0.03), rot=(0, D(-100), 0))
        self.snapshot()

    def wings(self, a):
        """Both wings tipped a degrees out of the picture plane (a flap); 0 is flat, the full scallop shown."""
        self.rot("wing_far", x=a)
        self.rot("wing_near", x=a)

    def hang(self, fold=1.0, sway=0.0):
        """Head down at the roost, wings folded along the body by fold (1 closed, 0 spread)."""
        self.rot("body", y=90 + sway)
        self.rot("wing_far", y=90 * fold)      # both fold along the body: the far one over, the near one under
        self.rot("wing_near", y=-90 * fold)
        for w in ("wing_far", "wing_near"):
            self.scale(w, 1, 1, 1 - 0.25 * fold)
        self.rot("head", y=10 * fold)


def bat_idle(b, i, n):
    t = i / n
    b.hang(1.0, 4 * math.sin(2 * math.pi * t))
    b.rot("head", y=10 + 6 * math.sin(2 * math.pi * t + 1))
    b.scale("eyes", 0.6 + 0.4 * abs(math.cos(math.pi * t)), 1, 0.6 + 0.4 * abs(math.cos(math.pi * t)))


def bat_unfurl(b, i, n):
    k = (0.25, 0.65, 1.0)[i]
    b.hang(1.0 - k)
    b.scale("eyes", 1 + 0.8 * k, 1, 1 + 0.8 * k)
    b.rot("head", y=-25 * k)
    b.move("body", z=0.05 * k)


def bat_swoop(b, i, n):
    b.rot("body", y=(28, 36)[i])
    b.wings((-15, 25)[i])
    b.scale("body", 1.12, 1, 0.9)
    b.rot("head", y=-10)
    b.scale("eyes", 1.6, 1, 1.6)


def bat_move(b, i, n):
    t = i / n
    b.wings(35 * math.sin(2 * math.pi * t))
    b.rot("body", y=-6)
    b.move("body", z=0.03 * math.sin(2 * math.pi * t))


def bat_hurt(b, i, n):
    k = (1.0, 0.5)[i]
    b.rot("body", y=-30 * k, z=12 * k)
    b.wings(55 * k)
    b.scale("body", 0.9, 1, 1.15)


def bat_death(b, i, n):
    t = min(1.0, i / 3)
    b.rot("body", y=-100 * t)
    b.move("body", z=-0.22 * t)
    b.rot("wing_far", y=70 * t)
    b.rot("wing_near", y=-70 * t)
    b.scale("eyes", 1 - 0.8 * t, 1, 1 - 0.8 * t)


BAT_CLIPS = [
    ("idle", 12, 4, True, bat_idle),
    ("unfurl", 12, 3, False, bat_unfurl),
    ("swoop", 24, 2, True, bat_swoop),
    ("move", 12, 4, True, bat_move),
    ("hurt", 12, 2, False, bat_hurt),
    ("death", 12, 4, False, bat_death),
]


# ================================================================ Salamander (1.2 x 0.5, pogo it: its back burns)

class Salamander(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        body = self.add("body", root, (0, 0, -0.02))
        head = self.add("head", body, (0.36, 0, 0.02))
        tail = self.add("tail", body, (-0.34, 0, 0.0))
        embers = self.add("embers", body, (0, 0, 0.10))
        basalt, belly, ember, hot, ink = mat("basalt", BASALT), mat("belly", BELLY), mat("ember", EMBER), mat("hot", EMBER_HOT), mat("ink", INK)
        sphere("trunk", 0.16, basalt, body, scale=(2.2, 0.9, 0.7))
        sphere("belly_m", 0.12, belly, body, loc=(0.02, 0, -0.06), scale=(2.4, 0.7, 0.45))
        sphere("head_mesh", 0.10, basalt, head, scale=(1.35, 0.95, 0.75))
        sphere("eye", 0.025, hot, head, loc=(0.06, -0.085, 0.045))
        cone("tail_m", 0.085, 0.006, 0.45, basalt, tail, loc=(-0.21, 0, 0), rot=(0, D(-90), 0))
        for k, x in enumerate((0.20, 0.07, -0.06, -0.19)):
            e = self.add("ember%d" % k, embers, (x, 0, 0.0))
            sphere("ember%d_m" % k, 0.045 if k % 2 else 0.055, ember if k % 2 else hot, e, scale=(1.2, 0.8, 0.8))
        crest = self.add("crest", embers, (0.0, 0, -0.02))
        cube("crest_m", (0.62, 0.02, 0.035), ember, crest)
        for k, (x, side) in enumerate([(0.22, -1), (-0.14, -1), (0.22, 1), (-0.14, 1)]):
            leg = self.add("leg%d" % k, body, (x, side * 0.13, -0.08))
            cone("leg%d_a" % k, 0.032, 0.02, 0.16, basalt, leg, loc=(0, side * 0.03, -0.06), rot=(D(-side * 40), 0, 0))
            cone("leg%d_b" % k, 0.02, 0.012, 0.12, basalt, leg, loc=(0.02, side * 0.10, -0.15), rot=(D(side * 35), D(10), 0))
            for f in (-1, 1):
                cone("leg%d_toe%d" % (k, f), 0.008, 0.002, 0.06, ink, leg, loc=(0.04 + 0.02 * f, side * 0.13, -0.20), rot=(0, D(80 + 15 * f), 0))
        self.snapshot()

    def legs_phase(self, t, amp=25.0):
        for k in range(4):
            ph = t * 2 * math.pi + (0 if k in (0, 3) else math.pi)
            self.rot("leg%d" % k, y=amp * math.sin(ph))

    def burn(self, k):
        """The embers up by k: bigger, brighter, the crest standing."""
        for i in range(4):
            self.scale("ember%d" % i, 1 + 0.6 * k, 1 + 0.4 * k, 1 + 1.2 * k)
        self.scale("crest", 1, 1, 1 + 2.5 * k)
        self.move("embers", z=0.02 * k)


def sal_idle(s, i, n):
    t = i / n
    b = math.sin(2 * math.pi * t)
    s.scale("body", 1, 1, 1 + 0.04 * b)
    s.rot("tail", y=5 * b)
    s.burn(0.1 + 0.1 * abs(b))
    s.rot("head", y=-3 * b)


def sal_move(s, i, n):
    t = i / n
    s.legs_phase(t)
    s.move("body", z=0.012 * abs(math.sin(2 * math.pi * t)))
    s.rot("tail", y=8 * math.sin(2 * math.pi * t))
    s.rot("body", y=3 * math.sin(2 * math.pi * t))
    s.burn(0.15)


def sal_flare(s, i, n):
    k = (0.3, 0.7, 1.0)[i]
    s.burn(k)
    s.rot("head", y=-28 * k)
    s.move("body", z=0.06 * k)
    s.rot("body", y=-8 * k)
    s.rot("tail", y=20 * k)
    s.rot("leg0", y=-20 * k); s.rot("leg2", y=-20 * k)


def sal_rush(s, i, n):
    s.scale("body", 1.2, 1, 0.85)
    s.rot("head", y=6)
    s.legs_phase((0.0, 0.5)[i], amp=42)
    s.rot("tail", y=-8 + 10 * i)
    s.burn(1.0)


def sal_cool(s, i, n):
    k = 1 - (i + 1) / (n + 1)
    s.burn(0.1 + 0.9 * k)
    s.move("body", z=-0.03 * (1 - k))
    s.rot("head", y=14 * (1 - k))
    s.scale("body", 1 + 0.1 * k, 1, 1 - 0.08 * (1 - k))


def sal_hurt(s, i, n):
    k = (1.0, 0.5)[i]
    s.scale("body", 0.9 * k + (1 - k), 1, 1.25 * k + (1 - k))
    s.rot("head", y=-30 * k)
    s.rot("tail", y=-25 * k)
    s.burn(1.2 * k)


def sal_death(s, i, n):
    t = min(1.0, i / 3)
    s.rot("body", x=165 * t)
    s.move("body", z=0.08 * math.sin(math.pi * t))
    s.burn(0.6 * (1 - t) - 0.2 * t)
    for k in range(4):
        s.rot("leg%d" % k, y=40 * math.sin(2 * math.pi * t + k))


SALAMANDER_CLIPS = [
    ("idle", 12, 4, True, sal_idle),
    ("move", 12, 6, True, sal_move),
    ("flare", 12, 3, False, sal_flare),
    ("rush", 24, 2, True, sal_rush),
    ("cool", 12, 3, False, sal_cool),
    ("hurt", 12, 2, False, sal_hurt),
    ("death", 12, 4, False, sal_death),
]


# ================================================================ all of them

# name, cell (units), builder, clips, line thickness at 2x, ink colour
CREATURES = [
    ("CaveBat", 1.6, CaveBat, BAT_CLIPS, 2.8, INK),
    ("Salamander", 1.6, Salamander, SALAMANDER_CLIPS, 3.0, INK),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in CREATURES:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink)


if __name__ == "__main__":
    main()

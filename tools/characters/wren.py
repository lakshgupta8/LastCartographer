"""Wren, built and animated in Blender (CHR-02, CHR-03): a small round bird in an ink-blue cowl with the
needle-quill, rendered side-on to frames the packer turns into sprite sheets.

Run from the repo root, then pack:

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/wren.py
    python tools/characters/pack.py wren
    ... -P tools/characters/wren.py -- idle run          (only those clips)
    ... -P tools/characters/wren.py -- turnaround        (the model sheet only)

The model is parts on a hierarchy of empties (root, hips, body, head, wings, tail, quill, legs), so every
clip is a function of time that sets a few rotations and offsets: no armature, nothing baked. Frames are
rendered at twice the game's density (art-direction 4: authored at 2x, 96 px per unit in game) with the
same flat washes and Freestyle ink line as the paper kits, so she and the world are one drawing.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, mat, lerp, run, argv_after_dashes, INK, PAPER

CELL = 2.0        # units per frame cell: room for the quill and the jumps

# Wren's colours (art-direction 4)
BROWN = (0.42, 0.38, 0.34)
BROWN_DARK = (0.33, 0.29, 0.26)
CREAM = (0.93, 0.88, 0.76)
COWL = (0.16, 0.22, 0.40)
BRASS = (0.78, 0.62, 0.30)
BEAK = (0.55, 0.42, 0.22)


class Wren(Rig):
    def __init__(self):
        super().__init__()
        root = self.add("root")
        hips = self.add("hips", root, (0, 0, 0.32))
        body = self.add("body", hips, (0, 0, 0.30))
        head = self.add("head", body, (0.14, 0, 0.42))
        wing_near = self.add("wing_near", body, (0.0, -0.27, 0.12))
        wing_far = self.add("wing_far", body, (0.0, 0.27, 0.12))
        quill = self.add("quill", wing_near, (0.16, -0.1, -0.14), rot=(0, D(30), 0))   # held up and forward
        tail = self.add("tail", body, (-0.30, 0, -0.02))
        leg_l = self.add("leg_l", root, (0.05, -0.1, 0.32))
        leg_r = self.add("leg_r", root, (0.05, 0.1, 0.32))

        brown, dark, cream, cowl = mat("brown", BROWN), mat("dark", BROWN_DARK), mat("cream", CREAM), mat("cowl", COWL)
        brass, ink, beak = mat("brass", BRASS), mat("ink", INK), mat("beak", BEAK)
        sphere("body_mesh", 0.33, brown, body, scale=(1.0, 0.85, 1.05))
        sphere("breast", 0.25, cream, body, loc=(0.14, 0, -0.09), scale=(0.9, 0.8, 1.0))
        cone("cowl", 0.41, 0.19, 0.44, cowl, body, loc=(0.0, 0, 0.14))
        sphere("clasp", 0.045, brass, body, loc=(0.20, -0.24, 0.28))
        sphere("head_mesh", 0.21, brown, head, scale=(1.05, 0.95, 1.0))
        cone("beak", 0.07, 0.006, 0.2, beak, head, loc=(0.27, 0, -0.02), rot=(0, D(90), 0))
        sphere("eye", 0.035, ink, head, loc=(0.10, -0.19, 0.05))
        sphere("glint", 0.011, mat("paper", PAPER), head, loc=(0.115, -0.222, 0.062))
        cone("crest", 0.05, 0.005, 0.16, dark, head, loc=(-0.08, 0, 0.19), rot=(0, D(-35), 0))
        sphere("wing_near_mesh", 0.2, dark, wing_near, loc=(-0.05, -0.04, -0.08), scale=(1.3, 0.35, 0.7), rot=(0, D(-15), 0))
        sphere("wing_far_mesh", 0.2, dark, wing_far, loc=(-0.05, 0.04, -0.08), scale=(1.3, 0.35, 0.7), rot=(0, D(-15), 0))
        cube("tail_mesh", (0.36, 0.16, 0.06), dark, tail, loc=(-0.14, 0, 0.04), rot=(0, D(-25), 0))
        cone("quill_mesh", 0.022, 0.016, 1.1, ink, quill, loc=(0, 0, 0.25))
        cone("nib", 0.02, 0.0, 0.1, brass, quill, loc=(0, 0, 0.85))
        sphere("quill_end", 0.03, brass, quill, loc=(0, 0, -0.30))
        for leg in (leg_l, leg_r):
            cone("shin" + leg.name[-2:], 0.026, 0.022, 0.32, dark, leg, loc=(0, 0, -0.16))
            cube("foot" + leg.name[-2:], (0.17, 0.06, 0.03), dark, leg, loc=(0.05, 0, -0.32))
        self.snapshot()

    def flap(self, a):
        """Both wings up by a degrees (about the body's forward axis)."""
        self.rot(self.wing_near, x=a)
        self.rot(self.wing_far, x=-a)

    def legs(self, l, r):
        """Legs swung forward by l and r degrees."""
        self.rot(self.leg_l, y=l)
        self.rot(self.leg_r, y=r)


# ---------------------------------------------------------------- clips
# Each clip: (fps, frames, loop, pose(w, i, n)). Rotations about Y lean forward for positive angles.

def idle(w, i, n):
    b = math.sin(2 * math.pi * i / n)
    w.move(w.body, z=0.02 * b)
    w.rot(w.head, y=-3 * b)
    w.rot(w.tail, y=4 * b)
    w.rot(w.wing_near, y=3 * b)
    w.rot(w.quill, y=2 * b)


def run_(w, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.legs(38 * s, -38 * s)
    w.move(w.body, z=0.05 * abs(math.sin(2 * math.pi * t)))
    w.rot(w.body, y=10)
    w.rot(w.head, y=-6 + 2 * s)
    w.rot(w.wing_near, y=-25)
    w.rot(w.wing_far, y=-25)
    w.rot(w.tail, y=15)
    w.rot(w.quill, y=12)


def jump(w, i, n):
    if i == 0:
        w.move(w.body, z=-0.07)
        w.legs(20, 20)
        w.scale(w.body, 1.1, 1, 0.9)
        w.flap(-10)
        return
    t = (i - 1) / max(1, n - 2)
    w.legs(-35, -45)
    w.flap(50 - 15 * t)
    w.rot(w.body, y=5)
    w.rot(w.head, y=-8)
    w.rot(w.tail, y=-10)
    w.scale(w.body, 0.96, 1, 1.06)


def fall(w, i, n):
    t = i / n
    w.flap(22 + 10 * math.sin(2 * math.pi * t))
    w.legs(15, 10)
    w.rot(w.head, y=8)
    w.rot(w.tail, y=8)
    w.rot(w.quill, y=-10)


def glide(w, i, n):
    t = i / n
    w.flap(72 + 8 * math.sin(2 * math.pi * t))
    w.legs(-30, -30)
    w.rot(w.body, y=-5)
    w.rot(w.tail, y=-12)


def land(w, i, n):
    k = (1.15, 1.06, 0.98)[i] if i < 3 else 1.0
    w.scale(w.body, k, 1, 2.0 - k)
    w.move(w.body, z=-0.08 * (1 - i / 3))
    w.legs(25 * (1 - i / 3), 25 * (1 - i / 3))
    w.flap(20 * (1 - i / 3))


def cling(w, i, n):
    w.rot(w.body, y=-12)
    w.flap(40 + 5 * i)
    w.legs(30, 25)
    w.rot(w.quill, y=-25)
    w.rot(w.head, y=-10)


def dash(w, i, n):
    w.rot(w.body, y=22)
    w.scale(w.body, 1.25, 1, 0.85)
    w.legs(-45, -50)
    w.flap(-8)
    w.rot(w.quill, y=55)
    w.rot(w.tail, y=25)


def thread(w, i, n):
    w.rot(w.body, y=28)
    w.scale(w.body, 1.15, 1, 0.9)
    w.legs(-40, -45)
    w.flap(35)
    w.rot(w.quill, y=45)
    w.move(w.quill, x=0.1, z=0.1)


def strike1(w, i, n):   # the forward thrust
    q = (-25, 60, 68, 50, 30, 12)[i]
    lean = (-5, 12, 14, 8, 4, 0)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=(0, 0.12, 0.16, 0.08, 0, 0)[i])
    w.rot(w.body, y=lean)
    w.rot(w.head, y=-lean * 0.5)
    w.flap((5, -10, -10, -5, 0, 0)[i])


def strike2(w, i, n):   # the rising slash
    q = (75, 40, -5, -25, -15, 0)[i]
    w.rot(w.quill, y=q)
    w.rot(w.body, y=(8, 4, -4, -8, -4, 0)[i])
    w.rot(w.head, y=(4, 0, -8, -10, -6, 0)[i])
    w.flap((-5, 5, 20, 25, 15, 5)[i])


def strike3(w, i, n):   # the overhead stab
    q = (-40, 10, 75, 90, 70, 45)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=(0, 0.05, 0.14, 0.18, 0.1, 0.02)[i], z=(0.08, 0.06, 0, -0.04, 0, 0)[i])
    w.rot(w.body, y=(-8, 0, 12, 16, 10, 4)[i])
    w.rot(w.head, y=(-6, -2, 6, 8, 4, 0)[i])
    w.flap((15, 5, -10, -12, -5, 0)[i])


def strike_up(w, i, n):
    q = (30, -10, -35, -30, -15, 0)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, z=(0, 0.08, 0.14, 0.12, 0.06, 0)[i])
    w.rot(w.body, y=(4, -4, -8, -6, -2, 0)[i])
    w.rot(w.head, y=(0, -8, -14, -12, -6, 0)[i])
    w.flap((0, 10, 20, 15, 8, 0)[i])


def pogo(w, i, n):   # the down-strike: the quill under her
    q = (90, 140, 152, 140)[i]
    w.rot(w.quill, y=q)
    w.move(w.quill, x=-0.05, z=(0, -0.1, -0.16, -0.1)[i])
    w.legs(-30, -30)
    w.scale(w.body, 0.95, 1, 1.06)
    w.flap((10, 30, 35, 30)[i])
    w.rot(w.head, y=(6, 12, 14, 12)[i])


def bind(w, i, n):   # the quill circles her: she redraws her own outline
    t = i / n
    w.move(w.quill, x=-0.16, z=0.14)
    w.rot(w.quill, y=-30 + 360 * t)
    w.rot(w.head, y=6)
    w.move(w.body, z=0.01 * math.sin(2 * math.pi * t))
    w.flap(5)


def survey(w, i, n):   # the quill held high; she looks out
    t = i / n
    w.rot(w.quill, y=-32 + 2 * math.sin(2 * math.pi * t))
    w.move(w.quill, z=0.12)
    w.rot(w.head, y=-16)
    w.rot(w.body, y=-4)
    w.rot(w.tail, y=-6)
    w.flap(6)


def hurt(w, i, n):
    k = (1.0, 0.7, 0.35)[i]
    w.rot(w.body, y=-22 * k)
    w.rot(w.head, y=-14 * k)
    w.flap(45 * k)
    w.legs(20 * k, 15 * k)
    w.rot(w.quill, y=70 * k)
    w.move(w.body, x=-0.06 * k)


def death(w, i, n):
    t = min(1.0, i / 5)
    w.rot(w.body, y=-85 * t)
    w.move(w.body, z=-0.28 * t, x=-0.15 * t)
    w.rot(w.head, y=-30 * t)
    w.flap(60 * t)
    w.legs(40 * t, 30 * t)
    w.rot(w.quill, y=120 * t)
    w.move(w.quill, x=0.1 * t, z=-0.2 * t)
    w.rot(w.hips, y=-40 * t)


CLIPS = [
    ("idle", 12, 8, True, idle),
    ("run", 12, 8, True, run_),
    ("jump", 12, 4, False, jump),
    ("fall", 12, 4, True, fall),
    ("glide", 12, 4, True, glide),
    ("land", 12, 3, False, land),
    ("cling", 12, 2, True, cling),
    ("dash", 24, 3, False, dash),
    ("thread", 24, 2, True, thread),
    ("strike1", 24, 6, False, strike1),
    ("strike2", 24, 6, False, strike2),
    ("strike3", 24, 6, False, strike3),
    ("strike_up", 24, 6, False, strike_up),
    ("pogo", 24, 4, False, pogo),
    ("bind", 12, 8, True, bind),
    ("survey", 12, 6, True, survey),
    ("hurt", 12, 3, False, hurt),
    ("death", 12, 8, False, death),
]


if __name__ == "__main__":
    run("Wren", CELL, CLIPS, Wren, CELL / 2, only=argv_after_dashes())

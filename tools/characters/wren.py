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


def glide(w, i, n):   # the Windmemory: wings wide and flat, a slow bob, the quill tucked
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.flap(74 + 7 * s)
    for wing in (w.wing_near, w.wing_far):
        w.scale(wing, 1.35, 1, 0.9)
    w.legs(-32, -30)
    w.rot(w.body, y=-6 + 2 * s)
    w.move(w.body, z=0.02 * s)
    w.rot(w.tail, y=-14)
    w.rot(w.head, y=-4)
    w.rot(w.quill, y=-12)


def glide_rise(w, i, n):   # carried up an updraft: wings cupped higher, the head up, the tail fanned down
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.flap(84 + 6 * s)
    for wing in (w.wing_near, w.wing_far):
        w.scale(wing, 1.3, 1, 1.0)
    w.legs(-40, -36)
    w.rot(w.body, y=-14 + 3 * s)
    w.move(w.body, z=0.03 * s)
    w.rot(w.tail, y=-26)
    w.rot(w.head, y=-14)
    w.rot(w.quill, y=-20)


def land(w, i, n):
    k = (1.15, 1.06, 0.98)[i] if i < 3 else 1.0
    w.scale(w.body, k, 1, 2.0 - k)
    w.move(w.body, z=-0.08 * (1 - i / 3))
    w.legs(25 * (1 - i / 3), 25 * (1 - i / 3))
    w.flap(20 * (1 - i / 3))


def cling(w, i, n):   # the Talonhold: gripping the wall, a breath, a glance up on the back half
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.rot(w.body, y=-14)
    w.move(w.body, x=0.06, z=0.02 * s)
    w.flap(42 + 4 * s)
    w.legs(32, 26)
    w.rot(w.quill, y=-28)
    w.rot(w.head, y=-14 - (6 if i >= n // 2 else 0))
    w.rot(w.tail, y=-16)


def slide(w, i, n):   # the slow slide after the hold: dragged down, the talons scraping, the wings scrabbling
    k = i
    w.rot(w.body, y=-8)
    w.move(w.body, x=0.05, z=-0.03 * k)
    w.flap(30 + 12 * (k % 2))
    w.legs(38 - 6 * k, 30 - 4 * k)
    w.rot(w.quill, y=-10)
    w.rot(w.head, y=12)
    w.rot(w.tail, y=-6)


def walljump(w, i, n):   # the push off the wall behind her: the crouch into it, the shove, the arc away
    k = (0.0, 0.6, 1.0)[i]
    w.rot(w.body, y=-18 + 36 * k)
    w.legs(40 - 80 * k, 35 - 75 * k)
    w.flap((30, 55, 40)[i])
    w.rot(w.quill, y=-20 + 50 * k)
    w.rot(w.head, y=-10 + 4 * k)
    w.rot(w.tail, y=-12 + 20 * k)
    w.scale(w.body, (1.0, 0.92, 1.0)[i], 1, (1.0, 1.1, 1.0)[i])


def dash(w, i, n):   # the Wingbeat: the wings snapped back, the streak, then thrown open to brake
    k = (0.5, 1.0, 1.0, 0.6)[i]
    w.rot(w.body, y=10 + 14 * k)
    w.scale(w.body, 1 + 0.3 * k, 1, 1 - 0.18 * k)
    w.legs(-40 * k - 5, -45 * k - 5)
    w.flap((-25, -12, -6, 40)[i])
    w.rot(w.quill, y=(20, 55, 60, 35)[i])
    w.move(w.quill, x=(0, 0.08, 0.1, 0.02)[i])
    w.rot(w.tail, y=(10, 28, 30, 5)[i])
    w.rot(w.head, y=(4, -6, -6, -2)[i])


def thread_cast(w, i, n):   # the Inkthread flung: the quill from over the shoulder to pointing at the anchor
    k = (0.5, 1.0)[i]
    w.rot(w.body, y=12 * k)
    w.rot(w.quill, y=-40 + 90 * k)
    w.move(w.quill, x=0.18 * k, z=0.12 * (1 - k))
    w.flap(20 - 30 * k)
    w.legs(10, 5)
    w.rot(w.head, y=-6)


def thread(w, i, n):   # the pull: stretched along the line, the wings swept back, the quill leading
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.rot(w.body, y=30)
    w.scale(w.body, 1.18, 1, 0.88)
    w.legs(-42 + 4 * s, -46 - 4 * s)
    w.flap(-30 + 6 * s)
    w.rot(w.quill, y=52)
    w.move(w.quill, x=0.14, z=0.1)
    w.rot(w.tail, y=24)
    w.rot(w.head, y=-8)


def thread_catch(w, i, n):   # the arrival: the hop up at the anchor, the quill pulled back in
    k = (0.0, 0.5, 1.0)[i]
    w.rot(w.body, y=18 - 22 * k)
    w.scale(w.body, 1.1 - 0.15 * k, 1, 0.95 + 0.1 * k)
    w.legs(-30 + 15 * k, -35 + 20 * k)
    w.flap(10 + 40 * k)
    w.rot(w.quill, y=45 - 60 * k)
    w.move(w.quill, x=0.1 * (1 - k), z=0.08 * k)
    w.rot(w.head, y=-10 - 4 * k)


def crosshatch(w, i, n):   # the Flourish: the quill scribbling in a cone before her, six strokes, then the recovery
    if i < 9:
        up = i % 2 == 0
        w.rot(w.quill, y=(70 if up else 30) + 4 * i)
        w.move(w.quill, x=0.1 + 0.02 * (i % 3), z=(0.1 if up else -0.08))
        w.rot(w.body, y=14 + (2 if up else -2))
        w.rot(w.head, y=-8)
        w.flap(-8 if up else 4)
        w.legs(6, -4)
        return
    k = (i - 9) / 3
    w.rot(w.quill, y=60 - 30 * k)
    w.move(w.quill, x=0.1 * (1 - k))
    w.rot(w.body, y=14 * (1 - k))
    w.rot(w.head, y=-8 * (1 - k))
    w.flap(10 * (1 - k))


def longstroke(w, i, n):   # the Flourish: the wind-up, then the long thrust with the whole body behind it
    w.rot(w.quill, y=(-30, -10, 85, 92, 80, 50)[i])
    w.move(w.quill, x=(0, -0.06, 0.26, 0.32, 0.22, 0.08)[i], z=(0.1, 0.12, -0.02, -0.02, 0, 0)[i])
    w.rot(w.body, y=(-10, -14, 24, 28, 18, 6)[i])
    w.scale(w.body, (1, 0.95, 1.25, 1.3, 1.15, 1.02)[i], 1, (1, 1.04, 0.86, 0.84, 0.92, 1)[i])
    w.legs((15, 20, -35, -40, -25, -5)[i], (10, 15, 30, 32, 20, 4)[i])
    w.flap((15, 25, -20, -22, -10, 0)[i])
    w.rot(w.head, y=(-6, -8, 8, 10, 6, 0)[i])
    w.rot(w.tail, y=(-8, -12, 20, 24, 14, 4)[i])


def blot(w, i, n):   # the Flourish: the quill stabbed down at her feet, the crouch, the burst, up and away
    w.rot(w.quill, y=(0, 110, 150, 150, 120, 60)[i])
    w.move(w.quill, x=(0, 0.05, 0.1, 0.1, 0.06, 0)[i], z=(0.08, -0.05, -0.18, -0.18, -0.08, 0)[i])
    w.move(w.body, z=(0.02, -0.08, -0.14, -0.1, 0.04, 0)[i])
    w.scale(w.body, (1, 1.1, 1.2, 1.15, 0.95, 1)[i], 1, (1, 0.9, 0.82, 0.86, 1.06, 1)[i])
    w.legs((0, 20, 30, 28, -5, 0)[i], (0, 18, 28, 26, -5, 0)[i])
    w.flap((5, 15, 20, 60, 70, 20)[i])
    w.rot(w.head, y=(0, 10, 16, 8, -8, 0)[i])
    w.rot(w.body, y=(0, 8, 12, 6, -4, 0)[i])


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
    ("glide", 12, 6, True, glide),
    ("glide_rise", 12, 4, True, glide_rise),
    ("land", 12, 3, False, land),
    ("cling", 12, 4, True, cling),
    ("slide", 12, 3, True, slide),
    ("walljump", 24, 3, False, walljump),
    ("dash", 24, 4, False, dash),
    ("thread_cast", 24, 2, False, thread_cast),
    ("thread", 24, 4, True, thread),
    ("thread_catch", 24, 3, False, thread_catch),
    ("crosshatch", 24, 12, False, crosshatch),
    ("longstroke", 24, 6, False, longstroke),
    ("blot", 24, 6, False, blot),
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

"""The returning cast, built and animated in Blender (CHR-11): Sable, Dotha, Isolde, Pell, Runa, Kettil, Teodor,
Idrenne, Maren, Corvin, Ilse, Corra, Marrow and Aury, each rendered side-on to frames for pack.py.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/cast.py
    ... -- Sable Dotha                    (only those characters)
    python tools/characters/pack.py sable dotha isolde pell runa kettil teodor idrenne maren corvin ilse corra marrow aury

Townsfolk are ovals (art-direction 4): one parametric bird, a body on legs with a neck, a head, a beak whose lower
half opens to talk, wings and a tail, and a spec per species for the sizes, the colours and the extras (Sable's
oilskin, Pell's satchel and charts, Runa's comb and fan, Kettil's stick, Teodor's hood, Maren's circlet, Corvin's
tufts, Isolde's cowl, Corra's page). Every clip is a function of time on the same parts, so idle, talk, walk and
asleep are shared and a character adds its own (Sable mends nets and reads the ledger, Dotha sings to the water,
Corra draws). Feet at the origin, facing +X; NpcSchedule and NpcTalker flip the root to face left.

Colour states (the Remnant grey, a place's fading) are not drawn here: everyone is rendered in their own colours,
and NpcInk washes the drawing at run time (docs/design/npc-animation.md).
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER

GREY_INK = lerp(INK, PAPER, 0.55)   # Marrow's line: a chick nobody finished drawing
BRASS = (0.78, 0.62, 0.30)


def m(rgb):
    return mat("c_%.2f_%.2f_%.2f" % rgb, rgb)


# ================================================================ the townsfolk bird

DEFAULT = dict(
    body_r=0.30, body_scale=(1.0, 0.85, 1.1), leg_len=0.34, leg_r=0.024, foot=0.15,
    neck_len=0.10, neck_r=0.07, neck_lean=25, head_r=0.15, head_scale=(1.05, 0.95, 1.0),
    beak_r=0.04, beak_len=0.16, beak_curve=0, tail_len=0.26, tail_w=0.14, tail_angle=-20,
    wing_len=1.2, eye_r=0.03, stoop=0, breast=None, breast_r=0.7,
    body=(0.5, 0.5, 0.5), dark=(0.35, 0.35, 0.35), beak=(0.5, 0.4, 0.25), eye=INK, leg=None, crest=None,
)


class Townsfolk(Rig):
    """An oval on two legs. spec overrides DEFAULT; extras(self, spec) adds a species' own parts and props."""

    def __init__(self, spec, extras=None):
        super().__init__()
        s = dict(DEFAULT)
        s.update(spec)
        self.spec = s
        body_c, dark, beak_c, eye_c = m(s["body"]), m(s["dark"]), m(s["beak"]), m(s["eye"])
        leg_c = m(s["leg"]) if s["leg"] else dark
        root = self.add("root")
        hips = self.add("hips", root, (0, 0, s["leg_len"]))
        body = self.add("body", hips, (0, 0, s["body_r"] * 0.95), rot=(0, D(s["stoop"]), 0))
        neck = self.add("neck", body, (s["body_r"] * 0.45, 0, s["body_r"] * 0.80), rot=(0, D(s["neck_lean"]), 0))
        head = self.add("head", neck, (0, 0, s["neck_len"] + s["head_r"] * 0.6), rot=(0, D(-s["neck_lean"]), 0))
        jaw = self.add("jaw", head, (s["head_r"] * 0.85, 0, -s["head_r"] * 0.25))
        eye = self.add("eye", head, (s["head_r"] * 0.45, -s["head_r"] * 0.86, s["head_r"] * 0.2))
        wing_near = self.add("wing_near", body, (-s["body_r"] * 0.1, -s["body_r"] * 0.8 * s["body_scale"][1], s["body_r"] * 0.35))
        wing_far = self.add("wing_far", body, (-s["body_r"] * 0.1, s["body_r"] * 0.8 * s["body_scale"][1], s["body_r"] * 0.35))
        tail = self.add("tail", body, (-s["body_r"] * 0.9, 0, -s["body_r"] * 0.1))
        leg_l = self.add("leg_l", hips, (0.02, -0.08, 0))
        leg_r = self.add("leg_r", hips, (0.02, 0.08, 0))

        sphere("body_m", s["body_r"], body_c, body, scale=s["body_scale"])
        if s["breast"]:
            sphere("breast", s["body_r"] * s["breast_r"], m(s["breast"]), body,
                   loc=(s["body_r"] * 0.42, 0, -s["body_r"] * 0.25), scale=(0.9, 0.8 * s["body_scale"][1], 1.0))
        if s["neck_len"] > 0.02:
            cone("neck_m", s["neck_r"] * 1.15, s["neck_r"], s["neck_len"] + s["head_r"] * 0.8, body_c, neck,
                 loc=(0, 0, s["neck_len"] * 0.5 + s["head_r"] * 0.1))
        sphere("head_m", s["head_r"], body_c, head, scale=s["head_scale"])
        hr = s["head_r"]
        # the beak: an upper half fixed to the head, a lower half on the jaw that opens to talk
        cone("beak_top", s["beak_r"], s["beak_r"] * 0.12, s["beak_len"], beak_c, head,
             loc=(hr * 0.85 + s["beak_len"] * 0.48, 0, -hr * 0.05 - s["beak_len"] * 0.12 * math.sin(D(s["beak_curve"]))),
             rot=(0, D(90 + s["beak_curve"]), 0), scale=(1, 1, 1))
        cone("beak_low", s["beak_r"] * 0.8, s["beak_r"] * 0.1, s["beak_len"] * 0.9, beak_c, jaw,
             loc=(s["beak_len"] * 0.43, 0, -s["beak_r"] * 0.35 - s["beak_len"] * 0.1 * math.sin(D(s["beak_curve"]))),
             rot=(0, D(90 + s["beak_curve"] * 1.2), 0), scale=(1, 0.8, 0.6))
        sphere("eye_m", s["eye_r"], eye_c, eye)
        sphere("glint", s["eye_r"] * 0.32, m(PAPER), eye, loc=(s["eye_r"] * 0.4, -s["eye_r"] * 0.75, s["eye_r"] * 0.35))
        if s["crest"]:
            cr, cl, ca = s["crest"]
            cone("crest", cr, 0.004, cl, dark, head, loc=(-hr * 0.4, 0, hr * 0.85), rot=(0, D(ca), 0))
        wl = s["wing_len"]
        sphere("wing_near_m", s["body_r"] * 0.62, dark, wing_near, loc=(-s["body_r"] * 0.2, -0.03, -s["body_r"] * 0.3),
               scale=(wl, 0.3, 0.72), rot=(0, D(-18), 0))
        sphere("wing_far_m", s["body_r"] * 0.62, dark, wing_far, loc=(-s["body_r"] * 0.2, 0.03, -s["body_r"] * 0.3),
               scale=(wl, 0.3, 0.72), rot=(0, D(-18), 0))
        cube("tail_m", (s["tail_len"], s["tail_w"], 0.05), dark, tail, loc=(-s["tail_len"] * 0.45, 0, 0), rot=(0, D(s["tail_angle"]), 0))
        for leg in (leg_l, leg_r):
            cone("shin" + leg.name[-2:], s["leg_r"], s["leg_r"] * 0.85, s["leg_len"], leg_c, leg, loc=(0, 0, -s["leg_len"] * 0.5))
            cube("foot" + leg.name[-2:], (s["foot"], 0.06, 0.03), leg_c, leg, loc=(s["foot"] * 0.3, 0, -s["leg_len"]))
        if extras:
            extras(self, s)
        self.snapshot()

    # ---- helpers the clips share
    def flap(self, a):
        self.rot("wing_near", x=a)
        self.rot("wing_far", x=-a)

    def legs(self, l, r):
        self.rot("leg_l", y=l)
        self.rot("leg_r", y=r)

    def open_beak(self, a):
        self.rot("jaw", y=a)

    def blink(self, closed):
        self.scale("eye", 1, 1, 1 - 0.85 * closed)


# ---------------------------------------------------------------- shared clips

def idle(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.move("body", z=0.02 * s)
    b.rot("neck", y=-3 * s)
    b.rot("head", y=2 * math.sin(2 * math.pi * t + 1))
    b.rot("tail", y=3 * s)
    b.rot("wing_near", y=2 * s)
    b.blink(1.0 if i == n - 1 else 0.0)


def talk(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.open_beak(6 + 14 * abs(math.sin(math.pi * t * 2)))
    b.rot("neck", y=4 * s)
    b.rot("head", y=-5 + 4 * math.sin(2 * math.pi * t + 0.8))
    b.rot("wing_near", y=-8 * abs(s))
    b.move("body", z=0.012 * s)


def walk(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.legs(30 * s, -30 * s)
    b.move("body", z=0.035 * abs(s))
    b.rot("body", y=6)
    b.rot("neck", y=3 * abs(s))
    b.rot("head", y=-4)
    b.rot("tail", y=8)
    b.rot("wing_near", y=-6 - 3 * s)
    b.rot("wing_far", y=-6 + 3 * s)


def asleep(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.move("body", z=-0.08 + 0.01 * s)
    b.rot("neck", y=-55)
    b.rot("head", y=35)
    b.move("head", z=-0.05)
    b.blink(1.0)
    b.rot("wing_near", y=6)
    b.rot("wing_far", y=6)
    b.legs(8, 8)
    b.rot("tail", y=-6)


COMMON = [
    ("idle", 12, 8, True, idle),
    ("talk", 12, 6, True, talk),
    ("walk", 12, 8, True, walk),
    ("asleep", 12, 4, True, asleep),
]


# ================================================================ the cast

# ---- Sable: a cormorant, Captain of the Ferrymen. Black gone brown at the edges from salt and lamp-oil; sits low.
SABLE = dict(body_r=0.34, body_scale=(1.05, 0.85, 1.0), leg_len=0.30, leg=(0.16, 0.14, 0.14), neck_len=0.22, neck_r=0.075,
             neck_lean=30, head_r=0.14, head_scale=(1.2, 0.9, 0.95), beak_r=0.035, beak_len=0.26, beak_curve=8,
             tail_len=0.24, tail_w=0.12, tail_angle=-30, stoop=8, eye_r=0.028,
             body=(0.05, 0.045, 0.05), dark=(0.12, 0.08, 0.06), beak=(0.18, 0.15, 0.12), eye=(0.30, 0.55, 0.55))


def sable_extras(b, s):
    oil = m((0.30, 0.28, 0.24))
    cone("oilskin", 0.37, 0.16, 0.36, oil, b.body, loc=(-0.02, 0, 0.17))
    cube("collar", (0.20, 0.34, 0.06), oil, b.body, loc=(0.06, 0, 0.30))
    # props: the net she mends, the ledger she reads
    net = b.add("net", b.root, (0.42, -0.1, 0.02))
    rope = m((0.62, 0.52, 0.36))
    for k in range(4):
        cone("net_a%d" % k, 0.008, 0.008, 0.5, rope, net, loc=(0, 0, 0.02 + 0.07 * k), rot=(0, D(90), 0))
        cone("net_b%d" % k, 0.008, 0.008, 0.28, rope, net, loc=(-0.2 + 0.13 * k, 0, 0.13), rot=(0, 0, 0))
    b.prop("net", net)
    ledger = b.add("ledger", b.wing_near, (0.22, -0.06, -0.10), rot=(0, D(-30), 0))
    cube("ledger_m", (0.26, 0.04, 0.2), m((0.55, 0.42, 0.30)), ledger)
    cube("ledger_pages", (0.24, 0.05, 0.17), m(PAPER), ledger, loc=(0.0, 0, 0.0))
    b.prop("ledger", ledger)


def sable_mending(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.show("net")
    b.move("body", z=-0.16)
    b.legs(38, 38)
    b.rot("body", y=10)
    b.rot("neck", y=22)
    b.rot("head", y=6 + 3 * s)
    b.rot("wing_near", y=-38 - 22 * s)
    b.move("wing_near", x=0.04 * s)
    b.rot("wing_far", y=-30 + 12 * s)
    b.blink(1.0 if i == 3 else 0.0)


def sable_reading(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.show("ledger")
    b.rot("wing_near", y=-70)
    b.rot("neck", y=26)
    b.rot("head", y=14 + 2 * s)
    b.move("body", z=0.01 * s)
    b.rot("wing_far", y=-10)


SABLE_CLIPS = COMMON + [("mending", 12, 8, True, sable_mending), ("reading", 12, 6, True, sable_reading)]

# ---- Aury: her brother, keeper of the third lighthouse; the same bird in a keeper's coat, a lamp at his feet.
AURY = dict(SABLE, dark=(0.14, 0.11, 0.09), body=(0.07, 0.065, 0.065), stoop=4)


def aury_extras(b, s):
    coat = m((0.40, 0.36, 0.30))
    cone("coat", 0.40, 0.20, 0.42, coat, b.body, loc=(-0.02, 0, 0.12))
    lamp = b.add("lamp", b.root, (0.40, -0.05, 0.0))
    cone("lamp_base", 0.09, 0.07, 0.06, m(BRASS), lamp, loc=(0, 0, 0.03))
    cone("lamp_glass", 0.06, 0.06, 0.16, m((0.92, 0.84, 0.55)), lamp, loc=(0, 0, 0.14))
    cone("lamp_cap", 0.08, 0.02, 0.06, m(BRASS), lamp, loc=(0, 0, 0.25))


AURY_CLIPS = COMMON

# ---- Dotha: last elder of Merrow's End. An oystercatcher: black back, white breast, the long red bill; old, stooped.
DOTHA = dict(body_r=0.31, body_scale=(1.05, 0.85, 1.0), leg_len=0.30, leg=(0.80, 0.50, 0.46), neck_len=0.12, neck_r=0.07,
             neck_lean=22, head_r=0.14, beak_r=0.03, beak_len=0.34, beak_curve=2, tail_len=0.20, tail_w=0.14, tail_angle=-32,
             stoop=16, eye_r=0.03, breast=(0.90, 0.90, 0.86), breast_r=0.72,
             body=(0.05, 0.05, 0.06), dark=(0.09, 0.09, 0.11), beak=(0.86, 0.36, 0.12), eye=(0.75, 0.22, 0.12))


def dotha_extras(b, s):
    shawl = m((0.46, 0.40, 0.36))
    cone("shawl", 0.37, 0.16, 0.34, shawl, b.body, loc=(-0.04, 0, 0.16))
    cube("shawl_fringe", (0.10, 0.30, 0.10), shawl, b.body, loc=(-0.30, 0, 0.02))


def dotha_singing(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.rot("body", y=-10 + 4 * s)
    b.rot("neck", y=-30)
    b.rot("head", y=-6 + 3 * s)
    b.open_beak(18 + 8 * abs(math.sin(2 * math.pi * t)))
    b.flap(14 + 6 * s)
    b.move("body", z=0.02 * s)
    b.rot("tail", y=-6 * s)


DOTHA_CLIPS = COMMON + [("singing", 12, 8, True, dotha_singing)]

# ---- Isolde Marr: Wren's mentor, a Guild surveyor. A curlew: streaked brown, the long down-curved bill, a Guild cowl.
ISOLDE = dict(body_r=0.32, body_scale=(1.0, 0.85, 1.1), leg_len=0.42, leg_r=0.02, leg=(0.36, 0.34, 0.32), neck_len=0.18, neck_r=0.07,
              neck_lean=22, head_r=0.14, beak_r=0.028, beak_len=0.42, beak_curve=24, tail_len=0.24, tail_w=0.14, tail_angle=-18,
              eye_r=0.03, breast=(0.80, 0.72, 0.58), breast_r=0.72,
              body=(0.52, 0.44, 0.34), dark=(0.36, 0.30, 0.24), beak=(0.30, 0.26, 0.22), eye=INK)


def isolde_extras(b, s):
    cowl = m((0.30, 0.34, 0.46))
    cone("cowl", 0.38, 0.17, 0.40, cowl, b.body, loc=(0.0, 0, 0.16))
    sphere("clasp", 0.04, m(BRASS), b.body, loc=(0.21, -0.22, 0.28))
    sat = b.add("satchel", b.body, (-0.12, -0.28, -0.14))
    cube("satchel_m", (0.22, 0.08, 0.16), m((0.40, 0.30, 0.22)), sat)
    cone("chart", 0.03, 0.03, 0.30, m(PAPER), sat, loc=(0.0, -0.02, 0.10), rot=(0, D(80), 0))


ISOLDE_CLIPS = COMMON

# ---- Pell: a jackdaw, journeyman, three more things than hands. Black, grey nape, pale eye; a satchel and rolled charts.
PELL = dict(body_r=0.26, body_scale=(1.0, 0.85, 1.05), leg_len=0.30, leg=(0.18, 0.18, 0.20), neck_len=0.08, neck_r=0.07,
            head_r=0.14, beak_r=0.035, beak_len=0.14, tail_len=0.24, tail_w=0.12, tail_angle=-16, eye_r=0.03,
            body=(0.06, 0.06, 0.075), dark=(0.04, 0.04, 0.05), beak=(0.05, 0.05, 0.06), eye=(0.86, 0.86, 0.82))


def pell_extras(b, s):
    nape = m((0.56, 0.56, 0.58))
    sphere("nape", 0.12, nape, b.head, loc=(-0.06, 0, -0.02), scale=(0.9, 1.05, 0.9))
    sphere("pupil", 0.014, m(INK), b.eye, loc=(0.01, -0.02, 0.0))
    sat = b.add("satchel", b.body, (-0.06, -0.24, -0.12))
    cube("satchel_m", (0.20, 0.08, 0.14), m((0.48, 0.36, 0.24)), sat)
    rolls = b.add("rolls", b.body, (-0.16, 0.0, 0.22))
    for k, (dz, ang) in enumerate(((0.0, 70), (0.06, 78), (0.12, 62))):
        cone("roll%d" % k, 0.028, 0.028, 0.34, m(PAPER), rolls, loc=(0.02 * k, 0.02 * (k - 1), dz), rot=(0, D(ang), 0))
        cone("roll%d_band" % k, 0.031, 0.031, 0.03, m((0.60, 0.20, 0.18)), rolls, loc=(0.02 * k, 0.02 * (k - 1), dz), rot=(0, D(ang), 0))


PELL_CLIPS = COMMON

# ---- Runa: a capercaillie, the strongest climber in Emberdown. Slate-dark, a red comb over the eye, the fan tail.
RUNA = dict(body_r=0.36, body_scale=(1.05, 0.9, 1.05), leg_len=0.32, leg_r=0.03, foot=0.2, leg=(0.22, 0.20, 0.18), neck_len=0.12,
            neck_r=0.09, neck_lean=20, head_r=0.15, beak_r=0.04, beak_len=0.14, beak_curve=6, tail_len=0.30, tail_w=0.40,
            tail_angle=-40, eye_r=0.03, breast=(0.18, 0.34, 0.30), breast_r=0.7,
            body=(0.10, 0.11, 0.14), dark=(0.16, 0.12, 0.10), beak=(0.86, 0.84, 0.72), eye=INK)


def runa_extras(b, s):
    comb = m((0.75, 0.15, 0.12))
    sphere("comb", 0.05, comb, b.head, loc=(0.05, -0.12, 0.09), scale=(1.2, 0.5, 0.7))
    # the fan: three more tail slabs spread
    for k, ang in enumerate((-70, -55, -25)):
        cube("fan%d" % k, (0.28, 0.16, 0.04), m(s["dark"]), b.tail, loc=(-0.10, 0.0, 0.03 * k), rot=(0, D(ang), 0))
    # a chalk bag: the old way needs chalk
    cube("chalk_bag", (0.12, 0.10, 0.10), m((0.60, 0.52, 0.40)), b.body, loc=(-0.12, -0.30, -0.22))


RUNA_CLIPS = COMMON

# ---- Old Kettil: capercaillie, mine foreman turned town-mother; greyer, stooped, a stick.
KETTIL = dict(RUNA, body=(0.32, 0.32, 0.34), dark=(0.36, 0.32, 0.30), breast=(0.30, 0.36, 0.36), stoop=14, leg_len=0.30,
              tail_angle=-28)


def kettil_extras(b, s):
    comb = m((0.60, 0.30, 0.28))
    sphere("comb", 0.04, comb, b.head, loc=(0.05, -0.12, 0.09), scale=(1.2, 0.5, 0.6))
    for k, ang in enumerate((-55, -40)):
        cube("fan%d" % k, (0.26, 0.16, 0.04), m(s["dark"]), b.tail, loc=(-0.10, 0.0, 0.03 * k), rot=(0, D(ang), 0))
    stick = b.add("stick", b.wing_near, (0.20, -0.06, -0.20))
    cone("stick_m", 0.018, 0.022, 0.72, m((0.40, 0.30, 0.20)), stick, loc=(0, 0, -0.06), rot=(0, D(-8), 0))
    sphere("knob", 0.035, m((0.40, 0.30, 0.20)), stick, loc=(0.05, 0, 0.30))


KETTIL_CLIPS = COMMON

# ---- Brother Teodor Ashe: a mourning dove, sixty, leader of the Unwriters. Soft grey-brown, small head, a hood.
TEODOR = dict(body_r=0.30, body_scale=(1.0, 0.85, 1.1), leg_len=0.28, leg=(0.60, 0.40, 0.40), neck_len=0.10, neck_r=0.06,
              head_r=0.12, beak_r=0.025, beak_len=0.12, tail_len=0.34, tail_w=0.08, tail_angle=-24, stoop=6, eye_r=0.026,
              breast=(0.72, 0.62, 0.56), breast_r=0.7,
              body=(0.62, 0.56, 0.50), dark=(0.52, 0.46, 0.40), beak=(0.30, 0.28, 0.26), eye=INK)


def teodor_extras(b, s):
    robe = m((0.48, 0.46, 0.44))
    cone("robe", 0.38, 0.14, 0.46, robe, b.body, loc=(-0.02, 0, 0.10))
    sphere("hood", 0.145, robe, b.head, loc=(-0.035, 0, 0.005), scale=(1.0, 1.05, 1.02))   # over the back of the head, the face clear
    cone("hood_fall", 0.16, 0.10, 0.16, robe, b.head, loc=(-0.10, 0, -0.10), rot=(0, D(-20), 0))


TEODOR_CLIPS = COMMON

# ---- Speaker Idrenne: a crane, the Windreach Clans. Tall, long neck and legs, a red crown, a wind-cloak.
IDRENNE = dict(body_r=0.32, body_scale=(1.15, 0.8, 0.95), leg_len=0.62, leg_r=0.018, foot=0.16, leg=(0.20, 0.20, 0.22),
               neck_len=0.50, neck_r=0.05, neck_lean=14, head_r=0.11, head_scale=(1.2, 0.9, 0.9), beak_r=0.025, beak_len=0.22,
               tail_len=0.30, tail_w=0.14, tail_angle=-20, eye_r=0.026,
               body=(0.66, 0.66, 0.64), dark=(0.30, 0.30, 0.32), beak=(0.34, 0.32, 0.28), eye=(0.70, 0.55, 0.20))


def idrenne_extras(b, s):
    sphere("crown", 0.05, m((0.78, 0.18, 0.14)), b.head, loc=(-0.02, 0, 0.09), scale=(1.4, 0.8, 0.5))
    cone("neck_front", 0.05, 0.045, 0.40, m(s["dark"]), b.neck, loc=(0.03, 0, 0.22))
    cloak = m((0.50, 0.42, 0.36))
    cone("cloak", 0.40, 0.14, 0.50, cloak, b.body, loc=(-0.06, 0, 0.06))
    sphere("pupil", 0.012, m(INK), b.eye, loc=(0.01, -0.02, 0.0))


IDRENNE_CLIPS = COMMON

# ---- Queen-Regent Maren Ostrell: a swan. White, the neck in an S, an orange bill with the black knob, a thin circlet.
MAREN = dict(body_r=0.36, body_scale=(1.25, 0.85, 0.95), leg_len=0.26, leg_r=0.03, foot=0.2, leg=(0.20, 0.20, 0.22),
             neck_len=0.44, neck_r=0.06, neck_lean=34, head_r=0.12, head_scale=(1.25, 0.9, 0.9), beak_r=0.035, beak_len=0.20,
             tail_len=0.26, tail_w=0.18, tail_angle=8, eye_r=0.024, wing_len=1.3,
             body=(0.90, 0.90, 0.90), dark=(0.82, 0.82, 0.84), beak=(0.88, 0.50, 0.18), eye=INK)


def maren_extras(b, s):
    sphere("knob", 0.03, m(INK), b.head, loc=(0.11, 0, 0.02), scale=(1, 0.8, 0.8))
    # the S: a second neck segment bending back
    cone("neck_curve", 0.06, 0.06, 0.20, m(s["body"]), b.neck, loc=(-0.06, 0, 0.12), rot=(0, D(-30), 0))
    cone("circlet", 0.11, 0.11, 0.015, m(BRASS), b.head, loc=(0, 0, 0.10))
    cube("wing_lift", (0.30, 0.26, 0.10), m(s["dark"]), b.body, loc=(-0.20, 0, 0.24))


MAREN_CLIPS = COMMON

# ---- Corvin Halloway, the Archivist: a great owl. A broad oval, ear tufts, the big eyes; a Remnant, greyed at run time.
CORVIN = dict(body_r=0.40, body_scale=(1.0, 0.9, 1.15), leg_len=0.22, leg_r=0.03, foot=0.18, leg=(0.42, 0.36, 0.30),
              neck_len=0.02, head_r=0.20, head_scale=(1.05, 1.0, 0.95), beak_r=0.03, beak_len=0.08, beak_curve=30,
              tail_len=0.22, tail_w=0.20, tail_angle=-30, stoop=4, eye_r=0.06, wing_len=1.05,
              breast=(0.66, 0.60, 0.52), breast_r=0.75,
              body=(0.46, 0.40, 0.34), dark=(0.36, 0.30, 0.26), beak=(0.20, 0.18, 0.16), eye=(0.86, 0.70, 0.22))


def corvin_extras(b, s):
    for side, y in (("n", -0.09), ("f", 0.09)):
        cone("tuft_" + side, 0.05, 0.004, 0.16, m(s["dark"]), b.head, loc=(-0.06, y, 0.20), rot=(0, D(-25), 0))
    sphere("pupil", 0.03, m(INK), b.eye, loc=(0.02, -0.04, 0.0))
    sphere("disc", 0.10, m((0.58, 0.52, 0.46)), b.head, loc=(0.10, -0.12, 0.0), scale=(0.6, 0.3, 1.0))
    cube("spectacles", (0.02, 0.06, 0.02), m(BRASS), b.head, loc=(0.16, -0.16, 0.05))
    stone = b.add("stone", b.wing_near, (0.24, -0.05, -0.12))
    cube("stone_m", (0.12, 0.08, 0.10), m((0.52, 0.52, 0.50)), stone)


CORVIN_CLIPS = COMMON

# ---- Ilse: Wren's mother, a wren, grey in the Hollow (greyed at run time); older, no cowl, a shawl.
ILSE = dict(body_r=0.30, body_scale=(1.0, 0.85, 1.05), leg_len=0.28, neck_len=0.04, head_r=0.16, beak_r=0.04, beak_len=0.14,
            tail_len=0.26, tail_w=0.14, tail_angle=-40, eye_r=0.03, stoop=3, breast=(0.93, 0.88, 0.76), breast_r=0.75,
            body=(0.42, 0.38, 0.34), dark=(0.33, 0.29, 0.26), beak=(0.55, 0.42, 0.22), eye=INK,
            crest=(0.04, 0.12, -35))


def ilse_extras(b, s):
    shawl = m((0.50, 0.48, 0.46))
    cone("shawl", 0.34, 0.15, 0.30, shawl, b.body, loc=(-0.04, 0, 0.14))


ILSE_CLIPS = COMMON

# ---- Corra: a heron chick, faded with the Old Capital. Small, grey-blue fluff, spiky crest, big feet; a page and charcoal.
CORRA = dict(body_r=0.22, body_scale=(1.0, 0.9, 1.1), leg_len=0.26, leg_r=0.02, foot=0.16, leg=(0.36, 0.38, 0.36), neck_len=0.10,
             neck_r=0.05, neck_lean=18, head_r=0.13, beak_r=0.025, beak_len=0.18, tail_len=0.12, tail_w=0.10, tail_angle=-20,
             eye_r=0.036, crest=(0.035, 0.14, -50),
             body=(0.60, 0.64, 0.68), dark=(0.48, 0.52, 0.58), beak=(0.62, 0.56, 0.36), eye=INK)


def corra_extras(b, s):
    for k, ang in enumerate((-70, -20)):
        cone("spike%d" % k, 0.03, 0.004, 0.12, m(s["dark"]), b.head, loc=(-0.02 + 0.03 * k, 0, 0.10), rot=(0, D(ang), 0))
    page = b.add("page", b.root, (0.30, -0.06, 0.0))
    cube("page_m", (0.30, 0.22, 0.01), m(PAPER), page, loc=(0, 0, 0.01))
    cube("drawing", (0.05, 0.08, 0.005), m(INK), page, loc=(0.03, 0, 0.02))
    cube("drawing_tall", (0.02, 0.04, 0.005), m(INK), page, loc=(-0.06, 0, 0.02))
    b.prop("page", page)
    coal = b.add("charcoal", b.wing_near, (0.12, -0.04, -0.14))
    cone("charcoal_m", 0.012, 0.012, 0.12, m(INK), coal, rot=(0, D(30), 0))


def corra_drawing(b, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.show("page")
    b.move("body", z=-0.10)
    b.legs(30, 30)
    b.rot("body", y=14)
    b.rot("neck", y=26)
    b.rot("head", y=8 + 2 * s)
    b.rot("wing_near", y=-55 - 10 * s)
    b.move("wing_near", x=0.05 * s)
    b.blink(1.0 if i == 5 else 0.0)


CORRA_CLIPS = COMMON + [("drawing", 12, 8, True, corra_drawing)]

# ---- Marrow: a grey chick, half-drawn, species unreadable. Small, round, a stub of a beak, the line half gone.
MARROW = dict(body_r=0.20, body_scale=(1.0, 0.9, 1.05), leg_len=0.18, leg_r=0.016, foot=0.10, neck_len=0.02, head_r=0.14,
              beak_r=0.02, beak_len=0.05, tail_len=0.08, tail_w=0.08, tail_angle=-20, eye_r=0.028, wing_len=0.9,
              body=(0.72, 0.72, 0.70), dark=(0.62, 0.62, 0.60), beak=(0.58, 0.56, 0.52), eye=GREY_INK, leg=(0.60, 0.60, 0.58))


def marrow_extras(b, s):
    # the down: a few soft tufts, no crest, nothing that names a species
    for k, (x, z, ang) in enumerate(((-0.06, 0.10, -60), (0.0, 0.12, -20), (0.05, 0.11, 20))):
        cone("down%d" % k, 0.02, 0.003, 0.06, m(s["dark"]), b.head, loc=(x, 0, z), rot=(0, D(ang), 0))


MARROW_CLIPS = COMMON


# ================================================================ the list

# (character, cell units, spec, extras, clips, line thickness, ink)
CAST = [
    ("Sable", 2.0, SABLE, sable_extras, SABLE_CLIPS, 3.0, INK),
    ("Dotha", 2.0, DOTHA, dotha_extras, DOTHA_CLIPS, 3.0, INK),
    ("Isolde", 2.2, ISOLDE, isolde_extras, ISOLDE_CLIPS, 3.0, INK),
    ("Pell", 2.0, PELL, pell_extras, PELL_CLIPS, 3.0, INK),
    ("Runa", 2.0, RUNA, runa_extras, RUNA_CLIPS, 3.0, INK),
    ("Kettil", 2.0, KETTIL, kettil_extras, KETTIL_CLIPS, 3.0, INK),
    ("Teodor", 2.0, TEODOR, teodor_extras, TEODOR_CLIPS, 3.0, INK),
    ("Idrenne", 2.6, IDRENNE, idrenne_extras, IDRENNE_CLIPS, 3.0, INK),
    ("Maren", 2.6, MAREN, maren_extras, MAREN_CLIPS, 3.0, INK),
    ("Corvin", 2.0, CORVIN, corvin_extras, CORVIN_CLIPS, 3.0, INK),
    ("Ilse", 1.6, ILSE, ilse_extras, ILSE_CLIPS, 3.0, INK),
    ("Corra", 1.4, CORRA, corra_extras, CORRA_CLIPS, 2.6, INK),
    ("Marrow", 1.2, MARROW, marrow_extras, MARROW_CLIPS, 2.4, GREY_INK),
    ("Aury", 2.0, AURY, aury_extras, AURY_CLIPS, 3.0, INK),
]


def main():
    only = argv_after_dashes()
    for name, cell, spec, extras, clips, line, ink in CAST:
        if only and name not in only:
            continue
        run(name, cell, clips, lambda spec=spec, extras=extras: Townsfolk(spec, extras), cell / 2, line=line, ink=ink)


if __name__ == "__main__":
    main()

"""The fledglings (CHR-14, bible 10 "Flight is memory"): the young birds who leap in the background of every region,
one species per region on the townsfolk bird at a chick's size: the coast's gull, the highland's grouse, the forest's
dove, the plateau's pigeon, the Steppe's crane, the Greyfold's outline and the Blank's grey chick.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/fledglings.py
    ... -- Fledgling_Gull Fledgling_Grey        (only those)
    python tools/characters/pack.py fledgling_gull fledgling_grouse fledgling_dove fledgling_pigeon fledgling_crane fledgling_outline fledgling_grey

Five clips, which FledglingLoop (World) plays along the leap it computes: idle on the perch, leap (crouch and
spring), glide (wings out, a slow bob), drop (half-open wings, legs flailing: they fall as they always have) and
land (the splat and the shake). Feet at the origin, facing +X; the loop mirrors the quad to leap the other way.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import lerp, run, argv_after_dashes, INK, PAPER
from cast import Townsfolk, GREY_INK

CELL = 1.25

# A chick: a big head on a small round body, stubby wings, short legs, a big eye.
CHICK = dict(body_r=0.16, body_scale=(1.0, 0.9, 1.05), leg_len=0.13, leg_r=0.015, foot=0.08,
             neck_len=0.03, neck_r=0.055, neck_lean=15, head_r=0.115, head_scale=(1.05, 1.0, 1.0),
             beak_r=0.03, beak_len=0.07, tail_len=0.10, tail_w=0.10, tail_angle=-15, wing_len=0.85, eye_r=0.03)

GULL = dict(CHICK, body=(0.86, 0.86, 0.82), dark=(0.55, 0.56, 0.56), beak=(0.80, 0.62, 0.25), leg=(0.78, 0.60, 0.30))
GROUSE = dict(CHICK, body=(0.48, 0.36, 0.24), dark=(0.28, 0.20, 0.14), beak=(0.25, 0.20, 0.15), crest=(0.03, 0.09, -20))
DOVE = dict(CHICK, body=(0.80, 0.74, 0.74), dark=(0.58, 0.50, 0.54), beak=(0.45, 0.35, 0.30))
PIGEON = dict(CHICK, body=(0.52, 0.56, 0.64), dark=(0.30, 0.34, 0.44), breast=(0.36, 0.56, 0.50), breast_r=0.7, beak=(0.30, 0.28, 0.28))
CRANE = dict(CHICK, body=(0.76, 0.62, 0.40), dark=(0.50, 0.40, 0.26), beak=(0.40, 0.36, 0.26), leg_len=0.20, leg_r=0.013,
             neck_len=0.08, neck_r=0.045, leg=(0.35, 0.30, 0.22))
# The Greyfold's: an outline with nothing inside it, seen at the edge of the eye.
OUTLINE = dict(CHICK, body=PAPER, dark=lerp(PAPER, INK, 0.08), beak=PAPER, eye=lerp(PAPER, INK, 0.5), leg=PAPER)
# The Blank's: the Remnant's chick, grey as Marrow.
GREY = dict(CHICK, body=lerp(PAPER, INK, 0.35), dark=lerp(PAPER, INK, 0.5), beak=lerp(PAPER, INK, 0.45), eye=GREY_INK, leg=lerp(PAPER, INK, 0.5))


# ---------------------------------------------------------------- the clips

def idle(b, i, n):
    """On the perch: a shuffle, the near wing stretched once, a blink."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.move("body", z=0.015 * s)
    b.rot("head", y=4 * math.sin(2 * math.pi * t + 1))
    b.rot("tail", y=4 * s)
    b.rot("wing_near", y=-10 if i == 2 else 0)
    b.blink(1.0 if i == n - 1 else 0.0)


def leap(b, i, n):
    """A crouch, then the spring with the wings thrown up."""
    t = i / (n - 1)
    crouch = max(0.0, 1 - 3 * t)
    spring = min(1.0, max(0.0, (t - 0.3) / 0.7))
    b.move("body", z=-0.06 * crouch + 0.05 * spring)
    b.rot("body", y=-8 * crouch + 14 * spring)
    b.legs(25 * crouch - 30 * spring, 25 * crouch - 30 * spring)
    b.flap(60 * spring)
    b.rot("neck", y=10 * spring)


def glide(b, i, n):
    """Wings out and flat, a slow bob, the legs trailing."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.rot("body", y=4 + 3 * s)
    for w in ("wing_near", "wing_far"):
        b.scale(w, 1.7, 1, 0.55)
        b.move(w, x=-0.05, z=0.10 + 0.02 * s)
        b.rot(w, y=-6 - 4 * s)
    b.legs(-70, -70)
    b.rot("tail", y=15)
    b.rot("neck", y=-10)
    b.rot("head", y=6)


def drop(b, i, n):
    """Half-open wings and flailing legs: they fall as they always have."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.rot("body", y=-25 + 10 * s)
    b.flap(35 + 25 * s)
    b.legs(40 * s, -40 * s)
    b.rot("neck", y=-15)
    b.rot("tail", y=-20 * s)
    b.blink(1.0 if i % 2 == 0 else 0.0)


def land(b, i, n):
    """The splat, then the shake."""
    t = i / (n - 1)
    squash = max(0.0, 1 - 2.5 * t)
    shake = math.sin(6 * math.pi * t) * (1 - t)
    b.move("body", z=-0.08 * squash)
    b.scale("body", 1 + 0.25 * squash, 1, 1 - 0.3 * squash)
    b.flap(20 * squash)
    b.rot("head", y=-10 * squash + 8 * shake)
    b.rot("tail", y=10 * shake)
    b.legs(30 * squash, 30 * squash)
    b.blink(1.0 if squash > 0.5 else 0.0)


CLIPS = [
    ("idle", 12, 6, True, idle),
    ("leap", 12, 4, False, leap),
    ("glide", 12, 6, True, glide),
    ("drop", 12, 4, True, drop),
    ("land", 12, 4, False, land),
]

LOOKS = [
    ("Fledgling_Gull", GULL, 2.6, INK),
    ("Fledgling_Grouse", GROUSE, 2.6, INK),
    ("Fledgling_Dove", DOVE, 2.6, INK),
    ("Fledgling_Pigeon", PIGEON, 2.6, INK),
    ("Fledgling_Crane", CRANE, 2.6, INK),
    ("Fledgling_Outline", OUTLINE, 2.0, lerp(PAPER, INK, 0.45)),
    ("Fledgling_Grey", GREY, 2.4, GREY_INK),
]


def main():
    only = argv_after_dashes()
    for name, spec, line, ink in LOOKS:
        if only and name not in only:
            continue
        run(name, CELL, CLIPS, lambda spec=spec: Townsfolk(spec), CELL * 0.5, turnaround=False, line=line, ink=ink)


if __name__ == "__main__":
    main()

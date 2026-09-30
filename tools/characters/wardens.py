"""The Warden family, built and animated in Blender (CHR-07): Warden-Sergeant Halvard, Cinder Warden Brann,
Warden-Captain Oriel, and two more generic Wardens (B with a cloak and an iron gorget, C with a helm and plume) beside
the one CHR-06 drew. Each is the Warden rig of saltmarrow_enemies.py with its own gear, colours and moves.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/wardens.py
    ... -- Halvard Oriel                 (only those)
    python tools/characters/pack.py halvard brann oriel warden_b warden_c

Clip names are what the bosses' Clip properties ask for (Halvard.Clip, Brann.Clip, Oriel.Clip) and, for Halvard,
what NpcAnimator asks for in the lighthouse (idle, talk, walk) before he is ever fought. Wardens are tall vertical
lines (art-direction 4): a heron, a crane, an egret. Every drawing is centred on the collider's centre, facing +X;
Halvard's manifest also records where his feet are so the lighthouse can stand him on the floor.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, sphere, cone, cube, mat, lerp, run, argv_after_dashes, INK, PAPER
from saltmarrow_enemies import (Warden, WARDEN, WARDEN_PALE, BRASS, warden_idle, warden_move, warden_measure,
                                warden_telegraph, warden_thrust, warden_recover, warden_hurt, warden_death, WARDEN_CLIPS)

CELL = 2.8
FEET = -0.82      # the Warden rig's feet, below the collider's centre (root at 0)
IRON = (0.30, 0.32, 0.36)
SASH = (0.55, 0.20, 0.16)


def recolour(rig, old, new_name, rgb):
    """Swap one wash for another on every mesh under the rig (the rig's mat() names are the colours' roles)."""
    new = mat(new_name, rgb)
    for ob in [rig.root] + list(rig.root.children_recursive):
        if ob.type == "MESH":
            for i, m in enumerate(ob.data.materials):
                if m is not None and m.name == old:
                    ob.data.materials[i] = new


def part(rig, name):
    for ob in rig.root.children_recursive:
        if ob.name == name:
            return ob
    return None


# ================================================================ Halvard: the sergeant (a heron; sash, plume, the long sighting-lance)

class Halvard(Warden):
    def __init__(self):
        super().__init__()
        brass, sash, ink = mat("brass", BRASS), mat("sash", SASH), mat("ink", INK)
        part(self, "gorget").scale = (1.25, 1.25, 1.3)
        cube("sash_m", (0.08, 0.5, 0.36), sash, self.body, loc=(0.12, 0, 0.1), rot=(D(20), 0, D(-35)))
        cone("plume", 0.03, 0.003, 0.30, ink, self.head, loc=(-0.16, 0, 0.10), rot=(0, D(-62), 0))
        cone("plume_band", 0.035, 0.035, 0.03, brass, self.head, loc=(-0.10, 0, 0.07), rot=(0, D(-62), 0))
        lance = part(self, "lance_m")
        lance.scale = (1, 1, 1.15)
        part(self, "lance_tip").location = (0, 0, 1.2)
        cube("count_notches", (0.02, 0.02, 0.5), brass, self.lance, loc=(0.02, 0, 0.5))
        self.snapshot()


def halvard_talk(w, i, n):
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.rot("neck", y=5 * s)
    w.rot("head", y=-6 + 5 * math.sin(2 * math.pi * t + 0.7))
    w.rot("lance", y=3 * s)
    w.move("body", z=0.01 * s)


def halvard_lunge(w, i, n):
    k = (0.5, 1.0, 1.0, 0.9)[i]
    w.rot("body", y=32 * k)
    w.move("lance", x=0.55 * k, z=-0.15 * k)
    w.rot("lance", y=8 * k)
    w.rot("neck", y=26 * k)
    w.rot("head", y=-8 * k)
    w.legs(62 * k, -42 * k)
    w.rot("wing", y=-40 * k)
    w.move("body", x=0.1 * k)


def halvard_survey(w, i, n):
    """The lance planted: he marks the floor under her."""
    k = min(1.0, (i + 1) / 3)
    w.rot("lance", y=95 * k)             # the tip to the ground
    w.move("lance", z=-0.25 * k, x=0.1 * k)
    w.rot("body", y=14 * k)
    w.rot("neck", y=18 * k)
    w.rot("head", y=-2 * k)
    w.legs(10 * k, -10 * k)


def halvard_call(w, i, n):
    """The count called: the lance raised high, the head up."""
    k = min(1.0, (i + 1) / 3)
    w.rot("lance", y=-55 * k)
    w.move("lance", z=0.35 * k, x=-0.1 * k)
    w.rot("neck", y=-18 * k)
    w.rot("head", y=14 * k)
    w.rot("body", y=-6 * k)
    w.rot("wing", x=25 * k)


def halvard_count(w, i, n):
    """The slam: every mark erupts."""
    k = (0.6, 1.0, 1.0)[i]
    w.rot("lance", y=-55 + 150 * k)
    w.move("lance", z=0.35 - 0.6 * k, x=0.15 * k)
    w.rot("body", y=-6 + 26 * k)
    w.rot("neck", y=-18 + 34 * k)
    w.legs(20 * k, -14 * k)
    w.rot("wing", x=25 - 45 * k)


def halvard_withdraw(w, i, n):
    """He does not die: at zero he straightens, lowers the lance, and steps back out of the fight."""
    k = min(1.0, i / (n - 1))
    w.rot("lance", y=40 * k)
    w.move("lance", z=-0.1 * k, x=-0.15 * k)
    w.rot("body", y=-4 * k)
    w.rot("neck", y=-8 * k)
    w.rot("head", y=6 * k)
    w.legs(-18 * k, 14 * k)
    w.move("hips", x=-0.15 * k)


HALVARD_CLIPS = [
    ("idle", 12, 4, True, warden_idle),
    ("move", 12, 8, True, warden_move),
    ("walk", 12, 8, True, warden_move),
    ("talk", 12, 6, True, halvard_talk),
    ("measure", 12, 4, False, warden_measure),
    ("telegraph", 12, 3, False, warden_telegraph),
    ("thrust", 24, 3, False, warden_thrust),
    ("lunge", 24, 4, False, halvard_lunge),
    ("survey", 12, 4, False, halvard_survey),
    ("call", 12, 4, False, halvard_call),
    ("count", 24, 3, False, halvard_count),
    ("recover", 12, 3, False, warden_recover),
    ("hurt", 12, 2, False, warden_hurt),
    ("death", 12, 6, False, halvard_withdraw),
]


# ================================================================ Brann: the Cinder Warden (a crane in furnace-blackened brass, twin lances)

BLACK_BRASS = (0.09, 0.08, 0.07)
CINDER = (0.20, 0.16, 0.14)
EMBER = (0.86, 0.42, 0.14)


class Brann(Warden):
    def __init__(self):
        super().__init__()
        recolour(self, "blue", "black_brass", BLACK_BRASS)
        recolour(self, "pale", "cinder", CINDER)
        brass, ember, ink = mat("brass", BRASS), mat("ember", EMBER), mat("ink", INK)
        part(self, "gorget").data.materials[0] = mat("brass_dull", lerp(BRASS, BLACK_BRASS, 0.35))
        sphere("crown", 0.05, ember, self.head, loc=(-0.02, 0, 0.09), scale=(1.4, 0.8, 0.5))
        for i, z in enumerate((0.2, 0.0, -0.2)):
            cube("plate_%d" % i, (0.10, 0.36, 0.06), brass, self.body, loc=(0.16, 0, z), rot=(0, D(-10), 0))
        lance2 = self.add("lance2", self.body, (0.16, 0.18, 0.02), rot=(0, D(78), 0))
        cone("lance2_m", 0.016, 0.012, 1.3, ink, lance2, loc=(0, 0, 0.35))
        cone("lance2_tip", 0.02, 0.0, 0.14, brass, lance2, loc=(0, 0, 1.05))
        cone("lance2_ember", 0.025, 0.02, 0.12, ember, lance2, loc=(0, 0, 0.95))
        cone("lance_ember", 0.025, 0.02, 0.12, ember, self.lance, loc=(0, 0, 0.95))
        # the far lance rests behind the shoulder until phase 2 asks for it
        self.snapshot()


def brann_idle(w, i, n):
    warden_idle(w, i, n)
    w.rot("lance2", y=-60)
    w.move("lance2", z=0.3, x=-0.3)


def brann_move(w, i, n):
    warden_move(w, i, n)
    w.rot("lance2", y=-60)
    w.move("lance2", z=0.3, x=-0.3)


def brann_telegraph(w, i, n):
    warden_telegraph(w, i, n)
    w.rot("lance2", y=-60)
    w.move("lance2", z=0.3, x=-0.3)


def brann_thrust(w, i, n):
    warden_thrust(w, i, n)
    w.rot("lance2", y=-60)
    w.move("lance2", z=0.3, x=-0.3)


def brann_charge(w, i, n):
    k = (0.6, 1.0, 1.0, 0.9)[i]
    s = math.sin(2 * math.pi * i / n)
    w.rot("body", y=34 * k)
    w.move("lance", x=0.5 * k, z=-0.1 * k)
    w.rot("lance", y=6 * k)
    w.rot("lance2", y=-60 + 30 * k)
    w.move("lance2", z=0.3 - 0.1 * k, x=-0.3 + 0.2 * k)
    w.rot("neck", y=30 * k)
    w.legs(45 * s, -45 * s)
    w.rot("wing", y=-45 * k)


def brann_crosscut(w, i, n):
    """Both lances out and sweeping across each other: jump it."""
    k = i / (n - 1)
    w.rot("body", y=10)
    w.rot("lance", y=-50 + 110 * k)
    w.move("lance", z=0.25 - 0.3 * k, x=0.1)
    w.rot("lance2", y=60 - 110 * k)
    w.move("lance2", z=-0.05 + 0.3 * k, x=0.15)
    w.rot("neck", y=8)
    w.legs(15, -15)
    w.rot("wing", y=-25)


def brann_hold(w, i, n):
    """The line he walks forward behind both lances."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.rot("body", y=12)
    w.rot("lance", y=8)
    w.move("lance", x=0.25, z=-0.05)
    w.rot("lance2", y=18)
    w.move("lance2", x=0.25, z=0.1)
    w.legs(18 * s, -18 * s)
    w.move("body", z=0.02 * abs(s))
    w.rot("neck", y=6)


def brann_recover(w, i, n):
    warden_recover(w, i, n)
    k = 1 - (i + 1) / (n + 1)
    w.rot("lance2", y=-60 + 30 * k)
    w.move("lance2", z=0.3, x=-0.3 + 0.2 * k)


def brann_hurt(w, i, n):
    warden_hurt(w, i, n)
    w.rot("lance2", y=-60)
    w.move("lance2", z=0.3, x=-0.3)


def brann_death(w, i, n):
    warden_death(w, i, n)
    t = min(1.0, i / 4)
    w.rot("lance2", y=-60 + 120 * t)
    w.move("lance2", z=0.3 - 0.5 * t, x=-0.3 + 0.2 * t)


BRANN_CLIPS = [
    ("idle", 12, 4, True, brann_idle),
    ("move", 12, 8, True, brann_move),
    ("telegraph", 12, 3, False, brann_telegraph),
    ("thrust", 24, 3, False, brann_thrust),
    ("charge", 24, 4, False, brann_charge),
    ("crosscut", 24, 4, False, brann_crosscut),
    ("hold", 12, 8, True, brann_hold),
    ("recover", 12, 3, False, brann_recover),
    ("hurt", 12, 2, False, brann_hurt),
    ("death", 12, 6, False, brann_death),
]


# ================================================================ Oriel: the captain (an egret; white, a captain's cloak, a short quill-lance)

EGRET = (0.88, 0.88, 0.86)
EGRET_SHADE = (0.80, 0.80, 0.78)
CLOAK = (0.36, 0.40, 0.50)


class Oriel(Warden):
    def __init__(self):
        super().__init__()
        recolour(self, "blue", "egret", EGRET)
        recolour(self, "pale", "egret_shade", EGRET_SHADE)
        brass, cloak, ink = mat("brass", BRASS), mat("cloak", CLOAK), mat("ink", INK)
        for name in ("thigh_l", "thigh_r", "shin_l", "shin_r"):
            p = part(self, name)
            if p is not None:
                p.data.materials[0] = ink
        cone("cloak_m", 0.30, 0.12, 0.55, cloak, self.body, loc=(-0.08, 0, 0.05))
        cube("clasp", (0.05, 0.05, 0.05), brass, self.body, loc=(0.02, -0.18, 0.32))
        # a captain's quill-lance: shorter, no sight
        self.lance.scale = (1, 1, 0.62)
        part(self, "sight").hide_render = True
        self.snapshot()


def oriel_strike1(w, i, n):
    """The mirrored combo's first: a downward cut."""
    k = (0.5, 1.0, 0.9)[i]
    w.rot("lance", y=-60 + 130 * k)
    w.move("lance", x=0.25 * k, z=0.2 - 0.3 * k)
    w.rot("body", y=18 * k)
    w.rot("neck", y=12 * k)
    w.legs(30 * k, -20 * k)


def oriel_strike2(w, i, n):
    """The second: back up across."""
    k = (0.5, 1.0, 0.9)[i]
    w.rot("lance", y=70 - 130 * k)
    w.move("lance", x=0.25 * k, z=-0.1 + 0.3 * k)
    w.rot("body", y=10 * k)
    w.rot("wing", y=-25 * k)
    w.legs(-15 * k, 25 * k)


def oriel_strike3(w, i, n):
    """The third: the thrust."""
    warden_thrust(w, i, n)


def oriel_flourish(w, i, n):
    """A Longstroke: she turns through it, the quill-lance level."""
    k = i / n
    w.rot("body", z=360 * k)
    w.rot("lance", y=10)
    w.move("lance", x=0.3)
    w.rot("wing", y=-40)
    w.legs(20, -20)
    w.move("body", z=0.05 * math.sin(math.pi * k))


def oriel_step(w, i, n):
    k = (0.6, 1.0, 0.7)[i]
    w.rot("body", y=-16 * k)
    w.legs(-40 * k, 45 * k)
    w.rot("lance", y=-20 * k)
    w.rot("neck", y=-10 * k)
    w.move("body", z=0.04 * k)


def oriel_bind(w, i, n):
    """Kneeling, the quill-lance planted: the Bind. Deny it."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    w.move("hips", z=-0.32)
    w.legs(70, 70)
    w.rot("body", y=20)
    w.rot("lance", y=95)
    w.move("lance", z=-0.05, x=0.25)
    w.rot("neck", y=30 + 3 * s)
    w.rot("head", y=-10)
    w.rot("wing", x=15 + 6 * s)


ORIEL_CLIPS = [
    ("idle", 12, 4, True, warden_idle),
    ("move", 12, 8, True, warden_move),
    ("telegraph", 12, 3, False, warden_telegraph),
    ("strike1", 24, 3, False, oriel_strike1),
    ("strike2", 24, 3, False, oriel_strike2),
    ("strike3", 24, 3, False, oriel_strike3),
    ("flourish", 24, 4, False, oriel_flourish),
    ("step", 12, 3, False, oriel_step),
    ("bind", 12, 6, True, oriel_bind),
    ("recover", 12, 3, False, warden_recover),
    ("hurt", 12, 2, False, warden_hurt),
    ("death", 12, 6, False, warden_death),
]


# ================================================================ the generic Wardens B and C (the anchored towns' patrols)

class WardenB(Warden):
    """A Warden in a road-cloak with an iron gorget: the one who has been out in the weather."""
    def __init__(self):
        super().__init__()
        cloak, iron = mat("road_cloak", (0.28, 0.30, 0.36)), mat("iron", IRON)
        part(self, "gorget").data.materials[0] = iron
        cone("cloak_m", 0.30, 0.14, 0.6, cloak, self.body, loc=(-0.06, 0, 0.0))
        cube("cloak_hem", (0.16, 0.34, 0.08), cloak, self.body, loc=(-0.2, 0, -0.28))
        self.snapshot()


class WardenC(Warden):
    """A Warden in a helm with a plume: the one who polishes it."""
    def __init__(self):
        super().__init__()
        iron, plume, brass = mat("iron", IRON), mat("plume", SASH), mat("brass", BRASS)
        cone("helm", 0.13, 0.08, 0.14, iron, self.head, loc=(-0.02, 0, 0.08))
        cube("visor", (0.1, 0.24, 0.03), iron, self.head, loc=(0.1, 0, 0.06))
        cone("helm_plume", 0.035, 0.003, 0.28, plume, self.head, loc=(-0.08, 0, 0.16), rot=(0, D(-45), 0))
        sphere("helm_boss", 0.03, brass, self.head, loc=(0.0, 0, 0.16))
        self.snapshot()


WARDENS = [
    ("Halvard", Halvard, HALVARD_CLIPS, FEET),
    ("Brann", Brann, BRANN_CLIPS, None),
    ("Oriel", Oriel, ORIEL_CLIPS, None),
    ("Warden_B", WardenB, WARDEN_CLIPS, None),
    ("Warden_C", WardenC, WARDEN_CLIPS, None),
]


def main():
    only = argv_after_dashes()
    for name, build, clips, feet in WARDENS:
        if only and name not in only:
            continue
        run(name, CELL, clips, build, 0.0, line=3.0, ink=INK, feet=feet)


if __name__ == "__main__":
    main()

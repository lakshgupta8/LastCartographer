"""The Smudge family and the Cantor family, built on the coast's two rigs (CHR-08, CHR-09). A Smudge is an ink-beast
formed from something forgotten (bible 6), so each region's smudge carries the shape of what it forgot inside its
scribble: the coast's a boat's ribs, the mine's a miner's lamp, the forest's a child's swing, the plateau's a
feather and a stone, and the smudge of Wren's own death her cowl and quill. The Cantors are doves on the coast and in
the forest, crows with cracked bells beyond, and the Choir's three doves in white wool.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/families.py
    ... -- Smudge_Ember ChoirDove          (only those)
    python tools/characters/pack.py smudge_ember smudge_leaf smudge_chalk memorysmudge cantor_crow choirdove

Clip names are the Smudge's and the Cantor's (enemy-animation.md §2); the Choir dove's ring is sought by the
Choir's ring progress like a Cantor's.
"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER
from saltmarrow_enemies import (Smudge, SMUDGE_CLIPS, Cantor, CANTOR_CLIPS, cantor_idle, cantor_ring, cantor_hurt, cantor_death,
                                GREY_INK, BRASS, DOVE, DOVE_DARK)
from wardens import recolour, part

EMBER = (0.86, 0.42, 0.14)


# ================================================================ the Smudges (cell 2.0, the Smudge's clips)

class SmudgeEmber(Smudge):
    """The mine's: ash-black, ember flecks, and the lamp it forgot."""
    def __init__(self):
        super().__init__()
        recolour(self, "dark", "ash", (0.16, 0.12, 0.10))
        recolour(self, "mid", "ash_mid", (0.36, 0.28, 0.22))
        ember, brass, glow = mat("ember", EMBER), mat("brass", BRASS), mat("lampglow", (0.98, 0.80, 0.42))
        rng = random.Random(4)
        for k in range(5):
            sphere("fleck%d" % k, 0.035, ember, self.body, loc=(rng.uniform(-0.4, 0.4), -0.3, rng.uniform(-0.3, 0.4)))
        lamp = self.add("lamp", self.body, (-0.05, -0.32, -0.05))
        cone("lamp_cap", 0.08, 0.04, 0.05, brass, lamp, loc=(0, 0, 0.14))
        cone("lamp_glass", 0.065, 0.07, 0.18, glow, lamp)
        cube("lamp_bar", (0.015, 0.015, 0.2), mat("ink", INK), lamp, loc=(-0.06, -0.06, 0))
        self.snapshot()


class SmudgeLeaf(Smudge):
    """The forest's: green-black, leaves in it, and a child's swing."""
    def __init__(self):
        super().__init__()
        recolour(self, "dark", "leaf_dark", (0.14, 0.18, 0.14))
        recolour(self, "mid", "leaf_mid", (0.30, 0.40, 0.30))
        leaf, rope, wood = mat("leaf", (0.44, 0.56, 0.32)), mat("rope", (0.62, 0.56, 0.44)), mat("wood", (0.46, 0.36, 0.26))
        rng = random.Random(5)
        for k in range(4):
            sphere("leaf%d" % k, 0.09, leaf, self.body, loc=(rng.uniform(-0.4, 0.4), -0.3, rng.uniform(-0.3, 0.4)), scale=(1.4, 0.4, 0.7), rot=(0, D(rng.uniform(-50, 50)), 0))
        swing = self.add("swing", self.body, (0.05, -0.32, 0.0))
        for side in (-1, 1):
            cube("swing_rope%d" % side, (0.015, 0.015, 0.5), rope, swing, loc=(side * 0.14, 0, 0.1))
        cube("swing_seat", (0.36, 0.08, 0.035), wood, swing, loc=(0, 0, -0.15))
        self.snapshot()


class SmudgeChalk(Smudge):
    """The plateau's and the steppe's, and the white beyond: pale, nearly gone; a feather and a stone in it."""
    def __init__(self):
        super().__init__()
        recolour(self, "dark", "chalk", (0.54, 0.54, 0.50))
        recolour(self, "mid", "chalk_mid", (0.70, 0.70, 0.66))
        recolour(self, "paper", "chalk_eye", (0.30, 0.30, 0.34))
        stone, feather = mat("stone", (0.52, 0.52, 0.48)), mat("feather", (0.90, 0.90, 0.86))
        cube("stone_m", (0.2, 0.12, 0.26), stone, self.body, loc=(-0.2, -0.3, -0.15), rot=(0, D(12), 0))
        f = self.add("feather", self.body, (0.15, -0.32, 0.1), rot=(0, D(-35), 0))
        cone("feather_m", 0.07, 0.0, 0.42, feather, f, loc=(0, 0, 0.0))
        cube("feather_shaft", (0.012, 0.012, 0.44), mat("ink", INK), f)
        self.snapshot()


class MemorySmudge(Smudge):
    """The smudge of Wren's own death: blue-black, with her cowl and quill faint inside it."""
    def __init__(self):
        super().__init__()
        recolour(self, "dark", "memory", (0.10, 0.14, 0.30))
        recolour(self, "mid", "memory_mid", (0.22, 0.26, 0.44))
        faint = mat("faint", (0.55, 0.55, 0.62))
        her = self.add("her", self.body, (0.0, -0.33, 0.0))
        cone("cowl", 0.16, 0.08, 0.3, faint, her, loc=(0, 0, 0.15))
        sphere("cowl_face", 0.07, mat("paper", PAPER), her, loc=(0.06, -0.04, 0.12), scale=(1, 0.5, 1))
        cone("quill", 0.015, 0.005, 0.45, faint, her, loc=(0.18, 0, -0.05), rot=(0, D(35), 0))
        self.snapshot()


# ================================================================ the Cantors (cell 2.4, the Cantor's clips)

CROW = (0.14, 0.14, 0.18)
CROW_DARK = (0.26, 0.26, 0.30)
WOOL = (0.96, 0.95, 0.92)
WOOL_DARK = (0.82, 0.80, 0.76)


class CantorCrow(Cantor):
    """The late regions' Cantor: a crow, and its bell is cracked."""
    def __init__(self):
        super().__init__()
        recolour(self, "dove", "crow", CROW)
        recolour(self, "dove_dark", "crow_dark", CROW_DARK)
        part(self, "beak").data.materials[0] = mat("ink", INK)
        part(self, "eye").data.materials[0] = mat("crow_eye", (0.88, 0.86, 0.80))
        part(self, "beak").scale = (1, 1, 1.3)
        slab("crack", [(-0.01, 0.0), (0.03, -0.06), (-0.02, -0.1), (0.02, -0.16)], 0.3, mat("paper", PAPER), self.bell, loc=(0.06, -0.12, -0.02))
        for k in range(3):
            cone("ruff%d" % k, 0.06, 0.0, 0.14, mat("crow_dark", CROW_DARK), self.body, loc=(0.05 + 0.08 * k, -0.2, 0.22 - 0.03 * k), rot=(0, D(-50 + 20 * k), 0))
        self.snapshot()


class ChoirDove(Cantor):
    """One of the Choir's three: a dove in white wool, ringing in turn over Aldermere's square."""
    def __init__(self):
        super().__init__()
        recolour(self, "dove", "wool", WOOL)
        recolour(self, "dove_dark", "wool_dark", WOOL_DARK)
        part(self, "flask_mesh").hide_render = True
        part(self, "flask_neck").hide_render = True
        cone("scarf", 0.17, 0.15, 0.1, mat("scarf", (0.90, 0.82, 0.70)), self.body, loc=(0.08, 0, 0.2))
        cube("scarf_end", (0.06, 0.05, 0.22), mat("scarf", (0.90, 0.82, 0.70)), self.body, loc=(-0.1, -0.2, 0.1), rot=(0, D(-15), 0))
        self.snapshot()


CHOIRDOVE_CLIPS = [
    ("idle", 12, 4, True, cantor_idle),
    ("ring", 12, 6, False, cantor_ring),
    ("hurt", 12, 2, False, cantor_hurt),
    ("death", 12, 4, False, cantor_death),
]


# ================================================================ all of them

# name, cell, builder, clips, line thickness at 2x, ink colour
FAMILIES = [
    ("Smudge_Ember", 2.0, SmudgeEmber, SMUDGE_CLIPS, 3.0, INK),
    ("Smudge_Leaf", 2.0, SmudgeLeaf, SMUDGE_CLIPS, 3.0, INK),
    ("Smudge_Chalk", 2.0, SmudgeChalk, SMUDGE_CLIPS, 2.6, lerp(INK, PAPER, 0.35)),
    ("MemorySmudge", 2.0, MemorySmudge, SMUDGE_CLIPS, 3.0, INK),
    ("Cantor_Crow", 2.4, CantorCrow, CANTOR_CLIPS, 3.0, INK),
    ("ChoirDove", 2.4, ChoirDove, CHOIRDOVE_CLIPS, 3.0, INK),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in FAMILIES:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink)


if __name__ == "__main__":
    main()

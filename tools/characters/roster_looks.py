"""The roster's second batch in two more regions each (CMB-09, enemy-animation.md §2e), built on roster_enemies.py's
three rigs the way families.py builds the Smudges: re-coloured, with a piece of the region on them.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/roster_looks.py
    ... -- Mothcloud_Ash Sketch_Chalk      (only those)
    python tools/characters/pack.py mothcloud_ash mothcloud_dust pulpwasp_cinder pulpwasp_gall sketch_chalk sketch_thin

The moths: the Greyfold's ash-moths (grey, the eye-spots gone white, ash on the wings) and Windreach's dust-moths
(tawny, a grass seed carried). The wasps: Emberdown's cinder-wasp (soot-black bands, the sac an ember) and the
Verdance's gall-wasp (green-brown, the oak gall it was born in on its back). The Sketch: the Blank's in chalk
(the line nearly white, the shade blue) and Lowmarket's thin one (the paint is thinner there: a thinner line,
a paler fill). Clip names are the families' (§2d); the setup picks a look by the room's region.
"""
import os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, Rig, sphere, cone, cube, slab, mat, lerp, run, argv_after_dashes, INK, PAPER
from roster_enemies import Mothcloud, CLOUD_CLIPS, Pulpwasp, WASP_CLIPS, Sketch, SKETCH_CLIPS, GREY_INK
from wardens import recolour


# ================================================================ the moths (cell 1.8, the cloud's clips)

class MothcloudAsh(Mothcloud):
    """The Greyfold's: grey moths, the eye-spots gone white, ash on the wings."""
    def __init__(self):
        super().__init__()
        recolour(self, "moth", "ash_moth", (0.62, 0.60, 0.58))
        recolour(self, "wing", "ash_wing", (0.74, 0.73, 0.70))
        recolour(self, "spot", "ash_spot", (0.94, 0.93, 0.90))
        recolour(self, "dark", "ash_dark", (0.36, 0.35, 0.36))
        ash = mat("ash", (0.48, 0.47, 0.46))
        rng = random.Random(11)
        for k in range(5):
            for j in range(2):
                sphere("ash%d_%d" % (k, j), 0.012, ash, getattr(self, "moth%d" % k), loc=(rng.uniform(-0.04, 0.08), rng.uniform(-0.05, 0.05), rng.uniform(0.02, 0.07)))
        self.snapshot()


class MothcloudDust(Mothcloud):
    """Windreach's: tawny moths of the long grass, one carrying a grass seed."""
    def __init__(self):
        super().__init__()
        recolour(self, "moth", "dust_moth", (0.72, 0.58, 0.38))
        recolour(self, "wing", "dust_wing", (0.84, 0.74, 0.54))
        recolour(self, "spot", "dust_spot", (0.42, 0.30, 0.20))
        seed, stalk = mat("seed", (0.80, 0.70, 0.46)), mat("stalk", (0.62, 0.52, 0.32))
        for k in (1, 3):
            m = getattr(self, "moth%d" % k)
            cone("seed_stalk%d" % k, 0.004, 0.003, 0.14, stalk, m, loc=(-0.06, 0, -0.03), rot=(0, D(-70), 0))
            sphere("seed%d" % k, 0.018, seed, m, loc=(-0.11, 0, -0.07), scale=(1.8, 0.8, 0.8), rot=(0, D(-20), 0))
        self.snapshot()


# ================================================================ the wasps (cell 1.6, the wasp's clips)

class PulpwaspCinder(Pulpwasp):
    """Emberdown's: soot-black bands on ash, the sac an ember, the wings smoked."""
    def __init__(self):
        super().__init__()
        recolour(self, "buff", "cinder_buff", (0.46, 0.40, 0.36))
        recolour(self, "band", "cinder_band", (0.12, 0.10, 0.10))
        recolour(self, "wing", "cinder_wing", (0.58, 0.54, 0.50))
        recolour(self, "pulp", "cinder_pulp", (0.96, 0.46, 0.12))
        ember = mat("ember_fleck", (0.96, 0.60, 0.20))
        rng = random.Random(12)
        for k in range(4):
            sphere("fleck%d" % k, 0.014, ember, self.abdomen, loc=(rng.uniform(-0.26, 0.0), rng.uniform(-0.1, 0.1), rng.uniform(0.04, 0.12)))
        self.snapshot()


class PulpwaspGall(Pulpwasp):
    """The Verdance's: green-brown, and the oak gall it was born in rides its back."""
    def __init__(self):
        super().__init__()
        recolour(self, "buff", "gall_buff", (0.56, 0.58, 0.36))
        recolour(self, "band", "gall_band", (0.26, 0.24, 0.14))
        recolour(self, "wing", "gall_wing", (0.80, 0.84, 0.70))
        recolour(self, "pulp", "gall_pulp", (0.82, 0.80, 0.56))
        gall = mat("gall", (0.70, 0.56, 0.34))
        sphere("gall_m", 0.085, gall, self.body, loc=(-0.02, 0, 0.14), scale=(1.0, 0.9, 0.9))
        for k in range(3):
            cone("gall_spike%d" % k, 0.012, 0.001, 0.06, gall, self.body, loc=(-0.02 + 0.04 * (k - 1), 0, 0.21), rot=(0, D(-30 + 30 * k), 0))
        self.snapshot()


# ================================================================ the Sketches (cell 2.0, the Sketch's clips)

class SketchChalk(Sketch):
    """The Blank's: chalk on white, the shade gone blue."""
    def __init__(self):
        super().__init__()
        recolour(self, "sketch", "chalk_sketch", (0.96, 0.96, 0.95))
        recolour(self, "shade", "chalk_shade", (0.72, 0.76, 0.84))
        recolour(self, "ink", "chalk_ink", (0.52, 0.56, 0.66))
        self.snapshot()


class SketchThin(Sketch):
    """Lowmarket's: the paint is thinner there, and so is everything else; a paler fill under a thinner line."""
    def __init__(self):
        super().__init__()
        recolour(self, "sketch", "thin_sketch", (0.93, 0.91, 0.87))
        recolour(self, "shade", "thin_shade", (0.84, 0.83, 0.80))
        self.snapshot()


# ================================================================ all of them

# name, cell, builder, clips, line thickness at 2x, ink colour
LOOKS = [
    ("Mothcloud_Ash", 1.8, MothcloudAsh, CLOUD_CLIPS, 2.2, lerp(INK, PAPER, 0.3)),
    ("Mothcloud_Dust", 1.8, MothcloudDust, CLOUD_CLIPS, 2.4, INK),
    ("Pulpwasp_Cinder", 1.6, PulpwaspCinder, WASP_CLIPS, 2.8, INK),
    ("Pulpwasp_Gall", 1.6, PulpwaspGall, WASP_CLIPS, 2.8, INK),
    ("Sketch_Chalk", 2.0, SketchChalk, SKETCH_CLIPS, 2.4, lerp(INK, PAPER, 0.6)),
    ("Sketch_Thin", 2.0, SketchThin, SKETCH_CLIPS, 1.8, lerp(INK, PAPER, 0.55)),
]


def main():
    only = argv_after_dashes()
    for name, cell, build, clips, line, ink in LOOKS:
        if only and name not in only:
            continue
        run(name, cell, clips, build, 0.0, line=line, ink=ink)


if __name__ == "__main__":
    main()

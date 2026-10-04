"""Wren large, for the key art and the capsules (ENV-13): the same rig and ink as her sheets, rendered at poster size.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/marketing/wren_hero.py

Two poses from her own clips: `glide` (side on, the wings out, over the Wind Gate in the key art) and `survey` (turned
toward us, the lens up, standing on the capsules). Writes logs/capture/hero_<pose>.png on transparency, 2400 px square.
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "characters"))
from inklib import D, reset_scene, setup_render, render, INK, ROOT
import wren

SIZE = 2400
LINE = 6.0        # about the sheets' weight at poster size (Freestyle's nib and taper do not scale in step)
OUT = os.path.join(ROOT, "logs", "capture")

# (name, clip, frame, yaw): the camera turned toward her face for the standing pose
POSES = [
    ("glide", "glide", 2, 0.0),
    ("survey", "survey", 3, D(30)),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    clips = {c[0]: c for c in wren.CLIPS}
    for name, clip, frame, yaw in POSES:
        reset_scene()
        rig = wren.Wren("Surveyor")
        rig.reset()
        _, fps, n, loop, pose = clips[clip]
        pose(rig, frame, n)
        setup_render(SIZE, wren.CELL * 1.1, wren.CELL / 2 - 0.06, yaw=yaw, line=LINE, ink=INK)
        render(os.path.join(OUT, "hero_%s.png" % name))
        print("[hero] %s: %s frame %d" % (name, clip, frame))


if __name__ == "__main__":
    main()

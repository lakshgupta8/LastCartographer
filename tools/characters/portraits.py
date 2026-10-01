"""Portraits for dialogue (CHR-13): every bird who speaks, head and shoulders, from the drawing they are met in.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/portraits.py
    ... -- Sable Hask                    (only those speakers)
    python tools/characters/portraits_pack.py

No new birds: each portrait builds the rig its speaker already walks the world on (the cast's bird in cast.py, the
Warden rig of wardens.py and bosses.py, a look from the townsfolk library for a minor named bird or one who asks),
at rest, and frames the head from three-quarters in front, turned toward the line on the page's right. Two frames:
`rest`, and `talk` with the beak open mid-word (the Warden rig has one bill, so the portrait gives it a lower half to
open). Rendered at twice the portrait's size into .frames/portraits/<Speaker>_<frame>.png; portraits_pack.py
downsamples them, adds the Remnant's grey (the shader's colour state, worked on the pixels) and writes the strips.

The list is SPEAKERS below and again in Unity (`Portraits.Faces`, Core), which the tests compare with the pack.
"""
import json, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, cone, empty, mat, reset_scene, setup_render, render, argv_after_dashes, FRAMES_ROOT, INK
import cast
import townsfolk
import wardens
import bosses
from saltmarrow_enemies import Warden

SIZE = 512          # rendered px; packed at half (art-direction 4: authored at 2x)
YAW = D(48)         # the camera swung toward her face: the beak points right, at the line
LINE = 3.0          # px at SIZE, so the packed line matches the sheets' weight at the portrait's scale
OUT = os.path.join(FRAMES_ROOT, "portraits")


def look(name):
    for lid, region, cell, spec in townsfolk.LOOKS:
        if lid == name:
            return spec
    raise KeyError(name)


def cast_spec(name):
    for n, cell, spec, extras, clips, line, ink in cast.CAST:
        if n == name:
            return spec, extras
    raise KeyError(name)


def bird(name):
    spec, extras = cast_spec(name)
    return lambda: cast.Townsfolk(spec, extras)


def folk(look_id):
    spec = look(look_id)
    return lambda: cast.Townsfolk(spec, townsfolk.marks)


# (speaker, the body the face is drawn from, how to build it)
SPEAKERS = [
    # the returning cast (cast.py); Marrow has no portrait (character-bibles.md §5)
    ("Sable", "Sable", bird("Sable")),
    ("Dotha", "Dotha", bird("Dotha")),
    ("Isolde", "Isolde", bird("Isolde")),
    ("Pell", "Pell", bird("Pell")),
    ("Runa", "Runa", bird("Runa")),
    ("Kettil", "Kettil", bird("Kettil")),
    ("Teodor", "Teodor", bird("Teodor")),
    ("Idrenne", "Idrenne", bird("Idrenne")),
    ("Maren", "Maren", bird("Maren")),
    ("Corvin", "Corvin", bird("Corvin")),
    ("Ilse", "Ilse", bird("Ilse")),
    ("Corra", "Corra", bird("Corra")),
    ("Aury", "Aury", bird("Aury")),
    # the Guild's birds on the Warden rig (wardens.py, bosses.py)
    ("Halvard", "Halvard", wardens.Halvard),
    ("Brann", "Brann", wardens.Brann),
    ("Oriel", "Oriel", wardens.Oriel),
    ("Warden", "Warden", Warden),
    ("Hale", "Hale", bosses.Hale),
    ("Voss", "Voss", bosses.Voss),
    # the minor named birds, in the look each wears (Townsfolk.Named)
    ("Hask", "Folk_Chough", folk("Chough")),
    ("Ostry", "Folk_Rook", folk("Rook")),
    ("Wend", "Folk_Finch", folk("Finch")),
    ("Tobin", "Folk_Woodpecker", folk("Woodpecker")),
    ("Ansel", "Folk_Owlet", folk("Owlet")),
    ("Hollin", "Folk_Jay", folk("Jay")),
    ("Arden", "Folk_Goose", folk("Goose")),
    ("Brisk", "Folk_Starling", folk("Starling")),
    ("Anvers", "Folk_Rook", folk("Rook")),
    ("Tam", "Folk_Sparrow", folk("Sparrow")),
    ("Keeper", "Folk_Magpie", folk("Magpie")),
    ("Innkeeper", "Folk_Nuthatch", folk("Nuthatch")),
    # the birds who ask, and the speakers the arcs name a species for but the greybox has not stood up yet
    ("Gannet", "Folk_Gannet", folk("Gannet")),
    ("Traveller", "Folk_Thrush", folk("Thrush")),
    ("Brek", "Folk_Crane", folk("Crane")),
    ("Ossa", "Folk_Plover", folk("Plover")),        # a child of the clan at the third fire (windreach-arc.md)
    ("Lorne", "Folk_Crane", folk("Crane")),         # the Guild's surveyor at the baths, a crane (emberdown-arc.md)
    ("Brask", "Folk_Chough", folk("Chough")),       # the Hollowvein's miner on his island (blank-islands.md)
]


def head_of(rig):
    """The head's centre and its size, in world units, from the world bounds of the head's mesh."""
    from mathutils import Vector
    bpy.context.view_layer.update()
    meshes = [c for c in rig.head.children if c.type == "MESH" and not c.hide_render]
    core = next((c for c in meshes if c.name.startswith("head_m")), meshes[0])
    pts = [core.matrix_world @ Vector(c) for c in core.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return (lo + hi) / 2, max(hi.x - lo.x, hi.z - lo.z)


def jaw_for_warden(rig):
    """The Warden rig's bill is one cone: give it a lower half, hinged at the base, so the portrait can open it."""
    ink = mat("ink", INK)
    jaw = empty("jaw", rig.head, (0.09, 0, -0.03))
    rig.parts.append(jaw)
    rig.jaw = jaw
    cone("bill_low", 0.026, 0.003, 0.28, ink, jaw, loc=(0.14, 0, -0.008), rot=(0, D(92), 0), scale=(1, 0.8, 0.6))
    rig.snapshot()


def talk(rig):
    """Mid-word: the beak open, the head a touch up and toward the line."""
    rig.rot("jaw", y=18)
    rig.rot("head", y=-5)


def frame(rig, size):
    centre, head = head_of(rig)
    span = head * 2.7                 # the head, the beak, the neck and the top of the shoulders
    cz = centre.z - head * 0.20
    across = head * 0.22              # the head a little left of centre, so the beak has room toward the line
    setup_render(SIZE, span, cz, yaw=YAW, line=LINE, ink=INK)
    cam = bpy.data.objects["Cam"]
    # setup_render looks through the origin; slide the camera across to the head, keeping the angle
    cam.location = (centre.x + 10 * math.sin(YAW) + across * math.cos(YAW),
                    centre.y - 10 * math.cos(YAW) + across * math.sin(YAW), cz)


def main():
    only = argv_after_dashes()
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "speakers.json"), "w") as f:
        json.dump([{"speaker": speaker, "body": body} for speaker, body, build in SPEAKERS], f, indent=2)
    for speaker, body, build in SPEAKERS:
        if only and speaker not in only:
            continue
        reset_scene()
        rig = build()
        if not hasattr(rig, "jaw"):
            jaw_for_warden(rig)
        rig.reset()
        frame(rig, SIZE)
        render(os.path.join(OUT, "%s_rest.png" % speaker))
        rig.reset()
        talk(rig)
        render(os.path.join(OUT, "%s_talk.png" % speaker))
        print("[portraits] %s (from %s)" % (speaker, body))


if __name__ == "__main__":
    main()

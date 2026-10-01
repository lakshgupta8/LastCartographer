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

Where two speakers wear the same look, one of them has a touch of their own in the portrait (TOUCHES): Brask a
miner's helmet and lamp, Lorne a grown crane's grey and spectacles; Garrow is the crane grown old. (Ostry, Anvers, Hollin and Wend are drawn as
themselves in townsfolk.py OWN, so their portraits are their own birds.)

The list is SPEAKERS below and again in Unity (`Portraits.Faces`, Core), which the tests compare with the pack.
"""
import json, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, cone, cube, sphere, empty, mat, reset_scene, setup_render, render, argv_after_dashes, FRAMES_ROOT, INK
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


# ---------------------------------------------------------------- the touches that tell a shared look apart

GUILD_BRASS = (0.78, 0.62, 0.30)


def ring(name, major, minor, material, parent, loc, rot):
    """A torus (a spectacle's rim) on a parent, in its space."""
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=28, minor_segments=8)
    ob = bpy.context.active_object
    ob.name = name
    ob.data.materials.append(material)
    ob.parent = parent
    ob.location = loc
    ob.rotation_euler = rot
    return ob


def touch_brask(b):
    """A Hollowvein miner, buried with his shift: the leather helmet and its lamp."""
    hr = b.spec["head_r"]
    leather, brass, glass = cast.m((0.32, 0.25, 0.18)), cast.m(GUILD_BRASS), cast.m((0.95, 0.88, 0.58))
    sphere("helmet", hr * 1.02, leather, b.head, loc=(-hr * 0.06, 0, hr * 0.36), scale=(1.06, 1.06, 0.62))
    cone("helmet_brim", hr * 1.12, hr * 1.12, 0.014, leather, b.head, loc=(-hr * 0.06, 0, hr * 0.22))
    cone("lamp_body", hr * 0.20, hr * 0.25, hr * 0.22, brass, b.head, loc=(hr * 0.92, 0, hr * 0.60), rot=(0, D(62), 0))
    sphere("lamp_glass", hr * 0.15, glass, b.head, loc=(hr * 1.04, 0, hr * 0.66))


def touch_lorne(b):
    """The Guild's surveyor at the baths: careful, correct, spectacles on a brass wire."""
    s = b.spec
    hr, er = s["head_r"], s["eye_r"]
    brass = cast.m(GUILD_BRASS)
    ex, ey, ez = hr * 0.45, -hr * 0.86, hr * 0.2          # cast.Townsfolk's eye
    ring("spectacle", er * 1.55, 0.0055, brass, b.head, (ex, ey - 0.012, ez), (D(90), 0, 0))
    cube("spectacle_arm", (hr * 0.85, 0.006, 0.008), brass, b.head, loc=(ex - er * 1.55 - hr * 0.42, ey + 0.004, ez + 0.004))
    cube("spectacle_bridge", (er * 1.1, 0.006, 0.008), brass, b.head, loc=(ex + er * 1.55 + er * 0.45, ey + 0.01, ez - 0.004))


TOUCHES = {"Brask": touch_brask, "Lorne": touch_lorne}

# Lorne is a grown crane, where Brek is a young one, tawny before the grey: grey, the black throat, the red crown.
LORNE = dict(body=(0.60, 0.62, 0.64), dark=(0.38, 0.40, 0.44), cap=(0.80, 0.18, 0.14), bib=(0.12, 0.12, 0.14), beak=(0.52, 0.50, 0.42))


def own(name):
    for n, cell, spec, extras in townsfolk.OWN:
        if n == name:
            return lambda: cast.Townsfolk(spec, extras)
    raise KeyError(name)


# Garrow is an old crane, who failed the leap forty-one years ago: ash-pale, stooped, the crown faded, the eye milky.
GARROW = dict(body=(0.80, 0.80, 0.78), dark=(0.58, 0.58, 0.56), cap=(0.66, 0.42, 0.38), beak=(0.62, 0.58, 0.48),
              eye=(0.72, 0.70, 0.60), stoop=14, neck_lean=32)


def folk_as(look_id, **changes):
    spec = dict(look(look_id), **changes)
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
    ("Tobin", "Folk_Woodpecker", folk("Woodpecker")),
    ("Ansel", "Folk_Owlet", folk("Owlet")),
    ("Arden", "Folk_Goose", folk("Goose")),
    ("Brisk", "Folk_Starling", folk("Starling")),
    ("Tam", "Folk_Sparrow", folk("Sparrow")),
    ("Keeper", "Folk_Magpie", folk("Magpie")),
    ("Innkeeper", "Folk_Nuthatch", folk("Nuthatch")),
    # the minor named birds drawn as themselves (townsfolk.py OWN): the nightjar, the heron, the thrush, the dove
    ("Ostry", "Ostry", own("Ostry")),
    ("Anvers", "Anvers", own("Anvers")),
    ("Hollin", "Hollin", own("Hollin")),
    ("Wend", "Wend", own("Wend")),
    # the birds who ask, and the speakers the arcs name a species for but the greybox has not stood up yet
    ("Gannet", "Folk_Gannet", folk("Gannet")),
    ("Traveller", "Folk_Thrush", folk("Thrush")),
    ("Brek", "Folk_Crane", folk("Crane")),
    ("Ossa", "Folk_Plover", folk("Plover")),        # a child of the clan at the third fire (windreach-arc.md)
    ("Lorne", "Folk_Crane", folk_as("Crane", **LORNE)),   # the Guild's surveyor at the baths, a crane (emberdown-arc.md)
    ("Garrow", "Folk_Crane", folk_as("Crane", **GARROW)), # the old crane at the third fire (windreach-arc.md)
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
        if speaker in TOUCHES:
            TOUCHES[speaker](rig)
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

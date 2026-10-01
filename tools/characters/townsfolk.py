"""The townsfolk library (CHR-12): thirty generic birds, six for each living region, on the same parametric townsfolk
bird as the returning cast (cast.py), so a crowd, a watcher, a picket line or a bird who asks is a drawing and not a
block. Each look is a spec of sizes, colours and the marks that name its species (a cap, wingtips, bars, speckles, a
bib, a crest, a comb, a facial disc, a bare face, a bound wing).

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/townsfolk.py
    ... -- Folk_Gull Folk_Eider                 (only those)
    python tools/characters/pack.py folk_gull folk_tern ... (or `all`)

Six clips each, at 12 fps, all looping: the hub's four (idle, talk, walk, asleep: cast.py's, shared) and the crowd's
two: `watching` (the head up and tracking something overhead: a leap, a line) and `cheering` (wings up and beating,
the beak open, a bounce: the clan at the Gate, the picket on its crate). Feet at the origin, facing +X; the setup
mirrors the quad to face the other way. The Remnant grey is not drawn: NpcInk washes a look at run time
(docs/design/npc-animation.md §4), so the gannet on the faded rail and the inn's traveller are these sheets, greyed.

The library is listed again in Unity (`Townsfolk.Looks`, Core), which the tests compare to what is packed here.
"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from inklib import D, sphere, cone, cube, run, argv_after_dashes, INK, PAPER
from cast import Townsfolk, COMMON, m

PREFIX = "Folk_"
WHITE = (0.92, 0.91, 0.88)
BLACK = (0.08, 0.08, 0.10)
BLACK_DARK = (0.05, 0.05, 0.07)
SOFT_EYE = (0.30, 0.30, 0.32)   # a black bird's eye, so it reads against the body


# ---------------------------------------------------------------- the marks that name a species

def marks(b, s):
    """Extras driven by keys in the spec, so one function dresses every look."""
    hr, br = s["head_r"], s["body_r"]
    sx, sy, sz = s["body_scale"]
    wl = s["wing_len"]
    L = br * 0.62 * wl                     # the wing ellipsoid's half-length along -X
    wing_loc = (-br * 0.2, 0.0, -br * 0.3)   # where cast.py centres the wing on its empty

    if s.get("cap"):     # a coloured crown (a tern's black cap, a woodpecker's red, a sparrow's grey)
        sphere("cap", hr * 0.86, m(s["cap"]), b.head, loc=(-hr * 0.08, 0, hr * 0.40), scale=(1.0, 1.0, 0.62))
    if s.get("brow"):    # a ptarmigan's red eyebrow
        sphere("brow", hr * 0.22, m(s["brow"]), b.head, loc=(hr * 0.38, -hr * 0.80, hr * 0.46), scale=(1.5, 0.5, 0.6))
    if s.get("disc"):    # a facial disc (the owlet), or the pale face (the puffin)
        sphere("disc", hr * 0.95, m(s["disc"]), b.head, loc=(hr * 0.28, 0, hr * 0.02), scale=(0.72, 1.0, 1.0))
    if s.get("face"):    # a rook's bare grey face at the bill's base
        sphere("face", hr * 0.36, m(s["face"]), b.head, loc=(hr * 0.74, 0, -hr * 0.04), scale=(1.0, 1.25, 0.85))
    if s.get("stripe"):  # a stripe through the eye (the nuthatch), or down from it (a kestrel's moustache)
        cube("stripe", (hr * 1.5, hr * 0.12, hr * 0.18), m(s["stripe"]), b.head, loc=(hr * 0.22, -hr * 0.92, hr * 0.18))
    if s.get("band"):    # the puffin's bill band
        cone("band", s["beak_r"] * 1.08, s["beak_r"] * 0.95, s["beak_len"] * 0.28, m(s["band"]), b.head,
             loc=(hr * 0.85 + s["beak_len"] * 0.22, 0, -hr * 0.05), rot=(0, D(90), 0))
    if s.get("comb"):    # a grouse's red comb
        sphere("comb", hr * 0.30, m(s["comb"]), b.head, loc=(hr * 0.36, -hr * 0.84, hr * 0.62), scale=(1.2, 0.5, 0.6))
    if s.get("fan_crest"):   # a hoopoe's fan of feathers, tipped dark
        col, n = s["fan_crest"]
        for k in range(n):
            ang = -75 + 32 * k
            cone("fan%d" % k, hr * 0.11, 0.004, hr * 1.0, m(col), b.head, loc=(-hr * 0.42 + k * hr * 0.16, 0, hr * 0.78), rot=(0, D(ang), 0))
            sphere("fan_tip%d" % k, hr * 0.09, m(s["dark"]), b.head,
                   loc=(-hr * 0.42 + k * hr * 0.16 + hr * 0.95 * math.sin(D(ang)), 0, hr * 0.78 + hr * 0.95 * math.cos(D(ang))))
    if s.get("tuft"):    # a raven's shaggy throat
        for k, ang in enumerate((100, 125, 150)):
            cone("tuft%d" % k, hr * 0.12, 0.004, hr * 0.5, m(s["tuft"]), b.neck, loc=(s["neck_r"] * 0.6, -s["neck_r"] * 0.3, -0.01), rot=(0, D(ang), 0))
    if s.get("bib"):     # a throat patch (a dipper's white, a sparrow's black, a jay's moustache)
        sphere("bib", s["neck_r"] * 1.55, m(s["bib"]), b.neck, loc=(s["neck_r"] * 0.35, 0, 0.0), scale=(1.0, 0.85, 1.25))
    if s.get("crescent"):    # the ring ouzel's white crescent across the breast
        cube("crescent", (br * 0.46, br * 0.34, br * 0.09), m(s["crescent"]), b.body, loc=(br * 0.46 * sx, -br * 0.52 * sy, br * 0.08), rot=(0, D(20), 0))
    if s.get("speckles"):    # spots on the near flank: a thrush's, a starling's, an owl's pale ones
        col = m(s["speckles"])
        for k, (nx, nz) in enumerate(((0.32, 0.22), (0.48, -0.08), (0.18, -0.30), (0.02, 0.06), (-0.16, -0.24), (0.12, 0.42), (-0.30, 0.10), (0.36, -0.36))):
            ny = -math.sqrt(max(0.0, 1.0 - nx * nx - nz * nz))
            sphere("speckle%d" % k, br * 0.055, col, b.body, loc=(br * sx * nx, br * sy * ny, br * sz * nz))
    if s.get("tips"):    # dark wingtips (a gull, a gannet, a tern)
        for w in (b.wing_near, b.wing_far):
            sphere("tip_" + w.name, br * 0.17, m(s["tips"]), w,
                   loc=(wing_loc[0] - L * 0.95 * math.cos(D(18)), wing_loc[1], wing_loc[2] - L * 0.95 * math.sin(D(18))), scale=(1.5, 1.0, 0.8))
    if s.get("bars"):    # bars across the wing (a pigeon's two, a finch's white, a jay's blue patch, a bustard's dark)
        col = m(s["bars"])
        for w in (b.wing_near, b.wing_far):
            for k in range(2):
                cube("bar%d_" % k + w.name, (br * 0.08, br * 0.62, br * 0.44), col, w,
                     loc=(wing_loc[0] - L * (0.25 + 0.35 * k) * math.cos(D(18)), wing_loc[1], wing_loc[2] - L * (0.25 + 0.35 * k) * math.sin(D(18))), rot=(0, D(-18), 0))
    if s.get("shoulder"):    # a magpie's white shoulder
        cube("shoulder", (br * 0.34, br * 0.62, br * 0.30), m(s["shoulder"]), b.wing_near,
             loc=(wing_loc[0] + L * 0.1, wing_loc[1], wing_loc[2] + L * 0.05), rot=(0, D(-18), 0))
    if s.get("shawl"):   # an elder's shawl over the back
        shawl = m(s["shawl"])
        cone("shawl", br * 1.2, br * 0.5, br * 1.1, shawl, b.body, loc=(-br * 0.12, 0, br * 0.5))
        cube("shawl_fringe", (br * 0.3, br * 1.0, br * 0.3), shawl, b.body, loc=(-br * 0.95, 0, br * 0.05))
    if s.get("cord"):    # the wings bound: two cords round the body over them, a knot on the near side
        cord = m(s["cord"])
        for k in range(2):
            cube("cord%d" % k, (br * 0.07, br * 2.25 * sy, br * 0.07), cord, b.body, loc=(-br * 0.25 + k * br * 0.4, 0, br * 0.05 + k * br * 0.28), rot=(0, D(8), 0))
        sphere("knot", br * 0.1, cord, b.body, loc=(-br * 0.05, -br * 1.12 * sy, br * 0.2))


# ---------------------------------------------------------------- the crowd's clips

def watching(b, i, n):
    """The head up, following something overhead: a lean back, the neck up, the head tracking; a blink."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    b.rot("body", y=-6)
    b.rot("neck", y=-28)
    b.rot("head", y=-14 + 8 * s)
    b.move("body", z=0.01 * s)
    b.rot("tail", y=-4)
    b.rot("wing_near", y=2 * s)
    b.blink(1.0 if i == n - 1 else 0.0)


def cheering(b, i, n):
    """Wings up and beating, the beak open, a bounce."""
    t = i / n
    s = math.sin(2 * math.pi * t)
    hop = abs(math.sin(2 * math.pi * t))
    b.move("body", z=0.06 * hop)
    b.rot("body", y=-4)
    b.rot("wing_near", y=70 + 16 * s)
    b.rot("wing_far", y=70 - 16 * s)
    b.rot("neck", y=-16)
    b.rot("head", y=-10 + 5 * s)
    b.open_beak(12 + 10 * hop)
    b.legs(-6 * hop, -6 * hop)
    b.rot("tail", y=-8 + 4 * s)


CLIPS = COMMON + [
    ("watching", 12, 8, True, watching),
    ("cheering", 12, 8, True, cheering),
]


# ---------------------------------------------------------------- the looks: (id, region, cell, spec)

LOOKS = [
    # ---- Saltmarrow: the coast's
    ("Gull", "Saltmarrow", 2.0, dict(body_r=0.31, body=(0.86, 0.86, 0.82), dark=(0.50, 0.52, 0.54), beak=(0.82, 0.66, 0.22), leg=(0.80, 0.60, 0.40),
                                     beak_len=0.18, beak_r=0.035, tips=BLACK)),
    ("Tern", "Saltmarrow", 2.0, dict(body_r=0.26, leg_len=0.22, body=(0.90, 0.90, 0.88), dark=(0.70, 0.72, 0.74), beak=(0.85, 0.25, 0.10), leg=(0.85, 0.25, 0.10),
                                     beak_len=0.17, beak_r=0.03, cap=(0.06, 0.06, 0.08), tail_len=0.34, tail_w=0.08, tail_angle=-14, tips=(0.20, 0.20, 0.22))),
    ("Gannet", "Saltmarrow", 2.0, dict(body_r=0.34, neck_len=0.20, neck_r=0.07, body=(0.92, 0.90, 0.84), dark=(0.86, 0.84, 0.76), beak=(0.60, 0.64, 0.62), leg=(0.25, 0.25, 0.28),
                                       beak_len=0.30, beak_r=0.035, cap=(0.86, 0.72, 0.42), tips=BLACK, eye=(0.70, 0.80, 0.84))),
    ("Puffin", "Saltmarrow", 2.0, dict(body_r=0.28, leg_len=0.22, stoop=4, body=BLACK, dark=(0.06, 0.06, 0.08), breast=WHITE, breast_r=0.78,
                                       beak=(0.90, 0.45, 0.12), beak_r=0.055, beak_len=0.14, band=(0.35, 0.40, 0.45), leg=(0.92, 0.50, 0.12), disc=(0.84, 0.84, 0.82), eye_r=0.03)),
    # the grandmother on the stilts: her wings bound by a cord, a shawl
    ("Eider", "Saltmarrow", 2.0, dict(body_r=0.34, body_scale=(1.2, 0.9, 0.9), leg_len=0.22, stoop=10, neck_len=0.06, body=(0.46, 0.36, 0.28), dark=(0.30, 0.24, 0.18),
                                      beak=(0.45, 0.48, 0.40), beak_len=0.20, beak_r=0.045, speckles=(0.22, 0.18, 0.14), cord=(0.55, 0.45, 0.30), shawl=(0.40, 0.36, 0.40))),
    ("Turnstone", "Saltmarrow", 1.6, dict(body_r=0.22, head_r=0.12, leg_len=0.20, body=(0.48, 0.32, 0.22), dark=(0.14, 0.12, 0.12), breast=WHITE, breast_r=0.72,
                                          beak=(0.12, 0.12, 0.14), beak_len=0.12, leg=(0.90, 0.50, 0.15), bib=(0.12, 0.12, 0.14))),
    # ---- Emberdown: the highland's
    ("Chough", "Emberdown", 2.0, dict(body_r=0.29, body=(0.10, 0.10, 0.12), dark=(0.06, 0.06, 0.08), beak=(0.85, 0.22, 0.10), beak_curve=12, beak_len=0.18, beak_r=0.03,
                                      leg=(0.85, 0.22, 0.10), eye=SOFT_EYE)),
    ("Raven", "Emberdown", 2.4, dict(body_r=0.36, head_r=0.16, leg_len=0.34, body=BLACK, dark=BLACK_DARK, beak=(0.10, 0.10, 0.12), beak_len=0.22, beak_r=0.045,
                                     tuft=BLACK, eye=SOFT_EYE)),
    ("Ptarmigan", "Emberdown", 2.0, dict(body_r=0.30, body_scale=(1.1, 0.9, 0.95), neck_len=0.04, leg_len=0.22, leg_r=0.035, body=(0.84, 0.82, 0.80), dark=(0.62, 0.60, 0.58),
                                         beak=(0.15, 0.14, 0.14), brow=(0.85, 0.15, 0.10), leg=(0.80, 0.78, 0.76))),
    ("Dipper", "Emberdown", 2.0, dict(body_r=0.26, leg_len=0.24, tail_len=0.12, body=(0.30, 0.24, 0.20), dark=(0.18, 0.15, 0.14), breast=WHITE, breast_r=0.6,
                                      bib=WHITE, beak=(0.20, 0.18, 0.16), leg=(0.25, 0.20, 0.18))),
    ("Ouzel", "Emberdown", 2.0, dict(body_r=0.27, body=(0.10, 0.10, 0.12), dark=(0.07, 0.07, 0.09), beak=(0.80, 0.70, 0.30), crescent=WHITE,
                                     leg=(0.25, 0.22, 0.20), eye=SOFT_EYE)),
    # the counter under the roosts
    ("Grouse", "Emberdown", 2.0, dict(body_r=0.30, body_scale=(1.1, 0.9, 0.95), neck_len=0.05, leg_r=0.03, body=(0.46, 0.28, 0.18), dark=(0.30, 0.18, 0.12),
                                      beak=(0.20, 0.16, 0.14), comb=(0.85, 0.15, 0.10), leg=(0.70, 0.66, 0.62), speckles=(0.26, 0.16, 0.10))),
    # ---- the Verdance: the forest's
    # the inn's traveller (a Remnant, greyed at run time)
    ("Thrush", "Verdance", 2.0, dict(body_r=0.27, body=(0.56, 0.44, 0.30), dark=(0.42, 0.32, 0.22), breast=(0.90, 0.86, 0.72), breast_r=0.74,
                                     speckles=(0.30, 0.22, 0.14), beak=(0.40, 0.32, 0.24), leg=(0.70, 0.56, 0.42))),
    ("Woodpecker", "Verdance", 2.0, dict(body_r=0.28, tail_angle=-40, body=(0.42, 0.56, 0.26), dark=(0.30, 0.42, 0.18), cap=(0.85, 0.15, 0.10),
                                         beak=(0.30, 0.30, 0.30), beak_len=0.22, beak_r=0.03, breast=(0.76, 0.80, 0.56), breast_r=0.7, leg=(0.35, 0.36, 0.30))),
    ("Finch", "Verdance", 1.6, dict(body_r=0.22, head_r=0.12, leg_len=0.22, body=(0.62, 0.48, 0.36), dark=(0.30, 0.30, 0.34), breast=(0.80, 0.50, 0.42), breast_r=0.74,
                                    cap=(0.42, 0.46, 0.56), bars=WHITE, beak=(0.42, 0.40, 0.38), beak_len=0.10, leg=(0.55, 0.45, 0.38))),
    ("Jay", "Verdance", 2.0, dict(body_r=0.29, tail_len=0.32, body=(0.74, 0.56, 0.46), dark=(0.30, 0.28, 0.30), beak=(0.12, 0.12, 0.14),
                                  bars=(0.30, 0.50, 0.85), bib=(0.12, 0.12, 0.14), leg=(0.60, 0.50, 0.42))),
    ("Nuthatch", "Verdance", 1.6, dict(body_r=0.24, head_r=0.12, leg_len=0.20, tail_len=0.12, body=(0.44, 0.54, 0.66), dark=(0.36, 0.44, 0.56), breast=(0.86, 0.60, 0.34), breast_r=0.74,
                                       stripe=(0.08, 0.08, 0.10), beak=(0.30, 0.30, 0.32), beak_len=0.18, beak_r=0.03)),
    # Brother Ansel on his page
    ("Owlet", "Verdance", 1.6, dict(body_r=0.26, body_scale=(1.0, 0.95, 1.1), head_r=0.17, head_scale=(1.0, 1.0, 0.95), neck_len=0.02, leg_len=0.18, leg_r=0.03, tail_len=0.10,
                                    body=(0.56, 0.48, 0.38), dark=(0.42, 0.36, 0.28), speckles=(0.90, 0.86, 0.76), disc=(0.84, 0.78, 0.66), eye_r=0.046,
                                    beak=(0.30, 0.28, 0.26), beak_len=0.07, beak_curve=30, leg=(0.80, 0.74, 0.66))),
    # ---- Halden: the Plateau's
    ("Pigeon", "Halden", 2.0, dict(body_r=0.27, body=(0.52, 0.56, 0.64), dark=(0.30, 0.34, 0.44), breast=(0.50, 0.36, 0.46), breast_r=0.7,
                                   bars=(0.12, 0.12, 0.16), beak=(0.30, 0.28, 0.28), leg=(0.80, 0.30, 0.30))),
    # the millworkers
    ("Starling", "Halden", 2.0, dict(body_r=0.25, tail_len=0.12, body=(0.14, 0.14, 0.18), dark=(0.10, 0.10, 0.14), speckles=(0.90, 0.88, 0.80),
                                     beak=(0.85, 0.75, 0.25), leg=(0.70, 0.40, 0.36), eye=SOFT_EYE)),
    # the Guild's clerks
    ("Rook", "Halden", 2.0, dict(body_r=0.32, body=BLACK, dark=BLACK_DARK, beak=(0.30, 0.30, 0.32), beak_len=0.22, beak_r=0.04,
                                 face=(0.70, 0.68, 0.64), leg=(0.10, 0.10, 0.12), eye=SOFT_EYE)),
    ("Sparrow", "Halden", 1.6, dict(body_r=0.20, head_r=0.11, leg_len=0.18, body=(0.56, 0.42, 0.30), dark=(0.36, 0.26, 0.18), cap=(0.46, 0.42, 0.40), bib=(0.10, 0.10, 0.10),
                                    breast=(0.74, 0.70, 0.62), breast_r=0.7, beak=(0.20, 0.18, 0.16), beak_len=0.08, leg=(0.60, 0.48, 0.40))),
    ("Goose", "Halden", 2.6, dict(body_r=0.36, body_scale=(1.2, 0.85, 0.95), head_r=0.12, head_scale=(1.2, 0.9, 0.9), neck_len=0.36, neck_r=0.06, neck_lean=10, tail_len=0.18,
                                  leg_r=0.03, foot=0.22, body=(0.62, 0.58, 0.52), dark=(0.48, 0.44, 0.38), beak=(0.90, 0.50, 0.20), beak_len=0.20, beak_r=0.05, leg=(0.90, 0.55, 0.30))),
    ("Magpie", "Halden", 2.0, dict(body_r=0.27, tail_len=0.46, tail_w=0.07, tail_angle=-10, body=(0.10, 0.10, 0.14), dark=(0.12, 0.16, 0.26), breast=WHITE, breast_r=0.74,
                                   shoulder=WHITE, beak=(0.10, 0.10, 0.12), leg=(0.10, 0.10, 0.12), eye=SOFT_EYE)),
    # ---- Windreach: the clans'
    ("Lark", "Windreach", 1.6, dict(body_r=0.24, head_r=0.12, leg_len=0.22, body=(0.62, 0.52, 0.36), dark=(0.44, 0.36, 0.24), breast=(0.84, 0.78, 0.64), breast_r=0.72,
                                    crest=(0.035, 0.11, -30), speckles=(0.36, 0.28, 0.18), beak=(0.42, 0.36, 0.28), leg=(0.70, 0.60, 0.46))),
    # the clan's singer
    ("Hoopoe", "Windreach", 2.0, dict(body_r=0.27, body=(0.80, 0.56, 0.34), dark=(0.12, 0.12, 0.14), bars=WHITE, fan_crest=((0.80, 0.56, 0.34), 5),
                                      beak=(0.20, 0.18, 0.16), beak_len=0.30, beak_r=0.025, beak_curve=10, leg=(0.40, 0.38, 0.34))),
    ("Kestrel", "Windreach", 2.0, dict(body_r=0.27, tail_len=0.30, body=(0.62, 0.38, 0.22), dark=(0.50, 0.28, 0.16), cap=(0.52, 0.56, 0.62), bars=(0.14, 0.12, 0.12),
                                       breast=(0.84, 0.72, 0.56), breast_r=0.7, beak=(0.30, 0.30, 0.32), beak_curve=35, beak_len=0.10, stripe=(0.14, 0.12, 0.12), leg=(0.85, 0.70, 0.25))),
    ("Bustard", "Windreach", 2.6, dict(body_r=0.38, body_scale=(1.2, 0.85, 0.95), head_r=0.13, neck_len=0.30, neck_r=0.07, neck_lean=12, leg_len=0.50, leg_r=0.03,
                                       body=(0.66, 0.52, 0.34), dark=(0.46, 0.34, 0.22), bars=(0.14, 0.12, 0.12), cap=(0.52, 0.54, 0.56), beak=(0.50, 0.46, 0.40), beak_len=0.16, beak_r=0.04,
                                       leg=(0.60, 0.56, 0.46))),
    ("Plover", "Windreach", 1.6, dict(body_r=0.22, head_r=0.12, leg_len=0.24, body=(0.70, 0.58, 0.26), dark=(0.22, 0.20, 0.14), speckles=(0.14, 0.12, 0.10),
                                      breast=(0.16, 0.14, 0.14), breast_r=0.7, beak=(0.14, 0.12, 0.12), beak_len=0.10, leg=(0.30, 0.28, 0.26))),
    # Brek: a young crane, tawny before the grey
    ("Crane", "Windreach", 2.4, dict(body_r=0.28, body_scale=(1.15, 0.8, 0.95), head_r=0.10, head_scale=(1.2, 0.9, 0.9), neck_len=0.36, neck_r=0.045, neck_lean=14,
                                     leg_len=0.50, leg_r=0.016, foot=0.14, tail_len=0.22, body=(0.74, 0.62, 0.44), dark=(0.50, 0.42, 0.30), beak=(0.40, 0.36, 0.28), beak_len=0.18,
                                     leg=(0.30, 0.28, 0.26), eye=(0.60, 0.50, 0.20))),
]


def main():
    only = argv_after_dashes()
    for look, region, cell, spec in LOOKS:
        name = PREFIX + look
        if only and name not in only and look not in only:
            continue
        run(name, cell, CLIPS, lambda spec=spec: Townsfolk(spec, marks), cell / 2, line=3.0, ink=INK)


if __name__ == "__main__":
    main()

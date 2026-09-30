"""The ink effects, rendered in Blender (ENV-12): one-shot clips of ink doing what ink does when the game hits,
swings, scribbles, binds, erases and breaks. Packed as the character "Fx" so InkFx can spawn them at run time.

    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P tools/characters/fx.py
    ... -P tools/characters/fx.py -- splash blot          (only those clips)
    python tools/characters/pack.py fx

Every clip is centred on its own origin (the point the game spawns it at) and faces +X where it has a direction;
InkFx rotates the quad for the strike's angle. Frames are built fresh each time from the same seed, so a clip is
a function of time like the characters' clips, only in geometry rather than in a rig's pose. 24 fps throughout:
ink moves fast and dries slow, so the last frames hold the soak.
"""
import json, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from inklib import D, sphere, cone, cube, slab, mat, lerp, reset_scene, setup_render, render, argv_after_dashes, INK, PAPER, PPU, SCALE, FRAMES_ROOT

ERASER = (0.86, 0.80, 0.70)
WET = (0.80, 0.78, 0.74)


def m(rgb):
    return mat("c_%.2f_%.2f_%.2f" % rgb, rgb)


def wash(k):
    """Ink soaking into paper: from ink toward paper by k."""
    return lerp(INK, PAPER, k)


def blob(name, x, z, r, rgb, y=0.0, squash=1.0, n=14, seed=0):
    """A round of ink with a slightly irregular edge: a scaled sphere with a wobble."""
    rng = random.Random(seed)
    ob = sphere(name, r, m(rgb), None, loc=(x, y, z), scale=(1.0 + rng.uniform(-0.12, 0.12), 0.35, squash * (1.0 + rng.uniform(-0.12, 0.12))))
    ob.rotation_euler = (0, rng.uniform(0, math.pi), 0)
    return ob


def stroke(name, x0, z0, x1, z1, thickness, rgb, y=0.0, taper=0.5):
    """A stroke of ink from one point to another: a cone lying along the line, thicker at the start."""
    dx, dz = x1 - x0, z1 - z0
    length = math.hypot(dx, dz)
    ang = math.atan2(dz, dx)
    ob = cone(name, thickness, thickness * taper, length, m(rgb), None, loc=((x0 + x1) / 2, y, (z0 + z1) / 2), rot=(0, D(90) - ang, 0))
    ob.scale = (1, 0.4, 1)
    return ob


def drop(name, x, z, r, rgb, vx, vz, y=0.0):
    """A flying drop: a teardrop pointing along its velocity."""
    ang = math.atan2(vz, vx)
    ob = cone(name, r, 0.0, r * 3.2, m(rgb), None, loc=(x, y, z), rot=(0, D(90) - ang + math.pi, 0))
    ob.scale = (1, 0.4, 1)
    return ob


# ---------------------------------------------------------------- the clips: (name, cell units, frames, build(i, n, rng))

def fx_splash(i, n, rng):
    """A landed hit: drops fly forward and out, then the ink soaks in where they land."""
    t = i / (n - 1)
    for k in range(9):
        ang = rng.uniform(-1.1, 1.1)
        speed = rng.uniform(0.5, 1.2)
        r = rng.uniform(0.03, 0.07)
        dist = 0.9 * speed * min(1.0, t * 1.6)
        vx, vz = math.cos(ang), math.sin(ang) - 0.9 * t
        x, z = dist * math.cos(ang), dist * math.sin(ang) - 0.35 * (t * speed) ** 2
        if t < 0.7:
            drop("drop_%d" % k, x, z, r * (1 - 0.4 * t), wash(0.1 * t), vx, vz)
        else:
            blob("soak_%d" % k, x, z, r * (1.6 + 1.5 * (t - 0.7)), wash(0.35 + 0.6 * (t - 0.7) / 0.3), squash=0.6, seed=k)
    blob("core", 0.05, 0.0, 0.22 * (1 - 0.6 * t) + 0.02, wash(0.15 + 0.7 * t), squash=0.8, seed=99)


def fx_slash(i, n, rng):
    """The swing: a calligraphic arc drawing itself in the strike direction, thick at the middle."""
    t = (i + 1) / n
    pts = 18
    r = 0.9
    for k in range(int(pts * t)):
        a0 = math.radians(-50 + 100 * k / pts)
        a1 = math.radians(-50 + 100 * (k + 1) / pts)
        thick = 0.03 + 0.11 * math.sin(math.pi * k / pts) * (1.0 - 0.5 * max(0.0, t - 0.7) / 0.3)
        stroke("arc_%d" % k, r * math.cos(a0) - 0.3, r * math.sin(a0), r * math.cos(a1) - 0.3, r * math.sin(a1), thick, wash(0.1 + 0.5 * max(0.0, t - 0.7) / 0.3), taper=1.0)


def fx_crosshatch(i, n, rng):
    """The Crosshatch: pen ticks laid quickly one way, then the other."""
    t = (i + 1) / n
    ticks = 10
    drawn = int(ticks * min(1.0, t * 1.2))
    for k in range(drawn):
        first = k < ticks // 2
        x = -0.6 + 1.2 * ((k % (ticks // 2)) / (ticks // 2 - 1)) + rng.uniform(-0.05, 0.05)
        if first:
            stroke("tick_%d" % k, x - 0.25, -0.45, x + 0.25, 0.45, 0.035, wash(0.05), taper=0.6)
        else:
            stroke("tick_%d" % k, x - 0.25, 0.45, x + 0.25, -0.45, 0.035, wash(0.05), taper=0.6)
    if t > 0.8:
        k = (t - 0.8) / 0.2
        for j in range(6):
            blob("spray_%d" % j, rng.uniform(-0.8, 0.8), rng.uniform(-0.5, 0.5), 0.02 + 0.03 * k, wash(0.3 + 0.5 * k), seed=j)


def fx_longstroke(i, n, rng):
    """The Longstroke: one long line from the origin out along +X, dry-brushed at its end."""
    t = (i + 1) / n
    reach = 1.4
    end = reach * min(1.0, t * 1.3)
    stroke("stroke", -0.2, 0.0, end, 0.02, 0.09, wash(0.05), taper=0.3)
    if end >= reach * 0.95:
        for j in range(5):
            stroke("dry_%d" % j, reach - 0.1, rng.uniform(-0.05, 0.05), reach + rng.uniform(0.05, 0.25), rng.uniform(-0.1, 0.1), 0.015, wash(0.2), taper=0.2)
    if t > 0.75:
        k = (t - 0.75) / 0.25
        for j in range(4):
            blob("fleck_%d" % j, reach + rng.uniform(-0.1, 0.3), rng.uniform(-0.25, 0.25), 0.02 + 0.02 * k, wash(0.3 + 0.5 * k), seed=j)


def fx_blot(i, n, rng):
    """The Blot: a round of ink spreading from the origin, drips running down, then soaking pale."""
    t = i / (n - 1)
    r = 0.25 + 0.75 * min(1.0, t * 1.5)
    blob("blot", 0, 0, r, wash(0.05 + 0.5 * max(0.0, t - 0.6) / 0.4), squash=0.9, seed=1)
    for j in range(5):
        x = -0.5 + 0.25 * j + rng.uniform(-0.05, 0.05)
        length = 0.15 + 0.5 * t * rng.uniform(0.5, 1.0)
        stroke("drip_%d" % j, x, -r * 0.6, x + rng.uniform(-0.03, 0.03), -r * 0.6 - length, 0.03, wash(0.1 + 0.4 * t), taper=0.4)
    if t > 0.3:
        for j in range(8):
            ang = rng.uniform(0, 2 * math.pi)
            d = r + 0.15 + 0.35 * (t - 0.3)
            blob("spatter_%d" % j, d * math.cos(ang), d * math.sin(ang) * 0.8, rng.uniform(0.02, 0.05), wash(0.2 + 0.5 * t), seed=j)


def fx_redraw(i, n, rng):
    """The Bind: a pen line tracing Wren's outline round, then the whole outline flaring and settling."""
    t = i / (n - 1)
    pts = 28
    r = 0.62
    drawn = int(pts * min(1.0, t * 1.4))
    for k in range(drawn):
        a0 = 2 * math.pi * k / pts - math.pi / 2
        a1 = 2 * math.pi * (k + 1) / pts - math.pi / 2
        wob = 1.0 + 0.06 * math.sin(3 * a0)
        flare = 1.0 + 0.35 * math.sin(math.pi * max(0.0, t - 0.7) / 0.3) if t > 0.7 else 1.0
        stroke("line_%d" % k, r * wob * math.cos(a0), r * 1.35 * wob * math.sin(a0) + 0.1, r * wob * math.cos(a1), r * 1.35 * wob * math.sin(a1) + 0.1,
               0.04 * flare, wash(0.0 if t < 0.85 else 0.6 * (t - 0.85) / 0.15), taper=1.0)
    if t > 0.6:
        k = (t - 0.6) / 0.4
        for j in range(6):
            ang = 2 * math.pi * j / 6 + rng.uniform(-0.3, 0.3)
            d = r * 1.3 + 0.4 * k
            blob("spark_%d" % j, d * math.cos(ang), d * 1.2 * math.sin(ang) + 0.1, 0.03 * (1 - 0.6 * k), wash(0.1 + 0.6 * k), seed=j)


def fx_eraser(i, n, rng):
    """Erasure: an eraser dragged across the drawing, shavings curling off, paper showing through behind it."""
    t = i / (n - 1)
    x = -1.2 + 2.4 * min(1.0, t * 1.15)
    cube("rubber", (0.5, 0.2, 0.28), m(ERASER), None, loc=(x, 0.0, 0.05), rot=(0, D(-12), 0))
    cube("rubber_edge", (0.5, 0.2, 0.06), m(lerp(ERASER, INK, 0.25)), None, loc=(x - 0.03, -0.02, -0.1), rot=(0, D(-12), 0))
    for j in range(7):
        sx = x - 0.3 - rng.uniform(0.0, 1.2) * t
        sz = rng.uniform(-0.25, 0.25) - 0.4 * t
        ob = cone("shaving_%d" % j, 0.02, 0.012, rng.uniform(0.08, 0.2), m(lerp(ERASER, PAPER, 0.2)), None, loc=(sx, -0.02, sz), rot=(0, rng.uniform(0, math.pi), 0))
        ob.scale = (1, 0.4, 1)
    # what it has rubbed out: a pale smear behind it
    if t > 0.1:
        cube("smear", ((x + 1.3), 0.12, 0.34), m(lerp(PAPER, WET, 0.4)), None, loc=((x - 1.3) / 2, 0.08, -0.02))


def fx_crumble(i, n, rng):
    """A weak floor giving way: plank pieces dropping and turning, ink dust hanging then settling."""
    t = i / (n - 1)
    for j in range(6):
        x = -0.8 + 0.32 * j + rng.uniform(-0.05, 0.05)
        vz = rng.uniform(0.2, 0.6)
        z = 0.1 + vz * t * 1.5 - 2.2 * t * t
        ang = rng.uniform(-1.0, 1.0) * t * 2.5
        cube("piece_%d" % j, (0.28, 0.16, 0.08), m(lerp((0.60, 0.36, 0.24), (0.64, 0.66, 0.64), 0.5)), None, loc=(x, 0.0, z), rot=(0, ang, 0))
    for j in range(10):
        blob("dust_%d" % j, rng.uniform(-1.0, 1.0), 0.15 + rng.uniform(0.0, 0.35) * (1 + t) - 0.3 * t * t, 0.02 + 0.03 * min(1.0, t * 2), wash(0.35 + 0.55 * t), seed=j)


def fx_mark(i, n, rng):
    """Halvard's survey mark: a scribbled square on the floor that will erupt. Two frames, looping: the ink wet."""
    for k in range(4):
        x0, x1 = -0.75 + 0.1 * rng.random(), 0.75 - 0.1 * rng.random()
        z0 = -0.12 + 0.08 * k + (0.02 if i == 1 else 0.0) * (1 if k % 2 else -1)
        stroke("hatch_%d" % k, x0, z0, x1, z0 + rng.uniform(-0.03, 0.03), 0.03, wash(0.05 + 0.1 * i), taper=0.7)
    stroke("edge_a", -0.8, -0.16, 0.8, -0.16, 0.035, wash(0.0), taper=1.0)
    stroke("edge_b", -0.8, 0.16, 0.8, 0.16, 0.035, wash(0.0), taper=1.0)


def fx_erupt(i, n, rng):
    """The count called: a mark erupting: a column of ink rising from the floor, then falling as drops."""
    t = i / (n - 1)
    h = 2.2 * min(1.0, t * 1.6)
    cube("column", (1.2 * (1 - 0.3 * t), 0.2, h), m(wash(0.05 + 0.4 * max(0.0, t - 0.6) / 0.4)), None, loc=(0, 0, -1.1 + h / 2))
    for j in range(8):
        x = rng.uniform(-0.7, 0.7)
        z = -1.1 + h + rng.uniform(0.0, 0.5) - 1.5 * max(0.0, t - 0.5) ** 2 * 4
        drop("drop_%d" % j, x, z, rng.uniform(0.03, 0.06), wash(0.1), x, 0.5 - 2 * t)


# name, cell (units), frames, loop, build
CLIPS = [
    ("splash", 2.0, 6, False, fx_splash),
    ("slash", 2.4, 4, False, fx_slash),
    ("crosshatch", 2.4, 6, False, fx_crosshatch),
    ("longstroke", 3.0, 5, False, fx_longstroke),
    ("blot", 2.4, 6, False, fx_blot),
    ("redraw", 2.4, 8, False, fx_redraw),
    ("eraser", 3.0, 6, False, fx_eraser),
    ("crumble", 2.4, 6, False, fx_crumble),
    ("mark", 2.0, 2, True, fx_mark),
    ("erupt", 2.4, 6, False, fx_erupt),
]
FPS = 24
CELL = 3.0   # every clip renders into the same cell so one sheet player serves them all


def main():
    only = argv_after_dashes()
    frames = os.path.join(FRAMES_ROOT, "fx")
    os.makedirs(frames, exist_ok=True)
    meta_path = os.path.join(frames, "clips.json")
    meta = {"character": "Fx", "ppu": PPU, "scale": SCALE, "cell": CELL, "clips": []}
    if only and os.path.exists(meta_path):
        with open(meta_path) as f:
            meta = json.load(f)
    res = int(CELL * PPU * SCALE)
    for name, _, n, loop, build in CLIPS:
        if only and name not in only:
            continue
        seed = sum(ord(c) for c in name)
        for i in range(n):
            reset_scene()
            build(i, n, random.Random(seed))
            setup_render(res, CELL, 0.0, line=2.4, ink=INK)
            render(os.path.join(frames, "%s_%02d.png" % (name, i)))
        print("[fx] %s: %d frames at %d fps" % (name, n, FPS))
        meta["clips"] = [c for c in meta["clips"] if c["name"] != name] + [{"name": name, "fps": FPS, "frames": n, "loop": loop}]
    order = [c[0] for c in CLIPS]
    meta["clips"].sort(key=lambda c: order.index(c["name"]) if c["name"] in order else 99)
    with open(meta_path, "w") as f:
        json.dump(meta, f, indent=2)


if __name__ == "__main__":
    main()

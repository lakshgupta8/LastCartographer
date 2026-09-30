"""Pack a character's rendered frames into sprite sheets (CHR-03), and its turnaround into the model sheet.

    python tools/characters/pack.py wren

Reads tools/characters/.frames/<name>/clips.json and the frames wren.py rendered at 2x, downsamples each to the
game's density with premultiplied alpha (no dark fringe on the ink line), and writes one horizontal strip per
clip, <Name>_<clip>.png, plus <name>.json for the Unity setup. The turnaround goes to docs/art/<name>-turnaround.png.
"""
import json, os, sys
from PIL import Image, ImageDraw

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))


def premultiplied_resize(im, size):
    im = im.convert("RGBA")
    r, g, b, a = im.split()
    pre = Image.merge("RGBA", (r.point(lambda v: v), g, b, a))
    px = pre.load()
    w, h = pre.size
    for y in range(h):
        for x in range(w):
            R, G, B, A = px[x, y]
            if A < 255:
                k = A / 255.0
                px[x, y] = (int(R * k), int(G * k), int(B * k), A)
    small = pre.resize(size, Image.LANCZOS)
    px = small.load()
    w, h = small.size
    for y in range(h):
        for x in range(w):
            R, G, B, A = px[x, y]
            if 0 < A < 255:
                px[x, y] = (min(255, int(R * 255 / A)), min(255, int(G * 255 / A)), min(255, int(B * 255 / A)), A)
    return small


def pack(name):
    folder = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Characters", name.capitalize())
    frames = os.path.join(ROOT, "tools", "characters", ".frames", name)
    os.makedirs(folder, exist_ok=True)
    with open(os.path.join(frames, "clips.json")) as f:
        meta = json.load(f)
    cell = int(meta["cell"] * meta["ppu"])
    out = {"character": meta["character"], "ppu": meta["ppu"], "cell": cell, "clips": []}
    for clip in meta["clips"]:
        n = clip["frames"]
        sheet = Image.new("RGBA", (cell * n, cell), (0, 0, 0, 0))
        for i in range(n):
            im = Image.open(os.path.join(frames, "%s_%02d.png" % (clip["name"], i)))
            sheet.alpha_composite(premultiplied_resize(im, (cell, cell)), (i * cell, 0))
        file = "%s_%s.png" % (meta["character"], clip["name"])
        sheet.save(os.path.join(folder, file))
        out["clips"].append({"name": clip["name"], "fps": clip["fps"], "frames": n, "loop": clip["loop"], "file": file})
        print("[pack] %s: %d frames -> %s (%dx%d)" % (clip["name"], n, file, sheet.width, sheet.height))
    with open(os.path.join(folder, name + ".json"), "w") as f:
        json.dump(out, f, indent=2)

    # The model sheet: four views at 2x on paper, with a one-unit scale bar.
    views = [("side", "side"), ("three_quarter", "three-quarter"), ("front", "front"), ("back", "back")]
    if all(os.path.exists(os.path.join(frames, "turnaround_%s.png" % v)) for v, _ in views):
        paper = (237, 227, 204, 255)
        size = cell * 2
        sheet = Image.new("RGBA", (size * len(views) + 40, size + 90), paper)
        draw = ImageDraw.Draw(sheet)
        for k, (v, label) in enumerate(views):
            im = Image.open(os.path.join(frames, "turnaround_%s.png" % v)).convert("RGBA").resize((size, size), Image.LANCZOS)
            sheet.alpha_composite(im, (20 + k * size, 20))
            draw.text((20 + k * size + 8, size + 30), label, fill=(20, 26, 40, 255))
        unit = meta["ppu"] * 2
        x0, y = 28, size + 62
        draw.line((x0, y, x0 + unit, y), fill=(20, 26, 40, 255), width=3)
        draw.text((x0 + unit + 8, y - 7), "1 unit  (Wren stands 1.2)", fill=(20, 26, 40, 255))
        docs = os.path.join(ROOT, "docs", "art")
        os.makedirs(docs, exist_ok=True)
        path = os.path.join(docs, "%s-turnaround.png" % name)
        sheet.save(path)
        print("[pack] model sheet -> " + path)


if __name__ == "__main__":
    pack(sys.argv[1] if len(sys.argv) > 1 else "wren")

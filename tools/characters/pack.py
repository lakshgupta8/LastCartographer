"""Pack a character's rendered frames into sprite sheets (CHR-03, CHR-06), and its turnaround into a model sheet.

    python tools/characters/pack.py wren
    python tools/characters/pack.py marshcrab reedskimmer smudge cantor warden lostremnant lampkeeper
    python tools/characters/pack.py all

Reads tools/characters/.frames/<name>/clips.json and the frames rendered at 2x, downsamples each to the game's
density with premultiplied alpha (no dark fringe on the ink line), and writes one horizontal strip per clip,
<Character>_<clip>.png, plus <character lower>.json for the Unity setup, under Assets/_Project/Art/Characters/
<Character>/. The turnaround goes to docs/art/<character lower>-turnaround.png.
"""
import json, os, sys
from PIL import Image, ImageDraw

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES_ROOT = os.path.join(ROOT, "tools", "characters", ".frames")


def premultiplied_resize(im, size):
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            R, G, B, A = px[x, y]
            if A < 255:
                k = A / 255.0
                px[x, y] = (int(R * k), int(G * k), int(B * k), A)
    small = im.resize(size, Image.LANCZOS)
    px = small.load()
    w, h = small.size
    for y in range(h):
        for x in range(w):
            R, G, B, A = px[x, y]
            if 0 < A < 255:
                px[x, y] = (min(255, int(R * 255 / A)), min(255, int(G * 255 / A)), min(255, int(B * 255 / A)), A)
    return small


def pack(name):
    frames = os.path.join(FRAMES_ROOT, name.lower())
    with open(os.path.join(frames, "clips.json")) as f:
        meta = json.load(f)
    character = meta["character"]
    folder = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Characters", character)
    os.makedirs(folder, exist_ok=True)
    cell = int(round(meta["cell"] * meta["ppu"]))
    out = {"character": character, "ppu": meta["ppu"], "cell": cell, "cellUnits": meta["cell"], "clips": []}
    for clip in meta["clips"]:
        n = clip["frames"]
        sheet = Image.new("RGBA", (cell * n, cell), (0, 0, 0, 0))
        for i in range(n):
            im = Image.open(os.path.join(frames, "%s_%02d.png" % (clip["name"], i)))
            sheet.alpha_composite(premultiplied_resize(im, (cell, cell)), (i * cell, 0))
        file = "%s_%s.png" % (character, clip["name"])
        sheet.save(os.path.join(folder, file))
        out["clips"].append({"name": clip["name"], "fps": clip["fps"], "frames": n, "loop": clip["loop"], "file": file})
        print("[pack] %s %s: %d frames -> %s (%dx%d)" % (character, clip["name"], n, file, sheet.width, sheet.height))
    with open(os.path.join(folder, character.lower() + ".json"), "w") as f:
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
        draw.text((x0 + unit + 8, y - 7), "1 unit", fill=(20, 26, 40, 255))
        docs = os.path.join(ROOT, "docs", "art")
        os.makedirs(docs, exist_ok=True)
        path = os.path.join(docs, "%s-turnaround.png" % character.lower())
        sheet.save(path)
        print("[pack] model sheet -> " + path)


if __name__ == "__main__":
    names = sys.argv[1:] or ["wren"]
    if names == ["all"]:
        names = sorted(d for d in os.listdir(FRAMES_ROOT) if os.path.exists(os.path.join(FRAMES_ROOT, d, "clips.json")))
    for n in names:
        pack(n)

"""Pack the UI's drawings (ENV-11): downsample the 2x renders from tools/ui/.frames/ to the game's density with
premultiplied alpha (no dark fringe on the ink line) into Assets/_Project/Art/UI/Resources/UI/, copy ui.json beside
them for InkArt, and lay every piece on one paper sheet for the docs, docs/art/ui-sheet.png.

    python tools/ui/pack.py
"""
import json, os, shutil, sys
from PIL import Image, ImageDraw

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES = os.path.join(ROOT, "tools", "ui", ".frames")
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "UI", "Resources", "UI")
DOCS = os.path.join(ROOT, "docs", "art")
sys.path.insert(0, os.path.join(ROOT, "tools", "characters"))
from pack import premultiplied_resize   # noqa: E402

PAPER = (237, 227, 204, 255)
INK = (20, 26, 40, 255)


def main():
    with open(os.path.join(FRAMES, "ui.json")) as f:
        manifest = json.load(f)
    os.makedirs(OUT, exist_ok=True)
    packed = []
    for piece in manifest["pieces"]:
        src = os.path.join(FRAMES, piece["name"] + ".png")
        if not os.path.exists(src):
            print("[ui] %s: not rendered, skipped" % piece["name"])
            continue
        im = Image.open(src)
        small = premultiplied_resize(im, (piece["w"], piece["h"]))
        small.save(os.path.join(OUT, piece["name"] + ".png"))
        packed.append(piece)
        print("[ui] %s -> %dx%d" % (piece["name"], piece["w"], piece["h"]))
    with open(os.path.join(OUT, "ui.json"), "w") as f:
        json.dump({"pieces": packed}, f, indent=2)

    # The sheet for the docs: the paper pieces across the top, the glyphs in rows below, each named.
    os.makedirs(DOCS, exist_ok=True)
    big = [p for p in packed if p["w"] >= 500]
    small = [p for p in packed if p["w"] < 500]
    pad, label = 24, 18
    width = 1600
    rows = []   # (y, [(piece, x, scale)])
    y = pad
    for p in big:
        scale = min(1.0, (width - 2 * pad) / p["w"])
        rows.append((y, [(p, pad, scale)]))
        y += int(p["h"] * scale) + label + pad
    x, row = pad, []
    tallest = 0
    for p in small:
        cell_w = max(p["w"], 96) + pad
        if x + cell_w > width - pad:
            rows.append((y, row)); y += tallest + label + pad
            x, row, tallest = pad, [], 0
        row.append((p, x, 1.0)); x += cell_w; tallest = max(tallest, p["h"])
    if row:
        rows.append((y, row)); y += tallest + label + pad
    sheet = Image.new("RGBA", (width, y), PAPER)
    draw = ImageDraw.Draw(sheet)
    for ry, items in rows:
        for p, x, scale in items:
            im = Image.open(os.path.join(OUT, p["name"] + ".png")).convert("RGBA")
            if scale != 1.0:
                im = im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS)
            sheet.alpha_composite(im, (x, ry))
            draw.text((x, ry + im.height + 2), p["name"].replace("UI_", ""), fill=INK)
    sheet.save(os.path.join(DOCS, "ui-sheet.png"))
    print("[ui] docs/art/ui-sheet.png (%dx%d), %d pieces" % (sheet.width, sheet.height, len(packed)))


if __name__ == "__main__":
    main()

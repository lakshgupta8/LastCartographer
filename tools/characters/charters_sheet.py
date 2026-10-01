"""Wren in each of her six Charters side by side (CHR-05): the model sheet for the silhouettes.

    python tools/characters/charters_sheet.py

One row per Charter: the side and three-quarter views of its turnaround (rendered by wren.py into .frames), then a
few frames from its packed sheets (standing, running, the first swing landing, the glide, its own Flourish), so the
cowl and the grip can be read at rest and in motion. Writes docs/art/wren-charters.png.
"""
import json, os
from PIL import Image, ImageDraw

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES_ROOT = os.path.join(ROOT, "tools", "characters", ".frames")
ART = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Characters")
# (Charter, the folder its sheets are in, its default Flourish: combat doc 5, late-charters.md)
CHARTERS = [("Surveyor", "Wren", "crosshatch"), ("Warden", "Wren_Warden", "blot"), ("Drifter", "Wren_Drifter", "longstroke"),
            ("Ferryman", "Wren_Ferryman", "longstroke"), ("Unwriter", "Wren_Unwriter", "blot"), ("Remnant", "Wren_Remnant", "crosshatch")]
PAPER = (237, 227, 204, 255)
INK = (20, 26, 40, 255)
VIEW = 240     # each turnaround view, px
CELL = 192     # each packed frame, px (the game's density)


def frame(folder, clip, i):
    path = os.path.join(ART, folder, "%s_%s.png" % (folder, clip))
    if not os.path.exists(path):
        return None
    im = Image.open(path).convert("RGBA")
    i = min(i, im.width // im.height - 1)
    return im.crop((i * im.height, 0, (i + 1) * im.height, im.height)).resize((CELL, CELL), Image.LANCZOS)


def main():
    picks = [("idle", 0), ("run", 2), ("strike1", 2), ("glide", 0), (None, 0)]
    width = 130 + 2 * VIEW + len(picks) * CELL + 20
    height = len(CHARTERS) * (VIEW + 10) + 40
    sheet = Image.new("RGBA", (width, height), PAPER)
    draw = ImageDraw.Draw(sheet)
    for k, label in enumerate(["side", "three-quarter", "idle", "run", "strike", "glide", "Flourish"]):
        x = 130 + (k * VIEW if k < 2 else 2 * VIEW + (k - 2) * CELL)
        draw.text((x + 8, 12), label, fill=INK)
    for r, (charter, folder, flourish) in enumerate(CHARTERS):
        y = 34 + r * (VIEW + 10)
        draw.text((10, y + VIEW // 2 - 6), charter, fill=INK)
        for k, view in enumerate(["side", "three_quarter"]):
            path = os.path.join(FRAMES_ROOT, folder.lower(), "turnaround_%s.png" % view)
            if os.path.exists(path):
                sheet.alpha_composite(Image.open(path).convert("RGBA").resize((VIEW, VIEW), Image.LANCZOS), (130 + k * VIEW, y))
        for k, (clip, i) in enumerate(picks):
            im = frame(folder, clip or flourish, i if clip else 4)
            if im is not None:
                sheet.alpha_composite(im, (130 + 2 * VIEW + k * CELL, y + (VIEW - CELL) // 2))
    out = os.path.join(ROOT, "docs", "art", "wren-charters.png")
    sheet.save(out)
    print("[charters] ->", out, sheet.size)


if __name__ == "__main__":
    main()

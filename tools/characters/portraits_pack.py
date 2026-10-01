"""Pack the dialogue portraits (CHR-13) that portraits.py rendered into one strip per speaker.

    python tools/characters/portraits_pack.py

Reads tools/characters/.frames/portraits/ (each speaker's `rest` and `talk` at 2x, and speakers.json), downsamples
with premultiplied alpha, and writes Assets/_Project/Art/Portraits/Portrait_<Speaker>.png: four 256-px frames in a
row, `rest`, `talk`, and the same two in the Remnant's grey. The grey is InkSprite's colour state (ColourState in
the shader, at _Wash 1 and _LineFade 1) worked on the pixels in linear light, so a Remnant's portrait is the drawing
the player sees in the room. portraits.json beside them lists every speaker, the body the face is drawn from and the
file, for the Unity setup and the tests. The contact sheet goes to docs/art/portraits.png.
"""
import json, os
from PIL import Image, ImageDraw
from pack import premultiplied_resize

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FRAMES = os.path.join(ROOT, "tools", "characters", ".frames", "portraits")
OUT = os.path.join(ROOT, "LastCartographer", "Assets", "_Project", "Art", "Portraits")
CELL = 256
STRIP = ["rest", "talk", "rest_remnant", "talk_remnant"]
PAPER = (0.93, 0.89, 0.80)          # InkSprite's _PaperColor
INK = (20, 26, 40, 255)


def to_linear(c):
    c /= 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def to_srgb(c):
    c = 12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055
    return max(0, min(255, int(round(c * 255))))


PAPER_LIN = tuple(to_linear(c * 255) for c in PAPER)
LUT = [to_linear(v) for v in range(256)]


def remnant(im, wash=1.0, line_fade=1.0):
    """InkSprite.shader ColourState: the fills toward paper, the line (darker than any wash) toward grey."""
    out = im.copy()
    px = out.load()
    fill_k = 0.85 * wash
    line_k = min(0.6, 0.35 * wash + 0.55 * line_fade)
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lr, lg, lb = LUT[r], LUT[g], LUT[b]
            lum = 0.299 * lr + 0.587 * lg + 0.114 * lb
            on_line = max(0.0, min(1.0, (0.16 - lum) / 0.10))
            k = fill_k + (line_k - fill_k) * on_line
            px[x, y] = (to_srgb(lr + (PAPER_LIN[0] - lr) * k), to_srgb(lg + (PAPER_LIN[1] - lg) * k),
                        to_srgb(lb + (PAPER_LIN[2] - lb) * k), a)
    return out


def main():
    with open(os.path.join(FRAMES, "speakers.json")) as f:
        speakers = json.load(f)
    os.makedirs(OUT, exist_ok=True)
    manifest = {"cell": CELL, "frames": STRIP, "speakers": []}
    tiles = []
    for entry in speakers:
        speaker = entry["speaker"]
        rest = premultiplied_resize(Image.open(os.path.join(FRAMES, "%s_rest.png" % speaker)), (CELL, CELL))
        talk = premultiplied_resize(Image.open(os.path.join(FRAMES, "%s_talk.png" % speaker)), (CELL, CELL))
        frames = [rest, talk, remnant(rest), remnant(talk)]
        strip = Image.new("RGBA", (CELL * len(frames), CELL), (0, 0, 0, 0))
        for i, im in enumerate(frames):
            strip.alpha_composite(im, (i * CELL, 0))
        file = "Portrait_%s.png" % speaker
        strip.save(os.path.join(OUT, file))
        manifest["speakers"].append({"speaker": speaker, "body": entry["body"], "file": file})
        tiles.append((speaker, entry["body"], frames))
        print("[portraits] %s -> %s" % (speaker, file))
    with open(os.path.join(OUT, "portraits.json"), "w") as f:
        json.dump(manifest, f, indent=2)

    # The contact sheet: each speaker at rest, mid-word and as a Remnant, on paper, named.
    t, cols, pad = 128, 4, 12
    tile_w, tile_h = 3 * t + pad, t + 24
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * tile_w + pad, rows * tile_h + pad), (237, 227, 204, 255))
    draw = ImageDraw.Draw(sheet)
    for k, (speaker, body, frames) in enumerate(tiles):
        x, y = pad + (k % cols) * tile_w, pad + (k // cols) * tile_h
        for i, im in enumerate((frames[0], frames[1], frames[2])):
            sheet.alpha_composite(im.resize((t, t), Image.LANCZOS), (x + i * t, y))
        draw.text((x + 4, y + t + 4), speaker if body == speaker else "%s (%s)" % (speaker, body), fill=INK)
    path = os.path.join(ROOT, "docs", "art", "portraits.png")
    sheet.save(path)
    print("[portraits] contact sheet -> %s %s" % (path, sheet.size))


if __name__ == "__main__":
    main()

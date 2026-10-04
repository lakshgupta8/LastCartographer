"""Pack the dialogue portraits (CHR-13) that portraits.py rendered into one strip per speaker.

    python tools/characters/portraits_pack.py

Reads tools/characters/.frames/portraits/ (each speaker's `rest` and `talk` in every mood at 2x, and speakers.json),
downsamples with premultiplied alpha, and writes Assets/_Project/Art/Portraits/Portrait_<Speaker>.png: a row per mood
(plain, bright, grave, wary, asking, top to bottom), each four 256-px frames, `rest`, `talk`, and the same two in the
Remnant's grey. The grey is InkSprite's colour state (ColourState in
the shader, at _Wash 1 and _LineFade 1) worked on the pixels in linear light, so a Remnant's portrait is the drawing
the player sees in the room. portraits.json beside them lists every speaker, the body the face is drawn from and the
file, for the Unity setup and the tests. The contact sheet goes to docs/art/portraits.png.

The townsfolk library's looks (speakers.json "looks", the islands' unnamed) are packed the same way to
Art/Portraits/Looks/Portrait_Folk_<Look>.png and listed under "looks", apart from the speakers the page carries;
their sheet, every look in its five moods and greyed, is docs/art/portrait-looks.png.
"""
import json, os
import numpy as np
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


PAPER_LIN = np.array([to_linear(c * 255) for c in PAPER])
LUT = np.array([to_linear(v) for v in range(256)])


def srgb_of(lin):
    lin = np.clip(lin, 0.0, 1.0)
    c = np.where(lin <= 0.0031308, 12.92 * lin, 1.055 * np.power(lin, 1 / 2.4) - 0.055)
    return np.clip(np.round(c * 255), 0, 255).astype(np.uint8)


def remnant(im, wash=1.0, line_fade=1.0):
    """InkSprite.shader ColourState: the fills toward paper, the line (darker than any wash) toward grey."""
    px = np.asarray(im.convert("RGBA")).copy()
    lin = LUT[px[..., :3]]
    fill_k = 0.85 * wash
    line_k = min(0.6, 0.35 * wash + 0.55 * line_fade)
    lum = lin @ np.array([0.299, 0.587, 0.114])
    on_line = np.clip((0.16 - lum) / 0.10, 0.0, 1.0)
    k = (fill_k + (line_k - fill_k) * on_line)[..., None]
    out = srgb_of(lin + (PAPER_LIN - lin) * k)
    seen = px[..., 3] > 0
    px[..., :3][seen] = out[seen]
    return Image.fromarray(px, "RGBA")


def main():
    with open(os.path.join(FRAMES, "speakers.json")) as f:
        rendered = json.load(f)
    moods, speakers = rendered["moods"], rendered["speakers"]
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(os.path.join(OUT, "Looks"), exist_ok=True)
    manifest = {"cell": CELL, "frames": STRIP, "moods": moods, "speakers": [], "looks": []}
    tiles, faces, looks = [], {}, []

    def pack(entry, file):
        speaker = entry["speaker"]
        sheet = Image.new("RGBA", (CELL * len(STRIP), CELL * len(moods)), (0, 0, 0, 0))
        rows = []
        for row, mood in enumerate(moods):
            def cell(pose):
                return premultiplied_resize(Image.open(os.path.join(FRAMES, "%s_%s_%s.png" % (speaker, mood, pose))), (CELL, CELL))
            rest, talk = cell("rest"), cell("talk")
            frames = [rest, talk, remnant(rest), remnant(talk)]
            for i, im in enumerate(frames):
                sheet.alpha_composite(im, (i * CELL, row * CELL))
            rows.append(frames)
        sheet.save(os.path.join(OUT, file))
        print("[portraits] %s -> %s" % (speaker, file))
        return rows

    for entry in speakers:
        speaker = entry["speaker"]
        file = "Portrait_%s.png" % speaker
        rows = pack(entry, file)
        tiles.append((speaker, entry["body"], rows[0]))
        faces[speaker] = [r[0] for r in rows]
        manifest["speakers"].append({"speaker": speaker, "body": entry["body"], "file": file})
    for entry in rendered.get("looks", []):
        file = "Looks/Portrait_%s.png" % entry["speaker"]
        rows = pack(entry, file)
        looks.append((entry["speaker"], rows))
        manifest["looks"].append({"speaker": entry["speaker"], "body": entry["body"], "file": file})
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

    # The moods' sheet: each speaker's five faces at rest, side by side, so a mood can be judged across the cast.
    t, cols, pad = 96, 3, 12
    tile_w, tile_h = len(moods) * t + pad, t + 24
    rows = (len(faces) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * tile_w + pad, rows * tile_h + pad + 20), (237, 227, 204, 255))
    draw = ImageDraw.Draw(sheet)
    for i, mood in enumerate(moods):
        for c in range(cols):
            draw.text((pad + c * tile_w + i * t + 4, 6), mood, fill=INK)
    for k, (speaker, rests) in enumerate(faces.items()):
        x, y = pad + (k % cols) * tile_w, pad + 20 + (k // cols) * tile_h
        for i, im in enumerate(rests):
            sheet.alpha_composite(im.resize((t, t), Image.LANCZOS), (x + i * t, y))
        draw.text((x + 4, y + t + 4), speaker, fill=INK)
    path = os.path.join(ROOT, "docs", "art", "portrait-moods.png")
    sheet.save(path)
    print("[portraits] moods sheet -> %s %s" % (path, sheet.size))

    # The looks' sheet: each look in its five moods at rest and then greyed, as an island's people speak.
    if looks:
        t, cols, pad = 80, 2, 12
        tile_w, tile_h = (len(moods) + 1) * t + pad, t + 22
        rows = (len(looks) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * tile_w + pad, rows * tile_h + pad + 20), (237, 227, 204, 255))
        draw = ImageDraw.Draw(sheet)
        for c in range(cols):
            for i, label in enumerate(moods + ["grey"]):
                draw.text((pad + c * tile_w + i * t + 4, 6), label, fill=INK)
        for k, (speaker, frames) in enumerate(looks):
            x, y = pad + (k % cols) * tile_w, pad + 20 + (k // cols) * tile_h
            for i, im in enumerate([r[0] for r in frames] + [frames[0][2]]):
                sheet.alpha_composite(im.resize((t, t), Image.LANCZOS), (x + i * t, y))
            draw.text((x + 4, y + t + 4), speaker[len("Folk_"):], fill=INK)
        path = os.path.join(ROOT, "docs", "art", "portrait-looks.png")
        sheet.save(path)
        print("[portraits] looks sheet -> %s %s" % (path, sheet.size))


if __name__ == "__main__":
    main()

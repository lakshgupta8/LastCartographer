"""The key art and the Steam capsules (ENV-13, docs/design/marketing-assets.md), composed from the game's own pictures.

    python tools/marketing/capsules.py

Reads logs/capture/key_art_ground.png (the Wind Gate at 3840x2160 with Wren hidden, MarketingCaptureTests) and
logs/capture/hero_glide.png, hero_survey.png (Wren at poster size, tools/marketing/wren_hero.py), and writes every size
Steam asks for (Marketing.Capsules in Unity) to docs/marketing/capsules/. The title is set in IM Fell English (SIL OFL,
tools/marketing/fonts/), a stand-in for the hand-cut title the art direction asks for (ENV-11): ink on the paper, with
a wash of paper behind it so it reads over the drawing. The library hero and the page background carry no text, as
Steam asks; the library logo is the title alone on transparency.
"""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
CAPTURE = os.path.join(ROOT, "logs", "capture")
OUT = os.path.join(ROOT, "docs", "marketing", "capsules")
FONT = os.path.join(HERE, "fonts", "IMFellEnglish-Regular.ttf")
INK = (20, 26, 40, 255)
PAPER = (237, 227, 204, 255)
TITLE = ("The Last", "Cartographer")

# The ground's useful part: the Gate's lip and sky, without the cliff face below and the pillar at the right edge.
GROUND_BOX = (60, 40, 3460, 1952)


def cover(img, w, h, focus=(0.5, 0.5)):
    """Crop to w:h around a focus (fractions of the image) and resize: the picture fills the frame, nothing squashed."""
    iw, ih = img.size
    scale = max(w / iw, h / ih)
    cw, ch = w / scale, h / scale
    x = min(max(focus[0] * iw - cw / 2, 0), iw - cw)
    y = min(max(focus[1] * ih - ch / 2, 0), ih - ch)
    return img.crop((int(x), int(y), int(x + cw), int(y + ch))).resize((w, h), Image.LANCZOS)


def trim(img):
    box = img.getchannel("A").getbbox()
    return img.crop(box) if box else img


def place_wren(canvas, wren, height, centre, angle=0.0):
    """Wren at a height in px with her centre at a point, turned a few degrees (nose up for the glide)."""
    w = trim(wren)
    k = height / w.height
    w = w.resize((max(1, int(w.width * k)), int(height)), Image.LANCZOS)
    if angle:
        w = w.rotate(angle, resample=Image.BICUBIC, expand=True)
    canvas.alpha_composite(w, (int(centre[0] - w.width / 2), int(centre[1] - w.height / 2)))


def title_block(lines, size, ratio=1.55, gap=0.05):
    """The title as an RGBA block: the first line smaller, the second large, ink, centred on each other."""
    small = ImageFont.truetype(FONT, int(size / ratio))
    big = ImageFont.truetype(FONT, int(size))
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    boxes = [probe.textbbox((0, 0), lines[0], font=small), probe.textbbox((0, 0), lines[1], font=big)]
    widths = [b[2] - b[0] for b in boxes]
    heights = [b[3] - b[1] for b in boxes]
    pad = int(size * 0.35)
    W = max(widths) + 2 * pad
    H = sum(heights) + int(size * gap) + 2 * pad
    block = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(block)
    y = pad
    for line, font, box, w, h in ((lines[0], small, boxes[0], widths[0], heights[0]), (lines[1], big, boxes[1], widths[1], heights[1])):
        d.text(((W - w) / 2 - box[0], y - box[1]), line, font=font, fill=INK)
        y += h + int(size * gap)
    # a rule under it, as on a map's cartouche
    rule_y = H - pad + int(size * 0.12)
    d.line(((W - widths[1]) / 2 + size * 0.4, rule_y, (W + widths[1]) / 2 - size * 0.4, rule_y), fill=INK, width=max(1, int(size / 40)))
    return block


def glow(block, radius):
    """A wash of paper behind the title's letters, so it reads over the drawing."""
    a = block.getchannel("A").filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(radius))
    a = a.point(lambda v: min(255, int(v * 2.2)))
    wash = Image.new("RGBA", block.size, PAPER)
    wash.putalpha(a)
    out = Image.new("RGBA", block.size, (0, 0, 0, 0))
    out.alpha_composite(wash)
    out.alpha_composite(block)
    return out


def place_title(canvas, size, anchor, align="centre"):
    block = glow(title_block(TITLE, size), max(2, size // 6))
    x, y = anchor
    if align == "centre":
        x -= block.width / 2
    elif align == "right":
        x -= block.width
    canvas.alpha_composite(block, (int(x), int(y)))


def wash(img, amount):
    """The ground thinned toward paper, so the title and Wren stand forward of it."""
    paper = Image.new("RGBA", img.size, PAPER)
    return Image.blend(img, paper, amount)


def main():
    os.makedirs(OUT, exist_ok=True)
    ground = Image.open(os.path.join(CAPTURE, "key_art_ground.png")).convert("RGBA").crop(GROUND_BOX)
    glide = Image.open(os.path.join(CAPTURE, "hero_glide.png")).convert("RGBA")
    survey = Image.open(os.path.join(CAPTURE, "hero_survey.png")).convert("RGBA")
    made = []

    def save(img, name):
        img.save(os.path.join(OUT, name))
        made.append((name, img.size))

    def wide(w, h, titled, wren_h=0.46, wren_at=(0.66, 0.29), title_h=0.105, title_at=(0.05, 0.07), focus=(0.55, 0.5), thin=0.12):
        """A wide picture: the Gate, Wren gliding over the updraft, the title in the sky at the left."""
        c = wash(cover(ground, w, h, focus), thin)
        if wren_h:
            place_wren(c, glide, h * wren_h, (w * wren_at[0], h * wren_at[1]), angle=6)
        if titled:
            place_title(c, int(h * title_h), (w * title_at[0], h * title_at[1]), align="left")
        return c

    def tall(w, h, titled, focus=(0.42, 0.62)):
        """A tall picture: the Gate's lip below, Wren standing large, the title above her."""
        c = wash(cover(ground, w, h, focus), 0.16)
        place_wren(c, survey, h * 0.58, (w * 0.5, h * 0.66))
        if titled:
            place_title(c, int(w * 0.13), (w * 0.5, h * 0.03))
        return c

    save(wide(3840, 2160, True), "key_art.png")
    save(wide(920, 430, True, wren_h=0.50, wren_at=(0.74, 0.29), title_h=0.13, title_at=(0.03, 0.10)), "header_capsule.png")
    save(wide(1232, 706, True, wren_h=0.46, wren_at=(0.72, 0.27), title_h=0.12, title_at=(0.04, 0.08)), "main_capsule.png")
    # the small capsule is read at a glance on a list: the title first, Wren a mark beside it
    small = wash(cover(ground, 462, 174, (0.55, 0.35)), 0.35)
    place_wren(small, glide, 174 * 0.70, (462 * 0.89, 174 * 0.44), angle=6)
    place_title(small, 48, (462 * 0.01, 174 * 0.04), align="left")
    save(small, "small_capsule.png")
    save(tall(748, 896, True), "vertical_capsule.png")
    save(tall(600, 900, True), "library_capsule.png")
    save(wide(3840, 1240, False, wren_h=0.56, wren_at=(0.62, 0.34), focus=(0.55, 0.42), thin=0.06), "library_hero.png")
    save(wide(1438, 810, False, wren_h=0, focus=(0.5, 0.5), thin=0.45), "page_background.png")   # behind the store's text: thin, and nobody in it
    logo = Image.new("RGBA", (1280, 720), (0, 0, 0, 0))
    block = title_block(TITLE, 230)
    k = min(1200 / block.width, 680 / block.height, 1.0)
    block = block.resize((int(block.width * k), int(block.height * k)), Image.LANCZOS)
    logo.alpha_composite(block, ((1280 - block.width) // 2, (720 - block.height) // 2))
    save(logo, "library_logo.png")
    for name, size in made:
        print("[capsules] %s %dx%d" % (name, size[0], size[1]))


if __name__ == "__main__":
    main()

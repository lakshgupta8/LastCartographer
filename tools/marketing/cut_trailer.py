"""A rough cut of the trailer (PRO-08), from the ENV-13 clips and stills and Saltmarrow's rendered theme.

    python tools/marketing/cut_trailer.py

Follows the beat sheet in docs/design/marketing-assets.md section 4: the white, then Wren running, the people, the quill,
four regions as stills with a slow push, the leap, and the title. Each beat becomes a segment at 1920x1080 and 30 fps,
the segments are joined with short cross-fades, and Saltmarrow's stems (docs/audio/music, AUD) play under it, looped
and faded out. Writes docs/marketing/trailer/the_last_cartographer_trailer.mp4 (H.264 + AAC). It is a rough cut for
the team to re-edit, not the trailer: there is no voice, no card but the title, and the footage is the greybox.
"""
import os, subprocess, sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
M = os.path.join(ROOT, "docs", "marketing")
MUSIC = os.path.join(ROOT, "docs", "audio", "music")
OUT = os.path.join(M, "trailer", "the_last_cartographer_trailer.mp4")
TMP = os.path.join(ROOT, "logs", "capture", "trailer")
FADE = 0.4   # seconds of cross-fade between beats

# (kind, source, seconds): the beat sheet, in order
BEATS = [
    ("still", "screenshots/blank_capital.png", 4.0),
    ("clip", "trailer/run_the_boardwalk.mp4", 4.0),
    ("clip", "trailer/sable_speaks.mp4", 3.0),
    ("clip", "trailer/quill_strikes.mp4", 3.0),
    ("still", "screenshots/emberdown_chimneys.png", 1.6),
    ("still", "screenshots/verdance_aldermere.png", 1.6),
    ("still", "screenshots/halden_lowmarket.png", 1.6),
    ("still", "screenshots/verdance_library.png", 1.6),
    ("clip", "trailer/the_leap.mp4", 5.0),
    ("still", "capsules/key_art.png", 3.5),
]
STEMS = ["saltmarrow_music_bed_67.wav", "saltmarrow_music_pulse_67.wav", "saltmarrow_music_lead_67.wav", "saltmarrow_music_voices_67.wav"]


def run(args):
    r = subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error"] + args)
    if r.returncode != 0:
        sys.exit("ffmpeg failed: " + " ".join(args[:6]) + " ...")


def segment(i, kind, source, seconds):
    path = os.path.join(TMP, "seg_%02d.mp4" % i)
    src = os.path.join(M, source)
    frames = int(round(seconds * 30))
    if kind == "still":
        # a slow push: the still at twice its size so the pan does not shimmer, zooming a little over its frames
        vf = ("scale=3840:2160,zoompan=z='1+0.06*on/%d':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1:s=1920x1080:fps=30,format=yuv420p" % frames)
        run(["-loop", "1", "-framerate", "30", "-t", "%.2f" % seconds, "-i", src, "-vf", vf, "-frames:v", str(frames), "-c:v", "libx264", "-preset", "fast", "-crf", "18", "-an", path])
    else:
        run(["-i", src, "-t", "%.2f" % seconds, "-vf", "scale=1920:1080,fps=30,format=yuv420p", "-c:v", "libx264", "-preset", "fast", "-crf", "18", "-an", path])
    return path, seconds


def main():
    os.makedirs(TMP, exist_ok=True)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    segs = [segment(i, *b) for i, b in enumerate(BEATS)]
    total = sum(s for _, s in segs) - FADE * (len(segs) - 1)

    # The video: every segment in, cross-faded in a chain; the first fades in from paper, the last out to it.
    args = []
    for path, _ in segs:
        args += ["-i", path]
    for stem in STEMS:
        args += ["-stream_loop", "-1", "-i", os.path.join(MUSIC, stem)]
    n = len(segs)
    f = []
    offset = 0.0
    last = "[0:v]"
    for i in range(1, n):
        offset += segs[i - 1][1] - FADE
        out = "[v%d]" % i if i < n - 1 else "[vx]"
        f.append("%s[%d:v]xfade=transition=fade:duration=%.2f:offset=%.2f%s" % (last, i, FADE, offset, out))
        last = out
    f.append("[vx]fade=t=in:st=0:d=0.8:color=0xede3cc,fade=t=out:st=%.2f:d=1.2:color=0xede3cc[v]" % (total - 1.2))
    # The music: the stems mixed, trimmed to the cut, and faded out over the title.
    mix = "".join("[%d:a]" % (n + k) for k in range(len(STEMS)))
    f.append("%samix=inputs=%d:normalize=0,volume=0.9,atrim=0:%.2f,afade=t=in:st=0:d=1.5,afade=t=out:st=%.2f:d=3[a]" % (mix, len(STEMS), total, total - 3))
    run(args + ["-filter_complex", ";".join(f), "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-t", "%.2f" % total, OUT])
    print("[trailer] -> %s (%.1f s, %d beats)" % (OUT, total, n))


if __name__ == "__main__":
    main()

#!/bin/bash
# The trailer's clips (ENV-13, docs/design/marketing-assets.md): MarketingCaptureTests.CaptureTheTrailerClips writes each
# clip's frames to logs/capture/<clip>/frame_NNNN.png at 30 fps; this encodes them to docs/marketing/trailer/<clip>.mp4
# (H.264, yuv420p, CRF 18, so an editor can cut them without a second generation of loss).
#
#   bash tools/marketing/encode_clips.sh
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/docs/marketing/trailer"
mkdir -p "$OUT"
for dir in "$ROOT"/logs/capture/*/; do
  clip="$(basename "$dir")"
  [ -f "$dir/frame_0000.png" ] || continue
  ffmpeg -y -loglevel error -framerate 30 -i "$dir/frame_%04d.png" -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart "$OUT/$clip.mp4"
  echo "[clips] $clip -> docs/marketing/trailer/$clip.mp4 ($(ls "$dir" | wc -l) frames)"
done

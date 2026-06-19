#!/usr/bin/env python3
"""Post-process a gpt-image-2 generation (flat magenta background) into a
transparent, downscaled RTS cameo.

  python3 postprocess_cameo.py --in raw.png --out cameo.png [--size 128]
  python3 postprocess_cameo.py --in raw.png --out cameo.png --width 60 --height 44

Steps: chroma-key the flat background to alpha, autocrop to the subject, then
scale the subject (preserving aspect) into a WxH transparent canvas. The OpenRA
sidebar draws cameos unscaled into ~62x46 cells (DrawSpriteCentered), so the
default 60x44 fills a cell without overflowing.
"""
import argparse
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--size", type=int, default=128, help="square fallback if --width/--height omitted")
    ap.add_argument("--width", type=int)
    ap.add_argument("--height", type=int)
    ap.add_argument("--key", default="255,0,255")
    ap.add_argument("--tol", type=int, default=70)
    args = ap.parse_args()

    tw = args.width or args.size
    th = args.height or args.size

    kr, kg, kb = (int(x) for x in args.key.split(","))
    tol2 = args.tol * args.tol

    img = Image.open(args.inp).convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            dr, dg, db = r - kr, g - kg, b - kb
            if dr * dr + dg * dg + db * db <= tol2:
                px[x, y] = (r, g, b, 0)

    # autocrop to opaque bounding box
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)

    # scale subject to fit within the target, preserving aspect, then center on a WxH canvas.
    sw, sh = img.size
    scale = min(tw / sw, th / sh)
    nw, nh = max(1, int(round(sw * scale))), max(1, int(round(sh * scale)))
    subject = img.resize((nw, nh), Image.LANCZOS)
    canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
    canvas.paste(subject, ((tw - nw) // 2, (th - nh) // 2), subject)
    canvas.save(args.out)
    print(f"OK wrote {args.out} {canvas.size}")


if __name__ == "__main__":
    main()

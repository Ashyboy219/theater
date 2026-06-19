#!/usr/bin/env python3
"""Post-process a gpt-image-2 generation (flat magenta background) into a
transparent, downscaled RTS cameo. Proves the second half of the art pipeline.

  python3 postprocess_cameo.py --in raw.png --out cameo.png [--size 128]
                               [--key 255,0,255] [--tol 60]

Steps: chroma-key the flat background to alpha, autocrop to the subject,
then downscale with high-quality resampling to a square cameo.
"""
import argparse
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--in", dest="inp", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--size", type=int, default=128)
    ap.add_argument("--key", default="255,0,255")
    ap.add_argument("--tol", type=int, default=70)
    args = ap.parse_args()

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

    # fit into a square canvas, then resize to target
    side = max(img.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(img, ((side - img.size[0]) // 2, (side - img.size[1]) // 2))
    cameo = canvas.resize((args.size, args.size), Image.LANCZOS)
    cameo.save(args.out)
    print(f"OK wrote {args.out} {cameo.size}")


if __name__ == "__main__":
    main()

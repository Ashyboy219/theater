#!/usr/bin/env python3
"""Repack the War Room raws into DISPLAY-sized power-of-two atlases.

OpenRA's ImageWidget draws a chrome region at its NATIVE pixel size (it does not scale to the
widget bounds), so each atlas region must already be the size we want on screen. The texture as a
whole still has to be power-of-two, so we pack small display-sized cells into POT atlases.

Reads the raws produced by generate_warroom_art.py (design/art/theater/warroom/*_raw.png) and
writes the final atlases into mods/theater/bits/warroom/. Safe to re-run.
"""
import os
import subprocess
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
RAW = os.path.join(HERE, "theater", "warroom")
OUT = os.path.join(REPO, "mods", "theater", "bits", "warroom")


def keyfit(name, cell):
    """chroma-key + fit a raw into a transparent cell x cell PNG; return an RGBA Image or None."""
    raw = os.path.join(RAW, f"{name}_raw.png")
    if not os.path.exists(raw):
        print(f"  MISS raw {name}")
        return None
    out = os.path.join(RAW, f"{name}_fit{cell}.png")
    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "postprocess_cameo.py"),
         "--in", raw, "--out", out, "--width", str(cell), "--height", str(cell), "--tol", "90"],
        capture_output=True, text=True)
    if r.returncode != 0:
        print(f"  FAIL keyfit {name}: {r.stderr}")
        return None
    return Image.open(out).convert("RGBA")


def atlas(out_name, atlas_size, cell, names):
    a = Image.new("RGBA", (atlas_size, atlas_size), (0, 0, 0, 0))
    spots = [(0, 0), (cell, 0), (0, cell), (cell, cell)]
    for name, (x, y) in zip(names, spots):
        im = keyfit(name, cell)
        if im is not None:
            a.paste(im, (x, y), im)
    a.save(os.path.join(OUT, out_name))
    print(f"  wrote {out_name} {a.size} (cell {cell})")


def main():
    os.makedirs(OUT, exist_ok=True)

    # President — center-crop the scene to ~1.6:1, resize to 420x260, place in a 512x512 POT canvas.
    raw = os.path.join(RAW, "president-warroom-bg_raw.png")
    if os.path.exists(raw):
        im = Image.open(raw).convert("RGB")
        w, h = im.size
        tw, th = 420, 260
        # crop to the target aspect about the center, then resize.
        ar = tw / th
        if w / h > ar:
            nw = int(h * ar); im = im.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
        else:
            nh = int(w / ar); im = im.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
        im = im.resize((tw, th), Image.LANCZOS).convert("RGBA")
        canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        canvas.paste(im, (0, 0))
        canvas.save(os.path.join(OUT, "president-warroom-bg.png"))
        print(f"  wrote president-warroom-bg.png (512,512) region 0,0,{tw},{th}")

    # Crest — 56px keyed into a 64x64 POT canvas.
    crest = keyfit("theater-crest", 56)
    if crest is not None:
        canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        canvas.paste(crest, (4, 4), crest)
        canvas.save(os.path.join(OUT, "theater-crest.png"))
        print("  wrote theater-crest.png (64,64) region 4,4,56,56")

    # 32px icon atlases (64x64 POT).
    atlas("domains.png", 64, 32, ["icon-vehicle", "icon-infantry", "icon-aircraft", "icon-naval"])
    atlas("icons.png", 64, 32, ["res-credits", "res-power", "res-supplies", "res-pop"])

    # 48px atlases (128x128 POT) — cards + staff.
    atlas("cards.png", 128, 48, ["card-satellite", "card-ew", "card-precision", "card-black"])
    atlas("staff.png", 128, 48, ["staff-general", "staff-admiral", "staff-scientist", "staff-intel"])

    print("\n=== final atlas sizes ===")
    import glob
    for p in sorted(glob.glob(os.path.join(OUT, "*.png"))):
        print(os.path.basename(p), Image.open(p).size)


if __name__ == "__main__":
    main()

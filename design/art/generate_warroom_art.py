#!/usr/bin/env python3
"""Batch-generate the THEATER War Room ("National Command") art with gpt-image-2,
then post-process into power-of-two PNGs / atlases wired into the mod.

Assets:
  president-warroom-bg.png  1024x1024  (full scene, NO magenta)            -> warroom-president
  staff.png                 512x512    (2x2 of 256, magenta-keyed)         -> warroom-staff
  domains.png               256x256    (2x2 of 128, magenta-keyed)         -> warroom-domains
  icons.png                 256x256    (2x2 of 128, magenta-keyed)         -> warroom-icons
  cards.png                 256x256    (2x2 of 128, magenta-keyed)         -> warroom-cards
  theater-crest.png         256x256    (single, magenta-keyed)             -> warroom-crest

Raws (gpt-image-2 output) live in design/art/theater/warroom/.
Final POT PNGs land in mods/theater/bits/warroom/.

Run from the repo root with the API key in the environment:
    zsh -ic 'python3 design/art/generate_warroom_art.py'
Pass --force to regenerate raws that already exist.
"""
import os
import subprocess
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
RAW_DIR = os.path.join(HERE, "theater", "warroom")
OUT_DIR = os.path.join(REPO, "mods", "theater", "bits", "warroom")

FORCE = "--force" in sys.argv

MAGENTA = (
    " The ENTIRE background is one flat solid pure magenta color "
    "(hex #FF00FF / rgb 255,0,255), evenly filled, so it can be keyed out."
)
STYLE_HEAD = (
    "Neo-retro contemporary-military command-staff portrait headshot, head and shoulders, "
    "centered, facing the viewer, semi-realistic painted style, crisp de-blurred HD detail, "
    "even dramatic studio lighting, serious composed expression, no text, no insignia text."
)
STYLE_ICON = (
    "Single minimalist HUD icon, one centered subject filling the frame, flat semi-realistic "
    "style, clean crisp edges, light steel-grey with a single accent color, even lighting, "
    "no text, no scenery."
)
STYLE_CARD = (
    "Small dramatic strategy-card illustration, one centered subject, contemporary-military "
    "techno-thriller style, moody cinematic lighting, crisp de-blurred HD detail, no text."
)

PRESIDENT_PROMPT = (
    "Cinematic wide establishing shot of a dark high-tech national emergency war room at night, "
    "photoreal, moody. A composed middle-aged male president in a dark suit sits at a sweeping "
    "command desk positioned slightly LEFT of center, seen from a respectful three-quarter front "
    "angle, lit from below by the cold blue glow of tactical monitors. Behind and to the RIGHT, "
    "floor-to-ceiling panoramic windows reveal a distant burning city skyline at night - orange "
    "fires, smoke columns, silhouettes of military drones and fighter jets streaking across a "
    "bruised sky. The UPPER-CENTER of the frame is deliberately dark, empty and uncluttered "
    "(negative space for an overlaid caption). Rich teal-and-amber cinematic color grade, "
    "volumetric haze, shallow depth of field, subtle film grain, contemporary military "
    "techno-thriller aesthetic, de-blurred crisp HD detail. No text, no logos, no captions, "
    "no UI, no watermark."
)

# key -> (prompt, post_cell_px). Each generated at 1024x1024 on magenta then keyed+fit to cell.
STAFF = {
    "staff-general": STYLE_HEAD + " Subject: an older four-star army general, close-cropped grey "
        "hair, square jaw, olive-green dress uniform with rows of campaign ribbons." + MAGENTA,
    "staff-admiral": STYLE_HEAD + " Subject: a navy admiral, Black male, white naval dress uniform "
        "with gold shoulder boards and peaked cap." + MAGENTA,
    "staff-scientist": STYLE_HEAD + " Subject: a female defense scientist in her late thirties, "
        "Eastern-European, white lab coat over a dark turtleneck, glasses, hair tied back." + MAGENTA,
    "staff-intel": STYLE_HEAD + " Subject: a lean intelligence director, dark suit and no tie, "
        "an earpiece, sharp watchful eyes." + MAGENTA,
}
DOMAINS = {
    "icon-vehicle": STYLE_ICON + " Subject: a top-down silhouette of a main battle tank, amber accent." + MAGENTA,
    "icon-infantry": STYLE_ICON + " Subject: a single helmeted soldier silhouette, amber accent." + MAGENTA,
    "icon-aircraft": STYLE_ICON + " Subject: a top-down silhouette of a fighter jet, cyan accent." + MAGENTA,
    "icon-naval": STYLE_ICON + " Subject: a top-down silhouette of a naval destroyer warship, blue accent." + MAGENTA,
}
ICONS = {
    "res-credits": STYLE_ICON + " Subject: a single gold coin with a faint dollar mark, gold accent." + MAGENTA,
    "res-power": STYLE_ICON + " Subject: a single lightning bolt, bright cyan accent." + MAGENTA,
    "res-supplies": STYLE_ICON + " Subject: a small stack of amber supply crates, amber accent." + MAGENTA,
    "res-pop": STYLE_ICON + " Subject: three stylized human figures grouped together, green accent." + MAGENTA,
}
CARDS = {
    "card-satellite": STYLE_CARD + " Subject: a reconnaissance satellite orbiting above the Earth, "
        "scanning beams sweeping the surface, cold blue glow." + MAGENTA,
    "card-ew": STYLE_CARD + " Subject: an electronic-warfare radar dish emitting concentric signal "
        "arcs and jamming waves, green-tinted." + MAGENTA,
    "card-precision": STYLE_CARD + " Subject: a cruise missile streaking toward a glowing red "
        "targeting crosshair, amber explosion glow." + MAGENTA,
    "card-black": STYLE_CARD + " Subject: a sleek classified stealth prototype aircraft under a "
        "single spotlight in a dark hangar, ominous." + MAGENTA,
}
CREST = (
    "A front-facing heraldic eagle inside a circular military seal, brushed-steel and muted-gold, "
    "wings spread, clean symmetric emblem, neo-retro insignia, no text, no letters." + MAGENTA
)


def gen(name, prompt, size="1024x1024"):
    raw = os.path.join(RAW_DIR, f"{name}_raw.png")
    if os.path.exists(raw) and not FORCE:
        print(f"[skip-gen] {name}", flush=True)
        return raw
    print(f"[gen] {name} ...", flush=True)
    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "generate_image.py"),
         "--out", raw, "--prompt", prompt,
         "--size", size, "--quality", "high", "--background", "opaque"],
        capture_output=True, text=True)
    if r.returncode != 0:
        print(f"[FAIL gen] {name}: {r.stdout}\n{r.stderr}", flush=True)
        return None
    print(f"[ok gen] {name}", flush=True)
    return raw


def post_cell(raw, cell):
    """chroma-key + fit the raw into a transparent cell x cell PNG; return the path."""
    out = raw.replace("_raw.png", "_cell.png")
    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "postprocess_cameo.py"),
         "--in", raw, "--out", out, "--width", str(cell), "--height", str(cell), "--tol", "90"],
        capture_output=True, text=True)
    if r.returncode != 0:
        print(f"[FAIL post] {raw}: {r.stdout}\n{r.stderr}", flush=True)
        return None
    return out


def atlas(out_name, size, cell, group):
    """Pack 4 keyed cells into a size x size POT atlas (2x2)."""
    a = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    spots = [(0, 0), (cell, 0), (0, cell), (cell, cell)]
    ok = True
    for (name, _), (x, y) in zip(group.items(), spots):
        cellpng = os.path.join(RAW_DIR, f"{name}_cell.png")
        if not os.path.exists(cellpng):
            print(f"[atlas MISS] {out_name}: {name}", flush=True)
            ok = False
            continue
        im = Image.open(cellpng).convert("RGBA")
        a.paste(im, (x, y), im)
    out = os.path.join(OUT_DIR, out_name)
    a.save(out)
    print(f"[atlas] {out} {'OK' if ok else 'PARTIAL'}", flush=True)


def main():
    os.makedirs(RAW_DIR, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)

    # 1. President hero background — full scene, no magenta, crop center square -> 1024 POT.
    raw = gen("president-warroom-bg", PRESIDENT_PROMPT, size="1536x1024")
    if raw:
        im = Image.open(raw).convert("RGB")
        w, h = im.size
        s = min(w, h)
        im = im.crop(((w - s) // 2, 0, (w - s) // 2 + s, s)).resize((1024, 1024), Image.LANCZOS)
        im.save(os.path.join(OUT_DIR, "president-warroom-bg.png"))
        print("[ok] president-warroom-bg.png 1024x1024", flush=True)

    # 2. Crest — single 256 POT.
    raw = gen("theater-crest", CREST)
    if raw:
        cell = post_cell(raw, 256)
        if cell:
            Image.open(cell).save(os.path.join(OUT_DIR, "theater-crest.png"))
            print("[ok] theater-crest.png 256x256", flush=True)

    # 3-6. Atlas groups.
    for group, out_name, size, cell in [
        (STAFF, "staff.png", 512, 256),
        (DOMAINS, "domains.png", 256, 128),
        (ICONS, "icons.png", 256, 128),
        (CARDS, "cards.png", 256, 128),
    ]:
        for name, prompt in group.items():
            r = gen(name, prompt)
            if r:
                post_cell(r, cell)
        atlas(out_name, size, cell, group)

    print("\n=== verify POT sizes ===", flush=True)
    import glob
    for p in sorted(glob.glob(os.path.join(OUT_DIR, "*.png"))):
        print(os.path.basename(p), Image.open(p).size)


if __name__ == "__main__":
    main()

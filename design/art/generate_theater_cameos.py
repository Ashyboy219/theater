#!/usr/bin/env python3
"""Batch-generate distinct THEATER unit/structure cameos with gpt-image-2, then
post-process each into a sidebar-sized transparent PNG wired straight into the mod.

Pipeline per unit:  generate (flat magenta bg) -> cameos/<u>_raw.png
                    chroma-key + fit          -> mods/theater/bits/theater/<u>-cameo.png

Units that already have a <u>_raw.png are NOT regenerated (their raw is reused);
pass --force to regenerate everything. Run from the repo root with the keg-only
dotnet env irrelevant here, but OPENAI_API_KEY must be in the environment:
    zsh -ic 'python3 design/art/generate_theater_cameos.py'
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
RAW_DIR = os.path.join(HERE, "theater", "cameos")
BITS_DIR = os.path.join(REPO, "mods", "theater", "bits")

STYLE = (
    "Retro-modern real-time-strategy unit cameo icon. A single subject, centered and filling "
    "the frame, three-quarter top-down view, clean semi-realistic style, crisp edges, even neutral "
    "lighting, no text, no UI, no insignia, no scenery. The ENTIRE background must be one flat solid "
    "pure magenta color (hex #FF00FF / rgb 255,0,255), evenly filled, so it can be keyed out."
)

UNITS = {
    "specter": "a sleek angular fifth-generation radar-evading stealth strike fighter jet, dark gunmetal grey, sharp faceted stealth surfaces, twin canted tails",
    "wingloong": "a slender pale-grey long-endurance reconnaissance-and-strike drone aircraft, straight high-aspect wings, bulbous nose sensor, V-tail, small underwing missiles",
    "reaper": "a modern single-main-rotor attack helicopter gunship, dark grey, short stub wings with rocket pods, a chin-mounted gun turret",
    "grad": "a heavy six-wheeled military truck carrying a large boxy multiple-rocket-launcher tube array on its flatbed, olive-drab green",
    "redoubt": "a boxy heavily-armored eight-wheeled personnel carrier with a small low turret, olive-drab green, angular sloped armor",
    "dazhbog": "a light four-wheeled military pickup truck with a small angled rack of rocket tubes mounted on the cargo bed, sand-grey",
    "pioneer": "a single modern combat-engineer soldier in grey tactical gear and helmet, carrying an entrenching tool and a demolition satchel, standing",
    "comms": "a military electronic-warfare vehicle with a large rotating radar dish and antenna mast on its roof, dark green",
    "aegis": "a compact automated point-defense missile turret emplacement on a square concrete base, white and grey, a cluster of short interceptor tubes beside a small radar panel",
    "hayabusa": "a small sleek autonomous interceptor drone aircraft, white with red accents, swept wings, slim air-to-air missiles",
    "akash": "a surface-to-air missile battery on a tracked platform with four large raised angled launch tubes, olive-drab green",
    "tejas": "a small tailless delta-wing single-engine multirole light fighter jet, light grey, with canards",
    "koral": "a military electronic-warfare van with a tall antenna mast and a dish array on the roof, sand-tan desert camouflage",
    "thcom": "a modern military command-headquarters structure, a low flat-roofed reinforced concrete bunker bristling with rooftop satellite dishes, radar antennas and communication masts, grey",
    # --- Land variants (per-faction) ---
    "bradley": "a modern tracked infantry fighting vehicle with a small turret mounting an autocannon and a twin anti-tank missile launcher box, desert-tan",
    "jtac": "a modern four-wheeled military scout truck with a roof sensor and laser-designator ball and a single guided missile, sand-tan",
    "bmpt": "a heavy tracked tank-support combat vehicle bristling with twin autocannons and anti-tank missile launchers, olive-drab green",
    "loiter": "a light fast military pickup truck carrying an angled launch rack of small loitering kamikaze drones on the cargo bed, grey",
    "cobra": "a fast wheeled armored scout car with a small autocannon turret and a sensor mast, sand-tan desert camouflage",
    "jackal": "a fast open-topped wheeled patrol vehicle with a roof sensor array and a mounted machine gun, woodland green",
    "namica": "a tracked tank-destroyer carrying a large elevating box launcher of anti-tank guided missiles, olive green",
    # --- Shared modern roster ---
    "manpads": "a single soldier shouldering a portable surface-to-air missile launch tube aimed upward, grey tactical gear and helmet",
    "scout": "a single reconnaissance soldier with binoculars and a backpack radio antenna, light recon gear, kneeling and observing",
    "ifv": "a wheeled eight-wheel armored infantry fighting vehicle with a small autocannon turret, NATO grey-green",
    "mobsam": "a wheeled military vehicle carrying a rotating launcher of four raised surface-to-air missiles, olive green",
    # --- Air ---
    "raptor": "a fifth-generation twin-engine air-superiority stealth fighter jet, pale grey, two large canted angular tail fins, sleek aggressive faceted silhouette, slim air-to-air missiles",
    "tunguska": "a tracked air-defense vehicle with a turret carrying raised surface-to-air missile tubes flanked by twin autocannons, olive-drab green",
    # --- Naval (faction signature ships) ---
    "burke": "a modern guided-missile destroyer warship, grey, with a flat helicopter deck at the stern, vertical missile launch cells and a flat-panel phased-array radar superstructure",
    "akula": "a large nuclear attack submarine, dark grey-black, long smooth teardrop hull with a rounded conning tower and dive planes",
    "houbei": "a small fast stealthy missile catamaran patrol boat, grey, angular faceted twin hull with boxy anti-ship missile canisters amidships",
    "barbaros": "a small unmanned surface vessel drone boat, grey, sleek single hull with a tall sensor mast and a small remote gun",
    "astute": "a sleek nuclear attack submarine, dark grey, smooth rounded teardrop hull with a low streamlined conning tower",
    "kolkata": "a modern guided-missile destroyer warship, haze grey, with vertical missile cells, a large lattice radar mast and a stern helicopter deck",
    # --- Doctrine unlocks ---
    "molot": "a heavy main battle tank with a large angular turret, a long 125mm smoothbore gun, and reactive armor blocks, olive-drab green",
    "dongfeng": "a cheap mass-produced light tank with a simple boxy welded turret and a medium gun, bare grey primer",
    "arjun": "a heavy main battle tank with a rounded welded turret and a long gun barrel, desert-tan camouflage",
    "raider": "a single elite special-forces soldier in dark stealth combat gear with a suppressed carbine and night-vision goggles, crouching",
}

FORCE = "--force" in sys.argv


def gen(unit, subject):
    raw = os.path.join(RAW_DIR, f"{unit}_raw.png")
    if os.path.exists(raw) and not FORCE:
        print(f"[skip-gen] {unit} (raw exists)")
        return raw
    prompt = f"{STYLE} The subject is {subject}."
    print(f"[gen] {unit} ...", flush=True)
    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "gen_image_cs.py"),
         "--out", raw, "--prompt", prompt,
         "--size", "1024x1024", "--quality", "high", "--background", "opaque"],
        capture_output=True, text=True)
    if r.returncode != 0:
        print(f"[FAIL gen] {unit}: {r.stdout}\n{r.stderr}", flush=True)
        return None
    print(f"[ok gen] {unit}", flush=True)
    return raw


def post(unit, raw):
    out = os.path.join(BITS_DIR, f"{unit}-cameo.png")
    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "postprocess_cameo.py"),
         "--in", raw, "--out", out, "--width", "60", "--height", "44", "--tol", "80"],
        capture_output=True, text=True)
    if r.returncode != 0:
        print(f"[FAIL post] {unit}: {r.stdout}\n{r.stderr}", flush=True)
        return False
    print(f"[ok post] {unit} -> {out}", flush=True)
    return True


def main():
    os.makedirs(BITS_DIR, exist_ok=True)
    done, failed = [], []
    for unit, subject in UNITS.items():
        raw = gen(unit, subject)
        if raw and post(unit, raw):
            done.append(unit)
        else:
            failed.append(unit)
    print(f"\n=== DONE: {len(done)} ok, {len(failed)} failed ===")
    if failed:
        print("FAILED:", ", ".join(failed))


if __name__ == "__main__":
    main()

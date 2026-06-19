# Wiring THEATER modern unit/building cameos into the RA sidebar (PNG-direct pipeline)

**Executable now:** True

**Summary:** OpenRA can render cameos directly from RGBA PNG with zero SHP conversion: the engine's SpriteRenderer explicitly ignores the palette for TextureChannel.RGBA sprites (SpriteRenderer.cs:126), so a PngSheet-loaded PNG draws its own true color while Buildable's IconPalette:chrome reference just needs to resolve for lint. The two real blockers are mechanical: the ra mod's SpriteFormats does not yet include PngSheet (must add it), and the existing cameos are 128x128 (the RA sidebar draws cameos at native, unscaled size into 62x46 cells, so they overflow ~2x and must be regenerated at ~64x48). Once those are fixed, wiring one cameo is: drop PNG into ra|bits, add an icon sequence keyed under the actor's RenderSprites.Image, and point Buildable.Icon at it.

## Key decisions
- Use PNG-direct (PngSheet), not SHP — preserves full-color HD art and needs no conversion; SHP path rejects RGBA (requires Indexed8) and would force palette quantization.
- Must add 'PngSheet' to ra mod.yaml SpriteFormats (line 277) — it is currently absent, so PNG sprites won't load in ra without this one-line manifest change.
- Cameos must be regenerated at ~64x48 (cells are 62x46, drawn unscaled & centered via DrawSpriteCentered); current 128x128 assets overflow ~2x and clip.
- Define each icon as a uniquely-named sequence under the actor's inherited RenderSprites.Image (e.g. 'loewe-icon' under '3tnk') — never overwrite the shared stock 'icon' sequence of the inherited unit.
- Keep IconPalette default 'chrome': it is ignored at draw for RGBA (SpriteRenderer.cs:126) but must resolve for the PaletteReference lint.
- Drop PNGs into the ra|bits folder package (override-mounted in mod.yaml) under a theater/ subfolder; sequence Filename must include the explicit .png extension.

## Files to touch
- /Users/ashishnaik/Projects/openra/mods/ra/mod.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/sequences/theater.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-units.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-structures.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/bits/theater/
- /Users/ashishnaik/Projects/openra/design/art/postprocess_cameo.py

## Risks
- Cameos are 128x128 but the sidebar draws at native size into 62x46 cells — they MUST be re-emitted at ~64x48 or they render oversized/clipped. postprocess_cameo.py only resizes to a SQUARE --size, so 62x46 needs a small script tweak (or accept a square ~48-64px and center it).
- Adding PngSheet to ra SpriteFormats is a global manifest change for the whole ra mod, not map-scoped — affects all rule loads. Low risk (purely additive loader) but it is mod-wide, unlike the existing map-scoped theater economy.
- LOEWE et al. inherit a shared Image (3tnk, yak, etc.) with stock units; the icon sequence name must be unique (loewe-icon) or you overwrite the original unit's cameo. Same Image is reused by BAYRAK and WINGLOONG (both Image: yak) — give each its own sequence key under yak.
- Final visual correctness (centering, crispness, transparency edges) can only be confirmed in a GUI run, not headlessly — the fun/look gate needs ./theater-play.sh.
- ConvertPngToShp is in OpenRA.Mods.Cnc (loaded by ra via Assemblies) so --shp is available, but it is the wrong tool here; if someone runs it on the RGBA cameos it throws 'All frames must be paletted'.

## Design

## How a sidebar cameo actually resolves (verified in code)

`ProductionPaletteWidget.cs:516-520` is the ground truth:
```csharp
var icon = new Animation(World, rsi.GetImage(item, faction)); // actor's RenderSprites.Image
var bi = item.TraitInfo<BuildableInfo>();
icon.Play(bi.Icon);                                            // Buildable.Icon = sequence name
var palette = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + owner : bi.IconPalette; // "chrome"
```
So the cameo is `sequence[ RenderSprites.Image ][ Buildable.Icon ]`, drawn with palette `Buildable.IconPalette` (default `chrome`).

- `BuildableInfo` defaults (`OpenRA.Mods.Common/Traits/Buildable.cs:40,44`): `Icon = "icon"`, `IconPalette = "chrome"`.
- The icon is drawn by `WidgetUtils.DrawSpriteCentered` (`WidgetUtils.cs:86-89`) at **native sprite size, no scale-to-fit**. RA cells are `IconSize: 62,46` (`mods/ra/chrome/ingame-player.yaml:21,460`; engine default 64x48). A 128x128 cameo therefore renders ~2x oversized and clipped.

### Why PNG-direct works (no SHP needed) — the load-bearing fact
`SpriteRenderer.ResolveTextureIndex` (`OpenRA.Game/Graphics/SpriteRenderer.cs:118-130`):
```csharp
// PERF: Remove useless palette assignments for RGBA sprites ...
if (s.Channel == TextureChannel.RGBA && !pal.HasColorShift)
    return 0;
```
An RGBA PNG → `SpriteFrameType.Rgba32` → `SheetType.BGRA` → `TextureChannel.RGBA` (`SheetBuilder.cs:61-63,73`). With channel RGBA, the renderer returns palette index 0 and the sprite's own colors are sampled directly. The `IconPalette: chrome` reference is effectively ignored at draw time — it only has to resolve to a defined palette name so the `PaletteReference` lint passes (`chrome` is `PaletteFromFile` in `mods/ra/rules/palettes.yaml:30`, fine to reference). This is exactly how d2k ships full-color PNG cameos (`mods/d2k`: `SpriteFormats: ... PngSheet`, `defaults.yaml:638 Palette: chrome`).

`PngSheetLoader.cs` (the loader) treats a single-frame PNG as one frame (RegionsFromSlices, no FrameSize/FrameAmount embedded), exactly what we want for a 1-frame cameo.

## Existing cameo assets (`design/art/theater/cameos/`)
5 finished cameos + their raw sources: `bastion, bayrak, garuda, kunai, loewe, pathfinder` (note: pathfinder.png present = 6 finished; `*_raw.png` are the 1024px GPT originals).
- Finished `*.png`: **128x128, 8-bit RGBA, non-interlaced, has transparency** (already chroma-keyed). Format is correct (RGBA PNG); **size is wrong** for the sidebar.
- `*_raw.png`: 1024x1024 RGB (no alpha) — source art, not sidebar-ready.

**Sidebar-readiness verdict:** format YES, transparency YES, but they are 128x128 and the sidebar draws unscaled into 62x46. They must be re-emitted at ~64x48 (or 62x46). The existing `design/art/postprocess_cameo.py` already does the chroma-key + resize but only to a **square** `--size` (default 128); it needs a non-square target. Trivial change / one extra resize — not a hard blocker.

## PNG-direct vs SHP — recommendation: **PNG-direct**
- PNG-direct: keeps full-color modern art (the whole point of goal #1, HD visuals). Just needs `PngSheet` enabled in the ra manifest. No conversion, alpha preserved.
- SHP path (`./utility.sh ra --shp ...`, `ConvertPngToShpCommand.cs`): **rejects RGBA** — `"All frames must be paletted"` (requires `SpriteFrameType.Indexed8`). You'd first have to quantize each cameo to the RA `chrome`/`temperat.pal` 256-color palette, losing color fidelity and gaining a palette-matching step. Only worth it if you want them to live inside a `.mix` or match the retro palette exactly. **Do not use SHP for THEATER cameos.**

If you ever did want SHP: paletted-PNG → `./utility.sh ra --shp loewe.png` (output `loewe.shp`, all inputs must be Indexed8 and same size).

## The pipeline (PNG cameo → in-game sidebar icon)

1. **Dimensions/format:** RGBA PNG, **64x48** (or 62x46 to exactly fill the cell). Single frame. Transparent background already handled by `postprocess_cameo.py` chroma-key.
2. **Enable PngSheet in the ra manifest** (one-time, required). `mods/ra/mod.yaml:277`:
   ```
   SpriteFormats: ShpD2, ShpTD, TmpRA, TmpTD, ShpTS, PngSheet
   ```
   (`SpriteExtensions` at `:286` is only used by the Asset Browser, not runtime — optional to add `.png` there.)
3. **Where the file goes:** into the `ra|bits` folder package (mounted in `mod.yaml` FileSystem after content packages so it overrides). Use a subfolder to stay organized: `mods/ra/bits/theater/loewe-cameo.png`. (d2k precedent: sequences reference `Filename: bits/destroyabletiles/...png` and `Filename: shroud.png` — path is relative to mounted package roots.)
4. **Sequence YAML** — add a new sequences file `mods/ra/sequences/theater.yaml` and register it under `Sequences:` in `mod.yaml`. The icon sequence must be keyed under the actor's `RenderSprites.Image`. LOEWE inherits `Image: 3tnk`, which is shared with the stock Soviet Heavy Tank, so do **not** overwrite `3tnk`'s `icon` sequence — add a uniquely-named sequence instead:
   ```yaml
   3tnk:
       loewe-icon:
           Filename: theater/loewe-cameo.png
   ```
   `Filename` MUST include the `.png` extension (runtime resolves the exact filename; there is no extension fallback at load time).
5. **Buildable reference** — in `mods/ra/rules/theater-units.yaml`, point the unit at its new sequence:
   ```yaml
   LOEWE:
       Buildable:
           Icon: loewe-icon
   ```
   `IconPalette` can be left default (`chrome`); it's ignored for RGBA draw but must resolve for lint.
6. **Validate:** `./utility.sh ra --check-yaml` (catches bad `SequenceReference`/`PaletteReference`). Then a GUI run to eyeball the sidebar — sizing/centering can only be confirmed visually.

## Worked example — LOEWE end-to-end (Rhine Compact heavy tank)

a. Re-emit the cameo at sidebar size (one-off):
```bash
python3 design/art/postprocess_cameo.py \
  --in design/art/theater/cameos/loewe_raw.png \
  --out design/art/theater/cameos/loewe-64.png --size 64
# (script currently makes a square; either accept 64x64 centered in the 62x46 cell,
#  or extend it to emit 64x48 / 62x46. 64x64 will slightly overflow vertically.)
```
b. Place it in the bits package:
```bash
mkdir -p mods/ra/bits/theater
cp design/art/theater/cameos/loewe-64.png mods/ra/bits/theater/loewe-cameo.png
```
c. `mods/ra/mod.yaml` — add `PngSheet` to `SpriteFormats` (line 277) and register the new sequence file under `Sequences:`:
```
Sequences:
    ...
    ra|sequences/theater.yaml
```
d. New file `mods/ra/sequences/theater.yaml`:
```yaml
3tnk:
    loewe-icon:
        Filename: theater/loewe-cameo.png
```
e. `mods/ra/rules/theater-units.yaml`, LOEWE block (add the Icon line under Buildable):
```yaml
LOEWE:
    Inherits: 3TNK
    RenderSprites:
        Image: 3tnk
    Buildable:
        Queue: Vehicle
        BuildPaletteOrder: 91
        Prerequisites: tech.rhine_compact, weap
        Icon: loewe-icon
```
f. `./utility.sh ra --check-yaml` → expect clean. Launch via `./theater-play.sh`, build LOEWE's prereqs, confirm the cameo shows in the Vehicle queue.

Repeat steps a–b, d (one sequence entry), e (one Icon line) for bastion/bayrak/garuda/kunai/pathfinder, each keyed under its own inherited image (`ttnk`, `yak`, `v2rl`, `shok`, `e6`) with a unique sequence name (e.g. `bastion-icon`). For the THCOM structure cameo, same pattern under its structure image.

## Palette notes (summary)
- Keep `IconPalette: chrome` (default). For RGBA PNG it is ignored at draw (`SpriteRenderer.cs:126`) but must reference a real palette for lint. No new palette trait needed.
- Do NOT set `IconPaletteIsPlayerPalette: true` (that would try to remap, irrelevant for true-color and would append owner name).
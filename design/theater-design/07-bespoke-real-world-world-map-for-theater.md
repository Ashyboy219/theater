# bespoke_real_world_world_map_for_THEATER

**Executable now:** True

**Summary:** A programmatic generator is the right path and is highly tractable: RA maps are flat Rectangular grids (MaximumTerrainHeight==0), so a world landmask PNG maps directly to a 2-tile palette (Water=Template@1, Clear=Template@255) written via the same `new Map(...).Save(ZipFileLoader.Create(...))` path that ImportRedAlertMapCommand already uses. Build it as a new IUtilityCommand `--import-worldmap` that reads a downscaled landmask PNG, fills Tiles, places N mpspawns on the largest continents, and optionally auto-tiles coasts with the engine's shipped Beach LatTiler/Terraformer. No GUI editor needed; the only real art cost is sourcing/hand-cleaning the landmask PNG.

## Key decisions
- Build a new offline IUtilityCommand `--import-worldmap` (in OpenRA.Mods.Common/UtilityCommands/), NOT a runtime trait and NOT the GUI editor — input is a landmask PNG, output an .oramap.
- Exploit RA's flat Rectangular grid (MaximumTerrainHeight==0): no heightmap, no projection, no ramps. map.bin Height layer is skipped entirely; tiles are a direct MPos->TerrainTile write.
- Use the proven `new Map(...) -> set Tiles/Resources/actors -> Map.Save(ZipFileLoader.Create())` path from ImportRedAlertMapCommand; never hand-pack the binary bytes — let SaveBinaryData() serialize.
- Two-tile minimum palette: Water = Template@1 (TerrainTile(1,0)), Land = Template@255 Clear via Terraformer.PickTile for index variety.
- Ship Tier-0 (hard water/clear boundary) first; add coast beauty later by reusing the engine's shipped LatTiler/Terraformer/MultiBrush coast auto-tiler driven by the existing Beach lattice rules in map-generators.yaml.
- Spawn placement = connected-component label the land mask, erode by a margin, then greedy farthest-point sampling across continents; emit mpspawn ActorReferences exactly like ImportGen1MapCommand.

## Files to touch
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/UtilityCommands/ImportWorldMapCommand.cs (NEW — the generator; model on OpenRA.Mods.Cnc/UtilityCommands/ImportRedAlertMapCommand.cs + ImportGen1MapCommand.cs)
- /Users/ashishnaik/Projects/openra/mods/ra/maps/theater-world/ (NEW output dir — generated map.yaml/map.bin/map.png)
- /Users/ashishnaik/Projects/openra/design/art/ (NEW — store the source landmask PNG, e.g. world-landmask-256.png, alongside existing art pipeline)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/MapGenerator/Terraformer.cs (READ/REUSE — PickTile, coast helpers)
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/MapGenerator/LatTiler.cs (READ/REUSE — Tier-1 coast auto-tiling)
- /Users/ashishnaik/Projects/openra/mods/ra/rules/map-generators.yaml (READ — Beach/Water/segment config to reuse for coast tiling)
- /Users/ashishnaik/Projects/openra/mods/ra/tilesets/temperat.yaml (READ — Template@1 Water, Template@255 Clear, Beach template IDs)

## Risks
- Sourcing a usable landmask PNG is the real cost/'art' here — must be hand-cleaned at the target resolution (256px) so coastlines aren't single-cell noise; auto-downscaling a detailed coastline raster produces jagged, unplayable isthmuses without erosion/smoothing.
- Map size: a recognizable multi-continent world wants ~256x256 (65k cells). That is larger than stock skirmish maps and may stress AI pathfinding, minimap, and load time — validate performance; consider 192x192 as a compromise.
- Tier-0 hard water/clear boundary is visually crude up close (no beach blend). Acceptable at strategic zoom but a known polish gap; Tier-1 LatTiler integration adds real engineering time (learning MultiBrush/LatTiler rule format).
- Oceans make most of the map naval/air-only; if THEATER's early game is land-centric, large water expanses are dead space and continents must be sized/spaced so land routes or intended naval play actually connect spawns.
- Spawn fairness: real continents are unequal in size/resources, so naive placement yields imbalanced matches. Need explicit balancing (equal resource scatter near each spawn, symmetric-ish continent selection) — real-world geography is inherently asymmetric.
- The generator depends on internal MapGenerator APIs (Terraformer, LatTiler, MultiBrush) that are not a stable public contract and could change across upstream bleed merges; pin/justify the reuse and keep the Tier-0 path dependency-free as a fallback.

## Design

## Bespoke Real-World World Map for THEATER — Approach & Generator Sketch

### TL;DR recommendation
Write a **new offline `IUtilityCommand` (`--import-worldmap`)** that turns a downscaled world **landmask PNG** into a finished `.oramap`. This is far more tractable than it looks because **RA maps are flat** (`MapGrid: Type: Rectangular` in `mods/ra/mod.yaml:269` → `Grid.MaximumTerrainHeight == 0`). Flatness collapses every hard part of the binary format:
- `MPos == PPos`, no isometric projection, no ramps, no cliff height propagation.
- `map.bin` `heightsOffset == 0` — **you write no Height layer at all** (see `Map.SaveBinaryData()` `OpenRA.Game/Map/Map.cs:690`).
- Each cell is just `Tiles[new MPos(i,j)] = new TerrainTile(type, index)`.

So a "world map" is, at minimum, a 2-value palette: **Water** and **Clear (land)**. Everything else (coast prettiness, resources, props) is optional polish layered on top.

### 1. Can we generate it programmatically? — YES, no GUI editor required
There is **no existing PNG/heightmap→map importer** in the tree (confirmed: `OpenRA.Mods.Common/UtilityCommands/` and `OpenRA.Mods.Cnc/UtilityCommands/` contain only legacy INI/MPR importers and sprite-PNG tools). But the **programmatic Map-write path is fully proven** by `ImportGen1MapCommand`/`ImportRedAlertMapCommand`:

```csharp
// pattern lifted from ImportGen1MapCommand.cs:79–116
var terrainInfo = ModData.DefaultTerrainInfo["TEMPERAT"];
var map = new Map(ModData, terrainInfo, new Size(W, H)) {
    Title = "THEATER — World", Author = "THEATER",
    RequiresMod = ModData.Manifest.Id,
};
// ...write map.Tiles[...], map.Resources[...], actor & player MiniYaml...
map.SetBounds(new PPos(1,1), new PPos(W-2, H-2));
map.PlayerDefinitions = mapPlayers.ToMiniYaml();
map.Save(ZipFileLoader.Create(destPath));   // writes map.yaml + map.bin + map.png
```
`Map.Save()` (`Map.cs:634`) serializes `map.yaml` via the `YamlFields` table, `map.bin` via `SaveBinaryData()`, and a preview `map.png` for free. **You never touch the binary byte layout yourself** — you set the in-memory `CellLayer<TerrainTile>` and let the engine serialize. (The binary layout, for reference: `byte Format(=2)`, `ushort W`, `ushort H`, `uint tilesOffset`, `uint heightsOffset`, `uint resourcesOffset`, then W×H × (`ushort tileType`,`byte tileIndex`), then — flat map — skip heights, then W×H × (`byte resType`,`byte resDensity`). See `Map.cs:676–733`.)

### 2. The generator (concrete recipe)

**Input:** a landmask PNG sized exactly to the map (e.g. 256×256). Convention: **blue/dark = water, land = anything else**, or simply a 1-bit mask. Hand-authored or derived from a public-domain equirectangular coastline raster, downscaled in any image tool to the target cell grid. (Equirectangular world outline at 256×256 reads recognizably: Americas left, Africa/Europe center, Asia/Australia right.)

**Tile palette (TEMPERAT, from `mods/ra/tilesets/temperat.yaml`):**
| Role | Template | Notes |
|---|---|---|
| Water | `Template@1` (Id 1, `Categories: Water`, `Tiles: 0: Water`) | `new TerrainTile(1, 0)` |
| Land/Clear | `Template@255` (PickAny, 16 Clear variants) | use `Terraformer.PickTile(random, 255)` to randomize the index, matching `ClearMapGenerator.Generate` `ClearMapGenerator.cs:90` |
| Coast (optional) | 54 `Categories: Beach` templates (`Template@3..`) | water↔land transition; auto-tiled, see step 4 |

**Algorithm:**
```
1. Load PNG -> bool[W,H] isLand   (threshold on blue channel or palette)
2. map = new Map(ModData, terrainInfo, new Size(W,H))
3. foreach (i,j): map.Tiles[new MPos(i,j)] =
        isLand[i,j] ? terraformer.PickTile(rng, 255)   // clear land
                    : new TerrainTile(1, 0)             // water
4. Connected-components label isLand (4-connectivity flood fill).
   Keep components with area >= minContinentCells -> "continents".
5. Spawn placement: for each of N spawns, pick cells that are
   (a) land, (b) >= margin from any water cell (erode the land mask
       by `margin` so spawns aren't on a 1-cell isthmus),
   (c) spread across DIFFERENT continents first, then spread within,
   maximizing pairwise distance (greedy farthest-point sampling).
   Emit an "mpspawn" ActorReference per chosen cell (see theater-phase0b
   map.yaml Actor52..Actor60 for the exact shape).
6. (optional) scatter `mine`/ore for the territory economy on land.
7. SetBounds to (1,1)..(W-2,H-2); build MapPlayers (NeutralN + MultiN).
8. map.Save(ZipFileLoader.Create("mods/ra/maps/theater-world/")).
```
Spawn/player/actor MiniYaml emission is exactly the `ActorReference(...).Save()` + `"Actor" + n` pattern in `ImportGen1MapCommand` (`:298–319`) and visible in the existing `theater-phase0b/map.yaml`.

### 3. Coastlines: three tiers of polish (pick based on time budget)
- **Tier 0 (ship first):** Water + Clear only, hard boundary. Fully playable, reads as continents at minimap zoom. RA's actual `clear1`/`w1` already abut acceptably at game zoom for a strategic-scale map. **This is `executableNow`.**
- **Tier 1:** Auto-tile the coast using the engine's shipped **`LatTiler`** (`OpenRA.Mods.Common/MapGenerator/LatTiler.cs`) — it consumes the tileset's Beach lattice rules and replaces water/land boundary cells with the correct Beach transition template given the 4 neighbors. The `Terraformer`/`MultiBrush` machinery (`MapGenerator/`) and the `BeachSegmentTypes: Beach` config already wired in `mods/ra/rules/map-generators.yaml:69` are directly reusable as a post-pass over the boundary cells.
- **Tier 2:** Reuse `ClassicMapGenerator`'s coast tiling end-to-end. Not recommended for v1 — it is noise/symmetry-driven and **cannot reproduce real continents**; you'd be fighting it. Borrow its helpers, not its driver.

### 4. Tileset limits & how "continents" read at RA scale
- **1024 world-units = 1 cell.** A 256×256 map ≈ 65k cells — large but within engine limits (stock skirmish maps run 128–192). Real continents at this downscale read as recognizable blobs; you are making a *stylized strategic* world, not a sim-accurate globe.
- **TEMPERAT has no "ocean depth" or open-sea decoration tiles** beyond flat Water (`Template@1`) + Water Cliffs (52 templates, for cliff-walled water, not needed). Deep ocean is just uniform Water — fine for strategy.
- **SNOW tileset** has the same Water(1)/Clear(255)/Beach structure (`mods/ra/tilesets/snow.yaml`), so a "winter world" or polar-cap variant is a one-line tileset swap.
- **Naval matters:** Water cells are pathable only by naval/air. If THEATER wants land-only early game, keep spawns inland and oceans as natural barriers — exactly the "territory as barrier" verb-design principle. If you want amphibious play, the Water tiles already support it natively.
- **PickAny indices:** `Template@255` is `PickAny: True` — randomize the `byte index` (0–15) per land cell via `Terraformer.PickTile` so the land isn't visibly tiled; do NOT write index 255 (the engine special-cases `byte.MaxValue` into `i%4 + j%4*4` at load, `Map.cs:394`, which is fine but less controllable).

### Validation loop
After generating, run `./utility.sh ra --check-yaml` against the new map (or drop it in `mods/ra/maps/`) — the same gate the existing THEATER slice passes. The generator itself is testable headlessly; only the *visual* read needs a GUI run.
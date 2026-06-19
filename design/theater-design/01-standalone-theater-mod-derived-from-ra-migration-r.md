# Standalone `theater` mod derived from `ra`: migration recipe

**Executable now:** True

**Summary:** Create a standalone mod at `mods/theater/` whose `mod.yaml` mounts the `ra` mod's folder as a system package (`$ra: ra`, giving `ra|` access) and `Include:`s nothing—it lists ra's rules/sequences/etc. directly via the `ra|` prefix while owning the theater-*.yaml files locally as `theater|`. No engine C# changes are needed: the theater traits live in `OpenRA.Mods.Common.dll`, which the new mod's `Assemblies` line still loads. Dropping the directory is enough for `Game.Mod=theater` to discover it; `./theater-play.sh Game.Mod=theater` launches it on this machine.

## Key decisions
- Approach (b): thin mods/theater that mounts the ra folder via `$ra: ra` (giving `ra|` prefix access) and references ra's baseline files by prefix, while owning theater-*.yaml locally as `theater|`. Chosen over copy-and-fold because divergence is incremental and ra stays the single source of truth; graduate to full ownership (a) per-category by swapping `ra|` lines for `theater|` as content is replaced.
- No engine C# changes: all THEATER traits (TheaterCommands, AttackMoveByDefault, DevelopsWhileHeld, SupplyNetwork, SupplyNode, DeveloperMode) are namespace OpenRA.Mods.Common.* and ship in OpenRA.Mods.Common.dll, which the new mod's unchanged `Assemblies:` line loads.
- Keep FileSystem: ContentInstallerFileSystem with ContentInstallerMod: ra-content and share ra's installed content dir (~^SupportDir|Content/ra/v2/) — no separate theater content install.
- Discovery is automatic: dropping mods/theater/mod.yaml into the live mods/ search path is enough for Game.Mod=theater (InstalledMods enumerates mods/ subdirs; mods load in-place, not copied to bin).
- Keep SupportsMapsFrom: ra so existing RequiresMod: ra maps remain loadable; migrated maps get their headers rewritten to RequiresMod: theater + Rules: theater|...
- Only the manifest and migrated map.yaml headers need `ra|`->`theater|` rewrites; the theater rule/lua bodies contain no ra| prefixes (verified) and resolve through the mounted ra baseline.
- Launch via ./theater-play.sh Game.Mod=theater — the explicit Game.Mod arg is mandatory on macOS (no zenity fallback) and theater-play.sh sets the keg-only dotnet PATH.

## Files to touch
- /Users/ashishnaik/Projects/openra/mods/theater/mod.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/rules/theater-factions.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/rules/theater-units.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/rules/theater-structures.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/rules/theater.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/fluent/theater.ftl
- /Users/ashishnaik/Projects/openra/mods/theater/scripts/theater-setup.lua
- /Users/ashishnaik/Projects/openra/mods/theater/maps/theater-phase0/map.yaml
- /Users/ashishnaik/Projects/openra/mods/theater/maps/theater-phase0b/map.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/mod.yaml

## Risks
- Hard dependency on mods/ra/ existing on disk: the `$ra: ra` mount fails (InvalidOperationException 'Could not load mod ra') if ra is removed or renamed. Acceptable while ra ships in-repo; it is the trigger to graduate to copy-and-own (approach a).
- Rewriting migrated map.yaml headers (RequiresMod/Rules) changes each map's SHA1 UID (Map.cs:284-292), invalidating any replays/saves bound to the old UID. Expected for dev maps; do the rewrite once and treat the new UID as canonical.
- Last-wins override ordering must be respected: any theater| rule/sequence that overrides a stock actor must be listed AFTER the corresponding ra| line in mod.yaml, or the override silently loses.
- If LoadScreen images are later pointed at theater|uibits without adding theater|uibits to SystemPackages and shipping the PNGs, the load screen sprite lookup fails at startup.
- make check is pre-existingly RED on this machine (IDE0055 in stock files per CLAUDE.md); gate on ./utility.sh theater --check-yaml + a targeted Debug-build warning grep instead, not on make check.
- Don't lose the local OpenRA.Game/Map/Map.cs Array.Empty<byte>() build patch on rebase — unrelated to the mod but required for the engine to compile under SDK 8.0.128.
- If Metadata.Title is left as a literal instead of a fluent key, AllowUnusedFluentMessagesInExternalPackages:false plus fluent lint will still pass, but the lobby/window title won't localize — acceptable, just a known limitation.

## Design

## Recommendation: Approach (b) now → (a) later — a thin `theater` mod that MOUNTS `ra`

I recommend a **thin `mods/theater/` that mounts the `ra` package (`$ra: ra`) and references ra's rule/sequence/weapon files by their `ra|` prefix**, while **owning the THEATER files locally under `theater|`**. This is *not* an `Include:` of ra's `mod.yaml` (that mechanism inlines a YAML *file* into the manifest, not another mod's manifest) — it is the same pattern `cnc`/`d2k` use against their *own* package, except we additionally mount a *second* mod (`ra`) and reference it by prefix.

### Why (b) over (a) — copy-and-fold

I verified the mount engine (`OpenRA.Game/FileSystem/FileSystem.cs:84-115`): in a `FileSystem:` entry like `$ra: ra`, the key `$ra` makes the loader strip `$`, look up `installedMods["ra"]`, and mount **ra's whole folder package** under the explicit name `ra`. That is exactly how the *current* ra mod resolves its own `ra|...` references (`$ra: ra` in `mods/ra/mod.yaml:13`). A `theater` mod that also does `$ra: ra` gets the entire ra asset/rules tree addressable as `ra|...` **for free, with zero file copying** — sprites, palettes, audio, tilesets, cursors, maps, ZoodRangmah font, loadscreen PNGs. ra's content `.mix` packages still install to `~^SupportDir|Content/ra/v2/` exactly once and are shared.

- **Approach (a) — copy `mods/ra` → `mods/theater`** duplicates ~all of ra's YAML and forces you to maintain two divergent copies; every upstream `bleed` rebase doubles. Premature: most of ra is still our baseline.
- **Approach (b)** keeps ra as the single source of truth for the shared baseline, and lets THEATER **selectively override**. Because mount order in `Rules:`/`Sequences:`/etc. is *last-wins* (later list entries override earlier — see `FileSystem.GetFromCache` using `LastOrDefault`, and `Ruleset` merge order), we can append `theater|rules/*.yaml` *after* the `ra|...` entries to override any actor/trait without touching ra. As we replace RA over time (new units, 4X, new art), we simply add more `theater|...` files and progressively drop `ra|...` lines — **no big-bang rewrite, and the path to full ownership (a) is incremental**: when a category is fully replaced, swap its `ra|` line for `theater|`.

The one real cost of (b): a hard dependency on `mods/ra/` existing on disk. That is fine here (ra ships in this repo) and is already implied by reusing ra's art. If you ever ship THEATER without ra, that is the moment to graduate to (a).

> NOTE on `FileSystem:` type. ra uses `ContentInstallerFileSystem` (`ContentInstallerFileSystemLoader`, requires `ContentInstallerMod`). For a derived mod we can keep `ContentInstallerFileSystem` and point `ContentInstallerMod: ra-content` so missing RA assets trigger the *same* installer ra uses. (`DefaultFileSystem` is the alternative but gives no installer; keep the installer so first-run on a clean machine still works.)

---

## The EXACT `mods/theater/mod.yaml`

Full file below. **Bold-comment lines flag every field that differs from `mods/ra/mod.yaml`.** Everything else is byte-identical to ra and resolves via the mounted `ra|`/`common|` prefixes.

```yaml
Metadata:
	Title: THEATER                          # CHANGED (ra: mod-title via fluent). Use a fluent key if you want it localized; literal is fine.
	Version: {DEV_VERSION}                  # SAME — make version stamps mods/*/mod.yaml, so this new file is covered automatically.
	Website: https://www.openra.net
	WebIcon32: https://www.openra.net/images/icons/ra_32x32.png
	WindowTitle: THEATER

PackageFormats: Mix                         # SAME — needed because we mount ra's .mix content packages.

FileSystem: ContentInstallerFileSystem
	SystemPackages:
		^EngineDir
		$theater: theater                   # CHANGED — mount THIS mod's folder as `theater|` (was `$ra: ra`).
		$ra: ra                             # ADDED — mount the ra mod folder so `ra|...` resolves to ra's tree.
		^EngineDir|mods/common: common
		~^SupportDir|Content/ra/v2/: content   # SAME — share ra's installed content dir (do NOT make a separate theater content dir).
		common|scripts
		ra|scripts
		theater|scripts                     # ADDED — our own Lua (theater-setup.lua) lives here.
		ra|uibits                           # SAME — loadscreen PNGs + ui art come from ra.
	ContentPackages:                        # SAME as ra — these are the installed RA .mix files, shared.
		content|allies.mix
		content|conquer.mix
		content|interior.mix
		content|lores.mix: lores
		content|hires.mix
		content|local.mix
		content|russian.mix
		content|snow.mix
		content|sounds.mix
		content|speech.mix
		content|temperat.mix
		content|expand
		content|expand/expand2.mix
		content|expand/lores1.mix
		content|expand/hires1.mix
		content|cnc/desert.mix
		~content|movies
		~content|scores.mix
		~content|general.mix
		ra|bits                             # SAME — ra's mod-provided override sprites (must load after content).
		ra|bits/desert
	RequiredContentFiles:                   # SAME as ra (copy the full block verbatim — the 22 expand/*.aud lines).
		content|expand/chrotnk1.aud
		# ... (copy lines 43-65 from mods/ra/mod.yaml verbatim) ...
		content|expand/myes1.aud
	ContentInstallerMod: ra-content         # CHANGED target intent (was the same string but conceptually now "borrow ra's installer").

MapFolders:
	theater|maps: System                    # CHANGED — our maps move here (was ra|maps).
	~ra|maps: System                        # ADDED (optional) — also expose ra's stock maps; drop later if undesired.
	~^SupportDir|maps/theater/{DEV_VERSION}: User   # CHANGED — per-mod user map dir keyed by mod id `theater`.

Rules:
	ra|rules/misc.yaml                      # all ra|... lines SAME as ra (baseline), MINUS the three theater-* lines.
	ra|rules/ai.yaml
	ra|rules/player.yaml
	ra|rules/palettes.yaml
	ra|rules/world.yaml
	ra|rules/defaults.yaml
	ra|rules/vehicles.yaml
	ra|rules/husks.yaml
	ra|rules/structures.yaml
	ra|rules/infantry.yaml
	ra|rules/civilian.yaml
	ra|rules/decoration.yaml
	ra|rules/aircraft.yaml
	ra|rules/ships.yaml
	ra|rules/fakes.yaml
	ra|rules/map-generators.yaml
	theater|rules/theater-factions.yaml     # CHANGED prefix ra| -> theater| (file moves into this mod).
	theater|rules/theater-units.yaml        # CHANGED prefix.
	theater|rules/theater-structures.yaml   # CHANGED prefix.

Sequences:                                  # ENTIRE block SAME as ra (all ra|sequences/*).
	ra|sequences/ships.yaml
	ra|sequences/vehicles.yaml
	ra|sequences/structures.yaml
	ra|sequences/infantry.yaml
	ra|sequences/aircraft.yaml
	ra|sequences/misc.yaml
	ra|sequences/decorations.yaml

TileSets:                                   # SAME as ra.
	ra|tilesets/snow.yaml
	ra|tilesets/interior.yaml
	ra|tilesets/temperat.yaml
	ra|tilesets/desert.yaml

Cursors:
	ra|cursors.yaml                         # SAME.

Chrome:
	ra|chrome.yaml                          # SAME.

Assemblies: OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll   # SAME — this is why TheaterCommands/SupplyNetwork/etc. load with no C# change.

ChromeLayout:                               # ENTIRE block SAME as ra (common|... + the ra|chrome/* overrides). Copy verbatim.
	# ... copy lines 117-172 from mods/ra/mod.yaml unchanged ...

FluentMessages:
	common|fluent/common.ftl                # SAME ...
	common|fluent/chrome.ftl
	common|fluent/ingame-debug.ftl
	common|fluent/hotkeys.ftl
	common|fluent/rules.ftl
	ra|fluent/ra.ftl
	ra|fluent/chrome.ftl
	ra|fluent/hotkeys.ftl
	ra|fluent/rules.ftl
	theater|fluent/theater.ftl              # CHANGED prefix ra| -> theater| (file moves).

AllowUnusedFluentMessagesInExternalPackages: false   # SAME.

Weapons:                                    # SAME as ra (all ra|weapons/*).
	ra|weapons/explosions.yaml
	ra|weapons/ballistics.yaml
	ra|weapons/missiles.yaml
	ra|weapons/other.yaml
	ra|weapons/smallcaliber.yaml
	ra|weapons/superweapons.yaml

Voices:
	ra|audio/voices.yaml                    # SAME.

Notifications:
	ra|audio/notifications.yaml             # SAME.

Music:
	ra|audio/music.yaml                     # SAME.

Hotkeys:                                    # SAME as ra.
	common|hotkeys/game.yaml
	common|hotkeys/observer.yaml
	common|hotkeys/production-common.yaml
	common|hotkeys/supportpowers.yaml
	common|hotkeys/viewport.yaml
	common|hotkeys/chat.yaml
	common|hotkeys/editor.yaml
	common|hotkeys/control-groups.yaml
	ra|hotkeys.yaml

LoadScreen: LogoStripeLoadScreen           # SAME class (verified: OpenRA.Mods.Common/LoadScreens/LogoStripeLoadScreen.cs).
	Image: ra|uibits/loadscreen.png         # SAME — reuse ra art (mounted via ra|uibits). Swap to theater| art later.
	Image2x: ra|uibits/loadscreen-2x.png
	Image3x: ra|uibits/loadscreen-3x.png

ServerTraits:                               # SAME as ra.
	LobbyCommands
	SkirmishLogic
	PlayerPinger
	MasterServerPinger

ChromeMetrics:
	common|metrics.yaml                     # SAME.
	ra|metrics.yaml

Fonts:                                      # SAME as ra — Title font is ra|ZoodRangmah.ttf (resolves via ra| mount).
	# ... copy lines 231-263 from mods/ra/mod.yaml unchanged ...

Missions:
	ra|missions.yaml                        # SAME for now; replace with theater|missions.yaml when campaigns diverge.

MapGrid:
	Type: Rectangular                       # SAME.

DefaultOrderGenerator: UnitOrderGenerator   # SAME.

SupportsMapsFrom: ra                        # KEEP — lets THEATER load maps authored as RequiresMod: ra (your phase0 maps). MapCompatibility = {theater, ra}.

SoundFormats: Aud, Wav                      # SAME.
SpriteFormats: ShpD2, ShpTD, TmpRA, TmpTD, ShpTS   # SAME.
VideoFormats: Vqa, Wsa                      # SAME.
TerrainFormat: DefaultTerrain               # SAME.
SpriteSequenceFormat: ClassicTilesetSpecificSpriteSequence   # SAME.

AssetBrowser:                               # SAME as ra.
	# ... copy lines 285-288 unchanged ...

GameSpeeds:                                 # SAME as ra — copy lines 290-316 unchanged.
	# ...

DiscordService:
	ApplicationId: 699222659766026240        # SAME (or change to a THEATER app id later).
```

### Field-by-field summary (what changes vs ra)
| Field | Action |
|---|---|
| `Metadata.Title/WindowTitle` | **Change** to THEATER (literal or new fluent key). |
| `Metadata.Version` | Keep `{DEV_VERSION}` — `make version` stamps it. |
| `FileSystem.SystemPackages` | `$ra: ra` → **`$theater: theater` + add `$ra: ra`**; add `theater|scripts`; keep `ra|scripts`, `ra|uibits`. |
| `FileSystem.ContentPackages` / `RequiredContentFiles` | **Same** (share ra's content). |
| `ContentInstallerMod` | `ra-content` (reuse ra's installer). |
| `MapFolders` | `ra|maps`→**`theater|maps`** + optional `~ra|maps`; user dir keyed `maps/theater/`. |
| `Rules` | `ra|...` baseline **same**, three `theater-*` lines re-prefixed **`theater|`**. |
| `Sequences/TileSets/Cursors/Chrome/Weapons/Voices/Notifications/Music/Hotkeys/ChromeLayout/ChromeMetrics/Fonts/Missions` | **All `ra|`/`common|` — unchanged** (resolve via mounts). |
| `FluentMessages` | `theater.ftl` line re-prefixed **`theater|`**; rest same. |
| `Assemblies` | **Same** — `OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll` (loads all THEATER C#). |
| `LoadScreen` | Same class + ra art (de-blur/replace later via `theater|` images). |
| `SupportsMapsFrom: ra` | **Keep** for map compatibility with existing `RequiresMod: ra` maps. |

---

## 3. Discovery — how `Game.Mod=theater` finds it

**Dropping `mods/theater/` with a valid `mod.yaml` is sufficient. No code change, no registry edit.** Verified path:
- `Game.cs:392-395`: `modSearchPaths = [Path.Combine(Platform.EngineDir, "mods")]`. With `launch-game.sh` passing `Engine.EngineDir=".."` (repo root, parent of `bin/`), the search path is `<repo>/mods/`.
- `InstalledMods.GetCandidateMods` (`InstalledMods.cs:33-55`) enumerates every subdir of `mods/` and keys each by its folder name; `LoadMod` accepts any dir containing `mod.yaml`. So `mods/theater/mod.yaml` ⇒ mod id `theater`.
- Mods are **loaded in-place from `mods/`, not copied into `bin/`** (Makefile never copies mod data for dev runs), so no rebuild/copy step is needed after adding files.
- `ExternalMods.Register` (the `~/Library/.../OpenRA/ModMetadata` entries) only matters for the cross-mod *launcher list / server browser*; in-process `Game.Mod=theater` does not require it. It self-populates on first successful launch.

Launch on this machine: `./theater-play.sh Game.Mod=theater` (theater-play.sh exports the keg-only dotnet PATH, and because the arg contains `Game.Mod=` it skips the zenity picker that has no fallback on macOS).

---

## 4. Migrating existing THEATER content in

**Files that MOVE into `mods/theater/` (the only files that physically move):**
- `mods/ra/rules/theater-factions.yaml` → `mods/theater/rules/theater-factions.yaml`
- `mods/ra/rules/theater-units.yaml` → `mods/theater/rules/theater-units.yaml`
- `mods/ra/rules/theater-structures.yaml` → `mods/theater/rules/theater-structures.yaml`
- `mods/ra/rules/theater.yaml` (map-scoped ruleset) → `mods/theater/rules/theater.yaml`
- `mods/ra/fluent/theater.ftl` → `mods/theater/fluent/theater.ftl`
- `mods/ra/scripts/theater-setup.lua` → `mods/theater/scripts/theater-setup.lua`
- `mods/ra/maps/theater-phase0/`, `mods/ra/maps/theater-phase0b/` → `mods/theater/maps/...`

**Path rewrites required after moving:**
- In `mod.yaml`: the three `ra|rules/theater-*.yaml` → `theater|rules/...`, and `ra|fluent/theater.ftl` → `theater|fluent/theater.ftl` (done in the YAML above).
- In each moved map's `map.yaml`: `Rules: ra|rules/theater.yaml` → `Rules: theater|rules/theater.yaml`, and `RequiresMod: ra` → `RequiresMod: theater`. (The theater-phase0b map currently has both.)
- Inside `theater.yaml` / `theater-*.yaml` / `theater-setup.lua`: I grepped these and found **no `ra|` prefixes inside the bodies** — they reference engine traits and stock actor names (FACT, HARV, etc.), which resolve through the mounted ra baseline. So **only the manifest + map headers need rewriting**, not the rule bodies.

**Files that STAY shared (NOT moved):**
- All engine C#: `OpenRA.Mods.Common/Commands/TheaterCommands.cs`, `Traits/AttackMoveByDefault.cs`, `Traits/DevelopsWhileHeld.cs`, `Traits/World/SupplyNetwork.cs`, `Traits/SupplyNode.cs`, and the `DeveloperMode.cs` changes. **Confirmed namespace `OpenRA.Mods.Common.*`** — these compile into `OpenRA.Mods.Common.dll`, which the theater mod's `Assemblies:` line loads identically to ra. **No C# edits, no project changes.** `git mv` is not needed for any C#.
- All ra art/audio/tilesets/cursors/fonts/loadscreen: stay in `mods/ra/`, reused via the `ra|` mount.

---

## 5. Pitfalls (each with the mitigation)

1. **`{DEV_VERSION}` stamping** — leave the literal `{DEV_VERSION}` in `mods/theater/mod.yaml`. The engine reads `EngineVersion` from the repo `VERSION` file (`Game.cs:359`); `make version` rewrites `mods/*/mod.yaml` at package time and the glob already includes the new file. Do **not** hand-substitute it.
2. **LoadScreen art/class** — `LogoStripeLoadScreen` exists in `OpenRA.Mods.Common`; its images come from `ra|uibits/...` which is mounted in SystemPackages. Verified the class path. If you later point `Image:` at `theater|uibits/...`, you must add `theater|uibits` to SystemPackages and ship the PNGs, or the load screen will fail to find the sprite.
3. **Map UID stability** — `Map.cs:284-292` computes the UID as SHA1 of the map's file *bytes*. Moving a map directory verbatim keeps the UID; **editing `map.yaml` (the `RequiresMod`/`Rules` rewrites above) WILL change the UID**. That is expected and fine for local/dev maps (it just means replays/saves tied to the old UID won't match). Do the header rewrite once, then treat the new UID as canonical.
4. **The local `Map.cs` build patch** — already applied and is **engine-level (OpenRA.Game), mod-agnostic**; the new mod needs nothing extra. Just don't lose it on rebase (noted in CLAUDE.md).
5. **`--check-yaml` for the new mod** — run `./utility.sh theater --check-yaml` (after exporting the keg-only dotnet PATH, or just run it through an env that has it). This lints the *theater* manifest+rules specifically and is the fastest gate that the `theater|`/`ra|` prefixes all resolve. Because the engine is already built, no rebuild is required to add the mod or run check-yaml.
6. **macOS launch / no zenity** — always pass `Game.Mod=theater` explicitly (theater-play.sh and launch-game.sh both bail to a no-op/zenity path otherwise). `./theater-play.sh Game.Mod=theater` is the canonical command; it sets `DOTNET_ROOT`/`PATH` for the keg-only dotnet.
7. **`SupportsMapsFrom: ra`** — keep it, or the existing `RequiresMod: ra` maps you migrate (before you rewrite their headers) won't appear; with it, `MapCompatibility = {theater, ra}` (verified `Manifest.cs:146-151`).
8. **`make check` is RED on this machine for pre-existing IDE0055 reasons** (per CLAUDE.md) — don't treat that as a theater regression; gate on `./utility.sh theater --check-yaml` + a Debug build grep for your filenames.

---

## 6. Ordered, copy-pasteable task list (keeps build green, ends launchable)

```bash
cd /Users/ashishnaik/Projects/openra

# 1. Scaffold the new mod tree (in-place; mods/ is the live search path).
mkdir -p mods/theater/rules mods/theater/fluent mods/theater/scripts mods/theater/maps

# 2. MOVE THEATER-owned data into the new mod (git mv keeps history).
git mv mods/ra/rules/theater-factions.yaml    mods/theater/rules/theater-factions.yaml
git mv mods/ra/rules/theater-units.yaml       mods/theater/rules/theater-units.yaml
git mv mods/ra/rules/theater-structures.yaml  mods/theater/rules/theater-structures.yaml
git mv mods/ra/rules/theater.yaml             mods/theater/rules/theater.yaml
git mv mods/ra/fluent/theater.ftl             mods/theater/fluent/theater.ftl
git mv mods/ra/scripts/theater-setup.lua      mods/theater/scripts/theater-setup.lua
git mv mods/ra/maps/theater-phase0            mods/theater/maps/theater-phase0
git mv mods/ra/maps/theater-phase0b           mods/theater/maps/theater-phase0b

# 3. Author mods/theater/mod.yaml using the FULL template in this report
#    (start from a copy of mods/ra/mod.yaml, then apply every CHANGED/ADDED line above).
#    cp mods/ra/mod.yaml mods/theater/mod.yaml   # then edit per the table.

# 4. REVERT ra's manifest back to vanilla (remove the 3 theater rule lines + the theater.ftl line
#    from mods/ra/mod.yaml lines ~90-92 and ~184) so ra is clean again and theater fully owns them.

# 5. Rewrite the migrated map headers: in each mods/theater/maps/*/map.yaml change
#       RequiresMod: ra            -> RequiresMod: theater
#       Rules: ra|rules/theater.yaml -> Rules: theater|rules/theater.yaml

# 6. Lint the new mod (export the keg-only dotnet first if not already on PATH).
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
./utility.sh theater --check-yaml      # must pass; fixes any unresolved theater|/ra| path
./utility.sh ra --check-yaml           # confirm ra still clean after the revert

# 7. Lua syntax gate for the moved script.
make check-scripts                     # luac -p over mods/*/scripts (now includes theater)

# 8. Launch it.
./theater-play.sh Game.Mod=theater
```

No `dotnet build` is needed for the mod itself (all C# already compiled into `OpenRA.Mods.Common.dll`). If you *did* touch any C#, rebuild with `make` first; otherwise steps 6-8 are the whole verification loop.
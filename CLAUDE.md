# OpenRA — Agent Working Map

A Libre/Free real-time-strategy **game engine** (recreates Westwood classics: Red Alert, Tiberian Dawn, Dune 2000). ~236k LOC of **C# targeting .NET 8**. Deterministic lock-step multiplayer simulation; everything game-specific is data-driven via a custom YAML (MiniYaml) + a trait (entity-component) system.

- Upstream: https://github.com/OpenRA/OpenRA  (branch model: `bleed` = dev)
- Wiki/docs: https://github.com/OpenRA/OpenRA/wiki , trait docs https://docs.openra.net
- License: GPLv3. `VERSION` = `{DEV_VERSION}` placeholder (substituted at package time).

## Build / Run / Test

Requires **.NET 8 SDK** plus native libs (SDL2, OpenAL, FreeType, Lua 5.1 — auto-downloaded via NuGet on x64; system libs needed on arm64/`unix-generic`).

```bash
make                       # build (Release) -> bin/.  Wraps `dotnet build`.
make check                 # StyleCop/Roslynator + yaml/lint checks (what CI runs)
make test                  # build + run lint/yaml checks
make tests                 # nunit unit tests (OpenRA.Test)
make check-scripts         # luac syntax check on mod Lua (needs lua5.1)
make clean
./theater-play.sh                 # run Red Alert (sets the keg-only dotnet PATH for you) — USE THIS ON THIS MACHINE
./launch-game.sh                  # upstream launcher — FAILS here unless dotnet is on PATH AND a Game.Mod= arg is given (no zenity on macOS)
./launch-game.sh Game.Mod=cnc     # Tiberian Dawn;  d2k = Dune 2000; ts = Tiberian Sun (experimental)
./utility.sh <mod> --<command>    # run an IUtilityCommand (docs gen, map import, lint…)
```
CI (`.github/workflows/ci.yml`): `make check && make tests` then `make TREAT_WARNINGS_AS_ERRORS=true test` on Linux/macOS/Windows with .NET 8.
Windows uses `make.ps1` (PowerShell) via `make.cmd`.

> SETUP (this machine, arm64 macOS): .NET 8 SDK installed via Homebrew but **keg-only** — it is NOT on PATH by default. Every build/utility shell must first export:
> ```bash
> export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
> export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
> ```
> Makefile auto-detects `osx-arm64`. Build verified ✅ (all 9 assemblies → `bin/`), and `./utility.sh ra --check-yaml` passes. The repo is a fresh shallow clone (no full git history).
>
> LOCAL PATCH: `OpenRA.Game/Map/Map.cs:286` was changed from `CryptoUtil.SHA1Hash([])` to `CryptoUtil.SHA1Hash(Array.Empty<byte>())` — the upstream bleed tip's empty collection-expression is ambiguous between the `byte[]`/`string` overloads under SDK 8.0.128 and fails to compile. Behavior-identical fix; not yet upstreamed.
>
> CAVEAT — `make check` is RED on this machine for pre-existing reasons: SDK 8.0.128's analyzers flag ~3,300 `IDE0055` formatting "errors" in untouched stock files (CVec.cs, WAngle.cs, CRC32.cs…) that upstream CI's SDK does not. This is an environment artifact, NOT your changes. To check whether *your* C# is clean, build Debug WITHOUT warnaserror (`dotnet build OpenRA.sln -c Debug`) and grep the warnings for your filenames. To validate a NEW/opt-in ruleset against the engine, temporarily add it to `mods/<mod>/mod.yaml` `Rules:`, run `./utility.sh <mod> --check-yaml`, then `git checkout` the mod.yaml.

## THEATER — contemporary 4X-flavored reimagining (in progress)

The user's flagship goal: a modern-military reimagining of RA with 4X-lite depth (diplomacy/research/real-ore economy/active defense), real-country blocs, de-blurred "retro but revamped" HD visuals, and a real-mineral Codex. Full design + honest "is it fun" validation in `design/contemporary-openra-design.md`; broader lore/expansion map in `design/game-design-vision.md`; raw validation data in `design/_wf_result.json`.
- **Art pipeline (proven):** GPT-image (`gpt-image-2`) for single-angle art (cameos/concept/codex/UI) + 3D Blender turntable for 32-facing unit bodies. Scripts: `design/art/generate_image.py`, `design/art/postprocess_cameo.py`. Gotcha: gpt-image-2 has no transparent-bg support → generate on flat magenta, chroma-key out.
- **Phase 0 (built + engine-validated):** "territory IS the economy" thesis slice. New traits `OpenRA.Mods.Common/Traits/World/SupplyNetwork.cs` (deterministic connectivity manager) + `OpenRA.Mods.Common/Traits/SupplyNode.cs`, and opt-in ruleset `mods/ra/rules/territory-economy.yaml` (harvest→0, capturable supply-connected Control Nodes). Compiles, analyzer-clean, passes `--check-yaml`. Test recipe in `design/PHASE0-README.md`. The fun-gate playtest itself needs a GUI run (not doable headlessly).

## Solution layout (`OpenRA.sln`)

| Project | Role |
|---|---|
| **OpenRA.Game** | Core engine: sim loop, actors/traits, network/lock-step, rendering orchestration, MiniYaml, mod loading, widgets base, Lua host. (226 .cs) |
| **OpenRA.Mods.Common** | The bulk of gameplay: ~1080 .cs — traits, activities, warheads, projectiles, widgets+logic, pathfinder, lint rules, utility commands, update rules. Shared by all mods. |
| **OpenRA.Mods.Cnc** | C&C/RA-specific traits & importers (140 .cs): chronoshift, disguise, infiltration, GPS, superpowers. |
| **OpenRA.Mods.D2k** | Dune 2000 specifics (23 .cs): sandworm, spice bloom. |
| **OpenRA.Platforms.Default** | SDL2 + OpenGL + OpenAL + FreeType backend (18 .cs). Pluggable via `IPlatform`. |
| **OpenRA.Server** | Thin dedicated-server entry point (server core lives in OpenRA.Game/Server). |
| **OpenRA.Utility** | CLI host that reflects over `IUtilityCommand`s. |
| **OpenRA.Test** | NUnit tests. |
| **OpenRA.Launcher / OpenRA.WindowsLauncher** | Native launchers. |
| `mods/` | Data (YAML/Lua/maps/art) for each game: `ra cnc d2k ts` + `*-content` + shared `common`/`all`. |
| `glsl/` | Shaders. `packaging/` | per-OS install/packaging. |

## The two big mental models

### 1. Deterministic lock-step simulation
Logic ticks are decoupled from render ticks. **Identical inputs → identical state on every client**; only player *orders* cross the network, and a sync-hash is exchanged each net frame to detect desync.

```
Game.Loop()  [OpenRA.Game/Game.cs ~L794]
 └ InnerLogicTick() [~L625]
     ├ OrderManager.TickImmediate()   # UI/chat/unsynced orders
     ├ OrderManager.TryTick()         # BLOCKS until all clients' orders for this NetFrame arrive
     │    └ ProcessOrders() -> UnitOrders.ProcessOrder() -> Actor.ResolveOrder() -> IResolveOrder traits
     │         then broadcast SyncHash  [OpenRA.Game/Network/OrderManager.cs]
     ├ world.OrderGenerator.Tick()    # turn local input into orders
     ├ world.Tick()                   # THE deterministic step  [OpenRA.Game/World.cs ~L413]
     │    ├ foreach actor: actor.Tick()        # runs its Activity state machine
     │    └ ApplyToActorsWithTrait<ITick>()    # every ITick trait
     └ world.TickRender()             # ITickRender, ignores pause (not synced)
 RenderTick() [~L701]                 # variable FPS, must be sync-safe (Sync.RunUnsynced guards it)
```
Key files: `Game.cs`, `World.cs`, `Network/OrderManager.cs`, `Network/Order.cs`, `Orders/UnitOrders.cs`, `Sync.cs`.
**Determinism rules when editing sim code:** no `float`/`double`, no `DateTime`/`Random`, no wall-clock or UI state in anything reached from `world.Tick()`. Use fixed-point world units: `WPos/WVec/WDist/WAngle/WRot` (1024 units = 1 cell; angles 0–1023) and cell coords `CPos/CVec/MPos`. Mark synced fields `[Sync]`; `Sync.cs` XORs them into the per-frame hash.

### 2. Actor + Trait (entity-component) system, all data-driven
- An **Actor** (`OpenRA.Game/Actor.cs`) is an ID + an `ActorInfo` template + a bag of trait instances. It caches hot interfaces (`IOccupySpace`, `IHealth`, `IFacing`, `IResolveOrder[]`, `IRender[]`…) and holds one `CurrentActivity`.
- A **trait** = pair of classes by convention: `FooInfo : TraitInfo<Foo>` (data, loaded from YAML) + `Foo` (runtime behavior). `Info.Create(init)` builds the runtime trait. Dependencies via `Requires<TInfo>` / ordering, topologically sorted in `ActorInfo.TraitsInConstructOrder()`.
- Traits are **indexed globally** in `TraitDictionary` (`OpenRA.Game/TraitDictionary.cs`), queried by interface: `actor.TraitsImplementing<T>()`, `actor.Trait<T>()`, `world.ActorsWithTrait<T>()`.
- **Trait interfaces** live in `OpenRA.Game/Traits/TraitsInterfaces.cs`: `ITick`, `ITickRender`, `IResolveOrder`/`IOrderVoice`, `INotifyCreated`/`INotifyAddedToWorld`/`INotifyKilled`/`INotifyBecomingIdle`/`INotifyIdle` (large `INotify*` family), `IOccupySpace`/`IPositionable`, `IHealth`, `IMove`, `IRender*`, `ISync`, condition interfaces, etc.

YAML → object: `MiniYaml.cs` parses the indent format; `FieldLoader.cs` reflects YAML keys onto `*Info` fields (`[FieldLoader.Require]`, `[Desc]`, type parsers for `WDist`, `Color`, `List<T>`, enums…). `FieldSaver.cs` is the reverse.

Mod load: `InstalledMods` finds `mod.yaml` → `Manifest.cs` (lists Assemblies/Rules/Sequences/Weapons/Chrome/TileSets) → `ModData.cs` builds `ObjectCreator` (reflection over the listed DLLs; resolves `"Mobile"`→`MobileInfo`) and the `Ruleset`. `ActorInfo` aggregates one `TraitInfo` per child node of an actor's YAML.

Example actor (`mods/ra/rules/vehicles.yaml`) — each child key is a trait:
```yaml
HARV:
  Inherits: ^Vehicle          # ^ = abstract template; Inherits composes templates
  Mobile:
    Speed: 113
  Health:
    HP: 60000
  Harvester:
    Resources: Ore,Gems
  Valued:
    Cost: 1100
```

### Activities (unit behavior)
Per-actor state machine; `Actor.CurrentActivity` is run each tick by `ActivityUtils.RunActivity()` (`OpenRA.Game/Traits/ActivityUtils.cs`). Base class `OpenRA.Game/Activities/Activity.cs` (states Queued→Active→Canceling→Done; `Queue()`/child activities). Concrete activities (Move, Attack, Harvest, Enter…) in `OpenRA.Mods.Common/Activities/` and `OpenRA.Game/Activities/`.

## Where things live (quick index)
- **Combat:** `Mods.Common/Traits/Armament.cs`, `Traits/Attack*`, `GameRules/WeaponInfo.cs`, `Mods.Common/Warheads/`, `Mods.Common/Projectiles/`.
- **Movement/path:** `Mods.Common/Traits/Mobile.cs`, `Traits/Air/Aircraft.cs`, `Mods.Common/Pathfinder/`, `World/ActorMap` & `Map/` for spatial indices.
- **Economy/production:** `Traits/Harvester.cs`, `Traits/Buildings/Production.cs`, `Refinery.cs`, `Buildable.cs`, `Valued.cs`.
- **Vision/shroud:** `Traits/RevealsShroud.cs`, `CreatesShroud.cs`, `World/Shroud`.
- **Rendering:** `OpenRA.Game/Graphics/` (`WorldRenderer`, `SpriteRenderer`, `Sheet`, `Animation`, `Palette`), render traits in `Mods.Common/Traits/Render/`. Backend in `OpenRA.Platforms.Default/`.
- **UI:** widget base `OpenRA.Game/Widgets/` (`Widget.cs`, `WidgetLoader.cs`, `Ui` manager); concrete widgets + `*Logic` behavior classes in `OpenRA.Mods.Common/Widgets/` (+ `/Logic`). Layout defined in mod `chrome/*.yaml` + `chrome.yaml`.
- **Lua scripting (missions):** host in `OpenRA.Game/Scripting/` (`ScriptContext`, Eluant); exposed API in `OpenRA.Mods.Common/Scripting/Global/` + `ScriptActorProperties`/`ScriptPlayerProperties`; maps load `.lua` via `LuaScript` trait. Map Lua lives under `mods/*/maps/*` and `mods/*/scripts/`.
- **Server/lobby:** core in `OpenRA.Game/Server/` (`Server.cs`, `Connection.cs`, `ServerTrait`s lifecycle), gameplay server traits in `OpenRA.Mods.Common/ServerTraits/`, entry point `OpenRA.Server/`.
- **Lint/validation:** `OpenRA.Mods.Common/Lint/` (run by `make check`) catches bad YAML/refs at build time.
- **YAML upgrade scripts:** `OpenRA.Mods.Common/UpdateRules/Rules/` — migrations applied when the YAML format changes between releases.
- **Utility commands:** `*/UtilityCommands/` — map import (`ImportTiberianDawnMapCommand`…), `ConvertPngToShp`, docs generation, etc. Run via `./utility.sh`.

## Conventions / gotchas
- Style enforced by **StyleCop + Roslynator** (`.editorconfig`, `Directory.Build.props`); Release builds skip analyzers, Debug enforces them. CI fails on warnings (`TREAT_WARNINGS_AS_ERRORS`). Tabs for indentation in C#; `LangVersion 12`, `Nullable disable`.
- After changing trait fields or YAML, run `make check` (yaml lint) — many invariants are caught there, not at runtime.
- `Info` classes hold *immutable config*; runtime traits hold mutable state. Don't put per-actor mutable state on `*Info`.
- Touching anything inside `world.Tick()`? Re-read the determinism rules above — desyncs are the #1 class of subtle bug here.

# THEATER Real-Mineral Economy + In-Game Codex

**Executable now:** True

**Summary:** Map 5 real strategic minerals onto OpenRA's existing data-driven ResourceType pipeline (ResourceLayer/ResourceRenderer/PlayerResources.ResourceValues), so the core economy is 100% YAML with zero new C#. The only new C# is two small, deterministic traits: a per-player MineralLedger that grants a tech prerequisite once a "tech mineral" (rare earths / uranium) has been refined, and a CodexLogic widget (a clone of EncyclopediaLogic) driven by a new world-actor CodexEntry data trait. Harvesting stays single-harvester-simple; the mineral *mix* matters because two advanced units are gated behind having refined a rare mineral, and that gate hybridizes cleanly with the existing territory control nodes.

## Key decisions
- Core economy is 100% YAML — minerals map directly onto the existing ResourceLayer/ResourceRenderer/PlayerResources.ResourceValues data traits; Ore-vs-Gems already proves multi-type coexistence with distinct value/density. Zero C# for income.
- 5 minerals: 3 are a pure cash gradient (Bauxite 22 / Lithium 45 / Cobalt 80) requiring no rules; 2 are 'tech minerals' (RareEarths, Uranium) whose only special rule is granting a tech-tree prerequisite when first refined — a VERB (unlock units), not a passive NUMBER, per the validated design rule.
- Harvesting stays single-harvester-simple: one HARV grabs every mineral type it drives over (no per-mineral micro). The only decision is WHERE you send it; the mix matters because advanced units are gated behind reaching a rare deposit.
- Mineral->tech gate is one new player-actor trait MineralLedger, a structural clone of ProvidesPrerequisite: it listens to Refinery's existing INotifyResourceAccepted hook, flips a one-way HashSet flag, and exposes ITechTreePrerequisite. Deterministic (integer/string only, no sim-state mutation) and capture-irrelevant.
- Codex is a sibling of the existing EncyclopediaLogic, not a fork: new CodexEntry world-actor data trait (clone of Encyclopedia.cs) + new CodexLogic widget that lists minerals, reads live value/density from loaded *Info (never drifts), uses design/art/codex_rare_earths.png as RareEarths hero art, and closes with Ui.CloseWindow() so it works in-game (stock encyclopedia is main-menu-only).
- Opened via a new MenuButton@CODEX_BUTTON added to the existing TOP_BUTTONS bar in ingame-player.yaml (modeled on OPTIONS_BUTTON) plus a Codex hotkey.
- Hybridizes with territory: control nodes (CashTrickler + SupplyNetwork) give passive hold-the-ground income; minerals give active income + unlocks into the same wallet. Placing rare-earth/uranium deposits downstream of supply-connected control nodes makes both systems reinforce each other. On a pure-territory map, set mineral cash values to 0 so minerals contribute only unlocks.

## Files to touch
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Traits/Player/MineralLedger.cs
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Traits/CodexEntry.cs
- /Users/ashishnaik/Projects/openra/OpenRA.Mods.Common/Widgets/Logic/CodexLogic.cs
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-minerals.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-units.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/chrome/codex.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/chrome/ingame-player.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/fluent/theater.ftl
- /Users/ashishnaik/Projects/openra/mods/ra/mod.yaml
- /Users/ashishnaik/Projects/openra/design/art/codex_rare_earths.png

## Risks
- Art is stubbed: the 5 minerals reuse stock gold*/gem* sequences palette-tinted, so Lithium/Cobalt/RareEarths/Uranium look identical on the map until distinct HD resource overlays are produced. Functional but visually ambiguous; needs the art pipeline before it reads as 'real minerals'.
- ResourceIndex is a single byte in the binary map format and must be unique per type (1-5 here); existing maps that already encode Ore=1/Gems=2 will silently remap to Bauxite/Lithium unless re-authored. Best confined to new THEATER maps or an opt-in ruleset, exactly like territory-economy.yaml.
- MineralLedger's tech unlock is permanent and match-scoped (you keep it even if you lose the deposit). That matches the validated 'match-layer civ is light/non-persistent' rule, but if a designer instead wants 'lose the deposit -> lose the unlock', that requires a different (refinery-presence) mechanism, not this one-way flag.
- CodexLogic must be built fresh rather than reusing EncyclopediaLogic wholesale, because the stock logic is actor-preview-driven and its BACK button calls Game.Disconnect() (main-menu only). Low effort (it's a slimmed clone) but it is genuinely new UI code that needs a GUI run to verify layout/art loading — not headlessly testable.
- Harvester carrying all 5 types means a player auto-collects tech minerals just by passing over them, which could trivialize the gate if deposits are placed carelessly. Map design must keep RareEarths/Uranium scarce and contested (low density, rough terrain, behind control nodes) for the unlock to feel earned.
- Adding the Codex MenuButton shifts/needs a free slot in TOP_BUTTONS and a new 'Codex' hotkey default; must avoid colliding with existing keybindings and provide a real icon (placeholder uses the options icon).

## Design

## THEATER Real-Mineral Economy + In-Game Codex — Design

### How the resource pipeline actually works (verified in code)

A "resource type" in OpenRA is **pure data spread across three traits on the World/Player actors**, plus a harvester list. There is no per-type C#:

| Concern | Trait / field | File |
|---|---|---|
| Where it spawns + density | `ResourceLayerInfo.ResourceTypes[name]` → `ResourceIndex`, `TerrainType`, `AllowedTerrainTypes`, `MaxDensity` | `OpenRA.Mods.Common/Traits/World/ResourceLayer.cs:32` |
| How it draws + minimap/tooltip name | `ResourceRendererInfo.ResourceTypes[name]` → `Image`, `Sequences`, `Palette`, `Name` | `Traits/World/ResourceRenderer.cs:26` |
| Cash per unit | `PlayerResourcesInfo.ResourceValues` (a `FrozenDictionary<string,int>`) | `Traits/Player/PlayerResources.cs:64` |
| Who can pick it up | `HarvesterInfo.Resources` (validated against `IStoresResources`) | `Traits/Harvester.cs:41` |

The **value→cash path** is `Refinery.AcceptResources` (`Traits/Buildings/Refinery.cs:55`): `value = count * ResourceValues[type]`, run through `IResourceValueModifier` percentage modifiers, then `GiveResources`/`ChangeCash`. **All integer math, no float/Random/DateTime — already determinism-safe.** Ore vs Gems already proves multiple types coexist with different `MaxDensity` (12 vs 3) and value (`world.yaml:202`, RA ore=25/gems=50 in rules), and `RecalculateResourceDensity: true` + `ResourceClaimLayer` already handle multi-type harvesting. **So adding minerals is 100% YAML.**

`Refinery` also fires `INotifyResourceAccepted.OnResourceAccepted(actor, refinery, resourceType, count, value)` for every owner-matched listener (`Refinery.cs:79`) — this is the clean, deterministic hook for the mineral→tech gate, and it runs inside the refinery's `AcceptResources` (sim path, integer-only).

---

### 1. The mineral set (5 minerals, distinct economic roles)

Replace generic `Ore`/`Gems` with five named minerals. Keep exactly **two harvested by the standard harvester** at first to stay fast; the rest are positioned as scarce, decision-driving deposits.

| Mineral | Role | Value/unit | MaxDensity | Where (AllowedTerrainTypes) | Strategic verb |
|---|---|---|---|---|---|
| **Bauxite** | Common bulk income (the new "ore") | 22 | 12 | Clear, Road | Baseline economy; spread everywhere |
| **Lithium** | Mid-value, medium density | 45 | 6 | Clear, Rough | Better $/trip; contested mid-map patches |
| **Cobalt** | High-value, low density | 80 | 3 | Rough | Reward for pushing to rough terrain |
| **RareEarths** | **Tech mineral** — refining ANY unlocks advanced air/EW tech | 60 | 4 | Clear, Rough | *Unlocks* units (a VERB, not a stat) |
| **Uranium** | **Tech mineral** — refining unlocks the superheavy/strategic tier | 110 | 2 | Rough, Ore-equivalent | *Unlocks* the top tier; rare, fought-over |

Design intent (matches the validated "verbs not numbers" rule): Bauxite/Lithium/Cobalt are a **value gradient** that only changes *how much* cash per trip — zero new rules, pure `ResourceValues`. RareEarths and Uranium add a **new thing you can DO**: refining a single load flips a permanent per-player flag that unlocks buildable units. A player who never controls a rare-earth deposit simply can't build Specter stealth fighters / Bayrak UCAVs; uranium gates the strategic tier (e.g. the THCOM paradrop escalation or a superheavy). That is what makes the *mix* matter without micro.

To keep harvesting simple, the **standard harvester carries `Resources: Bauxite, Lithium, Cobalt, RareEarths, Uranium`** (it grabs whatever it drives over — one harvester, no per-mineral micro). The strategy is purely **where you send it**, identical UX to today.

**YAML — `mods/ra/rules/theater-minerals.yaml` (NEW, data):**
```yaml
World:
  ResourceRenderer:
    ResourceTypes:
      Bauxite:    { Sequences: gold01,gold02,gold03,gold04, Palette: player, Name: resource-bauxite }
      Lithium:    { Sequences: gem01,gem02,gem03,gem04,    Palette: player, Name: resource-lithium }
      Cobalt:     { Sequences: gem01,gem02,gem03,gem04,    Palette: player, Name: resource-cobalt }
      RareEarths: { Sequences: gem01,gem02,gem03,gem04,    Palette: player, Name: resource-rareearths }
      Uranium:    { Sequences: gold01,gold02,gold03,gold04, Palette: player, Name: resource-uranium }
  ResourceLayer:
    RecalculateResourceDensity: true
    ResourceTypes:
      Bauxite:    { ResourceIndex: 1, TerrainType: Ore,  AllowedTerrainTypes: Clear,Road, MaxDensity: 12 }
      Lithium:    { ResourceIndex: 2, TerrainType: Gems, AllowedTerrainTypes: Clear,Rough, MaxDensity: 6 }
      Cobalt:     { ResourceIndex: 3, TerrainType: Gems, AllowedTerrainTypes: Rough, MaxDensity: 3 }
      RareEarths: { ResourceIndex: 4, TerrainType: Gems, AllowedTerrainTypes: Clear,Rough, MaxDensity: 4 }
      Uranium:    { ResourceIndex: 5, TerrainType: Ore,  AllowedTerrainTypes: Rough, MaxDensity: 2 }

^BasePlayer:                 # or Player: depending on the base template used in RA
  PlayerResources:
    ResourceValues:
      Bauxite: 22
      Lithium: 45
      Cobalt: 80
      RareEarths: 60
      Uranium: 110
  MineralLedger:             # NEW trait (see §2)
    TechMinerals:
      RareEarths: theater.tech-rareearths
      Uranium: theater.tech-uranium

HARV:
  Harvester:
    Resources: Bauxite, Lithium, Cobalt, RareEarths, Uranium
```
> NOTE on art: the five reuse stock `gold*`/`gem*` sequences for now (palette-tinted) so this passes `--check-yaml` with zero art. Distinct HD overlays are a later art-pipeline task; nothing in the design blocks on them. `ResourceIndex` must be unique (1–5) because maps encode it as a byte; the `Name:` keys are Fluent references for tooltips + the Codex.

---

### 2. Keeping it simple while the mix matters — the tech gate (NEW C#)

The only economic rule beyond "different cash per unit" is: **refining a tech mineral permanently grants a tech-tree prerequisite**, which unlocks units via stock `Buildable.Prerequisites` (the exact pattern `theater.yaml` already uses for the Mammoth/Tech-Lab unlock).

New trait `MineralLedger` (player actor) — listens to the refinery hook, flips a one-way flag per tech mineral, and exposes it as an `ITechTreePrerequisite`. This is a direct structural clone of `ProvidesPrerequisite` (`Traits/Player/ProvidesPrerequisite.cs`) which already implements `ITechTreePrerequisite` + calls `techTree.ActorChanged`.

**`OpenRA.Mods.Common/Traits/Player/MineralLedger.cs` (NEW C#):**
```csharp
[TraitLocation(SystemActors.Player)]
public class MineralLedgerInfo : TraitInfo, ITechTreePrerequisiteInfo
{
    [FieldLoader.Require]
    [Desc("Map of [resource type] -> [prerequisite granted once any amount is refined].")]
    public readonly Dictionary<string, string> TechMinerals = new();

    IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info) => TechMinerals.Values;
    public override object Create(ActorInitializer init) { return new MineralLedger(this); }
}

public class MineralLedger : INotifyResourceAccepted, ITechTreePrerequisite, INotifyCreated
{
    readonly MineralLedgerInfo info;
    readonly HashSet<string> unlocked = new();   // resource types already refined
    TechTree techTree;

    public MineralLedger(MineralLedgerInfo info) { this.info = info; }

    void INotifyCreated.Created(Actor self) { techTree = self.Owner.PlayerActor.Trait<TechTree>(); }

    // One-way flag flip. Integer/string only, runs in refinery's AcceptResources (sim path) — deterministic.
    void INotifyResourceAccepted.OnResourceAccepted(Actor self, Actor refinery, string resourceType, int count, int value)
    {
        if (info.TechMinerals.ContainsKey(resourceType) && unlocked.Add(resourceType))
            techTree.ActorChanged(self.Owner.PlayerActor);   // re-evaluate build palette
    }

    public IEnumerable<string> ProvidesPrerequisites
        => unlocked.Select(r => info.TechMinerals[r]);
}
```
> `INotifyResourceAccepted` listeners are gathered by `Refinery` via `world.ActorsWithTrait` and filtered to same-owner, so putting this on the **player actor** is correct and capture-irrelevant (the player keeps the unlock; this is a match-layer "you researched it" flag, matching the design rule that *match-layer civ is light/non-persistent*). Determinism: `HashSet.Add` returns a bool deterministically; iteration order of `ProvidesPrerequisites` only feeds the tech tree set-membership check, not the sim hash.

**Unit gating (data, in `theater-units.yaml`):**
```yaml
SPECTER:                       # Federation stealth fighter (already exists as reskin)
  Buildable:
    Prerequisites: theater.tech-rareearths
BAYRAK:
  Buildable:
    Prerequisites: theater.tech-rareearths
# Uranium gates the strategic tier, e.g.:
4TNK:                          # superheavy reuse
  Buildable:
    Prerequisites: theater.tech-uranium
```
Net effect: harvesting is one unchanged right-click; the **decision** is whether to spend a harvester cycle reaching the rare-earth/uranium patch to *open new options*, versus farming safe bauxite for raw cash. That is 4X-lite depth with zero added micro.

---

### 3. The Codex UI (NEW C# widget + chrome + data trait)

There's an existing **`EncyclopediaLogic`** (`Widgets/Logic/EncyclopediaLogic.cs`) + D2k/CnC `encyclopedia.yaml` chrome — but it's actor-driven (reads `EncyclopediaInfo` off each actor preview) and **main-menu only** (its BACK button calls `Game.Disconnect()`). The Codex needs to (a) list *minerals*, not actors, and (b) open **in-game**. Cleanest path: a sibling, not a fork of the actor logic.

**New data trait `CodexEntry` on the World actor (NEW C#, trivial — mirrors `EncyclopediaInfo`):**
```csharp
[TraitLocation(SystemActors.World)]
public class CodexEntryInfo : TraitInfo
{
    [FieldLoader.Require] public readonly string Resource = null;      // resource-type key
    [FluentReference] public readonly string Title = null;
    [FluentReference] public readonly string Description = null;       // lore + real-world note
    public readonly string Portrait = null;                            // png under codex/
    public readonly int Order = 0;
    public override object Create(ActorInitializer init) { return CodexEntry.Instance; }
}
public readonly struct CodexEntry { public static readonly object Instance = default(CodexEntry); }
```
Defined once on the world actor (data, in `theater-minerals.yaml`):
```yaml
World:
  CodexEntry@bauxite:    { Resource: Bauxite,    Title: codex-bauxite-title,    Description: codex-bauxite-desc,    Portrait: bauxite,    Order: 1 }
  CodexEntry@lithium:    { Resource: Lithium,    Title: codex-lithium-title,    Description: codex-lithium-desc,    Portrait: lithium,    Order: 2 }
  CodexEntry@cobalt:     { Resource: Cobalt,     Title: codex-cobalt-title,     Description: codex-cobalt-desc,     Portrait: cobalt,     Order: 3 }
  CodexEntry@rareearths: { Resource: RareEarths, Title: codex-rareearths-title, Description: codex-rareearths-desc, Portrait: rareearths, Order: 4 }
  CodexEntry@uranium:    { Resource: Uranium,    Title: codex-uranium-title,    Description: codex-uranium-desc,    Portrait: uranium,    Order: 5 }
```

**New widget logic `CodexLogic.cs` (NEW C#)** — a slimmed clone of `EncyclopediaLogic`'s list/description/portrait plumbing (lines 64–133, 162–259 are the parts to reuse), but:
- iterates `world.WorldActor.Info.TraitInfos<CodexEntryInfo>()` ordered by `Order` instead of actor previews;
- for each entry, reads live **value** from `PlayerResources.Info.ResourceValues[Resource]` and **MaxDensity** from `ResourceLayer.Info.ResourceTypes[Resource]` and renders a small stat block (value/unit, density, terrain) — these come straight from the loaded `*Info`, so the Codex never drifts from the actual economy;
- loads art via the same direct-`Png` path `EncyclopediaLogic` uses (`SelectActor`, lines 184–204) from `codex/<Portrait>.png`; **`design/art/codex_rare_earths.png` is the hero art** for the RareEarths entry (strip-mine + refinery + teal rare-earth crates — exactly on-theme);
- **BACK button just calls `Ui.CloseWindow()`** (no `Game.Disconnect`), so it works mid-match.

**Chrome `mods/ra/chrome/codex.yaml` (NEW, data)** — copy `d2k/chrome/encyclopedia.yaml` structure (`Background@CODEX_PANEL` with `Logic: CodexLogic`, a `ScrollPanel@MINERAL_LIST`, an info pane with `SpriteWidget@CODEX_PORTRAIT` + `Label@CODEX_TITLE` + a stats container + `ScrollPanel@CODEX_DESCRIPTION_PANEL`, and a `Button@BACK_BUTTON Key: escape`). Register it in `mod.yaml` `ChromeLayout:` for ra (or, for the slice, the map's own chrome include).

**Opening it (data, in `ingame-player.yaml`):** add a `MenuButton@CODEX_BUTTON` to the existing `Container@TOP_BUTTONS` (the bar that already holds BEACON/SELL/POWER/REPAIR/OPTIONS at `ingame-player.yaml:318`). Model it on `MenuButton@OPTIONS_BUTTON` (`:382`) — give it `Key: <a free hotkey, e.g. derived from a new "Codex" keybinding>` and an `OnClick` that opens the panel. The open itself follows the `MenuButtonsChromeLogic.OpenMenuPanel` pattern (`Widgets/Logic/Ingame/MenuButtonsChromeLogic.cs:84`) or, more simply, a one-liner logic class doing `Ui.OpenWindow("CODEX_PANEL", new WidgetArgs())`. Place it at `X: 128` (the slot after REPAIR, before OPTIONS at X:192).

```yaml
# in Container@TOP_BUTTONS children:
MenuButton@CODEX_BUTTON:
  Logic: AddFactionSuffixLogic
  X: 128
  Width: 28
  Height: 28
  Background: sidebar-button
  Key: Codex
  TooltipText: button-top-buttons-codex-tooltip
  TooltipContainer: TOOLTIP_CONTAINER
  Children:
    Image@ICON: { X: 6, Y: 6, ImageCollection: order-icons, ImageName: options }   # placeholder icon
```

---

### 4. Determinism + hybridization with territory control nodes

**Determinism:** Nothing here adds float/Random/DateTime to the sim. The economy core is the *existing* integer `Refinery`/`PlayerResources` path. `MineralLedger` is a one-way `HashSet<string>` flip driven by the already-deterministic `INotifyResourceAccepted` callback inside `Refinery.AcceptResources`; it only ever calls `techTree.ActorChanged` (build-palette re-eval, not sim state) and exposes a set of prereq strings. `CodexLogic`/`CodexEntry` are **pure UI/data** — the widget runs on the render side and reads immutable `*Info`, never touching `world.Tick()` or the sync hash.

**Hybridization with the territory economy (the key integration):** The two systems are *orthogonal income, shared scarcity*:
- **Territory control nodes** (`theater.yaml`: `CTRLCASH`/Oil-Derrick `CashTrickler`, gated by `DevelopsWhileHeld` + `SupplyNetwork`/`SupplyNode` supply-connectivity) provide **steady passive cash** for holding ground.
- **Minerals** provide **active, riskier income + the tech unlocks**. They share the same `PlayerResources` wallet and the same `CashTrickler`-vs-`GiveResources` plumbing, so no new accounting.
- **Place rare-earth/uranium deposits adjacent to or downstream of control nodes** so the supply-network mechanic does double duty: you must hold the territory chain *and* run a harvester there to convert "I own this ground" into "I can build Specters." A map can put a uranium patch behind a `FCOM` forward-command node — capturing the node lets you *project* production forward, and only then is the uranium reachable/defensible. This makes the validated "territory grants verbs" thesis and the mineral tech-gate reinforce each other instead of competing.
- For a **pure-territory map** (Phase 0 `territory-economy.yaml` already zeroes `Ore`/`Gems`), set all `ResourceValues` to 0 except the tech minerals — then minerals contribute *only* unlocks, not cash, and territory remains the sole income. The two yamls compose cleanly because both only edit `PlayerResources`/`World`.

---

### 5. New C# vs Data summary

**New C# (3 files, all small, all determinism-safe / UI-side):**
- `OpenRA.Mods.Common/Traits/Player/MineralLedger.cs` — refined-mineral → tech-prereq (clone of `ProvidesPrerequisite`'s `ITechTreePrerequisite` pattern).
- `OpenRA.Mods.Common/Traits/CodexEntry.cs` — trivial data trait (clone of `Encyclopedia.cs`).
- `OpenRA.Mods.Common/Widgets/Logic/CodexLogic.cs` — Codex panel (slimmed clone of `EncyclopediaLogic.cs`, in-game-safe BACK).

**Data only (YAML/Fluent/art):**
- `mods/ra/rules/theater-minerals.yaml` (NEW) — resource defs, values, harvester list, `MineralLedger` + `CodexEntry` config.
- `mods/ra/rules/theater-units.yaml` (EDIT) — add `Buildable.Prerequisites: theater.tech-*` to the gated units.
- `mods/ra/chrome/codex.yaml` (NEW) — panel layout (copy d2k encyclopedia chrome).
- `mods/ra/chrome/ingame-player.yaml` (EDIT) — add `MenuButton@CODEX_BUTTON`.
- `mods/ra/fluent/theater.ftl` (EDIT) — `resource-*` tooltip names + `codex-*-title`/`codex-*-desc` lore strings.
- `mods/ra/mod.yaml` (EDIT, or map-scoped) — list `codex.yaml` in `ChromeLayout:` and `theater-minerals.yaml` in `Rules:`; add a `Codex` hotkey default.
- `mods/ra/uibits/codex/*.png` (art) — `codex_rare_earths.png` → `codex/rareearths.png`; others can stub off it initially.

**Validation:** after wiring, `./utility.sh ra --check-yaml` (temporarily including `theater-minerals.yaml`), and a Debug build grep for the 3 new filenames to confirm analyzer-clean (per CLAUDE.md, `make check` is red for unrelated stock-file reasons on this machine).
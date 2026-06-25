# THEATER unified common roster — standalone-mod build strategy

**Executable now:** True

**Summary:** All 8 THEATER factions should build ONE shared modern combined-arms baseline plus their single faction-unique, by tagging the common roster with a new neutral `theater.common` prerequisite that every Construction Yard grants regardless of side — replacing the stock `~structures.allies/soviet` and `~vehicles/infantry/aircraft/ships.*` side tokens on the chosen common actors. This is executable today with existing RA art (no new sprites needed), is determinism-safe (pure YAML/prerequisite plumbing, nothing in world.Tick()), and fixes a latent bug: the new factions are currently in NO side `Factions:` list, so most side-gated stock units are silently unbuildable for them outside the dev sandbox.

## Key decisions
- Gate the common roster on a NEW neutral `theater.*` prerequisite family (theater.common/structures/infantry/vehicles/aircraft/navy) granted by each faction's conyard + production houses via Factions:-keyed ProvidesPrerequisite, rather than nulling prereqs or merging allied/soviet actors.
- Pick an Allied-styled set as the visual baseline (E1/E3/E6/MEDI/MECH, JEEP, 2TNK as the shared MBT, ARTY, MH60/TRAN, PT/LST) because it reads as generic modern military; flag only APC and FTRK (Soviet-styled) for a later reskin.
- Keep the existing tech.<faction> unique gating in theater-units.yaml completely unchanged — commons and uniques are orthogonal layers.
- Put all changes in a new standalone mods/ra/rules/theater-common.yaml, loaded after the stock roster files and after theater-factions.yaml, before theater-units.yaml, so stock RA/AI factions and campaigns are untouched.
- Use 2TNK (not 1TNK/3TNK) as the single shared MBT so the heavier 3TNK body stays reserved for the rhine_compact unique (LOEWE), avoiding actor merges.
- This change simultaneously fixes a latent bug: the 8 new factions are in no side Factions: list, so side-gated stock units are currently unbuildable for them outside the masked dev sandbox.

## Files to touch
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-common.yaml (NEW — token grants on FACT/WEAP/BARR/TENT/HPAD/AFLD/SYRD/SPEN + Buildable re-gates for the common actors)
- /Users/ashishnaik/Projects/openra/mods/ra/mod.yaml (add `ra|rules/theater-common.yaml` to Rules, between theater-factions.yaml and theater-units.yaml)
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-factions.yaml (optionally add theater.* tokens to the DEVUNLOCK Prerequisites list so the sandbox still surfaces the commons)
- /Users/ashishnaik/Projects/openra/mods/ra/fluent/theater.ftl (descriptions if any common actors get THEATER-specific tooltip/desc strings — optional for v1)

## Risks
- The DEVUNLOCK checkbox in theater-factions.yaml (and the test map) grants building-name + techlevel prereqs but NOT the new theater.* tokens — once commons are re-gated, confirm the sandbox still lists them. Likely need to add theater.common/infantry/vehicles/aircraft/navy to the DEVUNLOCK Prerequisites list (or rely on the conyard/houses granting them naturally, which is cleaner).
- Rule-load order is load-bearing: theater-common.yaml must come AFTER infantry/vehicles/aircraft/ships/structures.yaml (so Buildable overrides win) and after theater-factions.yaml (so FACT tech tokens exist). Wrong order = either stock prereqs survive or dangling-ref lint errors.
- Faction-unique actors that Inherit side-styled bodies (SPECTER/BASTION/LOEWE/GARUDA) must FULLY override Buildable.Prerequisites; verify none still inherit a residual ~vehicles.soviet/~aircraft.* token after the change (they currently replace Buildable, so should be fine).
- APC and FTRK ship Soviet-styled until a reskin pass; cosmetic inconsistency in an otherwise Allied-looking common army until the GPT-image/Blender pipeline retextures them.
- Naval is intentionally minimal (PT/LST) in v1; adding DD/CA/SS/MSUB later pulls in ~syrd dome / ~spen side-house chains that also need theater.* re-gating — defer deliberately, don't half-wire them.
- Stock RA `make check` is already RED on this machine for unrelated IDE0055 reasons (per CLAUDE.md); validate THESE changes specifically via ./utility.sh ra --check-yaml, not the global make check.

## Design

## Context I verified (not guessed)

- **Side gating is by `Factions:` internal-name, NOT by `Side`.** `OpenRA.Mods.Common/Traits/Player/ProvidesPrerequisite.cs:88-89` matches `Info.Factions.Contains(faction.InternalName)`. The faction's `Side` field plays no role in prerequisite resolution.
- **Consequence (latent bug):** `FACT` grants `structures.allies` only for `Factions: allies, england, france, germany` and `structures.soviet` only for `soviet, russia, ukraine` (`structures.yaml:1165-1191`). The 8 THEATER factions (`federation`, `northern_union`, …) are in **none** of those lists, and likewise absent from WEAP/AFLD/HPAD/SYRD/SPEN/BARR/TENT `ProvidesPrerequisite@*` lists. So a `federation` conyard grants **no** `structures.*`/`vehicles.*`/`infantry.*`/`aircraft.*`/`ships.*` token at all. Every side-gated stock unit (`1TNK ~vehicles.allies`, `3TNK ~vehicles.soviet`, `JEEP`, `APC`, `FTRK`, `MIG ✗`, all defenses, etc.) is therefore unbuildable for the new factions in real play. The dev map only *masks* this because DEVUNLOCK grants building-name + techlevel prereqs but still **not** the faction side tokens — so `~vehicles.allies`-gated actors stay locked even in the sandbox. The comment at `theater-factions.yaml:83` ("side gating … keeps the opposite side locked") is describing behavior that does not actually hold for these factions.
- **The fix the task wants and the bug fix are the same change**: give every conyard one neutral roster token and gate the common set on it.

So the unified-common design is not just a feature — it is the correct way to make these factions playable at all.

---

## 1. The shared common roster (existing RA bodies — zero new art to start)

All actor ids below already exist in `mods/ra/rules/*.yaml`. Picked for a coherent modern combined-arms baseline; "theme later" = art/audio is side-flavored today and gets a reskin pass after the roster is proven (see §4).

**Infantry** (from `infantry.yaml`)
- `E1` — Rifleman (baseline AT-capable rifle). Allied-styled GI art ✔ neutral enough.
- `E3` — AT / rocket infantry (RedEye + Dragon: AA + anti-armor). Allied-styled ✔.
- `E6` — Combat Engineer (capture/repair). Allied-styled ✔.
- `MEDI` — Medic (field heal). Allied-styled ✔.
- `MECH` — Mechanic (vehicle/husk repair + husk capture). Allied-styled ✔.
- *(E2 grenadier optional later; keep the core six lean.)*

**Vehicles** (from `vehicles.yaml`)
- `JEEP` — Scout / recon (fast, 1-passenger, big sight). Allied-styled ✔.
- `APC` — IFV / troop carrier (5 cap). **Soviet-styled** — retheme-later flag.
- `2TNK` — Main Battle Tank (Allied medium, balanced). Allied-styled ✔. *(Chosen as the shared MBT so the heavier `3TNK` body stays free for a faction-unique, which it already is — `LOEWE`.)*
- `ARTY` — Artillery (siege). Allied-styled ✔.
- `FTRK` — Mobile AA / flak track. **Soviet-styled** — retheme-later flag.
- `HARV` — Harvester (economy; already side-neutral, gated on `proc`). ✔
- `MCV` — Mobile Construction Vehicle (already side-neutral, gated on `fix`). ✔
- `MNLY` — Minelayer (already side-neutral). Optional.

**Air** (from `aircraft.yaml`)
- `MH60` — Gunship helicopter (the modern Black Hawk body, chaingun, `~hpad`). Already side-neutral-ish (no faction token, just `~hpad`). ✔ — ideal modern look.
- `YAK` — Light fighter/attack plane (`~afld`, currently no faction token). **Soviet-styled** — retheme-later, but note `YAK` is reused by two faction-uniques (`WINGLOONG`, `BAYRAK`), so for the *common* slot prefer the de-facto-neutral `MH60` + keep one fixed-wing. Use `MIG` body for the common fighter only after rethemize; until then ship `MH60` + `TRAN` and defer the common fixed-wing.
- `TRAN` — Transport helicopter (`~hpad`, side-neutral). ✔

**Navy** (from `ships.yaml`) — keep minimal for v1
- `LST` — Landing craft / transport (already fully side-neutral, `~techlevel.low`). ✔
- `PT` — Patrol boat (gated `~syrd` only, no faction token). ✔
- *(Defer `DD`/`CA`/`SS`/`MSUB` to a later naval pass; they pull in `~syrd dome`/`~spen` side-house chains.)*

This is ~6 infantry + ~7 vehicles + 2–3 air + 2 navy = a complete combined-arms baseline, all reusing shipped sprites.

---

## 2. The mechanism — ONE neutral `theater.common` token (recommended)

**Recommendation: a single shared prerequisite, granted by the Construction Yard for all THEATER factions, plus shared per-production-house tokens.** Do NOT try to unify the two sides' equivalent actors into one (that fights inheritance and husks/voices), and do NOT drop prereqs to nothing (that would let the AI/stock factions build them too and break lint references).

This lives in a NEW standalone file `mods/ra/rules/theater-common.yaml` (added to `mod.yaml` Rules after the stock roster files, before `theater-units.yaml`), so the stock RA rulesets stay untouched and reviewable.

### 2a. Grant the tokens from the conyard and production houses

```yaml
# theater-common.yaml — every THEATER faction's conyard/houses grant neutral roster tokens.
# Keyed on the 8 contemporary factions so stock RA/AI factions are unaffected.

FACT:
    ProvidesPrerequisite@theatercommon:
        Prerequisite: theater.common
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance
    # Make the common set side-agnostic: also grant BOTH structure tokens so any
    # stock ~structures.* gating in defenses/houses we keep also resolves.
    ProvidesPrerequisite@theaterstructures:
        Prerequisite: theater.structures
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance

WEAP:
    ProvidesPrerequisite@theatervehicles:
        Prerequisite: theater.vehicles
        RequiresPrerequisites: theater.structures
        Prerequisite: theater.vehicles
    ProvidesPrerequisite@theatervehiclesfac:
        Prerequisite: theater.vehicles
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance

BARR:
    ProvidesPrerequisite@theaterinfantry:
        Prerequisite: theater.infantry
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance
TENT:
    ProvidesPrerequisite@theaterinfantry:
        Prerequisite: theater.infantry
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance

HPAD:
    ProvidesPrerequisite@theateraircraft:
        Prerequisite: theater.aircraft
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance
AFLD:
    ProvidesPrerequisite@theateraircraft:
        Prerequisite: theater.aircraft
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance

SYRD:
    ProvidesPrerequisite@theaternavy:
        Prerequisite: theater.navy
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance
SPEN:
    ProvidesPrerequisite@theaternavy:
        Prerequisite: theater.navy
        Factions: federation, northern_union, continental_bloc, rhine_compact, isles_coalition, eastern_maritime, subcontinent_federation, anatolian_alliance
```

(The `@theatervehicles` RequiresPrerequisites variant above is illustrative; ship just the `Factions:`-keyed grant — simplest and sufficient. One `ProvidesPrerequisite@` block per house per token.)

### 2b. Re-gate the common actors onto the neutral tokens

For each chosen common actor, **replace** the side token in its `Buildable: Prerequisites` with the neutral one. Because the 8 factions are the only ones that get `theater.*`, stock factions are unaffected and lint stays green (the tokens are now *provided*, so they're not dangling). Example deltas (each goes in `theater-common.yaml`, overriding the stock `Buildable`):

```yaml
# ---- Infantry: swap side-house barracks tokens for theater.infantry ----
E1:
    Buildable:
        Queue: Infantry
        BuildAtProductionType: Soldier
        Prerequisites: theater.infantry, ~techlevel.infonly
E3:
    Buildable:
        Queue: Infantry
        BuildAtProductionType: Soldier
        Prerequisites: theater.infantry, ~techlevel.infonly
E6:
    Buildable:
        Queue: Infantry
        BuildAtProductionType: Soldier
        Prerequisites: theater.infantry, ~techlevel.infonly
MEDI:
    Buildable:
        Queue: Infantry
        BuildAtProductionType: Soldier
        Prerequisites: theater.infantry, ~techlevel.infonly
MECH:
    Buildable:
        Queue: Infantry
        BuildAtProductionType: Soldier
        Prerequisites: theater.infantry, fix, ~techlevel.medium

# ---- Vehicles: swap ~vehicles.allies/soviet for theater.vehicles ----
JEEP:
    Buildable:
        Queue: Vehicle
        Prerequisites: theater.vehicles, ~techlevel.low
APC:
    Buildable:
        Queue: Vehicle
        Prerequisites: theater.vehicles, ~techlevel.low
2TNK:
    Buildable:
        Queue: Vehicle
        Prerequisites: theater.vehicles, fix, ~techlevel.medium
ARTY:
    Buildable:
        Queue: Vehicle
        Prerequisites: theater.vehicles, dome, ~techlevel.medium
FTRK:
    Buildable:
        Queue: Vehicle
        Prerequisites: theater.vehicles, ~techlevel.low
# HARV / MCV already side-neutral (gated on proc / fix) — leave as-is.

# ---- Air ----
MH60:
    Buildable:
        Queue: Aircraft
        BuildAtProductionType: Helicopter
        Prerequisites: ~hpad, ~techlevel.medium      # already neutral; optionally add theater.aircraft
TRAN:
    Buildable:
        Queue: Aircraft
        BuildAtProductionType: Helicopter
        Prerequisites: ~hpad, ~techlevel.medium

# ---- Navy (already neutral; tighten onto theater.navy for clarity) ----
PT:
    Buildable:
        Queue: Ship
        BuildAtProductionType: Boat
        Prerequisites: theater.navy, ~techlevel.low
LST:
    Buildable:
        Queue: Ship
        Prerequisites: ~techlevel.low                # leave fully open (transport)
```

**Why this mechanism over the alternatives**
- *vs. "drop prereqs to nothing"*: would also unlock these for stock RA factions and the AI's house logic, and several `~structures.*`/`~vehicles.*` tokens are referenced by lint and by support powers; nulling them risks dangling refs. The neutral-token approach keeps every reference live.
- *vs. "merge allied+soviet equivalents into one actor"*: e.g. folding `1TNK`+`3TNK` into one MBT — fights `Inherits`, husk actors (`2TNK.Husk`), and per-actor voices; high churn for no gameplay gain. Re-gating is a 1-line `Buildable` override per actor.
- The token is **granted by the building you already need** (conyard for the roster gate; the production house itself for its queue token), so the natural "build a War Factory to get vehicles" tech flow is preserved — `theater.vehicles` comes from WEAP, `theater.infantry` from BARR/TENT, etc. Nothing is free.

---

## 3. How faction uniques layer on (unchanged)

The existing `tech.<faction>` gating in `theater-units.yaml` is already correct and orthogonal to the common roster — keep it exactly as-is:

- `FACT.ProvidesPrerequisite@<faction>` grants `tech.<faction>` only for that faction (`theater-factions.yaml:96-120`).
- Each unique (`SPECTER`, `BASTION`, `LOEWE`, …) gates on `tech.<faction>` + its production house (`afld`/`weap`/`tent`). A faction thus builds: **full `theater.common` baseline + its one `tech.<faction>` unique.**
- One cleanup: the uniques inherit side-styled bodies (`SPECTER:MIG`, `BASTION:TTNK`, `LOEWE:3TNK`, `GARUDA:V2RL`). Those inherit stock `Buildable.Prerequisites` with side tokens via `Inherits` — but each overrides `Buildable.Prerequisites` to `tech.<faction>, <house>`, so they do NOT need a side token. Verify after the change that none of them still resolve a `~vehicles.soviet`-style token (they shouldn't — they fully replace `Buildable`).

No change needed to §3 beyond keeping it; it already follows the "uniques layered on commons" model.

---

## 4. Allied-vs-Soviet visual/audio mismatch — pick a consistent set now, reskin later

The common set deliberately leans on **Allied-styled bodies** because they read as "generic modern NATO-ish military," which suits a contemporary reimagining better than Soviet retro hardware. Flagged mismatches to retheme in a later art pass (proven GPT-image + Blender turntable pipeline in `design/art/`):

| Common slot | Body used | Style today | Retheme priority |
|---|---|---|---|
| Rifleman/AT/Eng/Medic/Mech | E1/E3/E6/MEDI/MECH | Allied ✔ | low (already neutral GI look) |
| Scout | JEEP | Allied ✔ | low |
| MBT | 2TNK | Allied ✔ | low |
| Artillery | ARTY | Allied ✔ | low |
| Gunship/Transport | MH60/TRAN | Allied/neutral ✔ | low |
| **APC** | APC | **Soviet** | **medium** — reads as a Soviet half-track |
| **Mobile AA** | FTRK | **Soviet** | **medium** — flak track silhouette |
| Patrol boat | PT | neutral | low |

Interim rule: ship the Allied-styled set as the visual baseline; the two Soviet-styled bodies (`APC`, `FTRK`) are acceptable stand-ins (gameplay-correct) and get first priority in the reskin queue. Do **not** mix a Soviet rifleman with an Allied tank — the table above is internally consistent (one army's look) except those two flagged vehicles. Audio: these all use generic `Vehicle`/`Infantry` voice sets already; no Soviet-accent voice lines are in the common set (those live on `SHOK`/`TTNK`, which are faction-uniques, not commons).

---

## 5. Determinism, lint, and the standalone-mod path

**Determinism:** Zero risk. This is entirely `Buildable.Prerequisites` + `ProvidesPrerequisite` YAML — prerequisite resolution runs in `TechTree` (UI/tech layer), never inside `world.Tick()`. No floats, no Random, no DateTime, no new synced state. Nothing here touches the simulation step.

**Lint cleanliness:**
- Every `theater.*` token is both *provided* (by a conyard/house) and *required* (by a common actor), so `make check` / `--check-yaml` sees no dangling prerequisite.
- Keep changes in the new `theater-common.yaml`; load it in `mod.yaml` Rules **after** `infantry/vehicles/aircraft/ships/structures.yaml` (so the `Buildable` overrides win) and after `theater-factions.yaml` (so the FACT `ProvidesPrerequisite@<faction>` tech tokens are defined) — i.e. between `theater-factions.yaml` and `theater-units.yaml`.
- Validate exactly as CLAUDE.md prescribes: temporarily add `ra|rules/theater-common.yaml` to `mod.yaml` Rules (it's already going there), run `./utility.sh ra --check-yaml`. The stock-file analyzer noise (`IDE0055`) is unrelated.

**Standalone-mod coexistence (mod-creation brief):** When THEATER graduates from an `ra`-overlay to its own `mods/theater/mod.yaml`:
- This file (`theater-common.yaml`) and the other `theater-*.yaml` become first-class rules in the new mod's `Rules:` list; the stock `ra` roster files are *copied in* (or referenced) and the THEATER overrides applied on top — same content, just no longer surgically overlaying `ra`.
- The `theater.*` tokens become the **primary** gating, so at that point you can go further and *delete* the now-unused `~structures.allies/soviet` `ProvidesPrerequisite@*` blocks from FACT/WEAP/etc. and the dual-house stock units (`1TNK`/`3TNK` duplication collapses to the single `2TNK` MBT + faction-unique heavy). For the overlay phase, leave the stock blocks in place (least churn, campaigns still work).
- Hide the now-redundant side-token plumbing from the lobby is unnecessary — the 8 factions simply never reference allies/soviet tokens.
- The `theater.common` umbrella token (granted at the conyard) is the clean hook for any future "all THEATER factions get X" baseline ability without re-touching each actor.

**Net build flow per faction (any of the 8):** deploy MCV → conyard grants `theater.common` + `theater.structures` → build BARR/TENT (→`theater.infantry`), WEAP (→`theater.vehicles`), HPAD/AFLD (→`theater.aircraft`), SYRD/SPEN (→`theater.navy`) → full shared combined-arms roster, plus the one `tech.<faction>` unique from the conyard. Identical baseline for everyone; identity comes from the single unique (and later, civ VERBS).
# THEATER Expansion — Master Execution Plan

Facts confirmed: no `mods/theater/` yet, all theater C# traits already in `OpenRA.Mods.Common.dll`, 6 cameos present at wrong size, `PngSheet` absent from ra `SpriteFormats`. The plan is grounded. Here it is.

---

# THEATER — Master Sequenced Execution Plan

**Standalone mod. Verb-based 4X-lite reimagining of RA. Deterministic lock-step C#/.NET 8.**

---

## 0. Orienting facts (verified against the repo, not just the briefs)

- `mods/theater/` does **not exist yet** — the standalone mod is unbuilt. Everything THEATER currently lives as an *overlay inside `mods/ra/`* (4 rules files, 1 ftl, 1 lua, 2 maps).
- All THEATER C# (`TheaterCommands`, `AttackMoveByDefault`, `DevelopsWhileHeld`, `SupplyNetwork`, `SupplyNode`, `DeveloperMode` edits) is already compiled into `OpenRA.Mods.Common.dll`. **The new mod loads it with zero C# changes.**
- 6 cameos exist (`bastion, bayrak, garuda, kunai, loewe, pathfinder`) but at 128×128 — ~2× the 62×46 sidebar cell. `PngSheet` is **not** in ra's `SpriteFormats`.
- `make check` is RED on this machine for unrelated IDE0055 reasons. The real YAML gate is `./utility.sh <mod> --check-yaml`; the real C# gate is a Debug build + filename grep.

**The single biggest sequencing correction the briefs need:** every brief writes its files into `mods/ra/...` and says "add to `mods/ra/mod.yaml`." That is the *pre-standalone* world. Since standalone is final, **all of those paths must retarget to `mods/theater/...` and `mods/theater/mod.yaml`** — but only *after* Phase A creates the mod. Doing brief work against `mods/ra/` first means migrating it twice. **Phase A is therefore an absolute prerequisite for B–E, not a parallel track.** Build the house before furnishing it.

---

## CRITICAL PATH (the keystone chain)

```
A1 standalone mod  ─►  A2 unified roster  ─►  B faction vertical slice  ─►  [GO/NO-GO playtest]
   (keystone)          (fixes latent           (Federation, fully            ─► C 4X systems (gated)
                        unbuildable bug)         realized)                    ─► D HD pass
                                                                              ─► E campaign/persistence
```

Nothing in B–E can be correctly authored until **A1** exists, because every file lands in `mods/theater/`. **A2** (unified roster) is on the critical path too: until it lands, the 8 new factions can't build a normal army at all (latent prereq bug — they're in no side `Factions:` list), so any faction playtest is invalid. Cameo wiring (A3) is the one Phase-A item that can lag slightly without blocking B's *mechanics* (it blocks B's *look*, not its *function*).

**What parallelizes off the critical path** (once A1 lands):
- Cameo re-emission + `postprocess_cameo.py` 64×48 fix (art-side, no engine dependency).
- The world-map generator `--import-worldmap` (a standalone `IUtilityCommand`; touches no THEATER YAML).
- Each 4X system's **C# trait** can be written in parallel *as code*, but they must **land/integrate one at a time** behind their own lobby checkbox (determinism review per system — see gates).
- Faction doctrine *unit YAML* (the 2nd/3rd uniques) is pure data and can be drafted in parallel with B, validated in batches via `--check-yaml`.

---

## DO THIS FIRST, THIS TURN — highest-leverage executable-now shortlist

These are all executable now, unblock everything else, and carry low risk. Ordered:

1. **A1 — Create `mods/theater/` and `mod.yaml`; `git mv` the 6 THEATER files + 2 maps out of `mods/ra/`.** Mount `$ra: ra` + `$theater: theater`. Revert ra's manifest to vanilla. This is the keystone; do it first.
2. **A1-verify — `./utility.sh theater --check-yaml` and `./utility.sh ra --check-yaml`.** Both must pass. Then `make check-scripts` (lua), then `./theater-play.sh Game.Mod=theater` to confirm it launches.
3. **A2 — Author `mods/theater/rules/theater-common.yaml`** (neutral `theater.*` prereq tokens on conyard/houses + re-gate the ~17 common actors). This fixes the latent "8 factions can build nothing" bug. `--check-yaml`.
4. **A3-prep (parallel, art-side) — fix `postprocess_cameo.py` to emit 64×48** and re-emit the 6 existing cameos. No engine dependency; can run while 1–3 happen.

Do 1→2→3 strictly in order (each depends on the prior). 4 runs alongside.

---

## PHASE A — Foundations (the keystone) — **executable now**

### A1. Create the standalone `theater` mod

- **Steps (ordered):**
  1. `mkdir -p mods/theater/{rules,fluent,scripts,maps}`.
  2. `git mv` the 6 files + 2 map dirs out of `mods/ra/` into `mods/theater/` (preserves history).
  3. Author `mods/theater/mod.yaml`: start from a copy of `mods/ra/mod.yaml`; apply the field-by-field diff — `$theater: theater` + add `$ra: ra`; add `theater|scripts`; re-prefix the 3 theater rule lines + theater.ftl line to `theater|`; `Title/WindowTitle: THEATER`; keep `Assemblies`, content packages, `SupportsMapsFrom: ra`, `ContentInstallerMod: ra-content` unchanged.
  4. **Revert `mods/ra/mod.yaml`** to vanilla (strip the 3 theater rule lines + theater.ftl line) so ra is a clean baseline again.
  5. Rewrite the 2 migrated maps' headers: `RequiresMod: ra → theater`, `Rules: ra|... → theater|...`.
- **Dependencies:** none (keystone).
- **Executable-now:** ✅ yes — no C#, no rebuild (C# already in the DLL).
- **Files/systems:** `mods/theater/mod.yaml` (new), the 6 moved files, 2 map `map.yaml` headers, `mods/ra/mod.yaml` (revert).
- **Risk:** *Medium-low.* Last-wins override ordering in `Rules:` must be respected (theater overrides listed *after* the `ra|` lines). Map UID changes when headers are rewritten (expected; treat new UID as canonical). Hard dependency on `mods/ra/` existing on disk — acceptable while ra ships in-repo. Don't lose the `Map.cs` `Array.Empty<byte>()` build patch on rebase.
- **Gate:** `./utility.sh theater --check-yaml` ✅ AND `./utility.sh ra --check-yaml` ✅ AND `make check-scripts` ✅ AND `./theater-play.sh Game.Mod=theater` launches to lobby. **This is the GO/NO-GO for all of B–E.**

### A2. Unified common roster + latent-bug fix

- **Steps:**
  1. Create `mods/theater/rules/theater-common.yaml`: `ProvidesPrerequisite@theater*` tokens (`theater.common/structures/infantry/vehicles/aircraft/navy`) on FACT/WEAP/BARR/TENT/HPAD/AFLD/SYRD/SPEN, `Factions:`-keyed to all 8 blocs.
  2. Re-gate the ~17 common actors (E1/E3/E6/MEDI/MECH, JEEP/APC/2TNK/ARTY/FTRK, MH60/TRAN, PT/LST + HARV/MCV already neutral) — swap side tokens for `theater.*` in each `Buildable.Prerequisites`.
  3. Insert `theater|rules/theater-common.yaml` into `mod.yaml` `Rules:` **after** the stock roster files and after `theater-factions.yaml`, **before** `theater-units.yaml` (load order is load-bearing).
  4. Add `theater.common/infantry/vehicles/aircraft/navy` to the DEVUNLOCK Prerequisites list (or rely on conyard/houses granting them naturally — cleaner) so the sandbox still surfaces commons.
- **Dependencies:** A1 (files live in `mods/theater/` now).
- **Executable-now:** ✅ yes — pure YAML/prerequisite plumbing, nothing in `world.Tick()`.
- **Files/systems:** `mods/theater/rules/theater-common.yaml` (new), `mod.yaml` (Rules order), `theater-factions.yaml` (DEVUNLOCK).
- **Risk:** *Low.* Determinism-safe (TechTree is UI/tech layer). Real risk is rule-load order (wrong order → stock prereqs survive or dangling-ref lint). APC/FTRK ship Soviet-styled until reskin (cosmetic). Verify the inheriting uniques (SPECTER/BASTION/LOEWE/GARUDA) fully override `Buildable` and carry no residual side token.
- **Gate:** `./utility.sh theater --check-yaml` ✅, and a GUI smoke check that one new faction can deploy MCV → build BARR/WEAP/HPAD → full combined-arms roster appears.

### A3. Cameo wiring (PNG-direct pipeline)

- **Steps:**
  1. Fix `design/art/postprocess_cameo.py` to emit non-square **64×48** (currently square-only).
  2. Re-emit the 6 existing cameos at 64×48; drop into `mods/theater/bits/theater/` (new bits package, mounted in `mod.yaml` FileSystem after content packages).
  3. Add `PngSheet` to `mods/theater/mod.yaml` `SpriteFormats`.
  4. New `mods/theater/sequences/theater.yaml` with **uniquely-named** icon sequences keyed under each unit's inherited `Image` (e.g. `loewe-icon` under `3tnk`, `bastion-icon` under `ttnk`) — never overwrite the stock `icon` sequence. Register the file under `Sequences:`.
  5. Point each unit's `Buildable.Icon` at its sequence in `theater-units.yaml`. Keep `IconPalette: chrome` (ignored for RGBA at draw, must resolve for lint).
- **Dependencies:** A1 (mod + bits package). Independent of A2.
- **Executable-now:** ✅ data + a 1-line Python tweak. **The cameo art for the *other* ~20 units is blocked-on-art** (only 6 exist).
- **Files/systems:** `postprocess_cameo.py`, `mods/theater/bits/theater/*.png`, `mods/theater/sequences/theater.yaml` (new), `mod.yaml` (`SpriteFormats` + `Sequences`), `theater-units.yaml`.
- **Risk:** *Low-medium.* Sizing/centering only confirmable in a GUI run. `PngSheet` is a mod-wide additive loader change (low risk). Don't run `ConvertPngToShp` on RGBA cameos (throws).
- **Gate:** GUI run — the 6 cameos render crisp, centered, un-clipped in the sidebar.

> **Note the briefs missed:** the cameo brief writes paths into `mods/ra/`, but post-standalone the bits/sequence/SpriteFormats changes belong in `mods/theater/`. Apply `PngSheet` to the **theater** manifest, not ra's — keeping ra pristine is the whole point of A1.

---

## PHASE B — One faction fully realized (the template vertical slice)

**Pick Federation** (the briefs already nominate it; its Overwatch Ping is the lowest-risk verb — a near-stock timed-reveal support power). Federation becomes the *template* every other faction is cloned from.

### B1. Federation roster complete on the common baseline

- **Steps:** add the 2nd/3rd uniques (Sentinel AWACS on `u2`, Reaper gunship on `mh60`) to `theater-units.yaml`; wire `tech.federation` + house prereqs; add `theater.ftl` name/desc stanzas; wire the 3 cameos (Specter exists? — only `bastion/bayrak/garuda/kunai/loewe/pathfinder` art exists today, so Federation's cameos are **blocked-on-art** and ship with placeholder/stock icons).
- **Dependencies:** A1, A2, A3.
- **Executable-now:** ✅ unit YAML. Cameos blocked-on-art.
- **Files:** `theater-units.yaml`, `theater.ftl`.
- **Risk:** *Low.* `BuildPaletteOrder` collisions / faction-gating lint — run `--check-yaml` per batch.

### B2. Federation faction VERB — Overwatch Ping

- **Steps:** implement as a player-scoped support power granting a temporary `RevealsShroud`-style reveal at a target explored cell on cooldown. Must respect generated shroud (don't see through gap-gens). Wire in `theater.yaml`/faction rules, gated to Federation.
- **Dependencies:** A1, B1.
- **Executable-now:** ⚠️ **C# work** (support power + timed reveal). Anything touching `world.Tick()` follows determinism rules — synced integer timers, no float/Random/DateTime.
- **Files:** new support-power trait in `OpenRA.Mods.Common/`, faction rules YAML, `theater.ftl`.
- **Risk:** *Medium.* First verb implemented; sets the determinism pattern for all 7 other passives. Must not trivialize EW/stealth.
- **Gate:** **DETERMINISM REVIEW** before it lands (see gates section). Then GUI: ping reveals, respects shroud, cooldown reads correctly.

### 🚦 GO/NO-GO GATE #1 — **Vertical-slice fun-gate (the most important gate in the plan)**

**Playtest Federation, fully realized (common roster + 3 uniques + Overwatch Ping), in a GUI run before building any other faction.**
- **GO criteria:** the common+unique+verb loop is fun and readable; the verb feels like a *thing you do*, not a stat; the roster reads as a coherent modern army; no desyncs in a 2-player test.
- **NO-GO → STOP.** Do not scale to 8 factions on an unfun template. Iterate on Federation until it passes. This is the same discipline as the Phase 0 fun-gate. **Scaling an unvalidated template to 8 is the single biggest waste-risk in the whole program.**

### B3. Scale to the other 7 factions (only after GATE #1 = GO)

- **Steps:** clone the Federation template. Add each faction's 2nd/3rd unique units (pure YAML, batched, `--check-yaml` per batch). Implement the 7 remaining verbs **one at a time**, each its own trait, each determinism-reviewed.
- **Dependencies:** GATE #1 = GO.
- **Executable-now:** unit YAML ✅. The 7 verbs are **C# work**, staged.
- **Risk:** *Medium-high.* Double-booked bodies (YAK used by 4 factions, V2RL by 3) look identical until reskinned — flag for art, don't block data. Verb balance is unvalidated until playtested. Surge/Relocate/Intercept/No-Fly all reach `world.Tick()` — determinism-critical.

> **Brief contradiction to resolve:** the doctrine brief lists Continental's "Type-Charge Sapper" on an "E1-ish demo body" and Anatolian's "Kargu" on a "light aircraft body" — these bodies aren't in the confirmed reuse list and may need a new actor or a warhead graft. Treat those two as *needs-design* before *needs-art*, not executable-now.

---

## PHASE C — 4X systems, one at a time, each toggleable — **mixed (C# heavy)**

Order chosen by **leverage × risk**, not brief order. Research first (it's the unlock substrate the others lean on), then Minerals (reuses Research's prereq pattern + adds the Codex), then Diplomacy (the riskiest — mutates masks the engine never mutated at runtime).

Each system: **ships behind its own `LobbyPrerequisiteCheckbox`, default OFF**, in its own opt-in `theater-*.yaml`. Each lands independently and is determinism-reviewed before merge.

### C1. Research / tech-era system

- **Steps:** new `ResearchManager` player trait (ITick integer income + IResolveOrder StartResearch + ITechTreePrerequisite unlock + ISync) + trivial `ProvidesResearch` companion. v1 driven by `/research <id>` chat command in `TheaterCommands.cs` (issues a **synced order**, never mutates trait directly). v1.1: copied ProductionPalette-style widget. Opt-in `theater-research.yaml` with `LobbyPrerequisiteCheckbox@RESEARCH`.
- **Dependencies:** A1, A2 (unlocks gate units that must exist).
- **Executable-now:** ⚠️ C#. 1 real trait + 1 trivial companion. Unlock side is 100% existing plumbing (zero consumer code).
- **Risk:** *Medium.* Don't call `TechTree.Update()` per tick — only on Complete(). Income loop must stay interval-gated, no LINQ allocs in hot path. UI is the largest chunk — **ship the chat command first to de-risk**, defer the widget.
- **Gate:** determinism review (synced order, `[Sync]` on points/cost/interval); GUI: newly-unlocked buildables appear mid-match.

### C2. Real-mineral economy + Codex

- **Steps:** 100% YAML economy (5 minerals onto `ResourceLayer`/`ResourceRenderer`/`PlayerResources.ResourceValues`). New `MineralLedger` player trait (clone of `ProvidesPrerequisite`; listens to `INotifyResourceAccepted`, one-way flag → tech prereq). New `CodexEntry` data trait + `CodexLogic` widget (in-game-safe, `Ui.CloseWindow()` not `Game.Disconnect()`). MenuButton in `ingame-player.yaml` TOP_BUTTONS. Opt-in `theater-minerals.yaml`.
- **Dependencies:** A1, A2; reuses C1's prereq-gating pattern (uranium/rare-earths gate units).
- **Executable-now:** economy YAML ✅. `MineralLedger` C# ✅ (small). Codex widget = **new UI C#**, GUI-verify-only. Distinct mineral art is **blocked-on-art** (all 5 reuse gold*/gem* sprites initially).
- **Risk:** *Medium.* `ResourceIndex` is a single map byte — confine to new THEATER maps (existing maps silently remap). Harvester auto-collects all 5 → map design must keep rare-earths/uranium scarce/contested or the gate trivializes.
- **Gate:** determinism review of `MineralLedger`; `--check-yaml`; GUI for Codex layout + art load.

### C3. Diplomacy (highest risk — schedule last in C)

- **Steps:** new `DiplomacyManager` world-actor trait (4 levels War/Truce/Trade/Allied → binary mask flips; pair keys ordered `(minIdx,maxIdx)`; trade via integer `TakeCash`/`GiveCash` on interval) + `DiplomacyClient` player trait (IResolveOrder Propose/Accept/Cancel/DeclareWar). **One real engine change:** add `INotifyRelationshipChanged` to `OpenRA.Game/Traits/TraitsInterfaces.cs` and have `AffectsShroud` re-run `UpdateShroudCells` on it (+ `FrozenActorLayer.RefreshState`) — **without this, shared vision silently no-ops.** MVP: `/ally /truce /trade /war /accept` chat commands. Opt-in `theater.yaml` wiring.
- **Dependencies:** A1; lands last in C.
- **Executable-now:** ⚠️ C# + a genuine `OpenRA.Game` engine change. UI panel deferred (chat MVP first).
- **Risk:** *High.* Mutating relationship masks at runtime is a path this branch **has never exercised** — needs a GUI playtest to confirm nothing caches stale relationship state (palette/stance colors via `SetupRelationshipColors`). Truce-snipe griefing → cooldown-gate DeclareWar. Validate all orders against live players (reject dead/spectator/NonCombatant) or risk desync. AI safe by construction (bots never issue/accept).
- **Gate:** **DETERMINISM REVIEW (mandatory, this one is the most desync-prone)** + a dedicated 2-player GUI desync test before merge.

> **Sequencing call:** the briefs present C1/C2/C3 as co-equal. They are not. Diplomacy is the only one that touches `OpenRA.Game` and mutates never-before-mutated state — it must be **last in C and most heavily reviewed**. Research is the substrate the other two reference, so it's first.

---

## PHASE D — Visual / HD pass — **mostly blocked-on-art (runs partly in parallel)**

This phase is gated on the **art pipeline** (GPT-image + Blender turntable), not on engine work. It can *start* as soon as A3 proves the PNG-direct cameo path, and runs in parallel with B/C wherever artists are available — but it *completes* late because it depends on the full roster being finalized.

### D1. Cameo backlog (all units beyond the 6 existing)
- **Blocked-on-art.** Each new cameo: generate (GPT-image on magenta → chroma-key → 64×48) → drop in bits → 1 sequence entry → 1 `Buildable.Icon` line. Mechanical once art exists.
### D2. Reskin double-booked bodies
- **Blocked-on-art.** Priority queue: APC + FTRK (Soviet silhouettes in an Allied army), then YAK (4 factions) and V2RL (3 factions). Blender turntable for 32-facing bodies.
### D3. De-blur / HD resource overlays, loadscreen, distinct mineral sprites
- **Blocked-on-art.** Swap `LoadScreen` images to `theater|uibits` (must add `theater|uibits` to SystemPackages + ship PNGs or it fails at startup). Distinct mineral overlays so the 5 minerals stop looking identical.

- **Risk:** *Medium.* Pure throughput/consistency risk, not technical. Internal-consistency rule: don't mix a Soviet rifleman with an Allied tank — keep each army one look.

---

## PHASE E — Campaign + persistence — **last; depends on everything above**

### E1. World-map generator (can start EARLY — it's independent)
- New `--import-worldmap` `IUtilityCommand` (landmask PNG → `.oramap` via the proven `new Map(...).Save(ZipFileLoader.Create())` path). Tier-0 (hard water/clear, 2-tile palette) first; Tier-1 coast auto-tiling (LatTiler/Terraformer) later.
- **Dependencies:** none on THEATER YAML — **this parallelizes off the critical path and can be built during Phase B/C.** Only the *output map* depends on the mineral/territory rulesets to be interesting.
- **Executable-now:** ✅ Tier-0 is dependency-free C#. Sourcing/cleaning the landmask PNG is the real cost (art-side).
- **Risk:** *Medium.* 256×256 (~65k cells) may stress AI pathfinding/minimap/load — validate perf; 192×192 fallback. Real continents are asymmetric → explicit spawn balancing. Depends on internal MapGenerator APIs (not a stable contract) — keep Tier-0 dependency-free as fallback.

### E2. Campaign persistence
- Lives in **map Lua / mission state**, NOT in any engine trait. Research `completed`, mineral unlocks, diplomacy treaties persisted across missions via map script. `theater|missions.yaml` replaces `ra|missions.yaml` when campaigns diverge.
- **Dependencies:** C1/C2/C3 (the systems being persisted must exist and be validated).
- **Executable-now:** ❌ blocked on C. Additive Lua, no engine change.
- **Risk:** *Low-medium.* Keep save/load out of the runtime traits (briefs are explicit: don't bake persistence into ResearchManager/MineralLedger/DiplomacyManager).

---

## GO/NO-GO GATES (consolidated)

| Gate | When | Criteria | Failure action |
|---|---|---|---|
| **A1 launch gate** | After standalone mod created | both `--check-yaml` pass, lua passes, `Game.Mod=theater` launches to lobby | Fix prefixes/load-order; nothing downstream proceeds |
| **A2 roster gate** | After unified roster | a new faction builds a full combined-arms army in GUI | Fix prereq tokens / load order |
| **GATE #1 — VERTICAL-SLICE FUN-GATE** | After Federation fully realized (B) | the common+unique+verb loop is fun, readable, desync-free | **STOP. Iterate Federation. Do NOT scale to 8.** |
| **DETERMINISM REVIEW** | Before *every* sim trait lands (B2, each C system) | synced integer state, `[Sync]` marked, no float/Random/DateTime/wall-clock reached from `world.Tick()`; orders are synced orders | Block merge until clean |
| **Diplomacy desync gate** | Before C3 merges | 2-player GUI test, no stale relationship/color state | Block merge |
| **E1 perf gate** | After world-map gen | 256² map loads + AI paths acceptably | Fall back to 192² |

---

## What the briefs missed or contradicted (called out plainly)

1. **All 8 briefs target `mods/ra/...`** — pre-standalone paths. Post-standalone, *everything* retargets to `mods/theater/...`. **Phase A must precede all brief work**, or every file gets authored twice. This is the #1 sequencing fix.
2. **4X systems are presented as co-equal; they aren't.** Diplomacy is the only one touching `OpenRA.Game` + mutating never-mutated runtime state → schedule it **last in C, most reviewed**. Research is the unlock substrate → **first**.
3. **Doctrine brief vs unit-reuse list:** Continental "Type-Charge Sapper" and Anatolian "Kargu" reference bodies *not* in the confirmed-reuse set — they're *needs-design*, not executable-now data. Don't batch them with the clean YAML uniques.
4. **Cameo art reality:** only 6 cameos exist; Federation's own uniques (Specter/Sentinel/Reaper) have **no art**, so B ships with placeholder icons. The cameo *pipeline* (A3) is executable now; the cameo *content* for ~26 units is a D-phase art backlog.
5. **World-map generator is mis-placed in mental ordering** — it has zero dependency on THEATER YAML and should be built *in parallel during B/C* (it's E only because its useful *output* wants the mineral/territory rules).
6. **Persistence creep:** three briefs hint at saving state; all three also say keep it out of the runtime trait. The plan makes that explicit — persistence is E2, Lua-only.

---

**Bottom line:** the standalone mod (A1) is the keystone and the first thing to do this turn; A2 fixes a latent bug that makes the 8 factions unplayable; then prove **one** faction end-to-end before scaling; gate every sim system on a determinism review and ship each 4X layer toggleable behind a lobby checkbox, Diplomacy last. Art and the world-map generator parallelize off the critical path; campaign persistence is the final, Lua-driven phase.
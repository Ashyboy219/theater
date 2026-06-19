# THEATER — Expansion Prep & Gap Analysis

*Written 2026-06-18, before pivoting to the full build of the three founding goals.*

This is the "what we have / what we're missing / what we must decide" map to read before we start the
big push. It is deliberately honest about gaps and cost.

---

## The three founding goals (the north star)

1. **Modern, revamped-but-retro visuals** — de-blur the classic look, keep the readable retro silhouette, push it to a cleaner HD finish.
2. **Real modern countries as factions** — each with unique unit(s)/building(s) and a distinct identity, over a **shared common roster**.
3. **4X-lite depth done fast** — the good parts of Civ / Rise of Nations / Stellaris (research, diplomacy, a real-ore economy with a mineral Codex) without the slow parts. A *light, toggleable* civ layer for matches; a *heavier, persistent* one for campaign.

---

## Where we are today (inventory)

| Area | Built | Where |
|---|---|---|
| Factions | 8 contemporary blocs (USA, Russia, China, Germany, UK, Japan, India, Turkey), side-typed, stock subfactions hidden, faction-scoped `tech.<faction>` from the conyard | `mods/ra/rules/theater-factions.yaml` |
| Faction-unique units | 1 each (reskins of existing RA bodies), faction-gated | `mods/ra/rules/theater-units.yaml` |
| Shared structure | THCOM "Theater Command" — radar + combined-arms paradrop (a real *verb*) | `mods/ra/rules/theater-structures.yaml` |
| Territory economy (hybrid) | Control nodes that develop-while-held; capture-tech lab; bounty — **map-scoped slice** | `mods/ra/rules/theater.yaml`, `Traits/DevelopsWhileHeld.cs`, `Traits/World/SupplyNetwork.cs`, `SupplyNode.cs` |
| UX: attack-move by default | plain move auto-engages; Alt = plain move, Ctrl = force-fire | `Traits/AttackMoveByDefault.cs` (on `^Infantry/^Vehicle/^Ship` in theater.yaml) |
| Dev sandbox | `/dev` chat toggle (unlimited cash, instant build, build anywhere, free power), human-only, **faction-respecting** (no AllTech); `/visibility`; `/factions` | `Traits/Player/DeveloperMode.cs`, `Commands/TheaterCommands.cs` |
| Art pipeline | gpt-image-2 (single-angle) + Blender turntable (32-facing) — **proven, mostly unused** | `design/art/*.py`, `design/art/theater/` (8 faction emblems) |
| Maps | 2 test maps on an "A Path Beyond" base (stand-ins) | `mods/ra/maps/theater-phase0`, `theater-phase0b` |
| Design record | full design + honest fun-validation, hybrid-economy pivot, vision | `design/contemporary-openra-design.md`, `hybrid-territory-civ.md`, `game-design-vision.md` |

**Verified working:** Release build clean, `--check-yaml` clean, maps load into a live match with no crashes.

---

## Gap analysis — goal by goal

### Goal 1 — Visuals (biggest cost, lowest progress)
- **Have:** 8 faction emblems; a proven 2-track art pipeline (gpt-image-2 for flat art, Blender for unit turntables).
- **Missing:**
  - **Bespoke unit art.** Every "unique" unit is currently a *reskin* (reuses an RA sprite). Zero new 32-facing bodies have actually been rendered. The Blender pipeline is proven but unused on a real unit.
  - **Cameos not wired.** Even the cameos we generated aren't hooked into the sidebar (need icon sequences in `sequences/*.yaml` + `RenderSprites`/`Buildable: Icon:`).
  - **Terrain/building HD pass.** No de-blur/upscale of tilesets or structures yet — the "revamped retro" look is unstarted.
  - **Modern UI chrome.** Sidebar/menus are stock RA.
- **Risk:** art is the schedule driver. Need an explicit *priority order* and a *quality bar* per asset class.

### Goal 2 — Factions (furthest along, but shallow)
- **Have:** 8 factions, 1 unique unit each, faction gating that lint can't catch is all handled (flags, random pools, starting units, sidebar suffixes — see the runtime-crash notes in memory).
- **Missing:**
  - **A unified common roster.** Factions currently inherit their *Side's* roster (Allies vs Soviet), so "commons" differ by side. The vision is one shared modern baseline + per-faction uniques. This is a **structural refactor**, not a tweak.
  - **Depth per faction:** only 1 unique unit, **0 unique buildings**, no faction *identity* (passive doctrine, economy lean, or signature mechanic).
  - **Faction balance/playstyle design** — nothing written per faction beyond a one-line blurb.
- **Decision needed:** how many uniques per faction, and what each faction's *identity* is (doctrine, not just a skin).

### Goal 3 — 4X depth (most new engineering, least built)
- **Have:** the hybrid *territory* economy slice (verbs-not-numbers principle validated); a Codex art seed (`design/art/codex_rare_earths.png`).
- **Missing (each is a new system):**
  - **Research / tech-era system** — no in-game research. Needs new C# (a research trait + UI, or an era/tech-tree layer on top of prerequisites). Must be deterministic.
  - **Diplomacy** — only stock ally/enemy. Real relations (truce, trade, shared vision, betrayal) = new C# + UI + AI hooks.
  - **Real-mineral economy + in-game Codex** — ore is still RA ore; the "real minerals with a Codex" idea has art but no system or UI.
  - **The civ-layer toggle** — "light for matches / heavy persistent for campaign" needs a lobby option + a persistence mechanism.
- **Risk:** these touch the deterministic sim and the UI widget system. Highest-skill work; needs determinism discipline (no float/Random/DateTime in sim paths).

---

## Missing foundations / cross-cutting blockers

1. **Mod identity decision (do this FIRST).** THEATER is currently an *overlay on the `ra` mod*. For a real expansion we likely want a **standalone `theater` mod** (own `mod.yaml`, menu, content manifest, version). This affects packaging, the launcher, save compatibility, and how cleanly we can diverge from RA. Deciding late = painful migration.
2. **Bespoke world map.** The "real-world map" goal needs either the **in-game map editor** (GUI work) or a **programmatic map/`.bin` generator**. We're on an "A Path Beyond" stand-in. This blocks the headline map.
3. **Owner-change verbs.** "Capture → support power / forward production" needs new C# — `SupportPowerManager` registers via ActorAdded/Removed, which don't fire on capture (documented in memory). Several exciting territory verbs are gated on this.
4. **Common-roster refactor.** Prerequisite to a clean faction system (see Goal 2).
5. **Persistence / save-load** for the campaign civ layer.
6. **AI awareness.** New systems (research, diplomacy, territory verbs, new units) need AI hooks or the skirmish AI degrades.
7. **Determinism review gate.** Any new sim system must pass the no-float/Random/DateTime rule and `[Sync]` discipline. Worth a checklist before each system lands.
8. **Test/validation harness.** The headless `Launch.Map` smoke test works for "does it crash." We have no automated *gameplay* assertions; consider extending for regression as systems grow.

---

## Decisions to make before Phase A (the forks)

- **D1 — Mod identity:** standalone `theater` mod vs. keep overlaying `ra`. *(Recommend: branch a standalone mod early.)*
- **D2 — Art priority & bar:** which faction/units get bespoke 3D bodies first; HD-terrain approach (AI-upscale vs hand-redraw); the quality bar that counts as "done."
- **D3 — Faction depth:** uniques-per-faction count; each faction's identity/doctrine.
- **D4 — 4X scope for matches:** how *light* is the match-layer civ (research only? + diplomacy? + minerals?) vs. what's campaign-only.
- **D5 — World map:** editor vs. generator for the real-world map.

---

## Proposed roadmap (sequence, not schedule)

- **Phase A — Foundations:** resolve D1; unified common roster; wire cameos/sidebar art for the *current* units; pick the world-map approach. *(Low-risk, unblocks everything.)*
- **Phase B — One faction, fully realized (vertical slice):** take **one** bloc (e.g. Federation/USA) all the way — bespoke art, multiple uniques + a unique building, a real identity. This becomes the **template** every other faction is cut from. Validate fun before scaling to 8.
- **Phase C — 4X systems, incrementally & toggleable:** **research first** (most contained), then **mineral economy + Codex**, then **diplomacy**. Each ships as a lobby-toggleable layer with AI hooks and a determinism review.
- **Phase D — Visual overhaul:** HD terrain/building pass + modern UI chrome, once the unit-art pipeline is flowing.
- **Phase E — Campaign + persistence:** the heavier civ layer, save/load, the real-world map.

**Principle to keep (already validated):** territory/civ features should grant **verbs** (new things to *do*), not **numbers** (passive stat auras). Match-layer stays light and non-persistent; campaign carries the weight.

---

## Top risks

- **Art throughput** dominates the timeline — start the Blender pipeline on a real unit in Phase B to find the true per-unit cost early.
- **Scope creep in 4X** — three big systems; ship them one at a time, toggleable, or they'll each be half-done.
- **Determinism regressions** in new sim systems — the #1 subtle-bug class here.
- **Deciding mod identity late** — cheap now, expensive after content piles up.

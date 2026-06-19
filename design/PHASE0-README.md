# THEATER — Phase 0: the Zero-Art Thesis Gate

This is the **kill-gate** the design validation demanded: prove the single load-bearing idea —
**"territory IS the economy" beats "mass one unit and A-move"** — using only stock Red Alert art,
before a dollar is spent on factions, systems, or sprites. If this isn't fun, the whole project stops here.

## What it changes (the whole mechanic, in one breath)
- **Harvesting no longer makes money.** Ore/Gems are worth 0 (`territory-economy.yaml`).
- **Income comes from Control Nodes** — capturable buildings spread across the map (they reuse the
  Oil Derrick art/label, zero new art).
- **A node only pays you while it's *supply-connected* to your base** — i.e. it can trace a chain of
  *your* nodes, each within link range, back to your Construction Yard. **Cut the chain (capture or
  destroy a relaying node) and everything downstream stops paying** — without ever touching the enemy base.

That last rule is what makes this more than "King of the Hill with cash": it turns the *map* into the
economy and makes spreading out, holding ground, and raiding supply the core verbs — structurally
killing the deathball, because massing one blob and A-moving *abandons the map and your income*.

## What was built (all compiling; Release green; my files analyzer-clean)
| File | Role |
|---|---|
| [`OpenRA.Mods.Common/Traits/World/SupplyNetwork.cs`](../OpenRA.Mods.Common/Traits/World/SupplyNetwork.cs) | World manager. Every `Interval` ticks, flood-fills supply from each owner's source nodes through owned nodes within link range. Pure integer math → deterministic / lockstep-safe. |
| [`OpenRA.Mods.Common/Traits/SupplyNode.cs`](../OpenRA.Mods.Common/Traits/SupplyNode.cs) | Per-actor node. Grants a `supplied` condition while connected; `IsSource: true` marks the base anchor. |
| [`mods/ra/rules/territory-economy.yaml`](../mods/ra/rules/territory-economy.yaml) | The ruleset: ore→0, `SupplyNetwork` + the Lua auto-placer on the world, `SupplyNode` source on the Construction Yard, and the `CTRL` control-node actor (income gated on `enabled && supplied`). **Opt-in** — not in `mod.yaml`, so the shipping RA mod and `make check` are untouched. |
| [`mods/ra/scripts/territory-setup.lua`](../mods/ra/scripts/territory-setup.lua) | Auto-placer. On world load, spawns neutral Control Nodes in spokes from each player's start toward the map centre — so *any* skirmish map works with no manual placement. Deterministic (integer math only). Syntax-checked with `luac`. |
| [`mods/ra/maps/theater-phase0/`](../mods/ra/maps/theater-phase0/) | A ready-to-play skirmish map (**"THEATER Phase 0 - A Path Beyond"**, a copy of the stock 8-player map with the ruleset attached). Test-loads cleanly via `--check-yaml`. |

## How to try it — one click
> Honest caveat: I can't run the GUI here, so I've validated everything that loads headlessly
> (build, ruleset, map, Lua syntax) but **not the live playtest itself** — which *is* the gate.
> Income numbers and node spacing may want a little tuning once you feel it.

1. **Build:** `make` (verified green on this machine).
2. **Launch** Red Alert: `./launch-game.sh`.
3. **Skirmish → pick the map "THEATER Phase 0 - A Path Beyond"** → start. Control Nodes auto-place,
   harvesting earns nothing, and your income comes entirely from holding supply-connected territory.

*To use the mode on a different map:* add `Rules: ra|rules/territory-economy.yaml` to that map's
`map.yaml` (the Lua auto-places nodes — no editor work needed). For hand-tuned node layouts you can
still place `ctrl` actors manually in the Map Editor (category *Tech building*).

## The success metric (what you're actually judging)
The validation panel's new first-class metric: **"how much of a contested fight is your camera/cursor on
your own army vs. your base?"** Play 2–3 matches and watch for:
- Do you find yourself **fighting over the map** to hold/extend nodes, rather than turtling and massing?
- Does **cutting a relay node to choke the enemy's income** (instead of grinding his base) feel good?
- Does massing one unit feel **worse** than a spread, combined-arms hold?
- Can a new player **explain why they lost** ("you cut my supply / you out-held the map")?

If yes → the thesis holds; proceed to Phase 1 (the determinism spike + first strategic stockpile).
If it feels like KotH-with-extra-steps or a turtle-fest → we learned it cheaply and redesign.

## Tuning knobs (all in `territory-economy.yaml`)
- `CTRL → CashTrickler → Amount / Interval` — income per node (default 35 credits / 25 ticks ≈ 35/sec).
- `SupplyNode → LinkRange` (on `CTRL` and `FACT`) — how far supply jumps between nodes (default `12c0` = 12 cells).
- `SupplyNetwork → Interval` — connectivity refresh rate (default 25 ticks ≈ 1s of lag after a cut).
- Node **count & placement** in the map — the real design lever for "is the map worth fighting over."

## Known-good / known-limits
- ✅ Release build green; both new traits pass the StyleCop/Roslynator analyzers.
- ✅ Ruleset + Lua + ready map all load in the engine (`./utility.sh ra --check-yaml` passes, incl. test-loading
  "THEATER Phase 0 - A Path Beyond"). Lua syntax verified with `luac`.
- ⚠️ `make check` reports ~3,300 **pre-existing** `IDE0055` formatting errors in *untouched* stock files
  (CVec.cs, WAngle.cs, CRC32.cs…) — an artifact of this machine's .NET SDK (8.0.128) being stricter than
  upstream CI, **not** from this work. My two files are clean.
- ⬜ Not validated headlessly: the Lua *runtime* (only runs at actual play) and the **fun verdict** itself —
  both need a GUI run. If the auto-placer misbehaves at runtime, fall back to manual editor placement and ping me.

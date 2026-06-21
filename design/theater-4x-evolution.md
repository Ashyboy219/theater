# THEATER — 4X Evolution (the "more game" direction)

> North star for the `/loop "make THEATER a deeper 4X-RTS"` pass. Each loop iteration reads this,
> picks the next highest-value slice, designs it, implements it (data-first; C# only when forced),
> verifies (check-yaml → build → boot), and commits. Fallback when nothing substantive is queued:
> keep revamping visuals toward a modern-but-retro-revamped HD feel.

## Why (the user's own words, distilled)
The military-RTS slice is solid but **the player gets bored** — it lacks complexity, branching tech,
and meaningful decisions, and it's too short. The fix is **depth + longevity + spectacle**:
- Bring in the **economy / 4X / Civilization side** — not just military. Real resources, research,
  build decisions, escalation. Fuse the best of **Civ** (tech tree, eras, improvements), **Sins of a
  Solar Empire** (multi-resource economy, capital ships, persistent escalation) and **Rise of Nations**
  (territory/attrition, age advancement) — *while keeping OpenRA's real-time base-build + combat core*.
- **Spectacle / gargantuan scale**: "oh my god, look at this HUGE ship I just built." End-game
  super-units that are real payoffs.
- **More cool assets, more things to do, more decisions, longer games.**

## Design pillars
1. **Multi-resource economy.** Beyond a single cash pile: a primary (credits/ore) + a **strategic
   resource** (e.g. *rare-earths / alloys*) gated by territory, + the existing **power**. Big units and
   high tech cost strategic resource, so expansion and economic choices matter.
2. **Research tech tree (real, branching).** A **Research Lab** spends resources + time to unlock units,
   upgrades, abilities and *ages* — not just building prerequisites. Branching so two players diverge.
3. **Ages / tiers (escalation + longevity).** RoN-style age advancement (e.g. *Modern → Information →
   Autonomous → Orbital*) that gate tech tiers and **escalate the scale** of what you can build. This is
   the primary lever for "longer, deeper games" and for staging the spectacle units.
4. **Meaningful decisions.** Doctrines (have) + research priorities + economy/expansion choices +
   (later) light diplomacy. Mutually-exclusive branches so games play differently.
5. **Spectacle / scale.** Gargantuan flagship capital units per age — huge ships, mobile fortresses,
   super-aircraft — expensive, slow, awe-inspiring, with their own Blender mega-models.
6. **Keep the RTS heartbeat.** Real-time build + micro stays core; 4X systems layer *on top*, never
   replace the moment-to-moment RTS.
7. **Art: retro-revamped HD.** Blender 32-facing bodies (proven pipeline) + GPT-image cameos/UI/Codex.

## Engine-feasibility map (OpenRA realities — informs data-first vs C#)
- **Second resource / strategic resource:** OpenRA's economy is single-currency (`PlayerResources` cash).
  A true second resource needs **C#** (a parallel resource trait + UI), OR a data-only proxy first:
  model the strategic resource as a **slow-trickle prerequisite token** produced by a territory building
  (reuse the buy-once `ProvidesPrerequisite` + a timed/limited production queue, like the existing
  doctrine/capability system) so "you must hold X to build Y." Start data-only; graduate to C# when the
  proxy chafes.
- **Research lab / tech tree:** **data-first.** Reuse the proven Command-queue pattern
  (`ClassicProductionQueue@Research` on a Research Lab; each tech a non-occupying `BuildLimit:1` actor
  with `ProvidesPrerequisite` that gates units/upgrades). Branching = `~!other` negatives. No new C#.
- **Ages:** **data-first.** Each age = a buy-once `age.N` prerequisite (gated on the prior age + cost +
  a building), unlocking the next tech tier. `GrantConditionOnPrerequisite` on templates for age stat
  bumps (mirrors the doctrine condition pattern — grant on the unit template, NOT Player).
- **Spectacle units:** **data + art.** Big stats, multi-cell footprint, escalating weapons; Blender
  mega-bodies. Mostly reuses existing traits.
- **Map territory / expansion:** the Phase-0 `SupplyNetwork`/`SupplyNode` traits already exist
  (territory-as-economy thesis) — revive/extend them for the strategic-resource gating.

## Roadmap (loop pulls the next unchecked slice; keep each a clean, verified commit)
- [x] **P1 — Research Lab + first tech tree** ✓ (commits 852ee8f, d6f22e6): mid-tier `rlab` feeding the
      Command queue; 6 accumulating upgrades (ballistics/armor/optics/propulsion/small-arms/logistics) + a
      mass-vs-precision branch; condition-grants on ^Vehicle/^Infantry; AI builds the lab + researches.
      Reused the Command queue (no new tab). FOLLOW-UPS: a dedicated Research tab + a distinct rlab body.
- [x] **P2 — Ages/eras** ✓ (theater-ages.yaml): Modern→Information→Autonomous→Orbital, a buyable gated
      sequence; cumulative army-wide escalation (tankier/+firepower/+sight) on ^Vehicle/^Infantry/^Ship/
      ^Plane; `age.<era>` tokens gate future content (Orbital → P4 spectacle units); AI advances ages.
      FOLLOW-UP: gate some existing/late content on ages once balance-tested.
- [x] **P3 — Strategic resource (proxy)** ✓ (theater-economy.yaml): the Alloy Extractor (`alloyex`) — a
      buildable economy building that drips income (CashTrickler, so building several/expanding pays) AND
      provides the `alloys` token gating the apex tier (Orbital age now requires it; P4 spectacle units
      will too). Honest proxy: `alloys` is a boolean prereq + income, not yet a counted currency (P6=C#).
- [x] **P4 — Spectacle capital units** ✓ (theater-spectacle.yaml): the apex TRIO, all 128px (2x-size)
      Blender mega-models gated behind the full ladder (Orbital age), BuildLimit 2 each — LEVIATHAN
      (naval arsenal dreadnought, 500k HP), CITADEL (land mobile-fortress, 480k HP, siege cannon), ARCHANGEL
      (air heavy gunship, sustained barrage, AA-vulnerable). Technique: 128px / ortho 14 / single-body, all
      weapons baked into the hull. FOLLOW-UP: per-faction spectacle variants.
- [ ] **P5 — Economy buildings + "things to do"**: civilian/economy structures, upgrades, map objectives,
      secondary win conditions — more to manage between fights.
- [ ] **P6 — Strategic resource (real, C#)**: graduate the proxy to a true second resource + sidebar
      readout, once the data proxy proves the design.
- [ ] **P7 — Light diplomacy / inter-faction systems** (stretch): alliances, trade, tribute.
- [ ] **Always-available fallback — Visual revamp**: continue modernizing battlefield bodies, cameos,
      effects, and UI toward the retro-revamped HD feel (infantry bodies, husks, structures, projectiles).

## Guardrails
- **Keep it fun & playable each step** — every commit must check-yaml clean + boot clean; don't half-land
  a system. Prefer one complete vertical slice over many stubs.
- **Don't break what works** — the modern factions, the 34 custom bodies, doctrines, advanced targeting,
  formations all stay. 4X layers on top.
- **Honesty** — if a slice needs C# or a real GUI test that can't be done headlessly, say so in the commit
  and the loop summary; don't fake "done."
- **Public repo**: https://github.com/Ashyboy219/theater (branch `theater`) — push after each slice.

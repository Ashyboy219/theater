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
- [x] **P4 — Spectacle capital units** ✓ (theater-spectacle.yaml): the apex QUARTET, all 128px (2x-size)
      Blender mega-models gated behind the full ladder (Orbital age + alloys), BuildLimit 2 each — LEVIATHAN
      (naval arsenal dreadnought, 500k HP), CITADEL (land mobile-fortress, 480k HP, siege cannon), ARCHANGEL
      (air heavy gunship, sustained barrage, AA-vulnerable), TEMPEST (strategic rocket-artillery, 12-rocket
      saturation). Technique: 128px / ortho 14 / single-body, all weapons baked into the hull. POLISH (done):
      a shared TitanBlast death explosion, suppressed mismatched stock husks, and `large_explosion` impact
      blooms on the two biggest giants' main guns. FOLLOW-UP: per-faction spectacle variants.
- [x] **P5 — Economy buildings** ✓ (theater-economy.yaml): the Bank (pure income, stackable — invest-in-
      economy decision), the Fusion Reactor (3x power for the power-hungry late game), and the Economy Network
      research (CashTricklerMultiplier +30% to all income buildings). Real economy management/decisions.
      FOLLOW-UP (needs Lua/C#): map objectives + secondary win conditions.
- [x] **P5.1 — Tier-2 research** ✓ (theater-research.yaml): five advanced techs gated on the Information
      age (net-centric range, reactive armor, autoloaders, avionics, naval combat) — extends the tree's
      accumulating spine into the mid-game.
- [x] **P5.2 — Divergent decision-forks** ✓: beyond Mass-vs-Precision, two new mutually-exclusive research
      forks so games branch — **War Economy vs Industrial Base** (theater-research-economy.yaml: guns-vs-butter,
      unit cost vs income-building output) and **Maneuver vs Fortress** (theater-research-posture.yaml:
      mobility/vision on mobile units vs tougher/longer-range static `^Defense`). Three universal forks +
      the eight faction doctrines = a healthy decision-space; *hold here* — more forks risk decision-overload.
- [x] **P5.3 — The Codex (educational layer)** ✓ (design/theater-codex.md + in-game): one of the three
      founding goals — the real critical-minerals/rare-earths geopolitics behind `alloys`, and the real
      modern systems ~every unit is based on. Woven into the game via real-world tooltip lines on the
      flagships, signature naval/air/land units, and the Alloy Extractor. FOLLOW-UP (chrome): a Codex panel.
- [x] **P5.4 — Strategic-sidebar legibility** ✓: the ~31-item Command queue regrouped into tech-progression
      order (capabilities → doctrines → research T1 → ages → research T2 → economy), every item iconned.
- [x] **P6 — Strategic resource (real, C#)** ✓ (commits ca30f32, a44451e): graduated the `alloys` proxy to a
      true *counted* second currency. New self-contained C# — PlayerAlloys (counted pool), AlloyTrickler
      (banks it), ProvidesPrerequisiteOnAlloys (gates the apex tier on a stockpile via the tech tree, no
      ProductionQueue surgery), ConsumesAlloys (spends on build), IngameAlloyCounterLogic (sidebar readout).
      The Alloy Extractor now banks counted alloys; apex units require a 100-alloy stockpile AND spend 100.
      Inert for other mods. Build/check-yaml/boot clean. FOLLOW-UP (live look): nudge the readout bar's Y and
      balance the trickle/cost numbers in a playtest.
- [ ] **P7 — Light diplomacy / inter-faction systems** (stretch): alliances, trade, tribute. Needs C#/UI.
- [ ] **P8 — Map objectives / secondary win conditions** (Lua, per-map): economic/tech victory paths.
- [ ] **Custom building bodies** (visual, needs in-game tuning): rlab/alloyex/bank/fusion still reuse stock
      dome/silo/apwr bodies in-world (cameos are bespoke). Footprint alignment can't be verified headlessly.
- [ ] **Always-available fallback — Visual revamp**: continue modernizing battlefield bodies, cameos,
      effects, and UI toward the retro-revamped HD feel.

> **State (current):** the data-only 4X vision is essentially complete and the original three goals are all
> met (depth via research/ages/economy/forks; real factions + ~38 custom bodies; the Codex). The genuine
> remaining frontier is C#/chrome/Lua work (P6–P8 + the Codex panel + building bodies) that needs live GUI
> testing — not faked. Loop iterations now trend toward refinement/visuals unless a real data slice appears.

## Guardrails
- **Keep it fun & playable each step** — every commit must check-yaml clean + boot clean; don't half-land
  a system. Prefer one complete vertical slice over many stubs.
- **Don't break what works** — the modern factions, the 34 custom bodies, doctrines, advanced targeting,
  formations all stay. 4X layers on top.
- **Honesty** — if a slice needs C# or a real GUI test that can't be done headlessly, say so in the commit
  and the loop summary; don't fake "done."
- **Public repo**: https://github.com/Ashyboy219/theater (branch `theater`) — push after each slice.

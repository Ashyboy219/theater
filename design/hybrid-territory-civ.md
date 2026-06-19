# THEATER — Hybrid Economy & the Two-Tier Civilization Layer
### Design addendum (supersedes Phase 0's "replacement" thesis)

> Driven by the Phase 0 playtest: pure replacement (ore→0, derricks = 100%) was rejected — it gutted what makes RA fun. This is the corrected design: **territory AUGMENTS the harvester economy**, plus a two-tier "civilization" layer (light/toggleable in matches, heavy/persistent in campaign). Designed and adversarially validated via a 11-agent workflow; verdict at the bottom.

---

## 1. The corrected economy — *augment, don't replace* (enforced structurally)

**Don't touch ore at all.** The Phase 0 error wasn't a mis-tuned number — it was *deleting the harvester loop*. The fix keeps ore at full stock strength (Ore 25 / Gems 50) and makes territory **purely additive on top**:

- **Target felt split:** ~60% ore / ~40% territory — but of that 40%, only ~half is direct cash (~20% of total); the rest is **capability** (vision, repair, tempo). A player who ignores territory still runs a complete, ~75%-strength economy — *weaker, never broke.* That ~0.75 floor is the design, not an accident.
- **Per-node cash is capped at ≈ one harvester's income** (the AoE-relic rule: a cash node is worth *one* harvester, never five), with **diminishing returns** on additional cash nodes. This is what stops territory from becoming a second, dominant wallet.
- **The triangle is built by MAP DESIGN, not new systems** (free, highest-leverage): three ore tiers by *position* — **safe-poor** ore in your base pocket (forces expansion), **medium** ore at naturals (bread-and-butter, raid-exposed), **rich-contested** ore + gems in the center (co-located with objectives). Now *expand-to-ore / protect-harvesters / fight-for-map-control* all pull on the same hands.

---

## 2. The territory structures — heterogeneous by design

The fix for "it became a control-point game" is **most structures don't print cash.** Of an 11-structure design palette, only **3 touch cash** (and 2 of those *amplify your ore* rather than printing flat income); the other **8 pay in capability**. A given map ships 6–10, so cash-printers are always a minority of objectives.

| Structure | Type | RA basis | What holding it gives |
|---|---|---|---|
| **Oil Derrick** | economic | OILB | Flat cash trickle (the one classic, legible tempo magnet) |
| **Refinery Concession** | economic | Generals Tech Refinery | +15–25% cash-per-ore — *makes your harvesters richer* (zero value if you stop harvesting) |
| **Foundry / War Plant** | economic | Generals Tech Factory | −10–15% unit cost — *spends your ore further* (rewards aggression) |
| **Prospecting Station** | economic | CoH fuel-point / D2k spice | Slowly *enriches the ore field around it* — the purest "territory augments harvesting" |
| **Watchtower / Listening Post** | power | Comms/lookout | Forward vision over a chokepoint or ore route |
| **Field Hospital** | power | HOSP | Heals your infantry — turns a forward spot into a sustainable anchor |
| **Service Depot** | power | FIX | Forward vehicle repair — changes the math of every tank trade near it |
| **Radar Outpost** | power | DOME/GPS | Locational stealth/sub **detection** — a hard counter you only get where you hold |
| **Forward Command Post** | power | FCOM | Extends buildable area / forward staging — the "develop the ground" verb, physical |
| **Reinforcement Depot** | power | Generals Reinf. Pad | Periodic free unit / forward spawn — compresses reinforcement time |
| **Tech Center / Relay** | hybrid | TECN | The map's "relic": speeds your support-power charge **and** opens a tech unlock |

Engine reality (verified): vision/heal/repair/buildable-area/detection/production/tech-unlock are **all stock traits** bolted onto a `Capturable` building — mostly mod-data. Two exceptions the validation caught are flagged in §5.

---

## 3. The two-tier "civilization" layer

**Framing that beats the Stellaris/Civ trap: "Foothold, not Empire."** You develop a contested *region* in one theater, not a galaxy — that bounded scope is *why* it can't sprawl into a click-tax. And it's **map-anchored, never menu-anchored**: every civ action is clicked on a battlefield structure and *seen* as a sprite change; no off-map panel can pull your eyes off the fight.

### Matches (the main mode) — LIGHT, TOGGLEABLE, NON-PERSISTENT
Exactly **three** things, not a system, capped at ~3–4 decisions per 40-min match:
1. **Develop-one-tier:** a held structure exposes a single *Develop* command — pay credits + a build delay, it visibly upgrades and steps up its payoff. **One tier only in a match, ever** (no 2/3 ladder). The credit cost competes with army spend ("develop this node OR build two tanks") — that's what stops it being free mandatory value.
2. **One Foothold meter:** a single 0–100 bar fed *passively* by how much developed territory you hold; crosses at most **two** thresholds/match, each granting a small edge.
3. **One Doctrine pick:** a single identity choice across the match.

**The toggle ("Develop Territory" lobby checkbox) gates ONLY this develop layer — NOT the structures.** The capturable structures are *always* present in both states (a capturable tech building is core RA). So **OFF = a byte-identical map-control game** to ON; the toggle physically *cannot* fork balance at the level of map control. It's "RA-plus-a-bit," not a different game.

### Campaign — HEAVIER, PERSISTENT
**The one law: PERSISTENCE = CAPABILITY (who you are / who survived); RESET = CAPITAL (money, base, mass army, map control).** The economy resets near-broke each mission, so the RA triangle is re-fought from zero every time (pace can't go stale). Exactly **four persistent pillars** (named, never audited — new persistence must *replace* a pillar, not add one):
1. **The Commander** — one named hero carrying level (stock `GainsExperience`) + a few unlocked "Wargear" (granted conditions). Cheapest, highest emotional ROI — build first.
2. **The Veteran Cadre** — 3–6 hard-capped survivors you *extract* at mission end (XCOM roster + Homeworld fleet emotion), returning as pre-veteran reinforcements.
3. **A Dossier** — ≤5 unlocked perks.
4. **The Home Theater** — ≤10 developed region nodes.

Anti-death-spiral floors (the Homeworld trap): hero is **downed, not dead**; guaranteed minimum starting force/base every mission; difficulty scales with *campaign progress, not player strength* (no visible rubber-banding).

**Honest cost:** OpenRA campaigns are *stateless* — verified, nothing carries between missions today. Persistence needs **new C#**: a small `CampaignState` blob serialized at mission end and re-injected at the next mission's *start* as pre-game actor inits / granted conditions (never runtime sync mutation, so determinism is safe; single-player first). This is real engineering — **deferred** until matches prove out.

---

## 4. Anti-trap guard rails (the whole reason this can work)
- **Diminishing returns** on cash nodes + **hard map cap** (e.g. 2 cash nodes) — kills the CoH territory snowball.
- **One-wallet rule:** develop is paid in credits; Foothold is state that's never spent; territory mostly pays *capability* not cash.
- **Amplify, don't duplicate:** prefer structures whose value is *zero if you stop harvesting/producing* over flat-cash printers.
- **Attention-neutrality:** a clean-RA player must win ~50/50 vs a develop-heavy player (>60% = mandatory APM = failure). Develop actions are set-and-forget.
- **Choice budget:** ~3–4 discrete civ decisions/match, hard cap.
- **Progressive disclosure:** minute one is pure RA; the develop verb unlocks after your first capture, Doctrine after your first Foothold threshold.
- **Don't co-locate snowball accelerants** (radar/repair/tech) with the richest contested ore — or the leader compounds.

---

## 5. Verdict: CONDITIONAL GREEN, scoped way down

The corrected hybrid thesis is right and worth building — **but not at full size yet.** The adversarial panel (4 lenses, all "fun-with-fixes") converged, and verifying in the repo caught two real bugs in the *proposed* design:
- **The ore-amplifier doesn't work as mod-data.** `Refinery.cs` caches resource-value modifiers *once* at the refinery's creation, so a captured *player-scoped* amplifier changes nothing. Needs C# → deferred.
- **`GivesBounty` isn't on the stock harvester** — the "protect" leg was asserted, not real. Adding it is one line (done in Phase 0b).

### Phase 0b — the cheap next playtest (BUILT & validated)
One ruleset, one map, **zero new C#**, two binary questions. Ships in `mods/ra/rules/theater.yaml` + `mods/ra/scripts/theater-setup.lua` + the map **"THEATER Phase 0b - Hybrid Economy"**:
- **Ore restored to full stock** (the harvester loop is back).
- **Two heterogeneous structures**, capped & contested at map center: **CTRLCASH** (flat cash ≈ one harvester) + **HOSP** (stock Hospital → heals your infantry). One economic, one capability — exactly the heterogeneity thesis, both pure stock traits.
- **Harvester bounty added** → the protect leg is real.
- **No civ layer yet** (no toggle/meter/develop/doctrine) — the toggle only matters once there's a layer to gate.

**The two questions it answers:** (a) does *ignoring* both structures still leave you a complete tech tree + real army at ~75%? (b) do your eyes stay on the harvester loop? Pass both → the expensive layers (develop verb, Foothold, doctrine, then campaign persistence) are earned. Fail either → we learned it cheaply.

*Deferred behind 0b: the develop verb, Foothold meter, Doctrine, the C# amplifier & support-power-charge structures, and all campaign persistence.*

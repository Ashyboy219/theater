# THEATER — Contemporary Command
### A validated design for a modern, 4X-flavored reimagining of OpenRA
*(working title; internal codename "Bleed-Modern")*

> This document is the output of a research → design → adversarial-validation pass (16 agents: 9 research dimensions, 1 synthesis, a 5-lens "will this actually be fun?" panel, and an executive verdict). It is grounded in real game-design literature, real RTS/4X precedents, the actual OpenRA engine, and real-world military/resource facts. **The verdict is deliberately not a rubber stamp — read §1 first.**

---

## 1. The honest verdict: CONDITIONAL GO

**Green-light the core thesis. Red-light shipping the whole document at once.**

The fun is real and *locatable*. The single idea all five adversarial lenses independently endorsed is **"territory IS the economy"** — income and high-tech access come from *holding and cutting ground*, not from a base-bound harvester loop. That one mechanic:
- structurally kills the "mass one unit and A-move" deathball (massing abandons the map),
- removes tedious harvester micro,
- makes the whole map legible and strategic,
- is **proven** (this is the Company of Heroes model), and
- **needs zero new art** to test.

Everything bolted onto it is where the panel concentrated its fire — and the fires overlapped in a consistent pattern. The scope, not the idea, is the enemy:

| The risk the panel found | Why it matters |
|---|---|
| **"Fast Stellaris" can overdraw your attention** | The design budgets ~5 *on-screen* choices but leaves *event-driven interrupts* (diplomacy toasts, convoy raids, air-defense saturation) unbudgeted — and they arrive mid-fight on the **opponent's** tempo. Rise of Nations works because its strategic layer is low-frequency background state set *between* fights. This design risks drifting into high-density decisions *during* fights. |
| **The onboarding safety nets are vaporware** | Repo-verified: the Encyclopedia/Codex is a 47-line static card with **no event hooks** (the "card pops when you first mine a deposit" tutor doesn't exist yet), and OpenRA ships **zero tutorial infrastructure**. Onboarding is currently *unsolved*, not solved. |
| **The AI can't play any of the four new systems** | OpenRA's bot is fixed-policy scripting. Since shipped single-player *is* skirmish-vs-bot, diplomacy/research/economy degrade against it to "beat a static timer-bot" — the exact shallow loop we're trying to kill, now with extra UI the bot ignores. |
| **Combinatorial balance is unwinnable at full scope** | It's not 12 matchups — it's 12 factions × 4 irreversible doctrines × eras × diplomacy-on/off ≈ hundreds of archetypes, ~4× StarCraft's surface, for a small team. |
| **Live diplomacy is the riskiest engine work** | Mutating player relationships mid-match touches targeting, pathing, shroud, and frozen-actor layers — a whole new class of desync, not the "~250 LOC" first guessed. |
| **It's a multi-year total conversion mislabeled as a mod** | A playable contemporary base set is realistically ~150–250 new sprite sequences *before* a single hero unit. Art is the true schedule-setter. |

**The takeaway:** this is genuinely fun and reachable — *if* we serialize hard, prove the thesis with a zero-art kill-gate first, and let art throughput set the master schedule. The design below is the **destination**; §8 is the disciplined **path**.

---

## 2. Vision

THEATER is **Red Alert's fast, readable RTS heartbeat fused with Rise of Nations' strategic systems and Company of Heroes' decision-density** — a contemporary-military reimagining of OpenRA where ~12 lightly-fictionalized real-world blocs fight 20–40 minute matches that feel like *"Stellaris envelops you in complexity, minus the slowness."*

You still harvest, build, and counter-compose under fog of war. But the shallow loop is structurally dead:
- **territory is the economy**,
- **strategic mineral deposits gate your high-tech doctrines**,
- **supply convoys are cuttable**, so you can choke an enemy's drone/nuke production without touching his base,
- **live diplomacy with bribable neutral city-states** gives even a 1v1 a third party to fight over,
- **modern layered air-defense** turns "defense" into an active cost-exchange gamble instead of a turtle wall.

Every system **cross-couples** (economy gates research → research reshapes diplomacy leverage → diplomacy reshapes the map you fight over), so depth emerges from a *few interacting rules*, not many parallel meters. The look is **"HD-Pixel diorama"** — Blender-rendered clean sprites that de-blur the classics while keeping chunky, readable retro silhouettes — and an **in-game Codex** teaches the real critical-minerals and real military tech behind every unit, the moment it becomes relevant.

---

## 3. Design pillars

1. **Decision density, not management load.** Sid Meier's filter as an *engineering constraint*: every system must add ~1 genuinely interesting, non-dominated choice every 10–20 seconds and near-zero forced busywork. If the optimal play is always the same, the system is decoration → cut or redesign it. *This is the explicit answer to "adding diplomacy/research/economy ≠ fun."*
2. **Few systems that cross-couple** (the Rise of Nations / anti-Stellaris rule). Depth from a small number of systems feeding each other, never from many parallel auto-optimized meters. Combinatorial depth from *interaction*, not additive complexity from *multiplication*.
3. **Territory is the game** (the Company of Heroes lesson). The strategic map is simultaneously the economy, the tech-access layer, and the diplomacy object. Harvester micro is minimized; holding/cutting ground is where strategy lives — which also structurally kills the deathball.
4. **Asymmetry by strategic use, not stat tiers** (the StarCraft II / real-country answer). Blocs share a base set but differ by one signature mechanic + one unique unit + one unique building. Every strength is also an exploitable seam. Matchups generate the interesting decisions.
5. **Progressive disclosure keeps it fast** (the flow-channel rule). The core RTS loop is fun *before* any advanced system unlocks; research/diplomacy/resources reveal their UI in layers, and the Codex doubles as a just-in-time tutor. Minute-one is never a 4X system-dump.

---

## 4. The four strategic systems (each engineered to stay *fast*)

### 4.1 Economy & real resources — *"1 currency + 3 stockpiles + 2 soft meters + a supply overlay"*
The deliberate sweet spot between StarCraft's 2 resources and Civ's spreadsheet (the engine cleanly supports only ~7 tile-resource indices anyway).
- **Credits** — one fluid currency, fed by two **abundant open-field resources, Petroleum + Iron**, both mapping to the existing cash pool exactly like Ore/Gems today. The classic harvest-under-threat loop survives intact; a newcomer can rely on it alone.
- **Three scarce strategic stockpiles — Rare Earths / Uranium / Lithium** — separate counters fed by a few *contested central deposits*. Not spent like cash; they act as **prerequisite + slow-upkeep gates** on three distinct top-tier doctrines (drones/EW, nuclear, directed-energy). *You can win on credits with a conventional army, but you cannot field the high-tech edge without physically holding the deposit — and upkeep means losing it visibly degrades your edge.*
- **Food/Water** — a soft population/upkeep meter reusing the Power pattern (bigger army needs more territory; exceed the cap and production/healing slow). Expansion tension, zero micromanagement.
- **Supply lines** — stockpiles are *trucked* from deposit to a Stockpile building via the reskinned harvester loop. **Cut the convoy and choke the enemy's high-tech without touching his base** — economic warfare as a real attack vector, nearly free in art.
- Factions differ by resource *efficiency/access*, not set, so the same map plays differently per country.

### 4.2 Research — *three layers, not one tree*
- **Eras (3 advancements):** Present Day → Networked/EW → Autonomous (drone-swarm) → Near-Future (hypersonic + directed-energy). Each is a big paid, timed gate that **raises the ceiling** and re-opens rock-paper-scissors. Capped at 3 to bound snowball (vs Empire Earth's 15).
- **Doctrine (one irreversible fork):** Economy / Military / Defense / Diplomacy-Intel — the CoH-style identity choice, home to each country's unique unit+building, shown up front but revealed progressively.
- **Era upgrades (mutually-exclusive "pick N of M" forks):** Drone Swarm vs Hypersonic Strike vs Directed-Energy point-defense vs Cyber/EW — and *that counter-triangle is itself the strategic game* (swarm beats armor, lasers beat swarm, cyber beats networked).
- **Hard rule (lint-enforced):** any upgrade everyone always buys in the same order gets folded into the free era unlock — no mandatory stat-taxes. Tech competes with spend (same credit pool + build slot). At most ~5 live research choices on screen; the full tree lives read-only in the Codex.

### 4.3 Diplomacy — *live, fast, binding* (and the answer to "diplomacy is pointless in 1v1")
- Extends the engine's existing Ally/Enemy/Neutral mask; a `DiplomacyManager` layers per-pair **time-boxed binding agreements** from a fixed menu of 7 (Ceasefire, NAP, Alliance w/ shared vision+control, Defensive Pact, raidable Trade Route, Vision Share, Vassal/Protectorate comeback).
- **Keystone: 1–3 neutral bribable minor factions / city-states on every map** (a militia, an oil baron, a tech lab) — highest loyalty allies the minor; an enemy can out-bid or storm it. **This is what makes diplomacy live in 1v1 and skirmish — you race the opponent for the minors.**
- **Betrayal is the fun engine:** always allowed, but telegraphed to the victim with a ~3–5s grace window and priced via a persistent Trust stat the AI/minors remember.
- One-click radial UI + dismissable countdown-ring toasts. All-integer opinion scores (lockstep-safe). Anti-stalemate: pacts decay, max 1–2 alliances, war-restlessness on long peace. **Lobby toggle so ranked 1v1 can disable it.**

### 4.4 Defense — *layered, tiered, active cost-exchange* (defense buys time, never invulnerability)
The load-bearing real-world fact: a $35K drone soaking a $4.2M interceptor is a 120:1 loss — so defense is an *economic gamble*, not a wall.
- **3-tier air-defense ladder, band-locked:** cheap Gun-SHORAD → C-RAM/Iron-Dome (finite magazine, chance-to-intercept) → Theater SAM (expensive, long range, **minimum-range dead zone so it must be screened**). Using a high tier on a low threat is a deliberate waste.
- **Interception = success-chance + ammo + per-shot cost** (synced RNG), magazines shown as **pips** so a drained battery is an exploitable opening — saturation/swarm is the built-in anti-turtle valve.
- **Information/EW layer** as fragile, high-value buildings (Early-Warning Radar, Jammer, Counter-Battery, Anti-Drone RF) — wins by *denial*, invites raids → tempo instead of turtling.
- **Positional fortification** that shapes *where* fights happen (flushable bunkers, trenches, minefields, modular hardpoints, decoys), not walls that end them.
- *Validation note:* the panel flagged that 2–4 live defensive decisions per assault risks splitting the player's hands off their attack. **Mitigation adopted in the roadmap:** auto-resolve interception via pre-set priorities; give the defender exactly **one** optional active verb per assault, with the real decision made *before* the fight (provisioning/positioning).

---

## 5. The 12 blocs (asymmetry by role, not power)

Lightly-fictionalized real-world blocs on OpenRA's proven sub-faction template. **Balancing rules:** (1) every faction's strength is also the *seam* an opponent attacks; (2) signature units are sidegrades with explicit hard counters, never stat-superior reskins; (3) a strong unique unit costs more or has a killable enabler building; (4) the unique building changes *how* you play, not a flat economy boost. **Launch with the 6 cleanest anchors** (★) that span the whole design space; the other six are phase-2.

| Bloc (flavor) | Identity | Signature unit | Signature building | Strategic niche / seam |
|---|---|---|---|---|
| ★ **The Federation** (USA) | Networked air/naval power-projection | *Specter* stealth strike fighter (cloak-until-fire, mobile sensor) | Joint Command Network (army-wide targeting aura) | Tempo + info dominance — but expensive, and the **C2 node sniped collapses the aura** |
| ★ **Northern Union** (Russia) | Armor + EW + massed artillery | *Bastion* EW command tank (jams enemy targeting) | Artillery Fire-Direction Center (saturation barrage) | Brute attrition + degrade enemy coordination; slow, air-vulnerable |
| ★ **Continental Bloc** (China) | Mass + A2/AD missile umbrella + drones | *Vanguard* area-denial missile battery | Integrated Defense Grid (denial dome) | Zone control & attrition-by-denial; weak to mobile/flanking pressure |
| ★ **Levant Shield** (Israel) | Active-protection + interception + precision | *Aril* APS tank (intercepts the first missile) | Interception Dome (Iron-Dome; auto-kills artillery/missiles) | **Hard-counters** missile/drone factions; thin in raw attrition |
| ★ **Anatolian Alliance** (Turkey) | Cheap attritable drones + EW | *Bayrak* UCAV (mass, loiter, scout-strike) | Drone Command & EW Post (cheapens/extends drones) | Cheap air harassment + map vision; folds to splash AA |
| ★ **Peninsula Republic** (S. Korea) | Sensor-to-shooter C4I + artillery | *Cheondung K9* networked howitzer (fires faster when spotted) | C4I Fusion Center (shortens the kill chain) | Out-scouts and out-ranges; fragile if closed-on |
| **Rhine Compact** (Germany) | Engineering + armor durability | *Löwe* MBT (most durable; self-repairs stationary) | Field Engineering Depot (forward repair/bridging) | Resilient positional warfare + veterancy; slow, top-attack-vulnerable |
| **Gallic Republic** (France) | Expeditionary precision + deterrent | *Mistral* multirole jet (swaps loadouts on the fly) | Strategic Deterrence Silo (slows enemy super-power charge) | Adaptable; never strongest in one domain |
| **Isles Coalition** (UK) | Naval-expeditionary + intel | *Pathfinder SAS* cloaked recon team | Maritime Operations Centre (reveals enemy support-power launches) | Information asymmetry + raiding; thin in attrition |
| **Subcontinent Federation** (India) | Layered shielded arsenal + numbers | *Garuda* supersonic cruise-missile launcher | Layered Air-Defense Command (S-400/Akash umbrella) | Shielded long-range punch + numbers |
| **Eastern Maritime Pact** (Japan) | Naval Aegis defense + robotics | *Amatsu* Aegis BMD destroyer | Autonomous Systems Lab (free robot escorts) | Defensive tech-superiority from the sea |
| **Persian Resistance** (Iran) | Asymmetric swarm + denial | *Shahed* loitering-munition salvos | Dispersed "Mosaic" production (redundant, hard to decapitate) | Cost-imposition & saturation; loses any quality fight |

**Sensitivity stance:** fictional callsigns over real designations; all factions framed as *defensive doctrines*, no real wars/atrocities/leaders/insignia; nuclear/strategic weapons abstracted as telegraphed *deterrence* powers; Codex teaches real engineering/doctrine facts educationally; ship a toggle for fully-abstract sci-fi-bloc names for sensitive markets/esports.

---

## 6. The Codex (real ores + real tech as just-in-time onboarding)

The Codex extends the existing Encyclopedia widget into both the strategic reference *and* a diegetic teacher: the card pops the first time you mine a deposit or unlock a unit that needs it — turning real-world lore into onboarding for the new systems instead of required homework. Each entry: an AI-generated hero plate (the single-angle case where GPT-image excels — see §7), a stat block, and cross-links.

**Real strategic resources and the facts they teach:**

| Resource | In-game role | Codex hook (real fact) |
|---|---|---|
| **Petroleum** | Abundant credit-feeder #1 | ~34% of seaborne crude and ~20% of LNG squeeze through the Strait of Hormuz — a waterway ~33 km wide at its narrowest |
| **Iron / Steel** | Abundant credit-feeder #2 | Steel is the most-recycled material on Earth by tonnage — a modern tank is largely re-smelted scrap |
| **Rare Earths** | Stockpile → drone/EW/electronics doctrine | China refines >85% of the world's rare earths and ~90% of high-performance magnets — the magnets inside Western jets, subs, and guided missiles |
| **Uranium** | Stockpile → nuclear doctrine + power | Kazakhstan mines ~40% of uranium, but Russia controls ~38% of conversion and ~46% of enrichment — rich nations still depend on the supply chain |
| **Lithium** | Stockpile → directed-energy / mobile-power | Australia + Chile supply >half the world's lithium, yet the market is projected ~20% in deficit by 2030 |
| **Food & Water** | Soft upkeep meter | Fertilizer is so strategic it's sanctioned like a weapon — the US left Russian fertilizer duty-free even while tariffing |
| **Silicon** (codex-only) | Lore behind the Fabrication Plant | Taiwan makes >60% of all chips and >90% of the most advanced; TSMC alone holds ~64–70% of the foundry market |
| **Titanium** (codex-only) | Lore behind the Aerospace Works | Russia + China supply ~¾ of the world's titanium metal — China's share leapt from ~40% (2019) to >75% (2025) |

---

## 7. Visual & art direction — *"HD-Pixel diorama"* (and the pipeline, proven)

**Look:** between the C&C Remastered / They Are Billions / AoE-DE pole (clean 3D-rendered-to-2D sprites = *the de-blur*) and a restrained Octopath HD-2D post layer (= *the modern revamp*). Governing rule from the C&C Remaster postmortem: **at fixed top-down RTS zoom, more realism reads worse** — silhouette-first, chunky bold forms, every unit ID-able as a flat black silhouette at 100% zoom.

**The pipeline split is non-negotiable** — and I have already proven the cheap half this session:

- **(A) In-world rotating unit bodies (32 facings) → Blender 3D-turntable only.** Model → orthographic turntable (11.25° steps at OpenRA's ~30–35° pitch) → downscale → indexed palette + remap-mask pass = rotationally *perfect* facings by construction (exactly how Westwood and the Remaster did it). AI generation is **forbidden** here — rotational inconsistency (silhouette flips, palette drift) is its documented #1 failure. *The engine already ships the plumbing:* a real-time 3D-model renderer (`RenderVoxels.cs`, used by the TS mod), an RGBA-PNG sprite path (`PngSheetLoader.cs`), and a shipping `TiberianDawnHD` mod proving runtime classic/HD toggling. This is an **art effort, not an engine rewrite.**
- **(B) Everything single-angle → GPT-image.** Concept art, sidebar cameos, faction icons, loading/menu art, and Codex hero plates. **✅ Proven end-to-end this session** with `gpt-image-2`: generate on a flat keyable background (gpt-image-2 doesn't support transparency) → chroma-key → autocrop → downscale → clean transparent cameo. Reusable scripts: [generate_image.py](design/art/generate_image.py), [postprocess_cameo.py](design/art/postprocess_cameo.py).

**Concept proofs generated (illustrative, not committed production):**
- [unit_aril_cameo128.png](design/art/unit_aril_cameo128.png) — *Aril* APS tank cameo (Levant Shield)
- [proof_drone_cameo128.png](design/art/proof_drone_cameo128.png) — UCAV cameo (Anatolian Alliance / Persian Resistance)
- [codex_rare_earths.png](design/art/codex_rare_earths.png) — Rare Earths Codex hero plate

**UI:** "Clean military HUD" — flatter panels, crisp vector icons, faction accent colors — **but keep the command-sidebar layout intact** (the ladder player's reflexes are sacred). Progressive disclosure is the organizing principle: minute-one shows only the core RTS HUD; research/diplomacy/stockpile surfaces reveal as the match unlocks them.

---

## 8. The build path (serialize hard; let art set the schedule)

The roadmap is gated: **each phase is a kill-gate, not a milestone.** If a phase's core question fails, you stop having spent days, not years.

| Phase | Goal & core question | Art cost | Effort |
|---|---|---|---|
| **0 — Zero-Art Thesis Slice** | Stock RA + **one** change: capturable territory sectors pay income, *replacing* the harvester, with cuttable supply lines. **Does holding-and-cutting ground beat mass-and-A-move?** Instrument a new "camera-time-on-your-own-army-during-fights" metric. Test vs a **human** sparring partner (the bot gives false positives). *If this fails, the thesis is dead.* | **Zero** (recolored RA) | Days–2 wks |
| **1 — Determinism Spike + First Stockpile** | Standalone spike: two AI players toggling Ally/Enemy mid-match under sync-hash/replay validation (**go/no-go on live diplomacy** — if it can't be made desync-clean cheaply, cut it and keep static relationships). Add ONE stockpile (Rare Earths) gating an existing RA superweapon stand-in. | **Zero** | 3–5 wks |
| **2 — Three Blocs (YAML-only asymmetry)** | 3 flavor-neutral blocs (elite / swarm / denial) differing only in YAML + palette swap on a shared base set. Per-bloc bot profile that protects its enabler and screens its weakness. Post-match **loss-reason line** (seams teach, not punish). Balance gate: neither "greedy tech" nor "all-in rush" >55% across the 3×3. One reversible doctrine fork. | **Low** (skins) | 6–10 wks |
| **3 — Art Pipeline POC** | Push **one** unit fully through Blender → 32-facing → indexed → team-color, plus the engine "revamp" shader pass (soft shadows, contact AO, restrained bloom) that upgrades *all* existing art at once. **Measure wall-clock per unit** → multiply by the honest ~150–250-sequence base set = the real schedule. Decide with eyes open. | *This phase IS the art-cost discovery* | 2–4 wks |
| **4 — Bespoke art for proven factions** | Commission hero sprites **only** for blocs already proven fun. Real interactive tutorial mission. Event-triggered just-in-time Codex (the C# that doesn't exist yet). Optional real-country flavor skin. Expand toward 6 blocs only behind a green light that may never come. | **High** (dominant cost) | Open-ended |

**Must-fix before building anything** (the load-bearing risks): name & ship the zero-art slice as a kill-gate; serialize systems (forbid parallel development — live diplomacy comes *last*); run the determinism spike before designing diplomacy UI; build the just-in-time Codex as real engineering or stop calling onboarding solved; scope AI as a first-class deliverable per system *or* default the system off in skirmish; demote defense to "set-and-forget + one interrupt"; pick **one primary customer** (lean ranked 1v1 vs rich FFA hybrid) and balance for it; default to flavor-neutral fictional blocs.

---

## 9. Open design questions (the things only playtesting can answer)
1. **Time-boxed pact tuning** — too cheap/long → FFA freezes into one mega-bloc; too short → diplomacy is noise. Durations, alliance caps (1–2?), war-restlessness curve.
2. **Era-up timing** — *the* single most balance-sensitive number: too slow → "never tech, mass tier-1"; too cheap → runaway-leader. Needs cross-era hard counters (a Present-Day MANPADS still killing a Near-Future jet).
3. **Doctrine balance** — the irreversible fork is only fun if all four are viable per faction. Hard counter-relationship, or just numeric parity?
4. **How many stockpiles to ship** — design caps at 3, MVP ships 1. Is even 3 too much real-time cognitive load?
5. **Minor-faction rewards vs art ceiling** — the exciting reward (a free batch of a unique unit) needs art that won't exist at launch. Do phase-1 minors give only art-free payloads (credits, income %, tech, vision), and does that blunt the "race for the minors" fantasy?
6. **EMP / power-grid single point of failure** — tying the AD/EW net to power makes an EMP raid swingy. How much battery-backed graceful degradation before it's tension, not a coin-flip?
7. **Scope / solo-feasibility** — the realistic failure is *unfinished*, not *bad*. Is the right first ship even smaller than 6 factions?
8. **Ranked 1v1 vs the full hybrid** — diplomacy-off competitive vs diplomacy-on rich mode is two balance problems. Which is the primary customer?

---

*Provenance: §2–§7 synthesize a 9-dimension research corpus (game-design psychology, RTS×4X precedents, diplomacy, research/eras, defense, country roster, real-resource economy, visual direction, engine feasibility) validated against the real OpenRA repo. §1 and §8 are the executive synthesis of a 5-lens adversarial fun-validation panel. Concept art in `design/art/` was generated with gpt-image-2 as illustrative proof of the art pipeline, not committed production assets. Full machine-readable workflow output: `design/_wf_result.json`.*

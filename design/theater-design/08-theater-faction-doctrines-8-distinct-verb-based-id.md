# THEATER faction doctrines — 8 distinct verb-based identities (doctrine line, playstyle, 2-3 uniques on real RA bodies, faction passive VERB)

**Executable now:** True

**Summary:** Eight contemporary blocs get a doctrine each built around a VERB (a new thing to DO) rather than a NUMBER (a stat aura): a recon ping, a one-time relocate, build-from-forward, attritable swarm spam, an EW blind/jam, a sabotage option, etc. Every unique reuses an existing RA art body (MIG, YAK, TTNK, 3TNK, E6, SHOK, V2RL, MGG, MRJ, MH60, U2, ARTY, DD, APC, SS) so the data layer is buildable now; only cameos/reskins are deferred art. The match-layer passive is a light, non-persistent capability consistent with the validated "verbs not numbers, light at match layer" principle.

## Key decisions
- Keep the existing single wired unique per faction and fold it in as unique #1; add 1-2 more each, all on existing RA bodies (MIG/YAK/TTNK/3TNK/E6/SHOK/V2RL/MGG/MRJ/MH60/U2/SAM/APC/MECH) so the data layer builds today.
- Every faction passive is a VERB (timed recon ping, reusable orbiting scout, one-time relocate, surge-production toggle, build-from-forward outpost, mark-and-sabotage, reactive missile intercept, declared no-fly zone) — zero flat-stat auras, per the validated principle.
- Deliberately differentiate the four recon/vision verbs (instant ping vs killable orbiter vs soft scout+jam vs aircraft-only zone reveal) so no two factions feel the same.
- Split the strong economy/tempo verb (Continental Surge) from the strong map-control verb (Rhine Forward Works) across different factions to keep balance.
- Reuse the single per-faction tech.<faction> Construction-Yard token to gate ALL of a faction's uniques (multiple Buildable.Prerequisites resolve off one token) — no new prerequisite plumbing needed.
- Ship Federation Overwatch Ping first as the lowest-risk verb (near-stock timed-reveal support power) to validate the match-layer thesis before building heavier verbs.
- Mark which uniques are stock-body-reuse vs needs-art (new cameos/reskins) so the art pipeline backlog is explicit but never blocks the data work.

## Files to touch
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-units.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/rules/theater-factions.yaml
- /Users/ashishnaik/Projects/openra/mods/ra/fluent/theater.ftl

## Risks
- The 2nd/3rd unique units are executable-now as YAML, but the faction PASSIVES are mostly new traits/support-powers — that is real C# work, not data, and anything reaching world.Tick() (relocate, intercept, surge, loiter timers) must follow determinism rules (no float/Random/DateTime, fixed-point WPos/CPos, synced timers) or it desyncs in multiplayer.
- Some bodies are double-booked: YAK is reused by Continental (Wing Loong), Anatolian (Bayrak + Kargu), Eastern (Hayabusa), and Subcontinent (Tejas), and V2RL by three factions — without distinct reskins/cameos these will look identical in-game, weakening the 'distinct identity' goal until art lands.
- Verb-passive balance is unvalidated on the GUI fun-gate: e.g. Continental Surge (cheaper+faster but fragile) and Rhine Forward Works (forward production) could be over/undertuned; needs a playtest like Phase 0, which can't be done headlessly.
- Federation Overwatch Ping and Subcontinent No-Fly must not trivialize stealth/EW (Isles/Anatolian gap-gen, Bayrak) or they flatten the counter-web; the ping must respect generated shroud.
- Adding many new Buildable entries can trip lint/BuildPaletteOrder collisions and faction-gating lint rules — must run ./utility.sh ra --check-yaml after each batch (the local make check is RED for unrelated IDE0055 reasons, so rely on --check-yaml for YAML validation).
- Real-country framing (No-Fly over India, drone swarms for China/Turkey, EW for Russia) is doctrinal not crude, but naming uniques after real systems (Akash, Tejas, Bayraktar/Bayrak, Kargu, Koral, Grad) risks trademark/sensitivity concerns — consider lightly fictionalized names before any public release.

## Design

## THEATER — 8 Faction Doctrines (verbs, not numbers)

Design rules I held to:
- **Each passive is a VERB** — it grants a new action/option/mode, never a flat `+X%` aura.
- **Every unit reuses an existing RA body** (so it builds today); a `needs-art` tag marks where a later cameo/reskin lifts it above "stock unit in a hat."
- **Roughly balanced**: each faction gets exactly one "strong" verb plus 2-3 uniques; no faction gets both the best economy verb and the best army verb.
- The current single unique per faction is KEPT (it's already wired) and folded in as unique #1; I add 1-2 more each.
- Real-country flavor is doctrinal (how the army fights), not caricature.

Side note: today's `tech.<faction>` prerequisite (one per Construction Yard) gates ALL of a faction's uniques fine — multiple `Buildable.Prerequisites: tech.<faction>, ...` entries all resolve off the same token. So adding 2-3 uniques per faction needs no new prerequisite plumbing.

### Summary table

| Faction (country / side) | Doctrine line | Playstyle | Uniques (RA body) | Faction VERB passive |
|---|---|---|---|---|
| **Federation** (USA / Allies) | "See first, strike from nowhere." | Air-centric tempo; pay for information & precision | Specter stealth jet (MIG)*; Sentinel AWACS recon plane (U2); Reaper gunship (MH60) | **Overwatch Ping** — target any explored cell; reveal it for a few seconds on a cooldown (a recon verb, no permanent vision) |
| **Northern Union** (Russia / Soviet) | "Mass fires, then move the mass." | Heavy armor + artillery; reposition the deathball | Bastion EW command tank (TTNK)*; Grad rocket battery (V2RL, salvo); Redoubt mobile bunker (APC, deploys to garrison) | **Echelon Relocate** — one-time: teleport-march your selected ground group a short hop toward an explored point (a relocate verb, long cooldown) |
| **Continental Bloc** (China / Soviet) | "Attrition is a resource we print." | Cheap expendable spam; numbers as a verb | Wing Loong swarm drone (YAK)*; Type-charge sapper (E1-ish, demo body); Dazhbog rocket truck (V2RL, cheap) | **Surge Production** — toggle a build mode: units cost less & build faster but arrive at lower HP (a tempo-vs-fragility choice, not a passive discount) |
| **Rhine Compact** (Germany / Allies) | "We build forward and we don't break." | Engineering/expansion; project the base outward | Loewe heavy MBT (3TNK)*; Pioneer combat-engineer (MECH/engineer body); Bergepanzer recovery vehicle (tows/repairs husks) | **Forward Works** — build a deployable Forward Outpost that acts as a secondary production exit / rally near the front (a build-from-forward verb) |
| **Isles Coalition** (UK / Allies) | "The op is won before the shooting." | Intel + special forces; sabotage & deny | Pathfinder infiltrator team (E6)*; Spectre recon drone (U2-light, soft scout); Comms cell (MRJ, jam screen) | **Black Op** — mark an enemy structure; a free Pathfinder infiltrates over time to sabotage it once (a sabotage verb on cooldown) |
| **Eastern Maritime Pact** (Japan / Allies) | "Autonomy holds the line." | Defensive robotics + missile-defense; counter-punch | Kunai combat robot (SHOK)*; Aegis point-defense turret (Pillbox/SAM body, shoots down incoming); Hayabusa interceptor drone (YAK, anti-air) | **Active Intercept** — toggle: nearby defenses gain a chance to shoot down one incoming missile/shell on a cooldown (a reactive-defense verb, not flat armor) |
| **Subcontinent Federation** (India / Soviet) | "Layered skies, deep reach." | Layered air-defense + long-range strike | Garuda cruise-missile launcher (V2RL)*; Akash SAM battery (SAM/AA body); Tejas strike fighter (MIG/YAK, multi-role) | **No-Fly Declaration** — designate a zone; enemy aircraft entering it are auto-revealed & flagged for your AA (an area-denial verb, not a damage buff) |
| **Anatolian Alliance** (Turkey / Soviet) | "Cheap eyes, cheap teeth, everywhere." | Attritable drone harassment + EW | Bayrak armed loiter drone (YAK)*; Kargu kamikaze micro-drone (cheap one-shot, demo/aircraft body); Koral EW van (MRJ/MGG, blind enemy radar) | **Loiter Watch** — deploy a cheap reusable recon drone that orbits a point and feeds vision until shot down (a persistent-but-killable scouting verb) |

\* = the unit already wired in `theater-units.yaml`.

---

### Per-faction detail

#### 1. Federation (USA, Allies) — "See first, strike from nowhere."
**Playstyle:** Information-and-airpower tempo. You pay a premium to always know where the enemy is and to hit precise targets before they react. Weak in a straight armor brawl; strong at picking the fight.
- **Specter Stealth Fighter** — `Inherits: MIG` (already wired). Stealthed strike jet.
- **Sentinel AWACS** — `Inherits: U2` body (the recon plane). A buildable orbiting recon plane rather than a one-shot support power; reuses the existing U2 fly-over art.
- **Reaper Gunship** — `Inherits: MH60` (chaingun helicopter). Persistent CAS to complement the jet.
- **VERB passive — Overwatch Ping:** a low-cooldown order that reveals any *already-explored* cell for ~5 seconds. It's a recon verb (a thing you DO with timing), not permanent map vision. Implement as a player-scoped support power granting a temporary `RevealsShroud`-style reveal at the target cell. Does NOT see through gap-generators (keeps Isles/Anatolian EW relevant).
*Needs art later:* Specter/Sentinel/Reaper cameos; bodies are reused as-is.

#### 2. Northern Union (Russia, Soviet) — "Mass fires, then move the mass."
**Playstyle:** Build the biggest combined armor+artillery ball, then use the relocate verb to land it where the enemy isn't ready. Slow to start, brutal mid-game.
- **Bastion EW Command Tank** — `Inherits: TTNK` (already wired).
- **Grad Rocket Battery** — `Inherits: V2RL`, tuned as a salvo/area artillery (multiple rockets, shorter range than Garuda). Reuses V2 launcher art.
- **Redoubt Mobile Bunker** — `Inherits: APC` that can deploy into a static garrison (passengers fire out). Reuses APC body + a deploy condition.
- **VERB passive — Echelon Relocate:** select a ground group; once per long cooldown, "echelon-march" them a short hop toward an explored point (a brief uncontrollable move-order to that point, not a literal chrono-teleport — keeps it grounded). It's the reposition-the-deathball verb. Determinism: drive it as a queued Move order to a fixed CPos, no float.
*Needs art later:* Grad multi-rocket reskin; Redoubt deploy frames (can ship with stock APC art first).

#### 3. Continental Bloc (China, Soviet) — "Attrition is a resource we print."
**Playstyle:** Overwhelm with cheap, individually-weak units produced faster than the enemy can kill them. The verb makes spam a deliberate gear you shift into, with a real downside.
- **Wing Loong Swarm Drone** — `Inherits: YAK` (already wired), cheap mass air.
- **Type-Charge Sapper** — cheap infantry with a demolition charge (reuse an existing demo/grenadier body, e.g. E1/E2 + a satchel armament).
- **Dazhbog Rocket Truck** — `Inherits: V2RL` but cheaper/faster-building, lower HP — the "printed artillery."
- **VERB passive — Surge Production:** a toggle (player order) that puts production into surge mode: build cost down / build time down, but units exit at reduced max HP while the toggle is on. It's a tempo-vs-fragility *choice* you actively flip, not a silent discount. Implement as a player condition that production/Valued/Health hooks read.
*Needs art later:* drone-swarm cameo, sapper reskin. Bodies reused.

#### 4. Rhine Compact (Germany, Allies) — "We build forward and we don't break."
**Playstyle:** Expansion and durability. You creep your production envelope toward the enemy and grind with hard-to-kill armor. Best map-control verb in the set.
- **Loewe Heavy MBT** — `Inherits: 3TNK` (already wired), most durable tank.
- **Pioneer Combat-Engineer** — `Inherits: MECH`/engineer body; can capture + field-repair vehicles (reuses mechanic art).
- **Bergepanzer Recovery Vehicle** — tows/auto-repairs friendly husks and damaged armor near it (reuse a vehicle body + repair aura limited to *adjacent* units, framed as a verb: "recover this wreck").
- **VERB passive — Forward Works:** unlock a deployable **Forward Outpost** structure that serves as a secondary rally / production exit near the front (vehicles can be ordered to spawn-walk from there). It's the build-from-forward verb. Reuse an existing small-structure body for the outpost.
*Needs art later:* Forward Outpost building art (can prototype on a stock pillbox/silo body); Bergepanzer reskin.

#### 5. Isles Coalition (UK, Allies) — "The op is won before the shooting."
**Playstyle:** Intel and special forces. You scout, sabotage, and deny rather than out-muscle. High skill ceiling; punishes inattentive opponents.
- **Pathfinder Infiltrator Team** — `Inherits: E6` (already wired).
- **Spectre Recon Drone** — soft, unarmed scout (reuse U2-light or a cheap aircraft) for cheap persistent vision.
- **Comms Cell** — `Inherits: MRJ` (mobile radar jammer): a verb-y EW unit that blinds enemy radar/minimap in an area.
- **VERB passive — Black Op:** mark an enemy structure; over a timer a free Pathfinder spawns and infiltrates it once to sabotage (disable production briefly / steal vision / reset a support-power timer). It's a sabotage verb on a long cooldown — a thing you spend, not a stat.
*Needs art later:* Comms Cell + Spectre cameos. MRJ/E6/U2 bodies reused.

#### 6. Eastern Maritime Pact (Japan, Allies) — "Autonomy holds the line."
**Playstyle:** Turtling robotics + missile defense, then mechanized counter-punch. Strong defensively; the verb is reactive, so it rewards good positioning over raw stats.
- **Kunai Combat Robot** — `Inherits: SHOK` (already wired).
- **Aegis Point-Defense Turret** — defensive structure (reuse SAM/pillbox body) that can shoot down incoming projectiles in range.
- **Hayabusa Interceptor Drone** — `Inherits: YAK` retuned anti-air, to screen against enemy air.
- **VERB passive — Active Intercept:** a toggle that gives nearby defensive structures a *chance to shoot down one incoming missile/shell* on a per-defense cooldown. It's a reactive-defense verb (interception attempts), not flat armor. Implement via a player condition that defenses read to enable a `point-defense` armament/behavior.
*Needs art later:* Aegis turret + robot/interceptor cameos. SHOK/YAK/SAM bodies reused.

#### 7. Subcontinent Federation (India, Soviet) — "Layered skies, deep reach."
**Playstyle:** Air-denial + long-range strike. You own the air column over your territory and reach deep with cruise missiles. Vulnerable to ground rushes early.
- **Garuda Cruise-Missile Launcher** — `Inherits: V2RL` (already wired), longest-range strike.
- **Akash SAM Battery** — strong AA structure/vehicle (reuse SAM body) — the floor of the layered umbrella.
- **Tejas Strike Fighter** — `Inherits: MIG`/`YAK` multi-role fighter — the upper layer.
- **VERB passive — No-Fly Declaration:** designate a zone (cooldown); enemy aircraft that enter it are auto-revealed and flagged as priority targets for your AA (your SAMs/AA auto-engage them with priority). It's an area-denial verb (a declared zone you DO), not an AA damage buff. Implement as a temporary player-scoped reveal+target-priority field over a region.
*Needs art later:* Akash/Tejas cameos. V2RL/SAM/MIG bodies reused.

#### 8. Anatolian Alliance (Turkey, Soviet) — "Cheap eyes, cheap teeth, everywhere."
**Playstyle:** Attritable drone harassment + electronic warfare. You blanket the map in cheap eyes and chip the enemy, blinding their radar at the seams. Low per-unit value, high map presence.
- **Bayrak Armed Loiter Drone** — `Inherits: YAK` (already wired).
- **Kargu Kamikaze Micro-Drone** — very cheap one-shot suicide drone (reuse a light aircraft body + demolition warhead on contact).
- **Koral EW Van** — `Inherits: MRJ`/`MGG`: blinds enemy radar / generates shroud — the EW verb on wheels.
- **VERB passive — Loiter Watch:** deploy a cheap, *reusable but killable* recon drone that orbits a chosen point and feeds vision until shot down. It's a persistent-scouting verb with a counter (the enemy can kill it), distinct from Federation's instant ping and Isles' soft scout. Implement as a buildable/support-spawned circling actor with `RevealsShroud`.
*Needs art later:* Kargu + Koral cameos. YAK/MRJ/MGG bodies reused.

---

### How the 8 verbs stay distinct (no overlap)
- **Recon/vision verbs** are deliberately differentiated: Federation = *instant timed ping anywhere explored*; Anatolian = *persistent killable orbiting drone*; Isles = *cheap soft scout unit + radar-jam*; Subcontinent = *reveal only enemy aircraft in a declared zone*. Four different shapes of "see," no two identical.
- **Economy/tempo verb** (Continental Surge) and **map-control verb** (Rhine Forward Works) are split across different factions so no one owns both.
- **Reposition verb** (Northern Echelon) vs **build-forward verb** (Rhine) are distinct: one moves existing units, one extends production.
- **Defensive interception** (Eastern) vs **air-denial declaration** (Subcontinent) — one is reactive on your structures, one projects onto an enemy-occupied zone.
- **Sabotage** (Isles Black Op) is the only "reach into the enemy base" verb.

### Implementation sketch (data layer, buildable today)
Adding the extra uniques is pure YAML, mirroring the existing entries. Example for two new Federation uniques in `theater-units.yaml`:
```yaml
SENTINEL:
  Inherits: U2
  RenderSprites:
    Image: u2
  Tooltip:
    Name: actor-sentinel.name
  Buildable:
    Queue: Aircraft
    BuildPaletteOrder: 93
    Prerequisites: tech.federation, afld

REAPER:
  Inherits: MH60
  RenderSprites:
    Image: mh60
  Tooltip:
    Name: actor-reaper.name
  Buildable:
    Queue: Aircraft
    BuildAtProductionType: Helicopter
    BuildPaletteOrder: 94
    Prerequisites: tech.federation, hpad
```
And the matching `theater.ftl` stanza:
```ftl
actor-sentinel =
    .name = Sentinel AWACS
actor-reaper =
    .name = Reaper Gunship
```
The faction **passives are the real engineering work** — most are new player-scoped support powers / toggleable conditions (each its own small trait or a reuse of existing support-power + reveal/condition traits). They are listed here as design specs; only the *unit/building* additions are "executable now." All passive logic that touches `world.Tick()` must obey the determinism rules (no float/Random/DateTime; fixed-point WPos/CPos; sync the timers).

### Recommended build order
1. **Now (data):** add the 2nd/3rd uniques per faction to `theater-units.yaml` + `theater.ftl`; verify with `./utility.sh ra --check-yaml`. Update faction blurbs in `theater.ftl` to one-line doctrines.
2. **Next (1 cheap verb to validate the thesis):** ship Federation **Overwatch Ping** first — it's a near-stock support power (timed reveal), low risk, and immediately demonstrates "verbs not numbers" at the match layer.
3. **Then:** the relocate / surge / forward-works / sabotage verbs, each as its own trait, one faction at a time.
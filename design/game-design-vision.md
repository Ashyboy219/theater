# OpenRA — Seeing the Whole Game & Where It Can Go
### A design & lore brainstorming document (non-technical)

> Purpose: help you *see* OpenRA as a player and a designer — its worlds, its factions, the fantasies it sells, the loops underneath — and then map the spaces where we could add to it in ways that stay true to each world. Every section ends with **open questions** meant to spark your own thinking, and the final section collects the big strategic choices that will steer everything else.

---

## Part 0 — What OpenRA actually *is* (the canvas)

OpenRA is not one game. It's a **single RTS engine hosting several reimagined Westwood worlds**, plus the tools for the community to build more. That framing matters more than any single unit, because it tells you what *kind* of additions make sense.

Three things define the canvas:

1. **It's a reimagining, not a port.** OpenRA already *invents* within these worlds — country sub-factions that never existed in the originals, giant-ant missions, rebalanced rosters, modern controls. So "lore-aligned" here does **not** mean "licensed canon." It means *thematically true to the world's internal logic*. That's a creative license we should use deliberately.
2. **It lives in two audiences at once.** A serious **competitive multiplayer** scene (ranked ladders, balance discipline, the "right click to attack" muscle memory) **and** a **single-player / co-op / modding** crowd (campaigns, custom maps, total conversions). Almost every idea below lands differently for these two — naming which one you're serving is half the design decision.
3. **Content is gated by art, not ideas.** The hardest constraint on "add a faction / add a unit" is **2D sprite art** — every unit needs hand-made directional sprites, animations, icons, and audio. Systems and modes are comparatively cheap; new *things on screen* are expensive. Keep this in the back of your mind for every idea — it's the real cost curve.

**The worlds currently on the canvas:**

| Mod | World | State | One-line fantasy |
|---|---|---|---|
| **Red Alert** (`ra`) | Alt-history Cold War with Tesla coils & time travel | Mature, flagship | "What if WWII never ended and Einstein erased Hitler?" |
| **Tiberian Dawn** (`cnc`) | Near-future war over an alien mineral | Mature | "A cult and a coalition fight over a substance that's eating the Earth." |
| **Dune 2000** (`d2k`) | Feudal houses warring over a desert spice planet | Mature | "Three noble houses strip-mine a living desert that bites back." |
| **Tiberian Sun** (`ts`) | The Tiberium world a generation later, mutated | **Experimental** (1 mission, roster exists) | "The alien plague won; now we fight in mech walkers over a poisoned planet." |

> **Open questions (canvas level)**
> - Are we trying to **deepen a world that already works** (RA, cnc, d2k), **finish the half-built one** (ts), or **start a new one**?
> - Who is the primary customer of whatever we build — the **ladder player**, the **campaign/co-op player**, or the **modder**?
> - Do we want to stay **faithful** to each world's tone, or lean into OpenRA's existing **pulpy "what-if" license** (the giant ants energy)?

---

## Part 1 — The design DNA shared by every world

Before the worlds, the *grammar* they share. Every OpenRA game is built from the same five loops. Understanding these is how you tell whether a new idea will "feel like OpenRA."

**1. The economic loop — harvest under threat.**
A harvester drives into a contested resource field, fills up (and *slows down* while full — a built-in risk window), drives home to a refinery, converts to credits. The resource is always *out there*, in the open, often between you and the enemy. This single loop is the engine of the whole game's tension: **every fight is ultimately about who controls the fields.** Ore/Gems (RA), Tiberium (cnc/ts), Spice (d2k) are skins on the same nerve.

**2. The production loop — base, power, tech tree.**
Construction Yard → power plants → production buildings → tech buildings that unlock better things. **Power is a soft economy**: starve it and everything slows. The tech tree is a *commitment* device — you spend now to unlock a payoff later, and the enemy can scout and punish the gap.

**3. The combat loop — rock-paper-scissors with positioning.**
Units are deliberately *specialists* ("Strong vs Infantry, Weak vs Aircraft"). No unit is good at everything, so armies are *compositions*, and the meta-game is reading the enemy's composition and counter-building. Fog of war keeps that read imperfect — **scouting is a resource.**

**4. The support-power loop — the timed comeback.**
Superweapons and support powers (Chronosphere, Iron Curtain, Ion Cannon, nukes, paratroopers, spy planes) charge on a clock. They're rare, dramatic, and *telegraphed* (beacons warn the target). They exist to **break stalemates and create comeback moments** — a losing player's Iron Curtain push, a winning player's Ion Cannon snipe. They are the game's punctuation marks.

**5. The progression loop — veterancy.**
Units that survive and kill gain rank: more damage, more speed, less damage taken, eventually self-healing. A surviving veteran is *disproportionately* valuable. This quietly rewards micro and creates "ace unit" stories — the Mammoth Tank that's been alive all game and won't die.

**Plus the meta-layer OpenRA added on top of the originals:** attack-move and rally waypoints, a modern lobby + online server browser, replays and saves, full **Lua mission scripting**, an **in-engine map editor**, configurable lobby options (tech level, crates, bounty, time limit), and the community **Resource Center** for sharing maps. This meta-layer is itself a design surface — some of the best opportunities below are *not* new units at all.

> **Open questions (DNA level)**
> - Which of these five loops is the most **under-explored** today? (My instinct: the **economic loop** and the **living-world/PvE** space are the least developed.)
> - Is there room for a **sixth loop** the originals never had — e.g. a *map-control / territory* layer, a *trade/diplomacy* layer (the Dune Starport hints at it), or a *persistent commander* layer across matches?
> - When we add something, which loop is it *plugged into*? An idea that doesn't touch one of these usually feels bolted-on.

---

## Part 2 — The worlds, one at a time

Each world is profiled the same way: **the setting**, **the factions and their fantasy**, **the signature toys**, **the core tension**, **the story**, and **where it wants to grow**.

---

### 2A. Red Alert — the pulpy Cold War "what-if"

**The setting.** Einstein travels back and erases Hitler; without WWII, Stalin's USSR swallows Europe instead, and an Allied coalition fights back. The result is a Cold War gone *hot*, with real-WWII hardware sharing the field with **Tesla coils, time machines, and invulnerability fields.** The tone is knowingly pulpy — FMV-melodrama serious on the surface, gleefully absurd underneath (giant irradiated ants are canon).

**The factions & their fantasy.** Two sides, each split into **country sub-factions** with a signature toy — this sub-faction system is OpenRA's own invention and is the single most "ready to extend" structure in the whole game:

| Side | Fantasy | Sub-factions (current) |
|---|---|---|
| **Allies** | Precision, tech, mobility, **naval & air superiority**, espionage. The clever, surgical side. | **England** (counter-intel, Spy), **France** (deception, fake buildings, Phase Transport), **Germany** (Chronoshift mastery, Chrono Tank) |
| **Soviets** | Mass, armor, brute force, **overwhelming firepower**, unconventional terror weapons. The heavy, blunt side. | **Russia** (Tesla weapons, Tesla Tank, Shock Trooper), **Ukraine** (demolitions, Parabombs, Demo Truck) |

**Signature toys (the things players remember):**
- **Tanya** — one-woman army commando, dual pistols + C4, *max one on the field.* The hero fantasy.
- **Mammoth Tank** — the slow apex predator that beats everything and self-repairs.
- **Tesla Coil / Tesla Tank** — arcing electric death; the Soviet identity in one weapon.
- **Chronosphere** (teleport your army across the map) vs **Iron Curtain** (make your army briefly invulnerable) — the two signature superweapons, and a *perfect* asymmetry: Allied trickery vs Soviet unstoppability.
- **Spy / Thief / Engineer** — the infiltration toolkit: steal intel, steal cash, steal buildings.
- **The whole naval theater** — destroyers, cruisers, subs. RA is the most *naval* of the worlds.

**Core tension.** Allied *finesse* (out-position, out-tech, out-scout, control the sea) vs Soviet *pressure* (out-mass, out-armor, just keep coming). The Chrono/Iron-Curtain pairing is the thematic heart: outsmart vs overpower.

**The story.** Two ~12-mission campaigns (Allied & Soviet) plus expansion arcs, with recurring characters — **Tanya, Einstein, Stavros** (the Greek VIP escort), **Volkov & Chitzkoi** (the Soviet cyborg soldier and cyborg dog). And the **"It Came From Red Alert"** giant-ant missions — the canon proof that this world tolerates the weird.

**Where Red Alert wants to grow:**
- **Finish the sub-faction map.** The England/France/Germany // Russia/Ukraine pattern is begging for completion — lore supports **Greece, Spain, Turkey, Poland** (Allied) and more Soviet republics. Each is "one signature unit + one signature ability," so the design template already exists. *This is the lowest-risk, most obviously lore-aligned RA expansion.*
- **Lean into time/chrono as a theme, not just a button.** Chrono is currently one teleport power. A whole **temporal mechanics** space is unexplored — chrono-vortex hazards, units that phase, a "what-if timeline" co-op mode where history glitches.
- **Promote the weird-science thread to a real third force.** The ants hint at a PvE / monster faction: irradiated wildlife, escaped experiments — a co-op "survive the swarm" pillar that's *already canon*.
- **A hero/commander layer.** Tanya, Volkov, Stavros are beloved. A mode or campaign built around **named heroes with abilities** leans into RA's character-forward, melodramatic identity.

> **Open questions (Red Alert)**
> - If we complete the sub-factions, **which nations, and what's each one's single signature toy?** (The constraint is: one memorable idea each, balanced against the existing five.)
> - Is RA's future **more competitive depth** (sub-factions, balance) or **more spectacle** (heroes, monsters, time-travel set-pieces)?
> - The naval theater is underused on most ladder maps — is there a **naval-forward mode or map pool** worth designing?

---

### 2B. Tiberian Dawn (& Sun) — the alien plague world

**The setting.** A meteor brings **Tiberium**, a crystalline alien substance that leaches minerals from the soil (making it the perfect resource) while **poisoning everything it touches** and *mutating life into monsters.* Two forces fight over it: **GDI**, a UN military coalition trying to contain it, and the **Brotherhood of Nod**, a charismatic doomsday cult led by the messianic **Kane** who believe Tiberium is humanity's next evolutionary step. By **Tiberian Sun**, a generation later, the plague has *won* — the planet is mutating, the war is fought in mech walkers, and a third population (the mutated **Forgotten**) lives in the wastes.

**The factions & their fantasy:**

| Faction | Fantasy | Doctrine in units |
|---|---|---|
| **GDI** | Conventional superpower: **expensive, heavy, durable, high-tech**, orbital firepower. The hammer. | Mammoth Tank, Medium Tank, Orca gunship, the **Ion Cannon** (orbital strike), Advanced Guard Tower |
| **Nod** | Cult guerrillas: **cheap, fast, stealthy, fragile**, chemical & psychological warfare. The knife in the dark. | Stealth Tank (invisible), Recon Bike, Flame Tank, Chem Warrior, the **Obelisk of Light** (laser), the Nuclear Strike, the **Temple of Nod** |

**Signature toys:**
- **The Ion Cannon vs the Nuclear Strike** — GDI's clean orbital scalpel vs Nod's dirty mushroom cloud. Same asymmetry as RA's Chrono/Curtain: precision vs devastation.
- **Stealth Tank** — invisible until it fires; the purest expression of Nod's identity.
- **Obelisk of Light** — a single laser that deletes whatever it hits; Nod's "don't walk here" statement.
- **Mammoth Tank** — again the apex, self-repairing, anti-everything.
- **The Commando** — sniper + C4, GDI's hero unit.

**The unique ingredient: Tiberium as a *living world*.** This is what sets the Tiberium universe apart from every other RTS resource. Tiberium isn't inert ore — in the fiction (and partly in-game) it **spreads, grows from blossom trees, kills infantry that walk on it, and mutates the dead into Visceroids** (Tiberium monsters). By Tiberian Sun there are whole **Tiberium ecologies** — fiends, floaters, veins, veinholes, ion storms. **The map itself is a slow third player.** This is, to me, the single most exciting under-exploited idea in all of OpenRA.

**Core tension.** GDI's *methodical, expensive overwhelming force* vs Nod's *cheap, sneaky, ambush-and-vanish.* Plus the environmental tension of fighting *on top of* a substance that's hazardous to your own troops.

**The story.** Full GDI and Nod campaigns; **Kane** as one of gaming's great villains looming over Nod; the world-question of whether Tiberium is a plague to contain (GDI) or an ascension to embrace (Nod).

**Where the Tiberium world wants to grow:**
- **Make Tiberium a *dynamic actor*, not just a resource pile.** A system where Tiberium **spreads across the map over a match**, reshaping terrain, denying ground, spawning Visceroids in contested fields. It would make *map control* a living thing and create a tension no other RTS has. Plugs straight into the economic + combat loops.
- **Finish Tiberian Sun.** It has a roster (Titans, Wolverines, Cyborgs, subterranean APCs, the Mammoth Mk II) but **one mission and no real campaign.** This is the biggest single "complete the half-built thing" opportunity — a future-war Tiberium game largely already modeled.
- **The Forgotten / mutants as a third faction** — *canonical* in Tiberian Sun. Tiberium-mutated humans who live in the wastes, immune to the substance, with scavenged tech. A natural asymmetric third side and a PvE population.
- **Deepen Nod's asymmetry.** Subterranean warfare, stealth fields, cult "support powers" (fanaticism, suicide units) — push Nod further from a GDI mirror. Asymmetry is where this world's identity lives.

> **Open questions (Tiberium)**
> - How far do we take **"living Tiberium"**? A subtle version (slow spread, occasional Visceroid) is a flavor system; an aggressive version (the map is actively being consumed) is a *whole new game mode*. Which?
> - **Finish TS** as a faithful future-war RTS, or cherry-pick its best ideas (mechs, subterranean, mutants) **into the existing cnc** mod?
> - Should the **Forgotten** be a playable faction (big art cost) or a PvE/environmental force first (cheaper, and arguably more lore-true — they're survivors, not an army)?

---

### 2C. Dune 2000 — the desert that fights back

**The setting.** The desert planet **Arrakis**, only source of **Spice** — the most valuable substance in the universe. Three noble **Houses** wage proxy war to control spice production, under the eye of a distant Emperor and the trade cartel CHOAM. The land itself is lethal: the sun erodes your buildings, and **gigantic sandworms** (Shai-Hulud) roam the sand and *swallow anything that vibrates* — including your harvesters and your army.

**The factions & their fantasy:**

| House | Fantasy | Signature units |
|---|---|---|
| **Atreides** (noble, from the water world Caladan) | **Balanced & honorable**, air superiority, allied with the native Fremen. The "fair fight" house. | Sonic Tank, **Fremen** (cloaked native warriors), Ornithopter air strikes |
| **Harkonnen** (cruel, industrial) | **Brute force & atomics**, slow but devastating. The "overwhelm and annihilate" house. | **Devastator** (the heaviest tank on Dune, self-destructs), Sardaukar elite troopers, the **Death Hand** atomic missile |
| **Ordos** (greedy, secretive, mercenary) | **Speed, stealth, deception, forbidden tech.** The "win dirty" house. | **Deviator** (mind-controls enemy vehicles), Stealth Raider, Saboteur, mercenaries via the Starport |

**Signature toys:**
- **The Sandworm** — not a unit you build, but a roaming apex hazard that belongs to *no one*. Worms make Arrakis the only RTS map where **the terrain itself hunts you**, and they create a whole risk language: harvesters lure worms, the **Thumper** infantry can *bait* worms onto enemies.
- **The Deviator** — temporarily flips enemy vehicles to your side. Pure Ordos mischief.
- **The Devastator** — Harkonnen's slow doom-tank that can self-destruct in the enemy's face.
- **The Starport** — order pre-built units from the off-world CHOAM guild at *fluctuating market prices.* A second, *trade-based* economy layer no other OpenRA game has.
- **Concrete foundations** — build off-concrete and the desert *erodes your structures.* The environment is a cost.

**Core tension.** Three-way asymmetry (balanced / brute / sneaky) layered on top of a **hostile environment** — you're not just fighting the enemy, you're managing the worm, the erosion, and the spice fields' danger. Dune is the most *environmental* of the worlds.

**The story.** Three House campaigns, each with its own arc and tone, plus references to the Emperor, the Fremen, and CHOAM — a deep bench of lore factions standing just offstage.

**Where Dune wants to grow:**
- **More Houses & powers — the lore bench is enormous.** Frank Herbert's universe hands us **House Corrino** (the Emperor's terrifying Sardaukar legions), the **Fremen** (desert guerrillas who *ride the worms*), the **Spacing Guild**, **Bene Gesserit**, **Tleilaxu**, **Ix**, and the **Smugglers.** Each is a ready-made faction or minor power with a distinct fantasy. The Fremen "ride the sandworm into battle" fantasy alone is iconic and unbuilt.
- **The worm as a designed centerpiece, not a hazard.** Worm-baiting, worm-riding, a "summon the worm" Fremen power, a survival mode where worms escalate. The worm is Dune's equivalent of "living Tiberium" — a world-actor begging for systems.
- **Economic warfare via the Starport.** The CHOAM trade economy is a unique seed for a **market/economy-focused mode** — manipulate prices, embargo, smuggle. No other RTS has this.
- **Environment as the antagonist.** Sandstorms, erosion, spice blows, the day/night of the deep desert — a mode or campaign where *surviving Arrakis* is the objective.

> **Open questions (Dune)**
> - Of the lore Houses/powers, **which deserves to be playable first** — and is it a full faction or a minor/mercenary power (like the Starport's smugglers)?
> - Is the **worm** a hazard we deepen, or a *mechanic we build a mode around* (worm-riding Fremen, worm survival)?
> - Does the **Starport's market economy** want to become a real economic-warfare mode, or stay flavor?

---

## Part 3 — The expansion map (cross-cutting vectors)

Stepping back from individual worlds: here are the **kinds of additions** we could make, each with its lore fit, the fantasy it delivers, the design tension to watch, and its rough cost. Think of these as the menu; Part 4 is about choosing from it.

### Vector A — Complete & deepen factions (faithful expansion)
**What:** Finish RA's sub-faction roster; deepen Nod/GDI asymmetry; add Dune's lore Houses.
**Lore fit:** Highest — these all already exist in the worlds' fiction.
**Fantasy:** "My faction finally feels complete / distinct."
**Tension:** **Competitive balance.** Every new faction/unit must survive the ladder. More factions = exponentially more matchups to balance.
**Cost:** Medium–High (art per unit), but the *design* templates exist.

### Vector B — New game modes (systems, not art)
**What:** First-class **PvE / co-op survival** (ants, Visceroids, worms all fit); a **territory/map-control** victory mode; **economic-warfare** mode (Dune Starport); a **roguelite skirmish-campaign** built on the existing procedural map generator; objective modes beyond "destroy everything."
**Lore fit:** High and flexible — modes reskin per world.
**Fantasy:** "A fresh way to play the game I already own."
**Tension:** Must plug into the five core loops or it feels bolted-on. Needs to be *teachable* in the lobby.
**Cost:** **Low–Medium — mostly rules + scripting, little new art.** *Best effort-to-impact ratio.*

### Vector C — Living world / dynamic environment
**What:** **Tiberium that spreads & mutates the map**; **sandworms as escalating world-actors**; weather/ion-storms/sandstorms; erosion. The map as a third player.
**Lore fit:** Extremely high — this is *the* defining fiction of both Tiberium and Dune, currently underused.
**Fantasy:** "The world is alive and I have to survive it, not just the enemy."
**Tension:** Determinism & performance (the engine simulates this for every client identically); and it can feel *unfair* if not carefully telegraphed. Hardest to balance for competitive, *brilliant* for PvE/co-op.
**Cost:** Medium (systems + some art for spread/creatures).

### Vector D — Narrative & campaigns
**What:** New single-player campaigns; **finish the Tiberian Sun campaign**; co-op story missions; a **hero/commander** layer (Tanya, Volkov, Kane, House mentats); branching/what-if timelines.
**Lore fit:** High — these worlds are *story-rich* and have beloved characters.
**Fantasy:** "Live inside this world's story with friends."
**Tension:** Content-heavy (scripting, mission design, balancing PvE difficulty). Lua scripting tools exist and are strong.
**Cost:** Medium–High (design/scripting time), Low art if reusing rosters.

### Vector E — Meta-progression & systems around the match
**What:** Commander profiles, an in-game **encyclopedia/codex** (the Encyclopedia widget already exists — a perfect *lore-delivery* surface), cosmetic unlocks, achievements, post-match meta.
**Lore fit:** Neutral — it's a wrapper, lore lives in the codex content.
**Fantasy:** "A reason to keep coming back; a place to *read* the world."
**Tension:** **Competitive integrity** — progression must never become pay-to-win or grind-to-win. Cosmetic-only is the safe line.
**Cost:** Low–Medium.

### Vector F — A new world (total conversion)
**What:** A brand-new universe on the engine (original IP, or another Westwood-adjacent setting).
**Lore fit:** N/A — you're *authoring* the lore.
**Fantasy:** "Something nobody has played before."
**Tension:** Enormous scope; competes with the modding community's own total conversions; needs art, lore, and balance from zero.
**Cost:** **Very High.** The most ambitious and riskiest path.

> **Open questions (vectors)**
> - Which vector matches your **appetite for art cost**? (B and E are cheap; A and F are expensive.)
> - Which matches your **audience**? (A → competitive; B/C/D → single-player & co-op; E → retention; F → a statement.)
> - Is there a **combination** that compounds? (e.g. *Living Tiberium (C)* + *Survival mode (B)* + *a short campaign (D)* = a complete new pillar, mostly cheap, deeply lore-true.)

---

## Part 4 — A few concrete "starter pitches" to react to

Not commitments — *prompts.* Each is sized as a coherent first project and named so you can say "more like this / less like this."

**P1 — "Complete the Allies & Soviets."** Add 2–4 RA sub-factions (e.g. Greece, Spain // a second Soviet republic), each one signature unit + one signature power, balanced for the ladder. *Vector A. Faithful, competitive, art-moderate. The "respect the existing game" choice.*

**P2 — "The Tiberium is spreading."** A co-op survival mode where Tiberium grows across the map each minute, spawning Visceroids, denying ground, forcing you to push out before the field consumes your base. *Vectors C+B. Cheap-ish, wildly on-theme, single-player/co-op. The "do something no RTS does" choice.*

**P3 — "Ride the Worm."** A Dune mode/mini-campaign around the sandworm: Fremen who can summon and ride worms, Thumper baiting as a core verb, escalating worm survival. *Vectors C+D. Iconic Dune fantasy, mostly systems + scripting. The "make the signature mechanic the star" choice.*

**P4 — "Finish Tiberian Sun."** Build out the TS campaign and round the roster into a playable future-war game, optionally introducing the Forgotten. *Vector D (+A). The "complete the half-built world" choice — high effort, high payoff for fans.*

**P5 — "The Codex."** A rich in-game encyclopedia that turns every unit, building, and faction into a *readable lore entry*, with a light commander-profile meta around it. *Vector E. Cheap, deepens *understanding* of the world (very aligned with your own goal here), low competitive risk. The "make the world legible & sticky" choice.*

> **Open questions (pitches)**
> - Which pitch makes you most want to **keep talking**? Which makes you go "no"? (Both answers are useful.)
> - For your favorite: who's it **for**, and what's the *one screenshot* that sells it?

---

## Part 5 — The big strategic questions (these steer everything)

If we answer these, the rest of the design falls out almost mechanically. These are the ones worth sitting with.

1. **Heart-of-the-project world.** Red Alert (pulpy Cold War), the Tiberium world (alien-plague sci-fi), or Dune (feudal desert)? *Which one do you most want to live in?*
2. **Audience.** Competitive ladder players, single-player/co-op players, or modders/UGC creators? *You can't fully serve all three at once with a first project.*
3. **Faithful vs extrapolated.** Stay reverent to each world's canon tone, or use OpenRA's existing "what-if" license to go bigger and weirder?
4. **Art budget reality.** Are we willing to fund/make **new sprite art** (unlocks factions & units, Vectors A/F), or do we want **maximum design for minimum art** (modes, systems, living-world, codex — Vectors B/C/E)?
5. **Add *within* vs add *across*.** Deepen one existing world, or build a cross-cutting *system/mode* that lifts all of them at once?
6. **Competitive integrity stance.** How protective are we of ladder balance? (This is the gate on factions, units, and any progression with power.)
7. **The world-as-actor question.** How excited are you, specifically, by the **living-world idea** (Tiberium spread / sandworm ecology)? It's the most *distinctive* thing we could build and it recurs in two of the three worlds — but it's also the one most in tension with competitive balance. Your gut here is a big fork in the road.

---

### How to use this doc
Pick a **world** (Q1) and an **audience** (Q2); that narrows Part 2 to one profile and Part 3 to 1–2 vectors. Then react to the **starter pitches** in Part 4 — even just "P2 yes, P1 no" — and we can take the winner and go a level deeper: specific units, specific mode rules, a specific campaign beat sheet, mockups of the one screenshot that sells it.

*All faction identities, unit roles, superweapons, campaign characters, and systems described here were drawn from the actual game data in `mods/ra`, `mods/cnc`, `mods/d2k`, and `mods/ts`, and the shared traits in `OpenRA.Mods.Common`. Where I extrapolate beyond what's shipped (new factions, modes, mechanics), it's flagged as a proposal, not existing content.*

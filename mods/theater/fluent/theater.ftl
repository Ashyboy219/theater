# THEATER — contemporary factions, their unique units, and dev-mode strings.

## Mod identity (override ra's title strings).
mod-title = THEATER
mod-windowtitle = THEATER

## Factions — doctrine line, the army's strength/tradeoff, and unique units.
faction-federation =
    .name = The Federation
    .description = USA — "See first, strike from nowhere." Far-seeing, long-range, cheap & fast AIR; fragile ground.
     Uniques: Specter Stealth Fighter, Reaper Gunship.

faction-northern_union =
    .name = Northern Union
    .description = Russia — "Mass fires, then move the mass." TANKY, hard-hitting vehicles; SLOW.
     Uniques: Bastion EW Tank, Grad Rocket Battery, Redoubt Bunker.

faction-continental_bloc =
    .name = Continental Bloc
    .description = China — "Attrition is a resource we print." CHEAP, builds FAST; fragile (spam the map).
     Uniques: Wing Loong Swarm Drone, Dazhbog Rocket Truck.

faction-rhine_compact =
    .name = Rhine Compact
    .description = Germany — "Build forward, don't break." DURABLE units; EXPENSIVE and slow to build.
     Uniques: Loewe Heavy MBT, Pioneer Combat-Engineer.

faction-isles_coalition =
    .name = Isles Coalition
    .description = UK — "The op is won before the shooting." Long-range, far-seeing, cheaper INFANTRY; weak vehicles.
     Uniques: Pathfinder Team, Comms EW Cell.

faction-eastern_maritime =
    .name = Eastern Maritime Pact
    .description = Japan — "Hold the line, deny the sky." DURABLE but low-offense (turtle); layered point defense.
     Uniques: JGSDF Anti-Tank Team, Aegis Turret, Hayabusa Interceptor.

faction-subcontinent_federation =
    .name = Subcontinent Federation
    .description = India — "Layered skies, deep reach." Long-RANGE but slow; layered air defense.
     Uniques: Garuda Cruise Launcher, Akash SAM, Tejas Strike Fighter.

faction-anatolian_alliance =
    .name = Anatolian Alliance
    .description = Turkey — "Cheap eyes, cheap teeth, everywhere." Cheap, fast, far-seeing AIR; attritable.
     Uniques: Bayrak Loiter Drone, Koral EW Van.

## Unique units. Descriptions name the ROLE, the COUNTER, and the build GATE
## (which production building / tech tier) so the tech path reads off the tooltip.
actor-specter =
    .name = Specter Stealth Fighter
    .description = USA stealth strike jet: cloaks when idle, snaps visible the instant it fires or is hit. Build from an Airfield.

actor-bastion =
    .name = Bastion EW Command Tank
    .description = Electronic-warfare tank. Deflects enemy guided missiles (ATGMs, SAMs, AA) near it and blacks out enemy radar. Counter with gun-armed units — autocannons, flak and tank guns fire straight through the jamming. Build from a War Factory.

actor-wingloong =
    .name = Wing Loong Swarm Drone
    .description = China attritable strike drone: cheap and fragile, fires a guided missile and reveals ground. Sent in numbers and expected to die. Build from an Airfield.
    .dronehive-name = Drone Swarm
    .dronehive-description = Launch a swarm of strike drones at the target area.

actor-loewe =
    .name = Loewe Heavy MBT
    .description = Germany's most durable tank, with active protection that deflects enemy guided missiles — bring gun-armed units (autocannon, tank guns) to kill it. Build from a War Factory.

actor-pathfinder =
    .name = Pathfinder Team
    .description = UK elite stealth infiltrator: cloaks, sees far, detects enemy stealth. Build from a Barracks.

actor-kunai =
    .name = JGSDF Anti-Tank Team
    .description = Japan dedicated anti-tank team: a Dragon ATGM that kills armor but cannot touch aircraft — screen it from the air. Build from a Barracks.

actor-garuda =
    .name = Garuda Cruise-Missile Launcher
    .description = India precision cruise-missile launcher: one fast, accurate, long-range missile. Fragile — screen it. Build from a War Factory.

actor-bayrak =
    .name = Bayrak UCAV
    .description = Turkey cheap recon-harasser drone: a huge sight radius and a single light missile; folds to any AA. Build from an Airfield.

## Additional faction uniques
actor-reaper =
    .name = Reaper Gunship
    .description = USA persistent close-air-support gunship: precision Hellfire anti-armor. Killed by SAMs and MANPADS. Build from a Helipad.
actor-grad =
    .name = Grad Rocket Battery
    .description = Russia rocket battery: a 5-rocket salvo that saturates an area, then a long reload. Spread out to survive it. Build from a War Factory.
actor-redoubt =
    .name = Redoubt Bunker
    .description = Russia armored bunker: a heavily-armored 25mm transport that leads the push and shields its cargo. Build from a War Factory.
actor-dazhbog =
    .name = Dazhbog Rocket Truck
    .description = China cheap mass artillery: a short-range 2-rocket burst, expendable in numbers. Folds to any fast push. Build from a War Factory.
actor-pioneer =
    .name = Pioneer Combat-Engineer
    .description = Germany forward combat engineer: captures buildings and repairs vehicles in the field. Build from a Service Depot.
actor-comms =
    .name = Comms Sensor Van
    .description = UK mobile sensor van: long-range vision and stealth detection — the eyes of the army (not a jammer). Fragile. Needs a Radar Dome.
actor-aegis =
    .name = Aegis Point-Defense Turret
    .description = Japan hardened point-defense turret: shoots down aircraft and deflects incoming missiles. Needs a Radar Dome.
actor-hayabusa =
    .name = Hayabusa Interceptor
    .description = Japan autonomous interceptor drone: pure anti-air, helpless against ground attack. Build from an Airfield.
actor-akash =
    .name = Akash SAM Battery
    .description = India long-range SAM battery: the longest-reach air defense in the game, fired in 2-missile salvos. Needs a Radar Dome.
actor-tejas =
    .name = Tejas Strike Fighter
    .description = India resilient standoff strike fighter: a hardened airframe firing a 2-missile guided salvo from distance. Build from an Airfield.
actor-koral =
    .name = Koral EW Van
    .description = Turkey EW van: a wide missile-jamming umbrella that deflects enemy guided ATGMs/AA/SAMs over the task force. Unarmed — escort it; gun weapons ignore the jamming. Needs a Radar Dome.
actor-raptor =
    .name = Raptor Air-Superiority Fighter
    .description = USA air-superiority fighter: pure anti-air that owns the sky for your strike jets — helpless vs ground, killable by SAMs. Build from an Airfield.
actor-tunguska =
    .name = Tunguska Air-Defense System
    .description = Russia mobile SPAAG: a long-range SAM plus a 25mm autocannon — the only mobile AA that can also defend itself on the ground. Needs a Radar Dome + War Factory.
actor-burke =
    .name = Burke Aegis Destroyer
    .description = USA Aegis air-defense destroyer: dominates aircraft and incoming missiles, near-helpless vs ships/subs — screen it. Needs a Naval Yard + Radar Dome.
actor-akula =
    .name = Akula Attack Submarine
    .description = Russia heavy attack submarine: a 3-torpedo alpha salvo with a long reload. Surfaces to fire; countered by sonar + depth charges. Build from a Naval Yard.
actor-houbei =
    .name = Houbei Missile Boat
    .description = China fast missile boat: anti-ship missiles, no sub or air defense — alpha-strike and flee. Cheap; swarm the littoral. Build from a Naval Yard.
actor-barbaros =
    .name = Barbaros USV Patrol Drone
    .description = Turkey USV recon drone-boat: fast and far-seeing, detects subs and stealth; weak guns. Build from a Naval Yard.
actor-astute =
    .name = Astute Attack Submarine
    .description = UK intelligence submarine: the best sonar in the game plus a quiet precision torpedo — hunts subs and scouts unseen. Build from a Naval Yard.
actor-kolkata =
    .name = Kolkata Missile Destroyer
    .description = India standoff missile destroyer: long-reach anti-ship missiles and deep sensors. Needs a Naval Yard + Radar Dome.

## Late-game flagships + tech tier
actor-techcenter =
    .name = Advanced Command Center
actor-amx =
    .name = Abrams-X Super-Heavy Tank
    .description = USA late-game ground anchor: a 90,000-HP twin-gun super-heavy with self-repair and anti-air missiles — the durable spearhead USA's air-first doctrine otherwise lacks. Requires a Tech Center.
actor-armata =
    .name = T-14 Armata Heavy Tank
    .description = Russia siege super-heavy: a 120mm main gun plus a thermobaric fuel-air secondary that levels infantry and fortifications, advancing through defences. Requires a Tech Center.
actor-b21 =
    .name = B-21 Stealth Bomber
    .description = USA strategic stealth bomber: cloaks when idle, drops a heavy precision bomb stick, then vanishes again. Alert SAMs kill it the instant it bombs. Requires a Tech Center.
actor-gj11 =
    .name = GJ-11 Drone Gunship
    .description = China heavy attack drone: a survivable Hellfire gunship cheap enough to mass — the late-game drone air-arm. Requires a Tech Center.
actor-akinci =
    .name = Akinci Heavy UCAV
    .description = Turkey high-end combat drone: fast, far-seeing and precision-armed — strikes ground and scouts the deepest. Cheap and fast to build. Requires a Tech Center.
actor-agni =
    .name = Agni Ballistic Launcher
    .description = India road-mobile ballistic launcher: the longest reach in the game, raining a heavy conventional warhead from beyond return range. Slow and fragile — screen it. Requires a Tech Center.

## Dev mode
checkbox-devunlock =
    .label = Dev: Unlock Tech Tree
    .description = Grants all building + tech-level prerequisites for your OWN faction, so you can build your whole roster with no building-grind. Does not unlock other factions' units — pick another faction in the lobby to test it.

## Structures
actor-thcom =
    .name = Theater Command
    .description = Forward planning HQ. Provides radar and calls in a combined-arms paradrop to coordinate reinforcements from the sky with your ground push.
    .para-name = Combined Paradrop
    .para-description = Drop a mixed rifle-and-rocket infantry squad onto any visible ground.
    .usaair-name = Sweep & Strike
    .usaair-description = Vector a strike jet along a chosen heading for a precision bomb run on the target area.
    .usanet-name = Common Operating Picture
    .usanet-description = Fuse the sensor network into a satellite sweep, revealing the battlefield for a short window.
    .rusbarrage-name = Saturation Barrage
    .rusbarrage-description = Call a timed off-map rocket-artillery strike onto a target area.
    .chnswarm-name = Drone Swarm Strike
    .chnswarm-description = Call in a swarm of loiter-munition drones to bomb the target area.
    .uk-specops-name = Behind-the-Lines Insertion
    .uk-specops-description = Airdrop a cloaked SAS Raider and rifle escort onto any visible ground.
    .uk-intel-name = Recon Overflight
    .uk-intel-description = Fly a reconnaissance aircraft across the target lane, revealing it.
    .missile-name = Cruise-Missile Strike
    .missile-description = Call in a precision long-range cruise-missile strike on any visible target area.

## Command capabilities — bought from the Theater Command's Command queue (a sidebar tab).
button-production-types-command-tooltip = Command Capabilities

actor-cap-firecontrol =
    .name = Fire Control
    .description = Unlocks army-wide targeting focus — order your whole force to prioritise enemy structures, armor, or infantry. Bought once; lasts the match.

actor-cap-formations =
    .name = Formations
    .description = Unlocks formation movement — grouped units spread into a Line, Column, Wedge, or Box at the destination instead of funneling single-file. Bought once; lasts the match.

## ============================ Faction doctrine branches ============================

## USA (Federation) doctrine branches — bought from the Command queue.
actor-doctrine-usa-air =
    .name = Doctrine: Air Superiority
    .description = Own the sky. Air builds cheaper and faster, and the Theater Command can call a Sweep & Strike precision bomb run. Cost: your ground armor grows even more fragile and pricier. Locks out Networked Fires.

actor-doctrine-usa-networked =
    .name = Doctrine: Networked Fires
    .description = Sensor-to-shooter precision. The whole force gains reach, accuracy, and deeper vision, and the Theater Command can pull a Common Operating Picture map sweep. Cost: every unit is pricier — fewer of them. Locks out Air Superiority.

## Northern Union (Russia) mid-game doctrine choice
actor-doctrine-rus-armor =
    .name = Doctrine: Armored Breakthrough
    .description = Concentrate on a heavy, fast-rolling armored fist. Unlocks the T-14 Molot heavy MBT; your vehicles get tankier and shed the national slowness to drive like a normal army. Costs more iron, and buttoned-up crews lose sight range. Locks out Artillery Saturation. Choose once; lasts the match.

actor-doctrine-rus-arty =
    .name = Doctrine: Artillery Saturation
    .description = Saturate the enemy from beyond their range. Adds the Saturation Barrage support power at the Theater Command and gives your vehicles longer reach and heavier fires. Your front line turns fragile and even slower, so guard the flanks. Locks out Armored Breakthrough. Choose once; lasts the match.

## China (Continental Bloc) doctrine branches — bought from the Command queue.
actor-doctrine-chn-air =
    .name = Drone Swarm Command
    .description = UAV-centric doctrine. Unlocks a standing five-drone loiter-munition strike from the Theater Command and lets your drones see further — but your ground armor grows even more fragile. Bought once; locks out Industrial Mobilization.

actor-doctrine-chn-industrial =
    .name = Industrial Mobilization
    .description = Total war-economy mass production. Armor and infantry get even cheaper and faster to build and unlock the Dongfeng mass tank — but every unit is weaker and dies faster. Bought once; locks out Drone Swarm Command.

## Turkey (Anatolian Alliance) — exclusive doctrine branches (power-name/-description are this block's own keys).
actor-doctrine-tur-recon =
    .name = Drone Recon Network
    .description = Doctrine — eyes everywhere. Your whole army sees further (+25% sight) and you gain the UAV Sweep: an on-demand recon drone that peels back the fog over any region. Tradeoff: your forces hit softer (-10% firepower). Locks out Rapid Response. Bought once; lasts the match.
    .power-name = UAV Sweep
    .power-description = Send a recon drone over the target area to reveal a wide patch of shroud for several seconds. No ordnance — pure vision.

actor-doctrine-tur-rapid =
    .name = Rapid Response Doctrine
    .description = Doctrine — hit and run. Your whole army moves faster (+15% speed) and you gain Rapid Deployment: air-drop a light squad anywhere visible on a short timer. Tradeoff: your forces are more exposed (+12% damage taken). Locks out Drone Recon. Bought once; lasts the match.
    .power-name = Rapid Deployment
    .power-description = Air-drop a fast rifle-and-rocket squad onto any visible ground to seize position or reinforce a push.

## UK (Isles Coalition) doctrine branches — bought from the Theater Command's Command queue.
actor-doctrine-uk-specops =
    .name = Special Operations
    .description = Commit to a small, elite, cloaked raiding force. Unlocks the SAS Raider and a behind-the-lines insertion drop; your infantry hit harder and survive longer — but your armor grows weaker and pricier. Bought once; locks out Intelligence Dominance.

actor-doctrine-uk-intel =
    .name = Intelligence Dominance
    .description = Commit to seeing the whole battlefield first. Your entire force gains counter-stealth detection, longer range and sight, and an on-demand recon overflight — but your frontline stays thin and your vehicles fragile. Bought once; locks out Special Operations.

## India (Subcontinent Federation) doctrine branches.
actor-doctrine-ind-missile =
    .name = Doctrine: Missile Command
    .description = Lean into long-reach standoff fires. More vehicle range and firepower, and unlocks a precision long-range cruise-missile strike from the Theater Command. Tradeoff: your guns reload slowly and your vehicles are more fragile in close combat. Mutually exclusive with Integrated Battle Groups; bought once, lasts the match.

actor-doctrine-ind-networked =
    .name = Doctrine: Integrated Battle Groups
    .description = Resilient combined-arms. Your vehicles are tougher and more mobile, and you unlock the Arjun Heavy MBT. Tradeoff: you give up the faction's signature reach and your force costs more. Mutually exclusive with Missile Command; bought once, lasts the match.

## ============================ Doctrine-unlocked units ============================

## New unit — Northern Union Armored Breakthrough unlock
actor-molot =
    .name = T-14 Molot Heavy MBT
    .description = Heavy breakthrough main battle tank. Thick armor and a 120mm gun lead the armored spearhead.\n  Requires: Armored Breakthrough doctrine

## Dongfeng Mass Tank (China Industrial Mobilization unlock)
actor-dongfeng =
    .name = Dongfeng Mass Tank
    .generic-name = Tank
    .description = Dirt-cheap mass-produced light tank. Weak alone — win by flooding the field.

## UK Special Operations unit unlock.
actor-raider =
    .name = SAS Raider
    .description = Cloaked special-operations team. Slips behind enemy lines to infiltrate and sabotage buildings; breaks cover only when it moves or acts.

## Arjun Heavy MBT (India Integrated Battle Groups unlock)
actor-arjun =
    .name = Arjun Heavy MBT
    .description = Heavily armored main battle tank with a 120mm gun. Anchors an Integrated Battle Group.

## ============================ Modern land roster ============================

## New shared combined-arms roles
actor-manpads =
    .name = Air-Defense Team
    .description = Dedicated anti-air infantry (man-portable SAM). Shoots down aircraft but cannot fight on the ground — keep it escorted.

actor-scout =
    .name = Scout Team
    .description = Forward observer — a wide sight radius and cloak detection. Spots the enemy and reveals stealth; weak in a fight.

actor-ifv =
    .name = IFV
    .description = Infantry fighting vehicle. Carries a squad and supports it with a 25mm autocannon.

actor-mobsam =
    .name = Mobile SAM
    .description = Mobile anti-air vehicle. Denies the sky on the move; helpless against ground attack.

## Grounded names for the core land roster
actor-rifleman =
    .name = Rifleman
actor-at-team =
    .name = Anti-Tank Team
actor-combat-engineer =
    .name = Combat Engineer
actor-combat-medic =
    .name = Combat Medic
actor-special-forces =
    .name = Special Forces
actor-recon-vehicle =
    .name = Recon Vehicle
actor-light-tank =
    .name = Light Tank
actor-mbt =
    .name = Main Battle Tank
actor-spg =
    .name = Self-Propelled Artillery

## ============================ Per-faction land variants ============================

actor-bradley =
    .name = M2 Bradley IFV
    .description = USA — advanced networked IFV: 25mm autocannon, anti-tank ATGM, carries a squad, extra sight.

actor-jtac =
    .name = JTAC Humvee
    .description = USA — recon + precision strike: wide vision and a Hellfire-class missile to designate and kill priority targets. Fragile.

actor-bmpt =
    .name = BMPT Terminator
    .description = Russia — heavy tank-support: twin autocannons shred infantry and light vehicles, plus an anti-tank missile. No transport; escort for the armored fist.

actor-loiter =
    .name = Loitering-Munition Truck
    .description = China — cheap, fast launcher for a precision kamikaze drone against armor and structures. Field it in numbers; it dies if caught.

actor-cobra =
    .name = Cobra Recon-Strike
    .description = Turkey — fast wheeled scout/harasser: 25mm autocannon and a wide sight radius. Gets eyes and fire forward fast.

actor-jackal =
    .name = Jackal Recon
    .description = UK — fast mobile counter-stealth sensor: wide vision and cloak detection on the move. Reveals enemy stealth.

actor-namica =
    .name = NAMICA Tank-Destroyer
    .description = India — standoff ATGM carrier: kills armor from range but is helpless against infantry. Screen it.

## ============================ 4X: Research Lab + tech tree ============================
actor-rlab =
    .name = Research Lab
    .description = Unlocks the research tech tree in the Command tab — accumulating army-wide upgrades (ballistics, armor, optics, propulsion, logistics) plus a mass-vs-precision decision. Build one to start researching. Needs a Radar Dome.

actor-research-ballistics =
    .name = Research: Ballistics
    .description = +15% firepower for all your vehicles. Researched once; lasts the match.
actor-research-armor =
    .name = Research: Composite Armor
    .description = Your vehicles take 12% less damage. Researched once; lasts the match.
actor-research-optics =
    .name = Research: Optics
    .description = +20% sight range for vehicles and infantry — see further, react sooner.
actor-research-propulsion =
    .name = Research: Propulsion
    .description = +12% movement speed for all your vehicles.
actor-research-smallarms =
    .name = Research: Small Arms
    .description = +15% firepower for all your infantry.
actor-research-logistics =
    .name = Research: Logistics
    .description = Your vehicles cost 12% less to build — a leaner war economy.
actor-research-mass =
    .name = Research: Mass Production
    .description = DECISION — locks out Precision Manufacturing. Vehicles cost 15% less to build but are frailer (+10% damage taken). Win by quantity.
actor-research-precision =
    .name = Research: Precision Manufacturing
    .description = DECISION — locks out Mass Production. Vehicles hit 22% harder but cost 10% more. Win by quality.

## Tier 2 — advanced research (unlocked by advancing to the Information Age)
actor-research-netcentric =
    .name = Research: Network-Centric Warfare
    .description = +10% weapon range for all your vehicles. Requires the Information Age.
actor-research-reactivearmor =
    .name = Research: Reactive Armor
    .description = Your vehicles take a further 10% less damage (stacks with Composite Armor). Requires the Information Age.
actor-research-autoloaders =
    .name = Research: Autoloaders
    .description = Your vehicles reload 15% faster — higher rate of fire. Requires the Information Age.
actor-research-avionics =
    .name = Research: Advanced Avionics
    .description = Your aircraft hit 15% harder and fly 10% faster. Requires the Information Age.
actor-research-navalcombat =
    .name = Research: Integrated Naval Systems
    .description = Your ships hit 15% harder and take 10% less damage. Requires the Information Age.

## ============================ 4X: Ages / eras (escalation ladder) ============================
actor-age-information =
    .name = Advance: Information Age
    .description = Advance from the Modern age. Your whole military gets tougher (−6% damage taken) and hits harder (+5%). Opens the path to the Autonomous Age. Researched at the Research Lab.
actor-age-autonomous =
    .name = Advance: Autonomous Age
    .description = The drone-and-AI era. A further army-wide boost (tougher, +6% firepower, +5% sight) that STACKS on the Information Age. Needs the Information Age + a Tech Center. Opens the Orbital Age.
actor-age-orbital =
    .name = Advance: Orbital Age
    .description = The apex era. The biggest army-wide escalation yet (−8% damage taken, +8% firepower), stacking on the earlier ages, and the gate for the largest late-game platforms to come. Needs the Autonomous Age + an Alloy Extractor (strategic resource).

## ============================ 4X: Strategic resource / economy ============================
actor-alloyex =
    .name = Alloy Extractor
    .description = Strategic resource + economy building. Drips a steady income (build several across your territory — expansion pays) and produces the alloys your apex tier needs: the Orbital Age and the largest late-game platforms require one. Also stores resources. Needs a Refinery.

actor-bank =
    .name = Bank
    .description = Pure economy building — strong passive income, no military function. Stack several to out-economy your opponent, then boost them all with the Economy Network research. The invest-in-economy-vs-army decision. Needs a Refinery.

actor-fusion =
    .name = Fusion Reactor
    .description = High-output power plant (3x a standard plant) for the power-hungry late game — one Fusion replaces several power plants. Needs a Radar Dome.

actor-econ-network =
    .name = Research: Economy Network
    .description = Boosts the income of ALL your economy buildings (banks and alloy extractors) by 30%. Invest in economy infrastructure, then upgrade it. Researched at the Research Lab.

## ============================ 4X: Spectacle units (apex payoff) ============================
actor-leviathan =
    .name = Leviathan Arsenal Dreadnought
    .description = The apex super-warship — gargantuan, with the longest-reaching, heaviest naval guns in the game plus anti-air defense, and an enormous health pool. Slow and hugely expensive; limited to 2. The reward for climbing the whole tech ladder: needs a Naval Yard, the Orbital Age, and an Alloy Extractor.

actor-citadel =
    .name = Citadel Mobile Fortress
    .description = The land apex — a gargantuan tracked super-fortress with a devastating siege cannon, anti-air missiles, and a colossal health pool. Slow, hugely expensive, limited to 2. The land equivalent of the Leviathan: needs a War Factory and the Orbital Age (so the whole tech ladder).

actor-archangel =
    .name = Archangel Heavy Gunship
    .description = The air apex — a gargantuan flying fortress that loiters and rains a sustained heavy barrage. Huge but slow and exposed: SAMs and AA are its counter, so escort it. Limited to 2. Needs a Helipad and the Orbital Age.

actor-tempest =
    .name = Tempest Rocket Artillery
    .description = The strategic-bombardment apex — a gargantuan mobile launcher that rains a 12-rocket salvo across most of the map. Devastating area saturation, but a very long reload and far-forward deployment leave it exposed, so screen it. Limited to 2. Needs a War Factory and the Orbital Age.

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
    .description = Japan — "Autonomy holds the line." DURABLE but low-offense (turtle); point defense.
     Uniques: Kunai Combat Robot, Aegis Turret, Hayabusa Interceptor.

faction-subcontinent_federation =
    .name = Subcontinent Federation
    .description = India — "Layered skies, deep reach." Long-RANGE but slow; layered air defense.
     Uniques: Garuda Cruise Launcher, Akash SAM, Tejas Strike Fighter.

faction-anatolian_alliance =
    .name = Anatolian Alliance
    .description = Turkey — "Cheap eyes, cheap teeth, everywhere." Cheap, fast, far-seeing AIR; attritable.
     Uniques: Bayrak Loiter Drone, Koral EW Van.

## Unique units
actor-specter =
    .name = Specter Stealth Fighter

actor-bastion =
    .name = Bastion EW Command Tank

actor-wingloong =
    .name = Wing Loong Swarm Drone

actor-loewe =
    .name = Loewe Heavy MBT

actor-pathfinder =
    .name = Pathfinder Team

actor-kunai =
    .name = Kunai Combat Robot

actor-garuda =
    .name = Garuda Cruise-Missile Launcher

actor-bayrak =
    .name = Bayrak UCAV

## Additional faction uniques
actor-reaper =
    .name = Reaper Gunship
actor-grad =
    .name = Grad Rocket Battery
actor-redoubt =
    .name = Redoubt Bunker
actor-dazhbog =
    .name = Dazhbog Rocket Truck
actor-pioneer =
    .name = Pioneer Combat-Engineer
actor-comms =
    .name = Comms EW Cell
actor-aegis =
    .name = Aegis Point-Defense Turret
actor-hayabusa =
    .name = Hayabusa Interceptor
actor-akash =
    .name = Akash SAM Battery
actor-tejas =
    .name = Tejas Strike Fighter
actor-koral =
    .name = Koral EW Van

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

## War Room overlay
warroom-title = WAR ROOM
warroom-set-doctrine = SET ARMY TARGETING DOCTRINE (also: /target structures|armor|infantry|balanced)
warroom-tech-path = TECH PATH — how to unlock each tier:
button-doctrine-balanced = Balanced
button-doctrine-structures = Structures
button-doctrine-armor = Armor
button-doctrine-infantry = Infantry
button-warroom-close = Close

## National Command — the cinematic War Room dashboard
wr-brand = THEATER COMMAND
wr-title = NATIONAL COMMAND
wr-subtitle = EMERGENCY WAR ROOM
wr-under-attack = UNDER ATTACK

# Left — live resources + military
wr-national-resources = NATIONAL RESOURCES
wr-military-overview = MILITARY OVERVIEW
wr-res-credits = Treasury
wr-res-power = Power Grid
wr-res-supplies = Supplies
wr-res-pop = Personnel
wr-mil-veh = Vehicles
wr-mil-inf = Infantry
wr-mil-air = Aircraft
wr-mil-nav = Naval

# Center — the president
wr-president-name = PRESIDENT ANDREW HAWKINS
wr-president-role = COMMANDER IN CHIEF
wr-immediate-decisions = IMMEDIATE DECISIONS
wr-card-reserves = DEPLOY RESERVES
wr-card-air = AIR SUPPORT
wr-card-econ = ECONOMIC SHIFT
wr-card-address = NATIONAL ADDRESS
wr-verb-deploy = Deploy
wr-verb-authorize = Authorize
wr-verb-activate = Activate
wr-verb-broadcast = Broadcast

# Right — threat + active effects
wr-strategic-overview = STRATEGIC OVERVIEW
wr-active-effects = ACTIVE EFFECTS
wr-sector-n = North
wr-sector-e = East
wr-sector-s = South
wr-sector-w = West
wr-fx-ew = Advanced EW
wr-fx-prec = Precision Strikes
wr-fx-econ = Economic Shift
wr-fx-addr = National Address

# Bottom — doctrines
wr-war-doctrines = WAR DOCTRINES
wr-doc-structures = Focus Structures
wr-doc-armor = Focus Armor
wr-doc-defensive = Defensive Posture
wr-doc-assault = Rapid Assault
wr-doc-structures-desc = Your whole army targets enemy buildings and defenses first.
wr-doc-armor-desc = Your army hunts enemy vehicles first — good against tank pushes.
wr-doc-defensive-desc = Move ~20% slower but take ~20% less damage. Hold ground.
wr-doc-assault-desc = Move ~18% faster and hit ~12% harder. Press the attack.

# Bottom — strategic upgrades
wr-strategic-upgrades = STRATEGIC UPGRADES
wr-upg-satellite = Satellite Recon
wr-upg-ew = Advanced EW
wr-upg-precision = Precision Strikes
wr-upg-black = Black Projects
wr-upg-satellite-desc = Reveal the entire map for ~60 seconds. Costs $3,000.
wr-upg-ew-desc = +18% weapon range and tighter aim, army-wide (~80s). Costs $4,500.
wr-upg-precision-desc = +25% firepower, army-wide (~60s). Costs $4,600.
wr-upg-black-desc = Permanent: +10% firepower and +15% armor, army-wide. Costs $8,000.
wr-cost-3000 = $3,000
wr-cost-4500 = $4,500
wr-cost-4600 = $4,600
wr-cost-8000 = $8,000
wr-purchase = BUY

# Bottom — command staff
wr-command-staff = COMMAND STAFF
wr-staff-steel = Gen. Steel
wr-staff-jameson = Adm. Jameson
wr-staff-kovalenko = Dr. Kovalenko
wr-staff-marshall = Dir. Marshall

# Bottom bar
wr-return-battlefield = RETURN TO BATTLEFIELD

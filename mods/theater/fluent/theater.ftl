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

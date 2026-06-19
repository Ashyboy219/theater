--[[
   THEATER — Phase 0 territory auto-placer.

   Turns ANY skirmish map into a "fight over the map for income" map with no hand-editing:
   on world load it spawns neutral, capturable Control Nodes (ctrl) in spokes running from
   each player's start toward the map centre, forming capturable supply chains that the
   SupplyNetwork trait then scores. Activated automatically for any map that loads
   ra|rules/territory-economy.yaml (it wires this script onto the World).

   DETERMINISM: this runs inside the lock-step simulation on every client, so it uses
   integer math only — no math.random, no trig, no floating-point-sensitive logic.
]]

local AllowedTerrain = { Clear = true, Road = true, Rough = true, Beach = true }
local NodeType = "ctrl"
local Spacing = 9       -- cells between chained nodes (must stay under the CTRL LinkRange of 12)
local SearchRings = 6   -- how far to spiral-search outward for a buildable cell

local function FootprintClear(cell)
	-- CTRL has a 2x2 footprint: require all four cells to be buildable terrain.
	local offsets = { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } }
	for i = 1, #offsets do
		local c = CPos.New(cell.X + offsets[i][1], cell.Y + offsets[i][2])
		if not AllowedTerrain[Map.TerrainType(c)] then
			return false
		end
	end

	-- Reject if anything already occupies the area (bases, starting units, trees).
	return #Map.ActorsInCircle(Map.CenterOfCell(cell), WDist.New(2048)) == 0
end

local function FindBuildable(cell)
	if FootprintClear(cell) then
		return cell
	end

	for r = 1, SearchRings do
		for dx = -r, r do
			for dy = -r, r do
				if math.abs(dx) == r or math.abs(dy) == r then
					local c = CPos.New(cell.X + dx, cell.Y + dy)
					if FootprintClear(c) then
						return c
					end
				end
			end
		end
	end

	return nil
end

local function PlaceNode(cell, neutral)
	local spot = FindBuildable(cell)
	if spot ~= nil then
		Actor.Create(NodeType, true, { Owner = neutral, Location = spot })
	end
end

WorldLoaded = function()
	local neutral = Player.GetPlayer("Neutral")
	if neutral == nil then
		local nonCombatants = Player.GetPlayers(function(p) return p.IsNonCombatant end)
		neutral = nonCombatants[1]
	end

	if neutral == nil then
		return
	end

	local combatants = Player.GetPlayers(function(p) return not p.IsNonCombatant end)
	if #combatants == 0 then
		return
	end

	-- Map "centre" = integer average of the player start cells (the natural contested middle).
	local sumX, sumY = 0, 0
	for i = 1, #combatants do
		local home = combatants[i].HomeLocation
		sumX = sumX + home.X
		sumY = sumY + home.Y
	end

	local center = CPos.New(math.floor(sumX / #combatants), math.floor(sumY / #combatants))

	-- A spoke of nodes from each start toward the centre: a capturable supply chain
	-- where holding the inner links keeps the outer (richer, contested) nodes paying.
	for i = 1, #combatants do
		local home = combatants[i].HomeLocation
		local delta = center - home
		local steps = math.max(1, math.floor(delta.Length / Spacing))
		for k = 1, steps do
			local cell = CPos.New(home.X + math.floor(delta.X * k / steps),
				home.Y + math.floor(delta.Y * k / steps))
			PlaceNode(cell, neutral)
		end
	end

	-- A couple of prize nodes right at the contested centre.
	PlaceNode(center, neutral)
	PlaceNode(CPos.New(center.X + 2, center.Y + 2), neutral)
end

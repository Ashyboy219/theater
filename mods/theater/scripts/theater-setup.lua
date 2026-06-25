--[[
   THEATER — Match Slice v1 auto-placer.

   Spreads a varied set of capturable structures across the contested middle of any skirmish map so
   there are MULTIPLE fronts to fight over (not one clustered control point):
     • centre:            HOSP (heal) + FCOM (forward build area)   — the big shared prizes
     • east / west:       CTRLCASH (developable income)
     • north / south:     CTRLWATCH (developable vision)
   Ore is untouched — these are a bonus layer on top of a full harvester economy.

   DETERMINISM: runs in the lock-step sim on every client — integer math only (no random/trig/floats).
]]

-- Solid build terrain only: NO Beach (the playtest bug placed structures on the shoreline).
local AllowedTerrain = { Clear = true, Road = true, Rough = true }
local SearchRings = 14
local Ring = 16 -- cells from centre for the outer nodes

local function FootprintClear(cell)
	-- The 2x2 footprint must be solid build terrain...
	local offsets = { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } }
	for i = 1, #offsets do
		local c = CPos.New(cell.X + offsets[i][1], cell.Y + offsets[i][2])
		if not AllowedTerrain[Map.TerrainType(c)] then
			return false
		end
	end

	-- ...with a one-cell margin clear of water, so nothing lands on a shoreline.
	for dx = -1, 2 do
		for dy = -1, 2 do
			if Map.TerrainType(CPos.New(cell.X + dx, cell.Y + dy)) == "Water" then
				return false
			end
		end
	end

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

local function PlaceNode(actorType, cell, neutral)
	local spot = FindBuildable(cell)
	if spot ~= nil then
		Actor.Create(actorType, true, { Owner = neutral, Location = spot })
	end
end

-- NOTE: human-only sandbox cash is handled in C# now (DeveloperMode's per-tick top-up, gated to human
-- combatants), so there is no cash logic in this script anymore — it only places the territory nodes.

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

	local sumX, sumY = 0, 0
	for i = 1, #combatants do
		local home = combatants[i].HomeLocation
		sumX = sumX + home.X
		sumY = sumY + home.Y
	end
	local cx = math.floor(sumX / #combatants)
	local cy = math.floor(sumY / #combatants)

	-- The big prize at the contested centre: the Tech Lab (Mammoth unlock).
	PlaceNode("ctrllab", CPos.New(cx, cy), neutral)

	-- Positional footholds spread to either side; one economic node. Positional > economic by headcount.
	PlaceNode("fcom", CPos.New(cx - Ring, cy), neutral)
	PlaceNode("fcom", CPos.New(cx + Ring, cy), neutral)
	PlaceNode("ctrlcash", CPos.New(cx, cy - Ring), neutral)
end

# Formation refinement — WEDGE should be a filled, range-aware triangle (future pass)

**Live-test feedback (2026-06-19):** Fire Control passed and is useful; Formations are functional and worth
keeping. But the **WEDGE** shape is wrong.

## The issue
The current implementation (`OpenRA.Mods.Common/Orders/FormationOrderGenerator.cs`, `ShapeOffsets` for
`FormationShape.Wedge`) places units only on the **edges** of a triangle — a V/arrowhead *outline*:
pairs at `(±depth, depth)`. With many units this makes long thin arms, so trailing/flank units fall
**outside weapon range** or arrive **behind** the engagement, cutting effective DPS.

## Desired behaviour
A **compact, filled, stacked** triangle (a solid wedge — cumulative rows of increasing width:
row 0 = 1 unit, row 1 = 2, row 2 = 3, …). More generally, formations should be **range-aware**: keep
most units inside a tight enough footprint that they can all contribute when contact occurs. The goal is
**combat effectiveness**, not geometric prettiness.

## Priority (secondary — do NOT over-invest now)
Formations are a command-and-control convenience, not a core gameplay pillar. From testing:
- **Infantry:** fine without sophisticated formations.
- **Armor / artillery / naval + submarines:** benefit significantly — prioritize these when refining.
- **Air strike packages:** maybe later.

## Where to fix
`FormationOrderGenerator.ShapeOffsets(FormationShape.Wedge, n)` — replace the edge-only V with a filled
triangular packing (cumulative rows). Add a max-width cap so the footprint stays within the slowest
unit's weapon range. Keep it all integer / deterministic at order-generation time (no float/Random).
The Box/Line/Column shapes already pack reasonably; Wedge is the one to redo.

# THEATER — Unit Formations: Build Spec (v1)

**Goal:** a selected group moves *together in a chosen shape* (Wedge/Line/Column/Box/Echelon) instead of stringing single-file into a chokepoint. The active formation is picked in the War Room and changes how a multi-unit MOVE behaves. Deterministic, reuses existing systems, human-testable in a skirmish.

All file:line anchors below were verified against the working tree on branch `theater`.

---

## 1. APPROACH DECISION

### The three candidates

**A — Intercept order generation, assign integer slot offsets per unit (RECOMMENDED for v1).**
At order-issue time, read the player's active formation, compute one integer `CVec` slot offset per selected unit (rotated toward the move vector), and issue each unit its *own* `AttackMove` order targeting `destinationCell + slotOffset[i]`. Units still pathfind independently, but they now aim at *distinct, pre-arranged cells* spread in the chosen shape instead of all funneling to one cell.
- **Fixes:** the single-file death-funnel (each unit has its own lane/cell, so they fan out into the shape before the chokepoint and arrive arranged). Zero engine/activity changes. Reuses `Move`/`Mobile.MoveTo`/`AttackMoveActivity` and THEATER's attack-move-by-default unchanged. Fully deterministic. Testable in a skirmish immediately.
- **Does NOT fix:** true en-route cohesion. Fast units still outrun slow ones between waypoints; the shape is set at the *destination*, not maintained continuously. Mid-move losses don't re-pack. Units auto-engaging (attack-move) will temporarily peel off and loosen the shape (by design).

**B — A full `FormationMove` activity that keeps cohesion en route at slowest-unit speed.**
A new peer-aware activity that holds fast units, pulls in stragglers, and advances the group as a body (the AI's leader+regroup loop, `GroundStates.cs:121-196`, generalized for the player).
- **Fixes:** real cohesion; the shape is maintained the whole way.
- **Costs/risks:** must fight `Move`'s per-cell state machine, add cross-unit coordination inside `world.Tick()` (highest desync surface), and synthesize slowest-unit pacing (the engine has **none** — speed is per-actor per-cell via `Mobile.MovementSpeedForCell` (`Mobile.cs:748`), only adjustable through `ISpeedModifier`). Much harder to get deterministic; harder to test incrementally. Upstream never solved this (`AttackMove.cs:96` literal TODO).

**C — Hybrid:** A for the geometry now, plus an optional capped `ISpeedModifier` to approximate slowest-unit pacing (gate every unit's speed to the group's slowest `Info.Speed` at order time, reusing the `SpeedMultiplier` machinery — `mods/theater/rules/theater-command.yaml:35-44` already uses `SpeedMultiplier@DEF/@ASSAULT`).

### Decision: **Ship A for v1.** Keep C's pacing as a clearly-scoped follow-on.

Rationale, against the stated priorities:
1. **Deterministic** — all slot math runs at order-generation time, baked into each unit's own serialized `Target`. Nothing new executes inside `world.Tick()`; resolution (`AttackMove.ResolveOrder` / `Mobile.ResolveOrder`) is unchanged and already integer-only.
2. **Reuses existing systems** — no new activity. The formation state rides the exact `ArmyCommand` + War Room synced-order pattern. Geometry uses existing fixed-point helpers (`WAngle.ArcTan`, `WVec.Rotate`, `Exts.ISqrt`, `Map.CellContaining`, `Mobile.NearestMoveableCell`).
3. **Testable** — the moment you select N units, pick a shape, and right-click, you *see* the arrangement. No headless requirement to validate the core mechanic's wiring (the visual confirmation needs a GUI, but the build/lint do not).

B is deferred because its only marginal win over A (continuous cohesion) carries the entire desync risk class, and A already kills the single-file problem — which is the user's actual pain.

---

## 2. EXACT IMPLEMENTATION

### Integration seam (why a subclass, not an edit)

THEATER's `mods/theater/mod.yaml:282` declares `DefaultOrderGenerator: UnitOrderGenerator`. `World.cs:188` resolves that type name via `ObjectCreator`, and `World.CancelInputMode()` (`World.cs:172`) reconstructs it with the `(World)` ctor. So the clean, low-blast-radius hook is a **new subclass** `FormationOrderGenerator : UnitOrderGenerator` (overriding `OrderInner`), registered as THEATER's `DefaultOrderGenerator`. No edits to the shared stock `UnitOrderGenerator.cs`.

> Note on the default-move path: THEATER routes the plain ground click through `AttackMoveByDefault` (Priority 5 > Mobile's Move targeter 4, `AttackMoveByDefault.cs:25-29`). But that targeter still runs **inside `UnitOrderGenerator.OrderInner`** — each selected actor is handed `OrderForUnit(a, target, cell, mi)` (`UnitOrderGenerator.cs:68-71`), which calls the actor's own `AttackMoveByDefault.IssueOrder` and mints a per-unit `"AttackMove"` order. We override `OrderInner` so each unit's `target` is its *slot cell*, not the shared click cell. Because each unit already gets its own order here (the only grouped emission is the APM `CreateGroup` hack at `:79`), the per-unit-offset design needs **no** grouped-order surgery and is unaffected by `Order.FromGroupedOrder` sharing one Target.

### File 1 (NEW): `OpenRA.Mods.Common/Traits/Player/ActiveFormation.cs`

Clone of `ArmyCommand.cs:27-64`, minus the consumer/condition fan-out (formation is *pull-read* at move time, not pushed as a condition).

```csharp
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum Formation : byte { None = 0, Line = 1, Column = 2, Wedge = 3, Box = 4, Echelon = 5 }

	[TraitLocation(SystemActors.Player)]
	[Desc("THEATER: holds the player's active movement FORMATION. Read at move-order time to ",
		"arrange a multi-unit move into a shape. Set via the War Room or /formation; deterministic int.")]
	public class ActiveFormationInfo : TraitInfo
	{
		[Desc("Cell gap between slots. 1 = one empty cell between units. Integer only.")]
		public readonly int SpacingCells = 1;

		public override object Create(ActorInitializer init) { return new ActiveFormation(this); }
	}

	public class ActiveFormation : IResolveOrder, ISync
	{
		public const string OrderName = "SetFormation";

		public readonly ActiveFormationInfo Info;

		[Sync]
		int formation;

		public ActiveFormation(ActiveFormationInfo info) { Info = info; }

		public Formation Active => (Formation)formation;

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderName)
				return;

			var f = (int)order.ExtraData;
			if (f < 0 || f > (int)Formation.Echelon || f == formation)
				return;

			formation = f;
		}
	}
}
```
*(`[Sync]` XORs `formation` into the per-frame sync hash — `Sync.cs`. Mirrors `ArmyCommand.doctrine` `[VerifySync]` at `ArmyCommand.cs:35-36`.)*

### File 2 (NEW): `OpenRA.Mods.Common/Orders/FormationOrderGenerator.cs`

The hook point. Overrides `UnitOrderGenerator.OrderInner` (`UnitOrderGenerator.cs:65-83`). For shape `None` it falls straight through to the base (exact stock behavior, zero offsets). Otherwise it computes a slot per actor and rewrites the per-unit target cell.

```csharp
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Orders
{
	public class FormationOrderGenerator : UnitOrderGenerator
	{
		public FormationOrderGenerator(World world) : base(world) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var player = world.LocalPlayer;
			var fc = player?.PlayerActor.TraitOrDefault<ActiveFormation>();
			var shape = fc?.Active ?? Formation.None;

			// Only shape ground Mobile units; let everything else (air, immobile) use the base path.
			var movers = world.Selection.Actors
				.Where(a => !a.IsDead && a.Info.HasTraitInfo<MobileInfo>())
				.OrderBy(a => a.ActorID)               // network-stable slot ordering
				.ToList();

			// No formation, terrain-only target, single unit, or no movers -> stock behaviour.
			var target = TargetForInput(world, cell, worldPixel, mi);
			if (shape == Formation.None || movers.Count < 2 || target.Type != TargetType.Terrain)
				return base.OrderInner(world, cell, worldPixel, mi);

			var anchor = world.Map.Clamp(cell);
			var anchorPos = world.Map.CenterOfCell(anchor);

			// Forward heading = group centroid -> anchor, integer ArcTan (no float).
			var centroid = AverageCenter(movers);
			var dir = WAngle.ArcTan(anchorPos.Y - centroid.Y, anchorPos.X - centroid.X); // world Y is south-positive
			var rot = new WRot(WAngle.Zero, WAngle.Zero, dir);

			var spacing = fc.Info.SpacingCells * 1024;  // 1024 world units = 1 cell
			var slots = FormationLayout.Slots(shape, movers.Count, spacing); // local-frame WVecs (forward=+X, right=+Y)

			var slotCells = new CPos[movers.Count];
			var used = new HashSet<CPos>();
			for (var i = 0; i < movers.Count; i++)
			{
				var world3 = slots[i].Rotate(rot);
				var c = world.Map.Clamp(world.Map.CellContaining(anchorPos + world3));

				// Deconflict identical quantised cells deterministically by nudging outward.
				while (!used.Add(c))
					c = world.Map.Clamp(c + new CVec(1, 0));

				slotCells[i] = c;
			}

			// Emit one order per mover to its OWN slot cell; non-movers fall back to the base target.
			var slotIndex = movers.Select((a, i) => (a, i)).ToDictionary(p => p.a, p => p.i);
			var orders = new List<Order>();
			var actorsInvolved = new List<Actor>();
			foreach (var a in world.Selection.Actors.Where(a => !a.IsDead))
			{
				var unitTarget = slotIndex.TryGetValue(a, out var idx)
					? Target.FromCell(world, slotCells[idx])
					: target;

				var o = OrderForUnit(a, unitTarget, world.Map.CellContaining(unitTarget.CenterPosition), mi);
				if (o != null) { orders.Add(o.Order); actorsInvolved.Add(o.Actor); }

				if (o != null)
					orders[orders.Count - 1] = CheckSameOrder(o.Order,
						o.Trait.IssueOrder(o.Actor, o.Order, o.Target, mi.Modifiers.HasModifier(Modifiers.Shift)));
			}

			if (actorsInvolved.Count == 0)
				return Enumerable.Empty<Order>();

			// Preserve the APM CreateGroup hack (UnitOrderGenerator.cs:79).
			var result = new List<Order>
			{
				new Order("CreateGroup", actorsInvolved[0].Owner.PlayerActor, false, actorsInvolved.Distinct().ToArray())
			};
			result.AddRange(orders);
			return result;
		}

		static WPos AverageCenter(IReadOnlyList<Actor> actors)
		{
			long x = 0, y = 0, z = 0;
			foreach (var a in actors) { var p = a.CenterPosition; x += p.X; y += p.Y; z += p.Z; }
			var n = actors.Count;
			return new WPos((int)(x / n), (int)(y / n), (int)(z / n));
		}
	}
}
```

> **Implementation note — visibility of base helpers.** `OrderForUnit`, `CheckSameOrder`, and `TargetForInput` must be reachable from the subclass. `TargetForInput` is already `protected static` (`UnitOrderGenerator.cs:36`). Verify/relax `OrderForUnit` and `CheckSameOrder` to `protected` in the stock file (they are private today at `:69`/`:82`) — this is the **only** edit to shared code, and it is a visibility widening, behavior-identical. If you want *zero* shared-file edits, replicate the tiny `CheckSameOrder` body and call `a.TraitsImplementing<IIssueOrder>()` directly instead of `OrderForUnit`; the subclass approach with widened visibility is cleaner.

### File 3 (NEW): `OpenRA.Mods.Common/Orders/FormationLayout.cs` — integer slot geometry (NO floats)

Local frame: `+X` = forward (toward destination), `+Y` = right. All in world units (`1024 = 1 cell`). `SP` = spacing (world units). Centering uses the doubled-coordinate trick (`2*i-(N-1)` with `SP/2`) so odd/even counts stay symmetric without `(N-1)/2` truncation.

```csharp
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits; // Formation enum

namespace OpenRA.Mods.Common.Orders
{
	public static class FormationLayout
	{
		// Returns one local-frame WVec per slot, forward=+X, right=+Y. Caller rotates+quantises.
		public static WVec[] Slots(Formation shape, int n, int sp)
		{
			var s = new WVec[n];
			var half = sp / 2;
			switch (shape)
			{
				case Formation.Line:        // abreast, perpendicular to travel
					for (var i = 0; i < n; i++)
						s[i] = new WVec(0, (2 * i - (n - 1)) * half, 0);
					break;

				case Formation.Column:      // single file along travel, centred
					for (var i = 0; i < n; i++)
						s[i] = new WVec((2 * i - (n - 1)) * -half, 0, 0); // negative X = trailing
					break;

				case Formation.Wedge:       // V / arrowhead, tip at slot 0
					for (var i = 0; i < n; i++)
					{
						var depth = (i + 1) / 2;          // 0,1,1,2,2,3,3...
						var side = (i % 2 == 1) ? 1 : -1; // alternate right/left
						if (i == 0) { s[i] = WVec.Zero; }
						else s[i] = new WVec(-depth * sp, side * depth * sp, 0);
					}
					break;

				case Formation.Box:         // square block, integer grid
					var cols = Exts.ISqrt(n, Exts.ISqrtRoundMode.Ceiling);
					var rows = (n + cols - 1) / cols;
					for (var i = 0; i < n; i++)
					{
						var row = i / cols;
						var col = i % cols;
						s[i] = new WVec((2 * row - (rows - 1)) * -half, (2 * col - (cols - 1)) * half, 0);
					}
					break;

				case Formation.Echelon:     // diagonal stagger (right echelon)
					for (var i = 0; i < n; i++)
						s[i] = new WVec(-i * sp, i * sp, 0);
					break;

				default:                    // None -> all zero (caller won't reach here)
					for (var i = 0; i < n; i++) s[i] = WVec.Zero;
					break;
			}

			return s;
		}
	}
}
```

### How each per-unit Move order gets its offset target

It does **not** change resolution. `FormationOrderGenerator.OrderInner` builds `Target.FromCell(world, slotCells[i])` per unit, and `OrderForUnit` → `AttackMoveByDefault.IssueOrder` (`AttackMoveByDefault.cs:48-54`) mints the unit's `"AttackMove"` order with that per-unit target baked in. Resolution is the stock path, unchanged:
- `AttackMove.ResolveOrder` (`AttackMove.cs:82-99`): `Map.Clamp(CellContaining(target))` → `move.NearestMoveableCell(cell)` (`:93`) → `new AttackMoveActivity(self, () => move.MoveTo(targetLocation, 8, ...))` (`:97`). The `NearestMoveableCell` snap + `nearEnough:8` already deconflict slots that land on impassable/occupied cells. **Slot cells already separate the units**, so the `nearEnough:8` blob no longer collapses them into single file.
- Force-move (Alt) falls through to plain `Move` (`Mobile.cs:930-941`) — formation does not apply, which is correct (Alt = "ignore everything, just go there").

### File 4 (EDIT): `mods/theater/rules/theater-command.yaml:12-14`

Add the trait under the existing `Player:` block:
```yaml
Player:
	ArmyCommand:
	StrategicUpgrades:
	ActiveFormation:
		SpacingCells: 1
```

### File 5 (EDIT): `mods/theater/mod.yaml:282`

Point THEATER at the new generator:
```yaml
DefaultOrderGenerator: FormationOrderGenerator
```

### File 6 (EDIT): `OpenRA.Mods.Common/Commands/TheaterCommands.cs` — `/formation` chat command (keyboard test path)

Register alongside `/target` (`TheaterCommands.cs:46-47`, dispatch `:78-81`, handler clone of `SetTargetDoctrine` `:94-125`):
```csharp
console.RegisterCommand("formation", this);
console.RegisterCommand("form", this);
```
```csharp
case "formation":
case "form":
	SetFormation(arg);
	return;
```
```csharp
void SetFormation(string arg)
{
	var player = world.LocalPlayer;
	if (player?.PlayerActor.TraitOrDefault<ActiveFormation>() == null)
	{
		TextNotificationsManager.Debug("No formation control available (spectating?).");
		return;
	}

	int f;
	switch ((arg ?? "").Trim().ToLowerInvariant())
	{
		case "line": f = 1; break;
		case "column": case "col": f = 2; break;
		case "wedge": case "v": f = 3; break;
		case "box": case "square": f = 4; break;
		case "echelon": f = 5; break;
		case "none": case "off": case "": f = 0; break;
		default:
			TextNotificationsManager.Debug("Usage: /formation line | column | wedge | box | echelon | none");
			return;
	}

	world.IssueOrder(new Order(ActiveFormation.OrderName, player.PlayerActor, false) { ExtraData = (uint)f });
	TextNotificationsManager.Debug($"Formation: {(Formation)f}.");
}
```

---

## 3. WAR ROOM WIRING

Mirror `BindDoctrine` (`WarRoomLogic.cs:305-318`) exactly. The War Room logic already holds `world` and `player`. Add a formation-trait handle and a `BindFormations` block.

In `WarRoomLogic` setup (where `army`/`upgrades` are resolved):
```csharp
ActiveFormation formation;
// ...
formation = player?.PlayerActor.TraitOrDefault<ActiveFormation>();
```
Bindings (call from the panel init next to `BindDoctrines(w)`):
```csharp
void BindFormations(Widget w)
{
	BindFormation(w, "WR_FORM_LINE", 1);
	BindFormation(w, "WR_FORM_COLUMN", 2);
	BindFormation(w, "WR_FORM_WEDGE", 3);
	BindFormation(w, "WR_FORM_BOX", 4);
	BindFormation(w, "WR_FORM_ECHELON", 5);
}

void BindFormation(Widget w, string id, int index)
{
	var b = w.Get<ButtonWidget>(id);
	b.IsHighlighted = () => (int)(formation?.Active ?? Formation.None) == index;
	b.OnClick = () =>
	{
		if (player == null)
			return;

		// Toggle back to None if this formation is already active.
		var target = (int)(formation?.Active ?? Formation.None) == index ? 0 : index;
		world.IssueOrder(new Order(ActiveFormation.OrderName, player.PlayerActor, false) { ExtraData = (uint)target });
	};
}
```

**Chrome:** add five `ButtonWidget`s (`WR_FORM_LINE`/`_COLUMN`/`_WEDGE`/`_BOX`/`_ECHELON`) to the War Room layout YAML (the same chrome file that defines `WR_DOC_*` — find it with `grep -rln WR_DOC_STRUCT mods/theater/chrome`). Give each a `Text` key resolved through fluent.

**Fluent keys** (add to the THEATER `.ftl` that defines the `war-room-*` strings — `grep -rln WR_DOC mods/theater/languages` or the panel's existing fluent file):
```
war-room-formation-line = LINE
war-room-formation-column = COLUMN
war-room-formation-wedge = WEDGE
war-room-formation-box = BOX
war-room-formation-echelon = ECHELON
war-room-formation-line.tooltip = Move abreast — a firing line perpendicular to advance.
war-room-formation-column.tooltip = Move in single file along the advance — fast through narrow gaps.
war-room-formation-wedge.tooltip = Arrowhead — punch through and envelop.
war-room-formation-box.tooltip = Massed block — maximum local density.
war-room-formation-echelon.tooltip = Diagonal stagger — refused flank.
```

---

## 4. DETERMINISM CHECKLIST

Everything reached from `world.Tick()` must avoid float/double/Random/DateTime/wall-clock. This design keeps **all** geometry on the unsynced order-generation side and bakes only integer cells into synced orders.

| Concern | Why it is safe |
|---|---|
| **Slot math runs in `OrderInner` (unsynced UI)** | The generator runs only for the **local player**; its output is a set of `Order`s. The order *content* (per-unit `Target.FromCell(slotCell)`) is what crosses the network and is serialized identically. Each unit's offset rides its **own** order — no cross-client slot agreement needed. |
| **Slot ordering** | Movers sorted by `a.ActorID` (`OrderBy(a => a.ActorID)`) — network-stable, same on every client. **Never** trust raw `world.Selection.Actors` order (UI-local, HashSet-backed, unstable). |
| **Forward heading** | `WAngle.ArcTan(dy, dx)` (`WAngle.cs:130`) — integer fixed-point, table-based. No `Math.Atan2`. |
| **Rotation** | `WVec.Rotate(in WRot)` (`WVec.cs:49`) → `Int32Matrix4x4`, all `long`/`int`. No float. |
| **Centroid** | `AverageCenter` sums `WPos` (`int`) into `long`, divides by count — integer. |
| **Box grid sizing** | `Exts.ISqrt(n, Ceiling)` (`Exts.cs:302`) — integer sqrt. No `Math.Sqrt`. |
| **Quantise to cell** | `Map.CellContaining` + `Map.Clamp` — integer. |
| **Centering symmetry** | doubled-coordinate `2*i-(n-1)` with `SP/2` — avoids `(n-1)/2` integer truncation that would lopside odd counts. |
| **Slot dedup** | deterministic `HashSet<CPos>` add + fixed `+CVec(1,0)` nudge; no randomness. |
| **State storage** | `ActiveFormation.formation` is `[Sync]` (XORed into the frame hash, like `ArmyCommand.doctrine` `[VerifySync]`). |
| **Order application** | `ActiveFormation.ResolveOrder` and the per-unit `AttackMove.ResolveOrder` run on the synced path and do only integer clamp / `NearestMoveableCell` / `MoveTo` — all pre-existing, all integer. |
| **No grouped-order trap** | We emit **per-unit** orders, sidestepping `Order.FromGroupedOrder` (`Order.cs:277`) sharing one Target. If you ever switch to a single grouped order, you MUST sort `order.GroupedActors` by `ActorID` inside `ResolveOrder` and recompute offsets there. |

**Forbidden in this path (verify none appear):** `float`, `double`, `decimal`, `Math.Atan2`/`Sqrt`/`Sin`/`Cos`, `Random`/`MersenneTwister`/`World.LocalRandom`, `DateTime`/`Stopwatch`. The AI squad code uses `World.LocalRandom` — do **not** copy that into the formation path.

---

## 5. VERIFICATION

### Build / lint (headless — these DO work here)
```bash
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
dotnet build OpenRA.sln -c Debug          # analyzer-checked build of your new files
./utility.sh theater --check-yaml         # validates ActiveFormation trait + theater-command.yaml + mod.yaml
```
Grep the Debug build warnings for your filenames (`ActiveFormation`, `FormationOrderGenerator`, `FormationLayout`) to confirm *your* code is analyzer-clean. (Per CLAUDE.md, `make check` is RED on this machine for unrelated stock-file IDE0055 noise — judge cleanliness by your filenames only.)

### Skirmish (the real test — GUI, CANNOT be done headlessly)
```bash
./theater-play.sh
```
1. Start a skirmish, build/select **8+ ground units** (mix of tanks + infantry to exercise mixed locomotors).
2. Open the War Room (`/warroom` or select the Theater Command). Click **WEDGE**. Confirm the button highlights.
   - Keyboard alternative: `/formation wedge`. Expect the debug line `Formation: Wedge.`
3. Right-click a distant open spot. **Observe:** units splay into a V (tip toward the target) and advance arranged — not a single-file conga line.
4. Cycle **LINE / COLUMN / BOX / ECHELON**; right-click each time. Confirm the *shape rotates to face the move direction* (move north vs east → the shape reorients).
5. **The money test:** put a 1-cell-wide chokepoint (cliff gap / wall gap) between the group and the target. With formation **off** vs **on**, compare. v1 success = units arrive *pre-spread into the shape on the far side* and stop feeding in one-at-a-time. (They still narrow to pass the actual 1-wide gap — that's physics, not a bug; the win is they don't *string out for the whole approach*.)
6. Pick **NONE** (or `/formation none`) → confirm exact stock behavior returns (loose blob within 8 cells of the click).
7. Hold **Alt** while right-clicking → confirm plain move (no shape) still works.
8. Auto-engage check: move the formation past an enemy → units peel to fire (attack-move default) then re-form toward their slots. Confirm this still solves the funnel.

### What CANNOT be verified headlessly
- That units **visually** move in the shape and that the chokepoint single-file problem is gone — movement is GUI-only here. The build and YAML lint prove the wiring compiles and the trait/order plumbing is valid; only `./theater-play.sh` proves the *feel*.
- Multiplayer desync is not observable in single-player; the determinism checklist is the guarantee. If paranoid, run a 2-client LAN game and watch for a sync-error report after issuing several formation moves.

---

## 6. RISKS & FALLBACKS

| # | Risk | Mitigation / Fallback |
|---|---|---|
| 1 | **AttackMoveByDefault swallows the click before our offsets apply.** | It does NOT — its targeter runs *inside* `OrderInner`, which we override; we set each unit's per-unit `target` before `OrderForUnit`/`IssueOrder` fire. Verified at `UnitOrderGenerator.cs:68-71` + `AttackMoveByDefault.cs:48`. If a future change moved that targeter, fallback is to also intercept `AttackMoveOrderGenerator.OrderInner` (`AttackMove.cs:118-129`). |
| 2 | **Slot cells land on impassable/occupied terrain.** | Already handled downstream: `AttackMove.ResolveOrder` calls `move.NearestMoveableCell(cell)` (`AttackMove.cs:93`, radius-10 annulus) and `nearEnough:8`. Our `HashSet` dedup prevents two units sharing a quantised cell. |
| 3 | **Mixed locomotors** (tank slot on water, infantry slot on cliff). | Each unit snaps its *own* slot via *its* `NearestMoveableCell`, so impassable slots degrade per-unit. Fallback if ugly: shape only the dominant locomotor and let the rest use the anchor (already the structure — non-`MobileInfo` actors fall through to base target). |
| 4 | **Base helpers private** (`OrderForUnit`, `CheckSameOrder` at `UnitOrderGenerator.cs:69/82`). | One-line visibility widening to `protected` (behavior-identical). Fallback: inline `CheckSameOrder` and query `IIssueOrder` directly in the subclass for **zero** shared-file edits. |
| 5 | **No en-route cohesion / no slowest-unit pacing** (Approach A's honest gap). | Accepted for v1 — distinct slots already kill single-file. Follow-on (Approach C): at order time compute `min(Info.Speed)` over `movers` and grant faster units a capped `ISpeedModifier` condition (reuse `SpeedMultiplier`, cf. `theater-command.yaml:35`). Engine has **no** native group pacing (`Mobile.cs:748`), so this is the only deterministic route. |
| 6 | **Mid-move attrition doesn't re-pack** the shape. | Documented limitation: formations are computed per-move-order, not maintained. Re-issuing the move recomputes for the survivors. Full continuous maintenance = Approach B, deferred. |
| 7 | **Queued (Shift) waypoints reuse the first anchor's offsets.** | For v1, each Shift-click recomputes a fresh anchor/heading/centroid at that click (it's a new `OrderInner` call), so per-waypoint offsets are correct. The only subtlety is the centroid uses *current* positions, not projected ones — acceptable for v1. |
| 8 | **Tight spacing + diagonal heading collides two slots after quantisation.** | `HashSet` dedup nudge + `NearestMoveableCell`. Keep `SpacingCells >= 1`. |

**Simplest end-to-end fallback if the subclass route proves too deep:** ship only `ActiveFormation` + `/formation` + the War Room buttons (state plumbing, fully testable), and implement the geometry as the smallest possible special-case first — **Line only** — to validate the offset/rotate/snap pipeline visually in a skirmish before adding Wedge/Box/Echelon. The geometry is the only novel code; everything else is a verified clone of `ArmyCommand`/`BindDoctrine`.

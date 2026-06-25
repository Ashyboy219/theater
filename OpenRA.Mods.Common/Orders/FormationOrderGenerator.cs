#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	// THEATER: a UnitOrderGenerator that, when the local player owns the Formations capability and has a
	// shape selected, gives each selected mobile unit its OWN destination cell arranged in the chosen
	// shape (rotated to the travel axis) instead of sending the whole group to a single cell — fixing the
	// single-file funnel on group moves. All slot maths runs at order-generation time on the LOCAL client
	// using integers; the only thing that crosses the network is the resulting per-unit move orders, so no
	// new code runs inside the simulation tick and there is no new desync surface. With no shape selected
	// (the default) it behaves exactly like UnitOrderGenerator.
	public class FormationOrderGenerator : UnitOrderGenerator
	{
		static readonly string[] FormationsPrerequisite = ["cap.formations"];

		public FormationOrderGenerator(World world)
			: base(world) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!TryBuildFormation(world, cell, worldPixel, mi, out var units, out var slots))
			{
				foreach (var o in base.OrderInner(world, cell, worldPixel, mi))
					yield return o;

				yield break;
			}

			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);

			// Mirror the base APM-hack CreateGroup over the local selection.
			var involved = world.Selection.Actors.Where(a => a.Owner == world.LocalPlayer).ToArray();
			if (involved.Length > 0)
				yield return new Order("CreateGroup", involved[0].Owner.PlayerActor, false, involved);

			// Formation movers -> their individual slot cells.
			var formationSet = new HashSet<Actor>(units);
			for (var i = 0; i < units.Count; i++)
			{
				var result = OrderForUnit(units[i], Target.FromCell(world, slots[i]), slots[i], mi);
				if (result != null)
					yield return result.Trait.IssueOrder(result.Actor, result.Order, result.Target, queued);
			}

			// Anything else in the selection keeps its normal single-target order.
			var target = TargetForInput(world, cell, worldPixel, mi);
			foreach (var a in world.Selection.Actors)
			{
				if (formationSet.Contains(a))
					continue;

				var result = OrderForUnit(a, target, cell, mi);
				if (result != null)
					yield return result.Trait.IssueOrder(result.Actor, result.Order, result.Target, queued);
			}
		}

		bool TryBuildFormation(World world, CPos cell, int2 worldPixel, MouseInput mi, out List<Actor> units, out List<CPos> slots)
		{
			units = null;
			slots = null;

			var player = world.LocalPlayer;
			if (player == null)
				return false;

			var state = world.WorldActor.TraitOrDefault<FormationState>();
			if (state == null || state.Shape == FormationShape.None)
				return false;

			// Force-attack (Ctrl) is a fire order; clicking an actor is a target order. Neither forms up.
			if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
				return false;

			var target = TargetForInput(world, cell, worldPixel, mi);
			if (target.Type != TargetType.Terrain)
				return false;

			var techTree = player.PlayerActor.TraitOrDefault<TechTree>();
			if (techTree == null || !techTree.HasPrerequisites(FormationsPrerequisite))
				return false;

			// Deterministic ordering: Selection.Actors is a HashSet (unstable iteration), so sort by ActorID.
			var movers = world.Selection.Actors
				.Where(a => a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<MobileInfo>())
				.OrderBy(a => a.ActorID)
				.ToList();

			if (movers.Count < 2)
				return false;

			units = movers;
			slots = BuildSlots(world, movers, cell, state.Shape);
			return true;
		}

		static List<CPos> BuildSlots(World world, List<Actor> units, CPos baseCell, FormationShape shape)
		{
			var n = units.Count;

			// Travel direction (group centroid -> destination), snapped to the nearest cardinal so the
			// offset rotation is an exact integer transform (no Cos/Sin truncation).
			var sx = 0;
			var sy = 0;
			foreach (var u in units)
			{
				sx += u.Location.X;
				sy += u.Location.Y;
			}

			var dx = baseCell.X - sx / n;
			var dy = baseCell.Y - sy / n;

			// 0 = north (-Y forward), 1 = east (+X), 2 = south (+Y), 3 = west (-X).
			int facing;
			if (Math.Abs(dx) >= Math.Abs(dy))
				facing = dx >= 0 ? 1 : 3;
			else
				facing = dy >= 0 ? 2 : 0;

			var offsets = ShapeOffsets(shape, n);
			var slots = new List<CPos>(n);
			for (var i = 0; i < n; i++)
			{
				var slot = world.Map.Clamp(baseCell + Rotate(offsets[i], facing));

				// Fallback: if a unit can't traverse its slot cell, send it to the clicked cell instead.
				var mobile = units[i].TraitOrDefault<Mobile>();
				if (mobile != null && !mobile.CanEnterCell(slot, null, BlockedByActor.None))
					slot = baseCell;

				slots.Add(slot);
			}

			return slots;
		}

		// Offsets in formation-local space: forward = -Y, width = X, lead/centre near (0,0).
		static List<CVec> ShapeOffsets(FormationShape shape, int n)
		{
			var list = new List<CVec>(n);
			switch (shape)
			{
				case FormationShape.Column:
					for (var i = 0; i < n; i++)
						list.Add(new CVec(0, i));

					break;

				case FormationShape.Wedge:
				{
					// THEATER: a COMPACT FILLED triangle (arrowhead), not an edge-only V. Row r (r = 0 at the
					// leading apex, increasing toward the back at +Y) holds up to 2*r+1 units on a TIGHT 1-cell
					// pitch, centred on the travel axis and filled centre-out (0, -1, +1, -2, +2, ...) so a
					// partial final row stays balanced. Result: 1,3,5,... stacked rows that keep the group dense
					// and inside weapon range instead of the old hollow V that flung units onto the two arms.
					var placed = 0;
					for (var row = 0; placed < n; row++)
					{
						for (var c = 0; c <= row && placed < n; c++)
						{
							list.Add(new CVec(-c, row));
							placed++;
							if (c > 0 && placed < n)
							{
								list.Add(new CVec(c, row));
								placed++;
							}
						}
					}

					break;
				}

				case FormationShape.Box:
					var cols = 1;
					while (cols * cols < n)
						cols++;

					var boxStart = -(cols - 1) / 2;
					for (var i = 0; i < n; i++)
						list.Add(new CVec(boxStart + (i % cols), i / cols));

					break;

				case FormationShape.Line:
				default:
					var lineStart = -(n - 1) / 2;
					for (var i = 0; i < n; i++)
						list.Add(new CVec(lineStart + i, 0));

					break;
			}

			return list;
		}

		// Exact integer rotation of a local offset onto the cardinal travel direction.
		static CVec Rotate(CVec v, int facing)
		{
			switch (facing)
			{
				case 1: return new CVec(-v.Y, v.X);   // east  (+X forward)
				case 2: return new CVec(-v.X, -v.Y);  // south (+Y forward)
				case 3: return new CVec(v.Y, -v.X);   // west  (-X forward)
				default: return v;                    // north (-Y forward)
			}
		}
	}
}

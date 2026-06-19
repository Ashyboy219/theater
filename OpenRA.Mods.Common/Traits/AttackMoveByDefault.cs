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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Makes the plain move order (right-click on open ground) behave like attack-move: the unit ",
		"engages enemies it meets on the way while still heading to the destination — no separate ",
		"attack-move keybind needed. Hold the force-move modifier (Alt) for a plain move that ignores ",
		"enemies, or force-attack (Ctrl) to fire on the ground. Requires the AttackMove trait.")]
	public class AttackMoveByDefaultInfo : TraitInfo, Requires<AttackMoveInfo>
	{
		[Desc("Order issued in place of a plain \"Move\" for the default ground click. ",
			"Must be an order the actor's AttackMove trait resolves (AttackMove or AssaultMove).")]
		public readonly string OrderName = "AttackMove";

		[Desc("Targeter priority. Must be higher than Mobile's Move targeter (4) so this takes precedence ",
			"for the default ground click.")]
		public readonly int Priority = 5;

		public override object Create(ActorInitializer init) { return new AttackMoveByDefault(this); }
	}

	public class AttackMoveByDefault : IIssueOrder
	{
		readonly AttackMoveByDefaultInfo info;

		public AttackMoveByDefault(AttackMoveByDefaultInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new AttackMoveDefaultTargeter(info.OrderName, info.Priority); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is AttackMoveDefaultTargeter)
				return new Order(info.OrderName, self, target, queued);

			return null;
		}

		sealed class AttackMoveDefaultTargeter : IOrderTargeter
		{
			public AttackMoveDefaultTargeter(string orderID, int priority)
			{
				OrderID = orderID;
				OrderPriority = priority;
			}

			public string OrderID { get; }
			public int OrderPriority { get; }
			public bool IsQueued { get; private set; }

			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				// Mirror Mobile.MoveOrderTargeter so group-click behaviour is unchanged.
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				// Only take over the plain ground-move click. Force-move (Alt) and force-attack (Ctrl)
				// fall through to the normal Move / Attack handlers so the player keeps manual control.
				if (target.Type != TargetType.Terrain
					|| modifiers.HasModifier(TargetModifiers.ForceMove)
					|| modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				if (!self.World.Map.Contains(location))
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				cursor = self.Owner.Shroud.IsExplored(location) ? "attackmove" : "attackmove-blocked";
				return true;
			}
		}
	}
}

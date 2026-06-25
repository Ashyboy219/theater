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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Marks this actor as a node in the territory supply network (THEATER Phase 0 prototype).",
		"A node is 'supplied' while it can be traced back to an owned source node through a chain of",
		"owned nodes within LinkRange. While supplied, the configured condition is granted (gate income",
		"or other behaviour on it). Connectivity is recomputed by the world-level SupplyNetwork manager.")]
	public class SupplyNodeInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("Condition granted while this node is supply-connected. Leave empty for pure source/relay nodes (e.g. a base anchor).")]
		public readonly string SuppliedCondition = null;

		[Desc("Maximum distance to a neighbouring owned node for a supply link to form.")]
		public readonly WDist LinkRange = WDist.FromCells(10);

		[Desc("Source nodes originate supply: while owned by a combatant they are always supplied,",
			"and seed connectivity for the rest of the owner's network. Put this on the base/Construction Yard.")]
		public readonly bool IsSource = false;

		public override object Create(ActorInitializer init) { return new SupplyNode(this); }
	}

	public class SupplyNode : INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged
	{
		public readonly SupplyNodeInfo Info;

		SupplyNetwork network;
		int conditionToken = Actor.InvalidConditionToken;

		public bool IsSource => Info.IsSource;
		public WDist LinkRange => Info.LinkRange;
		public bool Supplied { get; private set; }

		public SupplyNode(SupplyNodeInfo info) { Info = info; }

		void INotifyCreated.Created(Actor self)
		{
			network = self.World.WorldActor.TraitOrDefault<SupplyNetwork>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			network?.Add(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			network?.Remove(self, this);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Ownership defines the network grouping; drop supply until the next recompute confirms reconnection.
			SetSupplied(self, false);
			network?.QueueRecompute();
		}

		public void SetSupplied(Actor self, bool value)
		{
			if (value == Supplied)
				return;

			Supplied = value;

			if (string.IsNullOrEmpty(Info.SuppliedCondition))
				return;

			if (Supplied && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.SuppliedCondition);
			else if (!Supplied && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}

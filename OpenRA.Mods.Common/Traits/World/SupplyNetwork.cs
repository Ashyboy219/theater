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
	[TraitLocation(SystemActors.World)]
	[Desc("Computes territory supply connectivity for SupplyNode actors (THEATER Phase 0 prototype).",
		"Each owner's nodes are flood-filled outward from their owned source nodes; any node reachable",
		"through a chain of owned nodes within link range is marked supplied. Cutting the chain (capturing",
		"or destroying a relaying node) strands everything downstream. Deterministic: integer math only.")]
	public class SupplyNetworkInfo : TraitInfo
	{
		[Desc("Ticks between connectivity recomputations.")]
		public readonly int Interval = 25;

		public override object Create(ActorInitializer init) { return new SupplyNetwork(this); }
	}

	public class SupplyNetwork : ITick
	{
		readonly SupplyNetworkInfo info;

		// Parallel lists keep a deterministic registration order (no hashed-container iteration in the sim path).
		readonly List<Actor> actors = [];
		readonly List<SupplyNode> nodes = [];

		int ticks;

		public SupplyNetwork(SupplyNetworkInfo info)
		{
			this.info = info;
			ticks = 0;
		}

		public void Add(Actor actor, SupplyNode node)
		{
			actors.Add(actor);
			nodes.Add(node);
			ticks = 0; // recompute promptly when the topology changes
		}

		public void Remove(Actor actor, SupplyNode node)
		{
			var i = actors.IndexOf(actor);
			if (i < 0)
				return;

			actors.RemoveAt(i);
			nodes.RemoveAt(i);
			ticks = 0;
		}

		public void QueueRecompute()
		{
			ticks = 0;
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks > 0)
				return;

			ticks = info.Interval;
			Recompute();
		}

		void Recompute()
		{
			var count = actors.Count;
			if (count == 0)
				return;

			// supplied[i] is the freshly computed state for node i this pass.
			var supplied = new bool[count];

			// Seed: owned source nodes (combatant owner) originate supply.
			var frontier = new Queue<int>();
			for (var i = 0; i < count; i++)
			{
				if (nodes[i].IsSource && !actors[i].Owner.NonCombatant)
				{
					supplied[i] = true;
					frontier.Enqueue(i);
				}
			}

			// Flood outward: a node joins the network if it shares an owner with an already-supplied node
			// and lies within either node's link range. O(n^2) over a handful of nodes — cheap and exact.
			while (frontier.Count > 0)
			{
				var i = frontier.Dequeue();
				var from = actors[i];

				for (var j = 0; j < count; j++)
				{
					if (supplied[j])
						continue;

					var to = actors[j];
					if (to.Owner != from.Owner)
						continue;

					var maxRange = nodes[i].LinkRange.Length > nodes[j].LinkRange.Length
						? nodes[i].LinkRange : nodes[j].LinkRange;

					if ((to.CenterPosition - from.CenterPosition).HorizontalLengthSquared <= maxRange.LengthSquared)
					{
						supplied[j] = true;
						frontier.Enqueue(j);
					}
				}
			}

			for (var i = 0; i < count; i++)
				nodes[i].SetSupplied(actors[i], supplied[i]);
		}
	}
}

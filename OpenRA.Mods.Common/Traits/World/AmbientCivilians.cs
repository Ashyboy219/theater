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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Populates the map with ambient, wandering civilians at game start for a \"living world\" feel.",
		"Deterministic: uses only the synced world RNG and spawns via a frame-end task, so it is desync-safe.",
		"The spawned civilian actor types should carry a Wanders trait so they actually move around.")]
	public class AmbientCiviliansInfo : TraitInfo
	{
		[ActorReference]
		[Desc("Civilian actor types to spawn (one is chosen at random per civilian).")]
		public readonly string[] Types = System.Array.Empty<string>();

		[Desc("How many civilians to spawn at game start.")]
		public readonly int Count = 0;

		[Desc("Terrain types civilians may spawn on.")]
		public readonly HashSet<string> ValidTerrain = ["Clear", "Road", "Rough", "Beach"];

		[Desc("Maximum attempts to find a valid spawn cell per civilian before giving up on that one.")]
		public readonly int MaxTries = 50;

		public override object Create(ActorInitializer init) { return new AmbientCivilians(this); }
	}

	public class AmbientCivilians : ITick
	{
		readonly AmbientCiviliansInfo info;
		bool spawned;

		public AmbientCivilians(AmbientCiviliansInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (spawned)
				return;

			spawned = true;

			if (info.Count <= 0 || info.Types.Length == 0)
				return;

			// Spawn from a frame-end task so we don't mutate the actor collection mid-tick. All randomness
			// uses the synced world RNG, so every client produces an identical population (no desync).
			self.World.AddFrameEndTask(w =>
			{
				var owner = w.WorldActor.Owner;
				for (var i = 0; i < info.Count; i++)
				{
					var cell = ChooseCell(w);
					if (cell == null)
						continue;

					var type = info.Types[w.SharedRandom.Next(info.Types.Length)];
					w.CreateActor(type, [new OwnerInit(owner), new LocationInit(cell.Value)]);
				}
			});
		}

		CPos? ChooseCell(World world)
		{
			for (var n = 0; n < info.MaxTries; n++)
			{
				var p = world.Map.ChooseRandomCell(world.SharedRandom);

				if (!info.ValidTerrain.Contains(world.Map.GetTerrainInfo(p).Type))
					continue;

				if (world.ActorMap.GetActorsAt(p).Any())
					continue;

				return p;
			}

			return null;
		}
	}
}

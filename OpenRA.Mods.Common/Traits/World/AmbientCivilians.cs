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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Populates the map with ambient, wandering civilians for a \"living world\" feel, and grows the",
		"population as the GameTimeline advances through its eras — \"Civilization but real-time\": the world",
		"fills with more life as time moves forward, and war casualties are replenished up to the target.",
		"Deterministic: uses only the synced world RNG and spawns via a frame-end task, so it is desync-safe.",
		"The spawned civilian actor types should carry a Wanders trait so they actually move around.")]
	public class AmbientCiviliansInfo : TraitInfo
	{
		[ActorReference]
		[Desc("Civilian actor types to spawn (one is chosen at random per civilian).")]
		public readonly string[] Types = [];

		[Desc("Baseline population — the target number of living civilians from game start.")]
		public readonly int Count = 0;

		[Desc("How much the target population grows for each era the world's GameTimeline advances,",
			"so the map gets busier as the match goes on. 0 keeps the population flat. Requires a GameTimeline",
			"on the world; with none, the population is simply seeded once at Count (the classic behaviour).")]
		public readonly int GrowthPerEra = 0;

		[Desc("Hard cap on the target population regardless of era. 0 means no cap.")]
		public readonly int MaxPopulation = 0;

		[Desc("Terrain types civilians may spawn on.")]
		public readonly HashSet<string> ValidTerrain = ["Clear", "Road", "Rough", "Beach"];

		[Desc("Maximum attempts to find a valid spawn cell per civilian before giving up on that one.")]
		public readonly int MaxTries = 50;

		public override object Create(ActorInitializer init) { return new AmbientCivilians(this); }
	}

	public class AmbientCivilians : INotifyCreated, ITick
	{
		readonly AmbientCiviliansInfo info;
		readonly List<Actor> civilians = [];

		GameTimeline timeline;
		int lastEra = -1;

		public AmbientCivilians(AmbientCiviliansInfo info)
		{
			this.info = info;
		}

		// Live count of the civilians this trait is keeping alive, for a HUD readout. Read-only (no mutation),
		// so it is safe to call from the unsynced render tick — it never touches the synced spawn bookkeeping.
		public int Population => civilians.Count(a => a.IsInWorld && !a.IsDead);

		void INotifyCreated.Created(Actor self)
		{
			// Optional: drives population growth. If absent, the population is seeded once and never grows.
			// `self` IS the world actor (this trait lives on SystemActors.World, as does GameTimeline), so we
			// query it directly — self.World.WorldActor isn't assigned yet while the world actor is initialising.
			timeline = self.TraitOrDefault<GameTimeline>();
		}

		void ITick.Tick(Actor self)
		{
			if (info.Types.Length == 0 || info.Count <= 0)
				return;

			// Only re-evaluate the population when the world reaches a new era (and once at the very start,
			// since lastEra begins at -1). Between eras the existing wandering civilians just live their lives.
			var era = timeline?.CurrentEra ?? 0;
			if (era == lastEra)
				return;

			lastEra = era;

			// The target swells with each era; war casualties below it are replenished at the next era boundary.
			var target = info.Count + (info.GrowthPerEra > 0 ? info.GrowthPerEra * era : 0);
			if (info.MaxPopulation > 0)
				target = Math.Min(target, info.MaxPopulation);

			// Forget civilians that have died or left the world so the deficit reflects the living population.
			civilians.RemoveAll(a => a.IsDead || !a.IsInWorld);

			var deficit = target - civilians.Count;
			if (deficit <= 0)
				return;

			// Spawn from a frame-end task so we don't mutate the actor collection mid-tick. All randomness
			// uses the synced world RNG, so every client produces an identical population (no desync).
			self.World.AddFrameEndTask(w =>
			{
				var owner = w.WorldActor.Owner;
				for (var i = 0; i < deficit; i++)
				{
					var cell = ChooseCell(w);
					if (cell == null)
						continue;

					var type = info.Types[w.SharedRandom.Next(info.Types.Length)];
					civilians.Add(w.CreateActor(type, [new OwnerInit(owner), new LocationInit(cell.Value)]));
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

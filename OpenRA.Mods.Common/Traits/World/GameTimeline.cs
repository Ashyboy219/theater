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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("A global real-time timeline that advances through named eras as the match goes on, for a",
		"\"Civilization but real-time\" sense of time moving forward. Distinct from the player-bought ages:",
		"this is the world's shared clock. Deterministic (tick-counted, no RNG). Read by a HUD readout.")]
	public class GameTimelineInfo : TraitInfo
	{
		[Desc("Era names, in order. The first is active from the start.")]
		public readonly string[] Eras = ["Mobilization", "Escalation", "Open Conflict", "Total War", "Final Hour"];

		[Desc("Game tick at which each era begins (same length as Eras; the first is usually 0).",
			"At the default game speed there are ~25 ticks per second, so 9000 ticks ~= 6 minutes.")]
		public readonly int[] EraTicks = [0, 9000, 18000, 27000, 36000];

		[Desc("Announce each era transition as a system message.")]
		public readonly bool Announce = true;

		public override object Create(ActorInitializer init) { return new GameTimeline(this); }
	}

	public class GameTimeline : ITick
	{
		readonly GameTimelineInfo info;

		public int Ticks { get; private set; }
		public int CurrentEra { get; private set; }

		public string CurrentEraName => info.Eras.Length > 0
			? info.Eras[Math.Min(CurrentEra, info.Eras.Length - 1)]
			: "";

		public GameTimeline(GameTimelineInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			Ticks++;

			var next = CurrentEra + 1;
			if (next < info.Eras.Length && next < info.EraTicks.Length && Ticks >= info.EraTicks[next])
			{
				CurrentEra = next;

				if (info.Announce)
					TextNotificationsManager.AddSystemLine($"The {info.Eras[CurrentEra]} era has begun.");
			}
		}
	}
}

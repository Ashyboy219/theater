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
	[TraitLocation(SystemActors.Player)]
	[Desc("Grants prerequisites as the global GameTimeline advances through its eras, so content can be",
		"gated on how far the match has progressed in real time (the world's timeline, not the player's tech).",
		"Prerequisites are granted cumulatively: the i-th token unlocks once the current era reaches i+1.")]
	public class ProvidesPrerequisiteOnEraInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Prerequisite tokens, one per era boundary. Prerequisites[0] is granted at era 1, [1] at era 2, etc.")]
		public readonly string[] Prerequisites = [];

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return Prerequisites;
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteOnEra(this); }
	}

	public class ProvidesPrerequisiteOnEra : ITechTreePrerequisite, INotifyCreated, ITick
	{
		readonly ProvidesPrerequisiteOnEraInfo info;

		TechTree techTree;
		GameTimeline timeline;
		int granted;

		public ProvidesPrerequisiteOnEra(ProvidesPrerequisiteOnEraInfo info)
		{
			this.info = info;
		}

		public IEnumerable<string> ProvidesPrerequisites => info.Prerequisites.Take(granted);

		void INotifyCreated.Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.Trait<TechTree>();
			timeline = self.World.WorldActor.TraitOrDefault<GameTimeline>();
		}

		void ITick.Tick(Actor self)
		{
			if (timeline == null || info.Prerequisites.Length == 0)
				return;

			// Cumulative: at era N, the first N tokens are granted (capped at the list length).
			var shouldGrant = Math.Min(timeline.CurrentEra, info.Prerequisites.Length);
			if (shouldGrant != granted)
			{
				granted = shouldGrant;
				techTree.ActorChanged(self);
			}
		}
	}
}

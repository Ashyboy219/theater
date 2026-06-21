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
	[Desc("Deducts alloys from the owner when this actor is built (added to the world). ",
		"Gate the actor's production on a ProvidesPrerequisiteOnAlloys prerequisite whose Threshold matches this Cost, ",
		"so it can only be built with enough banked alloys and the stockpile is spent on completion.")]
	public class ConsumesAlloysInfo : TraitInfo
	{
		[Desc("Alloys deducted when this actor is built.")]
		public readonly int Cost = 1;

		public override object Create(ActorInitializer init) { return new ConsumesAlloys(this); }
	}

	public class ConsumesAlloys : INotifyAddedToWorld
	{
		readonly ConsumesAlloysInfo info;

		public ConsumesAlloys(ConsumesAlloysInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (info.Cost <= 0)
				return;

			// Inert unless the owner runs the alloy economy (THEATER); other mods have no PlayerAlloys.
			var alloys = self.Owner.PlayerActor.TraitOrDefault<PlayerAlloys>();
			alloys?.TakeAlloys(info.Cost);
		}
	}
}

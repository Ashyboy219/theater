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
	[Desc("Declares an alloy cost for this actor. The ProductionQueue charges it up front when the actor is",
		"queued (and the alloys.stockpile prerequisite revokes once the stockpile is spent). Pair with a",
		"ProvidesPrerequisiteOnAlloys gate whose Threshold matches this Cost. Alloys are committed on build",
		"and not refunded on cancel.")]
	public class ConsumesAlloysInfo : TraitInfo
	{
		[Desc("Alloys charged when this actor is queued for production.")]
		public readonly int Cost = 1;

		public override object Create(ActorInitializer init) { return new ConsumesAlloys(); }
	}

	// Marker trait: the alloy cost is read from ConsumesAlloysInfo and charged by the ProductionQueue at
	// queue time, so the runtime trait carries no behaviour itself.
	public class ConsumesAlloys { }
}

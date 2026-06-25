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
	[TraitLocation(SystemActors.Player)]
	[Desc("Grants a prerequisite to the player while they hold at least a threshold of alloys. ",
		"Gate alloy-costed actors on this prerequisite so they can only be queued with enough banked alloys.")]
	public class ProvidesPrerequisiteOnAlloysInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[FieldLoader.Require]
		[Desc("The prerequisite type granted while the threshold is met.")]
		public readonly string Prerequisite = null;

		[Desc("Grant the prerequisite while the player holds at least this many alloys.")]
		public readonly int Threshold = 1;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return [Prerequisite];
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteOnAlloys(this); }
	}

	public class ProvidesPrerequisiteOnAlloys : ITechTreePrerequisite, INotifyCreated, INotifyAlloysChanged, INotifyOwnerChanged
	{
		readonly ProvidesPrerequisiteOnAlloysInfo info;
		readonly string[] prerequisites;

		bool enabled;
		TechTree techTree;
		PlayerAlloys alloys;

		public ProvidesPrerequisiteOnAlloys(ProvidesPrerequisiteOnAlloysInfo info)
		{
			this.info = info;
			prerequisites = [info.Prerequisite];
		}

		public IEnumerable<string> ProvidesPrerequisites => enabled ? prerequisites : Enumerable.Empty<string>();

		void INotifyCreated.Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.Trait<TechTree>();
			alloys = self.Owner.PlayerActor.Trait<PlayerAlloys>();
			enabled = alloys.Alloys >= info.Threshold;
		}

		void INotifyAlloysChanged.AlloysChanged(Actor self)
		{
			var shouldBeEnabled = alloys.Alloys >= info.Threshold;
			if (shouldBeEnabled == enabled)
				return;

			enabled = shouldBeEnabled;
			techTree.ActorChanged(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			alloys = newOwner.PlayerActor.Trait<PlayerAlloys>();
			enabled = alloys.Alloys >= info.Threshold;
			techTree.ActorChanged(self);
		}
	}
}

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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface INotifyAlloysChanged
	{
		void AlloysChanged(Actor self);
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks a per-player counted strategic resource (\"alloys\"), separate from cash. ",
		"Drives THEATER's 4X economy: produced by AlloyTrickler, gated by ProvidesPrerequisiteOnAlloys, spent by ConsumesAlloys. ",
		"Inert for mods that do not add the supporting traits.")]
	public class PlayerAlloysInfo : TraitInfo
	{
		[Desc("Alloys the player starts with.")]
		public readonly int InitialAlloys = 0;

		[Desc("Maximum alloys the player can stockpile (0 = unlimited).")]
		public readonly int MaxAlloys = 0;

		public override object Create(ActorInitializer init) { return new PlayerAlloys(this); }
	}

	public class PlayerAlloys : ISync, INotifyCreated
	{
		readonly PlayerAlloysInfo info;

		INotifyAlloysChanged[] notifyAlloysChanged = [];
		Actor playerActor;

		[VerifySync]
		public int Alloys;

		public int Earned;
		public int Spent;

		public PlayerAlloys(PlayerAlloysInfo info)
		{
			this.info = info;
			Alloys = info.InitialAlloys;
		}

		void INotifyCreated.Created(Actor self)
		{
			playerActor = self;
			notifyAlloysChanged = self.TraitsImplementing<INotifyAlloysChanged>().ToArray();
		}

		public bool CanGiveAlloys(int amount)
		{
			return info.MaxAlloys <= 0 || Alloys + amount <= info.MaxAlloys;
		}

		public void GiveAlloys(int amount)
		{
			if (amount <= 0)
				return;

			Alloys += amount;
			Earned += amount;

			if (info.MaxAlloys > 0 && Alloys > info.MaxAlloys)
			{
				Earned -= Alloys - info.MaxAlloys;
				Alloys = info.MaxAlloys;
			}

			NotifyChanged();
		}

		public bool CanTakeAlloys(int amount)
		{
			return Alloys >= amount;
		}

		public bool TakeAlloys(int amount)
		{
			if (Alloys < amount)
				return false;

			Alloys -= amount;
			Spent += amount;
			NotifyChanged();
			return true;
		}

		void NotifyChanged()
		{
			foreach (var n in notifyAlloysChanged)
				n.AlloysChanged(playerActor);
		}
	}
}

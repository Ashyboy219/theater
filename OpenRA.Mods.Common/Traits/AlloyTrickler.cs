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

using System.Globalization;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Periodically adds counted alloys (the strategic resource) to the owner. Put on a territory/economy building.")]
	public class AlloyTricklerInfo : PausableConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Number of ticks to wait between giving alloys.")]
		public readonly int Interval = 60;

		[Desc("Number of ticks to wait before giving the first alloys.")]
		public readonly int InitialDelay = 0;

		[Desc("Amount of alloys to give each interval.")]
		public readonly int Amount = 10;

		[Desc("Whether to show the alloy tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long to show the alloy tick indicator when enabled.")]
		public readonly int DisplayDuration = 30;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (ShowTicks && !info.HasTraitInfo<IOccupySpaceInfo>())
				throw new YamlException($"AlloyTrickler is defined with ShowTicks 'true' but actor '{info.Name}' occupies no space.");
		}

		public override object Create(ActorInitializer init) { return new AlloyTrickler(this); }
	}

	public class AlloyTrickler : PausableConditionalTrait<AlloyTricklerInfo>, ITick, ISync, INotifyOwnerChanged
	{
		readonly AlloyTricklerInfo info;
		PlayerAlloys alloys;

		[VerifySync]
		public int Ticks { get; private set; }

		public AlloyTrickler(AlloyTricklerInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.InitialDelay;
		}

		protected override void Created(Actor self)
		{
			alloys = self.Owner.PlayerActor.Trait<PlayerAlloys>();
			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			alloys = newOwner.PlayerActor.Trait<PlayerAlloys>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				Ticks = info.Interval;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--Ticks < 0)
			{
				Ticks = info.Interval;
				alloys.GiveAlloys(info.Amount);

				if (info.ShowTicks)
				{
					var text = "+" + info.Amount.ToString(CultureInfo.CurrentCulture);
					self.World.AddFrameEndTask(w =>
						w.Add(new FloatingText(self.CenterPosition, self.OwnerColor(), text, info.DisplayDuration)));
				}
			}
		}
	}
}

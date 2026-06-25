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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Ambient civilians wandering near the player's buildings form a tax base: every interval the",
		"player earns cash for the nearby population, so holding populated ground is worth something — the",
		"living world feeding the economy (and, as the population grows over the eras, worth more over time).",
		"Counts actors with a Wanders trait, so it tracks the AmbientCivilians population, not soldiers.",
		"Deterministic: interval-counted, integer cash, no RNG.")]
	public class CivilianTaxBaseInfo : TraitInfo
	{
		[Desc("Ticks between payouts (~25 ticks = 1 second at the default game speed).")]
		public readonly int Interval = 250;

		[Desc("How close a civilian must be to one of the player's buildings to be taxed.")]
		public readonly WDist Radius = WDist.FromCells(6);

		[Desc("Cash earned per nearby civilian, each payout.")]
		public readonly int CashPerCivilian = 5;

		[Desc("Maximum civilians counted in a single payout, so a population boom can't run away with the economy.")]
		public readonly int MaxCivilians = 20;

		public override object Create(ActorInitializer init) { return new CivilianTaxBase(this); }
	}

	public class CivilianTaxBase : ITick
	{
		readonly CivilianTaxBaseInfo info;

		// Reused each payout to count each civilian once even when several buildings overlap it.
		readonly HashSet<Actor> nearby = [];

		PlayerResources resources;
		int ticks;

		public CivilianTaxBase(CivilianTaxBaseInfo info)
		{
			this.info = info;
			ticks = info.Interval;
		}

		void ITick.Tick(Actor self)
		{
			// The civilian/neutral owner itself and other non-combatants don't collect a tax base.
			if (self.Owner.NonCombatant)
				return;

			if (--ticks > 0)
				return;

			ticks = info.Interval;

			resources ??= self.TraitOrDefault<PlayerResources>();
			if (resources == null)
				return;

			nearby.Clear();
			foreach (var b in self.World.ActorsWithTrait<Building>())
			{
				if (b.Actor.Owner != self.Owner || !b.Actor.IsInWorld)
					continue;

				foreach (var c in self.World.FindActorsInCircle(b.Actor.CenterPosition, info.Radius))
					if (c.Info.HasTraitInfo<WandersInfo>())
						nearby.Add(c);
			}

			if (nearby.Count == 0)
				return;

			var counted = Math.Min(nearby.Count, info.MaxCivilians);
			resources.GiveCash(counted * info.CashPerCivilian);
		}
	}
}

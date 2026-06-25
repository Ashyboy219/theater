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
	[Desc("THEATER War Room: lets the player buy strategic upgrades — spend cash, then grant a timed or ",
		"permanent player-wide condition that gated effect traits consume. Purchases arrive as synced ",
		"orders so the lock-step sim stays deterministic. Pair with StrategicUpgradeConsumer on the ",
		"affected actors (each consumer grants the subset of conditions it actually consumes).")]
	public class StrategicUpgradesInfo : TraitInfo
	{
		[Desc("Condition name per upgrade index; the buy order's ExtraData selects the index.")]
		public readonly string[] Conditions = { "upg-satrecon", "upg-ew", "upg-precision", "upg-blackproj", "upg-econshift", "upg-address" };

		[Desc("Cash cost per upgrade index (0 = free toggle).")]
		public readonly int[] Costs = { 3000, 4500, 4600, 8000, 0, 0 };

		[Desc("Active duration in ticks per upgrade index; 0 = permanent once bought.")]
		public readonly int[] Durations = { 1500, 2000, 1500, 0, 2000, 1250 };

		public override object Create(ActorInitializer init) { return new StrategicUpgrades(this); }
	}

	public class StrategicUpgrades : INotifyCreated, IResolveOrder, ITick, ISync
	{
		public const string OrderName = "BuyStrategicUpgrade";

		readonly StrategicUpgradesInfo info;

		// Affected actors self-register so a purchase reaches everything it should, and an actor built
		// mid-game inherits whatever is currently active. Registration order is deterministic.
		readonly List<StrategicUpgradeConsumer> consumers = [];

		// 0 = inactive; int.MaxValue = permanent active; otherwise the absolute WorldTick the effect ends.
		readonly int[] expiresAt;

		[VerifySync]
		int activeMask;

		Actor self;

		public StrategicUpgrades(StrategicUpgradesInfo info)
		{
			this.info = info;
			expiresAt = new int[info.Conditions.Length];
		}

		void INotifyCreated.Created(Actor self) { this.self = self; }

		public void Register(StrategicUpgradeConsumer consumer)
		{
			consumers.Add(consumer);
			consumer.Apply(ActiveSet());
		}

		public void Unregister(StrategicUpgradeConsumer consumer)
		{
			consumers.Remove(consumer);
		}

		public bool IsActive(int i) => i >= 0 && i < expiresAt.Length && expiresAt[i] != 0;

		public int RemainingTicks(int i)
		{
			if (i < 0 || i >= expiresAt.Length || expiresAt[i] == 0 || expiresAt[i] == int.MaxValue)
				return 0;
			return Math.Max(0, expiresAt[i] - self.World.WorldTick);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderName)
				return;

			var i = (int)order.ExtraData;
			if (i < 0 || i >= info.Conditions.Length)
				return;

			var permanent = info.Durations[i] == 0;
			if (permanent && expiresAt[i] != 0)
				return; // already owned — don't charge twice

			if (!self.Trait<PlayerResources>().TakeCash(info.Costs[i], true))
				return; // not enough cash

			expiresAt[i] = permanent ? int.MaxValue : self.World.WorldTick + info.Durations[i];
			Apply();
		}

		void ITick.Tick(Actor self)
		{
			var changed = false;
			var now = self.World.WorldTick;
			for (var i = 0; i < expiresAt.Length; i++)
				if (expiresAt[i] != 0 && expiresAt[i] != int.MaxValue && now >= expiresAt[i])
				{
					expiresAt[i] = 0;
					changed = true;
				}

			if (changed)
				Apply();
		}

		HashSet<string> ActiveSet()
		{
			var s = new HashSet<string>();
			for (var i = 0; i < expiresAt.Length; i++)
				if (expiresAt[i] != 0)
					s.Add(info.Conditions[i]);

			return s;
		}

		void Apply()
		{
			var active = ActiveSet();
			var mask = 0;
			for (var i = 0; i < expiresAt.Length; i++)
				if (expiresAt[i] != 0)
					mask |= 1 << i;

			activeMask = mask;
			foreach (var c in consumers)
				c.Apply(active);
		}
	}

	[Desc("THEATER War Room: grants this actor the subset of the player's purchased StrategicUpgrades ",
		"conditions that it consumes, while each is active. Add the matching Conditions: list per actor.")]
	public class StrategicUpgradeConsumerInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("Upgrade conditions this actor consumes; each is granted while the player's matching upgrade is active.")]
		public readonly string[] Conditions = [];

		public override object Create(ActorInitializer init) { return new StrategicUpgradeConsumer(this); }
	}

	public class StrategicUpgradeConsumer : INotifyCreated, INotifyOwnerChanged, INotifyRemovedFromWorld
	{
		readonly StrategicUpgradeConsumerInfo info;
		readonly Dictionary<string, int> tokens = new();
		Actor self;
		StrategicUpgrades upgrades;

		public StrategicUpgradeConsumer(StrategicUpgradeConsumerInfo info) { this.info = info; }

		void INotifyCreated.Created(Actor self)
		{
			this.self = self;
			upgrades = self.Owner.PlayerActor.TraitOrDefault<StrategicUpgrades>();
			upgrades?.Register(this);
		}

		public void Apply(HashSet<string> active)
		{
			foreach (var c in info.Conditions)
			{
				var on = active.Contains(c);
				var has = tokens.TryGetValue(c, out var token);
				if (on && !has)
					tokens[c] = self.GrantCondition(c);
				else if (!on && has)
				{
					self.RevokeCondition(token);
					tokens.Remove(c);
				}
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			upgrades?.Unregister(this);
			foreach (var token in tokens.Values)
				self.RevokeCondition(token);

			tokens.Clear();
			upgrades = newOwner.PlayerActor.TraitOrDefault<StrategicUpgrades>();
			upgrades?.Register(this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			upgrades?.Unregister(this);
		}
	}
}

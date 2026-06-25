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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("THEATER command layer: holds the player's army-wide targeting doctrine and propagates it to ",
		"every combat unit the player owns (each unit gates AutoTargetPriority profiles on the granted ",
		"condition, so the whole army re-prioritises at once). Set via the /target chat command now; ",
		"the war-room panel later.")]
	public class ArmyCommandInfo : TraitInfo
	{
		[Desc("Techtree prerequisite that unlocks advanced targeting focus (the Fire Control capability). ",
			"Until the player owns it, the army targeting doctrine stays locked to balanced (0).")]
		public readonly string FireControlPrerequisite = "cap.firecontrol";

		public override object Create(ActorInitializer init) { return new ArmyCommand(this); }
	}

	public class ArmyCommand : IResolveOrder, ISync
	{
		public const string OrderName = "SetTargetDoctrine";

		// Techtree prerequisite that unlocks non-balanced targeting focus (the Fire Control capability).
		readonly string[] fireControl;

		// Units self-register here on creation so a doctrine change reaches every one of them, and a unit
		// built mid-game picks up the current doctrine immediately. Registration order is deterministic.
		readonly List<ArmyCommandConsumer> consumers = [];

		[VerifySync]
		int doctrine;

		public int Doctrine => doctrine;

		public ArmyCommand(ArmyCommandInfo info)
		{
			fireControl = [info.FireControlPrerequisite];
		}

		public void Register(ArmyCommandConsumer consumer)
		{
			consumers.Add(consumer);
			consumer.Apply(doctrine);
		}

		public void Unregister(ArmyCommandConsumer consumer)
		{
			consumers.Remove(consumer);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderName)
				return;

			var d = (int)order.ExtraData;

			// Fire Control gate: only balanced (0) is available until the capability is purchased.
			if (d != 0)
			{
				var techTree = self.TraitOrDefault<TechTree>();
				if (techTree == null || !techTree.HasPrerequisites(fireControl))
					return;
			}

			if (d == doctrine)
				return;

			doctrine = d;
			foreach (var c in consumers)
				c.Apply(doctrine);
		}
	}

	[Desc("THEATER: makes a unit follow its owner's ArmyCommand targeting doctrine by granting itself the ",
		"matching condition (consumed by the unit's condition-gated AutoTargetPriority profiles).")]
	public class ArmyCommandConsumerInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("Condition granted to this unit for each targeting-doctrine index. Index 0 is balanced ",
			"(grants nothing). The order's ExtraData selects the index.")]
		public readonly string[] DoctrineConditions = ["", "cmd-focus-structures", "cmd-focus-armor", "cmd-focus-infantry"];

		public override object Create(ActorInitializer init) { return new ArmyCommandConsumer(this); }
	}

	public class ArmyCommandConsumer : INotifyCreated, INotifyOwnerChanged, INotifyRemovedFromWorld
	{
		readonly ArmyCommandConsumerInfo info;
		Actor self;
		ArmyCommand command;
		int token = Actor.InvalidConditionToken;
		string current = "";

		public ArmyCommandConsumer(ArmyCommandConsumerInfo info) { this.info = info; }

		void INotifyCreated.Created(Actor self)
		{
			this.self = self;
			command = self.Owner.PlayerActor.TraitOrDefault<ArmyCommand>();
			command?.Register(this);
		}

		public void Apply(int doctrine)
		{
			var condition = doctrine >= 0 && doctrine < info.DoctrineConditions.Length
				? info.DoctrineConditions[doctrine] : "";
			if (condition == current)
				return;

			if (token != Actor.InvalidConditionToken)
			{
				self.RevokeCondition(token);
				token = Actor.InvalidConditionToken;
			}

			current = condition ?? "";
			if (!string.IsNullOrEmpty(current))
				token = self.GrantCondition(current);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			command?.Unregister(this);
			if (token != Actor.InvalidConditionToken)
			{
				self.RevokeCondition(token);
				token = Actor.InvalidConditionToken;
			}

			current = "";
			command = newOwner.PlayerActor.TraitOrDefault<ArmyCommand>();
			command?.Register(this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			command?.Unregister(this);
		}
	}
}

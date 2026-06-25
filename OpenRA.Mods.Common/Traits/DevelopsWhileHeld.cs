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
	[Desc("Grants a condition once this actor has been continuously owned by a combatant player for a set time",
		"(THEATER territory 'develop the ground you hold' mechanic). Capturing it resets the development and timer,",
		"so the condition only sticks while one side keeps control. Gate the developed-tier effects on the condition.")]
	public class DevelopsWhileHeldInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition granted once developed.")]
		public readonly string DevelopedCondition = null;

		[Desc("Ticks of continuous combatant ownership required before developing.")]
		public readonly int DevelopTicks = 1500;

		[Desc("Reset development and the timer when ownership changes (e.g. on capture).")]
		public readonly bool ResetOnOwnerChange = true;

		public override object Create(ActorInitializer init) { return new DevelopsWhileHeld(this); }
	}

	public class DevelopsWhileHeld : ConditionalTrait<DevelopsWhileHeldInfo>, ITick, ISync, INotifyOwnerChanged
	{
		int conditionToken = Actor.InvalidConditionToken;

		[VerifySync]
		int ticks;

		public DevelopsWhileHeld(DevelopsWhileHeldInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			// Neutral-owned (uncaptured) or already-developed nodes don't advance.
			if (IsTraitDisabled || self.Owner.NonCombatant || conditionToken != Actor.InvalidConditionToken)
				return;

			if (++ticks >= Info.DevelopTicks)
				conditionToken = self.GrantCondition(Info.DevelopedCondition);
		}

		void Reset(Actor self)
		{
			ticks = 0;
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.ResetOnOwnerChange)
				Reset(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			Reset(self);
		}
	}
}

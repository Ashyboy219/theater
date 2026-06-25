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
using System.Globalization;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameAlloyCounterLogic : ChromeLogic
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly PlayerAlloys playerAlloys;
		readonly LabelWidget alloyLabel;

		int displayAlloys;

		[ObjectCreator.UseCtor]
		public IngameAlloyCounterLogic(Widget widget, World world)
		{
			var player = world.LocalPlayer;
			playerAlloys = player?.PlayerActor.TraitOrDefault<PlayerAlloys>();
			alloyLabel = widget.GetOrNull<LabelWidget>("ALLOYS");

			if (playerAlloys != null)
				displayAlloys = playerAlloys.Alloys;
		}

		public override void Tick()
		{
			if (playerAlloys == null || alloyLabel == null)
				return;

			var actual = playerAlloys.Alloys;
			var diff = Math.Abs(actual - displayAlloys);
			var move = Math.Min(Math.Max((int)(diff * DisplayFracPerFrame), DisplayDeltaPerFrame), diff);

			if (displayAlloys < actual)
				displayAlloys += move;
			else if (displayAlloys > actual)
				displayAlloys -= move;

			var text = displayAlloys.ToString(CultureInfo.CurrentCulture);
			alloyLabel.GetText = () => text;
		}
	}
}

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

using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameEraDisplayLogic : ChromeLogic
	{
		readonly GameTimeline timeline;
		readonly AmbientCivilians civilians;
		readonly int timestep;
		readonly LabelWidget label;

		[ObjectCreator.UseCtor]
		public IngameEraDisplayLogic(Widget widget, World world)
		{
			timeline = world.WorldActor.TraitOrDefault<GameTimeline>();
			civilians = world.WorldActor.TraitOrDefault<AmbientCivilians>();
			timestep = world.Timestep;
			label = widget.GetOrNull<LabelWidget>("ERA");
		}

		public override void Tick()
		{
			if (timeline == null || label == null)
				return;

			var seconds = timeline.Ticks * timestep / 1000;
			var text = $"{timeline.CurrentEraName}  ·  {seconds / 60:D2}:{seconds % 60:D2}";

			// Surface the living-world population so its growth over the eras is actually visible.
			if (civilians != null)
				text += $"  ·  Pop {civilians.Population}";

			label.GetText = () => text;
		}
	}
}

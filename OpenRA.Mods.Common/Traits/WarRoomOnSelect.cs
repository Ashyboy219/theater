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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("THEATER: opens the War Room overlay window when the local player selects this structure.")]
	public class WarRoomOnSelectInfo : TraitInfo
	{
		[Desc("Top-level chrome widget (window) to open.")]
		public readonly string Window = "WARROOM_ROOT";

		public override object Create(ActorInitializer init) { return new WarRoomOnSelect(this); }
	}

	public class WarRoomOnSelect : INotifySelected
	{
		readonly WarRoomOnSelectInfo info;

		public WarRoomOnSelect(WarRoomOnSelectInfo info) { this.info = info; }

		void INotifySelected.Selected(Actor self)
		{
			// Only for the local player's own structure, and don't stack duplicate windows.
			if (self.Owner != self.World.LocalPlayer)
				return;

			if (Ui.Root.GetOrNull<Widget>(info.Window) != null)
				return;

			Ui.OpenWindow(info.Window, new WidgetArgs { { "world", self.World } });
		}
	}
}

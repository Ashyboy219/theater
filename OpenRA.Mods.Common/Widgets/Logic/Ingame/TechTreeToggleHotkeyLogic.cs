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
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	// THEATER: toggles the centered tech-tree flowchart panel (a self-hidden Background) on a hotkey — a plain
	// Visible-flip, NOT the Ui.OpenWindow stack, so it doesn't pause the game or interrupt the sidebar.
	[ChromeLogicArgsHotkeys("ToggleTechTreeKey")]
	public class TechTreeToggleHotkeyLogic : SingleHotkeyBaseLogic
	{
		[ObjectCreator.UseCtor]
		public TechTreeToggleHotkeyLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "ToggleTechTreeKey", "TECHTREE_KEYHANDLER", logicArgs) { }

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			// Resolved on each press (not in the ctor) so panel-creation order never matters; GetOrNull keeps
			// a missing panel from throwing.
			var panel = Ui.Root.GetOrNull("TECHTREE_PANEL");
			if (panel != null)
			{
				panel.Visible = !panel.Visible;
				Ui.ResetTooltips();
			}

			return true;
		}
	}
}

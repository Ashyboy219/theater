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
	public enum FormationShape { None, Line, Column, Wedge, Box }

	[TraitLocation(SystemActors.World)]
	[Desc("THEATER: holds the local player's selected movement-formation shape (the Formations capability). ",
		"This is LOCAL, UNSYNCED UI state — it only influences order GENERATION, which emits ordinary ",
		"per-unit move orders, and is never read inside the simulation tick, so it must never enter the ",
		"sync hash. Read by FormationOrderGenerator; set by the /formation command and the formation UI.")]
	public class FormationStateInfo : TraitInfo<FormationState> { }

	public class FormationState
	{
		// Local, unsynced. Default None = formations off (normal movement).
		public FormationShape Shape = FormationShape.None;
	}
}

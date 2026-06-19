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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("THEATER: chat commands to look up faction info in-game. Attach to the world actor.")]
	public class TheaterCommandsInfo : TraitInfo<TheaterCommands> { }

	public class TheaterCommands : IChatCommand, IWorldLoaded
	{
		World world;
		ChatCommands console;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			console = world.WorldActor.Trait<ChatCommands>();
			console.RegisterCommand("factions", this);
			console.RegisterCommand("faction", this);
		}

		public void InvokeCommand(string name, string arg)
		{
			var factions = world.Map.Rules.Actors[SystemActors.World].TraitInfos<FactionInfo>()
				.Where(f => f.Selectable && f.RandomFactionMembers.Count == 0)
				.ToList();

			if (!string.IsNullOrWhiteSpace(arg))
			{
				var match = factions.FirstOrDefault(f =>
					string.Equals(f.InternalName, arg, StringComparison.OrdinalIgnoreCase) ||
					FluentProvider.GetMessage(f.Name).Contains(arg, StringComparison.OrdinalIgnoreCase));

				if (match == null)
					TextNotificationsManager.Debug($"No faction matching '{arg}'.");
				else
					PrintFaction(match);

				return;
			}

			TextNotificationsManager.Debug("THEATER factions (use /faction <name> for one):");
			foreach (var f in factions)
				PrintFaction(f);
		}

		static void PrintFaction(FactionInfo f)
		{
			var displayName = FluentProvider.GetMessage(f.Name);
			var description = f.Description != null
				? FluentProvider.GetMessage(f.Description).Replace("\n", " ").Replace("  ", " ").Trim()
				: "";

			TextNotificationsManager.Debug($"{displayName} — {description}");
		}
	}
}

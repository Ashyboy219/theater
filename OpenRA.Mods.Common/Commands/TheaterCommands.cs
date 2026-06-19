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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("THEATER: chat commands for the dev sandbox + faction info lookups. Attach to the world actor.")]
	public class TheaterCommandsInfo : TraitInfo<TheaterCommands> { }

	public class TheaterCommands : IChatCommand, IWorldLoaded
	{
		World world;
		ChatCommands console;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			console = world.WorldActor.Trait<ChatCommands>();

			// Dev sandbox toggles.
			console.RegisterCommand("dev", this);
			console.RegisterCommand("sandbox", this);
			console.RegisterCommand("visibility", this);
			console.RegisterCommand("reveal", this);

			// Faction info lookups.
			console.RegisterCommand("factions", this);
			console.RegisterCommand("faction", this);
		}

		public void InvokeCommand(string name, string arg)
		{
			switch (name)
			{
				case "dev":
				case "sandbox":
					IssueDevOrder(DeveloperMode.Orders.Sandbox,
						"Dev sandbox toggled: unlimited cash, instant build, build anywhere, free power, fast support powers. " +
						"Build options stay limited to your faction (use the lobby's faction picker to test another).");
					return;

				case "visibility":
				case "reveal":
					IssueDevOrder(DeveloperMode.Orders.Visibility,
						"Full-map visibility toggled (non-destructive — reveals the map without changing your real vision).");
					return;

				case "factions":
				case "faction":
					PrintFactions(arg);
					return;
			}
		}

		void IssueDevOrder(string devOrder, string hint)
		{
			var player = world.LocalPlayer;
			if (player == null)
			{
				TextNotificationsManager.Debug("No local player — dev mode is unavailable (spectating?).");
				return;
			}

			var dev = player.PlayerActor.TraitOrDefault<DeveloperMode>();
			if (dev == null || !dev.Enabled)
			{
				TextNotificationsManager.Debug("Cheats are not enabled in this game, so dev mode is unavailable.");
				return;
			}

			// Route through the order system so it stays deterministic / network-synced.
			world.IssueOrder(new Order(devOrder, player.PlayerActor, false));
			TextNotificationsManager.Debug(hint);
		}

		void PrintFactions(string arg)
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

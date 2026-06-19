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

			// Army-wide targeting doctrine (command layer).
			console.RegisterCommand("target", this);
			console.RegisterCommand("focus", this);
		}

		public void InvokeCommand(string name, string arg)
		{
			switch (name)
			{
				case "dev":
				case "sandbox":
					IssueDevOrder(DeveloperMode.Orders.Sandbox, d => d.Sandbox,
						"Dev sandbox ON — unlimited cash, instant build, build anywhere, free power, fast support powers. " +
						"Build options stay limited to YOUR faction (pick another faction in the lobby to test it). Type /dev again to turn it off.",
						"Dev sandbox OFF — back to a normal economy and build times. Type /dev again to turn it back on.");
					return;

				case "visibility":
				case "reveal":
					IssueDevOrder(DeveloperMode.Orders.Visibility, d => d.DisableShroud,
						"Full-map reveal ON — non-destructive (your real vision is unchanged). Type /visibility again to turn it off.",
						"Full-map reveal OFF — back to fog of war.");
					return;

				case "factions":
				case "faction":
					PrintFactions(arg);
					return;

				case "target":
				case "focus":
					SetTargetDoctrine(arg);
					return;
			}
		}

		// Army-wide targeting doctrine: tell the whole army what to prioritise.
		void SetTargetDoctrine(string arg)
		{
			var player = world.LocalPlayer;
			if (player == null || player.PlayerActor.TraitOrDefault<ArmyCommand>() == null)
			{
				TextNotificationsManager.Debug("No army command available (spectating?).");
				return;
			}

			int doctrine;
			switch ((arg ?? "").Trim().ToLowerInvariant())
			{
				case "structures": case "structure": case "buildings": case "building": doctrine = 1; break;
				case "armor": case "armour": case "tanks": case "vehicles": case "vehicle": doctrine = 2; break;
				case "infantry": case "inf": case "troops": doctrine = 3; break;
				case "balanced": case "balance": case "normal": case "off": case "": doctrine = 0; break;
				default:
					TextNotificationsManager.Debug("Usage: /target structures | armor | infantry | balanced");
					return;
			}

			world.IssueOrder(new Order(ArmyCommand.OrderName, player.PlayerActor, false) { ExtraData = (uint)doctrine });

			var label = doctrine switch
			{
				1 => "STRUCTURES first — your army prioritises enemy buildings & defenses.",
				2 => "ARMOR first — your army prioritises enemy vehicles/tanks.",
				3 => "INFANTRY first — your army prioritises enemy infantry.",
				_ => "BALANCED — normal targeting.",
			};
			TextNotificationsManager.Debug($"Army targeting doctrine: {label}");
		}

		// Toggles a DeveloperMode cheat and prints a state-accurate message. currentState reads the value
		// BEFORE the order resolves, so the new state is its negation (no other toggle is in flight locally).
		void IssueDevOrder(string devOrder, Func<DeveloperMode, bool> currentState, string onMessage, string offMessage)
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

			var willEnable = !currentState(dev);

			// Route through the order system so it stays deterministic / network-synced.
			world.IssueOrder(new Order(devOrder, player.PlayerActor, false));
			TextNotificationsManager.Debug(willEnable ? onMessage : offMessage);
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

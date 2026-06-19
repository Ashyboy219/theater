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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	// THEATER "War Room": a modal situation-room overlay opened from the Theater Command (or /warroom).
	// Shows the player's faction, the active army targeting doctrine (with buttons to change it), a
	// general portrait + line, and a plain-language tech-progression guide.
	public class WarRoomLogic : ChromeLogic
	{
		// Faction internal-name -> (flagship unique unit, the building that produces it).
		static readonly Dictionary<string, (string Unit, string Building)> Uniques = new()
		{
			{ "federation", ("Specter Stealth Fighter", "Airfield") },
			{ "northern_union", ("Bastion EW Command Tank", "War Factory") },
			{ "continental_bloc", ("Wing Loong Swarm Drone", "Airfield") },
			{ "rhine_compact", ("Loewe Heavy MBT", "War Factory") },
			{ "isles_coalition", ("Pathfinder Team", "Barracks") },
			{ "eastern_maritime", ("Kunai Combat Robot", "Barracks") },
			{ "subcontinent_federation", ("Garuda Cruise-Missile Launcher", "War Factory") },
			{ "anatolian_alliance", ("Bayrak Loiter Drone", "Airfield") },
		};

		[ObjectCreator.UseCtor]
		public WarRoomLogic(Widget widget, World world)
		{
			var player = world.LocalPlayer;

			widget.Get<LabelWidget>("FACTION").GetText = () =>
				player != null ? "Faction:  " + player.Faction.Name : "Faction:  —";

			widget.Get<LabelWidget>("DOCTRINE").GetText = () =>
				"Targeting doctrine:  " + DoctrineName(CurrentDoctrine(player));

			widget.Get<LabelWidget>("SPEECH").GetText = () => SpeechLine(CurrentDoctrine(player));

			widget.Get<LabelWidget>("TECHGUIDE").GetText = () => TechGuide(player);

			BindDoctrine(widget, "DOC_BALANCED", 0, world, player);
			BindDoctrine(widget, "DOC_STRUCT", 1, world, player);
			BindDoctrine(widget, "DOC_ARMOR", 2, world, player);
			BindDoctrine(widget, "DOC_INF", 3, world, player);

			widget.Get<ButtonWidget>("CLOSE").OnClick = Ui.CloseWindow;
		}

		static void BindDoctrine(Widget widget, string id, int index, World world, Player player)
		{
			var button = widget.Get<ButtonWidget>(id);
			button.IsHighlighted = () => CurrentDoctrine(player) == index;
			button.OnClick = () =>
			{
				if (player != null)
					world.IssueOrder(new Order(ArmyCommand.OrderName, player.PlayerActor, false) { ExtraData = (uint)index });
			};
		}

		static int CurrentDoctrine(Player player)
		{
			return player?.PlayerActor.TraitOrDefault<ArmyCommand>()?.Doctrine ?? 0;
		}

		static string DoctrineName(int d)
		{
			return d switch
			{
				1 => "Structures first",
				2 => "Armor first",
				3 => "Infantry first",
				_ => "Balanced",
			};
		}

		static string SpeechLine(int d)
		{
			return d switch
			{
				1 => "\"Their cities are the target, Commander — we'll bring the walls down.\"",
				2 => "\"Armor leads their advance. Gut their tanks and the rest folds.\"",
				3 => "\"We'll cut their infantry to pieces before they reach the line.\"",
				_ => "\"Standing by for your orders, Commander.\"",
			};
		}

		static string TechGuide(Player player)
		{
			var unique = "(pick a faction in the lobby)";
			if (player != null && Uniques.TryGetValue(player.Faction.InternalName, out var u))
				unique = u.Unit + "  —  build an " + u.Building;

			return
				"Build top-to-bottom; each structure unlocks the next tier:\n" +
				"   Power Plant        powers everything you build\n" +
				"   Ore Refinery       income  (also required for the War Factory)\n" +
				"   Barracks           >  INFANTRY\n" +
				"   War Factory        >  VEHICLES  (tanks, artillery, APC)\n" +
				"   Radar Dome         unlocks the Airfield / Helipad + advanced tech\n" +
				"   Helipad / Airfield >  AIRCRAFT\n" +
				"   Service Depot      repairs vehicles + builds the MCV\n" +
				"   Tech Center        >  ADVANCED units & superweapons\n" +
				"\n" +
				"YOUR FACTION UNIQUE:  " + unique;
		}
	}
}

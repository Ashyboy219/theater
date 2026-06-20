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
using System.Collections.Generic;
using System.Globalization;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	// THEATER "National Command / War Room": a full-screen command dashboard opened from the Theater
	// Command (or /warroom). Wired to live game state — funds, power, military counts, threat — plus the
	// army targeting doctrine and purchasable strategic upgrades. Read-only here; cash spends and
	// doctrine/upgrade changes go through synced orders so the lock-step sim stays deterministic.
	public class WarRoomLogic : ChromeLogic
	{
		static readonly NumberFormatInfo NF = NumberFormatInfo.CurrentInfo;
		static readonly WDist ThreatRange = WDist.FromCells(14);

		readonly World world;
		readonly Player player;
		readonly PlayerResources res;
		readonly PowerManager power;
		readonly PlayerStatistics stats;
		readonly ArmyCommand army;
		readonly StrategicUpgrades upgrades;

		// Per-scan caches, refreshed off the LogicTicker (unsynced UI path) so heavy queries don't run every frame.
		int threat;
		string vector = "—";
		int vehCount, infCount, airCount, navCount;
		readonly int[] quadrant = new int[4];   // N, E, S, W enemy counts
		int eArmor, eInf, eAir, eNaval;
		int tickCounter;

		[ObjectCreator.UseCtor]
		public WarRoomLogic(Widget widget, World world)
		{
			this.world = world;
			player = world.LocalPlayer;
			res = player?.PlayerActor.TraitOrDefault<PlayerResources>();
			power = player?.PlayerActor.TraitOrDefault<PowerManager>();
			stats = player?.PlayerActor.TraitOrDefault<PlayerStatistics>();
			army = player?.PlayerActor.TraitOrDefault<ArmyCommand>();
			upgrades = player?.PlayerActor.TraitOrDefault<StrategicUpgrades>();

			var ticker = widget.GetOrNull<LogicTickerWidget>("WR_TICKER");
			if (ticker != null)
				ticker.OnTick = () => { if (tickCounter++ % 15 == 0) Recache(); };
			Recache();

			BindTopBar(widget);
			BindThreatPanel(widget);
			BindCenter(widget);
			BindMilitary(widget);
			BindEffects(widget);
			BindDoctrines(widget);
			BindUpgrades(widget);
			BindStaff(widget);

			widget.Get<ButtonWidget>("WR_RETURN").OnClick = Ui.CloseWindow;
			var settings = widget.GetOrNull<ButtonWidget>("WR_SETTINGS");
			if (settings != null)
				settings.OnClick = Ui.CloseWindow;
		}

		// ---- live data scan (cached) ----
		void Recache()
		{
			threat = 0;
			vector = "—";
			vehCount = infCount = airCount = navCount = 0;
			eArmor = eInf = eAir = eNaval = 0;
			for (var i = 0; i < 4; i++)
				quadrant[i] = 0;

			if (player == null)
				return;

			foreach (var a in world.ActorsHavingTrait<Mobile>())
			{
				if (a.Owner != player || !a.IsInWorld || a.IsDead)
					continue;

				var t = a.GetEnabledTargetTypes();
				if (t.Contains("Ship") || t.Contains("WaterActor"))
					navCount++;
				else if (t.Contains("Infantry"))
					infCount++;
				else if (t.Contains("Vehicle"))
					vehCount++;
			}

			foreach (var a in world.ActorsHavingTrait<Aircraft>())
				if (a.Owner == player && a.IsInWorld && !a.IsDead)
					airCount++;

			// Threat: enemy combatants seen near our base buildings.
			var seen = new HashSet<Actor>();
			var baseX = 0L;
			var baseY = 0L;
			var bases = 0;
			foreach (var b in world.ActorsHavingTrait<BaseBuilding>())
			{
				if (b.Owner != player || !b.IsInWorld)
					continue;

				baseX += b.CenterPosition.X;
				baseY += b.CenterPosition.Y;
				bases++;
			}

			if (bases == 0)
				return;

			var cx = baseX / bases;
			var cy = baseY / bases;
			var sumX = 0L;
			var sumY = 0L;
			var n = 0L;
			foreach (var b in world.ActorsHavingTrait<BaseBuilding>())
			{
				if (b.Owner != player || !b.IsInWorld)
					continue;

				foreach (var e in world.FindActorsInCircle(b.CenterPosition, ThreatRange))
				{
					if (e.IsDead || !e.IsInWorld || seen.Contains(e))
						continue;
					if (player.RelationshipWith(e.Owner) != PlayerRelationship.Enemy)
						continue;
					if (!e.Info.HasTraitInfo<AttackBaseInfo>() || !e.CanBeViewedByPlayer(player))
						continue;

					seen.Add(e);
					var dx = e.CenterPosition.X - cx;
					var dy = e.CenterPosition.Y - cy;
					sumX += dx;
					sumY += dy;
					n++;
					quadrant[Cardinal(dx, dy)]++;

					var t = e.GetEnabledTargetTypes();
					if (t.Contains("Ship") || t.Contains("WaterActor"))
						eNaval++;
					else if (t.Contains("Infantry"))
						eInf++;
					else if (t.Contains("Vehicle"))
						eArmor++;
					if (e.Info.HasTraitInfo<AircraftInfo>())
						eAir++;
				}
			}

			threat = seen.Count;
			if (n > 0)
				vector = Octant(sumX, sumY);
		}

		// 0 = N, 1 = E, 2 = S, 3 = W (world y is south-positive).
		static int Cardinal(long dx, long dy)
		{
			if (Math.Abs(dx) >= Math.Abs(dy))
				return dx >= 0 ? 1 : 3;
			return dy >= 0 ? 2 : 0;
		}

		static string Octant(long dx, long dy)
		{
			var adx = Math.Abs(dx);
			var ady = Math.Abs(dy);
			if (adx > 2 * ady)
				return dx >= 0 ? "EAST" : "WEST";
			if (ady > 2 * adx)
				return dy >= 0 ? "SOUTH" : "NORTH";
			if (dy < 0)
				return dx >= 0 ? "NORTHEAST" : "NORTHWEST";
			return dx >= 0 ? "SOUTHEAST" : "SOUTHWEST";
		}

		// ---- top bar: funds + power + under-attack alert ----
		void BindTopBar(Widget w)
		{
			w.Get<LabelWidget>("WR_TOP_CREDITS").GetText = () =>
				res == null ? "--" : "$" + res.GetCashAndResources().ToString("N0", NF);

			var inc = w.Get<LabelWidget>("WR_TOP_INCOME");
			inc.GetText = () => stats == null || stats.DisplayIncome == 0 ? "" : "+" + stats.DisplayIncome.ToString("N0", NF);
			inc.GetColor = () => Color.LimeGreen;

			var pw = w.Get<LabelWidget>("WR_TOP_POWER");
			pw.GetText = () => power == null ? "" : "Power " + power.PowerDrained.ToString("N0", NF) + " / " + power.PowerProvided.ToString("N0", NF);
			pw.GetColor = () => power != null && power.ExcessPower < 0 ? Color.Red : Color.White;

			var alert = w.Get<ColorBlockWidget>("WR_ALERT_BG");
			alert.GetColor = () => Color.FromArgb(220, 170, 32, 32);
			alert.IsVisible = () => threat > 0;
			w.Get<LabelWidget>("WR_ALERT").IsVisible = () => threat > 0;
		}

		// ---- left/right threat panel ----
		void BindThreatPanel(Widget w)
		{
			w.Get<LabelWidget>("WR_ENEMY_VECTOR").GetText = () => "Main vector:  " + vector;
			w.Get<LabelWidget>("WR_ENEMY_COUNT").GetText = () => threat + " hostile" + (threat == 1 ? "" : "s") + " sighted";
			w.Get<LabelWidget>("WR_ENEMY_ARMOR").GetText = () => "Armor:  " + eArmor;
			w.Get<LabelWidget>("WR_ENEMY_INF").GetText = () => "Infantry:  " + eInf;
			w.Get<LabelWidget>("WR_ENEMY_AIR").GetText = () => "Air:  " + eAir;
			w.Get<LabelWidget>("WR_ENEMY_NAVAL").GetText = () => "Naval:  " + eNaval;

			BindSector(w, "WR_SECTOR_N_STATUS", 0);
			BindSector(w, "WR_SECTOR_E_STATUS", 1);
			BindSector(w, "WR_SECTOR_S_STATUS", 2);
			BindSector(w, "WR_SECTOR_W_STATUS", 3);
		}

		void BindSector(Widget w, string id, int q)
		{
			var l = w.GetOrNull<LabelWidget>(id);
			if (l == null)
				return;
			l.GetText = () => SectorStatus(quadrant[q]);
			l.GetColor = () => SectorColor(quadrant[q]);
		}

		static string SectorStatus(int c) => c == 0 ? "SECURE" : c <= 2 ? "STABLE" : c <= 5 ? "CONTESTED" : "CRITICAL";
		static Color SectorColor(int c) => c == 0 ? Color.LimeGreen : c <= 2 ? Color.Yellow : c <= 5 ? Color.Orange : Color.Red;

		// ---- center ----
		void BindCenter(Widget w)
		{
			// Faction.Name is a fluent-reference key — resolve it, don't print the raw key.
			w.Get<LabelWidget>("WR_FACTION").GetText = () =>
				player == null ? "—" : FluentProvider.GetMessage(player.Faction.Name);

			w.Get<LabelWidget>("WR_SPEECH").GetText = () => threat > 0
				? $"Mr. President, hostile forces are massing on the {vector} front. They will strike soon — we recommend immediate action."
				: "All sectors are holding, Commander. Your forces stand ready. Awaiting your orders.";
		}

		// ---- military counts ----
		void BindMilitary(Widget w)
		{
			w.Get<LabelWidget>("WR_MIL_VEH").GetText = () => vehCount.ToString("N0", NF);
			w.Get<LabelWidget>("WR_MIL_INF").GetText = () => infCount.ToString("N0", NF);
			w.Get<LabelWidget>("WR_MIL_AIR").GetText = () => airCount.ToString("N0", NF);
			w.Get<LabelWidget>("WR_MIL_NAV").GetText = () => navCount.ToString("N0", NF);
		}

		// Effect rows map to upgrade indices: EW(1), Precision(2), Economic(4), Address(5).
		static readonly (string Id, int Index)[] Effects =
		{
			("WR_FX_EW", 1), ("WR_FX_PREC", 2), ("WR_FX_ECON", 4), ("WR_FX_ADDR", 5),
		};

		void BindEffects(Widget w)
		{
			foreach (var (id, index) in Effects)
			{
				var i = index;
				var state = w.GetOrNull<LabelWidget>(id + "_STATE");
				if (state != null)
				{
					state.GetText = () => upgrades != null && upgrades.IsActive(i) ? "ACTIVE" : "STANDBY";
					state.GetColor = () => upgrades != null && upgrades.IsActive(i) ? Color.LimeGreen : Color.Gray;
				}

				var timer = w.GetOrNull<LabelWidget>(id + "_TIMER");
				if (timer != null)
				{
					timer.GetText = () =>
					{
						if (upgrades == null || !upgrades.IsActive(i))
							return "";
						var t = upgrades.RemainingTicks(i);
						if (t <= 0)
							return "PERM";
						var s = t * world.Timestep / 1000;
						return (s / 60).ToString("00", NF) + ":" + (s % 60).ToString("00", NF);
					};
				}
			}
		}

		// ---- doctrines ----
		void BindDoctrines(Widget w)
		{
			BindDoctrine(w, "WR_DOC_STRUCT", 1);
			BindDoctrine(w, "WR_DOC_ARMOR", 2);
			BindDoctrine(w, "WR_DOC_DEF", 4);
			BindDoctrine(w, "WR_DOC_ASSAULT", 5);
		}

		void BindDoctrine(Widget w, string id, int index)
		{
			var b = w.Get<ButtonWidget>(id);
			b.IsHighlighted = () => (army?.Doctrine ?? 0) == index;
			b.OnClick = () =>
			{
				if (player == null)
					return;

				// Toggle back to Balanced (0) if this doctrine is already active.
				var target = (army?.Doctrine ?? 0) == index ? 0 : index;
				world.IssueOrder(new Order(ArmyCommand.OrderName, player.PlayerActor, false) { ExtraData = (uint)target });
			};
		}

		// ---- strategic upgrades (buy) ----
		void BindUpgrades(Widget w)
		{
			BindBuy(w, "WR_UPG_SAT_BUY", 0, 3000);
			BindBuy(w, "WR_UPG_EW_BUY", 1, 4500);
			BindBuy(w, "WR_UPG_PREC_BUY", 2, 4600);
			BindBuy(w, "WR_UPG_BLACK_BUY", 3, 8000);
		}

		void BindBuy(Widget w, string id, int index, int cost)
		{
			var b = w.Get<ButtonWidget>(id);
			b.IsHighlighted = () => upgrades != null && upgrades.IsActive(index);
			b.IsDisabled = () => res == null
				|| res.GetCashAndResources() < cost
				|| (upgrades != null && upgrades.IsActive(index));
			b.OnClick = () =>
			{
				if (player != null)
					world.IssueOrder(new Order(StrategicUpgrades.OrderName, player.PlayerActor, false) { ExtraData = (uint)index });
			};
		}

		// ---- command staff (flavor; quotes react to the situation) ----
		void BindStaff(Widget w)
		{
			Quote(w, "WR_STAFF_STEEL_QUOTE",
				() => threat > 0 ? "\"They're at our gates, Mr. President. Give the word.\"" : "\"The line holds. Keep the factories running.\"");
			Quote(w, "WR_STAFF_JAMESON_QUOTE",
				() => navCount > 0 ? "\"The fleet is ready. Say where, and we sail.\"" : "\"We have no ships at sea, sir. We're blind to the coast.\"");
			Quote(w, "WR_STAFF_KOVALENKO_QUOTE",
				() => upgrades != null && upgrades.IsActive(3) ? "\"Black Projects are live. They won't know what hit them.\"" : "\"Fund Black Projects and I'll change this war.\"");
			Quote(w, "WR_STAFF_MARSHALL_QUOTE",
				() => threat > 0 ? $"\"Movement on the {vector} approach. Trust the intel.\"" : "\"All quiet on the wire. For now.\"");
		}

		static void Quote(Widget w, string id, Func<string> text)
		{
			var l = w.GetOrNull<LabelWidget>(id);
			if (l != null)
				l.GetText = text;
		}
	}
}

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

using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Marks a structure as a Space Program launch site — a player wins the science victory only while",
		"they still own at least one of these (so the victory countdown can be contested by destroying it).")]
	public class SpaceVictoryLaunchSiteInfo : TraitInfo<SpaceVictoryLaunchSite> { }
	public class SpaceVictoryLaunchSite { }

	[TraitLocation(SystemActors.Player)]
	[Desc("A 'Civilization but real-time' science victory: a player wins either by the usual conquest, OR by",
		"completing the Space Program (holding the OrbitalPrerequisite) and keeping a launch site alive for",
		"HoldDuration. Replaces ConquestVictoryConditions (it folds the same conquest win into one objective).")]
	[IncludeStaticFluentReferences(typeof(SpaceVictoryConditions))]
	public class SpaceVictoryConditionsInfo : TraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Prerequisite token that means 'the Space Program is complete' (the Orbital Command stage).")]
		public readonly string OrbitalPrerequisite = "space.orbital";

		[Desc("Ticks the player must hold the completed Space Program (with a launch site) to win.",
			"Defaults to 7500 ticks (5 minutes at the default game speed).")]
		public readonly int HoldDuration = 7500;

		[Desc("Reset the countdown if the player loses every launch site (e.g. the Theater Command is destroyed).")]
		public readonly bool ResetOnHoldLost = true;

		[Desc("Delay for the end game notification in milliseconds.")]
		public readonly int NotificationDelay = 1500;

		[Desc("Description of the objective.")]
		public readonly string Objective = "Destroy all opposition — or complete the Space Program and hold it!";

		[Desc("Disable the win/loss messages and audio notifications?")]
		public readonly bool SuppressNotifications = false;

		public override object Create(ActorInitializer init) { return new SpaceVictoryConditions(init.Self, this); }
	}

	public class SpaceVictoryConditions : ITick, ISync, INotifyWinStateChanged, INotifyTimeLimit
	{
		[FluentReference("player")]
		const string PlayerIsVictorious = "notification-player-is-victorious";

		[FluentReference("player")]
		const string PlayerIsDefeated = "notification-player-is-defeated";

		readonly SpaceVictoryConditionsInfo info;
		readonly Player player;
		readonly MissionObjectives mo;
		readonly bool shortGame;
		readonly string[] orbitalPrerequisite;

		[VerifySync]
		public int TicksLeft;

		TechTree techTree;
		Player[] otherPlayers;
		int objectiveID = -1;
		bool launching;

		public SpaceVictoryConditions(Actor self, SpaceVictoryConditionsInfo svcInfo)
		{
			info = svcInfo;
			TicksLeft = info.HoldDuration;
			player = self.Owner;
			mo = self.Trait<MissionObjectives>();
			shortGame = player.World.WorldActor.Trait<MapOptions>().ShortGame;
			orbitalPrerequisite = [info.OrbitalPrerequisite];
		}

		// The science victory is "live" only while the player has completed the Space Program AND still owns a
		// launch site — so an enemy can stall the countdown by destroying the player's Theater Command.
		public bool Holding
		{
			get
			{
				techTree ??= player.PlayerActor.Trait<TechTree>();
				if (!techTree.HasPrerequisites(orbitalPrerequisite))
					return false;

				foreach (var a in player.World.ActorsHavingTrait<SpaceVictoryLaunchSite>())
					if (a.Owner == player && a.IsInWorld)
						return true;

				return false;
			}
		}

		void ITick.Tick(Actor self)
		{
			if (player.WinState != WinState.Undefined || player.NonCombatant)
				return;

			if (objectiveID < 0)
				objectiveID = mo.Add(player, info.Objective, "Primary", inhibitAnnouncement: true);

			if (player.HasNoRequiredUnits(shortGame))
				mo.MarkFailed(player, objectiveID);

			// Conquest win/loss, identical to ConquestVictoryConditions: win when every enemy is out, lose if one
			// of them has already won. Players/relationships are fixed at game start so we cache the opponents.
			otherPlayers ??= self.World.Players.Where(p => !p.NonCombatant && !p.IsAlliedWith(player)).ToArray();

			// GUARD (this is the bit StrategicVictoryConditions omits but ConquestVictoryConditions has): with no
			// opponents at all — a solo/sandbox game — an empty loop would leave allOthersLost == true and instantly
			// "win" on tick 1. Only run the conquest check when there is actually someone to defeat.
			if (otherPlayers.Length > 0)
			{
				var allOthersLost = true;
				var anyOtherWon = false;
				foreach (var other in otherPlayers)
				{
					allOthersLost = allOthersLost && other.WinState == WinState.Lost;
					anyOtherWon = anyOtherWon || other.WinState == WinState.Won;
				}

				if (allOthersLost)
					mo.MarkCompleted(player, objectiveID);

				if (anyOtherWon)
					mo.MarkFailed(player, objectiveID);
			}

			// The science-victory countdown — the same objective is also completed by holding a finished
			// Space Program for HoldDuration, giving the 4X investment a real, alternative path to victory.
			// It is broadcast to everyone so the science win is a tense, COUNTERABLE race: opponents are told
			// to go destroy the launching player's Theater Command before the timer runs out.
			if (Holding)
			{
				if (!launching)
				{
					launching = true;
					Broadcast($"WARNING: {player.ResolvedPlayerName} has begun an orbital launch — destroy their Theater Command to stop it!");
				}

				if (TicksLeft == 1500)
					Broadcast($"{player.ResolvedPlayerName}'s orbital launch: 1 minute to victory!");
				else if (TicksLeft == 250)
					Broadcast($"{player.ResolvedPlayerName}'s orbital launch: 10 seconds!");

				if (--TicksLeft <= 0)
					mo.MarkCompleted(player, objectiveID);
			}
			else
			{
				if (launching)
				{
					launching = false;
					Broadcast($"{player.ResolvedPlayerName}'s orbital launch has been stalled.");
				}

				if (info.ResetOnHoldLost)
					TicksLeft = info.HoldDuration;
			}
		}

		void Broadcast(string text)
		{
			if (!info.SuppressNotifications)
				TextNotificationsManager.AddSystemLine(text);
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (objectiveID < 0)
				return;

			var myTeam = self.World.LobbyInfo.ClientWithIndex(self.Owner.ClientIndex).Team;
			var victoriousTeam = self.World.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics?.Experience ?? 0)
				.GroupBy(p => (self.World.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics?.Experience ?? 0))
				.First();

			if (victoriousTeam.Key == myTeam && (myTeam != 0 || victoriousTeam.First().Player == self.Owner))
			{
				mo.MarkCompleted(self.Owner, objectiveID);
				return;
			}

			mo.MarkFailed(self.Owner, objectiveID);
		}

		void INotifyWinStateChanged.OnPlayerLost(Player player)
		{
			foreach (var a in player.World.ActorsWithTrait<INotifyOwnerLost>().Where(a => a.Actor.Owner == player))
				a.Trait.OnOwnerLost(a.Actor);

			if (info.SuppressNotifications)
				return;

			TextNotificationsManager.AddSystemLine(PlayerIsDefeated, "player", player.ResolvedPlayerName);
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.LoseNotification, player.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(player, mo.Info.LoseTextNotification);
				}
			});
		}

		void INotifyWinStateChanged.OnPlayerWon(Player player)
		{
			if (info.SuppressNotifications)
				return;

			TextNotificationsManager.AddSystemLine(PlayerIsVictorious, "player", player.ResolvedPlayerName);
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", mo.Info.WinNotification, player.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(player, mo.Info.WinTextNotification);
				}
			});
		}
	}
}

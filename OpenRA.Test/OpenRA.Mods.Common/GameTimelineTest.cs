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
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class GameTimelineTest
	{
		// Announce is forced off so Tick never touches TextNotificationsManager — the era logic is self-contained
		// and ignores the actor, so we can drive it directly without a World.
		static GameTimeline MakeTimeline(string eras, string eraTicks)
		{
			var info = new GameTimelineInfo();
			var nodes = new List<MiniYamlNode>
			{
				new("Eras", eras),
				new("EraTicks", eraTicks),
				new("Announce", "false"),
			};

			FieldLoader.Load(info, new MiniYaml("", nodes));
			return new GameTimeline(info);
		}

		static void TickTimes(GameTimeline tl, int count)
		{
			for (var i = 0; i < count; i++)
				((ITick)tl).Tick(null);
		}

		[TestCase(TestName = "Starts at era 0 with the first era's name and zero ticks.")]
		public void Initial()
		{
			var tl = MakeTimeline("Alpha, Bravo, Charlie", "0, 3, 6");
			Assert.That(tl.Ticks, Is.EqualTo(0));
			Assert.That(tl.CurrentEra, Is.EqualTo(0));
			Assert.That(tl.CurrentEraName, Is.EqualTo("Alpha"));
		}

		[TestCase(TestName = "Counts every tick.")]
		public void CountsTicks()
		{
			var tl = MakeTimeline("Alpha, Bravo", "0, 100");
			TickTimes(tl, 5);
			Assert.That(tl.Ticks, Is.EqualTo(5));
		}

		[TestCase(TestName = "Advances one era as each EraTicks threshold is reached.")]
		public void AdvancesAtThresholds()
		{
			var tl = MakeTimeline("Alpha, Bravo, Charlie", "0, 3, 6");

			TickTimes(tl, 2);
			Assert.That(tl.CurrentEra, Is.EqualTo(0), "Should still be the first era before the threshold.");

			TickTimes(tl, 1); // Ticks == 3
			Assert.That(tl.CurrentEra, Is.EqualTo(1));
			Assert.That(tl.CurrentEraName, Is.EqualTo("Bravo"));

			TickTimes(tl, 3); // Ticks == 6
			Assert.That(tl.CurrentEra, Is.EqualTo(2));
			Assert.That(tl.CurrentEraName, Is.EqualTo("Charlie"));
		}

		[TestCase(TestName = "Stops at the final era and clamps the name past the last threshold.")]
		public void ClampsAtFinalEra()
		{
			var tl = MakeTimeline("Alpha, Bravo, Charlie", "0, 3, 6");
			TickTimes(tl, 100);
			Assert.That(tl.CurrentEra, Is.EqualTo(2));
			Assert.That(tl.CurrentEraName, Is.EqualTo("Charlie"));
		}
	}
}

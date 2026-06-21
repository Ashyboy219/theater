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
using System.Globalization;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class PlayerAlloysTest
	{
		static PlayerAlloys MakeAlloys(int initial = 0, int max = 0)
		{
			var info = new PlayerAlloysInfo();
			var nodes = new List<MiniYamlNode>
			{
				new("InitialAlloys", initial.ToString(CultureInfo.InvariantCulture)),
				new("MaxAlloys", max.ToString(CultureInfo.InvariantCulture)),
			};

			FieldLoader.Load(info, new MiniYaml("", nodes));
			return new PlayerAlloys(info);
		}

		[TestCase(TestName = "Starts empty by default and honours InitialAlloys.")]
		public void Initial()
		{
			Assert.That(MakeAlloys().Alloys, Is.EqualTo(0));
			Assert.That(MakeAlloys(initial: 50).Alloys, Is.EqualTo(50));
		}

		[TestCase(TestName = "Giving alloys raises the pool and tracks Earned.")]
		public void Give()
		{
			var a = MakeAlloys();
			a.GiveAlloys(100);
			Assert.That(a.Alloys, Is.EqualTo(100));
			Assert.That(a.Earned, Is.EqualTo(100));
		}

		[TestCase(TestName = "Giving zero or negative is a no-op.")]
		public void GiveNonPositive()
		{
			var a = MakeAlloys(initial: 30);
			a.GiveAlloys(0);
			a.GiveAlloys(-10);
			Assert.That(a.Alloys, Is.EqualTo(30));
			Assert.That(a.Earned, Is.EqualTo(0));
		}

		[TestCase(TestName = "Taking succeeds when affordable and tracks Spent.")]
		public void TakeAffordable()
		{
			var a = MakeAlloys();
			a.GiveAlloys(100);
			Assert.That(a.TakeAlloys(40), Is.True);
			Assert.That(a.Alloys, Is.EqualTo(60));
			Assert.That(a.Spent, Is.EqualTo(40));
		}

		[TestCase(TestName = "Taking more than held fails and leaves the pool untouched.")]
		public void TakeUnaffordable()
		{
			var a = MakeAlloys();
			a.GiveAlloys(50);
			Assert.That(a.TakeAlloys(51), Is.False);
			Assert.That(a.Alloys, Is.EqualTo(50));
			Assert.That(a.Spent, Is.EqualTo(0));
		}

		[TestCase(TestName = "CanTakeAlloys reflects the exact threshold.")]
		public void CanTake()
		{
			var a = MakeAlloys(initial: 50);
			Assert.That(a.CanTakeAlloys(50), Is.True);
			Assert.That(a.CanTakeAlloys(51), Is.False);
		}

		[TestCase(TestName = "MaxAlloys caps the pool and the surplus is not counted as Earned.")]
		public void Cap()
		{
			var a = MakeAlloys(max: 200);
			a.GiveAlloys(500);
			Assert.That(a.Alloys, Is.EqualTo(200));
			Assert.That(a.Earned, Is.EqualTo(200));
		}

		[TestCase(TestName = "CanGiveAlloys is unlimited at MaxAlloys 0 but bounded otherwise.")]
		public void CanGive()
		{
			Assert.That(MakeAlloys().CanGiveAlloys(1_000_000), Is.True);

			var capped = MakeAlloys(initial: 200, max: 200);
			Assert.That(capped.CanGiveAlloys(1), Is.False);
			Assert.That(MakeAlloys(initial: 150, max: 200).CanGiveAlloys(50), Is.True);
		}
	}
}

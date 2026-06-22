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
	sealed class AmbientCiviliansTest
	{
		static AmbientCivilians MakeCivilians(int count, int growthPerEra, int maxPopulation)
		{
			var info = new AmbientCiviliansInfo();
			var nodes = new List<MiniYamlNode>
			{
				new("Count", count.ToString(CultureInfo.InvariantCulture)),
				new("GrowthPerEra", growthPerEra.ToString(CultureInfo.InvariantCulture)),
				new("MaxPopulation", maxPopulation.ToString(CultureInfo.InvariantCulture)),
			};

			FieldLoader.Load(info, new MiniYaml("", nodes));
			return new AmbientCivilians(info);
		}

		[TestCase(TestName = "Era 0 targets the baseline Count.")]
		public void BaselineAtEraZero()
		{
			Assert.That(MakeCivilians(16, 8, 0).TargetPopulation(0), Is.EqualTo(16));
		}

		[TestCase(TestName = "Population grows by GrowthPerEra each era.")]
		public void GrowsPerEra()
		{
			var c = MakeCivilians(16, 8, 0);
			Assert.That(c.TargetPopulation(1), Is.EqualTo(24));
			Assert.That(c.TargetPopulation(2), Is.EqualTo(32));
			Assert.That(c.TargetPopulation(4), Is.EqualTo(48));
		}

		[TestCase(TestName = "GrowthPerEra 0 keeps the population flat at Count.")]
		public void NoGrowth()
		{
			var c = MakeCivilians(16, 0, 0);
			Assert.That(c.TargetPopulation(0), Is.EqualTo(16));
			Assert.That(c.TargetPopulation(5), Is.EqualTo(16));
		}

		[TestCase(TestName = "MaxPopulation caps the target; 0 leaves it uncapped.")]
		public void Cap()
		{
			var capped = MakeCivilians(16, 8, 40);
			Assert.That(capped.TargetPopulation(2), Is.EqualTo(32), "Below the cap is unchanged.");
			Assert.That(capped.TargetPopulation(4), Is.EqualTo(40), "Above the cap is clamped (would be 48).");

			Assert.That(MakeCivilians(16, 8, 0).TargetPopulation(10), Is.EqualTo(96), "0 cap is uncapped.");
		}
	}
}

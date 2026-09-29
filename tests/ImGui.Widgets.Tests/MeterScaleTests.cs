// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the decibel mapping and zone colours shared by the vertical meters. Pure — no ImGui context required.
/// </summary>
[TestClass]
public class MeterScaleTests
{
	[TestMethod]
	public void DbToFraction_EndsOfTheRange()
	{
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(-60f, -60f, 6f));
		Assert.AreEqual(1.0f, MeterScale.DbToFraction(6f, -60f, 6f));
	}

	[TestMethod]
	public void DbToFraction_Midpoint() =>
		Assert.AreEqual(0.5f, MeterScale.DbToFraction(-27f, -60f, 6f), 1e-6f);

	[TestMethod]
	public void DbToFraction_ClampsOutside()
	{
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(-100f, -60f, 6f));
		Assert.AreEqual(1.0f, MeterScale.DbToFraction(20f, -60f, 6f));
	}

	[TestMethod]
	public void DbToFraction_EmptyRangeIsZero()
	{
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(0f, 6f, 6f));
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(0f, 6f, -60f));
	}

	[TestMethod]
	public void DbToFraction_NaNIsZero_InfinitiesClamp()
	{
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(float.NaN, -60f, 6f));
		Assert.AreEqual(1.0f, MeterScale.DbToFraction(float.PositiveInfinity, -60f, 6f));
		Assert.AreEqual(0.0f, MeterScale.DbToFraction(float.NegativeInfinity, -60f, 6f));
	}

	[TestMethod]
	public void ZoneColor_MatchesDbMeterThresholds()
	{
		ImColor amber = new Srgb(0.90f, 0.78f, 0.20f).ToImColor(1.0f);

		Assert.AreEqual(MeterScale.Clip.Value, MeterScale.ZoneColor(0.5f).Value);
		Assert.AreEqual(amber.Value, MeterScale.ZoneColor(-3f).Value);
		Assert.AreEqual(MeterScale.Safe.Value, MeterScale.ZoneColor(-6f).Value);
		Assert.AreEqual(MeterScale.Safe.Value, MeterScale.ZoneColor(-20f).Value);
		Assert.AreNotEqual(MeterScale.Clip.Value, MeterScale.Safe.Value);
	}
}

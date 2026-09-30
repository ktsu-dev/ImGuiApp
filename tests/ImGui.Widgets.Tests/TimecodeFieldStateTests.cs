// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the gesture arithmetic behind TimecodeField: scrub steps with a carried remainder,
/// clamped stepping, the one-second step and range normalization. All pure — no ImGui context
/// required.
/// </summary>
[TestClass]
public class TimecodeFieldStateTests
{
	private const float Tolerance = 1e-6f;

	[TestMethod]
	public void ScrubStep_MovesOneFramePerPixel()
	{
		Assert.AreEqual(5, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.0f, 5.0f, 1.0f, out float remainder));
		Assert.AreEqual(0.0f, remainder, Tolerance);
	}

	[TestMethod]
	public void ScrubStep_CarriesFractions()
	{
		Assert.AreEqual(0, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.0f, 0.4f, 1.0f, out float first));
		Assert.AreEqual(0.4f, first, Tolerance);
		Assert.AreEqual(1, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.4f, 0.7f, 1.0f, out float second));
		Assert.AreEqual(0.1f, second, Tolerance);
	}

	[TestMethod]
	public void ScrubStep_Backwards_CarriesANegativeRemainder()
	{
		Assert.AreEqual(-2, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.0f, -2.5f, 1.0f, out float remainder));
		Assert.AreEqual(-0.5f, remainder, Tolerance);
	}

	[TestMethod]
	public void ScrubStep_WithShift_MovesTen() =>
		Assert.AreEqual(30, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.0f, 3.0f, 10.0f, out _));

	[TestMethod]
	public void ScrubStep_IgnoresNonFiniteInput()
	{
		Assert.AreEqual(0, ImGuiWidgets.TimecodeFieldState.ScrubStep(0.0f, float.NaN, 1.0f, out float remainder));
		Assert.AreEqual(0.0f, remainder);
	}

	[TestMethod]
	public void Step_ClampsWithoutOverflow()
	{
		Assert.AreEqual(int.MaxValue, ImGuiWidgets.TimecodeFieldState.Step(int.MaxValue - 1, 10, 0, int.MaxValue));
		Assert.AreEqual(0, ImGuiWidgets.TimecodeFieldState.Step(0, -1, 0, 100));
	}

	[TestMethod]
	public void OneSecond_IsTheNominalRate()
	{
		Assert.AreEqual(30, ImGuiWidgets.TimecodeFieldState.OneSecond(TimecodeRate.Fps29_97Drop));
		Assert.AreEqual(25, ImGuiWidgets.TimecodeFieldState.OneSecond(TimecodeRate.Fps25));
	}

	[TestMethod]
	public void Normalize_SwapsAReversedRange() =>
		Assert.AreEqual((2, 10), ImGuiWidgets.TimecodeFieldState.Normalize(10, 2));
}

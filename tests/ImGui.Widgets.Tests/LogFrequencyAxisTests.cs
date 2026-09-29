// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the logarithmic frequency axis shared by SpectrumAnalyzer and ParametricEq. Pure — no ImGui
/// context required.
/// </summary>
[TestClass]
public class LogFrequencyAxisTests
{
	[TestMethod]
	public void Constructor_RejectsWhatSpectrumAnalyzerStateRejects()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(0f, 100f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(-1f, 100f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(100f, 100f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(100f, 50f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(float.NaN, 100f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.LogFrequencyAxis(20f, float.PositiveInfinity));
	}

	[TestMethod]
	public void FrequencyToPosition_EndsAreZeroAndOne()
	{
		ImGuiWidgets.LogFrequencyAxis axis = new(20f, 20000f);

		Assert.AreEqual(0f, axis.FrequencyToPosition(20f), 1e-6f);
		Assert.AreEqual(1f, axis.FrequencyToPosition(20000f), 1e-6f);
	}

	[TestMethod]
	public void FrequencyToPosition_AnOctaveIsTheSameWidthEverywhere()
	{
		ImGuiWidgets.LogFrequencyAxis axis = new(20f, 20000f);

		float low = axis.FrequencyToPosition(200f) - axis.FrequencyToPosition(100f);
		float high = axis.FrequencyToPosition(8000f) - axis.FrequencyToPosition(4000f);

		Assert.AreEqual(low, high, 1e-6f);
	}

	[TestMethod]
	public void FrequencyToPosition_NonPositiveIsNegativeInfinity()
	{
		ImGuiWidgets.LogFrequencyAxis axis = new(20f, 20000f);

		Assert.AreEqual(float.NegativeInfinity, axis.FrequencyToPosition(0f));
		Assert.AreEqual(float.NegativeInfinity, axis.FrequencyToPosition(-5f));
	}

	[TestMethod]
	public void PositionToFrequency_InvertsFrequencyToPosition()
	{
		ImGuiWidgets.LogFrequencyAxis axis = new(20f, 20000f);

		foreach (float frequency in new[] { 20f, 63f, 1000f, 12345f, 20000f })
		{
			float roundTrip = axis.PositionToFrequency(axis.FrequencyToPosition(frequency));
			Assert.AreEqual(frequency, roundTrip, frequency * 1e-5f, $"{frequency} Hz");
		}

		float centre = MathF.Sqrt(20f * 20000f);
		Assert.AreEqual(centre, axis.PositionToFrequency(0.5f), centre * 1e-5f);
	}

	[TestMethod]
	public void FirstDecade_IsTheFirstPowerOfTenInRange()
	{
		Assert.AreEqual(100f, new ImGuiWidgets.LogFrequencyAxis(20f, 20000f).FirstDecade);
		Assert.AreEqual(100f, new ImGuiWidgets.LogFrequencyAxis(100f, 1000f).FirstDecade);
		Assert.AreEqual(1f, new ImGuiWidgets.LogFrequencyAxis(0.5f, 10f).FirstDecade);
	}

	[TestMethod]
	public void SpectrumAnalyzerState_DelegatesToItsAxis()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new(32, 30f, 16000f);

		Assert.AreEqual(30f, state.Axis.MinFrequency);
		Assert.AreEqual(16000f, state.Axis.MaxFrequency);
		Assert.AreEqual(state.Axis.FrequencyToPosition(1000f), state.FrequencyToPosition(1000f));
	}
}

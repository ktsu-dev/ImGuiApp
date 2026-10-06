// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the correlation, ballistics and mid/side mapping behind the stereo meters. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class StereoMetersStateTests
{
	private const float SampleRate = 48000f;

	private static float[] Sine(int length, float frequency, float amplitude = 1.0f, float phase = 0.0f)
	{
		float[] samples = new float[length];
		for (int i = 0; i < length; i++)
		{
			samples[i] = amplitude * MathF.Sin((2.0f * MathF.PI * frequency * i / SampleRate) + phase);
		}

		return samples;
	}

	private static float[] Negate(float[] samples)
	{
		float[] result = new float[samples.Length];
		for (int i = 0; i < samples.Length; i++)
		{
			result[i] = -samples[i];
		}

		return result;
	}

	[TestMethod]
	public void Correlate_IdenticalChannelsIsOne()
	{
		float[] tone = Sine(480, 1000f);
		Assert.AreEqual(1.0f, ImGuiWidgets.StereoMetersState.Correlate(tone, tone), 1e-6f);
	}

	[TestMethod]
	public void Correlate_InvertedChannelsIsMinusOne()
	{
		float[] tone = Sine(480, 1000f);
		Assert.AreEqual(-1.0f, ImGuiWidgets.StereoMetersState.Correlate(tone, Negate(tone)), 1e-6f);
	}

	[TestMethod]
	public void Correlate_QuadratureSinesIsZero()
	{
		// 1 kHz at 48 kHz is 48 samples a period, so 480 samples is exactly 10 periods.
		float[] sin = Sine(480, 1000f);
		float[] cos = Sine(480, 1000f, phase: MathF.PI / 2.0f);
		Assert.IsLessThan(1e-3f, MathF.Abs(ImGuiWidgets.StereoMetersState.Correlate(sin, cos)));
	}

	[TestMethod]
	public void Correlate_SilenceIsZero()
	{
		float[] silence = new float[480];
		Assert.AreEqual(0.0f, ImGuiWidgets.StereoMetersState.Correlate(silence, silence));
	}

	[TestMethod]
	public void Correlate_OneSilentSideIsZero()
	{
		Assert.AreEqual(0.0f, ImGuiWidgets.StereoMetersState.Correlate(Sine(480, 1000f), new float[480]));
	}

	[TestMethod]
	public void Correlate_UsesTheShorterSpan()
	{
		float[] left = Sine(100, 1000f);
		float[] right = Sine(50, 700f, phase: 0.4f);

		float expected = ImGuiWidgets.StereoMetersState.Correlate(left.AsSpan(0, 50), right);
		Assert.AreEqual(expected, ImGuiWidgets.StereoMetersState.Correlate(left, right));
	}

	[TestMethod]
	public void Correlate_SkipsNonFiniteSamples()
	{
		float[] left = Sine(480, 1000f);
		float[] right = (float[])left.Clone();
		left[100] = float.NaN;

		Assert.AreEqual(1.0f, ImGuiWidgets.StereoMetersState.Correlate(left, right), 1e-6f);
	}

	[TestMethod]
	public void Push_SmoothsTowardTheBlockCorrelation()
	{
		ImGuiWidgets.StereoMetersState state = new();
		float[] tone = Sine(480, 1000f);

		state.Push(tone, tone, 0.3f);

		Assert.AreEqual(1.0f - MathF.Exp(-1.0f), state.Correlation, 1e-4f);
	}

	[TestMethod]
	public void Push_ZeroIntegrationTimeJumps()
	{
		ImGuiWidgets.StereoMetersState state = new() { IntegrationTime = 0.0f };
		float[] tone = Sine(480, 1000f);

		state.Push(tone, Negate(tone), 0.01f);

		Assert.AreEqual(-1.0f, state.Correlation);
	}

	[TestMethod]
	public void Push_NonPositiveDeltaLeavesTheValue()
	{
		ImGuiWidgets.StereoMetersState state = new();
		float[] tone = Sine(480, 1000f);
		state.Push(tone, tone, 0.1f);
		float before = state.Correlation;

		state.Push(tone, Negate(tone), 0.0f);
		state.Push(tone, Negate(tone), -1.0f);
		state.Push(tone, Negate(tone), float.NaN);

		Assert.AreEqual(BitConverter.SingleToInt32Bits(before), BitConverter.SingleToInt32Bits(state.Correlation));
	}

	[TestMethod]
	public void Push_EmptyBlockDriftsTowardZero()
	{
		ImGuiWidgets.StereoMetersState state = new() { IntegrationTime = 0.0f };
		float[] tone = Sine(480, 1000f);

		state.Push(tone, tone, 0.01f);
		Assert.AreEqual(1.0f, state.Correlation, 1e-6f);

		state.Push([], [], 0.01f);
		Assert.AreEqual(0.0f, state.Correlation);
	}

	[TestMethod]
	public void IntegrationTime_NegativeClampsToZero_NaNRestoresDefault()
	{
		ImGuiWidgets.StereoMetersState state = new() { IntegrationTime = -1.0f };
		Assert.AreEqual(0.0f, state.IntegrationTime);

		state.IntegrationTime = float.NaN;
		Assert.AreEqual(ImGuiWidgets.StereoMetersState.DefaultIntegrationTime, state.IntegrationTime);
		Assert.AreEqual(0.3f, state.IntegrationTime);
	}

	[TestMethod]
	public void Reset_ReturnsCorrelationToZero()
	{
		ImGuiWidgets.StereoMetersState state = new() { IntegrationTime = 0.0f };
		float[] tone = Sine(480, 1000f);
		state.Push(tone, tone, 0.01f);

		state.Reset();

		Assert.AreEqual(0.0f, state.Correlation);
	}

	[TestMethod]
	public void ToMidSide_MonoLiesOnTheMidAxis() =>
		Assert.AreEqual(new Vector2(0.0f, 0.5f), ImGuiWidgets.StereoMetersState.ToMidSide(0.5f, 0.5f));

	[TestMethod]
	public void ToMidSide_OppositePolarityLiesOnTheSideAxis() =>
		Assert.AreEqual(new Vector2(0.5f, 0.0f), ImGuiWidgets.StereoMetersState.ToMidSide(0.5f, -0.5f));

	[TestMethod]
	public void ToMidSide_FullScaleMonoReachesOne() =>
		Assert.AreEqual(1.0f, ImGuiWidgets.StereoMetersState.ToMidSide(1.0f, 1.0f).Y);
}

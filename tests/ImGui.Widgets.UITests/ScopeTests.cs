// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Scope"/> on its own.</summary>
[TestClass]
public sealed class ScopeTests : WidgetTest
{
	private const string Label = "waveform";
	private const string Name = "scope";
	private static readonly Vector2 Size = new(300f, 120f);

	private float[] samples = BuildSine(128, 1f);
	private float amplitude = 1f;

	private static float[] BuildSine(int count, float scale)
	{
		float[] built = new float[count];

		for (int i = 0; i < count; i++)
		{
			built[i] = MathF.Sin(i / (float)count * MathF.Tau) * scale;
		}

		return built;
	}

	private void Draw()
	{
		ImGuiWidgets.Scope(Label, samples, Size, amplitude);
		Mark(Name);
	}

	[TestMethod]
	public void Scope_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The scope reserved {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The scope reserved {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void Scope_PlotsTheSamplesItIsGiven()
	{
		samples = BuildSine(128, 1f);
		Start(Draw);
		byte[] sine = Snapshot();

		samples = BuildSine(128, 0.25f);
		Step(2);

		Assert.IsTrue(PixelsChangedSince(sine) > 0, "A quieter waveform drew the same trace.");
	}

	[TestMethod]
	public void Scope_AmplitudeScalesTheTrace()
	{
		amplitude = 1f;
		Start(Draw);
		byte[] unscaled = Snapshot();

		amplitude = 0.2f;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(unscaled) > 0, "Scaling the amplitude changed nothing on screen.");
	}

	[TestMethod]
	public void Scope_ClampsSamplesToThePlotArea()
	{
		samples = [-8f, 8f, -8f, 8f];
		Start(Draw);

		Rectangle rect = RectOf(Name);
		CapturedFrame frame = Harness.Capture();

		// Out-of-range peaks must be clamped into the box rather than drawn over the rest of the
		// window, so the row just above the scope has to be untouched.
		Rgba32 above = frame.GetPixel(rect.MinX + (rect.Width / 2), Math.Max(rect.MinY - 3, 0));
		Rgba32 inside = frame.GetPixel(rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2));

		Assert.AreNotEqual(inside, above, "The trace drew outside the scope's own rectangle.");
	}

	[TestMethod]
	public void Scope_TooFewSamples_StillDrawsTheFrame()
	{
		samples = [0.5f];
		Start(Draw);

		Assert.IsTrue(IsVisible(Name), "A one-sample scope reserved no layout.");
		AssertSomethingWasDrawn("a one-sample scope");
	}
}

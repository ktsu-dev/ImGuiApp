// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.FlameGraph"/>, the Hexa-backed profile view, on its own.</summary>
[TestClass]
public sealed class FlameGraphTests : WidgetTest
{
	private const string Label = "profile";
	private const string Name = "flame";

	private static readonly FlameGraphSample[] Samples =
	[
		new(0f, 10f, 0, "root"),
		new(0f, 6f, 1, "load"),
		new(6f, 10f, 1, "render"),
	];

	private FlameGraphSample[] samples = Samples;
	private int selected = -1;
	private FlameGraphOptions options = new() { GraphSize = new Vector2(360f, 140f) };

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGuiWidgets.FlameGraph(Label, samples, ref selected, options);
		MarkSpan(Name, origin);
	}

	[TestMethod]
	public void FlameGraph_DrawsItsBars()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Name), "The flame graph drew nothing.");
		AssertSomethingWasDrawn("the flame graph");
	}

	[TestMethod]
	public void FlameGraph_UsesTheGraphSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		// The height is honored exactly; the width is the graph plus the room Hexa keeps for its
		// axis, so the requested width is a floor rather than an exact figure.
		Assert.IsTrue(Math.Abs(rect.Height - 140) <= 4, $"The graph was {rect.Height}px tall rather than 140.");
		Assert.IsTrue(rect.Width >= 360, $"The graph was {rect.Width}px wide, narrower than the 360 requested.");
	}

	// The bars are drawn as a band near the top of the graph, one row per level, with the rest of
	// the requested height left empty. The two level-one samples sit side by side on that band:
	// "load" covers the left of it and "render" the right.
	[TestMethod]
	public void FlameGraph_ClickingABarSelectsIt()
	{
		selected = -1;
		Start(Draw);

		ClickFraction(Name, 0.2f, 0.3f);

		Assert.AreEqual(1, selected, "Clicking the left-hand bar of the second row did not select 'load'.");
	}

	[TestMethod]
	public void FlameGraph_ClickingADifferentBarSelectsThatOne()
	{
		selected = -1;
		Start(Draw);

		ClickFraction(Name, 0.8f, 0.3f);

		Assert.AreEqual(2, selected, "Clicking the right-hand bar of the second row did not select 'render'.");
	}

	[TestMethod]
	public void FlameGraph_ClickingEmptySpaceSelectsNothing()
	{
		selected = -1;
		Start(Draw);

		ClickFraction(Name, 0.5f, 0.9f);

		Assert.AreEqual(-1, selected, "A click below the bars selected one anyway.");
	}

	[TestMethod]
	public void FlameGraph_OutOfRangeSelection_IsResetOnDraw()
	{
		selected = 99;
		Start(Draw);

		Assert.AreEqual(-1, selected, "A selection past the end of the samples was not reset.");
	}

	[TestMethod]
	public void FlameGraph_Flipped_DrawsDifferently()
	{
		options = new FlameGraphOptions { GraphSize = new Vector2(360f, 140f) };
		Start(Draw);
		MoveAway();
		byte[] upward = Snapshot();

		options = new FlameGraphOptions { GraphSize = new Vector2(360f, 140f), Flip = true };
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(upward) > 0, "A flipped graph drew the same as an upward one.");
	}

	[TestMethod]
	public void FlameGraph_OverlayTextIsDrawn()
	{
		options = new FlameGraphOptions { GraphSize = new Vector2(360f, 140f) };
		Start(Draw);
		MoveAway();
		byte[] plain = Snapshot();

		options = new FlameGraphOptions { GraphSize = new Vector2(360f, 140f), OverlayText = "16.6 ms" };
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(plain) > 0, "The overlay text drew nothing.");
	}

	[TestMethod]
	public void FlameGraph_NoSamples_DrawsAnEmptyGraph()
	{
		samples = [];
		Start(Draw);

		Assert.IsTrue(IsVisible(Name), "An empty flame graph reserved no layout.");
		Assert.AreEqual(-1, selected, "An empty flame graph reported a selection.");
	}
}

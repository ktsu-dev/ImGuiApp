// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Lines drawn through the whole pipeline, where Dear ImGui's own geometry meets the rasterizer's
/// texture sampling.
/// </summary>
[TestClass]
public sealed class LineRenderingTests
{
	[TestMethod]
	public void ThickTexturedLine_HasTheSameProfileAlongItsLength()
	{
		// A line of integer thickness is a quad whose edges are anti-aliased by the texture it
		// samples. Centred on a pixel edge, as here, every pixel centre across it lands on a texel
		// boundary, and point sampling picked a side by rounding error, so a straight vertical line
		// came out as a staircase of different widths.
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ => ImGui.GetForegroundDrawList().AddLine(new Vector2(40f, 10f), new Vector2(40f, 150f), 0xFFFFFFFF, 4f),
			},
			new HarnessOptions { Width = 100, Height = 160 });
		harness.Step(2);

		for (int y = 30; y < 140; y += 5)
		{
			for (int x = 34; x < 47; x++)
			{
				// A unit of rounding either way is interpolation noise, not a step.
				int top = harness.Target.GetPixel(x, 30).R;
				int here = harness.Target.GetPixel(x, y).R;
				Assert.IsTrue(Math.Abs(top - here) <= 1, $"Column {x} changes from {top} at row 30 to {here} at row {y}.");
			}
		}
	}

	[TestMethod]
	public void ThickTexturedLine_SoftensItsEdges()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ => ImGui.GetForegroundDrawList().AddLine(new Vector2(40f, 10f), new Vector2(40f, 150f), 0xFFFFFFFF, 4f),
			},
			new HarnessOptions { Width = 100, Height = 160 });
		harness.Step(2);

		// Centred on a pixel edge, the line's own edges fall on pixel centres, which a linear
		// filter shades part way, as a GPU does.
		Rgba32 background = harness.Target.GetPixel(10, 80);
		Rgba32 core = harness.Target.GetPixel(40, 80);
		bool softened = false;
		for (int x = 34; x < 47; x++)
		{
			byte r = harness.Target.GetPixel(x, 80).R;
			softened |= r > background.R && r < core.R;
		}

		Assert.IsTrue(softened, "The line's edges went straight from background to full brightness, with no anti-aliasing.");
	}
}

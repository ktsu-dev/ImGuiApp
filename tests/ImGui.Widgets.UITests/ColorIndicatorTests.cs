// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ColorIndicator"/> on its own.</summary>
[TestClass]
public sealed class ColorIndicatorTests : WidgetTest
{
	private const string Name = "indicator";

	private bool enabled = true;
	private Color color = Color.FromHex("#ff0000");

	private void Draw()
	{
		ImGuiWidgets.ColorIndicator(color.ToImColor(), enabled);
		Mark(Name);
	}

	[TestMethod]
	public void ColorIndicator_ReservesASquare()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(rect.Width > 0, "The indicator reserved no space.");
		Assert.AreEqual(rect.Width, rect.Height, "The indicator was not square.");
	}

	[TestMethod]
	public void ColorIndicator_Enabled_DrawsItsColor()
	{
		enabled = true;
		Start(Draw);

		Rectangle rect = RectOf(Name);
		CapturedFrame frame = Harness.Capture();
		Rgba32 pixel = frame.GetPixel(rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2));

		Assert.IsTrue(pixel.R > pixel.G && pixel.R > pixel.B, $"An indicator set to red drew ({pixel.R}, {pixel.G}, {pixel.B}).");
	}

	[TestMethod]
	public void ColorIndicator_Disabled_DrawsTheFrameColorInstead()
	{
		enabled = true;
		Start(Draw);
		byte[] on = Snapshot();

		enabled = false;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(on) > 0, "A disabled indicator drew its color anyway.");
	}

	[TestMethod]
	public void ColorIndicator_FollowsTheColorItIsGiven()
	{
		color = Color.FromHex("#ff0000");
		Start(Draw);
		byte[] red = Snapshot();

		color = Color.FromHex("#0000ff");
		Step(2);

		Assert.IsTrue(PixelsChangedSince(red) > 0, "Changing the color changed nothing on screen.");
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the linear colour controls: <see cref="ImGuiWidgets.ColorEdit(string, ref Vector4, ImGuiColorEditFlags)"/>,
/// its picker and its button. What they show is sRGB; what they store is linear.
/// </summary>
[TestClass]
public sealed class ColorEditTests : WidgetTest
{
	private const string Name = "Tint";

	// Linear 0.21404 is sRGB 0.5, so mid grey: the swatch draws at 128 and the hex box reads 808080.
	private const float MidGreyLinear = 0.21404114f;

	private Vector4 linear = new(MidGreyLinear, MidGreyLinear, MidGreyLinear, 1f);
	private Vector3 opaque = new(MidGreyLinear, MidGreyLinear, MidGreyLinear);
	private bool changed;
	private bool clicked;

	[TestMethod]
	public void ColorButton_FillsWithTheEncodedColour()
	{
		Start(() => clicked |= ImGuiWidgets.ColorButton("swatch", linear, ImGuiColorEditFlags.NoTooltip, new Vector2(40f, 40f)));

		Rectangle rect = RectOf("swatch");
		Rgba32 pixel = Harness.Capture().GetPixel(rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2));

		Assert.AreEqual(128, pixel.R, 1, $"Linear mid grey should be drawn at sRGB 128, not at its linear value; drew {pixel.R}.");

		Click("swatch");
		Assert.IsTrue(clicked, "Clicking the swatch did not report a click.");
	}

	[TestMethod]
	public void ColorEdit_ShowsTheEncodedHexAndStoresWhatIsTypedAsLinear()
	{
		Start(() => changed |= ImGuiWidgets.ColorEdit(Name, ref linear, ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.NoAlpha));

		ClickFraction(Name, 0.1f);
		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("FF0000");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.IsTrue(changed, "Typing a colour did not report a change.");
		Assert.AreEqual(1f, linear.X, 1e-4f);
		Assert.AreEqual(0f, linear.Y, 1e-4f);
		Assert.AreEqual(1f, linear.W, "Alpha changed though only the colour was typed.");
	}

	[TestMethod]
	public void ColorEdit_TypingMidGreyStoresItsLinearValue()
	{
		Start(() => changed |= ImGuiWidgets.ColorEdit(Name, ref opaque, ImGuiColorEditFlags.DisplayHex));
		opaque = Vector3.One;
		Step();

		ClickFraction(Name, 0.1f);
		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("808080");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		// 0x80 is 128/255, a hair over a half, so its linear value is a hair over mid grey's.
		Assert.AreEqual(0.21586f, opaque.X, 1e-4f, "The hex the user typed was stored without decoding it.");
	}

	[TestMethod]
	public void ColorEdit_LeavesAColourItOnlyShowsExactlyAsStored()
	{
		linear = new Vector4(0.123456789f, 0.5f, 0.75f, 0.3f);
		Vector4 stored = linear;
		Start(() => changed |= ImGuiWidgets.ColorEdit(Name, ref linear));
		Step(3);

		Assert.IsFalse(changed);
		Assert.AreEqual(stored, linear);
	}

	[TestMethod]
	public void ColorPicker_ClickingTheSquareStoresALinearColour()
	{
		linear = new Vector4(1f, 0f, 0f, 1f);
		Start(() => changed |= ImGuiWidgets.ColorPicker(Name, ref linear, ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha));

		// The top-left of the saturation-value square is white whatever the hue.
		Rectangle rect = RectOf(Name);
		Harness.Mouse.Click(rect.MinX + 2, rect.MinY + 2);
		Step();

		Assert.IsTrue(changed, "Clicking the picker did not report a change.");
		Assert.IsGreaterThan(0.9f, linear.Y, $"The picker's white corner stored ({linear.X}, {linear.Y}, {linear.Z}).");
	}

	[TestMethod]
	public void ColorPicker_OpaqueOverloadMarksItself()
	{
		Start(() => ImGuiWidgets.ColorPicker(Name, ref opaque));
		Assert.IsTrue(IsVisible(Name));
	}
}

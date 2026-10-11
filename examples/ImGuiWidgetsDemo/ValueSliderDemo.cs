// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Value sliders: a percentage, a bipolar range filled from zero, a whole-number radius in pixels, a
/// hue in degrees, and a logarithmic brush size, each with the value readable in the middle of its fill.
/// </summary>
internal static class ValueSliderDemo
{
	private static readonly ValueSliderOptions PercentOptions = new() { Format = "0", Unit = "%", ResetValue = 100.0 };
	private static readonly ValueSliderOptions BipolarOptions = new() { Format = "0", Fill = ValueSliderFill.FromZero, ResetValue = 0.0 };
	private static readonly ValueSliderOptions PixelOptions = new() { Unit = " px", ResetValue = 4.0 };
	private static readonly ValueSliderOptions DegreeOptions = new() { Unit = "°", Fill = ValueSliderFill.FromZero };
	private static readonly ValueSliderOptions SizeOptions = new() { Format = "0", Unit = " px", Logarithmic = true };

	private static float opacity = 75f;
	private static float temperature = -20f;
	private static int radius = 12;
	private static float hue;
	private static float brushSize = 20f;

	/// <summary>Gets the opacity slider's value.</summary>
	internal static float Opacity => opacity;

	/// <summary>Gets the temperature slider's value.</summary>
	internal static float Temperature => temperature;

	/// <summary>Gets the radius slider's value.</summary>
	internal static int Radius => radius;

	/// <summary>Gets the logarithmic brush size slider's value.</summary>
	internal static float BrushSize => brushSize;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		opacity = 75f;
		temperature = -20f;
		radius = 12;
		hue = 0f;
		brushSize = 20f;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Value Sliders"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag to change, Shift to go finely, Ctrl+click to type, double-click to reset:");
		ImGui.Separator();

		ImGui.PushItemWidth(240f);
		ImGuiWidgets.ValueSlider("Opacity##demoValueSlider", ref opacity, 0f, 100f, PercentOptions);
		ImGuiWidgets.ValueSlider("Temperature##demoValueSlider", ref temperature, -100f, 100f, BipolarOptions);
		ImGuiWidgets.ValueSlider("Radius##demoValueSlider", ref radius, 0, 64, PixelOptions);
		ImGuiWidgets.ValueSlider("Hue##demoValueSlider", ref hue, -180f, 180f, DegreeOptions);
		ImGuiWidgets.ValueSlider("Brush size (logarithmic)##demoValueSlider", ref brushSize, 1f, 1000f, SizeOptions);
		ImGui.PopItemWidth();
	}
}

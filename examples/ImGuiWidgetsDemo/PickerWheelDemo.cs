// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The picker wheel and the time picker built on it.
/// </summary>
internal static class PickerWheelDemo
{
	private static readonly string[] Sizes = ["Extra small", "Small", "Medium", "Large", "Extra large"];

	private static int size = 2;
	private static int quantity = 6;
	private static TimeOnly alarm = new(7, 30);
	private static bool twelveHour;

	/// <summary>Gets the selected size.</summary>
	internal static string Size => Sizes[size];

	/// <summary>Gets the chosen quantity.</summary>
	internal static int Quantity => quantity;

	/// <summary>Gets the alarm time.</summary>
	internal static TimeOnly Alarm => alarm;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		size = 2;
		quantity = 6;
		alarm = new TimeOnly(7, 30);
		twelveHour = false;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Picker Wheel"))
		{
			ImGui.TextWrapped("Drag a wheel, flick it, click a row above or below the middle, or roll the mouse wheel over it.");

			_ = ImGuiWidgets.PickerWheel("demoSize", ref size, Sizes);
			ImGui.SameLine();
			_ = ImGuiWidgets.PickerWheel("demoQuantity", ref quantity, 1, 99, format: "0");
			ImGui.SameLine();
			_ = ImGuiWidgets.TimePicker("demoAlarm", ref alarm, new TimePickerOptions { TwelveHour = twelveHour, MinuteStep = 5 });

			ImGui.TextUnformatted($"{quantity} x {Sizes[size]}, alarm at {alarm:t}");
			_ = DemoProbe.Checkbox("12-hour clock##demoTwelveHour", ref twelveHour);
		}
	}
}

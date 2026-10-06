// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The mobile form controls: switch, segmented control, chips, stepper and range slider.
/// </summary>
internal static class MobileFormControlsDemo
{
	/// <summary>Gets the value of the Wi-Fi switch.</summary>
	internal static bool SwitchWifi => switchWifi;

	/// <summary>Gets the quantity held by the stepper.</summary>
	internal static int StepperQuantity => stepperQuantity;

	private static bool switchWifi = true;
	private static bool switchBluetooth;
	private static int segmentSelected;
	private static int chipGroupSelected = 1;
	private static readonly List<string> chipTags = ["All", "Unread", "Flagged", "Archived", "Drafts", "Sent", "Spam"];
	private static int stepperQuantity = 3;
	private static float rangeLower = 25.0f;
	private static float rangeUpper = 75.0f;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		switchWifi = true;
		switchBluetooth = false;
		segmentSelected = 0;
		chipGroupSelected = 1;
		stepperQuantity = 3;
		rangeLower = 25.0f;
		rangeUpper = 75.0f;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Form Controls"))
		{
			ImGui.TextUnformatted("Mobile-style form controls (Switch, SegmentedControl, Chip, Stepper, RangeSlider):");
			ImGui.Separator();

			ImGui.TextUnformatted("Switch (iOS-style toggle with animated thumb):");
			ImGuiWidgets.Switch("Wi-Fi##switchWifi", ref switchWifi);
			ImGuiWidgets.Switch("Bluetooth##switchBluetooth", ref switchBluetooth);

			ImGui.Separator();
			ImGui.TextUnformatted("Segmented control (sliding highlight):");
			ImGuiWidgets.SegmentedControl("##viewMode", ref segmentSelected, "Day", "Week", "Month", "Year");
			ImGui.TextUnformatted($"Selected segment index: {segmentSelected}");

			ImGui.Separator();
			ImGui.TextUnformatted("Chips (single-select, wrapping group):");
			ImGuiWidgets.ChipGroup("##chipTags", chipTags, ref chipGroupSelected, allowDeselect: true);
			ImGui.TextUnformatted(chipGroupSelected >= 0 ? $"Filter: {chipTags[chipGroupSelected]}" : "Filter: (none)");

			ImGui.Separator();
			ImGui.TextUnformatted("Stepper (hold +/- to repeat):");
			ImGuiWidgets.Stepper("Quantity##stepperQuantity", ref stepperQuantity, step: 1, min: 0, max: 99);

			ImGui.Separator();
			ImGui.TextUnformatted("Range slider (dual handle, drag either grab):");
			ImGui.SetNextItemWidth(260.0f);
			ImGuiWidgets.RangeSlider("Price##rangePrice", ref rangeLower, ref rangeUpper, 0.0f, 100.0f, minGap: 5.0f);
			ImGui.TextUnformatted($"Range: {rangeLower:F0} - {rangeUpper:F0}");
		}
	}
}

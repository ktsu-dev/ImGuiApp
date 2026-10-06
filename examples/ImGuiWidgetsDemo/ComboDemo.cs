// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Collections.ObjectModel;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Strings;

/// <summary>
/// The combo boxes over an enum, strings and semantic strings.
/// </summary>
internal static class ComboDemo
{
	private static EnumValues selectedEnumValue = EnumValues.Value1;
	private static string selectedStringValue = "Hello";
	private static readonly Collection<string> possibleStringValues = ["Hello", "World", "Goodbye"];
	private static StrongStringExample selectedStrongString = "Strong Hello".As<StrongStringExample>();
	private static readonly Collection<StrongStringExample> possibleStrongStringValues = ["Strong Hello".As<StrongStringExample>(),
		 "Strong World".As<StrongStringExample>(), "Strong Goodbye".As<StrongStringExample>()];

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		selectedEnumValue = EnumValues.Value1;
		selectedStringValue = "Hello";
		selectedStrongString = "Strong Hello".As<StrongStringExample>();
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Combo Boxes"))
		{
			ImGui.TextUnformatted("Type-safe combo boxes for enums and collections:");
			ImGui.Separator();

			ImGuiWidgets.Combo("Enum Combo", ref selectedEnumValue);
			ImGui.TextUnformatted($"Selected: {selectedEnumValue}");

			ImGui.Separator();
			ImGuiWidgets.Combo("String Combo", ref selectedStringValue, possibleStringValues);
			ImGui.TextUnformatted($"Selected: {selectedStringValue}");

			ImGui.Separator();
			ImGuiWidgets.Combo("Strong String Combo", ref selectedStrongString, possibleStrongStringValues);
			ImGui.TextUnformatted($"Selected: {selectedStrongString}");
		}
	}
}

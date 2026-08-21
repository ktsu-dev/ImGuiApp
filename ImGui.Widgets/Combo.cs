// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.ObjectModel;
using System.Globalization;
using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Semantics.Strings;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// An ImGui.Combo implementation that works with Enums.
	/// </summary>
	/// <typeparam name="TEnum">Type of the Enum.</typeparam>
	/// <param name="label">Label for display and id.</param>
	/// <param name="selectedValue">The currently selected value.</param>
	/// <returns>If a combo value was selected.</returns>
	public static bool Combo<TEnum>(string label, ref TEnum selectedValue) where TEnum : Enum
	{
		Array possibleValues = Enum.GetValues(typeof(TEnum));
		int currentIndex = Array.IndexOf(possibleValues, selectedValue);
		string[] possibleValuesNames = Enum.GetNames(typeof(TEnum));
		bool changed = ImGui.Combo(label, ref currentIndex, possibleValuesNames, possibleValuesNames.Length);
		ImGuiProbes.MarkItem(label);
		if (changed)
		{
			selectedValue = (TEnum)possibleValues.GetValue(currentIndex)!;
			return true;
		}

		return false;
	}

	/// <summary>
	/// An ImGui.Combo implementation that works with IStrings.
	/// </summary>
	/// <typeparam name="TString">Type of the StrongString</typeparam>
	/// <param name="label">Label for display and id.</param>
	/// <param name="selectedValue">The currently selected value.</param>
	/// <param name="possibleValues">The collection of possible values.</param>
	/// <returns>If a combo value was selected.</returns>
	public static bool Combo<TString>(string label, ref TString selectedValue, Collection<TString> possibleValues) where TString : ISemanticString
	{
		Ensure.NotNull(possibleValues);

		int currentIndex = possibleValues.IndexOf(selectedValue);
		string[] possibleValuesNames = [.. possibleValues.Select(e => e.ToString(CultureInfo.InvariantCulture))];
		bool changed = ImGui.Combo(label, ref currentIndex, possibleValuesNames, possibleValuesNames.Length);
		ImGuiProbes.MarkItem(label);
		if (changed)
		{
			selectedValue = possibleValues[currentIndex];
			return true;
		}

		return false;
	}

	/// <summary>
	/// An ImGui.Combo implementation that works with strings.
	/// </summary>
	/// <param name="label">Label for display and id.</param>
	/// <param name="selectedValue">The currently selected value.</param>
	/// <param name="possibleValues">The collection of possible values.</param>
	/// <returns>If a combo value was selected.</returns>
	public static bool Combo(string label, ref string selectedValue, Collection<string> possibleValues)
	{
		Ensure.NotNull(possibleValues);

		int currentIndex = possibleValues.IndexOf(selectedValue);
		string[] possibleValuesNames = [.. possibleValues];
		bool changed = ImGui.Combo(label, ref currentIndex, possibleValuesNames, possibleValuesNames.Length);
		ImGuiProbes.MarkItem(label);
		if (changed)
		{
			selectedValue = possibleValues[currentIndex];
			return true;
		}

		return false;
	}
}

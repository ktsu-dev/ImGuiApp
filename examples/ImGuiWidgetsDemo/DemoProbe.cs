// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Submits plain ImGui controls and records them with <see cref="ImGuiProbes"/> so a UI test can
/// address them by label. The widgets from ktsu.ImGui.Widgets and ktsu.ImGui.Popups mark themselves,
/// but the buttons, headers and tabs this demo draws directly are otherwise anonymous.
/// </summary>
internal static class DemoProbe
{
	/// <summary>Submits a button and records it.</summary>
	/// <param name="label">The button label, which doubles as its probe name.</param>
	/// <returns>True when the button was activated.</returns>
	public static bool Button(string label)
	{
		bool clicked = ImGui.Button(label);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Submits a collapsing header and records it, so a test can expand or collapse it.</summary>
	/// <param name="label">The header label, which doubles as its probe name.</param>
	/// <returns>True while the header is expanded.</returns>
	public static bool Header(string label)
	{
		bool open = ImGui.CollapsingHeader(label);
		ImGuiProbes.MarkItem(label);
		return open;
	}

	/// <summary>Submits a tab and records its tab button, so a test can switch tabs by label.</summary>
	/// <param name="label">The tab label, which doubles as its probe name.</param>
	/// <returns>True while the tab is the selected one.</returns>
	public static bool TabItem(string label)
	{
		bool selected = ImGui.BeginTabItem(label);
		ImGuiProbes.MarkItem(label);
		return selected;
	}

	/// <summary>Submits a checkbox and records it.</summary>
	/// <param name="label">The checkbox label, which doubles as its probe name.</param>
	/// <param name="value">The value the checkbox toggles.</param>
	/// <returns>True when the value changed.</returns>
	public static bool Checkbox(string label, ref bool value)
	{
		bool changed = ImGui.Checkbox(label, ref value);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Submits a float slider and records it.</summary>
	/// <param name="label">The slider label, which doubles as its probe name.</param>
	/// <param name="value">The value the slider drives.</param>
	/// <param name="min">The lowest value.</param>
	/// <param name="max">The highest value.</param>
	/// <returns>True when the value changed.</returns>
	public static bool SliderFloat(string label, ref float value, float min, float max)
	{
		bool changed = ImGui.SliderFloat(label, ref value, min, max);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Submits an integer slider and records it.</summary>
	/// <param name="label">The slider label, which doubles as its probe name.</param>
	/// <param name="value">The value the slider drives.</param>
	/// <param name="min">The lowest value.</param>
	/// <param name="max">The highest value.</param>
	/// <returns>True when the value changed.</returns>
	public static bool SliderInt(string label, ref int value, int min, int max)
	{
		bool changed = ImGui.SliderInt(label, ref value, min, max);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Submits a float slider with a display format and records it.</summary>
	/// <param name="label">The slider label, which doubles as its probe name.</param>
	/// <param name="value">The value the slider drives.</param>
	/// <param name="min">The lowest value.</param>
	/// <param name="max">The highest value.</param>
	/// <param name="format">The printf-style format for the displayed value.</param>
	/// <returns>True when the value changed.</returns>
	public static bool SliderFloat(string label, ref float value, float min, float max, string format)
	{
		bool changed = ImGui.SliderFloat(label, ref value, min, max, format);
		ImGuiProbes.MarkItem(label);
		return changed;
	}
}

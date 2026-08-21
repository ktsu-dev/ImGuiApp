// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Submits plain ImGui controls and records them with <see cref="ImGuiProbes"/> so a UI test can
/// address them by label. The widgets from ktsu.ImGui.Widgets mark themselves, but the tabs,
/// buttons, headers and sliders this demo draws directly are otherwise anonymous.
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

	/// <summary>Submits a sized button and records it.</summary>
	/// <param name="label">The button label, which doubles as its probe name.</param>
	/// <param name="size">The button size.</param>
	/// <returns>True when the button was activated.</returns>
	public static bool Button(string label, System.Numerics.Vector2 size)
	{
		bool clicked = ImGui.Button(label, size);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Submits a small button and records it.</summary>
	/// <param name="label">The button label, which doubles as its probe name.</param>
	/// <returns>True when the button was activated.</returns>
	public static bool SmallButton(string label)
	{
		bool clicked = ImGui.SmallButton(label);
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

	/// <summary>Submits a collapsing header with flags and records it.</summary>
	/// <param name="label">The header label, which doubles as its probe name.</param>
	/// <param name="flags">The tree node flags to apply.</param>
	/// <returns>True while the header is expanded.</returns>
	public static bool Header(string label, ImGuiTreeNodeFlags flags)
	{
		bool open = ImGui.CollapsingHeader(label, flags);
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

	/// <summary>Submits a float input and records it.</summary>
	/// <param name="label">The input label, which doubles as its probe name.</param>
	/// <param name="value">The value being edited.</param>
	/// <returns>True when the value changed.</returns>
	public static bool InputFloat(string label, ref float value)
	{
		bool changed = ImGui.InputFloat(label, ref value);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Submits a menu item bound to a flag and records it.</summary>
	/// <param name="label">The menu item label, which doubles as its probe name.</param>
	/// <param name="shortcut">The shortcut text shown beside the label.</param>
	/// <param name="selected">The flag the item toggles.</param>
	/// <returns>True when the item was activated.</returns>
	public static bool MenuItem(string label, string shortcut, ref bool selected)
	{
		bool clicked = ImGui.MenuItem(label, shortcut, ref selected);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Submits a menu item and records it.</summary>
	/// <param name="label">The menu item label, which doubles as its probe name.</param>
	/// <returns>True when the item was activated.</returns>
	public static bool MenuItem(string label)
	{
		bool clicked = ImGui.MenuItem(label);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Submits a float input with step buttons and a display format, and records it.</summary>
	/// <param name="label">The input label, which doubles as its probe name.</param>
	/// <param name="value">The value being edited.</param>
	/// <param name="step">The step applied by the -/+ buttons.</param>
	/// <param name="stepFast">The step applied when the buttons are held.</param>
	/// <param name="format">The printf-style format for the displayed value.</param>
	/// <returns>True when the value changed.</returns>
	public static bool InputFloat(string label, ref float value, float step, float stepFast, string format)
	{
		bool changed = ImGui.InputFloat(label, ref value, step, stepFast, format);
		ImGuiProbes.MarkItem(label);
		return changed;
	}

	/// <summary>Opens a menu and records its bar entry, so a test can open it by name.</summary>
	/// <param name="label">The menu label, which doubles as its probe name.</param>
	/// <returns>True while the menu is open, in which case EndMenu must be called.</returns>
	public static bool BeginMenu(string label)
	{
		bool open = ImGui.BeginMenu(label);
		ImGuiProbes.MarkItem(label);
		return open;
	}
}

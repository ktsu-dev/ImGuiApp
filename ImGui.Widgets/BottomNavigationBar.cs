// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Animation;

/// <summary>One destination of a bottom navigation bar.</summary>
/// <param name="Icon">The glyph drawn above the label.</param>
/// <param name="Label">The destination's name.</param>
public readonly record struct BottomNavigationItem(string Icon, string Label);

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Gets the height a bottom navigation bar takes, in pixels.</summary>
	/// <remarks>
	/// Give the content above the bar a child window <c>-BottomNavigationBarHeight</c> tall, which fills
	/// the window except for the bar, so the bar never covers it.
	/// </remarks>
	public static float BottomNavigationBarHeight => (ImGui.GetTextLineHeight() * 2f) + InterfaceScale.Pixels(22f);

	/// <summary>
	/// A row of destinations along the bottom of the window, each an icon over a label, with the one
	/// that is showing highlighted, as a phone app's tab bar is.
	/// </summary>
	/// <remarks>
	/// The bar moves itself down to the bottom of the space left in the window and takes
	/// <see cref="BottomNavigationBarHeight"/> of it, across the whole width. The bar is probe-marked
	/// under <paramref name="id"/> and each destination under <c>id/label</c>.
	/// </remarks>
	/// <param name="id">A unique id for the bar.</param>
	/// <param name="selected">The index of the destination showing; updated when another is clicked.</param>
	/// <param name="items">The destinations, left to right.</param>
	/// <returns>True on the frame the selection changes.</returns>
	public static bool BottomNavigationBar(string id, ref int selected, IReadOnlyList<BottomNavigationItem> items) =>
		BottomNavigationBarImpl.Draw(id, ref selected, items);

	/// <summary>
	/// A bottom navigation bar over a <see cref="TabPanel"/>'s tabs, sharing its selection: clicking a
	/// destination switches the panel's tab, and switching the panel's tab moves the highlight.
	/// </summary>
	/// <remarks>
	/// Set the panel's <see cref="TabPanel.ShowTabStrip"/> to false so it draws only the active tab, and
	/// draw it in a child window above the bar, as <see cref="BottomNavigationBarHeight"/> describes.
	/// Hidden tabs are left out of the bar.
	/// </remarks>
	/// <param name="id">A unique id for the bar.</param>
	/// <param name="panel">The tab panel whose tabs the bar shows.</param>
	/// <param name="icons">A glyph for each of the panel's tabs, in the panel's order; a missing one draws none.</param>
	/// <returns>True on the frame the bar switches the panel's tab.</returns>
	public static bool BottomNavigationBar(string id, TabPanel panel, IReadOnlyList<string> icons)
	{
		Ensure.NotNull(panel);
		Ensure.NotNull(icons);

		List<BottomNavigationItem> items = [];
		List<int> tabIndices = [];
		int selected = -1;
		for (int i = 0; i < panel.Tabs.Count; i++)
		{
			Tab tab = panel.Tabs[i];
			if (!tab.IsVisible)
			{
				continue;
			}

			if (i == panel.ActiveTabIndex)
			{
				selected = items.Count;
			}

			tabIndices.Add(i);
			items.Add(new BottomNavigationItem(i < icons.Count ? icons[i] : string.Empty, tab.Label));
		}

		if (!BottomNavigationBarImpl.Draw(id, ref selected, items))
		{
			return false;
		}

		panel.ActiveTabIndex = tabIndices[selected];
		return true;
	}

	internal static class BottomNavigationBarImpl
	{
		private sealed class BarEntry
		{
			public Spring Indicator { get; } = new(float.NaN, 300f, 2f * MathF.Sqrt(300f)) { RestThreshold = 0.25f };

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, BarEntry> Entries = [];

		internal static bool Draw(string id, ref int selected, IReadOnlyList<BottomNavigationItem> items)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(items);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();
			if (!Entries.TryGetValue(key, out BarEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new BarEntry();
				Entries[key] = entry;
			}

			entry.LastFrame = frame;

			float height = BottomNavigationBarHeight;
			Vector2 available = ImGui.GetContentRegionAvail();
			if (available.Y > height)
			{
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + available.Y - height);
			}

			Vector2 origin = ImGui.GetCursorScreenPos();
			float width = MathF.Max(available.X, 1f);
			Vector2 max = origin + new Vector2(width, height);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(origin, max, ImGui.GetColorU32(ImGuiCol.MenuBarBg));
			drawList.AddLine(origin, new Vector2(max.X, origin.Y), ImGui.GetColorU32(ImGuiCol.Separator));
			ImGuiProbes.MarkRegion(id, origin, max);

			if (items.Count == 0)
			{
				ImGui.Dummy(new Vector2(width, height));
				return false;
			}

			float itemWidth = width / items.Count;
			bool changed = false;
			ImGui.PushID(id);
			for (int i = 0; i < items.Count; i++)
			{
				ImGui.SetCursorScreenPos(origin + new Vector2(i * itemWidth, 0f));
				ImGui.PushID(i);
				if (ImGui.InvisibleButton("##item", new Vector2(itemWidth, height)) && selected != i)
				{
					selected = i;
					changed = true;
				}

				ImGui.PopID();
				ImGuiProbes.MarkItem(id, items[i].Label);
				WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);
			}

			ImGui.PopID();
			DrawItems(drawList, entry, origin, itemWidth, height, selected, items);
			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(new Vector2(width, height));
			return changed;
		}

		private static void DrawItems(ImDrawListPtr drawList, BarEntry entry, Vector2 origin, float itemWidth, float height, int selected, IReadOnlyList<BottomNavigationItem> items)
		{
			float line = ImGui.GetTextLineHeight();
			float top = origin.Y + ((height - (line * 2f) - InterfaceScale.Pixels(6f)) * 0.5f);
			Vector2 pill = new(MathF.Min(itemWidth - InterfaceScale.Pixels(8f), InterfaceScale.Pixels(56f)), line + InterfaceScale.Pixels(6f));

			if (selected >= 0 && selected < items.Count)
			{
				float target = origin.X + ((selected + 0.5f) * itemWidth);
				if (float.IsNaN(entry.Indicator.Value))
				{
					entry.Indicator.SnapTo(target);
				}

				entry.Indicator.Target = target;
				float x = entry.Indicator.Update(ImGui.GetIO().DeltaTime);
				Vector2 pillMin = new(x - (pill.X * 0.5f), top - InterfaceScale.Pixels(3f));
				drawList.AddRectFilled(pillMin, pillMin + pill, ImGui.GetColorU32(ImGuiCol.HeaderActive), pill.Y * 0.5f);
			}

			for (int i = 0; i < items.Count; i++)
			{
				float center = origin.X + ((i + 0.5f) * itemWidth);
				uint color = ImGui.GetColorU32(i == selected ? ImGuiCol.Text : ImGuiCol.TextDisabled);
				Vector2 iconSize = ImGui.CalcTextSize(items[i].Icon);
				drawList.AddText(new Vector2(center - (iconSize.X * 0.5f), top), color, items[i].Icon);

				string label = items[i].Label;
				Vector2 labelSize = ImGui.CalcTextSize(label);
				Vector2 labelAt = new(MathF.Max(center - (labelSize.X * 0.5f), origin.X + (i * itemWidth)), top + line + InterfaceScale.Pixels(6f));
				drawList.PushClipRect(new Vector2(origin.X + (i * itemWidth), origin.Y), new Vector2(origin.X + ((i + 1) * itemWidth), origin.Y + height), true);
				drawList.AddText(labelAt, color, label);
				drawList.PopClipRect();
			}
		}
	}
}

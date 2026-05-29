// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a compact, pill-shaped chip. Filled with the accent colour when <paramref name="selected"/>
	/// is <see langword="true"/>, otherwise outlined. Useful for filter / choice tags.
	/// </summary>
	/// <param name="label">The chip caption; text after <c>##</c> is hidden but used for the ID.</param>
	/// <param name="selected">Whether the chip is in its selected (filled) state.</param>
	/// <returns><see langword="true"/> if the chip was clicked this frame; otherwise <see langword="false"/>.</returns>
	public static bool Chip(string label, bool selected = false) => ChipImpl.Draw(label, selected, false, out _);

	/// <summary>
	/// Draws a closable chip with a trailing <c>×</c> button (an "input" chip).
	/// </summary>
	/// <param name="label">The chip caption; text after <c>##</c> is hidden but used for the ID.</param>
	/// <param name="selected">Whether the chip is in its selected (filled) state.</param>
	/// <param name="closeClicked">Set to <see langword="true"/> when the trailing close button was clicked this frame.</param>
	/// <returns><see langword="true"/> if the chip body was clicked this frame; otherwise <see langword="false"/>.</returns>
	public static bool Chip(string label, bool selected, out bool closeClicked) => ChipImpl.Draw(label, selected, true, out closeClicked);

	/// <summary>
	/// Draws a horizontal, wrapping group of single-select chips.
	/// </summary>
	/// <param name="label">A unique label used for the widget ID; not drawn.</param>
	/// <param name="options">The chip captions.</param>
	/// <param name="selectedIndex">The selected chip index. Updated in place when a chip is clicked.</param>
	/// <param name="allowDeselect">When <see langword="true"/>, clicking the active chip clears the selection (sets it to -1).</param>
	/// <returns><see langword="true"/> if the selection changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool ChipGroup(string label, IReadOnlyList<string> options, ref int selectedIndex, bool allowDeselect = false) =>
		ChipImpl.DrawGroup(label, options, ref selectedIndex, allowDeselect);

	internal static class ChipImpl
	{
		public static bool Draw(string label, bool selected, bool closable, out bool closeClicked)
		{
			closeClicked = false;
			string visible = VisibleLabel(label);

			ImGuiStylePtr style = ImGui.GetStyle();
			float height = ImGui.GetFrameHeight();
			float rounding = height * 0.5f;
			float padX = style.FramePadding.X * 1.5f;

			Vector2 textSize = ImGui.CalcTextSize(visible);
			float closeSize = closable ? height * 0.55f : 0.0f;
			float closeGap = closable ? style.ItemInnerSpacing.X : 0.0f;
			float width = (padX * 2.0f) + textSize.X + closeGap + closeSize;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton(label, new Vector2(width, height));

			bool hovered = ImGui.IsItemHovered();
			bool bodyClicked = ImGui.IsItemClicked();

			Span<Vector4> colors = style.Colors;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			Vector2 max = new(origin.X + width, origin.Y + height);
			if (selected)
			{
				Vector4 fill = hovered ? colors[(int)ImGuiCol.ButtonHovered] : colors[(int)ImGuiCol.ButtonActive];
				drawList.AddRectFilled(origin, max, ImGui.GetColorU32(fill), rounding);
			}
			else
			{
				if (hovered)
				{
					drawList.AddRectFilled(origin, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBgHovered]), rounding);
				}

				drawList.AddRect(origin, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]), rounding);
			}

			Vector2 textPos = new(origin.X + padX, origin.Y + ((height - textSize.Y) * 0.5f));
			drawList.AddText(textPos, ImGui.GetColorU32(colors[(int)ImGuiCol.Text]), visible);

			if (closable)
			{
				Vector2 closeCenter = new(max.X - padX - (closeSize * 0.5f), origin.Y + (height * 0.5f));
				float r = closeSize * 0.5f;
				Vector2 closeMin = new(closeCenter.X - r, closeCenter.Y - r);
				Vector2 closeMax = new(closeCenter.X + r, closeCenter.Y + r);

				// Hit-test the close glyph against the mouse without consuming the body button.
				Vector2 mouse = ImGui.GetIO().MousePos;
				bool overClose = mouse.X >= closeMin.X && mouse.X <= closeMax.X && mouse.Y >= closeMin.Y && mouse.Y <= closeMax.Y;
				if (overClose && ImGui.IsItemClicked())
				{
					closeClicked = true;
					bodyClicked = false;
				}

				uint glyphColor = ImGui.GetColorU32(overClose ? colors[(int)ImGuiCol.Text] : colors[(int)ImGuiCol.TextDisabled]);
				float k = r * 0.5f;
				drawList.AddLine(new Vector2(closeCenter.X - k, closeCenter.Y - k), new Vector2(closeCenter.X + k, closeCenter.Y + k), glyphColor, 1.5f);
				drawList.AddLine(new Vector2(closeCenter.X - k, closeCenter.Y + k), new Vector2(closeCenter.X + k, closeCenter.Y - k), glyphColor, 1.5f);
			}

			return bodyClicked;
		}

		public static bool DrawGroup(string label, IReadOnlyList<string> options, ref int selectedIndex, bool allowDeselect)
		{
			Ensure.NotNull(options);

			ImGui.PushID(label);
			bool changed = false;
			ImGuiStylePtr style = ImGui.GetStyle();
			float rightEdge = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;

			for (int i = 0; i < options.Count; i++)
			{
				if (Draw($"{options[i]}##chip{i}", i == selectedIndex, false, out _))
				{
					int next = allowDeselect && i == selectedIndex ? -1 : i;
					if (next != selectedIndex)
					{
						selectedIndex = next;
						changed = true;
					}
				}

				// Keep the next chip on this line only if it fits before the content edge.
				if (i + 1 < options.Count)
				{
					float lastRight = ImGui.GetItemRectMax().X;
					float nextWidth = ImGui.CalcTextSize(VisibleLabel(options[i + 1])).X + (style.FramePadding.X * 3.0f);
					if (lastRight + style.ItemSpacing.X + nextWidth < rightEdge)
					{
						ImGui.SameLine();
					}
				}
			}

			ImGui.PopID();
			return changed;
		}
	}
}

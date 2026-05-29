// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a row of dots representing the pages of a carousel or paged view, with the current page
	/// highlighted. When <paramref name="interactive"/> is true the dots are clickable and the clicked page
	/// index is returned.
	/// </summary>
	/// <param name="id">Unique widget identifier.</param>
	/// <param name="currentPage">The zero-based index of the active page.</param>
	/// <param name="pageCount">The total number of pages.</param>
	/// <param name="interactive">When true, clicking a dot selects that page.</param>
	/// <param name="dotSize">Diameter of an inactive dot in pixels. When 0, derives from the text line height so it scales with DPI.</param>
	/// <returns>The selected page index, clamped to a valid range. Equals <paramref name="currentPage"/> unless a different dot was clicked.</returns>
	public static int PageIndicator(string id, int currentPage, int pageCount, bool interactive = false, float dotSize = 0f) =>
		PageIndicatorImpl.Draw(id, currentPage, pageCount, interactive, dotSize);

	/// <summary>
	/// Clamps a page index to the range 0..<paramref name="pageCount"/> - 1, returning 0 when there are no pages.
	/// </summary>
	/// <param name="page">The page index to clamp.</param>
	/// <param name="pageCount">The total number of pages.</param>
	/// <returns>The clamped page index.</returns>
	public static int ClampPage(int page, int pageCount) => pageCount <= 0 ? 0 : Math.Clamp(page, 0, pageCount - 1);

	internal static class PageIndicatorImpl
	{
		internal static int Draw(string id, int currentPage, int pageCount, bool interactive, float dotSize)
		{
			int selected = ClampPage(currentPage, pageCount);
			if (pageCount <= 0)
			{
				ImGui.Dummy(Vector2.Zero);
				return selected;
			}

			float radius = (dotSize > 0f ? dotSize : ImGui.GetTextLineHeight() * 0.4f) * 0.5f;
			float activeRadius = radius * 1.35f;
			float spacing = ImGui.GetStyle().ItemInnerSpacing.X + (radius * 2.0f);
			float slot = (activeRadius * 2.0f) + spacing;
			float height = activeRadius * 2.0f;
			float width = (slot * pageCount) - spacing;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint activeColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
			uint inactiveColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);

			for (int i = 0; i < pageCount; i++)
			{
				Vector2 center = new(origin.X + activeRadius + (i * slot), origin.Y + (height * 0.5f));
				bool isActive = i == selected;
				drawList.AddCircleFilled(center, isActive ? activeRadius : radius, isActive ? activeColor : inactiveColor, 0);

				if (interactive)
				{
					ImGui.SetCursorScreenPos(new Vector2(center.X - activeRadius, origin.Y));
					if (ImGui.InvisibleButton(string.Create(CultureInfo.InvariantCulture, $"{id}_dot{i}"), new Vector2(activeRadius * 2.0f, height)))
					{
						selected = i;
					}
				}
			}

			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(new Vector2(width, height));
			return selected;
		}
	}
}

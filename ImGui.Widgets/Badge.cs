// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Overlays a count badge on the top-right corner of the most recently submitted item. Nothing is drawn
	/// when <paramref name="count"/> is zero or negative; counts above <paramref name="maxCount"/> render as
	/// "{maxCount}+". Call this immediately after the item it decorates.
	/// </summary>
	/// <param name="count">The count to display.</param>
	/// <param name="maxCount">The largest exact value shown before switching to the "N+" form.</param>
	public static void Badge(int count, int maxCount = 99)
	{
		string text = FormatBadgeCount(count, maxCount);
		if (text.Length != 0)
		{
			BadgeImpl.DrawText(text, Palette.Semantic.Error.Value);
		}
	}

	/// <summary>
	/// Overlays a text badge on the top-right corner of the most recently submitted item. Call this
	/// immediately after the item it decorates.
	/// </summary>
	/// <param name="text">The badge text.</param>
	public static void Badge(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			BadgeImpl.DrawText(text, Palette.Semantic.Error.Value);
		}
	}

	/// <summary>
	/// Overlays a small solid dot on the top-right corner of the most recently submitted item (a presence /
	/// unread indicator with no count). Call this immediately after the item it decorates.
	/// </summary>
	public static void BadgeDot() => BadgeImpl.DrawDot(Palette.Semantic.Error.Value);

	/// <summary>
	/// Formats a badge count: an empty string for non-positive counts, the count itself when within
	/// <paramref name="maxCount"/>, otherwise "{maxCount}+".
	/// </summary>
	/// <param name="count">The count to format.</param>
	/// <param name="maxCount">The inclusive threshold above which the "N+" form is used.</param>
	/// <returns>The formatted badge label.</returns>
	public static string FormatBadgeCount(int count, int maxCount)
	{
		if (count <= 0)
		{
			return string.Empty;
		}

		return count > maxCount
			? string.Create(CultureInfo.InvariantCulture, $"{maxCount}+")
			: count.ToString(CultureInfo.InvariantCulture);
	}

	internal static class BadgeImpl
	{
		internal static void DrawText(string text, Vector4 background)
		{
			Vector2 anchor = ImGui.GetItemRectMax() with { Y = ImGui.GetItemRectMin().Y };

			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 textSize = ImGui.CalcTextSize(text);
			float paddingX = MathF.Max(style.FramePadding.X * 0.5f, 3.0f);
			float height = textSize.Y + (paddingX * 2.0f);
			float width = MathF.Max(textSize.X + (paddingX * 2.0f), height);

			Vector2 max = new(anchor.X + (width * 0.5f), anchor.Y + (height * 0.5f));
			Vector2 min = new(max.X - width, max.Y - height);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(background), height * 0.5f);

			Vector2 textPos = new(min.X + ((width - textSize.X) * 0.5f), min.Y + ((height - textSize.Y) * 0.5f));
			drawList.AddText(textPos, ImGui.GetColorU32(Vector4.One), text);
		}

		internal static void DrawDot(Vector4 background)
		{
			Vector2 anchor = ImGui.GetItemRectMax() with { Y = ImGui.GetItemRectMin().Y };
			float radius = MathF.Max(ImGui.GetTextLineHeight() * 0.25f, 4.0f);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddCircleFilled(anchor, radius, ImGui.GetColorU32(background), 0);
		}
	}
}

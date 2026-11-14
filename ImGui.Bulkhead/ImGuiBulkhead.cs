// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// The Bulkhead widgets: bars with rounded end caps, pill buttons, elbows, headers, segmented
/// gauges and framed panels, in an LCARS-inspired starship-console look.
/// </summary>
public static class ImGuiBulkhead
{
	private const float HoverLighten = 0.2f;
	private const float PressDarken = 0.15f;

	/// <summary>Draws a solid bar, optionally with rounded end caps and an upper-case label at its far end.</summary>
	/// <param name="id">The bar's ImGui id, also its probe name.</param>
	/// <param name="size">The bar's size. A zero height uses the frame height; a non-positive width follows <c>ImGui</c>'s conventions.</param>
	/// <param name="color">The fill, or <see cref="BulkheadPalette.Amber"/>.</param>
	/// <param name="caps">Which ends are rounded.</param>
	/// <param name="label">Optional text drawn in ink at the bar's end.</param>
	public static void Bar(string id, Vector2 size, Vector4? color = null, BulkheadCaps caps = BulkheadCaps.None, string? label = null)
	{
		Ensure.NotNull(id);
		Vector2 resolved = ResolveSize(size, 0f);
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + resolved;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		BulkheadDraw.Capsule(drawList, min, max, ImGui.GetColorU32(color ?? BulkheadPalette.Amber), caps);
		if (!string.IsNullOrEmpty(label))
		{
			DrawEndLabel(drawList, min, max, BulkheadDraw.DisplayText(label), caps);
		}

		ImGui.Dummy(resolved);
		ImGuiProbes.MarkItem(id);
	}

	/// <summary>Draws a pill button with its upper-case label in the lower-right corner.</summary>
	/// <param name="label">The label, which may carry a <c>##</c> id suffix.</param>
	/// <param name="size">The size. A zero width fits the label; a zero height uses the frame height.</param>
	/// <param name="color">The fill, or <see cref="BulkheadPalette.Amber"/>.</param>
	/// <param name="caps">Which ends are rounded.</param>
	/// <returns>Whether the button was clicked this frame.</returns>
	public static bool Button(string label, Vector2 size = default, Vector4? color = null, BulkheadCaps caps = BulkheadCaps.Both)
	{
		Ensure.NotNull(label);
		string text = BulkheadDraw.DisplayText(label);
		float height = size.Y > 0f ? size.Y : ImGui.GetFrameHeight();
		float natural = Math.Max(ImGui.CalcTextSize(text).X + (height * 2.5f), height * 3f);
		Vector2 resolved = new(BulkheadDraw.ResolveWidth(size.X, natural), height);

		bool clicked = ImGui.InvisibleButton(label, resolved);
		ImGuiProbes.MarkItem(label);
		bool hovered = ImGui.IsItemHovered();
		bool active = ImGui.IsItemActive();
		if (hovered || active)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		Vector4 fill = InteractionShade(color ?? BulkheadPalette.Amber, hovered, active);
		Vector2 min = ImGui.GetItemRectMin();
		Vector2 max = ImGui.GetItemRectMax();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		BulkheadDraw.Capsule(drawList, min, max, ImGui.GetColorU32(fill), caps);
		DrawEndLabel(drawList, min, max, text, caps);
		return clicked;
	}

	/// <summary>Draws a pill that switches between an on and an off colour when clicked.</summary>
	/// <param name="label">The label, which may carry a <c>##</c> id suffix.</param>
	/// <param name="value">The state, flipped by a click.</param>
	/// <param name="size">The size, as for <see cref="Button"/>.</param>
	/// <param name="onColor">The fill while on, or <see cref="BulkheadPalette.Seafoam"/>.</param>
	/// <param name="offColor">The fill while off, or <see cref="BulkheadPalette.Steel"/>.</param>
	/// <returns>Whether the value changed this frame.</returns>
	public static bool Toggle(string label, ref bool value, Vector2 size = default, Vector4? onColor = null, Vector4? offColor = null)
	{
		Vector4 fill = value ? onColor ?? BulkheadPalette.Seafoam : offColor ?? BulkheadPalette.Steel;
		if (Button(label, size, fill))
		{
			value = !value;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Draws an elbow: a vertical column that turns through a rounded corner into a horizontal bar,
	/// the joint every Bulkhead frame is built from.
	/// </summary>
	/// <param name="id">The elbow's ImGui id, also its probe name.</param>
	/// <param name="size">The rectangle the elbow fills.</param>
	/// <param name="corner">Which corner of the rectangle the bend sits in.</param>
	/// <param name="columnWidth">The width of the vertical column.</param>
	/// <param name="barThickness">The thickness of the horizontal bar.</param>
	/// <param name="color">The fill, or <see cref="BulkheadPalette.Amber"/>.</param>
	/// <param name="label">Optional upper-case text drawn inside the column.</param>
	public static void Elbow(string id, Vector2 size, BulkheadCorner corner, float columnWidth, float barThickness, Vector4? color = null, string? label = null)
	{
		Ensure.NotNull(id);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		DrawElbow(drawList, min, size, corner, columnWidth, barThickness, color ?? BulkheadPalette.Amber, label);
		ImGui.Dummy(size);
		ImGuiProbes.MarkItem(id);
	}

	/// <summary>Draws a section header: a rounded cap, the title in the bar's colour, and a bar running to the end.</summary>
	/// <param name="title">The title, which may carry a <c>##</c> id suffix.</param>
	/// <param name="width">The width; zero or negative follows <c>ImGui</c>'s conventions.</param>
	/// <param name="color">The colour, or <see cref="BulkheadPalette.Lilac"/>.</param>
	public static void Header(string title, float width = 0f, Vector4? color = null)
	{
		Ensure.NotNull(title);
		string text = BulkheadDraw.DisplayText(title);
		float height = ImGui.GetFrameHeight();
		float resolvedWidth = BulkheadDraw.ResolveWidth(width, Math.Max(1f, ImGui.GetContentRegionAvail().X));

		Vector4 fill = color ?? BulkheadPalette.Lilac;
		uint fillColor = ImGui.GetColorU32(fill);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float gap = height * 0.25f;
		float capWidth = height * 1.25f;
		Vector2 textSize = ImGui.CalcTextSize(text);

		BulkheadDraw.Capsule(drawList, min, min + new Vector2(capWidth, height), fillColor, BulkheadCaps.Start);
		float textX = min.X + capWidth + gap;
		drawList.AddText(new Vector2(textX, min.Y + ((height - textSize.Y) * 0.5f)), fillColor, text);
		float barStart = textX + textSize.X + gap;
		Vector2 max = min + new Vector2(resolvedWidth, height);
		if (barStart < max.X)
		{
			BulkheadDraw.Capsule(drawList, new Vector2(barStart, min.Y), max, fillColor, BulkheadCaps.End);
		}

		ImGui.Dummy(new Vector2(resolvedWidth, height));
		ImGuiProbes.MarkItem(title);
	}

	/// <summary>Draws a segmented gauge, the filled segments in colour and the rest dimmed.</summary>
	/// <param name="id">The gauge's ImGui id, also its probe name.</param>
	/// <param name="fraction">How full the gauge is, clamped to [0, 1]; a part-filled segment counts as filled from one half up.</param>
	/// <param name="size">The size. A zero height uses the frame height; a non-positive width follows <c>ImGui</c>'s conventions.</param>
	/// <param name="segments">The number of segments, at least one.</param>
	/// <param name="color">The fill, or <see cref="BulkheadPalette.Ice"/>.</param>
	public static void Gauge(string id, float fraction, Vector2 size = default, int segments = 12, Vector4? color = null)
	{
		Ensure.NotNull(id);
		Vector2 resolved = ResolveSize(size, ImGui.GetFrameHeight() * 8f);
		int count = Math.Max(1, segments);
		int lit = FilledSegments(fraction, count);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float gap = Math.Max(1f, resolved.Y * 0.15f);
		float segmentWidth = (resolved.X - (gap * (count - 1))) / count;
		uint litColor = ImGui.GetColorU32(color ?? BulkheadPalette.Ice);
		uint darkColor = ImGui.GetColorU32(BulkheadPalette.Fade(BulkheadPalette.Steel, 0.6f));
		for (int i = 0; i < count && segmentWidth > 0f; i++)
		{
			Vector2 segMin = new(min.X + (i * (segmentWidth + gap)), min.Y);
			Vector2 segMax = segMin + new Vector2(segmentWidth, resolved.Y);
			BulkheadCaps caps = (i == 0 ? BulkheadCaps.Start : BulkheadCaps.None) | (i == count - 1 ? BulkheadCaps.End : BulkheadCaps.None);
			uint fill = i < lit ? litColor : darkColor;
			if (caps == BulkheadCaps.None)
			{
				drawList.AddRectFilled(segMin, segMax, fill);
			}
			else
			{
				drawList.AddRectFilled(segMin, segMax, fill, resolved.Y * 0.5f, BulkheadDraw.CornerFlags(caps, wide: true));
			}
		}

		ImGui.Dummy(resolved);
		ImGuiProbes.MarkItem(id);
	}

	/// <summary>
	/// Draws a framed panel: an elbow across the top carrying the title, a column down the left
	/// (or a sidebar of the caller's own controls), an elbow across the bottom, and the content to
	/// the right of the column.
	/// </summary>
	/// <param name="title">The title, which may carry a <c>##</c> id suffix.</param>
	/// <param name="size">The panel's size; non-positive components follow <c>ImGui</c>'s conventions.</param>
	/// <param name="content">Draws the panel's content, inside a child window.</param>
	/// <param name="sidebar">Optionally draws controls into the column, usually <see cref="Button"/>s as wide as it is.</param>
	/// <param name="options">The frame's proportions and colours.</param>
	public static void Panel(string title, Vector2 size, Action content, Action? sidebar = null, BulkheadPanelOptions? options = null)
	{
		Ensure.NotNull(title);
		Ensure.NotNull(content);
		BulkheadPanelOptions resolvedOptions = options ?? new BulkheadPanelOptions();
		float frame = ImGui.GetFrameHeight();
		Vector2 avail = ImGui.GetContentRegionAvail();
		Vector2 resolved = new(
			size.X > 0f ? size.X : Math.Max(1f, avail.X + size.X),
			size.Y > 0f ? size.Y : Math.Max(1f, avail.Y + size.Y));
		PanelLayout layout = PanelLayout.For(resolved, frame, resolvedOptions);

		Vector4 color = resolvedOptions.Color ?? BulkheadPalette.Amber;
		Vector4 columnColor = resolvedOptions.ColumnColor ?? BulkheadPalette.Shade(color, -0.25f);
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		uint background = ImGui.GetColorU32(BulkheadPalette.Background);

		ImGui.PushID(title);

		// Top elbow, with the title cut into its bar on a black ground.
		DrawElbow(drawList, origin, layout.TopElbow, BulkheadCorner.TopLeft, layout.ColumnWidth, layout.BarThickness, color, null);
		string text = BulkheadDraw.DisplayText(title);
		Vector2 textSize = ImGui.CalcTextSize(text);
		float textRight = origin.X + resolved.X - (layout.BarThickness * 1.5f);
		Vector2 boxMin = new(textRight - textSize.X - (frame * 0.5f), origin.Y);
		Vector2 boxMax = new(textRight + (frame * 0.25f), origin.Y + layout.BarThickness);
		if (boxMin.X > origin.X + layout.ColumnWidth + layout.BarThickness)
		{
			drawList.AddRectFilled(boxMin, boxMax, background);
			drawList.AddText(new Vector2(boxMin.X + (frame * 0.25f), origin.Y + ((layout.BarThickness - textSize.Y) * 0.5f)), ImGui.GetColorU32(color), text);
		}

		// Bottom elbow.
		Vector2 bottomOrigin = origin + new Vector2(0f, resolved.Y - layout.BottomElbow.Y);
		DrawElbow(drawList, bottomOrigin, layout.BottomElbow, BulkheadCorner.BottomLeft, layout.ColumnWidth, layout.BarThickness, color, null);

		// The column between them: the caller's sidebar, or one plain block.
		Vector2 columnMin = origin + new Vector2(0f, layout.TopElbow.Y + resolvedOptions.Gap);
		Vector2 columnSize = new(layout.ColumnWidth, layout.ColumnHeight);
		if (columnSize.Y > 0f)
		{
			if (sidebar is null)
			{
				drawList.AddRectFilled(columnMin, columnMin + columnSize, ImGui.GetColorU32(columnColor));
			}
			else
			{
				ImGui.SetCursorScreenPos(columnMin);
				if (ImGui.BeginChild("##sidebar", columnSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, resolvedOptions.Gap));
					sidebar();
					ImGui.PopStyleVar();
				}

				ImGui.EndChild();
			}
		}

		// Content, right of the column and between the bars.
		Vector2 contentMin = origin + layout.ContentOffset;
		if (layout.ContentSize.X > 0f && layout.ContentSize.Y > 0f)
		{
			ImGui.SetCursorScreenPos(contentMin);
			if (ImGui.BeginChild("##content", layout.ContentSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoBackground))
			{
				content();
			}

			ImGui.EndChild();
		}

		ImGui.PopID();

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(resolved);
		ImGuiProbes.MarkItem(title);
	}

	/// <summary>How many of <paramref name="segments"/> a gauge at <paramref name="fraction"/> lights.</summary>
	internal static int FilledSegments(float fraction, int segments)
	{
		if (float.IsNaN(fraction))
		{
			return 0;
		}

		float clamped = Math.Clamp(fraction, 0f, 1f);
		return Math.Clamp((int)MathF.Round(clamped * segments, MidpointRounding.AwayFromZero), 0, segments);
	}

	private static Vector2 ResolveSize(Vector2 size, float naturalWidth)
	{
		float height = size.Y > 0f ? size.Y : ImGui.GetFrameHeight();
		float natural = naturalWidth > 0f ? naturalWidth : Math.Max(1f, ImGui.GetContentRegionAvail().X);
		return new Vector2(BulkheadDraw.ResolveWidth(size.X, natural), height);
	}

	private static Vector4 InteractionShade(Vector4 color, bool hovered, bool active)
	{
		if (active)
		{
			return BulkheadPalette.Shade(color, -PressDarken);
		}

		return hovered ? BulkheadPalette.Shade(color, HoverLighten) : color;
	}

	private static void DrawEndLabel(ImDrawListPtr drawList, Vector2 min, Vector2 max, string text, BulkheadCaps caps)
	{
		if (text.Length == 0)
		{
			return;
		}

		Vector2 size = max - min;
		Vector2 textSize = ImGui.CalcTextSize(text);
		float inset = caps.HasFlag(BulkheadCaps.End) ? size.Y * 0.5f : size.Y * 0.25f;
		float x = Math.Max(min.X + (size.Y * 0.25f), max.X - inset - textSize.X);
		float y = max.Y - textSize.Y - Math.Max(1f, (size.Y - textSize.Y) * 0.15f);
		drawList.PushClipRect(min, max, true);
		drawList.AddText(new Vector2(x, y), ImGui.GetColorU32(BulkheadPalette.Ink), text);
		drawList.PopClipRect();
	}

	private static void DrawElbow(ImDrawListPtr drawList, Vector2 min, Vector2 size, BulkheadCorner corner, float columnWidth, float barThickness, Vector4 color, string? label)
	{
		BulkheadDraw.Elbow(drawList, min, size, corner, columnWidth, barThickness, ImGui.GetColorU32(color));
		if (string.IsNullOrEmpty(label))
		{
			return;
		}

		string text = BulkheadDraw.DisplayText(label);
		Vector2 textSize = ImGui.CalcTextSize(text);
		bool right = corner is BulkheadCorner.TopRight or BulkheadCorner.BottomRight;
		bool bottom = corner is BulkheadCorner.BottomLeft or BulkheadCorner.BottomRight;
		float columnLeft = right ? min.X + size.X - columnWidth : min.X;
		float x = columnLeft + columnWidth - textSize.X - 4f;
		float y = bottom ? min.Y + 4f : min.Y + size.Y - textSize.Y - 4f;
		drawList.AddText(new Vector2(Math.Max(columnLeft + 2f, x), y), ImGui.GetColorU32(BulkheadPalette.Ink), text);
	}
}

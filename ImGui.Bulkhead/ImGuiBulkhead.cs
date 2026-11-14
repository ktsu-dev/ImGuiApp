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
	/// Opens a framed panel: an elbow across the top carrying the title, a column down the left,
	/// and an elbow across the bottom. Draw into it through <see cref="BulkheadPanel.Sidebar"/> and
	/// <see cref="BulkheadPanel.Content"/>, each a scope of its own, and dispose the panel to close it.
	/// </summary>
	/// <example>
	/// <code>
	/// using (BulkheadPanel panel = ImGuiBulkhead.Panel("Main Console", Vector2.Zero))
	/// {
	/// 	using (panel.Sidebar()) { ImGuiBulkhead.Button("Status", new Vector2(-1f, 0f)); }
	/// 	using (panel.Content()) { ImGui.TextUnformatted("All systems nominal"); }
	/// }
	/// </code>
	/// </example>
	/// <param name="title">The title, which may carry a <c>##</c> id suffix.</param>
	/// <param name="size">The panel's size; non-positive components follow <c>ImGui</c>'s conventions.</param>
	/// <param name="options">The frame's proportions and colours.</param>
	/// <returns>The panel scope; dispose it to close the panel.</returns>
	public static BulkheadPanel Panel(string title, Vector2 size, BulkheadPanelOptions? options = null)
	{
		Ensure.NotNull(title);
		return new BulkheadPanel(title, size, options ?? new BulkheadPanelOptions());
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

	internal static void DrawElbow(ImDrawListPtr drawList, Vector2 min, Vector2 size, BulkheadCorner corner, float columnWidth, float barThickness, Vector4 color, string? label)
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

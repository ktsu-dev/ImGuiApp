// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The ImGui side of a <see cref="TimelineView"/>: the wheel bindings and the scrollbar, shared by
	/// every widget that shows one so they all zoom and scroll the same way.
	/// </summary>
	internal static class TimelineViewChrome
	{
		/// <summary>How much one wheel notch zooms by.</summary>
		internal const float ZoomPerNotch = 1.25f;

		/// <summary>How much of the view one wheel notch scrolls by.</summary>
		internal const float ScrollPerNotch = 0.1f;

		/// <summary>
		/// Applies the wheel to <paramref name="view"/>. Call it while the widget's item is hovered,
		/// straight after submitting it.
		/// </summary>
		/// <param name="view">The view to zoom or scroll.</param>
		/// <param name="pointerPosition">The timeline position under the pointer, which a zoom keeps in place.</param>
		/// <returns><see langword="true"/> when the view changed.</returns>
		/// <remarks>
		/// Ctrl+wheel zooms about the pointer, Shift+wheel and the horizontal wheel scroll, and a plain
		/// wheel is left alone so it still scrolls the window. The wheel is claimed for the item only
		/// while a modifier says it is meant for the timeline, which is what stops a zoom also
		/// scrolling the window it happens in.
		/// </remarks>
		internal static bool HandleWheel(TimelineView view, float pointerPosition)
		{
			Ensure.NotNull(view);

			ImGuiIOPtr io = ImGui.GetIO();
			bool changed = false;

			if (io.KeyCtrl || io.KeyShift)
			{
				ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
			}

			ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelX);

			float wheel = io.MouseWheel;
			if (wheel != 0f)
			{
				if (io.KeyCtrl)
				{
					changed |= view.ZoomAt(MathF.Pow(ZoomPerNotch, wheel), pointerPosition);
				}
				else if (io.KeyShift)
				{
					changed |= view.ScrollBy(-wheel * ScrollPerNotch * view.ViewLength);
				}
			}

			float horizontal = io.MouseWheelH;
			if (horizontal != 0f)
			{
				changed |= view.ScrollBy(-horizontal * ScrollPerNotch * view.ViewLength);
			}

			return changed;
		}

		/// <summary>The narrowest the scrollbar thumb is drawn, as a fraction of a strip <paramref name="stripWidth"/> wide.</summary>
		internal static float MinThumbFraction(float stripWidth) =>
			stripWidth > 0f ? ImGui.GetStyle().GrabMinSize / stripWidth : 0f;

		/// <summary>Draws the scrollbar for <paramref name="view"/> into the strip <paramref name="min"/>..<paramref name="max"/>.</summary>
		/// <param name="drawList">The draw list to draw into.</param>
		/// <param name="min">The strip's top-left corner.</param>
		/// <param name="max">The strip's bottom-right corner.</param>
		/// <param name="view">The view the scrollbar shows.</param>
		/// <param name="hovered">Whether the pointer is over the strip.</param>
		/// <param name="active">Whether the thumb is being dragged.</param>
		/// <remarks>An empty timeline draws the track alone.</remarks>
		internal static void DrawScrollbar(ImDrawListPtr drawList, Vector2 min, Vector2 max, TimelineView view, bool hovered, bool active)
		{
			Ensure.NotNull(view);

			ImGuiStylePtr style = ImGui.GetStyle();
			Span<Vector4> colors = style.Colors;
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.ScrollbarBg]));

			float width = max.X - min.X;
			if (view.Duration <= 0f || width <= 0f)
			{
				return;
			}

			(float start, float length) = TimelineGestureState.ScrollThumb(view, MinThumbFraction(width));
			ImGuiCol thumb = active ? ImGuiCol.ScrollbarGrabActive : hovered ? ImGuiCol.ScrollbarGrabHovered : ImGuiCol.ScrollbarGrab;
			drawList.AddRectFilled(
				new Vector2(min.X + (start * width), min.Y),
				new Vector2(min.X + ((start + length) * width), max.Y),
				ImGui.GetColorU32(colors[(int)thumb]),
				style.ScrollbarRounding);
		}
	}
}

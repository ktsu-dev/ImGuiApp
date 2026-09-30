// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Draws a gradient bar with draggable colour stops and a colour editor for the selected stop.</summary>
	/// <param name="label">Unique label; pushed as an ID scope and used as the probe prefix.</param>
	/// <param name="stops">The stops, edited in place: moved, inserted, removed, recoloured. Kept sorted by position.</param>
	/// <param name="size">Bar size. Non-positive X uses CalcItemWidth(); non-positive Y uses GetFrameHeight().</param>
	/// <returns>True if <paramref name="stops"/> changed this frame (position, colour, insertion, removal, or sorting/seeding on entry).</returns>
	/// <remarks>
	/// Clicking empty space on the bar adds a stop coloured by the gradient at that point. Dragging a
	/// stop moves it without letting it cross a neighbour, and dragging it more than
	/// <see cref="GradientEditorState.RemoveDistance"/> pixels off the widget removes it on release.
	/// Delete removes the selected stop. No gesture removes a stop below
	/// <see cref="GradientEditorState.MinimumStops"/>. The bar is painted with
	/// <see cref="SampleGradient"/> one pixel column at a time, so what is drawn is exactly what a
	/// caller evaluating the same stops gets — vertex-colour interpolation would blend in sRGB and
	/// show a different gradient.
	/// </remarks>
	public static bool GradientEditor(string label, IList<GradientStop> stops, Vector2 size = default) =>
		GradientEditorImpl.Draw(label, stops, size);

	/// <summary>Evaluates a gradient at <paramref name="t"/>, interpolating in linear RGB (alpha interpolated linearly too). The editor draws with this function.</summary>
	/// <param name="stops">The stops. They need not be sorted.</param>
	/// <param name="t">Position along the gradient; clamped to 0..1, NaN treated as 0.</param>
	/// <returns>The colour at <paramref name="t"/>; fully transparent black for an empty list.</returns>
	public static Color SampleGradient(IReadOnlyList<GradientStop> stops, float t) =>
		GradientEditorState.Sample(stops, t);

	internal static class GradientEditorImpl
	{
		private const float MarkerRowHeight = 14f;
		private const float TriangleHeight = 4f;
		private const float SwatchSize = 10f;
		private const float GrabPixels = 6f;
		private const float PendingAlpha = 0.4f;

		private static readonly Dictionary<uint, GradientEditorState> States = [];

		public static bool Draw(string label, IList<GradientStop> stops, Vector2 size)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(stops);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out GradientEditorState? state))
			{
				state = new GradientEditorState();
				States[id] = state;
			}

			using ScopedId scope = new(label);

			bool changed = state.EnsureValid(stops);

			float width = MathF.Max(size.X > 0f ? size.X : ImGui.CalcItemWidth(), 1f);
			float barHeight = size.Y > 0f ? size.Y : ImGui.GetFrameHeight();

			ImGui.InvisibleButton("bar", new Vector2(width, barHeight + MarkerRowHeight));
			ImGuiProbes.MarkItem("bar");

			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			bool hovered = ImGui.IsItemHovered();
			bool focused = ImGui.IsItemFocused();
			bool active = ImGui.IsItemActive();

			Vector2 mouse = ImGui.GetIO().MousePos;
			float t = (mouse.X - min.X) / width;
			float grabRadius = GrabPixels / width;

			if (ImGui.IsItemActivated())
			{
				changed |= state.Press(stops, t, grabRadius);
			}

			if (active)
			{
				changed |= state.Drag(stops, t, DistanceOutside(mouse, min, max));
			}

			if (ImGui.IsItemDeactivated())
			{
				changed |= state.Release(stops);
			}

			if ((hovered || focused) && ImGui.IsKeyPressed(ImGuiKey.Delete, false))
			{
				changed |= state.DeleteSelected(stops);
			}

			int hoveredStop = hovered && !active ? GradientEditorState.FindStop(stops, t, grabRadius) : -1;
			UpdatePointer(state, stops, hoveredStop, active);

			Vector2 barMin = min;
			Vector2 barSize = new(width, barHeight);
			DrawBar(stops, barMin, barSize);
			DrawMarkers(stops, state, barMin, barSize, hoveredStop);

			changed |= DrawColorEditor(stops, state);
			return changed;
		}

		private static float DistanceOutside(Vector2 point, Vector2 min, Vector2 max)
		{
			float dx = MathF.Max(MathF.Max(min.X - point.X, point.X - max.X), 0f);
			float dy = MathF.Max(MathF.Max(min.Y - point.Y, point.Y - max.Y), 0f);
			return MathF.Sqrt((dx * dx) + (dy * dy));
		}

		private static void UpdatePointer(GradientEditorState state, IList<GradientStop> stops, int hoveredStop, bool active)
		{
			if (active && state.PendingRemoval)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
				return;
			}

			if (hoveredStop < 0)
			{
				return;
			}

			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			GradientStop stop = stops[hoveredStop];
			ImGui.BeginTooltip();
			ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"{stop.Position:0.###}  {stop.Color.ToHex()}"));
			ImGui.EndTooltip();
		}

		private static void DrawBar(IList<GradientStop> stops, Vector2 barMin, Vector2 barSize)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			DrawCheckerboard(drawList, barMin, barSize);

			IReadOnlyList<GradientStop> readOnly = stops as IReadOnlyList<GradientStop> ?? [.. stops];
			int columns = (int)MathF.Ceiling(barSize.X);
			for (int i = 0; i < columns; i++)
			{
				float left = barMin.X + i;
				float right = MathF.Min(left + 1f, barMin.X + barSize.X);
				uint color = SampleGradient(readOnly, (i + 0.5f) / barSize.X).ToImGuiU32();
				drawList.AddRectFilled(new Vector2(left, barMin.Y), new Vector2(right, barMin.Y + barSize.Y), color);
			}

			drawList.AddRect(barMin, barMin + barSize, ImGui.GetColorU32(ImGuiCol.Border), 0f, ImDrawFlags.None, 1f);
		}

		private static void DrawMarkers(IList<GradientStop> stops, GradientEditorState state, Vector2 barMin, Vector2 barSize, int hoveredStop)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint border = ImGui.GetColorU32(ImGuiCol.Border);
			uint frame = ImGui.GetColorU32(ImGuiCol.FrameBg);
			uint grab = ImGui.GetColorU32(ImGuiCol.SliderGrab);
			uint grabActive = ImGui.GetColorU32(ImGuiCol.SliderGrabActive);
			float bottom = barMin.Y + barSize.Y;
			float halfSwatch = SwatchSize * 0.5f;

			for (int i = 0; i < stops.Count; i++)
			{
				GradientStop stop = stops[i];
				float x = barMin.X + (stop.Position * barSize.X);

				drawList.AddTriangleFilled(
					new Vector2(x, bottom),
					new Vector2(x + halfSwatch, bottom + TriangleHeight),
					new Vector2(x - halfSwatch, bottom + TriangleHeight),
					border);

				Vector2 swatchMin = new(x - halfSwatch, bottom + TriangleHeight);
				Vector2 swatchMax = swatchMin + new Vector2(SwatchSize, SwatchSize);
				bool selected = i == state.SelectedIndex;
				Color fill = selected && state.PendingRemoval ? stop.Color.WithAlpha(stop.Color.A * PendingAlpha) : stop.Color;

				drawList.AddRectFilled(swatchMin, swatchMax, frame);
				drawList.AddRectFilled(swatchMin, swatchMax, fill.ToImGuiU32());

				if (selected)
				{
					drawList.AddRect(swatchMin, swatchMax, grabActive, 0f, ImDrawFlags.None, 2f);
				}
				else
				{
					drawList.AddRect(swatchMin, swatchMax, i == hoveredStop ? grab : border, 0f, ImDrawFlags.None, 1f);
				}

				ImGuiProbes.MarkRegion(string.Create(CultureInfo.InvariantCulture, $"stop{i}"), swatchMin, swatchMax);
			}
		}

		private static bool DrawColorEditor(IList<GradientStop> stops, GradientEditorState state)
		{
			int selected = state.SelectedIndex;
			if (selected < 0 || selected >= stops.Count)
			{
				ImGui.TextDisabled("Select a stop to edit its colour");
				return false;
			}

			Vector4 srgb = stops[selected].Color.ToSrgbVector4();
			bool edited = ImGui.ColorEdit4("colour", ref srgb, ImGuiColorEditFlags.AlphaBar);
			ImGuiProbes.MarkItem("colour");

			return edited && state.SetSelectedColor(stops, ColorImGuiExtensions.FromImGuiVector4(srgb));
		}
	}
}

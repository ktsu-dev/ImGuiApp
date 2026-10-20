// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

public static partial class ImGuiWidgets
{
	/// <summary>Number of segments the drawn curve is divided into across the editor's width.</summary>
	private const int CurveLineSegments = 128;

	/// <summary>Radius of a point marker in pixels at the reference interface scale.</summary>
	private const float CurvePointRadius = 4f;

	/// <summary>
	/// Draws an editable single curve.
	/// </summary>
	/// <param name="curve">The curve to edit, mutated in place.</param>
	/// <param name="size">Size of the editor in pixels. Neither dimension auto-sizes; a zero width
	/// collapses the editor to a sliver, so pass a real width.</param>
	/// <param name="rangeMin">Lower bound of the visible value range.</param>
	/// <param name="rangeMax">Upper bound of the visible value range.</param>
	/// <param name="selection">Index of the selected point, updated in place; -1 for none.</param>
	/// <param name="label">Label for display and identity.</param>
	/// <returns><see langword="true"/> if the curve changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="curve"/> or <paramref name="label"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// Press a point to select it and drag to move it; a point stays within the range and between its
	/// neighbours' X, so the curve's points stay in order. Double-click empty space to add a point
	/// there, which also works on an empty curve, and double-click a point to remove it.
	/// <para>
	/// The line is <see cref="CurveData.Sample"/> plotted in the same value range as the points, so a
	/// range wider than 0..1 shows the curve held flat beyond its ends rather than stretched across
	/// the frame.
	/// </para>
	/// </remarks>
	public static bool CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection, string label)
	{
		Ensure.NotNull(curve);
		Ensure.NotNull(label);

		selection = ClampSelection(selection, curve.PointCount);

		Vector2 frameSize = Vector2.Max(size, Vector2.One);
		Vector2 origin = ImGui.GetCursorScreenPos();
		CurveFieldGeometry geometry = new(origin, frameSize, rangeMin, rangeMax);
		float radius = InterfaceScale.Pixels(CurvePointRadius);

		ImGui.InvisibleButton(label, frameSize);
		ImGuiProbes.MarkItem(label);

		Vector2 mouse = ImGui.GetIO().MousePos;
		int hovered = ImGui.IsItemHovered() || ImGui.IsItemActive()
			? geometry.HitTest(curve, mouse, radius + 2f)
			: -1;
		if (hovered >= 0 || (ImGui.IsItemActive() && selection >= 0))
		{
			WidgetCursor.OnLastItem(ImGuiMouseCursor.ResizeAll);
		}

		bool changed = false;
		if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
		{
			if (hovered >= 0)
			{
				curve.RemovePoint(hovered);
				selection = -1;
			}
			else
			{
				Vector2 value = geometry.ToValue(mouse);
				int index = CurveFieldGeometry.InsertionIndex(curve, value.X);
				curve.InsertPoint(index, new CurveKnot(value, CurvePointKind.Smooth));
				selection = index;
			}

			changed = true;
		}
		else if (ImGui.IsItemActivated())
		{
			selection = hovered;
		}
		else if (ImGui.IsItemActive() && selection >= 0 && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 0f))
		{
			CurveKnot knot = curve.GetPoint(selection);
			Vector2 moved = geometry.ConstrainDrag(curve, selection, geometry.ToValue(mouse));
			if (moved != knot.Position)
			{
				curve.SetPoint(selection, knot with { Position = moved });
				changed = true;
			}
		}

		DrawCurveField(curve, geometry, selection, hovered, radius);
		return changed;
	}

	/// <summary>
	/// Constrains a point selection index to one a curve can actually address.
	/// </summary>
	/// <param name="selection">The selection index to constrain.</param>
	/// <param name="pointCount">How many points the curve holds.</param>
	/// <returns><paramref name="selection"/> when it addresses an existing point, otherwise -1.</returns>
	/// <remarks>
	/// The result always lies within <c>[-1, pointCount - 1]</c>. An index past the end resolves to
	/// -1 (no selection) rather than to the last point: a caller's curve can shrink between frames,
	/// and silently retargeting the drag at whichever point happens to be adjacent would move data
	/// the user never grabbed.
	/// </remarks>
	internal static int ClampSelection(int selection, int pointCount) =>
		selection >= 0 && selection < pointCount ? selection : -1;

	/// <summary>Draws the frame, grid, curve and point markers of a single-curve editor.</summary>
	/// <param name="curve">The curve being edited.</param>
	/// <param name="geometry">The mapping between the curve's values and the frame.</param>
	/// <param name="selection">The selected point, or -1.</param>
	/// <param name="hovered">The point under the pointer, or -1.</param>
	/// <param name="radius">Point marker radius in pixels.</param>
	private static void DrawCurveField(CurveData curve, CurveFieldGeometry geometry, int selection, int hovered, float radius)
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 min = geometry.Origin;
		Vector2 max = geometry.Origin + geometry.Size;
		ImGuiStylePtr style = ImGui.GetStyle();

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.FrameBg), style.FrameRounding);

		uint gridColor = ImGui.GetColorU32(ImGuiCol.Border);
		for (int i = 1; i < 4; i++)
		{
			float x = min.X + (geometry.Size.X * i / 4f);
			float y = min.Y + (geometry.Size.Y * i / 4f);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), gridColor);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), gridColor);
		}

		// Markers sit on the frame's edges when a point is at the end of the range, so the clip
		// leaves room for half of one outside it.
		Vector2 margin = new(radius + 2f);
		drawList.PushClipRect(min - margin, max + margin, true);

		if (curve.PointCount > 0)
		{
			uint lineColor = ImGui.GetColorU32(ImGuiCol.PlotLines);
			Vector2 previous = default;
			for (int i = 0; i <= CurveLineSegments; i++)
			{
				float x = geometry.RangeMin.X + ((geometry.RangeMax.X - geometry.RangeMin.X) * i / CurveLineSegments);
				Vector2 point = geometry.ToScreen(new Vector2(x, curve.Sample(x)));
				if (i > 0)
				{
					drawList.AddLine(previous, point, lineColor, 2f);
				}

				previous = point;
			}
		}

		for (int i = 0; i < curve.PointCount; i++)
		{
			Vector2 center = geometry.ToScreen(curve.GetPoint(i).Position);
			drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(CurvePointFill(i, selection, hovered)), 16);
			drawList.AddCircle(center, radius, ImGui.GetColorU32(ImGuiCol.Text), 16);
		}

		drawList.PopClipRect();
	}

	/// <summary>Picks a point's fill: the selected point stands out most, then the hovered one.</summary>
	/// <param name="index">The point being drawn.</param>
	/// <param name="selection">The selected point, or -1.</param>
	/// <param name="hovered">The point under the pointer, or -1.</param>
	/// <returns>The style colour to fill it with.</returns>
	private static ImGuiCol CurvePointFill(int index, int selection, int hovered)
	{
		if (index == selection)
		{
			return ImGuiCol.SliderGrabActive;
		}

		return index == hovered ? ImGuiCol.ButtonHovered : ImGuiCol.SliderGrab;
	}
}

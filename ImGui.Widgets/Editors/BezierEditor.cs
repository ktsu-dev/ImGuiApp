// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// The two control points of a cubic easing curve.
/// </summary>
/// <param name="First">The first control point.</param>
/// <param name="Second">The second control point.</param>
public readonly record struct BezierControlPoints(Vector2 First, Vector2 Second);

public static partial class ImGuiWidgets
{
	/// <summary>Radius of a control point handle in pixels at the reference interface scale.</summary>
	private const float BezierHandleRadius = 5f;

	/// <summary>
	/// Draws an editable bezier easing curve.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="points">The control points, updated in place when dragged.</param>
	/// <param name="size">Edge length of the square editor in pixels.</param>
	/// <returns><see langword="true"/> if a control point moved this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// The curve runs from (0, 0) at the bottom left to (1, 1) at the top right, as a CSS
	/// <c>cubic-bezier</c> does. Drag either handle to move its control point, or type the four
	/// numbers in the row above the plot. Every coordinate stays within 0..1: X because an easing
	/// curve has to move forward in time, and Y so a handle never leaves the plot.
	/// <para>
	/// The plot is probe-marked as <paramref name="label"/> and the row of numbers as
	/// <c>label/values</c>.
	/// </para>
	/// </remarks>
	public static bool BezierEditor(string label, ref BezierControlPoints points, float size = 128f)
	{
		Ensure.NotNull(label);

		Vector2 frameSize = new(MathF.Max(size, 1f));
		bool changed = false;

		// The four numbers, for exact entry, sit above the plot at its width with the label beside
		// them, as an ImGui field's label sits.
		Vector4 values = new(points.First.X, points.First.Y, points.Second.X, points.Second.Y);
		ImGui.SetNextItemWidth(frameSize.X);
		if (ImGui.SliderFloat4(label, ref values, 0f, 1f, "%.3f"))
		{
			points = new BezierControlPoints(new Vector2(values.X, values.Y), new Vector2(values.Z, values.W));
			changed = true;
		}

		ImGuiProbes.MarkItem(label, "values");

		Vector2 origin = ImGui.GetCursorScreenPos();
		CurveFieldGeometry geometry = new(origin, frameSize, Vector2.Zero, Vector2.One);
		float radius = InterfaceScale.Pixels(BezierHandleRadius);

		ImGui.PushID(label);
		ImGui.InvisibleButton("plot", frameSize);
		ImGui.PopID();
		ImGuiProbes.MarkItem(label);

		uint id = ImGui.GetItemID();
		ImGuiStoragePtr storage = ImGui.GetStateStorage();
		Vector2 mouse = ImGui.GetIO().MousePos;
		int hovered = ImGui.IsItemHovered() || ImGui.IsItemActive()
			? BezierHandleAt(geometry, points, mouse, radius + 2f)
			: -1;

		if (ImGui.IsItemActivated())
		{
			storage.SetInt(id, hovered);
		}

		int grabbed = ImGui.IsItemActive() ? storage.GetInt(id, -1) : -1;
		if (hovered >= 0 || grabbed >= 0)
		{
			WidgetCursor.OnLastItem(ImGuiMouseCursor.ResizeAll);
		}

		if (grabbed >= 0 && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 0f))
		{
			Vector2 value = geometry.ToValue(mouse);
			BezierControlPoints moved = grabbed == 0 ? points with { First = value } : points with { Second = value };
			if (moved != points)
			{
				points = moved;
				changed = true;
			}
		}

		DrawBezier(geometry, points, grabbed >= 0 ? grabbed : hovered, radius);
		return changed;
	}

	/// <summary>Finds the control point handle under a screen position.</summary>
	/// <param name="geometry">The mapping between the unit square and the frame.</param>
	/// <param name="points">The control points.</param>
	/// <param name="screen">The screen position, usually the pointer.</param>
	/// <param name="radius">How far from a handle's centre still counts as on it, in pixels.</param>
	/// <returns>0 for the first handle, 1 for the second, or -1 for neither. When the two overlap the
	/// nearer one wins.</returns>
	internal static int BezierHandleAt(CurveFieldGeometry geometry, BezierControlPoints points, Vector2 screen, float radius)
	{
		float first = Vector2.DistanceSquared(geometry.ToScreen(points.First), screen);
		float second = Vector2.DistanceSquared(geometry.ToScreen(points.Second), screen);
		float limit = radius * radius;
		if (first <= limit && first <= second)
		{
			return 0;
		}

		return second <= limit ? 1 : -1;
	}

	/// <summary>Draws the frame, curve, handle lines and handles of a bezier editor.</summary>
	/// <param name="geometry">The mapping between the unit square and the frame.</param>
	/// <param name="points">The control points.</param>
	/// <param name="highlighted">The handle under the pointer or being dragged, or -1.</param>
	/// <param name="radius">Handle radius in pixels.</param>
	private static void DrawBezier(CurveFieldGeometry geometry, BezierControlPoints points, int highlighted, float radius)
	{
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 min = geometry.Origin;
		Vector2 max = geometry.Origin + geometry.Size;

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.FrameBg), ImGui.GetStyle().FrameRounding);

		uint gridColor = ImGui.GetColorU32(ImGuiCol.Border);
		for (int i = 1; i < 4; i++)
		{
			float x = min.X + (geometry.Size.X * i / 4f);
			float y = min.Y + (geometry.Size.Y * i / 4f);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), gridColor);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), gridColor);
		}

		Vector2 start = geometry.ToScreen(Vector2.Zero);
		Vector2 end = geometry.ToScreen(Vector2.One);
		Vector2 first = geometry.ToScreen(points.First);
		Vector2 second = geometry.ToScreen(points.Second);

		uint handleLine = ImGui.GetColorU32(ImGuiCol.TextDisabled);
		drawList.AddLine(start, first, handleLine);
		drawList.AddLine(end, second, handleLine);
		drawList.AddBezierCubic(start, first, second, end, ImGui.GetColorU32(ImGuiCol.PlotLines), 2f);

		uint outline = ImGui.GetColorU32(ImGuiCol.Text);
		drawList.AddCircleFilled(first, radius, ImGui.GetColorU32(highlighted == 0 ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab), 16);
		drawList.AddCircle(first, radius, outline, 16);
		drawList.AddCircleFilled(second, radius, ImGui.GetColorU32(highlighted == 1 ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab), 16);
		drawList.AddCircle(second, radius, outline, 16);
	}
}

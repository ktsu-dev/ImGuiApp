// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using HexaBezierCurve = Hexa.NET.Mathematics.BezierCurve;
using HexaBezierWidget = Hexa.NET.ImGui.Widgets.Extras.ImGuiBezierWidget;

/// <summary>
/// The two control points of a cubic easing curve.
/// </summary>
/// <param name="First">The first control point.</param>
/// <param name="Second">The second control point.</param>
public readonly record struct BezierControlPoints(Vector2 First, Vector2 Second);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable bezier easing curve.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="points">The control points, updated in place when dragged.</param>
	/// <param name="size">Edge length of the square editor in pixels.</param>
	/// <returns><see langword="true"/> if a control point moved this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool BezierEditor(string label, ref BezierControlPoints points, float size = 128f)
	{
		Ensure.NotNull(label);

		HexaBezierCurve curve = ToVendorBezier(points);
		bool changed = HexaBezierWidget.Bezier(label, ref curve, size);
		if (changed)
		{
			points = FromVendorBezier(curve);
		}

		return changed;
	}

	/// <summary>
	/// Converts managed control points to the vendor curve.
	/// </summary>
	/// <param name="points">The managed control points.</param>
	/// <returns>The vendor curve.</returns>
	internal static HexaBezierCurve ToVendorBezier(BezierControlPoints points) =>
		new(points.First, points.Second);

	/// <summary>
	/// Converts a vendor curve to managed control points.
	/// </summary>
	/// <param name="curve">The vendor curve.</param>
	/// <returns>The managed control points.</returns>
	/// <remarks>
	/// The vendor type is an inline array of two vectors, so its control points are reached by
	/// index rather than by name.
	/// </remarks>
	internal static BezierControlPoints FromVendorBezier(HexaBezierCurve curve) =>
		new(curve[0], curve[1]);
}

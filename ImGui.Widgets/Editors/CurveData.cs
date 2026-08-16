// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Numerics;

using static ImGuiWidgets;

using HexaCurve = Hexa.NET.Mathematics.Curve;
using HexaCurvePoint = Hexa.NET.Mathematics.CurvePoint;

/// <summary>
/// A single authored curve point.
/// </summary>
/// <param name="Position">Where the point sits.</param>
/// <param name="Kind">Whether the curve smooths through the point or forms a corner at it.</param>
public readonly record struct CurveKnot(Vector2 Position, CurvePointKind Kind);

/// <summary>
/// An editable curve. Wraps the curve representation the underlying widget expects, so no vendor
/// type appears in this library's public surface.
/// </summary>
/// <remarks>
/// <see cref="Sample"/> recomputes the underlying sample cache when points have changed, so
/// callers never have to remember to do it themselves.
/// </remarks>
public sealed class CurveData
{
	private HexaCurve curve = new();
	private bool needsCompute = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="CurveData"/> class with no points.
	/// </summary>
	public CurveData()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CurveData"/> class from existing points.
	/// </summary>
	/// <param name="points">The points, in order.</param>
	/// <param name="shape">How the curve is shaped between points.</param>
	/// <exception cref="ArgumentNullException"><paramref name="points"/> is <see langword="null"/>.</exception>
	public CurveData(IEnumerable<CurveKnot> points, CurveShape shape = CurveShape.Smooth)
	{
		Ensure.NotNull(points);

		foreach (CurveKnot point in points)
		{
			curve.Points.Add(ToVendor(point));
		}

		curve.Type = MapShape(shape);
	}

	/// <summary>
	/// Gets the number of points on the curve.
	/// </summary>
	public int PointCount => curve.Points.Count;

	/// <summary>
	/// Gets or sets how the curve is shaped between its points.
	/// </summary>
	public CurveShape Shape
	{
		get => MapShapeBack(curve.Type);
		set
		{
			curve.Type = MapShape(value);
			needsCompute = true;
		}
	}

	/// <summary>
	/// Gets a point.
	/// </summary>
	/// <param name="index">Index of the point.</param>
	/// <returns>The point.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public CurveKnot GetPoint(int index)
	{
		ThrowIfOutOfRange(index);
		return FromVendor(curve.Points[index]);
	}

	/// <summary>
	/// Appends a point.
	/// </summary>
	/// <param name="point">The point to add.</param>
	public void AddPoint(CurveKnot point)
	{
		curve.Points.Add(ToVendor(point));
		needsCompute = true;
	}

	/// <summary>
	/// Replaces a point.
	/// </summary>
	/// <param name="index">Index of the point to replace.</param>
	/// <param name="point">The replacement.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public void SetPoint(int index, CurveKnot point)
	{
		ThrowIfOutOfRange(index);
		curve.Points[index] = ToVendor(point);
		needsCompute = true;
	}

	/// <summary>
	/// Removes a point.
	/// </summary>
	/// <param name="index">Index of the point to remove.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	public void RemovePoint(int index)
	{
		ThrowIfOutOfRange(index);
		curve.Points.RemoveAt(index);
		needsCompute = true;
	}

	/// <summary>
	/// Removes every point.
	/// </summary>
	public void Clear()
	{
		curve.Points.Clear();
		needsCompute = true;
	}

	/// <summary>
	/// Samples the computed curve.
	/// </summary>
	/// <param name="t">Position along the curve, from 0 to 1.</param>
	/// <returns>The sampled value, or zero if the curve has no points.</returns>
	public float Sample(float t)
	{
		EnsureComputed();

		if (curve.Samples is null || curve.Samples.Length == 0)
		{
			return 0f;
		}

		int index = (int)(Math.Clamp(t, 0f, 1f) * (curve.Samples.Length - 1));
		return curve.Samples[index];
	}

	/// <summary>
	/// Gets a reference to the underlying curve for the editor to mutate in place.
	/// </summary>
	/// <returns>A reference to the wrapped curve.</returns>
	/// <remarks>
	/// Only for the editor entry point. Callers must invoke <see cref="MarkDirty"/> afterwards,
	/// because the editor edits points directly and the sample cache goes stale.
	/// </remarks>
	internal ref HexaCurve AsVendorCurve() => ref curve;

	/// <summary>
	/// Marks the sample cache stale after an external edit.
	/// </summary>
	internal void MarkDirty() => needsCompute = true;

	/// <summary>
	/// Converts a managed point to the vendor representation.
	/// </summary>
	/// <param name="point">The managed point.</param>
	/// <returns>The vendor point.</returns>
	private static HexaCurvePoint ToVendor(CurveKnot point) =>
		new(point.Position, MapPointKind(point.Kind));

	/// <summary>
	/// Converts a vendor point to the managed representation.
	/// </summary>
	/// <param name="point">The vendor point.</param>
	/// <returns>The managed point.</returns>
	private static CurveKnot FromVendor(HexaCurvePoint point) =>
		new(point.Pos, MapPointKindBack(point.Type));

	/// <summary>
	/// Recomputes the sample cache if anything changed since the last computation.
	/// </summary>
	private void EnsureComputed()
	{
		if (!needsCompute)
		{
			return;
		}

		curve.Compute();
		needsCompute = false;
	}

	/// <summary>
	/// Throws when an index falls outside the curve.
	/// </summary>
	/// <param name="index">The index to check.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the curve.</exception>
	private void ThrowIfOutOfRange(int index)
	{
		if (index < 0 || index >= curve.Points.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, $"The curve has {curve.Points.Count} points.");
		}
	}
}

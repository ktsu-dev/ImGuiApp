// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

/// <summary>
/// The mapping between a single-curve editor's value range and its frame on screen, and the rules
/// for picking, inserting and dragging points. Makes no ImGui calls, so it is tested without a context.
/// </summary>
/// <param name="Origin">Top-left corner of the frame in screen pixels.</param>
/// <param name="Size">Size of the frame in pixels.</param>
/// <param name="RangeMin">The value drawn at the frame's left and bottom edges.</param>
/// <param name="RangeMax">The value drawn at the frame's right and top edges.</param>
internal readonly record struct CurveFieldGeometry(Vector2 Origin, Vector2 Size, Vector2 RangeMin, Vector2 RangeMax)
{
	/// <summary>Converts a value to a screen position. Larger Y values draw higher.</summary>
	/// <param name="value">The value to place.</param>
	/// <returns>The screen position.</returns>
	public Vector2 ToScreen(Vector2 value)
	{
		Vector2 fraction = Fraction(value);
		return new Vector2(Origin.X + (fraction.X * Size.X), Origin.Y + ((1f - fraction.Y) * Size.Y));
	}

	/// <summary>Converts a screen position to a value, clamped to the range.</summary>
	/// <param name="screen">The screen position.</param>
	/// <returns>The value under it.</returns>
	public Vector2 ToValue(Vector2 screen)
	{
		float fx = Size.X > 0f ? Math.Clamp((screen.X - Origin.X) / Size.X, 0f, 1f) : 0f;
		float fy = Size.Y > 0f ? Math.Clamp(1f - ((screen.Y - Origin.Y) / Size.Y), 0f, 1f) : 0f;
		return new Vector2(
			RangeMin.X + (fx * (RangeMax.X - RangeMin.X)),
			RangeMin.Y + (fy * (RangeMax.Y - RangeMin.Y)));
	}

	/// <summary>Finds the point nearest a screen position, within a radius.</summary>
	/// <param name="curve">The curve whose points are tested.</param>
	/// <param name="screen">The screen position, usually the pointer.</param>
	/// <param name="radius">How far from a point's centre still counts as on it, in pixels.</param>
	/// <returns>The index of the nearest point within <paramref name="radius"/>, or -1.</returns>
	public int HitTest(CurveData curve, Vector2 screen, float radius)
	{
		int best = -1;
		float bestDistance = radius * radius;
		for (int i = 0; i < curve.PointCount; i++)
		{
			float distance = Vector2.DistanceSquared(ToScreen(curve.GetPoint(i).Position), screen);
			if (distance <= bestDistance)
			{
				best = i;
				bestDistance = distance;
			}
		}

		return best;
	}

	/// <summary>
	/// Constrains where a dragged point may go: inside the range, and no further left than the point
	/// before it nor further right than the point after it, so the points stay in order.
	/// </summary>
	/// <param name="curve">The curve being edited.</param>
	/// <param name="index">The point being dragged.</param>
	/// <param name="value">Where the pointer would put it.</param>
	/// <returns>Where it goes.</returns>
	public Vector2 ConstrainDrag(CurveData curve, int index, Vector2 value)
	{
		float minX = Math.Min(RangeMin.X, RangeMax.X);
		float maxX = Math.Max(RangeMin.X, RangeMax.X);
		if (index > 0)
		{
			minX = Math.Max(minX, curve.GetPoint(index - 1).Position.X);
		}

		if (index < curve.PointCount - 1)
		{
			maxX = Math.Min(maxX, curve.GetPoint(index + 1).Position.X);
		}

		float minY = Math.Min(RangeMin.Y, RangeMax.Y);
		float maxY = Math.Max(RangeMin.Y, RangeMax.Y);
		return new Vector2(Math.Clamp(value.X, minX, Math.Max(minX, maxX)), Math.Clamp(value.Y, minY, maxY));
	}

	/// <summary>Finds where a new point at <paramref name="x"/> belongs so the points stay in order.</summary>
	/// <param name="curve">The curve receiving the point.</param>
	/// <param name="x">The new point's X.</param>
	/// <returns>The index to insert at: after every point at or left of <paramref name="x"/>.</returns>
	public static int InsertionIndex(CurveData curve, float x)
	{
		int index = 0;
		while (index < curve.PointCount && curve.GetPoint(index).Position.X <= x)
		{
			index++;
		}

		return index;
	}

	private Vector2 Fraction(Vector2 value)
	{
		Vector2 span = RangeMax - RangeMin;
		return new Vector2(
			span.X != 0f ? (value.X - RangeMin.X) / span.X : 0f,
			span.Y != 0f ? (value.Y - RangeMin.Y) / span.Y : 0f);
	}
}

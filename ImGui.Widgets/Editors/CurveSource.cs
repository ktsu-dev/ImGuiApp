// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.Semantics.Color;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Supplies curves to <see cref="CurveEditor(CurveSource, Vector2, string)"/>. Subclass it and
	/// override the members describing your curves.
	/// </summary>
	/// <remarks>
	/// The editor interrogates this object while drawing, so every member must be cheap and must
	/// not mutate anything other than in response to <see cref="EditPoint"/> or
	/// <see cref="AddPoint"/>.
	/// </remarks>
	public abstract class CurveSource
	{
		/// <summary>
		/// Gets how many curves to draw.
		/// </summary>
		public abstract int CurveCount { get; }

		/// <summary>
		/// Gets the lower corner of the visible value range.
		/// </summary>
		public abstract Vector2 ViewMin { get; }

		/// <summary>
		/// Gets the upper corner of the visible value range.
		/// </summary>
		public abstract Vector2 ViewMax { get; }

		/// <summary>
		/// Gets how many points a curve has.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The point count.</returns>
		public abstract int GetPointCount(int curveIndex);

		/// <summary>
		/// Gets a curve's points.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The points, in order.</returns>
		public abstract Span<Vector2> GetPoints(int curveIndex);

		/// <summary>
		/// Gets the color a curve is drawn in.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The curve color.</returns>
		public abstract Srgb GetCurveColor(int curveIndex);

		/// <summary>
		/// Moves a point.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <param name="pointIndex">Index of the point being moved.</param>
		/// <param name="value">The point's new position.</param>
		/// <returns>The point's index after any re-sort the source performs.</returns>
		/// <remarks>
		/// Returning a different index is how a source reports that moving a point past its
		/// neighbour reordered the curve. The editor tracks the point by the returned index.
		/// </remarks>
		public abstract int EditPoint(int curveIndex, int pointIndex, Vector2 value);

		/// <summary>
		/// Adds a point to a curve.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <param name="value">Where to add it.</param>
		public abstract void AddPoint(int curveIndex, Vector2 value);

		/// <summary>
		/// Gets whether a curve is drawn.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns><see langword="true"/> to draw it.</returns>
		public virtual bool IsVisible(int curveIndex) => true;

		/// <summary>
		/// Gets how a curve interpolates between its points.
		/// </summary>
		/// <param name="curveIndex">Index of the curve.</param>
		/// <returns>The interpolation mode.</returns>
		public virtual CurveInterpolation GetInterpolation(int curveIndex) => CurveInterpolation.Linear;

		/// <summary>
		/// Gets the editor's background color.
		/// </summary>
		public virtual Srgb BackgroundColor => new(0.125f, 0.125f, 0.125f);

		/// <summary>
		/// Called before a drag begins, so the source can open an undo scope.
		/// </summary>
		/// <param name="curveIndex">Index of the curve being edited.</param>
		public virtual void BeginEdit(int curveIndex)
		{
		}

		/// <summary>
		/// Called after a drag ends, so the source can close its undo scope.
		/// </summary>
		public virtual void EndEdit()
		{
		}
	}
}

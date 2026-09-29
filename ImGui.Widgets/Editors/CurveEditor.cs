// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

using HexaCurveContext = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveContext;
using HexaCurveEdit = Hexa.NET.ImGui.Widgets.ImCurveEdit.ImCurveEdit;
using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable multi-curve graph.
	/// </summary>
	/// <param name="source">Supplies the curves and receives edits.</param>
	/// <param name="size">Size of the editor in pixels.</param>
	/// <param name="id">Unique identifier for the editor.</param>
	/// <returns><see langword="true"/> if a curve changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="id"/> is <see langword="null"/>.</exception>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Hexa's Edit takes an optional selected-points pointer; we pass null and no pointer is created, retained or dereferenced here.")]
	public static unsafe bool CurveEditor(CurveSource source, Vector2 size, string id)
	{
		Ensure.NotNull(source);
		Ensure.NotNull(id);

		CurveAdapter adapter = new(source);

		// Upstream pushes no clip rect, so a point dragged past the view would be drawn over
		// whatever sits beside the editor. Keep room for a marker sitting on an edge.
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGui.PushClipRect(origin - new Vector2(PointMarkerMargin), origin + size + new Vector2(PointMarkerMargin), true);
		try
		{
			return HexaCurveEdit.Edit(adapter, size, ImGui.GetID(id), null) != 0;
		}
		finally
		{
			ImGui.PopClipRect();
		}
	}

	/// <summary>
	/// How far outside its frame a curve editor may draw: the half-width of the largest point
	/// marker either vendor editor draws, plus its outline.
	/// </summary>
	private const float PointMarkerMargin = 6f;

	/// <summary>
	/// Presents a <see cref="CurveSource"/> to Hexa's curve editor. Kept private so the vendor base
	/// type never reaches this library's public surface.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hexa does not read points in the source's value space. It draws a point at
	/// <c>(p + 0.5) * Range + Min</c>, then treats that as a fraction of the frame, so the only
	/// consistent reading is a canvas centred on zero, running -0.5 to 0.5 across the frame, with
	/// <c>Min</c> at zero and <c>Range</c> at one; its mouse deltas are measured in that canvas too.
	/// Handing it value-space points put a curve's origin at the centre of the frame and ran the
	/// rest off its right and bottom edges, upside down. So <c>Min</c> and <c>Max</c> stay at zero
	/// and one, and every point crosses the boundary through <see cref="ToCanvas"/> and
	/// <see cref="FromCanvas"/>, which also turn the y axis so larger values draw higher.
	/// </para>
	/// <para>
	/// Hexa reads the span it is given while it draws and after an edit, so the converted points
	/// live in a buffer per curve for the adapter's lifetime, which is one call to the editor.
	/// </para>
	/// </remarks>
	private sealed class CurveAdapter : HexaCurveContext
	{
		private const float CollapsedRange = 1e-6f;

		private readonly CurveSource source;
		private readonly Vector2 viewMin;
		private readonly Vector2 viewRange;
		private readonly Dictionary<int, Vector2[]> canvasPoints = [];

		/// <summary>Initializes a new instance of the <see cref="CurveAdapter"/> class.</summary>
		/// <param name="source">The source to forward to.</param>
		public CurveAdapter(CurveSource source)
		{
			this.source = source;
			viewMin = source.ViewMin;
			Vector2 range = source.ViewMax - viewMin;

			// A collapsed axis has no fraction to map to; show it as a unit range rather than
			// dividing by zero and drawing nothing at all.
			viewRange = new Vector2(
				MathF.Abs(range.X) < CollapsedRange ? 1f : range.X,
				MathF.Abs(range.Y) < CollapsedRange ? 1f : range.Y);
			Min = Vector2.Zero;
			Max = Vector2.One;
		}

		/// <summary>Converts a point from the source's value space to Hexa's canvas.</summary>
		/// <param name="point">A point in value space.</param>
		/// <returns>The same point in canvas space.</returns>
		public Vector2 ToCanvas(Vector2 point)
		{
			Vector2 fraction = (point - viewMin) / viewRange;
			return new Vector2(fraction.X - 0.5f, 0.5f - fraction.Y);
		}

		/// <summary>Converts a point from Hexa's canvas to the source's value space.</summary>
		/// <param name="canvas">A point in canvas space.</param>
		/// <returns>The same point in value space.</returns>
		public Vector2 FromCanvas(Vector2 canvas)
		{
			Vector2 fraction = new(canvas.X + 0.5f, 0.5f - canvas.Y);
			return viewMin + (fraction * viewRange);
		}

		/// <inheritdoc/>
		public override int GetCurveCount() => source.CurveCount;

		/// <inheritdoc/>
		public override int GetPointCount(int curveIndex) => source.GetPointCount(curveIndex);

		/// <inheritdoc/>
		public override Span<Vector2> GetPoints(int curveIndex)
		{
			Span<Vector2> points = source.GetPoints(curveIndex);
			if (!canvasPoints.TryGetValue(curveIndex, out Vector2[]? buffer) || buffer.Length < points.Length)
			{
				buffer = new Vector2[points.Length];
				canvasPoints[curveIndex] = buffer;
			}

			for (int i = 0; i < points.Length; i++)
			{
				buffer[i] = ToCanvas(points[i]);
			}

			return buffer.AsSpan(0, points.Length);
		}

		/// <inheritdoc/>
		public override uint GetCurveColor(int curveIndex) => source.GetCurveColor(curveIndex).ToImGuiU32();

		/// <inheritdoc/>
		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value) =>
			source.EditPoint(curveIndex, pointIndex, FromCanvas(value));

		/// <inheritdoc/>
		public override void AddPoint(int curveIndex, Vector2 value) => source.AddPoint(curveIndex, FromCanvas(value));

		/// <inheritdoc/>
		public override bool IsVisible(int curveIndex) => source.IsVisible(curveIndex);

		/// <inheritdoc/>
		public override HexaCurveEditType GetCurveType(int curveIndex) =>
			MapInterpolation(source.GetInterpolation(curveIndex));

		/// <inheritdoc/>
		public override uint GetBackgroundColor() => source.BackgroundColor.ToImGuiU32();

		/// <inheritdoc/>
		public override void BeginEdit(int index) => source.BeginEdit(index);

		/// <inheritdoc/>
		public override void EndEdit() => source.EndEdit();
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

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

		CurveAdapter adapter = new(source)
		{
			// Min and Max are inputs to Edit, which computes Range = Max - Min before calling
			// GetCurveCount, so they must be refreshed here rather than inside GetCurveCount.
			Min = source.ViewMin,
			Max = source.ViewMax,
		};
		return HexaCurveEdit.Edit(adapter, size, ImGui.GetID(id), null) != 0;
	}

	/// <summary>
	/// Presents a <see cref="CurveSource"/> to Hexa's curve editor. Kept private so the vendor base
	/// type never reaches this library's public surface.
	/// </summary>
	/// <param name="source">The source to forward to.</param>
	private sealed class CurveAdapter(CurveSource source) : HexaCurveContext
	{
		/// <inheritdoc/>
		public override int GetCurveCount() => source.CurveCount;

		/// <inheritdoc/>
		public override int GetPointCount(int curveIndex) => source.GetPointCount(curveIndex);

		/// <inheritdoc/>
		public override Span<Vector2> GetPoints(int curveIndex) => source.GetPoints(curveIndex);

		/// <inheritdoc/>
		public override uint GetCurveColor(int curveIndex) => source.GetCurveColor(curveIndex).ToImGuiU32();

		/// <inheritdoc/>
		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value) =>
			source.EditPoint(curveIndex, pointIndex, value);

		/// <inheritdoc/>
		public override void AddPoint(int curveIndex, Vector2 value) => source.AddPoint(curveIndex, value);

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

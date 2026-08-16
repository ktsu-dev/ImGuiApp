// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using HexaCurve = Hexa.NET.Mathematics.Curve;
using HexaCurveEditor = Hexa.NET.ImGui.Widgets.Extras.ImGuiCurveEditor;

public static partial class ImGuiWidgets
{
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
	/// Two upstream defects are absorbed here rather than passed through to callers.
	/// <para>
	/// An empty curve is never handed to the vendor: the call is skipped, a layout slot of
	/// <paramref name="size"/> is reserved so nothing below shifts, and <see langword="false"/> is
	/// returned. Upstream's add-a-point path (<c>ImGuiCurveEditor.cs:192</c>) evaluates
	/// <c>Points[key]</c> before testing <c>key != Points.Count</c>, so a double-click — the
	/// documented gesture for adding a point — throws immediately on a curve with no points, which
	/// is exactly the state the parameterless <see cref="CurveData"/> constructor produces. Because
	/// that gesture is also the only way to create a point, an empty curve cannot be populated
	/// through this editor: seed at least one point with <see cref="CurveData.AddPoint"/> first.
	/// </para>
	/// <para>
	/// <paramref name="selection"/> is normalised before and after the vendor call, so it never
	/// addresses a point that is not there. Upstream removes <c>Points[currentSelection]</c> on a
	/// double-click (<c>ImGuiCurveEditor.cs:208-212</c>) but writes the now-stale index straight back
	/// out (<c>:233</c>). Clamping alone would only cover deletion of the <em>last</em> point, since
	/// deleting any earlier one leaves the index addressing whichever point shifted down into that
	/// slot — so a shrink in point count clears the selection outright.
	/// </para>
	/// <para>
	/// One upstream defect is deliberately <em>not</em> absorbed: double-clicking to the right of the
	/// last point, when that point sits left of <paramref name="rangeMax"/>'s X, still walks
	/// <c>key</c> up to <c>Points.Count</c> and throws at <c>ImGuiCurveEditor.cs:192</c>. It could be
	/// prevented from out here, but only by rescaling the horizontal axis away from the range the
	/// caller asked for, or by duplicating the vendor's hit-test maths and suppressing its gesture
	/// through global IO state — both worse than the defect. Keep the last point at or beyond
	/// <paramref name="rangeMax"/>'s X to stay clear of it. Note the throw escapes between the
	/// vendor's <c>PushID</c> and <c>PopID</c>, so catching it and continuing to render leaves the
	/// ImGui ID stack unbalanced; it is not a recoverable exception.
	/// </para>
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The only pointer is taken by fixed over the caller's selection, is pinned for exactly the duration of the vendor call, and is neither retained nor published beyond it.")]
	public static unsafe bool CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection, string label)
	{
		Ensure.NotNull(curve);
		Ensure.NotNull(label);

		selection = ClampSelection(selection, curve.PointCount);

		if (curve.PointCount == 0)
		{
			// See the remarks: upstream's add-a-point path indexes Points[0] on an empty list.
			// Reserve the layout slot anyway, so an empty curve leaves a gap the caller sized rather
			// than silently collapsing to nothing and shifting everything below it.
			ImGui.Dummy(size);
			return false;
		}

		int pointCountBefore = curve.PointCount;
		ref HexaCurve vendorCurve = ref curve.AsVendorCurve();
		bool changed;

		// Hexa's ref-parameter overload (ImGuiCurveEditor's `ref int selection` convenience wrapper)
		// forwards Unsafe.AsPointer(ref selection) to the pointer overload with no pinned local at
		// all, and AsPointer does not pin. The resulting int* is then held across the entire draw
		// body, which reads it (ImGuiCurveEditor.cs:59), allocates along the way (ImGui.PushID,
		// Points.Add/Insert, an interpolated string) and finally writes through it
		// (ImGuiCurveEditor.cs:233). A caller holding selection as a field on a class — the natural
		// place for it — can have that object relocated by a gen0 compaction mid-call, and the write
		// then lands in memory belonging to something else. Call the pointer overload directly with
		// a real fixed so nobody "simplifies" this back to the broken convenience overload. Same
		// deliberate refusal of a vendor ref overload, for the same defect class, as Sequencer.cs:64-72.
		fixed (int* pSelection = &selection)
		{
			changed = HexaCurveEditor.Curve(label, size, ref vendorCurve, rangeMin, rangeMax, pSelection);
		}

		selection = ResolveSelectionAfterEdit(selection, pointCountBefore, curve.PointCount);

		if (changed)
		{
			// The editor moved points directly in the wrapped curve, so the sample cache is stale.
			curve.MarkDirty();
		}

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
	/// -1 (no selection) rather than to the last point: it is reached by upstream deleting the point
	/// that was selected, and silently retargeting the drag at whichever point happens to be adjacent
	/// would move data the user never grabbed.
	/// <para>
	/// This handles an index that is out of range. It cannot detect a stale index that still happens
	/// to be in range — the case where an earlier point was deleted and a later one shifted down into
	/// its slot. Callers relying on that must compare the point count across the call, as
	/// <see cref="CurveEditor(CurveData, Vector2, Vector2, Vector2, ref int, string)"/> does.
	/// </para>
	/// </remarks>
	internal static int ClampSelection(int selection, int pointCount) =>
		selection >= 0 && selection < pointCount ? selection : -1;

	/// <summary>
	/// Resolves a point selection after the editor has run, clearing it when the editor deleted a point.
	/// </summary>
	/// <param name="selection">The selection index the editor wrote back.</param>
	/// <param name="pointCountBefore">How many points the curve held before the call.</param>
	/// <param name="pointCountAfter">How many it holds now.</param>
	/// <returns>The selection to hand back to the caller.</returns>
	/// <remarks>
	/// A drop in point count means the editor deleted the selected point — <c>RemoveAt</c> at
	/// <c>ImGuiCurveEditor.cs:210</c> is its only removal path, and it removes the selected index.
	/// The editor then writes that index straight back out, so clamping alone would only catch the
	/// deletion of the <em>last</em> point: deleting any earlier one leaves the index addressing a
	/// real slot, the point that shifted down into it, and the next drag would move a point the user
	/// never grabbed. Any shrink therefore clears the selection outright.
	/// </remarks>
	internal static int ResolveSelectionAfterEdit(int selection, int pointCountBefore, int pointCountAfter) =>
		pointCountAfter < pointCountBefore ? -1 : ClampSelection(selection, pointCountAfter);
}

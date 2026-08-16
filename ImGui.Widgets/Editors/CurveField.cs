// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

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
	/// An empty curve is never handed to the vendor: the call is skipped and <see langword="false"/>
	/// returned. Upstream's add-a-point path (<c>ImGuiCurveEditor.cs:192</c>) evaluates
	/// <c>Points[key]</c> before testing <c>key != Points.Count</c>, so a double-click — the
	/// documented gesture for adding a point — throws immediately on a curve with no points, which
	/// is exactly the state the parameterless <see cref="CurveData"/> constructor produces.
	/// </para>
	/// <para>
	/// <paramref name="selection"/> is passed through <see cref="ClampSelection"/> both before and
	/// after the vendor call, so an index that does not address a point becomes -1. Upstream removes
	/// <c>Points[currentSelection]</c> on a double-click (<c>ImGuiCurveEditor.cs:208-212</c>) but
	/// writes the now-stale index straight back out (<c>:233</c>), so without this the next frame's
	/// drag would index a slot that no longer exists.
	/// </para>
	/// <para>
	/// One upstream defect is deliberately <em>not</em> absorbed: double-clicking to the right of the
	/// last point, when that point sits left of <paramref name="rangeMax"/>'s X, still walks
	/// <c>key</c> up to <c>Points.Count</c> and throws at <c>ImGuiCurveEditor.cs:192</c>. Preventing
	/// it from out here would mean mutating the caller's curve, so it is left upstream. Keep the last
	/// point at or beyond <paramref name="rangeMax"/>'s X to stay clear of it.
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
			return false;
		}

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

		selection = ClampSelection(selection, curve.PointCount);

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
	/// </remarks>
	internal static int ClampSelection(int selection, int pointCount) =>
		selection >= 0 && selection < pointCount ? selection : -1;
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using HexaCurve = Hexa.NET.Mathematics.Curve;
using HexaCurveEditor = Hexa.NET.ImGui.Widgets.Extras.ImGuiCurveEditor;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an editable single curve.
	/// </summary>
	/// <param name="curve">The curve to edit, mutated in place.</param>
	/// <param name="size">Size of the editor in pixels.</param>
	/// <param name="rangeMin">Lower bound of the visible value range.</param>
	/// <param name="rangeMax">Upper bound of the visible value range.</param>
	/// <param name="selection">Index of the selected point, updated in place; -1 for none.</param>
	/// <param name="label">Label for display and identity.</param>
	/// <returns><see langword="true"/> if the curve changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="curve"/> or <paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection, string label)
	{
		Ensure.NotNull(curve);
		Ensure.NotNull(label);

		ref HexaCurve vendorCurve = ref curve.AsVendorCurve();
		bool changed = HexaCurveEditor.Curve(label, size, ref vendorCurve, rangeMin, rangeMax, ref selection);

		if (changed)
		{
			// The editor moved points directly in the wrapped curve, so the sample cache is stale.
			curve.MarkDirty();
		}

		return changed;
	}
}

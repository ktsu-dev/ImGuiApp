// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaCurveEditType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;
using HexaCurvePointType = Hexa.NET.Mathematics.CurvePointType;
using HexaMathCurveType = Hexa.NET.Mathematics.CurveType;
using HexaSequencerOptions = Hexa.NET.ImGui.Widgets.ImSequencer.SequencerOptions;

/// <summary>
/// How a curve is interpolated between its points.
/// </summary>
public enum CurveInterpolation
{
	/// <summary>No interpolation.</summary>
	None,

	/// <summary>Steps between points without interpolating.</summary>
	Discrete,

	/// <summary>Straight lines between points.</summary>
	Linear,

	/// <summary>Smoothed through the points.</summary>
	Smooth,

	/// <summary>Bezier segments between points.</summary>
	Bezier,
}

/// <summary>
/// How an authored curve is shaped.
/// </summary>
public enum CurveShape
{
	/// <summary>Smoothed through the authored points.</summary>
	Smooth,

	/// <summary>Follows the authored points exactly.</summary>
	Freehand,
}

/// <summary>
/// Whether a curve point smooths through or forms a corner.
/// </summary>
public enum CurvePointKind
{
	/// <summary>The curve passes smoothly through the point.</summary>
	Smooth,

	/// <summary>The curve forms a corner at the point.</summary>
	Corner,
}

/// <summary>
/// Which sequencer interactions are enabled.
/// </summary>
[Flags]
public enum SequencerFeatures
{
	/// <summary>No editing.</summary>
	None = 0,

	/// <summary>Clip start and end may be dragged.</summary>
	EditStartEnd = 1 << 1,

	/// <summary>The current frame may be scrubbed.</summary>
	ChangeFrame = 1 << 3,

	/// <summary>Items may be added.</summary>
	Add = 1 << 4,

	/// <summary>Items may be deleted.</summary>
	Delete = 1 << 5,

	/// <summary>Items may be copied and pasted.</summary>
	CopyPaste = 1 << 6,

	/// <summary>Both editing interactions.</summary>
	EditAll = EditStartEnd | ChangeFrame,
}

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Converts an interpolation mode to Hexa's curve-edit type.
	/// </summary>
	/// <param name="value">The interpolation mode.</param>
	/// <returns>The equivalent Hexa value.</returns>
	internal static HexaCurveEditType MapInterpolation(CurveInterpolation value) => value switch
	{
		CurveInterpolation.None => HexaCurveEditType.None,
		CurveInterpolation.Discrete => HexaCurveEditType.CurveDiscrete,
		CurveInterpolation.Smooth => HexaCurveEditType.CurveSmooth,
		CurveInterpolation.Bezier => HexaCurveEditType.CurveBezier,
		_ => HexaCurveEditType.CurveLinear,
	};

	/// <summary>
	/// Converts Hexa's curve-edit type to an interpolation mode.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent interpolation mode.</returns>
	internal static CurveInterpolation MapInterpolationBack(HexaCurveEditType value) => value switch
	{
		HexaCurveEditType.None => CurveInterpolation.None,
		HexaCurveEditType.CurveDiscrete => CurveInterpolation.Discrete,
		HexaCurveEditType.CurveSmooth => CurveInterpolation.Smooth,
		HexaCurveEditType.CurveBezier => CurveInterpolation.Bezier,
		_ => CurveInterpolation.Linear,
	};

	/// <summary>
	/// Converts a curve shape to Hexa's mathematics curve type.
	/// </summary>
	/// <param name="value">The curve shape.</param>
	/// <returns>The equivalent Hexa value.</returns>
	/// <remarks>
	/// Deliberately separate from <see cref="MapInterpolation"/>. Hexa has two unrelated enums
	/// named <c>CurveType</c>, and neither member set is a subset of the other.
	/// </remarks>
	internal static HexaMathCurveType MapShape(CurveShape value) =>
		value == CurveShape.Freehand ? HexaMathCurveType.Freehand : HexaMathCurveType.Smooth;

	/// <summary>
	/// Converts Hexa's mathematics curve type to a curve shape.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent curve shape.</returns>
	internal static CurveShape MapShapeBack(HexaMathCurveType value) =>
		value == HexaMathCurveType.Freehand ? CurveShape.Freehand : CurveShape.Smooth;

	/// <summary>
	/// Converts a point kind to Hexa's curve point type.
	/// </summary>
	/// <param name="value">The point kind.</param>
	/// <returns>The equivalent Hexa value.</returns>
	internal static HexaCurvePointType MapPointKind(CurvePointKind value) =>
		value == CurvePointKind.Corner ? HexaCurvePointType.Corner : HexaCurvePointType.Smooth;

	/// <summary>
	/// Converts Hexa's curve point type to a point kind.
	/// </summary>
	/// <param name="value">The Hexa value.</param>
	/// <returns>The equivalent point kind.</returns>
	internal static CurvePointKind MapPointKindBack(HexaCurvePointType value) =>
		value == HexaCurvePointType.Corner ? CurvePointKind.Corner : CurvePointKind.Smooth;

	/// <summary>
	/// Converts sequencer features to Hexa's options.
	/// </summary>
	/// <param name="value">The features to enable.</param>
	/// <returns>The equivalent Hexa options.</returns>
	/// <remarks>
	/// The numeric values are chosen to match upstream exactly, including its unused bits at
	/// <c>1 &lt;&lt; 0</c> and <c>1 &lt;&lt; 2</c>, so the cast is faithful for arbitrary combinations.
	/// </remarks>
	internal static HexaSequencerOptions MapFeatures(SequencerFeatures value) => (HexaSequencerOptions)(int)value;
}

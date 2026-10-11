// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

/// <summary>Where a <see cref="ImGuiWidgets.ValueSlider(string, ref float, float, float, ValueSliderOptions?)"/> fill starts.</summary>
public enum ValueSliderFill
{
	/// <summary>The fill runs from the left edge, the minimum, up to the value.</summary>
	FromMinimum,

	/// <summary>
	/// The fill runs from zero to the value, either side of it, for a range such as -100..100 whose
	/// middle means "no change". A zero outside the range is clamped to its nearer end.
	/// </summary>
	FromZero,
}

/// <summary>Options for <see cref="ImGuiWidgets.ValueSlider(string, ref float, float, float, ValueSliderOptions?)"/> and its integer overload.</summary>
public sealed class ValueSliderOptions
{
	/// <summary>
	/// Gets or sets the .NET numeric format the value is shown with, such as <c>"0.0"</c>. Leave null to
	/// pick by the range: whole numbers when it spans a hundred or more, one decimal place from ten, two
	/// from one, and three below that. The integer overload shows whole numbers when this is null.
	/// </summary>
	public string? Format { get; set; }

	/// <summary>
	/// Gets or sets text written straight after the number, such as <c>"%"</c>, <c>"°"</c> or <c>" px"</c>.
	/// Nothing is inserted between the two, so include a space where the unit wants one. Typing the unit
	/// back in is accepted and ignored.
	/// </summary>
	public string? Unit { get; set; }

	/// <summary>
	/// Gets or sets text written straight before the number, such as <c>"Size "</c>, for a slider whose
	/// label sits inside its frame where a row has no room for one beside it. Nothing is inserted between
	/// the two, so include a space. The value opens for typing without it, and typing it back is accepted
	/// and ignored.
	/// </summary>
	public string? Prefix { get; set; }

	/// <summary>Gets or sets where the fill starts. <see cref="ValueSliderFill.FromMinimum"/> by default.</summary>
	public ValueSliderFill Fill { get; set; }

	/// <summary>
	/// Gets or sets whether the frame covers the range logarithmically, so each stretch of a drag multiplies
	/// the value by the same factor rather than adding the same amount. Suits a size or radius, where the
	/// difference between 2 and 4 matters as much as the difference between 200 and 400. Off by default.
	/// </summary>
	/// <remarks>
	/// A range starting at zero keeps zero at its left end and runs logarithmically from the smallest step
	/// the format shows, so 0..100 shown as <c>"0"</c> goes 0, then 1 to 100; with <c>"0.0"</c> it goes 0,
	/// then 0.1 to 100. A range wholly below zero mirrors one above it. A range that crosses zero is covered
	/// linearly, since no logarithm passes through it.
	/// </remarks>
	public bool Logarithmic { get; set; }

	/// <summary>
	/// Gets or sets the value a double-click returns the slider to. Leave null and a double-click opens
	/// the value for typing instead, as Ctrl+click always does.
	/// </summary>
	public double? ResetValue { get; set; }

	/// <summary>
	/// Gets or sets how much of a normal drag a Shift-held drag moves the value by. 0.1 by default, so
	/// holding Shift makes adjustments ten times finer. Shift can be pressed or released mid-drag.
	/// </summary>
	public float FineScale { get; set; } = 0.1f;
}

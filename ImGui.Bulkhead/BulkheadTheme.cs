// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Collections.ObjectModel;

using ktsu.Semantics.Color;
using ktsu.ThemeProvider;

/// <summary>
/// The Bulkhead colours as a ktsu.ThemeProvider semantic theme, so the rest of the suite
/// (<c>Theme.Apply</c>, <c>ScopedTheme</c>, <c>Palette</c>) can use them like any built-in theme.
/// </summary>
/// <remarks>
/// This sets colours only. <see cref="BulkheadStyle"/> also sets the shapes (rounding, spacing) that
/// make standard ImGui controls sit beside the Bulkhead widgets.
/// </remarks>
public sealed class BulkheadTheme : ISemanticTheme
{
	private static readonly SemanticPalette Palette = new()
	{
		Neutrals = ["#F0E6D2", "#B8AE9C", "#5C5650", "#1A1816", "#000000"],
		Primary = "#F5A04A",
		Alternate = "#C29BE0",
		Success = "#7FC8A0",
		CallToAction = "#C9799A",
		Information = "#8E9CF0",
		Caution = "#F2CC55",
		Warning = "#F7C08A",
		Error = "#E5553F",
		Failure = "#B8402E",
		Debug = "#9CCBEA",
	};

	/// <summary>Gets a shared instance; the theme holds no state.</summary>
	public static BulkheadTheme Instance { get; } = new();

	/// <inheritdoc/>
	public Dictionary<SemanticMeaning, Collection<Color>> SemanticMapping => Palette.ToSemanticMapping();

	/// <inheritdoc/>
	public bool IsDarkTheme => true;
}

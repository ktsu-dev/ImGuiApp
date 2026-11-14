// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using ktsu.ThemeProvider;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// The standby palette as a semantic theme, so it can go through <c>Theme.Apply</c> and
/// <c>ScopedTheme</c> like any other theme in the suite: orange leads, red means failure, amber
/// caution and green success, on a near-black field.
/// </summary>
public sealed class SortieTheme : ISemanticTheme
{
	private static readonly SemanticPalette Palette = new()
	{
		Neutrals = ["#ffc07a", "#070403"],
		Primary = "#ff7a12",
		Alternate = "#8a3c08",
		Success = "#48f06a",
		CallToAction = "#ffb24a",
		Information = "#ff7a12",
		Caution = "#ffd02a",
		Warning = "#ff7a12",
		Error = "#f01e24",
		Failure = "#f01e24",
		Debug = "#8a3c08",
	};

	/// <inheritdoc/>
	public Dictionary<SemanticMeaning, Collection<SemanticColor>> SemanticMapping => Palette.ToSemanticMapping();

	/// <inheritdoc/>
	public bool IsDarkTheme => true;
}

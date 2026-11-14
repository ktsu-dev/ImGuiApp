// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using ktsu.ThemeProvider;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// The green phosphor as a semantic theme, so it can go through <c>Theme.Apply</c> and
/// <c>ScopedTheme</c> like any other theme in the suite. Every meaning is the same hue at a
/// different brightness, because that is all a monochrome tube can show.
/// </summary>
public sealed class GreenPhosphorTheme : ISemanticTheme
{
	private static readonly SemanticPalette Palette = new()
	{
		Neutrals = ["#6cff86", "#020b05"],
		Primary = "#2ed058",
		Alternate = "#167a34",
		Success = "#6cff86",
		CallToAction = "#d8ffdc",
		Information = "#2ed058",
		Caution = "#6cff86",
		Warning = "#d8ffdc",
		Error = "#d8ffdc",
		Failure = "#d8ffdc",
		Debug = "#167a34",
	};

	/// <inheritdoc/>
	public Dictionary<SemanticMeaning, Collection<SemanticColor>> SemanticMapping => Palette.ToSemanticMapping();

	/// <inheritdoc/>
	public bool IsDarkTheme => true;
}

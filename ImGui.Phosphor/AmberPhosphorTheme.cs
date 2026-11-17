// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using ktsu.ThemeProvider;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// The amber phosphor as a semantic theme. See <see cref="GreenPhosphorTheme"/>.
/// </summary>
public sealed class AmberPhosphorTheme : ISemanticTheme
{
	private static readonly SemanticPalette Palette = new()
	{
		Neutrals = ["#ffc23a", "#0b0701"],
		Primary = "#e09610",
		Alternate = "#865506",
		Success = "#ffc23a",
		CallToAction = "#fff0c8",
		Information = "#e09610",
		Caution = "#ffc23a",
		Warning = "#fff0c8",
		Error = "#fff0c8",
		Failure = "#fff0c8",
		Debug = "#865506",
	};

	/// <inheritdoc/>
	public Dictionary<SemanticMeaning, Collection<SemanticColor>> SemanticMapping => Palette.ToSemanticMapping();

	/// <inheritdoc/>
	public bool IsDarkTheme => true;
}

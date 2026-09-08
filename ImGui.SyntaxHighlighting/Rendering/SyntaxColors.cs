// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;
using ktsu.SyntaxHighlighting;

/// <summary>
/// Turns a theme into the packed colors the draw list needs, filling the theme's unset members from
/// the active ImGui style so a code block keeps matching the surrounding UI.
/// </summary>
internal static class SyntaxColors
{
	/// <summary>
	/// Picks the palette to draw with: the configured one, or the built-in palette that suits the
	/// current window background.
	/// </summary>
	/// <param name="config">The active config.</param>
	/// <returns>The palette to use for this frame.</returns>
	public static SyntaxTheme Resolve(SyntaxHighlightConfig config)
	{
		if (config.Theme is not null)
		{
			return config.Theme;
		}

		Span<System.Numerics.Vector4> colors = ImGui.GetStyle().Colors;
		ImColor background = new() { Value = colors[(int)ImGuiCol.WindowBg] };
		return SyntaxTheme.ForBackgroundLuminance(background.GetRelativeLuminance());
	}

	/// <summary>Builds the packed color for every token kind, in <see cref="TokenKind"/> order.</summary>
	/// <param name="theme">The resolved palette.</param>
	/// <returns>Packed colors indexed by <see cref="TokenKind"/>.</returns>
	public static uint[] BuildPalette(SyntaxTheme theme)
	{
		uint plain = Plain(theme);
		TokenKind[] kinds = Enum.GetValues<TokenKind>();
		uint[] palette = new uint[kinds.Length];
		foreach (TokenKind kind in kinds)
		{
			Color? color = theme.ColorFor(kind);
			palette[(int)kind] = color.HasValue ? color.Value.ToImGuiU32() : plain;
		}

		return palette;
	}

	/// <summary>The color for unclassified text: the theme's, else ImGui's text color.</summary>
	/// <param name="theme">The resolved palette.</param>
	/// <returns>A packed color.</returns>
	public static uint Plain(SyntaxTheme theme) =>
		theme.Plain?.ToImGuiU32() ?? ImGui.GetColorU32(ImGuiCol.Text);

	/// <summary>The line-number color: the theme's, else ImGui's disabled text color.</summary>
	/// <param name="theme">The resolved palette.</param>
	/// <returns>A packed color.</returns>
	public static uint LineNumber(SyntaxTheme theme) =>
		theme.LineNumber?.ToImGuiU32() ?? ImGui.GetColorU32(ImGuiCol.TextDisabled);

	/// <summary>The background color: the theme's, else ImGui's frame background.</summary>
	/// <param name="theme">The resolved palette.</param>
	/// <returns>A packed color.</returns>
	public static uint Background(SyntaxTheme theme) =>
		theme.Background?.ToImGuiU32() ?? ImGui.GetColorU32(ImGuiCol.FrameBg);
}

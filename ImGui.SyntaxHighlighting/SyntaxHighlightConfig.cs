// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;

using Hexa.NET.ImGui;

using ktsu.SyntaxHighlighting;

/// <summary>
/// Rendering options for the syntax highlighting widgets. Every member is optional; the defaults
/// produce a themed code block that follows the application's ImGui theme.
/// </summary>
public sealed record SyntaxHighlightConfig
{
	/// <summary>
	/// Resolves the monospace font to draw code with, given the target pixel size. Return
	/// <see langword="null"/> (or supply no resolver) to keep the current font.
	/// </summary>
	public Func<float, ImFontPtr?>? FontResolver { get; init; }

	/// <summary>
	/// The color palette. When <see langword="null"/>, a built-in palette is chosen per frame from
	/// the luminance of the ImGui window background, so light and dark themes both read correctly.
	/// </summary>
	public SyntaxTheme? Theme { get; init; }

	/// <summary>Whether to draw a line-number gutter.</summary>
	public bool ShowLineNumbers { get; init; }

	/// <summary>The number given to the first line when the gutter is shown.</summary>
	public int FirstLineNumber { get; init; } = 1;

	/// <summary>The tab stop width in characters used when expanding tabs to spaces.</summary>
	public int TabWidth { get; init; } = 4;

	/// <summary>Whether to fill a background panel behind the code.</summary>
	public bool ShowBackground { get; init; } = true;

	/// <summary>The background panel's corner rounding, in pixels.</summary>
	public float BackgroundRounding { get; init; } = 3.0f;

	/// <summary>Padding between the background panel's edge and the code, in pixels.</summary>
	public float PaddingPixels { get; init; } = 6.0f;

	/// <summary>Extra vertical spacing added between code lines, in pixels.</summary>
	public float LineSpacingPixels { get; init; } = 2.0f;

	/// <summary>Spacing between the line-number gutter and the code, in pixels.</summary>
	public float GutterSpacingPixels { get; init; } = 10.0f;

	/// <summary>
	/// The code font's pixel size; when <see langword="null"/>, the current font size is used, so
	/// DPI and global scale are followed automatically.
	/// </summary>
	public float? FontSizePixels { get; init; }

	/// <summary>
	/// The block's width in pixels; when <see langword="null"/>, the available content width is used.
	/// Code is never wrapped — lines longer than the block are clipped by the surrounding window, so
	/// wrap the call in a horizontally scrolling child window when long lines must stay reachable.
	/// </summary>
	public float? Width { get; init; }
}

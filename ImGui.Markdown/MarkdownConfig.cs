// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.ImGui.Color;

/// <summary>
/// Identifies the typographic role of a run of markdown text so a font resolver can
/// supply the matching font variant.
/// </summary>
public enum MarkdownFontRole
{
	/// <summary>Normal body text.</summary>
	Body,
	/// <summary>Strong / bold text.</summary>
	Bold,
	/// <summary>Emphasised / italic text.</summary>
	Italic,
	/// <summary>Combined bold and italic text.</summary>
	BoldItalic,
	/// <summary>Monospace code text.</summary>
	Code,
	/// <summary>Level 1 heading.</summary>
	H1,
	/// <summary>Level 2 heading.</summary>
	H2,
	/// <summary>Level 3 heading.</summary>
	H3,
	/// <summary>Level 4 heading.</summary>
	H4,
	/// <summary>Level 5 heading.</summary>
	H5,
	/// <summary>Level 6 heading.</summary>
	H6,
}

/// <summary>
/// The result of resolving a local image source: the ImGui texture id and the size at
/// which to draw it.
/// </summary>
/// <param name="TextureId">The ImGui texture identifier.</param>
/// <param name="Size">The draw size in pixels.</param>
public readonly record struct MarkdownImageResult(nint TextureId, Vector2 Size);

/// <summary>
/// Rendering options for the markdown widgets. All members are optional; defaults produce
/// a self-contained renderer that uses faux emphasis styling and image placeholders.
/// </summary>
public sealed record MarkdownConfig
{
	/// <summary>Default heading size multipliers applied to the live body font size, H1 first.</summary>
	public static IReadOnlyList<float> DefaultHeadingScales { get; } = [2.0f, 1.6f, 1.35f, 1.15f, 1.0f, 0.9f];

	/// <summary>
	/// Resolves a font for a role at a target pixel size. Return <see langword="null"/> to
	/// fall back to the current font (with faux bold/italic styling for emphasis roles).
	/// </summary>
	public Func<MarkdownFontRole, float, ImFontPtr?>? FontResolver { get; init; }

	/// <summary>
	/// Invoked when a link is clicked. When <see langword="null"/>, http/https/mailto links
	/// open with the operating system default handler.
	/// </summary>
	public Action<string>? OnLinkClicked { get; init; }

	/// <summary>
	/// Resolves a local image source to a texture. Return <see langword="null"/> (or supply
	/// no resolver) to render a placeholder box with the alt text.
	/// </summary>
	public Func<string, MarkdownImageResult?>? ImageResolver { get; init; }

	/// <summary>Heading size multipliers applied to the live body font size, H1 first.</summary>
	public IReadOnlyList<float> HeadingScales { get; init; } = DefaultHeadingScales;

	/// <summary>Explicit wrap width in pixels; when <see langword="null"/>, the available content width is used.</summary>
	public float? WrapWidth { get; init; }

	/// <summary>Indentation applied per list nesting level, in pixels.</summary>
	public float ListIndentPixels { get; init; } = 20.0f;

	/// <summary>Vertical spacing added after paragraphs and blocks, in pixels.</summary>
	public float ParagraphSpacingPixels { get; init; } = 6.0f;

	/// <summary>Explicit link color; when <see langword="null"/>, a theme color is used.</summary>
	public ImGuiVector4? LinkColor { get; init; }
}

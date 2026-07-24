// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

/// <summary>Resolves markdown element colors from the active ImGui theme, with config overrides.</summary>
internal static class MarkdownColors
{
	/// <summary>The link color: config override, else a theme accent.</summary>
	/// <param name="config">The active config.</param>
	/// <returns>The link color as a vector.</returns>
	public static ImGuiVector4 Link(MarkdownConfig config)
	{
		if (config.LinkColor.HasValue)
		{
			return config.LinkColor.Value;
		}

		// ButtonHovered reads as an interactive accent across the bundled themes.
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		return new ImGuiVector4(colors[(int)ImGuiCol.ButtonHovered]);
	}

	/// <summary>Packed background color for inline code spans.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint InlineCodeBackground() => ImGui.GetColorU32(ImGuiCol.FrameBg);

	/// <summary>Packed color for the blockquote accent bar.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint BlockquoteBar() => ImGui.GetColorU32(ImGuiCol.Border);

	/// <summary>Packed color for thematic-break separators.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint Separator() => ImGui.GetColorU32(ImGuiCol.Separator);

	/// <summary>Packed color for normal text.</summary>
	/// <returns>An ImGui U32 color.</returns>
	public static uint TextU32() => ImGui.GetColorU32(ImGuiCol.Text);
}

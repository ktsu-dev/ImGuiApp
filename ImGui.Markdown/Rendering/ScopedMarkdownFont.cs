// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Pushes the font for a markdown role and pops it on dispose. When the config supplies no
/// resolver (or the resolver returns null for an emphasis role), records that faux styling
/// should be applied by the caller.
/// </summary>
internal sealed class ScopedMarkdownFont : IDisposable
{
	private bool disposed;

	/// <summary>Whether the caller should synthesize bold (no real bold glyphs available).</summary>
	public bool FauxBold { get; }

	/// <summary>Whether the caller should synthesize italic (no real italic glyphs available).</summary>
	public bool FauxItalic { get; }

	/// <summary>The pixel size the font was pushed at.</summary>
	public float PixelSize { get; }

	/// <summary>Pushes the resolved font for a role at a target pixel size.</summary>
	/// <param name="role">The typographic role.</param>
	/// <param name="pixelSize">The target pixel size (already scaled).</param>
	/// <param name="config">The active config providing the optional resolver.</param>
	public ScopedMarkdownFont(MarkdownFontRole role, float pixelSize, MarkdownConfig config)
	{
		PixelSize = pixelSize;
		ImFontPtr? resolved = config.FontResolver?.Invoke(role, pixelSize);

		if (resolved.HasValue && !resolved.Value.IsNull)
		{
			ImGui.PushFont(resolved.Value, pixelSize);
		}
		else
		{
			// No variant: keep the current font at the target size and fall back to faux styling.
			ImGui.PushFont(ImGui.GetFont(), pixelSize);
			FauxBold = role is MarkdownFontRole.Bold or MarkdownFontRole.BoldItalic;
			FauxItalic = role is MarkdownFontRole.Italic or MarkdownFontRole.BoldItalic;
		}
	}

	/// <summary>Pops the pushed font.</summary>
	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			ImGui.PopFont();
		}
	}
}

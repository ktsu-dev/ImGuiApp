// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;

/// <summary>
/// Helper class for configuring ImGui fonts with Unicode and emoji support.
/// Works with user-configured fonts rather than forcing system fonts.
/// </summary>
public static class FontHelper
{
	/// <summary>
	/// Stores GCHandle instances for custom fonts to prevent memory leaks.
	/// </summary>
	private static readonly List<GCHandle> customFontHandles = [];

	/// <summary>
	/// Cleans up all pinned memory handles for custom fonts.
	/// This should be called when shutting down the application.
	/// </summary>
	public static void CleanupCustomFonts()
	{
		foreach (GCHandle handle in customFontHandles)
		{
			try
			{
				if (handle.IsAllocated)
				{
					handle.Free();
				}
			}
			catch (InvalidOperationException)
			{
				// Handle was already freed, ignore
			}
		}
		customFontHandles.Clear();
	}

	/// <summary>
	/// Gets the extended Unicode glyph ranges that ImGuiApp uses by default.
	/// This includes ASCII, Latin Extended, common Unicode symbol blocks, and emoji ranges.
	/// </summary>
	/// <param name="fontAtlasPtr">The font atlas to use for building ranges.</param>
	/// <returns>Pointer to the extended Unicode glyph ranges.</returns>
	public static unsafe uint* GetExtendedUnicodeRanges(ImFontAtlasPtr fontAtlasPtr)
	{
		ImFontGlyphRangesBuilderPtr builder = new(ImGui.ImFontGlyphRangesBuilder());

		// Add default ranges (ASCII)
		builder.AddRanges(fontAtlasPtr.GetGlyphRangesDefault());

		// Add Latin Extended for accented characters
		builder.AddRanges(fontAtlasPtr.GetGlyphRangesLatinExt());

		// Add common Unicode blocks for symbols
		builder.AddChar(0x2000, 0x206F); // General Punctuation
		builder.AddChar(0x20A0, 0x20CF); // Currency Symbols
		builder.AddChar(0x2100, 0x214F); // Letterlike Symbols
		builder.AddChar(0x2190, 0x21FF); // Arrows
		builder.AddChar(0x2200, 0x22FF); // Mathematical Operators
		builder.AddChar(0x2300, 0x23FF); // Miscellaneous Technical
		builder.AddChar(0x2500, 0x257F); // Box Drawing
		builder.AddChar(0x2580, 0x259F); // Block Elements
		builder.AddChar(0x25A0, 0x25FF); // Geometric Shapes
		builder.AddChar(0x2600, 0x26FF); // Miscellaneous Symbols

		// Add emoji ranges (will only work if the font supports them)
		builder.AddChar(0x1F600, 0x1F64F); // Emoticons
		builder.AddChar(0x1F300, 0x1F5FF); // Miscellaneous Symbols and Pictographs
		builder.AddChar(0x1F680, 0x1F6FF); // Transport and Map Symbols
		builder.AddChar(0x1F700, 0x1F77F); // Alchemical Symbols
		builder.AddChar(0x1F780, 0x1F7FF); // Geometric Shapes Extended
		builder.AddChar(0x1F800, 0x1F8FF); // Supplemental Arrows-C
		builder.AddChar(0x1F900, 0x1F9FF); // Supplemental Symbols and Pictographs
		builder.AddChar(0x1FA00, 0x1FA6F); // Chess Symbols
		builder.AddChar(0x1FA70, 0x1FAFF); // Symbols and Pictographs Extended-A

		// Build the ranges
		ImVector<uint> ranges = new();
		builder.BuildRanges(&ranges);
		return ranges.Data;
	}

	/// <summary>
	/// Adds a custom font with specific glyph ranges to the ImGui font atlas.
	/// This allows you to load fonts with exactly the character ranges you need.
	/// The font data is pinned in memory and tracked for proper cleanup.
	/// Call <see cref="CleanupCustomFonts"/> when shutting down to prevent memory leaks.
	/// </summary>
	/// <param name="io">The ImGui IO pointer.</param>
	/// <param name="fontData">The font data as a byte array.</param>
	/// <param name="fontSize">The font size in pixels.</param>
	/// <param name="glyphRanges">The glyph ranges to include, or null for default ASCII.</param>
	/// <param name="mergeWithPrevious">Whether to merge this font with the previously added font.</param>
	/// <returns>The ImFont pointer for the added font, or null if failed.</returns>
	public static unsafe ImFontPtr? AddCustomFont(ImGuiIOPtr io, byte[] fontData, float fontSize, uint* glyphRanges = null, bool mergeWithPrevious = false)
	{
		ArgumentNullException.ThrowIfNull(fontData);

		GCHandle fontHandle = default;
		try
		{
			// Pin the font data in memory
			fontHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
			nint fontPtr = fontHandle.AddrOfPinnedObject();

			// Create font configuration
			ImFontConfigPtr fontConfig = ImGui.ImFontConfig();
			fontConfig.FontDataOwnedByAtlas = false; // We manage the memory
			fontConfig.PixelSnapH = true;
			fontConfig.MergeMode = mergeWithPrevious;

			// Use provided glyph ranges or default to ASCII
			uint* ranges = glyphRanges ?? io.Fonts.GetGlyphRangesDefault();

			// Add font to atlas
			ImFont* font = io.Fonts.AddFontFromMemoryTTF((void*)fontPtr, fontData.Length, fontSize, fontConfig, ranges);

			if (font is null)
			{
				// Font addition failed, free the handle immediately
				fontHandle.Free();
				return null;
			}

			// Store the handle for proper cleanup to prevent memory leaks
			customFontHandles.Add(fontHandle);

			return font;
		}
		catch (Exception ex)
		{
			// Free the handle if it was allocated
			if (fontHandle.IsAllocated)
			{
				fontHandle.Free();
			}

			ImGuiApp.DebugLogger.Log($"FontHelper.AddCustomFont failed: {ex.Message}");
			return null;
		}
	}
}

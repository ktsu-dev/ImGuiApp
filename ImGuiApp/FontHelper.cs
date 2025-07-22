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
	/// Stores the extended Unicode glyph ranges to prevent memory deallocation.
	/// </summary>
	private static ImVector<uint> extendedUnicodeRanges;

	/// <summary>
	/// Stores the emoji glyph ranges to prevent memory deallocation.
	/// </summary>
	private static ImVector<uint> emojiRanges;

	/// <summary>
	/// Tracks whether the extended Unicode ranges have been initialized.
	/// </summary>
	private static bool extendedUnicodeRangesInitialized;

	/// <summary>
	/// Tracks whether the emoji ranges have been initialized.
	/// </summary>
	private static bool emojiRangesInitialized;

	/// <summary>
	/// Cleans up all pinned memory handles for custom fonts and glyph ranges.
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

		// Reset glyph ranges
		CleanupGlyphRanges();
	}

	/// <summary>
	/// Cleans up the cached glyph ranges and resets initialization flags.
	/// This allows the ranges to be rebuilt if needed after cleanup.
	/// </summary>
	public static void CleanupGlyphRanges()
	{
		extendedUnicodeRangesInitialized = false;
		emojiRangesInitialized = false;
		// Note: ImVector<uint> instances are value types managed by ImGui
		// and will be properly cleaned up when reassigned
	}

	/// <summary>
	/// Gets the extended Unicode glyph ranges that ImGuiApp uses for main fonts.
	/// Includes ASCII, Latin Extended, symbols, arrows, math, shapes, and Nerd Font icons.
	/// Note: Emoji ranges (1F*** blocks) are handled separately to avoid conflicts.
	/// </summary>
	/// <param name="fontAtlasPtr">The font atlas to use for building ranges.</param>
	/// <returns>Pointer to the extended Unicode glyph ranges for main fonts.</returns>
	public static unsafe uint* GetExtendedUnicodeRanges(ImFontAtlasPtr fontAtlasPtr)
	{
		// Only build ranges once and store them to prevent memory deallocation
		if (!extendedUnicodeRangesInitialized)
		{
			ImFontGlyphRangesBuilderPtr builder = new(ImGui.ImFontGlyphRangesBuilder());

			// Add default ranges (ASCII)
			builder.AddRanges(fontAtlasPtr.GetGlyphRangesDefault());

			// Add Latin Extended ranges
			AddLatinExtendedRanges(builder);

			// Add common Unicode symbol blocks
			AddSymbolRanges(builder);

			// Add Nerd Font icon ranges
			AddNerdFontRanges(builder);

			// Build the ranges and store them in the static field
			extendedUnicodeRanges = new ImVector<uint>();
			fixed (ImVector<uint>* rangesPtr = &extendedUnicodeRanges)
			{
				builder.BuildRanges(rangesPtr);
			}
			extendedUnicodeRangesInitialized = true;
		}

		return extendedUnicodeRanges.Data;
	}

	/// <summary>
	/// Adds emoji-specific ranges to the glyph ranges builder.
	/// Only includes true emoji ranges (1F*** blocks) to avoid conflicts with main font symbol ranges.
	/// </summary>
	/// <param name="builder">The glyph ranges builder to add emoji ranges to.</param>
	private static void AddEmojiRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Variation Selectors (important for emoji presentation vs text presentation)
		for (uint c = 0xFE00; c <= 0xFE0F; c++) // Variation Selectors 1-16
		{
			builder.AddChar(c);
		}

		// Emoji-specific Unicode ranges (1F*** blocks only to avoid main font conflicts)
		(uint start, uint end, string description)[] emojiRanges = [
			(0x1F300, 0x1F5FF, "Miscellaneous Symbols and Pictographs"),
			(0x1F600, 0x1F64F, "Emoticons"),
			(0x1F650, 0x1F67F, "Ornamental Dingbats"),
			(0x1F680, 0x1F6FF, "Transport and Map Symbols"),
			(0x1F700, 0x1F77F, "Alchemical Symbols"),
			(0x1F780, 0x1F7FF, "Geometric Shapes Extended"),
			(0x1F800, 0x1F8FF, "Supplemental Arrows-C"),
			(0x1F900, 0x1F9FF, "Supplemental Symbols and Pictographs"),
			(0x1FA00, 0x1FA6F, "Chess Symbols"),
			(0x1FA70, 0x1FAFF, "Symbols and Pictographs Extended-A"),
			(0x1FB00, 0x1FBFF, "Symbols for Legacy Computing"),
		];

		foreach ((uint start, uint end, string _) in emojiRanges)
		{
			for (uint c = start; c <= end; c++)
			{
				builder.AddChar(c);
			}
		}
	}

	/// <summary>
	/// Gets emoji-specific glyph ranges for loading emoji fonts.
	/// Includes only true emoji ranges (1F*** blocks) plus ASCII and variation selectors.
	/// </summary>
	/// <returns>Pointer to the emoji glyph ranges.</returns>
	public static unsafe uint* GetEmojiRanges()
	{
		// Only build ranges once and store them to prevent memory deallocation
		if (!emojiRangesInitialized)
		{
			ImFontGlyphRangesBuilderPtr builder = new(ImGui.ImFontGlyphRangesBuilder());

			// Add emoji-specific ranges
			AddEmojiRanges(builder);

			// Build the ranges and store them in the static field
			emojiRanges = new ImVector<uint>();
			fixed (ImVector<uint>* rangesPtr = &emojiRanges)
			{
				builder.BuildRanges(rangesPtr);
			}
			emojiRangesInitialized = true;
		}

		return emojiRanges.Data;
	}

	/// <summary>
	/// Adds Latin Extended character ranges to the glyph ranges builder.
	/// Includes Latin Extended-A and Latin Extended-B character blocks.
	/// </summary>
	/// <param name="builder">The glyph ranges builder to add Latin Extended ranges to.</param>
	private static void AddLatinExtendedRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Latin Extended-A (U+0100–U+017F)
		for (uint c = 0x0100; c <= 0x017F; c++)
		{
			builder.AddChar(c);
		}
		// Latin Extended-B (U+0180–U+024F)
		for (uint c = 0x0180; c <= 0x024F; c++)
		{
			builder.AddChar(c);
		}
	}

	/// <summary>
	/// Adds common Unicode symbol blocks to the glyph ranges builder.
	/// Includes punctuation, currency, mathematical symbols, arrows, and various technical symbols.
	/// </summary>
	/// <param name="builder">The glyph ranges builder to add symbol ranges to.</param>
	private static void AddSymbolRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Define symbol ranges to add (avoiding emoji ranges which are handled separately)
		(uint start, uint end, string description)[] symbolRanges = [
			(0x2000, 0x206F, "General Punctuation"),
			(0x20A0, 0x20CF, "Currency Symbols"),
			(0x2100, 0x214F, "Letterlike Symbols"),
			(0x2190, 0x21FF, "Arrows"),
			(0x2200, 0x22FF, "Mathematical Operators"),
			(0x2300, 0x23FF, "Miscellaneous Technical"),
			(0x2460, 0x24FF, "Enclosed Alphanumerics"),
			(0x2500, 0x257F, "Box Drawing"),
			(0x2580, 0x259F, "Block Elements"),
			(0x25A0, 0x25FF, "Geometric Shapes"),
			(0x2600, 0x26FF, "Miscellaneous Symbols"), // Includes ☀️ ⭐ ❤️ etc. (non-emoji variants)
			(0x2700, 0x27BF, "Dingbats") // Includes ✂️ ✈️ ☎️ etc. (non-emoji variants)
		];

		foreach ((uint start, uint end, string _) in symbolRanges)
		{
			for (uint c = start; c <= end; c++)
			{
				builder.AddChar(c);
			}
		}
	}

	/// <summary>
	/// Adds Nerd Font icon ranges to the glyph ranges builder.
	/// Includes Font Awesome, Material Design Icons, Octicons, Weather Icons, and other common icon sets.
	/// </summary>
	/// <param name="builder">The glyph ranges builder to add Nerd Font ranges to.</param>
	private static void AddNerdFontRanges(ImFontGlyphRangesBuilderPtr builder)
	{
		// Define Nerd Font ranges to add
		(uint start, uint end, string description)[] nerdFontRanges = [
			// Powerline symbols
			(0xE0A0, 0xE0A2, "Powerline symbols"),
			(0xE0B0, 0xE0B3, "Powerline symbols"),
			// Pomicons
			(0xE000, 0xE00D, "Pomicons"),
			// Powerline Extra Symbols
			(0xE0A3, 0xE0A3, "Powerline Extra"),
			(0xE0B4, 0xE0C8, "Powerline Extra"),
			(0xE0CA, 0xE0CA, "Powerline Extra"),
			(0xE0CC, 0xE0D2, "Powerline Extra"),
			(0xE0D4, 0xE0D4, "Powerline Extra"),
			// Weather Icons
			(0xE300, 0xE3EB, "Weather Icons"),
			// Font Awesome Extension
			(0xE200, 0xE2A9, "Font Awesome Extension"),
			// Font Logos
			(0xF300, 0xF313, "Font Logos"),
			// Octicons (main range)
			(0xF400, 0xF4A8, "Octicons"),
			// Font Awesome
			(0xF000, 0xF2E0, "Font Awesome"),
			// Material Design Icons
			(0xF500, 0xFD46, "Material Design Icons"),
			// Devicons
			(0xE700, 0xE7C5, "Devicons"),
		];

		foreach ((uint start, uint end, string _) in nerdFontRanges)
		{
			for (uint c = start; c <= end; c++)
			{
				builder.AddChar(c);
			}
		}

		// Add individual Octicon symbols that are outside the main range
		uint[] individualOcticons = [0x2665, 0x26A1];
		foreach (uint c in individualOcticons)
		{
			builder.AddChar(c);
		}
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
			uint* ranges = glyphRanges != null ? glyphRanges : io.Fonts.GetGlyphRangesDefault();

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
		catch (ArgumentException ex)
		{
			// Free the handle if it was allocated
			if (fontHandle.IsAllocated)
			{
				fontHandle.Free();
			}

			ImGuiApp.DebugLogger.Log($"FontHelper.AddCustomFont failed with ArgumentException: {ex.Message}");
			return null;
		}
		catch (OutOfMemoryException ex)
		{
			// Free the handle if it was allocated
			if (fontHandle.IsAllocated)
			{
				fontHandle.Free();
			}

			ImGuiApp.DebugLogger.Log($"FontHelper.AddCustomFont failed with OutOfMemoryException: {ex.Message}");
			return null;
		}
	}
}

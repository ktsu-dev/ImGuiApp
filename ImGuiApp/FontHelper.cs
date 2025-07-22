// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System;
using System.IO;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ktsu.ImGuiApp.ImGuiController;

/// <summary>
/// Helper class for configuring ImGui fonts with Unicode and emoji support.
/// Works with user-configured fonts rather than forcing system fonts.
/// </summary>
public static class FontHelper
{
	/// <summary>
	/// Enables extended Unicode support for the current ImGui font configuration.
	/// This works with whatever fonts the user has already configured.
	/// Note: The font must support the Unicode characters you want to display.
	/// </summary>
	/// <param name="io">The ImGui IO pointer.</param>
	/// <returns>True if Unicode support was enabled successfully.</returns>
	public static unsafe bool EnableUnicodeSupport(ImGuiIOPtr io)
	{
		try
		{
			// Check if fonts are already loaded
			if (io.Fonts.Fonts.Size == 0)
			{
				// No fonts loaded yet - ImGuiApp will handle this with extended Unicode ranges
				// when fonts are loaded through the normal configuration process
				return true;
			}

			// Fonts are already loaded, we can't modify glyph ranges after the fact
			// Log a warning that Unicode support should be enabled before font loading
			ImGuiApp.DebugLogger.Log("FontHelper.EnableUnicodeSupport: Fonts already loaded. Unicode support should be enabled during app configuration.");
			return false;
		}
		catch (Exception ex)
		{
			ImGuiApp.DebugLogger.Log($"FontHelper.EnableUnicodeSupport failed: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Gets the extended Unicode glyph ranges that ImGuiApp uses by default.
	/// This includes ASCII, Latin Extended, and common Unicode symbol blocks.
	/// </summary>
	/// <param name="fontAtlasPtr">The font atlas to use for building ranges.</param>
	/// <returns>Pointer to the extended Unicode glyph ranges.</returns>
	public static unsafe uint* GetExtendedUnicodeRanges(ImFontAtlasPtr fontAtlasPtr)
	{
		var builder = new ImFontGlyphRangesBuilderPtr(ImGui.ImFontGlyphRangesBuilder());
		
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
		
		// Build the ranges
		builder.BuildRanges(out ImVectorPtr ranges);
		return (uint*)ranges.Data;
	}

	/// <summary>
	/// Gets emoji glyph ranges for fonts that support emoji.
	/// </summary>
	/// <param name="fontAtlasPtr">The font atlas to use for building ranges.</param>
	/// <returns>Pointer to emoji glyph ranges.</returns>
	public static unsafe uint* GetEmojiRanges(ImFontAtlasPtr fontAtlasPtr)
	{
		var builder = new ImFontGlyphRangesBuilderPtr(ImGui.ImFontGlyphRangesBuilder());
		
		// Add basic emoticons (U+1F600-U+1F64F)
		builder.AddChar(0x1F600, 0x1F64F);
		
		// Add miscellaneous symbols and pictographs (U+1F300-U+1F5FF)
		builder.AddChar(0x1F300, 0x1F5FF);
		
		// Add transport and map symbols (U+1F680-U+1F6FF)
		builder.AddChar(0x1F680, 0x1F6FF);
		
		// Add supplemental symbols and pictographs (U+1F900-U+1F9FF)
		builder.AddChar(0x1F900, 0x1F9FF);
		
		// Add symbols and pictographs extended-A (U+1FA70-U+1FAFF)
		builder.AddChar(0x1FA70, 0x1FAFF);
		
		// Build the ranges
		builder.BuildRanges(out ImVectorPtr ranges);
		return (uint*)ranges.Data;
	}

	/// <summary>
	/// Adds a custom font with specific glyph ranges to the ImGui font atlas.
	/// This allows you to load fonts with exactly the character ranges you need.
	/// </summary>
	/// <param name="io">The ImGui IO pointer.</param>
	/// <param name="fontData">The font data as a byte array.</param>
	/// <param name="fontSize">The font size in pixels.</param>
	/// <param name="glyphRanges">The glyph ranges to include, or null for default ASCII.</param>
	/// <param name="mergeWithPrevious">Whether to merge this font with the previously added font.</param>
	/// <returns>The ImFont pointer for the added font, or null if failed.</returns>
	public static unsafe ImFontPtr? AddCustomFont(ImGuiIOPtr io, byte[] fontData, float fontSize, uint* glyphRanges = null, bool mergeWithPrevious = false)
	{
		try
		{
			// Pin the font data in memory
			var fontHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
			var fontPtr = fontHandle.AddrOfPinnedObject();

			// Create font configuration
			ImFontConfigPtr fontConfig = ImGui.ImFontConfig();
			fontConfig.FontDataOwnedByAtlas = false; // We manage the memory
			fontConfig.PixelSnapH = true;
			fontConfig.MergeMode = mergeWithPrevious;

			// Use provided glyph ranges or default to ASCII
			uint* ranges = glyphRanges ?? io.Fonts.GetGlyphRangesDefault();

			// Add font to atlas
			var font = io.Fonts.AddFontFromMemoryTTF((void*)fontPtr, fontData.Length, fontSize, fontConfig, ranges);
			
			// Store the handle for cleanup (you should implement proper cleanup in your app)
			// fontHandle should be freed when the application shuts down
			
			return font;
		}
		catch (Exception ex)
		{
			ImGuiApp.DebugLogger.Log($"FontHelper.AddCustomFont failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Creates a simple example showing how to test Unicode and emoji rendering.
	/// </summary>
	/// <param name="windowTitle">The title for the test window.</param>
	public static void ShowUnicodeTestWindow(string windowTitle = "Unicode & Emoji Test")
	{
		if (ImGui.Begin(windowTitle))
		{
			ImGui.Text("Basic ASCII: Hello World!");
			ImGui.Text("Accented characters: café, naïve, résumé");
			ImGui.Text("Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑ ∏ √ ∫");
			ImGui.Text("Currency symbols: $ € £ ¥ ₹ ₿");
			ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓");
			ImGui.Text("Geometric shapes: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆");
			ImGui.Text("Miscellaneous symbols: ♠ ♣ ♥ ♦ ☀ ☁ ☂ ☃ ♪ ♫");
			ImGui.Separator();
			ImGui.Text("Emojis (if font supports them):");
			ImGui.Text("Faces: 😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇");
			ImGui.Text("Objects: 🚀 💻 📱 🎸 🎨 🏆 🌟 💎 ⚡ 🔥");
			ImGui.Text("Nature: 🌈 🌞 🌙 ⭐ 🌍 🌊 🌳 🌸 🦋 🐝");
			ImGui.Text("Food: 🍎 🍌 🍕 🍔 🍟 🍦 🎂 ☕ 🍺 🍷");
			
			ImGui.Separator();
			ImGui.TextWrapped("Note: Character display depends on your configured font's Unicode support. " +
			                 "If characters show as question marks, your font may not include those glyphs.");
		}
		ImGui.End();
	}
}
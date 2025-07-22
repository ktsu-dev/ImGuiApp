// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.ImGuiController;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Represents the configuration for an ImGui font.
/// </summary>
public readonly struct ImGuiFontConfig : IEquatable<ImGuiFontConfig>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ImGuiFontConfig"/> struct.
	/// </summary>
	/// <param name="fontPath">The path to the font file.</param>
	/// <param name="fontSize">The size of the font.</param>
	/// <param name="getGlyphRange">A function to get the glyph range for the font.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="fontSize"/> is less than or equal to zero.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="fontPath"/> is null.</exception>
	public ImGuiFontConfig(string fontPath, int fontSize, Func<ImGuiIOPtr, IntPtr>? getGlyphRange = null)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fontSize);
		FontPath = fontPath ?? throw new ArgumentNullException(nameof(fontPath));
		FontSize = fontSize;
		GetGlyphRange = getGlyphRange;
	}

	/// <summary>
	/// Gets the path to the font file.
	/// </summary>
	public string FontPath { get; }

	/// <summary>
	/// Gets the size of the font.
	/// </summary>
	public int FontSize { get; }

	/// <summary>
	/// Gets the function to retrieve the glyph range for the font.
	/// </summary>
	public Func<ImGuiIOPtr, IntPtr>? GetGlyphRange { get; }

	/// <summary>
	/// Creates a font configuration with extended Unicode support including common symbols and accented characters.
	/// </summary>
	/// <param name="fontPath">The path to the font file.</param>
	/// <param name="fontSize">The size of the font.</param>
	/// <returns>A font configuration with extended Unicode glyph ranges.</returns>
	public static ImGuiFontConfig WithUnicodeSupport(string fontPath, int fontSize)
	{
		return new ImGuiFontConfig(fontPath, fontSize, io =>
		{
			var builder = new ImFontGlyphRangesBuilderPtr(ImGui.ImFontGlyphRangesBuilder());
			
			// Add default ranges (ASCII)
			builder.AddRanges(io.Fonts.GetGlyphRangesDefault());
			
			// Add Latin Extended-A (U+0100-U+017F) - accented characters
			builder.AddChar(0x0100, 0x017F);
			
			// Add Latin Extended-B (U+0180-U+024F) - more accented characters
			builder.AddChar(0x0180, 0x024F);
			
			// Add General Punctuation (U+2000-U+206F) - various punctuation
			builder.AddChar(0x2000, 0x206F);
			
			// Add Currency Symbols (U+20A0-U+20CF)
			builder.AddChar(0x20A0, 0x20CF);
			
			// Add Mathematical Operators (U+2200-U+22FF)
			builder.AddChar(0x2200, 0x22FF);
			
			// Add Geometric Shapes (U+25A0-U+25FF)
			builder.AddChar(0x25A0, 0x25FF);
			
			// Add Miscellaneous Symbols (U+2600-U+26FF)
			builder.AddChar(0x2600, 0x26FF);
			
			// Build the ranges
			builder.BuildRanges(out ImVectorPtr ranges);
			return (IntPtr)ranges.Data;
		});
	}

	/// <summary>
	/// Creates a font configuration with full Unicode support including CJK characters.
	/// </summary>
	/// <param name="fontPath">The path to the font file.</param>
	/// <param name="fontSize">The size of the font.</param>
	/// <returns>A font configuration with comprehensive Unicode glyph ranges.</returns>
	public static ImGuiFontConfig WithFullUnicodeSupport(string fontPath, int fontSize)
	{
		return new ImGuiFontConfig(fontPath, fontSize, io =>
		{
			var builder = new ImFontGlyphRangesBuilderPtr(ImGui.ImFontGlyphRangesBuilder());
			
			// Add default ranges
			builder.AddRanges(io.Fonts.GetGlyphRangesDefault());
			
			// Add Latin Extended
			builder.AddRanges(io.Fonts.GetGlyphRangesLatinExt());
			
			// Add Cyrillic
			builder.AddRanges(io.Fonts.GetGlyphRangesCyrillic());
			
			// Add CJK (Chinese, Japanese, Korean) - warning: this adds many glyphs
			// builder.AddRanges(io.Fonts.GetGlyphRangesChineseFull());
			// builder.AddRanges(io.Fonts.GetGlyphRangesJapanese());
			// builder.AddRanges(io.Fonts.GetGlyphRangesKorean());
			
			// Add common Unicode blocks
			builder.AddChar(0x2000, 0x206F); // General Punctuation
			builder.AddChar(0x20A0, 0x20CF); // Currency Symbols
			builder.AddChar(0x2100, 0x214F); // Letterlike Symbols
			builder.AddChar(0x2190, 0x21FF); // Arrows
			builder.AddChar(0x2200, 0x22FF); // Mathematical Operators
			builder.AddChar(0x2300, 0x23FF); // Miscellaneous Technical
			builder.AddChar(0x2400, 0x243F); // Control Pictures
			builder.AddChar(0x2500, 0x257F); // Box Drawing
			builder.AddChar(0x2580, 0x259F); // Block Elements
			builder.AddChar(0x25A0, 0x25FF); // Geometric Shapes
			builder.AddChar(0x2600, 0x26FF); // Miscellaneous Symbols
			
			// Build the ranges
			builder.BuildRanges(out ImVectorPtr ranges);
			return (IntPtr)ranges.Data;
		});
	}

	/// <summary>
	/// Creates a font configuration specifically for emoji support.
	/// Note: Requires a font that supports emoji (like Noto Color Emoji).
	/// </summary>
	/// <param name="fontPath">The path to the emoji font file.</param>
	/// <param name="fontSize">The size of the font.</param>
	/// <returns>A font configuration with emoji glyph ranges.</returns>
	public static ImGuiFontConfig WithEmojiSupport(string fontPath, int fontSize)
	{
		return new ImGuiFontConfig(fontPath, fontSize, io =>
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
			return (IntPtr)ranges.Data;
		});
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object? obj) => obj is ImGuiFontConfig config && Equals(config);

	/// <summary>
	/// Serves as the default hash function.
	/// </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode() => HashCode.Combine(FontPath, FontSize, GetGlyphRange);

	/// <summary>
	/// Determines whether two specified instances of <see cref="ImGuiFontConfig"/> are equal.
	/// </summary>
	/// <param name="left">The first <see cref="ImGuiFontConfig"/> to compare.</param>
	/// <param name="right">The second <see cref="ImGuiFontConfig"/> to compare.</param>
	/// <returns>true if the two <see cref="ImGuiFontConfig"/> instances are equal; otherwise, false.</returns>
	public static bool operator ==(ImGuiFontConfig left, ImGuiFontConfig right) => left.Equals(right);

	/// <summary>
	/// Determines whether two specified instances of <see cref="ImGuiFontConfig"/> are not equal.
	/// </summary>
	/// <param name="left">The first <see cref="ImGuiFontConfig"/> to compare.</param>
	/// <param name="right">The second <see cref="ImGuiFontConfig"/> to compare.</param>
	/// <returns>true if the two <see cref="ImGuiFontConfig"/> instances are not equal; otherwise, false.</returns>
	public static bool operator !=(ImGuiFontConfig left, ImGuiFontConfig right) => !(left == right);

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
	public bool Equals(ImGuiFontConfig other)
	{
		return FontPath == other.FontPath
		&& FontSize == other.FontSize
		&& GetGlyphRange == other.GetGlyphRange;
	}
}

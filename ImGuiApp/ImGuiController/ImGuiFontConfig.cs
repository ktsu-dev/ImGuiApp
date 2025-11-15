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

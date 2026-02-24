// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Class responsible for applying fonts in ImGui.
/// </summary>
public class FontAppearance : ScopedAction
{
	/// <summary>
	/// The default font name.
	/// </summary>
	public const string DefaultFontName = "default";

	/// <summary>
	/// The default font point size.
	/// </summary>
	public const int DefaultFontPointSize = 14;

	/// <summary>
	/// Gets or sets the font resolver used to find fonts by name and size.
	/// When set, this resolver is used to look up fonts. When null, falls back to using the current ImGui font.
	/// The function takes a font name and size in points, and returns the font pointer and size in pixels.
	/// </summary>
	public static Func<string, int, (ImFontPtr Font, float SizePixels)>? FontResolver { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FontAppearance"/> class.
	/// Applies the specified font appearance to ImGui.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	/// <param name="sizePoints">The size of the font in points.</param>
	/// <param name="sizePixels">The size of the font in pixels.</param>
	public FontAppearance(string name, int sizePoints, out float sizePixels)
	{
		ImFontPtr font;
		if (FontResolver is not null)
		{
			(font, sizePixels) = FontResolver(name, sizePoints);
		}
		else
		{
			font = ImGui.GetFont();
			sizePixels = sizePoints;
		}

		ImGui.PushFont(font, sizePixels);
		OnClose = ImGui.PopFont;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FontAppearance"/> class.
	/// Applies the specified font appearance to ImGui.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	/// <param name="sizePoints">The size of the font in points.</param>
	public FontAppearance(string name, int sizePoints) : this(name, sizePoints, out _)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FontAppearance"/> class.
	/// Applies the default font appearance to ImGui with the specified size in points.
	/// </summary>
	/// <param name="sizePoints">The size of the font in points.</param>
	public FontAppearance(int sizePoints) : this(DefaultFontName, sizePoints, out _)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FontAppearance"/> class.
	/// Applies the specified font appearance to ImGui with the default font size.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	public FontAppearance(string name) : this(name, DefaultFontPointSize, out _)
	{
	}
}

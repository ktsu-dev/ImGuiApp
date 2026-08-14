// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaTextHelper = Hexa.NET.ImGui.Widgets.TextHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws text centred vertically within the current line.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredV(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredV(text);
	}

	/// <summary>
	/// Draws text centred horizontally within the available content region.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredH(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredH(text);
	}

	/// <summary>
	/// Draws text centred both vertically and horizontally within the available content region.
	/// </summary>
	/// <param name="text">The text to draw.</param>
	/// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
	public static void TextCenteredVH(string text)
	{
		Ensure.NotNull(text);
		HexaTextHelper.TextCenteredVH(text);
	}
}

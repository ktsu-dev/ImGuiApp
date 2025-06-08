// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Class responsible for scaling UI elements in ImGui.
/// </summary>
public class UIScaler : ScopedAction
{
	private const float ScaleChangeThreshold = 0.1f;

	internal static float ScaleFactor { get; private set; } = 1;

	internal static int[] SupportedPixelFontSizes { get; } = [12, 13, 14, 16, 18, 20, 24, 28, 32, 40, 48];

	/// <summary>
	/// Maps font names to a dictionary of pixel sizes and their corresponding font indices.
	/// Each font name maps to a dictionary where the key is the pixel size and the value is the font index.
	/// </summary>
	internal static Dictionary<string, Dictionary<int, int>> FontIndices { get; } = [];

	/// <summary>
	/// Registers a font in the FontIndices dictionary.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	/// <param name="pixelSize">The pixel size of the font.</param>
	/// <param name="index">The index of the font in the ImGui font collection.</param>
	internal static void RegisterFont(string name, int pixelSize, int index)
	{
		if (!FontIndices.TryGetValue(name, out var sizes))
		{
			sizes = [];
			FontIndices[name] = sizes;
		}

		sizes[pixelSize] = index;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UIScaler"/> class.
	/// Scales various ImGui style variables by the specified scale factor.
	/// </summary>
	/// <param name="scale">The scale factor to apply to the UI elements.</param>
	public UIScaler(float scale)
	{
		var style = ImGui.GetStyle();
		var numStyles = 0;
		PushStyleAndCount(ImGuiStyleVar.CellPadding, style.CellPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildBorderSize, style.ChildBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildRounding, style.ChildRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.DockingSeparatorSize, style.DockingSeparatorSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameBorderSize, style.FrameBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FramePadding, style.FramePadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameRounding, style.FrameRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabMinSize, style.GrabMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabRounding, style.GrabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.IndentSpacing, style.IndentSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemInnerSpacing, style.ItemInnerSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemSpacing, style.ItemSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupBorderSize, style.PopupBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupRounding, style.PopupRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarRounding, style.ScrollbarRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarSize, style.ScrollbarSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextBorderSize, style.SeparatorTextBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextPadding, style.SeparatorTextPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.TabRounding, style.TabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowBorderSize, style.WindowBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowMinSize, style.WindowMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowPadding, style.WindowPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowRounding, style.WindowRounding * scale, ref numStyles);

		OnClose = () => ImGuiApp.Invoker.Invoke(() => ImGui.PopStyleVar(numStyles));
	}

	/// <summary>
	/// Pushes a style variable and increments the style count.
	/// </summary>
	/// <param name="style">The style variable to push.</param>
	/// <param name="value">The value to set for the style variable.</param>
	/// <param name="numStyles">The reference to the style count.</param>
	private static void PushStyleAndCount(ImGuiStyleVar style, float value, ref int numStyles)
	{
		ImGuiApp.Invoker.Invoke(() => ImGui.PushStyleVar(style, value));
		++numStyles;
	}

	/// <summary>
	/// Pushes a style variable and increments the style count.
	/// </summary>
	/// <param name="style">The style variable to push.</param>
	/// <param name="value">The value to set for the style variable.</param>
	/// <param name="numStyles">The reference to the style count.</param>
	private static void PushStyleAndCount(ImGuiStyleVar style, Vector2 value, ref int numStyles)
	{
		ImGuiApp.Invoker.Invoke(() => ImGui.PushStyleVar(style, value));
		++numStyles;
	}

	internal static void Render(Action renderAction)
	{
		FindBestFontForAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out var bestFontSize);
		var scaleRatio = bestFontSize / (float)FontAppearance.DefaultFontPointSize;
		using (new UIScaler(scaleRatio))
		{
			RenderWithDefaultFont(renderAction);
		}
	}

	private static void RenderWithDefaultFont(Action action)
	{
		using (new FontAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize))
		{
			action();
		}
	}

	internal static ImFontPtr FindBestFontForAppearance(string name, int sizePoints, out int sizePixels)
	{
		var io = ImGui.GetIO();
		var fonts = io.Fonts.Fonts;
		sizePixels = PtsToPx(sizePoints);
		var sizePixelsLocal = sizePixels;

		var candidatesByFace = FontIndices
			.Where(f => f.Key == name)
			.SelectMany(f => f.Value)
			.OrderBy(f => f.Key)
			.ToArray();

		if (candidatesByFace.Length == 0)
		{
			throw new InvalidOperationException($"No fonts found for the specified font appearance: {name} {sizePoints}pt");
		}

		int[] candidatesBySize = [.. candidatesByFace
			.Where(x => x.Key >= sizePixelsLocal)
			.Select(x => x.Value)];

		if (candidatesBySize.Length != 0)
		{
			var bestFontIndex = candidatesBySize.First();
			return fonts[bestFontIndex];
		}

		// if there was no font size larger than our requested size, then fall back to the largest font size we have
		var largestFontIndex = candidatesByFace.Last().Value;
		return fonts[largestFontIndex];
	}

	/// <summary>
	/// Converts a value in ems to pixels based on the current ImGui font size.
	/// </summary>
	/// <param name="ems">The value in ems to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int EmsToPx(float ems) => ImGuiApp.Invoker.Invoke(() => (int)(ems * ImGui.GetFontSize()));

	/// <summary>
	/// Converts a value in points to pixels based on the current scale factor.
	/// </summary>
	/// <param name="pts">The value in points to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int PtsToPx(int pts) => (int)(pts * ScaleFactor);

	internal static void UpdateDpiScale()
	{
		var newScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

		if (Math.Abs(ScaleFactor - newScaleFactor) > ScaleChangeThreshold)
		{
			ScaleFactor = newScaleFactor;
		}
	}

	internal static void Init() => ForceDpiAware.Windows();
}

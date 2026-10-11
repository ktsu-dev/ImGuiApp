// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

/// <summary>How much larger than designed the interface is drawn, for widgets that measure in screen pixels.</summary>
/// <remarks>
/// A handle's size or a grab radius written as a fixed number of pixels stays that size when the text
/// around it grows with DPI or <c>ImGuiApp.SetGlobalScale</c>, so it ends up too small to see or to hit.
/// Those widgets multiply their pixel lengths by <see cref="Current"/>, read from the font, which carries
/// both.
/// </remarks>
internal static class InterfaceScale
{
	/// <summary>The font size, in pixels, the widgets' pixel lengths were designed against: ImGuiApp's default.</summary>
	internal const float ReferenceFontSize = 14f;

	/// <summary>Gets the current font size over the reference, or one outside a frame.</summary>
	internal static float Current
	{
		get
		{
			float fontSize = ImGui.GetFontSize();
			return float.IsFinite(fontSize) && fontSize > 0f ? fontSize / ReferenceFontSize : 1f;
		}
	}
}

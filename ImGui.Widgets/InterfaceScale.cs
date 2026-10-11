// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

/// <summary>How much larger than designed the interface is drawn, for anything that measures in screen pixels.</summary>
/// <remarks>
/// A handle's size or a grab radius written as a fixed number of pixels stays that size when the text
/// around it grows with DPI or <c>ImGuiApp.SetGlobalScale</c>, so it ends up too small to see or to hit.
/// Those widgets multiply their pixel lengths by <see cref="Current"/>, read from the font, which carries
/// both. Public so an application drawing its own handles scales them by the same factor.
/// </remarks>
public static class InterfaceScale
{
	/// <summary>The font size, in pixels, the widgets' pixel lengths were designed against: ImGuiApp's default.</summary>
	public const float ReferenceFontSize = 14f;

	/// <summary>Gets the current font size over the reference, or one when there is no ImGui context or no font yet.</summary>
	public static float Current
	{
		get
		{
			if (ImGui.GetCurrentContext().IsNull)
			{
				return 1f;
			}

			float fontSize = ImGui.GetFontSize();
			return float.IsFinite(fontSize) && fontSize > 0f ? fontSize / ReferenceFontSize : 1f;
		}
	}

	/// <summary>Scales a length given in pixels of the reference design to the interface's current size.</summary>
	/// <param name="pixels">The length at 100% on a 96 DPI display.</param>
	/// <returns>The length on screen now.</returns>
	public static float Pixels(float pixels) => pixels * Current;
}

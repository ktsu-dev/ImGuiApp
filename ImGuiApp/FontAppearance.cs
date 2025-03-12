namespace ktsu.ImGuiApp;

using ImGuiNET;

using ktsu.ScopedAction;

/// <summary>
/// Class responsible for applying fonts in ImGui.
/// </summary>
public class FontAppearance : ScopedAction
{
	/// <summary>
	/// The default font name.
	/// </summary>
	internal const string DefaultFontName = "default";

	/// <summary>
	/// The default font point size.
	/// </summary>
	internal const int DefaultFontPointSize = 13;

	/// <summary>
	/// Initializes a new instance of the <see cref="FontAppearance"/> class.
	/// Applies the specified font appearance to ImGui.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	/// <param name="sizePoints">The size of the font in points.</param>
	/// <param name="sizePixels">The size of the font in pixels.</param>
	public FontAppearance(string name, int sizePoints, out int sizePixels)
	{
		var font = ImGuiApp.FindBestFontForAppearance(name, sizePoints, out sizePixels);
		ImGui.PushFont(font);

		OnClose = () => ImGuiApp.InvokeOnWindowThread(() => ImGui.PopFont());
	}
}

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
		if (!ImGui.GetIO().Fonts.IsBuilt())
		{
			throw new InvalidOperationException("Fonts have not been built yet.");
		}

		var font = ImGuiApp.FindBestFontForAppearance(name, sizePoints, out sizePixels);
		ImGui.PushFont(font);

		OnClose = () => ImGuiApp.Invoker.Invoke(() => ImGui.PopFont());
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

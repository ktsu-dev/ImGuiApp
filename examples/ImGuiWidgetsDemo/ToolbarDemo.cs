// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows a single-line toolbar and a two-line one with labels under their glyphs.</summary>
internal static class ToolbarDemo
{
	// Material Icons code points; the demo merges that font in OnConfigureFonts.
	private const string OpenGlyph = "";
	private const string SaveGlyph = "";
	private const string UndoGlyph = "";
	private const string RedoGlyph = "";
	private const string BoldGlyph = "";
	private const string ItalicGlyph = "";
	private const string SettingsGlyph = "";
	private const string PlayGlyph = "";
	private const string StopGlyph = "";
	private const string LoopGlyph = "";

	private static bool bold;
	private static bool italic;
	private static bool loop;

	/// <summary>Gets the name of the last button clicked, or an empty string.</summary>
	internal static string LastAction { get; private set; } = string.Empty;

	/// <summary>Gets a value indicating whether the Bold toggle is on.</summary>
	internal static bool Bold => bold;

	/// <summary>Returns this section's state to its starting values.</summary>
	internal static void ResetState()
	{
		bold = false;
		italic = false;
		loop = false;
		LastAction = string.Empty;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Toolbar"))
		{
			return;
		}

		ImGui.TextUnformatted("Glyph leading (the default)");
		using (ImGuiWidgets.Toolbar("##editToolbar"))
		{
			Record("Open", ImGuiWidgets.ToolbarButton("Open", OpenGlyph, new ToolbarButtonOptions { Shortcut = "Ctrl+O" }));
			Record("Save", ImGuiWidgets.ToolbarButton("Save", SaveGlyph, new ToolbarButtonOptions { Shortcut = "Ctrl+S" }));
			ImGuiWidgets.ToolbarSeparator();
			Record("Undo", ImGuiWidgets.ToolbarButton("Undo", UndoGlyph, new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly, Shortcut = "Ctrl+Z" }));
			Record("Redo", ImGuiWidgets.ToolbarButton("Redo", RedoGlyph, new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly, Enabled = false }));
			ImGuiWidgets.ToolbarSeparator();
			ImGuiWidgets.ToolbarToggleButton("Bold", BoldGlyph, ref bold, new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly });
			ImGuiWidgets.ToolbarToggleButton("Italic", ItalicGlyph, ref italic, new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphOnly });
			ImGuiWidgets.ToolbarSeparator();
			Record("Settings", ImGuiWidgets.ToolbarButton("Settings", SettingsGlyph, new ToolbarButtonOptions { Flat = true }));
		}

		ImGui.TextUnformatted("Glyph above, label centred beneath");
		using (ImGuiWidgets.Toolbar("##transportToolbar", new ToolbarOptions { Layout = ToolbarButtonLayout.GlyphAbove }))
		{
			Record("Play", ImGuiWidgets.ToolbarButton("Play", PlayGlyph, new ToolbarButtonOptions { MinWidth = 56 }));
			Record("Stop", ImGuiWidgets.ToolbarButton("Stop", StopGlyph, new ToolbarButtonOptions { MinWidth = 56 }));
			ImGuiWidgets.ToolbarSeparator();
			ImGuiWidgets.ToolbarToggleButton("Loop", LoopGlyph, ref loop, new ToolbarButtonOptions { MinWidth = 56 });
		}

		ImGui.TextUnformatted($"Last action: {(LastAction.Length > 0 ? LastAction : "none")}");
	}

	private static void Record(string name, bool clicked)
	{
		if (clicked)
		{
			LastAction = name;
		}
	}
}

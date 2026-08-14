// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using HexaFileTreeView = Hexa.NET.ImGui.Widgets.ImGuiFileTreeView;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a navigable tree of the filesystem rooted at the machine's drives.
	/// </summary>
	/// <remarks>
	/// Requires a Material Icons font in the atlas — the control draws the Material <c>Home</c>
	/// glyph at U+E9B2 and <c>Computer</c> at U+E31E, and renders placeholder boxes without one.
	/// See <c>FontHelper.AddMaterialIconRanges</c> in ktsu.ImGui.App.
	/// </remarks>
	/// <param name="id">Unique identifier for the tree view.</param>
	/// <param name="size">Size of the tree view region, in pixels.</param>
	/// <param name="currentFolder">The selected folder, updated in place when a new folder is chosen.</param>
	/// <param name="homeFolder">The folder the home shortcut navigates to.</param>
	/// <returns><see langword="true"/> if the selected folder changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/>, <paramref name="currentFolder"/>, or <paramref name="homeFolder"/> is <see langword="null"/>.</exception>
	public static bool FileTreeView(string id, Vector2 size, ref AbsoluteDirectoryPath currentFolder, AbsoluteDirectoryPath homeFolder)
	{
		Ensure.NotNull(id);
		Ensure.NotNull(currentFolder);
		Ensure.NotNull(homeFolder);

		string current = currentFolder.ToString();
		bool changed = HexaFileTreeView.FileTreeView(id, size, ref current, homeFolder.ToString());
		if (changed)
		{
			currentFolder = current.As<AbsoluteDirectoryPath>();
		}

		return changed;
	}
}

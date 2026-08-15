// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaBreadcrumb = Hexa.NET.ImGui.Widgets.ImGuiBreadcrumb;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a clickable breadcrumb trail from a separator-delimited path.
	/// </summary>
	/// <remarks>
	/// The path is tokenized on both forward and back slashes for display only and is not
	/// required to exist on disk, so this accepts a plain string rather than a semantic path type.
	/// </remarks>
	/// <param name="id">Unique identifier for the breadcrumb.</param>
	/// <param name="path">The path to display, truncated in place to the clicked segment.</param>
	/// <returns><see langword="true"/> if a segment was clicked and the path changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
	public static bool Breadcrumb(string id, ref string path)
	{
		Ensure.NotNull(id);
		Ensure.NotNull(path);
		return HexaBreadcrumb.Breadcrumb(id, ref path);
	}
}

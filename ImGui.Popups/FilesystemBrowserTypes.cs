// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System.Numerics;
using ktsu.Semantics.Paths;

/// <summary>
/// Partial class containing various ImGui popup implementations.
/// </summary>
public partial class ImGuiPopups
{
	/// <summary>How the filesystem browser lays out a folder's contents.</summary>
	public enum FilesystemBrowserView
	{
		/// <summary>A table of names, sizes and dates, one entry per row.</summary>
		List,

		/// <summary>A grid of tiles, each with a thumbnail above its name.</summary>
		Thumbnails,
	}

	/// <summary>A picture of a file, supplied by the host to the filesystem browser.</summary>
	/// <param name="TextureId">The texture to draw, as an ImGui texture id.</param>
	/// <param name="Size">The picture's size in pixels, so it is drawn at its own aspect ratio.</param>
	public readonly record struct FilesystemBrowserThumbnail(nint TextureId, Vector2 Size);

	/// <summary>A folder the filesystem browser offers in its sidebar.</summary>
	/// <param name="Name">The name shown.</param>
	/// <param name="Directory">The folder clicking it goes to.</param>
	public sealed record FilesystemBrowserPlace(string Name, AbsoluteDirectoryPath Directory);
}

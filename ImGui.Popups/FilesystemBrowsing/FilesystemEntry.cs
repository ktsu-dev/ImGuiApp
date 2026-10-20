// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using ktsu.Semantics.Paths;

/// <summary>One row of a folder listing: a file or folder with what the browser shows about it.</summary>
/// <param name="Path">The entry's path, an <see cref="AbsoluteDirectoryPath"/> or an <see cref="AbsoluteFilePath"/>.</param>
/// <param name="Name">The entry's own name, without its folder.</param>
/// <param name="Size">The file's length in bytes, or <see langword="null"/> for a folder.</param>
/// <param name="Modified">When the entry last changed, in local time.</param>
/// <param name="IsHidden">Whether the system treats the entry as hidden.</param>
internal sealed record FilesystemEntry(IAbsolutePath Path, string Name, long? Size, DateTime Modified, bool IsHidden)
{
	/// <summary>Gets a value indicating whether the entry is a folder.</summary>
	public bool IsDirectory => Path is AbsoluteDirectoryPath;

	/// <summary>Gets the name a row shows and is probed by: a folder carries a trailing separator.</summary>
	public string DisplayName => IsDirectory ? Name + System.IO.Path.DirectorySeparatorChar : Name;
}

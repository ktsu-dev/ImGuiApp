// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>The user's own folders a file browser offers in its sidebar.</summary>
internal static class FilesystemPlaces
{
	/// <summary>Gets home, desktop, documents, pictures and downloads, each once and only where it exists.</summary>
	/// <returns>The places, in that order.</returns>
	/// <remarks>
	/// On Linux the documents folder .NET reports is home itself when no XDG documents folder is set,
	/// so places are kept once per folder rather than once per name.
	/// </remarks>
	public static Collection<ImGuiPopups.FilesystemBrowserPlace> Standard()
	{
		string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		(string Name, string Path)[] candidates =
		[
			("Home", home),
			("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),
			("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),
			("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)),
			("Downloads", string.IsNullOrEmpty(home) ? string.Empty : System.IO.Path.Join(home, "Downloads")),
		];

		Collection<ImGuiPopups.FilesystemBrowserPlace> places = [];
		HashSet<string> seen = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		foreach ((string name, string path) in candidates)
		{
			if (!string.IsNullOrEmpty(path) && System.IO.Path.IsPathRooted(path) && Directory.Exists(path) && seen.Add(System.IO.Path.TrimEndingDirectorySeparator(path)))
			{
				places.Add(new(name, path.As<AbsoluteDirectoryPath>()));
			}
		}

		return places;
	}
}

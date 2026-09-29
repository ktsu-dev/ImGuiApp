// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.IO;

/// <summary>
/// A small, made-up home folder for the file browsing widgets to show, so the gallery pictures a
/// tidy folder rather than whichever directory the tool happened to run in.
/// </summary>
internal static class SampleFolder
{
	private static readonly string[] Folders = ["Documents", "Music", "Pictures", "Projects", "Projects/widgets", "Projects/website"];

	private static readonly string[] Files = ["budget.xlsx", "holiday.jpg", "notes.txt", "Projects/README.md", "Documents/report.pdf"];

	private static readonly DateTime Timestamp = new(2026, 9, 29, 9, 0, 0, DateTimeKind.Local);

	/// <summary>
	/// A fixed name, so a rerun on the same machine draws the same pixels and regenerated images only
	/// show up in a diff when a widget actually changed.
	/// </summary>
	private static readonly string Parent = Path.GetFullPath(Path.Join(Path.GetTempPath(), "ktsu-widget-gallery"));

	private static readonly string RootPath = Path.Join(Parent, "home");

	/// <summary>Gets the folder's path, creating it whenever it does not exist, including after <see cref="Delete"/>.</summary>
	public static string Root
	{
		get
		{
			if (!Directory.Exists(RootPath))
			{
				Create();
			}

			return RootPath;
		}
	}

	/// <summary>Gets the path of a file inside the folder.</summary>
	/// <param name="relative">The file's path relative to the folder.</param>
	/// <returns>The absolute path.</returns>
	public static string PathOf(string relative) => Path.GetFullPath(Path.Join(Root, relative));

	/// <summary>Deletes the folder and the uniquely named directory it was created in.</summary>
	public static void Delete()
	{
		if (Directory.Exists(Parent))
		{
			Directory.Delete(Parent, recursive: true);
		}
	}

	private static void Create()
	{
		// Anything a crashed run left behind goes first, so the listing is exactly what is below.
		Delete();
		string root = RootPath;

		foreach (string folder in Folders)
		{
			Directory.CreateDirectory(Path.Join(root, folder));
		}

		foreach (string file in Files)
		{
			File.WriteAllText(Path.Join(root, file), "sample");
		}

		foreach (string entry in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
		{
			File.SetLastWriteTime(entry, Timestamp);
		}
	}
}

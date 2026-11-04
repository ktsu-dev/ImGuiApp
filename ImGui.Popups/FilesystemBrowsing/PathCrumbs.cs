// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System.Collections.ObjectModel;
using System.IO;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>One step of a breadcrumb path: the name shown and the folder it goes to.</summary>
/// <param name="Label">The folder's name, or the root as the system writes it.</param>
/// <param name="Directory">The folder clicking the step goes to.</param>
internal sealed record PathCrumb(string Label, AbsoluteDirectoryPath Directory);

/// <summary>Splits a folder into the steps of a breadcrumb path, root first.</summary>
internal static class PathCrumbs
{
	/// <summary>Gets the steps from the root down to <paramref name="directory"/>.</summary>
	/// <param name="directory">The folder being shown.</param>
	/// <returns>The root, then each folder below it, ending with <paramref name="directory"/> itself.</returns>
	public static Collection<PathCrumb> For(AbsoluteDirectoryPath directory)
	{
		Collection<PathCrumb> crumbs = [];
		string? current = Path.TrimEndingDirectorySeparator(directory.WeakString);

		while (!string.IsNullOrEmpty(current))
		{
			string? parent = Path.GetDirectoryName(current);
			string label = parent is null ? current : Path.GetFileName(current);
			crumbs.Insert(0, new(label, current.As<AbsoluteDirectoryPath>()));
			current = parent;
		}

		return crumbs;
	}
}

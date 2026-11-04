// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>The column a folder listing is sorted by.</summary>
internal enum FilesystemSortColumn
{
	/// <summary>By name, ignoring case.</summary>
	Name,

	/// <summary>By file length. Folders have none, so they keep name order.</summary>
	Size,

	/// <summary>By when the entry last changed.</summary>
	Modified,
}

/// <summary>
/// Reads, filters and orders what a file browser shows of one folder. No ImGui here, so every rule
/// the browser applies to a listing is a unit test.
/// </summary>
internal static class FilesystemListing
{
	private static readonly string[] SizeUnits = ["bytes", "KB", "MB", "GB", "TB"];

	/// <summary>Reads a folder's folders and the files a browser should offer from it.</summary>
	/// <param name="directory">The folder to read.</param>
	/// <param name="includeFiles">Whether files are listed at all; a folder chooser lists only folders.</param>
	/// <param name="glob">The file pattern, or several separated by semicolons.</param>
	/// <param name="showHidden">Whether hidden entries are listed.</param>
	/// <param name="error">Why the folder could not be read, or <see langword="null"/> when it could.</param>
	/// <returns>The entries, in no particular order.</returns>
	public static Collection<FilesystemEntry> Read(AbsoluteDirectoryPath directory, bool includeFiles, string glob, bool showHidden, out string? error)
	{
		Collection<FilesystemEntry> entries = [];
		error = null;

		try
		{
			DirectoryInfo folder = new(directory.WeakString);
			foreach (FileSystemInfo info in folder.EnumerateFileSystemInfos())
			{
				bool isHidden = IsHidden(info);
				if (isHidden && !showHidden)
				{
					continue;
				}

				if (info is DirectoryInfo)
				{
					entries.Add(new(info.FullName.As<AbsoluteDirectoryPath>(), info.Name, null, ModifiedOf(info), isHidden));
				}
				else if (includeFiles && info is FileInfo file && ImGuiPopups.FilesystemBrowser.MatchesGlob(file.Name, glob))
				{
					entries.Add(new(file.FullName.As<AbsoluteFilePath>(), file.Name, LengthOf(file), ModifiedOf(file), isHidden));
				}
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ArgumentException)
		{
			error = ex.Message;
		}

		return entries;
	}

	/// <summary>Determines whether the system treats an entry as hidden.</summary>
	/// <param name="info">The entry.</param>
	/// <returns><see langword="true"/> for an entry with the hidden attribute, or a dot-named one outside Windows.</returns>
	/// <remarks>Outside Windows a name starting with a dot is hidden by convention, which .NET reports as the attribute too, but only on some versions; both are checked.</remarks>
	internal static bool IsHidden(FileSystemInfo info)
	{
		if (!OperatingSystem.IsWindows() && info.Name.StartsWith('.'))
		{
			return true;
		}

		try
		{
			return (info.Attributes & FileAttributes.Hidden) != 0;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	private static long? LengthOf(FileInfo file)
	{
		try
		{
			return file.Length;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	private static DateTime ModifiedOf(FileSystemInfo info)
	{
		try
		{
			return info.LastWriteTime;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
		{
			return DateTime.MinValue;
		}
	}

	/// <summary>
	/// Orders entries for display: folders always come first, each group in the order asked for, with
	/// name breaking ties so equal sizes or times never shuffle between frames.
	/// </summary>
	/// <param name="entries">The entries to order.</param>
	/// <param name="column">The column to sort by.</param>
	/// <param name="ascending">Whether the order ascends.</param>
	/// <returns>The ordered entries.</returns>
	public static Collection<FilesystemEntry> Sort(IEnumerable<FilesystemEntry> entries, FilesystemSortColumn column, bool ascending)
	{
		IOrderedEnumerable<FilesystemEntry> folderFirst = entries.OrderBy(e => !e.IsDirectory);
		IOrderedEnumerable<FilesystemEntry> sorted = column switch
		{
			FilesystemSortColumn.Size => ascending ? folderFirst.ThenBy(e => e.Size ?? 0) : folderFirst.ThenByDescending(e => e.Size ?? 0),
			FilesystemSortColumn.Modified => ascending ? folderFirst.ThenBy(e => e.Modified) : folderFirst.ThenByDescending(e => e.Modified),
			_ => ascending ? folderFirst.ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase) : folderFirst.ThenByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
		};

		return [.. sorted.ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Name, StringComparer.Ordinal)];
	}

	/// <summary>Keeps the entries whose name contains the search text, ignoring case.</summary>
	/// <param name="entries">The entries to filter.</param>
	/// <param name="search">The text typed in the search box; empty keeps everything.</param>
	/// <returns>The entries that match.</returns>
	public static IEnumerable<FilesystemEntry> Filter(IEnumerable<FilesystemEntry> entries, string search)
	{
		string term = search.Trim();
		return term.Length == 0 ? entries : entries.Where(e => e.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>Writes a file length the way a file manager shows it: whole bytes, then one decimal place.</summary>
	/// <param name="bytes">The length in bytes.</param>
	/// <returns>The length as text, for example <c>12 bytes</c> or <c>4.2 MB</c>.</returns>
	public static string FormatSize(long bytes)
	{
		if (bytes < 1024)
		{
			return bytes == 1 ? "1 byte" : bytes.ToString("N0", CultureInfo.CurrentCulture) + " bytes";
		}

		double value = bytes;
		int unit = 0;
		while (value >= 1024 && unit < SizeUnits.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return value.ToString(value < 10 ? "0.0" : "0", CultureInfo.CurrentCulture) + " " + SizeUnits[unit];
	}

	/// <summary>Describes what a listing holds, for the line under it.</summary>
	/// <param name="entries">The entries shown.</param>
	/// <returns>For example <c>2 folders, 1 file</c>.</returns>
	public static string Summarize(IEnumerable<FilesystemEntry> entries)
	{
		int folders = 0;
		int files = 0;
		foreach (FilesystemEntry entry in entries)
		{
			if (entry.IsDirectory)
			{
				folders++;
			}
			else
			{
				files++;
			}
		}

		string folderText = folders == 1 ? "1 folder" : folders.ToString(CultureInfo.CurrentCulture) + " folders";
		string fileText = files == 1 ? "1 file" : files.ToString(CultureInfo.CurrentCulture) + " files";
		return folderText + ", " + fileText;
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups.Tests;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The browser's rules that need no ImGui: history, breadcrumbs, listing, sorting, sizes and typed paths.</summary>
[TestClass]
public sealed class FilesystemBrowserHelperTests
{
	private static readonly string Root = Path.Join(Path.GetTempPath(), "FilesystemBrowserHelperTests");

	private static AbsoluteDirectoryPath Folder(string name) => Path.Join(Root, name).As<AbsoluteDirectoryPath>();

	private static FilesystemEntry File(string name, long size, int day) =>
		new(Path.Join(Root, name).As<AbsoluteFilePath>(), name, size, new DateTime(2026, 10, day, 0, 0, 0, DateTimeKind.Local), false);

	private static FilesystemEntry Directory(string name, int day) =>
		new(Folder(name), name, null, new DateTime(2026, 10, day, 0, 0, 0, DateTimeKind.Local), false);

	[TestMethod]
	public void History_BackThenForward_ReturnsWhereItWas()
	{
		FilesystemBrowserHistory history = new();
		history.Visit(Folder("a"));

		Assert.IsTrue(history.TryGoBack(Folder("b"), out AbsoluteDirectoryPath back));
		Assert.AreEqual(Folder("a"), back);
		Assert.IsTrue(history.CanGoForward);

		Assert.IsTrue(history.TryGoForward(back, out AbsoluteDirectoryPath forward));
		Assert.AreEqual(Folder("b"), forward);
		Assert.IsFalse(history.CanGoForward);
	}

	[TestMethod]
	public void History_GoingSomewhereNew_ForgetsWhatWasAhead()
	{
		FilesystemBrowserHistory history = new();
		history.Visit(Folder("a"));
		history.TryGoBack(Folder("b"), out AbsoluteDirectoryPath back);

		history.Visit(back);

		Assert.IsFalse(history.CanGoForward);
		Assert.IsFalse(history.TryGoForward(Folder("c"), out AbsoluteDirectoryPath stay));
		Assert.AreEqual(Folder("c"), stay);
	}

	[TestMethod]
	public void History_WithNothingBehind_StaysPut()
	{
		FilesystemBrowserHistory history = new();

		Assert.IsFalse(history.CanGoBack);
		Assert.IsFalse(history.TryGoBack(Folder("a"), out AbsoluteDirectoryPath stay));
		Assert.AreEqual(Folder("a"), stay);
	}

	[TestMethod]
	public void Crumbs_RunFromTheRootDownToTheFolder()
	{
		AbsoluteDirectoryPath folder = Path.Join(Root, "one", "two").As<AbsoluteDirectoryPath>();

		Collection<PathCrumb> crumbs = PathCrumbs.For(folder);

		Assert.AreEqual(Path.GetPathRoot(Root), crumbs[0].Label);
		Assert.AreEqual("two", crumbs[^1].Label);
		Assert.AreEqual("one", crumbs[^2].Label);
		Assert.AreEqual(folder.WeakString, crumbs[^1].Directory.WeakString);
		Assert.AreEqual(Path.Join(Root, "one"), crumbs[^2].Directory.WeakString);
	}

	[TestMethod]
	public void Crumbs_ForARoot_AreTheRootAlone()
	{
		string root = Path.GetPathRoot(Root)!;

		Collection<PathCrumb> crumbs = PathCrumbs.For(root.As<AbsoluteDirectoryPath>());

		Assert.AreEqual(1, crumbs.Count);
		Assert.AreEqual(root, crumbs[0].Label);
	}

	[TestMethod]
	public void Sort_KeepsFoldersFirst_WhicheverColumnAndDirection()
	{
		List<FilesystemEntry> entries = [File("big.txt", 900, 1), Directory("zeta", 9), File("small.txt", 10, 5), Directory("alpha", 2)];

		foreach (FilesystemSortColumn column in Enum.GetValues<FilesystemSortColumn>())
		{
			foreach (bool ascending in new[] { true, false })
			{
				Collection<FilesystemEntry> sorted = FilesystemListing.Sort(entries, column, ascending);
				Assert.IsTrue(sorted[0].IsDirectory && sorted[1].IsDirectory, $"Folders were not first sorting by {column}, ascending {ascending}.");
			}
		}
	}

	[TestMethod]
	public void Sort_BySizeDescending_PutsTheLargestFileFirst()
	{
		List<FilesystemEntry> entries = [File("small.txt", 10, 1), File("big.txt", 900, 1), File("middle.txt", 200, 1)];

		List<string> names = [.. FilesystemListing.Sort(entries, FilesystemSortColumn.Size, ascending: false).Select(e => e.Name)];

		CollectionAssert.AreEqual(new List<string> { "big.txt", "middle.txt", "small.txt" }, names);
	}

	[TestMethod]
	public void Sort_ByModified_OrdersByDate_AndNameBreaksTies()
	{
		List<FilesystemEntry> entries = [File("b.txt", 1, 3), File("late.txt", 1, 9), File("a.txt", 1, 3)];

		List<string> names = [.. FilesystemListing.Sort(entries, FilesystemSortColumn.Modified, ascending: true).Select(e => e.Name)];

		CollectionAssert.AreEqual(new List<string> { "a.txt", "b.txt", "late.txt" }, names);
	}

	[TestMethod]
	public void Filter_MatchesAnywhereInTheName_IgnoringCase()
	{
		List<FilesystemEntry> entries = [File("Holiday.PNG", 1, 1), File("notes.txt", 1, 1), Directory("png exports", 1)];

		List<string> names = [.. FilesystemListing.Filter(entries, " png ").Select(e => e.Name)];

		CollectionAssert.AreEquivalent(new List<string> { "Holiday.PNG", "png exports" }, names);
	}

	[TestMethod]
	public void FormatSize_UsesBytesUnderAKilobyte_ThenOneDecimalPlaceUnderTen()
	{
		Assert.AreEqual("1 byte", FilesystemListing.FormatSize(1));
		Assert.AreEqual(512.ToString("N0", System.Globalization.CultureInfo.CurrentCulture) + " bytes", FilesystemListing.FormatSize(512));
		Assert.AreEqual(1.5.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " KB", FilesystemListing.FormatSize(1536));
		Assert.AreEqual("20 MB", FilesystemListing.FormatSize(20L * 1024 * 1024));
	}

	[TestMethod]
	public void Summarize_CountsFoldersAndFiles()
	{
		Assert.AreEqual("1 folder, 2 files", FilesystemListing.Summarize([Directory("a", 1), File("b", 1, 1), File("c", 1, 1)]));
		Assert.AreEqual("0 folders, 1 file", FilesystemListing.Summarize([File("b", 1, 1)]));
	}

	[TestMethod]
	public void Read_ListsFoldersAndMatchingFiles_AndHidesHiddenOnesUnlessAsked()
	{
		string folder = Path.Join(Path.GetTempPath(), $"listing-{Guid.NewGuid():N}");
		System.IO.Directory.CreateDirectory(Path.Join(folder, "sub"));
		System.IO.File.WriteAllText(Path.Join(folder, "keep.png"), "x");
		System.IO.File.WriteAllText(Path.Join(folder, "skip.txt"), "x");
		string hidden = Path.Join(folder, ".hidden.png");
		System.IO.File.WriteAllText(hidden, "x");
		System.IO.File.SetAttributes(hidden, FileAttributes.Hidden);

		try
		{
			Collection<FilesystemEntry> shown = FilesystemListing.Read(folder.As<AbsoluteDirectoryPath>(), includeFiles: true, "*.png", showHidden: false, out string? error);
			Collection<FilesystemEntry> all = FilesystemListing.Read(folder.As<AbsoluteDirectoryPath>(), includeFiles: true, "*.png", showHidden: true, out _);
			Collection<FilesystemEntry> folders = FilesystemListing.Read(folder.As<AbsoluteDirectoryPath>(), includeFiles: false, "*", showHidden: true, out _);

			Assert.IsNull(error);
			CollectionAssert.AreEquivalent(new List<string> { "sub", "keep.png" }, shown.Select(e => e.Name).ToList());
			CollectionAssert.AreEquivalent(new List<string> { "sub", "keep.png", ".hidden.png" }, all.Select(e => e.Name).ToList());
			CollectionAssert.AreEquivalent(new List<string> { "sub" }, folders.Select(e => e.Name).ToList());
			Assert.AreEqual(1L, shown.Single(e => e.Name == "keep.png").Size);
		}
		finally
		{
			System.IO.File.SetAttributes(hidden, FileAttributes.Normal);
			System.IO.Directory.Delete(folder, recursive: true);
		}
	}

	[TestMethod]
	public void Read_AFolderThatIsNotThere_ReportsWhyRatherThanThrowing()
	{
		AbsoluteDirectoryPath missing = Path.Join(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}").As<AbsoluteDirectoryPath>();

		Collection<FilesystemEntry> entries = FilesystemListing.Read(missing, includeFiles: true, "*", showHidden: false, out string? error);

		Assert.AreEqual(0, entries.Count);
		Assert.IsNotNull(error);
	}

	[TestMethod]
	public void ExpandHome_ReplacesALeadingTildeOnly()
	{
		string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		Assert.AreEqual(home, ImGuiPopups.FilesystemBrowser.ExpandHome("~"));
		Assert.AreEqual(home + "/Pictures", ImGuiPopups.FilesystemBrowser.ExpandHome("~/Pictures"));
		Assert.AreEqual("a~b", ImGuiPopups.FilesystemBrowser.ExpandHome("a~b"));
		Assert.AreEqual("~user", ImGuiPopups.FilesystemBrowser.ExpandHome("~user"));
	}

	[TestMethod]
	public void StandardPlaces_ExistAndAreEachListedOnce()
	{
		Collection<ImGuiPopups.FilesystemBrowserPlace> places = FilesystemPlaces.Standard();

		Assert.IsTrue(places.All(place => System.IO.Directory.Exists(place.Directory.WeakString)));
		Assert.AreEqual(places.Count, places.Select(place => Path.TrimEndingDirectorySeparator(place.Directory.WeakString)).Distinct().Count());
	}
}

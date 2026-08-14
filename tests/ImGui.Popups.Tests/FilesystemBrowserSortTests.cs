// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups.Tests;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FilesystemBrowserSortTests
{
	private static readonly string Root = Path.Combine(Path.GetTempPath(), "FilesystemBrowserSortTests");

	private static AbsoluteDirectoryPath MakeDirectory(string name) => Path.Combine(Root, name).As<AbsoluteDirectoryPath>();

	private static AbsoluteFilePath MakeFile(string name) => Path.Combine(Root, name).As<AbsoluteFilePath>();

	/// <summary>
	/// Regression test for https://github.com/ktsu-dev/ImGuiApp/issues/273 — sorting a mixed
	/// collection of paths used to throw because <see cref="IAbsolutePath"/> has no generic
	/// comparer and the semantic string fallback only accepts <see cref="string"/> arguments.
	/// </summary>
	[TestMethod]
	public void SortContents_MixedDirectoriesAndFiles_DoesNotThrow()
	{
		Collection<IAbsolutePath> contents =
		[
			MakeFile("b.txt"),
			MakeDirectory("zebra"),
			MakeFile("a.txt"),
			MakeDirectory("alpha"),
		];

		Collection<IAbsolutePath> sorted = ImGuiPopups.FilesystemBrowser.SortContents(contents);

		Assert.AreEqual(contents.Count, sorted.Count);
	}

	[TestMethod]
	public void SortContents_ListsDirectoriesBeforeFiles()
	{
		Collection<IAbsolutePath> contents =
		[
			MakeFile("a.txt"),
			MakeDirectory("zebra"),
			MakeFile("b.txt"),
			MakeDirectory("alpha"),
		];

		List<string> sorted = [.. ImGuiPopups.FilesystemBrowser.SortContents(contents).Select(p => Path.GetFileName(p.WeakString))];

		CollectionAssert.AreEqual(new List<string> { "alpha", "zebra", "a.txt", "b.txt" }, sorted);
	}

	[TestMethod]
	public void SortContents_SortsByNameIgnoringCase()
	{
		Collection<IAbsolutePath> contents =
		[
			MakeFile("Zebra.txt"),
			MakeFile("apple.txt"),
			MakeFile("Banana.txt"),
		];

		List<string> sorted = [.. ImGuiPopups.FilesystemBrowser.SortContents(contents).Select(p => Path.GetFileName(p.WeakString))];

		CollectionAssert.AreEqual(new List<string> { "apple.txt", "Banana.txt", "Zebra.txt" }, sorted);
	}

	[TestMethod]
	public void SortContents_EntriesDifferingOnlyByCase_OrderIsDeterministic()
	{
		Collection<IAbsolutePath> contents =
		[
			MakeFile("b.txt"),
			MakeFile("B.txt"),
		];

		Collection<IAbsolutePath> reversed =
		[
			MakeFile("B.txt"),
			MakeFile("b.txt"),
		];

		List<string> sorted = [.. ImGuiPopups.FilesystemBrowser.SortContents(contents).Select(p => p.WeakString)];
		List<string> sortedReversed = [.. ImGuiPopups.FilesystemBrowser.SortContents(reversed).Select(p => p.WeakString)];

		CollectionAssert.AreEqual(sorted, sortedReversed);
	}

	[TestMethod]
	public void SortContents_EmptyCollection_ReturnsEmpty()
	{
		Collection<IAbsolutePath> contents = [];

		Assert.AreEqual(0, ImGuiPopups.FilesystemBrowser.SortContents(contents).Count);
	}
}

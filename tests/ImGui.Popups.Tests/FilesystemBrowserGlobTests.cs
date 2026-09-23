// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FilesystemBrowserGlobTests
{
	/// <summary>
	/// A semicolon-separated glob is the natural way for a caller to ask for several extensions,
	/// but the glob library underneath has no separator of its own and treats a whole string as
	/// one pattern, so passing the combined string through as-is matched nothing and the browser
	/// listed no files at all.
	/// </summary>
	[TestMethod]
	public void MatchesGlob_SemicolonSeparatedPatterns_MatchesEveryExtension()
	{
		const string glob = "*.png;*.jpg;*.jpeg;*.webp";

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", glob));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.jpg", glob));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.jpeg", glob));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.webp", glob));
	}

	[TestMethod]
	public void MatchesGlob_SemicolonSeparatedPatterns_RejectsOtherExtensions()
	{
		const string glob = "*.png;*.jpg";

		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("notes.txt", glob));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("archive.zip", glob));
	}

	[TestMethod]
	public void MatchesGlob_SinglePattern_BehavesAsBefore()
	{
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", "*.png"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.jpg", "*.png"));
	}

	[TestMethod]
	public void MatchesGlob_DefaultGlob_MatchesEverything()
	{
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", "*"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("notes.txt", "*"));
	}

	[TestMethod]
	public void MatchesGlob_PatternsWithSurroundingWhitespace_AreTrimmed()
	{
		const string glob = "*.png; *.jpg ; *.webp";

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.jpg", glob));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.webp", glob));
	}

	[TestMethod]
	public void MatchesGlob_EmptyEntries_AreIgnored()
	{
		const string glob = ";;*.png;;";

		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", glob));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("notes.txt", glob));
	}

	/// <summary>
	/// A glob with nothing in it selects nothing, rather than everything. TextFilter treats an
	/// empty filter as "match anything", so the guard here is that the semicolon split yields no
	/// patterns and the name is never handed to TextFilter at all.
	/// </summary>
	[TestMethod]
	public void MatchesGlob_EmptyGlob_MatchesNothing()
	{
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", string.Empty));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", ";  ;"));
	}

	/// <summary>
	/// File dialogs have always matched case insensitively, and a camera writes IMG_1234.JPG.
	/// The glob library underneath is case sensitive by default, so dropping the explicit
	/// <c>CaseInsensitive</c> argument would stop the open dialog listing photographs.
	/// </summary>
	[TestMethod]
	public void MatchesGlob_IgnoresCaseOnBothSides()
	{
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("IMG_1234.JPG", "*.jpg"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("PHOTO.PNG", "*.png"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("photo.png", "*.PNG"));
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("notes.txt", "*.jpg"));
	}

	/// <summary>
	/// Each pattern is matched against the whole name. TextFilter's default ByWordAny would split
	/// the name on spaces as well as the filter, so a name whose first word happened to match
	/// would be listed on that word alone.
	/// </summary>
	[TestMethod]
	public void MatchesGlob_NameIsMatchedWhole_NotWordByWord()
	{
		Assert.IsFalse(ImGuiPopups.FilesystemBrowser.MatchesGlob("a.jpg b.txt", "*.jpg"));
		Assert.IsTrue(ImGuiPopups.FilesystemBrowser.MatchesGlob("my holiday photo.jpg", "*.jpg"));
	}
}

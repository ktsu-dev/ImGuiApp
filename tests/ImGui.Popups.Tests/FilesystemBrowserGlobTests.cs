// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups.Tests;

using Microsoft.Extensions.FileSystemGlobbing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class FilesystemBrowserGlobTests
{
	/// <summary>
	/// A semicolon-separated glob is the natural way for a caller to ask for several extensions,
	/// but <see cref="Matcher"/> has no separator of its own and treats a whole string as one
	/// pattern, so passing the combined string to a single AddInclude matched nothing and the
	/// browser listed no files at all.
	/// </summary>
	[TestMethod]
	public void BuildMatcher_SemicolonSeparatedPatterns_MatchesEveryExtension()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher("*.png;*.jpg;*.jpeg;*.webp");

		Assert.IsTrue(matcher.Match("photo.png").HasMatches);
		Assert.IsTrue(matcher.Match("photo.jpg").HasMatches);
		Assert.IsTrue(matcher.Match("photo.jpeg").HasMatches);
		Assert.IsTrue(matcher.Match("photo.webp").HasMatches);
	}

	[TestMethod]
	public void BuildMatcher_SemicolonSeparatedPatterns_RejectsOtherExtensions()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher("*.png;*.jpg");

		Assert.IsFalse(matcher.Match("notes.txt").HasMatches);
		Assert.IsFalse(matcher.Match("archive.zip").HasMatches);
	}

	[TestMethod]
	public void BuildMatcher_SinglePattern_BehavesAsBefore()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher("*.png");

		Assert.IsTrue(matcher.Match("photo.png").HasMatches);
		Assert.IsFalse(matcher.Match("photo.jpg").HasMatches);
	}

	[TestMethod]
	public void BuildMatcher_DefaultGlob_MatchesEverything()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher("*");

		Assert.IsTrue(matcher.Match("photo.png").HasMatches);
		Assert.IsTrue(matcher.Match("notes.txt").HasMatches);
	}

	[TestMethod]
	public void BuildMatcher_PatternsWithSurroundingWhitespace_AreTrimmed()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher("*.png; *.jpg ; *.webp");

		Assert.IsTrue(matcher.Match("photo.jpg").HasMatches);
		Assert.IsTrue(matcher.Match("photo.webp").HasMatches);
	}

	[TestMethod]
	public void BuildMatcher_EmptyEntries_AreIgnored()
	{
		Matcher matcher = ImGuiPopups.FilesystemBrowser.BuildMatcher(";;*.png;;");

		Assert.IsTrue(matcher.Match("photo.png").HasMatches);
		Assert.IsFalse(matcher.Match("notes.txt").HasMatches);
	}
}

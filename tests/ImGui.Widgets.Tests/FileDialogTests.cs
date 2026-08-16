// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.IO;

using ktsu.Semantics.Paths;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the path conversion at the file dialog boundary. The dialogs themselves need a live
/// ImGui context and are verified visually in ImGuiWidgetsDemo.
/// </summary>
[TestClass]
public sealed class FileDialogTests
{
	/// <summary>
	/// Normalizes a path for comparison, so a difference in trailing separator or separator
	/// direction does not fail a test that is about the path's content.
	/// </summary>
	/// <param name="value">The path to normalize.</param>
	/// <returns>The normalized path.</returns>
	private static string Normalize(string value) =>
		Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

	[TestMethod]
	public void TryParseFilePath_Null_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath(null));

	[TestMethod]
	public void TryParseFilePath_Empty_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath(string.Empty));

	[TestMethod]
	public void TryParseFilePath_Whitespace_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath("   "));

	[TestMethod]
	public void TryParseFilePath_AbsolutePath_RoundTrips()
	{
		string input = Path.Combine(AppContext.BaseDirectory, "example.txt");

		AbsoluteFilePath? parsed = ImGuiWidgets.TryParseFilePath(input);

		Assert.IsNotNull(parsed);
		Assert.AreEqual(Normalize(input), Normalize(parsed.ToString()));
	}

	/// <summary>
	/// Hexa's save dialog writes the text box straight into its private field, so a bare filename
	/// reaches this conversion verbatim. It must degrade to null: throwing here unwinds out of
	/// Hexa's draw loop and leaves its dialog manager permanently wedged.
	/// </summary>
	[TestMethod]
	public void TryParseFilePath_BareRelativeFilename_ReturnsNullWithoutThrowing() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath("example.txt"));

	[TestMethod]
	public void TryParseFilePath_RelativePath_ReturnsNullWithoutThrowing() =>
		Assert.IsNull(ImGuiWidgets.TryParseFilePath(Path.Combine("sub", "example.txt")));

	[TestMethod]
	public void TryParseDirectoryPath_Null_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseDirectoryPath(null));

	[TestMethod]
	public void TryParseDirectoryPath_BaseDirectory_RoundTrips()
	{
		string input = AppContext.BaseDirectory;

		AbsoluteDirectoryPath? parsed = ImGuiWidgets.TryParseDirectoryPath(input);

		Assert.IsNotNull(parsed);
		Assert.AreEqual(Normalize(input), Normalize(parsed.ToString()));
	}

	[TestMethod]
	public void TryParseDirectoryPath_RelativePath_ReturnsNullWithoutThrowing() =>
		Assert.IsNull(ImGuiWidgets.TryParseDirectoryPath("sub"));

	[TestMethod]
	public void ResolveSaveTarget_BareFilename_JoinsToCurrentFolder()
	{
		AbsoluteFilePath? resolved = ImGuiWidgets.ResolveSaveTarget("example.txt", AppContext.BaseDirectory);

		Assert.IsNotNull(resolved);
		Assert.AreEqual(
			Normalize(Path.Combine(AppContext.BaseDirectory, "example.txt")),
			Normalize(resolved.ToString()));
	}

	[TestMethod]
	public void ResolveSaveTarget_AbsoluteFilename_IsUsedAsIs()
	{
		string input = Path.Combine(AppContext.BaseDirectory, "example.txt");

		AbsoluteFilePath? resolved = ImGuiWidgets.ResolveSaveTarget(input, Path.GetTempPath());

		Assert.IsNotNull(resolved);
		Assert.AreEqual(Normalize(input), Normalize(resolved.ToString()));
	}

	[TestMethod]
	public void ResolveSaveTarget_NoSelection_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.ResolveSaveTarget("   ", AppContext.BaseDirectory));

	[TestMethod]
	public void ResolveSaveTarget_RelativeNameWithoutFolder_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.ResolveSaveTarget("example.txt", null));

	[TestMethod]
	public void ResolveSaveTarget_RelativeNameWithRelativeFolder_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.ResolveSaveTarget("example.txt", "sub"));

	[TestMethod]
	public void FileDialogOutcome_DefaultSelection_IsEmptyNotNull()
	{
		FileDialogOutcome outcome = new(DialogOutcome.Cancel, null, []);

		Assert.IsNotNull(outcome.Selection);
		Assert.AreEqual(0, outcome.Selection.Count);
	}
}

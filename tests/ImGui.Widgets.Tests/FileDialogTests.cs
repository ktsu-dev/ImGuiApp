// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the path conversion at the file dialog boundary. The dialogs themselves need a live
/// ImGui context and are verified visually in ImGuiWidgetsDemo.
/// </summary>
[TestClass]
public sealed class FileDialogTests
{
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
		string input = System.IO.Path.Combine(System.AppContext.BaseDirectory, "example.txt");

		Assert.IsNotNull(ImGuiWidgets.TryParseFilePath(input));
	}

	[TestMethod]
	public void TryParseDirectoryPath_Null_ReturnsNull() =>
		Assert.IsNull(ImGuiWidgets.TryParseDirectoryPath(null));

	[TestMethod]
	public void TryParseDirectoryPath_BaseDirectory_RoundTrips() =>
		Assert.IsNotNull(ImGuiWidgets.TryParseDirectoryPath(System.AppContext.BaseDirectory));

	[TestMethod]
	public void FileDialogOutcome_DefaultSelection_IsEmptyNotNull()
	{
		FileDialogOutcome outcome = new(DialogOutcome.Cancel, null, []);

		Assert.IsNotNull(outcome.Selection);
		Assert.AreEqual(0, outcome.Selection.Count);
	}
}

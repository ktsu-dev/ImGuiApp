// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.IO;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.RenameDialog"/> on its own.</summary>
/// <remarks>
/// The dialog moves the file itself unless told not to, so each test works on a file it creates in
/// a temporary directory of its own and deletes afterwards.
/// </remarks>
[TestClass]
public sealed class RenameDialogTests : WidgetTest
{
	private readonly List<RenameOutcome> outcomes = [];

	private string root = string.Empty;
	private AbsoluteFilePath source = new();
	private bool shown;
	private ImGuiWidgets.RenameDialog dialog = null!;

	[TestInitialize]
	public void CreateFile()
	{
		root = Path.Combine(Path.GetTempPath(), "rename-uitests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);

		string path = Path.Combine(root, "notes.txt");
		File.WriteAllText(path, "hello");
		source = path.As<AbsoluteFilePath>();

		outcomes.Clear();
		shown = false;
	}

	[TestCleanup]
	public void DeleteFile()
	{
		DismissOpenDialogs();

		if (root.Length > 0 && Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	private void Draw()
	{
		ImGuiWidgets.DrawDeferred();

		if (!shown)
		{
			shown = true;
			dialog.Show(outcomes.Add);
		}
	}

	private void StartDialog(bool skipAutomaticMove = true)
	{
		dialog = new ImGuiWidgets.RenameDialog(source) { SkipAutomaticMove = skipAutomaticMove };
		Start(Draw);
		Step(2);
	}

	[TestMethod]
	public void RenameDialog_AppearsOnceThePumpRuns()
	{
		dialog = new ImGuiWidgets.RenameDialog(source);
		Start(ImGuiWidgets.DrawDeferred);
		byte[] empty = Snapshot();

		dialog.Show(outcomes.Add);
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(empty), "The rename dialog drew nothing after the pump ran.");
		Assert.IsTrue(FindDialogButtons().Count >= 1, "The rename dialog offered no buttons.");
	}

	[TestMethod]
	public void RenameDialog_CancelReportsCancelAndMovesNothing()
	{
		StartDialog();

		// Cancel is the leftmost button in this dialog and Ok the one beside it -- the opposite
		// order from a message box.
		Assert.AreEqual(2, FindDialogButtons().Count, "The rename dialog did not offer two buttons.");
		ClickDialogButton(0);
		Step(2);

		Assert.AreEqual(1, outcomes.Count, "Cancelling reported no outcome.");
		Assert.AreEqual(DialogOutcome.Cancel, outcomes[0].Outcome, $"Cancelling reported {outcomes[0].Outcome}.");
		Assert.IsTrue(File.Exists(source.ToString()), "Cancelling the dialog moved the file anyway.");
	}

	[TestMethod]
	public void RenameDialog_SkipAutomaticMove_LeavesTheFileWhereItIs()
	{
		StartDialog(skipAutomaticMove: true);

		Assert.IsTrue(dialog.SkipAutomaticMove, "The dialog did not keep the setting it was given.");

		// Ok, not Cancel: the point is that committing the dialog still leaves the file alone.
		ClickDialogButton(1);
		Step(2);

		Assert.IsTrue(File.Exists(source.ToString()), "A dialog told not to move the file moved it anyway.");
	}

	[TestMethod]
	public void RenameDialog_OverwriteSettingRoundTrips()
	{
		dialog = new ImGuiWidgets.RenameDialog(source) { Overwrite = true };

		Assert.IsTrue(dialog.Overwrite, "The overwrite setting was not stored.");

		dialog.Overwrite = false;

		Assert.IsFalse(dialog.Overwrite, "The overwrite setting could not be cleared.");
	}

	[TestMethod]
	public void RenameDialog_ShownTwice_IsRefused()
	{
		StartDialog();

		Assert.ThrowsExactly<InvalidOperationException>(
			() => dialog.Show(outcomes.Add),
			"Showing an already-open rename dialog was allowed.");
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.IO;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.OpenFileDialog"/>, <see cref="ImGuiWidgets.SaveFileDialog"/> and
/// <see cref="ImGuiWidgets.OpenFolderDialog"/> on their own.
/// </summary>
/// <remarks>
/// These browse the real filesystem, so each test creates a directory of its own to point them at.
/// They also draw Material Icons glyphs for their toolbar and file tree and fall back to
/// placeholder boxes without that font, which is what a harness with no font registered gets.
/// </remarks>
[TestClass]
public sealed class FileDialogTests : WidgetTest
{
	private readonly List<FileDialogOutcome> fileOutcomes = [];
	private readonly List<FolderDialogOutcome> folderOutcomes = [];

	// The pickers are large windows: at the harness default they run off the bottom of the screen
	// and their action row is never drawn.
	private static readonly HarnessOptions PickerViewport = new() { Width = 1280, Height = 900 };

	// Where Hexa puts the picker window in that viewport: 1000x600 at (60, 60).
	private const float PickerWindowRight = 1060f;
	private const float PickerWindowBottom = 660f;

	private string root = string.Empty;

	[TestInitialize]
	public void CreateTree()
	{
		root = Path.Combine(Path.GetTempPath(), "filedialog-uitests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(root, "nested"));
		File.WriteAllText(Path.Combine(root, "document.txt"), "hello");

		fileOutcomes.Clear();
		folderOutcomes.Clear();
	}

	[TestCleanup]
	public void DeleteTree()
	{
		// A picker left open is drawn again over the next test by the next harness's pump, because
		// Hexa's dialog manager is process-static. Cancelling twice covers a test that left one
		// open and one that already closed its own.
		if (IsRunning)
		{
			CancelPicker();
			CancelPicker();
		}

		if (root.Length > 0 && Directory.Exists(root))
		{
			Directory.Delete(root, recursive: true);
		}
	}

	/// <summary>
	/// Clicks the Cancel button in the action row along the bottom of an open picker.
	/// </summary>
	/// <remarks>
	/// The pickers are vendor windows that mark nothing, and their buttons do not stand out from
	/// the surrounding chrome by color, so the only way to reach one is by position. Hexa opens
	/// every picker as the same 1000x600 window at the same place, with the action row along its
	/// bottom edge -- Cancel, then the confirm button -- and the harness pins the viewport, so
	/// these coordinates are fixed rather than a guess about the layout. Harmless when no picker
	/// is open: the click lands on the empty application window.
	/// </remarks>
	private void CancelPicker()
	{
		Harness.Mouse.Click(PickerWindowRight - 240f, PickerWindowBottom - 20f);
		Step(3);
	}

	[TestMethod]
	public void OpenFileDialog_AppearsOnceThePumpRuns()
	{
		ImGuiWidgets.OpenFileDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		byte[] empty = Snapshot();

		dialog.Show(fileOutcomes.Add);
		Step(3);

		Assert.IsNotNull(BoundsOfDifference(empty), "The open dialog drew nothing after the pump ran.");

		CancelPicker();
	}

	[TestMethod]
	public void OpenFileDialog_CancelReportsCancelAndNoFile()
	{
		ImGuiWidgets.OpenFileDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		byte[] empty = Snapshot();
		dialog.Show(fileOutcomes.Add);
		Step(3);

		CancelPicker();

		Assert.AreEqual(1, fileOutcomes.Count, "Cancelling the dialog reported no outcome.");
		Assert.AreEqual(DialogOutcome.Cancel, fileOutcomes[0].Outcome, $"Cancelling reported {fileOutcomes[0].Outcome}.");
		Assert.AreEqual(0, fileOutcomes[0].Selection.Count, "A cancelled dialog still reported files.");
		Assert.IsNull(fileOutcomes[0].Path, "A cancelled dialog still reported a path.");
	}

	[TestMethod]
	public void OpenFileDialog_ShownTwice_IsRefused()
	{
		ImGuiWidgets.OpenFileDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		dialog.Show(fileOutcomes.Add);
		Step(3);

		Assert.ThrowsExactly<InvalidOperationException>(
			() => dialog.Show(fileOutcomes.Add),
			"Showing an already-open file dialog was allowed.");

		CancelPicker();
	}

	[TestMethod]
	public void OpenFileDialog_MultipleSelectionSettingRoundTrips()
	{
		ImGuiWidgets.OpenFileDialog dialog = new() { AllowMultipleSelection = true };

		Assert.IsTrue(dialog.AllowMultipleSelection, "The multiple-selection setting was not stored.");

		dialog.AllowMultipleSelection = false;

		Assert.IsFalse(dialog.AllowMultipleSelection, "The multiple-selection setting could not be cleared.");
	}

	[TestMethod]
	public void SaveFileDialog_CancelReportsCancel()
	{
		ImGuiWidgets.SaveFileDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		byte[] empty = Snapshot();
		dialog.Show(fileOutcomes.Add);
		Step(3);

		CancelPicker();

		Assert.AreEqual(1, fileOutcomes.Count, "Cancelling the dialog reported no outcome.");
		Assert.AreEqual(DialogOutcome.Cancel, fileOutcomes[0].Outcome, $"Cancelling reported {fileOutcomes[0].Outcome}.");
	}

	[TestMethod]
	public void OpenFolderDialog_CancelReportsCancel()
	{
		ImGuiWidgets.OpenFolderDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		byte[] empty = Snapshot();
		dialog.Show(folderOutcomes.Add);
		Step(3);

		CancelPicker();

		Assert.AreEqual(1, folderOutcomes.Count, "Cancelling the dialog reported no outcome.");
		Assert.AreEqual(DialogOutcome.Cancel, folderOutcomes[0].Outcome, $"Cancelling reported {folderOutcomes[0].Outcome}.");

		// The folder it was browsing is reported even on Cancel: the wrapper passes Hexa's
		// SelectedFolder straight through without consulting the result, so Path is populated
		// here despite documenting itself as null when nothing was chosen. Callers have to check
		// Outcome rather than testing Path for null.
		Assert.IsNotNull(folderOutcomes[0].Path, "The recorded behavior changed: Path is now null on Cancel.");
	}

	[TestMethod]
	public void OpenFolderDialog_AppearsOnceThePumpRuns()
	{
		ImGuiWidgets.OpenFolderDialog dialog = new();

		Start(ImGuiWidgets.DrawDeferred, PickerViewport);
		byte[] empty = Snapshot();

		dialog.Show(folderOutcomes.Add);
		Step(3);

		Assert.IsNotNull(BoundsOfDifference(empty), "The folder dialog drew nothing after the pump ran.");

		CancelPicker();
	}
}

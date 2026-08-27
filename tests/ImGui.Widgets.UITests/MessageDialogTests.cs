// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.ShowMessageBox"/> and
/// <see cref="ImGuiWidgets.DialogMessageBox"/> on their own.
/// </summary>
/// <remarks>
/// Both are drawn by the deferred pump rather than by the call that shows them, so every test here
/// runs <see cref="ImGuiWidgets.DrawDeferred"/> from its render callback and shows the dialog on
/// the first frame.
/// </remarks>
[TestClass]
public sealed class MessageDialogTests : WidgetTest
{
	private readonly List<DialogOutcome> outcomes = [];
	private Action? showOnFirstFrame;
	private bool shown;

	// Hexa's dialog managers are static and outlive the harness, so a box left unanswered would be
	// drawn again over the next test. Every test here answers what it opens, and this is the net
	// under that.
	[TestCleanup]
	public void DismissLeftovers() => DismissOpenDialogs();

	private void Draw()
	{
		ImGuiWidgets.DrawDeferred();

		if (!shown && showOnFirstFrame is not null)
		{
			shown = true;
			showOnFirstFrame();
		}
	}

	private void StartWith(Action show)
	{
		outcomes.Clear();
		shown = false;
		showOnFirstFrame = show;
		Start(Draw);
		Step(2);
	}

	[TestMethod]
	public void ShowMessageBox_AppearsOnceThePumpRuns()
	{
		StartWith(() => { });
		byte[] empty = Snapshot();

		ImGuiWidgets.ShowMessageBox("Confirm", "Save your changes?", MessageBoxButtons.Ok, outcomes.Add);
		Step(2);

		Assert.IsNotNull(BoundsOfDifference(empty), "The message box drew nothing after the pump ran.");
	}

	[TestMethod]
	public void ShowMessageBox_OkButtonReportsOk()
	{
		StartWith(() => ImGuiWidgets.ShowMessageBox("Confirm", "All done.", MessageBoxButtons.Ok, outcomes.Add));

		ClickDialogButton(0);

		CollectionAssert.AreEqual(new[] { DialogOutcome.Ok }, outcomes, $"The box reported [{string.Join(", ", outcomes)}].");
	}

	[TestMethod]
	public void ShowMessageBox_YesNo_ReportsWhicheverWasPressed()
	{
		StartWith(() => ImGuiWidgets.ShowMessageBox("Confirm", "Save your changes?", MessageBoxButtons.YesNo, outcomes.Add));

		Assert.AreEqual(2, FindDialogButtons().Count, "A yes/no box did not offer two buttons.");

		ClickDialogButton(1);

		CollectionAssert.AreEqual(new[] { DialogOutcome.No }, outcomes, $"Pressing the second button reported [{string.Join(", ", outcomes)}].");
	}

	[TestMethod]
	public void ShowMessageBox_ClosesAfterAChoice()
	{
		StartWith(() => { });
		byte[] empty = Snapshot();

		ImGuiWidgets.ShowMessageBox("Confirm", "All done.", MessageBoxButtons.Ok, outcomes.Add);
		Step(2);
		ClickDialogButton(0);
		Step(2);

		Assert.IsNull(BoundsOfDifference(empty), "The message box stayed on screen after being answered.");
	}

	[TestMethod]
	public void ShowMessageBox_ReportsItsAnswerExactlyOnce()
	{
		StartWith(() => ImGuiWidgets.ShowMessageBox("Confirm", "All done.", MessageBoxButtons.Ok, outcomes.Add));

		ClickDialogButton(0);
		Step(10);

		Assert.AreEqual(1, outcomes.Count, $"The box reported {outcomes.Count} answers to one press.");
	}

	[TestMethod]
	public void DialogMessageBox_AppearsAndReportsItsAnswer()
	{
		ImGuiWidgets.DialogMessageBox box = new("Quit", "Discard unsaved work?", MessageBoxButtons.YesNo);

		StartWith(() => box.Show(outcomes.Add));

		ClickDialogButton(0);

		CollectionAssert.AreEqual(new[] { DialogOutcome.Yes }, outcomes, $"The dialog reported [{string.Join(", ", outcomes)}].");
	}

	[TestMethod]
	public void DialogMessageBox_ShownTwice_IsRefused()
	{
		// Hexa's Show registers the instance unconditionally, so a second call leaves a duplicate
		// entry that is never drawn, never closed and never removed -- which latches its input
		// block on for the life of the process. The guard is what stops that.
		ImGuiWidgets.DialogMessageBox box = new("Quit", "Discard unsaved work?", MessageBoxButtons.Ok);

		StartWith(() => box.Show(outcomes.Add));

		Assert.ThrowsExactly<InvalidOperationException>(
			() => box.Show(outcomes.Add),
			"Showing an already-open dialog was allowed.");
	}

	[TestMethod]
	public void DialogMessageBox_MayBeShownAgainAfterItCloses()
	{
		ImGuiWidgets.DialogMessageBox box = new("Quit", "Discard unsaved work?", MessageBoxButtons.Ok);

		StartWith(() => box.Show(outcomes.Add));
		ClickDialogButton(0);
		Step(2);

		box.Show(outcomes.Add);
		Step(2);
		ClickDialogButton(0);

		Assert.AreEqual(2, outcomes.Count, $"The dialog reported [{string.Join(", ", outcomes)}] across two showings.");
	}
}

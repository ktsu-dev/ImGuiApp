// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the rename dialog's outcome record. The dialog itself needs a live ImGui context.
/// </summary>
[TestClass]
public sealed class RenameDialogTests
{
	[TestMethod]
	public void RenameOutcome_CarriesTheFailureReason()
	{
		InvalidOperationException error = new("target exists");
		RenameOutcome outcome = new(DialogOutcome.Failed, null, error);

		Assert.AreEqual(DialogOutcome.Failed, outcome.Outcome);
		Assert.AreSame(error, outcome.Error);
	}

	[TestMethod]
	public void RenameOutcome_SuccessHasNoError()
	{
		RenameOutcome outcome = new(DialogOutcome.Ok, null, null);

		Assert.IsNull(outcome.Error);
	}

	/// <summary>
	/// Hexa catches a failing move and then still closes with <c>DialogResult.Ok</c>, so the raw
	/// result reports success for a rename that did not happen. A captured exception must win.
	/// </summary>
	[TestMethod]
	public void BuildRenameOutcome_ExceptionOverridesOk()
	{
		IOException error = new("access denied");
		string destination = Path.Combine(AppContext.BaseDirectory, "renamed.txt");

		RenameOutcome outcome = ImGuiWidgets.BuildRenameOutcome(DialogOutcome.Ok, destination, error);

		Assert.AreEqual(DialogOutcome.Failed, outcome.Outcome);
		Assert.AreSame(error, outcome.Error);
		Assert.IsNull(outcome.Destination);
	}

	[TestMethod]
	public void BuildRenameOutcome_OkCarriesTheDestination()
	{
		string destination = Path.Combine(AppContext.BaseDirectory, "renamed.txt");

		RenameOutcome outcome = ImGuiWidgets.BuildRenameOutcome(DialogOutcome.Ok, destination, null);

		Assert.AreEqual(DialogOutcome.Ok, outcome.Outcome);
		Assert.IsNotNull(outcome.Destination);
		Assert.IsNull(outcome.Error);
	}

	/// <summary>
	/// Hexa seeds its destination with the source path and recomputes it on every keystroke, so it
	/// is populated on Cancel too. It must not be reported as the result of a rename.
	/// </summary>
	[TestMethod]
	public void BuildRenameOutcome_CancelDropsTheDestination()
	{
		string destination = Path.Combine(AppContext.BaseDirectory, "renamed.txt");

		RenameOutcome outcome = ImGuiWidgets.BuildRenameOutcome(DialogOutcome.Cancel, destination, null);

		Assert.AreEqual(DialogOutcome.Cancel, outcome.Outcome);
		Assert.IsNull(outcome.Destination);
	}

	[TestMethod]
	public void BuildRenameOutcome_BareRelativeDestination_DoesNotThrow()
	{
		RenameOutcome outcome = ImGuiWidgets.BuildRenameOutcome(DialogOutcome.Ok, "renamed.txt", null);

		Assert.AreEqual(DialogOutcome.Ok, outcome.Outcome);
		Assert.IsNull(outcome.Destination);
	}
}

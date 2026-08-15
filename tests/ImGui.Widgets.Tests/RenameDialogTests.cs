// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

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
}

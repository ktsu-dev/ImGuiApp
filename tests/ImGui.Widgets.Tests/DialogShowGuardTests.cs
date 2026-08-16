// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the re-entry guard that stops a dialog instance being registered with Hexa's dialog
/// manager twice, which would leave an entry that is never drawn, never closed and never removed.
/// </summary>
[TestClass]
public sealed class DialogShowGuardTests
{
	[TestMethod]
	public void NewGuard_IsNotShown() =>
		Assert.IsFalse(new ImGuiWidgets.DialogShowGuard("OpenFileDialog").IsShown);

	[TestMethod]
	public void Enter_MarksTheDialogShown()
	{
		ImGuiWidgets.DialogShowGuard guard = new("OpenFileDialog");

		guard.Enter();

		Assert.IsTrue(guard.IsShown);
	}

	[TestMethod]
	public void Enter_WhileShown_Throws()
	{
		ImGuiWidgets.DialogShowGuard guard = new("OpenFileDialog");
		guard.Enter();

		Assert.ThrowsExactly<InvalidOperationException>(guard.Enter);
	}

	[TestMethod]
	public void Enter_WhileShown_NamesTheDialogInTheMessage()
	{
		ImGuiWidgets.DialogShowGuard guard = new("SaveFileDialog");
		guard.Enter();

		InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(guard.Enter);

		StringAssert.Contains(error.Message, "SaveFileDialog");
	}

	[TestMethod]
	public void Exit_AllowsShowingAgain()
	{
		ImGuiWidgets.DialogShowGuard guard = new("OpenFileDialog");
		guard.Enter();

		guard.Exit();

		Assert.IsFalse(guard.IsShown);
		guard.Enter();
		Assert.IsTrue(guard.IsShown);
	}

	[TestMethod]
	public void Exit_WhenNotShown_IsHarmless()
	{
		ImGuiWidgets.DialogShowGuard guard = new("OpenFileDialog");

		guard.Exit();

		Assert.IsFalse(guard.IsShown);
	}
}

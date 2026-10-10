// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what a frame asks the operating system's input method for, which is what the desktop
/// backend carries to the window.
/// </summary>
[TestClass]
public sealed class ImeCaretTests
{
	private static HarnessOptions Window() => new() { Width = 300, Height = 200 };

	[TestMethod]
	public void NothingTypedInto_RequestsNothing()
	{
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig { OnRender = _ => ImGui.TextUnformatted("hello") }, Window());

		harness.Step(2);

		Assert.IsNull(harness.ImePlacement);
	}

	[TestMethod]
	public void ACaretReportedThisFrame_IsWhatTheFrameRequests()
	{
		Vector2 caret = new(120.5f, 64f);
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(new ImGuiAppConfig { OnRender = _ => ImeCaret.Set(caret, 18f) }, Window());

		harness.Step(2);

		Assert.AreEqual(new ImePlacement(caret, 18f), harness.ImePlacement);
	}

	[TestMethod]
	public void ACaretNoLongerReported_LapsesAtTheNextFrame()
	{
		bool editing = true;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					if (editing)
					{
						ImeCaret.Set(new Vector2(10, 10), 16f);
					}
				},
			},
			Window());

		harness.Step();
		Assert.IsNotNull(harness.ImePlacement);

		editing = false;
		harness.Step();

		Assert.IsNull(harness.ImePlacement);
	}

	[TestMethod]
	public void AReportedCaret_LeavesWantTextInputAlone()
	{
		// The flag says an ImGui text field has the keyboard; a custom editor that gives way to one
		// would otherwise give way to itself.
		bool wantTextInput = true;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					wantTextInput = ImGui.GetIO().WantTextInput;
					ImeCaret.Set(new Vector2(10, 10), 16f);
				},
			},
			Window());

		harness.Step(2);

		Assert.IsFalse(wantTextInput);
	}

	[TestMethod]
	public void AFocusedInputText_RequestsItsOwnCaret()
	{
		string text = "abc";
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				OnRender = _ =>
				{
					ImGui.SetNextWindowPos(Vector2.Zero);
					ImGui.SetNextWindowSize(new Vector2(240, 150));
					ImGui.Begin("probe", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
					ImGui.SetKeyboardFocusHere();
					ImGui.InputText("field", ref text, 64);
					ImGui.End();
				},
			},
			Window());

		harness.Step(3);

		ImePlacement placement = harness.ImePlacement
			?? throw new AssertFailedException("ImGui's own text field should ask the input method to open at its caret.");
		Assert.IsTrue(placement.LineHeight > 0);
	}
}

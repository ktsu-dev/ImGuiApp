// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiPopupsDemo.UITests;

using System;
using System.Collections.Generic;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Popups;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiPopups.Prompt"/> on its own, for the answers the demo cannot provoke: a prompt
/// closed because another opened, and an answer that opens the next prompt.
/// </summary>
[TestClass]
public sealed class PromptTests
{
	private readonly ImGuiPopups.Prompt first = new();
	private readonly ImGuiPopups.Prompt second = new();
	private readonly List<string> answers = [];
	private ImGuiAppHarness harness = null!;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(PromptTests),
				SaveIniSettings = false,
				OnRender = _ =>
				{
					first.ShowIfOpen();
					second.ShowIfOpen();
				},
			},
			new HarnessOptions { Width = 800, Height = 600 });
		harness.Step(2);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private void OpenFirst(Action? onYes = null) =>
		first.Open(
			"First",
			"Carry on?",
			[
				new ImGuiPopups.PromptButton("Yes", onYes ?? (() => answers.Add("yes")), "first/yes"),
				new ImGuiPopups.PromptButton("No", () => answers.Add("no"), "first/no", IsCancel: true),
			]);

	[TestMethod]
	public void AnsweringRunsTheActionOnceAndClosesThePrompt()
	{
		OpenFirst();
		harness.Step(2);
		Assert.IsTrue(first.IsAwaitingAnswer);

		harness.Click("first/yes");
		harness.Step(2);

		Assert.AreEqual("yes", string.Join(",", answers));
		Assert.IsFalse(first.IsAwaitingAnswer);
		Assert.IsFalse(IsVisible("first/yes"));
	}

	/// <summary>
	/// ImGui closes a modal when another opens at the same level. Whoever asked the first is still
	/// waiting, so it answers with its cancel button.
	/// </summary>
	[TestMethod]
	public void APromptPushedAsideByAnotherGivesItsCancelAnswer()
	{
		OpenFirst();
		harness.Step(2);

		second.Open("Second", "Something else", [new ImGuiPopups.PromptButton("OK", () => answers.Add("ok"), "second/ok")]);
		harness.Step(3);

		Assert.AreEqual("no", string.Join(",", answers), "The prompt pushed aside did not give its cancel answer, or gave it twice.");
		Assert.IsFalse(first.IsAwaitingAnswer);
	}

	[TestMethod]
	public void AnAnswerCanOpenTheNextPrompt()
	{
		OpenFirst(() => second.Open("Second", "And then?", [new ImGuiPopups.PromptButton("Done", () => answers.Add("done"), "second/done")]));
		harness.Step(2);

		harness.Click("first/yes");
		harness.Step(3);
		Assert.IsTrue(IsVisible("second/done"), "The prompt opened by an answer did not appear.");

		harness.Click("second/done");
		harness.Step(2);

		Assert.AreEqual("done", string.Join(",", answers), "Opening the next prompt was taken for dismissing the first.");
	}

	[TestMethod]
	public void ClosingWithoutACancelButtonAnswersNothing()
	{
		first.Open("First", "Just so you know", [new ImGuiPopups.PromptButton("OK", () => answers.Add("ok"))]);
		harness.Step(2);

		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(2);

		Assert.IsEmpty(answers);
		Assert.IsFalse(first.IsAwaitingAnswer);
	}

	[TestMethod]
	public void ButtonsWithoutAProbeNameKeepTheDefault()
	{
		first.Open("First", "Pick", [new ImGuiPopups.PromptButton("Fine")]);
		harness.Step(2);

		Assert.IsTrue(IsVisible("prompt/Fine"));
	}

	/// <summary>
	/// A wrapped label is drawn as it is, not read as a printf format, so "%%" stays two characters. The
	/// unformatted layout, given the same label and size, is the reference: the two must match pixel for pixel.
	/// </summary>
	[TestMethod]
	[DataRow("Done: 100%%")]
	[DataRow("Saved to %s")]
	public void AWrappedLabelIsNotReadAsAFormat(string label)
	{
		System.Numerics.Vector2 size = new(400, 200);

		first.Open("First", label, [new ImGuiPopups.PromptButton("OK")], textLayoutType: ImGuiPopups.PromptTextLayoutType.Unformatted, size: size);
		harness.Step(3);
		byte[] unformatted = harness.Target.Pixels.ToArray();
		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(2);

		first.Open("First", label, [new ImGuiPopups.PromptButton("OK")], textLayoutType: ImGuiPopups.PromptTextLayoutType.Wrapped, size: size);
		harness.Step(3);

		CollectionAssert.AreEqual(unformatted, harness.Target.Pixels.ToArray(), "The wrapped label was drawn differently from the same label unformatted.");
	}

	[TestMethod]
	public void TwoCancelButtonsAreRefused() =>
		Assert.ThrowsExactly<ArgumentException>(() => first.Open(
			"First",
			"Which?",
			[
				new ImGuiPopups.PromptButton("A", IsCancel: true),
				new ImGuiPopups.PromptButton("B", IsCancel: true),
			]));
}

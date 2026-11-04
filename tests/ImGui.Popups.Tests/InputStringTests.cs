// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Popups.Tests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiPopups.InputString"/> on its own.</summary>
[TestClass]
public sealed class InputStringTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private ImGuiAppHarness harness = null!;
	private ImGuiPopups.InputString input = null!;
	private string? confirmed;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
	}

	private void Open(string defaultValue)
	{
		input = new ImGuiPopups.InputString();
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(InputStringTests),
				OnRender = _ => input.ShowIfOpen(),
				SaveIniSettings = false,
			},
			Viewport);

		input.Open("Rename", "Name", defaultValue, value => confirmed = value);

		// One frame to submit the popup, more for it to be sized, centred and given the keyboard.
		harness.Step(4);
	}

	[TestMethod]
	public void TypingAndEnter_ConfirmsTheValue()
	{
		Open("abc");

		harness.Keyboard.Press(ImGuiKey.End);
		harness.Keyboard.Type("d");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(2);

		Assert.AreEqual("abcd", confirmed);
	}

	/// <summary>
	/// Issue #553: the field had a fixed 100-byte buffer, and it takes the keyboard as it opens, so
	/// opening it on a longer default aborted the process at once.
	/// </summary>
	[TestMethod]
	[DataRow(1500, 'a', DisplayName = "1500 ASCII characters")]
	[DataRow(400, '漢', DisplayName = "400 CJK characters")]
	[DataRow(99, 'a', DisplayName = "Exactly the old capacity")]
	public void ADefaultLongerThanTheOldBuffer_CanBeExtended(int length, char filler)
	{
		string original = new(filler, length);
		Open(original);

		harness.Keyboard.Press(ImGuiKey.End);
		harness.Keyboard.Type("z");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(2);

		Assert.AreEqual(original + "z", confirmed);
	}
}

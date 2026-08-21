// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiPopupsDemo.UITests;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.Popups;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiPopupsDemo through the headless harness: opens each popup the demo offers, completes
/// or cancels it, and asserts on the demo state the popup's callback was supposed to write.
/// </summary>
[TestClass]
public sealed class PopupsDemoUITests
{
	private static readonly bool[] ExpectedFlagPattern = [true, false, true, false];

	// The demo stacks seven expanded sections, reaching about y=810. At the harness default height
	// of 720 the last of them sit below the fold, where a click on their recorded position lands on
	// nothing at all. A viewport tall enough to hold the whole demo keeps every control reachable.
	private static readonly HarnessOptions DemoViewport = new() { Width = 1280, Height = 1000 };

	private ImGuiAppHarness harness = null!;

	[TestInitialize]
	public void SetUp()
	{
		// Demo state lives in statics that outlive a harness, so each test starts from a known slate.
		ImGuiPopupsDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiPopupsDemo.BuildConfig(), DemoViewport);
		harness.Step(2);
	}

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// Reports whether an item was drawn in the frame just rendered. Probe.Rect remembers the last
	/// position an item ever occupied, so it answers "was this ever drawn" rather than "is this on
	/// screen now" -- exactly the distinction a popup that opens and closes turns on.
	/// </summary>
	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	/// <summary>Clicks an item, then advances frames so the popup it opened is submitted.</summary>
	private void ClickAndSettle(string name, int frames = 2)
	{
		harness.Click(name);
		harness.Step(frames);
	}

	[TestMethod]
	public void Config_WiresUpTheDemoCallbacks()
	{
		ImGuiAppConfig config = ImGuiPopupsDemo.BuildConfig();

		Assert.AreEqual("ImGui Popups Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsNotNull(config.OnStart);
		Assert.IsFalse(config.SaveIniSettings, "The demo opts out of layout persistence.");
	}

	[TestMethod]
	public void EverySection_IsRendered()
	{
		foreach (string section in new[]
		{
			"Input Popups",
			"Message & Prompt Popups",
			"Searchable Lists",
			"File System Browser",
			"Custom Modal",
			"Advanced Examples",
			"Tips & Features",
		})
		{
			Assert.IsTrue(IsVisible(section), $"Section '{section}' was never rendered.");
		}
	}

	[TestMethod]
	public void EveryLaunchButton_IsRendered()
	{
		foreach (string button in new[]
		{
			"Edit String", "Edit String (Custom Size)", "Edit Integer", "Edit Float",
			"Show Simple Message", "Show Long Message", "Show Custom Prompt", "Show Warning Prompt",
			"Choose Friend", "Choose Friend (Custom Size)", "Choose Color",
			"Open Any File", "Open C# File", "Open Image File", "Save Text File", "Save Any File",
			"Choose Directory", "Choose Directory (Large)",
			"Show Custom Modal", "Show Custom Modal (Large)",
		})
		{
			Assert.IsTrue(IsVisible(button), $"Button '{button}' was never rendered.");
		}
	}

	[TestMethod]
	public void EditString_TypedValueIsCommittedOnOk()
	{
		ClickAndSettle("Edit String");
		Assert.IsTrue(IsVisible("input/field"), "The string input popup did not open.");

		harness.Keyboard.Type("typed by a test");
		harness.Step();
		ClickAndSettle("input/ok");

		Assert.AreEqual("typed by a test", ImGuiPopupsDemo.stringInputValue);
	}

	[TestMethod]
	public void EditString_CustomSizeVariantOpensTheSamePopup()
	{
		ClickAndSettle("Edit String (Custom Size)");

		Assert.IsTrue(IsVisible("input/field"), "The custom-size string popup did not open.");
	}

	[TestMethod]
	public void EditInteger_OpensAndCommits()
	{
		ClickAndSettle("Edit Integer");
		Assert.IsTrue(IsVisible("input/field"), "The integer input popup did not open.");

		ClickAndSettle("input/ok");

		// Committing without editing keeps the starting value rather than zeroing it.
		Assert.AreEqual(42, ImGuiPopupsDemo.intInputValue);
	}

	[TestMethod]
	public void EditFloat_OpensAndCommits()
	{
		ClickAndSettle("Edit Float");
		Assert.IsTrue(IsVisible("input/field"), "The float input popup did not open.");

		ClickAndSettle("input/ok");

		Assert.AreEqual(3.14159f, ImGuiPopupsDemo.floatInputValue, 0.00001f);
	}

	[TestMethod]
	public void SimpleMessage_OpensAndClosesOnOk()
	{
		ClickAndSettle("Show Simple Message");
		Assert.IsTrue(IsVisible("prompt/OK"), "The message popup did not open.");

		ClickAndSettle("prompt/OK");

		Assert.IsFalse(IsVisible("prompt/OK"), "The message popup did not close.");
	}

	[TestMethod]
	public void LongMessage_OpensWithWrappedText()
	{
		ClickAndSettle("Show Long Message");

		Assert.IsTrue(IsVisible("prompt/OK"), "The long message popup did not open.");
	}

	[TestMethod]
	public void CustomPrompt_OffersEveryButton()
	{
		ClickAndSettle("Show Custom Prompt");

		foreach (string choice in new[] { "Yes", "No", "Maybe", "Cancel" })
		{
			Assert.IsTrue(IsVisible($"prompt/{choice}"), $"The prompt is missing '{choice}'.");
		}
	}

	[TestMethod]
	public void CustomPrompt_RecordsTheChosenButton()
	{
		ClickAndSettle("Show Custom Prompt");
		ClickAndSettle("prompt/Maybe");

		Assert.AreEqual("User clicked Maybe", ImGuiPopupsDemo.lastPromptResult);
	}

	[TestMethod]
	public void WarningPrompt_ConfirmingRecordsTheDeletion()
	{
		ClickAndSettle("Show Warning Prompt");
		Assert.IsTrue(IsVisible("prompt/Delete"), "The warning prompt did not open.");

		ClickAndSettle("prompt/Delete");

		Assert.AreEqual("User confirmed deletion", ImGuiPopupsDemo.lastPromptResult);
	}

	[TestMethod]
	public void ChooseFriend_SelectingAnItemRecordsIt()
	{
		ClickAndSettle("Choose Friend");
		Assert.IsTrue(IsVisible("searchable-list/search"), "The searchable list did not open.");

		ClickAndSettle("searchable-list/Charlie");
		ClickAndSettle("searchable-list/ok");

		Assert.AreEqual("Charlie", ImGuiPopupsDemo.selectedFriend);
	}

	[TestMethod]
	public void ChooseFriend_SearchFieldAcceptsInput()
	{
		ClickAndSettle("Choose Friend");
		Assert.IsTrue(IsVisible("searchable-list/search"), "The searchable list did not open.");

		byte[] beforeTyping = harness.Target.Pixels.ToArray();

		// The list auto-focuses its search field when it opens, so typing goes straight to it.
		harness.Keyboard.Type("Di");
		harness.Step(2);

		Assert.IsFalse(beforeTyping.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Typing into the search field should change what is drawn.");

		// The list ranks rather than filters: TextFilter.Rank reorders the entries and keeps the
		// non-matching ones, so every friend stays addressable while a search term is active.
		Assert.IsTrue(IsVisible("searchable-list/Diana"), "The searched-for entry should still be listed.");
		Assert.IsTrue(IsVisible("searchable-list/Alice"), "Ranking keeps non-matching entries in the list.");
	}

	[TestMethod]
	public void ChooseFriend_CancelLeavesTheSelectionAlone()
	{
		ClickAndSettle("Choose Friend");
		ClickAndSettle("searchable-list/cancel");

		Assert.AreEqual("None", ImGuiPopupsDemo.selectedFriend);
	}

	[TestMethod]
	public void FilesystemBrowser_OpensAndCanBeCancelled()
	{
		ClickAndSettle("Open Any File", frames: 6);
		Assert.IsTrue(IsVisible("filesystem-browser/cancel"), "The file browser did not open.");

		ClickAndSettle("filesystem-browser/cancel");

		Assert.AreEqual("None", ImGuiPopupsDemo.lastFileOpened);
	}

	[TestMethod]
	public void FilesystemBrowser_SaveVariantOffersAFilenameField()
	{
		ClickAndSettle("Save Text File", frames: 6);

		Assert.IsTrue(IsVisible("filesystem-browser/filename"), "A save browser needs a filename field.");
	}

	[TestMethod]
	public void FilesystemBrowser_DirectoryVariantOpens()
	{
		ClickAndSettle("Choose Directory", frames: 6);

		Assert.IsTrue(IsVisible("filesystem-browser/confirm"), "The directory chooser did not open.");
	}

	[TestMethod]
	public void CustomModal_SetResultCapturesTheControls()
	{
		ClickAndSettle("Show Custom Modal");
		Assert.IsTrue(IsVisible("Custom Checkbox"), "The custom modal did not open.");

		ClickAndSettle("Custom Checkbox");
		ClickAndSettle("Set Result");

		Assert.AreEqual("Checkbox: True, Slider: 0.50", ImGuiPopupsDemo.lastCustomModalResult);
	}

	[TestMethod]
	public void CustomModal_CloseRecordsThatNothingWasSet()
	{
		ClickAndSettle("Show Custom Modal");
		ClickAndSettle("Close");

		Assert.AreEqual("User closed without setting result", ImGuiPopupsDemo.lastCustomModalResult);
	}

	[TestMethod]
	public void AdvancedModal_ShowsEveryControl()
	{
		ClickAndSettle("Show Custom Modal (Large)");

		Assert.IsTrue(IsVisible("Select Option"), "combo");
		Assert.IsTrue(IsVisible("Color Picker"), "color picker");
		Assert.IsTrue(IsVisible("Flag 1"), "flags");
		Assert.IsTrue(IsVisible("Apply Settings"), "apply");
	}

	[TestMethod]
	public void AdvancedModal_ApplySettingsRecordsSelectionAndColor()
	{
		ClickAndSettle("Show Custom Modal (Large)");
		ClickAndSettle("Apply Settings");

		Assert.AreEqual("Selected: Option 1, Color: RGB(1.00, 0.50, 0.00)", ImGuiPopupsDemo.lastCustomModalResult);
	}

	[TestMethod]
	public void AdvancedModal_ResetRestoresTheFlagPattern()
	{
		ClickAndSettle("Show Custom Modal (Large)");
		ClickAndSettle("Flag 2");
		ClickAndSettle("Reset");

		CollectionAssert.AreEqual(ExpectedFlagPattern, ImGuiPopupsDemo.advancedModalFlags);
	}

	[TestMethod]
	public void MultiStepWorkflow_StartsAtTheNamePrompt()
	{
		ClickAndSettle("Advanced Examples");   // the section is collapsed by default
		ClickAndSettle("Multi-Step Workflow");

		Assert.IsTrue(IsVisible("input/field"), "The workflow should begin by asking for a name.");
	}

	[TestMethod]
	public void ValidationExample_RejectsAMalformedAddress()
	{
		ClickAndSettle("Advanced Examples");
		ClickAndSettle("Validation Example");

		harness.Keyboard.Type("not-an-email");
		harness.Step();
		ClickAndSettle("input/ok");

		Assert.IsTrue(IsVisible("prompt/OK"), "An invalid address should raise a message popup.");
		Assert.AreEqual("Hello World", ImGuiPopupsDemo.stringInputValue, "A rejected address must not be stored.");
	}

	[TestMethod]
	public void Demo_RendersIdenticallyAcrossRuns()
	{
		byte[] first = harness.Target.Pixels.ToArray();

		harness.Dispose();
		ImGuiPopupsDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiPopupsDemo.BuildConfig(), DemoViewport);
		harness.Step(2);

		CollectionAssert.AreEqual(first, harness.Target.Pixels.ToArray(), "Two runs of the same scenario should match.");
	}
}

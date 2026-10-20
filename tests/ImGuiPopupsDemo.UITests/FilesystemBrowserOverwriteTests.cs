// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiPopupsDemo.UITests;

using System;
using System.IO;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Popups;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiPopups.FilesystemBrowser.ConfirmOverwrite"/> in a folder of its own, so the
/// file a save would replace is one the test made.
/// </summary>
[TestClass]
public sealed class FilesystemBrowserOverwriteTests
{
	private string folder = null!;
	private string previousDirectory = null!;
	private ImGuiPopups.FilesystemBrowser browser = null!;
	private ImGuiAppHarness harness = null!;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;
	private string? saved;

	[TestInitialize]
	public void SetUp()
	{
		folder = Path.Join(Path.GetTempPath(), $"overwrite-{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		File.WriteAllText(Path.Join(folder, "existing.txt"), "keep me");

		// The browser starts in the working directory, so point it at the test's folder before making one.
		previousDirectory = Environment.CurrentDirectory;
		Environment.CurrentDirectory = folder;
		browser = new ImGuiPopups.FilesystemBrowser { ConfirmOverwrite = true };

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(FilesystemBrowserOverwriteTests),
				SaveIniSettings = false,
				OnRender = _ => browser.ShowIfOpen(),
			},
			new HarnessOptions { Width = 1000, Height = 800 });
		harness.Step(2);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
		Environment.CurrentDirectory = previousDirectory;
		Directory.Delete(folder, recursive: true);
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private void SaveAs(string name)
	{
		browser.FileSave("Save", file => saved = file.ToString(), "*.txt");
		harness.Step(4);
		harness.Click("filesystem-browser/filename");
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type(name);
		harness.Step();
		harness.Click("filesystem-browser/confirm");
		harness.Step(3);
	}

	[TestMethod]
	public void SavingOverAnExistingFile_AsksFirst()
	{
		SaveAs("existing.txt");

		Assert.IsNull(saved, "The save chose a file that exists without asking.");
		Assert.IsTrue(IsVisible("filesystem-browser/overwrite/replace"), "No replace question appeared.");
	}

	[TestMethod]
	public void Replace_ChoosesTheFileAndClosesTheBrowser()
	{
		SaveAs("existing.txt");

		harness.Click("filesystem-browser/overwrite/replace");
		harness.Step(3);

		Assert.AreEqual(Path.Join(folder, "existing.txt"), saved);
		Assert.IsFalse(IsVisible("filesystem-browser/confirm"), "The browser stayed open after Replace.");
	}

	[TestMethod]
	public void Cancel_LeavesTheBrowserOpenAndChoosesNothing()
	{
		SaveAs("existing.txt");

		harness.Click("filesystem-browser/overwrite/cancel");
		harness.Step(3);

		Assert.IsNull(saved);
		Assert.IsFalse(IsVisible("filesystem-browser/overwrite/replace"), "The question stayed open after Cancel.");
		Assert.IsTrue(IsVisible("filesystem-browser/confirm"), "Cancelling the question closed the browser too.");
	}

	[TestMethod]
	public void Escape_LeavesTheBrowserOpenAndChoosesNothing()
	{
		SaveAs("existing.txt");
		Assert.IsTrue(IsVisible("filesystem-browser/overwrite/replace"));

		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(3);

		Assert.IsNull(saved);
		Assert.IsFalse(IsVisible("filesystem-browser/overwrite/replace"), "The question stayed open after Escape.");
		Assert.IsTrue(IsVisible("filesystem-browser/confirm"), "Escape on the question closed the browser too.");
	}

	[TestMethod]
	public void EscapeOnTheBrowserItself_StillClosesIt()
	{
		browser.FileSave("Save", file => saved = file.ToString(), "*.txt");
		harness.Step(4);

		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(3);

		Assert.IsNull(saved);
		Assert.IsFalse(IsVisible("filesystem-browser/confirm"), "Escape with nothing above the browser should close it.");
	}

	/// <summary>
	/// A folder separator typed into the save box used to throw on the frame it was typed, which
	/// took the whole application down with it (#662).
	/// </summary>
	[TestMethod]
	public void APathTypedAsTheName_IsReportedRatherThanThrown()
	{
		SaveAs("a/b.txt");

		Assert.IsNull(saved, "A name holding a folder separator was chosen.");
		Assert.IsTrue(IsVisible("prompt/OK"), "No Invalid File Name message appeared.");
		Assert.IsTrue(IsVisible("filesystem-browser/confirm"), "The browser closed.");
	}

	[TestMethod]
	public void EscapeOnTheInvalidNameMessage_LeavesTheBrowserOpen()
	{
		SaveAs("a/b.txt");
		Assert.IsTrue(IsVisible("prompt/OK"));

		harness.Keyboard.Press(ImGuiKey.Escape);
		harness.Step(3);

		Assert.IsNull(saved);
		Assert.IsFalse(IsVisible("prompt/OK"), "The message stayed open after Escape.");
		Assert.IsTrue(IsVisible("filesystem-browser/confirm"), "Escape on the message closed the browser too.");
	}

	[TestMethod]
	public void SavingANewFile_DoesNotAsk()
	{
		SaveAs("new.txt");

		Assert.AreEqual(Path.Join(folder, "new.txt"), saved);
		Assert.IsFalse(IsVisible("filesystem-browser/overwrite/replace"));
	}

	[TestMethod]
	public void WithoutConfirmOverwrite_AnExistingFileIsChosenAtOnce()
	{
		browser.ConfirmOverwrite = false;

		SaveAs("existing.txt");

		Assert.AreEqual(Path.Join(folder, "existing.txt"), saved);
	}
}

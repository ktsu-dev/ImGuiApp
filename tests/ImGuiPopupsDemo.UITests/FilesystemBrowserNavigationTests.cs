// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiPopupsDemo.UITests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Popups;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the browser's toolbar, sidebar, search, views and new folder in a folder of its own:
/// <c>alpha/inner/</c>, <c>beta/</c>, <c>.secret</c>, <c>notes.txt</c>, <c>photo.png</c> and <c>shot.png</c>.
/// </summary>
[TestClass]
public sealed class FilesystemBrowserNavigationTests
{
	private static readonly string[] FilesInTheFolder = ["notes.txt", "photo.png", "shot.png"];

	private string folder = null!;
	private string previousDirectory = null!;
	private ImGuiPopups.FilesystemBrowser browser = null!;
	private ImGuiAppHarness harness = null!;
	private string? chosen;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		folder = Path.Join(Path.GetTempPath(), $"browse-{Guid.NewGuid():N}");
		Directory.CreateDirectory(Path.Join(folder, "alpha", "inner"));
		Directory.CreateDirectory(Path.Join(folder, "beta"));
		File.WriteAllText(Path.Join(folder, ".secret"), "hidden");
		File.WriteAllText(Path.Join(folder, "notes.txt"), "notes");
		File.WriteAllText(Path.Join(folder, "photo.png"), "not really a png");
		File.WriteAllText(Path.Join(folder, "shot.png"), "nor this");

		// .NET reports a dot-named file as hidden outside Windows; on Windows it has to be marked.
		File.SetAttributes(Path.Join(folder, ".secret"), FileAttributes.Hidden);

		previousDirectory = Environment.CurrentDirectory;
		Environment.CurrentDirectory = folder;
		browser = new ImGuiPopups.FilesystemBrowser();

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(FilesystemBrowserNavigationTests),
				SaveIniSettings = false,
				OnRender = _ => browser.ShowIfOpen(),
			},
			new HarnessOptions { Width = 1100, Height = 800 });
		harness.Step(2);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
		Environment.CurrentDirectory = previousDirectory;
		File.SetAttributes(Path.Join(folder, ".secret"), FileAttributes.Normal);
		Directory.Delete(folder, recursive: true);
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private static string Row(string name) => $"filesystem-browser/{name}";

	private static string FolderRow(string name) => Row(name + Path.DirectorySeparatorChar);

	private void OpenAll()
	{
		browser.FileOpen("Open", file => chosen = file.ToString());
		harness.Step(4);
	}

	private void DoubleClick(string name)
	{
		Rectangle rect = harness.Probe.Rect(name) ?? throw new AssertFailedException($"{name} was never drawn.");
		float x = rect.MinX + (rect.Width / 2f);
		float y = rect.MinY + (rect.Height / 2f);
		harness.Mouse.Click(x, y);
		harness.Mouse.Click(x, y);
		harness.Step(2);
	}

	[TestMethod]
	public void DoubleClickingAFolder_OpensIt_AndBackAndForwardRetraceTheWay()
	{
		OpenAll();
		DoubleClick(FolderRow("alpha"));
		Assert.IsTrue(IsVisible(FolderRow("inner")), "Double-clicking alpha did not open it.");

		harness.Click("filesystem-browser/back");
		harness.Step(2);
		Assert.IsTrue(IsVisible(Row("notes.txt")), "Back did not return to the folder before.");

		harness.Click("filesystem-browser/forward");
		harness.Step(2);
		Assert.IsTrue(IsVisible(FolderRow("inner")), "Forward did not go into alpha again.");
	}

	[TestMethod]
	public void Up_GoesToTheFolderAbove_WithTheFolderComeFromSelected()
	{
		browser.NavigateTo(Path.Join(folder, "alpha").As<AbsoluteDirectoryPath>());
		OpenAll();

		harness.Click("filesystem-browser/up");
		harness.Step(2);

		Assert.IsTrue(IsVisible(Row("notes.txt")), "Up did not go to the folder above.");
		Assert.AreEqual(folder, browser.Directory.WeakString);
	}

	[TestMethod]
	public void ABreadcrumb_GoesToItsFolder()
	{
		browser.NavigateTo(Path.Join(folder, "alpha", "inner").As<AbsoluteDirectoryPath>());
		OpenAll();

		// The last step is the folder shown, and the one before it is alpha.
		int deepest = 0;
		foreach (string name in harness.Probe.KnownNames.Where(name => name.Contains("filesystem-browser/crumb/", StringComparison.Ordinal)))
		{
			deepest = Math.Max(deepest, int.Parse(name[(name.LastIndexOf('/') + 1)..], System.Globalization.CultureInfo.InvariantCulture));
		}

		harness.Click($"filesystem-browser/crumb/{deepest - 1}");
		harness.Step(2);

		Assert.AreEqual(Path.Join(folder, "alpha"), browser.Directory.WeakString);
		Assert.IsTrue(IsVisible(FolderRow("inner")));
	}

	[TestMethod]
	public void TypingAPath_GoesThere()
	{
		OpenAll();

		harness.Click("filesystem-browser/path");
		harness.Step(2);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type(Path.Join(folder, "beta"));
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(Path.Join(folder, "beta"), browser.Directory.WeakString);
		Assert.IsTrue(IsVisible("filesystem-browser/empty"), "beta is empty, and the browser should say so.");
	}

	[TestMethod]
	public void TypingAFilesPath_ChoosesIt()
	{
		OpenAll();

		harness.Click("filesystem-browser/path");
		harness.Step(2);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type(Path.Join(folder, "notes.txt"));
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(Path.Join(folder, "notes.txt"), chosen);
	}

	[TestMethod]
	public void Search_KeepsOnlyWhatMatches()
	{
		OpenAll();

		harness.Click("filesystem-browser/search");
		harness.Keyboard.Type("PNG");
		harness.Step(2);

		Assert.IsTrue(IsVisible(Row("photo.png")));
		Assert.IsTrue(IsVisible(Row("shot.png")));
		Assert.IsFalse(IsVisible(Row("notes.txt")), "Search kept a file whose name does not hold the text.");
		Assert.IsFalse(IsVisible(FolderRow("alpha")), "Search kept a folder whose name does not hold the text.");
	}

	[TestMethod]
	public void HiddenFiles_AreListedOnlyWhenAskedFor()
	{
		OpenAll();
		Assert.IsFalse(IsVisible(Row(".secret")), "A hidden file was listed by default.");

		harness.Click("filesystem-browser/hidden");
		harness.Step(2);

		Assert.IsTrue(IsVisible(Row(".secret")), "Ticking Hidden files did not list the hidden file.");
		Assert.IsTrue(browser.ShowHiddenFiles);
	}

	[TestMethod]
	public void AHostsPlace_GoesToItsFolder()
	{
		browser.Places.Add(new("Fixtures", Path.Join(folder, "alpha").As<AbsoluteDirectoryPath>()));
		OpenAll();

		harness.Click("filesystem-browser/place/Fixtures");
		harness.Step(2);

		Assert.IsTrue(IsVisible(FolderRow("inner")), "The place did not go to its folder.");
	}

	[TestMethod]
	public void ChoosingAFile_RemembersItsFolderAsRecent()
	{
		OpenAll();
		DoubleClick(Row("notes.txt"));

		Assert.AreEqual(Path.Join(folder, "notes.txt"), chosen);
		Assert.AreEqual(folder, browser.RecentFolders.Single().WeakString);
	}

	[TestMethod]
	public void ChoosingAFolder_WithNothingSelected_ChoosesTheFolderShown()
	{
		browser.ChooseDirectory("Choose", directory => chosen = directory.ToString());
		harness.Step(4);

		harness.Click("filesystem-browser/confirm");
		harness.Step(2);

		Assert.AreEqual(folder, chosen);
	}

	[TestMethod]
	public void NewFolder_MakesTheFolderAndSelectsIt()
	{
		browser.ChooseDirectory("Choose", directory => chosen = directory.ToString());
		harness.Step(4);

		harness.Click("filesystem-browser/new-folder");
		harness.Step(3);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("made");
		harness.Click("input/ok");
		harness.Step(3);

		Assert.IsTrue(Directory.Exists(Path.Join(folder, "made")), "The folder was not made.");
		harness.Click("filesystem-browser/confirm");
		harness.Step(2);
		Assert.AreEqual(Path.Join(folder, "made"), chosen, "The new folder was not the one selected.");
	}

	[TestMethod]
	public void EnterInTheNameBox_Saves()
	{
		browser.FileSave("Save", file => chosen = file.ToString(), "*.txt");
		harness.Step(4);

		harness.Click("filesystem-browser/filename");
		harness.Keyboard.Type("fresh.txt");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(Path.Join(folder, "fresh.txt"), chosen);
	}

	[TestMethod]
	public void Thumbnails_AskTheHostForEachFileShown_AndADoubleClickedTileIsChosen()
	{
		List<string> asked = [];
		ImGuiAppTextureInfo? texture = null;
		browser.ThumbnailResolver = file =>
		{
			asked.Add(file.FileName.WeakString);
			texture ??= ImGuiApp.CreateTexture(Enumerable.Repeat((byte)200, 4 * 4 * 4).ToArray(), 4, 4);
			return new ImGuiPopups.FilesystemBrowserThumbnail(texture.TextureId, new Vector2(4, 2));
		};
		browser.View = ImGuiPopups.FilesystemBrowserView.Thumbnails;
		OpenAll();

		Assert.IsTrue(IsVisible(FolderRow("alpha")), "Folders were not drawn as tiles.");
		CollectionAssert.AreEquivalent(FilesInTheFolder, asked.Distinct().ToArray(), "The host was asked about something other than the files shown.");

		DoubleClick(Row("shot.png"));

		Assert.AreEqual(Path.Join(folder, "shot.png"), chosen);
	}

	[TestMethod]
	public void TheViewChoice_SwitchesBetweenTableAndTiles()
	{
		OpenAll();

		harness.Click("filesystem-browser/view/thumbnails");
		harness.Step(2);

		Assert.AreEqual(ImGuiPopups.FilesystemBrowserView.Thumbnails, browser.View);
		Assert.IsTrue(IsVisible("filesystem-browser/thumbnail-size"));
		Assert.IsFalse(IsVisible(Row("..")), "The tiles kept the table's parent row.");
	}

	[TestMethod]
	public void InAWindowTooSmallForItsDefaultSize_TheBrowserKeepsItsButtonsOnScreen()
	{
		// Fifty-six by twenty-eight lines of text is the browser's size when nothing else is asked for, which does not fit here.
		harness.Dispose();
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(FilesystemBrowserNavigationTests),
				SaveIniSettings = false,
				OnRender = _ => browser.ShowIfOpen(),
			},
			new HarnessOptions { Width = 640, Height = 360 });
		harness.Step(2);

		OpenAll();

		foreach (string button in new[] { "filesystem-browser/confirm", "filesystem-browser/cancel" })
		{
			Rectangle rect = harness.Probe.Rect(button) ?? throw new AssertFailedException($"{button} was never drawn.");
			Assert.IsLessThanOrEqualTo(640, rect.MaxX, $"{button} runs off the right of the window.");
			Assert.IsLessThanOrEqualTo(360, rect.MaxY, $"{button} runs off the bottom of the window.");
		}

		harness.Click("filesystem-browser/cancel");
		harness.Step(2);
		Assert.IsFalse(IsVisible("filesystem-browser/confirm"), "Cancel did not close the browser.");
	}
}

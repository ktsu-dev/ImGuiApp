// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.ImGui.WidgetGallery.UITests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ktsu.ImGui.WidgetGallery.Catalog;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the gallery tool end to end, so a tile that throws, asserts natively or leaves a dialog
/// open fails here rather than the next time someone regenerates the README images.
/// </summary>
[TestClass]
public sealed class WidgetGalleryTests
{
	private static readonly string[] SwitchTiles = ["switch.png", "toggleswitch.png"];

	private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	private string output = string.Empty;

	[TestInitialize]
	public void SetUp()
	{
		output = Path.Join(Path.GetTempPath(), "ktsu-widget-gallery-tests", Guid.NewGuid().ToString("N"));
	}

	[TestCleanup]
	public void TearDown()
	{
		if (Directory.Exists(output))
		{
			Directory.Delete(output, recursive: true);
		}
	}

	[TestMethod]
	public void Check_FindsATileForEveryWidget()
	{
		Assert.AreEqual(0, Program.Run(["--check"]));
	}

	[TestMethod]
	public void Help_Succeeds()
	{
		Assert.AreEqual(0, Program.Run(["--help"]));
	}

	[TestMethod]
	public void AnUnrecognisedArgument_IsRefused()
	{
		Assert.AreEqual(2, Program.Run(["--out"]));
		Assert.AreEqual(2, Program.Run(["gallery"]));
	}

	[TestMethod]
	public void TryParse_SeparatesOptionsFromFlags()
	{
		Assert.IsTrue(Program.TryParse(["--check", "--out", "dir", "--width", "800"], out Dictionary<string, string> options, out HashSet<string> flags));
		Assert.AreEqual("dir", options["--out"]);
		Assert.AreEqual("800", options["--width"]);
		Assert.IsTrue(flags.SetEquals(["--check"]));
	}

	[TestMethod]
	public void Only_CapturesTheMatchingTilesAndNoComposites()
	{
		Assert.AreEqual(0, Program.Run(["--out", output, "--only", "Switch"]));

		string[] tiles = [.. Directory.GetFiles(Path.Join(output, "widgets")).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];
		Assert.AreSequenceEqual(SwitchTiles, tiles);
		Assert.IsFalse(File.Exists(Path.Join(output, "widgets.png")));
	}

	[TestMethod]
	public void Render_WritesEveryTileAndEveryComposite()
	{
		Assert.AreEqual(0, Program.Run(["--out", output, "--width", "900"]));

		IReadOnlyList<GalleryEntry> catalog = GalleryCatalog.Build();

		foreach (GalleryEntry entry in catalog)
		{
			AssertIsPng(Path.Join(output, "widgets", entry.Slug + ".png"));
		}

		AssertIsPng(Path.Join(output, "widgets.png"));

		foreach (GalleryCategory category in catalog.Where(entry => entry.InComposite).Select(entry => entry.Category).Distinct())
		{
			AssertIsPng(Path.Join(output, "widgets-" + GalleryEntry.MakeSlug(GallerySettings.CategoryTitle(category)) + ".png"));
		}
	}

	private static void AssertIsPng(string path)
	{
		Assert.IsTrue(File.Exists(path), $"{path} was not written.");
		byte[] header = new byte[8];
		using FileStream stream = File.OpenRead(path);
		Assert.AreEqual(header.Length, stream.Read(header));
		Assert.AreSequenceEqual(PngSignature, header);
	}
}

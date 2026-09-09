// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PropertyGrid"/> on its own.</summary>
[TestClass]
public sealed class PropertyGridTests : WidgetTest
{
	private bool enabled;
	private int count = 3;
	private long total = 10L;
	private float ratio = 0.5f;
	private double precise = 1.5d;
	private string title = "hello";
	private Vector2 offset = new(1f, 2f);
	private Vector3 scale = new(1f, 1f, 1f);
	private ImGuiWidgets.DoubleVector2 measured = new(1d, 2d);
	private Color tint = Color.FromSrgb(1f, 0f, 0f, 1f);
	private string configPath = string.Empty;
	private string outputFolder = string.Empty;
	private string iconPath = string.Empty;
	private readonly List<string> tags = ["alpha", "beta"];
	private readonly List<bool> switches = [false, false];

	private ImGuiWidgets.PropertyGridOptions? options;
	private ImGuiAppTextureInfo? thumbnail;

	// A grid reports a change for the single frame it happened on, and a click renders a further
	// frame after the release, so the answer is latched rather than read back afterwards.
	private bool changed;

	private void Draw()
	{
		using ImGuiWidgets.PropertyGrid grid = new("Settings", options);

		grid.Value("Enabled", ref enabled);
		grid.Value("Count", ref count);
		grid.Value("Total", ref total);
		grid.Value("Ratio", ref ratio);
		grid.Value("Precise", ref precise);
		grid.Value("Title", ref title);
		grid.Value("Offset", ref offset);
		grid.Value("Scale", ref scale);
		grid.Value("Measured", ref measured);
		grid.Value("Tint", ref tint);
		grid.FilePath("Config", ref configPath);
		grid.DirectoryPath("Output", ref outputFolder);
		grid.ImagePath("Icon", ref iconPath);
		grid.List("Tags", tags);
		grid.List("Switches", switches);

		changed |= grid.Changed;
	}

	// Tall enough that the last row is still on screen, since a click lands where the item was
	// drawn rather than where the probe last recorded it.
	private static HarnessOptions TallViewport { get; } = new() { Width = 800, Height = 1400 };

	private void StartGrid() => Start(Draw, TallViewport);

	[TestMethod]
	public void PropertyGrid_DrawsOneRowPerProperty()
	{
		StartGrid();

		foreach (string row in (string[])["Enabled", "Count", "Total", "Ratio", "Precise", "Title", "Offset", "Scale", "Measured", "Tint", "Config", "Output", "Icon", "Tags", "Switches"])
		{
			Assert.IsTrue(IsVisible(row), $"The grid drew no row for '{row}'.");
		}
	}

	[TestMethod]
	public void PropertyGrid_ClickingABooleanRowTogglesIt()
	{
		StartGrid();

		Click("Enabled");

		Assert.IsTrue(enabled, "Clicking the checkbox did not set the value.");
	}

	[TestMethod]
	public void PropertyGrid_ReportsThatARowChanged()
	{
		StartGrid();

		changed = false;
		Click("Enabled");

		Assert.IsTrue(changed, "The grid did not report the row that changed.");
	}

	[TestMethod]
	public void PropertyGrid_ReportsNoChangeOnAQuietFrame()
	{
		StartGrid();

		changed = false;
		Step(2);

		Assert.IsFalse(changed, "The grid reported a change on a frame where nothing was touched.");
	}

	[TestMethod]
	public void PropertyGrid_TextRowEditsItsValue()
	{
		StartGrid();

		Click("Title");
		Harness.Keyboard.Type("!");
		Step(2);

		Assert.AreEqual("hello!", title, "The text row did not take the typed character.");
	}

	[TestMethod]
	public void PropertyGrid_ReadOnlyGridDoesNotEdit()
	{
		options = new ImGuiWidgets.PropertyGridOptions { ReadOnly = true };
		StartGrid();

		changed = false;
		Click("Enabled");

		Assert.IsFalse(enabled, "A read-only grid let the checkbox be toggled.");
		Assert.IsFalse(changed, "A read-only grid reported a change.");
	}

	[TestMethod]
	public void PropertyGrid_BrowseButtonAsksTheHostAndAdoptsTheAnswer()
	{
		ImGuiWidgets.PropertyPathRequest? asked = null;
		options = new ImGuiWidgets.PropertyGridOptions { OnBrowse = request => asked = request };
		StartGrid();

		Click("Config/browse");

		Assert.IsNotNull(asked, "The browse button raised no request.");
		Assert.AreEqual("Config", asked.Label);
		Assert.AreEqual(ImGuiWidgets.PropertyPathKind.File, asked.Kind);

		// The host answers whenever its dialog closes, which is always some later frame.
		asked.Complete("/tmp/settings.json");
		Step(2);

		Assert.AreEqual("/tmp/settings.json", configPath, "The row did not adopt the chosen path.");
	}

	[TestMethod]
	public void PropertyGrid_BrowseAsksForTheKindTheRowEdits()
	{
		List<ImGuiWidgets.PropertyPathKind> kinds = [];
		options = new ImGuiWidgets.PropertyGridOptions { OnBrowse = request => kinds.Add(request.Kind) };
		StartGrid();

		Click("Output/browse");
		Click("Icon/browse");

		CollectionAssert.AreEqual(
			(ImGuiWidgets.PropertyPathKind[])[ImGuiWidgets.PropertyPathKind.Directory, ImGuiWidgets.PropertyPathKind.Image],
			kinds);
	}

	[TestMethod]
	public void PropertyGrid_CancelledBrowseLeavesThePathAlone()
	{
		ImGuiWidgets.PropertyPathRequest? asked = null;
		options = new ImGuiWidgets.PropertyGridOptions { OnBrowse = request => asked = request };
		configPath = "/tmp/original.json";
		StartGrid();

		Click("Config/browse");
		asked!.Complete(null);
		Step(2);

		Assert.AreEqual("/tmp/original.json", configPath, "A cancelled browse cleared the path.");
	}

	[TestMethod]
	public void PropertyGrid_ImageRowDrawsTheResolvedThumbnail()
	{
		string? resolved = null;
		options = new ImGuiWidgets.PropertyGridOptions
		{
			// Lazily, because a texture can only be created once the application is running.
			ThumbnailResolver = path =>
			{
				resolved = path;
				thumbnail ??= CreateTestTexture();
				return thumbnail.TextureId;
			},
		};
		iconPath = "/tmp/icon.png";
		StartGrid();

		Assert.AreEqual("/tmp/icon.png", resolved, "The row did not ask the resolver about its path.");
		Assert.IsTrue(IsVisible("Icon/thumbnail"), "The image row drew no thumbnail.");
	}

	[TestMethod]
	public void PropertyGrid_ImageRowKeepsItsPreviewWithoutATexture()
	{
		StartGrid();

		Assert.IsTrue(IsVisible("Icon/thumbnail"), "The image row drew nothing where its preview goes.");
	}

	[TestMethod]
	public void PropertyGrid_ListDrawsAnEditorPerElement()
	{
		StartGrid();

		Assert.IsTrue(IsVisible("Tags/[0]"), "The list drew no editor for its first element.");
		Assert.IsTrue(IsVisible("Tags/[1]"), "The list drew no editor for its second element.");
	}

	[TestMethod]
	public void PropertyGrid_ListAddAppendsAnElement()
	{
		StartGrid();

		changed = false;
		Click("Tags/add");

		Assert.AreEqual(3, tags.Count, "The add button appended nothing.");
		Assert.AreEqual(string.Empty, tags[2], "The appended element was not the empty default.");
		Assert.IsTrue(changed, "Appending an element was not reported as a change.");
	}

	[TestMethod]
	public void PropertyGrid_ListRemoveDropsThatElement()
	{
		StartGrid();

		changed = false;
		Click("Switches/[0]/remove");

		Assert.AreEqual(1, switches.Count, "The remove button dropped nothing.");
		Assert.IsTrue(changed, "Removing an element was not reported as a change.");
	}

	[TestMethod]
	public void PropertyGrid_ListElementEditsWriteBack()
	{
		StartGrid();

		Click("Switches/[1]");

		Assert.IsTrue(switches[1], "Editing an element did not write back into the list.");
	}

	[TestMethod]
	public void PropertyGrid_ImagePathListDrawsAPreviewPerElement()
	{
		List<string> frames = ["/tmp/first.png", "/tmp/second.png"];
		Start(() =>
		{
			using ImGuiWidgets.PropertyGrid grid = new("Frames");
			grid.ImagePathList("Frames", frames);
		});

		Assert.IsTrue(IsVisible("Frames/[0]/thumbnail"), "The first image element drew no preview.");
		Assert.IsTrue(IsVisible("Frames/[1]/thumbnail"), "The second image element drew no preview.");

		Click("Frames/[0]/remove");

		Assert.AreEqual(1, frames.Count, "Removing an image element did nothing.");
	}

	[TestMethod]
	public void PropertyGrid_CollapsedListHidesItsElements()
	{
		StartGrid();

		Click("Tags");

		Assert.IsFalse(IsVisible("Tags/[0]"), "A collapsed list still drew its elements.");
	}

	[TestMethod]
	public void PropertyGrid_SectionHidesItsRowsWhenCollapsed()
	{
		bool inner = false;
		Start(() =>
		{
			using ImGuiWidgets.PropertyGrid grid = new("Sections");
			if (grid.Section("Transform"))
			{
				grid.Value("Inner", ref inner);
				grid.EndSection();
			}
		});

		Assert.IsTrue(IsVisible("Inner"), "A section that starts open drew none of its rows.");

		Click("Transform");

		Assert.IsFalse(IsVisible("Inner"), "A collapsed section still drew its rows.");
	}

	[TestMethod]
	public void PropertyGrid_DrawsSomethingForEveryRow()
	{
		StartGrid();

		AssertSomethingWasDrawn("the property grid");
	}
}

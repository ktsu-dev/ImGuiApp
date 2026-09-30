// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Toolbar</c> and <c>ImGuiWidgets.ToolbarSeparator</c> on their own.</summary>
[TestClass]
public sealed class ToolbarTests : WidgetTest
{
	private const string Strip = "Tools";

	private ToolbarButtonLayout layout = ToolbarButtonLayout.GlyphLeading;
	private float? glyphSize;
	private bool separator;
	private bool openClicked;
	private bool saveClicked;

	private void DrawToolbar()
	{
		using (ImGuiWidgets.Toolbar(Strip, new ToolbarOptions { Layout = layout, GlyphSize = glyphSize }))
		{
			openClicked |= ImGuiWidgets.ToolbarButton("Open", "O");
			if (separator)
			{
				ImGuiWidgets.ToolbarSeparator();
			}

			saveClicked |= ImGuiWidgets.ToolbarButton("Save", "S");
		}

		Hexa.NET.ImGui.ImGui.TextUnformatted("after");
	}

	[TestMethod]
	public void Toolbar_MarksItsRegion()
	{
		Start(DrawToolbar);

		Assert.IsTrue(IsVisible(Strip), "The toolbar marked no probe region.");
		AssertSomethingWasDrawn("the toolbar");
	}

	[TestMethod]
	public void Toolbar_LaysItsButtonsOutOnOneRow()
	{
		Start(DrawToolbar);

		Rectangle open = RectOf($"{Strip}/Open");
		Rectangle save = RectOf($"{Strip}/Save");
		Rectangle strip = RectOf(Strip);

		Assert.AreEqual(open.MinY, save.MinY, "The buttons were not on one row.");
		Assert.IsTrue(save.MinX > open.MaxX, "Save was not to the right of Open.");
		Assert.IsTrue(open.MinY >= strip.MinY && open.MaxY <= strip.MaxY, "A button sat outside the strip.");
	}

	[TestMethod]
	public void Toolbar_ButtonsReportTheirOwnClicks()
	{
		Start(DrawToolbar);

		Click($"{Strip}/Save");

		Assert.IsTrue(saveClicked, "Save did not report its click.");
		Assert.IsFalse(openClicked, "Open reported a click made on Save.");
	}

	[TestMethod]
	public void ToolbarSeparator_WidensTheGapBetweenGroups()
	{
		Start(DrawToolbar);
		int plainGap = RectOf($"{Strip}/Save").MinX - RectOf($"{Strip}/Open").MaxX;

		separator = true;
		Step(2);
		int separatedGap = RectOf($"{Strip}/Save").MinX - RectOf($"{Strip}/Open").MaxX;

		Assert.IsTrue(separatedGap > plainGap, $"The gap was {plainGap}px without a separator and {separatedGap}px with one.");
	}

	[TestMethod]
	public void ToolbarSeparator_OutsideAToolbarDoesNothing()
	{
		bool drawSeparator = false;
		Start(() =>
		{
			if (drawSeparator)
			{
				ImGuiWidgets.ToolbarSeparator();
			}
		});
		byte[] blank = Snapshot();

		drawSeparator = true;
		Step(2);

		Assert.AreEqual(0, PixelsChangedSince(blank), "A separator outside a toolbar drew something.");
	}

	[TestMethod]
	public void GlyphAboveToolbar_IsTallerWithTallerButtons()
	{
		Start(DrawToolbar);
		Rectangle leadingStrip = RectOf(Strip);
		Rectangle leadingButton = RectOf($"{Strip}/Open");

		layout = ToolbarButtonLayout.GlyphAbove;
		Step(2);
		Rectangle aboveStrip = RectOf(Strip);
		Rectangle aboveButton = RectOf($"{Strip}/Open");

		Assert.IsTrue(aboveStrip.Height > leadingStrip.Height, "A GlyphAbove toolbar was no taller than a single-line one.");
		Assert.IsTrue(aboveButton.Height > leadingButton.Height, "Buttons in a GlyphAbove toolbar were no taller.");
	}

	[TestMethod]
	public void GlyphAboveButton_InASingleLineToolbar_StaysOnOneLine()
	{
		Start(() =>
		{
			using (ImGuiWidgets.Toolbar(Strip))
			{
				ImGuiWidgets.ToolbarButton("Open", "O", new ToolbarButtonOptions { Layout = ToolbarButtonLayout.GlyphAbove });
			}

			Hexa.NET.ImGui.ImGui.TextUnformatted("after");
		});

		Rectangle button = RectOf($"{Strip}/Open");
		Rectangle strip = RectOf(Strip);

		Assert.IsTrue(button.MaxY <= strip.MaxY, "A GlyphAbove button overflowed a single-line toolbar.");
	}

	[TestMethod]
	public void GlyphSize_GrowsTheRowAndKeepsItsButtonsInside()
	{
		layout = ToolbarButtonLayout.GlyphAbove;
		Start(DrawToolbar);
		Rectangle normalStrip = RectOf(Strip);

		glyphSize = 40.0f;
		Step(2);
		Rectangle largeStrip = RectOf(Strip);
		Rectangle button = RectOf($"{Strip}/Open");

		Assert.IsTrue(largeStrip.Height > normalStrip.Height, $"A 40px glyph left the strip {largeStrip.Height}px tall against {normalStrip.Height}px.");
		Assert.IsTrue(button.MaxY <= largeStrip.MaxY && button.MinY >= largeStrip.MinY, "A button with a large glyph overflowed its toolbar.");
	}

	[TestMethod]
	public void GlyphSize_GrowsASingleLineRowToFitTheGlyph()
	{
		Start(DrawToolbar);
		Rectangle normalStrip = RectOf(Strip);

		glyphSize = 40.0f;
		Step(2);
		Rectangle largeStrip = RectOf(Strip);
		Rectangle button = RectOf($"{Strip}/Open");

		Assert.IsTrue(largeStrip.Height > normalStrip.Height, "A 40px glyph did not grow a single-line toolbar.");
		Assert.IsTrue(button.MaxY <= largeStrip.MaxY, "A button with a large glyph overflowed a single-line toolbar.");
	}
}

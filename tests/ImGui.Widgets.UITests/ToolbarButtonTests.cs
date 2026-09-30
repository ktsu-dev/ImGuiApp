// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.ToolbarButton</c> and <c>ImGuiWidgets.ToolbarToggleButton</c> on their own.</summary>
[TestClass]
public sealed class ToolbarButtonTests : WidgetTest
{
	private const string Label = "Save";
	private const string Glyph = "+";

	private bool clicked;
	private bool toggled;
	private bool enabled = true;
	private ToolbarButtonLayout layout = ToolbarButtonLayout.GlyphLeading;

	// Latched: a click is reported for the single frame it happens on.
	private void DrawButton() =>
		clicked |= ImGuiWidgets.ToolbarButton(Label, Glyph, new ToolbarButtonOptions { Layout = layout, Enabled = enabled });

	private void DrawToggle() => ImGuiWidgets.ToolbarToggleButton(Label, Glyph, ref toggled);

	[TestMethod]
	public void ToolbarButton_IsDrawnAndMarksItself()
	{
		Start(DrawButton);

		Assert.IsTrue(IsVisible(Label), "The toolbar button marked no probe item.");
		AssertSomethingWasDrawn("the toolbar button");
	}

	[TestMethod]
	public void ToolbarButton_ReportsAClick()
	{
		Start(DrawButton);

		Click(Label);

		Assert.IsTrue(clicked, "The toolbar button did not report being clicked.");
	}

	[TestMethod]
	public void ToolbarButton_ReportsNoClickWhenLeftAlone()
	{
		Start(DrawButton);

		Step(3);

		Assert.IsFalse(clicked, "The toolbar button reported a click nobody made.");
	}

	[TestMethod]
	public void ToolbarButton_DisabledIgnoresAClick()
	{
		enabled = false;
		Start(DrawButton);

		Click(Label);

		Assert.IsFalse(clicked, "A disabled toolbar button reported a click.");
	}

	[TestMethod]
	public void ToolbarToggleButton_FlipsItsValue()
	{
		Start(DrawToggle);

		Click(Label);
		Assert.IsTrue(toggled, "The first click did not turn the toggle on.");

		Click(Label);
		Assert.IsFalse(toggled, "The second click did not turn the toggle off.");
	}

	[TestMethod]
	public void ToolbarToggleButton_OnDrawsDifferently()
	{
		Start(DrawToggle);
		MoveAway();
		byte[] off = Snapshot();

		toggled = true;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(off) > 0, "A toggled-on button drew the same as a toggled-off one.");
	}

	[TestMethod]
	public void GlyphOnly_ShowsTheLabelAsATooltip()
	{
		layout = ToolbarButtonLayout.GlyphOnly;
		Start(DrawButton);

		Hover(Label);
		Step(30);

		Assert.IsTrue(IsVisible($"{Label}/tooltip"), "A glyph-only button showed no tooltip naming it.");
	}

	[TestMethod]
	public void GlyphLeading_ShowsNoTooltipByDefault()
	{
		Start(DrawButton);

		Hover(Label);
		Step(30);

		Assert.IsFalse(IsVisible($"{Label}/tooltip"), "A button already showing its label also showed it as a tooltip.");
	}

	[TestMethod]
	public void GlyphAbove_IsTallerAndNarrowerThanGlyphLeading()
	{
		Start(DrawButton);
		Rectangle leading = RectOf(Label);

		layout = ToolbarButtonLayout.GlyphAbove;
		Step(2);
		Rectangle above = RectOf(Label);

		Assert.IsTrue(above.Height > leading.Height, $"GlyphAbove was {above.Height}px tall against GlyphLeading's {leading.Height}px.");
		Assert.IsTrue(above.Width < leading.Width, $"GlyphAbove was {above.Width}px wide against GlyphLeading's {leading.Width}px.");
	}

	[TestMethod]
	public void GlyphAbove_ReportsAClick()
	{
		layout = ToolbarButtonLayout.GlyphAbove;
		Start(DrawButton);

		Click(Label);

		Assert.IsTrue(clicked, "The two-line button did not report being clicked.");
	}
}

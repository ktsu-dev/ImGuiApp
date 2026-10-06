// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ToolbarGeometryTests
{
	// A 13px font with the default style: FramePadding (4,3), ItemInnerSpacing (4,4), frame height 19.
	private static readonly Vector2 Glyph = new(13, 13);
	private static readonly Vector2 Label = new(40, 13);
	private static readonly Vector2 Pad = new(4, 3);
	private static readonly Vector2 Inner = new(4, 4);
	private const float FrameHeight = 19;

	private static ToolbarButtonMetrics Measure(ToolbarButtonLayout layout, Vector2? glyph = null, float minWidth = 0, float rowHeight = 0) =>
		ToolbarGeometry.Measure(glyph ?? Glyph, Label, layout, Pad, Inner, FrameHeight, minWidth, rowHeight);

	[TestMethod]
	public void GlyphLeading_PutsTheLabelAfterTheGlyphOnOneLine()
	{
		ToolbarButtonMetrics metrics = Measure(ToolbarButtonLayout.GlyphLeading);

		Assert.AreEqual(new Vector2(65, 19), metrics.Size);
		Assert.AreEqual(new Vector2(4, 3), metrics.GlyphOffset);
		Assert.AreEqual(new Vector2(21, 3), metrics.LabelOffset);
	}

	[TestMethod]
	public void GlyphAbove_CentresTheGlyphOverTheLabel()
	{
		ToolbarButtonMetrics metrics = Measure(ToolbarButtonLayout.GlyphAbove);

		Assert.AreEqual(new Vector2(48, 36), metrics.Size);
		Assert.AreEqual(new Vector2(17.5f, 3), metrics.GlyphOffset);
		Assert.AreEqual(new Vector2(4, 20), metrics.LabelOffset);
	}

	[TestMethod]
	public void GlyphAbove_CentresTheLabelUnderAWiderGlyph()
	{
		ToolbarButtonMetrics metrics = ToolbarGeometry.Measure(new Vector2(60, 13), Label,
			ToolbarButtonLayout.GlyphAbove, Pad, Inner, FrameHeight, 0, 0);

		Assert.AreEqual(68, metrics.Size.X);
		Assert.AreEqual(4, metrics.GlyphOffset.X);
		Assert.AreEqual(14, metrics.LabelOffset.X);
	}

	[TestMethod]
	public void GlyphOnly_IsAtLeastSquare()
	{
		Assert.AreEqual(new Vector2(21, 19), Measure(ToolbarButtonLayout.GlyphOnly).Size);
		Assert.AreEqual(new Vector2(4, 3), Measure(ToolbarButtonLayout.GlyphOnly).GlyphOffset);

		ToolbarButtonMetrics narrow = Measure(ToolbarButtonLayout.GlyphOnly, glyph: new Vector2(8, 13));
		Assert.AreEqual(19, narrow.Size.X);
		Assert.AreEqual(5.5f, narrow.GlyphOffset.X);
	}

	[TestMethod]
	public void LabelOnly_IsAPaddedLabel()
	{
		ToolbarButtonMetrics metrics = Measure(ToolbarButtonLayout.LabelOnly);

		Assert.AreEqual(new Vector2(48, 19), metrics.Size);
		Assert.AreEqual(new Vector2(4, 3), metrics.LabelOffset);
	}

	[TestMethod]
	public void MinWidth_CentresTheContentInTheExtra()
	{
		ToolbarButtonMetrics metrics = Measure(ToolbarButtonLayout.GlyphLeading, minWidth: 80);

		Assert.AreEqual(new Vector2(80, 19), metrics.Size);
		Assert.AreEqual(new Vector2(11.5f, 3), metrics.GlyphOffset);
		Assert.AreEqual(new Vector2(28.5f, 3), metrics.LabelOffset);
	}

	[TestMethod]
	public void MinWidth_NeverShrinksAButton()
	{
		Assert.AreEqual(new Vector2(65, 19), Measure(ToolbarButtonLayout.GlyphLeading, minWidth: 10).Size);
	}

	[TestMethod]
	public void RowHeight_CentresASingleLineButtonInATallRow()
	{
		ToolbarButtonMetrics metrics = Measure(ToolbarButtonLayout.GlyphLeading, rowHeight: 36);

		Assert.AreEqual(new Vector2(65, 36), metrics.Size);
		Assert.AreEqual(new Vector2(4, 11.5f), metrics.GlyphOffset);
	}

	[TestMethod]
	public void NeitherPart_IsStillAClickableFrame()
	{
		ToolbarButtonMetrics metrics = ToolbarGeometry.Measure(Vector2.Zero, Vector2.Zero,
			ToolbarButtonLayout.LabelOnly, Pad, Inner, FrameHeight, 0, 0);

		Assert.AreEqual(new Vector2(8, 19), metrics.Size);
	}

	[TestMethod]
	public void RowHeight_IsTwoLinesOnlyForAGlyphAboveToolbar()
	{
		Assert.AreEqual(36, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphAbove, 13, Pad, Inner, FrameHeight));
		Assert.AreEqual(19, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphLeading, 13, Pad, Inner, FrameHeight));
		Assert.AreEqual(19, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphOnly, 13, Pad, Inner, FrameHeight));
	}

	[TestMethod]
	public void RowHeight_MakesRoomForALargerGlyphLine()
	{
		// 3 + 26 + 4 + 13 + 3: the glyph line grows, the label line does not.
		Assert.AreEqual(49, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphAbove, 13, 26, Pad, Inner, FrameHeight));
		Assert.AreEqual(32, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphLeading, 13, 26, Pad, Inner, FrameHeight));
	}

	[TestMethod]
	public void RowHeight_NeverShrinksBelowAFrameForASmallerGlyph()
	{
		Assert.AreEqual(FrameHeight, ToolbarGeometry.RowHeight(ToolbarButtonLayout.GlyphLeading, 13, 8, Pad, Inner, FrameHeight));
	}

	[TestMethod]
	public void SeparatorWidth_IsALineWithSpacingEitherSide() =>
		Assert.AreEqual(9, ToolbarGeometry.SeparatorWidth(Inner));

	[TestMethod]
	[DataRow(ToolbarButtonLayout.GlyphLeading, true, true, ToolbarButtonLayout.GlyphLeading)]
	[DataRow(ToolbarButtonLayout.GlyphAbove, true, true, ToolbarButtonLayout.GlyphAbove)]
	[DataRow(ToolbarButtonLayout.GlyphLeading, false, true, ToolbarButtonLayout.LabelOnly)]
	[DataRow(ToolbarButtonLayout.GlyphAbove, false, true, ToolbarButtonLayout.LabelOnly)]
	[DataRow(ToolbarButtonLayout.GlyphLeading, true, false, ToolbarButtonLayout.GlyphOnly)]
	[DataRow(ToolbarButtonLayout.GlyphAbove, true, false, ToolbarButtonLayout.GlyphOnly)]
	[DataRow(ToolbarButtonLayout.GlyphOnly, false, true, ToolbarButtonLayout.LabelOnly)]
	[DataRow(ToolbarButtonLayout.LabelOnly, true, false, ToolbarButtonLayout.GlyphOnly)]
	[DataRow(ToolbarButtonLayout.GlyphOnly, false, false, ToolbarButtonLayout.LabelOnly)]
	public void Resolve_FallsBackToThePartThatIsPresent(ToolbarButtonLayout requested, bool hasGlyph, bool hasLabel, ToolbarButtonLayout expected) =>
		Assert.AreEqual(expected, ToolbarGeometry.Resolve(requested, hasGlyph, hasLabel));

	[TestMethod]
	public void ResolveInToolbar_FlattensGlyphAboveInASingleLineStrip()
	{
		Assert.AreEqual(ToolbarButtonLayout.GlyphLeading,
			ToolbarGeometry.ResolveInToolbar(ToolbarButtonLayout.GlyphAbove, ToolbarButtonLayout.GlyphLeading));
		Assert.AreEqual(ToolbarButtonLayout.GlyphAbove,
			ToolbarGeometry.ResolveInToolbar(ToolbarButtonLayout.GlyphAbove, ToolbarButtonLayout.GlyphAbove));
		Assert.AreEqual(ToolbarButtonLayout.GlyphOnly,
			ToolbarGeometry.ResolveInToolbar(ToolbarButtonLayout.GlyphOnly, ToolbarButtonLayout.GlyphAbove));
	}

	[TestMethod]
	public void GlyphInkCorrection_CentresAGlyphDrawnHighInItsSlot()
	{
		// An icon drawing in rows 1-9 of a 14px slot is 2.5px above centre; it moves down by that, rounded.
		Vector2 correction = ToolbarGeometry.GlyphInkCorrection(new Vector2(14, 14), new Vector2(1, 1), new Vector2(13, 9), ToolbarButtonLayout.GlyphAbove);

		Assert.AreEqual(new Vector2(0, 2), correction);
	}

	[TestMethod]
	public void GlyphInkCorrection_CentresHorizontallyOnlyWhereTheGlyphIsCentred()
	{
		Vector2 slot = new(14, 14);
		Vector2 inkMin = new(0, 3);
		Vector2 inkMax = new(10, 11);

		Assert.AreEqual(new Vector2(2, 0), ToolbarGeometry.GlyphInkCorrection(slot, inkMin, inkMax, ToolbarButtonLayout.GlyphOnly));
		Assert.AreEqual(new Vector2(2, 0), ToolbarGeometry.GlyphInkCorrection(slot, inkMin, inkMax, ToolbarButtonLayout.GlyphAbove));
		Assert.AreEqual(Vector2.Zero, ToolbarGeometry.GlyphInkCorrection(slot, inkMin, inkMax, ToolbarButtonLayout.GlyphLeading));
	}

	[TestMethod]
	public void GlyphInkCorrection_LeavesAGlyphThatDrawsNothingWhereItIs() =>
		Assert.AreEqual(Vector2.Zero, ToolbarGeometry.GlyphInkCorrection(new Vector2(14, 14), new Vector2(float.MaxValue), new Vector2(float.MinValue), ToolbarButtonLayout.GlyphOnly));
}

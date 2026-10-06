// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>Where a toolbar button places its glyph relative to its label.</summary>
public enum ToolbarButtonLayout
{
	/// <summary>Glyph on the left, label on the right, on one line. The default.</summary>
	GlyphLeading,

	/// <summary>Glyph centred on a line above the label, which is centred beneath it.</summary>
	GlyphAbove,

	/// <summary>Glyph alone. The visible label becomes the tooltip when no tooltip is set.</summary>
	GlyphOnly,

	/// <summary>Label alone. The glyph is not drawn.</summary>
	LabelOnly,
}

/// <summary>
/// The measured size of a toolbar button, and where its glyph and label sit inside it.
/// </summary>
/// <param name="Size">The button's outer size in pixels.</param>
/// <param name="GlyphOffset">The glyph's top-left corner, relative to the button's top-left.</param>
/// <param name="LabelOffset">The label's top-left corner, relative to the button's top-left.</param>
/// <param name="Layout">The layout the button is actually drawn with, after missing parts have collapsed it.</param>
public readonly record struct ToolbarButtonMetrics(Vector2 Size, Vector2 GlyphOffset, Vector2 LabelOffset, ToolbarButtonLayout Layout);

/// <summary>
/// The layout arithmetic behind <see cref="ImGuiWidgets.ToolbarButton"/> and <see cref="ImGuiWidgets.Toolbar"/>.
/// It takes plain sizes rather than an ImGui context, so every number here is testable without one.
/// </summary>
public static class ToolbarGeometry
{
	/// <summary>
	/// Collapses a requested layout to one that can be drawn with the parts that are present. A layout
	/// asking for a glyph with no glyph to draw, or a label with no label, falls back to the part
	/// there is; with neither, the result is <see cref="ToolbarButtonLayout.LabelOnly"/> over an empty
	/// label, which is still a clickable padded frame.
	/// </summary>
	/// <param name="requested">The layout asked for.</param>
	/// <param name="hasGlyph">Whether there is a glyph to draw.</param>
	/// <param name="hasLabel">Whether there is visible label text to draw.</param>
	/// <returns>The layout to draw with.</returns>
	public static ToolbarButtonLayout Resolve(ToolbarButtonLayout requested, bool hasGlyph, bool hasLabel)
	{
		if (!hasGlyph && !hasLabel)
		{
			return ToolbarButtonLayout.LabelOnly;
		}

		return requested switch
		{
			ToolbarButtonLayout.GlyphLeading or ToolbarButtonLayout.GlyphAbove when !hasGlyph => ToolbarButtonLayout.LabelOnly,
			ToolbarButtonLayout.GlyphLeading or ToolbarButtonLayout.GlyphAbove when !hasLabel => ToolbarButtonLayout.GlyphOnly,
			ToolbarButtonLayout.GlyphOnly when !hasGlyph => ToolbarButtonLayout.LabelOnly,
			ToolbarButtonLayout.LabelOnly when !hasLabel => ToolbarButtonLayout.GlyphOnly,
			_ => requested,
		};
	}

	/// <summary>
	/// Applies a toolbar's layout to a button inside it. A single-line strip has no room for a button
	/// two rows tall, so <see cref="ToolbarButtonLayout.GlyphAbove"/> becomes
	/// <see cref="ToolbarButtonLayout.GlyphLeading"/> there; every other request is kept.
	/// </summary>
	/// <param name="requested">The layout the button asked for.</param>
	/// <param name="toolbarLayout">The enclosing toolbar's layout.</param>
	/// <returns>The layout the button is drawn with.</returns>
	public static ToolbarButtonLayout ResolveInToolbar(ToolbarButtonLayout requested, ToolbarButtonLayout toolbarLayout) =>
		requested == ToolbarButtonLayout.GlyphAbove && toolbarLayout != ToolbarButtonLayout.GlyphAbove
			? ToolbarButtonLayout.GlyphLeading
			: requested;

	/// <summary>
	/// Measures a toolbar button and places its glyph and label inside it.
	/// </summary>
	/// <param name="glyphSize">The glyph's text size.</param>
	/// <param name="labelSize">The visible label's text size.</param>
	/// <param name="layout">The layout, already resolved with <see cref="Resolve"/>.</param>
	/// <param name="framePadding">The padding between the frame and its content.</param>
	/// <param name="innerSpacing">The gap between glyph and label.</param>
	/// <param name="frameHeight">The standard frame height, which a single-line button is never shorter than.</param>
	/// <param name="minWidth">A minimum width; content is centred in any extra.</param>
	/// <param name="rowHeight">A minimum height, the enclosing toolbar's row height; content is centred in any extra. Zero outside a toolbar.</param>
	/// <returns>The button's metrics.</returns>
	public static ToolbarButtonMetrics Measure(Vector2 glyphSize, Vector2 labelSize, ToolbarButtonLayout layout,
		Vector2 framePadding, Vector2 innerSpacing, float frameHeight, float minWidth, float rowHeight)
	{
		Vector2 size;
		Vector2 glyphOffset;
		Vector2 labelOffset;

		switch (layout)
		{
			case ToolbarButtonLayout.GlyphAbove:
			{
				float contentWidth = MathF.Max(glyphSize.X, labelSize.X);
				size = new Vector2(
					contentWidth + (2.0f * framePadding.X),
					framePadding.Y + glyphSize.Y + innerSpacing.Y + labelSize.Y + framePadding.Y);
				glyphOffset = new Vector2(framePadding.X + ((contentWidth - glyphSize.X) * 0.5f), framePadding.Y);
				labelOffset = new Vector2(framePadding.X + ((contentWidth - labelSize.X) * 0.5f), framePadding.Y + glyphSize.Y + innerSpacing.Y);
				break;
			}

			case ToolbarButtonLayout.GlyphOnly:
			{
				float height = SingleLineHeight(glyphSize.Y, glyphSize.Y, framePadding, frameHeight);
				size = new Vector2(MathF.Max(glyphSize.X + (2.0f * framePadding.X), frameHeight), height);
				glyphOffset = new Vector2((size.X - glyphSize.X) * 0.5f, (height - glyphSize.Y) * 0.5f);
				labelOffset = Vector2.Zero;
				break;
			}

			case ToolbarButtonLayout.LabelOnly:
			{
				float height = SingleLineHeight(labelSize.Y, labelSize.Y, framePadding, frameHeight);
				size = new Vector2(labelSize.X + (2.0f * framePadding.X), height);
				glyphOffset = Vector2.Zero;
				labelOffset = new Vector2(framePadding.X, (height - labelSize.Y) * 0.5f);
				break;
			}

			default:
			{
				float height = SingleLineHeight(glyphSize.Y, labelSize.Y, framePadding, frameHeight);
				size = new Vector2(glyphSize.X + innerSpacing.X + labelSize.X + (2.0f * framePadding.X), height);
				glyphOffset = new Vector2(framePadding.X, (height - glyphSize.Y) * 0.5f);
				labelOffset = new Vector2(framePadding.X + glyphSize.X + innerSpacing.X, (height - labelSize.Y) * 0.5f);
				break;
			}
		}

		Vector2 shift = Vector2.Zero;
		if (minWidth > size.X)
		{
			shift.X = (minWidth - size.X) * 0.5f;
			size.X = minWidth;
		}

		if (rowHeight > size.Y)
		{
			shift.Y = (rowHeight - size.Y) * 0.5f;
			size.Y = rowHeight;
		}

		return new ToolbarButtonMetrics(size, glyphOffset + shift, labelOffset + shift, layout);
	}

	/// <summary>
	/// Gets the height of a toolbar's button row: two lines tall for a
	/// <see cref="ToolbarButtonLayout.GlyphAbove"/> toolbar, one frame tall otherwise.
	/// </summary>
	/// <param name="toolbarLayout">The toolbar's layout.</param>
	/// <param name="lineHeight">The height of one line of text, used for both glyph and label.</param>
	/// <param name="framePadding">The padding between a button frame and its content.</param>
	/// <param name="innerSpacing">The gap between glyph and label.</param>
	/// <param name="frameHeight">The standard frame height.</param>
	/// <returns>The row height in pixels.</returns>
	public static float RowHeight(ToolbarButtonLayout toolbarLayout, float lineHeight, Vector2 framePadding, Vector2 innerSpacing, float frameHeight) =>
		RowHeight(toolbarLayout, lineHeight, lineHeight, framePadding, innerSpacing, frameHeight);

	/// <summary>
	/// Gets the height of a toolbar's button row when its glyphs are drawn at a different size from
	/// its labels: a glyph line over a label line for a <see cref="ToolbarButtonLayout.GlyphAbove"/>
	/// toolbar, otherwise one frame tall or the glyph line and padding, whichever is taller.
	/// </summary>
	/// <param name="toolbarLayout">The toolbar's layout.</param>
	/// <param name="lineHeight">The height of one line of label text.</param>
	/// <param name="glyphLineHeight">The height of one line of text at the glyph size.</param>
	/// <param name="framePadding">The padding between a button frame and its content.</param>
	/// <param name="innerSpacing">The gap between glyph and label.</param>
	/// <param name="frameHeight">The standard frame height.</param>
	/// <returns>The row height in pixels.</returns>
	public static float RowHeight(ToolbarButtonLayout toolbarLayout, float lineHeight, float glyphLineHeight, Vector2 framePadding, Vector2 innerSpacing, float frameHeight) =>
		toolbarLayout == ToolbarButtonLayout.GlyphAbove
			? framePadding.Y + glyphLineHeight + innerSpacing.Y + lineHeight + framePadding.Y
			: MathF.Max(frameHeight, glyphLineHeight + (2.0f * framePadding.Y));

	/// <summary>Gets the width a toolbar separator reserves: a one-pixel line with inner spacing either side.</summary>
	/// <param name="innerSpacing">The inner item spacing.</param>
	/// <returns>The width in pixels.</returns>
	public static float SeparatorWidth(Vector2 innerSpacing) => (2.0f * innerSpacing.X) + 1.0f;

	/// <summary>
	/// Gets how far to move a glyph from where <see cref="Measure"/> placed its text box so that its
	/// drawn pixels, rather than its text box, sit centred there. An icon font merged into a text font
	/// keeps its own vertical metrics, so an icon usually draws well above the middle of the line it
	/// occupies; centring its text box leaves it visibly high beside a label and leaves a gap under it
	/// above one. Vertical centring always applies; horizontal centring only where the glyph is centred
	/// in the button, since a leading glyph keeps its advance so the gap to its label stays the style's.
	/// </summary>
	/// <param name="textSize">The glyph size <see cref="Measure"/> was given, which is the slot the drawn pixels are centred in.</param>
	/// <param name="inkMin">The top-left of the glyph's drawn pixels, relative to its text box.</param>
	/// <param name="inkMax">The bottom-right of the glyph's drawn pixels, relative to its text box.</param>
	/// <param name="layout">The resolved layout.</param>
	/// <returns>The correction, or zero when the glyph draws nothing.</returns>
	public static Vector2 GlyphInkCorrection(Vector2 textSize, Vector2 inkMin, Vector2 inkMax, ToolbarButtonLayout layout)
	{
		if (inkMax.X <= inkMin.X || inkMax.Y <= inkMin.Y)
		{
			return Vector2.Zero;
		}

		float y = MathF.Round((textSize.Y - (inkMin.Y + inkMax.Y)) * 0.5f);
		float x = layout == ToolbarButtonLayout.GlyphLeading
			? 0.0f
			: MathF.Round((textSize.X - (inkMin.X + inkMax.X)) * 0.5f);
		return new Vector2(x, y);
	}

	private static float SingleLineHeight(float glyphHeight, float labelHeight, Vector2 framePadding, float frameHeight) =>
		MathF.Max(frameHeight, MathF.Max(glyphHeight, labelHeight) + (2.0f * framePadding.Y));
}

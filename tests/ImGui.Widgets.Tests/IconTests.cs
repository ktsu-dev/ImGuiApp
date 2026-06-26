// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class IconTests
{
	// CalcTextBlockSize aggregates per-line sizes into a single bounding box: width is the widest
	// line, height is the sum of line heights plus itemSpacing.Y between (but not after) lines.

	[TestMethod]
	public void CalcTextBlockSize_NoLines_ReturnsZero()
	{
		Vector2 result = ImGuiWidgets.CalcTextBlockSize([], new Vector2(4, 6));

		Assert.AreEqual(Vector2.Zero, result);
	}

	[TestMethod]
	public void CalcTextBlockSize_SingleLine_DoesNotAddTrailingSpacing()
	{
		(string, Vector2)[] lines = [("Alpha", new Vector2(50, 18))];

		Vector2 result = ImGuiWidgets.CalcTextBlockSize(lines, new Vector2(4, 6));

		// One line: width is the line width, height is the line height with no trailing spacing.
		Assert.AreEqual(new Vector2(50, 18), result);
	}

	[TestMethod]
	public void CalcTextBlockSize_MultipleLines_UsesWidestAndSumsHeightsWithInteriorSpacing()
	{
		(string, Vector2)[] lines =
		[
			("Alpha", new Vector2(50, 18)),
			("Bravo line", new Vector2(80, 20)),
			("C", new Vector2(10, 16)),
		];

		Vector2 result = ImGuiWidgets.CalcTextBlockSize(lines, new Vector2(4, 6));

		// Width = widest line (80). Height = 18 + 20 + 16 + 2 interior gaps of 6 = 54 + 12 = 66.
		Assert.AreEqual(new Vector2(80, 66), result);
	}

	[TestMethod]
	public void CalcTextBlockSize_ZeroSpacing_SumsHeightsOnly()
	{
		(string, Vector2)[] lines =
		[
			("One", new Vector2(30, 18)),
			("Two", new Vector2(30, 18)),
		];

		Vector2 result = ImGuiWidgets.CalcTextBlockSize(lines, Vector2.Zero);

		Assert.AreEqual(new Vector2(30, 36), result);
	}

	// CalcIconSize composes the image, the text block, item spacing and frame padding into the
	// widget's bounding box. The layout differs by IconAlignment.

	[TestMethod]
	public void CalcIconSize_Horizontal_PlacesTextBesideImage()
	{
		Vector2 textBlockSize = new(40, 16);
		Vector2 imageSize = new(32, 32);
		Vector2 itemSpacing = new(8, 4);
		Vector2 framePadding = new(2, 3);

		Vector2 result = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, ImGuiWidgets.IconAlignment.Horizontal, itemSpacing, framePadding);

		// Width = image.X + text.X + spacing.X + 2*padding.X = 32 + 40 + 8 + 4 = 84.
		// Height = max(image.Y, text.Y) + 2*padding.Y = max(32, 16) + 6 = 38.
		Assert.AreEqual(new Vector2(84, 38), result);
	}

	[TestMethod]
	public void CalcIconSize_Horizontal_TallTextDrivesHeight()
	{
		Vector2 textBlockSize = new(40, 50);
		Vector2 imageSize = new(32, 32);

		Vector2 result = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, ImGuiWidgets.IconAlignment.Horizontal, Vector2.Zero, Vector2.Zero);

		// Height comes from the taller text block (50), not the image (32).
		Assert.AreEqual(50f, result.Y);
		Assert.AreEqual(72f, result.X);
	}

	[TestMethod]
	public void CalcIconSize_Vertical_PlacesTextBelowImage()
	{
		Vector2 textBlockSize = new(40, 16);
		Vector2 imageSize = new(32, 32);
		Vector2 itemSpacing = new(8, 4);
		Vector2 framePadding = new(2, 3);

		Vector2 result = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, ImGuiWidgets.IconAlignment.Vertical, itemSpacing, framePadding);

		// Width = max(image.X, text.X) + 2*padding.X = max(32, 40) + 4 = 44.
		// Height = image.Y + text.Y + spacing.Y + 2*padding.Y = 32 + 16 + 4 + 6 = 58.
		Assert.AreEqual(new Vector2(44, 58), result);
	}

	[TestMethod]
	public void CalcIconSize_Vertical_WideTextDrivesWidth()
	{
		Vector2 textBlockSize = new(100, 16);
		Vector2 imageSize = new(32, 32);

		Vector2 result = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, ImGuiWidgets.IconAlignment.Vertical, Vector2.Zero, Vector2.Zero);

		// Width comes from the wider text block (100), not the image (32).
		Assert.AreEqual(100f, result.X);
		Assert.AreEqual(48f, result.Y);
	}

	[TestMethod]
	public void CalcIconSize_InvalidAlignment_Throws()
	{
		ImGuiWidgets.IconAlignment invalid = (ImGuiWidgets.IconAlignment)99;

		Assert.ThrowsExactly<NotImplementedException>(() =>
			ImGuiWidgets.CalcIconSize(new Vector2(10, 10), new Vector2(20, 20), invalid, Vector2.Zero, Vector2.Zero));
	}

	[TestMethod]
	public void CalcIconSize_FloatImageOverload_MatchesSquareVectorImage()
	{
		Vector2 textBlockSize = new(40, 16);
		Vector2 itemSpacing = new(8, 4);
		Vector2 framePadding = new(2, 3);
		const float squareSize = 32f;

		Vector2 fromFloat = ImGuiWidgets.CalcIconSize(textBlockSize, squareSize, ImGuiWidgets.IconAlignment.Horizontal, itemSpacing, framePadding);
		Vector2 fromVector = ImGuiWidgets.CalcIconSize(textBlockSize, new Vector2(squareSize), ImGuiWidgets.IconAlignment.Horizontal, itemSpacing, framePadding);

		Assert.AreEqual(fromVector, fromFloat);
	}

	[TestMethod]
	public void CalcIconSize_AlignmentOverload_DefaultsToHorizontal()
	{
		Vector2 textBlockSize = new(40, 16);
		Vector2 imageSize = new(32, 32);
		Vector2 itemSpacing = new(8, 4);
		Vector2 framePadding = new(2, 3);

		Vector2 defaulted = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, itemSpacing, framePadding);
		Vector2 explicitHorizontal = ImGuiWidgets.CalcIconSize(textBlockSize, imageSize, ImGuiWidgets.IconAlignment.Horizontal, itemSpacing, framePadding);

		Assert.AreEqual(explicitHorizontal, defaulted);
	}
}

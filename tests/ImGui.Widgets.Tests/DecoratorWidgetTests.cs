// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the pure logic backing the Phase 1 "decorator" widgets (Avatar, Badge, Rating, PageIndicator).
/// The draw paths are immediate-mode and verified visually in the demo; these cover the testable helpers.
/// </summary>
[TestClass]
public sealed class DecoratorWidgetTests
{
	[TestMethod]
	public void Initials_TwoTokens_UsesFirstAndLast()
	{
		Assert.AreEqual("JD", ImGuiWidgets.Initials("John Doe"));
		Assert.AreEqual("JD", ImGuiWidgets.Initials("john van doe"));
	}

	[TestMethod]
	public void Initials_SingleToken_UsesFirstCharacter()
	{
		Assert.AreEqual("A", ImGuiWidgets.Initials("alice"));
		Assert.AreEqual("X", ImGuiWidgets.Initials("X"));
	}

	[TestMethod]
	public void Initials_CollapsesWhitespaceAndUppercases()
	{
		Assert.AreEqual("JD", ImGuiWidgets.Initials("  john   doe  "));
	}

	[TestMethod]
	public void Initials_EmptyOrWhitespace_ReturnsQuestionMark()
	{
		Assert.AreEqual("?", ImGuiWidgets.Initials(""));
		Assert.AreEqual("?", ImGuiWidgets.Initials("   "));
		Assert.AreEqual("?", ImGuiWidgets.Initials(null!));
	}

	[TestMethod]
	public void FormatBadgeCount_NonPositive_IsEmpty()
	{
		Assert.AreEqual("", ImGuiWidgets.FormatBadgeCount(0, 99));
		Assert.AreEqual("", ImGuiWidgets.FormatBadgeCount(-5, 99));
	}

	[TestMethod]
	public void FormatBadgeCount_WithinMax_ShowsExactCount()
	{
		Assert.AreEqual("1", ImGuiWidgets.FormatBadgeCount(1, 99));
		Assert.AreEqual("99", ImGuiWidgets.FormatBadgeCount(99, 99));
	}

	[TestMethod]
	public void FormatBadgeCount_AboveMax_ShowsOverflow()
	{
		Assert.AreEqual("99+", ImGuiWidgets.FormatBadgeCount(100, 99));
		Assert.AreEqual("9+", ImGuiWidgets.FormatBadgeCount(10, 9));
	}

	[TestMethod]
	public void RatingValueFromOffset_WholeStars_RoundsUpToNextStar()
	{
		// pitch 20px, 5 stars. An offset just inside the first star yields 1.
		Assert.AreEqual(1f, ImGuiWidgets.RatingValueFromOffset(1f, 20f, 5, allowHalf: false), 1e-5f);
		Assert.AreEqual(3f, ImGuiWidgets.RatingValueFromOffset(41f, 20f, 5, allowHalf: false), 1e-5f);
	}

	[TestMethod]
	public void RatingValueFromOffset_Half_SnapsToHalfStars()
	{
		// Just past the midpoint of the first star (10px of a 20px pitch) → still first half → 0.5.
		Assert.AreEqual(0.5f, ImGuiWidgets.RatingValueFromOffset(5f, 20f, 5, allowHalf: true), 1e-5f);
		Assert.AreEqual(1.0f, ImGuiWidgets.RatingValueFromOffset(15f, 20f, 5, allowHalf: true), 1e-5f);
	}

	[TestMethod]
	public void RatingValueFromOffset_ClampsToRange()
	{
		Assert.AreEqual(0f, ImGuiWidgets.RatingValueFromOffset(-50f, 20f, 5, allowHalf: false), 1e-5f);
		Assert.AreEqual(5f, ImGuiWidgets.RatingValueFromOffset(9999f, 20f, 5, allowHalf: false), 1e-5f);
	}

	[TestMethod]
	public void RatingValueFromOffset_InvalidPitch_ReturnsZero()
	{
		Assert.AreEqual(0f, ImGuiWidgets.RatingValueFromOffset(50f, 0f, 5, allowHalf: false), 1e-5f);
	}

	[TestMethod]
	public void StarFillFraction_FullPartialEmpty()
	{
		Assert.AreEqual(1f, ImGuiWidgets.StarFillFraction(3.0f, 0), 1e-5f);
		Assert.AreEqual(0.5f, ImGuiWidgets.StarFillFraction(2.5f, 2), 1e-5f);
		Assert.AreEqual(0f, ImGuiWidgets.StarFillFraction(1.0f, 4), 1e-5f);
	}

	[TestMethod]
	public void ClampPage_ClampsAndHandlesEmpty()
	{
		Assert.AreEqual(0, ImGuiWidgets.ClampPage(-3, 5));
		Assert.AreEqual(4, ImGuiWidgets.ClampPage(99, 5));
		Assert.AreEqual(2, ImGuiWidgets.ClampPage(2, 5));
		Assert.AreEqual(0, ImGuiWidgets.ClampPage(3, 0));
	}
}

// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the pure logic backing the Phase 1 "container / loader" widgets (Card, SkeletonLoader, PinInput).
/// The draw paths are immediate-mode and verified visually in the demo; these cover the testable helpers.
/// </summary>
[TestClass]
public sealed class ContainerWidgetTests
{
	[TestMethod]
	public void NormalizePin_DigitsOnly_StripsNonDigits()
	{
		Assert.AreEqual("1234", ImGuiWidgets.NormalizePin("1a2b3c4d", 6, digitsOnly: true));
		Assert.AreEqual("0090", ImGuiWidgets.NormalizePin(" 00-90 ", 6, digitsOnly: true));
	}

	[TestMethod]
	public void NormalizePin_TruncatesToLength()
	{
		Assert.AreEqual("123456", ImGuiWidgets.NormalizePin("1234567890", 6, digitsOnly: true));
		Assert.AreEqual("ab", ImGuiWidgets.NormalizePin("abcdef", 2, digitsOnly: false));
	}

	[TestMethod]
	public void NormalizePin_NonDigitsKeptWhenNotDigitsOnly()
	{
		Assert.AreEqual("a1b2", ImGuiWidgets.NormalizePin("a1b2", 8, digitsOnly: false));
	}

	[TestMethod]
	public void NormalizePin_NullEmptyOrZeroLength_ReturnsEmpty()
	{
		Assert.AreEqual("", ImGuiWidgets.NormalizePin(null, 6, digitsOnly: true));
		Assert.AreEqual("", ImGuiWidgets.NormalizePin("", 6, digitsOnly: true));
		Assert.AreEqual("", ImGuiWidgets.NormalizePin("123", 0, digitsOnly: true));
	}

	[TestMethod]
	public void SetPinSlot_ReplaceExistingSlot()
	{
		Assert.AreEqual("1x34", ImGuiWidgets.SetPinSlot("1234", 1, 'x', 4));
	}

	[TestMethod]
	public void SetPinSlot_AppendAtNextFreeSlot()
	{
		Assert.AreEqual("125", ImGuiWidgets.SetPinSlot("12", 2, '5', 4));
	}

	[TestMethod]
	public void SetPinSlot_ClickingAheadStillFillsContiguously()
	{
		// Typing into a box past the current end appends rather than leaving a gap.
		Assert.AreEqual("125", ImGuiWidgets.SetPinSlot("12", 3, '5', 4));
	}

	[TestMethod]
	public void SetPinSlot_ClearRemovesAndShiftsLeft()
	{
		Assert.AreEqual("134", ImGuiWidgets.SetPinSlot("1234", 1, null, 4));
		Assert.AreEqual("123", ImGuiWidgets.SetPinSlot("1234", 3, null, 4));
	}

	[TestMethod]
	public void SetPinSlot_ClearEmptySlot_NoChange()
	{
		Assert.AreEqual("12", ImGuiWidgets.SetPinSlot("12", 3, null, 4));
	}

	[TestMethod]
	public void SetPinSlot_ReplaceWithinFullValue()
	{
		Assert.AreEqual("1294", ImGuiWidgets.SetPinSlot("1234", 2, '9', 4));
	}

	[TestMethod]
	public void SetPinSlot_IndexOutOfRange_ReturnsUnchanged()
	{
		Assert.AreEqual("12", ImGuiWidgets.SetPinSlot("12", -1, '9', 4));
		Assert.AreEqual("12", ImGuiWidgets.SetPinSlot("12", 4, '9', 4));
	}

	[TestMethod]
	public void SetPinSlot_NullValue_TreatedAsEmpty()
	{
		Assert.AreEqual("9", ImGuiWidgets.SetPinSlot(null!, 0, '9', 4));
	}
}

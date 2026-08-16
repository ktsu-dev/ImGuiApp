// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the selection guard the single-curve editor puts in front of Hexa's widget. The widget
/// itself needs a live ImGui context, but the guard is pure.
/// </summary>
[TestClass]
public sealed class CurveFieldTests
{
	[TestMethod]
	public void ClampSelection_InRangeIndex_IsUnchanged()
	{
		Assert.AreEqual(1, ImGuiWidgets.ClampSelection(1, 3));
	}

	[TestMethod]
	public void ClampSelection_FirstAndLastIndex_AreUnchanged()
	{
		Assert.AreEqual(0, ImGuiWidgets.ClampSelection(0, 3));
		Assert.AreEqual(2, ImGuiWidgets.ClampSelection(2, 3));
	}

	[TestMethod]
	public void ClampSelection_NoSelectionSentinel_IsPreserved()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(-1, 3));
	}

	[TestMethod]
	public void ClampSelection_IndexPastTheEnd_BecomesNoSelection()
	{
		// This is the state upstream leaves behind after deleting the last point on double-click:
		// it removes Points[currentSelection] but writes the stale index straight back out.
		// Deselecting is deliberate — retargeting the drag at the new last point would move a
		// point the user never grabbed.
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(3, 3));
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(99, 3));
	}

	[TestMethod]
	public void ClampSelection_EmptyCurve_AlwaysNoSelection()
	{
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(0, 0));
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(-1, 0));
	}

	[TestMethod]
	public void ClampSelection_NegativeBelowSentinel_NormalizesToSentinel()
	{
		// Only -1 means "nothing selected"; any other negative would be indexed just as blindly.
		Assert.AreEqual(-1, ImGuiWidgets.ClampSelection(-7, 3));
	}

	[TestMethod]
	public void ClampSelection_AlwaysLandsInAddressableRange()
	{
		for (int pointCount = 0; pointCount <= 4; pointCount++)
		{
			for (int selection = -4; selection <= 8; selection++)
			{
				int result = ImGuiWidgets.ClampSelection(selection, pointCount);

				Assert.IsGreaterThanOrEqualTo(-1, result);
				Assert.IsLessThanOrEqualTo(pointCount - 1, result);
			}
		}
	}
}

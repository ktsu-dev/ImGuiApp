// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the drag target and layout rules behind ImageCompare. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class ImageCompareStateTests
{
	[TestMethod]
	public void NormalizeSplit_ReplacesNaNWithHalf() =>
		Assert.AreEqual(0.5f, ImGuiWidgets.ImageCompareState.NormalizeSplit(float.NaN));

	[TestMethod]
	public void NormalizeSplit_Clamps()
	{
		Assert.AreEqual(0f, ImGuiWidgets.ImageCompareState.NormalizeSplit(-1f));
		Assert.AreEqual(1f, ImGuiWidgets.ImageCompareState.NormalizeSplit(2f));
	}

	[TestMethod]
	public void SplitFromPointer_IsTheFractionOfTheWidth() =>
		Assert.AreEqual(0.25f, ImGuiWidgets.ImageCompareState.SplitFromPointer(80f, 320f), 1e-6f);

	[TestMethod]
	public void SplitFromPointer_ClampsOutsideTheCanvas()
	{
		Assert.AreEqual(0f, ImGuiWidgets.ImageCompareState.SplitFromPointer(-10f, 320f));
		Assert.AreEqual(1f, ImGuiWidgets.ImageCompareState.SplitFromPointer(400f, 320f));
	}

	[TestMethod]
	public void SplitFromPointer_ZeroWidthIsHalf() =>
		Assert.AreEqual(0.5f, ImGuiWidgets.ImageCompareState.SplitFromPointer(10f, 0f));

	[TestMethod]
	public void HitsDivider_IsInclusiveOfTheGrabRadius()
	{
		Assert.IsTrue(ImGuiWidgets.ImageCompareState.HitsDivider(106f, 100f));
		Assert.IsTrue(ImGuiWidgets.ImageCompareState.HitsDivider(94f, 100f));
		Assert.IsFalse(ImGuiWidgets.ImageCompareState.HitsDivider(106.5f, 100f));
	}

	[TestMethod]
	public void Press_OnTheDividerInWipeTargetsTheDivider()
	{
		ImGuiWidgets.ImageCompareState state = new();
		state.Press(103f, 100f, ImGuiWidgets.ImageCompareMode.Wipe);
		Assert.AreEqual(ImGuiWidgets.ImageCompareState.DragTarget.Divider, state.Target);
	}

	[TestMethod]
	public void Press_AwayFromTheDividerPans()
	{
		ImGuiWidgets.ImageCompareState state = new();
		state.Press(150f, 100f, ImGuiWidgets.ImageCompareMode.Wipe);
		Assert.AreEqual(ImGuiWidgets.ImageCompareState.DragTarget.Pan, state.Target);
	}

	[TestMethod]
	public void Press_InSideBySideAlwaysPans()
	{
		ImGuiWidgets.ImageCompareState state = new();
		state.Press(100f, 100f, ImGuiWidgets.ImageCompareMode.SideBySide);
		Assert.AreEqual(ImGuiWidgets.ImageCompareState.DragTarget.Pan, state.Target);
	}

	[TestMethod]
	public void Release_ClearsTheTarget()
	{
		ImGuiWidgets.ImageCompareState state = new();
		state.Press(100f, 100f, ImGuiWidgets.ImageCompareMode.Wipe);
		state.Release();
		Assert.AreEqual(ImGuiWidgets.ImageCompareState.DragTarget.None, state.Target);
	}

	[TestMethod]
	public void SideBySideLayout_SplitsTheCanvasWithAGap()
	{
		(Vector2 left, Vector2 right, Vector2 pane) = ImGuiWidgets.ImageCompareState.SideBySideLayout(new Vector2(324f, 200f));

		Assert.AreEqual(new Vector2(160f, 200f), pane);
		Assert.AreEqual(Vector2.Zero, left);
		Assert.AreEqual(new Vector2(164f, 0f), right);
	}

	[TestMethod]
	public void SideBySideLayout_NeverProducesAnEmptyPane() =>
		Assert.AreEqual(1f, ImGuiWidgets.ImageCompareState.SideBySideLayout(new Vector2(2f, 10f)).PaneSize.X);
}

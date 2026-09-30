// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pixel picking behind <see cref="ImGuiWidgets.PixelLoupe"/>. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class PixelLoupeStateTests
{
	private static readonly Vector2 Sixteen = new(16, 16);
	private static readonly Vector2 Eight = new(8, 8);

	[TestMethod]
	public void TryPixelAt_FloorsToThePixel()
	{
		Assert.IsTrue(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(3.9f, 7.1f), Sixteen, out int x, out int y));
		Assert.AreEqual(3, x);
		Assert.AreEqual(7, y);
	}

	[TestMethod]
	public void TryPixelAt_LastPixelIsIncluded()
	{
		Assert.IsTrue(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(15.999f, 15.999f), Sixteen, out int x, out int y));
		Assert.AreEqual(15, x);
		Assert.AreEqual(15, y);
	}

	[TestMethod]
	public void TryPixelAt_TheFarEdgeIsOutside()
	{
		Assert.IsFalse(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(16, 0), Sixteen, out int x, out int y));
		Assert.AreEqual(-1, x);
		Assert.AreEqual(-1, y);
	}

	[TestMethod]
	public void TryPixelAt_NegativeIsOutside() =>
		Assert.IsFalse(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(-0.1f, 3), Sixteen, out _, out _));

	[TestMethod]
	public void TryPixelAt_NaNIsOutside() =>
		Assert.IsFalse(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(float.NaN, 3), Sixteen, out _, out _));

	[TestMethod]
	public void TryPixelAt_EmptyImageHasNoPixels() =>
		Assert.IsFalse(ImGuiWidgets.PixelLoupeState.TryPixelAt(new Vector2(0, 0), new Vector2(0, 10), out _, out _));

	[TestMethod]
	public void Update_StoresTheHoveredPixel()
	{
		ImGuiWidgets.PixelLoupeState state = new();

		Assert.IsTrue(state.Update(true, new Vector2(2.5f, 4.5f), Eight));
		Assert.IsTrue(state.HasPixel);
		Assert.AreEqual(2, state.X);
		Assert.AreEqual(4, state.Y);
	}

	[TestMethod]
	public void Update_KeepsTheLastPixelWhenThePointerLeaves()
	{
		ImGuiWidgets.PixelLoupeState state = new();
		state.Update(true, new Vector2(2.5f, 4.5f), Eight);

		Assert.IsFalse(state.Update(false, Vector2.Zero, Eight));
		Assert.AreEqual(2, state.X);
		Assert.IsTrue(state.HasPixel);
	}

	[TestMethod]
	public void Update_KeepsTheLastPixelOutsideTheImage()
	{
		ImGuiWidgets.PixelLoupeState state = new();
		state.Update(true, new Vector2(2.5f, 4.5f), Eight);

		Assert.IsFalse(state.Update(true, new Vector2(20, 20), Eight));
		Assert.AreEqual(2, state.X);
	}

	[TestMethod]
	public void Update_ForgetsAPixelTheImageNoLongerHas()
	{
		ImGuiWidgets.PixelLoupeState state = new();
		state.Update(true, new Vector2(6, 6), Eight);

		state.Update(false, Vector2.Zero, new Vector2(4, 4));

		Assert.IsFalse(state.HasPixel);
		Assert.AreEqual(-1, state.X);
	}

	[TestMethod]
	public void ClampRadius_Clamps()
	{
		Assert.AreEqual(0, ImGuiWidgets.PixelLoupeState.ClampRadius(-3));
		Assert.AreEqual(32, ImGuiWidgets.PixelLoupeState.ClampRadius(40));
	}

	[TestMethod]
	public void ClampCellSize_Clamps()
	{
		Assert.AreEqual(2f, ImGuiWidgets.PixelLoupeState.ClampCellSize(1f));
		Assert.AreEqual(64f, ImGuiWidgets.PixelLoupeState.ClampCellSize(100f));
		Assert.AreEqual(12f, ImGuiWidgets.PixelLoupeState.ClampCellSize(float.NaN));
	}

	[TestMethod]
	public void InImage_IsHalfOpen()
	{
		Assert.IsTrue(ImGuiWidgets.PixelLoupeState.InImage(0, 0, Eight));
		Assert.IsTrue(ImGuiWidgets.PixelLoupeState.InImage(7, 7, Eight));
		Assert.IsFalse(ImGuiWidgets.PixelLoupeState.InImage(8, 7, Eight));
	}
}

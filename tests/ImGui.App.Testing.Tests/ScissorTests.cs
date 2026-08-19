// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ScissorTests
{
	private static readonly Rgba32 Red = new(255, 0, 0, 255);
	private static readonly Rgba32 Black = new(0, 0, 0, 255);

	private static Vertex V(float x, float y) => new(new Vector2(x, y), Vector2.Zero, Red);

	[TestMethod]
	public void FillTriangle_OutsideScissor_IsNotDrawn()
	{
		Bitmap32 target = new(32, 32);
		target.Clear(Black);

		// The triangle covers most of the bitmap, but the scissor admits only a small box.
		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(32, 0), V(0, 32),
			texture: null,
			scissor: new Rectangle(4, 4, 8, 8));

		Assert.AreEqual(Red, target.GetPixel(5, 5), "Inside the scissor should be drawn.");
		Assert.AreEqual(Black, target.GetPixel(1, 1), "Outside the scissor must be untouched.");
		Assert.AreEqual(Black, target.GetPixel(10, 2), "Outside the scissor must be untouched.");
	}

	[TestMethod]
	public void FillTriangle_ScissorLargerThanTarget_ClampsWithoutThrowing()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(8, 0), V(0, 8),
			texture: null,
			scissor: new Rectangle(-100, -100, 1000, 1000));

		Assert.AreEqual(Red, target.GetPixel(1, 1), "An oversized scissor should clamp to the target.");
	}

	[TestMethod]
	public void FillTriangle_EmptyScissor_DrawsNothing()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0), V(8, 0), V(0, 8),
			texture: null,
			scissor: new Rectangle(4, 4, 4, 4));

		Assert.AreEqual(Black, target.GetPixel(4, 4), "A zero-area scissor should admit nothing.");
	}

	[TestMethod]
	public void Rectangle_ReportsNonNegativeExtents()
	{
		Rectangle inverted = new(10, 10, 4, 4);

		Assert.AreEqual(0, inverted.Width, "An inverted rectangle should report zero width, not a negative one.");
		Assert.AreEqual(0, inverted.Height);
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SoftwareRasterizerTests
{
	private static Vertex V(float x, float y, Rgba32 color) => new(new Vector2(x, y), Vector2.Zero, color);

	private static readonly Rgba32 Red = new(255, 0, 0, 255);
	private static readonly Rgba32 Black = new(0, 0, 0, 255);

	[TestMethod]
	public void FillTriangle_CoversInteriorPixels()
	{
		Bitmap32 target = new(16, 16);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, Red), V(12, 0, Red), V(0, 12, Red),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(Red, target.GetPixel(2, 2), "A pixel well inside the triangle should be filled.");
	}

	[TestMethod]
	public void FillTriangle_LeavesExteriorPixelsUntouched()
	{
		Bitmap32 target = new(16, 16);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, Red), V(12, 0, Red), V(0, 12, Red),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(Black, target.GetPixel(14, 14), "A pixel outside the triangle must not be touched.");
	}

	[TestMethod]
	public void FillTriangle_ClampsToTargetBounds()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(Black);
		Rgba32 green = new(0, 255, 0, 255);

		// Deliberately spills far outside the target on every side.
		SoftwareRasterizer.FillTriangle(
			target,
			V(-50, -50, green), V(500, -50, green), V(-50, 500, green),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(green, target.GetPixel(0, 0), "Rasterizing off target must clip rather than throw.");
	}

	[TestMethod]
	public void FillTriangle_DegenerateTriangle_DrawsNothing()
	{
		Bitmap32 target = new(8, 8);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(1, 1, Red), V(1, 1, Red), V(1, 1, Red),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Assert.AreEqual(Black, target.GetPixel(1, 1), "A zero-area triangle should draw nothing.");
	}

	[TestMethod]
	public void FillTriangle_IsWindingOrderIndependent()
	{
		Bitmap32 clockwise = new(16, 16);
		Bitmap32 counterClockwise = new(16, 16);
		clockwise.Clear(Black);
		counterClockwise.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			clockwise,
			V(0, 0, Red), V(12, 0, Red), V(0, 12, Red),
			texture: null,
			scissor: Rectangle.FullSize(clockwise));

		SoftwareRasterizer.FillTriangle(
			counterClockwise,
			V(0, 0, Red), V(0, 12, Red), V(12, 0, Red),
			texture: null,
			scissor: Rectangle.FullSize(counterClockwise));

		// ImGui emits both windings, so a rasterizer that culls one would silently drop geometry.
		CollectionAssert.AreEqual(
			clockwise.Pixels.ToArray(),
			counterClockwise.Pixels.ToArray(),
			"Both windings of the same triangle should rasterize identically.");
	}

	[TestMethod]
	public void FillTriangle_InterpolatesVertexColors()
	{
		Bitmap32 target = new(32, 32);
		target.Clear(Black);

		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, new Rgba32(255, 0, 0, 255)),
			V(30, 0, new Rgba32(0, 255, 0, 255)),
			V(0, 30, new Rgba32(0, 0, 255, 255)),
			texture: null,
			scissor: Rectangle.FullSize(target));

		Rgba32 nearRedCorner = target.GetPixel(1, 1);
		Rgba32 nearGreenCorner = target.GetPixel(27, 1);

		Assert.IsTrue(nearRedCorner.R > nearRedCorner.G, "Near the red vertex, red should dominate.");
		Assert.IsTrue(nearGreenCorner.G > nearGreenCorner.R, "Near the green vertex, green should dominate.");
	}

	[TestMethod]
	public void FillTriangle_WithTexture_ModulatesVertexColorByTexel()
	{
		Bitmap32 target = new(16, 16);
		target.Clear(Black);

		Bitmap32 texel = new(1, 1);
		texel.SetPixel(0, 0, new Rgba32(0, 255, 0, 255));

		Rgba32 white = new(255, 255, 255, 255);
		SoftwareRasterizer.FillTriangle(
			target,
			V(0, 0, white), V(12, 0, white), V(0, 12, white),
			new TextureSource(texel),
			Rectangle.FullSize(target));

		Assert.AreEqual(new Rgba32(0, 255, 0, 255), target.GetPixel(2, 2), "White vertex color times a green texel is green.");
	}

	[TestMethod]
	public void FillTriangle_NullTarget_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => SoftwareRasterizer.FillTriangle(
			null!,
			V(0, 0, Red), V(1, 0, Red), V(0, 1, Red),
			texture: null,
			scissor: new Rectangle(0, 0, 1, 1)));
	[TestMethod]
	public void FillTriangle_TranslucentQuad_BlendsItsDiagonalOnce()
	{
		// ImGui draws a rectangle as two triangles sharing a diagonal. On a square whose corners sit
		// on pixel corners, that diagonal passes exactly through a row of pixel centres; taking
		// them in both triangles blended them twice and drew the diagonal as a line.
		Bitmap32 target = new(8, 8);
		target.Clear(Black);
		Rgba32 translucent = new(255, 255, 255, 128);

		FillQuad(target, 0, 0, 8, 8, translucent);

		Rgba32 expected = target.GetPixel(6, 1);
		for (int i = 0; i < 8; i++)
		{
			Assert.AreEqual(expected, target.GetPixel(i, i), $"Pixel ({i},{i}) on the diagonal was blended a different number of times from its neighbours.");
		}
	}

	[TestMethod]
	public void FillTriangle_AdjacentQuads_ShareTheirEdgeWithoutGapOrOverlap()
	{
		// Two rectangles meeting at x = 4.5, a line through a column of pixel centres. Each of those
		// pixels belongs to exactly one of them.
		Bitmap32 target = new(8, 4);
		target.Clear(Black);
		Rgba32 translucent = new(255, 255, 255, 128);

		FillQuad(target, 0.5f, 0, 4.5f, 4, translucent);
		FillQuad(target, 4.5f, 0, 8, 4, translucent);

		Rgba32 once = target.GetPixel(2, 1);
		Assert.AreNotEqual(Black, once);
		for (int y = 0; y < 4; y++)
		{
			Assert.AreEqual(once, target.GetPixel(4, y), $"Pixel (4,{y}) on the shared edge must be blended exactly once.");
		}
	}

	[TestMethod]
	public void FillTriangle_TopLeftRule_TakesTopAndLeftEdgesOnly()
	{
		// A rectangle from 0.5 to 3.5 has pixel centres on all four of its edges. The rule a GPU
		// applies takes the top and left ones and leaves the bottom and right to the neighbours.
		Bitmap32 target = new(5, 5);
		target.Clear(Black);

		FillQuad(target, 0.5f, 0.5f, 3.5f, 3.5f, Red);

		Assert.AreEqual(Red, target.GetPixel(0, 0), "Top-left corner pixel lies on a top and a left edge.");
		Assert.AreEqual(Red, target.GetPixel(2, 0), "Top edge pixels are taken.");
		Assert.AreEqual(Red, target.GetPixel(0, 2), "Left edge pixels are taken.");
		Assert.AreEqual(Black, target.GetPixel(3, 2), "Right edge pixels are left to the neighbour.");
		Assert.AreEqual(Black, target.GetPixel(2, 3), "Bottom edge pixels are left to the neighbour.");
	}

	private static void FillQuad(Bitmap32 target, float x0, float y0, float x1, float y1, Rgba32 color)
	{
		// The index order ImGui's PrimRect uses: (0, 1, 2) and (0, 2, 3), clockwise on screen.
		Vertex a = V(x0, y0, color);
		Vertex b = V(x1, y0, color);
		Vertex c = V(x1, y1, color);
		Vertex d = V(x0, y1, color);
		SoftwareRasterizer.FillTriangle(target, a, b, c, texture: null, Rectangle.FullSize(target));
		SoftwareRasterizer.FillTriangle(target, a, c, d, texture: null, Rectangle.FullSize(target));
	}
}

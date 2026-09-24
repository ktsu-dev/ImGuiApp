// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;
using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the software backend's <see cref="IRenderer3D"/> implementation.
/// </summary>
/// <remarks>
/// Every assertion here is about pixels the renderer actually produced, because that is the only
/// thing a 3D path can be checked against and because it is what the rest of the repository's
/// headless suites already do. The awkward cases — near-plane clipping and perspective-correct
/// interpolation — are the two an implementation can omit and still look right in a screenshot, so
/// they are pinned by a number rather than by an eye.
/// </remarks>
[TestClass]
public sealed class Renderer3DTests : IDisposable
{
	private const int Size = 128;

	private static readonly Rgba32 Background = new(0, 0, 0, 255);

	private SoftwareRenderer renderer = null!;
	private nint target;

	[TestInitialize]
	public void SetUp()
	{
		renderer = new SoftwareRenderer(Size, Size);
		target = renderer.CreateRenderTarget(Size, Size, depth: true);
		renderer.Clear(target, Vector4.Zero with { W = 1f }, 1f);
	}

	[TestCleanup]
	public void TearDown() => Dispose();

	/// <summary>Releases the renderer. MSTest constructs one instance per test, so this is per test.</summary>
	public void Dispose()
	{
		renderer?.DeleteRenderTarget(target);
		renderer?.Dispose();
		renderer = null!;
	}

	[TestMethod]
	public void ATriangleFacingTheCameraIsDrawn()
	{
		DrawTriangle(Ortho(), 0f, Red);

		Assert.AreEqual(Red, Pixel(Size / 2, (Size / 2) + 10), "The middle of the triangle should be its colour.");
		Assert.AreEqual(Background, Pixel(2, 2), "A corner outside it should be untouched.");
	}

	[TestMethod]
	public void TheNearerTriangleWinsWhicheverOrderTheyAreSubmitted()
	{
		// The whole reason for a depth attachment. Submitted far-then-near and near-then-far, the
		// answer has to be the same, and the painter's-order answer is the opposite one.
		DrawTriangle(Ortho(), 0.8f, Blue);
		DrawTriangle(Ortho(), 0.2f, Red);

		Assert.AreEqual(Red, Pixel(Size / 2, (Size / 2) + 10));

		renderer.Clear(target, Vector4.Zero with { W = 1f }, 1f);
		DrawTriangle(Ortho(), 0.2f, Red);
		DrawTriangle(Ortho(), 0.8f, Blue);

		Assert.AreEqual(Red, Pixel(Size / 2, (Size / 2) + 10), "Submission order must not decide it.");
	}

	[TestMethod]
	public void WithoutADepthTestTheLastDrawWins()
	{
		DrawTriangle(Ortho(), 0.2f, Red, DepthMode.None);
		DrawTriangle(Ortho(), 0.8f, Blue, DepthMode.None);

		Assert.AreEqual(Blue, Pixel(Size / 2, (Size / 2) + 10));
	}

	[TestMethod]
	public void TestWithoutWriteLetsTwoDrawsAtTheSameDepthBothLand()
	{
		DrawTriangle(Ortho(), 0.5f, Red, DepthMode.Test);
		DrawTriangle(Ortho(), 0.5f, Blue, DepthMode.Test);

		// Neither wrote, so the second is still tested against the cleared far plane and passes.
		// With TestAndWrite the first would have claimed the pixel and the second would be rejected.
		Assert.AreEqual(Blue, Pixel(Size / 2, (Size / 2) + 10));

		renderer.Clear(target, Vector4.Zero with { W = 1f }, 1f);
		DrawTriangle(Ortho(), 0.5f, Red);
		DrawTriangle(Ortho(), 0.5f, Blue);

		Assert.AreEqual(Red, Pixel(Size / 2, (Size / 2) + 10), "Equal depth is not nearer, so the first keeps the pixel.");
	}

	[TestMethod]
	public void CullingDiscardsExactlyOneFacing()
	{
		// Getting the winding sense backwards culls precisely the geometry meant to be drawn, and
		// looks like nothing rendering at all — so both modes are checked against the same triangle
		// rather than one mode being assumed from the other.
		DrawTriangle(Ortho(), 0f, Red, DepthMode.TestAndWrite, CullMode.Back);
		Assert.AreEqual(Red, Pixel(Size / 2, (Size / 2) + 10), "A front-facing triangle survives back-face culling.");

		renderer.Clear(target, Vector4.Zero with { W = 1f }, 1f);
		DrawTriangle(Ortho(), 0f, Red, DepthMode.TestAndWrite, CullMode.Front);
		Assert.AreEqual(Background, Pixel(Size / 2, (Size / 2) + 10), "And is discarded by front-face culling.");
	}

	[TestMethod]
	public void ATriangleStraddlingTheEyeIsClippedRatherThanSmearedAcrossTheTarget()
	{
		// The failure this prevents is spectacular and easy to ship: a vertex behind the eye has a
		// negative w, and dividing by it puts the vertex on the wrong side of the screen, so the
		// triangle is drawn inside-out and usually covers everything. Without near clipping this
		// test fails by the target being *more* covered, not less.
		Matrix4x4 mvp = Perspective();

		Vertex3D[] vertices =
		[
			new(new Vector3(-0.5f, -0.5f, -1f), Vector2.Zero, PackedRed),   // in front of the eye
			new(new Vector3(0.5f, -0.5f, -1f), Vector2.Zero, PackedRed),
			new(new Vector3(0f, 0.5f, 1f), Vector2.Zero, PackedRed),        // behind it
		];

		renderer.Draw(target, vertices, [0u, 1u, 2u], new DrawState3D
		{
			ModelViewProjection = mvp,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.None,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.TriangleList,
		});

		int covered = CountCovered();

		Assert.IsGreaterThan(0, covered, "The part in front of the eye should still be drawn.");
		Assert.IsLessThan(Size * Size / 2, covered,
			$"{covered} of {Size * Size} pixels covered: the clipped triangle should be a small wedge, not most of the target.");
	}

	[TestMethod]
	public void InterpolationIsPerspectiveCorrect()
	{
		// The measurable difference between a correct 3D rasterizer and an affine one, and the
		// thing most likely to be left out because the result still looks broadly plausible.
		//
		// A quad recedes from the camera: its near edge is red, its far edge blue. The colour is
		// linear along the surface in 3D, so the halfway colour sits at the surface's 3D midpoint —
		// and perspective compresses distance, so that midpoint projects much nearer the far edge
		// than the near one. It is the checkerboard argument: squares shrink towards the horizon,
		// so equal steps in an attribute occupy fewer and fewer pixels as they go back.
		//
		// The expected value is derived rather than observed. The camera sits at world z = +3, so
		// the near edge is at view depth 4.5 and the far edge at 11. Screen position along the span
		// goes as 1/z, the 3D midpoint is at depth 7.75, and
		//
		//     (1/7.75 - 1/11) / (1/4.5 - 1/11) = 0.290
		//
		// of the way from the far edge. An affine rasterizer would answer 0.500 — it interpolates
		// in screen space and knows nothing about w — so the two are 0.21 apart and the test can
		// be tight rather than one-sided.
		//
		// Written the first time asserting the shift ran the other way, towards the near edge. It
		// does not, and the derivation above is what settled it rather than the measurement.
		const float perspectiveCorrect = 0.290f;
		const float affine = 0.500f;

		Matrix4x4 mvp = Perspective();

		Vertex3D[] vertices =
		[
			new(new Vector3(-1f, -1f, -1.5f), Vector2.Zero, PackedRed),   // near, bottom of the target
			new(new Vector3(1f, -1f, -1.5f), Vector2.Zero, PackedRed),
			new(new Vector3(1f, 1f, -8f), Vector2.Zero, PackedBlue),      // far, top of the target
			new(new Vector3(-1f, 1f, -8f), Vector2.Zero, PackedBlue),
		];

		renderer.Draw(target, vertices, [0u, 1u, 2u, 0u, 2u, 3u], new DrawState3D
		{
			ModelViewProjection = mvp,
			Depth = DepthMode.TestAndWrite,
			Cull = CullMode.None,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.TriangleList,
		});

		(int top, int bottom) = CoveredRowSpan(Size / 2);
		int halfway = FindHalfwayRow(Size / 2, top, bottom);
		float fraction = (float)(halfway - top) / (bottom - top);

		// A twentieth of the span, which is about three rows here. The residual is the byte
		// threshold landing between two rows rather than exactly on the crossing.
		Assert.AreEqual(perspectiveCorrect, fraction, 0.06f,
			$"The halfway colour sits {fraction:F3} of the way down; perspective-correct is {perspectiveCorrect:F3} and affine would be {affine:F3}.");

		Assert.IsLessThan(affine - 0.06f, fraction, "And is nowhere near the affine answer.");
	}

	[TestMethod]
	public void ARenderTargetsTextureIsNotADeletableTexture()
	{
		// The contract hazard the design calls out: on OpenGL the returned id comes from the same
		// namespace CreateTexture draws from, so accepting it here would free a live target's
		// colour attachment out from under it. The CPU backend reproduces the rule so a test can
		// pin it, rather than the rule living only in a doc comment about a backend nothing here
		// can run.
		nint textureId = renderer.GetTargetTexture(target);

		Assert.IsTrue(renderer.IsRenderTargetTexture(textureId));
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DeleteTexture(textureId));

		nint ordinary = renderer.CreateTexture(new byte[4], 1, 1);

		Assert.IsFalse(renderer.IsRenderTargetTexture(ordinary));
		renderer.DeleteTexture(ordinary);
	}

	[TestMethod]
	public void ResizingKeepsTheTextureIdSoACallerNeedNotRebindEveryFrame()
	{
		// A viewport sized to the content region changes size whenever a splitter is dragged. If
		// the id moved with the storage, every caller would have to re-read it after any resize,
		// and one that did not would sample freed storage.
		nint before = renderer.GetTargetTexture(target);

		Assert.IsTrue(renderer.ResizeRenderTarget(target, 64, 32));
		Assert.AreEqual(before, renderer.GetTargetTexture(target));
		Assert.IsFalse(renderer.ResizeRenderTarget(9999, 8, 8), "An unknown handle is answered, not thrown at.");
	}

	[TestMethod]
	public void ATargetWithoutADepthBufferIgnoresEveryDepthMode()
	{
		nint flat = renderer.CreateRenderTarget(Size, Size, depth: false);

		try
		{
			renderer.Clear(flat, Vector4.Zero with { W = 1f }, 1f);
			DrawTriangleInto(flat, Ortho(), 0.2f, Red);
			DrawTriangleInto(flat, Ortho(), 0.8f, Blue);

			Assert.AreEqual(Blue, PixelOf(flat, Size / 2, (Size / 2) + 10),
				"With nothing to test against, the later draw wins — as DepthMode.None would.");
		}
		finally
		{
			renderer.DeleteRenderTarget(flat);
		}
	}

	[TestMethod]
	public void AnUnknownTargetHandleIsRejectedRatherThanIgnored()
	{
		Assert.ThrowsExactly<ArgumentException>(() => renderer.GetTargetTexture(9999));
		Assert.ThrowsExactly<ArgumentException>(() => renderer.Clear(9999, Vector4.One, 1f));
	}

	[TestMethod]
	public void LinesAreDrawn()
	{
		Vertex3D[] vertices =
		[
			new(new Vector3(-0.8f, 0f, 0f), Vector2.Zero, PackedRed),
			new(new Vector3(0.8f, 0f, 0f), Vector2.Zero, PackedRed),
		];

		renderer.Draw(target, vertices, [0u, 1u], new DrawState3D
		{
			ModelViewProjection = Ortho(),
			Depth = DepthMode.None,
			Cull = CullMode.None,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.LineList,
		});

		Assert.AreEqual(Red, Pixel(Size / 2, Size / 2), "The line should cross the middle.");
		Assert.AreEqual(Background, Pixel(Size / 2, Size / 4), "And nothing above it.");
	}

	[TestMethod]
	public void ATargetCanBeSampledAsATextureByALaterDraw()
	{
		// The round trip Viewport3D is made of: render the scene into an offscreen target, then
		// draw that target's colour buffer as a textured quad. Everything else in this suite draws
		// into a target and reads its pixels directly, which never exercises the target's texture
		// id as an input — so a backend whose GetTargetTexture returned a plausible-looking handle
		// that sampled nothing would pass every other test here and produce a blank viewport.
		//
		// The quad is a pixel-for-pixel copy, so the destination should reproduce the source. The
		// vertex colour is white because the shader modulates by it, and a tinted quad would hide
		// a texture that arrived wrong.
		DrawTriangle(Ortho(), 0f, Red);

		nint destination = renderer.CreateRenderTarget(Size, Size, depth: false);

		try
		{
			renderer.Clear(destination, new Vector4(0f, 1f, 0f, 1f), 1f);
			BlitFullScreen(destination, renderer.GetTargetTexture(target));

			Assert.AreEqual(Red, PixelOf(destination, Size / 2, (Size / 2) + 10),
				"The triangle should have arrived through the target's own texture id.");
			Assert.AreEqual(Background, PixelOf(destination, 2, 2),
				"And so should the source's cleared corner — not the destination's green clear, "
				+ "which would mean the quad drew nothing at all.");
		}
		finally
		{
			renderer.DeleteRenderTarget(destination);
		}
	}

	[TestMethod]
	public void ASampledTargetArrivesTheRightWayUp()
	{
		// Separated from the round trip above because a vertical flip survives it: the triangle
		// spans raster rows 25.6 to 102.4, a range symmetric about the middle of a 128 px target,
		// so a flipped copy covers the same rows and a centre probe reads red either way.
		//
		// What a flip does change is which end is narrow. At row 40 the correctly oriented copy is
		// near the apex and 7.2 px wide either side of centre; flipped, row 40 is near the base and
		// 31 px wide. A probe 20 px off centre is outside the first and inside the second.
		DrawTriangle(Ortho(), 0f, Red);

		nint destination = renderer.CreateRenderTarget(Size, Size, depth: false);

		try
		{
			renderer.Clear(destination, Vector4.Zero with { W = 1f }, 1f);
			BlitFullScreen(destination, renderer.GetTargetTexture(target));

			Assert.AreEqual(Background, PixelOf(destination, (Size / 2) + 20, 40),
				"20 px off centre at row 40 is outside the apex end of the triangle; reading red "
				+ "there means the sampled target arrived upside down.");
			Assert.AreEqual(Red, PixelOf(destination, (Size / 2) + 20, 90),
				"The same offset near the base end is inside it, which is what makes the probe "
				+ "above a statement about orientation rather than about the quad missing.");
		}
		finally
		{
			renderer.DeleteRenderTarget(destination);
		}
	}

	/// <summary>Draws <paramref name="texture"/> over the whole of <paramref name="into"/>, unscaled and untinted.</summary>
	/// <remarks>
	/// NDC y runs up and raster y runs down, so v is <c>(1 - y) / 2</c> rather than <c>(y + 1) / 2</c>.
	/// </remarks>
	private void BlitFullScreen(nint into, nint texture)
	{
		const uint white = 0xFFFFFFFFu;

		Vertex3D[] vertices =
		[
			new(new Vector3(-1f, -1f, 0f), new Vector2(0f, 1f), white),
			new(new Vector3(1f, -1f, 0f), new Vector2(1f, 1f), white),
			new(new Vector3(1f, 1f, 0f), new Vector2(1f, 0f), white),
			new(new Vector3(-1f, 1f, 0f), new Vector2(0f, 0f), white),
		];

		renderer.Draw(into, vertices, [0u, 1u, 2u, 0u, 2u, 3u], new DrawState3D
		{
			ModelViewProjection = Ortho(),
			TextureId = texture,
			Depth = DepthMode.None,
			Cull = CullMode.None,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.TriangleList,
		});
	}

	[TestMethod]
	public void TheBackendAdvertisesItselfThroughTryGetRenderer3D()
	{
		ImGuiApp.BeginExternalFrameSession(renderer);

		try
		{
			Assert.IsTrue(ImGuiApp.TryGetRenderer3D(out IRenderer3D? found));
			Assert.AreSame(renderer, found);
		}
		finally
		{
			ImGuiApp.EndExternalFrameSession();
		}

		Assert.IsFalse(ImGuiApp.TryGetRenderer3D(out IRenderer3D? none), "With no backend installed there is no 3D renderer.");
		Assert.IsNull(none);
	}

	private static readonly Rgba32 Red = new(255, 0, 0, 255);
	private static readonly Rgba32 Blue = new(0, 0, 255, 255);
	private const uint PackedRed = 0xFF0000FFu;
	private const uint PackedBlue = 0xFFFF0000u;

	private static Matrix4x4 Ortho() => Matrix4x4.Identity;

	private static Matrix4x4 Perspective() =>
		Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 3f), Vector3.Zero, Vector3.UnitY)
		* Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 1f, 0.1f, 100f);

	private void DrawTriangle(Matrix4x4 mvp, float z, Rgba32 color, DepthMode depth = DepthMode.TestAndWrite, CullMode cull = CullMode.None) =>
		DrawTriangleInto(target, mvp, z, color, depth, cull);

	private void DrawTriangleInto(nint into, Matrix4x4 mvp, float z, Rgba32 color, DepthMode depth = DepthMode.TestAndWrite, CullMode cull = CullMode.None)
	{
		uint packed = (uint)(color.R | (color.G << 8) | (color.B << 16) | (color.A << 24));

		// Counter-clockwise in the y-up sense, which is front-facing.
		Vertex3D[] vertices =
		[
			new(new Vector3(-0.6f, -0.6f, z), Vector2.Zero, packed),
			new(new Vector3(0.6f, -0.6f, z), Vector2.Zero, packed),
			new(new Vector3(0f, 0.6f, z), Vector2.Zero, packed),
		];

		renderer.Draw(into, vertices, [0u, 1u, 2u], new DrawState3D
		{
			ModelViewProjection = mvp,
			Depth = depth,
			Cull = cull,
			Blend = BlendMode.Opaque,
			Topology = PrimitiveTopology.TriangleList,
		});
	}

	private Rgba32 Pixel(int x, int y) => PixelOf(target, x, y);

	private Rgba32 PixelOf(nint which, int x, int y)
	{
		TextureSource source = renderer.GetTexture(renderer.GetTargetTexture(which));
		return source.Pixels.GetPixel(x, y);
	}

	private int CountCovered()
	{
		int covered = 0;

		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				if (Pixel(x, y) != Background)
				{
					covered++;
				}
			}
		}

		return covered;
	}

	private (int Top, int Bottom) CoveredRowSpan(int column)
	{
		int top = -1;
		int bottom = -1;

		for (int y = 0; y < Size; y++)
		{
			if (Pixel(column, y) == Background)
			{
				continue;
			}

			if (top < 0)
			{
				top = y;
			}

			bottom = y;
		}

		Assert.IsGreaterThan(0, top, "Nothing was drawn in the sampled column.");
		Assert.IsGreaterThan(top, bottom, "The drawn shape is one row tall.");

		return (top, bottom);
	}

	/// <summary>The first row where the gradient has passed its halfway colour.</summary>
	/// <param name="column">The column to walk.</param>
	/// <param name="top">First covered row.</param>
	/// <param name="bottom">Last covered row.</param>
	/// <returns>The row index.</returns>
	private int FindHalfwayRow(int column, int top, int bottom)
	{
		for (int y = bottom; y >= top; y--)
		{
			if (Pixel(column, y).B >= 128)
			{
				return y;
			}
		}

		return top;
	}
}

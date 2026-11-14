// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers <see cref="RenderTargetFormat.Rgba16F"/> targets, <see cref="BlendMode.Additive"/> and
/// <see cref="IRenderer3D.ToneMap"/> on the software backend, and the <see cref="ToneMap3D"/>
/// arithmetic every backend has to match.
/// </summary>
/// <remarks>
/// The point of a float target is what eight bits lose: values above one, and contributions too
/// faint to survive rounding. Each test here is built so that an eight-bit target would give a
/// visibly different number, which is what makes it a test of the format rather than of drawing.
/// </remarks>
[TestClass]
public sealed class HdrRenderTargetTests : IDisposable
{
	private const int Size = 16;

	private SoftwareRenderer renderer = null!;

	[TestInitialize]
	public void SetUp() => renderer = new SoftwareRenderer(Size, Size);

	[TestCleanup]
	public void TearDown() => Dispose();

	/// <summary>Releases the renderer. MSTest constructs one instance per test, so this is per test.</summary>
	public void Dispose()
	{
		renderer?.Dispose();
		renderer = null!;
	}

	[TestMethod]
	public void TheSoftwareRendererSupportsBothFormats()
	{
		IRenderer3D asInterface = renderer;
		Assert.IsTrue(asInterface.SupportsFormat(RenderTargetFormat.Rgba8));
		Assert.IsTrue(asInterface.SupportsFormat(RenderTargetFormat.Rgba16F));
	}

	[TestMethod]
	public void AdditiveLightAboveOneSurvivesInAFloatTargetAndSaturatesInAnEightBitOne()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		nint ldr = renderer.CreateRenderTarget(Size, Size, depth: false);
		nint shown = renderer.CreateRenderTarget(Size, Size, depth: false);
		renderer.Clear(hdr, new Vector4(0f, 0f, 0f, 1f), 1f);
		renderer.Clear(ldr, new Vector4(0f, 0f, 0f, 1f), 1f);

		// Four additive layers of half-intensity red: 2.0 in total.
		for (int i = 0; i < 4; i++)
		{
			FillTarget(hdr, 0x7F0000FFu, BlendMode.Additive);
			FillTarget(ldr, 0x7F0000FFu, BlendMode.Additive);
		}

		// Exposure 0.25 brings 2.0 back to 0.5. The eight-bit target already clipped at 1.0, so it
		// comes back as 0.25 instead.
		ToneMap3D quarter = new(0.25f, 1f, ToneCurve.Clamp);
		renderer.ToneMap(hdr, shown, quarter);
		Assert.AreEqual(128, Pixel(shown).R, 2, "The float target lost the light above one.");

		renderer.ToneMap(ldr, shown, quarter);
		Assert.AreEqual(64, Pixel(shown).R, 2, "An eight-bit target should have saturated at one.");
	}

	[TestMethod]
	public void FaintAdditiveLayersAccumulateInAFloatTarget()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(Size, Size, depth: false);
		renderer.Clear(hdr, new Vector4(0f, 0f, 0f, 1f), 1f);

		// Each layer is white at alpha 1/255, adding 1/255 per channel: 200 of them is 200/255.
		for (int i = 0; i < 200; i++)
		{
			FillTarget(hdr, 0x01FFFFFFu, BlendMode.Additive);
		}

		renderer.ToneMap(hdr, shown, ToneMap3D.Default);
		Assert.AreEqual(200, Pixel(shown).G, 1);
	}

	[TestMethod]
	public void AFloatTargetsTextureShowsItsValuesClamped()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		renderer.Clear(hdr, new Vector4(3f, 0.5f, 0f, 1f), 1f);

		Rgba32 shown = Pixel(hdr);
		Assert.AreEqual(255, shown.R);
		Assert.AreEqual(128, shown.G, 1);
	}

	[TestMethod]
	public void ToneMapWritesWhatToneMap3DApplyComputes()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(Size, Size, depth: false);
		Vector4 colour = new(4f, 1f, 0.25f, 1f);
		renderer.Clear(hdr, colour, 1f);

		ToneMap3D toneMap = new(0.5f, 0.8f, ToneCurve.Exponential);
		renderer.ToneMap(hdr, shown, toneMap);

		Vector4 expected = toneMap.Apply(colour);
		Rgba32 actual = Pixel(shown);
		Assert.AreEqual(expected.X * 255f, actual.R, 1f);
		Assert.AreEqual(expected.Y * 255f, actual.G, 1f);
		Assert.AreEqual(expected.Z * 255f, actual.B, 1f);
	}

	[TestMethod]
	public void ResizingAFloatTargetKeepsItAFloatTarget()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		Assert.IsTrue(renderer.ResizeRenderTarget(hdr, Size * 2, Size * 2));

		nint shown = renderer.CreateRenderTarget(Size * 2, Size * 2, depth: false);
		renderer.Clear(hdr, new Vector4(2f, 2f, 2f, 1f), 1f);
		renderer.ToneMap(hdr, shown, new ToneMap3D(0.25f, 1f, ToneCurve.Clamp));

		Assert.AreEqual(128, Pixel(shown).R, 1, "A resize that dropped to eight bits would have clamped 2.0 to 1.0 first.");
	}

	[TestMethod]
	public void ATargetCannotBeToneMappedIntoItself()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		Assert.ThrowsExactly<ArgumentException>(() => renderer.ToneMap(hdr, hdr, ToneMap3D.Default));
	}

	[TestMethod]
	public void ARendererWithoutFloatTargetsSaysSoThroughTheDefaults()
	{
		IRenderer3D minimal = new MinimalRenderer();
		Assert.IsTrue(minimal.SupportsFormat(RenderTargetFormat.Rgba8));
		Assert.IsFalse(minimal.SupportsFormat(RenderTargetFormat.Rgba16F));
		Assert.AreEqual(7, minimal.CreateRenderTarget(1, 1, depth: false, RenderTargetFormat.Rgba8));
		Assert.ThrowsExactly<NotSupportedException>(() => minimal.CreateRenderTarget(1, 1, depth: false, RenderTargetFormat.Rgba16F));
		Assert.ThrowsExactly<NotSupportedException>(() => minimal.ToneMap(1, 2, ToneMap3D.Default));
	}

	[TestMethod]
	[DataRow(ToneCurve.Clamp, 0.5f, 0.5f)]
	[DataRow(ToneCurve.Clamp, 3f, 1f)]
	[DataRow(ToneCurve.Reinhard, 1f, 0.5f)]
	[DataRow(ToneCurve.Reinhard, 3f, 0.75f)]
	[DataRow(ToneCurve.Exponential, 1f, 0.63212055f)]
	public void ToneCurvesMapAsDocumented(ToneCurve curve, float input, float expected) =>
		Assert.AreEqual(expected, new ToneMap3D(1f, 1f, curve).Channel(input), 1e-5f);

	[TestMethod]
	public void ExposureAppliesBeforePowerAndPowerBeforeTheCurve()
	{
		// (2 * 2)^0.5 = 2, then Reinhard: 2 / 3. Any other order gives a different number.
		Assert.AreEqual(2f / 3f, new ToneMap3D(2f, 0.5f, ToneCurve.Reinhard).Channel(2f), 1e-5f);
	}

	[TestMethod]
	public void PreservingHueKeepsTheChannelRatiosOfABrightColour()
	{
		// A warm colour well into saturation: per channel, all three end up near 1 (white); preserving
		// hue maps the brightest and scales the others with it, so the ratios survive.
		Vector4 warm = new(8f, 4f, 2f, 1f);
		ToneMap3D perChannel = new(1f, 1f, ToneCurve.Exponential);
		ToneMap3D preserving = perChannel with { PreserveHue = true };

		Vector4 washed = perChannel.Apply(warm);
		Assert.IsGreaterThan(0.85f, washed.Z, "Per channel, the blue of a bright warm colour saturates too.");

		Vector4 kept = preserving.Apply(warm);
		Assert.AreEqual(perChannel.Channel(8f), kept.X, 1e-5f, "The brightest channel maps as it would on its own.");
		Assert.AreEqual(0.5f, kept.Y / kept.X, 1e-5f);
		Assert.AreEqual(0.25f, kept.Z / kept.X, 1e-5f);
	}

	[TestMethod]
	public void ToneMapPreservingHueMatchesApplyOnTheSoftwareRenderer()
	{
		nint hdr = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		nint shown = renderer.CreateRenderTarget(Size, Size, depth: false);
		Vector4 colour = new(6f, 3f, 1f, 1f);
		renderer.Clear(hdr, colour, 1f);

		ToneMap3D toneMap = new(0.5f, 1f, ToneCurve.Reinhard, PreserveHue: true);
		renderer.ToneMap(hdr, shown, toneMap);

		Vector4 expected = toneMap.Apply(colour);
		Rgba32 actual = Pixel(shown);
		Assert.AreEqual(expected.X * 255f, actual.R, 1f);
		Assert.AreEqual(expected.Y * 255f, actual.G, 1f);
		Assert.AreEqual(expected.Z * 255f, actual.B, 1f);
	}

	[TestMethod]
	public void ToneMappingClampsNegativesAndAlpha()
	{
		Vector4 mapped = new ToneMap3D(1f, 1f, ToneCurve.Exponential).Apply(new Vector4(-1f, 0f, 0f, 2.5f));
		Assert.AreEqual(0f, mapped.X);
		Assert.AreEqual(1f, mapped.W, "Alpha is only clamped, never curved.");
	}

	/// <summary>Draws a quad over the whole target in one colour, packed as ImGui packs one.</summary>
	private void FillTarget(nint target, uint packed, BlendMode blend)
	{
		Vertex3D[] vertices =
		[
			new(new Vector3(-1f, -1f, 0f), Vector2.Zero, packed),
			new(new Vector3(1f, -1f, 0f), Vector2.Zero, packed),
			new(new Vector3(1f, 1f, 0f), Vector2.Zero, packed),
			new(new Vector3(-1f, 1f, 0f), Vector2.Zero, packed),
		];

		renderer.Draw(target, vertices, [0u, 1u, 2u, 0u, 2u, 3u], new DrawState3D
		{
			ModelViewProjection = Matrix4x4.Identity,
			Blend = blend,
		});
	}

	private Rgba32 Pixel(nint target) => renderer.GetTexture(renderer.GetTargetTexture(target)).Pixels.GetPixel(Size / 2, Size / 2);

	/// <summary>An implementation written before float targets existed: only the required members.</summary>
	private sealed class MinimalRenderer : IRenderer3D
	{
		public nint CreateRenderTarget(int width, int height, bool depth) => 7;

		public bool ResizeRenderTarget(nint target, int width, int height) => false;

		public void DeleteRenderTarget(nint target)
		{
		}

		public nint GetTargetTexture(nint target) => target;

		public void Clear(nint target, Vector4 color, float depth)
		{
		}

		public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state)
		{
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.Numerics;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers volume textures and <see cref="IRenderer3D.DrawVolume"/> on the software backend.
/// </summary>
/// <remarks>
/// Each scene uses the identity view-projection, so clip space is world space and every pixel's ray
/// runs straight along Z from depth 0 to depth 1, one world unit long. A volume of constant value
/// over a slab of known thickness then integrates to value × thickness, a number the test can state.
/// </remarks>
[TestClass]
public sealed class VolumeTests : IDisposable
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
	public void AConstantSlabIntegratesToValueTimesThickness()
	{
		// A slab from z = 0.25 to 0.75 covering the whole view, value 0.8: 0.8 × 0.5 = 0.4.
		nint target = HdrTarget();
		nint volume = Constant(4, 4, 4, 0.8f);

		renderer.DrawVolume(target, volume, State(Slab(-1f, 1f, 0.25f, 0.75f)));

		Assert.AreEqual(0.4f, Shown(target, Size / 2, Size / 2), 0.01f);
	}

	[TestMethod]
	public void RaysThatMissTheBoxAddNothing()
	{
		nint target = HdrTarget();
		nint volume = Constant(4, 4, 4, 1f);

		// The box covers only the right half of the view.
		renderer.DrawVolume(target, volume, State(Matrix4x4.CreateScale(1f, 2f, 0.5f) * Matrix4x4.CreateTranslation(0f, -1f, 0.25f)));

		Assert.AreEqual(0f, Shown(target, 2, Size / 2), 1e-6f);
		Assert.AreEqual(0.5f, Shown(target, Size - 3, Size / 2), 0.01f);
	}

	[TestMethod]
	public void TheFadeSuppressesEmissionNearTheEye()
	{
		// Within 0.5 of the eye nothing is emitted, so only the slab's far half (0.5 to 0.75) counts.
		nint target = HdrTarget();
		nint volume = Constant(4, 4, 4, 1f);
		VolumeDrawState state = State(Slab(-1f, 1f, 0.25f, 0.75f)) with { Eye = new Vector3(0f, 0f, 0f), FadeInStart = 0.5f, FadeInEnd = 0.5001f };

		renderer.DrawVolume(target, volume, state);

		Assert.AreEqual(0.25f, Shown(target, Size / 2, Size / 2), 0.02f);
	}

	[TestMethod]
	public void AdditivePassesAccumulate()
	{
		nint target = HdrTarget();
		nint volume = Constant(2, 2, 2, 1f);
		VolumeDrawState state = State(Slab(-1f, 1f, 0f, 0.3f));

		renderer.DrawVolume(target, volume, state);
		renderer.DrawVolume(target, volume, state);

		Assert.AreEqual(0.6f, Shown(target, Size / 2, Size / 2), 0.01f);
	}

	[TestMethod]
	public void TheColourScalesTheEmission()
	{
		nint target = HdrTarget();
		nint volume = Constant(2, 2, 2, 1f);

		renderer.DrawVolume(target, volume, State(Slab(-1f, 1f, 0f, 0.5f)) with { Color = new Vector4(0.5f, 1f, 0f, 1f) });

		Vector4 pixel = Hdr(target, Size / 2, Size / 2);
		Assert.AreEqual(0.25f, pixel.X, 0.01f);
		Assert.AreEqual(0.5f, pixel.Y, 0.01f);
		Assert.AreEqual(0f, pixel.Z, 1e-6f);
	}

	[TestMethod]
	public void SamplesAreTrilinear()
	{
		// Two voxels along X, 0 and 1: halfway between their centres the value is 0.5.
		nint target = HdrTarget();
		nint volume = renderer.CreateVolumeTexture(2, 1, 1, [(Half)0f, (Half)1f]);

		renderer.DrawVolume(target, volume, State(Slab(-1f, 1f, 0f, 1f)));

		// Pixel 8 of 16 is centred at x = 0.0625 in NDC, texture u = 0.53125: 0.5625 voxels past the
		// first voxel's centre, so a trilinear sample there is 0.5625.
		Assert.AreEqual(0.5625f, Hdr(target, Size / 2, Size / 2).X, 0.01f, "The middle of the view samples between the two voxel centres.");
		Assert.IsGreaterThan(Hdr(target, 2, Size / 2).X, Hdr(target, Size - 3, Size / 2).X, "The value rises along X.");
	}

	[TestMethod]
	public void NoiseVariesThePixelsButKeepsTheirAverage()
	{
		// A constant slab, 0.25 thick at value 1, with strong noise: pixels differ, but over the view
		// the multiplier averages about 1, so the mean stays near 0.25.
		nint target = HdrTarget();
		nint volume = Constant(2, 2, 2, 1f);
		renderer.DrawVolume(target, volume, State(Slab(-1f, 1f, 0f, 0.25f)) with { NoiseAmplitude = 0.8f, NoiseScale = 0.05f });

		float min = float.MaxValue;
		float max = float.MinValue;
		float sum = 0f;
		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				float value = Hdr(target, x, y).X;
				min = MathF.Min(min, value);
				max = MathF.Max(max, value);
				sum += value;
			}
		}

		Assert.IsGreaterThan(0.05f, max - min, "Noise should vary the pixels.");
		Assert.AreEqual(0.25f, sum / (Size * Size), 0.05f, "Noise should not change the average brightness much.");
	}

	[TestMethod]
	public void NoiseFadesOutWhereItsCellsAreUnderAPixel()
	{
		VolumeRay ray = VolumeRay.Create(new VolumeDrawState { NoiseAmplitude = 1f, NoiseScale = 1f, PixelAngle = 1f });

		// At distance 10 a pixel is 10 units: cells of 1 are far below a pixel, so no grain.
		Assert.AreEqual(1f, ray.Grain(new Vector3(0.3f, 0.7f, 0.2f), 10f), 1e-6f);
		Assert.AreNotEqual(1f, ray.Grain(new Vector3(0.3f, 0.7f, 0.2f), 0.01f), "Close up, the noise applies.");
	}

	[TestMethod]
	public void NoiseIsDeterministicAndInTheUnitInterval()
	{
		for (int i = 0; i < 1000; i++)
		{
			Vector3 p = new(i * 0.731f, -i * 0.377f, i * 1.913f);
			float n = VolumeRay.Noise(p);
			Assert.IsTrue(n is >= 0f and < 1f, $"Noise {n} at {p}.");
			Assert.AreEqual(n, VolumeRay.Noise(p));
		}
	}

	[TestMethod]
	public void JitterStaysInTheUnitInterval()
	{
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				float jitter = VolumeRay.Jitter(x, y);
				Assert.IsTrue(jitter is >= 0f and < 1f, $"Jitter {jitter} at ({x}, {y}).");
			}
		}
	}

	[TestMethod]
	public void AVolumeOfTheWrongSizeIsRejectedAndAnUnknownOneToo()
	{
		Assert.ThrowsExactly<ArgumentException>(() => renderer.CreateVolumeTexture(2, 2, 2, new Half[7]));
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DeleteVolumeTexture(12345));
		Assert.ThrowsExactly<ArgumentException>(() => renderer.DrawVolume(HdrTarget(), 12345, State(Matrix4x4.Identity)));
	}

	[TestMethod]
	public void ARendererWithoutVolumesSaysSoThroughTheDefaults()
	{
		IRenderer3D minimal = new MinimalRenderer();
		Assert.IsFalse(minimal.SupportsVolumes);
		Assert.ThrowsExactly<NotSupportedException>(() => minimal.CreateVolumeTexture(1, 1, 1, new Half[1]));
		Assert.ThrowsExactly<NotSupportedException>(() => minimal.DrawVolume(1, 2, default));
	}

	private static VolumeDrawState State(Matrix4x4 volumeToWorld) => new()
	{
		ViewProjection = Matrix4x4.Identity,
		VolumeToWorld = volumeToWorld,
		Color = Vector4.One,
		SamplesPerVoxel = 16,
		Blend = BlendMode.Additive,
	};

	/// <summary>A box covering [min, max] in X and Y and [z0, z1] in Z.</summary>
	private static Matrix4x4 Slab(float min, float max, float z0, float z1) =>
		Matrix4x4.CreateScale(max - min, max - min, z1 - z0) * Matrix4x4.CreateTranslation(min, min, z0);

	private nint HdrTarget()
	{
		nint target = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		renderer.Clear(target, new Vector4(0f, 0f, 0f, 1f), 1f);
		return target;
	}

	private nint Constant(int width, int height, int depth, float value)
	{
		Half[] values = new Half[width * height * depth];
		Array.Fill(values, (Half)value);
		return renderer.CreateVolumeTexture(width, height, depth, values);
	}

	/// <summary>The red channel of a pixel, read through an identity tone map into an eight-bit target.</summary>
	private float Shown(nint target, int x, int y) => Hdr(target, x, y).X;

	private Vector4 Hdr(nint target, int x, int y)
	{
		nint shown = renderer.CreateRenderTarget(Size, Size, depth: false, RenderTargetFormat.Rgba16F);
		renderer.ToneMap(target, shown, new ToneMap3D(1f, 1f, ToneCurve.Clamp));
		Rgba32 pixel = renderer.GetTexture(renderer.GetTargetTexture(shown)).Pixels.GetPixel(x, y);
		renderer.DeleteRenderTarget(shown);
		return new Vector4(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f);
	}

	/// <summary>An implementation written before volumes existed: only the required members.</summary>
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

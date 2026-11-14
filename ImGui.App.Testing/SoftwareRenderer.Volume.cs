// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;
using ktsu.ImGui.Rendering;

/// <summary>
/// The software backend's volume textures and emissive ray-marching pass.
/// </summary>
/// <remarks>
/// This is the reference for the arithmetic: the OpenGL shader does the same per fragment, and
/// <see cref="VolumeRay"/> is shared so a test can state the expected pixel.
/// </remarks>
public sealed partial class SoftwareRenderer
{
	private readonly Dictionary<nint, VolumeData> volumes = [];

	/// <summary>Gets whether this renderer supports volumes: it does.</summary>
	public bool SupportsVolumes => true;

	/// <summary>Creates a volume texture.</summary>
	/// <param name="width">Voxels along X.</param>
	/// <param name="height">Voxels along Y.</param>
	/// <param name="depth">Voxels along Z.</param>
	/// <param name="values">The values, X fastest, then Y, then Z.</param>
	/// <returns>A handle, released with <see cref="DeleteVolumeTexture"/>.</returns>
	/// <exception cref="ArgumentException">The span is not <c>width · height · depth</c> long.</exception>
	public nint CreateVolumeTexture(int width, int height, int depth, ReadOnlySpan<Half> values)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
		if (values.Length != width * height * depth)
		{
			throw new ArgumentException($"Expected {width * height * depth} values, got {values.Length}.", nameof(values));
		}

		float[] data = new float[values.Length];
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = (float)values[i];
		}

		nint handle = nextId++;
		volumes[handle] = new VolumeData(width, height, depth, data);
		return handle;
	}

	/// <summary>Releases a volume texture.</summary>
	/// <param name="volume">A handle from <see cref="CreateVolumeTexture"/>.</param>
	/// <exception cref="ArgumentException">The handle is not a volume.</exception>
	public void DeleteVolumeTexture(nint volume)
	{
		if (!volumes.Remove(volume))
		{
			throw new ArgumentException($"No volume texture with handle {volume}.", nameof(volume));
		}
	}

	/// <summary>Ray-marches an emissive volume over every pixel of a target.</summary>
	/// <param name="target">The target.</param>
	/// <param name="volume">The volume.</param>
	/// <param name="state">The pass.</param>
	/// <exception cref="ArgumentException">Either handle is unknown.</exception>
	public void DrawVolume(nint target, nint volume, in VolumeDrawState state)
	{
		RenderTarget3D resolved = Resolve(target, nameof(target));
		if (!volumes.TryGetValue(volume, out VolumeData? data))
		{
			throw new ArgumentException($"No volume texture with handle {volume}.", nameof(volume));
		}

		RasterizeDeferredDrawData();

		int width = resolved.Color.Width;
		int height = resolved.Color.Height;
		VolumeRay ray = VolumeRay.Create(state);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float column = ray.Integrate(x, y, width, height, data.Sample, data.Width, data.Height, data.Depth);
				if (column <= 0f)
				{
					continue;
				}

				Vector4 emitted = new(state.Color.X * column, state.Color.Y * column, state.Color.Z * column, Math.Clamp(state.Color.W * column, 0f, 1f));
				Vector4 source = state.Blend == BlendMode.Additive ? emitted with { W = 1f } : emitted;
				SoftwareRasterizer.WriteFloat(resolved.Color, resolved.Hdr, x, y, source, state.Blend);
			}
		}
	}

	/// <summary>A volume's voxels, sampled trilinearly with clamp-to-edge, as a GPU samples a 3D texture.</summary>
	private sealed class VolumeData(int width, int height, int depth, float[] values)
	{
		public int Width { get; } = width;

		public int Height { get; } = height;

		public int Depth { get; } = depth;

		public float Sample(Vector3 uvw)
		{
			float fx = (uvw.X * Width) - 0.5f;
			float fy = (uvw.Y * Height) - 0.5f;
			float fz = (uvw.Z * Depth) - 0.5f;
			int x0 = (int)MathF.Floor(fx);
			int y0 = (int)MathF.Floor(fy);
			int z0 = (int)MathF.Floor(fz);
			float tx = fx - x0;
			float ty = fy - y0;
			float tz = fz - z0;

			float c00 = Lerp(At(x0, y0, z0), At(x0 + 1, y0, z0), tx);
			float c10 = Lerp(At(x0, y0 + 1, z0), At(x0 + 1, y0 + 1, z0), tx);
			float c01 = Lerp(At(x0, y0, z0 + 1), At(x0 + 1, y0, z0 + 1), tx);
			float c11 = Lerp(At(x0, y0 + 1, z0 + 1), At(x0 + 1, y0 + 1, z0 + 1), tx);
			return Lerp(Lerp(c00, c10, ty), Lerp(c01, c11, ty), tz);
		}

		private float At(int x, int y, int z) =>
			values[(((Math.Clamp(z, 0, Depth - 1) * Height) + Math.Clamp(y, 0, Height - 1)) * Width) + Math.Clamp(x, 0, Width - 1)];

		private static float Lerp(float a, float b, float t) => a + ((b - a) * t);
	}
}

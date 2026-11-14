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
	/// <param name="channels">Values per voxel: 1 or 4.</param>
	/// <param name="values">The values, channels interleaved, then X fastest, then Y, then Z.</param>
	/// <returns>A handle, released with <see cref="DeleteVolumeTexture"/>.</returns>
	/// <exception cref="ArgumentException">The span is the wrong length, or the channel count is not 1 or 4.</exception>
	public nint CreateVolumeTexture(int width, int height, int depth, int channels, ReadOnlySpan<Half> values)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
		if (channels is not (1 or 4))
		{
			throw new ArgumentException($"A volume has 1 or 4 channels, not {channels}.", nameof(channels));
		}

		if (values.Length != width * height * depth * channels)
		{
			throw new ArgumentException($"Expected {width * height * depth * channels} values, got {values.Length}.", nameof(values));
		}

		float[] data = new float[values.Length];
		for (int i = 0; i < data.Length; i++)
		{
			data[i] = (float)values[i];
		}

		nint handle = nextId++;
		volumes[handle] = new VolumeData(width, height, depth, channels, data);
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

	/// <summary>Ray-marches a volume over every pixel of a target.</summary>
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
				VolumeIntegral integral = ray.Integrate(x, y, width, height, data.Sample, data.Width, data.Height, data.Depth);
				Vector4 output = integral.Output(state);
				if (output.X <= 0f && output.Y <= 0f && output.Z <= 0f && (state.Blend == BlendMode.Additive || output.W <= 0f))
				{
					continue;
				}

				SoftwareRasterizer.WriteFloat(resolved.Color, resolved.Hdr, x, y, output, state.Blend);
			}
		}
	}

	/// <summary>A volume's voxels, sampled trilinearly with clamp-to-edge, as a GPU samples a 3D texture.</summary>
	private sealed class VolumeData(int width, int height, int depth, int channels, float[] values)
	{
		public int Width { get; } = width;

		public int Height { get; } = height;

		public int Depth { get; } = depth;

		public Vector4 Sample(Vector3 uvw)
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

			Vector4 c00 = Vector4.Lerp(At(x0, y0, z0), At(x0 + 1, y0, z0), tx);
			Vector4 c10 = Vector4.Lerp(At(x0, y0 + 1, z0), At(x0 + 1, y0 + 1, z0), tx);
			Vector4 c01 = Vector4.Lerp(At(x0, y0, z0 + 1), At(x0 + 1, y0, z0 + 1), tx);
			Vector4 c11 = Vector4.Lerp(At(x0, y0 + 1, z0 + 1), At(x0 + 1, y0 + 1, z0 + 1), tx);
			return Vector4.Lerp(Vector4.Lerp(c00, c10, ty), Vector4.Lerp(c01, c11, ty), tz);
		}

		private Vector4 At(int x, int y, int z)
		{
			int i = ((((Math.Clamp(z, 0, Depth - 1) * Height) + Math.Clamp(y, 0, Height - 1)) * Width) + Math.Clamp(x, 0, Width - 1)) * channels;
			return channels == 1 ? new Vector4(values[i], 0f, 0f, 0f) : new Vector4(values[i], values[i + 1], values[i + 2], values[i + 3]);
		}
	}
}

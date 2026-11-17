// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

/// <summary>
/// The colour storage of a <see cref="ktsu.ImGui.Rendering.RenderTargetFormat.Rgba16F"/> target on the
/// software renderer: four floats per pixel, unclamped.
/// </summary>
/// <remarks>
/// Full single precision rather than an emulation of half floats. What a test of a high-range
/// target checks is that values above one and contributions below 1/255 survive, which single
/// precision keeps as well; matching half precision bit for bit would buy nothing a test can see.
/// </remarks>
/// <param name="width">Width in pixels.</param>
/// <param name="height">Height in pixels.</param>
internal sealed class HdrBuffer(int width, int height)
{
	private readonly float[] values = new float[width * height * 4];

	public int Width { get; } = width;

	public int Height { get; } = height;

	public Vector4 this[int x, int y]
	{
		get
		{
			int i = ((y * Width) + x) * 4;
			return new Vector4(values[i], values[i + 1], values[i + 2], values[i + 3]);
		}

		set
		{
			int i = ((y * Width) + x) * 4;
			values[i] = value.X;
			values[i + 1] = value.Y;
			values[i + 2] = value.Z;
			values[i + 3] = value.W;
		}
	}

	public void Clear(Vector4 color)
	{
		for (int i = 0; i < values.Length; i += 4)
		{
			values[i] = color.X;
			values[i + 1] = color.Y;
			values[i + 2] = color.Z;
			values[i + 3] = color.W;
		}
	}

	/// <summary>The value an eight-bit view of this pixel shows: each channel clamped to [0, 1].</summary>
	public static Rgba32 ToRgba32(Vector4 color) => new(Byte(color.X), Byte(color.Y), Byte(color.Z), Byte(color.W));

	private static byte Byte(float value) => (byte)Math.Clamp(MathF.Round(value * 255f), 0f, 255f);
}

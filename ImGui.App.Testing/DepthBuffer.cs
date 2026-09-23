// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// A single-precision depth attachment, the companion to <see cref="Bitmap32"/>.
/// </summary>
/// <remarks>
/// Depth runs from 0 at the near plane to 1 at the far plane, and the test is "less is closer".
/// That is the convention <see cref="System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView"/>
/// produces — the .NET matrices are Direct3D-style with a [0, 1] depth range rather than OpenGL's
/// [-1, 1] — and taking it from the matrix helper rather than from a graphics API is what keeps the
/// CPU path and a future GPU path agreeing about what a depth value means.
/// </remarks>
public sealed class DepthBuffer
{
	private readonly float[] depths;

	/// <summary>Creates a buffer, cleared to the far plane.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <exception cref="ArgumentOutOfRangeException">A dimension is not positive.</exception>
	public DepthBuffer(int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		Width = width;
		Height = height;
		depths = new float[width * height];
		Clear(1f);
	}

	/// <summary>Gets the width in pixels.</summary>
	public int Width { get; }

	/// <summary>Gets the height in pixels.</summary>
	public int Height { get; }

	/// <summary>Gets the raw depth values, row-major.</summary>
	public Span<float> Depths => depths;

	/// <summary>Fills the buffer with one value.</summary>
	/// <param name="depth">The value, usually 1 for the far plane.</param>
	public void Clear(float depth) => Array.Fill(depths, depth);

	/// <summary>Reads one depth value.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The stored depth.</returns>
	public float this[int x, int y] => depths[(y * Width) + x];
}

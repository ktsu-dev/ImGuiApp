// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using ktsu.ImGuiApp.ImGuiController;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

/// <summary>
/// A test implementation of IGL for testing purposes.
/// </summary>
public sealed class TestGL : IGL
{
	private bool _disposed;

	/// <inheritdoc/>
	public void GetInteger(GLEnum pname, out int data)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		data = 0;
	}

	/// <inheritdoc/>
	public float GetFloat(GLEnum pname)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return 0;
	}

	/// <inheritdoc/>
	public void Enable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void Disable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void BlendEquation(GLEnum mode)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void Viewport(Vector2D<int> size)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void ClearColor(Color color)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void Clear(uint mask)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void BindTexture(GLEnum target, uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void DeleteTexture(uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public uint GenTexture()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return 1;
	}

	/// <inheritdoc/>
	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public unsafe void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
		}
	}
}

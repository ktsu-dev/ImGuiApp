// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Test;

using ktsu.ImGui.App.ImGuiController;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

/// <summary>
/// A test implementation of IGL for testing purposes.
/// </summary>
public sealed unsafe class TestGL : IGL
{
	private bool _disposed;

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	public void GetInteger(GLEnum pname, out int data)
	{
		ThrowIfDisposed();
		data = 0;
	}

	public float GetFloat(GLEnum pname)
	{
		ThrowIfDisposed();
		return 0;
	}

	public void Enable(GLEnum cap)
	{
		ThrowIfDisposed();
	}

	public void Disable(GLEnum cap)
	{
		ThrowIfDisposed();
	}

	public void BlendEquation(GLEnum mode)
	{
		ThrowIfDisposed();
	}

	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		ThrowIfDisposed();
	}

	public void Viewport(Vector2D<int> size)
	{
		ThrowIfDisposed();
	}

	public void ClearColor(Color color)
	{
		ThrowIfDisposed();
	}

	public void Clear(uint mask)
	{
		ThrowIfDisposed();
	}

	public void BindTexture(GLEnum target, uint texture)
	{
		ThrowIfDisposed();
	}

	public void DeleteTexture(uint texture)
	{
		ThrowIfDisposed();
	}

	public uint GenTexture()
	{
		ThrowIfDisposed();
		return 1;
	}

	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ThrowIfDisposed();
	}

	public void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ThrowIfDisposed();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
		}
	}
}

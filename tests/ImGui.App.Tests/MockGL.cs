// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.Test;

using ktsu.ImGui.App.ImGuiController;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

/// <summary>
/// A mock GL implementation that delegates to TestGL.
/// </summary>
public sealed unsafe class MockGL(TestGL testGL) : IGL
{
	private readonly TestGL _testGL = testGL ?? throw new ArgumentNullException(nameof(testGL));
	private bool _disposed;

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	public void GetInteger(GLEnum pname, out int data)
	{
		ThrowIfDisposed();
		_testGL.GetInteger(pname, out data);
	}

	public float GetFloat(GLEnum pname)
	{
		ThrowIfDisposed();
		return _testGL.GetFloat(pname);
	}

	public void Enable(GLEnum cap)
	{
		ThrowIfDisposed();
		_testGL.Enable(cap);
	}

	public void Disable(GLEnum cap)
	{
		ThrowIfDisposed();
		_testGL.Disable(cap);
	}

	public void BlendEquation(GLEnum mode)
	{
		ThrowIfDisposed();
		_testGL.BlendEquation(mode);
	}

	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		ThrowIfDisposed();
		_testGL.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
	}

	public void Viewport(Vector2D<int> size)
	{
		ThrowIfDisposed();
		_testGL.Viewport(size);
	}

	public void ClearColor(Color color)
	{
		ThrowIfDisposed();
		_testGL.ClearColor(color);
	}

	public void Clear(uint mask)
	{
		ThrowIfDisposed();
		_testGL.Clear(mask);
	}

	public void BindTexture(GLEnum target, uint texture)
	{
		ThrowIfDisposed();
		_testGL.BindTexture(target, texture);
	}

	public void DeleteTexture(uint texture)
	{
		ThrowIfDisposed();
		_testGL.DeleteTexture(texture);
	}

	public uint GenTexture()
	{
		ThrowIfDisposed();
		return _testGL.GenTexture();
	}

	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ThrowIfDisposed();
		_testGL.TexParameter(target, pname, param);
	}

	public void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ThrowIfDisposed();
		_testGL.TexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_testGL.Dispose();
			_disposed = true;
		}
	}
}

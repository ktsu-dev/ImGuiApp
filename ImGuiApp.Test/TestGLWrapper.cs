// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

/// <summary>
/// A test wrapper around TestGL that provides GL conversion capabilities for testing purposes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestGLWrapper"/> class.
/// </remarks>
/// <param name="testGL">The test GL implementation to delegate to.</param>
public sealed class TestGLWrapper(TestGL testGL) : IDisposable
{
	private readonly TestGL _testGL = testGL ?? throw new ArgumentNullException(nameof(testGL));
	private bool _disposed;

	public static implicit operator GL(TestGLWrapper impl)
	{
		ArgumentNullException.ThrowIfNull(impl);
		ObjectDisposedException.ThrowIf(impl._disposed, impl);

		return new GL(null);
	}

	/// <inheritdoc/>
	public void GetInteger(GLEnum pname, out int data)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.GetInteger(pname, out data);
	}

	/// <inheritdoc/>
	public float GetFloat(GLEnum pname)
	{
		return _disposed ? throw new ObjectDisposedException(nameof(TestGLWrapper)) : _testGL.GetFloat(pname);
	}

	/// <inheritdoc/>
	public void Enable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.Enable(cap);
	}

	/// <inheritdoc/>
	public void Disable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.Disable(cap);
	}

	/// <inheritdoc/>
	public void BlendEquation(GLEnum mode)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.BlendEquation(mode);
	}

	/// <inheritdoc/>
	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
	}

	/// <inheritdoc/>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public void Viewport(int x, int y, uint width, uint height)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.Viewport(new Vector2D<int>((int)width, (int)height));
	}

	/// <inheritdoc/>
	public void ClearColor(float red, float green, float blue, float alpha)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.ClearColor(Color.FromArgb((int)(alpha * 255), (int)(red * 255), (int)(green * 255), (int)(blue * 255)));
	}

	/// <inheritdoc/>
	public void Clear(uint mask)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.Clear(mask);
	}

	/// <inheritdoc/>
	public void BindTexture(GLEnum target, uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.BindTexture(target, texture);
	}

	/// <inheritdoc/>
	public void DeleteTexture(uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.DeleteTexture(texture);
	}

	/// <inheritdoc/>
	public uint GenTexture()
	{
		return _disposed ? throw new ObjectDisposedException(nameof(TestGLWrapper)) : _testGL.GenTexture();
	}

	/// <inheritdoc/>
	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.TexParameter(target, pname, param);
	}

	/// <inheritdoc/>
	public unsafe void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		_testGL.TexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!_disposed)
		{
			_testGL.Dispose();
			_disposed = true;
		}
	}
}

// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.ImGuiController;

using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

/// <summary>
/// Wrapper class that implements IGL by delegating to a real GL instance.
/// </summary>
public sealed class GLWrapper(GL gl) : IGL
{
	internal bool _disposed;

	/// <summary>
	/// Gets the underlying GL instance.
	/// </summary>
	public GL UnderlyingGL { get; } = Ensure.NotNull(gl);

	/// <inheritdoc/>
	public void GetInteger(GLEnum pname, out int data)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.GetInteger(pname, out data);
	}

	/// <inheritdoc/>
	public float GetFloat(GLEnum pname)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return UnderlyingGL.GetFloat(pname);
	}

	/// <inheritdoc/>
	public void Enable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.Enable(cap);
	}

	/// <inheritdoc/>
	public void Disable(GLEnum cap)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.Disable(cap);
	}

	/// <inheritdoc/>
	public void BlendEquation(GLEnum mode)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.BlendEquation(mode);
	}

	/// <inheritdoc/>
	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
	}

	/// <inheritdoc/>
	public void Viewport(Vector2D<int> size)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.Viewport(size);
	}

	/// <inheritdoc/>
	public void ClearColor(Color color)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
	}

	/// <inheritdoc/>
	public void Clear(uint mask)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.Clear(mask);
	}

	/// <inheritdoc/>
	public void BindTexture(GLEnum target, uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.BindTexture(target, texture);
	}

	/// <inheritdoc/>
	public void DeleteTexture(uint texture)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.DeleteTexture(texture);
	}

	/// <inheritdoc/>
	public uint GenTexture()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return UnderlyingGL.GenTexture();
	}

	/// <inheritdoc/>
	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.TexParameter(target, pname, param);
	}

	/// <inheritdoc/>
	public unsafe void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		UnderlyingGL.TexImage2D(target, level, internalformat, width, height, border, format, type, pixels);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!_disposed)
		{
			UnderlyingGL.Dispose();
			_disposed = true;
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Collections.ObjectModel;
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

	/// <summary>
	/// Gets the texture calls this instance has received, in order, so a test can assert on the
	/// sequence a backend issues rather than only on its outcome.
	/// </summary>
	public Collection<string> Calls { get; } = [];

	/// <summary>Gets or sets the name handed out by the next <see cref="GenTexture"/>.</summary>
	public uint NextTextureName { get; set; } = 1;

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
		Calls.Add($"BindTexture({texture})");
	}

	public void DeleteTexture(uint texture)
	{
		ThrowIfDisposed();
		Calls.Add($"DeleteTexture({texture})");
	}

	public uint GenTexture()
	{
		ThrowIfDisposed();
		Calls.Add($"GenTexture->{NextTextureName}");
		return NextTextureName;
	}

	public void TexParameter(GLEnum target, GLEnum pname, int param)
	{
		ThrowIfDisposed();
		Calls.Add($"TexParameter({pname},{param})");
	}

	public void PixelStore(GLEnum pname, int param)
	{
		ThrowIfDisposed();
		Calls.Add($"PixelStore({pname},{param})");
	}

	public void CheckError(string title)
	{
		ThrowIfDisposed();
	}

	public void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels)
	{
		ThrowIfDisposed();
		Calls.Add($"TexImage2D({width}x{height})");
	}

	public void TexSubImage2D(GLEnum target, int level, int xoffset, int yoffset, uint width, uint height, GLEnum format, GLEnum type, void* pixels)
	{
		ThrowIfDisposed();
		Calls.Add($"TexSubImage2D({width}x{height}@{xoffset},{yoffset})");
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
		}
	}
}

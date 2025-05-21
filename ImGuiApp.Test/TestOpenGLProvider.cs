// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Test;

using Silk.NET.OpenGL;

/// <summary>
/// A test implementation of IOpenGLProvider that returns a mock GL instance.
/// </summary>
public sealed class TestOpenGLProvider(IGL mockGL) : IOpenGLProvider
{
	private readonly IGL _mockGL = mockGL ?? throw new ArgumentNullException(nameof(mockGL));
	private bool _disposed;

	public IGL GetGL()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return _mockGL;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_mockGL.Dispose();
			_disposed = true;
		}
	}

	GL IOpenGLProvider.GetGL() => throw new NotImplementedException();
}

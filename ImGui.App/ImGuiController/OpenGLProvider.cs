// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.ImGuiController;

/// <summary>
/// Provides access to OpenGL functionality.
/// </summary>
public sealed class OpenGLProvider(IOpenGLFactory factory) : IDisposable
{
	internal readonly IOpenGLFactory _factory = Ensure.NotNull(factory);
	internal GLWrapper? _gl;
	internal bool _disposed;

	/// <summary>
	/// Gets the OpenGL instance, creating it if necessary.
	/// </summary>
	/// <returns>The OpenGL instance.</returns>
	/// <exception cref="ObjectDisposedException">Thrown when the provider has been disposed.</exception>
	public IGL GetGL()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (_gl == null)
		{
			Silk.NET.OpenGL.GL gl = _factory.CreateGL();
			_gl = new GLWrapper(gl);
		}

		return _gl;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!_disposed)
		{
			_gl?.Dispose();
			_gl = null;
			_disposed = true;
		}
	}
}

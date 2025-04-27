// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.ImGuiController;

/// <summary>
/// Provides access to OpenGL functionality.
/// </summary>
public sealed class OpenGLProvider(IOpenGLFactory factory) : IDisposable
{
	private readonly IOpenGLFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
	private GLWrapper? _gl;
	private bool _disposed;

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
			var gl = _factory.CreateGL();
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

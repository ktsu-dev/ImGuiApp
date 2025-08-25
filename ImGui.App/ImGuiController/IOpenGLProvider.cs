// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;
using Silk.NET.OpenGL;

/// <summary>
/// Provides an interface for accessing an OpenGL context.
/// </summary>
public interface IOpenGLProvider : IDisposable
{
	/// <summary>
	/// Retrieves the OpenGL context.
	/// </summary>
	/// <returns>An instance of the <see cref="GL"/> class representing the OpenGL context.</returns>
	public GL GetGL();
}

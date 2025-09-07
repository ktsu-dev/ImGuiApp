// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App.ImGuiController;

using Silk.NET.OpenGL;
using Silk.NET.Windowing;

/// <summary>
/// Implementation of <see cref="IOpenGLFactory"/> that creates OpenGL contexts from a window.
/// </summary>
/// <param name="window">The window to create OpenGL contexts from.</param>
/// <exception cref="ArgumentNullException">Thrown when window is null.</exception>
public class WindowOpenGLFactory(IWindow window) : IOpenGLFactory
{
	internal readonly IWindow _window = window ?? throw new ArgumentNullException(nameof(window));

	/// <inheritdoc/>
	public GL CreateGL() => GL.GetApi(_window);
}

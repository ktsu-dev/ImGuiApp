// Copyright (c) 2023-2026 ktsu.dev contributors

namespace ktsu.ImGui.App.ImGuiController;

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

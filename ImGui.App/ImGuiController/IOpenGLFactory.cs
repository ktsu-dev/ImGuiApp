// Copyright (c) 2023-2026 ktsu.dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using Silk.NET.OpenGL;

/// <summary>
/// Interface for creating OpenGL contexts.
/// </summary>
public interface IOpenGLFactory
{
	/// <summary>
	/// Creates an OpenGL context.
	/// </summary>
	/// <returns>The created OpenGL context.</returns>
	public GL CreateGL();
}

// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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

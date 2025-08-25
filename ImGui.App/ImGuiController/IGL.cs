// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.ImGuiController;

using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Color = System.Drawing.Color;

/// <summary>
/// Interface that abstracts OpenGL functionality for testing purposes.
/// </summary>
public interface IGL : IDisposable
{
	/// <summary>
	/// Gets an integer parameter value.
	/// </summary>
	/// <param name="pname">The parameter name.</param>
	/// <param name="data">The output parameter value.</param>
	public void GetInteger(GLEnum pname, out int data);

	/// <summary>
	/// Gets a float parameter value.
	/// </summary>
	/// <param name="pname">The parameter name.</param>
	/// <returns>The parameter value.</returns>
	public float GetFloat(GLEnum pname);

	/// <summary>
	/// Enables a GL capability.
	/// </summary>
	/// <param name="cap">The capability to enable.</param>
	public void Enable(GLEnum cap);

	/// <summary>
	/// Disables a GL capability.
	/// </summary>
	/// <param name="cap">The capability to disable.</param>
	public void Disable(GLEnum cap);

	/// <summary>
	/// Sets the blend equation.
	/// </summary>
	/// <param name="mode">The blend equation mode.</param>
	public void BlendEquation(GLEnum mode);

	/// <summary>
	/// Sets the blend function parameters.
	/// </summary>
	public void BlendFuncSeparate(GLEnum srcRGB, GLEnum dstRGB, GLEnum srcAlpha, GLEnum dstAlpha);

	/// <summary>
	/// Sets the viewport dimensions.
	/// </summary>
	public void Viewport(Vector2D<int> size);

	/// <summary>
	/// Sets the clear color.
	/// </summary>
	public void ClearColor(Color color);

	/// <summary>
	/// Clears the specified buffer bits.
	/// </summary>
	public void Clear(uint mask);

	/// <summary>
	/// Binds a texture to the specified target.
	/// </summary>
	public void BindTexture(GLEnum target, uint texture);

	/// <summary>
	/// Deletes a texture.
	/// </summary>
	public void DeleteTexture(uint texture);

	/// <summary>
	/// Generates a new texture.
	/// </summary>
	public uint GenTexture();

	/// <summary>
	/// Sets a texture parameter.
	/// </summary>
	public void TexParameter(GLEnum target, GLEnum pname, int param);

	/// <summary>
	/// Specifies a two-dimensional texture image.
	/// </summary>
	public unsafe void TexImage2D(GLEnum target, int level, int internalformat, uint width, uint height, int border, GLEnum format, GLEnum type, void* pixels);
}

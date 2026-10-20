// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;

using Silk.NET.OpenGL;

/// <summary>
/// The OpenGL calls <see cref="OpenGLRenderer"/> issues, and nothing more.
/// </summary>
/// <remarks>
/// A seam for the same reason <see cref="IGL3D"/> is one: what the renderer decides (what state it
/// saves and puts back, which commands it draws and with what scissor, which textures it refuses to
/// delete) is what its tests assert, and none of that needs a graphics context to check. Internal
/// and separate from <see cref="IGL"/>, which is public, so adding a call here breaks no one.
/// </remarks>
internal interface IGLRenderer
{
	/// <summary>Reads one integer state value.</summary>
	public int GetInteger(GLEnum pname);

	/// <summary>Reads a multi-valued integer state, such as the scissor box.</summary>
	public void GetInteger(GLEnum pname, Span<int> data);

	/// <summary>Whether a capability is enabled.</summary>
	public bool IsEnabled(GLEnum cap);

	/// <summary>Enables a capability.</summary>
	public void Enable(GLEnum cap);

	/// <summary>Disables a capability.</summary>
	public void Disable(GLEnum cap);

	/// <summary>Selects the active texture unit.</summary>
	public void ActiveTexture(GLEnum unit);

	/// <summary>Binds a texture.</summary>
	public void BindTexture(GLEnum target, uint texture);

	/// <summary>Deletes a texture.</summary>
	public void DeleteTexture(uint texture);

	/// <summary>
	/// Creates an RGBA8 texture holding <paramref name="rgba"/>, with linear filtering, and leaves it
	/// bound to <see cref="GLEnum.Texture2D"/>.
	/// </summary>
	/// <returns>The texture's name.</returns>
	public uint CreateTexture(int width, int height, ReadOnlySpan<byte> rgba);

	/// <summary>Replaces the whole of the texture bound to <see cref="GLEnum.Texture2D"/>.</summary>
	public void ReplaceBoundTexture(int width, int height, ReadOnlySpan<byte> rgba);

	/// <summary>Sets the blend equation for colour and alpha together.</summary>
	public void BlendEquation(GLEnum mode);

	/// <summary>Sets the blend equations for colour and alpha separately.</summary>
	public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha);

	/// <summary>Sets the blend factors for colour and alpha separately.</summary>
	public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha);

	/// <summary>Sets the viewport.</summary>
	public void Viewport(int x, int y, int width, int height);

	/// <summary>Sets the scissor box.</summary>
	public void Scissor(int x, int y, uint width, uint height);

	/// <summary>Sets the polygon mode for both faces.</summary>
	public void PolygonMode(GLEnum mode);

	/// <summary>Compiles and links a program from vertex and fragment shader source.</summary>
	/// <returns>The program's name.</returns>
	public uint CreateProgram(string vertexSource, string fragmentSource);

	/// <summary>Looks up a uniform's location in a program made by <see cref="CreateProgram"/>.</summary>
	public int GetUniformLocation(uint program, string name);

	/// <summary>Looks up an attribute's location in a program made by <see cref="CreateProgram"/>.</summary>
	public int GetAttribLocation(uint program, string name);

	/// <summary>Makes a program current.</summary>
	public void UseProgram(uint program);

	/// <summary>Deletes a program made by <see cref="CreateProgram"/>.</summary>
	public void DeleteProgram(uint program);

	/// <summary>Sets an integer uniform of the current program.</summary>
	public void Uniform1(int location, int value);

	/// <summary>Sets a 4x4 matrix uniform of the current program, column-major.</summary>
	public void UniformMatrix4(int location, ReadOnlySpan<float> matrix);

	/// <summary>Binds a sampler object to a texture unit.</summary>
	public void BindSampler(uint unit, uint sampler);

	/// <summary>Creates a vertex array object.</summary>
	public uint GenVertexArray();

	/// <summary>Binds a vertex array object.</summary>
	public void BindVertexArray(uint array);

	/// <summary>Deletes a vertex array object.</summary>
	public void DeleteVertexArray(uint array);

	/// <summary>Creates a buffer object.</summary>
	public uint GenBuffer();

	/// <summary>Binds a buffer object.</summary>
	public void BindBuffer(GLEnum target, uint buffer);

	/// <summary>Deletes a buffer object.</summary>
	public void DeleteBuffer(uint buffer);

	/// <summary>Replaces the contents of the buffer bound to <paramref name="target"/>, for streaming.</summary>
	public void BufferData(GLEnum target, nint size, nint data);

	/// <summary>Enables a vertex attribute array.</summary>
	public void EnableVertexAttribArray(uint index);

	/// <summary>Describes a vertex attribute within the bound array buffer.</summary>
	public void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, uint stride, nint offset);

	/// <summary>Draws indexed 16-bit triangles from the bound buffers.</summary>
	public void DrawElementsBaseVertex(uint count, nint indexByteOffset, int baseVertex);

	/// <summary>Reports any pending OpenGL error under <paramref name="title"/>.</summary>
	public void CheckError(string title);
}

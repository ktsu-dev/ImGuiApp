// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;

using Silk.NET.OpenGL;

/// <summary>
/// The OpenGL calls <see cref="GLRenderer3D"/> issues, and nothing more.
/// </summary>
/// <remarks>
/// <para>
/// A seam rather than a direct dependency on <see cref="GL"/> for the same reason
/// <see cref="IGL"/> exists: the decisions in <see cref="GLRenderer3D"/> — which handles are
/// targets, what state a draw touches and whether it is all put back — are what its tests assert,
/// and none of them needs a graphics context to check.
/// </para>
/// <para>
/// Internal and separate from <see cref="IGL"/> rather than added to it, because <see cref="IGL"/>
/// is public and every member added to a public interface breaks whoever implements it.
/// </para>
/// </remarks>
internal interface IGL3D
{
	/// <summary>Reads one integer state value. Booleans read as 0 or 1.</summary>
	public int GetInteger(GLEnum pname);

	/// <summary>Reads a multi-valued integer state, such as the viewport.</summary>
	public void GetInteger(GLEnum pname, Span<int> data);

	/// <summary>Reads a multi-valued float state, such as the clear colour.</summary>
	public void GetFloat(GLEnum pname, Span<float> data);

	/// <summary>Whether a capability is enabled.</summary>
	public bool IsEnabled(GLEnum cap);

	/// <summary>Enables a capability.</summary>
	public void Enable(GLEnum cap);

	/// <summary>Disables a capability.</summary>
	public void Disable(GLEnum cap);

	/// <summary>Generates a texture name.</summary>
	public uint GenTexture();

	/// <summary>Binds a texture.</summary>
	public void BindTexture(GLEnum target, uint texture);

	/// <summary>Sets a texture parameter on the bound texture.</summary>
	public void TexParameter(GLEnum target, GLEnum pname, int param);

	/// <summary>Allocates storage for the bound texture, leaving its contents undefined.</summary>
	public void TexImage2DEmpty(GLEnum target, GLEnum internalFormat, int width, int height);

	/// <summary>Deletes a texture.</summary>
	public void DeleteTexture(uint texture);

	/// <summary>Selects the active texture unit.</summary>
	public void ActiveTexture(GLEnum unit);

	/// <summary>Binds a sampler object to a texture unit.</summary>
	public void BindSampler(uint unit, uint sampler);

	/// <summary>Generates a framebuffer name.</summary>
	public uint GenFramebuffer();

	/// <summary>Binds a framebuffer.</summary>
	public void BindFramebuffer(GLEnum target, uint framebuffer);

	/// <summary>Attaches a texture to the bound framebuffer.</summary>
	public void FramebufferTexture2D(GLEnum target, GLEnum attachment, GLEnum textureTarget, uint texture);

	/// <summary>Attaches a renderbuffer to the bound framebuffer.</summary>
	public void FramebufferRenderbuffer(GLEnum target, GLEnum attachment, uint renderbuffer);

	/// <summary>Reports whether the bound framebuffer is complete.</summary>
	public GLEnum CheckFramebufferStatus(GLEnum target);

	/// <summary>Deletes a framebuffer.</summary>
	public void DeleteFramebuffer(uint framebuffer);

	/// <summary>Generates a renderbuffer name.</summary>
	public uint GenRenderbuffer();

	/// <summary>Binds a renderbuffer.</summary>
	public void BindRenderbuffer(uint renderbuffer);

	/// <summary>Allocates storage for the bound renderbuffer.</summary>
	public void RenderbufferStorage(GLEnum internalFormat, int width, int height);

	/// <summary>Deletes a renderbuffer.</summary>
	public void DeleteRenderbuffer(uint renderbuffer);

	/// <summary>Sets the viewport.</summary>
	public void Viewport(int x, int y, int width, int height);

	/// <summary>Sets the colour a clear fills with.</summary>
	public void ClearColor(float red, float green, float blue, float alpha);

	/// <summary>Sets the depth a clear fills with.</summary>
	public void ClearDepth(float depth);

	/// <summary>Clears the buffers named by the mask.</summary>
	public void Clear(ClearBufferMask mask);

	/// <summary>Enables or disables depth writes.</summary>
	public void DepthMask(bool write);

	/// <summary>Sets the depth comparison.</summary>
	public void DepthFunc(GLEnum func);

	/// <summary>Sets which facing culling discards.</summary>
	public void CullFace(GLEnum mode);

	/// <summary>Sets which winding is front-facing.</summary>
	public void FrontFace(GLEnum mode);

	/// <summary>Sets the blend equations.</summary>
	public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha);

	/// <summary>Sets the blend factors.</summary>
	public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha);

	/// <summary>Compiles and links a program from two shader sources.</summary>
	/// <exception cref="InvalidOperationException">A shader did not compile or the program did not link.</exception>
	public uint CreateProgram(string vertexSource, string fragmentSource);

	/// <summary>Looks up a uniform's location.</summary>
	public int GetUniformLocation(uint program, string name);

	/// <summary>Makes a program current.</summary>
	public void UseProgram(uint program);

	/// <summary>Deletes a program.</summary>
	public void DeleteProgram(uint program);

	/// <summary>Sets an integer uniform on the current program.</summary>
	public void Uniform1(int location, int value);

	/// <summary>Sets a matrix uniform on the current program, column-major.</summary>
	public void UniformMatrix4(int location, ReadOnlySpan<float> value);

	/// <summary>Generates a buffer name.</summary>
	public uint GenBuffer();

	/// <summary>Binds a buffer.</summary>
	public void BindBuffer(GLEnum target, uint buffer);

	/// <summary>Replaces a bound buffer's contents.</summary>
	public void BufferData(GLEnum target, ReadOnlySpan<byte> data);

	/// <summary>Deletes a buffer.</summary>
	public void DeleteBuffer(uint buffer);

	/// <summary>Generates a vertex array name.</summary>
	public uint GenVertexArray();

	/// <summary>Binds a vertex array.</summary>
	public void BindVertexArray(uint vertexArray);

	/// <summary>Deletes a vertex array.</summary>
	public void DeleteVertexArray(uint vertexArray);

	/// <summary>Enables a vertex attribute on the bound vertex array.</summary>
	public void EnableVertexAttribArray(uint index);

	/// <summary>Describes a vertex attribute in the bound array buffer.</summary>
	public void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, int stride, int offset);

	/// <summary>Draws from the bound element buffer, reading 32-bit indices from its start.</summary>
	public void DrawElements(GLEnum mode, int count);
}

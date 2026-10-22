// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Silk.NET.OpenGL;

/// <summary>
/// Carries <see cref="IGLRenderer"/> calls to a real OpenGL context.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "One-line forwards to a live OpenGL context, which unit tests do not have. Every decision is in OpenGLRenderer, which is covered against a stand-in for this seam.")]
internal sealed class GLRendererWrapper : IGLRenderer
{
	private readonly GL gl;
	private readonly Dictionary<uint, Shader> programs = [];

	/// <summary>Wraps a context.</summary>
	/// <param name="gl">The context. Not owned: the application creates and disposes it.</param>
	public GLRendererWrapper(GL gl)
	{
		Ensure.NotNull(gl);
		this.gl = gl;
	}

	public int GetInteger(GLEnum pname)
	{
		gl.GetInteger(pname, out int data);
		return data;
	}

	public void GetInteger(GLEnum pname, Span<int> data) => gl.GetInteger(pname, data);

	public bool IsEnabled(GLEnum cap) => gl.IsEnabled(cap);

	public void Enable(GLEnum cap) => gl.Enable(cap);

	public void Disable(GLEnum cap) => gl.Disable(cap);

	public void ActiveTexture(GLEnum unit) => gl.ActiveTexture(unit);

	public void BindTexture(GLEnum target, uint texture) => gl.BindTexture(target, texture);

	public void DeleteTexture(uint texture) => gl.DeleteTexture(texture);

	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The Texture wrapper's resource is the GL texture whose name is returned; disposing it here would delete that texture.")]
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The pixels are pinned for the upload only and the pointer is not retained.")]
	public unsafe uint CreateTexture(int width, int height, ReadOnlySpan<byte> rgba)
	{
		fixed (byte* pixels = rgba)
		{
			Texture texture = new(gl, width, height, (IntPtr)pixels, pxFormat: PixelFormat.Rgba);
			texture.Bind();
			texture.SetMagFilter(TextureMagFilter.Linear);
			texture.SetMinFilter(TextureMinFilter.Linear);
			return texture.GlTexture;
		}
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The pixels are pinned for the upload only and the pointer is not retained.")]
	public unsafe void ReplaceBoundTexture(int width, int height, ReadOnlySpan<byte> rgba)
	{
		fixed (byte* pixels = rgba)
		{
			gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.UnsignedByte, pixels);
		}
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The pixels are pinned for the upload only and the pointer is not retained.")]
	public unsafe uint CreateHalfTexture(int width, int height, ReadOnlySpan<Half> rgba)
	{
		uint texture = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, texture);
		fixed (Half* pixels = rgba)
		{
			gl.TexImage2D(GLEnum.Texture2D, 0, (int)GLEnum.Rgba16f, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.HalfFloat, pixels);
		}

		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
		return texture;
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The pixels are pinned for the upload only and the pointer is not retained.")]
	public unsafe void ReplaceBoundHalfTexture(int width, int height, ReadOnlySpan<Half> rgba)
	{
		fixed (Half* pixels = rgba)
		{
			gl.TexSubImage2D(GLEnum.Texture2D, 0, 0, 0, (uint)width, (uint)height, GLEnum.Rgba, GLEnum.HalfFloat, pixels);
		}
	}

	public void BlendEquation(GLEnum mode) => gl.BlendEquation(mode);

	public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha) => gl.BlendEquationSeparate(modeRgb, modeAlpha);

	public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha) =>
		gl.BlendFuncSeparate(srcRgb, dstRgb, srcAlpha, dstAlpha);

	public void Viewport(int x, int y, int width, int height) => gl.Viewport(x, y, (uint)width, (uint)height);

	public void Scissor(int x, int y, uint width, uint height) => gl.Scissor(x, y, width, height);

	public void PolygonMode(GLEnum mode) => gl.PolygonMode(GLEnum.FrontAndBack, mode);

	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Kept in the programs table and disposed by DeleteProgram.")]
	public uint CreateProgram(string vertexSource, string fragmentSource)
	{
		Shader shader = new(gl, vertexSource, fragmentSource);
		programs[shader.Program] = shader;
		return shader.Program;
	}

	public int GetUniformLocation(uint program, string name) => programs[program].GetUniformLocation(name);

	public int GetAttribLocation(uint program, string name) => programs[program].GetAttribLocation(name);

	public void UseProgram(uint program) => gl.UseProgram(program);

	public void DeleteProgram(uint program)
	{
		if (programs.Remove(program, out Shader? shader))
		{
			shader.Dispose();
		}
	}

	public void Uniform1(int location, int value) => gl.Uniform1(location, value);

	public void UniformMatrix4(int location, ReadOnlySpan<float> matrix) => gl.UniformMatrix4(location, 1, false, matrix);

	public void BindSampler(uint unit, uint sampler) => gl.BindSampler(unit, sampler);

	public uint GenVertexArray() => gl.GenVertexArray();

	public void BindVertexArray(uint array) => gl.BindVertexArray(array);

	public void DeleteVertexArray(uint array) => gl.DeleteVertexArray(array);

	public uint GenBuffer() => gl.GenBuffer();

	public void BindBuffer(GLEnum target, uint buffer) => gl.BindBuffer(target, buffer);

	public void DeleteBuffer(uint buffer) => gl.DeleteBuffer(buffer);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The data pointer comes from ImGui's draw list and is read by OpenGL during the call only.")]
	public unsafe void BufferData(GLEnum target, nint size, nint data) => gl.BufferData(target, (nuint)size, (void*)data, GLEnum.StreamDraw);

	public void EnableVertexAttribArray(uint index) => gl.EnableVertexAttribArray(index);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The offset is a byte offset into the bound buffer passed in OpenGL's pointer argument, not an address.")]
	public unsafe void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, uint stride, nint offset) =>
		gl.VertexAttribPointer(index, size, type, normalized, stride, (void*)offset);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "The offset is a byte offset into the bound index buffer passed in OpenGL's pointer argument, not an address.")]
	public unsafe void DrawElementsBaseVertex(uint count, nint indexByteOffset, int baseVertex) =>
		gl.DrawElementsBaseVertex(GLEnum.Triangles, count, GLEnum.UnsignedShort, (void*)indexByteOffset, baseVertex);

	public void CheckError(string title) => gl.CheckGlError(title);
}

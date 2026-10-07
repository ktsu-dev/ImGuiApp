// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Diagnostics.CodeAnalysis;

using Silk.NET.OpenGL;

/// <summary>
/// Carries <see cref="IGL3D"/> calls to a real OpenGL context.
/// </summary>
/// <param name="gl">The context. Not owned: the application creates and disposes it.</param>
internal sealed class GL3DWrapper(GL gl) : IGL3D
{
	private readonly GL gl = gl;

	public int GetInteger(GLEnum pname)
	{
		gl.GetInteger(pname, out int data);
		return data;
	}

	public void GetInteger(GLEnum pname, Span<int> data) => gl.GetInteger(pname, data);

	public void GetFloat(GLEnum pname, Span<float> data) => gl.GetFloat(pname, data);

	public bool IsEnabled(GLEnum cap) => gl.IsEnabled(cap);

	public void Enable(GLEnum cap) => gl.Enable(cap);

	public void Disable(GLEnum cap) => gl.Disable(cap);

	public uint GenTexture() => gl.GenTexture();

	public void BindTexture(GLEnum target, uint texture) => gl.BindTexture(target, texture);

	public void TexParameter(GLEnum target, GLEnum pname, int param) => gl.TexParameter(target, pname, param);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "A null pixel pointer asks OpenGL to allocate without uploading; nothing is read through it.")]
	public unsafe void TexImage2DEmpty(GLEnum target, GLEnum internalFormat, int width, int height) =>
		gl.TexImage2D(target, 0, (int)internalFormat, (uint)width, (uint)height, 0, GLEnum.Rgba, GLEnum.UnsignedByte, null);

	public void DeleteTexture(uint texture) => gl.DeleteTexture(texture);

	public void ActiveTexture(GLEnum unit) => gl.ActiveTexture(unit);

	public void BindSampler(uint unit, uint sampler) => gl.BindSampler(unit, sampler);

	public uint GenFramebuffer() => gl.GenFramebuffer();

	public void BindFramebuffer(GLEnum target, uint framebuffer) => gl.BindFramebuffer(target, framebuffer);

	public void FramebufferTexture2D(GLEnum target, GLEnum attachment, GLEnum textureTarget, uint texture) =>
		gl.FramebufferTexture2D(target, attachment, textureTarget, texture, 0);

	public void FramebufferRenderbuffer(GLEnum target, GLEnum attachment, uint renderbuffer) =>
		gl.FramebufferRenderbuffer(target, attachment, GLEnum.Renderbuffer, renderbuffer);

	public GLEnum CheckFramebufferStatus(GLEnum target) => gl.CheckFramebufferStatus(target);

	public void DeleteFramebuffer(uint framebuffer) => gl.DeleteFramebuffer(framebuffer);

	public uint GenRenderbuffer() => gl.GenRenderbuffer();

	public void BindRenderbuffer(uint renderbuffer) => gl.BindRenderbuffer(GLEnum.Renderbuffer, renderbuffer);

	public void RenderbufferStorage(GLEnum internalFormat, int width, int height) =>
		gl.RenderbufferStorage(GLEnum.Renderbuffer, internalFormat, (uint)width, (uint)height);

	public void DeleteRenderbuffer(uint renderbuffer) => gl.DeleteRenderbuffer(renderbuffer);

	public void Viewport(int x, int y, int width, int height) => gl.Viewport(x, y, (uint)width, (uint)height);

	public void ClearColor(float red, float green, float blue, float alpha) => gl.ClearColor(red, green, blue, alpha);

	public void ClearDepth(float depth) => gl.ClearDepth(depth);

	public void Clear(ClearBufferMask mask) => gl.Clear(mask);

	public void DepthMask(bool write) => gl.DepthMask(write);

	public void DepthFunc(GLEnum func) => gl.DepthFunc(func);

	public void CullFace(GLEnum mode) => gl.CullFace(mode);

	public void FrontFace(GLEnum mode) => gl.FrontFace(mode);

	public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha) => gl.BlendEquationSeparate(modeRgb, modeAlpha);

	public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha) =>
		gl.BlendFuncSeparate(srcRgb, dstRgb, srcAlpha, dstAlpha);

	public uint CreateProgram(string vertexSource, string fragmentSource)
	{
		uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
		uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

		uint program = gl.CreateProgram();
		gl.AttachShader(program, vertex);
		gl.AttachShader(program, fragment);
		gl.LinkProgram(program);
		gl.DetachShader(program, vertex);
		gl.DetachShader(program, fragment);
		gl.DeleteShader(vertex);
		gl.DeleteShader(fragment);

		gl.GetProgram(program, GLEnum.LinkStatus, out int linked);
		if (linked == 0)
		{
			string log = gl.GetProgramInfoLog(program);
			gl.DeleteProgram(program);
			throw new InvalidOperationException($"The 3D program did not link: {log}");
		}

		return program;
	}

	public int GetUniformLocation(uint program, string name) => gl.GetUniformLocation(program, name);

	public void UseProgram(uint program) => gl.UseProgram(program);

	public void DeleteProgram(uint program) => gl.DeleteProgram(program);

	public void Uniform1(int location, int value) => gl.Uniform1(location, value);

	public void UniformMatrix4(int location, ReadOnlySpan<float> value) => gl.UniformMatrix4(location, 1, false, value);

	public uint GenBuffer() => gl.GenBuffer();

	public void BindBuffer(GLEnum target, uint buffer) => gl.BindBuffer(target, buffer);

	public void BufferData(GLEnum target, ReadOnlySpan<byte> data) => gl.BufferData(target, data, GLEnum.StreamDraw);

	public void DeleteBuffer(uint buffer) => gl.DeleteBuffer(buffer);

	public uint GenVertexArray() => gl.GenVertexArray();

	public void BindVertexArray(uint vertexArray) => gl.BindVertexArray(vertexArray);

	public void DeleteVertexArray(uint vertexArray) => gl.DeleteVertexArray(vertexArray);

	public void EnableVertexAttribArray(uint index) => gl.EnableVertexAttribArray(index);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "OpenGL takes a byte offset into the bound buffer as a pointer; it is never dereferenced here.")]
	public unsafe void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, int stride, int offset) =>
		gl.VertexAttribPointer(index, size, type, normalized, (uint)stride, (void*)offset);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "A null offset means the start of the bound element buffer; it is never dereferenced here.")]
	public unsafe void DrawElements(GLEnum mode, int count) =>
		gl.DrawElements(mode, (uint)count, GLEnum.UnsignedInt, null);

	private uint CompileShader(ShaderType type, string source)
	{
		uint shader = gl.CreateShader(type);
		gl.ShaderSource(shader, source);
		gl.CompileShader(shader);

		gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compiled);
		if (compiled == 0)
		{
			string log = gl.GetShaderInfoLog(shader);
			gl.DeleteShader(shader);
			throw new InvalidOperationException($"The 3D {type} did not compile: {log}");
		}

		return shader;
	}
}

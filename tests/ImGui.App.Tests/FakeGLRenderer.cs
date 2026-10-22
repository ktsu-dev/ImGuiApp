// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Collections.Generic;

using ktsu.ImGui.App.ImGuiController;

using Silk.NET.OpenGL;

/// <summary>
/// Stands in for an OpenGL context under <see cref="OpenGLRenderer"/>: it records every call and
/// models the state the renderer reads and writes, so a test can compare the context before and
/// after a frame.
/// </summary>
internal sealed class FakeGLRenderer : IGLRenderer
{
	public const uint Program = 7;

	private uint nextName = 100;

	public List<string> Calls { get; } = [];

	/// <summary>Gets the integer state, keyed by what reads it.</summary>
	public Dictionary<GLEnum, int> Integers { get; } = [];

	/// <summary>Gets the enabled capabilities.</summary>
	public HashSet<GLEnum> Enabled { get; } = [];

	public int[] ScissorBox { get; } = new int[4];

	public int[] PolygonModes { get; } = new int[2];

	/// <summary>Gets each draw as (scissor, texture, element count, index byte offset, base vertex).</summary>
	public List<(int X, int Y, uint Width, uint Height, uint Texture, uint Count, nint IndexOffset, int BaseVertex)> Draws { get; } = [];

	/// <summary>Gets the size of each buffer upload, by target.</summary>
	public List<(GLEnum Target, nint Size)> Uploads { get; } = [];

	/// <summary>Gets the last projection matrix set.</summary>
	public float[] Projection { get; private set; } = [];

	/// <summary>Gets the texture uploads as (texture, width, height, bytes).</summary>
	public List<(uint Texture, int Width, int Height, int Bytes)> TextureWrites { get; } = [];

	public int GetInteger(GLEnum pname) => Integers.TryGetValue(pname, out int value) ? value : 0;

	public void GetInteger(GLEnum pname, Span<int> data)
	{
		int[] source = pname switch
		{
			GLEnum.ScissorBox => ScissorBox,
			GLEnum.PolygonMode => PolygonModes,
			_ => throw new ArgumentOutOfRangeException(nameof(pname), pname, "Not modelled."),
		};
		source.AsSpan(0, data.Length).CopyTo(data);
	}

	public bool IsEnabled(GLEnum cap) => Enabled.Contains(cap);

	public void Enable(GLEnum cap)
	{
		Calls.Add($"Enable({cap})");
		Enabled.Add(cap);
	}

	public void Disable(GLEnum cap)
	{
		Calls.Add($"Disable({cap})");
		Enabled.Remove(cap);
	}

	public void ActiveTexture(GLEnum unit) => Integers[GLEnum.ActiveTexture] = (int)unit;

	public void BindTexture(GLEnum target, uint texture)
	{
		Calls.Add($"BindTexture({texture})");
		Integers[GLEnum.TextureBinding2D] = (int)texture;
	}

	public void DeleteTexture(uint texture) => Calls.Add($"DeleteTexture({texture})");

	public uint CreateTexture(int width, int height, ReadOnlySpan<byte> rgba)
	{
		uint name = nextName++;
		Calls.Add($"CreateTexture({name})");
		Integers[GLEnum.TextureBinding2D] = (int)name;
		TextureWrites.Add((name, width, height, rgba.Length));
		return name;
	}

	public void ReplaceBoundTexture(int width, int height, ReadOnlySpan<byte> rgba) =>
		TextureWrites.Add(((uint)GetInteger(GLEnum.TextureBinding2D), width, height, rgba.Length));

	public uint CreateHalfTexture(int width, int height, ReadOnlySpan<Half> rgba)
	{
		uint name = nextName++;
		Calls.Add($"CreateHalfTexture({name})");
		Integers[GLEnum.TextureBinding2D] = (int)name;
		TextureWrites.Add((name, width, height, rgba.Length * 2));
		return name;
	}

	public void ReplaceBoundHalfTexture(int width, int height, ReadOnlySpan<Half> rgba) =>
		TextureWrites.Add(((uint)GetInteger(GLEnum.TextureBinding2D), width, height, rgba.Length * 2));

	public void BlendEquation(GLEnum mode)
	{
		Integers[GLEnum.BlendEquationRgb] = (int)mode;
		Integers[GLEnum.BlendEquationAlpha] = (int)mode;
	}

	public void BlendEquationSeparate(GLEnum modeRgb, GLEnum modeAlpha)
	{
		Integers[GLEnum.BlendEquationRgb] = (int)modeRgb;
		Integers[GLEnum.BlendEquationAlpha] = (int)modeAlpha;
	}

	public void BlendFuncSeparate(GLEnum srcRgb, GLEnum dstRgb, GLEnum srcAlpha, GLEnum dstAlpha)
	{
		Calls.Add($"BlendFuncSeparate({srcRgb}, {dstRgb}, {srcAlpha}, {dstAlpha})");
		Integers[GLEnum.BlendSrcRgb] = (int)srcRgb;
		Integers[GLEnum.BlendDstRgb] = (int)dstRgb;
		Integers[GLEnum.BlendSrcAlpha] = (int)srcAlpha;
		Integers[GLEnum.BlendDstAlpha] = (int)dstAlpha;
	}

	public void Viewport(int x, int y, int width, int height) => Calls.Add($"Viewport({x}, {y}, {width}, {height})");

	public void Scissor(int x, int y, uint width, uint height)
	{
		ScissorBox[0] = x;
		ScissorBox[1] = y;
		ScissorBox[2] = (int)width;
		ScissorBox[3] = (int)height;
	}

	public void PolygonMode(GLEnum mode)
	{
		PolygonModes[0] = (int)mode;
		PolygonModes[1] = (int)mode;
	}

	public uint CreateProgram(string vertexSource, string fragmentSource)
	{
		Calls.Add("CreateProgram");
		return Program;
	}

	public int GetUniformLocation(uint program, string name) => name == "Texture" ? 0 : 1;

	public int GetAttribLocation(uint program, string name) => name switch
	{
		"Position" => 0,
		"UV" => 1,
		_ => 2,
	};

	public void UseProgram(uint program) => Integers[GLEnum.CurrentProgram] = (int)program;

	public void DeleteProgram(uint program) => Calls.Add($"DeleteProgram({program})");

	public void Uniform1(int location, int value)
	{
	}

	public void UniformMatrix4(int location, ReadOnlySpan<float> matrix) => Projection = matrix.ToArray();

	public void BindSampler(uint unit, uint sampler) => Integers[GLEnum.SamplerBinding] = (int)sampler;

	public uint GenVertexArray() => nextName++;

	public void BindVertexArray(uint array) => Integers[GLEnum.VertexArrayBinding] = (int)array;

	public void DeleteVertexArray(uint array) => Calls.Add($"DeleteVertexArray({array})");

	public uint GenBuffer() => nextName++;

	public void BindBuffer(GLEnum target, uint buffer)
	{
		if (target == GLEnum.ArrayBuffer)
		{
			Integers[GLEnum.ArrayBufferBinding] = (int)buffer;
		}
	}

	public void DeleteBuffer(uint buffer) => Calls.Add($"DeleteBuffer({buffer})");

	public void BufferData(GLEnum target, nint size, nint data) => Uploads.Add((target, size));

	public void EnableVertexAttribArray(uint index)
	{
	}

	public void VertexAttribPointer(uint index, int size, GLEnum type, bool normalized, uint stride, nint offset) =>
		Calls.Add($"VertexAttribPointer({index}, {size}, {type}, {normalized}, {stride}, {offset})");

	public void DrawElementsBaseVertex(uint count, nint indexByteOffset, int baseVertex) =>
		Draws.Add((ScissorBox[0], ScissorBox[1], (uint)ScissorBox[2], (uint)ScissorBox[3], (uint)GetInteger(GLEnum.TextureBinding2D), count, indexByteOffset, baseVertex));

	public void CheckError(string title)
	{
	}
}

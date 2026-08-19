// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Turns ImGui draw data into pixels on the CPU. Mirrors the shape of the renderer seam the OpenGL
/// and Metal backends implement, but deliberately does not implement that interface: it is internal
/// to ktsu.ImGui.App, and depending on it would require friend access, which in turn makes every
/// polyfilled call in this assembly ambiguous between two copies of the same source-only package.
/// Nothing inside ktsu.ImGui.App consumes this renderer, so the interface buys nothing here.
/// </summary>
/// <param name="width">Render target width in pixels.</param>
/// <param name="height">Render target height in pixels.</param>
public sealed class SoftwareRenderer(int width, int height) : IDisposable
{
	private readonly Dictionary<nint, TextureSource> textures = [];
	private nint nextId = 1;
	private bool disposed;

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	public Bitmap32 Target { get; } = new Bitmap32(width, height);

	/// <summary>Fills the render target with one color, discarding the previous frame.</summary>
	/// <param name="color">The clear color.</param>
	public void Clear(Rgba32 color) => Target.Clear(color);

	/// <summary>Uploads a texture and returns an opaque handle usable as an ImGui texture id.</summary>
	/// <param name="rgba">Tightly packed RGBA8 pixels.</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <returns>A handle for later use.</returns>
	public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height)
	{
		nint id = nextId++;
		textures[id] = new TextureSource(ToBitmap(rgba, width, height));
		return id;
	}

	/// <summary>Replaces the contents of an existing texture.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	/// <param name="rgba">Tightly packed RGBA8 pixels.</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <returns>Always true. A CPU texture can always be replaced in place.</returns>
	public bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height)
	{
		textures[id] = new TextureSource(ToBitmap(rgba, width, height));
		return true;
	}

	/// <summary>Releases a texture.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	public void DeleteTexture(nint id) => textures.Remove(id);

	/// <summary>Looks up a texture by handle.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	/// <returns>The texture behind that handle.</returns>
	public TextureSource GetTexture(nint id) => textures[id];

	/// <summary>Rasterizes a complete ImGui draw-data tree into the render target.</summary>
	/// <param name="drawData">Draw data obtained after calling <c>ImGui.Render</c>.</param>
	public unsafe void RenderDrawData(ImDrawDataPtr drawData)
	{
		if (drawData.Handle is null || drawData.CmdListsCount == 0)
		{
			return;
		}

		Vector2 origin = drawData.DisplayPos;

		for (int list = 0; list < drawData.CmdListsCount; list++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[list];

			for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
			{
				ImDrawCmd cmd = cmdList.CmdBuffer[cmdIndex];

				// A user callback replaces drawing for that command. The harness cannot execute an
				// application's native callback, so the command is skipped rather than guessed at.
				if (cmd.UserCallback is not null)
				{
					continue;
				}

				Rectangle scissor = new(
					(int)MathF.Floor(cmd.ClipRect.X - origin.X),
					(int)MathF.Floor(cmd.ClipRect.Y - origin.Y),
					(int)MathF.Ceiling(cmd.ClipRect.Z - origin.X),
					(int)MathF.Ceiling(cmd.ClipRect.W - origin.Y));

				textures.TryGetValue(cmd.GetTexID(), out TextureSource? texture);

				for (uint element = 0; element + 2 < cmd.ElemCount; element += 3)
				{
					ushort i0 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + element)];
					ushort i1 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + element + 1)];
					ushort i2 = cmdList.IdxBuffer[(int)(cmd.IdxOffset + element + 2)];

					Vertex a = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i0)], origin);
					Vertex b = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i1)], origin);
					Vertex c = ToVertex(cmdList.VtxBuffer[(int)(cmd.VtxOffset + i2)], origin);

					SoftwareRasterizer.FillTriangle(Target, a, b, c, texture, scissor);
				}
			}
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		textures.Clear();
		disposed = true;
	}

	private static Vertex ToVertex(ImDrawVert vertex, Vector2 origin) => new(
		new Vector2(vertex.Pos.X - origin.X, vertex.Pos.Y - origin.Y),
		new Vector2(vertex.Uv.X, vertex.Uv.Y),
		FromAbgr(vertex.Col));

	// ImGui packs vertex colors as ABGR, so the red channel is the low byte.
	private static Rgba32 FromAbgr(uint packed) => new(
		(byte)(packed & 0xFF),
		(byte)((packed >> 8) & 0xFF),
		(byte)((packed >> 16) & 0xFF),
		(byte)((packed >> 24) & 0xFF));

	private static Bitmap32 ToBitmap(ReadOnlySpan<byte> rgba, int width, int height)
	{
		Bitmap32 bitmap = new(width, height);
		rgba[..(width * height * 4)].CopyTo(bitmap.Pixels);
		return bitmap;
	}
}

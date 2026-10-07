// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Turns ImGui draw data into pixels on the CPU, as the renderer backend for a headless session.
/// </summary>
/// <remarks>
/// This implements the same seam the OpenGL and Metal backends do, which is what lets an
/// application under test upload its own textures through <see cref="ImGuiApp.CreateTexture(ReadOnlySpan{byte}, int, int)"/> and
/// have them reach the rasterizer that is actually drawing. It previously only mirrored the seam's
/// shape without implementing it, because the interface was internal and friend access would have
/// made every polyfilled call in this assembly ambiguous between two compiled copies of a
/// source-only package. The interface is public now, so neither problem applies.
/// </remarks>
/// <param name="width">Render target width in pixels.</param>
/// <param name="height">Render target height in pixels.</param>
public sealed partial class SoftwareRenderer(int width, int height) : IRendererBackend
{
	private readonly Dictionary<nint, TextureSource> textures = [];
	private readonly Bitmap32 target = new(width, height);
	private nint nextId = 1;
	private bool disposed;

	// A frame the harness has finished but not yet rasterized. Nearly every frame a UI test steps
	// through is never looked at, and rasterizing on the CPU is almost all of what a frame costs,
	// so the harness hands the frame over here and it is drawn only when its pixels are read.
	// ImGui keeps the draw data alive until the next NewFrame, and the harness discards the
	// deferred frame before calling it, so the pointer is never read after ImGui has reused it.
	private Rgba32? deferredClear;
	private ImDrawDataPtr deferredDrawData;
	private bool drawDataDeferred;

	/// <summary>Gets the render target holding the most recently rendered frame.</summary>
	/// <remarks>
	/// Under <see cref="ImGuiAppHarness"/> a frame is rasterized when this is read rather than when
	/// it ends, so read it again after stepping rather than holding the bitmap across steps: a
	/// reference kept from before a step still shows the frame it was read on.
	/// </remarks>
	public Bitmap32 Target
	{
		get
		{
			RasterizeDeferredFrame();
			return target;
		}
	}

	/// <summary>Gets how many deferred frames have been rasterized, which tests read to tell a skipped frame from a drawn one.</summary>
	internal int DeferredFramesRasterized { get; private set; }

	/// <summary>Fills the render target with one color, discarding the previous frame.</summary>
	/// <param name="color">The clear color.</param>
	public void Clear(Rgba32 color)
	{
		DiscardDeferredFrame();
		target.Clear(color);
	}

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
		RasterizeDeferredDrawData();
		textures[id] = new TextureSource(ToBitmap(rgba, width, height));
		return true;
	}

	/// <summary>Releases a texture.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	/// <exception cref="ArgumentException">
	/// <paramref name="id"/> is a render target's colour attachment. Those are released through
	/// <see cref="DeleteRenderTarget"/> and nothing else — see <see cref="IsRenderTargetTexture"/>
	/// for why silently accepting it would be worse than throwing.
	/// </exception>
	public void DeleteTexture(nint id)
	{
		if (IsRenderTargetTexture(id))
		{
			throw new ArgumentException(
				$"Texture {id} is a render target's colour attachment. Release it with DeleteRenderTarget.", nameof(id));
		}

		RasterizeDeferredDrawData();
		textures.Remove(id);
	}

	/// <summary>Looks up a texture by handle.</summary>
	/// <param name="id">A handle returned by <see cref="CreateTexture"/>.</param>
	/// <returns>The texture behind that handle.</returns>
	public TextureSource GetTexture(nint id) => textures[id];

	/// <summary>Rasterizes a complete ImGui draw-data tree into the render target.</summary>
	/// <param name="drawData">Draw data obtained after calling <c>ImGui.Render</c>.</param>
	public void RenderDrawData(ImDrawDataPtr drawData)
	{
		RasterizeDeferredFrame();
		Rasterize(drawData);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		DiscardDeferredFrame();
		textures.Clear();
		disposed = true;
	}

	/// <summary>
	/// Starts a frame whose rasterization is deferred until its pixels are read, discarding any
	/// earlier deferred frame unread. Call it before <c>ImGui.NewFrame</c>, which invalidates the
	/// draw data of the frame being discarded.
	/// </summary>
	/// <param name="clearColor">The color the frame is cleared to.</param>
	internal void BeginDeferredFrame(Rgba32 clearColor)
	{
		DiscardDeferredFrame();
		deferredClear = clearColor;
	}

	/// <summary>
	/// Records the draw data ending a deferred frame, to be rasterized when the target is next read
	/// or before anything it samples changes.
	/// </summary>
	/// <param name="drawData">Draw data obtained after calling <c>ImGui.Render</c>.</param>
	internal void DeferDrawData(ImDrawDataPtr drawData)
	{
		deferredDrawData = drawData;
		drawDataDeferred = true;
	}

	/// <summary>Forgets a deferred frame without drawing it.</summary>
	internal void DiscardDeferredFrame()
	{
		deferredClear = null;
		deferredDrawData = default;
		drawDataDeferred = false;
	}

	/// <summary>Draws the deferred frame, if there is one, so the target holds it.</summary>
	private void RasterizeDeferredFrame()
	{
		if (deferredClear is Rgba32 clear)
		{
			deferredClear = null;
			target.Clear(clear);
		}

		if (drawDataDeferred)
		{
			ImDrawDataPtr drawData = deferredDrawData;
			deferredDrawData = default;
			drawDataDeferred = false;
			DeferredFramesRasterized++;
			Rasterize(drawData);
		}
	}

	/// <summary>
	/// Draws a deferred frame's draw data before a texture it may sample changes, so the frame is
	/// drawn with the textures it was submitted with. A deferred clear alone samples nothing and is
	/// left deferred.
	/// </summary>
	private void RasterizeDeferredDrawData()
	{
		if (drawDataDeferred)
		{
			RasterizeDeferredFrame();
		}
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required to read a native function pointer during ImGui interop; the pointer is compared and never dereferenced or retained.")]
	private void Rasterize(ImDrawDataPtr drawData)
	{
		if (drawData.Equals(default) || drawData.CmdListsCount == 0)
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
				// UserCallback is a function pointer, so reading it needs an unsafe context; the
				// block is kept to that single comparison.
				bool hasUserCallback;
				unsafe
				{
					hasUserCallback = cmd.UserCallback is not null;
				}

				if (hasUserCallback)
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

					SoftwareRasterizer.FillTriangle(target, a, b, c, texture, scissor);
				}
			}
		}
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

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;
using ktsu.ImGui.Rendering;

/// <summary>
/// The software backend's <see cref="IRenderer3D"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is a first-class deliverable rather than polish.</strong> The repository has six
/// headless UI suites, a one-class-per-widget isolation rule, and a <c>WidgetTest</c> base offering
/// <c>Snapshot</c>, <c>PixelsChangedSince</c> and <c>BoundsOfDifference</c> — all of it resting on
/// this renderer. A 3D path that only worked on OpenGL would be the first thing in the suite that
/// could not be rendered headlessly: no isolation test for a 3D viewport could exist, and the
/// pixel-difference helpers would have nothing to compare.
/// </para>
/// </remarks>
public sealed partial class SoftwareRenderer : IRenderer3D
{
	private readonly Dictionary<nint, RenderTarget3D> targets = [];
	private readonly HashSet<nint> targetTextureIds = [];

	/// <summary>Creates an offscreen target.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="depth">Whether to attach a depth buffer.</param>
	/// <returns>A handle, released with <see cref="DeleteRenderTarget"/>.</returns>
	public nint CreateRenderTarget(int width, int height, bool depth) =>
		CreateRenderTarget(width, height, depth, RenderTargetFormat.Rgba8);

	/// <summary>Whether this renderer can create targets in a format: it can create both.</summary>
	/// <param name="format">The format.</param>
	/// <returns><see langword="true"/> for every <see cref="RenderTargetFormat"/>.</returns>
	public bool SupportsFormat(RenderTargetFormat format) => format is RenderTargetFormat.Rgba8 or RenderTargetFormat.Rgba16F;

	/// <summary>Creates an offscreen target with a colour attachment in the given format.</summary>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="depth">Whether to attach a depth buffer.</param>
	/// <param name="format">The colour attachment's format.</param>
	/// <returns>A handle, released with <see cref="DeleteRenderTarget"/>.</returns>
	/// <remarks>
	/// A <see cref="RenderTargetFormat.Rgba16F"/> target keeps its colour in floats; its texture id
	/// shows those values clamped to [0, 1], as sampling a float target unmapped does on a GPU.
	/// </remarks>
	public nint CreateRenderTarget(int width, int height, bool depth, RenderTargetFormat format)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		if (!SupportsFormat(format))
		{
			throw new NotSupportedException($"No {format} render targets.");
		}

		nint handle = nextId++;
		nint textureId = nextId++;
		RenderTarget3D target = new(width, height, depth, textureId, format);

		targets[handle] = target;
		targetTextureIds.Add(textureId);

		// Registered in the same table the 2D path samples from, and holding the target's own
		// bitmap rather than a copy of it, so a draw into the target is visible to the next
		// ImGui.Image of it with nothing to flush in between.
		textures[textureId] = new TextureSource(target.Color);

		return handle;
	}

	/// <summary>Resizes a target, discarding its contents.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="width">New width in pixels.</param>
	/// <param name="height">New height in pixels.</param>
	/// <returns><see langword="true"/> if resized; <see langword="false"/> for an unknown handle.</returns>
	public bool ResizeRenderTarget(nint target, int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		if (!targets.TryGetValue(target, out RenderTarget3D? existing))
		{
			return false;
		}

		RasterizeDeferredDrawData();

		RenderTarget3D resized = new(width, height, existing.Depth is not null, existing.TextureId, existing.Format);

		targets[target] = resized;
		textures[existing.TextureId] = new TextureSource(resized.Color);

		return true;
	}

	/// <summary>Releases a target and everything attached to it.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	public void DeleteRenderTarget(nint target)
	{
		if (!targets.TryGetValue(target, out RenderTarget3D? existing))
		{
			return;
		}

		RasterizeDeferredDrawData();
		targets.Remove(target);

		targetTextureIds.Remove(existing.TextureId);
		textures.Remove(existing.TextureId);
	}

	/// <summary>Gets a target's colour attachment as an ImGui texture id.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <returns>The texture id.</returns>
	/// <exception cref="ArgumentException">The handle is not one this renderer issued.</exception>
	public nint GetTargetTexture(nint target) =>
		targets.TryGetValue(target, out RenderTarget3D? existing)
			? existing.TextureId
			: throw new ArgumentException($"No render target with handle {target}.", nameof(target));

	/// <summary>Clears a target's colour and depth attachments.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="color">The colour to fill with, straight alpha, each channel in [0, 1].</param>
	/// <param name="depth">The depth to fill with. Far is 1.</param>
	/// <exception cref="ArgumentException">The handle is not one this renderer issued.</exception>
	public void Clear(nint target, Vector4 color, float depth)
	{
		RenderTarget3D resolved = Resolve(target, nameof(target));

		RasterizeDeferredDrawData();
		resolved.Color.Clear(new Rgba32(Channel(color.X), Channel(color.Y), Channel(color.Z), Channel(color.W)));
		resolved.Hdr?.Clear(color);
		resolved.Depth?.Clear(depth);
	}

	/// <summary>Draws one target into another through a tone map, replacing the destination's contents.</summary>
	/// <param name="source">The target to read; its whole area is stretched over the destination's.</param>
	/// <param name="destination">The target to write.</param>
	/// <param name="toneMap">The mapping.</param>
	/// <exception cref="ArgumentException">Either handle is unknown, or they are the same target.</exception>
	/// <remarks>Samples the nearest source pixel, which is exact when the two targets are the same size.</remarks>
	public void ToneMap(nint source, nint destination, in ToneMap3D toneMap)
	{
		RenderTarget3D from = Resolve(source, nameof(source));
		RenderTarget3D to = Resolve(destination, nameof(destination));
		if (source == destination)
		{
			throw new ArgumentException("A target cannot be tone-mapped into itself.", nameof(destination));
		}

		RasterizeDeferredDrawData();

		for (int y = 0; y < to.Color.Height; y++)
		{
			int sy = Math.Min(from.Color.Height - 1, (int)((y + 0.5f) * from.Color.Height / to.Color.Height));
			for (int x = 0; x < to.Color.Width; x++)
			{
				int sx = Math.Min(from.Color.Width - 1, (int)((x + 0.5f) * from.Color.Width / to.Color.Width));
				Vector4 value = from.Hdr is not null ? from.Hdr[sx, sy] : ToVector(from.Color.GetPixel(sx, sy));
				Vector4 mapped = toneMap.Apply(value);

				to.Color.SetPixel(x, y, HdrBuffer.ToRgba32(mapped));
				to.Hdr?[x, y] = mapped;
			}
		}
	}

	/// <summary>Draws indexed geometry into a target.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="vertices">The vertices, in model space.</param>
	/// <param name="indices">Indices into <paramref name="vertices"/>.</param>
	/// <param name="state">Everything about the draw that is not its vertices.</param>
	/// <exception cref="ArgumentException">The handle is not one this renderer issued, or an index is out of range.</exception>
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state)
	{
		RenderTarget3D resolved = Resolve(target, nameof(target));
		RasterizeDeferredDrawData();
		TextureSource? texture = state.TextureId != 0 && textures.TryGetValue(state.TextureId, out TextureSource? found)
			? found
			: null;

		int stride = state.Topology == PrimitiveTopology.LineList ? 2 : 3;

		for (int i = 0; i + stride <= indices.Length; i += stride)
		{
			if (state.Topology == PrimitiveTopology.LineList)
			{
				SoftwareRasterizer.DrawLine3D(
					resolved.Color,
					resolved.Hdr,
					resolved.Depth,
					ToClip(vertices, indices[i], state.ModelViewProjection),
					ToClip(vertices, indices[i + 1], state.ModelViewProjection),
					state);

				continue;
			}

			SoftwareRasterizer.FillTriangle3D(
				resolved.Color,
				resolved.Hdr,
				resolved.Depth,
				ToClip(vertices, indices[i], state.ModelViewProjection),
				ToClip(vertices, indices[i + 1], state.ModelViewProjection),
				ToClip(vertices, indices[i + 2], state.ModelViewProjection),
				texture,
				state);
		}
	}

	/// <summary>Whether an id is a render target's colour attachment rather than an uploaded texture.</summary>
	/// <param name="id">The id to test.</param>
	/// <returns><see langword="true"/> if it belongs to a render target.</returns>
	/// <remarks>
	/// The hazard this exists for: <see cref="GetTargetTexture"/> returns something usable as an
	/// ImGui texture id, and on OpenGL that is a name from the same namespace
	/// <c>CreateTexture</c> draws from — so a caller who passed it to <c>DeleteTexture</c> would
	/// free a live target's colour attachment out from under it, and on a GPU backend the
	/// corruption would show up somewhere else entirely. Rejecting it here means the CPU backend
	/// reproduces the contract a GPU backend has to keep, so a test can pin it.
	/// </remarks>
	public bool IsRenderTargetTexture(nint id) => targetTextureIds.Contains(id);

	private static SoftwareRasterizer.ClipVertex ToClip(ReadOnlySpan<Vertex3D> vertices, uint index, Matrix4x4 matrix)
	{
		if (index >= (uint)vertices.Length)
		{
			throw new ArgumentException($"Index {index} is past the end of a {vertices.Length}-vertex buffer.", nameof(index));
		}

		Vertex3D vertex = vertices[(int)index];

		return new SoftwareRasterizer.ClipVertex(
			Vector4.Transform(new Vector4(vertex.Position, 1f), matrix),
			vertex.Uv,
			Unpack(vertex.Color));
	}

	/// <summary>Unpacks a colour the way ImGui packs one: red in the low byte.</summary>
	/// <param name="packed">The packed value.</param>
	/// <returns>The colour.</returns>
	private static Rgba32 Unpack(uint packed) => new(
		(byte)(packed & 0xFF),
		(byte)((packed >> 8) & 0xFF),
		(byte)((packed >> 16) & 0xFF),
		(byte)((packed >> 24) & 0xFF));

	private static byte Channel(float value) => (byte)Math.Clamp(MathF.Round(value * 255f), 0f, 255f);

	private static Vector4 ToVector(Rgba32 colour) => new(colour.R / 255f, colour.G / 255f, colour.B / 255f, colour.A / 255f);

	private RenderTarget3D Resolve(nint target, string parameterName) =>
		targets.TryGetValue(target, out RenderTarget3D? existing)
			? existing
			: throw new ArgumentException($"No render target with handle {target}.", parameterName);

	/// <summary>
	/// A colour attachment, an optional depth attachment, and the texture id standing for the pair.
	/// A high-range target also holds its colour in floats, with <see cref="Color"/> mirroring it clamped.
	/// </summary>
	private sealed class RenderTarget3D(int width, int height, bool depth, nint textureId, RenderTargetFormat format)
	{
		public Bitmap32 Color { get; } = new Bitmap32(width, height);

		public HdrBuffer? Hdr { get; } = format == RenderTargetFormat.Rgba16F ? new HdrBuffer(width, height) : null;

		public DepthBuffer? Depth { get; } = depth ? new DepthBuffer(width, height) : null;

		public nint TextureId { get; } = textureId;

		public RenderTargetFormat Format { get; } = format;
	}
}

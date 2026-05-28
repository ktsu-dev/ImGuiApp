// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System;
using Hexa.NET.ImGui;

/// <summary>
/// Platform-agnostic seam for the GPU side of an ImGui frame. Each platform port
/// provides an implementation: desktop uses <c>ktsu.ImGui.App.ImGuiController.ImGuiController</c>
/// (OpenGL via Silk.NET); the future iOS port will provide a Metal-backed implementation.
/// The interface deliberately covers only what differs between backends — atlas/user
/// texture upload, texture release, and final draw-data submission. Per-frame state
/// (input, NewFrame/EndFrame, font configuration) stays in the concrete backend until
/// the broader split described in the iOS port plan is in place.
/// </summary>
internal interface IRendererBackend : IDisposable
{
	/// <summary>
	/// Uploads an RGBA8 pixel buffer to the GPU and returns an opaque, pointer-sized handle.
	/// On OpenGL the value is the GL texture name widened to <see cref="nint"/>; on Metal
	/// it will be a retained <c>id&lt;MTLTexture&gt;</c>.
	/// </summary>
	/// <param name="rgba">Tightly packed RGBA8 pixel data (<paramref name="width"/> * <paramref name="height"/> * 4 bytes).</param>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <returns>An opaque handle suitable for use as an ImGui texture id.</returns>
	public nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height);

	/// <summary>
	/// Releases a texture previously returned by <see cref="CreateTexture"/>.
	/// </summary>
	/// <param name="id">The handle returned by <see cref="CreateTexture"/>.</param>
	public void DeleteTexture(nint id);

	/// <summary>
	/// Submits a fully-built ImGui draw-data tree to the GPU. The backend is responsible
	/// for setting up its own pipeline / state and restoring any state it touches.
	/// </summary>
	/// <param name="drawData">The draw data returned by <c>ImGui.GetDrawData()</c> after <c>ImGui.Render()</c>.</param>
	public void RenderDrawData(ImDrawDataPtr drawData);
}

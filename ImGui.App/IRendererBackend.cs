// Copyright (c) 2023-2026 ktsu-dev contributors

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
/// <remarks>
/// Public because a host that drives its own frames through
/// <c>ImGuiApp.BeginExternalFrameSession</c> has to supply one. Without that, every texture the
/// application uploads through <c>ImGuiApp.CreateTexture</c> fails on a backend that was never
/// installed, which rules out testing any application that shows an image it generated. Keeping
/// the seam internal and reaching it through friend access was tried and does not work:
/// <c>Polyfill</c> is source-only, so friend access makes every polyfilled call ambiguous between
/// the two compiled copies.
/// <para>
/// Referred to here in plain code rather than with a cref: this file compiles for the iOS target
/// framework as well, where the desktop host type is not part of the compilation.
/// </para>
/// </remarks>
public interface IRendererBackend : IDisposable
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
	/// Replaces the pixel contents of an existing texture in place, reusing its GPU storage.
	/// </summary>
	/// <param name="id">The handle returned by <see cref="CreateTexture"/>.</param>
	/// <param name="rgba">Tightly packed RGBA8 pixel data (<paramref name="width"/> * <paramref name="height"/> * 4 bytes).</param>
	/// <param name="width">Texture width in pixels. Must match the width the texture was created with.</param>
	/// <param name="height">Texture height in pixels. Must match the height the texture was created with.</param>
	/// <returns>
	/// <see langword="true"/> if the texture was updated in place. <see langword="false"/> if this backend
	/// cannot update textures at all, in which case the caller should recreate the texture instead.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// The backend supports in-place update but its rendering context is not initialized. That is a
	/// different condition from returning <see langword="false"/>, and recreating the texture will not
	/// help, so it is reported as a fault rather than as a declined capability.
	/// </exception>
	public bool UpdateTexture(nint id, ReadOnlySpan<byte> rgba, int width, int height);

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

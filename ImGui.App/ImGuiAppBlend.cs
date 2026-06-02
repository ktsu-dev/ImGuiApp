// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;
using Hexa.NET.ImGui;

/// <summary>
/// Blend modes that can be applied to a region of an ImGui draw list via
/// <see cref="ImGuiApp.SetDrawBlendMode"/>.
/// </summary>
public enum ImGuiAppBlendMode
{
	/// <summary>
	/// Standard alpha-over compositing (<c>src*srcAlpha + dst*(1-srcAlpha)</c>). This is the
	/// default state the renderer sets up for every frame.
	/// </summary>
	AlphaBlend,

	/// <summary>
	/// Additive blending (<c>src*srcAlpha + dst</c>). Overlapping translucent shapes accumulate
	/// toward white instead of compositing alpha-over, producing a neon/glow look.
	/// </summary>
	Additive,
}

public static partial class ImGuiApp
{
	// AddCallback marshals these delegates to native function pointers that live in the ImGui
	// draw list and are invoked by the renderer during RenderDrawData. They must stay rooted for
	// the lifetime of the process so the marshalled thunks remain valid.
	private static readonly ImDrawCallback blendAdditiveCallback = CreateAdditiveCallback();
	private static readonly ImDrawCallback blendAlphaCallback = CreateAlphaCallback();

	// The method-group-to-delegate conversion crosses an unmanaged (pointer) signature, so it must
	// happen in an unsafe context; field initializers do not provide one.
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Converts a method group to the unmanaged ImDrawCallback delegate; no pointers are dereferenced here.")]
	private static unsafe ImDrawCallback CreateAdditiveCallback() => SetBlendModeAdditive;

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Converts a method group to the unmanaged ImDrawCallback delegate; no pointers are dereferenced here.")]
	private static unsafe ImDrawCallback CreateAlphaCallback() => SetBlendModeAlpha;

	/// <summary>
	/// Inserts a blend-mode change into <paramref name="drawList"/> at the current point in its
	/// command stream. Primitives recorded after this call are drawn with <paramref name="mode"/>
	/// until another <see cref="SetDrawBlendMode"/> call changes it again.
	/// </summary>
	/// <remarks>
	/// This is the supported way to get additive/glow blending on the desktop (OpenGL) head: the
	/// renderer keeps its <c>GL</c> instance private, so callers cannot flip <c>glBlendFunc</c>
	/// directly. After drawing a glow region, call this again with
	/// <see cref="ImGuiAppBlendMode.AlphaBlend"/> to restore normal compositing for the rest of the
	/// frame — the blend func is global GL state for the remainder of the draw pass.
	/// </remarks>
	/// <param name="drawList">The draw list to record the blend-mode change into. Typically the
	/// window, background, or foreground draw list.</param>
	/// <param name="mode">The blend mode to activate for subsequent primitives.</param>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "AddCallback requires a void* userdata argument; we pass null and retain no pointers.")]
	public static unsafe void SetDrawBlendMode(ImDrawListPtr drawList, ImGuiAppBlendMode mode)
	{
		ImDrawCallback callback = mode == ImGuiAppBlendMode.Additive ? blendAdditiveCallback : blendAlphaCallback;
		drawList.AddCallback(callback, null);
	}

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Matches the ImDrawCallback unmanaged signature; the pointers are supplied by the renderer and not retained.")]
	private static unsafe void SetBlendModeAdditive(ImDrawList* parentList, ImDrawCmd* cmd) => controller?.SetBlendMode(ImGuiAppBlendMode.Additive);

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Matches the ImDrawCallback unmanaged signature; the pointers are supplied by the renderer and not retained.")]
	private static unsafe void SetBlendModeAlpha(ImDrawList* parentList, ImDrawCmd* cmd) => controller?.SetBlendMode(ImGuiAppBlendMode.AlphaBlend);
}

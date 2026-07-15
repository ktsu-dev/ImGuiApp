// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Semantics.Color;

/// <summary>
/// Direct bridges from a gamma-encoded <see cref="Srgb"/> value to ImGui's color types. ImGui works in
/// sRGB, so these pack the channels straight through with no linear round-trip — prefer them for colors
/// authored as sRGB (fixed UI colors, overlays, HSL-derived hues) rather than routing through the linear
/// <see cref="ktsu.Semantics.Color.Color"/>. Alpha is supplied separately since <see cref="Srgb"/> carries only RGB.
/// </summary>
public static class SrgbImGuiExtensions
{
	/// <summary>Converts to an ImGui <see cref="ImColor"/>, packing the sRGB channels directly.</summary>
	/// <param name="srgb">The sRGB color.</param>
	/// <param name="alpha">Alpha (0-1, default opaque).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor ToImColor(this Srgb srgb, float alpha = 1f) =>
		new() { Value = new Vector4((float)srgb.R, (float)srgb.G, (float)srgb.B, alpha) };

	/// <summary>Converts to a strong sRGB <see cref="ImGuiVector4"/>.</summary>
	/// <param name="srgb">The sRGB color.</param>
	/// <param name="alpha">Alpha (0-1, default opaque).</param>
	/// <returns>The sRGB vector ImGui expects.</returns>
	public static ImGuiVector4 ToImGuiVector4(this Srgb srgb, float alpha = 1f) =>
		new((float)srgb.R, (float)srgb.G, (float)srgb.B, alpha);

	/// <summary>
	/// Converts to ImGui's packed <c>ImU32</c> for drawing, applying the global style alpha exactly like
	/// <c>ImGui.GetColorU32</c> (see <see cref="ColorImGuiExtensions.ToImGuiU32(ImColor)"/>). Requires an
	/// active ImGui context, so call it during rendering.
	/// </summary>
	/// <param name="srgb">The sRGB color.</param>
	/// <param name="alpha">Alpha (0-1, default opaque).</param>
	/// <returns>The packed <c>ImU32</c>.</returns>
	public static uint ToImGuiU32(this Srgb srgb, float alpha = 1f) => srgb.ToImColor(alpha).ToImGuiU32();
}

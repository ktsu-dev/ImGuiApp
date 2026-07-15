// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Semantics.Color;

/// <summary>
/// Extension methods bridging the semantic <see cref="Color"/> and ImGui's <see cref="ImColor"/>
/// / <see cref="Vector4"/>. All conversions emit or accept gamma-encoded sRGB — the form ImGui expects —
/// so callers never reason about the sRGB/linear boundary.
/// </summary>
public static class ColorImGuiExtensions
{
	/// <summary>Converts a color to an ImGui <see cref="ImColor"/> (sRGB-encoded).</summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor ToImColor(this Color color) => new() { Value = color.ToImGuiVector4() };

	/// <summary>Creates a color from an ImGui <see cref="ImColor"/>, interpreting its channels as sRGB.</summary>
	/// <param name="color">The ImGui color.</param>
	/// <returns>The semantic color.</returns>
	public static Color FromImColor(this ImColor color) =>
		Color.FromSrgb(color.Value.X, color.Value.Y, color.Value.Z, color.Value.W);

	/// <summary>Converts a color to a strong sRGB-encoded <see cref="ImGuiVector4"/>.</summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The sRGB vector ImGui expects.</returns>
	public static ImGuiVector4 ToImGuiVector4(this Color color) => new(color.ToSrgbVector4());

	/// <summary>Creates a color from a strong <see cref="ImGuiVector4"/>, interpreting it as sRGB.</summary>
	/// <param name="srgb">The sRGB vector.</param>
	/// <returns>The semantic color.</returns>
	public static Color FromImGuiVector4(ImGuiVector4 srgb) =>
		Color.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);

	/// <summary>Creates a color from a raw sRGB <see cref="Vector4"/> (e.g. an ImGui value), interpreting it as sRGB.</summary>
	/// <param name="srgb">The raw sRGB vector.</param>
	/// <returns>The semantic color.</returns>
	public static Color FromImGuiVector4(Vector4 srgb) =>
		Color.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);

	/// <summary>
	/// Converts a color to ImGui's packed 32-bit <c>ImU32</c> representation (sRGB-encoded), using the default
	/// <c>IM_COL32</c> byte layout of <c>0xAABBGGRR</c> (red in the low byte, alpha in the high byte).
	/// </summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The packed <c>ImU32</c> value, matching <c>ImGui.ColorConvertFloat4ToU32</c>.</returns>
	public static uint ToImGuiU32(this Color color)
	{
		ImGuiVector4 srgb = color.ToImGuiVector4();
		uint r = ToByte(srgb.X);
		uint g = ToByte(srgb.Y);
		uint b = ToByte(srgb.Z);
		uint a = ToByte(srgb.W);
		return r | (g << 8) | (b << 16) | (a << 24);
	}

	/// <summary>
	/// Converts an <see cref="ImColor"/> to ImGui's packed 32-bit <c>ImU32</c> for drawing, applying the current
	/// global <see cref="ImGuiStyle.Alpha"/> exactly as <c>ImGui.GetColorU32</c> does — so custom-drawn widgets
	/// still dim inside disabled or alpha-faded scopes. The channels are already sRGB and are packed directly,
	/// with no linear round-trip. Requires an active ImGui context, so call it during rendering.
	/// </summary>
	/// <param name="color">The ImGui color.</param>
	/// <returns>The packed <c>ImU32</c>, equivalent to <c>ImGui.GetColorU32(color.Value)</c>.</returns>
	public static uint ToImGuiU32(this ImColor color) => ImGui.GetColorU32(color.Value);

	/// <summary>
	/// Creates a color from ImGui's packed 32-bit <c>ImU32</c> representation, interpreting its channels as sRGB.
	/// Assumes the default <c>IM_COL32</c> byte layout of <c>0xAABBGGRR</c>.
	/// </summary>
	/// <param name="packed">The packed <c>ImU32</c> value (as produced by <c>ImGui.ColorConvertFloat4ToU32</c>).</param>
	/// <returns>The semantic color.</returns>
	public static Color FromImGuiU32(uint packed)
	{
		const float scale = 1f / 255f;
		float r = (packed & 0xFF) * scale;
		float g = ((packed >> 8) & 0xFF) * scale;
		float b = ((packed >> 16) & 0xFF) * scale;
		float a = ((packed >> 24) & 0xFF) * scale;
		return Color.FromSrgb(r, g, b, a);
	}

	/// <summary>Saturates a normalized channel to [0, 1] and rounds to a byte, matching ImGui's <c>IM_F32_TO_INT8_SAT</c>.</summary>
	private static uint ToByte(float value)
	{
		float saturated = value < 0f ? 0f : value > 1f ? 1f : value;
		return (uint)((saturated * 255f) + 0.5f);
	}
}

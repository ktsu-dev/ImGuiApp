// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System.Numerics;

using Hexa.NET.ImGui;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// Extension methods bridging the semantic <see cref="SemanticColor"/> and ImGui's <see cref="ImColor"/>
/// / <see cref="Vector4"/>. All conversions emit or accept gamma-encoded sRGB — the form ImGui expects —
/// so callers never reason about the sRGB/linear boundary.
/// </summary>
public static class ColorImGuiExtensions
{
	/// <summary>Converts a color to an ImGui <see cref="ImColor"/> (sRGB-encoded).</summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor ToImColor(this SemanticColor color) => new() { Value = color.ToSrgbVector4() };

	/// <summary>Creates a color from an ImGui <see cref="ImColor"/>, interpreting its channels as sRGB.</summary>
	/// <param name="color">The ImGui color.</param>
	/// <returns>The semantic color.</returns>
	public static SemanticColor FromImColor(this ImColor color) =>
		SemanticColor.FromSrgb(color.Value.X, color.Value.Y, color.Value.Z, color.Value.W);

	/// <summary>Converts a color to a strong sRGB-encoded <see cref="ImGuiVector4"/>.</summary>
	/// <param name="color">The color to convert.</param>
	/// <returns>The sRGB vector ImGui expects.</returns>
	public static ImGuiVector4 ToImGuiVector4(this SemanticColor color) => new(color.ToSrgbVector4());

	/// <summary>Creates a color from a strong <see cref="ImGuiVector4"/>, interpreting it as sRGB.</summary>
	/// <param name="srgb">The sRGB vector.</param>
	/// <returns>The semantic color.</returns>
	public static SemanticColor FromImGuiVector4(ImGuiVector4 srgb) =>
		SemanticColor.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);

	/// <summary>Creates a color from a raw sRGB <see cref="Vector4"/> (e.g. an ImGui value), interpreting it as sRGB.</summary>
	/// <param name="srgb">The raw sRGB vector.</param>
	/// <returns>The semantic color.</returns>
	public static SemanticColor FromImGuiVector4(Vector4 srgb) =>
		SemanticColor.FromSrgb(srgb.X, srgb.Y, srgb.Z, srgb.W);
}

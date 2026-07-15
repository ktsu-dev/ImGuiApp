// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System.Numerics;

/// <summary>
/// An sRGB-encoded, ImGui-ready color vector. Widens implicitly to <see cref="Vector4"/> so it drops
/// into any ImGui call expecting a <see cref="Vector4"/> without ceremony.
/// </summary>
/// <param name="X">Red channel (sRGB, 0..1).</param>
/// <param name="Y">Green channel (sRGB, 0..1).</param>
/// <param name="Z">Blue channel (sRGB, 0..1).</param>
/// <param name="W">Alpha channel (0..1).</param>
public readonly record struct ImGuiVector4(float X, float Y, float Z, float W)
{
	/// <summary>The identity tint — opaque white <c>(1, 1, 1, 1)</c>, matching <see cref="Vector4.One"/>.</summary>
	public static ImGuiVector4 One { get; } = new(1f, 1f, 1f, 1f);

	/// <summary>The fully-transparent zero color <c>(0, 0, 0, 0)</c>, matching <see cref="Vector4.Zero"/>.</summary>
	public static ImGuiVector4 Zero { get; } = new(0f, 0f, 0f, 0f);

	/// <summary>Creates an <see cref="ImGuiVector4"/> from a <see cref="Vector4"/>.</summary>
	/// <param name="value">The source vector (interpreted as sRGB RGBA).</param>
	public ImGuiVector4(Vector4 value) : this(value.X, value.Y, value.Z, value.W)
	{
	}

	/// <summary>Widens to a <see cref="Vector4"/> for ImGui calls expecting one.</summary>
	/// <param name="value">The strong vector to widen.</param>
	public static implicit operator Vector4(ImGuiVector4 value) => new(value.X, value.Y, value.Z, value.W);

	/// <summary>Converts to a <see cref="Vector4"/> (named alternate for the implicit operator).</summary>
	/// <returns>The equivalent <see cref="Vector4"/>.</returns>
	public Vector4 ToVector4() => new(X, Y, Z, W);
}

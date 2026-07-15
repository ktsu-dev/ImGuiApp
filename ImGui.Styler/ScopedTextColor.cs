// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

/// <summary>
/// Represents a scoped text color change in ImGui.
/// </summary>
/// <param name="color">The color to apply to the text.</param>
public class ScopedTextColor(ImColor color) : ScopedColor(ImGuiCol.Text, color)
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedTextColor"/> class with a semantic color.
	/// </summary>
	/// <param name="color">The semantic color to apply to the text.</param>
	public ScopedTextColor(Color color) : this(color.ToImColor())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedTextColor"/> class with an sRGB color.
	/// </summary>
	/// <param name="srgb">The sRGB color to apply to the text; packed directly with no linear round-trip.</param>
	/// <param name="alpha">The alpha to apply (0-1, default opaque).</param>
	public ScopedTextColor(Srgb srgb, float alpha = 1f) : this(srgb.ToImColor(alpha))
	{
	}
}

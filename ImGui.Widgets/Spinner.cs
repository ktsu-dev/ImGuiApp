// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaSpinner = Hexa.NET.ImGui.Widgets.ImGuiSpinner;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an indeterminate loading spinner that animates from the ImGui frame time.
	/// </summary>
	/// <remarks>
	/// Hexa's spinner centres its arc <paramref name="radius"/> in from the cursor and sizes its item
	/// to the radius alone, so half the stroke lands outside the item: left of the cursor, and past
	/// the right edge of the space it reserves. This insets the cursor by half the stroke before
	/// drawing and reserves the stroke's width as well, so the whole arc is inside the item.
	/// </remarks>
	/// <param name="radius">Radius of the spinner in pixels.</param>
	/// <param name="thickness">Stroke thickness of the spinner arc in pixels.</param>
	/// <param name="color">Color of the spinner arc.</param>
	public static void Spinner(float radius, float thickness, Srgb color)
	{
		float halfStroke = MathF.Max(thickness, 0f) / 2f;
		float framePaddingY = ImGui.GetStyle().FramePadding.Y;
		float insetY = MathF.Max(halfStroke - framePaddingY, 0f);
		Vector2 origin = ImGui.GetCursorScreenPos();

		ImGui.BeginGroup();
		ImGui.SetCursorScreenPos(origin + new Vector2(halfStroke, insetY));
		HexaSpinner.Spinner(radius, thickness, color.ToImGuiU32());
		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2((radius * 2f) + (halfStroke * 2f), ((radius + framePaddingY) * 2f) + (insetY * 2f)));
		ImGui.EndGroup();
	}
}

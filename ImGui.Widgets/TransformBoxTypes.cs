// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>What part of a <see cref="ImGuiWidgets.TransformBox"/> the pointer has hold of.</summary>
public enum TransformBoxHandle
{
	/// <summary>Nothing: the pointer is outside the box and off its handles.</summary>
	None,

	/// <summary>The inside of the box, which moves it.</summary>
	Body,

	/// <summary>The left edge.</summary>
	Left,

	/// <summary>The right edge.</summary>
	Right,

	/// <summary>The top edge.</summary>
	Top,

	/// <summary>The bottom edge.</summary>
	Bottom,

	/// <summary>The top-left corner.</summary>
	TopLeft,

	/// <summary>The top-right corner.</summary>
	TopRight,

	/// <summary>The bottom-left corner.</summary>
	BottomLeft,

	/// <summary>The bottom-right corner.</summary>
	BottomRight,
}

/// <summary>An axis-aligned rectangle in a <see cref="ImGuiWidgets.TransformBox"/>'s own frame.</summary>
/// <param name="Min">The corner with the smaller coordinates, which is the top left when the frame is drawn unturned.</param>
/// <param name="Max">The corner with the larger coordinates.</param>
public readonly record struct TransformBoxRect(Vector2 Min, Vector2 Max)
{
	/// <summary>Gets the unit square, from (0, 0) to (1, 1).</summary>
	public static TransformBoxRect Unit { get; } = new(Vector2.Zero, Vector2.One);

	/// <summary>Gets the rectangle's width and height.</summary>
	public Vector2 Size => Max - Min;

	/// <summary>Gets the four corners, clockwise from the top left.</summary>
	/// <returns>Top left, top right, bottom right and bottom left, in the frame.</returns>
	public Vector2[] Corners() => [Min, new(Max.X, Min.Y), Max, new(Min.X, Max.Y)];
}

/// <summary>A drag on a <see cref="ImGuiWidgets.TransformBox"/>, as the frame sees it.</summary>
/// <param name="Handle">What was pressed.</param>
/// <param name="PressRect">The rectangle when the press began.</param>
/// <param name="PressPoint">The pointer when the press began, in the frame.</param>
/// <param name="Point">The pointer now, in the frame.</param>
/// <param name="Uniform">
/// Whether a corner drag should keep the rectangle's proportions: the box's
/// <see cref="TransformBoxOptions.UniformCorners"/>, inverted while Shift is held.
/// </param>
/// <param name="FrameToScreen">The transform the box was drawn through.</param>
/// <remarks>
/// A drag is always described from its start, never from the previous frame, so a rule that applies
/// it cannot accumulate rounding into drift and a constraint never re-anchors mid-drag.
/// </remarks>
public readonly record struct TransformBoxDrag(
	TransformBoxHandle Handle,
	TransformBoxRect PressRect,
	Vector2 PressPoint,
	Vector2 Point,
	bool Uniform,
	Matrix3x2 FrameToScreen)
{
	/// <summary>Gets how far the pointer has moved since the press, in the frame.</summary>
	public Vector2 Delta => Point - PressPoint;
}

/// <summary>What a <see cref="ImGuiWidgets.TransformBox"/> did this frame.</summary>
/// <param name="Changed">Whether the rectangle changed.</param>
/// <param name="Held">Whether a handle is held at the end of the frame.</param>
/// <param name="Released">Whether a held handle was let go this frame.</param>
/// <param name="Handle">The handle held, or hovered when nothing is; <see cref="TransformBoxHandle.None"/> otherwise.</param>
public readonly record struct TransformBoxResult(bool Changed, bool Held, bool Released, TransformBoxHandle Handle);

/// <summary>How a <see cref="ImGuiWidgets.TransformBox"/> behaves.</summary>
public sealed class TransformBoxOptions
{
	/// <summary>Gets or sets whether a corner drag keeps proportions unless Shift is held. Off by default, so Shift locks them instead.</summary>
	public bool UniformCorners { get; set; }

	/// <summary>Gets or sets the smallest the built-in rule lets either side become on screen, in pixels. Defaults to 4.</summary>
	public float MinimumScreenSize { get; set; } = 4f;

	/// <summary>Gets or sets whether the edge handles exist. On by default.</summary>
	public bool EdgeHandles { get; set; } = true;

	/// <summary>
	/// Gets or sets a rule that turns a drag into the new rectangle, replacing the built-in one: for an
	/// aspect constraint, a canvas to stay inside, or a snap. Leave null to move, resize and scale freely.
	/// </summary>
	public Func<TransformBoxDrag, TransformBoxRect>? Resize { get; set; }
}

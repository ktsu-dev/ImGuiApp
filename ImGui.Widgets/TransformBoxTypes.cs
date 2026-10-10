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

/// <summary>Four corners of a distorted <see cref="ImGuiWidgets.TransformBox"/>, in its frame.</summary>
/// <param name="TopLeft">The corner that was the rectangle's top left.</param>
/// <param name="TopRight">The corner that was its top right.</param>
/// <param name="BottomRight">The corner that was its bottom right.</param>
/// <param name="BottomLeft">The corner that was its bottom left.</param>
/// <remarks>
/// Each corner keeps the name it had on the rectangle, however far it has been dragged, so a caller
/// mapping the rectangle onto the quadrilateral pairs them up by name.
/// </remarks>
public readonly record struct TransformBoxQuad(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft)
{
	/// <summary>Gets the corners as an array, clockwise from the top left.</summary>
	/// <returns>Top left, top right, bottom right and bottom left.</returns>
	public Vector2[] ToArray() => [TopLeft, TopRight, BottomRight, BottomLeft];

	/// <summary>Gets the quadrilateral with every corner moved by the same amount.</summary>
	/// <param name="offset">How far to move it.</param>
	/// <returns>The moved quadrilateral.</returns>
	public TransformBoxQuad Offset(Vector2 offset) =>
		new(TopLeft + offset, TopRight + offset, BottomRight + offset, BottomLeft + offset);

	/// <summary>Builds a quadrilateral from four corners, clockwise from the top left.</summary>
	/// <param name="corners">Top left, top right, bottom right and bottom left.</param>
	/// <returns>The quadrilateral.</returns>
	/// <exception cref="ArgumentException">There are not exactly four corners.</exception>
	public static TransformBoxQuad FromCorners(ReadOnlySpan<Vector2> corners) => corners.Length == 4
		? new(corners[0], corners[1], corners[2], corners[3])
		: throw new ArgumentException("A quadrilateral has four corners.", nameof(corners));
}

/// <summary>A rectangle in a <see cref="ImGuiWidgets.TransformBox"/>'s own frame, optionally skewed into a parallelogram or distorted into any convex quadrilateral.</summary>
/// <param name="Min">The corner with the smaller coordinates, which is the top left when the frame is drawn unturned.</param>
/// <param name="Max">The corner with the larger coordinates.</param>
/// <remarks>
/// <para>
/// <see cref="Min"/> and <see cref="Max"/> describe the rectangle before its <see cref="Skew"/>, which
/// shears it about its own centre: a box that has never been skewed is exactly the rectangle between
/// them. <see cref="Shape"/> carries the rectangle onto the parallelogram, and <see cref="Corners"/>
/// answers the parallelogram's corners.
/// </para>
/// <para>
/// A distorted box has a <see cref="Quad"/>, and its corners are that quadrilateral's however
/// <see cref="Min"/>, <see cref="Max"/> and <see cref="Skew"/> read: they describe the rectangle the
/// distortion started from and nothing else. Mapping the rectangle onto the quadrilateral is a
/// projective transform, which no <see cref="Matrix3x2"/> can hold, so the box hands back the corners
/// and the caller builds whatever transform it needs from them.
/// </para>
/// </remarks>
public readonly record struct TransformBoxRect(Vector2 Min, Vector2 Max)
{
	/// <summary>Gets the unit square, from (0, 0) to (1, 1).</summary>
	public static TransformBoxRect Unit { get; } = new(Vector2.Zero, Vector2.One);

	/// <summary>Gets the rectangle's width and height, before its skew.</summary>
	public Vector2 Size => Max - Min;

	/// <summary>Gets the rectangle's centre, which its skew leaves where it is.</summary>
	public Vector2 Center => (Min + Max) / 2f;

	/// <summary>
	/// Gets the shear applied about the centre, in the frame. <c>X</c> is how far the box's points move
	/// along x for each unit they sit below the centre, which slants the left and right sides and is what
	/// dragging the top or bottom edge sideways sets; <c>Y</c> is how far they move along y for each unit
	/// to the right, which slants the top and bottom. Zero, the default, is an unskewed rectangle.
	/// </summary>
	public Vector2 Skew { get; init; }

	/// <summary>Gets whether the box is skewed at all.</summary>
	public bool IsSkewed => Skew != Vector2.Zero;

	/// <summary>
	/// Gets the box's corners once a corner has been dragged on its own, or null while it is a
	/// rectangle or a parallelogram. Set by a distort drag (Ctrl+Shift on a corner, with
	/// <see cref="TransformBoxOptions.Distort"/> on); a caller may also set it to show a box it
	/// distorted earlier.
	/// </summary>
	public TransformBoxQuad? Quad { get; init; }

	/// <summary>Gets whether the box has been distorted into a free quadrilateral.</summary>
	public bool IsDistorted => Quad.HasValue;

	/// <summary>Gets the transform that carries the unskewed rectangle onto the box, within the frame.</summary>
	/// <returns>The identity when the box is not skewed; otherwise the shear about <see cref="Center"/>.</returns>
	public Matrix3x2 Shape()
	{
		if (!IsSkewed)
		{
			return Matrix3x2.Identity;
		}

		Vector2 center = Center;
		Matrix3x2 shear = new(1f, Skew.Y, Skew.X, 1f, 0f, 0f);
		return Matrix3x2.CreateTranslation(-center) * shear * Matrix3x2.CreateTranslation(center);
	}

	/// <summary>Gets the four corners, clockwise from the top left, with the skew applied, or the distorted corners when there are some.</summary>
	/// <returns>Top left, top right, bottom right and bottom left, in the frame.</returns>
	public Vector2[] Corners()
	{
		if (Quad is TransformBoxQuad quad)
		{
			return quad.ToArray();
		}

		Vector2[] corners = [Min, new(Max.X, Min.Y), Max, new(Min.X, Max.Y)];
		if (IsSkewed)
		{
			Matrix3x2 shape = Shape();
			for (int index = 0; index < corners.Length; index++)
			{
				corners[index] = Vector2.Transform(corners[index], shape);
			}
		}

		return corners;
	}
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

	/// <summary>
	/// Gets whether the drag skews rather than resizes: an edge pressed with Ctrl held while
	/// <see cref="TransformBoxOptions.Skew"/> is on. The edge slides along itself and the opposite edge
	/// stays where it is.
	/// </summary>
	public bool Skew { get; init; }

	/// <summary>
	/// Gets whether the drag distorts: a corner pressed with Ctrl and Shift held while
	/// <see cref="TransformBoxOptions.Distort"/> is on. That corner moves on its own and the other three
	/// stay where they are.
	/// </summary>
	public bool Distort { get; init; }
}

/// <summary>What a <see cref="ImGuiWidgets.TransformBox"/> did this frame.</summary>
/// <param name="Changed">Whether the rectangle changed.</param>
/// <param name="Held">Whether a handle is held at the end of the frame.</param>
/// <param name="Released">Whether a held handle was let go this frame.</param>
/// <param name="Handle">The handle held, or hovered when nothing is; <see cref="TransformBoxHandle.None"/> otherwise.</param>
public readonly record struct TransformBoxResult(bool Changed, bool Held, bool Released, TransformBoxHandle Handle)
{
	/// <summary>
	/// Gets whether <see cref="Handle"/> skews: the held drag is a skew, or, with nothing held, a press
	/// on the hovered edge would start one because Ctrl is down.
	/// </summary>
	public bool Skewing { get; init; }

	/// <summary>
	/// Gets whether <see cref="Handle"/> distorts: the held drag moves one corner alone, or, with nothing
	/// held, a press on the hovered corner would because Ctrl and Shift are down.
	/// </summary>
	public bool Distorting { get; init; }
}

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
	/// Gets or sets whether Ctrl-dragging an edge handle skews the box along that edge instead of moving
	/// it. Off by default, since a crop window or a paragraph box has no use for a slant.
	/// </summary>
	public bool Skew { get; set; }

	/// <summary>
	/// Gets or sets whether Ctrl+Shift-dragging a corner moves that corner alone, distorting the box into
	/// a free quadrilateral. Off by default. Once a box is distorted every handle moves corners rather
	/// than resizing: a corner moves itself, an edge its two corners, and the body all four. The
	/// quadrilateral is kept convex, so the projective transform onto it stays defined.
	/// </summary>
	public bool Distort { get; set; }

	/// <summary>
	/// Gets or sets a rule that turns a drag into the new rectangle, replacing the built-in one: for an
	/// aspect constraint, a canvas to stay inside, or a snap. Leave null to move, resize and scale freely.
	/// </summary>
	public Func<TransformBoxDrag, TransformBoxRect>? Resize { get; set; }
}

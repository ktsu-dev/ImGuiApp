// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The hit-testing and drag rules behind <see cref="TransformBox"/>, with no ImGui dependency.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Hit-testing happens on screen and dragging happens in the frame. The grab radius is a number of
	/// screen pixels, and the frame can be stretched or turned by any amount on its way to the screen, so
	/// no one radius in frame units would be the same reach on every side. A drag, though, is a change to
	/// the rectangle, which lives in the frame, so the pointer is carried back through the inverse.
	/// </para>
	/// <para>
	/// Corners are tested before edges and edges before the body, so the corner of a small box is still a
	/// corner. Inside the box the reach of each handle is capped at a quarter of the side it sits on, so a
	/// box too small on screen for its handles to leave any body still has one to move it by.
	/// </para>
	/// </remarks>
	internal sealed class TransformBoxState
	{
		/// <summary>How close, in screen pixels, a pointer has to be to hold a handle.</summary>
		public const float GrabPixels = 8f;

		private TransformBoxRect pressRect;
		private Vector2 pressPoint;
		private Matrix3x2 pressInverse;
		private Matrix3x2 pressFrameToScreen;

		/// <summary>Gets the handle the current press holds.</summary>
		public TransformBoxHandle Active { get; private set; }

		/// <summary>Finds the handle under a point on screen.</summary>
		/// <param name="rect">The box, in its frame.</param>
		/// <param name="frameToScreen">The transform the box is drawn through.</param>
		/// <param name="screenPoint">The pointer, in screen pixels.</param>
		/// <param name="edgeHandles">Whether the edges can be held.</param>
		/// <returns>The first handle within reach: a corner, then an edge, then the body.</returns>
		public static TransformBoxHandle HitTest(TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 screenPoint, bool edgeHandles)
		{
			// A frame that cannot be inverted could never be dragged, so none of it is offered.
			if (!IsFinite(frameToScreen) || !IsFinite(rect.Min) || !IsFinite(rect.Max) || !Matrix3x2.Invert(frameToScreen, out _))
			{
				return TransformBoxHandle.None;
			}

			Vector2[] corners = ScreenCorners(rect, frameToScreen);
			float width = Vector2.Distance(corners[0], corners[1]);
			float height = Vector2.Distance(corners[0], corners[3]);
			bool inside = Contains(rect, frameToScreen, screenPoint);

			// Along the top and bottom edges the side that matters is the width, and along the left and
			// right ones the height, so each reach is capped by the side it would eat into.
			float acrossVertical = inside ? MathF.Min(GrabPixels, width / 4f) : GrabPixels;
			float acrossHorizontal = inside ? MathF.Min(GrabPixels, height / 4f) : GrabPixels;
			float cornerReach = MathF.Min(acrossVertical, acrossHorizontal);

			TransformBoxHandle[] cornerHandles = [TransformBoxHandle.TopLeft, TransformBoxHandle.TopRight, TransformBoxHandle.BottomRight, TransformBoxHandle.BottomLeft];
			for (int index = 0; index < 4; index++)
			{
				if (Vector2.Distance(screenPoint, corners[index]) <= cornerReach)
				{
					return cornerHandles[index];
				}
			}

			if (edgeHandles)
			{
				if (DistanceToSegment(screenPoint, corners[0], corners[1]) <= acrossHorizontal)
				{
					return TransformBoxHandle.Top;
				}

				if (DistanceToSegment(screenPoint, corners[3], corners[2]) <= acrossHorizontal)
				{
					return TransformBoxHandle.Bottom;
				}

				if (DistanceToSegment(screenPoint, corners[0], corners[3]) <= acrossVertical)
				{
					return TransformBoxHandle.Left;
				}

				if (DistanceToSegment(screenPoint, corners[1], corners[2]) <= acrossVertical)
				{
					return TransformBoxHandle.Right;
				}
			}

			return inside ? TransformBoxHandle.Body : TransformBoxHandle.None;
		}

		/// <summary>The box's corners on screen, clockwise from the frame's top left.</summary>
		/// <param name="rect">The box, in its frame.</param>
		/// <param name="frameToScreen">The transform the box is drawn through.</param>
		/// <returns>Top left, top right, bottom right and bottom left, as the frame names them.</returns>
		public static Vector2[] ScreenCorners(TransformBoxRect rect, Matrix3x2 frameToScreen)
		{
			Vector2[] corners = rect.Corners();
			for (int index = 0; index < corners.Length; index++)
			{
				corners[index] = Vector2.Transform(corners[index], frameToScreen);
			}

			return corners;
		}

		/// <summary>Applies a drag with the built-in rule: move the body, an edge, or a corner.</summary>
		/// <param name="drag">The drag, from its start.</param>
		/// <param name="minimumScreenSize">The smallest either side may become on screen, in pixels.</param>
		/// <returns>The rectangle the drag asks for.</returns>
		/// <remarks>
		/// No side passes its opposite: a box dragged inside out stops at the minimum rather than
		/// flipping, since flipping is a different edit with its own control. A proportional corner drag
		/// scales about the opposite corner by the pointer's distance along the diagonal <em>on
		/// screen</em>, which is the distance the user sees; measured in the frame, a stretched frame would
		/// make one axis of the pointer's travel count for more than the other.
		/// </remarks>
		public static TransformBoxRect Resize(TransformBoxDrag drag, float minimumScreenSize)
		{
			TransformBoxRect from = drag.PressRect;
			Vector2 delta = drag.Delta;
			Vector2 minimum = MinimumFrameSize(drag.FrameToScreen, minimumScreenSize);

			return drag.Handle switch
			{
				TransformBoxHandle.Body => new TransformBoxRect(from.Min + delta, from.Max + delta),
				TransformBoxHandle.Left or TransformBoxHandle.Right or TransformBoxHandle.Top or TransformBoxHandle.Bottom =>
					MoveEdges(from, drag.Handle, delta, minimum),
				TransformBoxHandle.TopLeft or TransformBoxHandle.TopRight or TransformBoxHandle.BottomLeft or TransformBoxHandle.BottomRight =>
					drag.Uniform ? ScaleAboutOppositeCorner(drag, minimum) : MoveEdges(from, drag.Handle, delta, minimum),
				_ => from,
			};
		}

		/// <summary>Starts a press on a handle.</summary>
		/// <param name="handle">The handle pressed, usually from <see cref="HitTest"/>.</param>
		/// <param name="rect">The box at the press.</param>
		/// <param name="frameToScreen">The transform the box is drawn through at the press.</param>
		/// <param name="screenPoint">The pointer at the press, in screen pixels.</param>
		/// <returns>False, and nothing held, when the transform cannot be inverted.</returns>
		/// <remarks>
		/// The inverse is taken once, at the press, and every frame of the drag is carried back through
		/// it. A caller whose edit changes the transform the box is drawn through, as moving a layer does
		/// when the box is the layer's own frame, would otherwise measure each frame against a frame the
		/// previous one had already moved.
		/// </remarks>
		public bool Begin(TransformBoxHandle handle, TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 screenPoint)
		{
			if (handle == TransformBoxHandle.None || !Matrix3x2.Invert(frameToScreen, out Matrix3x2 inverse) || !IsFinite(inverse))
			{
				Active = TransformBoxHandle.None;
				return false;
			}

			Active = handle;
			pressRect = rect;
			pressInverse = inverse;
			pressFrameToScreen = frameToScreen;
			pressPoint = Vector2.Transform(screenPoint, inverse);
			return true;
		}

		/// <summary>Describes the drag so far, for a pointer now at a point on screen.</summary>
		/// <param name="screenPoint">The pointer now, in screen pixels.</param>
		/// <param name="uniform">Whether a corner keeps the rectangle's proportions.</param>
		/// <returns>The drag, measured in the frame the press began in.</returns>
		public TransformBoxDrag Drag(Vector2 screenPoint, bool uniform) =>
			new(Active, pressRect, pressPoint, Vector2.Transform(screenPoint, pressInverse), uniform, pressFrameToScreen);

		/// <summary>Ends the press.</summary>
		public void End() => Active = TransformBoxHandle.None;

		private static TransformBoxRect MoveEdges(TransformBoxRect from, TransformBoxHandle handle, Vector2 delta, Vector2 minimum)
		{
			float left = from.Min.X;
			float right = from.Max.X;
			float top = from.Min.Y;
			float bottom = from.Max.Y;

			if (handle is TransformBoxHandle.Left or TransformBoxHandle.TopLeft or TransformBoxHandle.BottomLeft)
			{
				left = MathF.Min(left + delta.X, right - minimum.X);
			}

			if (handle is TransformBoxHandle.Right or TransformBoxHandle.TopRight or TransformBoxHandle.BottomRight)
			{
				right = MathF.Max(right + delta.X, left + minimum.X);
			}

			if (handle is TransformBoxHandle.Top or TransformBoxHandle.TopLeft or TransformBoxHandle.TopRight)
			{
				top = MathF.Min(top + delta.Y, bottom - minimum.Y);
			}

			if (handle is TransformBoxHandle.Bottom or TransformBoxHandle.BottomLeft or TransformBoxHandle.BottomRight)
			{
				bottom = MathF.Max(bottom + delta.Y, top + minimum.Y);
			}

			return new TransformBoxRect(new Vector2(left, top), new Vector2(right, bottom));
		}

		private static TransformBoxRect ScaleAboutOppositeCorner(TransformBoxDrag drag, Vector2 minimum)
		{
			TransformBoxRect from = drag.PressRect;
			(Vector2 held, Vector2 opposite) = drag.Handle switch
			{
				TransformBoxHandle.TopLeft => (from.Min, from.Max),
				TransformBoxHandle.TopRight => (new Vector2(from.Max.X, from.Min.Y), new Vector2(from.Min.X, from.Max.Y)),
				TransformBoxHandle.BottomLeft => (new Vector2(from.Min.X, from.Max.Y), new Vector2(from.Max.X, from.Min.Y)),
				_ => (from.Max, from.Min),
			};

			Vector2 diagonal = Vector2.TransformNormal(held - opposite, drag.FrameToScreen);
			float length = diagonal.LengthSquared();
			if (length <= 0f || !float.IsFinite(length))
			{
				return from;
			}

			Vector2 pointer = Vector2.TransformNormal(held + drag.Delta - opposite, drag.FrameToScreen);
			float scale = Vector2.Dot(pointer, diagonal) / length;

			Vector2 size = Vector2.Abs(from.Size);
			float smallest = MathF.Max(
				size.X > 0f ? minimum.X / size.X : 0f,
				size.Y > 0f ? minimum.Y / size.Y : 0f);
			scale = MathF.Max(scale, smallest);

			Vector2 corner = opposite + ((held - opposite) * scale);
			return new TransformBoxRect(Vector2.Min(opposite, corner), Vector2.Max(opposite, corner));
		}

		/// <summary>How long each side of the frame has to be to span a number of screen pixels.</summary>
		private static Vector2 MinimumFrameSize(Matrix3x2 frameToScreen, float screenPixels)
		{
			float pixels = MathF.Max(screenPixels, 0f);
			float alongX = new Vector2(frameToScreen.M11, frameToScreen.M12).Length();
			float alongY = new Vector2(frameToScreen.M21, frameToScreen.M22).Length();
			return new Vector2(
				alongX > 0f ? pixels / alongX : 0f,
				alongY > 0f ? pixels / alongY : 0f);
		}

		private static bool Contains(TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 screenPoint)
		{
			if (!Matrix3x2.Invert(frameToScreen, out Matrix3x2 inverse))
			{
				return false;
			}

			Vector2 point = Vector2.Transform(screenPoint, inverse);
			Vector2 min = Vector2.Min(rect.Min, rect.Max);
			Vector2 max = Vector2.Max(rect.Min, rect.Max);
			return point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;
		}

		private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
		{
			Vector2 along = end - start;
			float length = along.LengthSquared();
			float t = length > 0f ? Math.Clamp(Vector2.Dot(point - start, along) / length, 0f, 1f) : 0f;
			return Vector2.Distance(point, start + (along * t));
		}

		private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

		private static bool IsFinite(Matrix3x2 value) =>
			float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M21)
			&& float.IsFinite(value.M22) && float.IsFinite(value.M31) && float.IsFinite(value.M32);
	}
}

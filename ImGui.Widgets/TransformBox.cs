// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a rectangle with corner and edge handles in its own frame, through a transform to the screen,
	/// and lets the user move it, resize it, and scale it.
	/// </summary>
	/// <param name="label">Unique id; also the probe scope of the handles.</param>
	/// <param name="rect">The rectangle in its frame. Owned by the caller; the widget writes a drag's result into it.</param>
	/// <param name="frameToScreen">
	/// Carries the frame to screen pixels. A crop window in fractions of an image passes the image's
	/// rectangle on screen; a layer passes its own placement composed with that, so its handles sit on
	/// its corners however it has been turned or stretched. Must be invertible for the box to be held.
	/// </param>
	/// <param name="clipMin">The top-left of the area the box is drawn and held in, in screen pixels.</param>
	/// <param name="clipMax">The bottom-right of that area.</param>
	/// <param name="options">How the box behaves, or null for the defaults.</param>
	/// <returns>What changed, and whether a handle is held or was released.</returns>
	/// <remarks>
	/// <para>
	/// <b>The box knows nothing about what it frames.</b> Dragging the body moves the rectangle, an edge
	/// moves that edge, and a corner moves both of its edges, or scales about the opposite corner when
	/// <see cref="TransformBoxOptions.UniformCorners"/> is on (Shift inverts it). A caller with its own
	/// rules — an aspect preset, a canvas to stay inside — supplies <see cref="TransformBoxOptions.Resize"/>
	/// and the box draws whatever that returns.
	/// </para>
	/// <para>
	/// <b>It takes the pointer only over itself.</b> The box submits its hit area only while the pointer
	/// is over a handle or the body, or while one is held, so a press anywhere else reaches whatever was
	/// drawn underneath, such as an <see cref="ImageCanvas"/>'s pan. Submit the item underneath with
	/// <c>ImGui.SetNextItemAllowOverlap()</c>, or it keeps the hover and the box is never pressed. The
	/// cursor is put back where it was found, so submit something afterwards, as any layout does.
	/// </para>
	/// <para>
	/// A drag is measured in the frame as it was at the press, so a caller whose edit moves the frame
	/// itself — a layer whose placement is the frame — gets the same answer whichever frame it passes
	/// while the drag goes on. The handles are marked for probes as <c>body</c>, <c>topLeft</c>,
	/// <c>topRight</c>, <c>bottomRight</c>, <c>bottomLeft</c>, <c>top</c>, <c>bottom</c>, <c>left</c> and
	/// <c>right</c> inside <paramref name="label"/>.
	/// </para>
	/// </remarks>
	public static TransformBoxResult TransformBox(string label, ref TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 clipMin, Vector2 clipMax, TransformBoxOptions? options = null)
	{
		Ensure.NotNull(label);
		return TransformBoxImpl.Draw(label, ref rect, frameToScreen, clipMin, clipMax, options ?? TransformBoxImpl.Defaults);
	}

	/// <summary>The ImGui half of <see cref="TransformBox"/>: input, drawing and probe regions.</summary>
	internal static class TransformBoxImpl
	{
		/// <summary>The options used when none are given.</summary>
		internal static readonly TransformBoxOptions Defaults = new();

		/// <summary>The press in progress, per box id.</summary>
		private static readonly Dictionary<uint, TransformBoxState> States = [];

		/// <summary>Half the side of a corner handle, in screen pixels.</summary>
		private const float CornerHalf = 4f;

		/// <summary>Half the length and half the thickness of an edge handle, in screen pixels.</summary>
		private static readonly Vector2 EdgeHalf = new(6f, 2f);

		private static readonly string[] CornerNames = ["topLeft", "topRight", "bottomRight", "bottomLeft"];

		private static readonly TransformBoxHandle[] CornerHandles =
			[TransformBoxHandle.TopLeft, TransformBoxHandle.TopRight, TransformBoxHandle.BottomRight, TransformBoxHandle.BottomLeft];

		internal static TransformBoxResult Draw(string label, ref TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 clipMin, Vector2 clipMax, TransformBoxOptions options)
		{
			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out TransformBoxState? state))
			{
				state = new TransformBoxState();
				States[id] = state;
			}

			Vector2 mouse = ImGui.GetMousePos();
			bool heldBefore = ImGuiP.GetActiveID() == id;

			// Nothing is offered while another item has the mouse, such as the canvas mid-pan, or while
			// the pointer is over a different window.
			bool reachable = heldBefore
				|| (!ImGui.IsAnyItemActive()
					&& ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
					&& mouse.X >= clipMin.X && mouse.Y >= clipMin.Y && mouse.X < clipMax.X && mouse.Y < clipMax.Y);
			TransformBoxHandle under = reachable
				? TransformBoxState.HitTest(rect, frameToScreen, mouse, options.EdgeHandles)
				: TransformBoxHandle.None;

			bool changed = false;
			bool released = false;

			// Submitted only when it is wanted, so a press anywhere else is never taken from the item
			// underneath. While held it is submitted every frame, or ImGui would drop the active id.
			if (under != TransformBoxHandle.None || heldBefore)
			{
				Vector2 cursor = ImGui.GetCursorScreenPos();
				ImGui.SetCursorScreenPos(clipMin);
				ImGui.InvisibleButton(label, Vector2.Max(clipMax - clipMin, Vector2.One));
				ImGuiProbes.MarkItem(label);
				bool activated = ImGui.IsItemActivated();
				bool active = ImGui.IsItemActive();
				bool deactivated = ImGui.IsItemDeactivated();
				ImGui.SetCursorScreenPos(cursor);

				if (activated)
				{
					state.Begin(under, rect, frameToScreen, mouse);
				}

				if (active && state.Active != TransformBoxHandle.None)
				{
					bool uniform = options.UniformCorners != ImGui.GetIO().KeyShift;
					TransformBoxDrag drag = state.Drag(mouse, uniform);
					TransformBoxRect next = options.Resize is { } resize
						? resize(drag)
						: TransformBoxState.Resize(drag, options.MinimumScreenSize);

					if (next != rect)
					{
						rect = next;
						changed = true;
					}
				}

				if (deactivated)
				{
					released = state.Active != TransformBoxHandle.None;
					state.End();
				}
			}
			else if (state.Active != TransformBoxHandle.None)
			{
				// Activity was lost without this box seeing it end: the window closed, or another item
				// took the mouse.
				released = true;
				state.End();
			}

			TransformBoxHandle hot = state.Active != TransformBoxHandle.None ? state.Active : under;
			if (hot != TransformBoxHandle.None)
			{
				SetCursor(hot, frameToScreen);
			}

			Render(label, rect, frameToScreen, clipMin, clipMax, options.EdgeHandles, hot);
			return new TransformBoxResult(changed, state.Active != TransformBoxHandle.None, released, hot);
		}

		private static void SetCursor(TransformBoxHandle handle, Matrix3x2 frameToScreen)
		{
			if (handle == TransformBoxHandle.Body)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
				return;
			}

			// The direction the handle pulls in on screen, which a turned or mirrored frame changes, so
			// the cursor is picked from that rather than from the handle's name.
			Vector2 outward = handle switch
			{
				TransformBoxHandle.Left => new Vector2(-1f, 0f),
				TransformBoxHandle.Right => new Vector2(1f, 0f),
				TransformBoxHandle.Top => new Vector2(0f, -1f),
				TransformBoxHandle.Bottom => new Vector2(0f, 1f),
				TransformBoxHandle.TopLeft => new Vector2(-1f, -1f),
				TransformBoxHandle.TopRight => new Vector2(1f, -1f),
				TransformBoxHandle.BottomLeft => new Vector2(-1f, 1f),
				_ => new Vector2(1f, 1f),
			};

			Vector2 onScreen = Vector2.TransformNormal(outward, frameToScreen);
			float degrees = MathF.Atan2(onScreen.Y, onScreen.X) * (180f / MathF.PI);

			// Folded onto a half turn, since a resize cursor points both ways, then picked by the nearest
			// of the four the platform offers.
			degrees = ((degrees % 180f) + 180f) % 180f;
			ImGuiMouseCursor cursor = degrees switch
			{
				< 22.5f or >= 157.5f => ImGuiMouseCursor.ResizeEw,
				< 67.5f => ImGuiMouseCursor.ResizeNwse,
				< 112.5f => ImGuiMouseCursor.ResizeNs,
				_ => ImGuiMouseCursor.ResizeNesw,
			};

			ImGui.SetMouseCursor(cursor);
		}

		private static void Render(string label, TransformBoxRect rect, Matrix3x2 frameToScreen, Vector2 clipMin, Vector2 clipMax, bool edgeHandles, TransformBoxHandle hot)
		{
			Vector2[] screen = TransformBoxState.ScreenCorners(rect, frameToScreen);
			foreach (Vector2 corner in screen)
			{
				if (!float.IsFinite(corner.X) || !float.IsFinite(corner.Y))
				{
					return;
				}
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.PushClipRect(clipMin, clipMax, true);

			uint outline = ImGui.GetColorU32(ImGuiCol.Text);
			for (int index = 0; index < 4; index++)
			{
				drawList.AddLine(screen[index], screen[(index + 1) % 4], outline, 1.5f);
			}

			// The box's own axes on screen, so each handle lies along its edge however the frame is turned.
			Vector2 along = Normalized(screen[1] - screen[0], Vector2.UnitX);
			Vector2 down = Normalized(screen[3] - screen[0], Vector2.UnitY);

			for (int index = 0; index < 4; index++)
			{
				DrawBar(drawList, screen[index], along * CornerHalf, down * CornerHalf, HandleColor(hot, CornerHandles[index]));
				MarkBox(label, CornerNames[index], screen[index], new Vector2(CornerHalf));
			}

			if (edgeHandles)
			{
				Vector2 top = (screen[0] + screen[1]) / 2f;
				Vector2 bottom = (screen[3] + screen[2]) / 2f;
				Vector2 left = (screen[0] + screen[3]) / 2f;
				Vector2 right = (screen[1] + screen[2]) / 2f;

				DrawBar(drawList, top, along * EdgeHalf.X, down * EdgeHalf.Y, HandleColor(hot, TransformBoxHandle.Top));
				DrawBar(drawList, bottom, along * EdgeHalf.X, down * EdgeHalf.Y, HandleColor(hot, TransformBoxHandle.Bottom));
				DrawBar(drawList, left, down * EdgeHalf.X, along * EdgeHalf.Y, HandleColor(hot, TransformBoxHandle.Left));
				DrawBar(drawList, right, down * EdgeHalf.X, along * EdgeHalf.Y, HandleColor(hot, TransformBoxHandle.Right));
				MarkBox(label, "top", top, new Vector2(CornerHalf));
				MarkBox(label, "bottom", bottom, new Vector2(CornerHalf));
				MarkBox(label, "left", left, new Vector2(CornerHalf));
				MarkBox(label, "right", right, new Vector2(CornerHalf));
			}

			drawList.PopClipRect();

			// The body's mark is its centre, a few pixels across: the bounding box of a turned rectangle
			// covers ground outside it, and a test aiming at the middle of that would miss.
			Vector2 centre = (screen[0] + screen[2]) / 2f;
			ImGuiProbes.MarkRegion($"{label}/body", centre - new Vector2(2f), centre + new Vector2(2f));
		}

		private static void DrawBar(ImDrawListPtr drawList, Vector2 centre, Vector2 halfAlong, Vector2 halfAcross, uint color) =>
			drawList.AddQuadFilled(
				centre - halfAlong - halfAcross,
				centre + halfAlong - halfAcross,
				centre + halfAlong + halfAcross,
				centre - halfAlong + halfAcross,
				color);

		/// <summary>Marks a handle by a small square about its centre, which is always within its reach.</summary>
		private static void MarkBox(string label, string handle, Vector2 centre, Vector2 half) =>
			ImGuiProbes.MarkRegion($"{label}/{handle}", centre - half, centre + half);

		private static uint HandleColor(TransformBoxHandle hot, TransformBoxHandle handle) =>
			ImGui.GetColorU32(hot == handle ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab);

		private static Vector2 Normalized(Vector2 vector, Vector2 fallback)
		{
			float length = vector.Length();
			return length > 1e-6f ? vector / length : fallback;
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

using CropHandle = ImGuiWidgets.CropOverlayState.CropHandle;

/// <summary>A crop rectangle in image pixels: centre, unrotated size, and a clockwise rotation about the centre.</summary>
/// <param name="Center">The crop's centre, in image pixels.</param>
/// <param name="Size">The crop's width and height before rotation, in image pixels.</param>
/// <param name="AngleDegrees">The clockwise rotation about <paramref name="Center"/>, in degrees.</param>
public readonly record struct CropRect(Vector2 Center, Vector2 Size, float AngleDegrees)
{
	/// <summary>The whole image, unrotated.</summary>
	/// <param name="imageSize">Native image size in pixels.</param>
	/// <returns>A crop covering the image exactly.</returns>
	public static CropRect FullImage(Vector2 imageSize) => new(imageSize / 2f, imageSize, 0f);

	/// <summary>The four corners in image pixels, clockwise from top-left, after rotation.</summary>
	/// <returns>Top-left, top-right, bottom-right and bottom-left.</returns>
	public Vector2[] Corners()
	{
		Vector2 half = Size / 2f;
		return
		[
			Center + ImGuiWidgets.CropOverlayState.Rotate(new Vector2(-half.X, -half.Y), AngleDegrees),
			Center + ImGuiWidgets.CropOverlayState.Rotate(new Vector2(half.X, -half.Y), AngleDegrees),
			Center + ImGuiWidgets.CropOverlayState.Rotate(new Vector2(half.X, half.Y), AngleDegrees),
			Center + ImGuiWidgets.CropOverlayState.Rotate(new Vector2(-half.X, half.Y), AngleDegrees),
		];
	}
}

public static partial class ImGuiWidgets
{
	/// <summary>Draws an interactive crop rectangle over an <see cref="ImageCanvas"/> drawn immediately before it.</summary>
	/// <param name="label">Unique id; also the probe name.</param>
	/// <param name="crop">The crop in image pixels. Owned by the caller; normalized on entry.</param>
	/// <param name="imageSize">Native image size in pixels, as passed to ImageCanvas.</param>
	/// <param name="canvasState">The same state passed to ImageCanvas. Pan, double-click fit and wheel zoom over the canvas are forwarded to it.</param>
	/// <param name="canvasMin">Screen position of the canvas's top-left; pass <c>ImGui.GetItemRectMin()</c> right after ImageCanvas.</param>
	/// <param name="canvasSize">The canvas size passed to ImageCanvas.</param>
	/// <param name="aspectRatio">Width / height to lock to; 0 (default) means free.</param>
	/// <param name="showThirds">Draw rule-of-thirds guides inside the crop (default true).</param>
	/// <param name="allowRotation">Show the rotate handle (default true); when false, AngleDegrees is forced to 0.</param>
	/// <returns>True if <paramref name="crop"/> changed this frame, including by normalization.</returns>
	/// <remarks>
	/// The overlay reserves no space: it submits one invisible button over the canvas and puts the cursor
	/// back where it found it, like <see cref="HandleTrack"/>. ImGui asserts if a window's last act is a
	/// cursor move with nothing after it, so submit something afterwards, as any real layout already does.
	/// </remarks>
	public static bool CropOverlay(string label, ref CropRect crop, Vector2 imageSize, ImageCanvasState canvasState, Vector2 canvasMin, Vector2 canvasSize, float aspectRatio = 0f, bool showThirds = true, bool allowRotation = true)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(canvasState);
		return CropOverlayImpl.Draw(label, ref crop, imageSize, canvasState, canvasMin, canvasSize, aspectRatio, showThirds, allowRotation);
	}

	/// <summary>The ImGui half of <see cref="CropOverlay"/>: input, drawing and probe regions.</summary>
	internal static class CropOverlayImpl
	{
		/// <summary>The drag in progress, per overlay id.</summary>
		private static readonly Dictionary<uint, CropOverlayState> States = [];

		/// <summary>Half the side of a corner handle, in screen pixels.</summary>
		private const float CornerHalf = 4f;

		/// <summary>Half the length and half the thickness of an edge handle, in screen pixels.</summary>
		private static readonly Vector2 EdgeHalf = new(6f, 2f);

		/// <summary>The rotate handle's radius, in screen pixels.</summary>
		private const float RotateRadius = 5f;

		/// <summary>Draws the overlay; see <see cref="CropOverlay"/>.</summary>
		/// <param name="label">Unique id; also the probe name.</param>
		/// <param name="crop">The crop in image pixels.</param>
		/// <param name="imageSize">Native image size in pixels.</param>
		/// <param name="canvasState">The canvas view state.</param>
		/// <param name="canvasMin">Screen position of the canvas's top-left.</param>
		/// <param name="canvasSize">The canvas size.</param>
		/// <param name="aspectRatio">Width / height to lock to; 0 means free.</param>
		/// <param name="showThirds">Draw rule-of-thirds guides.</param>
		/// <param name="allowRotation">Show the rotate handle.</param>
		/// <returns>True if <paramref name="crop"/> changed.</returns>
		internal static bool Draw(string label, ref CropRect crop, Vector2 imageSize, ImageCanvasState canvasState, Vector2 canvasMin, Vector2 canvasSize, float aspectRatio, bool showThirds, bool allowRotation)
		{
			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out CropOverlayState? state))
			{
				state = new CropOverlayState();
				States[id] = state;
			}

			canvasSize = Vector2.Max(canvasSize, Vector2.One);
			bool validImage = imageSize.X > 0f && imageSize.Y > 0f && float.IsFinite(imageSize.X) && float.IsFinite(imageSize.Y);

			bool changed = false;
			if (validImage)
			{
				CropRect normalized = CropOverlayState.Normalize(crop, imageSize, aspectRatio, allowRotation);
				if (normalized != crop)
				{
					crop = normalized;
					changed = true;
				}
			}

			Vector2 cursor = ImGui.GetCursorScreenPos();
			ImGui.SetCursorScreenPos(canvasMin);
			ImGui.InvisibleButton(label, canvasSize);
			ImGuiProbes.MarkItem(label);
			bool hovered = ImGui.IsItemHovered();
			bool activated = ImGui.IsItemActivated();
			bool active = ImGui.IsItemActive();
			bool deactivated = ImGui.IsItemDeactivated();
			ImGui.SetCursorScreenPos(cursor);

			Vector2 pointer = validImage
				? canvasState.ViewportToImage(ImGui.GetMousePos() - canvasMin, imageSize, canvasSize)
				: Vector2.Zero;

			if (activated && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				CropHandle pressed = validImage ? CropOverlayState.HitTest(crop, pointer, canvasState.Zoom, allowRotation) : CropHandle.Pan;
				state.Begin(pressed, crop, pointer);
			}

			if (active && state.Active != CropHandle.None)
			{
				changed |= Drag(state, ref crop, pointer, imageSize, canvasState, aspectRatio);
			}

			if (deactivated)
			{
				state.End();
			}

			CropHandle hot = state.Active;
			if (hovered)
			{
				if (hot == CropHandle.None && validImage)
				{
					hot = CropOverlayState.HitTest(crop, pointer, canvasState.Zoom, allowRotation);
				}

				if (validImage && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && CropOverlayState.HitTest(crop, pointer, canvasState.Zoom, allowRotation) == CropHandle.Pan)
				{
					canvasState.FitToViewport(imageSize, canvasSize);
				}

				ImageCanvasInput.ApplyWheelZoom(canvasState, canvasMin, canvasSize);
			}

			if (!validImage)
			{
				return false;
			}

			if (hovered || active)
			{
				SetCursor(hot);
			}

			if (state.Active is not (CropHandle.None or CropHandle.Pan))
			{
				ImGui.SetTooltip(string.Create(CultureInfo.InvariantCulture, $"{MathF.Round(crop.Size.X)} × {MathF.Round(crop.Size.Y)} px, {crop.AngleDegrees:0.0}°"));
			}

			Render(label, crop, imageSize, canvasState, canvasMin, canvasSize, showThirds, allowRotation, hot);
			return changed;
		}

		private static bool Drag(CropOverlayState state, ref CropRect crop, Vector2 pointer, Vector2 imageSize, ImageCanvasState canvasState, float aspectRatio)
		{
			if (state.Active == CropHandle.Pan)
			{
				// The total drag less what has already been applied, rather than MouseDelta: ImGui only
				// reports a drag once the pointer passes its threshold, and MouseDelta would drop the travel
				// that happened before then.
				if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
				{
					Vector2 total = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left);
					canvasState.PanBy(total - state.PanApplied);
					state.PanApplied = total;
				}

				return false;
			}

			CropRect next = state.Update(pointer, imageSize, aspectRatio);
			if (next == crop)
			{
				return false;
			}

			crop = next;
			return true;
		}

		private static void SetCursor(CropHandle handle)
		{
			ImGuiMouseCursor cursor = handle switch
			{
				CropHandle.TopLeft or CropHandle.BottomRight => ImGuiMouseCursor.ResizeNwse,
				CropHandle.TopRight or CropHandle.BottomLeft => ImGuiMouseCursor.ResizeNesw,
				CropHandle.Left or CropHandle.Right => ImGuiMouseCursor.ResizeEw,
				CropHandle.Top or CropHandle.Bottom => ImGuiMouseCursor.ResizeNs,
				CropHandle.Body => ImGuiMouseCursor.ResizeAll,
				CropHandle.Rotate => ImGuiMouseCursor.Hand,
				_ => ImGuiMouseCursor.Arrow,
			};

			if (cursor != ImGuiMouseCursor.Arrow)
			{
				ImGui.SetMouseCursor(cursor);
			}
		}

		private static void Render(string label, CropRect crop, Vector2 imageSize, ImageCanvasState canvasState, Vector2 canvasMin, Vector2 canvasSize, bool showThirds, bool allowRotation, CropHandle hot)
		{
			Vector2 ToScreen(Vector2 imagePoint) => canvasMin + canvasState.ImageToViewport(imagePoint, imageSize, canvasSize);

			Vector2[] corners = crop.Corners();
			Vector2[] screen = [ToScreen(corners[0]), ToScreen(corners[1]), ToScreen(corners[2]), ToScreen(corners[3])];
			(Vector2 imageMin, Vector2 imageMax) = canvasState.ImageRectInViewport(imageSize, canvasSize);
			Vector2[] image = [canvasMin + imageMin, canvasMin + new Vector2(imageMax.X, imageMin.Y), canvasMin + imageMax, canvasMin + new Vector2(imageMin.X, imageMax.Y)];

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.PushClipRect(canvasMin, canvasMin + canvasSize, true);

			uint dim = ImGui.GetColorU32(ImGuiCol.ModalWindowDimBg);
			for (int i = 0; i < 4; i++)
			{
				int next = (i + 1) % 4;
				drawList.AddQuadFilled(image[i], image[next], screen[next], screen[i], dim);
			}

			uint outline = ImGui.GetColorU32(ImGuiCol.Text);
			for (int i = 0; i < 4; i++)
			{
				drawList.AddLine(screen[i], screen[(i + 1) % 4], outline, 1.5f);
			}

			if (showThirds)
			{
				uint border = ImGui.GetColorU32(ImGuiCol.Border);
				for (int third = 1; third <= 2; third++)
				{
					float t = third / 3f;
					drawList.AddLine(Vector2.Lerp(screen[0], screen[1], t), Vector2.Lerp(screen[3], screen[2], t), border, 1f);
					drawList.AddLine(Vector2.Lerp(screen[0], screen[3], t), Vector2.Lerp(screen[1], screen[2], t), border, 1f);
				}
			}

			// The crop's own axes on screen, so the edge bars lie along their edges.
			Vector2 along = Normalized(screen[1] - screen[0], Vector2.UnitX);
			Vector2 down = Normalized(screen[3] - screen[0], Vector2.UnitY);

			CropHandle[] cornerHandles = [CropHandle.TopLeft, CropHandle.TopRight, CropHandle.BottomRight, CropHandle.BottomLeft];
			string[] cornerNames = ["topLeft", "topRight", "bottomRight", "bottomLeft"];
			for (int i = 0; i < 4; i++)
			{
				DrawBar(drawList, screen[i], along * CornerHalf, down * CornerHalf, HandleColor(hot, cornerHandles[i]));
				MarkBox(label, cornerNames[i], screen[i], new Vector2(CornerHalf, CornerHalf));
			}

			Vector2 top = (screen[0] + screen[1]) / 2f;
			Vector2 bottom = (screen[3] + screen[2]) / 2f;
			Vector2 left = (screen[0] + screen[3]) / 2f;
			Vector2 right = (screen[1] + screen[2]) / 2f;

			DrawBar(drawList, top, along * EdgeHalf.X, down * EdgeHalf.Y, HandleColor(hot, CropHandle.Top));
			DrawBar(drawList, bottom, along * EdgeHalf.X, down * EdgeHalf.Y, HandleColor(hot, CropHandle.Bottom));
			DrawBar(drawList, left, down * EdgeHalf.X, along * EdgeHalf.Y, HandleColor(hot, CropHandle.Left));
			DrawBar(drawList, right, down * EdgeHalf.X, along * EdgeHalf.Y, HandleColor(hot, CropHandle.Right));
			MarkBox(label, "top", top, EdgeHalf);
			MarkBox(label, "bottom", bottom, EdgeHalf);
			MarkBox(label, "left", left, new Vector2(EdgeHalf.Y, EdgeHalf.X));
			MarkBox(label, "right", right, new Vector2(EdgeHalf.Y, EdgeHalf.X));

			if (allowRotation)
			{
				uint rotateColor = HandleColor(hot, CropHandle.Rotate);
				Vector2 handle = top - (down * CropOverlayState.RotateHandleOffsetPixels);
				drawList.AddLine(top, handle, rotateColor, 1f);
				drawList.AddCircleFilled(handle, RotateRadius, rotateColor);
				MarkBox(label, "rotate", handle, new Vector2(RotateRadius, RotateRadius));
			}

			drawList.PopClipRect();

			Vector2 bodyMin = Vector2.Min(Vector2.Min(screen[0], screen[1]), Vector2.Min(screen[2], screen[3]));
			Vector2 bodyMax = Vector2.Max(Vector2.Max(screen[0], screen[1]), Vector2.Max(screen[2], screen[3]));
			ImGuiProbes.MarkRegion($"{label}/body", bodyMin, bodyMax);
		}

		private static void DrawBar(ImDrawListPtr drawList, Vector2 centre, Vector2 halfAlong, Vector2 halfAcross, uint color) =>
			drawList.AddQuadFilled(
				centre - halfAlong - halfAcross,
				centre + halfAlong - halfAcross,
				centre + halfAlong + halfAcross,
				centre - halfAlong + halfAcross,
				color);

		private static void MarkBox(string label, string handle, Vector2 centre, Vector2 half)
		{
			Vector2 grown = half + new Vector2(CropOverlayState.HandleGrabPixels);
			ImGuiProbes.MarkRegion($"{label}/{handle}", centre - grown, centre + grown);
		}

		private static uint HandleColor(CropHandle hot, CropHandle handle) =>
			ImGui.GetColorU32(hot == handle ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab);

		private static Vector2 Normalized(Vector2 vector, Vector2 fallback)
		{
			float length = vector.Length();
			return length > 1e-6f ? vector / length : fallback;
		}
	}
}

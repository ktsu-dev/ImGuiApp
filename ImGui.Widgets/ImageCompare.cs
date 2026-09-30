// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

public static partial class ImGuiWidgets
{
	/// <summary>How <see cref="ImageCompare"/> lays out its two images.</summary>
	public enum ImageCompareMode
	{
		/// <summary>One view, split by a draggable vertical divider: before on the left, after on the right.</summary>
		Wipe,

		/// <summary>Two panes side by side, 4 px apart, panning and zooming together.</summary>
		SideBySide,
	}

	/// <summary>Draws two same-sized textures in one pannable, zoomable view, split by a divider or side by side.</summary>
	/// <param name="id">Unique widget id; pushed as an ID scope and used as the probe prefix.</param>
	/// <param name="beforeTextureId">Texture shown left of the divider (or in the left pane).</param>
	/// <param name="afterTextureId">Texture shown right of the divider (or in the right pane).</param>
	/// <param name="imageSize">Native image size in pixels; both textures are drawn at this size.</param>
	/// <param name="state">Shared view state, mutated by pan, zoom and double-click fit. Owned by the caller.</param>
	/// <param name="split">Divider position as a fraction of the canvas width, 0..1. Owned by the caller; NaN becomes 0.5.</param>
	/// <param name="canvasSize">Size of the whole widget in screen pixels; each component at least 1.</param>
	/// <param name="mode">Wipe (default) or side by side.</param>
	/// <returns>True if <paramref name="split"/> changed this frame. Pan and zoom changes are read from <paramref name="state"/>.</returns>
	/// <remarks>
	/// The whole widget is one invisible button, and the divider is hit-tested inside it when a press
	/// starts: within 6 px of the divider the press drags it, anywhere else it pans. A divider laid over
	/// the canvas as a separate item would never be pressed, because ImGui gives hover to the first item
	/// submitted over a point. Both images are placed by the one <paramref name="state"/>, so they line up
	/// to the pixel, and a caller may share that state with an <see cref="ImageCanvas"/> elsewhere.
	/// </remarks>
	public static bool ImageCompare(string id, nint beforeTextureId, nint afterTextureId, Vector2 imageSize, ImageCanvasState state, ref float split, Vector2 canvasSize, ImageCompareMode mode = ImageCompareMode.Wipe) =>
		ImageCompareImpl.Draw(id, beforeTextureId, afterTextureId, imageSize, state, ref split, canvasSize, mode);

	internal static class ImageCompareImpl
	{
		private const string BeforeLabel = "Before";
		private const string AfterLabel = "After";
		private const float LabelInset = 6f;
		private const float LabelPadding = 3f;
		private const float DividerThickness = 2f;
		private static readonly Vector2 GripSize = new(8f, 24f);

		private static readonly Dictionary<uint, ImageCompareState> States = [];

		public static bool Draw(string id, nint beforeTextureId, nint afterTextureId, Vector2 imageSize, ImageCanvasState state, ref float split, Vector2 canvasSize, ImageCompareMode mode)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(state);

			uint key = ImGui.GetID(id);
			if (!States.TryGetValue(key, out ImageCompareState? compare))
			{
				compare = new ImageCompareState();
				States[key] = compare;
			}

			using ScopedId scope = new(id);

			Vector2 origin = ImGui.GetCursorScreenPos();
			Vector2 size = Vector2.Max(canvasSize, Vector2.One);

			ImGui.InvisibleButton("canvas", size);
			ImGuiProbes.MarkItem("canvas");

			float normalized = ImageCompareState.NormalizeSplit(split);
			bool changed = FloatBits.Differ(normalized, split);
			split = normalized;

			ImGuiIOPtr io = ImGui.GetIO();
			Vector2 local = io.MousePos - origin;
			bool hovered = ImGui.IsItemHovered();
			(Vector2 leftOrigin, Vector2 rightOrigin, Vector2 paneSize) = ImageCompareState.SideBySideLayout(size);

			if (ImGui.IsItemActivated())
			{
				compare.Press(local.X, split * size.X, mode);
			}

			if (ImGui.IsItemActive())
			{
				if (compare.Target == ImageCompareState.DragTarget.Divider)
				{
					float next = ImageCompareState.SplitFromPointer(local.X, size.X);
					if (FloatBits.Differ(next, split))
					{
						split = next;
						changed = true;
					}
				}
				else if (compare.Target == ImageCompareState.DragTarget.Pan && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
				{
					state.PanBy(io.MouseDelta);
				}
			}

			bool draggingDivider = compare.Target == ImageCompareState.DragTarget.Divider;

			if (ImGui.IsItemDeactivated())
			{
				compare.Release();
			}

			float dividerX = split * size.X;
			bool overDivider = mode == ImageCompareMode.Wipe && hovered && ImageCompareState.HitsDivider(local.X, dividerX);

			if (hovered)
			{
				Vector2 fitSize = mode == ImageCompareMode.Wipe ? size : paneSize;
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && !overDivider)
				{
					state.FitToViewport(imageSize, fitSize);
				}

				Vector2 paneOrigin = origin;
				if (mode == ImageCompareMode.SideBySide)
				{
					paneOrigin += local.X >= rightOrigin.X ? rightOrigin : leftOrigin;
				}

				ImageCanvasInput.ApplyWheelZoom(state, paneOrigin, fitSize);
			}

			if (overDivider || draggingDivider)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			if (mode == ImageCompareMode.Wipe)
			{
				DrawWipe(drawList, origin, size, beforeTextureId, afterTextureId, imageSize, state, dividerX, overDivider || draggingDivider);
			}
			else
			{
				DrawPane(drawList, origin + leftOrigin, paneSize, beforeTextureId, imageSize, state, BeforeLabel, "before");
				DrawPane(drawList, origin + rightOrigin, paneSize, afterTextureId, imageSize, state, AfterLabel, "after");
			}

			return changed;
		}

		private static void DrawWipe(ImDrawListPtr drawList, Vector2 origin, Vector2 size, nint before, nint after, Vector2 imageSize, ImageCanvasState state, float dividerX, bool dividerActive)
		{
			drawList.PushClipRect(origin, origin + size, true);
			DrawCheckerboard(drawList, origin, size);

			Vector2 dividerTop = origin + new Vector2(dividerX, 0f);
			Vector2 dividerBottom = origin + new Vector2(dividerX, size.Y);

			if (imageSize.X > 0f && imageSize.Y > 0f)
			{
				(Vector2 min, Vector2 max) = state.ImageRectInViewport(imageSize, size);

				drawList.PushClipRect(origin, dividerBottom, true);
				AddImage(drawList, before, origin + min, origin + max);
				drawList.PopClipRect();

				drawList.PushClipRect(dividerTop, origin + size, true);
				AddImage(drawList, after, origin + min, origin + max);
				drawList.PopClipRect();
			}

			uint grab = ImGui.GetColorU32(dividerActive ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab);
			drawList.AddLine(dividerTop, dividerBottom, grab, DividerThickness);
			Vector2 gripCentre = origin + new Vector2(dividerX, size.Y / 2f);
			drawList.AddRectFilled(gripCentre - (GripSize / 2f), gripCentre + (GripSize / 2f), grab, 2f);

			DrawLabel(drawList, origin + new Vector2(LabelInset, LabelInset), BeforeLabel);
			Vector2 afterSize = ImGui.CalcTextSize(AfterLabel);
			DrawLabel(drawList, origin + new Vector2(size.X - LabelInset - afterSize.X, LabelInset), AfterLabel);

			drawList.PopClipRect();

			ImGuiProbes.MarkRegion("before", origin, dividerBottom);
			ImGuiProbes.MarkRegion("after", dividerTop, origin + size);
			ImGuiProbes.MarkRegion(
				"divider",
				origin + new Vector2(dividerX - ImageCompareState.DividerGrabRadius, 0f),
				origin + new Vector2(dividerX + ImageCompareState.DividerGrabRadius, size.Y));
		}

		private static void DrawPane(ImDrawListPtr drawList, Vector2 paneOrigin, Vector2 paneSize, nint texture, Vector2 imageSize, ImageCanvasState state, string label, string probeName)
		{
			drawList.PushClipRect(paneOrigin, paneOrigin + paneSize, true);
			DrawCheckerboard(drawList, paneOrigin, paneSize);

			if (imageSize.X > 0f && imageSize.Y > 0f)
			{
				(Vector2 min, Vector2 max) = state.ImageRectInViewport(imageSize, paneSize);
				AddImage(drawList, texture, paneOrigin + min, paneOrigin + max);
			}

			DrawLabel(drawList, paneOrigin + new Vector2(LabelInset, LabelInset), label);
			drawList.PopClipRect();

			ImGuiProbes.MarkRegion(probeName, paneOrigin, paneOrigin + paneSize);
		}

		private static void DrawLabel(ImDrawListPtr drawList, Vector2 textMin, string text)
		{
			Vector2 padding = new(LabelPadding, LabelPadding);
			Vector2 textSize = ImGui.CalcTextSize(text);
			drawList.AddRectFilled(textMin - padding, textMin + textSize + padding, ImGui.GetColorU32(ImGuiCol.FrameBg));
			drawList.AddText(textMin, ImGui.GetColorU32(ImGuiCol.Text), text);
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; pointer is scoped to the call and not retained.")]
		private static void AddImage(ImDrawListPtr drawList, nint texture, Vector2 min, Vector2 max)
		{
			unsafe
			{
				drawList.AddImage(new ImTextureRef(texId: texture), min, max);
			}
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws draggable handles over a rectangle the caller supplies, for example one just occupied
	/// by a histogram plot. The handles stay ordered and at least <paramref name="minGap"/> apart.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="handles">The handle positions, updated in place. Kept sorted ascending.</param>
	/// <param name="rectMin">The top-left of the rectangle to overlay.</param>
	/// <param name="rectMax">The bottom-right of the rectangle to overlay.</param>
	/// <param name="lowerBound">The value at the left edge.</param>
	/// <param name="upperBound">The value at the right edge.</param>
	/// <param name="minGap">The minimum distance kept between neighboring handles.</param>
	/// <param name="handleRadius">The grab radius in pixels. Non-positive derives it from the rectangle height.</param>
	/// <param name="handleCenterY">The vertical center of the handles. <see cref="float.NaN"/> uses the rectangle's center.</param>
	/// <returns><see langword="true"/> if a handle moved this frame; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// The widget places an invisible button over the rectangle so activation and dragging work
	/// through ImGui's normal item mechanics, then restores the cursor so the caller's layout is
	/// undisturbed. It draws handles and nothing else — no track, no fill — because it is designed
	/// to sit on top of content it does not own, for example a <see cref="Histogram"/> already drawn
	/// into the same rectangle. An empty <paramref name="handles"/> returns immediately, before the
	/// invisible button is submitted and before the probe is marked, so a call with no handles
	/// consumes no layout and registers no probe — the opposite policy to <see cref="Histogram"/>,
	/// which always reserves layout and draws its frame.
	/// </remarks>
	public static bool HandleTrack(
		string label,
		Span<float> handles,
		Vector2 rectMin,
		Vector2 rectMax,
		float lowerBound,
		float upperBound,
		float minGap = 0.0f,
		float handleRadius = 0.0f,
		float handleCenterY = float.NaN) =>
		HandleTrackImpl.Draw(label, handles, rectMin, rectMax, lowerBound, upperBound, minGap, handleRadius, handleCenterY);

	internal static class HandleTrackImpl
	{
		private static readonly Dictionary<uint, HandleTrackState> States = [];

		public static bool Draw(
			string label,
			Span<float> handles,
			Vector2 rectMin,
			Vector2 rectMax,
			float lowerBound,
			float upperBound,
			float minGap,
			float handleRadius,
			float handleCenterY)
		{
			if (handles.Length == 0)
			{
				return false;
			}

			if (lowerBound > upperBound)
			{
				(lowerBound, upperBound) = (upperBound, lowerBound);
			}

			float height = MathF.Max(rectMax.Y - rectMin.Y, 1.0f);
			float radius = handleRadius > 0.0f ? handleRadius : height * 0.35f;
			float centerY = float.IsNaN(handleCenterY) ? rectMin.Y + (height * 0.5f) : handleCenterY;

			float trackMinX = rectMin.X + radius;
			float trackMaxX = rectMax.X - radius;
			float span = MathF.Max(trackMaxX - trackMinX, 1.0f);

			HandleTrackState.Normalize(handles, lowerBound, upperBound, minGap);

			// Overlay an item on the caller's rectangle, then put the cursor back where it was so
			// this widget does not consume layout of its own.
			Vector2 cursor = ImGui.GetCursorScreenPos();
			ImGui.SetCursorScreenPos(rectMin);
			ImGui.InvisibleButton(label, new Vector2(MathF.Max(rectMax.X - rectMin.X, 1.0f), height));
			ImGuiProbes.MarkItem(label);
			ImGui.SetCursorScreenPos(cursor);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out HandleTrackState? state))
			{
				state = new HandleTrackState();
				States[id] = state;
			}

			float mouseValue = lowerBound
				+ (Math.Clamp((ImGui.GetIO().MousePos.X - trackMinX) / span, 0.0f, 1.0f) * (upperBound - lowerBound));

			if (ImGui.IsItemActivated())
			{
				state.Activate(handles, mouseValue);
			}

			bool changed = false;
			if (ImGui.IsItemActive())
			{
				changed = state.Drag(handles, mouseValue, lowerBound, upperBound, minGap);
			}
			else
			{
				state.Release();
			}

			DrawHandles(handles, lowerBound, upperBound, trackMinX, span, centerY, radius);

			return changed;
		}

		private static void DrawHandles(
			ReadOnlySpan<float> handles,
			float lowerBound,
			float upperBound,
			float trackMinX,
			float span,
			float centerY,
			float radius)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			bool hovered = ImGui.IsItemHovered() || ImGui.IsItemActive();
			uint grabColor = ImGui.GetColorU32(hovered ? colors[(int)ImGuiCol.SliderGrabActive] : colors[(int)ImGuiCol.SliderGrab]);

			foreach (float handle in handles)
			{
				float x = trackMinX + (ValueToFraction(handle, lowerBound, upperBound) * span);
				drawList.AddCircleFilled(new Vector2(x, centerY), radius, grabColor, 24);
			}
		}

		private static float ValueToFraction(float value, float min, float max) =>
			max <= min ? 0.0f : Math.Clamp((value - min) / (max - min), 0.0f, 1.0f);
	}
}

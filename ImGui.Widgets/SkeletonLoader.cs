// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a shimmering placeholder used while real content is loading. A bright band sweeps
	/// horizontally across a muted base fill, giving the familiar "skeleton" loading effect. The
	/// animation is driven by <see cref="ImGui.GetTime"/> so it is frame-rate independent and needs
	/// no per-widget state.
	/// </summary>
	public static void SkeletonLine(string id, float width = 0f, float height = 0f)
	{
		float resolvedHeight = height > 0f ? height : ImGui.GetTextLineHeight();
		float resolvedWidth = width > 0f ? width : ImGui.GetContentRegionAvail().X;
		SkeletonImpl.Draw(id, new Vector2(resolvedWidth, resolvedHeight), resolvedHeight * 0.35f);
	}

	/// <summary>
	/// Draws a rectangular shimmering placeholder of the given size (e.g. an image or thumbnail slot).
	/// </summary>
	public static void SkeletonRect(string id, Vector2 size, float rounding = -1f)
	{
		float resolvedRounding = rounding >= 0f ? rounding : MathF.Max(ImGui.GetStyle().FrameRounding, 4.0f);
		SkeletonImpl.Draw(id, size, resolvedRounding);
	}

	/// <summary>
	/// Draws a circular shimmering placeholder (e.g. an avatar slot).
	/// </summary>
	public static void SkeletonCircle(string id, float diameter = 0f)
	{
		float resolved = diameter > 0f ? diameter : ImGui.GetFrameHeight() * 2.0f;
		SkeletonImpl.Draw(id, new Vector2(resolved, resolved), resolved * 0.5f);
	}

	internal static class SkeletonImpl
	{
		// Seconds for the shimmer band to travel once across the placeholder.
		private const float SweepPeriod = 1.4f;

		// Width of the moving highlight band as a fraction of the placeholder width.
		private const float BandFraction = 0.4f;

		/// <summary>
		/// Normalized sweep phase in <c>[0, 1)</c> for the given absolute <paramref name="time"/> and
		/// <paramref name="period"/>. Pure helper so the sweep can be unit tested without a GL context.
		/// </summary>
		internal static float ShimmerPhase(double time, float period)
		{
			if (period <= 0.0f)
			{
				return 0.0f;
			}

			double cycles = time / period;
			return (float)(cycles - Math.Floor(cycles));
		}

		public static void Draw(string id, Vector2 size, float rounding)
		{
			if (size.X <= 0.0f || size.Y <= 0.0f)
			{
				return;
			}

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(size);

			Vector2 min = origin;
			Vector2 max = origin + size;

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			// Muted base and a lighter highlight, both pulled from the active theme.
			ImColor baseColor = new() { Value = colors[(int)ImGuiCol.FrameBg] };
			ImColor highlight = new() { Value = colors[(int)ImGuiCol.FrameBgHovered] };

			drawList.AddRectFilled(min, max, baseColor.ToImGuiU32(), rounding);

			// Clip the moving band to the placeholder bounds so it never bleeds past the edges.
			drawList.PushClipRect(min, max, true);

			float bandWidth = MathF.Max(size.X * BandFraction, 8.0f);
			float travel = size.X + bandWidth;

			// Offset each placeholder's sweep by a stable amount derived from its id so a group of
			// skeletons shimmers in a staggered wave rather than in lockstep.
			double offsetSeconds = ImGui.GetID(id) % 1000u / 1000.0 * SweepPeriod;
			float phase = ShimmerPhase(ImGui.GetTime() + offsetSeconds, SweepPeriod);
			float center = min.X - (bandWidth * 0.5f) + (phase * travel);

			float left = center - (bandWidth * 0.5f);
			float right = center + (bandWidth * 0.5f);

			uint edge = highlight.WithAlpha(0f).ToImGuiU32();
			uint core = highlight.ToImGuiU32();

			// Two halves form a transparent -> bright -> transparent horizontal gradient.
			drawList.AddRectFilledMultiColor(new Vector2(left, min.Y), new Vector2(center, max.Y), edge, core, core, edge);
			drawList.AddRectFilledMultiColor(new Vector2(center, min.Y), new Vector2(right, max.Y), core, edge, edge, core);

			drawList.PopClipRect();
		}
	}
}

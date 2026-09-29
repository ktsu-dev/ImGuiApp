// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

			// The band is the placeholder's own shape cut down to the band's width, not a rectangle
			// clipped to its bounding box: a clip rect is square, so a band crossing a circle or a
			// rounded corner used to paint the corners outside the shape as a vertical bar.
			DrawBand(drawList, min, max, rounding, left, center, right, highlight.Value);
			drawList.PopClipRect();
		}

		/// <summary>
		/// Fills the part of the rounded rectangle between <paramref name="left"/> and
		/// <paramref name="right"/> with <paramref name="color"/>, transparent at both ends and
		/// fully bright at <paramref name="center"/>.
		/// </summary>
		/// <remarks>
		/// The fill goes through ImGui's anti-aliased convex fill and is recoloured afterwards, one
		/// vertex at a time, by where each vertex sits in the band. Barycentric interpolation only
		/// reproduces a linear ramp, and the band is two, so the outline carries a vertex wherever
		/// it crosses the centre and starts from the top one: ImGui fans the fill out from the
		/// first vertex, which then puts the centre line along a triangle edge and every triangle
		/// on one side of it.
		/// </remarks>
		[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Drawing primitive parameterized by geometry; grouping would obscure the single call site.")]
		private static void DrawBand(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float left, float center, float right, Vector4 color)
		{
			List<Vector2> band = BandOutline(min, max, rounding, left, center, right);
			if (band.Count < 3)
			{
				return;
			}

			int first = drawList.VtxBuffer.Size;
			foreach (Vector2 point in band)
			{
				drawList.PathLineTo(point);
			}

			drawList.PathFillConvex(uint.MaxValue);

			// ImVector is a view over native memory, so a copy of it writes through to the list.
			ImVector<ImDrawVert> vertices = drawList.VtxBuffer;
			for (int i = first; i < vertices.Size; i++)
			{
				ImDrawVert vertex = vertices[i];

				// The anti-aliasing fringe arrives transparent; keep that and scale the band's
				// strength into what is left.
				float coverage = (vertex.Col >> 24) / 255f;
				float strength = BandStrength(vertex.Pos.X, left, center, right);
				vertex.Col = ImGui.GetColorU32(color with { W = color.W * strength * coverage });
				vertices[i] = vertex;
			}
		}

		/// <summary>How bright the band is at <paramref name="x"/>: zero at either end, one at the centre.</summary>
		/// <param name="x">Horizontal position.</param>
		/// <param name="left">Where the band starts.</param>
		/// <param name="center">Where the band is brightest.</param>
		/// <param name="right">Where the band ends.</param>
		/// <returns>The band's strength, from 0 to 1.</returns>
		internal static float BandStrength(float x, float left, float center, float right)
		{
			float t = x <= center
				? (x - left) / MathF.Max(center - left, 1e-6f)
				: (right - x) / MathF.Max(right - center, 1e-6f);
			return Math.Clamp(t, 0f, 1f);
		}

		/// <summary>
		/// The outline of a rounded rectangle cut to the band between <paramref name="left"/> and
		/// <paramref name="right"/>, clockwise, with a vertex wherever it crosses
		/// <paramref name="center"/> and starting from the topmost of those when there is one.
		/// </summary>
		/// <param name="min">Top-left of the rectangle.</param>
		/// <param name="max">Bottom-right of the rectangle.</param>
		/// <param name="rounding">Corner radius, clamped to half the shorter side as ImGui does.</param>
		/// <param name="left">Left edge of the band.</param>
		/// <param name="center">Centre of the band.</param>
		/// <param name="right">Right edge of the band.</param>
		/// <returns>The outline, or fewer than three points when the band misses the shape.</returns>
		internal static List<Vector2> BandOutline(Vector2 min, Vector2 max, float rounding, float left, float center, float right)
		{
			List<Vector2> outline = RoundedOutline(min, max, rounding);
			outline = ClipToHalfPlane(outline, left, keepRight: true);
			outline = ClipToHalfPlane(outline, right, keepRight: false);
			outline = WithoutRepeats(SplitAt(outline, center));

			int start = -1;
			for (int i = 0; i < outline.Count; i++)
			{
				if (MathF.Abs(outline[i].X - center) <= 1e-4f && (start < 0 || outline[i].Y < outline[start].Y))
				{
					start = i;
				}
			}

			return start <= 0 ? outline : [.. outline.Skip(start), .. outline.Take(start)];
		}

		private static List<Vector2> RoundedOutline(Vector2 min, Vector2 max, float rounding)
		{
			Vector2 size = max - min;
			float radius = Math.Clamp(rounding, 0f, MathF.Min(size.X, size.Y) * 0.5f);
			List<Vector2> points = [];

			// Clockwise on screen from the top-left corner, each corner a quarter turn.
			(Vector2 Center, float StartAngle)[] corners =
			[
				(new Vector2(min.X + radius, min.Y + radius), MathF.PI),
				(new Vector2(max.X - radius, min.Y + radius), MathF.PI * 1.5f),
				(new Vector2(max.X - radius, max.Y - radius), 0f),
				(new Vector2(min.X + radius, max.Y - radius), MathF.PI * 0.5f),
			];

			int steps = radius <= 0f ? 0 : Math.Max(2, (int)MathF.Ceiling(radius * 0.5f));
			foreach ((Vector2 cornerCenter, float startAngle) in corners)
			{
				for (int i = 0; i <= steps; i++)
				{
					float angle = startAngle + (MathF.PI * 0.5f * i / Math.Max(steps, 1));
					points.Add(cornerCenter + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius));
					if (steps == 0)
					{
						break;
					}
				}
			}

			return points;
		}

		/// <summary>Sutherland-Hodgman against one vertical line.</summary>
		private static List<Vector2> ClipToHalfPlane(List<Vector2> polygon, float x, bool keepRight)
		{
			List<Vector2> result = [];
			for (int i = 0; i < polygon.Count; i++)
			{
				Vector2 current = polygon[i];
				Vector2 next = polygon[(i + 1) % polygon.Count];
				bool currentInside = keepRight ? current.X >= x : current.X <= x;
				bool nextInside = keepRight ? next.X >= x : next.X <= x;

				if (currentInside)
				{
					result.Add(current);
				}

				if (currentInside != nextInside)
				{
					result.Add(CrossingAt(current, next, x));
				}
			}

			return result;
		}

		private static List<Vector2> SplitAt(List<Vector2> polygon, float x)
		{
			List<Vector2> result = [];
			for (int i = 0; i < polygon.Count; i++)
			{
				Vector2 current = polygon[i];
				Vector2 next = polygon[(i + 1) % polygon.Count];
				result.Add(current);

				if ((current.X < x && next.X > x) || (current.X > x && next.X < x))
				{
					result.Add(CrossingAt(current, next, x));
				}
			}

			return result;
		}

		/// <summary>
		/// Drops points that repeat their predecessor. Arcs meet the straight sides at shared points,
		/// and a zero-length edge gives ImGui's fill no normal to build its fringe from.
		/// </summary>
		private static List<Vector2> WithoutRepeats(List<Vector2> polygon)
		{
			List<Vector2> result = [];
			for (int i = 0; i < polygon.Count; i++)
			{
				if (result.Count == 0 || Vector2.DistanceSquared(result[^1], polygon[i]) > 1e-6f)
				{
					result.Add(polygon[i]);
				}
			}

			while (result.Count > 1 && Vector2.DistanceSquared(result[0], result[^1]) <= 1e-6f)
			{
				result.RemoveAt(result.Count - 1);
			}

			return result;
		}

		private static Vector2 CrossingAt(Vector2 from, Vector2 to, float x)
		{
			float t = (x - from.X) / (to.X - from.X);
			return new Vector2(x, from.Y + ((to.Y - from.Y) * t));
		}
	}
}

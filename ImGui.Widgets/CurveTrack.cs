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
	/// Draws an editable curve: a plot of a function the caller supplies, with draggable control
	/// points over it.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="points">The control points, updated in place and kept ordered by x.</param>
	/// <param name="sample">
	/// What the curve does. Called across the rectangle to plot it, so it must be cheap and must not
	/// mutate anything.
	/// </param>
	/// <param name="rectMin">The top-left of the rectangle to draw into.</param>
	/// <param name="rectMax">The bottom-right of the rectangle to draw into.</param>
	/// <param name="lowerBound">The value at the bottom-left corner.</param>
	/// <param name="upperBound">The value at the top-right corner.</param>
	/// <param name="pinEnds">Whether the first and last points are held at the edges of the domain.</param>
	/// <param name="minGap">The narrowest horizontal gap kept between neighboring points.</param>
	/// <param name="grabRadius">The grab radius in pixels. Non-positive derives one from the box.</param>
	/// <returns><see langword="true"/> if a point moved, was added or was removed this frame.</returns>
	/// <remarks>
	/// <para>
	/// <b>The caller owns the interpolation.</b> This widget plots <paramref name="sample"/> and
	/// knows nothing else about the shape between the points — which is the whole point of it. A
	/// curve editor is the one control whose drawing is its specification: a curve drawn differently
	/// from the one being applied is a lie about what the user is editing. Taking the function in
	/// makes the drawn curve and the applied curve the same function by construction, rather than
	/// two implementations that agree today.
	/// </para>
	/// <para>
	/// The gestures are: press near a point to drag it, press away from every point to add one there
	/// and drag it straight away, and right-click a point to remove it. Adding on a plain press
	/// rather than on a double-click is deliberate — a double-click has to be disambiguated from the
	/// press that starts a drag, and the widget that does it that way upstream throws when the
	/// gesture lands past its last point.
	/// </para>
	/// <para>
	/// Like <see cref="HandleTrack"/>, this overlays a rectangle the caller owns and restores the
	/// cursor afterwards, so it consumes no layout of its own and paints no background. That is what
	/// lets it sit on top of content it does not own — a <see cref="Histogram"/> drawn into the same
	/// rectangle, which is the arrangement a tone curve is normally edited in. A caller with nothing
	/// underneath can reserve the box and draw the empty frame with a degenerate
	/// <see cref="Histogram"/> call, which is defined to do exactly that.
	/// </para>
	/// <para>
	/// An empty <paramref name="points"/> is drawn empty rather than refused, and can be populated by
	/// pressing on the track. Unlike the vendor editors this one holds no state an empty curve can
	/// corrupt, and its add gesture does not index a point before checking there is one.
	/// </para>
	/// </remarks>
	public static bool CurveTrack(
		string label,
		IList<Vector2> points,
		Func<float, float> sample,
		Vector2 rectMin,
		Vector2 rectMax,
		Vector2 lowerBound,
		Vector2 upperBound,
		bool pinEnds = true,
		float minGap = 0.0f,
		float grabRadius = 0.0f) =>
		CurveTrackImpl.Draw(label, points, sample, rectMin, rectMax, lowerBound, upperBound, pinEnds, minGap, grabRadius);

	internal static class CurveTrackImpl
	{
		private static readonly Dictionary<uint, CurveTrackState> States = [];

		/// <summary>How far apart the plotted samples are, in pixels.</summary>
		/// <remarks>
		/// One sample per pixel column would be the obvious choice and is wasted: the polyline is
		/// drawn with straight segments between samples, and at this spacing the error against the
		/// real curve is well under a pixel for anything smooth enough to be a tone curve. It also
		/// bounds how often the caller's delegate is invoked per frame, which at one per column
		/// would scale with the window.
		/// </remarks>
		private const float SampleSpacing = 2.0f;

		public static bool Draw(
			string label,
			IList<Vector2> points,
			Func<float, float> sample,
			Vector2 rectMin,
			Vector2 rectMax,
			Vector2 lowerBound,
			Vector2 upperBound,
			bool pinEnds,
			float minGap,
			float grabRadius)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(points);
			Ensure.NotNull(sample);

			Vector2 min = rectMin;
			Vector2 max = new(MathF.Max(rectMax.X, rectMin.X + 1.0f), MathF.Max(rectMax.Y, rectMin.Y + 1.0f));

			// Half a line, floored at six pixels. The same radius draws the points and catches the
			// press, so it is also how big they look — and a point small enough to be hard to hit is
			// small enough to be hard to see.
			float lineHeight = ImGui.GetTextLineHeight();
			float radius = grabRadius > 0.0f ? grabRadius : MathF.Max(lineHeight * 0.5f, 6.0f);

			// Overlay an item on the caller's rectangle, then put the cursor back where it was so
			// this widget consumes no layout of its own.
			Vector2 cursor = ImGui.GetCursorScreenPos();
			ImGui.SetCursorScreenPos(min);
			ImGui.InvisibleButton(label, max - min);
			ImGuiProbes.MarkItem(label);
			ImGui.SetCursorScreenPos(cursor);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			CurveTrackState.Normalize(points, lowerBound, upperBound, minGap, pinEnds);

			DrawGrid(drawList, min, max, colors);
			PlotCurve(drawList, sample, min, max, lowerBound, upperBound, colors);

			bool changed = Interact(label, points, min, max, lowerBound, upperBound, pinEnds, minGap, radius);

			DrawPoints(drawList, points, min, max, lowerBound, upperBound, radius, colors);

			return changed;
		}

		/// <summary>Maps a value-space point to where it is drawn.</summary>
		/// <remarks>
		/// y is inverted because the value axis runs upward and the screen's runs down. Every
		/// mapping in this file goes through here or its inverse, so the inversion is stated once.
		/// </remarks>
		internal static Vector2 ToScreen(Vector2 value, Vector2 min, Vector2 max, Vector2 lowerBound, Vector2 upperBound)
		{
			float x = Remap(value.X, lowerBound.X, upperBound.X, min.X, max.X);
			float y = Remap(value.Y, lowerBound.Y, upperBound.Y, max.Y, min.Y);
			return new Vector2(x, y);
		}

		/// <summary>Maps a drawn position back to value space.</summary>
		internal static Vector2 ToValue(Vector2 screen, Vector2 min, Vector2 max, Vector2 lowerBound, Vector2 upperBound)
		{
			float x = Remap(screen.X, min.X, max.X, lowerBound.X, upperBound.X);
			float y = Remap(screen.Y, max.Y, min.Y, lowerBound.Y, upperBound.Y);
			return new Vector2(x, y);
		}

		private static float Remap(float value, float fromLow, float fromHigh, float toLow, float toHigh)
		{
			float span = fromHigh - fromLow;

			// A collapsed span has no position to report. The low end is the only answer that is not
			// an infinity, and it keeps a degenerate box drawing rather than scattering NaN through
			// the draw list.
			//
			// Compared against float.Epsilon rather than to zero: that is the smallest denormal, so
			// this is "exactly zero" plus the handful of values a subtraction can leave that divide
			// into something absurd rather than something useful. It is not a tolerance, and it is
			// not meant as one.
			return MathF.Abs(span) <= float.Epsilon ? toLow : toLow + ((value - fromLow) / span * (toHigh - toLow));
		}

		private static void DrawGrid(ImDrawListPtr drawList, Vector2 min, Vector2 max, ReadOnlySpan<Vector4> colors)
		{
			uint grid = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

			// Quarters, and the diagonal the identity curve lies along. The diagonal is what makes
			// "this curve does nothing" readable at a glance, which is the state a tone curve spends
			// most of its life near.
			for (int i = 1; i < 4; i++)
			{
				float t = i / 4.0f;
				float x = min.X + ((max.X - min.X) * t);
				float y = min.Y + ((max.Y - min.Y) * t);
				drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), grid);
				drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), grid);
			}

			drawList.AddLine(new Vector2(min.X, max.Y), new Vector2(max.X, min.Y), grid);
		}

		private static void PlotCurve(
			ImDrawListPtr drawList,
			Func<float, float> sample,
			Vector2 min,
			Vector2 max,
			Vector2 lowerBound,
			Vector2 upperBound,
			ReadOnlySpan<Vector4> colors)
		{
			float width = max.X - min.X;
			int steps = Math.Max(2, (int)(width / SampleSpacing));
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);

			Vector2 previous = Vector2.Zero;
			bool havePrevious = false;

			for (int i = 0; i <= steps; i++)
			{
				float t = i / (float)steps;
				float x = lowerBound.X + ((upperBound.X - lowerBound.X) * t);
				float y = sample(x);

				if (!float.IsFinite(y))
				{
					// A sampler that returns NaN or an infinity would otherwise put a segment
					// through the whole draw list. Break the line rather than drawing nonsense.
					havePrevious = false;
					continue;
				}

				Vector2 point = ToScreen(new Vector2(x, y), min, max, lowerBound, upperBound);
				point.Y = Math.Clamp(point.Y, min.Y, max.Y);

				if (havePrevious)
				{
					drawList.AddLine(previous, point, color, 2.0f);
				}

				previous = point;
				havePrevious = true;
			}
		}

		private static void DrawPoints(
			ImDrawListPtr drawList,
			IList<Vector2> points,
			Vector2 min,
			Vector2 max,
			Vector2 lowerBound,
			Vector2 upperBound,
			float radius,
			ReadOnlySpan<Vector4> colors)
		{
			bool hot = ImGui.IsItemHovered() || ImGui.IsItemActive();
			uint fill = ImGui.GetColorU32(hot ? colors[(int)ImGuiCol.SliderGrabActive] : colors[(int)ImGuiCol.SliderGrab]);

			foreach (Vector2 point in points)
			{
				drawList.AddCircleFilled(ToScreen(point, min, max, lowerBound, upperBound), radius, fill, 16);
			}
		}

		private static bool Interact(
			string label,
			IList<Vector2> points,
			Vector2 min,
			Vector2 max,
			Vector2 lowerBound,
			Vector2 upperBound,
			bool pinEnds,
			float minGap,
			float radius)
		{
			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out CurveTrackState? state))
			{
				state = new CurveTrackState();
				States[id] = state;
			}

			Vector2 pointer = ToValue(ImGui.GetIO().MousePos, min, max, lowerBound, upperBound);

			// The grab radius is given in pixels but compared in value space, so it has to cross the
			// same mapping the points do. Taking it from the x axis alone would make a tall, narrow
			// box grab along one axis and not the other.
			Vector2 radiusInValue = ToValue(min + new Vector2(radius, radius), min, max, lowerBound, upperBound)
				- ToValue(min, min, max, lowerBound, upperBound);
			float grabInValue = MathF.Max(MathF.Abs(radiusInValue.X), MathF.Abs(radiusInValue.Y));

			bool changed = false;

			// Activate has the side effect of setting the active point, and short-circuiting is what
			// keeps it from running on a frame the item was not activated on.
			if (ImGui.IsItemActivated() && !state.Activate(points, pointer, grabInValue))
			{
				// Empty track: add a point where the press landed and drag it from here, so the
				// gesture that creates a point is the same one that positions it.
				int added = CurveTrackState.Add(points, pointer, lowerBound, upperBound, minGap);
				if (added >= 0)
				{
					state.Grab(added);
					changed = true;
				}
			}

			if (ImGui.IsItemActive())
			{
				changed |= state.Drag(points, pointer, lowerBound, upperBound, minGap, pinEnds);
			}
			else
			{
				state.Release();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				CurveTrackState removing = new();
				if (removing.Activate(points, pointer, grabInValue))
				{
					changed |= CurveTrackState.Remove(points, removing.ActivePoint, pinEnds);
				}
			}

			return changed;
		}
	}
}

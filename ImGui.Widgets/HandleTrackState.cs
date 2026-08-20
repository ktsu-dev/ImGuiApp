// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The interaction behind a set of draggable handles on a track: which one is being dragged,
	/// where a drag moves it, and how the set stays ordered and separated.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui. Callers convert a mouse position to a value on the track and
	/// hand that in, which is what lets every rule here be tested without a graphics context —
	/// the same split <see cref="ImageCanvasState"/> and the gesture types already use.
	/// </remarks>
	public sealed class HandleTrackState
	{
		/// <summary>
		/// Clamps handles into the bounds, sorts them ascending, and opens each neighbouring pair
		/// to at least <paramref name="minGap"/>.
		/// </summary>
		/// <remarks>
		/// Run before interaction so the handles are in a valid state whatever the caller passed.
		/// The gap pass walks upward and then back down, because pushing up alone would drive the
		/// last handle past <paramref name="upperBound"/> when the gaps cannot all fit.
		/// If the requested <paramref name="minGap"/> is too wide to fit all handles, it is narrowed
		/// so the handles spread evenly across the range — a sensible default for UI controls.
		/// </remarks>
		public static void Normalize(Span<float> handles, float lowerBound, float upperBound, float minGap)
		{
			if (lowerBound > upperBound)
			{
				(lowerBound, upperBound) = (upperBound, lowerBound);
			}

			minGap = Math.Clamp(minGap, 0f, handles.Length > 1 ? (upperBound - lowerBound) / (handles.Length - 1) : 0f);

			for (int i = 0; i < handles.Length; i++)
			{
				handles[i] = Math.Clamp(handles[i], lowerBound, upperBound);
			}

			handles.Sort();

			for (int i = 1; i < handles.Length; i++)
			{
				handles[i] = MathF.Max(handles[i], handles[i - 1] + minGap);
			}

			// The upward pass can overshoot the top when the gaps do not all fit; settle downward.
			for (int i = handles.Length - 1; i >= 0; i--)
			{
				float ceiling = i == handles.Length - 1 ? upperBound : handles[i + 1] - minGap;
				handles[i] = MathF.Min(handles[i], ceiling);
			}
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;

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
		/// <summary>Gets the index of the handle being dragged, or -1 when none is.</summary>
		public int ActiveHandle { get; private set; } = -1;

		/// <summary>Selects the handle nearest <paramref name="value"/> as the one being dragged.</summary>
		/// <remarks>
		/// Nearest by value rather than by pixel. The widget converts the mouse position to a value
		/// before calling in, which keeps every rule here testable without a graphics context.
		/// Ties resolve to the lower index so the midpoint between two handles behaves the same way
		/// every time.
		/// </remarks>
		public void Activate(ReadOnlySpan<float> handles, float value)
		{
			int nearest = -1;
			float bestDistance = float.MaxValue;

			for (int i = 0; i < handles.Length; i++)
			{
				float distance = MathF.Abs(handles[i] - value);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					nearest = i;
				}
			}

			ActiveHandle = nearest;
		}

		/// <summary>Clears the active handle.</summary>
		public void Release() => ActiveHandle = -1;

		/// <summary>Moves the active handle to <paramref name="value"/>, held inside its neighbours and the bounds.</summary>
		/// <returns><see langword="true"/> if the handle moved; otherwise <see langword="false"/>.</returns>
		/// <remarks>
		/// A drag that resolves to where the handle already sits reports no change. Consumers turn
		/// changes into undo entries, and a stationary mouse held down must not mint one per frame.
		/// </remarks>
		[SuppressMessage("Major Code Smell", "S1244:Do not check floating point inequality with exact values, use a range instead.", Justification = "Exact comparison is intentional: it detects whether the clamped value differs from the stored one at all, and a tolerance would suppress genuine small drags.")]
		public bool Drag(Span<float> handles, float value, float lowerBound, float upperBound, float minGap)
		{
			if (ActiveHandle < 0 || ActiveHandle >= handles.Length)
			{
				return false;
			}

			float floor = ActiveHandle == 0 ? lowerBound : handles[ActiveHandle - 1] + minGap;
			float ceiling = ActiveHandle == handles.Length - 1 ? upperBound : handles[ActiveHandle + 1] - minGap;

			// A gap wider than the space available would invert these; keep the window non-empty.
			ceiling = MathF.Max(ceiling, floor);

			float clamped = Math.Clamp(value, floor, ceiling);
			if (clamped == handles[ActiveHandle])
			{
				return false;
			}

			handles[ActiveHandle] = clamped;
			return true;
		}

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

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>The fixed geometry of a step grid, in pixels relative to the grid's top-left corner.</summary>
	/// <remarks>
	/// <para>
	/// Deliberately fixed rather than reflowed. In a step grid the column count is the pattern
	/// length, so column <c>i</c> is always step <c>i</c>: a layout that fitted its columns to the
	/// available width would move step 9 under step 1 and change what the pattern means.
	/// </para>
	/// <para>
	/// Cells are half-open rectangles <c>[CellMin, CellMin + CellSize)</c>, gaps belong to no cell,
	/// and the index of cell (row, step) is always <c>row * Steps + step</c>.
	/// </para>
	/// </remarks>
	/// <param name="Rows">The number of rows (voices).</param>
	/// <param name="Steps">The number of columns (time steps).</param>
	/// <param name="CellSize">The size of one cell.</param>
	/// <param name="Gap">The space between neighbouring cells, on both axes.</param>
	internal readonly record struct StepGridLayout(int Rows, int Steps, Vector2 CellSize, float Gap)
	{
		/// <summary>
		/// Gets the total size: <c>(Steps * CellSize.X + (Steps - 1) * Gap, Rows * CellSize.Y + (Rows - 1) * Gap)</c>,
		/// or zero when either count is not positive.
		/// </summary>
		public Vector2 Size => Rows <= 0 || Steps <= 0
			? Vector2.Zero
			: new Vector2(
				(Steps * CellSize.X) + ((Steps - 1) * Gap),
				(Rows * CellSize.Y) + ((Rows - 1) * Gap));

		/// <summary>Gets the top-left of cell (<paramref name="row"/>, <paramref name="step"/>).</summary>
		/// <param name="row">The cell's row.</param>
		/// <param name="step">The cell's step.</param>
		/// <returns><c>(step * (CellSize.X + Gap), row * (CellSize.Y + Gap))</c>.</returns>
		public Vector2 CellMin(int row, int step) =>
			new(step * (CellSize.X + Gap), row * (CellSize.Y + Gap));

		/// <summary>Finds the cell under a point.</summary>
		/// <param name="local">The point, relative to the grid's top-left corner.</param>
		/// <returns>
		/// The row-major index of the cell under <paramref name="local"/>, or -1 when the point is
		/// NaN, outside the grid, or in a gap.
		/// </returns>
		public int HitTest(Vector2 local)
		{
			Vector2 size = Size;

			// Written as negated comparisons so a NaN on either axis fails them and lands here too.
			if (!(local.X >= 0f && local.Y >= 0f && local.X < size.X && local.Y < size.Y))
			{
				return -1;
			}

			float pitchX = CellSize.X + Gap;
			float pitchY = CellSize.Y + Gap;
			if (!(pitchX > 0f && pitchY > 0f))
			{
				return -1;
			}

			// Clamped because a point a hair inside the far edge can divide out to the count itself
			// when the gap is zero; the offset test below still places it correctly.
			int step = Math.Min((int)MathF.Floor(local.X / pitchX), Steps - 1);
			int row = Math.Min((int)MathF.Floor(local.Y / pitchY), Rows - 1);

			if (local.X - (step * pitchX) >= CellSize.X || local.Y - (row * pitchY) >= CellSize.Y)
			{
				return -1;
			}

			return (row * Steps) + step;
		}

		/// <summary>Reports whether a step starts a beat.</summary>
		/// <param name="step">The step.</param>
		/// <param name="stepsPerBeat">How many steps make a beat. Zero or less means no beats.</param>
		/// <returns><see langword="true"/> when <paramref name="stepsPerBeat"/> &gt; 0 and <paramref name="step"/> is a multiple of it.</returns>
		public static bool IsBeatStart(int step, int stepsPerBeat) =>
			stepsPerBeat > 0 && step % stepsPerBeat == 0;
	}

	/// <summary>The toggle-then-paint gesture behind a step grid, independent of ImGui.</summary>
	/// <remarks>
	/// A press toggles the cell it lands on, and the new value of that cell becomes the value the
	/// rest of the gesture paints, so one drag either fills or clears a run of cells and never
	/// flickers them back and forth. <see cref="PaintValue"/> changes only on <see cref="Press"/>.
	/// </remarks>
	internal sealed class StepGridState
	{
		private Vector2 lastPoint;

		/// <summary>Gets a value indicating whether a gesture is in progress: true between a press that landed on a cell and <see cref="Release"/>.</summary>
		public bool IsPainting { get; private set; }

		/// <summary>Gets the value being painted: the new value of the cell the gesture started on.</summary>
		public bool PaintValue { get; private set; }

		/// <summary>Starts a gesture at <paramref name="local"/>.</summary>
		/// <remarks>
		/// A press always toggles, even when it lands on the same cell as the previous gesture. A
		/// press on a gap or outside the grid starts nothing, so dragging onto cells from there
		/// paints nothing either.
		/// </remarks>
		/// <param name="steps">The row-major cell values. Edited in place.</param>
		/// <param name="layout">The grid's geometry.</param>
		/// <param name="local">The press position, relative to the grid's top-left corner.</param>
		/// <returns><see langword="true"/> if the press toggled a cell.</returns>
		public bool Press(Span<bool> steps, StepGridLayout layout, Vector2 local)
		{
			int index = layout.HitTest(local);
			if (index < 0 || index >= steps.Length)
			{
				IsPainting = false;
				return false;
			}

			PaintValue = !steps[index];
			steps[index] = PaintValue;
			IsPainting = true;
			lastPoint = local;
			return true;
		}

		/// <summary>Paints every cell on the segment from the last point to <paramref name="local"/>.</summary>
		/// <remarks>
		/// <para>
		/// The segment is sampled at half the smaller cell dimension, both ends included, so a fast
		/// drag that crosses several cells between two frames still paints every one of them. A cell
		/// already holding <see cref="PaintValue"/> does not count as a change, which makes painting
		/// idempotent: dragging back over painted cells reports nothing.
		/// </para>
		/// <para>
		/// The segment is clipped to the grid before it is sampled. Samples outside the grid hit no
		/// cell anyway, and clipping bounds the sample count by the grid's size, where the pointer's
		/// distance is unbounded: Dear ImGui reports an unavailable mouse at <c>-FLT_MAX</c>.
		/// </para>
		/// </remarks>
		/// <param name="steps">The row-major cell values. Edited in place.</param>
		/// <param name="layout">The grid's current geometry; indices are recomputed from it on every call.</param>
		/// <param name="local">The pointer position, relative to the grid's top-left corner.</param>
		/// <returns><see langword="true"/> if at least one cell's value changed.</returns>
		public bool PaintTo(Span<bool> steps, StepGridLayout layout, Vector2 local)
		{
			if (!IsPainting)
			{
				return false;
			}

			bool changed = false;
			Vector2 from = lastPoint;
			Vector2 delta = local - from;

			if (float.IsFinite(delta.X) && float.IsFinite(delta.Y))
			{
				if (TryClip(from, delta, layout.Size, out float t0, out float t1))
				{
					Vector2 start = from + (delta * t0);
					Vector2 end = from + (delta * t1);
					changed = PaintSegment(steps, layout, start, end);
				}
			}
			else
			{
				// No usable segment: fall back to the end point alone, which HitTest rejects if it is
				// not a number or outside the grid.
				changed = PaintAt(steps, layout, local);
			}

			lastPoint = local;
			return changed;
		}

		/// <summary>Ends the gesture. <see cref="PaintValue"/> keeps its value, unused until the next <see cref="Press"/>.</summary>
		public void Release() => IsPainting = false;

		private bool PaintSegment(Span<bool> steps, StepGridLayout layout, Vector2 start, Vector2 end)
		{
			float spacing = MathF.Min(layout.CellSize.X, layout.CellSize.Y) / 2f;
			float length = Vector2.Distance(start, end);
			int count = spacing > 0f && float.IsFinite(length)
				? Math.Max(1, (int)MathF.Ceiling(length / spacing))
				: 1;

			bool changed = false;
			for (int i = 0; i <= count; i++)
			{
				changed |= PaintAt(steps, layout, Vector2.Lerp(start, end, i / (float)count));
			}

			return changed;
		}

		private bool PaintAt(Span<bool> steps, StepGridLayout layout, Vector2 point)
		{
			int index = layout.HitTest(point);
			if (index < 0 || index >= steps.Length || steps[index] == PaintValue)
			{
				return false;
			}

			steps[index] = PaintValue;
			return true;
		}

		// Liang-Barsky: narrows the parameter range [t0, t1] of from + t * delta to the part that lies
		// inside [0, size] on both axes. Returns false when none of it does.
		private static bool TryClip(Vector2 from, Vector2 delta, Vector2 size, out float t0, out float t1)
		{
			t0 = 0f;
			t1 = 1f;

			return ClipAxis(-delta.X, from.X, ref t0, ref t1)
				&& ClipAxis(delta.X, size.X - from.X, ref t0, ref t1)
				&& ClipAxis(-delta.Y, from.Y, ref t0, ref t1)
				&& ClipAxis(delta.Y, size.Y - from.Y, ref t0, ref t1);
		}

		private static bool ClipAxis(float p, float q, ref float t0, ref float t1)
		{
			if (MathF.Abs(p) <= float.Epsilon)
			{
				// Parallel to this boundary: inside it or not at all.
				return q >= 0f;
			}

			float r = q / p;
			if (p < 0f)
			{
				if (r > t1)
				{
					return false;
				}

				t0 = MathF.Max(t0, r);
			}
			else
			{
				if (r < t0)
				{
					return false;
				}

				t1 = MathF.Min(t1, r);
			}

			return true;
		}
	}
}

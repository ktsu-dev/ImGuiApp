// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The interaction behind <see cref="ParametricEq"/>: where each band's node sits, which node a
	/// press grabs, how a drag moves it, how the wheel changes its Q, and how bands are held inside
	/// the axes.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="CurveTrackState"/>. The widget hands in pointer
	/// positions in pixels and the rectangle it draws into, which is what lets every rule here be
	/// tested without a graphics context.
	/// </remarks>
	internal sealed class ParametricEqState
	{
		/// <summary>How much one wheel detent multiplies a band's Q by.</summary>
		internal const float QStepPerDetent = 1.2f;

		/// <summary>
		/// How far, in pixels, a drag target may sit from its node and still count as not having moved.
		/// </summary>
		/// <remarks>
		/// A thousandth of a pixel: far below anything a pointer can express, and far above the rounding
		/// that adding and subtracting the grab offset leaves behind.
		/// </remarks>
		internal const float StillTolerance = 1e-3f;

		/// <summary>Gets the index of the band being dragged, or -1 when none is.</summary>
		public int ActiveBand { get; private set; } = -1;

		/// <summary>
		/// Gets the offset from the pointer to the grabbed node's centre at the press, so a press
		/// slightly off-centre does not make the node jump under the pointer.
		/// </summary>
		public Vector2 GrabOffset { get; private set; }

		/// <summary>Finds where a band's node is drawn.</summary>
		/// <param name="band">The band.</param>
		/// <param name="axis">The frequency axis.</param>
		/// <param name="minDb">The gain at the bottom of the rectangle.</param>
		/// <param name="maxDb">The gain at the top of the rectangle.</param>
		/// <param name="min">The rectangle's top-left corner, in pixels.</param>
		/// <param name="size">The rectangle's size, in pixels.</param>
		/// <returns>The node's centre, in pixels. A band without a gain sits on the 0 dB line.</returns>
		public static Vector2 NodePosition(EqBand band, LogFrequencyAxis axis, float minDb, float maxDb, Vector2 min, Vector2 size)
		{
			Ensure.NotNull(axis);

			float x = min.X + (axis.FrequencyToPosition(band.Frequency) * size.X);
			float gain = Math.Clamp(band.HasGain ? band.GainDb : 0f, minDb, maxDb);
			float y = min.Y + ((maxDb - gain) / (maxDb - minDb) * size.Y);
			return new Vector2(x, y);
		}

		/// <summary>Finds the node nearest the pointer, if one is within reach.</summary>
		/// <param name="nodes">Every node's centre, in band order.</param>
		/// <param name="pointer">The pointer, in pixels.</param>
		/// <param name="grabRadius">How far from a node's centre still grabs it, in pixels.</param>
		/// <returns>The nearest node's index, or -1 when none is within <paramref name="grabRadius"/>.</returns>
		/// <remarks>
		/// On an exact tie the later band wins, because later bands are drawn on top: the node a press
		/// grabs is the one the user can see.
		/// </remarks>
		public static int Pick(ReadOnlySpan<Vector2> nodes, Vector2 pointer, float grabRadius)
		{
			int nearest = -1;
			float bestDistance = float.MaxValue;

			for (int i = 0; i < nodes.Length; i++)
			{
				float distance = Vector2.Distance(nodes[i], pointer);
				if (distance <= grabRadius && distance <= bestDistance)
				{
					bestDistance = distance;
					nearest = i;
				}
			}

			return nearest;
		}

		/// <summary>Grabs the node under the pointer, if there is one.</summary>
		/// <param name="nodes">Every node's centre, in band order.</param>
		/// <param name="pointer">The pointer, in pixels.</param>
		/// <param name="grabRadius">How far from a node's centre still grabs it, in pixels.</param>
		/// <returns><see langword="true"/> if a node was grabbed.</returns>
		public bool Activate(ReadOnlySpan<Vector2> nodes, Vector2 pointer, float grabRadius)
		{
			ActiveBand = Pick(nodes, pointer, grabRadius);
			GrabOffset = ActiveBand >= 0 ? nodes[ActiveBand] - pointer : Vector2.Zero;
			return ActiveBand >= 0;
		}

		/// <summary>Moves the grabbed band to follow the pointer.</summary>
		/// <param name="bands">The bands, edited in place.</param>
		/// <param name="pointer">The pointer, in pixels.</param>
		/// <param name="axis">The frequency axis.</param>
		/// <param name="minDb">The gain at the bottom of the rectangle.</param>
		/// <param name="maxDb">The gain at the top of the rectangle.</param>
		/// <param name="min">The rectangle's top-left corner, in pixels.</param>
		/// <param name="size">The rectangle's size, in pixels.</param>
		/// <returns><see langword="true"/> if the band's frequency or gain actually changed.</returns>
		/// <remarks>
		/// A band without a gain moves only horizontally and keeps its <see cref="EqBand.GainDb"/>
		/// untouched. A drag that resolves to the values the band already has writes nothing, so a
		/// held pointer does not report a change every frame.
		/// </remarks>
		public bool Drag(Span<EqBand> bands, Vector2 pointer, LogFrequencyAxis axis, float minDb, float maxDb, Vector2 min, Vector2 size)
		{
			Ensure.NotNull(axis);

			if (ActiveBand < 0 || ActiveBand >= bands.Length)
			{
				return false;
			}

			Vector2 target = pointer + GrabOffset;
			EqBand band = bands[ActiveBand];

			// An axis the target has not moved along keeps its value exactly. Mapping the node's own
			// position back through the axis would land an ulp or two away from where it started, and a
			// press that does not move would then report a change.
			Vector2 node = NodePosition(band, axis, minDb, maxDb, min, size);

			float frequency = MathF.Abs(target.X - node.X) <= StillTolerance
				? band.Frequency
				: axis.PositionToFrequency(Math.Clamp((target.X - min.X) / size.X, 0f, 1f));
			float gain = band.HasGain && MathF.Abs(target.Y - node.Y) > StillTolerance
				? Math.Clamp(maxDb - ((target.Y - min.Y) / size.Y * (maxDb - minDb)), minDb, maxDb)
				: band.GainDb;

			// Compared as a whole band: a record compares its fields exactly, which is the question
			// here, whether anything moved at all, rather than whether it moved far.
			EqBand moved = band with { Frequency = frequency, GainDb = gain };
			if (moved == band)
			{
				return false;
			}

			bands[ActiveBand] = moved;
			return true;
		}

		/// <summary>Changes a band's Q by some wheel detents.</summary>
		/// <param name="bands">The bands, edited in place.</param>
		/// <param name="index">Which band.</param>
		/// <param name="wheel">How many detents, positive to narrow the band.</param>
		/// <returns><see langword="true"/> if the Q changed.</returns>
		/// <remarks>Each detent multiplies Q by 1.2, so the wheel feels the same at every width.</remarks>
		public static bool AdjustQ(Span<EqBand> bands, int index, float wheel)
		{
			if (index < 0 || index >= bands.Length || MathF.Abs(wheel) <= float.Epsilon)
			{
				return false;
			}

			EqBand band = bands[index];
			EqBand adjusted = band with { Q = Math.Clamp(band.Q * MathF.Pow(QStepPerDetent, wheel), EqBand.MinQ, EqBand.MaxQ) };
			if (adjusted == band)
			{
				return false;
			}

			bands[index] = adjusted;
			return true;
		}

		/// <summary>Holds every band inside the axes and the Q range, and replaces NaN.</summary>
		/// <param name="bands">The bands, edited in place.</param>
		/// <param name="axis">The frequency axis.</param>
		/// <param name="minDb">The lowest gain.</param>
		/// <param name="maxDb">The highest gain.</param>
		/// <returns><see langword="true"/> if anything was written.</returns>
		/// <remarks>
		/// A NaN frequency becomes the axis's geometric centre, a NaN gain 0 dB and a NaN Q
		/// <see cref="EqBand.DefaultQ"/>. An infinity clamps to the nearer bound, like any other value
		/// out of range. Gainless bands have their gain held in range too, so a band switched to a type
		/// with a gain starts somewhere the widget can draw.
		/// </remarks>
		public static bool Normalize(Span<EqBand> bands, LogFrequencyAxis axis, float minDb, float maxDb)
		{
			Ensure.NotNull(axis);

			bool changed = false;
			for (int i = 0; i < bands.Length; i++)
			{
				EqBand band = bands[i];

				float frequency = float.IsNaN(band.Frequency)
					? MathF.Sqrt(axis.MinFrequency * axis.MaxFrequency)
					: Math.Clamp(band.Frequency, axis.MinFrequency, axis.MaxFrequency);
				float gain = float.IsNaN(band.GainDb) ? 0f : Math.Clamp(band.GainDb, minDb, maxDb);
				float q = float.IsNaN(band.Q) ? EqBand.DefaultQ : Math.Clamp(band.Q, EqBand.MinQ, EqBand.MaxQ);

				EqBand normalized = band with { Frequency = frequency, GainDb = gain, Q = q };
				if (normalized == band)
				{
					continue;
				}

				bands[i] = normalized;
				changed = true;
			}

			return changed;
		}

		/// <summary>Clears the grabbed band.</summary>
		public void Release() => ActiveBand = -1;
	}
}

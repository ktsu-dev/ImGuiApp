// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The console fader taper shared by <see cref="ChannelFader"/> and its meter. Position 0 is the
	/// bottom stop, 1 the top.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Unity gain sits at <see cref="UnityPosition"/>. Between the breakpoints below the level is
	/// linear in position, which stretches the working range and compresses the bottom of the travel
	/// towards -inf:
	/// </para>
	/// <code>
	/// position  0.05  0.15  0.30  0.50  0.75  1.00
	/// dB         -60   -40   -20   -10     0  maxDb
	/// </code>
	/// <para>
	/// Below 0.05 the level falls logarithmically, joining -60 continuously and reaching -inf only
	/// at exactly 0. Only the top segment depends on <c>maxDb</c>, which every method clamps to
	/// <c>[1, 24]</c> and reads as 6 when it is <see cref="float.NaN"/>.
	/// </para>
	/// <para>
	/// Public and free of ImGui so that a host converting automation data or a controller position
	/// uses exactly the numbers the widget draws with.
	/// </para>
	/// </remarks>
	public static class FaderTaper
	{
		/// <summary>Position of unity gain (0 dB): 0.75.</summary>
		public const float UnityPosition = 0.75f;

		/// <summary>The lowest finite level on the scale; anything quieter is -inf. -60.</summary>
		public const float FloorDb = -60f;

		private const float DefaultMaxDb = 6f;
		private const float MinimumMaxDb = 1f;
		private const float MaximumMaxDb = 24f;

		// The breakpoints, lowest first. The last level is maxDb and is filled in per call.
		private static readonly double[] Positions = [0.05, 0.15, 0.30, 0.50, 0.75, 1.00];
		private static readonly double[] Levels = [-60.0, -40.0, -20.0, -10.0, 0.0, double.NaN];

		/// <summary>Maps a travel position in [0, 1] to dB. 0 returns <see cref="float.NegativeInfinity"/>.</summary>
		/// <param name="position">The travel position. Clamped to [0, 1]; <see cref="float.NaN"/> is read as <see cref="UnityPosition"/>.</param>
		/// <param name="maxDb">The gain at the top of the travel. Clamped to [1, 24]; <see cref="float.NaN"/> is read as 6.</param>
		/// <returns>The gain in dB.</returns>
		public static float PositionToDb(float position, float maxDb = DefaultMaxDb)
		{
			double top = ClampMaxDb(maxDb);
			double p = float.IsNaN(position) ? UnityPosition : Math.Clamp(position, 0f, 1f);

			if (p <= 0.0)
			{
				return float.NegativeInfinity;
			}

			if (p < Positions[0])
			{
				return (float)(FloorDb + (20.0 * Math.Log10(p / Positions[0])));
			}

			for (int i = 1; i < Positions.Length; i++)
			{
				if (p <= Positions[i] || i == Positions.Length - 1)
				{
					double lowLevel = Levels[i - 1];
					double highLevel = LevelAt(i, top);
					double t = (p - Positions[i - 1]) / (Positions[i] - Positions[i - 1]);
					return (float)(lowLevel + ((highLevel - lowLevel) * t));
				}
			}

			return (float)top;
		}

		/// <summary>
		/// Maps dB to a travel position in [0, 1]. The exact inverse of <see cref="PositionToDb"/> on (0, 1].
		/// </summary>
		/// <param name="db">The gain in dB. -inf maps to 0, anything at or above <paramref name="maxDb"/> to 1, and <see cref="float.NaN"/> is read as 0 dB.</param>
		/// <param name="maxDb">The gain at the top of the travel. Clamped to [1, 24]; <see cref="float.NaN"/> is read as 6.</param>
		/// <returns>The travel position.</returns>
		public static float DbToPosition(float db, float maxDb = DefaultMaxDb)
		{
			double top = ClampMaxDb(maxDb);
			double level = float.IsNaN(db) ? 0.0 : db;

			if (double.IsNegativeInfinity(level))
			{
				return 0f;
			}

			if (level >= top)
			{
				return 1f;
			}

			if (level < FloorDb)
			{
				return (float)(Positions[0] * Math.Pow(10.0, (level - FloorDb) / 20.0));
			}

			for (int i = 1; i < Positions.Length; i++)
			{
				double highLevel = LevelAt(i, top);
				if (level <= highLevel)
				{
					double lowLevel = Levels[i - 1];
					double t = (level - lowLevel) / (highLevel - lowLevel);
					return (float)(Positions[i - 1] + ((Positions[i] - Positions[i - 1]) * t));
				}
			}

			return 1f;
		}

		/// <summary>
		/// Steps <paramref name="db"/> by <paramref name="notches"/> x <paramref name="stepDb"/>, falling
		/// to -inf below the floor and clamped to maxDb.
		/// </summary>
		/// <param name="db">The gain in dB. <see cref="float.NaN"/> is read as 0 dB, as <see cref="DbToPosition"/> reads it.</param>
		/// <param name="notches">How many steps to take; negative steps down. 0 or <see cref="float.NaN"/> leaves <paramref name="db"/> unchanged.</param>
		/// <param name="stepDb">The size of one step in dB.</param>
		/// <param name="maxDb">The gain at the top of the travel. Clamped to [1, 24]; <see cref="float.NaN"/> is read as 6.</param>
		/// <returns>
		/// The stepped gain. From -inf any upward step lands exactly on <see cref="FloorDb"/> and any
		/// downward one stays at -inf.
		/// </returns>
		public static float Nudge(float db, float notches, float stepDb, float maxDb = DefaultMaxDb)
		{
			// Exactly zero notches is the documented no-op; a tolerance would swallow a genuine fractional wheel delta.
			if (float.IsNaN(notches) || notches is 0f)
			{
				return db;
			}

			float top = ClampMaxDb(maxDb);
			float delta = notches * stepDb;

			if (float.IsNegativeInfinity(db))
			{
				return delta > 0f ? FloorDb : float.NegativeInfinity;
			}

			float result = (float.IsNaN(db) ? 0f : db) + delta;
			if (float.IsNaN(result))
			{
				return db;
			}

			if (result < FloorDb)
			{
				return float.NegativeInfinity;
			}

			return MathF.Min(result, top);
		}

		/// <summary>Clamps a top-of-travel gain to [1, 24], reading <see cref="float.NaN"/> as 6.</summary>
		internal static float ClampMaxDb(float maxDb) =>
			float.IsNaN(maxDb) ? DefaultMaxDb : Math.Clamp(maxDb, MinimumMaxDb, MaximumMaxDb);

		private static double LevelAt(int index, double top) =>
			index == Levels.Length - 1 ? top : Levels[index];
	}
}

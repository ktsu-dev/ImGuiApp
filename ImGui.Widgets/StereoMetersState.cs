// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Phase-correlation ballistics and the mid/side mapping behind the stereo meters. Owned by the
	/// caller, one per correlation meter, kept across frames. Has no ImGui dependency.
	/// </summary>
	/// <remarks>
	/// Feed it every audio block with <see cref="Push"/> and hand <see cref="Correlation"/> to
	/// <see cref="CorrelationMeter(string, float, Vector2)"/>. The widget never sees the state, so a
	/// host that computes correlation elsewhere can pass its own value instead.
	/// </remarks>
	public sealed class StereoMetersState
	{
		/// <summary>Default <see cref="IntegrationTime"/>, in seconds.</summary>
		public const float DefaultIntegrationTime = 0.3f;

		/// <summary>
		/// Below this much energy a channel counts as silent, and silence is neither in phase nor out.
		/// </summary>
		private const double SilenceThreshold = 1e-12;

		/// <summary>
		/// Gets or sets the exponential smoothing time constant in seconds. Negative clamps to 0 (no
		/// smoothing); <see cref="float.NaN"/> resets to <see cref="DefaultIntegrationTime"/>.
		/// </summary>
		public float IntegrationTime
		{
			get;
			set => field = float.IsNaN(value) ? DefaultIntegrationTime : Math.Max(value, 0.0f);
		} = DefaultIntegrationTime;

		/// <summary>Gets the smoothed correlation, always in <c>[-1, 1]</c>. Starts at 0.</summary>
		public float Correlation { get; private set; }

		/// <summary>Feeds one block and advances the ballistics by <paramref name="deltaSeconds"/>.</summary>
		/// <param name="left">The left channel block.</param>
		/// <param name="right">The right channel block. Only the first min(left.Length, right.Length) pairs are used.</param>
		/// <param name="deltaSeconds">The time the block covers. Zero, negative or <see cref="float.NaN"/> changes nothing.</param>
		public void Push(ReadOnlySpan<float> left, ReadOnlySpan<float> right, float deltaSeconds)
		{
			float target = Correlate(left, right);
			if (!(deltaSeconds > 0.0f))
			{
				return;
			}

			if (IntegrationTime == 0.0f)
			{
				Correlation = target;
				return;
			}

			float alpha = 1.0f - MathF.Exp(-deltaSeconds / IntegrationTime);
			Correlation = Math.Clamp(Correlation + (alpha * (target - Correlation)), -1.0f, 1.0f);
		}

		/// <summary>Returns <see cref="Correlation"/> to 0.</summary>
		public void Reset() => Correlation = 0.0f;

		/// <summary>
		/// The instantaneous correlation of one block: sum(LR) / sqrt(sum(L^2) * sum(R^2)), or 0 when
		/// either channel is silent.
		/// </summary>
		/// <param name="left">The left channel block.</param>
		/// <param name="right">The right channel block. Only the first min(left.Length, right.Length) pairs are used.</param>
		/// <returns>The correlation in <c>[-1, 1]</c>. Pairs containing a non-finite sample are skipped.</returns>
		public static float Correlate(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
		{
			int count = Math.Min(left.Length, right.Length);
			double sumLR = 0.0;
			double sumLL = 0.0;
			double sumRR = 0.0;

			for (int i = 0; i < count; i++)
			{
				float l = left[i];
				float r = right[i];
				if (!float.IsFinite(l) || !float.IsFinite(r))
				{
					continue;
				}

				sumLR += (double)l * r;
				sumLL += (double)l * l;
				sumRR += (double)r * r;
			}

			if (sumLL < SilenceThreshold || sumRR < SilenceThreshold)
			{
				return 0.0f;
			}

			return (float)Math.Clamp(sumLR / Math.Sqrt(sumLL * sumRR), -1.0, 1.0);
		}

		/// <summary>
		/// Maps one sample pair to goniometer space: X = side = (L - R) / 2, Y = mid = (L + R) / 2.
		/// </summary>
		/// <param name="left">The left sample.</param>
		/// <param name="right">The right sample.</param>
		/// <returns>
		/// The point, rotated 45 degrees from (L, R) and scaled by 1/sqrt(2), so full-scale mono lands at mid = 1.
		/// </returns>
		public static Vector2 ToMidSide(float left, float right) => new((left - right) * 0.5f, (left + right) * 0.5f);
	}
}

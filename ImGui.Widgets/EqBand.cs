// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

public static partial class ImGuiWidgets
{
	/// <summary>The filter shape of one EQ band.</summary>
	public enum EqBandType
	{
		/// <summary>A bell: boosts or cuts around its frequency, and leaves both ends alone.</summary>
		Peak,

		/// <summary>Boosts or cuts everything below its frequency.</summary>
		LowShelf,

		/// <summary>Boosts or cuts everything above its frequency.</summary>
		HighShelf,

		/// <summary>A high-pass: removes everything below its frequency. Has no gain.</summary>
		LowCut,

		/// <summary>A low-pass: removes everything above its frequency. Has no gain.</summary>
		HighCut,

		/// <summary>Removes a narrow band around its frequency. Has no gain.</summary>
		Notch,
	}

	/// <summary>
	/// One EQ band. <see cref="EqBandType.LowCut"/>, <see cref="EqBandType.HighCut"/> and
	/// <see cref="EqBandType.Notch"/> ignore <see cref="GainDb"/>.
	/// </summary>
	/// <param name="Frequency">The band's centre, corner or cutoff frequency, in hertz.</param>
	/// <param name="GainDb">The band's boost (positive) or cut (negative), in decibels.</param>
	/// <param name="Q">How narrow the band is; higher is narrower.</param>
	/// <param name="Type">The band's filter shape.</param>
	public readonly record struct EqBand(float Frequency, float GainDb, float Q, EqBandType Type = EqBandType.Peak)
	{
		/// <summary>The lowest Q a band can be given by the widget.</summary>
		public const float MinQ = 0.1f;

		/// <summary>The highest Q a band can be given by the widget.</summary>
		public const float MaxQ = 18f;

		/// <summary>The Q a NaN is replaced with: 1/sqrt(2), the Butterworth value.</summary>
		public const float DefaultQ = 0.70710677f;

		/// <summary>
		/// Gets a value indicating whether the band has a gain: true for <see cref="EqBandType.Peak"/>,
		/// <see cref="EqBandType.LowShelf"/> and <see cref="EqBandType.HighShelf"/>, the types whose node
		/// moves vertically.
		/// </summary>
		public bool HasGain => Type is EqBandType.Peak or EqBandType.LowShelf or EqBandType.HighShelf;
	}

	/// <summary>
	/// RBJ Audio EQ Cookbook biquad magnitudes, context-free, for callers with no filter math of their own.
	/// </summary>
	/// <remarks>
	/// Each band is one second-order section with the cookbook's coefficients, evaluated on the unit
	/// circle in double precision. A caller whose DSP uses different filters should plot its own
	/// response instead, so the curve on screen stays the curve that is applied.
	/// </remarks>
	public static class EqResponse
	{
		/// <summary>One band's magnitude response in dB at <paramref name="frequency"/> Hz.</summary>
		/// <param name="band">The band.</param>
		/// <param name="frequency">Where to evaluate the response, in hertz.</param>
		/// <param name="sampleRate">The sample rate the biquad runs at, in hertz.</param>
		/// <returns>
		/// The band's gain at <paramref name="frequency"/>, in decibels. Both frequencies are clamped to
		/// [1, 0.499 * <paramref name="sampleRate"/>]; a NaN in the band or the frequency gives NaN.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="sampleRate"/> is not finite, or is too low for the clamp range to be non-empty
		/// (below about 2 Hz).
		/// </exception>
		public static float BandDb(EqBand band, float frequency, float sampleRate = 48000f)
		{
			double nyquistLimit = 0.499 * sampleRate;
			if (!float.IsFinite(sampleRate) || nyquistLimit < 1.0)
			{
				throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "The sample rate must be finite and above about 2 Hz.");
			}

			double f0 = Clamp(band.Frequency, nyquistLimit);
			double f = Clamp(frequency, nyquistLimit);

			double w0 = 2.0 * Math.PI * f0 / sampleRate;
			double cos = Math.Cos(w0);
			double alpha = Math.Sin(w0) / (2.0 * band.Q);
			double a = Math.Pow(10.0, band.GainDb / 40.0);

			(double b0, double b1, double b2, double a0, double a1, double a2) = band.Type switch
			{
				EqBandType.LowShelf => LowShelf(a, cos, alpha),
				EqBandType.HighShelf => HighShelf(a, cos, alpha),
				EqBandType.LowCut => ((1.0 + cos) / 2.0, -(1.0 + cos), (1.0 + cos) / 2.0, 1.0 + alpha, -2.0 * cos, 1.0 - alpha),
				EqBandType.HighCut => ((1.0 - cos) / 2.0, 1.0 - cos, (1.0 - cos) / 2.0, 1.0 + alpha, -2.0 * cos, 1.0 - alpha),
				EqBandType.Notch => (1.0, -2.0 * cos, 1.0, 1.0 + alpha, -2.0 * cos, 1.0 - alpha),
				_ => (1.0 + (alpha * a), -2.0 * cos, 1.0 - (alpha * a), 1.0 + (alpha / a), -2.0 * cos, 1.0 - (alpha / a)),
			};

			// z^-1 on the unit circle at the evaluation frequency.
			double w = 2.0 * Math.PI * f / sampleRate;
			Complex z1 = Complex.FromPolarCoordinates(1.0, -w);
			Complex z2 = z1 * z1;

			double numerator = (b0 + (b1 * z1) + (b2 * z2)).Magnitude;
			double denominator = (a0 + (a1 * z1) + (a2 * z2)).Magnitude;

			return (float)(20.0 * Math.Log10(numerator / denominator));
		}

		/// <summary>The sum of <see cref="BandDb"/> over every band (cascaded biquads add in dB).</summary>
		/// <param name="bands">The bands, in any order.</param>
		/// <param name="frequency">Where to evaluate the response, in hertz.</param>
		/// <param name="sampleRate">The sample rate the biquads run at, in hertz.</param>
		/// <returns>The total gain in decibels; 0 for no bands.</returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="sampleRate"/> is not finite, or is below about 2 Hz, and there is at least one band.
		/// </exception>
		public static float TotalDb(ReadOnlySpan<EqBand> bands, float frequency, float sampleRate = 48000f)
		{
			float total = 0f;
			foreach (EqBand band in bands)
			{
				total += BandDb(band, frequency, sampleRate);
			}

			return total;
		}

		// Math.Clamp passes a NaN through, so a NaN band still reads as NaN. It throws when the range
		// is empty, which the sample-rate check in BandDb has already ruled out.
		private static double Clamp(float value, double max) => Math.Clamp(value, 1.0, max);

		private static (double B0, double B1, double B2, double A0, double A1, double A2) LowShelf(double a, double cos, double alpha)
		{
			double root = 2.0 * Math.Sqrt(a) * alpha;
			double plus = a + 1.0;
			double minus = a - 1.0;
			return (
				a * (plus - (minus * cos) + root),
				2.0 * a * (minus - (plus * cos)),
				a * (plus - (minus * cos) - root),
				plus + (minus * cos) + root,
				-2.0 * (minus + (plus * cos)),
				plus + (minus * cos) - root);
		}

		private static (double B0, double B1, double B2, double A0, double A1, double A2) HighShelf(double a, double cos, double alpha)
		{
			double root = 2.0 * Math.Sqrt(a) * alpha;
			double plus = a + 1.0;
			double minus = a - 1.0;
			return (
				a * (plus + (minus * cos) + root),
				-2.0 * a * (minus + (plus * cos)),
				a * (plus + (minus * cos) - root),
				plus - (minus * cos) + root,
				2.0 * (minus - (plus * cos)),
				plus - (minus * cos) - root);
		}
	}
}

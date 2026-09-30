// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// A levels adjustment: input black, input white and gamma, remapped to an output range. All
/// values are 0..1 except <see cref="Gamma"/>.
/// </summary>
/// <param name="InputBlack">The input value that maps to <see cref="OutputBlack"/>. Everything below it clips.</param>
/// <param name="InputWhite">The input value that maps to <see cref="OutputWhite"/>. Everything above it clips.</param>
/// <param name="Gamma">The midtone exponent. One is linear, above one brightens the midtones, below one darkens them.</param>
/// <param name="OutputBlack">The darkest output value.</param>
/// <param name="OutputWhite">The brightest output value.</param>
/// <remarks>
/// The widget that edits this value and the caller that applies it to pixels use the same
/// function, <see cref="Apply"/>, so the curve on screen and the curve applied to an image cannot
/// disagree. Grey point and gamma follow the Photoshop convention: a grey handle at the midpoint of
/// black and white is gamma one, and moving it towards black raises gamma and brightens.
/// </remarks>
public readonly record struct LevelsAdjustment(float InputBlack, float InputWhite, float Gamma, float OutputBlack, float OutputWhite)
{
	/// <summary>The smallest gap kept between neighbouring handles: one 8-bit step.</summary>
	public const float MinGap = 1f / 255f;

	/// <summary>The smallest gamma, reached with the grey point hard against white.</summary>
	public const float MinGamma = 0.1f;

	/// <summary>The largest gamma, reached with the grey point hard against black.</summary>
	public const float MaxGamma = 9.99f;

	/// <summary>How close to an end of the input range the grey point's fraction is allowed to get.</summary>
	private const float GreyFractionLimit = 1e-6f;

	/// <summary>How close a handle has to be to the current value to count as not having moved.</summary>
	private const float HandleTolerance = 1e-6f;

	/// <summary>Gets the adjustment that changes nothing: (0, 1, 1, 0, 1).</summary>
	public static LevelsAdjustment Identity { get; } = new(0f, 1f, 1f, 0f, 1f);

	/// <summary>Gets where the grey handle sits for this gamma, between <see cref="InputBlack"/> and <see cref="InputWhite"/>.</summary>
	public float GreyPoint => GreyPointFromGamma(InputBlack, InputWhite, Gamma);

	/// <summary>
	/// Maps one normalized channel value through the adjustment. The widget and the caller use this
	/// same function.
	/// </summary>
	/// <param name="value">A channel value, normally 0..1.</param>
	/// <returns>The output value in <see cref="OutputBlack"/>..<see cref="OutputWhite"/>.</returns>
	/// <remarks>
	/// This does not normalize: hold a value that came back from the widget or from
	/// <see cref="Normalized"/>. A NaN <paramref name="value"/> returns <see cref="OutputBlack"/>.
	/// </remarks>
	public float Apply(float value)
	{
		if (float.IsNaN(value))
		{
			return OutputBlack;
		}

		float x = Math.Clamp((value - InputBlack) / (InputWhite - InputBlack), 0f, 1f);
		float y = MathF.Pow(x, 1f / Gamma);
		return OutputBlack + ((OutputWhite - OutputBlack) * y);
	}

	/// <summary>Returns a copy with every field valid.</summary>
	/// <returns>
	/// A copy in which a NaN or infinite field has taken its <see cref="Identity"/> value, the 0..1
	/// fields and <see cref="Gamma"/> are clamped to their ranges, the input range is at least two
	/// <see cref="MinGap"/> wide (room for the grey handle) and the output range at least one.
	/// </returns>
	public LevelsAdjustment Normalized()
	{
		float inputBlack = Clamp01(Finite(InputBlack, Identity.InputBlack));
		float inputWhite = Clamp01(Finite(InputWhite, Identity.InputWhite));
		float gamma = Math.Clamp(Finite(Gamma, Identity.Gamma), MinGamma, MaxGamma);
		float outputBlack = Clamp01(Finite(OutputBlack, Identity.OutputBlack));
		float outputWhite = Clamp01(Finite(OutputWhite, Identity.OutputWhite));

		(inputBlack, inputWhite) = OpenRange(inputBlack, inputWhite, 2f * MinGap);
		(outputBlack, outputWhite) = OpenRange(outputBlack, outputWhite, MinGap);

		return new LevelsAdjustment(inputBlack, inputWhite, gamma, outputBlack, outputWhite);
	}

	/// <summary>
	/// Gamma for a grey handle at <paramref name="grey"/>, Photoshop convention: the midpoint is 1,
	/// and left of the midpoint brightens (gamma &gt; 1).
	/// </summary>
	/// <param name="black">The input black point.</param>
	/// <param name="white">The input white point.</param>
	/// <param name="grey">The grey handle's position.</param>
	/// <returns>A gamma in <see cref="MinGamma"/>..<see cref="MaxGamma"/>, or 1 for an empty range or a NaN argument.</returns>
	public static float GammaFromGreyPoint(float black, float white, float grey)
	{
		if (float.IsNaN(black) || float.IsNaN(white) || float.IsNaN(grey) || white - black <= 0f)
		{
			return 1f;
		}

		float fraction = Math.Clamp((grey - black) / (white - black), GreyFractionLimit, 1f - GreyFractionLimit);
		float gamma = MathF.Log(fraction) / MathF.Log(0.5f);
		return Math.Clamp(gamma, MinGamma, MaxGamma);
	}

	/// <summary>Grey handle position for <paramref name="gamma"/>; the inverse of <see cref="GammaFromGreyPoint"/>.</summary>
	/// <param name="black">The input black point.</param>
	/// <param name="white">The input white point.</param>
	/// <param name="gamma">The midtone exponent.</param>
	/// <returns>
	/// A position at least <see cref="MinGap"/> inside black and white, or their midpoint when the
	/// range is narrower than two gaps.
	/// </returns>
	public static float GreyPointFromGamma(float black, float white, float gamma)
	{
		if (white - black < 2f * MinGap)
		{
			return (black + white) / 2f;
		}

		float grey = black + ((white - black) * MathF.Pow(0.5f, gamma));
		return Math.Clamp(grey, black + MinGap, white - MinGap);
	}

	/// <summary>Applies the input handle row, [black, grey, white], to this adjustment.</summary>
	/// <param name="handles">The three input handle positions.</param>
	/// <returns>
	/// The normalized result. If black and white are where they were, the grey handle moved and only
	/// gamma changes; otherwise black or white moved and gamma is kept, so the grey handle follows.
	/// </returns>
	internal LevelsAdjustment WithInputHandles(ReadOnlySpan<float> handles)
	{
		bool blackStill = MathF.Abs(handles[0] - InputBlack) <= HandleTolerance;
		bool whiteStill = MathF.Abs(handles[2] - InputWhite) <= HandleTolerance;

		LevelsAdjustment result = blackStill && whiteStill
			? this with { Gamma = GammaFromGreyPoint(InputBlack, InputWhite, handles[1]) }
			: this with { InputBlack = handles[0], InputWhite = handles[2] };

		return result.Normalized();
	}

	/// <summary>Applies the output handle row, [outBlack, outWhite], to this adjustment.</summary>
	/// <param name="handles">The two output handle positions.</param>
	/// <returns>The normalized result.</returns>
	internal LevelsAdjustment WithOutputHandles(ReadOnlySpan<float> handles) =>
		(this with { OutputBlack = handles[0], OutputWhite = handles[1] }).Normalized();

	private static float Finite(float value, float fallback) => float.IsFinite(value) ? value : fallback;

	private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

	/// <summary>Widens a range narrower than <paramref name="gap"/>, keeping its low end unless that would pass one.</summary>
	private static (float Low, float High) OpenRange(float low, float high, float gap)
	{
		if (high - low >= gap)
		{
			return (low, high);
		}

		high = MathF.Min(1f, low + gap);
		low = high - gap;
		return (low, high);
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using ktsu.Semantics.Color;

/// <summary>
/// The setting of one colour-grading wheel: which way its trackball is pushed, how far, and where
/// its master slider sits.
/// </summary>
/// <param name="Hue">The direction of the push as a hue in degrees, <c>0</c> red, <c>120</c> green, <c>240</c> blue.</param>
/// <param name="Strength">How far the push goes, from <c>0</c> (neutral) to <c>1</c> (the rim).</param>
/// <param name="Master">The master slider, from <c>-1</c> to <c>1</c>: an equal offset to all three channels, which moves luminance without moving colour.</param>
/// <remarks>
/// A wheel at zero strength still carries a hue. That is deliberate: the hue of a neutral wheel
/// is the direction it was last pushed, so pulling the handle back through the centre and out
/// again does not snap it to red on the way.
/// </remarks>
public readonly record struct ColorWheelValue(float Hue, float Strength, float Master = 0.0f)
{
	/// <summary>How close to zero a master value has to be to count as zero.</summary>
	/// <remarks>
	/// A millionth of the slider's range, which is well under a pixel of any bar that could be drawn,
	/// so a master that reads as zero here also looks like zero on screen.
	/// </remarks>
	internal const float MasterTolerance = 1e-6f;

	/// <summary>Gets a wheel that does nothing: zero strength and master, pointing at red.</summary>
	public static ColorWheelValue Neutral { get; } = new(0.0f, 0.0f);

	/// <summary>Gets a value indicating whether the wheel has no effect.</summary>
	public bool IsNeutral => Strength <= 0.0f && MathF.Abs(Master) <= MasterTolerance;

	/// <summary>
	/// Returns the per-channel colour offset this wheel stands for: the colour of <see cref="Hue"/>
	/// with its mean removed and scaled by <see cref="Strength"/>, plus <see cref="Master"/> on every
	/// channel.
	/// </summary>
	/// <returns>A red, green and blue offset whose three channels sum to three times <see cref="Master"/>.</returns>
	/// <remarks>
	/// <para>
	/// Removing the mean is what makes the trackball a balance rather than a tint: pushing towards
	/// red raises red and lowers green and blue by the same total, so the ball moves colour without
	/// moving the average of the three channels. At full strength the largest channel moves by two
	/// thirds, whichever hue is chosen. Brightness is the master slider's job, and it moves all
	/// three channels together.
	/// </para>
	/// <para>
	/// How the offset is applied is the caller's business, since lift, gamma and gain are applied
	/// differently and no two grading pipelines agree on the exact curves. A typical use adds
	/// <c>offset * (1 - x)</c> for lift, raises to <c>1 / (1 + offset)</c> for gamma, and multiplies by
	/// <c>1 + offset</c> for gain, where <c>x</c> is the input channel.
	/// </para>
	/// </remarks>
	public Vector3 ToRgbOffset()
	{
		Vector3 rgb = HueToRgb(Hue);
		float mean = (rgb.X + rgb.Y + rgb.Z) / 3.0f;
		float strength = float.IsFinite(Strength) ? Math.Clamp(Strength, 0.0f, 1.0f) : 0.0f;
		float master = float.IsFinite(Master) ? Math.Clamp(Master, -1.0f, 1.0f) : 0.0f;
		return ((rgb - new Vector3(mean)) * strength) + new Vector3(master);
	}

	/// <summary>Returns the fully saturated, full-value colour of a hue in degrees.</summary>
	/// <param name="hue">The hue in degrees. Any value is accepted and wrapped into <c>[0, 360)</c>.</param>
	/// <returns>The colour in gamma-encoded sRGB.</returns>
	public static Srgb HueToSrgb(float hue) => new Hsv(WrapHue(hue), 1.0, 1.0).ToSrgb();

	/// <summary>Returns the fully saturated, full-value colour of a hue in degrees as a vector.</summary>
	/// <param name="hue">The hue in degrees. Any value is accepted and wrapped into <c>[0, 360)</c>.</param>
	/// <returns>The colour as gamma-encoded red, green and blue in <c>[0, 1]</c>.</returns>
	public static Vector3 HueToRgb(float hue)
	{
		Srgb srgb = HueToSrgb(hue);
		return new Vector3((float)srgb.R, (float)srgb.G, (float)srgb.B);
	}

	/// <summary>Wraps a hue in degrees into <c>[0, 360)</c>.</summary>
	/// <param name="hue">The hue in degrees.</param>
	/// <returns>The same direction as a value in <c>[0, 360)</c>, or <c>0</c> for a non-finite input.</returns>
	public static float WrapHue(float hue)
	{
		if (!float.IsFinite(hue))
		{
			return 0.0f;
		}

		float wrapped = hue % 360.0f;
		if (wrapped < 0.0f)
		{
			wrapped += 360.0f;
		}

		// A tiny negative input wraps to 360 exactly in single precision, which is outside the range.
		return wrapped >= 360.0f ? 0.0f : wrapped;
	}
}

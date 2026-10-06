// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A DAHDSR envelope. Times are in seconds, <see cref="Sustain"/> is a level in 0..1, tensions are in -1..1.
	/// Positive tension moves fast at the start of a segment and slowly at its end; 0 is linear.
	/// An ADSR is this with <see cref="Delay"/> and <see cref="Hold"/> at 0.
	/// </summary>
	/// <param name="Delay">Seconds of silence after note-on, before the attack starts.</param>
	/// <param name="Attack">Seconds to rise from 0 to full level.</param>
	/// <param name="Hold">Seconds held at full level before the decay starts.</param>
	/// <param name="Decay">Seconds to fall from full level to <paramref name="Sustain"/>.</param>
	/// <param name="Sustain">The level held while the note is down, in 0..1.</param>
	/// <param name="Release">Seconds to fall to 0 after note-off.</param>
	/// <param name="AttackTension">How the attack bends, in -1..1. 0 is linear.</param>
	/// <param name="DecayTension">How the decay bends, in -1..1. 0 is linear.</param>
	/// <param name="ReleaseTension">How the release bends, in -1..1. 0 is linear.</param>
	/// <remarks>
	/// <para>
	/// <b>The envelope carries its own evaluator.</b> <see cref="LevelAt"/> is the one definition of
	/// what these numbers mean, and <see cref="EnvelopeEditor"/> draws with it rather than with a
	/// copy, so the curve on screen is the curve a synth applying <see cref="LevelAt"/> produces —
	/// the same rule <see cref="CurveTrack"/> keeps by taking its sampler from the caller.
	/// </para>
	/// <para>
	/// The evaluator is defensive about values the editor would normalise: a time that is negative,
	/// NaN or infinite counts as 0, a NaN sustain as 1, and every tension and the sustain level are
	/// clamped. So the result is always in 0..1, whatever the fields hold.
	/// </para>
	/// </remarks>
	public readonly record struct Envelope(
		float Delay,
		float Attack,
		float Hold,
		float Decay,
		float Sustain,
		float Release,
		float AttackTension = 0f,
		float DecayTension = 0f,
		float ReleaseTension = 0f)
	{
		/// <summary>Below this magnitude a tension is linear, so the exponential form never divides by nearly zero.</summary>
		private const float LinearTension = 1e-4f;

		/// <summary>How steep the steepest tension is: the exponent at a tension of ±1.</summary>
		private const float Steepness = 6f;

		/// <summary>An ADSR with no delay and no hold.</summary>
		/// <param name="attack">Seconds to rise from 0 to full level.</param>
		/// <param name="decay">Seconds to fall from full level to <paramref name="sustain"/>.</param>
		/// <param name="sustain">The level held while the note is down, in 0..1.</param>
		/// <param name="release">Seconds to fall to 0 after note-off.</param>
		/// <returns>The envelope.</returns>
		public static Envelope Adsr(float attack, float decay, float sustain, float release) => new(0f, attack, 0f, decay, sustain, release);

		/// <summary>Time from note-on until the sustain level is reached: Delay + Attack + Hold + Decay.</summary>
		public float TimeToSustain => Delay + Attack + Hold + Decay;

		/// <summary>
		/// The envelope's level at <paramref name="time"/> seconds after note-on, with the note released at
		/// <paramref name="noteOffTime"/> (never, by default). Release starts from the level at note-off, not from Sustain.
		/// </summary>
		/// <param name="time">Seconds since note-on.</param>
		/// <param name="noteOffTime">Seconds from note-on to note-off.</param>
		/// <returns>The level, in 0..1. 0 for a negative or NaN <paramref name="time"/>.</returns>
		/// <remarks>
		/// Starting the release from the level at note-off rather than from <see cref="Sustain"/> is
		/// what keeps a note released during its attack from jumping up to the sustain level first.
		/// A zero-length segment is skipped, so a zero attack jumps straight to full level.
		/// </remarks>
		public float LevelAt(float time, float noteOffTime = float.PositiveInfinity)
		{
			if (float.IsNaN(time) || time < 0f)
			{
				return 0f;
			}

			// A NaN note-off never arrives, the same as the default.
			if (float.IsNaN(noteOffTime) || time < noteOffTime)
			{
				return HeldLevel(time);
			}

			float release = Length(Release);
			if (release <= 0f)
			{
				return 0f;
			}

			float u = (time - noteOffTime) / release;
			if (u >= 1f)
			{
				return 0f;
			}

			float start = HeldLevel(noteOffTime);
			return Math.Clamp(start * (1f - Curve(u, ReleaseTension)), 0f, 1f);
		}

		/// <summary>The shared segment shape: 0 at t = 0, 1 at t = 1, bent by <paramref name="tension"/>. t and tension are clamped.</summary>
		/// <param name="t">How far through the segment, in 0..1. NaN counts as 0.</param>
		/// <param name="tension">The bend, in -1..1. NaN counts as 0, which is linear.</param>
		/// <returns>The shaped fraction, in 0..1 and monotone in <paramref name="t"/>.</returns>
		/// <remarks>
		/// The exponential <c>(e^(c·t) − 1) / (e^c − 1)</c> with <c>c = −6·tension</c>. It meets both
		/// ends exactly for every tension, which is what lets three segments of different tensions
		/// join without a step, and it is its own mirror image under a sign change of the tension.
		/// </remarks>
		public static float Curve(float t, float tension)
		{
			if (float.IsNaN(t))
			{
				return 0f;
			}

			t = Math.Clamp(t, 0f, 1f);
			float k = float.IsNaN(tension) ? 0f : Math.Clamp(tension, -1f, 1f);

			if (MathF.Abs(k) < LinearTension)
			{
				return t;
			}

			float c = -Steepness * k;
			float shaped = (MathF.Exp(c * t) - 1f) / (MathF.Exp(c) - 1f);
			return Math.Clamp(shaped, 0f, 1f);
		}

		/// <summary>The level while the note is held, before any release.</summary>
		private float HeldLevel(float time)
		{
			float t = time;

			float delay = Length(Delay);
			if (t < delay)
			{
				return 0f;
			}

			t -= delay;

			float attack = Length(Attack);
			if (t < attack)
			{
				return Curve(t / attack, AttackTension);
			}

			t -= attack;

			float hold = Length(Hold);
			if (t < hold)
			{
				return 1f;
			}

			t -= hold;

			float sustain = SustainLevel;
			float decay = Length(Decay);
			if (t < decay)
			{
				return Math.Clamp(1f + ((sustain - 1f) * Curve(t / decay, DecayTension)), 0f, 1f);
			}

			return sustain;
		}

		/// <summary>Gets the sustain level clamped to 0..1, with NaN read as full level, as the editor normalises it.</summary>
		internal float SustainLevel => float.IsNaN(Sustain) ? 1f : Math.Clamp(Sustain, 0f, 1f);

		/// <summary>A segment length the evaluator can use: a time that cannot be drawn is no time at all.</summary>
		internal static float Length(float seconds) => float.IsFinite(seconds) && seconds > 0f ? seconds : 0f;
	}
}

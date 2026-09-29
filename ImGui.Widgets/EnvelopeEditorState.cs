// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>A draggable part of an <see cref="EnvelopeEditor"/>.</summary>
	/// <remarks>
	/// The breakpoints come before the tension handles, and that order is load-bearing: picking
	/// resolves a tie to the earlier handle, which is how a breakpoint beats a tension handle
	/// sitting on top of it.
	/// </remarks>
	internal enum EnvelopeHandle
	{
		/// <summary>No handle.</summary>
		None,

		/// <summary>The end of the delay, where the attack starts.</summary>
		DelayEnd,

		/// <summary>The top of the attack.</summary>
		AttackPeak,

		/// <summary>The end of the hold, where the decay starts.</summary>
		HoldEnd,

		/// <summary>The end of the decay, at the sustain level.</summary>
		DecayEnd,

		/// <summary>The end of the drawn sustain plateau, where the release starts.</summary>
		SustainEnd,

		/// <summary>The end of the release, at level 0.</summary>
		ReleaseEnd,

		/// <summary>The midpoint of the attack curve.</summary>
		AttackTension,

		/// <summary>The midpoint of the decay curve.</summary>
		DecayTension,

		/// <summary>The midpoint of the release curve.</summary>
		ReleaseTension,
	}

	/// <summary>
	/// The interaction behind an <see cref="EnvelopeEditor"/>: where each handle is drawn, which one
	/// a press picks, what dragging it does to the envelope, and how invalid values are repaired.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="CurveTrackState"/>. Callers hand in the box and
	/// the pointer in pixels, which is what lets every rule here be tested without a context.
	/// </remarks>
	internal sealed class EnvelopeEditorState
	{
		/// <summary>How much of the time span the sustain plateau is drawn across.</summary>
		/// <remarks>
		/// Sustain has no duration — it lasts as long as the note is held — so it is drawn a fixed
		/// width, and the timed segments share what is left: the budget.
		/// </remarks>
		public const float PlateauFraction = 0.15f;

		/// <summary>The number of handles, which is every <see cref="EnvelopeHandle"/> but <see cref="EnvelopeHandle.None"/>.</summary>
		public const int HandleCount = 9;

		/// <summary>
		/// A change smaller than this fraction of a pixel is taken as no change. Mapping a length to
		/// pixels and back rounds, and a press that does not move must not report an edit.
		/// </summary>
		private const float PixelTolerance = 0.01f;

		private float tensionAtActivation;
		private float pointerYAtActivation;
		private bool tensionCaptured;

		/// <summary>Gets the handle being dragged.</summary>
		public EnvelopeHandle Active { get; private set; }

		/// <summary>Gets the offset from the pointer to the grabbed handle, recorded when it was grabbed.</summary>
		/// <remarks>
		/// Dragging moves the handle by as much as the pointer moves rather than snapping it under
		/// the pointer, so grabbing a handle off-centre does not make it jump.
		/// </remarks>
		public Vector2 GrabOffset { get; private set; }

		/// <summary>Gets the index of a handle in the span <see cref="GetHandlePositions"/> fills.</summary>
		/// <param name="handle">Any handle but <see cref="EnvelopeHandle.None"/>.</param>
		/// <returns>The index.</returns>
		public static int IndexOf(EnvelopeHandle handle) => (int)handle - 1;

		/// <summary>Gets whether a handle is a tension handle rather than a breakpoint.</summary>
		/// <param name="handle">The handle.</param>
		/// <returns><see langword="true"/> for the three tension handles.</returns>
		public static bool IsTension(EnvelopeHandle handle) =>
			handle is EnvelopeHandle.AttackTension or EnvelopeHandle.DecayTension or EnvelopeHandle.ReleaseTension;

		/// <summary>Gets the display time at which the drawn sustain plateau ends and the release begins.</summary>
		/// <param name="e">The envelope.</param>
		/// <param name="timeSpan">Seconds across the full width.</param>
		/// <returns>Seconds from the left edge.</returns>
		public static float SustainEndTime(Envelope e, float timeSpan) =>
			Envelope.Length(e.Delay) + Envelope.Length(e.Attack) + Envelope.Length(e.Hold) + Envelope.Length(e.Decay)
			+ (PlateauFraction * timeSpan);

		/// <summary>Computes where every handle is drawn.</summary>
		/// <param name="e">The envelope.</param>
		/// <param name="timeSpan">Seconds across the full width.</param>
		/// <param name="min">The top-left of the box.</param>
		/// <param name="size">The size of the box.</param>
		/// <param name="positions">Receives 9 positions, in <see cref="EnvelopeHandle"/> order without <see cref="EnvelopeHandle.None"/>.</param>
		public static void GetHandlePositions(Envelope e, float timeSpan, Vector2 min, Vector2 size, Span<Vector2> positions)
		{
			if (positions.Length < HandleCount)
			{
				throw new ArgumentException($"Needs room for {HandleCount} handle positions.", nameof(positions));
			}

			float delay = Envelope.Length(e.Delay);
			float attack = Envelope.Length(e.Attack);
			float hold = Envelope.Length(e.Hold);
			float decay = Envelope.Length(e.Decay);
			float release = Envelope.Length(e.Release);
			float sustain = e.SustainLevel;

			float attackPeak = delay + attack;
			float holdEnd = attackPeak + hold;
			float decayEnd = holdEnd + decay;
			float sustainEnd = decayEnd + (PlateauFraction * timeSpan);
			float releaseEnd = sustainEnd + release;

			Vector2 At(float time, float level) =>
				new(min.X + (time / timeSpan * size.X), min.Y + ((1f - level) * size.Y));

			positions[IndexOf(EnvelopeHandle.DelayEnd)] = At(delay, 0f);
			positions[IndexOf(EnvelopeHandle.AttackPeak)] = At(attackPeak, 1f);
			positions[IndexOf(EnvelopeHandle.HoldEnd)] = At(holdEnd, 1f);
			positions[IndexOf(EnvelopeHandle.DecayEnd)] = At(decayEnd, sustain);
			positions[IndexOf(EnvelopeHandle.SustainEnd)] = At(sustainEnd, sustain);
			positions[IndexOf(EnvelopeHandle.ReleaseEnd)] = At(releaseEnd, 0f);

			// Each tension handle sits on its own curve at the segment's midpoint, so dragging it
			// visibly drags the curve. A segment with no length has no midpoint to show, and its
			// handle sits on the breakpoint it collapses to.
			positions[IndexOf(EnvelopeHandle.AttackTension)] = attack > 0f
				? At(delay + (attack * 0.5f), Envelope.Curve(0.5f, e.AttackTension))
				: positions[IndexOf(EnvelopeHandle.AttackPeak)];
			positions[IndexOf(EnvelopeHandle.DecayTension)] = decay > 0f
				? At(holdEnd + (decay * 0.5f), 1f + ((sustain - 1f) * Envelope.Curve(0.5f, e.DecayTension)))
				: positions[IndexOf(EnvelopeHandle.DecayEnd)];
			positions[IndexOf(EnvelopeHandle.ReleaseTension)] = release > 0f
				? At(sustainEnd + (release * 0.5f), sustain * (1f - Envelope.Curve(0.5f, e.ReleaseTension)))
				: positions[IndexOf(EnvelopeHandle.ReleaseEnd)];
		}

		/// <summary>Finds the handle nearest the pointer, if one is within reach.</summary>
		/// <param name="positions">The handle positions, as <see cref="GetHandlePositions"/> fills them. A NaN position cannot be picked.</param>
		/// <param name="pointer">The pointer.</param>
		/// <param name="grabRadius">How far from a handle still grabs it, in pixels.</param>
		/// <param name="includeDelayAndHold">Whether the delay and hold handles can be picked.</param>
		/// <returns>The nearest handle in reach, or <see cref="EnvelopeHandle.None"/>.</returns>
		/// <remarks>
		/// Handles are tried in enum order and only a strictly nearer one replaces the best so far,
		/// so a tie goes to the earlier handle — and every breakpoint is earlier than every tension
		/// handle. Handles stacked on one point, as in an envelope of all zeros, therefore resolve to
		/// the first breakpoint among them.
		/// </remarks>
		public static EnvelopeHandle Pick(ReadOnlySpan<Vector2> positions, Vector2 pointer, float grabRadius, bool includeDelayAndHold)
		{
			EnvelopeHandle best = EnvelopeHandle.None;
			float bestDistance = float.MaxValue;
			int count = Math.Min(positions.Length, HandleCount);

			for (int i = 0; i < count; i++)
			{
				EnvelopeHandle handle = (EnvelopeHandle)(i + 1);
				if (!includeDelayAndHold && handle is EnvelopeHandle.DelayEnd or EnvelopeHandle.HoldEnd)
				{
					continue;
				}

				// NaN compares false both ways, so a position marked NaN is never in reach.
				float distance = Vector2.Distance(positions[i], pointer);
				if (distance <= grabRadius && distance < bestDistance)
				{
					bestDistance = distance;
					best = handle;
				}
			}

			return best;
		}

		/// <summary>Grabs the handle under the pointer, if there is one.</summary>
		/// <param name="positions">The handle positions.</param>
		/// <param name="pointer">The pointer.</param>
		/// <param name="grabRadius">How far from a handle still grabs it, in pixels.</param>
		/// <param name="includeDelayAndHold">Whether the delay and hold handles can be grabbed.</param>
		/// <returns><see langword="true"/> if a handle was grabbed.</returns>
		public bool Activate(ReadOnlySpan<Vector2> positions, Vector2 pointer, float grabRadius, bool includeDelayAndHold)
		{
			Active = Pick(positions, pointer, grabRadius, includeDelayAndHold);
			GrabOffset = Active == EnvelopeHandle.None ? Vector2.Zero : positions[IndexOf(Active)] - pointer;
			pointerYAtActivation = pointer.Y;
			tensionAtActivation = 0f;
			tensionCaptured = false;
			return Active != EnvelopeHandle.None;
		}

		/// <summary>Moves the grabbed handle to follow the pointer.</summary>
		/// <param name="e">The envelope, replaced when the drag changes it.</param>
		/// <param name="pointer">The pointer.</param>
		/// <param name="timeSpan">Seconds across the full width.</param>
		/// <param name="min">The top-left of the box.</param>
		/// <param name="size">The size of the box.</param>
		/// <returns><see langword="true"/> if the envelope changed.</returns>
		/// <remarks>
		/// A time handle sets only the segment that ends at it, so the breakpoints after it move with
		/// it rather than being squeezed. The timed segments together may not outgrow the width left
		/// once the sustain plateau is drawn; a segment already past that, because the envelope was
		/// handed in longer than the box, can be shortened but not lengthened.
		/// </remarks>
		public bool Drag(ref Envelope e, Vector2 pointer, float timeSpan, Vector2 min, Vector2 size)
		{
			if (Active == EnvelopeHandle.None || size.X <= 0f || size.Y <= 0f || float.IsNaN(timeSpan) || timeSpan <= 0f)
			{
				return false;
			}

			if (IsTension(Active))
			{
				return DragTension(ref e, pointer, size);
			}

			Vector2 target = pointer + GrabOffset;
			float time = (target.X - min.X) / size.X * timeSpan;
			float level = Math.Clamp(1f - ((target.Y - min.Y) / size.Y), 0f, 1f);

			float secondsPerPixel = timeSpan / size.X;
			float levelPerPixel = 1f / size.Y;

			Envelope moved = Active switch
			{
				EnvelopeHandle.DelayEnd => e with { Delay = Retime(e, e.Delay, 0f, time, timeSpan, secondsPerPixel) },
				EnvelopeHandle.AttackPeak => e with { Attack = Retime(e, e.Attack, Envelope.Length(e.Delay), time, timeSpan, secondsPerPixel) },
				EnvelopeHandle.HoldEnd => e with
				{
					Hold = Retime(e, e.Hold, Envelope.Length(e.Delay) + Envelope.Length(e.Attack), time, timeSpan, secondsPerPixel),
				},
				EnvelopeHandle.DecayEnd => e with
				{
					Decay = Retime(
						e,
						e.Decay,
						Envelope.Length(e.Delay) + Envelope.Length(e.Attack) + Envelope.Length(e.Hold),
						time,
						timeSpan,
						secondsPerPixel),
					Sustain = Relevel(e.Sustain, level, levelPerPixel),
				},
				EnvelopeHandle.SustainEnd => e with { Sustain = Relevel(e.Sustain, level, levelPerPixel) },
				EnvelopeHandle.ReleaseEnd => e with { Release = Retime(e, e.Release, SustainEndTime(e, timeSpan), time, timeSpan, secondsPerPixel) },
				_ => e,
			};

			if (moved == e)
			{
				return false;
			}

			e = moved;
			return true;
		}

		/// <summary>Sets a tension back to linear.</summary>
		/// <param name="e">The envelope, replaced when the tension was not already linear.</param>
		/// <param name="handle">The tension handle. Any other handle does nothing.</param>
		/// <returns><see langword="true"/> if the tension changed.</returns>
		public static bool ResetTension(ref Envelope e, EnvelopeHandle handle)
		{
			Envelope reset = handle switch
			{
				EnvelopeHandle.AttackTension => e with { AttackTension = 0f },
				EnvelopeHandle.DecayTension => e with { DecayTension = 0f },
				EnvelopeHandle.ReleaseTension => e with { ReleaseTension = 0f },
				_ => e,
			};

			if (reset == e)
			{
				return false;
			}

			e = reset;
			return true;
		}

		/// <summary>Repairs every value the editor cannot draw.</summary>
		/// <param name="e">The envelope, replaced when anything was repaired.</param>
		/// <returns><see langword="true"/> if anything changed.</returns>
		/// <remarks>
		/// A NaN, negative or infinite time becomes 0: a time that cannot be drawn is not a time. A
		/// NaN sustain becomes full level and any other is clamped to 0..1, and a NaN tension becomes
		/// linear and any other is clamped to -1..1. An envelope longer than the box is left alone:
		/// it is drawn clipped, and only a drag shortens it.
		/// </remarks>
		public static bool Normalize(ref Envelope e)
		{
			Envelope normal = new(
				Envelope.Length(e.Delay),
				Envelope.Length(e.Attack),
				Envelope.Length(e.Hold),
				Envelope.Length(e.Decay),
				e.SustainLevel,
				Envelope.Length(e.Release),
				Tension(e.AttackTension),
				Tension(e.DecayTension),
				Tension(e.ReleaseTension));

			if (normal == e)
			{
				return false;
			}

			e = normal;
			return true;
		}

		/// <summary>Lets go of the grabbed handle.</summary>
		public void Release() => Active = EnvelopeHandle.None;

		private static float Tension(float tension) => float.IsNaN(tension) ? 0f : Math.Clamp(tension, -1f, 1f);

		/// <summary>The new length of a segment whose end is dragged to <paramref name="time"/>.</summary>
		private static float Retime(Envelope e, float current, float start, float time, float timeSpan, float secondsPerPixel)
		{
			float length = Envelope.Length(current);
			float others = Envelope.Length(e.Delay) + Envelope.Length(e.Attack) + Envelope.Length(e.Hold)
				+ Envelope.Length(e.Decay) + Envelope.Length(e.Release) - length;
			float budget = (1f - PlateauFraction) * timeSpan;

			// A segment already past the budget may shrink towards it but never grow further.
			float limit = MathF.Max(MathF.Max(budget - others, 0f), length);

			float wanted = Math.Clamp(time - start, 0f, limit);
			return MathF.Abs(wanted - length) < PixelTolerance * secondsPerPixel ? current : wanted;
		}

		private static float Relevel(float current, float level, float levelPerPixel) =>
			MathF.Abs(level - current) < PixelTolerance * levelPerPixel ? current : level;

		private bool DragTension(ref Envelope e, Vector2 pointer, Vector2 size)
		{
			// Activate is handed positions rather than the envelope, so the tension the drag starts
			// from is read on the drag's first frame, which the widget runs on the activation frame.
			if (!tensionCaptured)
			{
				tensionAtActivation = Active switch
				{
					EnvelopeHandle.AttackTension => Tension(e.AttackTension),
					EnvelopeHandle.DecayTension => Tension(e.DecayTension),
					_ => Tension(e.ReleaseTension),
				};
				tensionCaptured = true;
			}

			// Only vertical movement bends a curve. Up raises the midpoint of whichever segment it
			// is: a rising attack gets more positive, a falling decay or release more negative.
			float up = pointerYAtActivation - pointer.Y;
			float delta = up / (size.Y * 0.5f);

			Envelope bent = Active switch
			{
				EnvelopeHandle.AttackTension => e with { AttackTension = Math.Clamp(tensionAtActivation + delta, -1f, 1f) },
				EnvelopeHandle.DecayTension => e with { DecayTension = Math.Clamp(tensionAtActivation - delta, -1f, 1f) },
				EnvelopeHandle.ReleaseTension => e with { ReleaseTension = Math.Clamp(tensionAtActivation - delta, -1f, 1f) },
				_ => e,
			};

			if (bent == e)
			{
				return false;
			}

			e = bent;
			return true;
		}
	}
}

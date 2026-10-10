// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an envelope and lets its breakpoints and tensions be dragged. Reserves <paramref name="size"/> at the cursor.
	/// </summary>
	/// <param name="label">ID and probe name.</param>
	/// <param name="envelope">The envelope, replaced when edited.</param>
	/// <param name="size">A component &lt;= 0 means: width = available content width, height = 8 text line heights.</param>
	/// <param name="timeSpan">Seconds across the full width. The sustain plateau is drawn 15% of it wide.</param>
	/// <param name="showDelayAndHold">False hides the delay and hold handles (an ADSR editor); their values still apply.</param>
	/// <returns>True on a frame where the envelope's value actually changed.</returns>
	/// <remarks>
	/// <para>
	/// <b>The curve on screen is <see cref="Envelope.LevelAt"/>.</b> The widget plots the envelope's
	/// own evaluator rather than a drawing of its own, so what the user shapes is exactly what a synth
	/// applying <see cref="Envelope.LevelAt"/> plays — the rule <see cref="CurveTrack"/> keeps by
	/// taking its sampler from the caller.
	/// </para>
	/// <para>
	/// Drag a breakpoint to retime the segment that ends at it; the breakpoints after it move along.
	/// The decay's end also sets the sustain level, and the end of the sustain plateau sets only the
	/// level, because sustain has no duration and is drawn a fixed 15% of the width. Drag a tension
	/// handle up or down to bend its segment, and right-click one to make the segment linear again.
	/// </para>
	/// <para>
	/// Every frame starts by repairing values that cannot be drawn — a NaN or negative time, a NaN
	/// sustain or tension — and a frame that repaired anything reports a change. An envelope longer
	/// than the box is drawn clipped at the right edge; its handles past the edge cannot be grabbed,
	/// and dragging a visible one can shorten it but never lengthen it further.
	/// </para>
	/// </remarks>
	public static bool EnvelopeEditor(string label, ref Envelope envelope, Vector2 size = default, float timeSpan = 5f, bool showDelayAndHold = true) =>
		EnvelopeEditorImpl.Draw(label, ref envelope, size, timeSpan, showDelayAndHold);

	internal static class EnvelopeEditorImpl
	{
		private static readonly Dictionary<uint, EnvelopeEditorState> States = [];

		/// <summary>The probe name of each handle, in <see cref="EnvelopeHandle"/> order without <see cref="EnvelopeHandle.None"/>.</summary>
		private static readonly string[] HandleNames =
			["delay", "attack", "hold", "decay", "sustain", "release", "attackTension", "decayTension", "releaseTension"];

		/// <summary>How far apart the plotted samples are, in pixels. The same spacing <see cref="CurveTrack"/> plots at.</summary>
		private const float SampleSpacing = 2.0f;

		/// <summary>How far outside the box a handle may sit and still count as visible, in pixels.</summary>
		/// <remarks>
		/// The release end sits exactly on the bottom edge and the attack peak on the top one, so an
		/// exact containment test would hide them whenever rounding put them a hair outside.
		/// </remarks>
		private const float EdgeTolerance = 0.5f;

		public static bool Draw(string label, ref Envelope envelope, Vector2 size, float timeSpan, bool showDelayAndHold)
		{
			Ensure.NotNull(label);

			Vector2 available = ImGui.GetContentRegionAvail();
			float width = size.X > 0f ? size.X : available.X;
			float height = size.Y > 0f ? size.Y : ImGui.GetTextLineHeight() * 8f;

			if (!float.IsFinite(width) || !float.IsFinite(height) || width < 1f || height < 1f)
			{
				ImGui.Dummy(new Vector2(1f, 1f));
				ImGuiProbes.MarkItem(label);
				return false;
			}

			ImGui.InvisibleButton(label, new Vector2(width, height));
			ImGuiProbes.MarkItem(label);

			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			Vector2 box = max - min;

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			// A time span that maps no seconds onto the box leaves nothing to place a handle at, so
			// only the frame is drawn — and the envelope is left untouched, not even normalised.
			if (!float.IsFinite(timeSpan) || timeSpan <= 0f)
			{
				drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));
				drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
				return false;
			}

			bool changed = EnvelopeEditorState.Normalize(ref envelope);

			float radius = MathF.Max(ImGui.GetTextLineHeight() * 0.5f, 6f);

			Span<Vector2> positions = stackalloc Vector2[EnvelopeEditorState.HandleCount];
			Span<Vector2> reachable = stackalloc Vector2[EnvelopeEditorState.HandleCount];
			Locate(envelope, timeSpan, min, max, box, showDelayAndHold, positions, reachable);

			uint id = ImGui.GetID(label);
			EnvelopeHandle hot = Interact(id, ref envelope, reachable, timeSpan, min, box, radius, showDelayAndHold, ref changed);

			// The drag may have moved every handle after the grabbed one, so draw and mark where
			// they are now rather than where they were when the frame began.
			Locate(envelope, timeSpan, min, max, box, showDelayAndHold, positions, reachable);

			drawList.PushClipRect(min, max, true);
			DrawBackground(drawList, min, max, timeSpan, colors);
			DrawCurve(drawList, envelope, timeSpan, min, max, colors);
			drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
			drawList.PopClipRect();

			// The handles are drawn outside the box's clip. The attack peak is always at full level
			// and the start and release ends at silence, so those handles sit on the box's top and
			// bottom edges, and clipped to the box half of each grab target would be hidden.
			DrawHandles(drawList, reachable, radius, hot, colors);

			MarkHandles(label, reachable, radius, min, max);

			if (hot != EnvelopeHandle.None)
			{
				ImGui.SetMouseCursor(CursorFor(hot));
				ImGui.BeginTooltip();
				ImGui.TextUnformatted(Describe(envelope, hot));
				ImGui.EndTooltip();
			}

			return changed;
		}

		/// <summary>Formats a time the way the tooltip shows it: milliseconds below a second, seconds from one.</summary>
		internal static string FormatTime(float seconds) =>
			seconds < 1f
				? string.Format(CultureInfo.InvariantCulture, "{0:0} ms", seconds * 1000f)
				: string.Format(CultureInfo.InvariantCulture, "{0:0.00} s", seconds);

		/// <summary>The tooltip text for a handle.</summary>
		internal static string Describe(Envelope e, EnvelopeHandle handle)
		{
			string sustain = string.Format(CultureInfo.InvariantCulture, "Sustain {0:0}%", e.SustainLevel * 100f);
			return handle switch
			{
				EnvelopeHandle.DelayEnd => $"Delay {FormatTime(e.Delay)}",
				EnvelopeHandle.AttackPeak => $"Attack {FormatTime(e.Attack)}",
				EnvelopeHandle.HoldEnd => $"Hold {FormatTime(e.Hold)}",
				EnvelopeHandle.DecayEnd => $"Decay {FormatTime(e.Decay)} · {sustain}",
				EnvelopeHandle.SustainEnd => sustain,
				EnvelopeHandle.ReleaseEnd => $"Release {FormatTime(e.Release)}",
				EnvelopeHandle.AttackTension => $"Attack tension {FormatTension(e.AttackTension)}",
				EnvelopeHandle.DecayTension => $"Decay tension {FormatTension(e.DecayTension)}",
				EnvelopeHandle.ReleaseTension => $"Release tension {FormatTension(e.ReleaseTension)}",
				_ => string.Empty,
			};
		}

		private static string FormatTension(float tension) =>
			tension.ToString("+0.00;-0.00;+0.00", CultureInfo.InvariantCulture);

		private static ImGuiMouseCursor CursorFor(EnvelopeHandle handle) => handle switch
		{
			EnvelopeHandle.DecayEnd => ImGuiMouseCursor.ResizeAll,
			EnvelopeHandle.SustainEnd or EnvelopeHandle.AttackTension or EnvelopeHandle.DecayTension or EnvelopeHandle.ReleaseTension => ImGuiMouseCursor.ResizeNs,
			_ => ImGuiMouseCursor.ResizeEw,
		};

		/// <summary>
		/// Places every handle, and fills <paramref name="reachable"/> with the same positions except
		/// NaN for any handle that is hidden or outside the box — which <see cref="EnvelopeEditorState.Pick"/>
		/// never picks, so what cannot be seen cannot be grabbed.
		/// </summary>
		private static void Locate(
			Envelope envelope,
			float timeSpan,
			Vector2 min,
			Vector2 max,
			Vector2 box,
			bool showDelayAndHold,
			Span<Vector2> positions,
			Span<Vector2> reachable)
		{
			EnvelopeEditorState.GetHandlePositions(envelope, timeSpan, min, box, positions);

			for (int i = 0; i < positions.Length; i++)
			{
				EnvelopeHandle handle = (EnvelopeHandle)(i + 1);
				Vector2 position = positions[i];
				bool hidden = !showDelayAndHold && handle is EnvelopeHandle.DelayEnd or EnvelopeHandle.HoldEnd;
				bool inside = position.X >= min.X - EdgeTolerance && position.X <= max.X + EdgeTolerance
					&& position.Y >= min.Y - EdgeTolerance && position.Y <= max.Y + EdgeTolerance;
				reachable[i] = hidden || !inside ? new Vector2(float.NaN, float.NaN) : position;
			}
		}

		private static EnvelopeHandle Interact(
			uint id,
			ref Envelope envelope,
			ReadOnlySpan<Vector2> reachable,
			float timeSpan,
			Vector2 min,
			Vector2 box,
			float radius,
			bool showDelayAndHold,
			ref bool changed)
		{
			Vector2 pointer = ImGui.GetIO().MousePos;

			// A press away from every handle grabs nothing, and holds no state for the rest of it.
			if (ImGui.IsItemActivated())
			{
				EnvelopeEditorState grabbing = new();
				if (grabbing.Activate(reachable, pointer, radius, showDelayAndHold))
				{
					States[id] = grabbing;
				}
			}

			EnvelopeHandle active = EnvelopeHandle.None;
			if (States.TryGetValue(id, out EnvelopeEditorState? state))
			{
				if (ImGui.IsItemActive())
				{
					changed |= state.Drag(ref envelope, pointer, timeSpan, min, box);
					active = state.Active;
				}
				else
				{
					// Deactivated this frame, or a drag whose item stopped being submitted: either
					// way the gesture is over.
					state.Release();
					States.Remove(id);
				}
			}

			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				EnvelopeHandle picked = EnvelopeEditorState.Pick(reachable, pointer, radius, showDelayAndHold);
				changed |= EnvelopeEditorState.ResetTension(ref envelope, picked);
			}

			if (active != EnvelopeHandle.None)
			{
				return active;
			}

			return ImGui.IsItemHovered() ? EnvelopeEditorState.Pick(reachable, pointer, radius, showDelayAndHold) : EnvelopeHandle.None;
		}

		private static void DrawBackground(ImDrawListPtr drawList, Vector2 min, Vector2 max, float timeSpan, ReadOnlySpan<Vector4> colors)
		{
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			uint grid = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
			float step = MathF.Pow(10f, MathF.Floor(MathF.Log10(timeSpan)));
			float width = max.X - min.X;

			// A power of ten at or below the span always gives between one and ten lines.
			for (int i = 1; i * step < timeSpan; i++)
			{
				float x = min.X + (i * step / timeSpan * width);
				drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), grid);
			}

			float middle = min.Y + ((max.Y - min.Y) * 0.5f);
			drawList.AddLine(new Vector2(min.X, middle), new Vector2(max.X, middle), grid);
		}

		/// <summary>The level drawn at a display time: held up to the plateau's end, then released from it.</summary>
		private static float DisplayLevel(Envelope envelope, float time, float timeToSustain, float sustainEnd) =>
			time <= sustainEnd
				? envelope.LevelAt(MathF.Min(time, timeToSustain))
				: envelope.LevelAt(timeToSustain + (time - sustainEnd), noteOffTime: timeToSustain);

		private static void DrawCurve(ImDrawListPtr drawList, Envelope envelope, float timeSpan, Vector2 min, Vector2 max, ReadOnlySpan<Vector4> colors)
		{
			float width = max.X - min.X;
			float height = max.Y - min.Y;
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);

			float delay = envelope.Delay;
			float attackPeak = delay + envelope.Attack;
			float holdEnd = attackPeak + envelope.Hold;
			float timeToSustain = holdEnd + envelope.Decay;
			float sustainEnd = timeToSustain + (EnvelopeEditorState.PlateauFraction * timeSpan);
			float releaseEnd = sustainEnd + envelope.Release;
			float sustain = envelope.SustainLevel;

			// Each breakpoint with the level its segment arrives at. Sampling alone would step over a
			// zero-length segment — the jump straight to full level of a zero attack — so every
			// breakpoint is drawn exactly: once as its segment arrives, once as the next one leaves.
			ReadOnlySpan<Vector2> breakpoints =
			[
				new(delay, 0f),
				new(attackPeak, 1f),
				new(holdEnd, 1f),
				new(timeToSustain, sustain),
				new(sustainEnd, sustain),
				new(releaseEnd, 0f),
			];

			Vector2 ToScreen(float time, float level) => new(min.X + (time / timeSpan * width), max.Y - (level * height));

			int steps = Math.Max(2, (int)MathF.Ceiling(width / SampleSpacing));
			int next = 0;
			Vector2 previous = ToScreen(0f, DisplayLevel(envelope, 0f, timeToSustain, sustainEnd));

			for (int i = 1; i <= steps; i++)
			{
				float time = i / (float)steps * timeSpan;

				while (next < breakpoints.Length && breakpoints[next].X <= time)
				{
					float at = breakpoints[next].X;
					Vector2 arrive = ToScreen(at, breakpoints[next].Y);
					Vector2 leave = ToScreen(at, DisplayLevel(envelope, at, timeToSustain, sustainEnd));
					drawList.AddLine(previous, arrive, color, 2f);
					drawList.AddLine(arrive, leave, color, 2f);
					previous = leave;
					next++;
				}

				Vector2 point = ToScreen(time, DisplayLevel(envelope, time, timeToSustain, sustainEnd));
				drawList.AddLine(previous, point, color, 2f);
				previous = point;
			}

			// The plateau is where the curve stands for "as long as the note is held", which no
			// sample can show, so it is marked with a dashed line just above it.
			uint dash = ImGui.GetColorU32(colors[(int)ImGuiCol.TextDisabled]);
			float y = max.Y - (sustain * height) - 3f;
			float from = ToScreen(timeToSustain, 0f).X;
			float to = MathF.Min(ToScreen(sustainEnd, 0f).X, max.X);
			for (float x = from; x < to; x += 8f)
			{
				drawList.AddLine(new Vector2(x, y), new Vector2(MathF.Min(x + 4f, to), y), dash);
			}
		}

		private static void DrawHandles(ImDrawListPtr drawList, ReadOnlySpan<Vector2> reachable, float radius, EnvelopeHandle hot, ReadOnlySpan<Vector4> colors)
		{
			uint grab = ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrab]);
			uint grabActive = ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrabActive]);

			// Tension handles first, so a breakpoint sitting on one — a zero-length segment — is
			// drawn over it, the same way it wins the pick.
			for (int i = 0; i < reachable.Length; i++)
			{
				EnvelopeHandle handle = (EnvelopeHandle)(i + 1);
				if (!EnvelopeEditorState.IsTension(handle) || float.IsNaN(reachable[i].X))
				{
					continue;
				}

				if (handle == hot)
				{
					drawList.AddCircleFilled(reachable[i], radius * 0.6f, grabActive, 16);
				}
				else
				{
					drawList.AddCircle(reachable[i], radius * 0.6f, grab, 16, 1.5f);
				}
			}

			for (int i = 0; i < reachable.Length; i++)
			{
				EnvelopeHandle handle = (EnvelopeHandle)(i + 1);
				if (EnvelopeEditorState.IsTension(handle) || float.IsNaN(reachable[i].X))
				{
					continue;
				}

				drawList.AddCircleFilled(reachable[i], radius, handle == hot ? grabActive : grab, 16);
			}
		}

		private static void MarkHandles(string label, ReadOnlySpan<Vector2> reachable, float radius, Vector2 min, Vector2 max)
		{
			// Building the names allocates, so it is skipped entirely unless a probe is listening.
			if (!ImGuiProbes.IsRecording)
			{
				return;
			}

			// Each region is clipped to the box, like the handle drawn in it. The release end sits on
			// the bottom edge, and the half of it below the box is neither drawn nor inside the item,
			// so a press there reaches nothing: the centre of the clipped region is a point that
			// actually grabs the handle, where the centre of the whole square would not.
			Vector2 extent = new(radius, radius);
			for (int i = 0; i < reachable.Length; i++)
			{
				if (!float.IsNaN(reachable[i].X))
				{
					Vector2 regionMin = Vector2.Max(reachable[i] - extent, min);
					Vector2 regionMax = Vector2.Min(reachable[i] + extent, max);
					ImGuiProbes.MarkRegion($"{label}/{HandleNames[i]}", regionMin, regionMax);
				}
			}
		}
	}
}

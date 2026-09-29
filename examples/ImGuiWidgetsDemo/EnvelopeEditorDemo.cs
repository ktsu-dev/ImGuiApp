// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Shows the envelope editor over a DAHDSR envelope, with a gate button that plays the envelope
/// through the same evaluator the editor draws with.
/// </summary>
internal static class EnvelopeEditorDemo
{
	private static readonly ImGuiWidgets.Envelope DefaultEnvelope = new(0.05f, 0.2f, 0.1f, 0.4f, 0.6f, 0.8f, 0.4f, 0.5f, 0.6f);

	private static ImGuiWidgets.Envelope envelope = DefaultEnvelope;
	private static bool adsrOnly;
	private static float timeSpan = 5f;
	private static bool gateHeld;
	private static bool sounding;
	private static float elapsed;
	private static float noteOff = float.PositiveInfinity;

	/// <summary>Gets the envelope being edited.</summary>
	internal static ImGuiWidgets.Envelope Envelope => envelope;

	/// <summary>Gets whether the delay and hold handles are hidden.</summary>
	internal static bool AdsrOnly => adsrOnly;

	/// <summary>Gets the seconds across the editor's width.</summary>
	internal static float TimeSpan => timeSpan;

	/// <summary>Gets the level the gate is playing at, which is 0 when nothing is sounding.</summary>
	internal static float Level { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		envelope = DefaultEnvelope;
		adsrOnly = false;
		timeSpan = 5f;
		gateHeld = false;
		sounding = false;
		elapsed = 0f;
		noteOff = float.PositiveInfinity;
		Level = 0f;
	}

	internal static void Show()
	{
		if (!DemoProbe.Header("Envelope Editor"))
		{
			return;
		}

		ImGui.TextUnformatted("A DAHDSR envelope. Drag a breakpoint to retime it, drag a tension handle to bend its segment,");
		ImGui.TextUnformatted("and right-click a tension handle to make the segment linear again:");
		ImGui.Separator();

		ImGuiWidgets.EnvelopeEditor("##demoEnvelope", ref envelope, new Vector2(0f, 180f), timeSpan, showDelayAndHold: !adsrOnly);

		DemoProbe.Checkbox("ADSR only##envelope", ref adsrOnly);
		ImGui.SameLine();
		ImGui.SetNextItemWidth(200f);
		DemoProbe.SliderFloat("Time span##envelope", ref timeSpan, 1f, 20f, "%.1f s");

		ImGui.TextUnformatted(string.Format(
			CultureInfo.InvariantCulture,
			"Delay {0:0.000}  Attack {1:0.000}  Hold {2:0.000}  Decay {3:0.000}  Sustain {4:0.00}  Release {5:0.000}",
			envelope.Delay,
			envelope.Attack,
			envelope.Hold,
			envelope.Decay,
			envelope.Sustain,
			envelope.Release));
		ImGui.TextUnformatted(string.Format(
			CultureInfo.InvariantCulture,
			"Tension: attack {0:+0.00;-0.00;+0.00}  decay {1:+0.00;-0.00;+0.00}  release {2:+0.00;-0.00;+0.00}",
			envelope.AttackTension,
			envelope.DecayTension,
			envelope.ReleaseTension));

		DemoProbe.Button("Gate##envelope");
		UpdateGate(ImGui.IsItemActive());
		ImGui.SameLine();
		ImGui.ProgressBar(Level, new Vector2(-1f, 0f), string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Level));
		ImGui.TextUnformatted("Hold Gate to play the note; the bar is Envelope.LevelAt, the evaluator the editor draws.");
	}

	// The gate is a note: pressing is note-on, letting go is note-off, and the note keeps sounding
	// through its release. Every level shown comes from Envelope.LevelAt, the function a synth
	// applying this envelope would call.
	private static void UpdateGate(bool down)
	{
		if (down && !gateHeld)
		{
			gateHeld = true;
			sounding = true;
			elapsed = 0f;
			noteOff = float.PositiveInfinity;
		}
		else if (!down && gateHeld)
		{
			gateHeld = false;
			noteOff = elapsed;
		}

		if (!sounding)
		{
			Level = 0f;
			return;
		}

		elapsed += ImGui.GetIO().DeltaTime;
		Level = envelope.LevelAt(elapsed, noteOff);

		if (!gateHeld && elapsed >= noteOff + envelope.Release)
		{
			sounding = false;
			Level = 0f;
		}
	}
}

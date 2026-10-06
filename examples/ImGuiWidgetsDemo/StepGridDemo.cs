// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows the step grid as a four-voice drum pattern, with a play head the demo clocks itself.</summary>
internal static class StepGridDemo
{
	private const int Rows = 4;
	private const int Steps = 16;
	private const float DefaultTempo = 120.0f;

	private static readonly string[] Voices = ["Kick", "Snare", "Hat", "Clap"];
	private static readonly bool[] Pattern = new bool[Rows * Steps];

	private static bool playing;
	private static float tempo = DefaultTempo;
	private static float sinceLastStep;

	static StepGridDemo() => LoadPreset();

	/// <summary>Gets the pattern the grid edits, row-major.</summary>
	internal static ReadOnlySpan<bool> CurrentPattern => Pattern;

	/// <summary>Gets the step the play head is on, or -1 when stopped.</summary>
	internal static int PlayingStep { get; private set; } = -1;

	/// <summary>Gets the number of cells that are on.</summary>
	internal static int StepsOn => Pattern.Count(static on => on);

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		LoadPreset();
		playing = false;
		tempo = DefaultTempo;
		PlayingStep = -1;
		sinceLastStep = 0.0f;
	}

	// A basic rock beat: kick on 1 and 3, snare on 2 and 4, eighth-note hats.
	private static void LoadPreset()
	{
		Array.Clear(Pattern);
		Pattern[(0 * Steps) + 0] = true;
		Pattern[(0 * Steps) + 8] = true;
		Pattern[(1 * Steps) + 4] = true;
		Pattern[(1 * Steps) + 12] = true;
		for (int step = 0; step < Steps; step += 2)
		{
			Pattern[(2 * Steps) + step] = true;
		}
	}

	internal static void Show()
	{
		if (!DemoProbe.Header("Step Grid"))
		{
			return;
		}

		ImGui.TextUnformatted("A step sequencer grid. Click a cell to toggle it; drag to paint the same value across a run:");
		ImGui.Separator();

		DemoProbe.Checkbox("Play##stepGrid", ref playing);
		ImGui.SameLine();
		ImGui.PushItemWidth(200.0f);
		DemoProbe.SliderFloat("Tempo##stepGrid", ref tempo, 60.0f, 180.0f, "%.0f BPM");
		ImGui.PopItemWidth();

		Advance();

		ImGuiWidgets.StepGrid("##demoStepGrid", Pattern, Rows, Steps, PlayingStep, rowLabels: Voices);

		ImGui.TextUnformatted($"{StepsOn} steps on");
	}

	// The host owns the clock: a sixteenth note is a quarter of a beat.
	private static void Advance()
	{
		if (!playing)
		{
			PlayingStep = -1;
			sinceLastStep = 0.0f;
			return;
		}

		float sixteenth = 60.0f / tempo / 4.0f;
		if (PlayingStep < 0)
		{
			PlayingStep = 0;
			sinceLastStep = 0.0f;
			return;
		}

		sinceLastStep += ImGui.GetIO().DeltaTime;
		while (sinceLastStep >= sixteenth)
		{
			sinceLastStep -= sixteenth;
			PlayingStep = (PlayingStep + 1) % Steps;
		}
	}
}

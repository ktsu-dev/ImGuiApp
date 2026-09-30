// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Shows the frame-time graph twice: live, over this demo's own frames, and over a fixed series
/// with a regular hitch and one long stall.
/// </summary>
internal static class FrameTimeGraphDemo
{
	private const int HistoryCapacity = 240;
	private const float SpikeMilliseconds = 50f;

	private static readonly string[] BudgetNames = ["30 fps", "60 fps", "120 fps", "144 fps", "None"];
	private static readonly float[] BudgetFps = [30f, 60f, 120f, 144f, 0f];
	private static readonly float[] Synthetic = BuildSynthetic();

	private static int budgetIndex = 1;
	private static bool live = true;
	private static bool showStatistics = true;

	/// <summary>Gets the live history, fed once per frame while "Live" is on.</summary>
	internal static FrameTimeHistory History { get; } = new(HistoryCapacity);

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		History.Clear();
		budgetIndex = 1;
		live = true;
		showStatistics = true;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Frame Time Graph"))
		{
			return;
		}

		ImGui.TextUnformatted("Frame times, newest on the right, against a budget line. Hover a bar for its frame:");
		ImGui.Separator();

		if (live)
		{
			History.AddDeltaSeconds(ImGui.GetIO().DeltaTime);
		}

		DemoProbe.Checkbox("Live##frameTimeLive", ref live);
		ImGui.SameLine();
		DemoProbe.Checkbox("Statistics##frameTimeStatistics", ref showStatistics);
		ImGui.SameLine();
		if (DemoProbe.Button("Inject spike"))
		{
			History.Add(SpikeMilliseconds);
		}

		ImGui.SameLine();
		ImGui.SetNextItemWidth(120f);
		ImGui.Combo("Budget##frameTimeBudget", ref budgetIndex, BudgetNames, BudgetNames.Length);

		float fps = BudgetFps[budgetIndex];
		float budget = fps > 0f ? 1000f / fps : 0f;
		float width = ImGui.GetContentRegionAvail().X;

		ImGuiWidgets.FrameTimeGraph("Live##demoFrameTimeLive", History, new FrameTimeGraphOptions
		{
			BudgetMilliseconds = budget,
			ShowStatistics = showStatistics,
			Size = new Vector2(width, 100f),
		});

		ImGui.Spacing();
		ImGui.TextUnformatted("A fixed series: 12 ms frames, a 28 ms hitch every 30th, one 70 ms stall past the scale:");
		ImGuiWidgets.FrameTimeGraph("Synthetic##demoFrameTimeSynthetic", Synthetic, new FrameTimeGraphOptions
		{
			BudgetMilliseconds = budget,
			ShowStatistics = showStatistics,
			Size = new Vector2(width, 100f),
		});
	}

	private static float[] BuildSynthetic()
	{
		float[] frames = new float[120];
		Array.Fill(frames, 12f);
		for (int i = 29; i < frames.Length; i += 30)
		{
			frames[i] = 28f;
		}

		frames[75] = 70f;
		return frames;
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Globalization;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>Shows a timecode field at each preset frame rate, and a duration field bounded to an hour.</summary>
internal static class TimecodeFieldDemo
{
	private static readonly TimecodeRate[] Rates =
	[
		TimecodeRate.Fps23_976,
		TimecodeRate.Fps24,
		TimecodeRate.Fps25,
		TimecodeRate.Fps29_97Drop,
		TimecodeRate.Fps29_97NonDrop,
		TimecodeRate.Fps30,
		TimecodeRate.Fps48,
		TimecodeRate.Fps50,
		TimecodeRate.Fps59_94Drop,
		TimecodeRate.Fps59_94NonDrop,
		TimecodeRate.Fps60,
	];

	private static TimecodeRate rate = TimecodeRate.Fps29_97Drop;
	private static int frame;
	private static int duration = TimecodeRate.Fps29_97Drop.NominalFramesPerSecond * 10;

	/// <summary>Gets the position field's frame count.</summary>
	internal static int Frame => frame;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		rate = TimecodeRate.Fps29_97Drop;
		frame = 0;
		duration = rate.NominalFramesPerSecond * 10;
	}

	internal static void Show()
	{
		if (!DemoProbe.Header("Timecode Field"))
		{
			return;
		}

		ImGui.SetNextItemWidth(160.0f);
		bool open = ImGui.BeginCombo("Rate", rate.ToString());
		ImGuiProbes.MarkItem("Rate");
		if (open)
		{
			foreach (TimecodeRate preset in Rates)
			{
				if (ImGui.Selectable(preset.ToString(), preset == rate))
				{
					rate = preset;
				}
			}

			ImGui.EndCombo();
		}

		int hour = rate.NominalFramesPerSecond * 3600;
		duration = Math.Clamp(duration, 1, hour);

		ImGuiWidgets.TimecodeField("Position", ref frame, rate);
		ImGui.TextDisabled(string.Format(CultureInfo.InvariantCulture, "Frame {0}", frame));
		ImGui.TextDisabled(string.Format(CultureInfo.InvariantCulture, "{0:0.000} s", Timecode.ToSeconds(frame, rate)));

		ImGuiWidgets.TimecodeField("Duration", ref duration, rate, 1, hour);

		ImGui.TextUnformatted("Drag to scrub (Shift ×10), click to type, Shift+/- steps a second.");
	}
}

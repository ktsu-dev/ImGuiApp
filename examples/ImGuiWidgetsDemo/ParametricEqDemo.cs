// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows the parametric EQ over four bands, with the RBJ cookbook response at 48 kHz.</summary>
internal static class ParametricEqDemo
{
	private const float SampleRate = 48000f;

	private static readonly ImGuiWidgets.LogFrequencyAxis Axis = new(20f, 20000f);
	private static readonly ImGuiWidgets.EqBand[] Bands = DefaultBands();
	private static readonly Func<float, float> Response = frequency => ImGuiWidgets.EqResponse.TotalDb(Bands, frequency, SampleRate);
	private static int selectedBand = -1;

	/// <summary>Gets the demo's bands, for tests to read back what a gesture did.</summary>
	internal static ReadOnlySpan<ImGuiWidgets.EqBand> CurrentBands => Bands;

	/// <summary>Gets the selected band's index, or -1.</summary>
	internal static int SelectedBand => selectedBand;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		DefaultBands().CopyTo(Bands, 0);
		selectedBand = -1;
	}

	private static ImGuiWidgets.EqBand[] DefaultBands() =>
	[
		new(40f, 0f, ImGuiWidgets.EqBand.DefaultQ, ImGuiWidgets.EqBandType.LowCut),
		new(250f, -3f, 1.2f),
		new(2500f, 4f, 2f),
		new(8000f, 2f, ImGuiWidgets.EqBand.DefaultQ, ImGuiWidgets.EqBandType.HighShelf),
	];

	public static void Show()
	{
		if (!DemoProbe.Header("Parametric EQ"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag a node to move its band; the wheel over a node changes its Q.");
		ImGui.Separator();

		ImGuiWidgets.ParametricEq("##demoEq", Bands, Response, Axis, ref selectedBand, new Vector2(0f, 220f));

		if (DemoProbe.Button("Reset bands"))
		{
			ResetState();
		}

		if (selectedBand >= 0)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(160f);
			ImGuiWidgets.EqBandType type = Bands[selectedBand].Type;
			if (ImGuiWidgets.Combo($"Band {selectedBand + 1} type", ref type))
			{
				Bands[selectedBand] = Bands[selectedBand] with { Type = type };
			}
		}

		ShowTable();
	}

	private static void ShowTable()
	{
		if (!ImGui.BeginTable("ParametricEqBands", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			return;
		}

		ImGui.TableSetupColumn("Band");
		ImGui.TableSetupColumn("Type");
		ImGui.TableSetupColumn("Frequency");
		ImGui.TableSetupColumn("Gain");
		ImGui.TableSetupColumn("Q");
		ImGui.TableHeadersRow();

		for (int i = 0; i < Bands.Length; i++)
		{
			ImGuiWidgets.EqBand band = Bands[i];
			ImGui.TableNextRow();
			Cell((i + 1).ToString(CultureInfo.InvariantCulture));
			Cell(band.Type.ToString());
			Cell(band.Frequency < 1000f
				? string.Format(CultureInfo.InvariantCulture, "{0:0} Hz", band.Frequency)
				: string.Format(CultureInfo.InvariantCulture, "{0:0.00} kHz", band.Frequency / 1000f));
			Cell(band.HasGain ? string.Format(CultureInfo.InvariantCulture, "{0:+0.0;-0.0} dB", band.GainDb) : "-");
			Cell(band.Q.ToString("0.00", CultureInfo.InvariantCulture));
		}

		ImGui.EndTable();
	}

	private static void Cell(string text)
	{
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(text);
	}
}

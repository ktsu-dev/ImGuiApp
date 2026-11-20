// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Monsoon;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Monsoon;
using ktsu.ImGui.Probes;

/// <summary>
/// A rain-soaked operator console: every Monsoon widget on one screen, so the theme can be judged
/// as a whole rather than one control at a time.
/// </summary>
internal static class ImGuiMonsoonDemo
{
	/// <summary>The probe name of the left panel.</summary>
	internal const string StatusPanelId = "status";

	/// <summary>The probe name of the right panel.</summary>
	internal const string ScanPanelId = "scan";

	/// <summary>The probe name of the scan grid.</summary>
	internal const string ScanGridId = "grid";

	/// <summary>The labels of the buttons on the status panel, in the order they are drawn.</summary>
	internal static readonly string[] ActionLabels = ["UPLINK", "ENHANCE", "PURGE"];

	private static readonly string[] MotionLabels = ["Full motion", "Reduced motion", "No motion"];

	/// <summary>Gets the label of the last button pressed, or an empty string when none has been.</summary>
	internal static string LastAction { get; private set; } = string.Empty;

	/// <summary>Gets the scan grid's target, each axis from 0 to 1.</summary>
	internal static Vector2 Target => target;

	private static Vector2 target = new(0.62f, 0.38f);

	/// <summary>
	/// Builds the configuration the demo runs on. Extracted from <c>Main</c> so a UI test drives the
	/// real configuration rather than a parallel one written for testing.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.Monsoon - Demo",
		SaveIniSettings = false,
		OnStart = MonsoonTheme.Apply,
		OnRender = _ => Render(),
	};

	/// <summary>Puts every piece of demo state back as it starts, since statics outlive a harness.</summary>
	internal static void ResetState()
	{
		LastAction = string.Empty;
		target = new(0.62f, 0.38f);
		MonsoonEffects.Motion = MonsoonMotion.Full;
	}

	private static void Main() => ImGuiApp.Start(BuildConfig());

	private static void Render()
	{
		MonsoonWidgets.NeonSign("モンスーン", 44f);
		ImGui.SameLine(0f, 24f);
		using (new ScopedGroup())
		{
			ImGui.Dummy(new Vector2(0f, 6f));
			MonsoonWidgets.GlitchText("MONSOON // UPLINK 2049-B");
			MonsoonWidgets.NeonText("SECTOR 7 / LEVEL 88 / NIGHT CYCLE", MonsoonPalette.TextDim, 0.05f);
		}

		ImGui.SameLine(0f, 32f);
		DrawMotionSelector();

		ImGui.Dummy(new Vector2(0f, 6f));

		Vector2 avail = ImGui.GetContentRegionAvail();
		float gap = ImGui.GetStyle().ItemSpacing.X;
		float leftWidth = MathF.Max(260f, (avail.X - gap) * 0.42f);
		Vector2 panelSize = new(leftWidth, MathF.Max(320f, avail.Y));

		using (MonsoonGlassPanel panel = MonsoonWidgets.GlassPanel(StatusPanelId, "STATUS // ステータス", panelSize, MonsoonPalette.Cyan))
		{
			if (panel.IsVisible)
			{
				DrawStatus();
			}
		}

		ImGui.SameLine();
		Vector2 rightSize = new(MathF.Max(260f, avail.X - leftWidth - gap), panelSize.Y);
		using (MonsoonGlassPanel panel = MonsoonWidgets.GlassPanel(ScanPanelId, "SCAN // スキャン", rightSize, MonsoonPalette.Magenta))
		{
			if (panel.IsVisible)
			{
				DrawScan();
			}
		}
	}

	private static void DrawMotionSelector()
	{
		using ScopedGroup group = new();
		int motion = (int)MonsoonEffects.Motion;
		for (int i = 0; i < MotionLabels.Length; i++)
		{
			if (ImGui.RadioButton(MotionLabels[i], motion == i))
			{
				MonsoonEffects.Motion = (MonsoonMotion)i;
			}

			ImGuiProbes.MarkItem(MotionLabels[i]);
		}
	}

	private static void DrawStatus()
	{
		double time = MonsoonEffects.Time;
		float load = 0.55f + (0.25f * MathF.Sin((float)time * 0.7f));

		MonsoonWidgets.DataReadout("SUBJECT", "N6-MAA-10816");
		MonsoonWidgets.DataReadout("INCEPT", "2016-04-08");
		MonsoonWidgets.DataReadout("HUMIDITY", "98 %", MonsoonPalette.Cyan);
		MonsoonWidgets.DataReadout("WIND", string.Format(CultureInfo.InvariantCulture, "{0:0.0} KT", 14.2 + (time % 3.0)), MonsoonPalette.Amber);
		ImGui.Spacing();

		MonsoonWidgets.SegmentedBar("POWER", 0.82f, color: MonsoonPalette.Cyan);
		MonsoonWidgets.SegmentedBar("CORE LOAD", load, color: MonsoonPalette.Amber);
		MonsoonWidgets.SegmentedBar("COOLANT", 0.97f);
		ImGui.Spacing();

		MonsoonWidgets.StatusLamp("NETWORK", MonsoonLampState.Ok);
		ImGui.SameLine(0f, 18f);
		MonsoonWidgets.StatusLamp("DRONES", MonsoonLampState.Warn);
		ImGui.SameLine(0f, 18f);
		MonsoonWidgets.StatusLamp("PERIMETER", MonsoonLampState.Alarm);
		ImGui.Spacing();

		Vector4[] colors = [MonsoonPalette.Cyan, MonsoonPalette.Magenta, MonsoonPalette.Alarm];
		for (int i = 0; i < ActionLabels.Length; i++)
		{
			if (i > 0)
			{
				ImGui.SameLine();
			}

			if (MonsoonWidgets.NeonButton(ActionLabels[i], colors[i]))
			{
				LastAction = ActionLabels[i];
			}
		}

		ImGui.Spacing();
		MonsoonWidgets.NeonText(LastAction.Length == 0 ? "> AWAITING COMMAND" : "> " + LastAction + " ACKNOWLEDGED", MonsoonPalette.Amber);
	}

	private static void DrawScan()
	{
		float width = ImGui.GetContentRegionAvail().X;
		float side = MathF.Min(width, 260f);
		MonsoonWidgets.ScanGrid(ScanGridId, new Vector2(side, side), ref target);
		ImGui.Spacing();
		MonsoonWidgets.HexStream("feed", 6);
		ImGui.Spacing();
		MonsoonWidgets.NeonSign("データ", 28f, MonsoonPalette.Amber);
	}
}

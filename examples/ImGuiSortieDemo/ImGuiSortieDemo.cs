// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Sortie;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Sortie;

/// <summary>
/// Shows every widget in ktsu.ImGui.Sortie on one command-room screen: a sync gauge and pilot readouts, a
/// hex sector map, a reserve-power countdown, a pattern monitor, status lamps, and the command buttons that
/// raise and stand down the emergency condition.
/// </summary>
internal static class ImGuiSortieDemo
{
	internal static readonly TimeSpan FullReserve = TimeSpan.FromMinutes(5);

	internal static readonly Dictionary<HexCell, SortieLamp> Sectors = [];

	internal static SortieCondition Condition { get; private set; } = SortieCondition.Standby;

	internal static TimeSpan Reserve { get; private set; } = FullReserve;

	internal static int SectorsMarked { get; private set; }

	private static readonly float[] Pattern = new float[160];

	private static float time;

	/// <summary>Restores every field a test can disturb.</summary>
	internal static void ResetState()
	{
		Condition = SortieCondition.Standby;
		Reserve = FullReserve;
		SectorsMarked = 0;
		time = 0f;
		Sectors.Clear();
		Sectors[new HexCell(2, 1)] = SortieLamp.Nominal;
		Sectors[new HexCell(3, 1)] = SortieLamp.Nominal;
		Sectors[new HexCell(5, 3)] = SortieLamp.Caution;
		Sectors[new HexCell(8, 2)] = SortieLamp.Alert;
	}

	static ImGuiSortieDemo() => ResetState();

	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.Sortie - Demo",
		OnStart = () => SortieStyle.Apply(Condition),
		OnRender = Render,
	};

	internal static void SetCondition(SortieCondition condition)
	{
		Condition = condition;
		SortieStyle.Apply(condition);
	}

	private static void Render(float delta)
	{
		time += delta;
		if (Condition == SortieCondition.Emergency)
		{
			Reserve = Reserve > TimeSpan.FromSeconds(delta) ? Reserve - TimeSpan.FromSeconds(delta) : TimeSpan.Zero;
		}

		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.WorkPos);
		ImGui.SetNextWindowSize(viewport.WorkSize);
		ImGui.Begin("Command", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

		DrawHeader();
		ImGui.Spacing();

		ImGuiStylePtr style = ImGui.GetStyle();
		Vector2 available = ImGui.GetContentRegionAvail();
		float buttonRow = ImGui.GetFrameHeight() + 12f + style.ItemSpacing.Y;
		float height = available.Y - buttonRow;
		float side = MathF.Floor((available.X - (style.ItemSpacing.X * 2f)) * 0.27f);
		float middle = available.X - (side * 2f) - (style.ItemSpacing.X * 2f);

		DrawPilot(new Vector2(side, height));
		ImGui.SameLine();
		DrawSectors(new Vector2(middle, height));
		ImGui.SameLine();
		DrawPower(new Vector2(side, height));

		DrawCommands();
		ImGui.End();
	}

	private static void DrawHeader()
	{
		if (Condition == SortieCondition.Emergency)
		{
			ImGuiSortie.EmergencyBanner("EMERGENCY");
			return;
		}

		ImGuiSortie.HeavyText("TACTICAL COMMAND", 38f);
		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 200f);
		ImGuiSortie.StatusLamp("CONDITION GREEN", SortieLamp.Nominal);
	}

	private static void DrawPilot(Vector2 size)
	{
		ImGuiSortie.BeginPanel("PILOT STATUS", size, "01");
		float ratio = Condition == SortieCondition.Emergency
			? 0.32f + (0.08f * MathF.Sin(time * 3f))
			: 0.84f + (0.04f * MathF.Sin(time * 0.7f));
		ImGuiSortie.SyncGauge("SYNC RATIO", ratio, new Vector2(0f, 110f));
		ImGui.Spacing();
		ImGuiSortie.Readout("HARMONICS", Condition == SortieCondition.Emergency ? "UNSTABLE" : "NORMAL", null,
			Condition == SortieCondition.Emergency ? SortieLamp.Alert : SortieLamp.Nominal);
		ImGuiSortie.Readout("PLUG DEPTH", "0.42", "M", SortieLamp.Nominal);
		ImGuiSortie.Readout("NEURAL LINK", $"{ratio * 100f:0.0}", "%", ratio < 0.4f ? SortieLamp.Caution : SortieLamp.Nominal);
		ImGuiSortie.Readout("CORE TEMP", "311", "K", SortieLamp.Off);
		ImGui.Spacing();
		ImGuiSortie.StatusLamp("LIFE SUPPORT", SortieLamp.Nominal);
		ImGuiSortie.StatusLamp("UMBILICAL", Condition == SortieCondition.Emergency ? SortieLamp.Alert : SortieLamp.Nominal, Condition == SortieCondition.Emergency);
		ImGuiSortie.StatusLamp("RESTRAINTS", SortieLamp.Caution);
		ImGuiSortie.StatusLamp("LAUNCH TRACK", SortieLamp.Off);
		ImGuiSortie.EndPanel();
	}

	private static void DrawSectors(Vector2 size)
	{
		ImGuiSortie.BeginPanel("SECTOR MAP", size, "02");
		Vector2 available = ImGui.GetContentRegionAvail();
		if (ImGuiSortie.HexGrid("sector", new Vector2(0f, available.Y - ImGui.GetFrameHeightWithSpacing()), 26f, Sectors, out HexCell clicked))
		{
			SectorsMarked++;
			if (Sectors.TryGetValue(clicked, out SortieLamp lamp) && lamp == SortieLamp.Alert)
			{
				Sectors.Remove(clicked);
			}
			else
			{
				Sectors[clicked] = SortieLamp.Alert;
			}
		}

		ImGui.TextUnformatted($"SECTORS FLAGGED {Sectors.Count:00}   CLICK A CELL TO MARK IT");
		ImGuiSortie.EndPanel();
	}

	private static void DrawPower(Vector2 size)
	{
		ImGuiSortie.BeginPanel("RESERVE POWER", size, "03");
		ImGuiSortie.Countdown("ACTIVE LIMIT", Reserve, 44f);
		ImGui.Spacing();
		ImGuiSortie.Readout("SUPPLY", Condition == SortieCondition.Emergency ? "INTERNAL" : "EXTERNAL", null,
			Condition == SortieCondition.Emergency ? SortieLamp.Caution : SortieLamp.Nominal);
		ImGui.Spacing();

		float speed = Condition == SortieCondition.Emergency ? 9f : 2.5f;
		for (int i = 0; i < Pattern.Length; i++)
		{
			float x = (i / (float)Pattern.Length * 12f) + (time * speed);
			float spike = Condition == SortieCondition.Emergency && i % 37 == 0 ? 0.6f : 0f;
			Pattern[i] = Math.Clamp((0.55f * MathF.Sin(x)) + (0.25f * MathF.Sin(x * 3.7f)) + spike, -1f, 1f);
		}

		ImGuiSortie.PatternMonitor("WAVE PATTERN", Pattern, new Vector2(0f, ImGui.GetContentRegionAvail().Y), Condition == SortieCondition.Emergency ? SortieLamp.Alert : SortieLamp.Off);
		ImGuiSortie.EndPanel();
	}

	private static void DrawCommands()
	{
		Vector2 size = new(180f, ImGui.GetFrameHeight() + 12f);
		if (ImGuiSortie.Button("SORTIE", size, SortieLamp.Alert))
		{
			SetCondition(SortieCondition.Emergency);
		}

		ImGui.SameLine();
		if (ImGuiSortie.Button("STAND DOWN", size))
		{
			SetCondition(SortieCondition.Standby);
		}

		ImGui.SameLine();
		if (ImGuiSortie.Button("RESTORE POWER", size, SortieLamp.Nominal))
		{
			Reserve = FullReserve;
		}
	}

	[STAThread]
	private static void Main() => ImGuiApp.Start(BuildConfig());
}

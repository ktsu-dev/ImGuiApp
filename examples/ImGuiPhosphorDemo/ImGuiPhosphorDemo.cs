// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Phosphor;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Phosphor;
using ktsu.ImGui.Probes;

/// <summary>
/// Shows every widget in ktsu.ImGui.Phosphor on one ship's-console screen: a terminal typing out a
/// boot log, a radar, an oscilloscope, a perspective grid, readouts, status lights and an alert.
/// </summary>
internal static class ImGuiPhosphorDemo
{
	private static readonly string[] BootLog =
	[
		"SHIP SYSTEMS ONLINE. AWAITING QUERY.",
		"",
		"> STATUS HULL",
		"  HULL INTEGRITY ............ NOMINAL",
		"  ATMOSPHERE ................ 21.0% O2",
		"> STATUS ENGINES",
		"  DRIVE A ................... STANDBY",
		"  DRIVE B ................... STANDBY",
		"> PLOT COURSE",
		"  CALCULATING TRAJECTORY ...",
	];

	internal static readonly List<RadarContact> Contacts =
	[
		new(0.6f, 0.45f),
		new(2.3f, 0.8f),
		new(4.1f, 0.25f),
	];

	internal static PhosphorTint Tint { get; set; } = PhosphorTint.Green;

	internal static bool ShowCrt { get; set; } = true;

	internal static bool AlertActive { get; set; } = true;

	internal static int CommandsRun { get; set; }

	internal static PhosphorTerminal Terminal { get; private set; } = CreateTerminal();

	private static float time;

	/// <summary>Restores every field a test can disturb.</summary>
	internal static void ResetState()
	{
		Tint = PhosphorTint.Green;
		ShowCrt = true;
		AlertActive = true;
		CommandsRun = 0;
		time = 0f;
		Terminal = CreateTerminal();
	}

	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.Phosphor - Demo",
		OnStart = () => PhosphorStyle.Apply(Tint),
		OnRender = Render,
	};

	private static PhosphorTerminal CreateTerminal()
	{
		PhosphorTerminal terminal = new(90f);
		foreach (string line in BootLog)
		{
			terminal.WriteLine(line);
		}

		return terminal;
	}

	private static void SetTint(PhosphorTint tint)
	{
		Tint = tint;
		PhosphorStyle.Apply(tint);
	}

	private static void Render(float delta)
	{
		time += delta;
		ImGuiViewportPtr viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowPos(viewport.WorkPos);
		ImGui.SetNextWindowSize(viewport.WorkSize);
		ImGui.Begin("Console", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

		DrawHeader();
		ImGui.Spacing();

		Vector2 available = ImGui.GetContentRegionAvail();
		float column = (available.X - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
		float top = MathF.Floor((available.Y - ImGui.GetStyle().ItemSpacing.Y) * 0.55f);
		ImGui.BeginGroup();
		DrawTerminal(new Vector2(column, top));
		DrawScope(new Vector2(column, 0f));
		ImGui.EndGroup();
		ImGui.SameLine();
		ImGui.BeginGroup();
		DrawNavigation(new Vector2(column, top));
		DrawSystems(new Vector2(column, 0f));
		ImGui.EndGroup();

		ImGui.End();

		if (ShowCrt)
		{
			ImGuiPhosphor.CrtOverlay();
		}
	}

	private static void DrawHeader()
	{
		ImGuiPhosphor.GlowText("MAIN CONSOLE // DECK C", PhosphorShade.Hot);
		ImGui.SameLine();
		if (ImGuiPhosphor.Button("GREEN"))
		{
			SetTint(PhosphorTint.Green);
		}

		ImGui.SameLine();
		if (ImGuiPhosphor.Button("AMBER"))
		{
			SetTint(PhosphorTint.Amber);
		}

		ImGui.SameLine();
		bool crt = ShowCrt;
		if (ImGui.Checkbox("CRT", ref crt))
		{
			ShowCrt = crt;
		}

		ImGuiProbes.MarkItem("CRT");

		if (AlertActive)
		{
			ImGuiPhosphor.AlertBanner("PROXIMITY WARNING");
		}
	}

	private static void DrawTerminal(Vector2 size)
	{
		using (PhosphorFrame frame = ImGuiPhosphor.Frame("TERMINAL", size))
		{
			if (frame.IsVisible)
			{
				Terminal.Draw("log", new Vector2(-1f, -ImGui.GetFrameHeightWithSpacing()));
				if (ImGuiPhosphor.Button("RUN DIAGNOSTIC"))
				{
					CommandsRun++;
					Terminal.WriteLine($"> DIAGNOSTIC {CommandsRun:D2}");
					Terminal.WriteLine("  ALL SYSTEMS WITHIN TOLERANCE");
				}

				ImGui.SameLine();
				if (ImGuiPhosphor.Button("ACKNOWLEDGE"))
				{
					AlertActive = false;
				}
			}
		}
	}

	private static void DrawScope(Vector2 size)
	{
		using (PhosphorFrame frame = ImGuiPhosphor.Frame("SIGNAL", size))
		{
			if (frame.IsVisible)
			{
				Span<float> samples = stackalloc float[160];
				for (int i = 0; i < samples.Length; i++)
				{
					float t = (i / (float)samples.Length * 4f * MathF.PI) + (time * 3f);
					samples[i] = (0.6f * MathF.Sin(t)) + (0.25f * MathF.Sin((t * 3.7f) + 1f));
				}

				ImGuiPhosphor.Oscilloscope("carrier", samples, new Vector2(-1f, -1f));
			}
		}
	}

	private static void DrawNavigation(Vector2 size)
	{
		using (PhosphorFrame frame = ImGuiPhosphor.Frame("NAVIGATION", size))
		{
			if (frame.IsVisible)
			{
				float radius = MathF.Floor(MathF.Min(ImGui.GetContentRegionAvail().Y, ImGui.GetContentRegionAvail().X * 0.5f) * 0.5f);
				ImGuiPhosphor.RadarSweep("proximity", radius, Contacts);
				ImGui.SameLine();
				ImGuiPhosphor.VectorGrid("approach", new Vector2(-1f, radius * 2f));
			}
		}
	}

	private static void DrawSystems(Vector2 size)
	{
		using (PhosphorFrame frame = ImGuiPhosphor.Frame("SYSTEMS", size))
		{
			if (frame.IsVisible)
			{
				float readout = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * 2f)) / 3f;
				ImGuiPhosphor.Readout("VELOCITY", "0.42 C", readout);
				ImGui.SameLine();
				ImGuiPhosphor.Readout("HEADING", "214.07", readout);
				ImGui.SameLine();
				ImGuiPhosphor.Readout("CREW", "7 / 7", readout);
				ImGuiPhosphor.StatusLight("LIFE SUPPORT", lit: true);
				ImGui.SameLine();
				ImGuiPhosphor.StatusLight("BEACON", lit: true, blinkHertz: 1f);
				ImGui.SameLine();
				ImGuiPhosphor.StatusLight("COMMS", lit: false);
				ImGuiPhosphor.SegmentBar("FUEL", 0.63f);
				ImGuiPhosphor.SegmentBar("COOLANT", 0.31f);
			}
		}
	}

	private static void Main() => ImGuiApp.Start(BuildConfig());
}

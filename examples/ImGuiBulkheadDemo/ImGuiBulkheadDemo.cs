// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Bulkhead;

/// <summary>
/// A console drawn entirely from <see cref="ImGuiBulkhead"/> widgets: one panel, a sidebar of
/// sections, and the bars, toggles and gauges each section shows.
/// </summary>
internal static class ImGuiBulkheadDemo
{
	/// <summary>The sections the sidebar switches between, in sidebar order.</summary>
	internal static readonly string[] Sections = ["Status", "Power", "Comms"];

	/// <summary>Gets the section the sidebar last selected.</summary>
	internal static string Section { get; private set; } = Sections[0];

	/// <summary>Gets a value indicating whether the shields toggle is on.</summary>
	internal static bool Shields => shields;

	/// <summary>Gets a value indicating whether the sensors toggle is on.</summary>
	internal static bool Sensors => sensors;

	/// <summary>Gets how many times the hail button has been pressed.</summary>
	internal static int Hails { get; private set; }

	/// <summary>Gets the reactor output the power section shows, from 0 to 1.</summary>
	internal static float Output { get; private set; } = 0.62f;

	private static bool shields = true;
	private static bool sensors;

	/// <summary>
	/// Builds the configuration the demo runs on, so a UI test drives the real configuration rather
	/// than a copy of it.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.Bulkhead - Demo",
		OnStart = BulkheadStyle.Apply,
		OnRender = _ => Render(),
	};

	/// <summary>Restores every value a test can change, since the demo's state outlives a harness.</summary>
	internal static void ResetState()
	{
		Section = Sections[0];
		shields = true;
		sensors = false;
		Hails = 0;
		Output = 0.62f;
	}

	private static void Main() => ImGuiApp.Start(BuildConfig());

	// ImGuiApp already draws OnRender inside a window covering the viewport, so the panel goes
	// straight into it. A second full-screen window would sit behind that one and never be seen.
	private static void Render()
	{
		using BulkheadPanel panel = ImGuiBulkhead.Panel("Main Console", Vector2.Zero);
		using (panel.Sidebar())
		{
			DrawSidebar();
		}

		using (panel.Content())
		{
			DrawContent();
		}
	}

	private static void DrawSidebar()
	{
		float width = ImGui.GetContentRegionAvail().X;
		Vector4[] colors = [BulkheadPalette.Lilac, BulkheadPalette.Periwinkle, BulkheadPalette.Mauve];
		for (int i = 0; i < Sections.Length; i++)
		{
			string name = Sections[i];
			Vector4 color = name == Section ? BulkheadPalette.Sunflower : colors[i];
			if (ImGuiBulkhead.Button(name, new Vector2(width, ImGui.GetFrameHeight() * 1.5f), color, BulkheadCaps.None))
			{
				Section = name;
			}
		}
	}

	private static void DrawContent()
	{
		ImGuiBulkhead.Header(Section);
		ImGui.Spacing();

		switch (Section)
		{
			case "Power":
				DrawPower();
				break;
			case "Comms":
				DrawComms();
				break;
			default:
				DrawStatus();
				break;
		}
	}

	private static void DrawStatus()
	{
		ImGuiBulkhead.Toggle("Shields", ref shields);
		ImGui.SameLine();
		ImGuiBulkhead.Toggle("Sensors", ref sensors);
		ImGui.Spacing();

		float width = ImGui.GetContentRegionAvail().X;
		float height = ImGui.GetFrameHeight() * 0.6f;
		ImGuiBulkhead.Bar("hull", new Vector2(width * 0.7f, height), BulkheadPalette.Ice, BulkheadCaps.Start, "Hull 98%");
		ImGuiBulkhead.Bar("deflector", new Vector2(width * 0.45f, height), BulkheadPalette.Seafoam, BulkheadCaps.Start, "Deflector");
		ImGuiBulkhead.Bar("warning", new Vector2(width * 0.25f, height), BulkheadPalette.Ember, BulkheadCaps.Both, "Alert");
	}

	private static void DrawPower()
	{
		ImGuiBulkhead.Gauge("output", Output);
		ImGui.SameLine();
		ImGui.TextUnformatted($"{Output * 100f:0}%");
		if (ImGuiBulkhead.Button("Raise", default, BulkheadPalette.Amber, BulkheadCaps.Start))
		{
			Output = Math.Min(1f, Output + 0.1f);
		}

		ImGui.SameLine();
		if (ImGuiBulkhead.Button("Lower", default, BulkheadPalette.Apricot, BulkheadCaps.End))
		{
			Output = Math.Max(0f, Output - 0.1f);
		}

		ImGuiBulkhead.Gauge("reserve", 0.3f, default, 8, BulkheadPalette.Sunflower);
	}

	private static void DrawComms()
	{
		ImGuiBulkhead.Elbow("relay", new Vector2(ImGui.GetFrameHeight() * 8f, ImGui.GetFrameHeight() * 3f), BulkheadCorner.TopLeft, ImGui.GetFrameHeight() * 2f, ImGui.GetFrameHeight() * 0.6f, BulkheadPalette.Periwinkle, "Relay");
		if (ImGuiBulkhead.Button("Hail"))
		{
			Hails++;
		}

		ImGui.SameLine();
		ImGui.TextUnformatted($"Hails sent: {Hails}");
	}
}

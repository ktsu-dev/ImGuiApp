// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// Coach marks: a short tour that lights up one toolbar button at a time and explains it.
/// </summary>
internal static class CoachMarkDemo
{
	private static readonly CoachMarkStep[] Steps =
	[
		new("new", "New", "Starts an empty document."),
		new("open", "Open", "Opens a document you saved before."),
		new("share", "Share", "Sends the document to someone else."),
	];

	private static readonly OverlayHost Host = new();

	/// <summary>Gets how the last tour ended.</summary>
	internal static string Outcome { get; private set; } = "(not taken)";

	/// <summary>Gets the tour, for tests to read its step.</summary>
	internal static ImGuiWidgets.Tutorial Tour { get; private set; } = Create();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		Tour = Create();
		Outcome = "(not taken)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Coach Marks"))
		{
			ImGui.TextWrapped("A tour dims the window except one control at a time, with a note beside it.");

			if (DemoProbe.Button("Take the tour"))
			{
				Tour.Start();
			}

			ImGui.SameLine();
			ImGui.TextUnformatted($"Tour: {Outcome}");

			DemoProbe.Button("New##coachNew");
			Tour.Target("new");
			ImGui.SameLine();
			DemoProbe.Button("Open##coachOpen");
			Tour.Target("open");
			ImGui.SameLine();
			DemoProbe.Button("Share##coachShare");
			Tour.Target("share");
			Host.Render();
		}
	}

	private static ImGuiWidgets.Tutorial Create() =>
		new(Host, "demoTour", Steps) { Finished = completed => Outcome = completed ? "Finished" : "Skipped" };
}

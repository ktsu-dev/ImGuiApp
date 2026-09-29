// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

/// <summary>
/// Groups the sections that live in their own classes, so the main demo couples to one type
/// rather than one per section.
/// </summary>
internal static class SectionDemos
{
	/// <summary>Returns every grouped section's state to its starting values.</summary>
	internal static void ResetState()
	{
		DiffViewDemo.ResetState();
		WaveformDemo.ResetState();
		DataTableDemo.ResetState();
	}

	/// <summary>Shows the grouped sections that sit among the basic widgets, in demo order.</summary>
	internal static void Show()
	{
		WaveformDemo.Show();
		DiffViewDemo.Show();
	}
}

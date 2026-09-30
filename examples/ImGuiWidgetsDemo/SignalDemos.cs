// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

/// <summary>
/// The audio signal sections, drawn one after another. Grouped so the main demo class takes one
/// dependency for all of them rather than one per widget, which keeps it under the analyzers'
/// class-coupling limit as signal widgets are added.
/// </summary>
internal static class SignalDemos
{
	/// <summary>Returns every signal section to its starting state.</summary>
	internal static void ResetState()
	{
		WaveformDemo.ResetState();
		TransportScrubberDemo.ResetState();
		SpectrumAnalyzerDemo.ResetState();
		EnvelopeEditorDemo.ResetState();
		ParametricEqDemo.ResetState();
	}

	/// <summary>Draws every signal section.</summary>
	public static void Show()
	{
		WaveformDemo.Show();
		TransportScrubberDemo.Show();
		SpectrumAnalyzerDemo.Show();
		EnvelopeEditorDemo.Show();
		ParametricEqDemo.Show();
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

/// <summary>One section of the demo: how to draw it, reset it, and start it up.</summary>
/// <param name="Show">Draws the section.</param>
/// <param name="ResetState">Returns the section's state to its starting values.</param>
/// <param name="Initialize">Does the section's start-up work, or null when it has none.</param>
internal readonly record struct DemoSection(Action Show, Action ResetState, Action? Initialize = null);

/// <summary>
/// The demo's sections, in the order they are drawn. The single registry: a new section is one
/// line here and nothing in <c>ImGuiWidgetsDemo</c>, which is what keeps that class under the
/// analyzers' class-coupling limit however many widgets are added.
/// </summary>
internal static class DemoSections
{
	/// <summary>Gets the sections of the Widget Demos tab, in demo order.</summary>
	internal static IReadOnlyList<DemoSection> WidgetDemos { get; } =
	[
		new(MobileFormControlsDemo.Show, MobileFormControlsDemo.ResetState),
		new(PropertyGridDemo.Show, PropertyGridDemo.ResetState),
		new(KnobDemo.Show, KnobDemo.ResetState),
		new(RadialProgressBarDemo.Show, RadialProgressBarDemo.ResetState),
		new(ColorIndicatorDemo.Show, ColorIndicatorDemo.ResetState),
		new(ComboDemo.Show, ComboDemo.ResetState),
		new(TextUtilitiesDemo.Show, TextUtilitiesDemo.ResetState),
		new(ScopedUtilitiesDemo.Show, ScopedUtilitiesDemo.ResetState),
		new(TreeViewDemo.Show, TreeViewDemo.ResetState),
		new(MobileDecoratorsDemo.Show, MobileDecoratorsDemo.ResetState),
		new(MobileContainersDemo.Show, MobileContainersDemo.ResetState),
		new(HistogramAndHandleTrackDemo.Show, HistogramAndHandleTrackDemo.ResetState),
		new(CurveTrackDemo.Show, CurveTrackDemo.ResetState),
		new(TimecodeFieldDemo.Show, TimecodeFieldDemo.ResetState),
		new(WaveformDemo.Show, WaveformDemo.ResetState),
		new(TransportScrubberDemo.Show, TransportScrubberDemo.ResetState),
		new(SpectrumAnalyzerDemo.Show, SpectrumAnalyzerDemo.ResetState),
		new(EnvelopeEditorDemo.Show, EnvelopeEditorDemo.ResetState),
		new(ParametricEqDemo.Show, ParametricEqDemo.ResetState),
		new(StereoMetersDemo.Show, StereoMetersDemo.ResetState),
		new(ChannelFaderDemo.Show, ChannelFaderDemo.ResetState),
		new(PianoKeyboardDemo.Show, PianoKeyboardDemo.ResetState),
		new(StepGridDemo.Show, StepGridDemo.ResetState),
		new(ColorWheelDemo.Show, ColorWheelDemo.ResetState),
		new(DiffViewDemo.Show, DiffViewDemo.ResetState),
	];

	/// <summary>Gets the sections of the Advanced Demos tab, in demo order.</summary>
	internal static IReadOnlyList<DemoSection> AdvancedDemos { get; } =
	[
		new(ImagesAndIconsDemo.Show, ImagesAndIconsDemo.ResetState),
		new(ImageCanvasDemo.Show, ImageCanvasDemo.ResetState),
		new(LevelsDemo.Show, LevelsDemo.ResetState),
		new(AssetBrowserDemo.Show, AssetBrowserDemo.ResetState),
		new(GradientDemo.Show, GradientDemo.ResetState),
		new(TabPanelDemo.Show, TabPanelDemo.ResetState, TabPanelDemo.Initialize),
		new(SearchBoxDemo.Show, SearchBoxDemo.ResetState),
		new(GridDemo.Show, GridDemo.ResetState, GridDemo.Initialize),
		new(VirtualTableDemo.Show, VirtualTableDemo.ResetState),
		new(DataTableDemo.Show, DataTableDemo.ResetState),
		new(DividerDemo.Show, DividerDemo.ResetState, DividerDemo.Initialize),
	];

	/// <summary>Draws every section of a list, in order.</summary>
	/// <param name="sections">The sections to draw.</param>
	internal static void ShowAll(IReadOnlyList<DemoSection> sections)
	{
		foreach (DemoSection section in sections)
		{
			section.Show();
		}
	}

	/// <summary>Returns every section's state, on both tabs, to its starting values.</summary>
	internal static void ResetState()
	{
		foreach (DemoSection section in WidgetDemos.Concat(AdvancedDemos))
		{
			section.ResetState();
		}
	}

	/// <summary>Runs the start-up work of every section that has some, in registry order.</summary>
	internal static void Initialize()
	{
		foreach (DemoSection section in WidgetDemos.Concat(AdvancedDemos))
		{
			section.Initialize?.Invoke();
		}
	}
}

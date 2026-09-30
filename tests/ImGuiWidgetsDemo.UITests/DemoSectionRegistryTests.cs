// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiWidgetsDemo.UITests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ktsu.ImGui.Examples.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Pins the one-class-per-section layout of the widgets demo: every section class is drawn, each is
/// drawn once, and each resets its own state. Reflection only, so no frame is rendered.
/// </summary>
[TestClass]
public sealed class DemoSectionRegistryTests
{
	private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

	private static readonly string[] WidgetDemoOrder =
	[
		nameof(MobileFormControlsDemo), nameof(PropertyGridDemo), nameof(KnobDemo),
		nameof(RadialProgressBarDemo), nameof(ColorIndicatorDemo), nameof(ComboDemo),
		nameof(TextUtilitiesDemo), nameof(ScopedUtilitiesDemo), nameof(TreeViewDemo),
		nameof(MobileDecoratorsDemo), nameof(MobileContainersDemo), nameof(HistogramAndHandleTrackDemo),
		nameof(CurveTrackDemo), nameof(TimecodeFieldDemo), nameof(WaveformDemo), nameof(TransportScrubberDemo),
		nameof(SpectrumAnalyzerDemo), nameof(EnvelopeEditorDemo), nameof(ParametricEqDemo), nameof(StereoMetersDemo), nameof(ChannelFaderDemo), nameof(PianoKeyboardDemo), nameof(StepGridDemo), nameof(ColorWheelDemo),
		nameof(DiffViewDemo),
	];

	private static readonly string[] AdvancedDemoOrder =
	[
		nameof(ImagesAndIconsDemo), nameof(ImageCanvasDemo), nameof(LevelsDemo),
		nameof(TabPanelDemo), nameof(SearchBoxDemo), nameof(GridDemo), nameof(VirtualTableDemo), nameof(DataTableDemo),
		nameof(DividerDemo),
	];

	private static IEnumerable<DemoSection> AllSections => DemoSections.WidgetDemos.Concat(DemoSections.AdvancedDemos);

	/// <summary>
	/// A section class that exists but is not registered is never drawn and never reset, which is
	/// exactly the failure a registry makes easy: the file compiles and the demo quietly omits it.
	/// </summary>
	[TestMethod]
	public void EverySectionClass_IsRegisteredExactlyOnce()
	{
		List<Type> registered = [.. AllSections.Select(section => section.Show.Method.DeclaringType!)];
		Assert.AreEqual(registered.Count, registered.Distinct().Count(), "A section is registered more than once.");

		string[] sectionClasses =
		[
			.. typeof(DemoSections).Assembly.GetTypes()
				.Where(type => HasParameterlessMethod(type, "Show") && HasParameterlessMethod(type, "ResetState"))
				.Select(type => type.Name)
				.Order(StringComparer.Ordinal),
		];
		string[] registeredClasses = [.. registered.Select(type => type.Name).Order(StringComparer.Ordinal)];

		CollectionAssert.AreEqual(sectionClasses, registeredClasses);
	}

	/// <summary>
	/// A section's reset has to be its own: one that reset a neighbour would leave its own state to
	/// whichever test ran before.
	/// </summary>
	[TestMethod]
	public void EveryRegisteredSection_ResetsItsOwnState()
	{
		foreach (DemoSection section in AllSections)
		{
			Type owner = section.Show.Method.DeclaringType!;
			Assert.AreEqual(owner, section.ResetState.Method.DeclaringType, $"{owner.Name} is reset by another class.");

			if (section.Initialize is not null)
			{
				Assert.AreEqual(owner, section.Initialize.Method.DeclaringType, $"{owner.Name} is initialized by another class.");
			}
		}
	}

	/// <summary>The split moved code, not the demo: sections still appear in the order they always did.</summary>
	[TestMethod]
	public void SectionOrder_MatchesTheDemo()
	{
		CollectionAssert.AreEqual(WidgetDemoOrder, NamesOf(DemoSections.WidgetDemos));
		CollectionAssert.AreEqual(AdvancedDemoOrder, NamesOf(DemoSections.AdvancedDemos));
	}

	private static string[] NamesOf(IReadOnlyList<DemoSection> sections) =>
		[.. sections.Select(section => section.Show.Method.DeclaringType!.Name)];

	private static bool HasParameterlessMethod(Type type, string name) =>
		type.GetMethod(name, AnyStatic, binder: null, Type.EmptyTypes, modifiers: null) is not null;
}

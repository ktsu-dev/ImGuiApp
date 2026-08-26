// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiWidgetsDemo.UITests;

using System;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiWidgetsDemo through the headless harness: visits all five tabs, expands each demo
/// section, and operates the widgets inside them.
/// </summary>
[TestClass]
public sealed class WidgetsDemoUITests
{
	// The demo nests long sections inside a tab bar, so the content runs well past the harness
	// default of 720. A viewport this size keeps expanded sections reachable by a click.
	private static readonly HarnessOptions DemoViewport = new() { Width = 1600, Height = 1400 };

	private const string WidgetDemosTab = "Widget Demos";
	private const string AdvancedDemosTab = "Advanced Demos";
	private const string ComparisonTab = "Hexa vs ktsu";
	private const string NetNewTab = "Net New";
	private const string DialogsTab = "Dialogs";

	private static readonly string[] AllTabs =
	[
		WidgetDemosTab, AdvancedDemosTab, ComparisonTab, NetNewTab, DialogsTab,
	];

	private static readonly string[] WidgetDemoSections =
	[
		"Mobile - Form Controls", "Knobs", "Radial Progress Bar", "Color Indicators",
		"Combo Boxes", "Text Utilities", "Scoped Utilities", "Tree View",
		"Mobile - Decorators", "Mobile - Containers & Loaders",
	];

	private static readonly string[] AdvancedDemoSections =
	[
		"Images & Icons", "ImageCanvas", "TabPanel", "SearchBox", "Grid Layout", "Divider Container",
	];

	private static readonly string[] NetNewSections =
	[
		"Breadcrumb", "Buttons", "Date and year pickers", "Flame graph", "File tree view",
		"Rename and message dialogs", "Curve and bezier fields", "Multi-curve editor", "Sequencer",
	];

	private ImGuiAppHarness harness = null!;

	[TestInitialize]
	public void SetUp()
	{
		// Demo state lives in statics that outlive a harness, so each test starts from a known slate.
		ImGuiWidgetsDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiWidgetsDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// Reports whether an item was drawn in the frame just rendered. Probe.Rect remembers the last
	/// position an item ever occupied, so it answers "was this ever drawn" rather than "is this on
	/// screen now" -- and almost everything here lives behind a tab or a collapsed header.
	/// </summary>
	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	// A click renders three frames of its own -- the pointer move, the press and the release -- and
	// the demo animates nothing between frames, so the layout has already settled by the time the
	// release frame is drawn and one further frame is enough to confirm it. Measured across all
	// twenty-five sections: every one of them expands to a pixel-identical result at this depth,
	// while the suite renders a third fewer frames. Raising this back to three costs about a third
	// of the run time and buys nothing.
	private const int SettleFrames = 1;

	private void OpenTab(string tab)
	{
		harness.Click(tab);
		harness.Step(SettleFrames);
	}

	/// <summary>Expands a collapsing header. The demo's headers all start collapsed.</summary>
	private void ExpandSection(string header)
	{
		harness.Click(header);
		harness.Step(SettleFrames);
	}

	private void OpenSection(string tab, string header)
	{
		OpenTab(tab);
		ExpandSection(header);
	}

	/// <summary>Copies the pixels of the frame just rendered, so a later frame can be compared to it.</summary>
	private byte[] Snapshot() => harness.Target.Pixels.ToArray();

	/// <summary>
	/// Asserts that expanding a section drew something beyond the header itself.
	/// </summary>
	/// <remarks>
	/// A header scrolled outside the tab's scrolling region is still submitted every frame, so it
	/// stays "visible" and a click on it silently does nothing. An assertion on the header alone
	/// therefore passes whether or not the section ever opened, which is what makes it a weak
	/// guard. A section's content is drawn below its header, so requiring a change outside the
	/// header's own rectangle is what separates a real expansion from a click that went nowhere.
	/// The demo animates nothing between frames, so in the collapsed case the two frames are
	/// byte for byte identical outside that rectangle.
	/// </remarks>
	/// <param name="section">The section that was just expanded.</param>
	/// <param name="collapsed">The frame captured before the section was expanded.</param>
	private void AssertSectionDrewContent(string section, byte[] collapsed)
	{
		Rectangle header = harness.Probe.Rect(section)
			?? throw new InvalidOperationException($"Section '{section}' was never recorded by the probe.");

		Span<byte> expanded = harness.Target.Pixels;
		int width = harness.Options.Width;
		int height = harness.Options.Height;

		for (int y = 0; y < height; y++)
		{
			bool rowInHeader = y >= header.MinY && y < header.MaxY;

			for (int x = 0; x < width; x++)
			{
				if (rowInHeader && x >= header.MinX && x < header.MaxX)
				{
					continue;
				}

				int i = ((y * width) + x) * 4;

				if (collapsed[i] != expanded[i]
					|| collapsed[i + 1] != expanded[i + 1]
					|| collapsed[i + 2] != expanded[i + 2]
					|| collapsed[i + 3] != expanded[i + 3])
				{
					return;
				}
			}
		}

		Assert.Fail(
			$"Section '{section}' drew nothing outside its own header when expanded, so it never opened. "
			+ "The click most likely landed outside the tab's scrolling region.");
	}

	[TestMethod]
	public void Config_WiresUpTheDemoCallbacks()
	{
		ImGuiAppConfig config = ImGuiWidgetsDemo.BuildConfig();

		Assert.AreEqual("ImGuiWidgets - Complete Library Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnConfigureFonts, "The demo registers Material Icons for the Hexa widgets.");
	}

	[TestMethod]
	public void EveryTab_IsRendered()
	{
		foreach (string tab in AllTabs)
		{
			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' was never rendered.");
		}
	}

	[TestMethod]
	public void EveryTab_CanBeOpenedWithoutError()
	{
		foreach (string tab in AllTabs)
		{
			OpenTab(tab);

			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' vanished after being selected.");
			Assert.IsNotNull(harness.Capture().FindBounds(p => p.A > 0), $"Tab '{tab}' rendered a blank frame.");
		}
	}

	[TestMethod]
	public void WidgetDemos_ListsEverySection()
	{
		OpenTab(WidgetDemosTab);

		foreach (string section in WidgetDemoSections)
		{
			Assert.IsTrue(IsVisible(section), $"Section '{section}' was never rendered.");
		}
	}

	[TestMethod]
	public void AdvancedDemos_ListsEverySection()
	{
		OpenTab(AdvancedDemosTab);

		foreach (string section in AdvancedDemoSections)
		{
			Assert.IsTrue(IsVisible(section), $"Section '{section}' was never rendered.");
		}
	}

	[TestMethod]
	public void NetNew_ListsEverySection()
	{
		OpenTab(NetNewTab);

		foreach (string section in NetNewSections)
		{
			Assert.IsTrue(IsVisible(section), $"Section '{section}' was never rendered.");
		}
	}

	[TestMethod]
	public void EverySection_CanBeExpandedWithoutError()
	{
		// Expanding a section is what actually runs the widget code behind it, so this is the
		// broadest guard in the suite: a widget that throws on submission fails here whatever else
		// the more specific tests happen to cover.
		foreach ((string tab, string[] sections) in new[]
		{
			(WidgetDemosTab, WidgetDemoSections),
			(AdvancedDemosTab, AdvancedDemoSections),
			(NetNewTab, NetNewSections),
		})
		{
			OpenTab(tab);

			foreach (string section in sections)
			{
				byte[] collapsed = Snapshot();
				ExpandSection(section);
				Assert.IsTrue(IsVisible(section), $"Section '{section}' vanished when expanded.");
				AssertSectionDrewContent(section, collapsed);

				// Collapse again so the next section starts from a comparable layout, rather than
				// being pushed off the bottom by everything expanded above it. This is load
				// bearing, not tidiness: with it removed, later sections are pushed outside the
				// tab's scrolling region, their headers stop being clickable, and eight of the
				// twenty-five stop opening at all.
				ExpandSection(section);
			}
		}
	}

	[TestMethod]
	public void MobileFormControls_ShowEveryControl()
	{
		OpenSection(WidgetDemosTab, "Mobile - Form Controls");

		foreach (string widget in new[]
		{
			"Wi-Fi##switchWifi", "Bluetooth##switchBluetooth", "##viewMode",
			"All##chip0", "Unread##chip1", "##value", "Price##rangePrice",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The form controls section is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void MobileFormControls_SwitchTogglesTheBoundValue()
	{
		OpenSection(WidgetDemosTab, "Mobile - Form Controls");
		Assert.IsTrue(ImGuiWidgetsDemo.SwitchWifi, "Precondition: the Wi-Fi switch starts on.");

		harness.Click("Wi-Fi##switchWifi");
		harness.Step(2);

		Assert.IsFalse(ImGuiWidgetsDemo.SwitchWifi, "Clicking the switch should turn Wi-Fi off.");
	}

	[TestMethod]
	public void Knobs_ShowEveryVariantAndReset()
	{
		OpenSection(WidgetDemosTab, "Knobs");

		foreach (string knob in new[] { "Wiper", "Wiper Only", "Wiper Dot", "Tick", "Stepped", "Space" })
		{
			Assert.IsTrue(IsVisible(knob), $"The knobs section is missing '{knob}'.");
		}

		Assert.IsTrue(IsVisible("Reset to 0.5"), "The knobs section should offer its reset button.");
	}

	[TestMethod]
	public void RadialProgressBar_ButtonsDriveTheValue()
	{
		OpenSection(WidgetDemosTab, "Radial Progress Bar");

		harness.Click("Set to 100%");
		harness.Step(2);
		Assert.AreEqual(1.0f, ImGuiWidgetsDemo.ProgressValue, 0.001f);

		harness.Click("Reset to 0%");
		harness.Step(2);
		Assert.AreEqual(0.0f, ImGuiWidgetsDemo.ProgressValue, 0.001f);

		harness.Click("Set to 50%");
		harness.Step(2);
		Assert.AreEqual(0.5f, ImGuiWidgetsDemo.ProgressValue, 0.001f);
	}

	[TestMethod]
	public void RadialProgressBar_OffersItsTimers()
	{
		OpenSection(WidgetDemosTab, "Radial Progress Bar");

		foreach (string control in new[]
		{
			"Animate", "Speed", "Run Countdown", "Reset Countdown", "Run Count-Up", "Reset Count-Up",
		})
		{
			Assert.IsTrue(IsVisible(control), $"The radial progress section is missing '{control}'.");
		}
	}

	[TestMethod]
	public void ScopedUtilities_ShowDisabledControlsAndScopedIds()
	{
		OpenSection(WidgetDemosTab, "Scoped Utilities");

		Assert.IsTrue(IsVisible("Disabled Checkbox"), "scoped disable");
		Assert.IsTrue(IsVisible("Disabled Button"), "scoped disable");

		// ScopedId pushes a probe scope, which is what keeps three identically labelled buttons
		// apart. Without it they would collide and the probe would refuse to resolve them.
		foreach (string scoped in new[] { "0/Same Label", "1/Same Label", "2/Same Label" })
		{
			Assert.IsTrue(IsVisible(scoped), $"ScopedId should have qualified '{scoped}'.");
		}
	}

	[TestMethod]
	public void TreeView_ShowsItsNodes()
	{
		OpenSection(WidgetDemosTab, "Tree View");

		foreach (string node in new[] { "Parent Node 1", "Child 1", "Grandchild", "Child 2", "Parent Node 2" })
		{
			Assert.IsTrue(IsVisible(node), $"The tree is missing '{node}'.");
		}
	}

	[TestMethod]
	public void MobileDecorators_ShowAvatarsBadgesAndRatings()
	{
		OpenSection(WidgetDemosTab, "Mobile - Decorators");

		foreach (string widget in new[]
		{
			"AvatarImage", "AvatarJD", "AvatarAB", "AvatarGrace",
			"Inbox", "Messages", "Updates", "Count",
			"WholeRating", "HalfRating", "ReadOnlyRating",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The decorators section is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void MobileContainers_OfferTheirControls()
	{
		OpenSection(WidgetDemosTab, "Mobile - Containers & Loaders");

		Assert.IsTrue(IsVisible("Action##cardAction"), "card action");
		Assert.IsTrue(IsVisible("Loading##skeletonToggle"), "skeleton toggle");
	}

	[TestMethod]
	public void ComboBoxes_AreAddressableAndTypeSafe()
	{
		OpenSection(WidgetDemosTab, "Combo Boxes");

		foreach (string combo in new[] { "Enum Combo", "String Combo", "Strong String Combo" })
		{
			Assert.IsTrue(IsVisible(combo), $"The combo section is missing '{combo}'.");
		}
	}

	[TestMethod]
	public void ImageCanvas_OffersFitAndOneToOne()
	{
		OpenSection(AdvancedDemosTab, "ImageCanvas");

		Assert.IsTrue(IsVisible("Fit"), "fit button");
		Assert.IsTrue(IsVisible("1:1"), "one-to-one button");

		harness.Click("Fit");
		harness.Step(2);
		Assert.IsTrue(IsVisible("Fit"), "The canvas should survive being fitted.");
	}

	[TestMethod]
	public void TabPanel_AddsATabOnDemand()
	{
		OpenSection(AdvancedDemosTab, "TabPanel");

		foreach (string control in new[] { "Mark Active Tab Dirty", "Mark Active Tab Clean", "Add New Tab" })
		{
			Assert.IsTrue(IsVisible(control), $"The tab panel section is missing '{control}'.");
		}

		int before = ImGuiWidgetsDemo.TabPanel.Tabs.Count;
		harness.Click("Add New Tab");
		harness.Step(3);

		Assert.AreEqual(before + 1, ImGuiWidgetsDemo.TabPanel.Tabs.Count, "Adding a tab should grow the panel.");
	}

	[TestMethod]
	public void TabPanel_DirtyAndCleanMarkersApply()
	{
		OpenSection(AdvancedDemosTab, "TabPanel");

		harness.Click("Mark Active Tab Dirty");
		harness.Step(2);
		harness.Click("Mark Active Tab Clean");
		harness.Step(2);

		Assert.IsTrue(IsVisible("Mark Active Tab Clean"), "The panel should survive the dirty/clean cycle.");
	}

	[TestMethod]
	public void SearchBox_OffersEveryFilterMode()
	{
		OpenSection(AdvancedDemosTab, "SearchBox");

		foreach (string box in new[]
		{
			"##BasicSearch", "##FilteredSearch", "##RankedSearch", "##GlobSearch", "##RegexSearch",
		})
		{
			Assert.IsTrue(IsVisible(box), $"The search box section is missing '{box}'.");
		}
	}

	[TestMethod]
	public void SearchBox_AcceptsTypedInput()
	{
		OpenSection(AdvancedDemosTab, "SearchBox");
		byte[] beforeTyping = harness.Target.Pixels.ToArray();

		harness.Click("##BasicSearch");
		harness.Keyboard.Type("apple");
		harness.Step(3);

		Assert.IsFalse(beforeTyping.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Typing a filter should change what the search box shows.");
	}

	[TestMethod]
	public void GridLayout_ExposesItsConfiguration()
	{
		OpenSection(AdvancedDemosTab, "Grid Layout");

		foreach (string control in new[]
		{
			"Show Grid Debug Draw", "Show Icon Debug Draw", "Big Icons", "Fit to Contents",
			"Items", "Height", "Order", "Icon Layout",
		})
		{
			Assert.IsTrue(IsVisible(control), $"The grid section is missing '{control}'.");
		}
	}

	[TestMethod]
	public void DividerContainer_DrawsBothZones()
	{
		OpenSection(AdvancedDemosTab, "Divider Container");

		Assert.IsTrue(IsVisible("DividerDemoLeft"), "left zone");
		Assert.IsTrue(IsVisible("DividerDemoRight"), "right zone");
	}

	[TestMethod]
	public void Comparison_DrawsTheSharedControls()
	{
		OpenTab(ComparisonTab);

		foreach (string control in new[] { "##ktsuSwitch", "Shared progress", "Shared image size" })
		{
			Assert.IsTrue(IsVisible(control), $"The comparison tab is missing '{control}'.");
		}
	}

	[TestMethod]
	public void Dialogs_OffersBothImplementationsOfEveryDialog()
	{
		OpenTab(DialogsTab);

		foreach (string button in new[]
		{
			"Open##ktsuOpenFile", "Open##hexaOpenFile",
			"Pick##ktsuFolder", "Pick##hexaFolder",
			"Save##ktsuSave", "Save##hexaSave",
			"Ask##ktsuMessage", "Ask##hexaMessage",
		})
		{
			Assert.IsTrue(IsVisible(button), $"The dialogs tab is missing '{button}'.");
		}
	}

	[TestMethod]
	public void Dialogs_KtsuMessageBoxOpens()
	{
		OpenTab(DialogsTab);

		harness.Click("Ask##ktsuMessage");
		harness.Step(3);

		// ktsu's MessageOK is a Prompt with a single OK button, which the popup library marks.
		Assert.IsTrue(IsVisible("prompt/OK"), "The ktsu message box did not open.");
	}

	[TestMethod]
	public void Demo_RendersIdenticallyAcrossRuns()
	{
		byte[] first = harness.Target.Pixels.ToArray();

		harness.Dispose();
		ImGuiWidgetsDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiWidgetsDemo.BuildConfig(), DemoViewport);
		harness.Step(3);

		CollectionAssert.AreEqual(first, harness.Target.Pixels.ToArray(), "Two runs of the same scenario should match.");
	}
}

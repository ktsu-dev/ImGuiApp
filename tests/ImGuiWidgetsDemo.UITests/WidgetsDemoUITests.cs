// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiWidgetsDemo.UITests;

using System;
using System.Collections.Generic;

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

	private static readonly int[] FirstHunkOnly = [0];

	private static readonly string[] AllTabs =
	[
		WidgetDemosTab, AdvancedDemosTab, ComparisonTab, NetNewTab, DialogsTab,
	];

	private static readonly string[] WidgetDemoSections =
	[
		"Mobile - Form Controls", "Property Grid", "Knobs", "Radial Progress Bar", "Color Indicators",
		"Combo Boxes", "Text Utilities", "Scoped Utilities", "Tree View",
		"Mobile - Decorators", "Mobile - Containers & Loaders", "Waveform", "Transport Scrubber", "Spectrum Analyzer", "Stereo Meters", "Channel Fader", "Color wheels", "Diff view", "Toolbar",
		"Piano Keyboard",
		"Parametric EQ",
		"Step Grid",
		"Envelope Editor",
	];

	private static readonly string[] AdvancedDemoSections =
	[
		"Images & Icons", "ImageCanvas", "Levels Control", "Asset Browser", "Gradient Editor", "Swatch Palette", "Image Compare", "Crop Overlay", "Pixel Loupe", "TabPanel", "SearchBox", "Grid Layout",
		"Virtual Table", "Data Table", "Divider Container",
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
	public void PropertyGrid_EditsThePropertiesItLists()
	{
		OpenSection(WidgetDemosTab, "Property Grid");

		Assert.IsTrue(IsVisible("Item Name"), "The property grid drew no row for the item name.");
		Assert.IsTrue(IsVisible("Icon/thumbnail"), "The image row drew no preview.");

		harness.Click("Tags/add");
		harness.Step(SettleFrames);

		Assert.HasCount(3, ImGuiWidgetsDemo.PropertyTags, "The list row's add button appended nothing.");
		Assert.IsGreaterThan(0, ImGuiWidgetsDemo.PropertyEditCount, "The grid reported no edit for the appended element.");
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
	public void TimecodeFieldDemo_IncrementAdvancesTheFrame()
	{
		OpenSection(WidgetDemosTab, "Timecode Field");

		// The field's buttons are marked inside its own scope, so the name is qualified by the
		// demo window as well as by the field's label.
		IReadOnlyList<string> increment = harness.Probe.Matches("Position/inc");
		Assert.HasCount(1, increment, "The position field's increment button should be marked exactly once.");
		harness.Click(increment[0]);
		harness.Step(SettleFrames);

		Assert.AreEqual(1, TimecodeFieldDemo.Frame);
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

		// The branch and leaf section above the scope form. These are drawn inside branch callbacks, so
		// their appearing at all is what proves an expanded branch runs its body.
		foreach (string node in new[] { "Lemon", "Lime", "Apple", "Carrot" })
		{
			Assert.IsTrue(IsVisible(node), $"The branch and leaf tree is missing '{node}'.");
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
	public void Waveform_ClickSeeksThePlayhead()
	{
		OpenSection(WidgetDemosTab, "Waveform");

		Assert.IsTrue(IsVisible("##demoWaveform"), "The waveform section drew no waveform.");
		Assert.AreEqual(0f, WaveformDemo.Playhead, "Precondition: the demo starts with the playhead at the top.");

		// Three quarters of the way across: clear of both loop edges, which sit at a quarter and a half.
		Rectangle rect = harness.Probe.Rect("##demoWaveform")
			?? throw new InvalidOperationException("The waveform was never recorded by the probe.");
		harness.Mouse.Click(rect.MinX + (rect.Width * 0.75f), rect.MinY + (rect.Height * 0.5f));
		harness.Step();

		Assert.AreEqual(4.5f, WaveformDemo.Playhead, 0.2f, "A click on the waveform did not seek to where it landed.");
	}

	[TestMethod]
	public void StepGrid_ClickTogglesACell()
	{
		OpenSection(WidgetDemosTab, "Step Grid");

		Assert.IsTrue(IsVisible("##demoStepGrid"), "The step grid section drew no grid.");
		int before = StepGridDemo.StepsOn;

		// The clap row (the last of four) starts empty; aim at its second step.
		Rectangle rect = harness.Probe.Rect("##demoStepGrid")
			?? throw new InvalidOperationException("The step grid was never recorded by the probe.");
		harness.Mouse.Click(rect.MinX + (rect.Width * 1.5f / 16f), rect.MinY + (rect.Height * 3.5f / 4f));
		harness.Step();

		Assert.AreEqual(before + 1, StepGridDemo.StepsOn, "A click on an empty cell did not turn it on.");
		Assert.IsTrue(StepGridDemo.CurrentPattern[(3 * 16) + 1], "The click landed on the wrong cell.");
	}

	[TestMethod]
	public void WaveformDemo_LongClipIsDrawn()
	{
		OpenSection(WidgetDemosTab, "Waveform");

		// The long clip's peak cache is built on a worker thread the first time the section is
		// shown, so the waveform appears some frames later rather than on the first one.
		for (int frame = 0; frame < 300 && !IsVisible("Waveform/long clip/scrollbar"); frame++)
		{
			harness.Step();
		}

		Assert.IsTrue(IsVisible("Waveform/long clip/scrollbar"), "The long clip's zoomable waveform was never drawn.");
	}

	[TestMethod]
	public void TransportScrubberDemo_ClickSeeks()
	{
		OpenSection(WidgetDemosTab, "Transport Scrubber");

		Assert.IsTrue(IsVisible("Transport Scrubber/scrubber/track"), "The transport scrubber section drew no track.");
		Assert.AreEqual(0f, TransportScrubberDemo.Playhead, "Precondition: the demo starts with the playhead at the top.");

		// The middle of a 30-second clip: well clear of the in and out points, which sit at 4 and 10.
		Rectangle rect = harness.Probe.Rect("Transport Scrubber/scrubber/track")
			?? throw new InvalidOperationException("The scrubber's track was never recorded by the probe.");
		harness.Mouse.Click(rect.MinX + (rect.Width * 0.5f), rect.MinY + (rect.Height * 0.5f));
		harness.Step();

		Assert.AreEqual(15f, TransportScrubberDemo.Playhead, 0.1f, "A click on the scrubber's track did not seek to where it landed.");
	}

	[TestMethod]
	public void DiffView_DrawsAHunkPerViewAndTicksTheOneThatWasClicked()
	{
		OpenSection(WidgetDemosTab, "Diff view");

		Assert.IsTrue(IsVisible("##diffOne/[0]/heading"), "The selectable view drew no heading for its first hunk.");
		Assert.IsTrue(IsVisible("##diffTwo/[0]/heading"), "The side-by-side view drew no heading for its first hunk.");
		Assert.IsTrue(IsVisible("##diffThree/[0]/heading"), "The view with neither option drew no heading for its first hunk.");

		Assert.IsEmpty(DiffViewDemo.FirstSelected, "Precondition: the demo starts with nothing staged.");

		harness.Click("##diffOne/[0]/select");
		harness.Step(2);

		Assert.AreSequenceEqual(
			FirstHunkOnly,
			DiffViewDemo.FirstSelected,
			"Ticking a hunk's checkbox is the whole point of the widget, so it has to reach the caller's set.");
	}

	[TestMethod]
	public void Toolbar_ButtonsReportTheirClicksAndTogglesLatch()
	{
		OpenSection(WidgetDemosTab, "Toolbar");

		Assert.IsTrue(IsVisible("##editToolbar"), "The single-line toolbar was not drawn.");
		Assert.IsTrue(IsVisible("##transportToolbar"), "The two-line toolbar was not drawn.");

		harness.Click("##editToolbar/Save");
		harness.Step(2);
		Assert.AreEqual("Save", ToolbarDemo.LastAction, "Clicking Save did not reach the demo.");

		harness.Click("##transportToolbar/Play");
		harness.Step(2);
		Assert.AreEqual("Play", ToolbarDemo.LastAction, "Clicking Play in the two-line toolbar did not reach the demo.");

		harness.Click("##editToolbar/Redo");
		harness.Step(2);
		Assert.AreEqual("Play", ToolbarDemo.LastAction, "The disabled Redo button reported a click.");

		harness.Click("##editToolbar/Bold");
		harness.Step(2);
		Assert.IsTrue(ToolbarDemo.Bold, "The Bold toggle did not turn on.");
	}

	[TestMethod]
	public void ColorWheels_DraggingGainReachesTheDemoAndResetClearsIt()
	{
		OpenSection(WidgetDemosTab, "Color wheels");

		Assert.IsTrue(IsVisible("##grade/Lift"), "The lift wheel was not drawn.");
		Assert.IsTrue(IsVisible("##grade/Gamma"), "The gamma wheel was not drawn.");
		Assert.IsTrue(IsVisible("##grade/Gain"), "The gain wheel was not drawn.");

		Rectangle gain = harness.Probe.Rect("##grade/Gain")!.Value;
		float x = gain.MinX + (gain.Width / 2f);
		float y = gain.MinY + (gain.Height / 2f);
		harness.Mouse.Drag(x, y, x + 30f, y - 30f);
		harness.Step(2);

		Assert.IsTrue(ColorWheelDemo.Gain.Strength > 0f, "Dragging the gain wheel did not reach the demo's value.");
		Assert.IsTrue(ColorWheelDemo.Lift.IsNeutral, "Dragging the gain wheel moved lift.");

		harness.Click("Reset all");
		harness.Step(2);

		Assert.IsTrue(ColorWheelDemo.Gain.IsNeutral, "Reset all left the gain wheel pushed.");
	}

	[TestMethod]
	public void LevelsDemo_DraggingTheBlackPointChangesTheLevels()
	{
		OpenSection(AdvancedDemosTab, "Levels Control");

		Assert.IsTrue(IsVisible("levels_demo/input"), "The levels control's input handles were not drawn.");

		Rectangle input = harness.Probe.Rect("levels_demo/input")!.Value;
		float y = input.MinY + (input.Height / 2f);
		harness.Mouse.Drag(input.MinX + (input.Width * 0.01f), y, input.MinX + (input.Width * 0.3f), y);
		harness.Step(2);

		Assert.IsTrue(LevelsDemo.Levels.InputBlack > 0.2f, $"Dragging the black point left it at {LevelsDemo.Levels.InputBlack}.");

		harness.Click("Reset levels");
		harness.Step(2);

		Assert.AreEqual(ktsu.ImGui.Widgets.LevelsAdjustment.Identity, LevelsDemo.Levels, "Reset levels left the adjustment changed.");
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
	public void AssetBrowserDemo_ClickSelects()
	{
		OpenSection(AdvancedDemosTab, "Asset Browser");

		harness.Click("asset_browser_demo/[2]");
		harness.Step(2);

		Assert.AreSequenceEqual([2], AssetBrowserDemo.State.SelectedIndices);
		Assert.IsTrue(IsVisible("Asset drop target"), "The drop target sits under the browser.");
	}

	[TestMethod]
	public void GradientDemo_ClickingTheBarAddsAStop()
	{
		OpenSection(AdvancedDemosTab, "Gradient Editor");

		Rectangle bar = harness.Probe.Rect("gradient_demo/bar")
			?? throw new InvalidOperationException("The gradient bar was never recorded by the probe.");
		harness.Mouse.Click(bar.MinX + (bar.Width * 0.9f), bar.MinY + (bar.Height * 0.3f));
		harness.Step(2);

		Assert.AreEqual(5, GradientDemo.StopCount, "Clicking empty space on the bar should add a stop.");
	}

	[TestMethod]
	public void SwatchPaletteDemo_ClickingASwatchSelectsIt()
	{
		OpenSection(AdvancedDemosTab, "Swatch Palette");

		harness.Click("swatch_demo/3");
		harness.Step(2);

		Assert.AreEqual(3, SwatchPaletteDemo.Selected, "Clicking a swatch should select it.");
	}

	[TestMethod]
	public void ImageCompareDemo_DraggingTheDividerMovesTheSplit()
	{
		OpenSection(AdvancedDemosTab, "Image Compare");

		Rectangle divider = harness.Probe.Rect("compare_demo/divider")!.Value;
		float x = divider.MinX + (divider.Width / 2f);
		float y = divider.MinY + (divider.Height / 2f);
		harness.Mouse.Drag(x, y, x + 200f, y);
		harness.Step(2);

		Assert.IsGreaterThan(0.55f, ImageCompareDemo.Split, "Dragging the divider should move the split right.");
	}

	[TestMethod]
	public void CropDemo_DraggingTheBodyMovesTheCrop()
	{
		OpenSection(AdvancedDemosTab, "Crop Overlay");

		Assert.IsTrue(IsVisible("crop_demo/body"), "The crop overlay did not mark its body.");
		float before = CropDemo.Crop.Center.X;

		Rectangle body = harness.Probe.Rect("crop_demo/body")!.Value;
		float x = body.MinX + (body.Width / 2f);
		float y = body.MinY + (body.Height / 2f);
		harness.Mouse.Drag(x, y, x + 30f, y);
		harness.Step(2);

		Assert.IsTrue(CropDemo.Crop.Center.X > before, "Dragging the crop did not move it.");
	}

	[TestMethod]
	public void PixelLoupe_HoveringTheImageReportsAPixel()
	{
		OpenSection(AdvancedDemosTab, "Pixel Loupe");

		Assert.IsTrue(IsVisible("pixel_loupe"), "The loupe should be drawn beside the canvas.");
		Assert.AreEqual(-1, PixelLoupeDemo.PixelX, "Nothing has been hovered yet.");

		Rectangle canvas = harness.Probe.Rect("pixel_loupe_canvas")
			?? throw new InvalidOperationException("The loupe's canvas was never recorded by the probe.");
		harness.Mouse.MoveTo(canvas.MinX + (canvas.Width / 2), canvas.MinY + (canvas.Height / 2));
		harness.Step(2);

		Assert.IsTrue(PixelLoupeDemo.PixelX >= 0, "Hovering the middle of the image should pick a pixel.");
		Assert.IsTrue(PixelLoupeDemo.PixelY >= 0, "Hovering the middle of the image should pick a pixel.");
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

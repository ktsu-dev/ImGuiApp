// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiAppDemo.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.App;
using ktsu.ImGui.Examples.App.Demos;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiAppDemo through the headless harness. The demo is a tab bar over fifteen sections,
/// including the ImGuizmo, ImNodes and ImPlot extensions, so the broadest guarantee here is that
/// every tab renders and its controls respond.
/// </summary>
[TestClass]
public sealed class AppDemoUITests
{
	// The demo's tabs are long pages, and its lower content sits past the harness default of 720.
	private static readonly HarnessOptions DemoViewport = new() { Width = 1600, Height = 1200 };

	private const string BasicWidgetsTab = "Basic Widgets";
	private const string LayoutTab = "Layout & Tables";
	private const string GraphicsTab = "Graphics & Drawing";
	private const string InputTab = "Input & Interaction";
	private const string AnimationTab = "Animation & Effects";
	private const string ImGuizmoTab = "ImGuizmo 3D Gizmos";
	private const string Viewport3DTab = "3D Viewport";
	private const string ImNodesTab = "ImNodes Editor";
	private const string ImPlotTab = "ImPlot Charts";
	private const string CleanImNodesTab = "Clean ImNodes";
	private const string UtilitiesTab = "Utilities & Tools";

	private ImGuiAppHarness harness = null!;

	[TestInitialize]
	public void SetUp()
	{
		// Demo state lives in statics that outlive a harness, so each test starts from a known slate.
		ImGuiAppDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiAppDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// Reports whether an item was drawn in the frame just rendered. Probe.Rect remembers the last
	/// position an item ever occupied, so it answers "was this ever drawn" rather than "is this on
	/// screen now" -- and with fifteen tabs, most items are off screen most of the time.
	/// </summary>
	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private void OpenTab(string tab)
	{
		harness.Click(tab);
		harness.Step(4);
	}

	private void ClickIn(string tab, string item)
	{
		OpenTab(tab);
		harness.Click(item);
		harness.Step(3);
	}

	[TestMethod]
	public void Config_WiresUpTheDemoCallbacks()
	{
		ImGuiAppConfig config = ImGuiAppDemo.BuildConfig();

		Assert.AreEqual("ImGuiApp Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnStart);
		Assert.IsNotNull(config.OnConfigureFonts);
		Assert.IsNotNull(config.OnAppMenu);
		Assert.IsFalse(config.SaveIniSettings);
		Assert.IsTrue(config.EnableDocking, "The demo draws a docked window, which requires docking up front.");
		Assert.IsTrue(config.PerformanceSettings.EnableThrottledRendering);
	}

	[TestMethod]
	public void Demo_RegistersEveryTab()
	{
		Assert.HasCount(15, ImGuiAppDemo.TabNames);

		foreach (string tab in ImGuiAppDemo.TabNames)
		{
			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' was never rendered.");
		}
	}

	[TestMethod]
	public void EveryTab_RendersWithoutError()
	{
		// The broadest guard in the suite. Each tab's Render runs only while it is selected, so
		// this is what actually executes all fifteen code paths -- including the three extension
		// tabs, which fault inside native code if ImGuizmo, ImNodes or ImPlot were never handed the
		// ImGui context.
		foreach (string tab in ImGuiAppDemo.TabNames)
		{
			OpenTab(tab);

			Assert.IsTrue(IsVisible(tab), $"Tab '{tab}' vanished after being selected.");
			Assert.IsNotNull(harness.Capture().FindBounds(p => p.A > 0), $"Tab '{tab}' rendered a blank frame.");
		}
	}

	[TestMethod]
	public void BasicWidgets_ShowsItsControls()
	{
		OpenTab(BasicWidgetsTab);

		foreach (string widget in new[]
		{
			"Regular Button", "Small", "Checkbox", "Float Slider", "Angle", "Int Slider", "Float Input",
		})
		{
			Assert.IsTrue(IsVisible(widget), $"The basic widgets tab is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void BasicWidgets_ButtonAndCheckboxRespond()
	{
		OpenTab(BasicWidgetsTab);
		byte[] before = harness.Target.Pixels.ToArray();

		harness.Click("Checkbox");
		harness.Step(2);

		Assert.IsFalse(before.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Toggling the checkbox should change what is drawn.");

		harness.Click("Regular Button");
		harness.Step(2);
		Assert.IsTrue(IsVisible("Regular Button"), "The tab should survive the button being pressed.");
	}

	[TestMethod]
	public void Layout_TableTogglesRespond()
	{
		OpenTab(LayoutTab);

		Assert.IsTrue(IsVisible("Show Headers"), "header toggle");
		Assert.IsTrue(IsVisible("Show Borders"), "border toggle");

		byte[] before = harness.Target.Pixels.ToArray();
		harness.Click("Show Borders");
		harness.Step(2);

		Assert.IsFalse(before.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Turning table borders off should change what is drawn.");
	}

	[TestMethod]
	public void Graphics_CanvasOffersItsControls()
	{
		OpenTab(GraphicsTab);

		Assert.IsTrue(IsVisible("Brush Size"), "brush size");
		Assert.IsTrue(IsVisible("Clear Canvas"), "clear canvas");

		harness.Click("Clear Canvas");
		harness.Step(2);
		Assert.IsTrue(IsVisible("Clear Canvas"), "The canvas should survive being cleared.");
	}

	/// <summary>
	/// The raw-pixel section uploads one picture from each PixelLayout and streams a padded BGR
	/// buffer through UpdateTexture every frame. A stride or layout mistake throws from the upload
	/// rather than drawing wrong, so drawing the tab for a while is the guard.
	/// </summary>
	[TestMethod]
	public void Graphics_UploadsTexturesFromEachPixelLayout()
	{
		OpenTab(GraphicsTab);
		harness.Step(5);

		Assert.IsTrue(IsVisible("Animate streamed pixels"), "The raw-pixel section should be showing.");
		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the raw-pixel section as misuse.");

		harness.Click("Animate streamed pixels");
		harness.Step(2);
		harness.Click("Animate streamed pixels");
		harness.Step(2);
		Assert.IsTrue(IsVisible("Animate streamed pixels"), "The tab should survive pausing and resuming the stream.");
	}

	/// <summary>
	/// The harness's software renderer implements IRenderer3D, so the tab draws its cube into an
	/// offscreen target and shows that as an image. The cube's faces are saturated colours on a
	/// near-grey clear, so saturated pixels inside the viewport are the cube having arrived.
	/// </summary>
	[TestMethod]
	public void Viewport3D_DrawsTheCubeThroughIRenderer3D()
	{
		OpenTab(Viewport3DTab);

		Assert.IsTrue(ImGuiAppDemo.GetTab<Viewport3DDemo>().LastFrameUsedRenderer3D, "The harness offers IRenderer3D, so the tab should use it.");
		Assert.IsGreaterThan(2000, CountSaturatedPixelsIn("Viewport"), "The cube should be drawn inside the viewport.");
	}

	[TestMethod]
	public void Viewport3D_FallsBackToACpuWireframe()
	{
		ClickIn(Viewport3DTab, "Force CPU wireframe");

		Assert.IsFalse(ImGuiAppDemo.GetTab<Viewport3DDemo>().LastFrameUsedRenderer3D, "The fallback should not use IRenderer3D.");
		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the wireframe as misuse.");
		Assert.IsLessThan(200, CountSaturatedPixelsIn("Viewport"), "The wireframe is drawn in grey, not as the shaded faces.");
	}

	[TestMethod]
	public void Viewport3D_DraggingOrbitsAndTheWheelDollies()
	{
		OpenTab(Viewport3DTab);
		ktsu.ImGui.Widgets.ImGuiWidgets.Viewport3DState camera = ImGuiAppDemo.GetTab<Viewport3DDemo>().Camera;
		float yaw = camera.Yaw;
		float distance = camera.Distance;

		Rectangle rect = harness.Probe.Rect("Viewport")
			?? throw new AssertFailedException("The viewport was never marked.");
		float y = rect.MinY + (rect.Height / 2f);
		harness.Mouse.Drag(rect.MinX + (rect.Width * 0.3f), y, rect.MinX + (rect.Width * 0.7f), y);
		harness.Step(2);

		Assert.AreNotEqual(yaw, camera.Yaw, "A horizontal drag should turn the camera about its target.");

		harness.Mouse.Wheel(rect.MinX + (rect.Width / 2f), y, 3);
		harness.Step(2);

		Assert.IsLessThan(distance, camera.Distance, "Wheeling forward should move the camera in.");
	}

	private int CountSaturatedPixelsIn(string item)
	{
		Rectangle rect = harness.Probe.Rect(item)
			?? throw new AssertFailedException($"No item matching '{item}' has been marked.");
		CapturedFrame frame = harness.Capture();

		int count = 0;
		for (int y = rect.MinY; y < rect.MaxY; y++)
		{
			for (int x = rect.MinX; x < rect.MaxX; x++)
			{
				Rgba32 p = frame.GetPixel(x, y);
				int max = Math.Max(p.R, Math.Max(p.G, p.B));
				int min = Math.Min(p.R, Math.Min(p.G, p.B));
				if (max - min > 60)
				{
					count++;
				}
			}
		}

		return count;
	}

	[TestMethod]
	public void Input_OffersDragDropAndPopups()
	{
		OpenTab(InputTab);

		foreach (string widget in new[] { "Drag Source", "Drop Target", "Word Wrap", "Show Modal", "Show Popup" })
		{
			Assert.IsTrue(IsVisible(widget), $"The input tab is missing '{widget}'.");
		}
	}

	[TestMethod]
	public void Input_ModalOpensAndIsDismissed()
	{
		ClickIn(InputTab, "Show Modal");

		// The modal is drawn by ImGui itself rather than by a marked widget, so the observable
		// signal is that the frame changed and the tab is still rendering behind it.
		Assert.IsTrue(IsVisible(InputTab), "The demo should survive opening its modal.");

		harness.Keyboard.Press(Hexa.NET.ImGui.ImGuiKey.Escape);
		harness.Step(3);

		Assert.IsTrue(IsVisible("Show Modal"), "The tab should be interactive again once the modal closes.");
	}

	[TestMethod]
	public void Input_DragAndDropBetweenTheMarkedTargets()
	{
		OpenTab(InputTab);

		Rectangle? source = harness.Probe.Rect("Drag Source");
		Rectangle? target = harness.Probe.Rect("Drop Target");
		Assert.IsNotNull(source, "drag source");
		Assert.IsNotNull(target, "drop target");

		harness.Mouse.Drag(
			(source.Value.MinX + source.Value.MaxX) / 2f,
			(source.Value.MinY + source.Value.MaxY) / 2f,
			(target.Value.MinX + target.Value.MaxX) / 2f,
			(target.Value.MinY + target.Value.MaxY) / 2f,
			steps: 24);
		harness.Step(3);

		Assert.IsTrue(IsVisible("Drop Target"), "The demo should survive a drag between the two zones.");
	}

	[TestMethod]
	public void Animation_OffersItsSpeedControl()
	{
		OpenTab(AnimationTab);

		Assert.IsTrue(IsVisible("Text Speed"), "text speed");
	}

	[TestMethod]
	public void Animation_KeepsMovingBetweenFrames()
	{
		OpenTab(AnimationTab);
		byte[] first = harness.Target.Pixels.ToArray();

		// The tab animates from its Update, so simply advancing time should redraw it differently.
		harness.Step(20);

		Assert.IsFalse(first.AsSpan().SequenceEqual(harness.Target.Pixels),
			"An animation tab should not be showing the same frame twenty frames later.");
	}

	[TestMethod]
	public void ImGuizmo_TabRendersAndTogglesTheGizmo()
	{
		OpenTab(ImGuizmoTab);

		Assert.IsTrue(IsVisible("Enable Gizmo"), "gizmo toggle");
		Assert.IsTrue(IsVisible("Reset Transform"), "reset transform");

		harness.Click("Enable Gizmo");
		harness.Step(3);
		Assert.IsTrue(IsVisible("Enable Gizmo"), "The gizmo tab should survive being toggled.");

		harness.Click("Reset Transform");
		harness.Step(3);
		Assert.IsTrue(IsVisible("Reset Transform"), "The gizmo tab should survive a transform reset.");
	}

	[TestMethod]
	public void ImNodes_TabAddsAndClearsNodes()
	{
		OpenTab(ImNodesTab);

		foreach (string control in new[] { "Add Node", "Clear All", "Reset Demo", "Fix Links" })
		{
			Assert.IsTrue(IsVisible(control), $"The ImNodes tab is missing '{control}'.");
		}

		harness.Click("Add Node");
		harness.Step(3);
		harness.Click("Clear All");
		harness.Step(3);

		Assert.IsTrue(IsVisible("Add Node"), "The node editor should survive adding and clearing nodes.");
	}

	[TestMethod]
	public void ImNodes_LayoutAndCanvasControlsRespond()
	{
		OpenTab(ImNodesTab);

		foreach (string control in new[]
		{
			"Automatic Layout", "Reset Canvas to Origin", "Center Canvas on Nodes", "Show Debug Visualization",
		})
		{
			Assert.IsTrue(IsVisible(control), $"The ImNodes tab is missing '{control}'.");
		}

		harness.Click("Center Canvas on Nodes");
		harness.Step(3);
		Assert.IsTrue(IsVisible("Center Canvas on Nodes"), "The canvas controls should survive being used.");
	}

	[TestMethod]
	public void ImPlot_TabRendersAndRegeneratesData()
	{
		OpenTab(ImPlotTab);

		Assert.IsTrue(IsVisible("Generate New Data"), "regenerate button");

		byte[] before = harness.Target.Pixels.ToArray();
		harness.Click("Generate New Data");
		harness.Step(3);

		Assert.IsFalse(before.AsSpan().SequenceEqual(harness.Target.Pixels),
			"Regenerating the data should redraw the charts.");
	}

	[TestMethod]
	public void CleanImNodes_AddsEachNodeKind()
	{
		OpenTab(CleanImNodesTab);

		foreach (string control in new[] { "Add Input Node", "Add Process Node", "Add Output Node", "Reset Demo", "Clear All" })
		{
			Assert.IsTrue(IsVisible(control), $"The clean ImNodes tab is missing '{control}'.");
		}

		harness.Click("Add Input Node");
		harness.Step(2);
		harness.Click("Add Process Node");
		harness.Step(2);
		harness.Click("Add Output Node");
		harness.Step(3);

		Assert.IsTrue(IsVisible("Add Output Node"), "The editor should survive adding one of each node kind.");
	}

	[TestMethod]
	public void CleanImNodes_PhysicsControlsRespond()
	{
		OpenTab(CleanImNodesTab);

		foreach (string control in new[] { "Run simulation", "Reset all", "Gentle Physics", "Strong Physics" })
		{
			Assert.IsTrue(IsVisible(control), $"The clean ImNodes tab is missing '{control}'.");
		}

		harness.Click("Gentle Physics");
		harness.Step(3);
		harness.Click("Strong Physics");
		harness.Step(3);

		Assert.IsTrue(IsVisible("Strong Physics"), "The physics presets should survive being applied.");
	}

	/// <summary>
	/// Every tunable the simulation has is on this panel, grouped under collapsing headers by the
	/// force it belongs to, so reaching one takes an expand first.
	/// </summary>
	/// <remarks>
	/// The panel itself comes from ktsu.ImGui.NodeEditor rather than this demo, so what this covers is
	/// that a consuming application gets working controls, not just that the demo drew some.
	/// </remarks>
	[TestMethod]
	public void CleanImNodes_EverySettingGroupOffersItsSliders()
	{
		OpenTab(CleanImNodesTab);

		(string Header, string[] Sliders)[] groups =
		[
			("Repulsion", ["Repulsion strength", "Minimum distance"]),
			("Link springs", ["Spring strength", "Rest length", "Left-to-right bias"]),
			("Link shaping", ["Flattening", "Flattening margin", "Untwisting"]),
			("Gravity", ["Gravity strength", "Origin anchor"]),
			("Overlap", ["Clearance", "Maximum correction"]),
			("Motion and limits", ["Damping", "Maximum force", "Maximum speed", "Substep rate", "Settled below"]),
		];

		foreach ((string header, string[] sliders) in groups)
		{
			Assert.IsTrue(IsVisible(header), $"The tuning panel is missing its '{header}' group.");
			harness.Click(header);
			harness.Step(2);

			foreach (string slider in sliders)
			{
				Assert.IsTrue(IsVisible(slider), $"Expanding '{header}' should reveal '{slider}'.");
				DragSliderTrack(slider);
				harness.Step(2);
			}

			// Collapse it again, so the next group's controls are not pushed below the fold.
			harness.Click(header);
			harness.Step(2);
		}
	}

	/// <summary>
	/// Tests that the panel reports what the simulation is doing, which is what says whether a change
	/// to a setting helped.
	/// </summary>
	[TestMethod]
	public void CleanImNodes_ReportsWhatTheSimulationIsDoing()
	{
		OpenTab(CleanImNodesTab);

		foreach (string readout in new[] { "Energy", "Settled", "Substeps", "Substep", "Bodies" })
		{
			Assert.IsTrue(IsVisible(readout), $"The tuning panel is missing its '{readout}' readout.");
		}
	}

	/// <summary>
	/// The Blob Filter node's inputs are typed and unconnected, so the renderer draws an editor on
	/// each of their rows. Widgets submitted inside an ImNodes attribute are exactly where a cursor
	/// or ID stack mistake shows up, and ImGui reports that as misuse rather than by crashing.
	/// </summary>
	[TestMethod]
	public void CleanImNodes_TabDrawsInlineParameterEditorsWithoutError()
	{
		OpenTab(CleanImNodesTab);
		harness.Step(10);

		Assert.AreEqual(
			0,
			ImGui.GetCurrentContext().ErrorCountCurrentFrame,
			"ImGui reported the node editor's drawing as misuse.");
	}

	[TestMethod]
	public void CleanImNodes_FitToViewFitsTheGraph()
	{
		OpenTab(CleanImNodesTab);
		Assert.IsTrue(IsVisible("Zoom"), "The view controls should offer a zoom slider.");

		harness.Click("Fit To View");
		harness.Step(3);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported fitting the view as misuse.");
		Assert.IsTrue(IsVisible("Fit To View"), "The tab should survive fitting the view.");
	}

	/// <summary>
	/// The body hook submits widgets inside every node, between ImNodes' BeginNode and EndNode,
	/// which is where an ID stack or cursor mistake would show. Switched back off afterwards,
	/// because the demo tabs outlive the harness.
	/// </summary>
	[TestMethod]
	public void CleanImNodes_NodeBodyHookAndInlineEditorTogglesDrawWithoutError()
	{
		OpenTab(CleanImNodesTab);

		foreach (string toggle in new[] { "Show connection summary in each node", "Inline parameter editors" })
		{
			harness.Click(toggle);
			harness.Step(3);
			Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, $"ImGui reported '{toggle}' as misuse.");

			harness.Click(toggle);
			harness.Step(3);
			Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, $"ImGui reported undoing '{toggle}' as misuse.");
		}
	}

	/// <summary>
	/// Drags a slider along its track so its value actually moves, which is what drives the caller's
	/// change branch. A slider's item rectangle spans the track *and* the label drawn to its right, so
	/// the drag stays in the left portion to be sure it lands on the track rather than the text.
	/// </summary>
	private void DragSliderTrack(string name)
	{
		Rectangle rect = harness.Probe.Rect(name)
			?? throw new AssertFailedException($"No item matching '{name}' has been marked.");

		float y = rect.MinY + (rect.Height / 2f);
		harness.Mouse.Drag(rect.MinX + (rect.Width * 0.1f), y, rect.MinX + (rect.Width * 0.45f), y);
	}

	[TestMethod]
	public void Utilities_OffersTheBuiltInImGuiWindows()
	{
		OpenTab(UtilitiesTab);

		foreach (string control in new[] { "Show ImGui Demo", "Show Style Editor", "Show Metrics" })
		{
			Assert.IsTrue(IsVisible(control), $"The utilities tab is missing '{control}'.");
		}
	}

	[TestMethod]
	public void Utilities_ImGuiDemoWindowOpens()
	{
		ClickIn(UtilitiesTab, "Show ImGui Demo");

		// ImGui's own demo window is a large addition to the frame, so the pixel count jumps.
		Assert.IsGreaterThan(50000, harness.Capture().CountPixels(p => p.A > 0),
			"Opening ImGui's demo window should add a great deal to the frame.");
	}

	[TestMethod]
	public void AppMenu_HelpAboutOpensTheAboutWindow()
	{
		Assert.IsTrue(IsVisible("Help"), "The demo should offer a Help menu.");
		Assert.IsFalse(ImGuiAppDemo.ShowAbout, "Precondition: the About window starts closed.");

		harness.Click("Help");
		harness.Step(2);
		harness.Click("About");
		harness.Step(3);

		Assert.IsTrue(ImGuiAppDemo.ShowAbout, "Choosing Help > About should open the About window.");
	}

	[TestMethod]
	public void AppMenu_ViewOffersOverlayMode()
	{
		Assert.IsTrue(IsVisible("View"), "The demo should offer a View menu.");

		harness.Click("View");
		harness.Step(2);

		Assert.IsTrue(IsVisible("Overlay mode"), "The View menu should offer overlay mode.");
		Assert.IsFalse(ImGuiAppDemo.OverlayEnabled, "Overlay mode starts off.");
	}

	[TestMethod]
	public void Demo_SurvivesSustainedRendering()
	{
		// Several tabs animate from Update, and the docked-window pump runs every frame. Holding
		// the demo open proves none of that accumulates into a fault.
		harness.Step(60);

		Assert.AreEqual(63, harness.FrameCount);
		Assert.IsNotNull(harness.Capture().FindBounds(p => p.A > 0), "The demo stopped drawing.");
	}

	[TestMethod]
	public void Demo_RendersIdenticallyAcrossRuns()
	{
		byte[] first = harness.Target.Pixels.ToArray();

		harness.Dispose();
		ImGuiAppDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiAppDemo.BuildConfig(), DemoViewport);
		harness.Step(3);

		CollectionAssert.AreEqual(first, harness.Target.Pixels.ToArray(), "Two runs of the same scenario should match.");
	}
}

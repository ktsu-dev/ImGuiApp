// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiMonsoonDemo.UITests;

using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Examples.Monsoon;
using ktsu.ImGui.Monsoon;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiMonsoonDemo through the headless harness with motion off, so every frame is the
/// same: the theme reaches the window, the panels draw, and the buttons and scan grid respond.
/// </summary>
[TestClass]
public sealed class MonsoonDemoUITests
{
	private ImGuiAppHarness? harness;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		ImGuiMonsoonDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiMonsoonDemo.BuildConfig(), new HarnessOptions { Width = 1400, Height = 900 });
		MonsoonEffects.Motion = MonsoonMotion.None;
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
		ImGuiMonsoonDemo.ResetState();
	}

	private ImGuiAppHarness Demo => harness!;

	private bool IsVisible(string name) => Demo.Probe.WasSeenInFrame(name, Demo.FrameCount - 1);

	[TestMethod]
	public void Config_UsesTheDemoTitle()
	{
		ImGuiAppConfig config = ImGuiMonsoonDemo.BuildConfig();
		Assert.AreEqual("ImGui.Monsoon - Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
	}

	[TestMethod]
	public void Demo_DrawsBothPanels()
	{
		Assert.IsTrue(IsVisible(ImGuiMonsoonDemo.StatusPanelId), "The status panel was not drawn.");
		Assert.IsTrue(IsVisible(ImGuiMonsoonDemo.ScanPanelId), "The scan panel was not drawn.");
		Assert.IsTrue(IsVisible(ImGuiMonsoonDemo.ScanGridId), "The scan grid was not drawn.");
	}

	[TestMethod]
	public void Demo_WindowIsBlueBlack()
	{
		CapturedFrame frame = Demo.Capture();
		Rgba32 corner = frame.GetPixel(1395, 895);
		Assert.IsLessThan((byte)40, corner.R, $"Corner {corner} is not dark.");
		Assert.IsLessThan((byte)40, corner.G, $"Corner {corner} is not dark.");
		Assert.IsLessThan((byte)60, corner.B, $"Corner {corner} is not dark.");
		Assert.IsGreaterThanOrEqualTo(corner.R, corner.B, $"Corner {corner} is not blue-black.");
	}

	[TestMethod]
	public void ActionButtons_ReportTheirLabel()
	{
		foreach (string label in ImGuiMonsoonDemo.ActionLabels)
		{
			Demo.Click(label);
			Demo.Step(1);
			Assert.AreEqual(label, ImGuiMonsoonDemo.LastAction);
		}
	}

	[TestMethod]
	public void ScanGrid_ClickMovesTheTarget()
	{
		Rectangle grid = Demo.Probe.Rect(ImGuiMonsoonDemo.ScanGridId)!.Value;
		Vector2 before = ImGuiMonsoonDemo.Target;

		Demo.Mouse.Click(grid.MinX + (grid.Width * 0.2f), grid.MinY + (grid.Height * 0.8f));
		Demo.Step(1);

		Vector2 after = ImGuiMonsoonDemo.Target;
		Assert.AreNotEqual(before, after, "Clicking the grid did not move its target.");
		Assert.IsLessThan(0.3f, after.X);
		Assert.IsGreaterThan(0.7f, after.Y);
	}

	[TestMethod]
	public void MotionSelector_SwitchesMotion()
	{
		Demo.Click("Full motion");
		Demo.Step(1);
		Assert.AreEqual(MonsoonMotion.Full, MonsoonEffects.Motion);
		Demo.Click("Reduced motion");
		Demo.Step(1);
		Assert.AreEqual(MonsoonMotion.Reduced, MonsoonEffects.Motion);
	}

	[TestMethod]
	public void Screenshot_IsWrittenForReview()
	{
		string directory = Environment.GetEnvironmentVariable("MONSOON_SCREENSHOT_DIR") ?? Path.Join(Path.GetTempPath(), "monsoon");
		Directory.CreateDirectory(directory);
		string path = Path.Join(directory, "monsoon-demo.png");
		Demo.Capture().SavePng(path);
		Assert.IsTrue(File.Exists(path));
	}
}

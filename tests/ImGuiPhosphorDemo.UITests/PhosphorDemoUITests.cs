// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiPhosphorDemo.UITests;

using System;
using System.IO;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Examples.Phosphor;
using ktsu.ImGui.Phosphor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiPhosphorDemo through the headless harness: every widget is drawn and marks itself, the
/// buttons act, the tint switch recolours the screen, and the CRT overlay can be turned off.
/// </summary>
[TestClass]
public sealed class PhosphorDemoUITests
{
	private static readonly HarnessOptions DemoViewport = new() { Width = 1400, Height = 900 };

	private ImGuiAppHarness? harness;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp() => ImGuiPhosphorDemo.ResetState();

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
		ImGuiPhosphorDemo.ResetState();
	}

	private ImGuiAppHarness StartDemo()
	{
		harness = ImGuiAppHarness.Start(ImGuiPhosphorDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
		return harness;
	}

	private bool IsVisible(string name) => harness!.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	[TestMethod]
	public void Config_UsesTheDemoTitleAndRenderCallback()
	{
		ImGuiAppConfig config = ImGuiPhosphorDemo.BuildConfig();

		Assert.AreEqual("ImGui.Phosphor - Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
	}

	[TestMethod]
	[DataRow("frame/TERMINAL")]
	[DataRow("frame/SIGNAL")]
	[DataRow("frame/NAVIGATION")]
	[DataRow("frame/SYSTEMS")]
	[DataRow("terminal/log")]
	[DataRow("oscilloscope/carrier")]
	[DataRow("radar/proximity")]
	[DataRow("vector-grid/approach")]
	[DataRow("readout/VELOCITY")]
	[DataRow("status-light/BEACON")]
	[DataRow("segment-bar/FUEL")]
	[DataRow("alert/PROXIMITY WARNING")]
	[DataRow("crt")]
	public void EveryWidget_IsDrawnAndMarksItself(string name)
	{
		StartDemo();

		Assert.IsTrue(IsVisible(name), $"{name} was not drawn in the last frame.");
		Assert.IsNotNull(harness!.Probe.Rect(name));
	}

	[TestMethod]
	public void RunDiagnostic_WritesToTheTerminal()
	{
		ImGuiAppHarness demo = StartDemo();
		int before = ImGuiPhosphorDemo.Terminal.Lines.Count;

		demo.Click("RUN DIAGNOSTIC");
		demo.Step(2);

		Assert.AreEqual(1, ImGuiPhosphorDemo.CommandsRun);
		Assert.AreEqual(before + 2, ImGuiPhosphorDemo.Terminal.Lines.Count);
		Assert.AreEqual("> DIAGNOSTIC 01", ImGuiPhosphorDemo.Terminal.Lines[before]);
	}

	[TestMethod]
	public void Acknowledge_ClearsTheAlert()
	{
		ImGuiAppHarness demo = StartDemo();

		demo.Click("ACKNOWLEDGE");
		demo.Step(2);

		Assert.IsFalse(ImGuiPhosphorDemo.AlertActive);
		Assert.IsFalse(IsVisible("alert/PROXIMITY WARNING"));
	}

	[TestMethod]
	public void CrtToggle_RemovesTheOverlay()
	{
		ImGuiAppHarness demo = StartDemo();

		demo.Click("CRT");
		demo.Step(2);

		Assert.IsFalse(ImGuiPhosphorDemo.ShowCrt);
		Assert.IsFalse(IsVisible("crt"));
	}

	[TestMethod]
	public void TintSwitch_RecoloursTheScreen()
	{
		ImGuiAppHarness demo = StartDemo();
		int greenDominant = demo.Capture().CountPixels(p => p.G > p.R + 40);
		Assert.IsGreaterThan(1000, greenDominant, "The green screen should be mostly green.");

		demo.Click("AMBER");
		demo.Step(2);
		CapturedFrame amber = demo.Capture();

		Assert.AreEqual(PhosphorTint.Amber, PhosphorStyle.Tint);
		Assert.IsGreaterThan(1000, amber.CountPixels(p => p.R > p.G + 40), "The amber screen should lean red.");
		Assert.IsLessThan(greenDominant / 10, amber.CountPixels(p => p.G > p.R + 40), "No green should survive the switch.");
	}

	/// <summary>
	/// Writes a screenshot of each tint to <c>IMGUI_PHOSPHOR_SHOTS</c> when it is set; otherwise does nothing.
	/// </summary>
	[TestMethod]
	public void Screenshots_WhenRequested()
	{
		string? folder = Environment.GetEnvironmentVariable("IMGUI_PHOSPHOR_SHOTS");
		if (string.IsNullOrEmpty(folder))
		{
			return;
		}

		ImGuiAppHarness demo = StartDemo();
		demo.Step(60);
		Directory.CreateDirectory(folder);
		demo.Capture().SavePng(Path.Join(folder, "phosphor-green.png"));
		demo.Click("AMBER");
		demo.Step(30);
		demo.Capture().SavePng(Path.Join(folder, "phosphor-amber.png"));
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiSortieDemo.UITests;

using System;
using System.IO;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Examples.Sortie;
using ktsu.ImGui.Sortie;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives the Sortie demo headlessly: every widget on screen, the condition switch, the hex map and the timer.</summary>
[TestClass]
public sealed class SortieDemoUITests
{
	private static readonly HarnessOptions Viewport = new() { Width = 1400, Height = 900 };

	private ImGuiAppHarness? harness;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp() => ImGuiSortieDemo.ResetState();

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
		harness = null;
		ImGuiSortieDemo.ResetState();
	}

	private ImGuiAppHarness StartDemo()
	{
		harness = ImGuiAppHarness.Start(ImGuiSortieDemo.BuildConfig(), Viewport);
		harness.Step(3);
		return harness;
	}

	private bool IsVisible(string name) => harness!.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	private static int WarmPixels(CapturedFrame frame) => frame.CountPixels(p => p.R > 150 && p.R > p.G + 40 && p.B < 90);

	private static int RedPixels(CapturedFrame frame) => frame.CountPixels(p => p.R > 150 && p.G < 70 && p.B < 70);

	[TestMethod]
	public void BuildConfig_DescribesTheDemo()
	{
		ImGuiAppConfig config = ImGuiSortieDemo.BuildConfig();
		Assert.AreEqual("ImGui.Sortie - Demo", config.Title);
		Assert.IsNotNull(config.OnRender);
		Assert.IsNotNull(config.OnStart);
	}

	[TestMethod]
	[DataRow("heavy-text/TACTICAL COMMAND")]
	[DataRow("panel/PILOT STATUS")]
	[DataRow("panel/SECTOR MAP")]
	[DataRow("panel/RESERVE POWER")]
	[DataRow("sync-gauge/SYNC RATIO")]
	[DataRow("readout/HARMONICS")]
	[DataRow("readout/NEURAL LINK")]
	[DataRow("status-lamp/LIFE SUPPORT")]
	[DataRow("status-lamp/UMBILICAL")]
	[DataRow("sector")]
	[DataRow("countdown/ACTIVE LIMIT")]
	[DataRow("pattern-monitor/WAVE PATTERN")]
	[DataRow("SORTIE")]
	[DataRow("STAND DOWN")]
	[DataRow("RESTORE POWER")]
	public void EveryWidget_IsOnScreen(string name)
	{
		StartDemo();
		Assert.IsTrue(IsVisible(name), $"{name} was not drawn in the last frame");
		Assert.IsNotNull(harness!.Probe.Rect(name));
	}

	[TestMethod]
	public void Sortie_RaisesTheEmergencyAndStandDownClearsIt()
	{
		ImGuiAppHarness demo = StartDemo();
		Assert.IsFalse(IsVisible("emergency/EMERGENCY"));

		demo.Click("SORTIE");
		demo.Step(2);
		Assert.AreEqual(SortieCondition.Emergency, ImGuiSortieDemo.Condition);
		Assert.AreEqual(SortieCondition.Emergency, SortieStyle.Condition);
		Assert.IsTrue(IsVisible("emergency/EMERGENCY"));
		Assert.IsFalse(IsVisible("heavy-text/TACTICAL COMMAND"));

		demo.Click("STAND DOWN");
		demo.Step(2);
		Assert.AreEqual(SortieCondition.Standby, ImGuiSortieDemo.Condition);
		Assert.IsFalse(IsVisible("emergency/EMERGENCY"));
	}

	[TestMethod]
	public void TheReserve_RunsDownOnlyInAnEmergency_AndRestoreRefillsIt()
	{
		ImGuiAppHarness demo = StartDemo();
		demo.Step(5);
		Assert.AreEqual(ImGuiSortieDemo.FullReserve, ImGuiSortieDemo.Reserve);

		demo.Click("SORTIE");
		demo.Step(10);
		Assert.IsLessThan(ImGuiSortieDemo.FullReserve, ImGuiSortieDemo.Reserve);

		demo.Click("RESTORE POWER");
		demo.Step(1);
		Assert.IsGreaterThan(ImGuiSortieDemo.FullReserve - TimeSpan.FromSeconds(1), ImGuiSortieDemo.Reserve);
	}

	[TestMethod]
	public void ClickingTheSectorMap_MarksACellAndClickingAgainClearsIt()
	{
		ImGuiAppHarness demo = StartDemo();
		int before = ImGuiSortieDemo.Sectors.Count;

		demo.Click("sector");
		demo.Step(2);
		Assert.AreEqual(1, ImGuiSortieDemo.SectorsMarked);
		int marked = ImGuiSortieDemo.Sectors.Count;
		Assert.AreNotEqual(before, marked);

		demo.Click("sector");
		demo.Step(2);
		Assert.AreEqual(2, ImGuiSortieDemo.SectorsMarked);
		Assert.AreEqual(before, ImGuiSortieDemo.Sectors.Count);
	}

	[TestMethod]
	public void TheScreen_IsOrangeOnBlack_AndTurnsRedInAnEmergency()
	{
		ImGuiAppHarness demo = StartDemo();
		CapturedFrame standby = demo.Capture();
		int pixels = 1400 * 900;
		Assert.IsGreaterThan(pixels / 100, WarmPixels(standby), "standby should be dominated by orange");
		Assert.IsGreaterThan(pixels / 2, standby.CountPixels(p => p.R < 40 && p.G < 40 && p.B < 40), "the field should be black");
		int standbyRed = RedPixels(standby);

		demo.Click("SORTIE");
		demo.Step(2);
		Assert.IsGreaterThan(standbyRed * 2, RedPixels(demo.Capture()), "an emergency should turn the screen red");
	}

	[TestMethod]
	public void Screenshots_AreWrittenWhenAsked()
	{
		string? folder = Environment.GetEnvironmentVariable("IMGUI_SORTIE_SHOTS");
		if (string.IsNullOrEmpty(folder))
		{
			Assert.Inconclusive("Set IMGUI_SORTIE_SHOTS to a folder to write the demo screenshots.");
		}

		Directory.CreateDirectory(folder);
		ImGuiAppHarness demo = StartDemo();
		demo.Click("sector");
		demo.Step(20);
		demo.Capture().SavePng(Path.Join(folder, "sortie-standby.png"));

		demo.Click("SORTIE");
		demo.Step(30);
		demo.Capture().SavePng(Path.Join(folder, "sortie-emergency.png"));
	}
}

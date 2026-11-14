// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead.UITests;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Examples.Bulkhead;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the demo's real configuration. When <c>BULKHEAD_SCREENSHOTS</c> names a directory, each
/// section is also saved there as a PNG.
/// </summary>
[TestClass]
public sealed class DemoTests
{
	private ImGuiAppHarness? harness;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		ImGuiBulkheadDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiBulkheadDemo.BuildConfig(), new HarnessOptions { Width = 1100, Height = 640 });
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
	}

	private void Save(string name)
	{
		string? directory = Environment.GetEnvironmentVariable("BULKHEAD_SCREENSHOTS");
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
			harness!.Capture().SavePng(Path.Join(directory, $"{Path.GetFileName(name)}.png"));
		}
	}

	[TestMethod]
	public void Sidebar_SwitchesSections()
	{
		foreach (string section in ImGuiBulkheadDemo.Sections)
		{
			harness!.Click(harness.Probe.Matches(section).Single(n => n.Contains("##sidebar", StringComparison.Ordinal)));
			harness.Step(2);
			Assert.AreEqual(section, ImGuiBulkheadDemo.Section);
			Save(section.ToLowerInvariant());
		}
	}

	[TestMethod]
	public void Status_TogglesFlip()
	{
		harness!.Click("Sensors");
		Assert.IsTrue(ImGuiBulkheadDemo.Sensors);
		harness.Click("Shields");
		Assert.IsFalse(ImGuiBulkheadDemo.Shields);
	}

	[TestMethod]
	public void Power_ButtonsMoveTheGauge()
	{
		harness!.Click("Power");
		harness.Step(2);
		harness.Click("Raise");
		Assert.AreEqual(0.72f, ImGuiBulkheadDemo.Output, 0.001f);
		harness.Click("Lower");
		harness.Click("Lower");
		Assert.AreEqual(0.52f, ImGuiBulkheadDemo.Output, 0.001f);
	}

	[TestMethod]
	public void Comms_HailCounts()
	{
		harness!.Click("Comms");
		harness.Step(2);
		harness.Click("Hail");
		Assert.AreEqual(1, ImGuiBulkheadDemo.Hails);
	}
}

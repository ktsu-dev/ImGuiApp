// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Bulkhead;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Each widget alone in a window, driven through real frames.</summary>
[TestClass]
public sealed class WidgetTests
{
	private ImGuiAppHarness? harness;
	private int clicks;
	private bool toggled;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
	}

	private ImGuiAppHarness Start(Action draw)
	{
		ImGuiAppConfig config = new()
		{
			Title = "Bulkhead widget",
			OnStart = BulkheadStyle.Apply,
			OnRender = _ =>
			{
				ImGui.SetNextWindowPos(Vector2.Zero);
				ImGui.SetNextWindowSize(new Vector2(640, 360));
				ImGui.Begin("Widget", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoSavedSettings);
				draw();
				ImGui.End();
			},
		};
		harness = ImGuiAppHarness.Start(config, new HarnessOptions { Width = 640, Height = 360 });
		harness.Step(2);
		return harness;
	}

	private static bool IsNear(Rgba32 pixel, Vector4 color) =>
		Math.Abs(pixel.R - (color.X * 255f)) < 12f && Math.Abs(pixel.G - (color.Y * 255f)) < 12f && Math.Abs(pixel.B - (color.Z * 255f)) < 12f;

	[TestMethod]
	public void Button_ClickIsReported()
	{
		ImGuiAppHarness app = Start(() =>
		{
			if (ImGuiBulkhead.Button("Engage"))
			{
				clicks++;
			}
		});

		app.Click("Engage");
		Assert.AreEqual(1, clicks);
	}

	[TestMethod]
	public void Button_IsAPillInItsColour()
	{
		ImGuiAppHarness app = Start(() => ImGuiBulkhead.Button("Engage", new Vector2(200, 40), BulkheadPalette.Amber));
		Rectangle rect = app.Probe.Rect("Engage") ?? throw new AssertFailedException("The button was not marked.");
		CapturedFrame frame = app.Capture();

		Rgba32 middle = frame.GetPixel((rect.MinX + rect.MaxX) / 2, rect.MinY + 4);
		Assert.IsTrue(IsNear(middle, BulkheadPalette.Amber), $"The middle of the button was {middle}, not amber.");

		// The corner of the box is outside the rounded end cap, so it shows the background.
		Rgba32 corner = frame.GetPixel(rect.MinX + 1, rect.MinY + 1);
		Assert.IsFalse(IsNear(corner, BulkheadPalette.Amber), "The button's corner is filled, so its end is square.");
	}

	[TestMethod]
	public void Toggle_ClickFlipsTheValue()
	{
		ImGuiAppHarness app = Start(() => ImGuiBulkhead.Toggle("Shields", ref toggled));
		app.Click("Shields");
		Assert.IsTrue(toggled);
		app.Click("Shields");
		Assert.IsFalse(toggled);
	}

	[TestMethod]
	public void Gauge_FillsInProportion()
	{
		ImGuiAppHarness app = Start(() => ImGuiBulkhead.Gauge("level", 0.5f, new Vector2(400, 30), 10, BulkheadPalette.Ice));
		Rectangle rect = app.Probe.Rect("level") ?? throw new AssertFailedException("The gauge was not marked.");
		CapturedFrame frame = app.Capture();
		int y = (rect.MinY + rect.MaxY) / 2;
		Assert.IsTrue(IsNear(frame.GetPixel(rect.MinX + 20, y), BulkheadPalette.Ice), "The first segment is not lit.");
		Assert.IsFalse(IsNear(frame.GetPixel(rect.MaxX - 20, y), BulkheadPalette.Ice), "The last segment is lit at half.");
	}

	[TestMethod]
	public void Every_WidgetMarksItself()
	{
		ImGuiAppHarness app = Start(() =>
		{
			ImGuiBulkhead.Header("Systems");
			ImGuiBulkhead.Bar("bar", new Vector2(200, 12));
			ImGuiBulkhead.Elbow("elbow", new Vector2(160, 60), BulkheadCorner.BottomRight, 40, 14);
			ImGuiBulkhead.Gauge("gauge", 0.3f);
		});

		foreach (string name in new[] { "Systems", "bar", "elbow", "gauge" })
		{
			Assert.IsTrue(app.Probe.WasSeenInFrame(name, app.FrameCount - 1), $"{name} was not marked.");
		}
	}

	[TestMethod]
	public void Panel_HostsContentAndSidebar()
	{
		ImGuiAppHarness app = Start(() => ImGuiBulkhead.Panel("Ops", Vector2.Zero, () =>
		{
			if (ImGuiBulkhead.Button("Inner"))
			{
				clicks++;
			}
		}, () => ImGuiBulkhead.Button("Side", new Vector2(ImGui.GetContentRegionAvail().X, 0), null, BulkheadCaps.None)));

		Assert.IsTrue(app.Probe.WasSeenInFrame("Ops", app.FrameCount - 1));
		Rectangle side = app.Probe.Rect("Side") ?? throw new AssertFailedException("The sidebar did not draw.");
		Rectangle inner = app.Probe.Rect("Inner") ?? throw new AssertFailedException("The content did not draw.");
		Assert.IsGreaterThanOrEqualTo(side.MaxX, inner.MinX, "The content overlaps the sidebar.");
		app.Click("Inner");
		Assert.AreEqual(1, clicks);
	}
}

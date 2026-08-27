// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="OverlayHost"/> on its own.</summary>
[TestClass]
public sealed class OverlayHostTests : WidgetTest
{
	private static readonly string[] DialogThenToast = ["dialog", "toast"];
	private static readonly string[] FirstThenSecond = ["first", "second"];

	private OverlayHost host = new();
	private readonly List<string> drawOrder = [];

	private void DrawOverlayWindow(string key)
	{
		drawOrder.Add(key);

		if (ImGui.Begin(key))
		{
			ImGui.TextUnformatted(key);
			Mark(key);
		}

		ImGui.End();
	}

	private void Draw() => host.Render();

	[TestMethod]
	public void OverlayHost_DrawsARegisteredOverlay()
	{
		host = new OverlayHost();
		host.Show("toast", () => DrawOverlayWindow("toast"));

		Start(Draw);

		Assert.IsTrue(IsVisible("toast"), "A registered overlay was never drawn.");
		Assert.IsTrue(host.HasOverlays, "The host reported no overlays while one was registered.");
	}

	[TestMethod]
	public void OverlayHost_DrawsNothingWhenEmpty()
	{
		host = new OverlayHost();

		Start(Draw);

		Assert.AreEqual(0, host.Count, "An empty host reported overlays.");
		Assert.AreEqual(0, drawOrder.Count, "An empty host invoked a draw callback.");
	}

	[TestMethod]
	public void OverlayHost_DismissedOverlayStopsBeingDrawn()
	{
		host = new OverlayHost();
		host.Show("toast", () => DrawOverlayWindow("toast"));

		Start(Draw);
		Assert.IsTrue(IsVisible("toast"), "The overlay was never drawn in the first place.");

		host.Dismiss("toast");
		Step(2);

		Assert.IsFalse(IsVisible("toast"), "A dismissed overlay was still drawn.");
		Assert.IsFalse(host.IsShown("toast"), "A dismissed overlay was still reported as shown.");
	}

	[TestMethod]
	public void OverlayHost_DrawsLowerLayersFirst()
	{
		host = new OverlayHost();
		host.Show("toast", () => DrawOverlayWindow("toast"), OverlayLayer.Toast);
		host.Show("dialog", () => DrawOverlayWindow("dialog"), OverlayLayer.Dialog);

		Start(Draw);
		drawOrder.Clear();
		Step();

		CollectionAssert.AreEqual(
			DialogThenToast,
			drawOrder,
			$"Overlays were drawn in the order [{string.Join(", ", drawOrder)}] rather than by ascending layer.");
	}

	[TestMethod]
	public void OverlayHost_ReshowingAKeyKeepsItsPlaceInTheOrder()
	{
		host = new OverlayHost();
		host.Show("first", () => DrawOverlayWindow("first"));
		host.Show("second", () => DrawOverlayWindow("second"));

		Start(Draw);

		// Re-showing replaces the callback without moving the overlay to the front, which is what
		// lets an application call Show every frame without thrashing the z-order.
		host.Show("first", () => DrawOverlayWindow("first"));
		drawOrder.Clear();
		Step();

		CollectionAssert.AreEqual(
			FirstThenSecond,
			drawOrder,
			$"Re-showing an overlay reordered the host: [{string.Join(", ", drawOrder)}].");
	}

	[TestMethod]
	public void OverlayHost_ClearRemovesEverything()
	{
		host = new OverlayHost();
		host.Show("toast", () => DrawOverlayWindow("toast"));
		host.Show("dialog", () => DrawOverlayWindow("dialog"), OverlayLayer.Dialog);

		Start(Draw);
		host.Clear();
		Step(2);

		Assert.IsFalse(host.HasOverlays, "The host still reported overlays after being cleared.");
		Assert.IsFalse(IsVisible("toast"), "A cleared overlay was still drawn.");
	}

	[TestMethod]
	public void OverlayHost_AnOverlayMayDismissItselfWhileDrawing()
	{
		host = new OverlayHost();
		host.Show("toast", () =>
		{
			DrawOverlayWindow("toast");
			host.Dismiss("toast");
		});

		Start(Draw);

		Assert.IsFalse(host.IsShown("toast"), "The overlay did not remove itself.");
		Assert.AreEqual(1, drawOrder.Count, "Mutating the registry mid-render disturbed the frame being drawn.");
	}
}

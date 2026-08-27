// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.DockedWindow"/> on its own.</summary>
/// <remarks>
/// A docked window is only ever drawn by <see cref="ImGuiWidgets.DrawDeferredDocked"/>, which in
/// turn requires the docking flag, so these tests start their harness with docking enabled.
/// </remarks>
[TestClass]
public sealed class DockedWindowTests : WidgetTest
{
	private const string Content = "docked-content";

	/// <summary>A window whose content marks itself, so a test can see whether it was drawn.</summary>
	private sealed class ProbeWindow(string title) : ImGuiWidgets.DockedWindow
	{
		public int DrawCount { get; private set; }

		protected override string Title { get; } = title;

		protected override void DrawContent()
		{
			DrawCount++;
			ImGui.TextUnformatted("Contents");
			Mark(Content);
		}
	}

	private ProbeWindow window = null!;

	[TestCleanup]
	public void CloseWindow() => window?.Close();

	[TestMethod]
	public void DockedWindow_IsDrawnByTheDockedPump()
	{
		window = new ProbeWindow("Inspector");
		Start(ImGuiWidgets.DrawDeferredDocked, enableDocking: true);

		window.Show();
		Step(3);

		Assert.IsTrue(IsVisible(Content), "A shown window was not drawn by the docked pump.");
		Assert.IsTrue(window.DrawCount > 0, "The window's content callback never ran.");
	}

	[TestMethod]
	public void DockedWindow_IsNotDrawnByThePlainPump()
	{
		// The plain pump does not drive Hexa's widget manager, which owns the registered windows,
		// so a window shown under it is registered and never rendered.
		window = new ProbeWindow("Inspector");
		Start(ImGuiWidgets.DrawDeferred);

		window.Show();
		Step(3);

		Assert.AreEqual(0, window.DrawCount, "The plain pump drew a docked window.");
	}

	[TestMethod]
	public void DockedWindow_StopsBeingDrawnAfterClose()
	{
		window = new ProbeWindow("Inspector");
		Start(ImGuiWidgets.DrawDeferredDocked, enableDocking: true);

		window.Show();
		Step(3);
		Assert.IsTrue(window.DrawCount > 0, "The window was never drawn in the first place.");

		window.Close();
		Step(3);
		int afterClose = window.DrawCount;
		Step(3);

		Assert.AreEqual(afterClose, window.DrawCount, "A closed window was still being drawn.");
		Assert.IsFalse(IsVisible(Content), "A closed window's content was still on screen.");
	}

	[TestMethod]
	public void DockedWindow_DrawsItsOwnContentEveryFrame()
	{
		window = new ProbeWindow("Inspector");
		Start(ImGuiWidgets.DrawDeferredDocked, enableDocking: true);

		window.Show();
		Step(3);
		int first = window.DrawCount;
		Step(5);

		Assert.IsTrue(window.DrawCount > first, $"The content ran {window.DrawCount} times across eight frames.");
	}

	[TestMethod]
	public void DockedWindow_OpensFloatingRatherThanDocked()
	{
		// Hexa only pins a window to the dockspace when it is marked embedded, which it is not for
		// a window registered through Show, so it opens floating over the viewport.
		window = new ProbeWindow("Inspector");
		Start(ImGuiWidgets.DrawDeferredDocked, enableDocking: true);

		byte[] dockspaceOnly = Snapshot();
		window.Show();
		Step(3);

		Rectangle drawn = BoundsOfDifference(dockspaceOnly) ?? throw new InvalidOperationException("The window drew nothing.");

		Assert.IsTrue(
			drawn.Width < Harness.Options.Width,
			$"The window covered the full {Harness.Options.Width}px width, so it was docked rather than floating.");
	}
}

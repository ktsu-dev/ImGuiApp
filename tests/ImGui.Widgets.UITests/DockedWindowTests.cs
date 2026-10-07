// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

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
	private ProbeWindow? other;
	private Action? beforePump;

	[TestCleanup]
	public void CloseWindow()
	{
		window?.Close();
		other?.Close();
	}

	/// <summary>Runs a one-shot action inside the next frame, ahead of the docked pump.</summary>
	private void RunInNextFrame(Action action)
	{
		beforePump = action;
		Step();
	}

	private void DrawPump()
	{
		Action? action = beforePump;
		beforePump = null;
		action?.Invoke();
		ImGuiWidgets.DrawDeferredDocked();
	}

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

	[TestMethod]
	public void DockedWindow_TabbedBehindAnotherIsStillThereWhenSelected()
	{
		// ImGui.Begin answers false for a docked tab that is not selected, and Hexa used to read that
		// as a close and unregister the window, so the tab behind was lost for good.
		window = new ProbeWindow("Alpha");
		other = new ProbeWindow("Beta");
		Start(DrawPump, enableDocking: true);

		window.Show();
		other.Show();
		Step(3);

		RunInNextFrame(() =>
		{
			ImGuiP.DockBuilderDockWindow("Alpha", Hexa.NET.ImGui.Widgets.WidgetManager.DockSpaceId);
			ImGuiP.DockBuilderDockWindow("Beta", Hexa.NET.ImGui.Widgets.WidgetManager.DockSpaceId);
		});
		Step(5);

		RunInNextFrame(() => ImGui.SetWindowFocus("Alpha"));
		Step(2);
		int alpha = window.DrawCount;
		Step(3);
		Assert.IsTrue(window.DrawCount > alpha, "Alpha was not drawn after its tab was selected.");

		RunInNextFrame(() => ImGui.SetWindowFocus("Beta"));
		Step(2);
		int beta = other.DrawCount;
		Step(3);
		Assert.IsTrue(other.DrawCount > beta, "Beta was not drawn after its tab was selected.");
	}

	[TestMethod]
	public void DockedWindow_SurvivesBeingCollapsed()
	{
		window = new ProbeWindow("Inspector");
		Start(DrawPump, enableDocking: true);

		window.Show();
		Step(3);

		RunInNextFrame(() => ImGui.SetWindowCollapsed("Inspector", true));
		Step(3);
		int collapsed = window.DrawCount;
		Step(3);
		Assert.AreEqual(collapsed, window.DrawCount, "A collapsed window still drew its content.");

		RunInNextFrame(() => ImGui.SetWindowCollapsed("Inspector", false));
		Step(3);
		Assert.IsTrue(window.DrawCount > collapsed, "The window did not come back after being expanded.");
	}

	[TestMethod]
	public void DockedWindow_CloseButtonStillCloses()
	{
		// The veto that keeps a collapsed or tabbed window registered must not swallow a real close.
		window = new ProbeWindow("Inspector");
		Start(DrawPump, enableDocking: true);

		window.Show();
		Step(3);

		Vector2 closeButton = Vector2.Zero;
		RunInNextFrame(() =>
		{
			ImGuiWindowPtr found = ImGuiP.FindWindowByName("Inspector");
			ImGuiStylePtr style = ImGui.GetStyle();
			float fontSize = ImGui.GetFontSize();
			closeButton = new Vector2(
				found.Pos.X + found.Size.X - style.FramePadding.X - (fontSize * 0.5f),
				found.Pos.Y + style.FramePadding.Y + (fontSize * 0.5f));
		});

		Harness.Mouse.Click(closeButton.X, closeButton.Y);
		Step(3);
		int afterClose = window.DrawCount;
		Step(3);

		Assert.AreEqual(afterClose, window.DrawCount, "The close button did not close the window.");
	}
}

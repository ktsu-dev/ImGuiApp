// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the per-frame pumps <see cref="ImGuiWidgets.DrawDeferred"/> and
/// <see cref="ImGuiWidgets.DrawDeferredDocked"/> on their own.
/// </summary>
/// <remarks>
/// The pumps are what draw Hexa's dialogs and docked windows, and the messages tested here are the
/// ones an application hits when it forgets one: showing a dialog before any pump has run, or
/// asking for the docked pump without docking enabled.
/// </remarks>
[TestClass]
public sealed class DeferredDrawingTests : WidgetTest
{
	[TestMethod]
	public void DrawDeferred_RunsWithNothingOpen()
	{
		Start(ImGuiWidgets.DrawDeferred);
		Step(3);

		// With no dialog registered the pump is a no-op that still has to keep the frame valid.
		Assert.IsTrue(Harness.FrameCount > 3, "The pump did not survive being called on an empty frame.");
	}

	[TestMethod]
	public void DrawDeferredDocked_WithoutDocking_IsRefused()
	{
		// The flag cannot be turned on from the pump -- ImGui only accepts it before the first
		// frame -- so the pump refuses rather than drawing a dockspace that would do nothing.
		Start(() => { });

		Assert.ThrowsExactly<InvalidOperationException>(
			ImGuiWidgets.DrawDeferredDocked,
			"The docked pump ran without the docking flag.");
	}

	[TestMethod]
	public void DrawDeferredDocked_WithDockingEnabled_Runs()
	{
		Start(ImGuiWidgets.DrawDeferredDocked, enableDocking: true);
		Step(3);

		AssertSomethingWasDrawn("the dockspace");
	}

	[TestMethod]
	public void DrawDeferred_AfterTheDockedPump_StillRuns()
	{
		// The two pumps share one tracker, and running them in the same frame draws every dialog
		// twice -- a traced warning, not a throw. Alternating frames is legitimate and must work.
		bool docked = true;

		Start(
			() =>
			{
				if (docked)
				{
					ImGuiWidgets.DrawDeferredDocked();
				}
				else
				{
					ImGuiWidgets.DrawDeferred();
				}
			},
			enableDocking: true);

		docked = false;
		Step(3);

		Assert.IsTrue(Harness.FrameCount > 3, "Switching pumps between frames broke the frame.");
	}
}

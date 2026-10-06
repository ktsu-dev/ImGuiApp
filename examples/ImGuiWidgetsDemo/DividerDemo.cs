// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// A live divider container, drawn inside its own section.
/// </summary>
internal static class DividerDemo
{
	// The DividerContainer is no longer this demo's top-level layout -- the main window is a tab bar
	// now -- so the widget keeps a live instance of its own, drawn inside its demo section.
	private static ImGuiWidgets.DividerContainer DividerDemoContainer { get; } = new("DividerDemoContainer");

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the zones' sizes are not restored by the demo's reset.
	}

	/// <summary>Adds the container's two zones.</summary>
	internal static void Initialize()
	{
		DividerDemoContainer.Add(new("DividerDemoLeft", 0.5f, _ =>
			ImGui.TextWrapped("Drag the handle between these zones to resize them, or double-click it to reset.")));
		DividerDemoContainer.Add(new("DividerDemoRight", 0.5f, _ =>
			ImGui.TextWrapped("Each zone is its own child window, so it scrolls and clips independently.")));
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Divider Container"))
		{
			ImGui.TextUnformatted("DividerContainer features:");
			ImGui.BulletText("Resizable panes with drag handle");
			ImGui.BulletText("Persistent sizing ratios");
			ImGui.BulletText("Automatic content management");
			ImGui.BulletText("Nested dividers support");

			ImGui.Separator();

			// The container lays itself out into the whole remaining content region, so it is given a
			// fixed-height host to draw into rather than being left to swallow the rest of the tab.
			ImGui.BeginChild("DividerDemoHost", new Vector2(0f, 120f), ImGuiChildFlags.None);
			DividerDemoContainer.Tick(DemoContext.DeltaTime);
			ImGui.EndChild();
		}
	}
}

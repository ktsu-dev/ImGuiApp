// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// Toasts and snackbars: short messages that slide up from the bottom of the window and go away by
/// themselves.
/// </summary>
internal static class ToastDemo
{
	private static readonly OverlayHost Host = new();

	/// <summary>Gets how many times a snackbar's Undo was pressed.</summary>
	internal static int Undone { get; private set; }

	/// <summary>Gets the toaster, so a test can read what it shows.</summary>
	internal static ImGuiWidgets.Toaster Toaster { get; private set; } = new(Host, "demoToasts");

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		Toaster = new ImGuiWidgets.Toaster(Host, "demoToasts");
		Undone = 0;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Toasts"))
		{
			ImGui.TextWrapped("Toasts slide up from the bottom of the window and go away after a few seconds. Several wait their turn rather than replacing each other. Rest the pointer on one to keep it.");

			if (DemoProbe.Button("Show a toast##toastShow"))
			{
				Toaster.Show("Settings saved");
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Show a snackbar##toastSnackbar"))
			{
				Toaster.Show("Message deleted", new ToastOptions { ActionLabel = "Undo", OnAction = () => Undone++ });
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Queue three##toastQueue"))
			{
				Toaster.Show("Uploading 1 of 3", new ToastOptions { Duration = 1.5f });
				Toaster.Show("Uploading 2 of 3", new ToastOptions { Duration = 1.5f });
				Toaster.Show("All 3 uploaded", new ToastOptions { Duration = 1.5f });
			}

			ImGui.TextUnformatted($"Waiting or showing: {Toaster.Count}    Undone: {Undone}");
			Host.Render();
		}
	}
}

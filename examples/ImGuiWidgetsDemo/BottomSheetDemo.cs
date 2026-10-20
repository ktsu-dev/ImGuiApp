// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The bottom sheet: a panel that slides up from the bottom and rests at peek, half or full height.
/// </summary>
internal static class BottomSheetDemo
{
	private static readonly string[] Places = ["Harbour Bridge", "Opera House", "Botanic Garden", "Bondi Beach", "Manly Ferry", "Taronga Zoo", "Darling Harbour", "The Rocks"];
	private static readonly OverlayHost Host = new();

	/// <summary>Gets the place last picked from the Sheet.</summary>
	internal static string Picked { get; private set; } = "(none)";

	/// <summary>Gets the sheet, so a test can read where it rests.</summary>
	internal static ImGuiWidgets.BottomSheet Sheet { get; private set; } = Create();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		Sheet = Create();
		Picked = "(none)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Bottom Sheet"))
		{
			ImGui.TextWrapped("Drag the handle to move the sheet between peek, half and full. Drag it right down, tap above it or press Escape to close it.");

			if (DemoProbe.Button("Peek##bottomSheetPeek"))
			{
				Sheet.Open(BottomSheetDetent.Peek);
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Half##bottomSheetHalf"))
			{
				Sheet.Open(BottomSheetDetent.Half);
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Full##bottomSheetFull"))
			{
				Sheet.Open(BottomSheetDetent.Full);
			}

			ImGui.SameLine();
			ImGui.TextUnformatted($"Picked: {Picked}");
			Host.Render();
		}
	}

	private static ImGuiWidgets.BottomSheet Create() => new(Host, "demoBottomSheet", DrawContent);

	private static void DrawContent(Vector2 size)
	{
		ImGui.TextUnformatted("Nearby");
		ImGui.Separator();
		foreach (string place in Places)
		{
			if (ImGui.Selectable(place, place == Picked, ImGuiSelectableFlags.None, new Vector2(size.X, 0f)))
			{
				Picked = place;
				Sheet.Close();
			}
		}
	}
}

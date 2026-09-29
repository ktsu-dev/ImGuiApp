// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>
/// The swatch palette, with the adding, removing and editing the widget leaves to its host.
/// </summary>
internal static class SwatchPaletteDemo
{
	private static readonly List<Color> swatches = [];
	private static int selected = -1;

	static SwatchPaletteDemo() => ResetState();

	/// <summary>Gets the demo palette, for tests.</summary>
	internal static IReadOnlyList<Color> Swatches => swatches;

	/// <summary>Gets the selected swatch's index, or -1, for tests.</summary>
	internal static int Selected => selected;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		swatches.Clear();
		swatches.AddRange(
		[
			NamedColors.Black, NamedColors.White, NamedColors.Red, NamedColors.Green,
			NamedColors.Blue, NamedColors.Yellow, NamedColors.Cyan, NamedColors.Magenta,
			NamedColors.Gray, NamedColors.Orange, NamedColors.Purple, NamedColors.Transparent,
		]);
		selected = -1;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Swatch Palette"))
		{
			ImGui.TextUnformatted("Click a swatch to select it, drag one to reorder the palette:");
			ImGui.Separator();

			ImGuiWidgets.SwatchPalette("swatch_demo", swatches, ref selected, 24.0f);

			if (selected >= 0)
			{
				Vector4 colour = swatches[selected].ToSrgbVector4();
				if (ImGui.ColorEdit4("Colour", ref colour))
				{
					swatches[selected] = ColorImGuiExtensions.FromImGuiVector4(colour);
				}
			}

			if (DemoProbe.Button("Add"))
			{
				swatches.Add(NamedColors.White);
				selected = swatches.Count - 1;
			}

			if (selected >= 0)
			{
				ImGui.SameLine();
				if (DemoProbe.Button("Remove"))
				{
					swatches.RemoveAt(selected);
					selected = -1;
				}
			}

			ImGui.TextUnformatted($"Swatches: {swatches.Count}, selected: {selected}");
		}
	}
}

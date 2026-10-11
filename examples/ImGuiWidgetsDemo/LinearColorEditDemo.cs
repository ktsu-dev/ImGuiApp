// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// The linear colour controls: colours stored in linear light, shown and picked sRGB-encoded.
/// </summary>
internal static class LinearColorEditDemo
{
	private const float MidGrey = 0.21404114f;

	/// <summary>Gets the fill colour the demo edits, in linear light.</summary>
	internal static Vector4 Fill { get; private set; } = new(MidGrey, MidGrey, MidGrey, 1f);

	private static Vector3 stroke = new(0.8f, 0.05f, 0.02f);

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Fill = new(MidGrey, MidGrey, MidGrey, 1f);
		stroke = new(0.8f, 0.05f, 0.02f);
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Linear Colour Edit"))
		{
			return;
		}

		ImGui.TextUnformatted("Stored linear, shown sRGB: mid grey is 0.214 in linear light and reads #808080.");

		Vector4 fill = Fill;
		ImGuiWidgets.ColorEdit("Fill", ref fill, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf);
		Fill = fill;
		ImGuiWidgets.ColorEdit("Stroke", ref stroke, ImGuiColorEditFlags.DisplayHex);

		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"Fill stored: ({fill.X:0.000}, {fill.Y:0.000}, {fill.Z:0.000}, {fill.W:0.00})"));
		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"Stroke stored: ({stroke.X:0.000}, {stroke.Y:0.000}, {stroke.Z:0.000})"));

		ImGuiWidgets.ColorButton("Fill swatch", fill, ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(48f, 24f));
		ImGui.SameLine();
		ImGuiWidgets.ColorButton("Stroke swatch", new Vector4(stroke, 1f), ImGuiColorEditFlags.None, new Vector2(48f, 24f));
	}
}

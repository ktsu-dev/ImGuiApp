// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

/// <summary>
/// Widgets for a command-display interface: captioned warning panels, hex grids, emergency banners, countdowns,
/// sync gauges, status lamps and readouts, all drawn in the palette of <see cref="SortieStyle.Condition"/>.
/// </summary>
/// <remarks>
/// Every widget is immediate mode and keeps no state of its own. Each marks itself for
/// <see cref="ktsu.ImGui.Probes.ImGuiProbes"/>, so a headless test can find it by name.
/// </remarks>
public static partial class ImGuiSortie
{
	/// <summary>Gives the part of an ImGui label shown on screen, which is everything before a <c>##</c>.</summary>
	/// <param name="label">The label.</param>
	/// <returns>The visible part.</returns>
	internal static string VisibleLabel(string label)
	{
		int hidden = label.IndexOf("##", System.StringComparison.Ordinal);
		return hidden < 0 ? label : label[..hidden];
	}
}

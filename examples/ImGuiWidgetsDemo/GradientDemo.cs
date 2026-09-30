// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>
/// The gradient editor, driving a heatmap drawn from the same stops it edits.
/// </summary>
internal static class GradientDemo
{
	private const int HeatmapColumns = 32;
	private const int HeatmapRows = 8;
	private const float HeatmapCell = 12.0f;

	private static readonly List<GradientStop> stops = [];

	static GradientDemo() => ResetState();

	/// <summary>Gets how many stops the demo gradient has, for tests.</summary>
	internal static int StopCount => stops.Count;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		stops.Clear();
		stops.Add(new GradientStop(0.0f, Color.FromBytes(0, 0, 0, 255)));
		stops.Add(new GradientStop(0.35f, Color.FromBytes(255, 0, 0, 255)));
		stops.Add(new GradientStop(0.7f, Color.FromBytes(255, 255, 0, 255)));
		stops.Add(new GradientStop(1.0f, Color.FromBytes(255, 255, 255, 255)));
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Gradient Editor"))
		{
			ImGui.TextUnformatted("A heat palette. Click the bar to add a stop, drag one to move it, drag it away or press Delete to remove it:");
			ImGui.Separator();

			ImGuiWidgets.GradientEditor("gradient_demo", stops, new Vector2(400.0f, 24.0f));

			ImGui.Spacing();
			ImGui.TextUnformatted("The heatmap below is coloured by SampleGradient over the same stops:");
			DrawHeatmap();

			if (DemoProbe.Button("Reset gradient"))
			{
				ResetState();
			}

			ImGui.TextUnformatted($"Stops: {stops.Count}");
		}
	}

	/// <summary>Draws a small field of values through the gradient, as a consumer would apply it.</summary>
	private static void DrawHeatmap()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		for (int y = 0; y < HeatmapRows; y++)
		{
			for (int x = 0; x < HeatmapColumns; x++)
			{
				float value = 0.5f + (0.5f * MathF.Sin(x * 0.4f) * MathF.Cos(y * 0.7f));
				Vector2 min = origin + new Vector2(x * HeatmapCell, y * HeatmapCell);
				drawList.AddRectFilled(min, min + new Vector2(HeatmapCell, HeatmapCell), ImGuiWidgets.SampleGradient(stops, value).ToImGuiU32());
			}
		}

		ImGui.Dummy(new Vector2(HeatmapColumns * HeatmapCell, HeatmapRows * HeatmapCell));
	}
}

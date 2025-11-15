// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGuiApp;
using ktsu.ImGuiAppDemo.Properties;

/// <summary>
/// Demo for data visualization features
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Used for dummy data purposes")]
internal sealed class DataVisualizationDemo : IDemoTab
{
	private readonly List<float> plotValues = [];
	private readonly Random random = new();
	private float plotRefreshTime;

	public string TabName => "Data Visualization";

	public void Update(float deltaTime)
	{
		// Update plot data
		plotRefreshTime += deltaTime;
		if (plotRefreshTime >= 0.1f) // Update every 100ms
		{
			plotRefreshTime = 0;
			plotValues.Add((float)random.NextDouble());
			if (plotValues.Count > 100) // Keep last 100 values
			{
				plotValues.RemoveAt(0);
			}
		}
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.SeparatorText("Real-time Data Plots:");

			// Line plot
			if (plotValues.Count > 0)
			{
				float[] values = [.. plotValues];
				ImGui.PlotLines("Random Values", ref values[0], values.Length, 0,
					$"Current: {values[^1]:F2}", 0.0f, 1.0f, new Vector2(ImGui.GetContentRegionAvail().X, 100));

				ImGui.PlotHistogram("Distribution", ref values[0], values.Length, 0,
					"Histogram", 0.0f, 1.0f, new Vector2(ImGui.GetContentRegionAvail().X, 100));
			}

			// Performance note
			ImGui.SeparatorText("Performance Metrics:");
			ImGui.TextWrapped("Performance monitoring is now available in the Debug menu! Use 'Debug > Show Performance Monitor' to see real-time FPS graphs and throttling state.");

			// Font demonstrations
			ImGui.SeparatorText("Custom Font Rendering:");
			using (new FontAppearance(nameof(Resources.CARDCHAR), 16))
			{
				ImGui.Text("Small custom font text");
			}

			using (new FontAppearance(nameof(Resources.CARDCHAR), 24))
			{
				ImGui.Text("Medium custom font text");
			}

			using (new FontAppearance(nameof(Resources.CARDCHAR), 32))
			{
				ImGui.Text("Large custom font text");
			}

			// Text formatting examples
			ImGui.SeparatorText("Text Formatting:");
			ImGui.TextColored(new Vector4(1, 0, 0, 1), "Red text");
			ImGui.TextColored(new Vector4(0, 1, 0, 1), "Green text");
			ImGui.TextColored(new Vector4(0, 0, 1, 1), "Blue text");
			ImGui.TextWrapped("This is a long line of text that should wrap to multiple lines when the window is not wide enough to contain it all on a single line.");

			ImGui.EndTabItem();
		}
	}
}

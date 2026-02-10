// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

/// <summary>
/// Demo for ImPlot advanced plotting
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Used for dummy data purposes")]
internal sealed class ImPlotDemo : IDemoTab
{
	private readonly List<float> sinData = [];
	private readonly List<float> cosData = [];
	private readonly List<float> noiseData = [];
	private readonly List<float> plotValues = [];
	private float plotTime;
	private readonly Random plotRandom = new();
	private float plotRefreshTime;

	public string TabName => "ImPlot Charts";

	public ImPlotDemo()
	{
		// Initialize plot data
		for (int i = 0; i < 100; i++)
		{
			float x = i * 0.1f;
			sinData.Add(MathF.Sin(x));
			cosData.Add(MathF.Cos(x));
			noiseData.Add((float)((plotRandom.NextDouble() * 2.0) - 1.0));
		}
	}

	public void Update(float deltaTime)
	{
		plotTime += deltaTime;

		// Update plot data for real-time demo
		plotRefreshTime += deltaTime;
		if (plotRefreshTime >= 0.1f) // Update every 100ms
		{
			plotRefreshTime = 0;
			plotValues.Add((float)plotRandom.NextDouble());
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
			ImGui.TextWrapped("ImPlot provides advanced plotting capabilities with various chart types.");
			ImGui.Separator();

			// Plot controls
			if (ImGui.Button("Generate New Data"))
			{
				sinData.Clear();
				cosData.Clear();
				noiseData.Clear();

				for (int i = 0; i < 100; i++)
				{
					float x = i * 0.1f;
					sinData.Add(MathF.Sin(x + plotTime));
					cosData.Add(MathF.Cos(x + plotTime));
					noiseData.Add((float)((plotRandom.NextDouble() * 2.0) - 1.0));
				}
			}

			ImGui.Separator();

			// Line plot
			if (ImPlot.BeginPlot("Trigonometric Functions", new Vector2(-1, 200)))
			{
				unsafe
				{
					fixed (float* sinPtr = sinData.ToArray())
					fixed (float* cosPtr = cosData.ToArray())
					{
						ImPlot.PlotLine("sin(x)", sinPtr, sinData.Count);
						ImPlot.PlotLine("cos(x)", cosPtr, cosData.Count);
					}
				}
				ImPlot.EndPlot();
			}

			// Scatter plot
			if (ImPlot.BeginPlot("Noise Data (Scatter)", new Vector2(-1, 200)))
			{
				unsafe
				{
					fixed (float* noisePtr = noiseData.ToArray())
					{
						ImPlot.PlotScatter("Random Noise", noisePtr, noiseData.Count);
					}
				}
				ImPlot.EndPlot();
			}

			// Bar chart
			if (ImPlot.BeginPlot("Sample Bar Chart", new Vector2(-1, 200)))
			{
				float[] barData = [1.0f, 2.5f, 3.2f, 1.8f, 4.1f, 2.9f, 3.6f];
				unsafe
				{
					fixed (float* barPtr = barData)
					{
						ImPlot.PlotBars("Values", barPtr, barData.Length);
					}
				}
				ImPlot.EndPlot();
			}

			// Real-time plot
			if (ImPlot.BeginPlot("Real-time Data", new Vector2(-1, 200)))
			{
				// Update real-time data
				if (plotValues.Count > 0)
				{
					unsafe
					{
						fixed (float* plotPtr = plotValues.ToArray())
						{
							ImPlot.PlotLine("Live Data", plotPtr, plotValues.Count);
						}
					}
				}
				ImPlot.EndPlot();
			}

			ImGui.Text($"Plot Time: {plotTime:F2}");
			ImGui.Text($"Data Points: Sin({sinData.Count}), Cos({cosData.Count}), Noise({noiseData.Count})");

			ImGui.EndTabItem();
		}
	}
}

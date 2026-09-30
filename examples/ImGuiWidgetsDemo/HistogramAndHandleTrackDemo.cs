// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The histogram, with a handle track overlaid on it.
/// </summary>
internal static class HistogramAndHandleTrackDemo
{
	private static readonly float[] histogramBins = BuildHistogramBins();

	/// <summary>Gets the histogram bins, which the curve track section draws its histogram from too.</summary>
	internal static ReadOnlySpan<float> Bins => histogramBins;
	private static readonly float[] handleTrackHandles = [0.15f, 0.5f, 0.85f];

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the handles are not restored by the demo's reset, and the bins never change.
	}

	private static float[] BuildHistogramBins()
	{
		// Three offset bell curves, so the overlay and the per-series colors are both visible.
		float[] bins = new float[3 * 256];
		for (int series = 0; series < 3; series++)
		{
			float center = 96.0f + (series * 32.0f);
			for (int bin = 0; bin < 256; bin++)
			{
				float d = (bin - center) / 40.0f;
				bins[(series * 256) + bin] = MathF.Exp(-d * d);
			}
		}

		return bins;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (ImGui.CollapsingHeader("Histogram & Handle Track"))
		{
			ImGui.TextUnformatted("Three overlaid series, with a HandleTrack overlaying the exact rectangle Histogram just drew into:");
			ImGui.Separator();

			ImGuiWidgets.Histogram("##demoHistogram", histogramBins, 3, new Vector2(420.0f, 160.0f));

			// The handoff that makes the two widgets compose: read the rectangle Histogram just
			// occupied straight back off the item stack and hand it to HandleTrack unchanged.
			Vector2 histogramMin = ImGui.GetItemRectMin();
			Vector2 histogramMax = ImGui.GetItemRectMax();

			// The box above is tall, so the default grab radius (derived from rectangle height)
			// would be oversized; pass an explicit radius and center the handles on the bottom edge.
			ImGuiWidgets.HandleTrack(
				"##demoHandleTrack",
				handleTrackHandles,
				histogramMin,
				histogramMax,
				0.0f,
				1.0f,
				minGap: 0.02f,
				handleRadius: 6.0f,
				handleCenterY: histogramMax.Y);

			ImGui.Separator();
			ImGui.TextUnformatted("Drag a handle -- they stay ordered and at least 0.02 apart:");
			ImGui.TextUnformatted($"Handles: {handleTrackHandles[0]:0.00}  {handleTrackHandles[1]:0.00}  {handleTrackHandles[2]:0.00}");
		}
	}
}

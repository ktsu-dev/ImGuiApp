// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The curve track, drawn over a function the demo owns.
/// </summary>
internal static class CurveTrackDemo
{
	// CurveTrack demo state. The points are the demo's, and so is the function drawn through them:
	// CurveTrack plots whatever it is handed, which is what keeps the drawn curve and the one a
	// consumer applies to its data the same function rather than two that agree.
	private static readonly List<Vector2> curveTrackPoints =
	[
		new Vector2(0.0f, 0.0f),
		new Vector2(0.35f, 0.25f),
		new Vector2(1.0f, 1.0f),
	];

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the curve's points are not restored by the demo's reset.
	}

	/// <summary>Straight lines between the demo's control points, clamped outside them.</summary>
	private static float SampleDemoCurve(float x)
	{
		if (curveTrackPoints.Count == 0)
		{
			return 0.0f;
		}

		if (x <= curveTrackPoints[0].X)
		{
			return curveTrackPoints[0].Y;
		}

		for (int i = 1; i < curveTrackPoints.Count; i++)
		{
			Vector2 previous = curveTrackPoints[i - 1];
			Vector2 current = curveTrackPoints[i];
			if (x > current.X)
			{
				continue;
			}

			float span = current.X - previous.X;
			float t = span <= 0.0f ? 0.0f : (x - previous.X) / span;
			return previous.Y + ((current.Y - previous.Y) * t);
		}

		return curveTrackPoints[^1].Y;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (ImGui.CollapsingHeader("Curve Track"))
		{
			ImGui.TextUnformatted("An editable curve drawn over a histogram of the data it is shaping:");
			ImGui.Separator();

			// Same handoff as the handle track above: Histogram reserves the box and draws into it,
			// and CurveTrack overlays the rectangle it just occupied.
			ImGuiWidgets.Histogram("##curveHistogram", HistogramAndHandleTrackDemo.Bins, 3, new Vector2(320.0f, 320.0f));
			Vector2 curveMin = ImGui.GetItemRectMin();
			Vector2 curveMax = ImGui.GetItemRectMax();

			ImGuiWidgets.CurveTrack(
				"##demoCurveTrack",
				curveTrackPoints,
				SampleDemoCurve,
				curveMin,
				curveMax,
				Vector2.Zero,
				Vector2.One,
				pinEnds: true,
				minGap: 0.02f);

			ImGui.Separator();
			ImGui.TextUnformatted("Drag a point to move it, press empty track to add one, right-click a point to remove it.");
			ImGui.TextUnformatted("The ends hold their x so the curve covers the whole domain; their y is free, which is how clipping is expressed.");
			ImGui.TextUnformatted($"Points: {curveTrackPoints.Count}");
		}
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>Shows the lift, gamma and gain wheels grading a grey ramp.</summary>
internal static class ColorWheelDemo
{
	private const int RampSteps = 64;

	private static ColorWheelValue lift = ColorWheelValue.Neutral;
	private static ColorWheelValue gamma = ColorWheelValue.Neutral;
	private static ColorWheelValue gain = ColorWheelValue.Neutral;

	/// <summary>Gets the gain wheel's value.</summary>
	internal static ColorWheelValue Gain => gain;

	/// <summary>Gets the lift wheel's value.</summary>
	internal static ColorWheelValue Lift => lift;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		lift = ColorWheelValue.Neutral;
		gamma = ColorWheelValue.Neutral;
		gain = ColorWheelValue.Neutral;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Color wheels"))
		{
			return;
		}

		ImGui.TextUnformatted("Lift, gamma and gain trackballs. Drag to push a wheel towards a colour, hold Shift for fine");
		ImGui.TextUnformatted("adjustment, and double-click a wheel to return it to neutral.");
		ImGui.Separator();

		ImGuiWidgets.LiftGammaGain("##grade", ref lift, ref gamma, ref gain);

		if (ImGui.Button("Reset all"))
		{
			ResetState();
		}

		ImGuiProbes.MarkItem("Reset all");

		ImGui.Separator();
		ImGui.TextUnformatted("A grey ramp before and after the grade:");

		// The widget only edits values; applying them is the demo's business. This is one common
		// shape for it — lift raises the shadows, gamma bends the midtones, gain scales the
		// highlights — and a real grader would pick its own.
		Vector3 liftOffset = lift.ToRgbOffset() * 0.5f;
		Vector3 gammaOffset = gamma.ToRgbOffset();
		Vector3 gainOffset = gain.ToRgbOffset();

		float width = ImGui.GetContentRegionAvail().X;
		width = MathF.Min(width, 480.0f);
		DrawRamp("##rampBefore", width, x => new Vector3(x));
		DrawRamp("##rampAfter", width, x => Grade(new Vector3(x), liftOffset, gammaOffset, gainOffset));
	}

	/// <summary>Applies a lift, gamma and gain offset to one colour.</summary>
	internal static Vector3 Grade(Vector3 input, Vector3 liftOffset, Vector3 gammaOffset, Vector3 gainOffset)
	{
		Vector3 lifted = input + (liftOffset * (Vector3.One - input));
		Vector3 gained = lifted * (Vector3.One + gainOffset);

		return new Vector3(
			ApplyGamma(gained.X, gammaOffset.X),
			ApplyGamma(gained.Y, gammaOffset.Y),
			ApplyGamma(gained.Z, gammaOffset.Z));
	}

	private static float ApplyGamma(float value, float offset)
	{
		float exponent = 1.0f / MathF.Max(0.1f, 1.0f + offset);
		return Math.Clamp(MathF.Pow(Math.Clamp(value, 0.0f, 1.0f), exponent), 0.0f, 1.0f);
	}

	private static void DrawRamp(string id, float width, Func<float, Vector3> shade)
	{
		float height = ImGui.GetFrameHeight();
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(width, height));
		ImGuiProbes.MarkItem(id);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float step = width / RampSteps;
		for (int i = 0; i < RampSteps; i++)
		{
			Vector3 rgb = shade((i + 0.5f) / RampSteps);
			Vector2 cellMin = new(min.X + (i * step), min.Y);
			Vector2 cellMax = new(min.X + ((i + 1) * step) + 1.0f, min.Y + height);
			drawList.AddRectFilled(cellMin, cellMax, ImGui.GetColorU32(new Vector4(rgb, 1.0f)));
		}
	}
}

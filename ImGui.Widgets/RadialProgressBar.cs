// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Options for customizing the appearance of the radial progress bar.
/// </summary>
[Flags]
public enum ImGuiRadialProgressBarOptions
{
	/// <summary>
	/// No options selected.
	/// </summary>
	None = 0,
	/// <summary>
	/// Hides the percentage text in the center.
	/// </summary>
	NoText = 1 << 0,
	/// <summary>
	/// Uses a clockwise direction instead of counter-clockwise.
	/// </summary>
	Clockwise = 1 << 1,
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a radial progress bar.
	/// </summary>
	/// <param name="progress">The progress value (0.0 to 1.0).</param>
	/// <param name="radius">The radius of the progress bar in pixels. If 0, uses line height * 2.</param>
	/// <param name="thickness">The thickness of the progress bar in pixels. If 0, uses radius * 0.2.</param>
	/// <param name="segments">The number of segments to use for drawing the arc. More segments = smoother arc.</param>
	/// <param name="options">Options for customizing the appearance.</param>
	public static void RadialProgressBar(float progress, float radius = 0, float thickness = 0, int segments = 32, ImGuiRadialProgressBarOptions options = ImGuiRadialProgressBarOptions.None) =>
		RadialProgressBarImpl.Draw(progress, radius, thickness, segments, options);

	internal static class RadialProgressBarImpl
	{
		public static void Draw(float progress, float radius, float thickness, int segments, ImGuiRadialProgressBarOptions options)
		{
			// Validate input parameters
			progress = Math.Clamp(progress, 0.0f, 1.0f);
			segments = Math.Max(4, segments); // Ensure minimum segments for smooth rendering

			// Calculate dimensions
			float lineHeight = ImGui.GetTextLineHeight();
			float calculatedRadius = radius > 0 ? radius : lineHeight * 2.0f;
			float calculatedThickness = thickness > 0 ? thickness : calculatedRadius * 0.2f;
			float diameter = calculatedRadius * 2.0f;

			// Reserve space for the widget
			Vector2 cursorPos = ImGui.GetCursorScreenPos();
			ImGui.Dummy(new Vector2(diameter, diameter));

			// Get draw list and colors
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			Vector4 backgroundColor = colors[(int)ImGuiCol.FrameBg];
			Vector4 progressColor = colors[(int)ImGuiCol.ButtonHovered];

			// Calculate center point
			Vector2 center = new(cursorPos.X + calculatedRadius, cursorPos.Y + calculatedRadius);

			// Draw background circle
			uint bgColor = ImGui.GetColorU32(backgroundColor);
			DrawArc(drawList, center, calculatedRadius, 0.0f, 2.0f * MathF.PI, calculatedThickness, bgColor, segments);

			// Draw progress arc
			if (progress > 0.0f)
			{
				uint fgColor = ImGui.GetColorU32(progressColor);
				float startAngle = -MathF.PI * 0.5f; // Start at top (12 o'clock)
				float sweepAngle = 2.0f * MathF.PI * progress;

				if (options.HasFlag(ImGuiRadialProgressBarOptions.Clockwise))
				{
					// Clockwise: start at top, sweep clockwise
					float endAngle = startAngle + sweepAngle;
					DrawArc(drawList, center, calculatedRadius, startAngle, endAngle, calculatedThickness, fgColor, segments);
				}
				else
				{
					// Counter-clockwise: start at top, sweep counter-clockwise
					float endAngle = startAngle - sweepAngle;
					DrawArc(drawList, center, calculatedRadius, endAngle, startAngle, calculatedThickness, fgColor, segments);
				}
			}

			// Draw percentage text in center
			if (!options.HasFlag(ImGuiRadialProgressBarOptions.NoText))
			{
				// Note: String allocation per frame is acceptable in immediate mode GUI
				int percentage = (int)(progress * 100.0f);
				string percentageText = $"{percentage}%";
				Vector2 textSize = ImGui.CalcTextSize(percentageText);
				Vector2 textPos = new(center.X - (textSize.X * 0.5f), center.Y - (textSize.Y * 0.5f));
				drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), percentageText);
			}
		}

		private static void DrawArc(ImDrawListPtr drawList, Vector2 center, float radius, float startAngle, float endAngle, float thickness, uint color, int segments)
		{
			// Calculate the number of segments based on the arc length
			float arcLength = MathF.Abs(endAngle - startAngle);
			int numSegments = Math.Max(1, (int)(segments * arcLength / (2.0f * MathF.PI)));

			// Draw the arc as a series of lines
			for (int i = 0; i < numSegments; i++)
			{
				float t0 = i / (float)numSegments;
				float t1 = (i + 1) / (float)numSegments;

				float angle0 = startAngle + (arcLength * t0);
				float angle1 = startAngle + (arcLength * t1);

				Vector2 point0 = new(center.X + (MathF.Cos(angle0) * radius), center.Y + (MathF.Sin(angle0) * radius));
				Vector2 point1 = new(center.X + (MathF.Cos(angle1) * radius), center.Y + (MathF.Sin(angle1) * radius));

				drawList.AddLine(point0, point1, color, thickness);
			}
		}
	}
}

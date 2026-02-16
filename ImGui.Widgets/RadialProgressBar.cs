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
	/// Hides the text in the center.
	/// </summary>
	NoText = 1 << 0,
	/// <summary>
	/// Uses a counter-clockwise direction instead of clockwise.
	/// </summary>
	CounterClockwise = 1 << 1,
	/// <summary>
	/// Starts the progress bar at the bottom instead of the top.
	/// </summary>
	StartAtBottom = 1 << 2,
}

/// <summary>
/// Text display mode for the radial progress bar.
/// </summary>
public enum ImGuiRadialProgressBarTextMode
{
	/// <summary>
	/// Display as percentage (0% - 100%).
	/// </summary>
	Percentage,
	/// <summary>
	/// Display as time in MM:SS format (or HH:MM:SS if >= 1 hour).
	/// </summary>
	Time,
	/// <summary>
	/// Display custom text provided by the user.
	/// </summary>
	Custom,
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
	/// <param name="textMode">The text display mode (percentage, time, or custom).</param>
	/// <param name="timeValue">The time value in seconds (used when textMode is Time).</param>
	/// <param name="customText">Custom text to display (used when textMode is Custom).</param>
	public static void RadialProgressBar(float progress, float radius = 0, float thickness = 0, int segments = 32, ImGuiRadialProgressBarOptions options = ImGuiRadialProgressBarOptions.None, ImGuiRadialProgressBarTextMode textMode = ImGuiRadialProgressBarTextMode.Percentage, float timeValue = 0, string? customText = null) =>
		RadialProgressBarImpl.Draw(progress, radius, thickness, segments, options, textMode, timeValue, customText);

	/// <summary>
	/// Draws a radial countdown timer showing time remaining.
	/// </summary>
	/// <param name="currentTime">The current time in seconds.</param>
	/// <param name="totalTime">The total duration in seconds.</param>
	/// <param name="radius">The radius of the progress bar in pixels. If 0, uses line height * 2.</param>
	/// <param name="thickness">The thickness of the progress bar in pixels. If 0, uses radius * 0.2.</param>
	/// <param name="segments">The number of segments to use for drawing the arc. More segments = smoother arc.</param>
	/// <param name="options">Options for customizing the appearance.</param>
	public static void RadialCountdown(float currentTime, float totalTime, float radius = 0, float thickness = 0, int segments = 32, ImGuiRadialProgressBarOptions options = ImGuiRadialProgressBarOptions.None)
	{
		float progress = totalTime > 0 ? Math.Clamp(currentTime / totalTime, 0.0f, 1.0f) : 0.0f;
		RadialProgressBarImpl.Draw(progress, radius, thickness, segments, options, ImGuiRadialProgressBarTextMode.Time, currentTime, null);
	}

	/// <summary>
	/// Draws a radial count-up timer showing elapsed time.
	/// </summary>
	/// <param name="elapsedTime">The elapsed time in seconds.</param>
	/// <param name="totalTime">The total duration in seconds (for progress calculation).</param>
	/// <param name="radius">The radius of the progress bar in pixels. If 0, uses line height * 2.</param>
	/// <param name="thickness">The thickness of the progress bar in pixels. If 0, uses radius * 0.2.</param>
	/// <param name="segments">The number of segments to use for drawing the arc. More segments = smoother arc.</param>
	/// <param name="options">Options for customizing the appearance.</param>
	public static void RadialCountUp(float elapsedTime, float totalTime, float radius = 0, float thickness = 0, int segments = 32, ImGuiRadialProgressBarOptions options = ImGuiRadialProgressBarOptions.None)
	{
		float progress = totalTime > 0 ? Math.Clamp(elapsedTime / totalTime, 0.0f, 1.0f) : 0.0f;
		RadialProgressBarImpl.Draw(progress, radius, thickness, segments, options, ImGuiRadialProgressBarTextMode.Time, elapsedTime, null);
	}

	internal static class RadialProgressBarImpl
	{
		public static void Draw(float progress, float radius, float thickness, int segments, ImGuiRadialProgressBarOptions options, ImGuiRadialProgressBarTextMode textMode, float timeValue, string? customText)
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

				// Determine start position
				float startAngle = options.HasFlag(ImGuiRadialProgressBarOptions.StartAtBottom)
					? MathF.PI * 0.5f  // Start at bottom (6 o'clock)
					: -MathF.PI * 0.5f; // Start at top (12 o'clock)

				float sweepAngle = 2.0f * MathF.PI * progress;

				if (options.HasFlag(ImGuiRadialProgressBarOptions.CounterClockwise))
				{
					// Counter-clockwise: sweep counter-clockwise from start position
					float endAngle = startAngle - sweepAngle;
					DrawArc(drawList, center, calculatedRadius, endAngle, startAngle, calculatedThickness, fgColor, segments);
				}
				else
				{
					// Clockwise (default): sweep clockwise from start position
					float endAngle = startAngle + sweepAngle;
					DrawArc(drawList, center, calculatedRadius, startAngle, endAngle, calculatedThickness, fgColor, segments);
				}
			}

			// Draw text in center
			if (!options.HasFlag(ImGuiRadialProgressBarOptions.NoText))
			{
				// Note: String allocation per frame is acceptable in immediate mode GUI
				string displayText = textMode switch
				{
					ImGuiRadialProgressBarTextMode.Percentage => $"{(int)(progress * 100.0f)}%",
					ImGuiRadialProgressBarTextMode.Time => FormatTime(timeValue),
					ImGuiRadialProgressBarTextMode.Custom => customText ?? string.Empty,
					_ => string.Empty
				};

				if (!string.IsNullOrEmpty(displayText))
				{
					Vector2 textSize = ImGui.CalcTextSize(displayText);
					Vector2 textPos = new(center.X - (textSize.X * 0.5f), center.Y - (textSize.Y * 0.5f));
					drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), displayText);
				}
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

		/// <summary>
		/// Formats a time value in seconds to a string in MM:SS or HH:MM:SS format.
		/// </summary>
		/// <param name="timeInSeconds">The time value in seconds.</param>
		/// <returns>Formatted time string.</returns>
		private static string FormatTime(float timeInSeconds)
		{
			// Handle negative times (count up from negative)
			bool isNegative = timeInSeconds < 0;
			int totalSeconds = (int)MathF.Abs(timeInSeconds);

			int hours = totalSeconds / 3600;
			int minutes = totalSeconds % 3600 / 60;
			int seconds = totalSeconds % 60;

			string formattedTime;
			if (hours > 0)
			{
				formattedTime = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
			}
			else
			{
				formattedTime = $"{minutes:D2}:{seconds:D2}";
			}

			return isNegative ? $"-{formattedTime}" : formattedTime;
		}
	}
}

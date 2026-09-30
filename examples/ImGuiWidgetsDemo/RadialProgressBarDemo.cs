// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The radial progress bar, with the countdown and count-up timers built on it.
/// </summary>
internal static class RadialProgressBarDemo
{
	/// <summary>Gets the value the progress bars are drawn at.</summary>
	internal static float ProgressValue => progressValue;

	private static float progressValue = 0.65f;
	private static bool progressAnimating;
	private static float progressAnimationSpeed = 0.3f;
	private static float countdownTime = 300.0f; // 5 minutes
	private const float CountdownTotal = 300.0f;
	private static bool countdownRunning;
	private static float countupTime;
	private const float CountupTotal = 180.0f; // 3 minutes
	private static bool countupRunning;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		progressValue = 0.65f;
		progressAnimating = false;
		progressAnimationSpeed = 0.3f;
		countdownTime = CountdownTotal;
		countdownRunning = false;
		countupTime = 0.0f;
		countupRunning = false;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Radial Progress Bar"))
		{
			ImGui.TextUnformatted("Circular progress indicators for loading and progress tracking:");
			ImGui.Separator();

			// Animation controls
			ImGui.TextUnformatted("Animation:");
			DemoProbe.Checkbox("Animate", ref progressAnimating);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(150);
			DemoProbe.SliderFloat("Speed", ref progressAnimationSpeed, 0.1f, 2.0f, "%.1fx");

			// Update animation
			if (progressAnimating)
			{
				progressValue += progressAnimationSpeed * ImGui.GetIO().DeltaTime * 0.2f;
				if (progressValue > 1.0f)
				{
					progressValue = 0.0f;
				}
			}

			ImGui.Separator();

			// Manual progress control
			ImGui.TextUnformatted("Manual Control:");
			if (DemoProbe.SliderFloat("Progress", ref progressValue, 0.0f, 1.0f, "%.2f"))
			{
				progressAnimating = false;
			}

			ImGui.Separator();

			// Show different sizes and styles
			ImGui.TextUnformatted("Different Sizes:");
			ImGui.Columns(4, "ProgressBarColumns");

			ImGuiWidgets.RadialProgressBar(progressValue, 30);
			ImGui.TextUnformatted("Small");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 50);
			ImGui.TextUnformatted("Medium");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 70);
			ImGui.TextUnformatted("Large");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 90);
			ImGui.TextUnformatted("Extra Large");

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted("Options:");

			ImGui.Columns(4, "ProgressBarOptionsColumns");

			ImGuiWidgets.RadialProgressBar(progressValue);
			ImGui.TextUnformatted("Default (CW, Top)");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.NoText);
			ImGui.TextUnformatted("No Text");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.CounterClockwise);
			ImGui.TextUnformatted("Counter-Clockwise");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.StartAtBottom);
			ImGui.TextUnformatted("Start at Bottom");

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted($"Current Progress: {progressValue * 100.0f:F1}%");

			if (DemoProbe.Button("Reset to 0%"))
			{
				progressValue = 0.0f;
				progressAnimating = false;
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Set to 50%"))
			{
				progressValue = 0.5f;
				progressAnimating = false;
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Set to 100%"))
			{
				progressValue = 1.0f;
				progressAnimating = false;
			}

			ImGui.Separator();

			// Countdown Timer Demo
			ImGui.TextUnformatted("Countdown Timer:");
			DemoProbe.Checkbox("Run Countdown", ref countdownRunning);
			ImGui.SameLine();
			if (DemoProbe.Button("Reset Countdown"))
			{
				countdownTime = CountdownTotal;
				countdownRunning = false;
			}

			if (countdownRunning && countdownTime > 0.0f)
			{
				countdownTime -= ImGui.GetIO().DeltaTime;
				if (countdownTime < 0.0f)
				{
					countdownTime = 0.0f;
					countdownRunning = false;
				}
			}

			ImGui.Columns(3, "CountdownColumns");
			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50);
			ImGui.TextUnformatted("Countdown (Top)");
			ImGui.NextColumn();

			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50, 0, 32, ImGuiRadialProgressBarOptions.CounterClockwise);
			ImGui.TextUnformatted("Counter-Clockwise");
			ImGui.NextColumn();

			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50, 0, 32, ImGuiRadialProgressBarOptions.StartAtBottom);
			ImGui.TextUnformatted("Start at Bottom");
			ImGui.Columns(1);

			ImGui.Separator();

			// Count-Up Timer Demo
			ImGui.TextUnformatted("Count-Up Timer:");
			DemoProbe.Checkbox("Run Count-Up", ref countupRunning);
			ImGui.SameLine();
			if (DemoProbe.Button("Reset Count-Up"))
			{
				countupTime = 0.0f;
				countupRunning = false;
			}

			if (countupRunning && countupTime < CountupTotal)
			{
				countupTime += ImGui.GetIO().DeltaTime;
				if (countupTime > CountupTotal)
				{
					countupTime = CountupTotal;
					countupRunning = false;
				}
			}

			ImGui.Columns(2, "CountUpColumns");
			ImGuiWidgets.RadialCountUp(countupTime, CountupTotal, 60);
			ImGui.TextUnformatted("Count-Up");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(countupTime / CountupTotal, 60, 0, 32, ImGuiRadialProgressBarOptions.None, ImGuiRadialProgressBarTextMode.Custom, 0, $"{countupTime:F1}s");
			ImGui.TextUnformatted("Custom Text");
			ImGui.Columns(1);
		}
	}
}

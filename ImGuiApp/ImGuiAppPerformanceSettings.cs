// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

/// <summary>
/// Represents performance settings for throttled rendering to save system resources.
/// </summary>
public class ImGuiAppPerformanceSettings
{
	/// <summary>
	/// Gets or sets a value indicating whether throttled rendering is enabled.
	/// When true, the application will reduce frame rate when unfocused or idle.
	/// </summary>
	public bool EnableThrottledRendering { get; init; } = true;

	/// <summary>
	/// Gets or sets the target frame rate (FPS) when the application window is focused and active.
	/// </summary>
	public double FocusedFps { get; init; } = 30.0;

	/// <summary>
	/// Gets or sets the target frame rate (FPS) when the application window is unfocused.
	/// </summary>
	public double UnfocusedFps { get; init; } = 5.0;

	/// <summary>
	/// Gets or sets the target frame rate (FPS) when the application is idle (no user input).
	/// </summary>
	public double IdleFps { get; init; } = 10.0;

	/// <summary>
	/// Gets or sets the target frame rate (FPS) when the application window is not visible (minimized or hidden).
	/// </summary>
	public double NotVisibleFps { get; init; } = 0.2;

	/// <summary>
	/// Gets or sets a value indicating whether idle detection is enabled.
	/// When true, the application will detect when there's no user input and reduce frame rate further.
	/// </summary>
	public bool EnableIdleDetection { get; init; } = true;

	/// <summary>
	/// Gets or sets the time (in seconds) of no user input before the application is considered idle.
	/// </summary>
	public double IdleTimeoutSeconds { get; init; } = 30.0;
}
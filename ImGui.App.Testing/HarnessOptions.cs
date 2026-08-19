// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

/// <summary>
/// Everything that would otherwise vary between runs, pinned so a scenario produces the same result
/// on a developer machine and on a continuous integration runner.
/// </summary>
public sealed record HarnessOptions
{
	/// <summary>Gets the display width in pixels.</summary>
	public int Width { get; init; } = 1280;

	/// <summary>Gets the display height in pixels.</summary>
	public int Height { get; init; } = 720;

	/// <summary>Gets the framebuffer scale. Fixed so layout does not follow the host display.</summary>
	public float DpiScale { get; init; } = 1.0f;

	/// <summary>
	/// Gets the seconds reported to the application for every frame, regardless of how long the
	/// frame really took. Wall-clock timing is what makes user interface suites flaky.
	/// </summary>
	public float FrameDelta { get; init; } = 1f / 60f;

	/// <summary>Gets the color the render target is filled with before each frame.</summary>
	public Rgba32 ClearColor { get; init; } = new(0, 0, 0, 255);
}

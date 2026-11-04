// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;

/// <summary>
/// A platform's HDR presentation for a window drawn with OpenGL: a target OpenGL draws the finished
/// frame into, which the platform then shows in HDR.
/// </summary>
internal interface IHdrPresenter : IDisposable
{
	/// <summary>Gets whether the target's first row is its top, as in Direct3D and Metal, so OpenGL has to draw it upside down.</summary>
	public bool FlipsVertically { get; }

	/// <summary>Asks the platform what the screen the window is on can show now.</summary>
	/// <returns>What it can show.</returns>
	public HdrDisplayInfo QueryDisplay();

	/// <summary>
	/// Gets how a linear value, 1 being SDR white, maps onto the target: the factor it is multiplied by
	/// and the largest value the target is given.
	/// </summary>
	/// <param name="display">What the screen can show.</param>
	/// <returns>The factor and the limit.</returns>
	public (float Scale, float Limit) OutputRange(HdrDisplayInfo display);

	/// <summary>Makes the target ready for a frame of the given size and gives its framebuffer.</summary>
	/// <param name="frameWidth">The frame's width in pixels.</param>
	/// <param name="frameHeight">The frame's height in pixels.</param>
	/// <param name="framebuffer">The OpenGL framebuffer to draw the frame into.</param>
	/// <returns>Whether the target is ready; false skips the frame.</returns>
	public bool BeginOutput(int frameWidth, int frameHeight, out uint framebuffer);

	/// <summary>Presents what was drawn since <see cref="BeginOutput"/>.</summary>
	public void EndOutput();
}

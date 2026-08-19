// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Injects mouse events straight into ImGui's event queue, the same mechanism the iOS platform port
/// uses. Nothing is sent to the operating system, so no window needs focus.
/// </summary>
/// <param name="harness">The harness whose frames these events feed.</param>
public sealed class HarnessMouse(ImGuiAppHarness harness)
{
	/// <summary>Gets the last position the pointer was moved to.</summary>
	public (float X, float Y) Position { get; private set; }

	/// <summary>Moves the pointer without advancing a frame.</summary>
	/// <param name="x">Target column in display pixels.</param>
	/// <param name="y">Target row in display pixels.</param>
	public void MoveTo(float x, float y)
	{
		Position = (x, y);
		ImGui.GetIO().AddMousePosEvent(x, y);
	}

	/// <summary>Presses a button without advancing a frame.</summary>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public static void Down(int button) => ImGui.GetIO().AddMouseButtonEvent(button, true);

	/// <summary>Releases a button without advancing a frame.</summary>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public static void Up(int button) => ImGui.GetIO().AddMouseButtonEvent(button, false);

	/// <summary>
	/// Clicks at a position, advancing the frames the interaction needs.
	/// </summary>
	/// <remarks>
	/// ImGui activates a button on release, and only notices a press that was visible during a
	/// completed frame, so a press and release inside one frame does nothing at all.
	/// </remarks>
	/// <param name="x">Column in display pixels.</param>
	/// <param name="y">Row in display pixels.</param>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Click(float x, float y, int button = 0)
	{
		Ensure.NotNull(harness);

		MoveTo(x, y);
		harness.Step();

		Down(button);
		harness.Step();

		Up(button);
		harness.Step();
	}

	/// <summary>Presses at one position, moves through intermediate points, and releases at another.</summary>
	/// <param name="fromX">Start column.</param>
	/// <param name="fromY">Start row.</param>
	/// <param name="toX">End column.</param>
	/// <param name="toY">End row.</param>
	/// <param name="steps">How many intermediate positions to visit. Must be positive.</param>
	/// <param name="button">Zero for left, one for right, two for middle.</param>
	public void Drag(float fromX, float fromY, float toX, float toY, int steps = 16, int button = 0)
	{
		Ensure.NotNull(harness);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);

		MoveTo(fromX, fromY);
		harness.Step();

		Down(button);
		harness.Step();

		for (int i = 1; i <= steps; i++)
		{
			float t = (float)i / steps;
			MoveTo(fromX + ((toX - fromX) * t), fromY + ((toY - fromY) * t));
			harness.Step();
		}

		Up(button);
		harness.Step();
	}

	/// <summary>Scrolls the wheel at a position and advances one frame.</summary>
	/// <param name="x">Column in display pixels.</param>
	/// <param name="y">Row in display pixels.</param>
	/// <param name="clicks">Wheel detents. Positive scrolls up.</param>
	public void Wheel(float x, float y, int clicks)
	{
		Ensure.NotNull(harness);

		// The move gets its own frame. ImGui batches input events and defers some of them when a
		// position change arrives in the same frame as another input, so combining the two here
		// silently loses the wheel delta.
		MoveTo(x, y);
		harness.Step();

		ImGui.GetIO().AddMouseWheelEvent(0f, clicks);
		harness.Step();
	}
}

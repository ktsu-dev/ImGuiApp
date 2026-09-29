// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;
using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

/// <summary>
/// What an entry's callbacks can reach while its tile is being captured: the live harness, a sample
/// image, and the handful of gestures a tile needs to show a widget in an interesting state.
/// </summary>
/// <param name="harness">The harness the entry is drawn in.</param>
internal sealed class GalleryContext(ImGuiAppHarness harness)
{
	/// <summary>Gets the pixels of the frame drawn before the entry was, set by the capture.</summary>
	public byte[] Baseline { get; internal set; } = [];

	/// <summary>Gets the live harness.</summary>
	public ImGuiAppHarness Harness { get; } = harness;

	/// <summary>Gets the seconds of simulated time since the harness started.</summary>
	public float Time => Harness.FrameCount * Harness.Options.FrameDelta;

	/// <summary>
	/// Gets a sample image for the widgets that show one, created on first use.
	/// </summary>
	/// <remarks>
	/// Generated rather than loaded, because the widgets demo's <c>ktsu.png</c> arrives as a Git LFS
	/// pointer in some checkouts, and a gallery showing a broken image is worse than one showing a
	/// gradient.
	/// </remarks>
	public ImGuiAppTextureInfo SampleTexture => field ??= SampleImage.Create();

	/// <summary>Gets the sample image's texture id.</summary>
	public nint SampleTextureId => SampleTexture.TextureId;

	/// <summary>Moves the pointer to a point and lets hover state settle.</summary>
	/// <param name="point">The point, in display pixels.</param>
	public void HoverAt(Vector2 point)
	{
		Harness.Mouse.MoveTo(point.X, point.Y);
		Harness.Step(3);
	}

	/// <summary>Moves the pointer to the centre of a marked item and lets hover state settle.</summary>
	/// <param name="name">A probe name or its trailing part.</param>
	public void Hover(string name) => HoverAt(CenterOf(name));

	/// <summary>Clicks the centre of a marked item and draws the result.</summary>
	/// <param name="name">A probe name or its trailing part.</param>
	public void Click(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Step(3);
	}

	/// <summary>Clicks a point and draws the result.</summary>
	/// <param name="point">The point, in display pixels.</param>
	public void ClickAt(Vector2 point)
	{
		Harness.Mouse.Click(point.X, point.Y);
		Harness.Step(3);
	}

	/// <summary>Gets the rectangle recorded for a marked item.</summary>
	/// <param name="name">A probe name or its trailing part.</param>
	/// <returns>The rectangle.</returns>
	public Rectangle RectOf(string name) => Harness.Probe.Rect(name)
		?? throw new InvalidOperationException(
			$"No item matching '{name}' was marked. Marked so far: {string.Join(", ", Harness.Probe.KnownNames)}.");

	/// <summary>Gets the centre of a marked item.</summary>
	/// <param name="name">A probe name or its trailing part.</param>
	/// <returns>The centre.</returns>
	public Vector2 CenterOf(string name)
	{
		Rectangle rect = RectOf(name);
		return new Vector2(rect.MinX + (rect.Width / 2f), rect.MinY + (rect.Height / 2f));
	}

	/// <summary>
	/// Clicks the leftmost button along the bottom of an open Hexa dialog, which is how a message
	/// box or a rename dialog is answered when nothing about it can be addressed by name.
	/// </summary>
	/// <returns>True when a button was found and clicked.</returns>
	/// <remarks>
	/// The same pixel search the widget suite's <c>WidgetTest.FindDialogButtons</c> uses: the
	/// buttons are the lowest run of theme-blue pixels on screen.
	/// </remarks>
	public bool ClickFirstDialogButton()
	{
		List<Rectangle> buttons = FindDialogButtons();

		if (buttons.Count == 0)
		{
			return false;
		}

		Rectangle button = buttons[0];
		ClickAt(new Vector2(button.MinX + (button.Width / 2f), button.MinY + (button.Height / 2f)));
		return true;
	}

	/// <summary>
	/// Closes a dialog through the close button at the top right of its title bar, which runs the
	/// same close path as its Cancel button.
	/// </summary>
	/// <param name="window">The dialog's rectangle on screen.</param>
	public void CloseWindowAt(Rectangle window) =>
		ClickAt(new Vector2(window.MaxX - 13f, window.MinY + 10f));

	/// <summary>Finds the tight rectangle around every pixel that differs from <see cref="Baseline"/>.</summary>
	/// <returns>The rectangle, or null when nothing differs.</returns>
	public Rectangle? ChangedBounds() => TileCapture.BoundsOfDifference(Baseline, Harness.Target);

	private List<Rectangle> FindDialogButtons()
	{
		int width = Harness.Options.Width;
		byte[] pixels = Harness.Target.Pixels.ToArray();

		bool IsBlue(int x, int y)
		{
			int i = ((y * width) + x) * 4;
			return pixels[i + 2] > pixels[i] + 40 && pixels[i + 2] > 90;
		}

		int bottom = LowestRowWhere(width, Harness.Options.Height, IsBlue);
		return bottom < 0 ? [] : RunsAlongRow(width, Math.Max(bottom - 4, 0), bottom, IsBlue);
	}

	/// <summary>Finds the lowest row holding a pixel that matches.</summary>
	private static int LowestRowWhere(int width, int height, Func<int, int, bool> matches)
	{
		for (int y = height - 1; y >= 0; y--)
		{
			for (int x = 0; x < width; x++)
			{
				if (matches(x, y))
				{
					return y;
				}
			}
		}

		return -1;
	}

	/// <summary>Turns each run of matching pixels along one row into a button rectangle ending at <paramref name="bottom"/>.</summary>
	private static List<Rectangle> RunsAlongRow(int width, int row, int bottom, Func<int, int, bool> matches)
	{
		List<Rectangle> buttons = [];
		int runStart = -1;

		for (int x = 0; x <= width; x++)
		{
			bool match = x < width && matches(x, row);

			if (match && runStart < 0)
			{
				runStart = x;
			}
			else if (!match && runStart >= 0)
			{
				buttons.Add(new Rectangle(runStart, bottom - 8, x, bottom));
				runStart = -1;
			}
		}

		return buttons;
	}
}

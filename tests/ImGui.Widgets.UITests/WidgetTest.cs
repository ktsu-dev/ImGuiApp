// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Probes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Base class for the widget suites: starts a headless application whose entire render callback is
/// the widget under test, and offers the handful of operations those tests need.
/// </summary>
/// <remarks>
/// <para>
/// The point of this suite is isolation. Each test drives one widget with nothing else on screen,
/// so a failure names the widget rather than a demo page that happens to contain it, and so a
/// widget is covered whether or not any example application uses it. That is the difference from
/// the per-demo suites in <c>tests/&lt;Demo&gt;.UITests</c>, which drive a real application's
/// configuration end to end.
/// </para>
/// <para>
/// The render callback runs inside ImGuiApp's own full-viewport window, exactly as an application's
/// does, because the harness drives <see cref="ImGuiApp.RenderFrameContents"/> rather than a copy of
/// it. Probe names are therefore qualified as <c>##mainWindow/&lt;name&gt;</c>, and a lookup by the
/// trailing segment alone resolves.
/// </para>
/// </remarks>
public abstract class WidgetTest
{
	/// <summary>
	/// The viewport a widget test gets unless it asks for another.
	/// </summary>
	/// <remarks>
	/// Deliberately small. The software rasterizer's cost scales with the area it fills, and one
	/// widget on an otherwise empty window needs nowhere near the harness default of 1280x720.
	/// </remarks>
	protected static HarnessOptions DefaultViewport { get; } = new() { Width = 640, Height = 480 };

	private ImGuiAppHarness? harness;

	/// <summary>Gets a value indicating whether a harness is running.</summary>
	protected bool IsRunning => harness is not null;

	/// <summary>Gets the live harness. Valid only after <see cref="Start"/>.</summary>
	protected ImGuiAppHarness Harness => harness
		?? throw new InvalidOperationException("No harness is running. Call Start first.");

	/// <summary>
	/// Starts an application whose only content is the supplied draw callback, and advances enough
	/// frames for the first layout to settle.
	/// </summary>
	/// <remarks>
	/// ImGui sizes and positions many things from the previous frame, so a widget's recorded
	/// rectangle is only trustworthy once a second frame has been drawn. Every test therefore
	/// starts from a settled layout rather than from frame zero.
	/// </remarks>
	/// <param name="draw">Draws the widget under test. Called once per frame.</param>
	/// <param name="options">Viewport and determinism settings. Defaults to <see cref="DefaultViewport"/>.</param>
	/// <param name="enableDocking">Whether the docking flag is set before the first frame.</param>
	protected void Start(Action draw, HarnessOptions? options = null, bool enableDocking = false)
	{
		ArgumentNullException.ThrowIfNull(draw);

		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = GetType().Name,
				OnRender = _ => draw(),
				EnableDocking = enableDocking,
				SaveIniSettings = false,
			},
			options ?? DefaultViewport);

		harness.Step(2);
	}

	/// <summary>Releases the harness, and with it the ImGui context, so the next test can start one.</summary>
	[TestCleanup]
	public void DisposeHarness()
	{
		harness?.Dispose();
		harness = null;
	}

	/// <summary>Advances frames.</summary>
	/// <param name="frames">How many frames to advance.</param>
	protected void Step(int frames = 1) => Harness.Step(frames);

	/// <summary>
	/// Records the item just submitted, for a widget that does not mark itself.
	/// </summary>
	/// <remarks>
	/// Most of <c>ktsu.ImGui.Widgets</c> marks its own items, but the purely decorative ones — a
	/// badge, a skeleton line, a spinner — do not, because nothing interactive lives inside them.
	/// A test that needs to locate one marks it from the draw callback, the same way the demos mark
	/// the plain ImGui controls they draw.
	/// </remarks>
	/// <param name="name">A stable name for the item.</param>
	protected static void Mark(string name) => ImGuiProbes.MarkItem(name);

	/// <summary>
	/// Records everything a widget drew between a remembered cursor position and the item it
	/// submitted last, for a widget that submits several items and marks none of them.
	/// </summary>
	/// <remarks>
	/// A PIN input is a row of separate text boxes; a stepper is two buttons around a value. Their
	/// parts are real ImGui items with identifiers of their own, so a test can only aim at one by
	/// measuring the span they occupy together and clicking a fraction across it.
	/// </remarks>
	/// <param name="name">A stable name for the span.</param>
	/// <param name="origin">The cursor position captured before the widget was drawn.</param>
	protected static void MarkSpan(string name, Vector2 origin) =>
		ImGuiProbes.MarkRegion(name, origin, ImGui.GetItemRectMax());

	/// <summary>
	/// Reports whether an item was drawn in the frame just rendered.
	/// </summary>
	/// <remarks>
	/// <see cref="ItemProbe.Rect"/> returns the last position an item ever occupied and never
	/// expires, so it answers "was this ever drawn" rather than "is it on screen now". Anything
	/// that can disappear — a popup, a collapsed node's children — has to be asked this way.
	/// </remarks>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <returns>True when the item was drawn in the most recent frame.</returns>
	protected bool IsVisible(string name) => Harness.Probe.WasSeenInFrame(name, Harness.FrameCount - 1);

	/// <summary>Gets the rectangle recorded for a named item, failing the test when there is none.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <returns>The recorded rectangle.</returns>
	protected Rectangle RectOf(string name) => Harness.Probe.Rect(name)
		?? throw new InvalidOperationException(
			$"No item matching '{name}' was ever marked. Marked so far: {string.Join(", ", Harness.Probe.KnownNames)}.");

	/// <summary>Gets the center of a named item, in display pixels.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <returns>The center point.</returns>
	protected Vector2 CenterOf(string name)
	{
		Rectangle rect = RectOf(name);
		return new Vector2(rect.MinX + (rect.Width / 2f), rect.MinY + (rect.Height / 2f));
	}

	/// <summary>Clicks a named item and advances one further frame so the result is drawn.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	protected void Click(string name)
	{
		Harness.Click(name);
		Harness.Step();
	}

	/// <summary>Clicks a point inside a named item, offset from its top-left corner.</summary>
	/// <remarks>
	/// For widgets whose behavior depends on where inside them the click landed — which star of a
	/// rating, which segment of a segmented control — the center is the wrong place to aim.
	/// </remarks>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <param name="offsetX">Horizontal offset from the item's left edge.</param>
	/// <param name="offsetY">Vertical offset from the item's top edge.</param>
	protected void ClickWithin(string name, float offsetX, float offsetY)
	{
		Rectangle rect = RectOf(name);
		Harness.Mouse.Click(rect.MinX + offsetX, rect.MinY + offsetY);
		Harness.Step();
	}

	/// <summary>Clicks a fraction of the way across and down a named item.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <param name="fractionX">Horizontal position, zero at the left edge and one at the right.</param>
	/// <param name="fractionY">Vertical position, zero at the top edge and one at the bottom.</param>
	protected void ClickFraction(string name, float fractionX, float fractionY = 0.5f)
	{
		Rectangle rect = RectOf(name);
		ClickWithin(name, rect.Width * fractionX, rect.Height * fractionY);
	}

	/// <summary>Drags from one fraction across a named item to another.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	/// <param name="fromFractionX">Where the press lands, as a fraction of the item's width.</param>
	/// <param name="toFractionX">Where the release lands, as a fraction of the item's width.</param>
	/// <param name="fractionY">The vertical position of both, as a fraction of the item's height.</param>
	protected void DragAcross(string name, float fromFractionX, float toFractionX, float fractionY = 0.5f)
	{
		Rectangle rect = RectOf(name);
		float y = rect.MinY + (rect.Height * fractionY);

		Harness.Mouse.Drag(
			rect.MinX + (rect.Width * fromFractionX),
			y,
			rect.MinX + (rect.Width * toFractionX),
			y);

		Harness.Step();
	}

	/// <summary>Moves the pointer over a named item and advances a frame so hover state settles.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	protected void Hover(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.MoveTo(center.X, center.Y);
		Harness.Step(2);
	}

	/// <summary>Clicks well clear of a named item, so the click reaches the window and nothing else.</summary>
	/// <param name="name">A full probe name or its trailing part.</param>
	protected void ClickAwayFrom(string name)
	{
		Rectangle rect = RectOf(name);
		Harness.Mouse.Click(rect.MaxX + 40f, rect.MaxY + 40f);
		Harness.Step();
	}

	/// <summary>Moves the pointer to a corner no widget occupies, so nothing is hovered.</summary>
	protected void MoveAway()
	{
		Harness.Mouse.MoveTo(-1f, -1f);
		Harness.Step();
	}

	/// <summary>Copies the pixels of the frame just rendered, so a later frame can be compared to it.</summary>
	/// <returns>A copy of the current framebuffer.</returns>
	protected byte[] Snapshot() => Harness.Target.Pixels.ToArray();

	/// <summary>
	/// Counts the pixels that differ between a snapshot and the frame just rendered.
	/// </summary>
	/// <remarks>
	/// Nothing here animates between frames unless a widget animates it, so for a static widget
	/// two consecutive frames are identical byte for byte. That makes a pixel difference a usable
	/// signal for "something visibly changed" without a golden image to maintain.
	/// </remarks>
	/// <param name="before">A snapshot taken earlier.</param>
	/// <returns>The number of differing bytes.</returns>
	protected int PixelsChangedSince(byte[] before)
	{
		ArgumentNullException.ThrowIfNull(before);

		Span<byte> now = Harness.Target.Pixels;
		int changed = 0;

		for (int i = 0; i < before.Length && i < now.Length; i++)
		{
			if (before[i] != now[i])
			{
				changed++;
			}
		}

		return changed;
	}

	/// <summary>
	/// Finds the tight rectangle around every pixel that differs from an earlier snapshot.
	/// </summary>
	/// <remarks>
	/// The probe records the rectangle ImGui reported for an item, which is the right answer for a
	/// widget that submits one. It is the wrong answer for the alignment helpers: they position the
	/// cursor, draw, and then leave a zero-width spacer as the last item, so a mark taken after the
	/// call measures the spacer. Diffing the frame against one drawn without the widget measures
	/// what actually landed on screen instead of what ImGui last called an item.
	/// </remarks>
	/// <param name="baseline">A snapshot of the frame drawn without the widget.</param>
	/// <returns>The bounding rectangle of the changed pixels, or null when nothing changed.</returns>
	protected Rectangle? BoundsOfDifference(byte[] baseline)
	{
		ArgumentNullException.ThrowIfNull(baseline);

		Span<byte> now = Harness.Target.Pixels;
		int width = Harness.Options.Width;
		int height = Harness.Options.Height;

		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int i = ((y * width) + x) * 4;

				if (baseline[i] == now[i]
					&& baseline[i + 1] == now[i + 1]
					&& baseline[i + 2] == now[i + 2]
					&& baseline[i + 3] == now[i + 3])
				{
					continue;
				}

				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x + 1);
				maxY = Math.Max(maxY, y + 1);
			}
		}

		return minX == int.MaxValue ? null : new Rectangle(minX, minY, maxX, maxY);
	}

	/// <summary>
	/// Finds the buttons along the bottom of an open Hexa dialog.
	/// </summary>
	/// <remarks>
	/// Hexa's dialogs are vendor windows that mark nothing and expose no geometry, so a test can
	/// only reach their buttons through the pixels. Both the title bar and the buttons are drawn in
	/// the theme's blue, and the buttons are the lowest blue thing on screen, so the bottom row of
	/// the blue region is the button row and each run of blue columns across it is one button.
	/// </remarks>
	/// <returns>The buttons, left to right. Empty when no dialog is open.</returns>
	protected IReadOnlyList<Rectangle> FindDialogButtons()
	{
		int width = Harness.Options.Width;
		int height = Harness.Options.Height;
		byte[] pixels = Harness.Target.Pixels.ToArray();

		bool IsBlue(int x, int y)
		{
			int i = ((y * width) + x) * 4;
			return pixels[i + 2] > pixels[i] + 40 && pixels[i + 2] > 90;
		}

		int bottom = -1;

		for (int y = height - 1; y >= 0 && bottom < 0; y--)
		{
			for (int x = 0; x < width; x++)
			{
				if (IsBlue(x, y))
				{
					bottom = y;
					break;
				}
			}
		}

		if (bottom < 0)
		{
			return [];
		}

		// A few rows up from the last blue row, to sample the middle of the button rather than its
		// antialiased bottom edge.
		int row = Math.Max(bottom - 4, 0);
		List<Rectangle> buttons = [];
		int runStart = -1;

		for (int x = 0; x <= width; x++)
		{
			bool blue = x < width && IsBlue(x, row);

			if (blue && runStart < 0)
			{
				runStart = x;
			}
			else if (!blue && runStart >= 0)
			{
				buttons.Add(new Rectangle(runStart, bottom - 8, x, bottom));
				runStart = -1;
			}
		}

		return buttons;
	}

	/// <summary>
	/// Closes a dialog through the close button in its title bar.
	/// </summary>
	/// <remarks>
	/// The file dialogs are large windows whose action buttons sit at the bottom of a layout the
	/// test cannot measure, but every one of them has a close button at the top right of its title
	/// bar, and Hexa runs the same close path for it as for Cancel.
	/// </remarks>
	/// <param name="beforeItOpened">A snapshot taken before the dialog was shown, used to find it.</param>
	protected void CloseDialogWindow(byte[] beforeItOpened)
	{
		Rectangle window = BoundsOfDifference(beforeItOpened)
			?? throw new InvalidOperationException("No dialog is open, so there is nothing to close.");

		Harness.Mouse.Click(window.MaxX - 13f, window.MinY + 10f);
		Harness.Step();
	}

	/// <summary>
	/// Answers every dialog left open, so none of them survives into the next test.
	/// </summary>
	/// <remarks>
	/// Hexa's dialog managers are process-static: an unanswered dialog outlives the harness that
	/// showed it and is drawn again by the next test's pump, on top of whatever that test is
	/// looking at. Call this from a test cleanup in any suite that opens one.
	/// </remarks>
	protected void DismissOpenDialogs()
	{
		if (!IsRunning)
		{
			return;
		}

		for (int attempt = 0; attempt < 8 && FindDialogButtons().Count > 0; attempt++)
		{
			ClickDialogButton(0);
			Harness.Step(2);
		}
	}

	/// <summary>Clicks one of the buttons along the bottom of an open Hexa dialog.</summary>
	/// <param name="index">Which button, counted from the left.</param>
	protected void ClickDialogButton(int index)
	{
		IReadOnlyList<Rectangle> buttons = FindDialogButtons();

		if (index >= buttons.Count)
		{
			throw new InvalidOperationException(
				$"The open dialog has {buttons.Count} buttons, so there is no button {index}.");
		}

		Rectangle button = buttons[index];
		Harness.Mouse.Click(button.MinX + (button.Width / 2f), button.MinY + (button.Height / 2f));
		Harness.Step();
	}

	/// <summary>Asserts that the widget drew something other than the window background.</summary>
	/// <param name="because">What the assertion is guarding, quoted back on failure.</param>
	protected void AssertSomethingWasDrawn(string because)
	{
		CapturedFrame frame = Harness.Capture();

		Assert.IsNotNull(
			frame.FindBounds(pixel => pixel.A > 0),
			$"The frame was entirely blank, so {because} drew nothing at all.");
	}

	/// <summary>
	/// Creates a texture the software rasterizer can sample, for the widgets that take one.
	/// </summary>
	/// <remarks>
	/// Generated rather than loaded from disk: a test that needs an image should not also depend on
	/// a file being where it expects. The texture belongs to the live renderer, so it is created
	/// after <see cref="Start"/> and dies with the harness.
	/// </remarks>
	/// <param name="size">The width and height in pixels.</param>
	/// <returns>The texture handle to pass to the widget.</returns>
	protected static ImGuiAppTextureInfo CreateTestTexture(int size = 32)
	{
		byte[] rgba = new byte[size * size * 4];

		for (int i = 0; i < size * size; i++)
		{
			rgba[(i * 4) + 0] = 255;
			rgba[(i * 4) + 1] = 128;
			rgba[(i * 4) + 2] = 0;
			rgba[(i * 4) + 3] = 255;
		}

		return ImGuiApp.CreateTexture(rgba, size, size);
	}
}

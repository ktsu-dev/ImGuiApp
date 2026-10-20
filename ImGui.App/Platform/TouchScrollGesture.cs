// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Turns touches into ImGui mouse input: one finger is the mouse, and two or more fingers moving
/// together scroll whatever is under them.
/// </summary>
/// <remarks>
/// <para>
/// Dear ImGui scrolls only through the wheel or a scrollbar drag, so without this a list taller
/// than its window can be scrolled on a touch screen only by hitting its scrollbar. A one-finger
/// drag stays a mouse drag, because that is what moves a slider or a window.
/// </para>
/// <para>
/// When a second finger lands, the first finger's press is released with the mouse moved off the
/// screen first, so the release hovers nothing and a button under the first finger is not clicked.
/// A press already applied stays applied: a slider the first finger touched has its new value.
/// The mouse then goes to the fingers' centroid and stays there for the whole pan, as a trackpad's
/// pointer does, so the wheel scrolls the window the pan began over.
/// </para>
/// <para>
/// The content follows the fingers: a pan of one wheel step's worth of points scrolls one step,
/// and moving the fingers down reveals what is above. Once a pan has begun, lifting fingers until
/// one is left ends it without pressing anything; the touch is spent until every finger is up.
/// </para>
/// <para>
/// Nothing here touches ImGui or UIKit, so the rules are unit tested on any platform.
/// </para>
/// </remarks>
/// <param name="sink">Where the mouse input goes.</param>
internal sealed class TouchScrollGesture(ITouchPointerSink sink)
{
	/// <summary>The position ImGui reads as "no mouse".</summary>
	internal static readonly Vector2 Offscreen = new(-float.MaxValue, -float.MaxValue);

	/// <summary>
	/// Gets how many points one wheel step scrolls a window, across and down: Dear ImGui scrolls
	/// two lines' height across and five down per step, truncated to whole points, and less for a
	/// window too small to hold that.
	/// </summary>
	/// <param name="fontSize">The window's font size, in points.</param>
	/// <param name="innerSize">The window's inner size, in points.</param>
	/// <returns>The points of pan that make one wheel step on each axis.</returns>
	internal static Vector2 PointsPerWheelStep(float fontSize, Vector2 innerSize) => new(
		MathF.Truncate(Math.Min(2f * fontSize, innerSize.X * 0.67f)),
		MathF.Truncate(Math.Min(5f * fontSize, innerSize.Y * 0.67f)));

	private readonly Dictionary<object, Vector2> touches = [];
	private object? primary;
	private Vector2 lastCentroid;

	private enum Mode
	{
		Idle,
		Pointer,
		Pan,
		Spent,
	}

	private Mode mode = Mode.Idle;

	/// <summary>Gets a value indicating whether two or more fingers are scrolling.</summary>
	internal bool IsPanning => mode == Mode.Pan;

	/// <summary>Reports a finger landing.</summary>
	/// <param name="id">The touch, the same object for its whole life.</param>
	/// <param name="position">Where it landed, in points.</param>
	internal void Began(object id, Vector2 position)
	{
		Ensure.NotNull(id);
		touches[id] = position;

		switch (mode)
		{
			case Mode.Idle when touches.Count == 1:
				mode = Mode.Pointer;
				primary = id;
				sink.MovePointer(position);
				sink.SetButton(down: true);
				break;

			case Mode.Pointer when touches.Count >= 2:
				sink.MovePointer(Offscreen);
				sink.SetButton(down: false);
				primary = null;
				StartPan();
				break;

			case Mode.Spent when touches.Count >= 2:
				StartPan();
				break;

			case Mode.Pan:
				// Another finger joins: re-measure so the centroid's jump is not read as a scroll.
				lastCentroid = Centroid();
				break;

			default:
				break;
		}
	}

	/// <summary>Reports a finger moving.</summary>
	/// <param name="id">The touch.</param>
	/// <param name="position">Where it is now, in points.</param>
	/// <param name="pointsPerWheelStep">How many points of pan make one wheel step, across and down.</param>
	internal void Moved(object id, Vector2 position, Vector2 pointsPerWheelStep)
	{
		Ensure.NotNull(id);
		if (!touches.ContainsKey(id))
		{
			return;
		}

		touches[id] = position;

		if (mode == Mode.Pointer && Equals(id, primary))
		{
			sink.MovePointer(position);
		}
		else if (mode == Mode.Pan)
		{
			Vector2 centroid = Centroid();
			Vector2 delta = centroid - lastCentroid;
			lastCentroid = centroid;
			if (delta != Vector2.Zero && pointsPerWheelStep.X > 0 && pointsPerWheelStep.Y > 0)
			{
				sink.Scroll(delta.X / pointsPerWheelStep.X, delta.Y / pointsPerWheelStep.Y);
			}
		}
	}

	/// <summary>Reports a finger lifting, or UIKit cancelling the touch.</summary>
	/// <param name="id">The touch.</param>
	/// <param name="position">Where it was last, in points.</param>
	internal void Ended(object id, Vector2 position)
	{
		Ensure.NotNull(id);
		if (!touches.Remove(id))
		{
			return;
		}

		switch (mode)
		{
			case Mode.Pointer when Equals(id, primary):
				sink.MovePointer(position);
				sink.SetButton(down: false);
				primary = null;
				break;

			case Mode.Pan when touches.Count >= 2:
				lastCentroid = Centroid();
				break;

			case Mode.Pan:
				mode = Mode.Spent;
				break;

			default:
				break;
		}

		if (touches.Count == 0)
		{
			mode = Mode.Idle;
		}
	}

	private void StartPan()
	{
		mode = Mode.Pan;
		lastCentroid = Centroid();
		sink.MovePointer(lastCentroid);
	}

	private Vector2 Centroid()
	{
		Vector2 sum = Vector2.Zero;
		foreach (Vector2 position in touches.Values)
		{
			sum += position;
		}

		return sum / Math.Max(1, touches.Count);
	}
}

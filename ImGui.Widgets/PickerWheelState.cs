// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>How a picker wheel looks and behaves.</summary>
public sealed record PickerWheelOptions
{
	/// <summary>Gets how many rows the wheel shows, the selected one in the middle. Odd numbers keep it centred; the default is five.</summary>
	public int VisibleRows { get; init; } = 5;

	/// <summary>Gets the wheel's width in pixels, or zero to fit its widest item.</summary>
	public float Width { get; init; }

	/// <summary>
	/// Gets a value indicating whether the items repeat, so the first follows the last, as the hours and
	/// minutes on a clock do. Off by default, when the wheel stops at either end.
	/// </summary>
	public bool Wrap { get; init; }
}

/// <summary>
/// A picker wheel's position and snapping rules, with no ImGui calls, so the arithmetic can be tested on
/// its own.
/// </summary>
/// <remarks>
/// The position is measured in rows: 3 has the fourth item in the middle, and 3.5 sits halfway between
/// the fourth and fifth. A wheel that wraps lets the position run past either end, and an item's index is
/// the position modulo the count, so spinning through the end of the list keeps the motion continuous.
/// While a drag is under way the position follows the pointer; on release it carries the drag's momentum
/// into a critically damped spring that settles on a whole row.
/// </remarks>
internal sealed class PickerWheelState
{
	/// <summary>
	/// The rate a fling's speed decays at, per second, matching <see cref="Scroll.InertialScroll"/>'s
	/// default. A fling coasts its speed divided by this, which is how far ahead
	/// <see cref="ResolveTarget"/> looks.
	/// </summary>
	internal const float Friction = 6f;

	/// <summary>How far past the first or last item a drag may pull, in rows, on a wheel that does not wrap.</summary>
	internal const float Overscroll = 0.5f;

	private const float Stiffness = 200f;

	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.002f };
	private float pressPosition;

	/// <summary>Initializes a new instance of the <see cref="PickerWheelState"/> class.</summary>
	/// <param name="index">The item to start on.</param>
	public PickerWheelState(int index)
	{
		Target = index;
		spring.SnapTo(index);
	}

	/// <summary>Gets the position, in rows.</summary>
	public float Position => spring.Value;

	/// <summary>
	/// Gets the whole row the wheel is showing or settling on. On a wheel that wraps it can lie outside
	/// the items; <see cref="WrapIndex"/> turns it into one.
	/// </summary>
	public int Target { get; private set; }

	/// <summary>Gets a value indicating whether a drag is under way.</summary>
	public bool IsDragging { get; private set; }

	/// <summary>Starts a drag from wherever the wheel is now.</summary>
	public void BeginDrag()
	{
		pressPosition = Position;
		IsDragging = true;
	}

	/// <summary>Turns the wheel with the pointer.</summary>
	/// <param name="deltaY">How far the pointer has moved since the drag began, in pixels, positive downwards.</param>
	/// <param name="rowHeight">The height of one row, in pixels.</param>
	/// <param name="count">How many items there are.</param>
	/// <param name="wrap">Whether the items repeat.</param>
	public void Drag(float deltaY, float rowHeight, int count, bool wrap)
	{
		// Dragging down brings earlier items into the middle, as on a phone.
		float position = pressPosition - (deltaY / MathF.Max(rowHeight, 1f));
		if (!wrap)
		{
			position = Math.Clamp(position, -Overscroll, MathF.Max(count - 1, 0) + Overscroll);
		}

		spring.SnapTo(position);
	}

	/// <summary>Ends a drag and sets the wheel settling on a row.</summary>
	/// <param name="velocityY">The pointer's vertical speed at release, in pixels per second.</param>
	/// <param name="rowHeight">The height of one row, in pixels.</param>
	/// <param name="count">How many items there are.</param>
	/// <param name="wrap">Whether the items repeat.</param>
	/// <returns>The item it settles on.</returns>
	public int Release(float velocityY, float rowHeight, int count, bool wrap)
	{
		IsDragging = false;
		float rowsPerSecond = -velocityY / MathF.Max(rowHeight, 1f);
		Target = ResolveTarget(Position, rowsPerSecond, count, wrap);
		spring.Target = Target;
		spring.SetVelocity(rowsPerSecond);
		return WrapIndex(Target, count);
	}

	/// <summary>Moves the selection by whole rows, as the mouse wheel or a click on a neighbouring row does.</summary>
	/// <param name="rows">How many rows to move, positive towards later items.</param>
	/// <param name="count">How many items there are.</param>
	/// <param name="wrap">Whether the items repeat.</param>
	/// <returns>The item it settles on.</returns>
	public int Step(int rows, int count, bool wrap)
	{
		IsDragging = false;
		Target = wrap ? Target + rows : ClampIndex(Target + rows, count);
		spring.Target = Target;
		return WrapIndex(Target, count);
	}

	/// <summary>Sets the wheel turning to an item chosen from outside, by the shorter way round on a wheel that wraps.</summary>
	/// <param name="index">The item.</param>
	/// <param name="count">How many items there are.</param>
	/// <param name="wrap">Whether the items repeat.</param>
	public void GoTo(int index, int count, bool wrap)
	{
		IsDragging = false;
		Target = wrap ? NearestEquivalent(Target, index, count) : ClampIndex(index, count);
		spring.Target = Target;
	}

	/// <summary>Advances the settling animation. Does nothing while a drag is under way.</summary>
	/// <param name="deltaTime">Seconds since the last frame.</param>
	public void Update(float deltaTime)
	{
		if (!IsDragging)
		{
			_ = spring.Update(deltaTime);
		}
	}

	/// <summary>Decides which row a released wheel settles on.</summary>
	/// <remarks>
	/// The wheel looks ahead to where the fling would coast to and takes the nearest row there, so a hard
	/// flick spins through many items and a gentle one moves a few. Unlike a carousel there is no limit on
	/// how far one fling goes, since the items on a wheel are values to scroll past rather than pages to
	/// read.
	/// </remarks>
	/// <param name="position">The position at release, in rows.</param>
	/// <param name="rowsPerSecond">The speed at release, in rows per second, positive towards later items.</param>
	/// <param name="count">How many items there are.</param>
	/// <param name="wrap">Whether the items repeat.</param>
	/// <returns>The row to settle on, which lies outside the items only on a wheel that wraps.</returns>
	internal static int ResolveTarget(float position, float rowsPerSecond, int count, bool wrap)
	{
		float projected = position + (rowsPerSecond / Friction);
		int row = (int)MathF.Round(projected, MidpointRounding.AwayFromZero);
		return wrap ? row : ClampIndex(row, count);
	}

	/// <summary>Turns a row on a wheel that may wrap into the item it shows.</summary>
	/// <param name="row">The row.</param>
	/// <param name="count">How many items there are.</param>
	/// <returns>The item's index, from zero to one less than <paramref name="count"/>.</returns>
	internal static int WrapIndex(int row, int count) => count <= 0 ? 0 : ((row % count) + count) % count;

	/// <summary>Finds the row nearest <paramref name="current"/> that shows <paramref name="index"/>.</summary>
	/// <param name="current">The row the wheel is on.</param>
	/// <param name="index">The item wanted.</param>
	/// <param name="count">How many items there are.</param>
	/// <returns>The row.</returns>
	internal static int NearestEquivalent(int current, int index, int count)
	{
		if (count <= 0)
		{
			return 0;
		}

		int difference = WrapIndex(index - current, count);
		return difference > count / 2 ? current + difference - count : current + difference;
	}

	private static int ClampIndex(int index, int count) => count <= 0 ? 0 : Math.Clamp(index, 0, count - 1);
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>The heights a bottom sheet rests at.</summary>
public enum BottomSheetDetent
{
	/// <summary>A strip showing the top of the sheet.</summary>
	Peek,

	/// <summary>Half the window.</summary>
	Half,

	/// <summary>Nearly the whole window.</summary>
	Full,
}

/// <summary>The three heights a bottom sheet can rest at, in pixels.</summary>
/// <param name="Peek">The peek height.</param>
/// <param name="Half">The half height.</param>
/// <param name="Full">The full height.</param>
internal readonly record struct BottomSheetHeights(float Peek, float Half, float Full)
{
	/// <summary>Gets the height of a detent.</summary>
	/// <param name="detent">The detent.</param>
	/// <returns>Its height, in pixels.</returns>
	public float Of(BottomSheetDetent detent) => detent switch
	{
		BottomSheetDetent.Peek => Peek,
		BottomSheetDetent.Half => Half,
		_ => Full,
	};
}

/// <summary>
/// A bottom sheet's height and where it is heading, with no ImGui calls, so the snap rules can be
/// tested on their own.
/// </summary>
/// <remarks>
/// A drag moves the sheet with the pointer. Letting go throws it: the height it would reach a little
/// later at the speed it was moving picks the nearest detent, so a flick upwards from peek can carry it
/// past half, and a flick downwards from below half of peek dismisses it.
/// </remarks>
internal sealed class BottomSheetState
{
	/// <summary>How far ahead a release looks, in seconds, when it projects where the sheet is heading.</summary>
	internal const float Projection = 0.15f;

	private const float Stiffness = 320f;

	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.5f };

	/// <summary>Gets the detent the sheet is resting at or heading for, or null when it is closed or closing.</summary>
	public BottomSheetDetent? Detent { get; private set; }

	/// <summary>Gets a value indicating whether the sheet is open.</summary>
	public bool IsOpen => Detent is not null;

	/// <summary>Gets a value indicating whether the sheet is being dragged.</summary>
	public bool IsDragging { get; private set; }

	/// <summary>Gets how much of the sheet is showing, in pixels.</summary>
	public float Height => MathF.Max(0f, spring.Value);

	/// <summary>Gets a value indicating whether any of the sheet is on screen.</summary>
	public bool IsVisible => IsOpen || Height > 0f;

	/// <summary>Picks the detent a release heading for a height lands on.</summary>
	/// <param name="height">The projected height, in pixels.</param>
	/// <param name="heights">The detents' heights.</param>
	/// <returns>The nearest detent, or null when the height is below half of peek, which dismisses the sheet.</returns>
	internal static BottomSheetDetent? Resolve(float height, BottomSheetHeights heights)
	{
		if (height < heights.Peek * 0.5f)
		{
			return null;
		}

		BottomSheetDetent best = BottomSheetDetent.Peek;
		foreach (BottomSheetDetent detent in (ReadOnlySpan<BottomSheetDetent>)[BottomSheetDetent.Half, BottomSheetDetent.Full])
		{
			if (MathF.Abs(heights.Of(detent) - height) < MathF.Abs(heights.Of(best) - height))
			{
				best = detent;
			}
		}

		return best;
	}

	/// <summary>Opens the sheet, or moves an open one, to a detent.</summary>
	/// <param name="detent">Where it rests.</param>
	public void Open(BottomSheetDetent detent) => Detent = detent;

	/// <summary>Sends the sheet down and away.</summary>
	public void Close()
	{
		Detent = null;
		IsDragging = false;
	}

	/// <summary>Starts a drag where the sheet is.</summary>
	public void BeginDrag() => IsDragging = true;

	/// <summary>Moves the sheet with the pointer.</summary>
	/// <param name="deltaY">How far the pointer moved down, in pixels.</param>
	/// <param name="heights">The detents' heights; the sheet stops at full.</param>
	public void Drag(float deltaY, BottomSheetHeights heights)
	{
		if (IsDragging)
		{
			spring.SnapTo(Math.Clamp(Height - deltaY, 0f, heights.Full));
		}
	}

	/// <summary>Lets go of the sheet.</summary>
	/// <param name="velocityY">How fast the pointer was moving down, in pixels a second.</param>
	/// <param name="heights">The detents' heights.</param>
	public void Release(float velocityY, BottomSheetHeights heights)
	{
		if (!IsDragging)
		{
			return;
		}

		IsDragging = false;
		Detent = Resolve(Height - (velocityY * Projection), heights);
		spring.SetVelocity(-velocityY);
	}

	/// <summary>Moves the sheet towards its detent.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <param name="heights">The detents' heights, which follow the window's size.</param>
	/// <returns>True on the update the sheet finishes going away.</returns>
	public bool Update(float deltaTime, BottomSheetHeights heights)
	{
		if (IsDragging)
		{
			return false;
		}

		bool wasVisible = Height > 0f;
		spring.Target = Detent is { } detent ? heights.Of(detent) : 0f;
		spring.Update(deltaTime);
		if (Detent is null && spring.Value <= 0.5f)
		{
			spring.SnapTo(0f);
			return wasVisible;
		}

		return false;
	}
}

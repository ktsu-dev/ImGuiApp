// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>Which side of a swipeable row an action sits on.</summary>
public enum SwipeEdge
{
	/// <summary>The left side, revealed by dragging the row to the right.</summary>
	Leading,

	/// <summary>The right side, revealed by dragging the row to the left.</summary>
	Trailing,
}

/// <summary>An action a swipeable row reveals when it is dragged aside.</summary>
/// <param name="Label">What the action's button says. Also its probe name, under the row's id.</param>
/// <param name="Destructive">True for an action that deletes or discards, which is drawn in the theme's error color.</param>
public readonly record struct SwipeAction(string Label, bool Destructive = false);

/// <summary>The action a swipeable row reported this frame.</summary>
/// <param name="Edge">The side the action is on.</param>
/// <param name="Index">Its position in that side's list, counting from the outer edge inward.</param>
public readonly record struct SwipeActionResult(SwipeEdge Edge, int Index);

/// <summary>How a swipeable row behaves.</summary>
public sealed record SwipeableListItemOptions
{
	/// <summary>
	/// Gets a value indicating whether dragging the row most of the way across commits the outermost
	/// action without a second tap, the way a mail list deletes on a long swipe. On by default.
	/// </summary>
	public bool FullSwipe { get; init; } = true;

	/// <summary>Gets the fraction of the row's width a drag has to cover to commit a full swipe.</summary>
	public float FullSwipeFraction { get; init; } = 0.6f;

	/// <summary>Gets the width of one action button at the reference font size, before interface scaling. A wider label widens its button.</summary>
	public float ActionWidth { get; init; } = 72f;
}

/// <summary>
/// The geometry a release is judged against: how far each side's actions reach, and how wide the row is.
/// </summary>
/// <param name="LeadingWidth">Total width of the leading actions, zero when there are none.</param>
/// <param name="TrailingWidth">Total width of the trailing actions, zero when there are none.</param>
/// <param name="RowWidth">Width of the row.</param>
/// <param name="FullSwipe">Whether a long drag commits the outermost action.</param>
/// <param name="FullSwipeFraction">The fraction of the row a full swipe has to cover.</param>
internal readonly record struct SwipeLayout(float LeadingWidth, float TrailingWidth, float RowWidth, bool FullSwipe, float FullSwipeFraction);

/// <summary>Where a released row goes, and the action a full swipe committed, if any.</summary>
/// <param name="Target">The offset the row settles at: zero, the leading width, or minus the trailing width.</param>
/// <param name="Committed">The side whose outermost action a full swipe committed, or null.</param>
internal readonly record struct SwipeRelease(float Target, SwipeEdge? Committed);

/// <summary>
/// One swipeable row's offset and the rules that move it, with no ImGui calls, so the snapping and
/// committing can be tested on their own.
/// </summary>
/// <remarks>
/// A positive offset reveals the leading actions and a negative one the trailing actions. While a
/// drag is under way the offset follows the pointer exactly; once it ends a critically damped spring
/// carries it to wherever <see cref="ResolveRelease"/> decided.
/// </remarks>
internal sealed class SwipeableListItemState
{
	/// <summary>Release speed, in pixels per second, past which a flick opens or closes the row whatever its offset.</summary>
	internal const float FlingVelocity = 600f;

	private const float Stiffness = 400f;

	// Half a pixel is close enough to rest: the row settles exactly on its target from there, so a
	// closed row draws no sliver of its actions while the spring's tail decays.
	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.5f };
	private float pressOffset;

	/// <summary>Gets the row's current offset in pixels.</summary>
	public float Offset => spring.Value;

	/// <summary>Gets the offset the row is settling towards.</summary>
	public float Target => spring.Target;

	/// <summary>Gets a value indicating whether a drag is under way.</summary>
	public bool IsDragging { get; private set; }

	/// <summary>Gets a value indicating whether the row is open, or opening, on either side.</summary>
	public bool IsOpen => IsDragging ? Offset != 0f : spring.Target != 0f;

	/// <summary>Starts a drag from wherever the row is now.</summary>
	public void BeginDrag()
	{
		pressOffset = Offset;
		IsDragging = true;
	}

	/// <summary>Moves the row with the pointer.</summary>
	/// <param name="deltaX">How far the pointer has moved since the drag began.</param>
	/// <param name="layout">The row's geometry.</param>
	public void Drag(float deltaX, SwipeLayout layout) => spring.SnapTo(ClampOffset(pressOffset + deltaX, layout));

	/// <summary>Ends a drag and sets the row settling.</summary>
	/// <param name="velocityX">The pointer's horizontal speed at release, in pixels per second.</param>
	/// <param name="layout">The row's geometry.</param>
	/// <returns>The side whose outermost action a full swipe committed, or null.</returns>
	public SwipeEdge? Release(float velocityX, SwipeLayout layout)
	{
		IsDragging = false;
		SwipeRelease release = ResolveRelease(Offset, velocityX, layout);
		spring.Target = release.Target;
		return release.Committed;
	}

	/// <summary>Sets the row settling closed.</summary>
	public void Close()
	{
		IsDragging = false;
		spring.Target = 0f;
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

	/// <summary>
	/// Keeps an offset within what the row can show: nothing on a side with no actions, and never
	/// further than the row is wide.
	/// </summary>
	/// <param name="offset">The offset the pointer asks for.</param>
	/// <param name="layout">The row's geometry.</param>
	/// <returns>The offset the row takes.</returns>
	internal static float ClampOffset(float offset, SwipeLayout layout)
	{
		float max = layout.LeadingWidth > 0f ? layout.RowWidth : 0f;
		float min = layout.TrailingWidth > 0f ? -layout.RowWidth : 0f;
		return Math.Clamp(offset, min, max);
	}

	/// <summary>Decides where a released row settles.</summary>
	/// <remarks>
	/// A row dragged past the full-swipe fraction commits its outermost action and closes. Otherwise
	/// a flick decides by its direction, and a slow release opens the row once more than half its
	/// actions are showing.
	/// </remarks>
	/// <param name="offset">The offset at release.</param>
	/// <param name="velocityX">The pointer's horizontal speed at release, in pixels per second.</param>
	/// <param name="layout">The row's geometry.</param>
	/// <returns>Where the row settles, and what a full swipe committed.</returns>
	internal static SwipeRelease ResolveRelease(float offset, float velocityX, SwipeLayout layout)
	{
		if (offset > 0f)
		{
			return Resolve(offset, velocityX, layout.LeadingWidth, layout, SwipeEdge.Leading);
		}

		if (offset < 0f)
		{
			SwipeRelease mirrored = Resolve(-offset, -velocityX, layout.TrailingWidth, layout, SwipeEdge.Trailing);
			return mirrored with { Target = -mirrored.Target };
		}

		return new SwipeRelease(0f, null);
	}

	private static SwipeRelease Resolve(float reach, float outwardVelocity, float actionsWidth, SwipeLayout layout, SwipeEdge edge)
	{
		if (layout.FullSwipe && reach >= layout.RowWidth * layout.FullSwipeFraction)
		{
			return new SwipeRelease(0f, edge);
		}

		bool open = outwardVelocity >= FlingVelocity
			|| (outwardVelocity > -FlingVelocity && reach >= actionsWidth * 0.5f);

		return new SwipeRelease(open ? actionsWidth : 0f, null);
	}
}

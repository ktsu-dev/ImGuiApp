// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// An expandable item's open and close animation, with no ImGui calls: how tall the body is drawn on
/// each frame, given how tall its content measured.
/// </summary>
internal sealed class ExpandableState
{
	/// <summary>How long a full open or close takes, in seconds.</summary>
	internal const float Duration = 0.2f;

	/// <summary>Initializes a new instance of the <see cref="ExpandableState"/> class.</summary>
	/// <param name="open">Whether the item starts open, fully expanded.</param>
	public ExpandableState(bool open) => Transition.Snap(open);

	/// <summary>Gets the open and close timing.</summary>
	public OverlayTransition Transition { get; } = new(Duration);

	/// <summary>Gets a value indicating whether the item is open, or opening.</summary>
	public bool IsOpen => Transition.IsOpen;

	/// <summary>Gets how tall the body's content measured, the last time it was drawn.</summary>
	public float ContentHeight { get; private set; }

	/// <summary>Gets a value indicating whether the body is on screen at all.</summary>
	public bool IsBodyVisible => Transition.IsVisible;

	/// <summary>Gets a value indicating whether the body is growing or shrinking.</summary>
	public bool IsAnimating => Transition.IsAnimating;

	/// <summary>Gets how tall to draw the body, from zero when closed to the content's height when open.</summary>
	public float Height => Transition.Eased * ContentHeight;

	/// <summary>Opens or closes the item, animating from wherever it is.</summary>
	/// <param name="open">Whether the item should end up open.</param>
	public void SetOpen(bool open)
	{
		if (open)
		{
			Transition.Open();
		}
		else
		{
			Transition.Close();
		}
	}

	/// <summary>Records how tall the body's content is.</summary>
	/// <param name="height">The height the content took up, in pixels.</param>
	public void Measure(float height) => ContentHeight = Math.Max(0f, height);

	/// <summary>Advances the animation.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	public void Update(float deltaTime) => Transition.Update(deltaTime);
}

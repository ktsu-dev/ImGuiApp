// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>
/// How far a navigation drawer is open and where it is heading, with no ImGui calls, so the edge zone
/// and the snap rules can be tested on their own.
/// </summary>
internal sealed class NavigationDrawerState
{
	/// <summary>How far ahead a release looks, in seconds, when it projects where the drawer is heading.</summary>
	internal const float Projection = 0.15f;

	private const float Stiffness = 320f;

	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.001f };

	/// <summary>Gets a value indicating whether the drawer is open, or opening.</summary>
	public bool IsOpen { get; private set; }

	/// <summary>Gets a value indicating whether the drawer is being dragged.</summary>
	public bool IsDragging { get; private set; }

	/// <summary>Gets how far the drawer is open, from 0 (shut) to 1 (fully open).</summary>
	public float Openness => Math.Clamp(spring.Value, 0f, 1f);

	/// <summary>Gets a value indicating whether any of the drawer is on screen.</summary>
	public bool IsVisible => IsOpen || IsDragging || Openness > 0f;

	/// <summary>Reports whether a press lands where it can pull the drawer out.</summary>
	/// <param name="x">Where the press is, across.</param>
	/// <param name="edge">Where the window's leading edge is.</param>
	/// <param name="zone">How wide the strip along the edge is.</param>
	/// <returns>True when the press is inside the strip.</returns>
	internal static bool InEdgeZone(float x, float edge, float zone) => x >= edge && x <= edge + zone;

	/// <summary>Starts the drawer opening.</summary>
	public void Open()
	{
		IsOpen = true;
		IsDragging = false;
	}

	/// <summary>Starts the drawer closing.</summary>
	public void Close()
	{
		IsOpen = false;
		IsDragging = false;
	}

	/// <summary>Starts a drag where the drawer is.</summary>
	public void BeginDrag() => IsDragging = true;

	/// <summary>Moves the drawer with the pointer.</summary>
	/// <param name="deltaX">How far the pointer moved towards the trailing edge, in pixels.</param>
	/// <param name="width">The drawer's width, in pixels.</param>
	public void Drag(float deltaX, float width)
	{
		if (IsDragging && width > 0f)
		{
			spring.SnapTo(Math.Clamp(Openness + (deltaX / width), 0f, 1f));
		}
	}

	/// <summary>Lets go of the drawer, which then springs fully open or shut.</summary>
	/// <param name="velocityX">How fast the pointer was moving towards the trailing edge, in pixels a second.</param>
	/// <param name="width">The drawer's width, in pixels.</param>
	public void Release(float velocityX, float width)
	{
		if (!IsDragging)
		{
			return;
		}

		IsDragging = false;
		float velocity = width > 0f ? velocityX / width : 0f;
		IsOpen = Openness + (velocity * Projection) >= 0.5f;
		spring.SetVelocity(velocity);
	}

	/// <summary>Moves the drawer towards open or shut.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <returns>True on the update the drawer finishes closing.</returns>
	public bool Update(float deltaTime)
	{
		if (IsDragging)
		{
			return false;
		}

		bool wasVisible = spring.Value > 0f;
		spring.Target = IsOpen ? 1f : 0f;
		spring.Update(deltaTime);
		if (!IsOpen && spring.Value <= 0.001f)
		{
			spring.SnapTo(0f);
			return wasVisible;
		}

		return false;
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Overlays;

using System;

using ktsu.ImGui.Probes;

/// <summary>
/// How far an overlay has come in or gone out, with no ImGui calls: the open and close timing every
/// sliding overlay shares, so a sheet, a drawer and a search surface all move at the same pace.
/// </summary>
/// <param name="duration">How long a full open or close takes, in seconds.</param>
internal sealed class OverlayTransition(float duration = OverlayTransition.DefaultDuration)
{
	/// <summary>How long a full open or close takes by default, in seconds.</summary>
	internal const float DefaultDuration = 0.22f;

	/// <summary>Gets a value indicating whether the overlay is open, or opening.</summary>
	public bool IsOpen { get; private set; }

	/// <summary>Gets how far the overlay has come in, from 0 (gone) to 1 (fully in), before easing.</summary>
	public float Progress { get; private set; }

	/// <summary>Gets <see cref="Progress"/> eased so the motion slows as it arrives.</summary>
	public float Eased => Animation.Easing.OutCubic(Progress);

	/// <summary>Gets a value indicating whether anything of the overlay is on screen.</summary>
	public bool IsVisible => IsOpen || Progress > 0f;

	/// <summary>Gets a value indicating whether the overlay is moving.</summary>
	public bool IsAnimating => IsOpen ? Progress < 1f : Progress > 0f;

	/// <summary>Starts the overlay coming in, from wherever it is.</summary>
	public void Open() => IsOpen = true;

	/// <summary>Starts the overlay going out, from wherever it is.</summary>
	public void Close() => IsOpen = false;

	/// <summary>Puts the overlay fully in or fully out at once.</summary>
	/// <param name="open">Whether the overlay ends up open.</param>
	public void Snap(bool open)
	{
		IsOpen = open;
		Progress = open ? 1f : 0f;
	}

	/// <summary>Advances the transition.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <returns>True on the update the overlay finishes going out.</returns>
	public bool Update(float deltaTime)
	{
		if (!IsAnimating)
		{
			return false;
		}

		float step = duration <= 0f ? 1f : Math.Max(deltaTime, 0f) / duration;
		Progress = IsOpen ? Math.Min(1f, Progress + step) : Math.Max(0f, Progress - step);
		if (IsAnimating)
		{
			AnimationActivity.RequestActiveFrame();
			return false;
		}

		return !IsOpen;
	}
}

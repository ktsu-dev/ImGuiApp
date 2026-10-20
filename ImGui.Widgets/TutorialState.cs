// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// A tutorial's steps and its spotlight, with no ImGui calls: which step is showing, how a tutorial
/// ends, and the spotlight gliding from one target to the next.
/// </summary>
internal sealed class TutorialState
{
	// How quickly the spotlight closes on a new target, per second; it covers 95% of the way in about 0.17 s.
	private const float SpotlightRate = 18f;

	/// <summary>Gets the fade in and out.</summary>
	public OverlayTransition Transition { get; } = new(0.18f);

	/// <summary>Gets how many steps the tutorial has.</summary>
	public int StepCount { get; private set; }

	/// <summary>Gets the step showing, or -1 before the tutorial has started.</summary>
	public int Index { get; private set; } = -1;

	/// <summary>Gets a value indicating whether the tutorial is running.</summary>
	public bool IsActive => Transition.IsOpen;

	/// <summary>Gets a value indicating whether the step showing is the last.</summary>
	public bool IsLastStep => Index == StepCount - 1;

	/// <summary>Gets a value indicating whether the spotlight is on a target.</summary>
	public bool HasSpotlight { get; private set; }

	/// <summary>Gets the spotlight's top left corner.</summary>
	public Vector2 SpotlightMin { get; private set; }

	/// <summary>Gets the spotlight's bottom right corner.</summary>
	public Vector2 SpotlightMax { get; private set; }

	/// <summary>Starts the tutorial.</summary>
	/// <param name="stepCount">How many steps there are.</param>
	/// <param name="index">The step to start at.</param>
	/// <returns>True when it started; false when there are no steps.</returns>
	public bool Start(int stepCount, int index = 0)
	{
		if (stepCount <= 0)
		{
			return false;
		}

		StepCount = stepCount;
		Index = Math.Clamp(index, 0, stepCount - 1);
		HasSpotlight = false;
		Transition.Open();
		return true;
	}

	/// <summary>Moves on a step, or ends the tutorial after the last.</summary>
	/// <returns>True when this ended the tutorial.</returns>
	public bool Next()
	{
		if (!IsActive)
		{
			return false;
		}

		if (IsLastStep)
		{
			Transition.Close();
			return true;
		}

		Index++;
		return false;
	}

	/// <summary>Goes back a step, if there is one.</summary>
	/// <returns>True when it moved.</returns>
	public bool Back()
	{
		if (!IsActive || Index <= 0)
		{
			return false;
		}

		Index--;
		return true;
	}

	/// <summary>Ends the tutorial before its last step.</summary>
	/// <returns>True when the tutorial was running.</returns>
	public bool Skip()
	{
		if (!IsActive)
		{
			return false;
		}

		Transition.Close();
		return true;
	}

	/// <summary>Moves the spotlight towards the current step's target.</summary>
	/// <param name="target">The target's corners, or null when the step's target was not drawn.</param>
	/// <param name="deltaTime">Seconds since the last update.</param>
	public void TrackSpotlight((Vector2 Min, Vector2 Max)? target, float deltaTime)
	{
		if (target is not { } rect)
		{
			HasSpotlight = false;
			return;
		}

		// The first target is lit where it is; later ones are reached by gliding there.
		if (!HasSpotlight)
		{
			HasSpotlight = true;
			SpotlightMin = rect.Min;
			SpotlightMax = rect.Max;
			return;
		}

		float t = 1f - MathF.Exp(-SpotlightRate * Math.Max(deltaTime, 0f));
		SpotlightMin = Vector2.Lerp(SpotlightMin, rect.Min, t);
		SpotlightMax = Vector2.Lerp(SpotlightMax, rect.Max, t);
		if (Vector2.DistanceSquared(SpotlightMin, rect.Min) + Vector2.DistanceSquared(SpotlightMax, rect.Max) < 0.25f)
		{
			SpotlightMin = rect.Min;
			SpotlightMax = rect.Max;
		}
		else
		{
			Probes.AnimationActivity.RequestActiveFrame();
		}
	}

	/// <summary>Advances the fade.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <returns>True on the update the tutorial finishes fading out.</returns>
	public bool Update(float deltaTime) => Transition.Update(deltaTime);
}

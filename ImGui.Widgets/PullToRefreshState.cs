// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Animation;

/// <summary>Where a pull-to-refresh region is in its cycle.</summary>
internal enum PullPhase
{
	/// <summary>At rest, or springing back to rest.</summary>
	Idle,

	/// <summary>Being pulled down, not yet far enough to refresh.</summary>
	Pulling,

	/// <summary>Pulled far enough that letting go refreshes.</summary>
	Armed,

	/// <summary>Released while armed, or refreshing at the caller's say: held open with the spinner turning.</summary>
	Refreshing,
}

/// <summary>
/// A pull-to-refresh region's pull distance and phase, with no ImGui calls, so the threshold and the
/// hand-off to the caller can be tested on their own.
/// </summary>
/// <remarks>
/// The region opens by less than the pointer moves, and by less and less the further it goes, so a pull
/// feels like stretching something rather than dragging it. Letting go past the threshold triggers a
/// refresh and holds the region open at the threshold, where the spinner sits, until the caller reports
/// the refresh has finished; letting go short of it springs back.
/// </remarks>
/// <param name="threshold">How far the region must open, in pixels, before letting go refreshes.</param>
internal sealed class PullToRefreshState(float threshold)
{
	/// <summary>
	/// How long a triggered refresh holds open waiting for the caller to report it has started, in
	/// seconds, before deciding it never will and springing back.
	/// </summary>
	internal const float StartGrace = 0.25f;

	private const float Stiffness = 250f;

	private readonly Spring spring = new(0f, Stiffness, 2f * MathF.Sqrt(Stiffness)) { RestThreshold = 0.5f };
	private float waited;
	private bool sawRefreshing;

	/// <summary>Gets how far the region is open, in pixels.</summary>
	public float Offset => spring.Value;

	/// <summary>Gets where the region is in its cycle.</summary>
	public PullPhase Phase { get; private set; }

	/// <summary>Gets how far the region must open before letting go refreshes, in pixels.</summary>
	public float Threshold { get; set; } = threshold;

	/// <summary>Gets how far the region opens for a pull of so many pixels.</summary>
	/// <remarks>
	/// Half the pull at first, flattening towards twice the threshold however far the pointer goes:
	/// <c>2t · d / (d + 4t)</c>, which reaches the threshold when the pointer has moved four times it.
	/// </remarks>
	/// <param name="distance">How far the pointer has moved down since the pull began, in pixels.</param>
	/// <param name="threshold">The refresh threshold, in pixels.</param>
	/// <returns>How far the region opens, in pixels.</returns>
	internal static float Resist(float distance, float threshold)
	{
		if (distance <= 0f || threshold <= 0f)
		{
			return 0f;
		}

		return 2f * threshold * distance / (distance + (4f * threshold));
	}

	/// <summary>Follows the pointer during a pull.</summary>
	/// <param name="distance">How far the pointer has moved down since the pull began, in pixels.</param>
	public void Pull(float distance)
	{
		if (Phase == PullPhase.Refreshing)
		{
			return;
		}

		spring.SnapTo(Resist(distance, Threshold));
		Phase = Offset >= Threshold ? PullPhase.Armed : PullPhase.Pulling;
	}

	/// <summary>Ends a pull.</summary>
	/// <returns><see langword="true"/> when the pull was far enough to refresh.</returns>
	public bool Release()
	{
		if (Phase == PullPhase.Armed)
		{
			StartRefreshing();
			return true;
		}

		if (Phase == PullPhase.Pulling)
		{
			Phase = PullPhase.Idle;
			spring.Target = 0f;
		}

		return false;
	}

	/// <summary>Advances the springs and follows the caller's report of whether a refresh is under way.</summary>
	/// <param name="deltaTime">Seconds since the last frame.</param>
	/// <param name="isRefreshing">Whether the caller says a refresh is under way.</param>
	public void Update(float deltaTime, bool isRefreshing)
	{
		if (isRefreshing && Phase is PullPhase.Idle)
		{
			// A refresh the caller started itself opens the region to show the spinner.
			StartRefreshing();
		}

		if (Phase == PullPhase.Refreshing)
		{
			sawRefreshing |= isRefreshing;
			waited += deltaTime;
			if (!isRefreshing && (sawRefreshing || waited >= StartGrace))
			{
				Phase = PullPhase.Idle;
				spring.Target = 0f;
			}
		}

		if (Phase is PullPhase.Idle or PullPhase.Refreshing)
		{
			_ = spring.Update(deltaTime);
		}
	}

	private void StartRefreshing()
	{
		Phase = PullPhase.Refreshing;
		waited = 0f;
		sawRefreshing = false;
		spring.Target = Threshold;
	}
}

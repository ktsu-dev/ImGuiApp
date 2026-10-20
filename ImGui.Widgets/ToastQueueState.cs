// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

using ktsu.ImGui.Probes;

/// <summary>Where the toast on screen is in its life.</summary>
internal enum ToastPhase
{
	/// <summary>No toast is on screen.</summary>
	Hidden,

	/// <summary>Sliding in.</summary>
	Entering,

	/// <summary>Fully in, counting down to leaving.</summary>
	Showing,

	/// <summary>Sliding out.</summary>
	Leaving,
}

/// <summary>One message waiting to be shown, or being shown.</summary>
/// <param name="Message">The text.</param>
/// <param name="Options">How long it stays and what its button does.</param>
internal sealed record ToastRequest(string Message, ToastOptions Options);

/// <summary>
/// The toast queue's timing, with no ImGui calls: one toast at a time, each sliding in, staying for its
/// duration and sliding out before the next one starts, so a burst of messages is read in order rather
/// than each replacing the last.
/// </summary>
internal sealed class ToastQueueState
{
	/// <summary>How long a toast takes to slide in, in seconds.</summary>
	internal const float EnterTime = 0.18f;

	/// <summary>How long a toast takes to slide out, in seconds.</summary>
	internal const float LeaveTime = 0.18f;

	private readonly Queue<ToastRequest> pending = new();

	/// <summary>Gets the toast on screen, or null.</summary>
	public ToastRequest? Current { get; private set; }

	/// <summary>Gets where the toast on screen is in its life.</summary>
	public ToastPhase Phase { get; private set; }

	/// <summary>Gets the seconds spent in the current phase.</summary>
	public float Elapsed { get; private set; }

	/// <summary>Gets how many toasts are on screen or waiting.</summary>
	public int Count => pending.Count + (Current is null ? 0 : 1);

	/// <summary>Gets how far the toast on screen has come in, from 0 to 1.</summary>
	public float Visibility => Phase switch
	{
		ToastPhase.Entering => Math.Clamp(Elapsed / EnterTime, 0f, 1f),
		ToastPhase.Showing => 1f,
		ToastPhase.Leaving => Math.Clamp(1f - (Elapsed / LeaveTime), 0f, 1f),
		_ => 0f,
	};

	/// <summary>Adds a toast to the end of the queue.</summary>
	/// <param name="request">The toast.</param>
	public void Enqueue(ToastRequest request) => pending.Enqueue(request);

	/// <summary>Sends the toast on screen out early, from wherever it is.</summary>
	public void Dismiss()
	{
		if (Phase is ToastPhase.Entering or ToastPhase.Showing)
		{
			// Start the slide out from where the slide in had got to, so nothing jumps.
			Elapsed = (1f - Visibility) * LeaveTime;
			Phase = ToastPhase.Leaving;
		}
	}

	/// <summary>Drops every waiting toast and sends the one on screen out.</summary>
	public void Clear()
	{
		pending.Clear();
		Dismiss();
	}

	/// <summary>Advances the queue.</summary>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <param name="holding">
	/// Whether the pointer is resting on the toast, which pauses its countdown so it can be read or
	/// its button reached.
	/// </param>
	public void Update(float deltaTime, bool holding)
	{
		if (Current is null)
		{
			if (pending.Count == 0)
			{
				return;
			}

			Current = pending.Dequeue();
			Phase = ToastPhase.Entering;
			Elapsed = 0f;
		}

		if (Phase != ToastPhase.Showing || !holding)
		{
			Elapsed += Math.Max(deltaTime, 0f);
		}

		Advance();

		if (Phase is ToastPhase.Entering or ToastPhase.Leaving)
		{
			AnimationActivity.RequestActiveFrame();
		}
	}

	private void Advance()
	{
		if (Phase == ToastPhase.Entering && Elapsed >= EnterTime)
		{
			Elapsed -= EnterTime;
			Phase = ToastPhase.Showing;
		}

		// A duration of zero or less stays until it is dismissed.
		float duration = Current!.Options.Duration;
		if (Phase == ToastPhase.Showing && duration > 0f && Elapsed >= duration)
		{
			Elapsed -= duration;
			Phase = ToastPhase.Leaving;
		}

		if (Phase == ToastPhase.Leaving && Elapsed >= LeaveTime)
		{
			Current = null;
			Phase = ToastPhase.Hidden;
			Elapsed = 0f;
		}
	}
}

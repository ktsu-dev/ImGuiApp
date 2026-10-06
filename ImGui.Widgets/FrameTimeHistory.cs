// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// A fixed-capacity ring buffer of frame times in milliseconds, oldest first. Not thread-safe; feed
/// it from the render thread.
/// </summary>
/// <remarks>
/// Owned by the caller and fed once per frame, typically with
/// <c>history.AddDeltaSeconds(ImGui.GetIO().DeltaTime)</c>, then handed to
/// <see cref="ImGuiWidgets.FrameTimeGraph(string, FrameTimeHistory, FrameTimeGraphOptions?)"/>. The
/// statistics are computed the same way the graph computes them over a span, so the two agree.
/// Has no ImGui dependency.
/// </remarks>
public sealed class FrameTimeHistory
{
	private readonly float[] frames;
	private int start;

	/// <summary>Creates an empty history.</summary>
	/// <param name="capacity">The maximum number of frames kept.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
	public FrameTimeHistory(int capacity = 240)
	{
		if (capacity < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A frame-time history must hold at least one frame.");
		}

		frames = new float[capacity];
	}

	/// <summary>Gets the maximum number of frames kept.</summary>
	public int Capacity => frames.Length;

	/// <summary>Gets the number of frames currently held, from 0 to <see cref="Capacity"/>.</summary>
	public int Count { get; private set; }

	/// <summary>Gets the newest frame time, or 0 when empty.</summary>
	public float Latest => Count == 0 ? 0.0f : this[Count - 1];

	/// <summary>Gets the mean frame time, or 0 when empty.</summary>
	public float Average => FrameTimeGraphMath.Average(Held());

	/// <summary>Gets the shortest frame time, or 0 when empty.</summary>
	public float Minimum => FrameTimeGraphMath.Minimum(Held());

	/// <summary>Gets the longest frame time, or 0 when empty.</summary>
	public float Maximum => FrameTimeGraphMath.Maximum(Held());

	/// <summary>Gets the frame time at <paramref name="index"/>, where 0 is the oldest.</summary>
	/// <param name="index">The position, oldest first.</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside <c>[0, Count)</c>.</exception>
	public float this[int index]
	{
		get
		{
			if (index < 0 || index >= Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index), index, $"The history holds {Count} frames.");
			}

			return frames[(start + index) % frames.Length];
		}
	}

	/// <summary>
	/// Appends a frame time in milliseconds, dropping the oldest when full. <see cref="float.NaN"/>,
	/// infinities and negative values are ignored.
	/// </summary>
	/// <param name="milliseconds">The frame time.</param>
	public void Add(float milliseconds)
	{
		if (!float.IsFinite(milliseconds) || milliseconds < 0.0f)
		{
			return;
		}

		if (Count < frames.Length)
		{
			frames[(start + Count) % frames.Length] = milliseconds;
			Count++;
			return;
		}

		frames[start] = milliseconds;
		start = (start + 1) % frames.Length;
	}

	/// <summary>Appends a frame time given in seconds, such as ImGui's <c>DeltaTime</c>.</summary>
	/// <param name="seconds">The frame time in seconds.</param>
	public void AddDeltaSeconds(float seconds) => Add(seconds * 1000.0f);

	/// <summary>Removes every frame.</summary>
	public void Clear()
	{
		start = 0;
		Count = 0;
	}

	/// <summary>Copies the frames into <paramref name="destination"/>, oldest first.</summary>
	/// <param name="destination">Where to copy them; must hold at least <see cref="Count"/> values.</param>
	/// <returns><see cref="Count"/>.</returns>
	/// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <see cref="Count"/>.</exception>
	public int CopyTo(Span<float> destination)
	{
		if (destination.Length < Count)
		{
			throw new ArgumentException($"The destination holds {destination.Length} values but the history holds {Count} frames.", nameof(destination));
		}

		int firstRun = Math.Min(Count, frames.Length - start);
		frames.AsSpan(start, firstRun).CopyTo(destination);
		frames.AsSpan(0, Count - firstRun).CopyTo(destination[firstRun..]);
		return Count;
	}

	/// <summary>The nearest-rank percentile of the held frames.</summary>
	/// <param name="p">The percentile, clamped to <c>[0, 100]</c>.</param>
	/// <returns>The frame time at that rank, or 0 when empty.</returns>
	public float Percentile(float p) => FrameTimeGraphMath.Percentile(Held(), p);

	/// <summary>Counts the frames strictly longer than <paramref name="budgetMilliseconds"/>.</summary>
	/// <param name="budgetMilliseconds">The budget. Not finite and positive counts nothing.</param>
	/// <returns>How many frames went over budget.</returns>
	public int CountOver(float budgetMilliseconds) => FrameTimeGraphMath.CountOver(Held(), budgetMilliseconds);

	// Statistics are rare next to Add, so they pay for a copy rather than Add paying to keep the
	// frames contiguous.
	private float[] Held()
	{
		float[] held = new float[Count];
		CopyTo(held);
		return held;
	}
}

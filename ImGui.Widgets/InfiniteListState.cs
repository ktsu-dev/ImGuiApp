// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

/// <summary>
/// When an infinite list asks for more, with no ImGui calls. It asks once each time the scroll comes
/// within <see cref="Threshold"/> of the end, never while a load is under way or when there is nothing
/// more, and asks again only after the scroll has moved away or new items have arrived.
/// </summary>
/// <param name="threshold">How close to the end, in pixels, the scroll has to come.</param>
internal sealed class InfiniteListState(float threshold)
{
	private bool armed = true;
	private int lastCount = -1;

	/// <summary>Gets or sets how close to the end, in pixels, the scroll has to come.</summary>
	public float Threshold { get; set; } = threshold;

	/// <summary>Decides whether to ask for more on this frame.</summary>
	/// <param name="distanceToEnd">How far the scroll is from the end of the list, in pixels.</param>
	/// <param name="itemCount">How many items the list holds now.</param>
	/// <param name="isLoading">Whether a load the list asked for is still under way.</param>
	/// <param name="hasMore">Whether there is anything more to load.</param>
	/// <returns>True on the frame the list should ask for more.</returns>
	public bool Update(float distanceToEnd, int itemCount, bool isLoading, bool hasMore)
	{
		// New items are a new approach: a short list that still ends inside the view keeps filling.
		// Not on the frame they arrive, though: the scroll range is laid out a frame behind the items,
		// so that frame still reads the old end as where the scroll is.
		if (itemCount != lastCount)
		{
			lastCount = itemCount;
			armed = true;
			return false;
		}

		if (distanceToEnd > Threshold)
		{
			armed = true;
			return false;
		}

		if (!armed || isLoading || !hasMore)
		{
			return false;
		}

		armed = false;
		return true;
	}
}

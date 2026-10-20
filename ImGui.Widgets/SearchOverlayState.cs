// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// A search overlay's query and highlighted result, with no ImGui calls, so the keyboard rules can be
/// tested on their own.
/// </summary>
internal sealed class SearchOverlayState
{
	/// <summary>Gets the slide in and out.</summary>
	public OverlayTransition Transition { get; } = new();

	/// <summary>Gets a value indicating whether the overlay is open.</summary>
	public bool IsOpen => Transition.IsOpen;

	/// <summary>Gets or sets what has been typed.</summary>
	public string Query { get; set; } = string.Empty;

	/// <summary>Gets the highlighted result's position in the results, or -1 when none is.</summary>
	public int Highlighted { get; private set; } = -1;

	/// <summary>Opens the overlay with an empty query.</summary>
	public void Open()
	{
		Query = string.Empty;
		Highlighted = -1;
		Transition.Open();
	}

	/// <summary>Closes the overlay.</summary>
	public void Close() => Transition.Close();

	/// <summary>
	/// Keeps the highlight on a result after the results change: the first one once something has been
	/// typed, so Enter picks the best match, and none while the query is empty.
	/// </summary>
	/// <param name="count">How many results there are now.</param>
	public void ResultsChanged(int count)
	{
		if (count == 0)
		{
			Highlighted = -1;
		}
		else if (Highlighted < 0)
		{
			Highlighted = Query.Length > 0 ? 0 : -1;
		}
		else
		{
			Highlighted = Math.Min(Highlighted, count - 1);
		}
	}

	/// <summary>Moves the highlight up or down the results, stopping at either end.</summary>
	/// <param name="delta">How many results to move; positive moves down.</param>
	/// <param name="count">How many results there are.</param>
	public void MoveHighlight(int delta, int count)
	{
		if (count == 0)
		{
			Highlighted = -1;
			return;
		}

		Highlighted = Highlighted < 0
			? (delta > 0 ? 0 : count - 1)
			: Math.Clamp(Highlighted + delta, 0, count - 1);
	}
}

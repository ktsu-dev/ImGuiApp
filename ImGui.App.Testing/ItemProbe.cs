// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Records where named items were drawn, so tests address widgets by name rather than by position.
/// </summary>
/// <remarks>
/// An application marks items through <see cref="ImGuiApp.MarkItem"/>. This is deliberately not Dear
/// ImGui's test engine, which would address every widget by name without the application marking
/// anything, but is licensed separately from Dear ImGui and would pass a commercial obligation to
/// everyone consuming this package. See the design document for the full reasoning.
/// </remarks>
public sealed class ItemProbe
{
	private readonly Dictionary<string, (Rectangle Rect, int Frame, bool Ambiguous)> seen = [];

	/// <summary>Gets the names recorded so far, for diagnostics when a lookup fails.</summary>
	public IReadOnlyCollection<string> KnownNames => seen.Keys;

	/// <summary>Gets the most recent rectangle recorded for a name, or null when never seen.</summary>
	/// <param name="name">The item name.</param>
	/// <returns>The rectangle, or null.</returns>
	public Rectangle? Rect(string name) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame, bool Ambiguous) entry) ? entry.Rect : null;

	/// <summary>
	/// Gets a value indicating whether a name was marked more than once in the same frame, which
	/// means it does not identify one item.
	/// </summary>
	/// <param name="name">The item name.</param>
	/// <returns>True when the name is ambiguous.</returns>
	public bool IsAmbiguous(string name) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame, bool Ambiguous) entry) && entry.Ambiguous;

	/// <summary>Gets a value indicating whether a name was marked during a given frame.</summary>
	/// <param name="name">The item name.</param>
	/// <param name="frame">The frame number to check.</param>
	/// <returns>True when the item was drawn in that frame.</returns>
	public bool WasSeenInFrame(string name, int frame) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame, bool Ambiguous) entry) && entry.Frame == frame;

	internal void Record(string name, Vector2 min, Vector2 max, int frame)
	{
		Rectangle rect = new(
			(int)MathF.Round(min.X),
			(int)MathF.Round(min.Y),
			(int)MathF.Round(max.X),
			(int)MathF.Round(max.Y));

		// A name marked twice within one frame identifies two different items, which happens easily
		// once libraries mark automatically and two widgets share a label. Recording that rather than
		// letting the last one win means a click on it fails instead of hitting whichever happened to
		// be drawn second.
		bool ambiguous = seen.TryGetValue(name, out (Rectangle Rect, int Frame, bool Ambiguous) previous)
			&& previous.Frame == frame
			&& previous.Rect != rect;

		seen[name] = (rect, frame, ambiguous);
	}
}

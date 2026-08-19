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
	private readonly Dictionary<string, (Rectangle Rect, int Frame)> seen = [];

	/// <summary>Gets the names recorded so far, for diagnostics when a lookup fails.</summary>
	public IReadOnlyCollection<string> KnownNames => seen.Keys;

	/// <summary>Gets the most recent rectangle recorded for a name, or null when never seen.</summary>
	/// <param name="name">The item name.</param>
	/// <returns>The rectangle, or null.</returns>
	public Rectangle? Rect(string name) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame) entry) ? entry.Rect : null;

	/// <summary>Gets a value indicating whether a name was marked during a given frame.</summary>
	/// <param name="name">The item name.</param>
	/// <param name="frame">The frame number to check.</param>
	/// <returns>True when the item was drawn in that frame.</returns>
	public bool WasSeenInFrame(string name, int frame) =>
		seen.TryGetValue(name, out (Rectangle Rect, int Frame) entry) && entry.Frame == frame;

	internal void Record(string name, Vector2 min, Vector2 max, int frame) =>
		seen[name] = (
			new Rectangle(
				(int)MathF.Round(min.X),
				(int)MathF.Round(min.Y),
				(int)MathF.Round(max.X),
				(int)MathF.Round(max.Y)),
			frame);
}

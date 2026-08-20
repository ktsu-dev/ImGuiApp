// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// Records where named items were drawn, so tests address widgets by name rather than by position.
/// </summary>
/// <remarks>
/// <para>
/// Names are recorded fully qualified, as the ImGui window followed by any pushed scopes and then the
/// item's own name. Qualification is what keeps two identically labeled widgets apart, the same job
/// ImGui's identifier stack does for the widgets themselves.
/// </para>
/// <para>
/// Tests do not have to write the whole path. A lookup matches on trailing path segments, so
/// <c>a.png</c> finds <c>Open Image/filesystem-browser/a.png</c> as long as nothing else ends the same
/// way. A partial name matching several items is reported as ambiguous rather than resolved to
/// whichever was recorded last.
/// </para>
/// </remarks>
public sealed class ItemProbe
{
	private readonly Dictionary<string, (Rectangle Rect, int Frame, bool Duplicated)> seen = [];

	/// <summary>Gets the fully qualified names recorded so far, for diagnostics when a lookup fails.</summary>
	public IReadOnlyCollection<string> KnownNames => seen.Keys;

	/// <summary>
	/// Finds the fully qualified names matching a query, either exactly or by trailing path segments.
	/// </summary>
	/// <param name="query">A full name or the trailing part of one.</param>
	/// <returns>Every matching name. Empty when nothing matches.</returns>
	public IReadOnlyList<string> Matches(string query)
	{
		if (seen.ContainsKey(query))
		{
			return [query];
		}

		string suffix = "/" + query;
		return [.. seen.Keys.Where(k => k.EndsWith(suffix, StringComparison.Ordinal))];
	}

	/// <summary>Gets the rectangle recorded for a name, or null when nothing matches.</summary>
	/// <param name="query">A full name or the trailing part of one.</param>
	/// <returns>The rectangle, or null.</returns>
	/// <exception cref="InvalidOperationException">The query matches more than one item.</exception>
	public Rectangle? Rect(string query)
	{
		IReadOnlyList<string> matches = Matches(query);

		return matches.Count switch
		{
			0 => null,
			1 => seen[matches[0]].Rect,
			_ => throw new InvalidOperationException(
				$"'{query}' matches {matches.Count} items: {string.Join(", ", matches)}. Qualify it further."),
		};
	}

	/// <summary>
	/// Gets a value indicating whether a query does not identify exactly one item, either because it
	/// matches several names or because one name was marked twice in the same frame.
	/// </summary>
	/// <param name="query">A full name or the trailing part of one.</param>
	/// <returns>True when the query is ambiguous.</returns>
	public bool IsAmbiguous(string query)
	{
		IReadOnlyList<string> matches = Matches(query);

		return matches.Count > 1 || (matches.Count == 1 && seen[matches[0]].Duplicated);
	}

	/// <summary>Gets a value indicating whether a query resolved to an item drawn in a given frame.</summary>
	/// <param name="query">A full name or the trailing part of one.</param>
	/// <param name="frame">The frame number to check.</param>
	/// <returns>True when the item was drawn in that frame.</returns>
	public bool WasSeenInFrame(string query, int frame)
	{
		IReadOnlyList<string> matches = Matches(query);

		return matches.Count == 1 && seen[matches[0]].Frame == frame;
	}

	internal void Record(string name, Vector2 min, Vector2 max, int frame)
	{
		Rectangle rect = new(
			(int)MathF.Round(min.X),
			(int)MathF.Round(min.Y),
			(int)MathF.Round(max.X),
			(int)MathF.Round(max.Y));

		// One fully qualified name marked twice in a frame means two different items ended up with
		// identical context, which qualification could not separate. Recording that rather than
		// letting the last one win means a click on it fails instead of hitting whichever happened to
		// be drawn second.
		bool duplicated = seen.TryGetValue(name, out (Rectangle Rect, int Frame, bool Duplicated) previous)
			&& previous.Frame == frame
			&& previous.Rect != rect;

		seen[name] = (rect, frame, duplicated);
	}
}

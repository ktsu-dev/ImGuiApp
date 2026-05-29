// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Overlays;

using System;
using System.Collections.Generic;

/// <summary>
/// Z-ordered registry for retained overlays — toasts, bottom sheets, action sheets, drawers, and
/// the like — that must paint on top of the rest of a frame in a predictable order. Register an
/// overlay once with <see cref="Show"/>; it persists across frames until <see cref="Dismiss"/> or
/// <see cref="Clear"/> removes it. Call <see cref="Render"/> exactly once near the end of the
/// user's frame to invoke every registered overlay's draw callback in ascending
/// <see cref="OverlayLayer"/> order.
/// </summary>
/// <remarks>
/// <para>
/// The host is intentionally free of any Dear ImGui calls: each overlay's draw callback owns its
/// own windowing. That keeps the ordering logic unit-testable and lets higher-level widgets
/// (<c>Toast</c>, <c>BottomSheet</c>, <c>NavigationDrawer</c>) layer their own presentation on
/// top. Use <see cref="HasOverlays"/> to decide whether to keep the application's frame rate
/// elevated while overlays are on screen.
/// </para>
/// <para>
/// <see cref="Render"/> snapshots the current set before invoking any callback, so an overlay may
/// safely <see cref="Show"/> or <see cref="Dismiss"/> overlays (including itself) from inside its
/// own draw delegate. A dismissal mid-frame still leaves already-snapshotted overlays to paint
/// this frame; newly shown overlays first appear on the next frame.
/// </para>
/// </remarks>
public sealed class OverlayHost
{
	private sealed record OverlayEntry(OverlayLayer Layer, long Sequence, Action Draw);

	private readonly Dictionary<string, OverlayEntry> _overlays = [];
	private readonly List<OverlayEntry> _renderBuffer = [];
	private long _sequenceCounter;

	/// <summary>Number of overlays currently registered.</summary>
	public int Count => _overlays.Count;

	/// <summary>True while at least one overlay is registered. Gate frame-rate boosts on this flag.</summary>
	public bool HasOverlays => _overlays.Count > 0;

	/// <summary>
	/// Register (or update) an overlay under <paramref name="key"/>. Re-showing an existing key
	/// replaces its draw callback and layer while preserving the original registration order, so
	/// calling <see cref="Show"/> every frame for the same key keeps a stable z-position rather
	/// than thrashing it to the front.
	/// </summary>
	/// <param name="key">Stable identity for the overlay. Re-using a key updates in place.</param>
	/// <param name="draw">Callback invoked by <see cref="Render"/>; owns its own ImGui windowing.</param>
	/// <param name="layer">Z-order band. Overlays are drawn in ascending layer order.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="draw"/> is null.</exception>
	public void Show(string key, Action draw, OverlayLayer layer = OverlayLayer.Toast)
	{
		Ensure.NotNull(key);
		Ensure.NotNull(draw);

		_overlays[key] = _overlays.TryGetValue(key, out OverlayEntry? existing)
			? existing with { Layer = layer, Draw = draw }
			: new OverlayEntry(layer, _sequenceCounter++, draw);
	}

	/// <summary>Remove the overlay registered under <paramref name="key"/>.</summary>
	/// <param name="key">Key previously passed to <see cref="Show"/>.</param>
	/// <returns>True if an overlay was removed; false if none was registered under that key.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public bool Dismiss(string key)
	{
		Ensure.NotNull(key);
		return _overlays.Remove(key);
	}

	/// <summary>Report whether an overlay is currently registered under <paramref name="key"/>.</summary>
	/// <param name="key">Key previously passed to <see cref="Show"/>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public bool IsShown(string key)
	{
		Ensure.NotNull(key);
		return _overlays.ContainsKey(key);
	}

	/// <summary>Remove every registered overlay.</summary>
	public void Clear() => _overlays.Clear();

	/// <summary>
	/// Invoke every registered overlay's draw callback in ascending <see cref="OverlayLayer"/>
	/// order, breaking ties by registration order. Call once near the end of the frame.
	/// </summary>
	public void Render()
	{
		if (_overlays.Count == 0)
		{
			return;
		}

		// Snapshot before drawing so callbacks may mutate the registry without disturbing this frame.
		_renderBuffer.Clear();
		_renderBuffer.AddRange(_overlays.Values);
		_renderBuffer.Sort(static (a, b) =>
		{
			int byLayer = ((int)a.Layer).CompareTo((int)b.Layer);
			return byLayer != 0 ? byLayer : a.Sequence.CompareTo(b.Sequence);
		});

		foreach (OverlayEntry entry in _renderBuffer)
		{
			entry.Draw();
		}

		_renderBuffer.Clear();
	}
}

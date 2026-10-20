// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A scrolling region that refreshes when pulled down from its top, as a feed on a phone does. Draw
	/// the content inside a <c>using</c> block.
	/// </summary>
	/// <remarks>
	/// <para>
	/// With the content scrolled to the top, a drag down that starts anywhere a control does not claim it
	/// pulls the content down, opening a gap with a spinner that fills in as the pull goes on. Letting go
	/// once the spinner is whole sets <see cref="Triggered"/> for that frame; the caller starts its
	/// refresh and passes <c>isRefreshing: true</c> until it finishes, and the region holds open with the
	/// spinner turning until then. Letting go sooner springs back. Passing <c>isRefreshing: true</c>
	/// without a pull shows the spinner too, for a refresh the caller started itself.
	/// </para>
	/// <para>
	/// <see cref="Triggered"/> is known when the region begins, so a caller can act on it inside the block.
	/// The region is probe-marked under its id.
	/// </para>
	/// <code>
	/// using (ImGuiWidgets.PullToRefresh feed = new("feed", new Vector2(0f, 300f), loading))
	/// {
	///     if (feed.Triggered) { StartLoading(); }
	///     foreach (Post post in posts) { ImGui.TextUnformatted(post.Title); }
	/// }
	/// </code>
	/// </remarks>
	public sealed class PullToRefresh : IDisposable
	{
		private sealed class RegionEntry(float threshold)
		{
			public PullToRefreshState State { get; } = new(threshold);

			public bool Pressed { get; set; }

			public bool Pulling { get; set; }

			public float PressY { get; set; }

			public float ScrollY { get; set; }

			public int LastFrame { get; set; } = int.MinValue;

			public uint OuterMoveId { get; set; }

			public uint InnerMoveId { get; set; }
		}

		// How far the pointer must move down before a press becomes a pull, the same as a pan in GestureSettings.
		private const float PullStartDistance = 6f;

		// The id Dear ImGui gives a window's move handle, which it makes active when empty space is pressed.
		private const string WindowMoveId = "#MOVE";

		private static readonly Dictionary<uint, RegionEntry> Entries = [];

		private readonly RegionEntry entry;
		private bool disposed;

		/// <summary>Begins a pull-to-refresh region. Call inside a <c>using</c> block.</summary>
		/// <param name="id">A unique id for the region.</param>
		/// <param name="size">The region's size. A zero component fills the space available, as for a child window.</param>
		/// <param name="isRefreshing">Whether a refresh is under way. Pass <see langword="true"/> from the frame <see cref="Triggered"/> is set until the refresh finishes.</param>
		public PullToRefresh(string id, Vector2 size, bool isRefreshing)
		{
			Ensure.NotNull(id);
			float threshold = ImGui.GetFrameHeight() * 2.5f;
			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();

			// A region not drawn last frame starts closed, rather than finishing a pull the user never
			// saw, or one left by an earlier ImGui context.
			if (!Entries.TryGetValue(key, out RegionEntry? found) || frame - found.LastFrame != 1)
			{
				found = new RegionEntry(threshold);
				Entries[key] = found;
			}

			entry = found;
			entry.LastFrame = frame;
			entry.State.Threshold = threshold;

			// NoMove on both windows, so a pull on empty space does not drag a floating window that holds the region.
			ImGui.BeginChild(id, size, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove);
			entry.OuterMoveId = ImGui.GetID(WindowMoveId);
			Vector2 origin = ImGui.GetCursorScreenPos();
			Vector2 inner = ImGui.GetContentRegionAvail();
			Vector2 windowMin = ImGui.GetWindowPos();
			ImGuiProbes.MarkRegion(id, windowMin, windowMin + ImGui.GetWindowSize());

			Triggered = HandleInput(ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows));
			entry.State.Update(ImGui.GetIO().DeltaTime, isRefreshing);

			float offset = MathF.Max(entry.State.Offset, 0f);
			if (offset > 0.5f || entry.State.Phase == PullPhase.Refreshing)
			{
				DrawSpinner(origin, inner.X, offset, threshold);
			}

			ImGui.SetCursorScreenPos(origin + new Vector2(0f, offset));
			ImGui.BeginChild("##content", new Vector2(inner.X, MathF.Max(inner.Y - offset, 1f)), ImGuiChildFlags.None, ImGuiWindowFlags.NoMove);
			entry.InnerMoveId = ImGui.GetID(WindowMoveId);
		}

		/// <summary>Gets a value indicating whether the user let go of a pull far enough to refresh, on this frame only.</summary>
		public bool Triggered { get; }

		/// <summary>Ends the region.</summary>
		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			entry.ScrollY = ImGui.GetScrollY();
			ImGui.EndChild();
			ImGui.EndChild();
		}

		private bool HandleInput(bool hovered)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			PullToRefreshState state = entry.State;

			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && hovered && entry.ScrollY <= 0f && state.Phase != PullPhase.Refreshing)
			{
				entry.Pressed = true;
				entry.Pulling = false;
				entry.PressY = io.MousePos.Y;
				return false;
			}

			if (!entry.Pressed)
			{
				return false;
			}

			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				entry.Pressed = false;
				bool wasPulling = entry.Pulling;
				entry.Pulling = false;
				return wasPulling && state.Release();
			}

			// A press a control took for itself, such as a slider's, is the control's drag, not a pull. A
			// press on empty space makes the window under it active, which is no control.
			uint active = ImGuiP.GetActiveID();
			if (!entry.Pulling && active != 0 && active != entry.OuterMoveId && active != entry.InnerMoveId)
			{
				entry.Pressed = false;
				return false;
			}

			float distance = io.MousePos.Y - entry.PressY;
			if (!entry.Pulling && distance > PullStartDistance)
			{
				entry.Pulling = true;
			}

			if (entry.Pulling)
			{
				state.Pull(distance);
			}

			return false;
		}

		private void DrawSpinner(Vector2 origin, float width, float offset, float threshold)
		{
			PullToRefreshState state = entry.State;
			float radius = threshold * 0.2f;
			float thickness = MathF.Max(2f, radius * 0.25f);

			// The spinner rides in the middle of the gap, and never higher than its own size allows.
			Vector2 center = new(origin.X + (width * 0.5f), origin.Y + MathF.Max(offset * 0.5f, radius + thickness));
			Vector4 accent = ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark];

			float start;
			float sweep;
			float alpha;
			if (state.Phase == PullPhase.Refreshing)
			{
				// Turning while the refresh is under way, which needs every frame drawn.
				start = (float)(ImGui.GetTime() * Math.Tau * 1.2 % Math.Tau);
				sweep = MathF.Tau * 0.75f;
				alpha = 1f;
				AnimationActivity.RequestActiveFrame();
			}
			else
			{
				// Filling in as the pull nears the threshold, so the user can see when letting go will refresh.
				float progress = Math.Clamp(offset / threshold, 0f, 1f);
				start = -MathF.PI * 0.5f;
				sweep = MathF.Tau * 0.85f * progress;
				alpha = 0.35f + (0.65f * progress);
			}

			if (sweep <= 0.01f)
			{
				return;
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.PathArcTo(center, radius, start, start + sweep, 32);
			drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(accent with { W = accent.W * alpha }), ImDrawFlags.None, thickness);
		}
	}
}

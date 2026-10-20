// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>Settings for <see cref="ImGuiWidgets.InfiniteList"/>.</summary>
public sealed record InfiniteListOptions
{
	/// <summary>Gets how close to the end of the list, in unscaled pixels, the scroll has to come before more is asked for.</summary>
	public float Threshold { get; init; } = 120f;

	/// <summary>Gets the height of every item, or zero to let the list measure the first.</summary>
	public float ItemHeight { get; init; }

	/// <summary>Gets the text shown beside the spinner while a load is under way.</summary>
	public string LoadingText { get; init; } = "Loading…";

	/// <summary>Gets the text shown at the end once there is nothing more, or null to show nothing.</summary>
	public string? EndText { get; init; }
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A scrolling list that asks for more as the scroll nears its end, as a feed does. Only the items
	/// in view are drawn, so the list can grow without limit.
	/// </summary>
	/// <param name="id">A unique id for the list, also its probe name.</param>
	/// <param name="size">The list's size. A zero component fills the space available, as for a child window.</param>
	/// <param name="itemCount">How many items the list holds now.</param>
	/// <param name="drawItem">Draws one item, given its index. Each item is drawn inside its own id scope.</param>
	/// <param name="loadMore">Called once each time the scroll comes near the end; start a load, and pass <paramref name="isLoading"/> until it finishes.</param>
	/// <param name="isLoading">Whether a load is under way. A loading row is shown at the end, and nothing more is asked for, while it is.</param>
	/// <param name="hasMore">Whether there is anything more to load.</param>
	/// <param name="options">Threshold, item height and texts, or null for the defaults.</param>
	/// <remarks>
	/// More is asked for once per approach: the list asks again only after the scroll has moved away
	/// from the end, or new items have arrived. A list too short to fill its view keeps asking, one load
	/// at a time, until it does or <paramref name="hasMore"/> goes false. The loading row is marked as
	/// <c>{id}/loading</c>.
	/// </remarks>
	public static void InfiniteList(string id, Vector2 size, int itemCount, Action<int> drawItem, Action loadMore, bool isLoading, bool hasMore = true, InfiniteListOptions? options = null) =>
		InfiniteListImpl.Draw(id, size, itemCount, drawItem, loadMore, isLoading, hasMore, options ?? new InfiniteListOptions());

	internal static class InfiniteListImpl
	{
		private sealed class Entry(float threshold)
		{
			public InfiniteListState State { get; } = new(threshold);

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, Entry> Entries = [];

		internal static void Draw(string id, Vector2 size, int itemCount, Action<int> drawItem, Action loadMore, bool isLoading, bool hasMore, InfiniteListOptions options)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(drawItem);
			Ensure.NotNull(loadMore);

			float threshold = InterfaceScale.Pixels(options.Threshold);
			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();
			if (!Entries.TryGetValue(key, out Entry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new Entry(threshold);
				Entries[key] = entry;
			}

			entry.LastFrame = frame;
			entry.State.Threshold = threshold;

			if (ImGui.BeginChild(id, size, ImGuiChildFlags.None, ImGuiWindowFlags.None))
			{
				Vector2 windowMin = ImGui.GetWindowPos();
				ImGuiProbes.MarkRegion(id, windowMin, windowMin + ImGui.GetWindowSize());
				DrawItems(itemCount, drawItem, options.ItemHeight);

				if (isLoading)
				{
					DrawLoadingRow(id, options.LoadingText);
				}
				else if (!hasMore && options.EndText is not null)
				{
					ImGui.TextDisabled(options.EndText);
				}

				// The state waits out the frame the item count changes, the first included, since the
				// scroll range is laid out a frame behind the items.
				float distanceToEnd = ImGui.GetScrollMaxY() - ImGui.GetScrollY();
				if (entry.State.Update(distanceToEnd, itemCount, isLoading, hasMore))
				{
					loadMore();
				}
			}

			ImGui.EndChild();
		}

		private static void DrawItems(int itemCount, Action<int> drawItem, float itemHeight)
		{
			ImGuiListClipper clipper = default;
			TableClipping.Begin(ref clipper, Math.Max(itemCount, 0), itemHeight > 0f ? InterfaceScale.Pixels(itemHeight) : 0f, forcedRow: -1);
			while (clipper.Step())
			{
				for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
				{
					ImGui.PushID(i);
					drawItem(i);
					ImGui.PopID();
				}
			}

			clipper.End();
		}

		private static void DrawLoadingRow(string id, string text)
		{
			float height = ImGui.GetFrameHeight() * 1.5f;
			Vector2 min = ImGui.GetCursorScreenPos();
			float width = Math.Max(ImGui.GetContentRegionAvail().X, height);
			ImGui.Dummy(new Vector2(width, height));
			ImGuiProbes.MarkItem(id, "loading");

			Vector2 textSize = ImGui.CalcTextSize(text);
			float radius = ImGui.GetFontSize() * 0.45f;
			float gap = ImGui.GetStyle().ItemInnerSpacing.X;
			float left = min.X + ((width - ((radius * 2f) + gap + textSize.X)) * 0.5f);
			Vector2 center = new(left + radius, min.Y + (height * 0.5f));

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float start = (float)(ImGui.GetTime() * 6.0 % (Math.PI * 2.0));
			drawList.PathArcTo(center, radius, start, start + (MathF.PI * 1.5f), 24);
			drawList.PathStroke(ImGui.GetColorU32(ImGuiCol.CheckMark), ImDrawFlags.None, Math.Max(2f, radius * 0.25f));
			drawList.AddText(new Vector2(center.X + radius + gap, min.Y + ((height - textSize.Y) * 0.5f)), ImGui.GetColorU32(ImGuiCol.TextDisabled), text);
			AnimationActivity.RequestActiveFrame();
		}
	}
}

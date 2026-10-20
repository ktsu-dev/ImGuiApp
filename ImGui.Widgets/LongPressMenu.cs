// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Gestures;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws something that opens a menu when held down, as a long press does on a phone, or when
	/// right-clicked.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="content"/> draws the target. While the pointer is held on it the target shrinks a
	/// little, and when the hold becomes a long press it springs back with a small bump and the menu opens
	/// at the pointer. A press that moves away, or is let go too soon, opens nothing, so controls inside
	/// the target keep their own clicks. A click outside the menu, or Escape, closes it.
	/// </para>
	/// <para>
	/// The shrink is applied to what <paramref name="content"/> drew into the window's draw list, so a
	/// child window inside it does not shrink with the rest. The target is probe-marked under
	/// <paramref name="id"/> and each menu item under <c>id/label</c>.
	/// </para>
	/// </remarks>
	/// <param name="id">A unique id for the target and its menu.</param>
	/// <param name="items">The menu's items.</param>
	/// <param name="content">Draws the target.</param>
	/// <returns>The index of the item chosen on this frame, or -1.</returns>
	public static int LongPressMenu(string id, IReadOnlyList<string> items, Action content) =>
		LongPressMenuImpl.Draw(id, items, content);

	internal static class LongPressMenuImpl
	{
		private sealed class MenuEntry
		{
			public LongPressMenuState State { get; } = new();

			public GestureMachine Gestures { get; } = new();

			public bool Holding { get; set; }

			public int LastFrame { get; set; } = int.MinValue;
		}

		private const string PopupId = "##longPressMenu";

		private static readonly Dictionary<uint, MenuEntry> Entries = [];

		internal static int Draw(string id, IReadOnlyList<string> items, Action content)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(items);
			Ensure.NotNull(content);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();
			if (!Entries.TryGetValue(key, out MenuEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new MenuEntry();
				Entries[key] = entry;
			}

			entry.LastFrame = frame;

			ImGui.PushID(id);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			int firstVertex = drawList.VtxBuffer.Size;
			ImGui.BeginGroup();
			content();
			ImGui.EndGroup();
			int endVertex = drawList.VtxBuffer.Size;
			ImGuiProbes.MarkItem(id);

			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			bool hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			bool opened = HandlePress(entry, hovered, out float pressDuration);

			entry.State.Update(ImGui.GetIO().DeltaTime, entry.Holding, pressDuration, entry.Gestures.Settings.LongPressMinDuration, opened);
			ScaleVertices(drawList, firstVertex, endVertex, (min + max) * 0.5f, entry.State.Scale);

			int chosen = DrawMenu(id, items);
			ImGui.PopID();
			return chosen;
		}

		private static bool HandlePress(MenuEntry entry, bool hovered, out float pressDuration)
		{
			if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				entry.Holding = true;
			}

			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				entry.Holding = false;
			}

			GestureResult gesture = entry.Gestures.Update(entry.Holding, ImGui.GetMousePos(), ImGui.GetIO().DeltaTime);
			pressDuration = gesture.PressDuration;

			// A press that strays becomes a drag, which is not a long press.
			if ((gesture.Gestures & GestureFlags.PanStart) != 0)
			{
				entry.Holding = false;
			}

			bool opened = gesture.LongPressed || (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Right));
			if (opened)
			{
				entry.Holding = false;
				ImGui.OpenPopup(PopupId);
			}

			return opened;
		}

		private static void ScaleVertices(ImDrawListPtr drawList, int first, int end, Vector2 center, float scale)
		{
			if (MathF.Abs(scale - 1f) < 1e-4f)
			{
				return;
			}

			ImVector<ImDrawVert> vertices = drawList.VtxBuffer;
			for (int i = first; i < end; i++)
			{
				ImDrawVert vertex = vertices[i];
				vertex.Pos = center + ((vertex.Pos - center) * scale);
				vertices[i] = vertex;
			}
		}

		private static int DrawMenu(string id, IReadOnlyList<string> items)
		{
			int chosen = -1;
			if (ImGui.BeginPopup(PopupId))
			{
				for (int i = 0; i < items.Count; i++)
				{
					if (ImGui.MenuItem(items[i]))
					{
						chosen = i;
					}

					ImGuiProbes.MarkItem($"{id}/{items[i]}");
				}

				// Dear ImGui closes a popup on Escape only when keyboard navigation is on; a menu should either way.
				if (chosen < 0 && ImGui.IsKeyPressed(ImGuiKey.Escape))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}

			return chosen;
		}
	}
}

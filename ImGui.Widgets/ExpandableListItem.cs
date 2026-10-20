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
	/// A full-width row that opens and closes the content beneath it, the body growing and shrinking
	/// rather than appearing at once.
	/// </summary>
	/// <param name="label">The row's text, also its ImGui id and probe name.</param>
	/// <param name="open">Whether the item is open. A click on the row flips it.</param>
	/// <param name="body">Draws the content. Called only while any of it is on screen.</param>
	/// <returns>True on the frame a click opened or closed the item.</returns>
	/// <remarks>
	/// The body is clipped to its animated height, and measured as it is drawn, so content that
	/// changes size is followed without a call to say so, a frame later.
	/// </remarks>
	public static bool ExpandableListItem(string label, ref bool open, Action body) =>
		ExpandableImpl.Draw(label, ref open, body);

	/// <summary>
	/// A stack of <see cref="ExpandableListItem"/> rows that remembers which are open, by default
	/// letting only one be open at a time.
	/// </summary>
	/// <remarks>
	/// <code>
	/// ImGuiWidgets.Accordion faq = new();
	/// faq.Item("Shipping", () => ImGui.TextWrapped(shipping));
	/// faq.Item("Returns", () => ImGui.TextWrapped(returns));
	/// </code>
	/// </remarks>
	public sealed class Accordion
	{
		private readonly HashSet<string> open = new(StringComparer.Ordinal);

		/// <summary>Gets or sets a value indicating whether several items can be open at once.</summary>
		public bool AllowMultiple { get; set; }

		/// <summary>Draws one item of the accordion.</summary>
		/// <param name="label">The item's text, also its ImGui id and probe name.</param>
		/// <param name="body">Draws the item's content.</param>
		/// <returns>True on the frame a click opened or closed the item.</returns>
		public bool Item(string label, Action body)
		{
			Ensure.NotNull(label);
			bool isOpen = open.Contains(label);
			bool toggled = ExpandableImpl.Draw(label, ref isOpen, body);
			if (toggled)
			{
				if (isOpen)
				{
					Open(label);
				}
				else
				{
					_ = open.Remove(label);
				}
			}

			return toggled;
		}

		/// <summary>Gets whether an item is open.</summary>
		/// <param name="label">The item's label.</param>
		/// <returns>True when the item is open, or opening.</returns>
		public bool IsOpen(string label) => open.Contains(label);

		/// <summary>Opens an item, closing the others unless <see cref="AllowMultiple"/> is set.</summary>
		/// <param name="label">The item's label.</param>
		public void Open(string label)
		{
			Ensure.NotNull(label);
			if (!AllowMultiple)
			{
				open.Clear();
			}

			_ = open.Add(label);
		}

		/// <summary>Closes an item.</summary>
		/// <param name="label">The item's label.</param>
		public void Close(string label) => open.Remove(label);

		/// <summary>Closes every item.</summary>
		public void CloseAll() => open.Clear();
	}

	internal static class ExpandableImpl
	{
		private sealed class Entry(bool open)
		{
			public ExpandableState State { get; } = new(open);

			public int LastFrame { get; set; } = int.MinValue;
		}

		private const ImGuiWindowFlags BodyFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoNav;

		private static readonly Dictionary<uint, Entry> Entries = [];

		internal static bool Draw(string label, ref bool open, Action body)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(body);

			uint key = ImGui.GetID(label);
			int frame = ImGui.GetFrameCount();

			// An item not drawn last frame shows as it stands rather than animating a change nobody saw.
			if (!Entries.TryGetValue(key, out Entry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new Entry(open);
				Entries[key] = entry;
			}

			entry.LastFrame = frame;
			ExpandableState state = entry.State;
			state.SetOpen(open);

			bool toggled = DrawHeader(label, state.Transition.Eased);
			if (toggled)
			{
				open = !open;
				state.SetOpen(open);
			}

			state.Update(ImGui.GetIO().DeltaTime);
			if (state.IsBodyVisible)
			{
				DrawBody(label, state, body);
			}

			return toggled;
		}

		private static bool DrawHeader(string label, float turned)
		{
			float height = ImGui.GetFrameHeight() + InterfaceScale.Pixels(8f);
			Vector2 min = ImGui.GetCursorScreenPos();
			float width = Math.Max(ImGui.GetContentRegionAvail().X, height);
			bool clicked = ImGui.InvisibleButton(label, new Vector2(width, height));
			ImGuiProbes.MarkItem(label);
			WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);
			bool hovered = ImGui.IsItemHovered();
			bool active = ImGui.IsItemActive();

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Vector2 max = min + new Vector2(width, height);
			if (hovered || active)
			{
				drawList.AddRectFilled(min, max, ImGui.GetColorU32(active ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered), ImGui.GetStyle().FrameRounding);
			}

			// A chevron pointing right when closed that turns to point down as the body opens.
			float size = ImGui.GetFontSize() * 0.3f;
			Vector2 center = new(min.X + ImGui.GetStyle().FramePadding.X + size, min.Y + (height * 0.5f));
			float angle = turned * MathF.PI * 0.5f;
			Vector2 forward = new(MathF.Cos(angle), MathF.Sin(angle));
			Vector2 side = new(-forward.Y, forward.X);
			uint text = ImGui.GetColorU32(ImGuiCol.Text);
			drawList.AddTriangleFilled(center + (forward * size), center - (forward * size * 0.6f) + (side * size), center - (forward * size * 0.6f) - (side * size), text);

			Vector2 labelSize = ImGui.CalcTextSize(label, true);
			drawList.AddText(new Vector2(center.X + (size * 2f) + ImGui.GetStyle().ItemInnerSpacing.X, min.Y + ((height - labelSize.Y) * 0.5f)), text, VisibleLabel(label));
			drawList.AddLine(new Vector2(min.X, max.Y - 0.5f), new Vector2(max.X, max.Y - 0.5f), ImGui.GetColorU32(ImGuiCol.Separator));
			return clicked;
		}

		private static string VisibleLabel(string label)
		{
			int hidden = label.IndexOf("##", StringComparison.Ordinal);
			return hidden < 0 ? label : label[..hidden];
		}

		private static void DrawBody(string label, ExpandableState state, Action body)
		{
			// Always the same child, so the body's items keep one id path whether it is moving or
			// still; fully open it is as tall as the content measured last frame.
			ImGui.PushID(label);
			float width = ImGui.GetContentRegionAvail().X;
			ImGui.BeginChild("##body", new Vector2(width, Math.Max(state.Height, 1f)), ImGuiChildFlags.None, BodyFlags);
			body();
			state.Measure(ImGui.GetCursorPosY() - ImGui.GetStyle().ItemSpacing.Y);
			ImGui.EndChild();
			ImGui.PopID();
		}
	}
}

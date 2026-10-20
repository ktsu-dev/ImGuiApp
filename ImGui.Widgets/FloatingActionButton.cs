// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>The corner of the window a floating action button sits in.</summary>
public enum FloatingActionButtonCorner
{
	/// <summary>The bottom right corner.</summary>
	BottomRight,

	/// <summary>The bottom left corner.</summary>
	BottomLeft,

	/// <summary>The top right corner.</summary>
	TopRight,

	/// <summary>The top left corner.</summary>
	TopLeft,
}

/// <summary>Where a floating action button sits and how big it is.</summary>
public sealed record FloatingActionButtonOptions
{
	/// <summary>Gets the corner of the window the button sits in.</summary>
	public FloatingActionButtonCorner Corner { get; init; } = FloatingActionButtonCorner.BottomRight;

	/// <summary>Gets the button's diameter, in pixels at 100% interface scale.</summary>
	public float Size { get; init; } = 56f;

	/// <summary>Gets the gap between the button and the window's edges, in pixels at 100% interface scale.</summary>
	public float Margin { get; init; } = 16f;

	/// <summary>Gets a tooltip shown while the pointer rests on the button, or null for none.</summary>
	public string? Tooltip { get; init; }
}

/// <summary>One of a speed dial's actions.</summary>
/// <param name="Icon">The glyph drawn in the action's small button.</param>
/// <param name="Label">The text beside it.</param>
public readonly record struct SpeedDialAction(string Icon, string Label);

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A round button floating in a corner of the current window, above its content, for the one thing
	/// a screen is mostly for.
	/// </summary>
	/// <remarks>
	/// The button is drawn in a child window at the corner of the window's visible area, so it stays put
	/// while the content scrolls beneath it and nothing under it can be clicked through it. Call it after
	/// the window's content, as the last thing in the window. It is probe-marked under
	/// <paramref name="id"/>.
	/// </remarks>
	/// <param name="id">A unique id for the button.</param>
	/// <param name="icon">The glyph drawn in the button.</param>
	/// <param name="options">Where the button sits and how big it is; null for the bottom right corner.</param>
	/// <returns>True on the frame the button is clicked.</returns>
	public static bool FloatingActionButton(string id, string icon, FloatingActionButtonOptions? options = null) =>
		FloatingActionButtonImpl.DrawButton(id, icon, options ?? new FloatingActionButtonOptions());

	/// <summary>
	/// A floating action button that opens into a column of smaller labelled actions, as a speed dial
	/// does.
	/// </summary>
	/// <remarks>
	/// Clicking the button opens the actions out of it, away from the corner it sits in; clicking it again,
	/// clicking anywhere else, or choosing an action folds them away. Call it as the last thing in the
	/// window, as <see cref="FloatingActionButton"/> is. The button is probe-marked under
	/// <paramref name="id"/> and each action under <c>id/label</c>.
	/// </remarks>
	/// <param name="id">A unique id for the speed dial.</param>
	/// <param name="icon">The glyph drawn in the main button.</param>
	/// <param name="actions">The actions, nearest the main button first.</param>
	/// <param name="options">Where the button sits and how big it is; null for the bottom right corner.</param>
	/// <returns>The index of the action chosen on this frame, or -1.</returns>
	public static int SpeedDial(string id, string icon, IReadOnlyList<SpeedDialAction> actions, FloatingActionButtonOptions? options = null) =>
		FloatingActionButtonImpl.DrawSpeedDial(id, icon, actions, options ?? new FloatingActionButtonOptions());

	internal static class FloatingActionButtonImpl
	{
		private const ImGuiWindowFlags ChildFlags = ImGuiWindowFlags.NoBackground
			| ImGuiWindowFlags.NoScrollbar
			| ImGuiWindowFlags.NoScrollWithMouse
			| ImGuiWindowFlags.NoSavedSettings;

		private sealed class DialEntry
		{
			public OverlayTransition Transition { get; } = new(0.18f);

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, DialEntry> Entries = [];

		internal static bool DrawButton(string id, string icon, FloatingActionButtonOptions options)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(icon);

			float diameter = InterfaceScale.Pixels(options.Size);
			Vector2 size = new(diameter, diameter);
			ImGui.SetCursorScreenPos(CornerPosition(size, options));
			bool clicked = false;
			if (ImGui.BeginChild($"##{id}", size, ImGuiChildFlags.None, ChildFlags))
			{
				clicked = MainButton(id, icon, ImGui.GetCursorScreenPos(), diameter, options.Tooltip);
			}

			ImGui.EndChild();
			return clicked;
		}

		internal static int DrawSpeedDial(string id, string icon, IReadOnlyList<SpeedDialAction> actions, FloatingActionButtonOptions options)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(icon);
			Ensure.NotNull(actions);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();
			if (!Entries.TryGetValue(key, out DialEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new DialEntry();
				Entries[key] = entry;
			}

			entry.LastFrame = frame;
			entry.Transition.Update(ImGui.GetIO().DeltaTime);

			DialLayout layout = DialLayout.Measure(actions, options, entry.Transition.IsVisible);
			ImGui.SetCursorScreenPos(CornerPosition(layout.Size, options));
			int chosen = -1;
			bool hovered = false;
			if (ImGui.BeginChild($"##{id}", layout.Size, ImGuiChildFlags.None, ChildFlags))
			{
				hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

				// Taken before the actions move the cursor, which is what the main button is placed from.
				Vector2 origin = ImGui.GetCursorScreenPos();
				if (entry.Transition.IsVisible)
				{
					chosen = DrawActions(id, actions, layout, entry.Transition);
				}

				if (MainButton(id, icon, origin + layout.MainOffset, layout.Diameter, options.Tooltip))
				{
					if (entry.Transition.IsOpen)
					{
						entry.Transition.Close();
					}
					else
					{
						entry.Transition.Open();
					}
				}
			}

			ImGui.EndChild();

			bool clickedElsewhere = !hovered && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right));
			if (chosen >= 0 || (clickedElsewhere && entry.Transition.IsOpen))
			{
				entry.Transition.Close();
			}

			return chosen;
		}

		private static Vector2 CornerPosition(Vector2 size, FloatingActionButtonOptions options)
		{
			ImRect inner = ImGuiP.GetCurrentWindow().InnerRect;
			float margin = InterfaceScale.Pixels(options.Margin);
			bool right = options.Corner is FloatingActionButtonCorner.BottomRight or FloatingActionButtonCorner.TopRight;
			bool bottom = options.Corner is FloatingActionButtonCorner.BottomRight or FloatingActionButtonCorner.BottomLeft;
			return new Vector2(
				right ? inner.Max.X - margin - size.X : inner.Min.X + margin,
				bottom ? inner.Max.Y - margin - size.Y : inner.Min.Y + margin);
		}

		private static bool MainButton(string id, string icon, Vector2 origin, float diameter, string? tooltip)
		{
			ImGui.SetCursorScreenPos(origin);
			bool clicked = ImGui.InvisibleButton("##main", new Vector2(diameter, diameter));
			ImGuiProbes.MarkItem(id);
			WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);
			if (tooltip is not null)
			{
				ImGui.SetItemTooltip(tooltip);
			}

			DrawDisc(origin + new Vector2(diameter * 0.5f), diameter * 0.5f, icon, ImGui.IsItemHovered(), ImGui.IsItemActive(), 1f);
			return clicked;
		}

		private static int DrawActions(string id, IReadOnlyList<SpeedDialAction> actions, DialLayout layout, OverlayTransition transition)
		{
			int chosen = -1;
			Vector2 origin = ImGui.GetCursorScreenPos();
			Vector2 mainCenter = origin + layout.MainOffset + new Vector2(layout.Diameter * 0.5f);
			float shown = transition.Eased;
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * shown);
			for (int i = 0; i < actions.Count; i++)
			{
				Vector2 center = layout.ActionCenter(origin, i);
				center = mainCenter + ((center - mainCenter) * shown);
				SpeedDialAction action = actions[i];

				Vector2 labelSize = ImGui.CalcTextSize(action.Label);
				float radius = layout.MiniDiameter * 0.5f;
				float gap = InterfaceScale.Pixels(12f);
				Vector2 pad = new(InterfaceScale.Pixels(10f), InterfaceScale.Pixels(4f));
				Vector2 labelMin = layout.LabelsOnLeft
					? new Vector2(center.X - radius - gap - labelSize.X - (pad.X * 2f), center.Y - (labelSize.Y * 0.5f) - pad.Y)
					: new Vector2(center.X + radius + gap, center.Y - (labelSize.Y * 0.5f) - pad.Y);
				Vector2 labelMax = labelMin + labelSize + (pad * 2f);

				Vector2 rowMin = new(MathF.Min(labelMin.X, center.X - radius), center.Y - radius);
				Vector2 rowMax = new(MathF.Max(labelMax.X, center.X + radius), center.Y + radius);
				ImGui.SetCursorScreenPos(rowMin);
				ImGui.PushID(i);
				if (ImGui.InvisibleButton("##action", rowMax - rowMin) && transition.IsOpen)
				{
					chosen = i;
				}

				ImGui.PopID();
				ImGuiProbes.MarkItem(id, action.Label);
				WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);
				bool hovered = ImGui.IsItemHovered();

				ImDrawListPtr drawList = ImGui.GetWindowDrawList();
				drawList.AddRectFilled(labelMin, labelMax, ImGui.GetColorU32(ImGuiCol.PopupBg), InterfaceScale.Pixels(6f));
				drawList.AddText(labelMin + pad, ImGui.GetColorU32(ImGuiCol.Text), action.Label);
				DrawDisc(center, radius, action.Icon, hovered, ImGui.IsItemActive(), 0.85f);
			}

			ImGui.PopStyleVar();
			return chosen;
		}

		private static void DrawDisc(Vector2 center, float radius, string icon, bool hovered, bool active, float strength)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Vector4 fill = ImGui.GetStyle().Colors[(int)(active ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.ButtonActive)];
			fill.W = strength;
			if (!hovered && !active)
			{
				fill = new Vector4(fill.X * 0.85f, fill.Y * 0.85f, fill.Z * 0.85f, fill.W);
			}

			drawList.AddCircleFilled(center + new Vector2(0f, InterfaceScale.Pixels(2f)), radius, ImGui.GetColorU32(ImGuiCol.ModalWindowDimBg, 0.5f * strength));
			drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(fill));
			Vector2 iconSize = ImGui.CalcTextSize(icon);
			drawList.AddText(center - (iconSize * 0.5f), ImGui.GetColorU32(ImGuiCol.Text), icon);
		}

		private readonly record struct DialLayout(Vector2 Size, Vector2 MainOffset, float Diameter, float MiniDiameter, float Spacing, bool LabelsOnLeft, bool Upwards, int Count)
		{
			internal static DialLayout Measure(IReadOnlyList<SpeedDialAction> actions, FloatingActionButtonOptions options, bool expanded)
			{
				float diameter = InterfaceScale.Pixels(options.Size);
				float mini = InterfaceScale.Pixels(40f);
				float spacing = InterfaceScale.Pixels(16f);
				bool labelsOnLeft = options.Corner is FloatingActionButtonCorner.BottomRight or FloatingActionButtonCorner.TopRight;
				bool upwards = options.Corner is FloatingActionButtonCorner.BottomRight or FloatingActionButtonCorner.BottomLeft;
				if (!expanded || actions.Count == 0)
				{
					return new DialLayout(new Vector2(diameter), Vector2.Zero, diameter, mini, spacing, labelsOnLeft, upwards, 0);
				}

				float labelWidth = 0f;
				foreach (SpeedDialAction action in actions)
				{
					labelWidth = MathF.Max(labelWidth, ImGui.CalcTextSize(action.Label).X);
				}

				float side = labelWidth + InterfaceScale.Pixels(12f + 20f) + ((diameter - mini) * 0.5f);
				float height = diameter + (actions.Count * (mini + spacing));
				Vector2 size = new(diameter + side, height);
				Vector2 mainOffset = new(labelsOnLeft ? side : 0f, upwards ? height - diameter : 0f);
				return new DialLayout(size, mainOffset, diameter, mini, spacing, labelsOnLeft, upwards, actions.Count);
			}

			internal Vector2 ActionCenter(Vector2 origin, int index)
			{
				Vector2 main = origin + MainOffset + new Vector2(Diameter * 0.5f);
				float step = (Diameter * 0.5f) + Spacing + (MiniDiameter * 0.5f) + (index * (MiniDiameter + Spacing));
				return main + new Vector2(0f, Upwards ? -step : step);
			}
		}
	}
}

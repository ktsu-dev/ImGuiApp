// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
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
	/// Draws a picker wheel: a column of items on a turning cylinder, the selected one in a band across
	/// the middle and its neighbours crowding together and fading as they turn away.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Drag the wheel up or down to turn it; a flick spins it on and it slows to a stop on the nearest
	/// item. Clicking an item above or below the band turns it to the middle, and the mouse wheel moves one
	/// item per notch. Changing <paramref name="selectedIndex"/> from outside turns the wheel there.
	/// </para>
	/// <para>
	/// The selection changes as soon as the wheel's destination is known, on release or click, rather than
	/// when it comes to rest. The wheel is probe-marked under <paramref name="id"/>.
	/// </para>
	/// </remarks>
	/// <param name="id">A unique id for the wheel.</param>
	/// <param name="selectedIndex">The selected item. Updated when the user picks another.</param>
	/// <param name="items">The items, in order.</param>
	/// <param name="options">How the wheel looks and behaves, or null for the defaults.</param>
	/// <returns><see langword="true"/> on the frame the user picked another item; otherwise <see langword="false"/>.</returns>
	public static bool PickerWheel(string id, ref int selectedIndex, IReadOnlyList<string> items, PickerWheelOptions? options = null)
	{
		Ensure.NotNull(items);
		return PickerWheelImpl.Draw(id, ref selectedIndex, items.Count, i => items[i], options ?? new PickerWheelOptions());
	}

	/// <summary>
	/// Draws a picker wheel over a range of whole numbers. See
	/// <see cref="PickerWheel(string, ref int, IReadOnlyList{string}, PickerWheelOptions?)"/> for how it behaves.
	/// </summary>
	/// <param name="id">A unique id for the wheel.</param>
	/// <param name="value">The selected number. Updated when the user picks another, and moved onto the range and its steps if it is off them.</param>
	/// <param name="min">The first number.</param>
	/// <param name="max">The last number. It is shown only when it falls on a step from <paramref name="min"/>.</param>
	/// <param name="step">The difference between neighbouring numbers. Must be positive.</param>
	/// <param name="format">A .NET numeric format for each number, such as <c>"00"</c> for minutes, or null for plain digits.</param>
	/// <param name="options">How the wheel looks and behaves, or null for the defaults.</param>
	/// <returns><see langword="true"/> on the frame the user picked another number; otherwise <see langword="false"/>.</returns>
	public static bool PickerWheel(string id, ref int value, int min, int max, int step = 1, string? format = null, PickerWheelOptions? options = null)
	{
		Ensure.NotNull(id);
		if (step <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(step), step, "The step must be positive.");
		}

		int count = max < min ? 0 : ((max - min) / step) + 1;
		int index = count == 0 ? 0 : Math.Clamp((value - min) / step, 0, count - 1);
		bool changed = PickerWheelImpl.Draw(id, ref index, count, i => (min + (i * step)).ToString(format, CultureInfo.CurrentCulture), options ?? new PickerWheelOptions());
		if (count > 0)
		{
			value = min + (index * step);
		}

		return changed;
	}

	internal static class PickerWheelImpl
	{
		private sealed class WheelEntry(int index)
		{
			public PickerWheelState State { get; } = new(index);

			public GestureMachine Gestures { get; } = new();

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, WheelEntry> Entries = [];

		/// <summary>Gets the height of one row: the font's line height with the frame's padding above and below.</summary>
		internal static float RowHeight => ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2f);

		/// <summary>Gets the angle between neighbouring rows on the cylinder, in radians, for a wheel showing so many rows.</summary>
		/// <param name="visibleRows">How many rows the wheel shows.</param>
		/// <returns>The angle.</returns>
		internal static float RowAngle(int visibleRows) => MathF.PI / (visibleRows + 1);

		internal static bool Draw(string id, ref int selectedIndex, int count, Func<int, string> label, PickerWheelOptions options)
		{
			Ensure.NotNull(id);
			bool wrap = options.Wrap;
			int rows = Math.Max(options.VisibleRows, 1);
			selectedIndex = count <= 0 ? 0 : Math.Clamp(selectedIndex, 0, count - 1);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();

			// A wheel not drawn last frame starts on its item at rest, rather than finishing a spin the
			// user never saw, or one left by an earlier ImGui context.
			if (!Entries.TryGetValue(key, out WheelEntry? entry) || frame - entry.LastFrame != 1)
			{
				entry = new WheelEntry(selectedIndex);
				Entries[key] = entry;
			}

			entry.LastFrame = frame;
			PickerWheelState state = entry.State;
			if (!state.IsDragging && PickerWheelState.WrapIndex(state.Target, count) != selectedIndex)
			{
				state.GoTo(selectedIndex, count, wrap);
			}

			state.Update(ImGui.GetIO().DeltaTime);

			float rowHeight = RowHeight;
			float width = options.Width > 0f ? options.Width : FitWidth(count, label);
			Vector2 size = new(width, rowHeight * rows);
			Vector2 origin = ImGui.GetCursorScreenPos();

			ImGui.PushID(id);
			ImGui.InvisibleButton("##wheel", size);
			ImGuiProbes.MarkItem(id);
			WidgetCursor.OnLastItem(ImGuiMouseCursor.ResizeNs);
			bool activated = ImGui.IsItemActivated();
			bool hovered = ImGui.IsItemHovered();
			GestureResult gesture = entry.Gestures.Update(ImGui.IsItemActive(), ImGui.GetMousePos(), ImGui.GetIO().DeltaTime);

			int before = selectedIndex;
			if (count > 0)
			{
				selectedIndex = HandleInput(state, gesture, activated, hovered, origin, size, rowHeight, count, rows, wrap, selectedIndex);
			}

			DrawWheel(origin, size, rowHeight, rows, count, wrap, state.Position, label);
			ImGui.PopID();
			return selectedIndex != before;
		}

		private static int HandleInput(PickerWheelState state, GestureResult gesture, bool activated, bool hovered, Vector2 origin, Vector2 size, float rowHeight, int count, int rows, bool wrap, int selectedIndex)
		{
			if (activated)
			{
				state.BeginDrag();
			}

			if ((gesture.Gestures & GestureFlags.Pan) != 0)
			{
				state.Drag(gesture.Delta.Y, rowHeight, count, wrap);
			}

			if (state.IsDragging && !gesture.IsPressed)
			{
				if (gesture.Tapped || gesture.DoubleTapped)
				{
					// A click turns the row under it to the middle.
					int offset = RowAt(gesture.StartPos.Y - (origin.Y + (size.Y * 0.5f)), size.Y * 0.5f, rows);
					return state.Step(offset, count, wrap);
				}

				return state.Release((gesture.Gestures & GestureFlags.PanEnd) != 0 ? gesture.Velocity.Y : 0f, rowHeight, count, wrap);
			}

			if (hovered && !state.IsDragging)
			{
				ImGui.SetItemKeyOwner(ImGuiKey.MouseWheelY);
				float wheel = ImGui.GetIO().MouseWheel;
				if (wheel != 0f)
				{
					// The mouse wheel rolls the picker the way it scrolls a list: away from you shows earlier
					// items. A trackpad's fraction of a notch still moves one item.
					int notches = (int)MathF.Round(wheel);
					return state.Step(-(notches != 0 ? notches : Math.Sign(wheel)), count, wrap);
				}
			}

			return selectedIndex;
		}

		/// <summary>Finds which row, counted from the middle, lies under a point on the cylinder.</summary>
		/// <param name="offsetY">The point's height below the wheel's middle, in pixels.</param>
		/// <param name="radius">The cylinder's radius, half the wheel's height.</param>
		/// <param name="rows">How many rows the wheel shows.</param>
		/// <returns>The row, negative above the middle.</returns>
		internal static int RowAt(float offsetY, float radius, int rows)
		{
			float angle = MathF.Asin(Math.Clamp(offsetY / MathF.Max(radius, 1f), -1f, 1f));
			int row = (int)MathF.Round(angle / RowAngle(rows), MidpointRounding.AwayFromZero);
			return Math.Clamp(row, -rows / 2, rows / 2);
		}

		private static float FitWidth(int count, Func<int, string> label)
		{
			float widest = 0f;
			for (int i = 0; i < count; i++)
			{
				widest = MathF.Max(widest, ImGui.CalcTextSize(label(i)).X);
			}

			return widest + (ImGui.GetStyle().FramePadding.X * 4f);
		}

		private static void DrawWheel(Vector2 origin, Vector2 size, float rowHeight, int rows, int count, bool wrap, float position, Func<int, string> label)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float rounding = ImGui.GetStyle().FrameRounding;
			float centerY = origin.Y + (size.Y * 0.5f);
			float radius = size.Y * 0.5f;

			drawList.AddRectFilled(origin, origin + size, ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);
			Vector2 bandMin = new(origin.X, centerY - (rowHeight * 0.5f));
			drawList.AddRectFilled(bandMin, bandMin + new Vector2(size.X, rowHeight), ImGui.GetColorU32(ImGuiCol.FrameBgActive), rounding);

			if (count <= 0)
			{
				return;
			}

			ImGui.PushClipRect(origin, origin + size, true);
			Vector4 text = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
			float rowAngle = RowAngle(rows);
			float reach = (rows * 0.5f) + 1f;

			int first = (int)MathF.Floor(position - reach);
			int last = (int)MathF.Ceiling(position + reach);
			for (int row = first; row <= last; row++)
			{
				if (!wrap && (row < 0 || row >= count))
				{
					continue;
				}

				float angle = (row - position) * rowAngle;
				if (MathF.Abs(angle) >= MathF.PI * 0.5f)
				{
					continue;
				}

				// Rows crowd together and dim as they turn away from the viewer, as on a real cylinder. The
				// text keeps its size: every size a font is drawn at is baked into the atlas, and a size that
				// changed with the wheel's position would bake a new one on every frame of a spin.
				float facing = MathF.Cos(angle);
				string itemLabel = label(PickerWheelState.WrapIndex(row, count));
				Vector2 textSize = ImGui.CalcTextSize(itemLabel);
				float y = centerY + (MathF.Sin(angle) * radius);
				Vector2 position2D = new(origin.X + ((size.X - textSize.X) * 0.5f), y - (textSize.Y * 0.5f));
				uint color = ImGui.ColorConvertFloat4ToU32(text with { W = text.W * facing * facing });
				drawList.AddText(position2D, color, itemLabel);
			}

			ImGui.PopClipRect();
		}
	}
}

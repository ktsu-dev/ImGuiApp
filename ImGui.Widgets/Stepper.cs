// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a <c>[-] value [+]</c> integer stepper with hold-to-repeat on the increment/decrement
	/// buttons (after a short initial delay the value keeps changing while a button is held).
	/// </summary>
	/// <param name="label">A unique label used for the widget ID; text after <c>##</c> is hidden but used for the ID.</param>
	/// <param name="value">The current value. Updated in place and clamped to <paramref name="min"/>/<paramref name="max"/>.</param>
	/// <param name="step">The amount added or subtracted per activation.</param>
	/// <param name="min">The inclusive lower bound.</param>
	/// <param name="max">The inclusive upper bound.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool Stepper(string label, ref int value, int step = 1, int min = int.MinValue, int max = int.MaxValue) =>
		StepperImpl.Draw(label, ref value, step, min, max);

	internal static class StepperImpl
	{
		// Per-ID seconds a repeat button has been held; resets when released.
		private static readonly Dictionary<uint, float> HoldTime = [];

		private const float RepeatDelay = 0.4f;
		private const float RepeatInterval = 0.06f;

		public static bool Draw(string label, ref int value, int step, int min, int max)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			ImGui.PushID(label);
			bool changed = false;

			float height = ImGui.GetFrameHeight();
			Vector2 buttonSize = new(height, height);

			if (RepeatButton("-##dec", buttonSize))
			{
				int next = Math.Clamp(SafeAdd(value, -step), min, max);
				if (next != value)
				{
					value = next;
					changed = true;
				}
			}

			ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);

			string text = value.ToString(CultureInfo.CurrentCulture);
			float valueWidth = MathF.Max(ImGui.CalcTextSize(text).X + (ImGui.GetStyle().FramePadding.X * 2.0f), height * 1.5f);
			Vector2 valueOrigin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton("##value", new Vector2(valueWidth, height));

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			drawList.AddRectFilled(valueOrigin, new Vector2(valueOrigin.X + valueWidth, valueOrigin.Y + height), ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), ImGui.GetStyle().FrameRounding);
			Vector2 textSize = ImGui.CalcTextSize(text);
			drawList.AddText(new Vector2(valueOrigin.X + ((valueWidth - textSize.X) * 0.5f), valueOrigin.Y + ((height - textSize.Y) * 0.5f)), ImGui.GetColorU32(colors[(int)ImGuiCol.Text]), text);

			ImGui.SameLine(0.0f, ImGui.GetStyle().ItemInnerSpacing.X);

			if (RepeatButton("+##inc", buttonSize))
			{
				int next = Math.Clamp(SafeAdd(value, step), min, max);
				if (next != value)
				{
					value = next;
					changed = true;
				}
			}

			string visible = VisibleLabel(label);
			if (visible.Length > 0)
			{
				ImGui.SameLine();
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted(visible);
			}

			ImGui.PopID();
			return changed;
		}

		// A button that fires once on press, then repeatedly while held past the initial delay.
		private static bool RepeatButton(string label, Vector2 size)
		{
			uint id = ImGui.GetID(label);
			bool fired = ImGui.Button(label, size);

			if (ImGui.IsItemActive())
			{
				float prev = HoldTime.GetValueOrDefault(id, 0.0f);
				float now = prev + ImGui.GetIO().DeltaTime;
				HoldTime[id] = now;
				fired |= ShouldRepeat(prev, now, RepeatDelay, RepeatInterval);
			}
			else
			{
				HoldTime.Remove(id);
			}

			return fired;
		}

		/// <summary>
		/// Pure hold-to-repeat decision: returns <see langword="true"/> when the hold crosses the
		/// initial <paramref name="delay"/>, or each time it crosses an <paramref name="interval"/>
		/// boundary thereafter, while advancing from <paramref name="prev"/> to <paramref name="now"/> seconds.
		/// </summary>
		internal static bool ShouldRepeat(float prev, float now, float delay, float interval)
		{
			if (prev < delay)
			{
				return now >= delay;
			}

			// Fire once per interval boundary crossed past the delay.
			float a = (prev - delay) / interval;
			float b = (now - delay) / interval;
			return MathF.Floor(b) > MathF.Floor(a);
		}

		private static int SafeAdd(int value, int delta)
		{
			long result = (long)value + delta;
			return result > int.MaxValue ? int.MaxValue : result < int.MinValue ? int.MinValue : (int)result;
		}
	}
}

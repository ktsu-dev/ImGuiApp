// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a time picker: an hour wheel and a minute wheel side by side, and an AM/PM wheel on a
	/// twelve-hour clock, each a <see cref="PickerWheel(string, ref int, System.Collections.Generic.IReadOnlyList{string}, PickerWheelOptions?)"/>.
	/// </summary>
	/// <remarks>
	/// The hour and minute wheels wrap, so spinning past 59 minutes comes back round to 00; they do not
	/// carry into each other. The wheels are probe-marked under <c>id/hour</c>, <c>id/minute</c> and
	/// <c>id/period</c>. A time between minute steps shows the step below it until the user turns a
	/// wheel, and seconds are dropped from any time the picker writes.
	/// </remarks>
	/// <param name="id">A unique id for the picker.</param>
	/// <param name="time">The time shown. Updated when the user turns a wheel.</param>
	/// <param name="options">How the picker looks and behaves, or null for the defaults.</param>
	/// <returns><see langword="true"/> on the frame the user changed the time; otherwise <see langword="false"/>.</returns>
	public static bool TimePicker(string id, ref TimeOnly time, TimePickerOptions? options = null)
	{
		Ensure.NotNull(id);
		options ??= new TimePickerOptions();
		int step = TimePickerState.ValidStep(options.MinuteStep);
		bool twelveHour = options.TwelveHour;

		TimeWheels wheels = TimePickerState.ToWheels(time, twelveHour, step);
		int hour = wheels.Hour;
		int minutes = wheels.Minute * step;
		int period = wheels.Afternoon ? 1 : 0;

		PickerWheelOptions wrapping = new() { VisibleRows = options.VisibleRows, Wrap = true };
		float spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.BeginGroup();
		bool changed = PickerWheel($"{id}/hour", ref hour, TimePickerState.HourLabels(twelveHour), wrapping);
		ImGui.SameLine(0f, spacing);
		changed |= PickerWheel($"{id}/minute", ref minutes, 0, 60 - step, step, "00", wrapping);
		if (twelveHour)
		{
			ImGui.SameLine(0f, spacing);
			changed |= PickerWheel($"{id}/period", ref period, TimePickerState.PeriodLabels(), new PickerWheelOptions { VisibleRows = options.VisibleRows });
		}

		ImGui.EndGroup();

		if (changed)
		{
			time = TimePickerState.FromWheels(new TimeWheels(hour, minutes / step, period == 1), twelveHour, step);
		}

		return changed;
	}
}

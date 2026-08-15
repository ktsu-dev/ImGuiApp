// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Text;

using HexaDatePicker = Hexa.NET.ImGui.Widgets.DatePicker;
using HexaYearPicker = Hexa.NET.ImGui.Widgets.YearPicker;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Encodes a label to a null-terminated UTF-8 buffer for Hexa's span-based overloads.
	/// </summary>
	/// <param name="label">The label to encode.</param>
	/// <returns>The encoded bytes, including a trailing null terminator.</returns>
	private static byte[] EncodeLabel(string label)
	{
		int byteCount = Encoding.UTF8.GetByteCount(label);
		byte[] buffer = new byte[byteCount + 1];
		Encoding.UTF8.GetBytes(label, buffer);
		buffer[byteCount] = 0;
		return buffer;
	}

	/// <summary>
	/// Draws a calendar control for picking a date.
	/// </summary>
	/// <remarks>
	/// Requires a Material Icons font in the atlas — the control draws the Material
	/// <c>CalendarToday</c> glyph at U+E935 and renders a placeholder box without one. See
	/// <c>FontHelper.AddMaterialIconRanges</c> in ktsu.ImGui.App.
	/// </remarks>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="date">The selected date, updated in place when a new date is picked.</param>
	/// <returns><see langword="true"/> if the date changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool DatePicker(string label, ref DateTime date)
	{
		Ensure.NotNull(label);

		DateTime before = date;
		HexaDatePicker.Draw(EncodeLabel(label), ref date);
		return date != before;
	}

	/// <summary>
	/// Draws a grid control for picking a year.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="date">The selected date, whose year is updated in place when a new year is picked.</param>
	/// <returns><see langword="true"/> if the year changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static bool YearPicker(string label, ref DateTime date)
	{
		Ensure.NotNull(label);
		return HexaYearPicker.Draw(EncodeLabel(label), ref date);
	}
}

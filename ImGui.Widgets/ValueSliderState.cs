// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// One value slider's interaction between frames, and the arithmetic behind it, with no ImGui
	/// dependency so the rules are unit tests.
	/// </summary>
	internal sealed class ValueSliderState
	{
		/// <summary>Gets or sets the unrounded value a drag has reached, so an integer slider moved a pixel at a time still steps.</summary>
		internal double DragValue { get; set; }

		/// <summary>Gets or sets whether the press under way has passed the drag threshold.</summary>
		internal bool IsDragging { get; set; }

		/// <summary>Gets or sets whether releasing the press under way opens the value for typing.</summary>
		internal bool OpenOnRelease { get; set; }

		/// <summary>Gets or sets whether the value is open for typing.</summary>
		internal bool IsEditing { get; set; }

		/// <summary>Gets or sets the text being typed.</summary>
		internal string EditText { get; set; } = string.Empty;

		/// <summary>Gets or sets the text the editor opened with, so closing it unchanged changes nothing.</summary>
		internal string EditStartText { get; set; } = string.Empty;

		/// <summary>Gets or sets whether the editor has yet to ask for the keyboard.</summary>
		internal bool FocusPending { get; set; }

		/// <summary>Gets or sets how many frames the editor has been drawn without holding the keyboard.</summary>
		internal int EditFramesUnfocused { get; set; }

		/// <summary>Picks a format for a range when the caller gave none.</summary>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <returns>A .NET numeric format.</returns>
		internal static string DefaultFormat(double min, double max)
		{
			double span = Math.Abs(max - min);
			return span >= 100.0 ? "0" : span >= 10.0 ? "0.0" : span >= 1.0 ? "0.00" : "0.000";
		}

		/// <summary>Writes a value as the slider shows it.</summary>
		/// <param name="value">The value.</param>
		/// <param name="format">The .NET numeric format.</param>
		/// <param name="unit">The unit written after the number, or null.</param>
		/// <param name="culture">The culture to format in.</param>
		/// <returns>The text.</returns>
		internal static string Format(double value, string format, string? unit, IFormatProvider culture)
		{
			string number = value.ToString(format, culture);

			// "-0" reads as a value below zero; a slider resting on zero should say so plainly.
			if (number.Length > 1 && number[0] == '-' && number.AsSpan(1).IndexOfAnyInRange('1', '9') < 0)
			{
				number = number[1..];
			}

			return number + unit;
		}

		/// <summary>Reads a typed value, ignoring surrounding space and the unit if it was typed too.</summary>
		/// <param name="text">The typed text.</param>
		/// <param name="unit">The slider's unit, or null.</param>
		/// <param name="culture">The culture tried first; the invariant culture is tried after it.</param>
		/// <param name="value">The value read.</param>
		/// <returns>Whether the text held a finite number.</returns>
		internal static bool TryParse(string text, string? unit, IFormatProvider culture, out double value)
		{
			string trimmed = text.Trim();
			string unitText = unit?.Trim() ?? string.Empty;
			if (unitText.Length > 0 && trimmed.EndsWith(unitText, StringComparison.OrdinalIgnoreCase))
			{
				trimmed = trimmed[..^unitText.Length].TrimEnd();
			}

			// No thousands separators: where the culture's decimal mark is a comma its group mark is often a
			// point, and "2.5" would read as 25 instead of falling through to the invariant culture.
			const NumberStyles Styles = NumberStyles.Float;
			bool parsed = double.TryParse(trimmed, Styles, culture, out value)
				|| double.TryParse(trimmed, Styles, CultureInfo.InvariantCulture, out value);

			return parsed && double.IsFinite(value);
		}

		/// <summary>Moves a value by a horizontal drag, the frame's full width covering the full range.</summary>
		/// <param name="value">The value before this movement.</param>
		/// <param name="deltaPixels">How far the pointer moved, in pixels.</param>
		/// <param name="width">The frame's width, in pixels.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="scale">1 for a normal drag, or the fine scale while Shift is held.</param>
		/// <returns>The moved value, kept inside the range.</returns>
		internal static double Drag(double value, float deltaPixels, float width, double min, double max, float scale)
		{
			if (width <= 0.0f)
			{
				return Math.Clamp(value, min, max);
			}

			double moved = value + (deltaPixels / (double)width * (max - min) * scale);
			return Math.Clamp(moved, min, max);
		}

		/// <summary>Finds the part of the frame the fill covers, as fractions of its width.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="fill">Where the fill starts.</param>
		/// <returns>The fill's left and right edges, each from zero to one, left never past right.</returns>
		internal static (float Start, float End) FillSpan(double value, double min, double max, ValueSliderFill fill)
		{
			float at = Fraction(value, min, max);
			if (fill == ValueSliderFill.FromMinimum)
			{
				return (0.0f, at);
			}

			float zero = Fraction(0.0, min, max);
			return at < zero ? (at, zero) : (zero, at);
		}

		/// <summary>Finds where a value sits across the range.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <returns>Zero at the minimum and one at the maximum, clamped.</returns>
		internal static float Fraction(double value, double min, double max) =>
			max <= min ? 0.0f : (float)Math.Clamp((value - min) / (max - min), 0.0, 1.0);
	}
}

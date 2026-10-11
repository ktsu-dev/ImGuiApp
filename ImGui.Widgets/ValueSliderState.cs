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
		/// <param name="logarithmic">Whether the width covers the range logarithmically.</param>
		/// <param name="epsilon">The smallest magnitude a logarithmic range starting at zero reaches before zero.</param>
		/// <returns>The moved value, kept inside the range.</returns>
		internal static double Drag(double value, float deltaPixels, float width, double min, double max, float scale, bool logarithmic = false, double epsilon = 0.0)
		{
			if (width <= 0.0f)
			{
				return Math.Clamp(value, min, max);
			}

			if (logarithmic && IsLogarithmic(min, max, epsilon))
			{
				// The drag moves the position along the frame, and the value follows the position, so equal
				// distances multiply the value by equal factors wherever the drag happens.
				double position = Position(value, min, max, logarithmic: true, epsilon) + (deltaPixels / (double)width * scale);
				return ValueAt(position, min, max, logarithmic: true, epsilon);
			}

			double moved = value + (deltaPixels / (double)width * (max - min) * scale);
			return Math.Clamp(moved, min, max);
		}

		/// <summary>
		/// Finds the smallest magnitude a logarithmic range that starts at zero reaches before zero itself:
		/// the smallest step the format can show, so the slider's left end reads 0 and the next thing it can
		/// show is the first value worth showing.
		/// </summary>
		/// <param name="format">The .NET numeric format the value is shown with.</param>
		/// <param name="isInteger">Whether the slider holds whole numbers.</param>
		/// <returns>A positive magnitude.</returns>
		internal static double LogarithmicEpsilon(string format, bool isInteger)
		{
			if (isInteger)
			{
				return 1.0;
			}

			int decimals = 0;
			string section = format.Split(';')[0];
			int point = section.IndexOf('.', StringComparison.Ordinal);
			if (point >= 0)
			{
				foreach (char c in section.AsSpan(point + 1))
				{
					if (c is '0' or '#')
					{
						decimals++;
					}
					else if (c is 'e' or 'E' or '%' or '\\' or '\'' or '"')
					{
						break;
					}
				}
			}
			else if (section.Length > 1 && char.IsLetter(section[0]) && int.TryParse(section.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out int precision))
			{
				// A standard format such as "F2" names its precision outright.
				decimals = precision;
			}

			return Math.Pow(10.0, -Math.Clamp(decimals, 0, 10));
		}

		/// <summary>
		/// Whether a range can be covered logarithmically: it must not cross zero, and a range starting at
		/// zero must reach past the epsilon. Anything else is covered linearly.
		/// </summary>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="epsilon">The smallest magnitude a range starting at zero reaches before zero.</param>
		/// <returns>Whether the range is logarithmic.</returns>
		internal static bool IsLogarithmic(double min, double max, double epsilon)
		{
			if (max <= min || (min < 0.0 && max > 0.0))
			{
				return false;
			}

			(double near, double far) = min >= 0.0 ? (min, max) : (-max, -min);
			return near > 0.0 || (epsilon > 0.0 && far > epsilon);
		}

		/// <summary>Finds the value at a position across the range, the inverse of <see cref="Fraction(double, double, double, bool, double)"/>.</summary>
		/// <param name="fraction">Zero at the left edge and one at the right; clamped.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="logarithmic">Whether the range is covered logarithmically.</param>
		/// <param name="epsilon">The smallest magnitude a logarithmic range starting at zero reaches before zero.</param>
		/// <returns>The value, inside the range.</returns>
		internal static double ValueAt(double fraction, double min, double max, bool logarithmic = false, double epsilon = 0.0)
		{
			double t = Math.Clamp(fraction, 0.0, 1.0);
			if (!logarithmic || !IsLogarithmic(min, max, epsilon))
			{
				return Math.Clamp(min + (t * (max - min)), min, max);
			}

			if (min >= 0.0)
			{
				return MagnitudeAt(t, min, max, epsilon);
			}

			// A range wholly below zero mirrors one above it: its far left is its largest magnitude.
			return -MagnitudeAt(1.0 - t, -max, -min, epsilon);
		}

		private static double MagnitudeAt(double t, double near, double far, double epsilon)
		{
			if (t <= 0.0)
			{
				return near;
			}

			if (t >= 1.0)
			{
				return far;
			}

			double low = near > 0.0 ? near : epsilon;
			return Math.Clamp(low * Math.Pow(far / low, t), near, far);
		}

		private static double MagnitudeFraction(double magnitude, double near, double far, double epsilon)
		{
			double low = near > 0.0 ? near : epsilon;
			if (magnitude <= low)
			{
				return 0.0;
			}

			return magnitude >= far ? 1.0 : Math.Log(magnitude / low) / Math.Log(far / low);
		}

		/// <summary>
		/// Whether the fill should stay out from behind the number: a fill lying wholly under it, such as a
		/// value near zero filled from zero, would otherwise run through the digits as a thin bar.
		/// </summary>
		/// <param name="fillStart">The fill's left edge, in pixels.</param>
		/// <param name="fillEnd">The fill's right edge, in pixels.</param>
		/// <param name="textStart">The number's left edge, padded, in pixels.</param>
		/// <param name="textEnd">The number's right edge, padded, in pixels.</param>
		/// <returns>Whether to draw the fill only above and below the number.</returns>
		internal static bool FillAvoidsText(float fillStart, float fillEnd, float textStart, float textEnd) =>
			fillEnd > fillStart && fillStart >= textStart && fillEnd <= textEnd;

		/// <summary>Finds the part of the frame the fill covers, as fractions of its width.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="fill">Where the fill starts.</param>
		/// <param name="logarithmic">Whether the range is covered logarithmically.</param>
		/// <param name="epsilon">The smallest magnitude a logarithmic range starting at zero reaches before zero.</param>
		/// <returns>The fill's left and right edges, each from zero to one, left never past right.</returns>
		internal static (float Start, float End) FillSpan(double value, double min, double max, ValueSliderFill fill, bool logarithmic = false, double epsilon = 0.0)
		{
			float at = Fraction(value, min, max, logarithmic, epsilon);
			if (fill == ValueSliderFill.FromMinimum)
			{
				return (0.0f, at);
			}

			float zero = Fraction(0.0, min, max, logarithmic, epsilon);
			return at < zero ? (at, zero) : (zero, at);
		}

		/// <summary>Finds where a value sits across the range.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The lowest value.</param>
		/// <param name="max">The highest value.</param>
		/// <param name="logarithmic">Whether the range is covered logarithmically.</param>
		/// <param name="epsilon">The smallest magnitude a logarithmic range starting at zero reaches before zero.</param>
		/// <returns>Zero at the minimum and one at the maximum, clamped.</returns>
		internal static float Fraction(double value, double min, double max, bool logarithmic = false, double epsilon = 0.0) =>
			(float)Position(value, min, max, logarithmic, epsilon);

		/// <summary>Finds where a value sits across the range at full precision, which a drag accumulates in.</summary>
		private static double Position(double value, double min, double max, bool logarithmic, double epsilon)
		{
			if (max <= min)
			{
				return 0.0;
			}

			if (!logarithmic || !IsLogarithmic(min, max, epsilon))
			{
				return Math.Clamp((value - min) / (max - min), 0.0, 1.0);
			}

			double clamped = Math.Clamp(value, min, max);
			return min >= 0.0
				? MagnitudeFraction(clamped, min, max, epsilon)
				: 1.0 - MagnitudeFraction(-clamped, -max, -min, epsilon);
		}
	}
}

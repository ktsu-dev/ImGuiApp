// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

public static partial class ImGuiWidgets
{
	/// <summary>Turns cell values into text and back, always in the invariant culture.</summary>
	/// <remarks>
	/// Invariant so that a value copied or typed on one machine means the same on every other. A
	/// column that wants local formatting for display supplies its own <c>Format</c>.
	/// </remarks>
	internal static class DataTableText
	{
		private static readonly char[] CharactersNeedingQuotes = ['\t', '\n', '\r', '"'];

		internal static string FormatInvariant<TValue>(TValue value) => value switch
		{
			null => string.Empty,
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString() ?? string.Empty,
		};

		internal static bool IsTextEditable(Type type) =>
			type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double);

		internal static bool HasBuiltInEditor(Type type) =>
			IsTextEditable(type) || type == typeof(bool) || type.IsEnum;

		internal static bool TryParse<TValue>(string text, [MaybeNullWhen(false)] out TValue value)
		{
			object? parsed = null;

			if (typeof(TValue) == typeof(string))
			{
				parsed = text;
			}
			else if (typeof(TValue) == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int asInt))
			{
				parsed = asInt;
			}
			else if (typeof(TValue) == typeof(long) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long asLong))
			{
				parsed = asLong;
			}
			else if (typeof(TValue) == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float asFloat))
			{
				parsed = asFloat;
			}
			else if (typeof(TValue) == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double asDouble))
			{
				parsed = asDouble;
			}

			if (parsed is TValue typed)
			{
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Quotes a value the way a spreadsheet reads it back, so a tab, a line break or a quote inside
		/// it pastes as part of one cell rather than splitting it.
		/// </summary>
		internal static string QuoteForCopy(string text) =>
			text.IndexOfAny(CharactersNeedingQuotes) < 0
				? text
				: $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
	}
}

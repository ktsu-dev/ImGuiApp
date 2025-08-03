// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// String manipulation operations as static method nodes.

namespace ktsu.NodeGraph.Library.Operations;
using System;
using System.ComponentModel;

/// <summary>
/// String manipulation operations as static method nodes.
/// </summary>
public static class StringOperations
{
	[Node("Format")]
	[Description("Formats a string with a single parameter")]
	public static string Format(string format, object value)
	{
		try
		{
			return string.Format(format, value);
		}
		catch
		{
			return "Format Error";
		}
	}

	[Node("Concatenate")]
	[Description("Combines two strings")]
	public static string Concatenate(string first, string second) => first + second;

	[Node("To Upper")]
	[Description("Converts string to uppercase")]
	public static string ToUpper(string input) => input?.ToUpperInvariant() ?? string.Empty;

	[Node("To Lower")]
	[Description("Converts string to lowercase")]
	public static string ToLower(string input) => input?.ToLowerInvariant() ?? string.Empty;

	[Node("String Length")]
	[Description("Gets the length of a string")]
	public static int Length(string input) => input?.Length ?? 0;

	[Node("Contains")]
	[Description("Checks if string contains a substring")]
	public static bool Contains(string input, string substring) =>
		!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(substring) &&
		input.Contains(substring, StringComparison.Ordinal);

	[Node("Replace")]
	[Description("Replaces all occurrences of old value with new value")]
	public static string Replace(string input, string oldValue, string newValue) =>
		string.IsNullOrEmpty(input) ? string.Empty : input.Replace(oldValue, newValue, StringComparison.Ordinal);

	[Node("Substring")]
	[Description("Extracts a substring starting at specified index")]
	public static string Substring(string input, int startIndex, int length = -1)
	{
		if (string.IsNullOrEmpty(input) || startIndex < 0 || startIndex >= input.Length)
		{
			return string.Empty;
		}

		return length < 0 ? input[startIndex..] : input.Substring(startIndex, Math.Min(length, input.Length - startIndex));
	}
}

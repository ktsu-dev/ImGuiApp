// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Collection and array operations as static method nodes.

namespace ktsu.NodeGraph.Library.Operations;
using System;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// Collection operations as static method nodes.
/// </summary>
public static class Collections
{
	[Node("Array Length")]
	[Description("Gets the length of an array")]
	public static int ArrayLength<T>(T[] array) => array?.Length ?? 0;

	[Node("Array Contains")]
	[Description("Checks if array contains a value")]
	public static bool ArrayContains<T>(T[] array, T value) => array?.Contains(value) ?? false;

	[Node("Array Index Of")]
	[Description("Finds the index of a value in an array")]
	public static int ArrayIndexOf<T>(T[] array, T value) => array != null ? Array.IndexOf(array, value) : -1;

	[Node("Array First")]
	[Description("Gets the first element of an array")]
	public static T? ArrayFirst<T>(T[] array) => array != null && array.Length > 0 ? array[0] : default;

	[Node("Array Last")]
	[Description("Gets the last element of an array")]
	public static T? ArrayLast<T>(T[] array) => array != null && array.Length > 0 ? array[^1] : default;

	[Node("Array Slice")]
	[Description("Extracts a portion of an array")]
	public static T[] ArraySlice<T>(T[] array, int start, int length)
	{
		if (array == null || start < 0 || start >= array.Length || length <= 0)
		{
			return [];
		}

		int actualLength = Math.Min(length, array.Length - start);
		T[] result = new T[actualLength];
		Array.Copy(array, start, result, 0, actualLength);
		return result;
	}

	[Node("Range")]
	[Description("Creates an array of integers from start to end")]
	public static int[] Range(int start, int count) =>
		count > 0 ? [.. Enumerable.Range(start, count)] : [];

	[Node("Repeat")]
	[Description("Creates an array with a repeated value")]
	public static T[] Repeat<T>(T value, int count) =>
		count > 0 ? [.. Enumerable.Repeat(value, count)] : [];
}

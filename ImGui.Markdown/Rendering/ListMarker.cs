// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Globalization;

/// <summary>Pure formatting for list item markers.</summary>
internal static class ListMarker
{
	/// <summary>Formats the marker text for a list item.</summary>
	/// <param name="ordered">Whether the enclosing list is ordered.</param>
	/// <param name="itemIndex">The zero-based index of the item within the list.</param>
	/// <param name="startNumber">The ordered list's starting number.</param>
	/// <param name="taskChecked">For task-list items, the checked state; otherwise <see langword="null"/>.</param>
	/// <returns>The marker text, e.g. "-", "3.", "[x]".</returns>
	public static string For(bool ordered, int itemIndex, int startNumber, bool? taskChecked)
	{
		if (taskChecked.HasValue)
		{
			return taskChecked.Value ? "[x]" : "[ ]";
		}

		if (ordered)
		{
			int number = startNumber + itemIndex;
			return number.ToString(CultureInfo.InvariantCulture) + ".";
		}

		return "-";
	}
}

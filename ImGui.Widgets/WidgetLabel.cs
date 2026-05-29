// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Returns the visible portion of an ImGui label: everything before the <c>##</c> ID delimiter.
	/// Returns the whole string when no delimiter is present.
	/// </summary>
	internal static string VisibleLabel(string label)
	{
		int hash = label.IndexOf("##", StringComparison.Ordinal);
		return hash >= 0 ? label[..hash] : label;
	}
}

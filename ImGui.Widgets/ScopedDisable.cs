// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using Hexa.NET.ImGui;

using ScopedAction;

/// <summary>
/// Represents a scoped disable action which will set Dear ImGui elements as functionally and visually disabled until
/// the class is disposed.
/// </summary>
public class ScopedDisable : ScopedAction
{
	/// <summary>
	/// Note as per the Dear ImGui documentation: "Those can be nested, but it cannot
	/// be used to enable an already disabled section (a single BeginDisabled(true)
	/// in the stack is enough to keep everything disabled)"
	/// </summary>
	/// <param name="enabled">Should the elements within the scope be disabled</param>
	public ScopedDisable(bool enabled)
	{
		ImGui.BeginDisabled(enabled);
		OnClose = ImGui.EndDisabled;
	}
}

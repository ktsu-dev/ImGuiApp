// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Monsoon;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>Lays out everything drawn inside it as one item: begins an ImGui group, and ends it when disposed.</summary>
internal sealed class ScopedGroup : ScopedAction
{
	public ScopedGroup()
		: base(ImGui.BeginGroup, ImGui.EndGroup)
	{
	}
}

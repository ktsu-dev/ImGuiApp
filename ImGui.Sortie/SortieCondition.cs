// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

/// <summary>Which state the command display is in, and so which colour leads it.</summary>
public enum SortieCondition
{
	/// <summary>Routine operations: orange leads, red is held back for alerts.</summary>
	Standby,

	/// <summary>An emergency is declared: red leads and the whole display runs hot.</summary>
	Emergency,
}

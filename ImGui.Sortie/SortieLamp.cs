// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

/// <summary>What a <see cref="ImGuiSortie.StatusLamp"/> is reporting.</summary>
public enum SortieLamp
{
	/// <summary>Dark: nothing to report.</summary>
	Off,

	/// <summary>Green: the system is within limits.</summary>
	Nominal,

	/// <summary>Amber: the system needs watching.</summary>
	Caution,

	/// <summary>Red: the system has failed or is out of limits.</summary>
	Alert,
}

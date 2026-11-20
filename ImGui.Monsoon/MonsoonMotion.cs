// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

/// <summary>
/// How much the Monsoon widgets move.
/// </summary>
public enum MonsoonMotion
{
	/// <summary>Rain, flicker and glitch bursts, all running.</summary>
	Full,

	/// <summary>Rain falls slowly; nothing flickers or glitches.</summary>
	Reduced,

	/// <summary>Everything holds still, drawn as it would be at time zero.</summary>
	None,
}

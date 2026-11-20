// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

/// <summary>What a <see cref="MonsoonWidgets.StatusLamp"/> shows.</summary>
public enum MonsoonLampState
{
	/// <summary>Unlit.</summary>
	Off,

	/// <summary>Lit cyan: nominal.</summary>
	Ok,

	/// <summary>Lit amber: attention.</summary>
	Warn,

	/// <summary>Blinking red: something is wrong.</summary>
	Alarm,
}

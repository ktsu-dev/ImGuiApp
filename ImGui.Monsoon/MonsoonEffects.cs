// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// The clock and the motion setting every Monsoon widget animates from.
/// </summary>
public static class MonsoonEffects
{
	/// <summary>
	/// Gets or sets how much the widgets move. <see cref="MonsoonMotion.Full"/> by default.
	/// </summary>
	/// <remarks>
	/// Offer <see cref="MonsoonMotion.Reduced"/> or <see cref="MonsoonMotion.None"/> to anyone who
	/// finds flicker uncomfortable. With <see cref="MonsoonMotion.None"/> nothing requests extra
	/// frames, so an idle application can throttle down as usual.
	/// </remarks>
	public static MonsoonMotion Motion { get; set; } = MonsoonMotion.Full;

	/// <summary>Gets the time the widgets animate from, in seconds; zero when motion is off.</summary>
	public static double Time => Motion == MonsoonMotion.None ? 0d : ImGui.GetTime();

	/// <summary>Gets a value indicating whether flicker and glitch bursts are allowed.</summary>
	internal static bool AllowsFlicker => Motion == MonsoonMotion.Full;

	/// <summary>Gets how fast rain falls relative to full motion.</summary>
	internal static float RainSpeedScale => Motion switch
	{
		MonsoonMotion.Full => 1f,
		MonsoonMotion.Reduced => 0.3f,
		_ => 0f,
	};

	/// <summary>Asks the host for another frame while anything is still moving.</summary>
	internal static void RequestFrame()
	{
		if (Motion != MonsoonMotion.None)
		{
			AnimationActivity.RequestActiveFrame();
		}
	}
}

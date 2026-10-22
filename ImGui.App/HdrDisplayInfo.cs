// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System;

/// <summary>
/// What the screen the window is on can show, as far as the window's presentation reaches it.
/// </summary>
/// <remarks>
/// A value of 1 in a half-float texture is the screen's SDR white, which is where everything ImGui
/// draws is placed. <see cref="Headroom"/> is how far above that the screen goes, so a texture value
/// that decodes to a linear 3 is shown three times as bright as white when the headroom is at least 3,
/// and at the headroom when it is less.
/// </remarks>
/// <param name="IsHdr">Whether the window presents in HDR and the screen shows it.</param>
/// <param name="SdrWhiteNits">The luminance the screen shows SDR white at, in nits.</param>
/// <param name="PeakNits">The brightest the screen goes, in nits.</param>
/// <param name="Headroom">The brightest the screen goes over its SDR white: 1 for an SDR screen.</param>
public sealed record HdrDisplayInfo(bool IsHdr, double SdrWhiteNits, double PeakNits, double Headroom)
{
	/// <summary>The nominal SDR white of a screen that reports none, in nits: sRGB's reference white.</summary>
	public const double ReferenceWhiteNits = 80;

	/// <summary>Gets what an SDR screen shows: white at the reference white and nothing above it.</summary>
	public static HdrDisplayInfo Sdr { get; } = new(false, ReferenceWhiteNits, ReferenceWhiteNits, 1);

	/// <summary>Describes an HDR screen from its SDR white and peak.</summary>
	/// <param name="sdrWhiteNits">The luminance SDR white is shown at, in nits.</param>
	/// <param name="peakNits">The brightest the screen goes, in nits.</param>
	/// <returns>The description, with a headroom of at least 1.</returns>
	public static HdrDisplayInfo FromLuminance(double sdrWhiteNits, double peakNits)
	{
		double white = sdrWhiteNits > 0 && double.IsFinite(sdrWhiteNits) ? sdrWhiteNits : ReferenceWhiteNits;
		double peak = peakNits > white && double.IsFinite(peakNits) ? peakNits : white;
		return new(peak > white, white, peak, peak / white);
	}

	/// <summary>Describes an HDR screen from its headroom alone, as macOS reports one.</summary>
	/// <param name="headroom">The brightest the screen goes over its SDR white.</param>
	/// <param name="sdrWhiteNits">The luminance taken for SDR white, in nits.</param>
	/// <returns>The description.</returns>
	public static HdrDisplayInfo FromHeadroom(double headroom, double sdrWhiteNits) =>
		FromLuminance(sdrWhiteNits, sdrWhiteNits * Math.Max(1, double.IsFinite(headroom) ? headroom : 1));
}

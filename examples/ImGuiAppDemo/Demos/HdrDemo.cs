// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Probes;

/// <summary>
/// Demo for HDR output: what the screen reports, and a half-float ramp running from black past SDR
/// white to the screen's peak, which an HDR screen shows brighter than the interface's white.
/// </summary>
internal sealed class HdrDemo : IDemoTab
{
	private const int RampWidth = 512;
	private const int RampHeight = 32;

	public string TabName => "HDR";

	/// <summary>Gets the display the ramp was last made for, which a test reads.</summary>
	internal HdrDisplayInfo? RampDisplay { get; private set; }

	/// <summary>Gets the ramp's texture, which a test reads.</summary>
	internal ImGuiAppTextureInfo? Ramp { get; private set; }

	/// <summary>Forgets the ramp, since a texture belongs to the renderer that made it and demo tabs outlive a harness.</summary>
	internal void ResetState()
	{
		Ramp = null;
		RampDisplay = null;
	}

	public void Update(float deltaTime)
	{
	}

	public void Render()
	{
		if (DemoProbe.TabItem(TabName))
		{
			HdrDisplayInfo display = ImGuiApp.HdrDisplay;
			ImGui.TextWrapped("The window asks for HDR output with ImGuiAppConfig.HdrOutput. On an HDR screen the ramp below runs past the interface's white to the screen's peak; on an SDR screen it stops at white.");
			ImGui.Text($"HDR output active: {ImGuiApp.IsHdrOutputActive}");
			ImGui.Text($"Screen shows HDR: {display.IsHdr}");
			ImGui.Text($"SDR white: {display.SdrWhiteNits:0} nits, peak: {display.PeakNits:0} nits, headroom: {display.Headroom:0.00}x");

			if (Ramp is null || RampDisplay != display)
			{
				MakeRamp(display);
			}

			ImGui.Image(Ramp!.TextureRef, new Vector2(RampWidth, RampHeight));
			ImGuiProbes.MarkItem("hdr/ramp");
			ImGui.TextDisabled($"Black at the left, SDR white at {100 / display.Headroom:0}% of the way, {display.Headroom:0.00}x white at the right.");
			ImGui.EndTabItem();
		}
	}

	private void MakeRamp(HdrDisplayInfo display)
	{
		Half[] pixels = new Half[RampWidth * RampHeight * 4];
		for (int x = 0; x < RampWidth; x++)
		{
			float linear = (float)(x / (double)(RampWidth - 1) * display.Headroom);
			Half encoded = (Half)HdrEncoding.Encode(linear);
			for (int y = 0; y < RampHeight; y++)
			{
				int index = ((y * RampWidth) + x) * 4;
				pixels[index] = encoded;
				pixels[index + 1] = encoded;
				pixels[index + 2] = encoded;
				pixels[index + 3] = Half.One;
			}
		}

		if (Ramp is null)
		{
			Ramp = ImGuiApp.CreateTexture(pixels, RampWidth, RampHeight);
		}
		else
		{
			ImGuiApp.UpdateTexture(Ramp, pixels, RampWidth, RampHeight);
		}

		RampDisplay = display;
	}
}

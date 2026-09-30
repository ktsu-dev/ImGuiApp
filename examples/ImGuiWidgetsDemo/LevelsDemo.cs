// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Images;
using ktsu.ImGui.Widgets;

/// <summary>Shows a levels control adjusting the ktsu logo, drawn from the logo's own luminance histogram.</summary>
internal static class LevelsDemo
{
	private const int BinCount = 256;
	private const float PreviewSize = 128.0f;

	private static LevelsAdjustment levels = LevelsAdjustment.Identity;
	private static float[]? bins;
	private static ImagePixels? source;
	private static byte[]? adjusted;
	private static ImGuiAppTextureInfo? preview;

	/// <summary>Gets the levels the demo holds.</summary>
	internal static LevelsAdjustment Levels => levels;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness, and a texture does not survive the harness that created it.
	/// </summary>
	internal static void ResetState()
	{
		levels = LevelsAdjustment.Identity;
		bins = null;
		source = null;
		adjusted = null;
		preview = null;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Levels Control"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag the black, grey and white input handles under the histogram, or the output handles");
		ImGui.TextUnformatted("under the ramp. The preview is the logo put through LevelsAdjustment.Apply.");
		ImGui.Separator();

		EnsureLoaded();

		if (ImGuiWidgets.LevelsControl("levels_demo", bins, 1, ref levels, new Vector2(400.0f, 140.0f)))
		{
			ApplyLevels();
		}

		if (DemoProbe.Button("Reset levels"))
		{
			levels = LevelsAdjustment.Identity;
			ApplyLevels();
		}

		if (preview is not null)
		{
			ImGui.Image(preview.TextureRef, new Vector2(PreviewSize, PreviewSize));
		}
	}

	private static void EnsureLoaded()
	{
		if (source is null)
		{
			source = ImageDecoder.Load(DemoContext.KtsuIconPath);
			bins = BuildLuminanceHistogram(source.ReadOnlyPixels);
			adjusted = (byte[])source.GetBuffer().Clone();
		}

		preview ??= ImGuiApp.CreateTexture(adjusted, source.Width, source.Height);
	}

	private static float[] BuildLuminanceHistogram(ReadOnlySpan<byte> rgba)
	{
		float[] histogram = new float[BinCount];
		for (int i = 0; i + 3 < rgba.Length; i += 4)
		{
			if (rgba[i + 3] == 0)
			{
				continue;
			}

			float luminance = (0.2126f * rgba[i]) + (0.7152f * rgba[i + 1]) + (0.0722f * rgba[i + 2]);
			int bin = Math.Clamp((int)MathF.Round(luminance), 0, BinCount - 1);
			histogram[bin]++;
		}

		return histogram;
	}

	private static void ApplyLevels()
	{
		if (source is null || adjusted is null || preview is null)
		{
			return;
		}

		ReadOnlySpan<byte> original = source.ReadOnlyPixels;
		for (int i = 0; i + 3 < original.Length; i += 4)
		{
			adjusted[i] = ToByte(levels.Apply(original[i] / 255.0f));
			adjusted[i + 1] = ToByte(levels.Apply(original[i + 1] / 255.0f));
			adjusted[i + 2] = ToByte(levels.Apply(original[i + 2] / 255.0f));
			adjusted[i + 3] = original[i + 3];
		}

		ImGuiApp.UpdateTexture(preview, adjusted, source.Width, source.Height);
	}

	private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255.0f), 0, 255);
}

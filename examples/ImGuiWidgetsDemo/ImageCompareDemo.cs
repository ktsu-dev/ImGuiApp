// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Images;
using ktsu.ImGui.Widgets;

/// <summary>
/// The before/after comparison, over the ktsu logo and a colour-inverted copy of it.
/// </summary>
internal static class ImageCompareDemo
{
	private static readonly ImGuiWidgets.ImageCanvasState state = new();
	private static ImGuiAppTextureInfo? inverted;
	private static float split = 0.5f;
	private static ImGuiWidgets.ImageCompareMode mode = ImGuiWidgets.ImageCompareMode.Wipe;

	/// <summary>Gets the divider's position, for tests.</summary>
	internal static float Split => split;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		split = 0.5f;
		mode = ImGuiWidgets.ImageCompareMode.Wipe;
		state.ResetToActualSize();

		// A texture belongs to the harness that created it, so the next one builds its own.
		inverted = null;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Image Compare"))
		{
			ImGui.TextUnformatted("Before and after in one view: drag the divider to wipe, drag elsewhere to pan, scroll to zoom:");
			ImGui.Separator();

			ImGuiAppTextureInfo before = DemoContext.KtsuTexture;
			inverted ??= CreateInverted();
			Vector2 imageSize = new(before.Width, before.Height);
			Vector2 canvasSize = new(ImGui.GetContentRegionAvail().X, 300f);

			if (ImGui.RadioButton("Wipe", mode == ImGuiWidgets.ImageCompareMode.Wipe))
			{
				mode = ImGuiWidgets.ImageCompareMode.Wipe;
			}

			ImGui.SameLine();
			if (ImGui.RadioButton("Side by side", mode == ImGuiWidgets.ImageCompareMode.SideBySide))
			{
				mode = ImGuiWidgets.ImageCompareMode.SideBySide;
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Fit##compare"))
			{
				Vector2 viewport = mode == ImGuiWidgets.ImageCompareMode.Wipe ? canvasSize : new Vector2((canvasSize.X - 4f) / 2f, canvasSize.Y);
				state.FitToViewport(imageSize, viewport);
			}

			ImGuiWidgets.ImageCompare("compare_demo", before.TextureId, inverted.TextureId, imageSize, state, ref split, canvasSize, mode);
			ImGui.TextUnformatted($"Split: {split:0.00}, zoom: {state.Zoom:0.##}x");
		}
	}

	private static ImGuiAppTextureInfo CreateInverted()
	{
		ImagePixels image = ImageDecoder.Load(DemoContext.KtsuIconPath);
		Span<byte> pixels = image.Pixels;
		for (int i = 0; i < pixels.Length; i += 4)
		{
			pixels[i] = (byte)(255 - pixels[i]);
			pixels[i + 1] = (byte)(255 - pixels[i + 1]);
			pixels[i + 2] = (byte)(255 - pixels[i + 2]);
		}

		return ImGuiApp.CreateTexture(pixels, image.Width, image.Height);
	}
}

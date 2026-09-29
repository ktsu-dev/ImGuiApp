// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery;

using System;

using ktsu.ImGui.App;

/// <summary>A generated picture for the widgets that display an image.</summary>
internal static class SampleImage
{
	/// <summary>The width and height of the generated image, in pixels.</summary>
	public const int Size = 128;

	/// <summary>
	/// Uploads a sunset-like gradient with a sun disc in it: recognisably a picture at thumbnail
	/// size, and asymmetric enough that a flipped or cropped image looks wrong.
	/// </summary>
	/// <returns>The uploaded texture.</returns>
	public static ImGuiAppTextureInfo Create()
	{
		byte[] rgba = new byte[Size * Size * 4];

		for (int y = 0; y < Size; y++)
		{
			for (int x = 0; x < Size; x++)
			{
				float u = x / (float)(Size - 1);
				float v = y / (float)(Size - 1);

				// Sky from deep blue at the top to warm orange at the horizon, ground below it.
				float r;
				float g;
				float b;

				if (v < 0.68f)
				{
					float t = v / 0.68f;
					r = Lerp(0.16f, 0.98f, t);
					g = Lerp(0.20f, 0.55f, t);
					b = Lerp(0.45f, 0.30f, t);
				}
				else
				{
					float t = (v - 0.68f) / 0.32f;
					r = Lerp(0.20f, 0.08f, t);
					g = Lerp(0.30f, 0.16f, t);
					b = Lerp(0.22f, 0.10f, t);
				}

				// The sun, a little left of centre and sitting on the horizon.
				float dx = u - 0.38f;
				float dy = v - 0.60f;
				float distance = MathF.Sqrt((dx * dx) + (dy * dy));
				float sun = Math.Clamp((0.17f - distance) / 0.02f, 0f, 1f) * (v < 0.68f ? 1f : 0f);
				r = Lerp(r, 1.00f, sun);
				g = Lerp(g, 0.90f, sun);
				b = Lerp(b, 0.55f, sun);

				int i = ((y * Size) + x) * 4;
				rgba[i + 0] = ToByte(r);
				rgba[i + 1] = ToByte(g);
				rgba[i + 2] = ToByte(b);
				rgba[i + 3] = 255;
			}
		}

		return ImGuiApp.CreateTexture(rgba, Size, Size);
	}

	private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

	private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
}

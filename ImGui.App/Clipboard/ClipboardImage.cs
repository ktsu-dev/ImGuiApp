// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using ktsu.ImGui.App.Images;

/// <summary>An image on its way to the clipboard, in the forms the platforms want it.</summary>
/// <remarks>
/// Both forms, because Windows applications differ in which they read: browsers and Office read the
/// registered PNG format and keep transparency, while older ones read only a device-independent bitmap.
/// An application with its own PNG encoder builds one directly; <see cref="FromPixels"/> encodes one here.
/// </remarks>
/// <param name="Png">The image as a PNG file.</param>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
/// <param name="Bgra">The pixels as 8-bit sRGB, blue, green, red and straight alpha, top row first.</param>
public sealed record ClipboardImage(ReadOnlyMemory<byte> Png, int Width, int Height, ReadOnlyMemory<byte> Bgra)
{
	/// <summary>Builds a clipboard image from decoded pixels, encoding the PNG.</summary>
	/// <param name="pixels">The pixels, straight-alpha RGBA8.</param>
	/// <returns>The image.</returns>
	public static ClipboardImage FromPixels(ImagePixels pixels)
	{
		Ensure.NotNull(pixels);
		ReadOnlySpan<byte> rgba = pixels.ReadOnlyPixels;
		byte[] bgra = new byte[rgba.Length];
		for (int i = 0; i < rgba.Length; i += 4)
		{
			bgra[i] = rgba[i + 2];
			bgra[i + 1] = rgba[i + 1];
			bgra[i + 2] = rgba[i];
			bgra[i + 3] = rgba[i + 3];
		}

		return new ClipboardImage(PngEncoder.Encode(pixels), pixels.Width, pixels.Height, bgra);
	}
}

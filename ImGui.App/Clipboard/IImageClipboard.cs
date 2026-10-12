// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

/// <summary>Reads an image from the system clipboard and puts one on it.</summary>
/// <remarks>
/// GLFW's and SDL's clipboards carry text only, so each platform reaches its own: the Win32 clipboard,
/// the Wayland and X11 command-line tools, and the macOS pasteboard through AppleScript. The image
/// travels as an encoded file rather than as pixels, so a reader only needs a codec to know what it
/// holds; <see cref="ImageClipboard.TryReadPixels"/> decodes it with <see cref="Images.ImageDecoder"/>.
/// Get the platform's clipboard from <see cref="ImageClipboard.ForThisPlatform"/>; an application's
/// tests can stand in their own implementation.
/// </remarks>
public interface IImageClipboard
{
	/// <summary>Reads the image on the clipboard.</summary>
	/// <param name="image">The encoded image (a PNG, JPEG or BMP file's bytes) when there is one.</param>
	/// <param name="message">Why there isn't one: the clipboard holds no image, or it can't be reached.</param>
	/// <returns>Whether an image was read.</returns>
	public bool TryRead(out byte[]? image, out string? message);

	/// <summary>Replaces what the clipboard holds with an image.</summary>
	/// <param name="image">The image, as a PNG and as top-down straight-alpha BGRA rows.</param>
	/// <param name="message">Why it couldn't be written, when it couldn't.</param>
	/// <returns>Whether the clipboard now holds the image.</returns>
	public bool TryWrite(ClipboardImage image, out string? message);
}

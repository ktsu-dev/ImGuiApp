// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using ktsu.ImGui.App.Images;

/// <summary>The image clipboard for the platform the application runs on, and helpers over any <see cref="IImageClipboard"/>.</summary>
/// <remarks>
/// <para>
/// Windows reads the registered <c>PNG</c> format, then <c>CF_DIBV5</c> and <c>CF_DIB</c>, and writes PNG and
/// <c>CF_DIB</c>. Linux and FreeBSD run <c>wl-paste</c>/<c>wl-copy</c> under Wayland and <c>xclip</c> under
/// X11, and macOS runs <c>osascript</c>; each tool is found in the system's own folders rather than on the
/// path, and every run is stopped after a few seconds. Anywhere else, every call fails with a message.
/// </para>
/// <para>
/// Reading starts a short-lived process everywhere but Windows, so it takes a moment; an application can
/// call it on the render thread, as a paste usually wants, or on a worker.
/// </para>
/// </remarks>
public static class ImageClipboard
{
	/// <summary>Said when the clipboard holds something that isn't an image.</summary>
	public const string NoImageMessage = "The clipboard doesn't hold an image.";

	/// <summary>Said on Linux when neither wl-clipboard nor xclip is installed.</summary>
	public const string NoToolMessage = "Copying and pasting images needs wl-clipboard (on Wayland) or xclip (on X11). Install one with your package manager.";

	/// <summary>Gets the image clipboard for this platform.</summary>
	/// <returns>The clipboard.</returns>
	public static IImageClipboard ForThisPlatform()
	{
#if IOS
		return new UnsupportedImageClipboard();
#else
		if (OperatingSystem.IsWindows())
		{
			return new WindowsImageClipboard();
		}

		SystemClipboardProcesses processes = new();
		if (OperatingSystem.IsMacOS())
		{
			return new MacImageClipboard(processes);
		}

		return OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()
			? LinuxImageClipboard.ForThisSession(processes)
			: new UnsupportedImageClipboard();
#endif
	}

	/// <summary>Reads the clipboard's image and decodes it.</summary>
	/// <param name="clipboard">The clipboard.</param>
	/// <param name="pixels">The decoded image, straight-alpha RGBA8, when there is one.</param>
	/// <param name="message">Why there isn't one: no image, an unreachable clipboard, or an image <see cref="ImageDecoder"/> can't read.</param>
	/// <returns>Whether an image was read and decoded.</returns>
	public static bool TryReadPixels(this IImageClipboard clipboard, out ImagePixels? pixels, out string? message)
	{
		Ensure.NotNull(clipboard);
		pixels = null;
		if (!clipboard.TryRead(out byte[]? image, out message) || image is null)
		{
			message ??= NoImageMessage;
			return false;
		}

		try
		{
			pixels = ImageDecoder.Decode(image);
			return true;
		}
		catch (InvalidImageDataException exception)
		{
			message = $"The clipboard's image couldn't be read: {exception.Message}";
			return false;
		}
	}

	/// <summary>Puts decoded pixels on the clipboard, as a PNG and as a bitmap.</summary>
	/// <param name="clipboard">The clipboard.</param>
	/// <param name="pixels">The image, straight-alpha RGBA8.</param>
	/// <param name="message">Why it couldn't be written, when it couldn't.</param>
	/// <returns>Whether the clipboard now holds the image.</returns>
	public static bool TryWritePixels(this IImageClipboard clipboard, ImagePixels pixels, out string? message)
	{
		Ensure.NotNull(clipboard);
		return clipboard.TryWrite(ClipboardImage.FromPixels(pixels), out message);
	}
}

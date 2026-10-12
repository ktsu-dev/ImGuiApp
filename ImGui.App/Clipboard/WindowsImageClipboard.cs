// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using static ktsu.ImGui.App.Clipboard.NativeMethods;

/// <summary>The Win32 clipboard.</summary>
/// <remarks>
/// Reads the registered <c>PNG</c> format first, which browsers, Office and most editors offer and which
/// keeps transparency, then <c>CF_DIBV5</c> and <c>CF_DIB</c>, which Windows synthesizes from each other
/// and from any bitmap an older application copies. Writes both PNG and <c>CF_DIB</c>.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class WindowsImageClipboard : IImageClipboard
{
	// Another application can hold the clipboard open for a moment; asking again shortly is the usual remedy.
	private const int OpenAttempts = 10;

	private static readonly TimeSpan OpenRetryDelay = TimeSpan.FromMilliseconds(20);

	private readonly uint pngFormat = RegisterClipboardFormat("PNG");

	/// <inheritdoc />
	public bool TryRead(out byte[]? image, out string? message)
	{
		image = null;
		if (!Open())
		{
			message = "The clipboard is in use by another application. Try again in a moment.";
			return false;
		}

		try
		{
			if (pngFormat != 0 && Read(pngFormat) is byte[] png)
			{
				image = png;
			}
			else if (Read(DibV5Format) is byte[] v5)
			{
				image = DeviceIndependentBitmap.ToBitmapFile(v5);
			}
			else if (Read(DibFormat) is byte[] dib)
			{
				image = DeviceIndependentBitmap.ToBitmapFile(dib);
			}
		}
		finally
		{
			_ = CloseClipboard();
		}

		message = image is null ? ImageClipboard.NoImageMessage : null;
		return image is not null;
	}

	/// <inheritdoc />
	public bool TryWrite(ClipboardImage image, out string? message)
	{
		Ensure.NotNull(image);
		if (!Open())
		{
			message = "The clipboard is in use by another application. Try again in a moment.";
			return false;
		}

		try
		{
			_ = EmptyClipboard();
			bool written = pngFormat != 0 && Write(pngFormat, image.Png.ToArray());
			written |= Write(DibFormat, DeviceIndependentBitmap.FromBgra(image.Width, image.Height, image.Bgra.Span));
			message = written ? null : "The image couldn't be put on the clipboard.";
			return written;
		}
		finally
		{
			_ = CloseClipboard();
		}
	}

	private static bool Open()
	{
		for (int attempt = 0; attempt < OpenAttempts; attempt++)
		{
			if (OpenClipboard(0))
			{
				return true;
			}

			Thread.Sleep(OpenRetryDelay);
		}

		return false;
	}

	private static byte[]? Read(uint format)
	{
		if (!IsClipboardFormatAvailable(format))
		{
			return null;
		}

		nint handle = GetClipboardData(format);
		if (handle == 0)
		{
			return null;
		}

		nuint size = GlobalSize(handle);
		nint data = GlobalLock(handle);
		if (data == 0 || size == 0 || size > int.MaxValue)
		{
			return null;
		}

		try
		{
			byte[] bytes = new byte[(int)size];
			Marshal.Copy(data, bytes, 0, bytes.Length);
			return bytes;
		}
		finally
		{
			_ = GlobalUnlock(handle);
		}
	}

	private static bool Write(uint format, byte[] bytes)
	{
		nint handle = GlobalAlloc(Moveable, (nuint)bytes.Length);
		if (handle == 0)
		{
			return false;
		}

		nint data = GlobalLock(handle);
		if (data == 0)
		{
			_ = GlobalFree(handle);
			return false;
		}

		Marshal.Copy(bytes, 0, data, bytes.Length);
		_ = GlobalUnlock(handle);

		// On success the clipboard owns the memory; only a refusal leaves it ours to free.
		if (SetClipboardData(format, handle) == 0)
		{
			_ = GlobalFree(handle);
			return false;
		}

		return true;
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

/// <summary>The clipboard on a platform with no image clipboard this library can reach.</summary>
internal sealed class UnsupportedImageClipboard : IImageClipboard
{
	private const string Message = "Copying and pasting images isn't supported on this platform.";

	/// <inheritdoc />
	public bool TryRead(out byte[]? image, out string? message)
	{
		image = null;
		message = Message;
		return false;
	}

	/// <inheritdoc />
	public bool TryWrite(ClipboardImage image, out string? message)
	{
		message = Message;
		return false;
	}
}

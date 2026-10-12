// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

/// <summary>The macOS pasteboard, reached through AppleScript.</summary>
/// <remarks>
/// <c>osascript</c> ships with every macOS and reads and writes the general pasteboard's PNG flavour,
/// which screenshots, browsers and image editors all put there, without native code here. AppleScript
/// can't hand binary data to its standard output, so the image goes through a temporary file.
/// </remarks>
/// <param name="processes">Runs <c>osascript</c>.</param>
internal sealed class MacImageClipboard(IClipboardProcesses processes) : IImageClipboard
{
	private const string Program = "osascript";

	/// <inheritdoc />
	public bool TryRead(out byte[]? image, out string? message)
	{
		image = null;
		string path = TemporaryPath();
		try
		{
			ClipboardProcessResult? result = processes.Run(Program, ReadArguments(path), null, captureOutput: false);
			if (result is null)
			{
				message = "The clipboard couldn't be reached: osascript couldn't be started.";
				return false;
			}

			// Real disk: osascript reads and writes these temporary files.
			if (result.ExitCode != 0 || !File.Exists(path) || new FileInfo(path).Length == 0)
			{
				message = ImageClipboard.NoImageMessage;
				return false;
			}

			// Real disk: osascript reads and writes these temporary files.
			image = File.ReadAllBytes(path);
			message = null;
			return true;
		}
		finally
		{
			File.Delete(path);
		}
	}

	/// <inheritdoc />
	public bool TryWrite(ClipboardImage image, out string? message)
	{
		Ensure.NotNull(image);
		string path = TemporaryPath();
		try
		{
			// Real disk: osascript reads and writes these temporary files.
			File.WriteAllBytes(path, image.Png.ToArray());
			ClipboardProcessResult? result = processes.Run(Program, WriteArguments(path), null, captureOutput: false);
			message = result is { ExitCode: 0 } ? null : "The image couldn't be put on the clipboard.";
			return message is null;
		}
		finally
		{
			File.Delete(path);
		}
	}

	/// <summary>The script that writes the pasteboard's PNG to <paramref name="path"/>, failing when there is none.</summary>
	internal static string[] ReadArguments(string path) =>
	[
		"-e", "set png to (the clipboard as «class PNGf»)",
		"-e", $"set target to open for access (POSIX file \"{Escape(path)}\") with write permission",
		"-e", "set eof target to 0",
		"-e", "write png to target",
		"-e", "close access target",
	];

	/// <summary>The script that puts the PNG at <paramref name="path"/> on the pasteboard.</summary>
	internal static string[] WriteArguments(string path) =>
		["-e", $"set the clipboard to (read (POSIX file \"{Escape(path)}\") as «class PNGf»)"];

	// The temporary directory can contain anything a user's name can; inside an AppleScript string only
	// the quote and the backslash mean something.
	private static string Escape(string path) => path.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

	private static string TemporaryPath() => Path.Join(Path.GetTempPath(), $"imgui-clipboard-{Guid.NewGuid():N}.png");
}

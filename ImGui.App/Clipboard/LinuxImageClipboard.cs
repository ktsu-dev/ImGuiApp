// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.Text;

/// <summary>The clipboard on Linux, through <c>wl-paste</c>/<c>wl-copy</c> under Wayland and <c>xclip</c> under X11.</summary>
/// <remarks>
/// Neither display server lets a client read or offer a clipboard without running an event loop GLFW
/// doesn't expose, so the standard command-line tools do it. A Wayland session prefers wl-clipboard and
/// falls back to <c>xclip</c>, which reaches XWayland's clipboard that most Wayland compositors keep in
/// step with their own.
/// </remarks>
/// <param name="processes">Runs the tools.</param>
/// <param name="wayland">Whether the session is a Wayland one.</param>
internal sealed class LinuxImageClipboard(IClipboardProcesses processes, bool wayland) : IImageClipboard
{
	private const string Png = "image/png";

	/// <summary>The image types <see cref="Images.ImageDecoder"/> can read, best first: lossless before lossy.</summary>
	/// <remarks>WebP, GIF and the rest are left out, since picking one would fail to decode even when a readable type was on offer.</remarks>
	private static readonly string[] DecodableTypes = [Png, "image/bmp", "image/x-bmp", "image/jpeg"];

	/// <summary>Gets the clipboard for this session, Wayland or not as its environment says.</summary>
	/// <param name="processes">Runs the tools.</param>
	/// <returns>The clipboard.</returns>
	public static LinuxImageClipboard ForThisSession(IClipboardProcesses processes) =>
		new(processes, !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")));

	/// <inheritdoc />
	public bool TryRead(out byte[]? image, out string? message)
	{
		image = null;
		bool anyTool = false;
		foreach (Tool tool in Tools())
		{
			ClipboardProcessResult? types = processes.Run(tool.Program, tool.ListTypes, null, captureOutput: true);
			if (types is null)
			{
				continue;
			}

			anyTool = true;
			if (types.ExitCode != 0 || PickType(Encoding.UTF8.GetString(types.Output)) is not string type)
			{
				continue;
			}

			ClipboardProcessResult? read = processes.Run(tool.Program, tool.Read(type), null, captureOutput: true);
			if (read is { ExitCode: 0, Output.Length: > 0 })
			{
				image = read.Output;
				message = null;
				return true;
			}
		}

		message = anyTool ? ImageClipboard.NoImageMessage : ImageClipboard.NoToolMessage;
		return false;
	}

	/// <inheritdoc />
	public bool TryWrite(ClipboardImage image, out string? message)
	{
		Ensure.NotNull(image);
		byte[] png = image.Png.ToArray();
		bool anyTool = false;
		foreach (Tool tool in Tools())
		{
			ClipboardProcessResult? written = processes.Run(tool.WriteProgram, tool.Write, png, captureOutput: false);
			if (written is null)
			{
				continue;
			}

			anyTool = true;
			if (written.ExitCode == 0)
			{
				message = null;
				return true;
			}
		}

		message = anyTool ? "The image couldn't be put on the clipboard." : ImageClipboard.NoToolMessage;
		return false;
	}

	/// <summary>Picks the image type to read from a tool's list of what the clipboard offers: PNG, then BMP, then JPEG.</summary>
	/// <param name="types">The list, one type per line.</param>
	/// <returns>The type as the list spells it, or null when nothing on offer is an image the decoder can read.</returns>
	internal static string? PickType(string types)
	{
		string[] offered = [.. types.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
		return DecodableTypes
			.Select(wanted => offered.FirstOrDefault(type => type.Equals(wanted, StringComparison.OrdinalIgnoreCase)))
			.FirstOrDefault(static type => type is not null);
	}

	private IEnumerable<Tool> Tools()
	{
		if (wayland)
		{
			yield return new Tool(
				"wl-paste",
				["--list-types"],
				static type => ["--no-newline", "--type", type],
				"wl-copy",
				["--type", Png]);
		}

		yield return new Tool(
			"xclip",
			["-selection", "clipboard", "-target", "TARGETS", "-out"],
			static type => ["-selection", "clipboard", "-target", type, "-out"],
			"xclip",
			["-selection", "clipboard", "-target", Png, "-in"]);
	}

	/// <summary>One tool's commands. Reading and writing are separate programs under Wayland.</summary>
	private sealed record Tool(string Program, string[] ListTypes, Func<string, string[]> Read, string WriteProgram, string[] Write);
}

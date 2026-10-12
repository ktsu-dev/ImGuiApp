// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

/// <summary>Runs the command-line tools a platform's clipboard is reached through.</summary>
/// <remarks>An interface so tests can stand in for tools the machine running them doesn't have.</remarks>
internal interface IClipboardProcesses
{
	/// <summary>Runs a program and waits for it to finish.</summary>
	/// <param name="program">The program's file name, looked for in the system's own folders.</param>
	/// <param name="arguments">Its arguments, passed exactly.</param>
	/// <param name="input">What to write to its standard input, or null for nothing.</param>
	/// <param name="captureOutput">
	/// Whether to read its standard output. Off when writing to the clipboard: <c>xclip</c> and
	/// <c>wl-copy</c> leave a child behind to serve the clipboard, which holds a redirected output open
	/// for as long as it serves, so reading it would wait until something else is copied.
	/// </param>
	/// <returns>How it ended, or null when it couldn't be started, which usually means it isn't installed.</returns>
	public ClipboardProcessResult? Run(string program, IReadOnlyList<string> arguments, byte[]? input, bool captureOutput);
}

/// <summary>How a clipboard tool ended.</summary>
/// <param name="ExitCode">Its exit code, or -1 when it was stopped for taking too long.</param>
/// <param name="Output">What it wrote to standard output, when that was read.</param>
internal sealed record ClipboardProcessResult(int ExitCode, byte[] Output);

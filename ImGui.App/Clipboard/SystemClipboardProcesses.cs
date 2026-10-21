// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>Runs clipboard tools as real processes.</summary>
/// <remarks>
/// <para>
/// Run on the render thread for a paste, so every run is bounded: a tool that hasn't finished in a few
/// seconds is stopped and reported as having failed, rather than leaving the window frozen.
/// </para>
/// <para>
/// A tool is found in the system's own folders rather than on the path, as the file type registrar
/// finds its tools, so nothing earlier on someone's path can stand in for it.
/// </para>
/// </remarks>
internal sealed class SystemClipboardProcesses : IClipboardProcesses
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

	private static readonly string[] ToolDirectories = ["/usr/bin", "/usr/local/bin", "/bin", "/opt/homebrew/bin"];

	/// <inheritdoc />
	public ClipboardProcessResult? Run(string program, IReadOnlyList<string> arguments, byte[]? input, bool captureOutput)
	{
		// Real disk: looks for an operating system tool to run.
		string? path = ToolDirectories.Select(directory => Path.Join(directory, program)).FirstOrDefault(File.Exists);
		if (path is null)
		{
			return null;
		}

		ProcessStartInfo start = new(path)
		{
			UseShellExecute = false,
			RedirectStandardInput = input is not null,

			// Always redirected, never inherited: xclip leaves a child serving the clipboard that keeps
			// whatever standard output and error it was given, so an inherited one would hold the
			// application's own open, and a script reading it would wait until something else is copied.
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};
		foreach (string argument in arguments)
		{
			start.ArgumentList.Add(argument);
		}

		Process? process;
		try
		{
			process = Process.Start(start);
		}
		catch (Win32Exception)
		{
			return null;
		}

		if (process is null)
		{
			return null;
		}

		using (process)
		{
			Task<byte[]> output = captureOutput ? ReadAllAsync(process.StandardOutput.BaseStream) : Task.FromResult(Array.Empty<byte>());
			if (captureOutput)
			{
				// A tool's complaints are not the application's to print; the exit code is what counts.
				_ = ReadAllAsync(process.StandardError.BaseStream);
			}
			else
			{
				// Nothing is read from a write, and the child left serving the clipboard would hold both
				// pipes open for as long as it serves, so let go of them now.
				process.StandardOutput.Dispose();
				process.StandardError.Dispose();
			}

			if (input is not null)
			{
				try
				{
					using Stream stdin = process.StandardInput.BaseStream;
					stdin.Write(input);
				}
				catch (IOException)
				{
					// It exited without reading everything; its exit code says why.
				}
			}

			if (!process.WaitForExit(Timeout) || !output.Wait(Timeout))
			{
				Kill(process);
				return new ClipboardProcessResult(-1, []);
			}

			return new ClipboardProcessResult(process.ExitCode, output.Result);
		}
	}

	private static async Task<byte[]> ReadAllAsync(Stream stream)
	{
		using MemoryStream buffer = new();
		await stream.CopyToAsync(buffer).ConfigureAwait(false);
		return buffer.ToArray();
	}

	private static void Kill(Process process)
	{
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch (InvalidOperationException)
		{
			// It finished on its own in the meantime.
		}
		catch (Win32Exception)
		{
			// It can't be stopped from here; there is nothing more to do about it.
		}
	}
}

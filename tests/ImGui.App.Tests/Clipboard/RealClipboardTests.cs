// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Clipboard;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using ktsu.ImGui.App.Clipboard;
using ktsu.ImGui.App.Images;
using ktsu.ImGui.App.Tests.Images;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The image clipboard against the desktop's real one, with another program on the other end: <c>xclip</c>
/// under X11, wl-clipboard under Wayland, PowerShell's Windows Forms clipboard on Windows, and AppleScript
/// with <c>sips</c> on macOS.
/// </summary>
/// <remarks>
/// <para>
/// Opt-in through <c>IMGUI_REAL_CLIPBOARD=1</c>, because they overwrite whatever the person running the
/// suite last copied. <c>.github/workflows/real-clipboard.yml</c> runs them on every desktop: X11 under
/// Xvfb and Wayland under a headless sway on Linux, and the hosted Windows and macOS runners' own sessions.
/// Without the variable they are inconclusive, which the runner reports as skipped.
/// </para>
/// <para>
/// The other program never shares a process with the clipboard under test, which is the point: a
/// clipboard that only ever reads back what it wrote itself can agree with itself about a format nobody
/// else uses.
/// </para>
/// </remarks>
[TestClass]
[TestCategory("RealClipboard")]
public sealed class RealClipboardTests
{
	private const string OptIn = "IMGUI_REAL_CLIPBOARD";

	private const string OtherProgramChoice = "IMGUI_REAL_CLIPBOARD_OTHER";

	private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(20);

	private string directory = string.Empty;

	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		if (Environment.GetEnvironmentVariable(OptIn) != "1")
		{
			Assert.Inconclusive($"Set {OptIn}=1 to run these against the desktop's real clipboard; they replace what is on it.");
		}

		directory = Path.Join(Path.GetTempPath(), $"imgui-real-clipboard-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
	}

	[TestCleanup]
	public void TearDown()
	{
		if (directory.Length > 0 && Directory.Exists(directory))
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	/// <summary>Six distinct opaque colours over three columns and two rows, so a flipped, mirrored or channel-swapped read shows.</summary>
	private static ImagePixels Opaque() => new(3, 2,
	[
		255, 0, 0, 255, /**/ 0, 255, 0, 255, /**/ 0, 0, 255, 255,
		255, 255, 0, 255, /**/ 0, 255, 255, 255, /**/ 255, 0, 255, 255,
	]);

	/// <summary>The same with alpha that varies, which only a PNG on the clipboard keeps.</summary>
	private static ImagePixels Translucent() => new(2, 2,
	[
		255, 0, 0, 255, /**/ 0, 255, 0, 128,
		0, 0, 255, 64, /**/ 255, 255, 255, 0,
	]);

	[TestMethod]
	public void WhatThisClipboardWrites_ItReadsBack()
	{
		IImageClipboard clipboard = ImageClipboard.ForThisPlatform();
		ImagePixels written = Translucent();

		Assert.IsTrue(clipboard.TryWritePixels(written, out string? writeMessage), writeMessage);
		Assert.IsTrue(clipboard.TryReadPixels(out ImagePixels? read, out string? readMessage), readMessage);

		AssertSamePixels(written, read!, compareTransparentColor: false);
	}

	[TestMethod]
	public void WritingReturnsPromptly_AndTheImageOutlivesTheWrite()
	{
		IImageClipboard clipboard = ImageClipboard.ForThisPlatform();
		Stopwatch elapsed = Stopwatch.StartNew();

		Assert.IsTrue(clipboard.TryWritePixels(Opaque(), out string? message), message);
		Assert.IsLessThan(TimeSpan.FromSeconds(4), elapsed.Elapsed, "Writing waited on the tool left serving the clipboard.");

		// The tool that serves the clipboard on Linux has to keep serving it once the write has returned.
		System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
		Assert.IsTrue(clipboard.TryReadPixels(out ImagePixels? read, out string? readMessage), readMessage);
		AssertSamePixels(Opaque(), read!, compareTransparentColor: true);
	}

	[TestMethod]
	public void TheToolLeftServingTheClipboard_HoldsNoneOfThisProcesssOutput()
	{
		if (!OperatingSystem.IsLinux())
		{
			Assert.Inconclusive("Only Linux leaves a tool serving the clipboard.");
		}

		Assert.IsTrue(ImageClipboard.ForThisPlatform().TryWritePixels(Opaque(), out string? message), message);

		// A child holding this process's standard output keeps a script reading it waiting until
		// something else is copied, which is what xclip did before it was given pipes of its own.
		string[] ours = [Target("/proc/self/fd/1"), Target("/proc/self/fd/2")];
		string[] held = [.. Directory.EnumerateDirectories("/proc")
			.Where(path => int.TryParse(Path.GetFileName(path), out _))
			.Where(path => File.Exists(Path.Join(path, "comm")) && Read(Path.Join(path, "comm")) is "xclip" or "wl-copy")
			.SelectMany(path => new[] { Target(Path.Join(path, "fd", "1")), Target(Path.Join(path, "fd", "2")) })
			.Where(target => target.Length > 0 && ours.Contains(target))];

		Assert.IsEmpty(held, $"The tool serving the clipboard holds this process's output: {string.Join(", ", held)}");

		static string Read(string path)
		{
			try
			{
				return File.ReadAllText(path).Trim();
			}
			catch (IOException)
			{
				return string.Empty;
			}
			catch (UnauthorizedAccessException)
			{
				return string.Empty;
			}
		}

		static string Target(string link)
		{
			try
			{
				return new FileInfo(link).LinkTarget ?? string.Empty;
			}
			catch (IOException)
			{
				return string.Empty;
			}
			catch (UnauthorizedAccessException)
			{
				return string.Empty;
			}
		}
	}

	[TestMethod]
	public void WhatThisClipboardWrites_AnotherProgramReadsAsAPng()
	{
		Assert.IsTrue(ImageClipboard.ForThisPlatform().TryWritePixels(Translucent(), out string? message), message);

		byte[] png = OtherProgram().ReadPng(directory);

		AssertSamePixels(Translucent(), ImageDecoder.Decode(png), compareTransparentColor: false);
	}

	[TestMethod]
	public void APngAnotherProgramWrites_IsRead()
	{
		ImagePixels expected = Translucent();
		string path = Path.Join(directory, "other.png");
		File.WriteAllBytes(path, ClipboardImage.FromPixels(expected).Png.ToArray());
		OtherProgram().WritePng(path);

		Assert.IsTrue(ImageClipboard.ForThisPlatform().TryReadPixels(out ImagePixels? read, out string? message), message);

		AssertSamePixels(expected, read!, compareTransparentColor: false);
	}

	[TestMethod]
	public void ABitmapAnotherProgramWrites_IsRead()
	{
		// What an editor that offers no PNG leaves: a BMP on Linux, a DIB on Windows, a TIFF on macOS.
		string path = Path.Join(directory, "other.png");
		File.WriteAllBytes(path, ClipboardImage.FromPixels(Opaque()).Png.ToArray());
		OtherProgram().WriteBitmapOnly(path, directory);

		Assert.IsTrue(ImageClipboard.ForThisPlatform().TryReadPixels(out ImagePixels? read, out string? message), message);

		AssertSamePixels(Opaque(), read!, compareTransparentColor: true);
	}

	[TestMethod]
	public void TextAnotherProgramWrites_IsNoImage()
	{
		OtherProgram().WriteText("not an image", directory);

		Assert.IsFalse(ImageClipboard.ForThisPlatform().TryReadPixels(out ImagePixels? read, out string? message));
		Assert.IsNull(read);
		Assert.AreEqual(ImageClipboard.NoImageMessage, message);
	}

	private static void AssertSamePixels(ImagePixels expected, ImagePixels actual, bool compareTransparentColor)
	{
		Assert.AreEqual(expected.Width, actual.Width, "width");
		Assert.AreEqual(expected.Height, actual.Height, "height");
		byte[] want = expected.ReadOnlyPixels.ToArray();
		byte[] got = actual.ReadOnlyPixels.ToArray();
		for (int i = 0; i < want.Length; i += 4)
		{
			int pixel = i / 4;
			string where = $"pixel ({pixel % expected.Width}, {pixel / expected.Width})";
			Assert.AreEqual(want[i + 3], got[i + 3], $"alpha at {where}");
			if (want[i + 3] == 0 && !compareTransparentColor)
			{
				continue;
			}

			CollectionAssert.AreEqual(want[i..(i + 3)], got[i..(i + 3)], $"colour at {where}");
		}
	}

	private static OtherClipboardProgram OtherProgram()
	{
		if (OperatingSystem.IsWindows())
		{
			return new WindowsForms();
		}

		if (OperatingSystem.IsMacOS())
		{
			return new AppleScript();
		}

		// GNOME's compositor offers wl-clipboard only the focus it takes from a keyboard, which a headless
		// session has none of; there the other program is an X11 one, through XWayland, as ours falls back to.
		bool x11 = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
			|| Environment.GetEnvironmentVariable(OtherProgramChoice) == "xclip";
		return x11 ? new Xclip() : new WlClipboard();
	}

	/// <summary>Runs a program to the end, its output to a file rather than a pipe, since a tool left serving the clipboard would hold a pipe open.</summary>
	private static string Run(string program, params string[] arguments)
	{
		string output = Path.GetTempFileName();
		try
		{
			ProcessStartInfo start = new(program) { UseShellExecute = false, CreateNoWindow = true };
			foreach (string argument in arguments)
			{
				start.ArgumentList.Add(argument);
			}

			// The shell wrapper sends the tool's output to a file, so nothing it leaves behind holds a pipe of ours.
			ProcessStartInfo shell = OperatingSystem.IsWindows() ? start : ShellRedirecting(start, output);
			if (OperatingSystem.IsWindows())
			{
				shell.RedirectStandardOutput = true;
				shell.RedirectStandardError = true;
			}

			using Process process = Process.Start(shell)!;
			string text = OperatingSystem.IsWindows() ? process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd() : string.Empty;
			Assert.IsTrue(process.WaitForExit(ToolTimeout), $"{program} didn't finish.");
			if (!OperatingSystem.IsWindows())
			{
				text = File.ReadAllText(output);
			}

			Assert.AreEqual(0, process.ExitCode, $"{program} {string.Join(' ', arguments)} failed: {text}");
			return text;
		}
		finally
		{
			File.Delete(output);
		}
	}

	private static ProcessStartInfo ShellRedirecting(ProcessStartInfo start, string output)
	{
		ProcessStartInfo shell = new("/bin/sh") { UseShellExecute = false, CreateNoWindow = true };
		shell.ArgumentList.Add("-c");
		shell.ArgumentList.Add("\"$0\" \"$@\" < /dev/null > \"$OUT\" 2>&1");
		shell.ArgumentList.Add(start.FileName);
		foreach (string argument in start.ArgumentList)
		{
			shell.ArgumentList.Add(argument);
		}

		shell.Environment["OUT"] = output;
		return shell;
	}

	/// <summary>Runs a program with a file as its standard input, the way the tools take what to copy.</summary>
	private static void RunWithInput(string input, string program, params string[] arguments)
	{
		ProcessStartInfo shell = new("/bin/sh") { UseShellExecute = false, CreateNoWindow = true };
		shell.ArgumentList.Add("-c");
		shell.ArgumentList.Add("\"$0\" \"$@\" < \"$IN\" > /dev/null 2>&1");
		shell.ArgumentList.Add(program);
		foreach (string argument in arguments)
		{
			shell.ArgumentList.Add(argument);
		}

		shell.Environment["IN"] = input;
		using Process process = Process.Start(shell)!;
		Assert.IsTrue(process.WaitForExit(ToolTimeout), $"{program} didn't finish.");
		Assert.AreEqual(0, process.ExitCode, $"{program} {string.Join(' ', arguments)} failed.");
	}

	/// <summary>Reads into a file through the shell, for tools that write binary to standard output.</summary>
	private static byte[] RunToFile(string outputPath, string program, params string[] arguments)
	{
		ProcessStartInfo shell = new("/bin/sh") { UseShellExecute = false, CreateNoWindow = true };
		shell.ArgumentList.Add("-c");
		shell.ArgumentList.Add("\"$0\" \"$@\" < /dev/null > \"$OUT\" 2>/dev/null");
		shell.ArgumentList.Add(program);
		foreach (string argument in arguments)
		{
			shell.ArgumentList.Add(argument);
		}

		shell.Environment["OUT"] = outputPath;
		using Process process = Process.Start(shell)!;
		Assert.IsTrue(process.WaitForExit(ToolTimeout), $"{program} didn't finish.");
		Assert.AreEqual(0, process.ExitCode, $"{program} {string.Join(' ', arguments)} failed.");
		return File.ReadAllBytes(outputPath);
	}

	/// <summary>Another program using the same clipboard.</summary>
	private abstract class OtherClipboardProgram
	{
		public abstract byte[] ReadPng(string directory);

		public abstract void WritePng(string path);

		public abstract void WriteBitmapOnly(string pngPath, string directory);

		public abstract void WriteText(string text, string directory);

		protected static string Bmp(string pngPath, string directory)
		{
			ImagePixels pixels = ImageDecoder.Decode(File.ReadAllBytes(pngPath));
			byte[][] rows = [.. Enumerable.Range(0, pixels.Height).Select(y =>
			{
				byte[] row = new byte[pixels.Width * 3];
				for (int x = 0; x < pixels.Width; x++)
				{
					int i = ((y * pixels.Width) + x) * 4;
					row[(x * 3) + 0] = pixels.ReadOnlyPixels[i + 2];
					row[(x * 3) + 1] = pixels.ReadOnlyPixels[i + 1];
					row[(x * 3) + 2] = pixels.ReadOnlyPixels[i];
				}

				return row;
			})];
			string path = Path.Join(directory, "other.bmp");
			File.WriteAllBytes(path, TestImageBuilder.Bmp(pixels.Width, pixels.Height, 24, rows));
			return path;
		}

		protected static string TextFile(string text, string directory)
		{
			string path = Path.Join(directory, "other.txt");
			File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			return path;
		}
	}

	private sealed class Xclip : OtherClipboardProgram
	{
		public override byte[] ReadPng(string directory)
		{
			string targets = Run("xclip", "-selection", "clipboard", "-target", "TARGETS", "-out");
			StringAssert.Contains(targets, "image/png");
			return RunToFile(Path.Join(directory, "read.png"), "xclip", "-selection", "clipboard", "-target", "image/png", "-out");
		}

		public override void WritePng(string path) => RunWithInput(path, "xclip", "-selection", "clipboard", "-target", "image/png", "-in");

		public override void WriteBitmapOnly(string pngPath, string directory) =>
			RunWithInput(Bmp(pngPath, directory), "xclip", "-selection", "clipboard", "-target", "image/bmp", "-in");

		public override void WriteText(string text, string directory) =>
			RunWithInput(TextFile(text, directory), "xclip", "-selection", "clipboard", "-in");
	}

	private sealed class WlClipboard : OtherClipboardProgram
	{
		public override byte[] ReadPng(string directory)
		{
			string types = Run("wl-paste", "--list-types");
			StringAssert.Contains(types, "image/png");
			return RunToFile(Path.Join(directory, "read.png"), "wl-paste", "--no-newline", "--type", "image/png");
		}

		public override void WritePng(string path) => RunWithInput(path, "wl-copy", "--type", "image/png");

		public override void WriteBitmapOnly(string pngPath, string directory) =>
			RunWithInput(Bmp(pngPath, directory), "wl-copy", "--type", "image/bmp");

		public override void WriteText(string text, string directory) =>
			RunWithInput(TextFile(text, directory), "wl-copy", "--type", "text/plain;charset=utf-8");
	}

	/// <summary>Windows PowerShell's <c>System.Windows.Forms.Clipboard</c>, the clipboard .NET Framework applications use.</summary>
	private sealed class WindowsForms : OtherClipboardProgram
	{
		public override byte[] ReadPng(string directory)
		{
			// Forms reads the bitmap formats rather than the registered PNG; asking for the PNG stream is how a browser reads it.
			string path = Path.Join(directory, "read.png");
			PowerShell($"$s = [Windows.Forms.Clipboard]::GetData('PNG'); if ($s -eq $null) {{ exit 3 }}; [IO.File]::WriteAllBytes('{path}', $s.ToArray())");
			return File.ReadAllBytes(path);
		}

		public override void WritePng(string path) =>
			PowerShell($"$d = New-Object Windows.Forms.DataObject; $d.SetData('PNG', $false, (New-Object IO.MemoryStream(,[IO.File]::ReadAllBytes('{path}')))); [Windows.Forms.Clipboard]::SetDataObject($d, $true)");

		public override void WriteBitmapOnly(string pngPath, string directory) =>
			// SetImage offers a bitmap, which Windows turns into CF_DIB and CF_DIBV5; no PNG.
			PowerShell($"Add-Type -AssemblyName System.Drawing; [Windows.Forms.Clipboard]::SetImage([Drawing.Image]::FromFile('{pngPath}'))");

		public override void WriteText(string text, string directory) =>
			PowerShell($"[Windows.Forms.Clipboard]::SetText('{text}')");

		private static void PowerShell(string script) =>
			Run("powershell.exe", "-NoProfile", "-NonInteractive", "-STA", "-Command", "Add-Type -AssemblyName System.Windows.Forms; " + script);
	}

	/// <summary>AppleScript for the pasteboard and <c>sips</c> for the TIFF that Preview and Safari put there.</summary>
	private sealed class AppleScript : OtherClipboardProgram
	{
		public override byte[] ReadPng(string directory)
		{
			string path = Path.Join(directory, "read.png");
			Run("osascript",
				"-e", "set png to (the clipboard as «class PNGf»)",
				"-e", $"set target to open for access (POSIX file \"{path}\") with write permission",
				"-e", "write png to target",
				"-e", "close access target");
			return File.ReadAllBytes(path);
		}

		public override void WritePng(string path) =>
			Run("osascript", "-e", $"set the clipboard to (read (POSIX file \"{path}\") as «class PNGf»)");

		public override void WriteBitmapOnly(string pngPath, string directory)
		{
			string tiff = Path.Join(directory, "other.tiff");
			Run("/usr/bin/sips", "-s", "format", "tiff", pngPath, "--out", tiff);
			Run("osascript", "-e", $"set the clipboard to (read (POSIX file \"{tiff}\") as TIFF picture)");
		}

		public override void WriteText(string text, string directory) =>
			Run("osascript", "-e", $"set the clipboard to \"{text}\"");
	}
}

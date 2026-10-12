// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Clipboard;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using ktsu.ImGui.App.Clipboard;
using ktsu.ImGui.App.Images;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The platform clipboards' failure paths, the bitmap header's other shapes, and the parts that need a real system.</summary>
[TestClass]
public sealed partial class PlatformClipboardTests
{
	private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G'];

	private static ClipboardImage Picture() => new(Png, 1, 1, new byte[4]);

	[TestMethod]
	public void TheUnsupportedClipboard_RefusesBothWays()
	{
		UnsupportedImageClipboard clipboard = new();

		Assert.IsFalse(clipboard.TryRead(out byte[]? image, out string? readMessage));
		Assert.IsNull(image);
		Assert.IsFalse(string.IsNullOrEmpty(readMessage));
		Assert.IsFalse(clipboard.TryWrite(Picture(), out string? writeMessage));
		Assert.AreEqual(readMessage, writeMessage);
	}

	[TestMethod]
	public void ReadingPixels_WhenTheClipboardHandsBackNothing_SaysThereIsNoImage()
	{
		ScriptedClipboard clipboard = new() { Succeeds = true };

		Assert.IsFalse(clipboard.TryReadPixels(out ImagePixels? pixels, out string? message));
		Assert.IsNull(pixels);
		Assert.AreEqual(ImageClipboard.NoImageMessage, message);
	}

	[TestMethod]
	public void ReadingPixels_PassesOnTheClipboardsOwnMessage()
	{
		ScriptedClipboard clipboard = new() { Message = "busy" };

		Assert.IsFalse(clipboard.TryReadPixels(out _, out string? message));
		Assert.AreEqual("busy", message);
	}

	[TestMethod]
	public void OnLinux_AFailedWrite_SaysSo()
	{
		ScriptedProcesses processes = new((_, _, _) => new ClipboardProcessResult(1, []));

		Assert.IsFalse(new LinuxImageClipboard(processes, wayland: true).TryWrite(Picture(), out string? message));
		Assert.AreEqual("The image couldn't be put on the clipboard.", message);
		Assert.HasCount(2, processes.Programs, "Each tool is tried before giving up.");
	}

	[TestMethod]
	public void OnLinux_WithNoToolToWriteWith_SaysWhatToInstall()
	{
		ScriptedProcesses processes = new((_, _, _) => null);

		Assert.IsFalse(new LinuxImageClipboard(processes, wayland: false).TryWrite(Picture(), out string? message));
		Assert.AreEqual(ImageClipboard.NoToolMessage, message);
	}

	[TestMethod]
	public void OnLinux_AToolThatFails_OrReadsNothing_IsNoImage()
	{
		ScriptedProcesses listingFails = new((_, _, _) => new ClipboardProcessResult(1, []));
		ScriptedProcesses readsNothing = new((_, arguments, _) =>
			new ClipboardProcessResult(0, arguments.Contains("TARGETS") ? "image/png"u8.ToArray() : []));

		Assert.IsFalse(new LinuxImageClipboard(listingFails, wayland: false).TryRead(out _, out string? failed));
		Assert.IsFalse(new LinuxImageClipboard(readsNothing, wayland: false).TryRead(out byte[]? image, out string? empty));
		Assert.AreEqual(ImageClipboard.NoImageMessage, failed);
		Assert.AreEqual(ImageClipboard.NoImageMessage, empty);
		Assert.IsNull(image);
	}

	[TestMethod]
	public void ForThisSession_FollowsWaylandDisplay()
	{
		ScriptedProcesses processes = new((_, _, _) => null);

		Assert.IsNotNull(LinuxImageClipboard.ForThisSession(processes));
	}

	[TestMethod]
	public void OnMac_ReadsWhatOsascriptWrote_AndCleansUp()
	{
		string? used = null;
		ScriptedProcesses processes = new((_, arguments, _) =>
		{
			used = ScriptPath(arguments);
			File.WriteAllBytes(used, Png);
			return new ClipboardProcessResult(0, []);
		});

		bool read = new MacImageClipboard(processes).TryRead(out byte[]? image, out string? message);

		Assert.IsTrue(read, message);
		CollectionAssert.AreEqual(Png, image);
		Assert.AreEqual("osascript", processes.Programs.Single());
		Assert.IsFalse(File.Exists(used), "The temporary file is deleted.");
	}

	[TestMethod]
	public void OnMac_AnEmptyOrFailedRead_IsNoImage()
	{
		ScriptedProcesses fails = new((_, _, _) => new ClipboardProcessResult(1, []));
		ScriptedProcesses writesNothing = new((_, arguments, _) =>
		{
			File.WriteAllBytes(ScriptPath(arguments), []);
			return new ClipboardProcessResult(0, []);
		});

		Assert.IsFalse(new MacImageClipboard(fails).TryRead(out _, out string? failed));
		Assert.IsFalse(new MacImageClipboard(writesNothing).TryRead(out _, out string? empty));
		Assert.AreEqual(ImageClipboard.NoImageMessage, failed);
		Assert.AreEqual(ImageClipboard.NoImageMessage, empty);
	}

	[TestMethod]
	public void OnMac_WithoutOsascript_SaysItCouldNotStart()
	{
		ScriptedProcesses processes = new((_, _, _) => null);

		Assert.IsFalse(new MacImageClipboard(processes).TryRead(out _, out string? message));
		StringAssert.Contains(message, "osascript");
		Assert.IsFalse(new MacImageClipboard(processes).TryWrite(Picture(), out string? writeMessage));
		Assert.AreEqual("The image couldn't be put on the clipboard.", writeMessage);
	}

	[TestMethod]
	public void OnMac_AWrite_HandsOsascriptThePng()
	{
		byte[]? handed = null;
		ScriptedProcesses processes = new((_, arguments, _) =>
		{
			handed = File.ReadAllBytes(ScriptPath(arguments));
			return new ClipboardProcessResult(0, []);
		});

		Assert.IsTrue(new MacImageClipboard(processes).TryWrite(Picture(), out string? message), message);
		CollectionAssert.AreEqual(Png, handed);
	}

	[TestMethod]
	public void AnInfoHeaderWithAPalette_PointsPastIt()
	{
		byte[] dib = Header(size: DeviceIndependentBitmap.InfoHeaderSize, bitCount: 8, compression: 0, colorsUsed: 0, extra: (256 * 4) + 16);

		byte[]? file = DeviceIndependentBitmap.ToBitmapFile(dib);

		Assert.IsNotNull(file);
		Assert.AreEqual(14 + 40 + (256 * 4), BitConverter.ToInt32(file, 10));
	}

	[TestMethod]
	public void AnInfoHeader_CountsItsColoursUsed_AndAlphaMasks()
	{
		byte[] palette = Header(size: DeviceIndependentBitmap.InfoHeaderSize, bitCount: 4, compression: 0, colorsUsed: 3, extra: 64);
		byte[] alpha = Header(size: DeviceIndependentBitmap.InfoHeaderSize, bitCount: 32, compression: 6, colorsUsed: 0, extra: 64);

		Assert.AreEqual(14 + 40 + 12, BitConverter.ToInt32(DeviceIndependentBitmap.ToBitmapFile(palette)!, 10));
		Assert.AreEqual(14 + 40 + 16, BitConverter.ToInt32(DeviceIndependentBitmap.ToBitmapFile(alpha)!, 10));
	}

	[TestMethod]
	public void AVersion5Header_HoldsItsOwnMasks()
	{
		byte[] dib = Header(size: 124, bitCount: 32, compression: 3, colorsUsed: 0, extra: 16);

		Assert.AreEqual(14 + 124, BitConverter.ToInt32(DeviceIndependentBitmap.ToBitmapFile(dib)!, 10));
	}

	[TestMethod]
	public void AHeaderClaimingMoreThanThereIs_IsRefused()
	{
		byte[] tooBig = Header(size: DeviceIndependentBitmap.InfoHeaderSize, bitCount: 32, compression: 0, colorsUsed: 0, extra: 8);
		BitConverter.GetBytes(200).CopyTo(tooBig, 0);
		byte[] tooSmall = Header(size: 12, bitCount: 32, compression: 0, colorsUsed: 0, extra: 0);
		byte[] paletteMissing = Header(size: DeviceIndependentBitmap.InfoHeaderSize, bitCount: 8, compression: 0, colorsUsed: 0, extra: 4);

		Assert.IsNull(DeviceIndependentBitmap.ToBitmapFile(tooBig));
		Assert.IsNull(DeviceIndependentBitmap.ToBitmapFile(tooSmall));
		Assert.IsNull(DeviceIndependentBitmap.ToBitmapFile(paletteMissing));
	}

	[TestMethod]
	public void FromBgra_RefusesPixelsThatDoNotFitTheSize()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DeviceIndependentBitmap.FromBgra(0, 1, []));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DeviceIndependentBitmap.FromBgra(1, 0, []));
		Assert.ThrowsExactly<ArgumentException>(() => DeviceIndependentBitmap.FromBgra(2, 2, new byte[4]));
	}

	[TestMethod]
	[OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
	public void SystemProcesses_RunATool_WithInputAndOutput()
	{
		SystemClipboardProcesses processes = new();

		ClipboardProcessResult? echoed = processes.Run("cat", [], Png, captureOutput: true);
		ClipboardProcessResult? failed = processes.Run("false", [], null, captureOutput: false);

		Assert.IsNotNull(echoed);
		Assert.AreEqual(0, echoed.ExitCode);
		CollectionAssert.AreEqual(Png, echoed.Output);
		Assert.IsNotNull(failed);
		Assert.AreNotEqual(0, failed.ExitCode);
		Assert.IsNull(processes.Run("no-such-clipboard-tool", [], null, captureOutput: false));
	}

	[TestMethod]
	[OSCondition(ConditionMode.Exclude, OperatingSystems.Windows)]
	public void SystemProcesses_StopAToolThatHangs()
	{
		ClipboardProcessResult? result = new SystemClipboardProcesses().Run("sleep", ["30"], null, captureOutput: false);

		Assert.IsNotNull(result);
		Assert.AreEqual(-1, result.ExitCode);
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	public void OnWindows_APictureRoundTripsThroughTheClipboard()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		WindowsImageClipboard clipboard = new();
		ImagePixels pixels = new(2, 1, [255, 0, 0, 255, 0, 0, 255, 255]);
		if (!clipboard.TryWritePixels(pixels, out string? written))
		{
			Assert.Inconclusive(written ?? "The clipboard refused the picture.");
		}

		Assert.IsTrue(clipboard.TryReadPixels(out ImagePixels? read, out string? message), message);
		CollectionAssert.AreEqual(pixels.Pixels.ToArray(), read!.Pixels.ToArray());
	}

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	public void OnWindows_ADibAlone_IsReadAsABitmap_AndNothingIsNoImage()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		WindowsImageClipboard clipboard = new();
		byte[] dib = DeviceIndependentBitmap.FromBgra(1, 1, [0, 0, 255, 255]);
		if (!PutOnClipboard(NativeMethods.DibFormat, dib))
		{
			Assert.Inconclusive("The clipboard couldn't be opened.");
		}

		Assert.IsTrue(clipboard.TryReadPixels(out ImagePixels? read, out string? message), message);
		// Windows offers the bitmap as a version 5 one too, which is read first; the colour survives either way.
		CollectionAssert.AreEqual(new byte[] { 255, 0, 0 }, read!.Pixels[..3].ToArray());

		if (!PutOnClipboard(0, []))
		{
			Assert.Inconclusive("The clipboard couldn't be opened.");
		}

		Assert.IsFalse(clipboard.TryRead(out _, out string? empty));
		Assert.AreEqual(ImageClipboard.NoImageMessage, empty);
	}

	// Empties the clipboard and, given a format, leaves those bytes on it alone.
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	private static bool PutOnClipboard(uint format, byte[] bytes)
	{
		if (!NativeMethods.OpenClipboard(0))
		{
			return false;
		}

		try
		{
			_ = NativeMethods.EmptyClipboard();
			if (format == 0)
			{
				return true;
			}

			nint handle = NativeMethods.GlobalAlloc(NativeMethods.Moveable, (nuint)bytes.Length);
			nint data = NativeMethods.GlobalLock(handle);
			Marshal.Copy(bytes, 0, data, bytes.Length);
			_ = NativeMethods.GlobalUnlock(handle);
			return NativeMethods.SetClipboardData(format, handle) != 0;
		}
		finally
		{
			_ = NativeMethods.CloseClipboard();
		}
	}

	private static byte[] Header(int size, ushort bitCount, uint compression, uint colorsUsed, int extra)
	{
		byte[] dib = new byte[size + extra];
		if (dib.Length >= 4)
		{
			BitConverter.GetBytes(size).CopyTo(dib, 0);
		}

		if (dib.Length >= DeviceIndependentBitmap.InfoHeaderSize)
		{
			BitConverter.GetBytes(bitCount).CopyTo(dib, 14);
			BitConverter.GetBytes(compression).CopyTo(dib, 16);
			BitConverter.GetBytes(colorsUsed).CopyTo(dib, 32);
		}

		return dib;
	}

	private static string ScriptPath(IReadOnlyList<string> arguments) =>
		arguments.Select(argument => PosixFile().Match(argument)).First(match => match.Success).Groups[1].Value;

	[GeneratedRegex("POSIX file \"([^\"]+)\"")]
	private static partial Regex PosixFile();

	/// <summary>Runs every program through one function, and records which were asked for.</summary>
	private sealed class ScriptedProcesses(Func<string, IReadOnlyList<string>, byte[]?, ClipboardProcessResult?> run) : IClipboardProcesses
	{
		public List<string> Programs { get; } = [];

		public ClipboardProcessResult? Run(string program, IReadOnlyList<string> arguments, byte[]? input, bool captureOutput)
		{
			Programs.Add(program);
			return run(program, arguments, input);
		}
	}

	/// <summary>A clipboard whose answer is set by the test.</summary>
	private sealed class ScriptedClipboard : IImageClipboard
	{
		public bool Succeeds { get; init; }

		public string? Message { get; init; }

		public bool TryRead(out byte[]? image, out string? message)
		{
			image = null;
			message = Message;
			return Succeeds;
		}

		public bool TryWrite(ClipboardImage image, out string? message)
		{
			message = Message;
			return Succeeds;
		}
	}
}

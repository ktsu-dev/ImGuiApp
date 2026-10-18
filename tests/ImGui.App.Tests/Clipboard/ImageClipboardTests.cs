// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Clipboard;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ktsu.ImGui.App.Clipboard;
using ktsu.ImGui.App.Images;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The platform clipboards' pure parts: bitmap conversion, PNG encoding, and which tool runs with which arguments.</summary>
[TestClass]
public sealed class ImageClipboardTests
{
	private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G'];

	// Two by two, as BGRA: opaque red, half-transparent green / opaque blue, transparent white.
	private static byte[] Corners() =>
	[
		0, 0, 255, 255, 0, 255, 0, 128,
		255, 0, 0, 255, 255, 255, 255, 0,
	];

	// The same picture as RGBA, as ImagePixels holds it.
	private static ImagePixels CornersRgba() => new(2, 2,
	[
		255, 0, 0, 255, 0, 255, 0, 128,
		0, 0, 255, 255, 255, 255, 255, 0,
	]);

	[TestMethod]
	public void ADibWeWrite_ReadsBackAsTheSamePixels_TopRowFirst()
	{
		byte[] dib = DeviceIndependentBitmap.FromBgra(2, 2, Corners());
		Assert.AreEqual(2, BitConverter.ToInt32(dib, 8), "Written bottom-up, as a positive height.");

		ImagePixels pixels = ImageDecoder.Decode(DeviceIndependentBitmap.ToBitmapFile(dib)!);

		Assert.AreEqual(2, pixels.Width);
		Assert.AreEqual(2, pixels.Height);
		CollectionAssert.AreEqual(CornersRgba().GetBuffer(), pixels.GetBuffer());
	}

	[TestMethod]
	public void APngWeEncode_DecodesToTheSamePixels()
	{
		ImagePixels source = CornersRgba();

		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(source));

		Assert.AreEqual(source.Width, decoded.Width);
		Assert.AreEqual(source.Height, decoded.Height);
		CollectionAssert.AreEqual(source.GetBuffer(), decoded.GetBuffer());
	}

	[TestMethod]
	public void APngWeEncode_OfAWideImage_DecodesToTheSamePixels()
	{
		ImagePixels source = new(37, 5);
		Random random = new(7);
		random.NextBytes(source.Pixels);

		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(source));

		CollectionAssert.AreEqual(source.GetBuffer(), decoded.GetBuffer());
	}

	[TestMethod]
	public void FromPixels_CarriesBothForms()
	{
		ClipboardImage image = ClipboardImage.FromPixels(CornersRgba());

		Assert.AreEqual(2, image.Width);
		Assert.AreEqual(2, image.Height);
		CollectionAssert.AreEqual(Corners(), image.Bgra.ToArray());
		CollectionAssert.AreEqual(CornersRgba().GetBuffer(), ImageDecoder.Decode(image.Png.Span).GetBuffer());
	}

	[TestMethod]
	public void WritingAndReadingPixels_RoundTripsThroughAClipboard()
	{
		MemoryClipboard clipboard = new();

		Assert.IsTrue(clipboard.TryWritePixels(CornersRgba(), out _));
		Assert.IsTrue(clipboard.TryReadPixels(out ImagePixels? pixels, out string? message), message);

		CollectionAssert.AreEqual(CornersRgba().GetBuffer(), pixels!.GetBuffer());
	}

	[TestMethod]
	public void ReadingPixels_FromAnEmptyClipboard_SaysThereIsNoImage()
	{
		Assert.IsFalse(new MemoryClipboard().TryReadPixels(out ImagePixels? pixels, out string? message));
		Assert.IsNull(pixels);
		Assert.AreEqual(ImageClipboard.NoImageMessage, message);
	}

	[TestMethod]
	public void ReadingPixels_OfSomethingUndecodable_SaysSo()
	{
		MemoryClipboard clipboard = new() { Held = [1, 2, 3, 4, 5, 6, 7, 8] };

		Assert.IsFalse(clipboard.TryReadPixels(out _, out string? message));
		StringAssert.StartsWith(message, "The clipboard's image couldn't be read");
	}

	[TestMethod]
	public void ForThisPlatform_GivesAClipboard() => Assert.IsNotNull(ImageClipboard.ForThisPlatform());

	[TestMethod]
	public void TheFileHeader_PointsPastBitFieldMasks()
	{
		byte[] dib = DeviceIndependentBitmap.FromBgra(1, 1, [1, 2, 3, 4]);
		BitConverter.TryWriteBytes(dib.AsSpan(16), 3u);
		byte[] withMasks = [.. dib.AsSpan(0, DeviceIndependentBitmap.InfoHeaderSize), .. new byte[12], .. dib.AsSpan(DeviceIndependentBitmap.InfoHeaderSize)];

		byte[] file = DeviceIndependentBitmap.ToBitmapFile(withMasks)!;

		Assert.AreEqual(14 + 40 + 12, BitConverter.ToInt32(file, 10));
		Assert.AreEqual(file.Length, BitConverter.ToInt32(file, 2));
	}

	[TestMethod]
	public void ATruncatedDib_IsRefused() => Assert.IsNull(DeviceIndependentBitmap.ToBitmapFile(new byte[12]));

	[TestMethod]
	public void PickType_PrefersPng_ThenAnyImage_AndIgnoresText()
	{
		Assert.AreEqual("image/png", LinuxImageClipboard.PickType("text/html\nimage/jpeg\nimage/png\n"));
		Assert.AreEqual("image/jpeg", LinuxImageClipboard.PickType("TARGETS\nimage/jpeg\n"));
		Assert.IsNull(LinuxImageClipboard.PickType("UTF8_STRING\ntext/plain\n"));
	}

	[TestMethod]
	public void PickType_SkipsTypesTheDecoderCannotRead()
	{
		Assert.AreEqual("image/bmp", LinuxImageClipboard.PickType("image/webp\nimage/bmp\n"));
		Assert.AreEqual("image/x-bmp", LinuxImageClipboard.PickType("image/gif\nimage/x-bmp\n"));

		// Nothing readable on offer is no image at all, not one that fails to decode.
		Assert.IsNull(LinuxImageClipboard.PickType("image/webp\n"));
	}

	[TestMethod]
	public void PickType_PrefersLosslessBmpOverJpeg_WhateverTheOrder()
	{
		Assert.AreEqual("image/bmp", LinuxImageClipboard.PickType("image/jpeg\nimage/bmp\n"));
		Assert.AreEqual("image/png", LinuxImageClipboard.PickType("image/jpeg\nimage/bmp\nimage/png\n"));
	}

	[TestMethod]
	public void OnWayland_ReadsThroughWlPaste()
	{
		FakeProcesses processes = new() { Types = "image/png\ntext/plain", Image = Png };

		bool read = new LinuxImageClipboard(processes, wayland: true).TryRead(out byte[]? image, out _);

		Assert.IsTrue(read);
		CollectionAssert.AreEqual(Png, image);
		Assert.AreEqual("wl-paste --no-newline --type image/png", processes.Calls[^1]);
	}

	[TestMethod]
	public void OnWayland_WithoutWlClipboard_FallsBackToXclip()
	{
		FakeProcesses processes = new() { Missing = { "wl-paste", "wl-copy" }, Types = "TARGETS\nimage/png", Image = Png };
		LinuxImageClipboard clipboard = new(processes, wayland: true);

		Assert.IsTrue(clipboard.TryRead(out _, out _));
		Assert.AreEqual("xclip -selection clipboard -target image/png -out", processes.Calls[^1]);

		Assert.IsTrue(clipboard.TryWrite(new ClipboardImage(Png, 1, 1, new byte[4]), out _));
		Assert.AreEqual("xclip -selection clipboard -target image/png -in", processes.Calls[^1]);
		CollectionAssert.AreEqual(Png, processes.LastInput);
		Assert.IsFalse(processes.LastCaptured, "Reading a writer's output would wait for as long as it serves the clipboard.");
	}

	[TestMethod]
	public void OnX11_WritesWithXclipOnly()
	{
		FakeProcesses processes = new();

		Assert.IsTrue(new LinuxImageClipboard(processes, wayland: false).TryWrite(new ClipboardImage(Png, 1, 1, new byte[4]), out _));
		Assert.HasCount(1, processes.Calls);
	}

	[TestMethod]
	public void WithNoToolInstalled_SaysWhatToInstall()
	{
		FakeProcesses processes = new() { Missing = { "wl-paste", "wl-copy", "xclip" } };

		Assert.IsFalse(new LinuxImageClipboard(processes, wayland: true).TryRead(out _, out string? message));
		Assert.AreEqual(ImageClipboard.NoToolMessage, message);
	}

	[TestMethod]
	public void WithTextOnTheClipboard_SaysThereIsNoImage()
	{
		FakeProcesses processes = new() { Types = "UTF8_STRING" };

		Assert.IsFalse(new LinuxImageClipboard(processes, wayland: false).TryRead(out _, out string? message));
		Assert.AreEqual(ImageClipboard.NoImageMessage, message);
	}

	[TestMethod]
	public void TheMacScripts_QuoteThePath()
	{
		string[] read = MacImageClipboard.ReadArguments("/tmp/a \"b\".png");
		string[] write = MacImageClipboard.WriteArguments("/tmp/a.png");

		Assert.Contains("set target to open for access (POSIX file \"/tmp/a \\\"b\\\".png\") with write permission", read);
		Assert.AreEqual("set the clipboard to (read (POSIX file \"/tmp/a.png\") as «class PNGf»)", write[1]);
	}

	/// <summary>Stands in for wl-clipboard and xclip.</summary>
	private sealed class FakeProcesses : IClipboardProcesses
	{
		public HashSet<string> Missing { get; } = [];

		public string Types { get; init; } = string.Empty;

		public byte[] Image { get; init; } = [];

		public List<string> Calls { get; } = [];

		public byte[]? LastInput { get; private set; }

		public bool LastCaptured { get; private set; }

		public ClipboardProcessResult? Run(string program, IReadOnlyList<string> arguments, byte[]? input, bool captureOutput)
		{
			if (Missing.Contains(program))
			{
				return null;
			}

			Calls.Add($"{program} {string.Join(' ', arguments)}");
			LastInput = input;
			LastCaptured = captureOutput;
			bool listing = arguments.Contains("--list-types") || arguments.Contains("TARGETS");
			return new ClipboardProcessResult(0, listing ? Encoding.UTF8.GetBytes(Types) : Image);
		}
	}

	/// <summary>A clipboard held in memory.</summary>
	private sealed class MemoryClipboard : IImageClipboard
	{
		public byte[]? Held { get; set; }

		public bool TryRead(out byte[]? image, out string? message)
		{
			image = Held;
			message = image is null ? ImageClipboard.NoImageMessage : null;
			return image is not null;
		}

		public bool TryWrite(ClipboardImage image, out string? message)
		{
			Held = image.Png.ToArray();
			message = null;
			return true;
		}
	}
}

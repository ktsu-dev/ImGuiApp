// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the JPEG decoder over the coding modes it claims to support.
/// </summary>
/// <remarks>
/// Each fixture is the same 32x32 image: four flat colour quadrants, which survive quantization
/// well enough to assert against with a small tolerance. The quadrant centres are sampled rather
/// than the edges, where the block transform rings against the colour step.
/// </remarks>
[TestClass]
public class JpegDecoderTests
{
	private const int Tolerance = 6;

	private static readonly (byte R, byte G, byte B)[] Quadrants =
	[
		(200, 30, 40),
		(30, 180, 60),
		(40, 70, 220),
		(240, 230, 20),
	];

	[TestMethod]
	[DataRow(nameof(Baseline420))]
	[DataRow(nameof(Baseline444))]
	[DataRow(nameof(Progressive))]
	[DataRow(nameof(RestartMarkers))]
	public void Decode_ColourFixture_ReproducesQuadrants(string fixture)
	{
		ImagePixels image = ImageDecoder.Decode(Convert.FromBase64String(Lookup(fixture)));

		Assert.AreEqual(32, image.Width);
		Assert.AreEqual(32, image.Height);

		AssertQuadrant(image, 8, 8, Quadrants[0]);
		AssertQuadrant(image, 24, 8, Quadrants[1]);
		AssertQuadrant(image, 8, 24, Quadrants[2]);
		AssertQuadrant(image, 24, 24, Quadrants[3]);
	}

	[TestMethod]
	public void Decode_Greyscale_ReplicatesLumaAcrossChannels()
	{
		ImagePixels image = ImageDecoder.Decode(Convert.FromBase64String(Greyscale));

		Assert.AreEqual(32, image.Width);
		Assert.AreEqual(32, image.Height);

		// Luma of the four quadrant colours, as the encoder computed them.
		int[] expected = [82, 121, 78, 209];
		(int X, int Y)[] centres = [(8, 8), (24, 8), (8, 24), (24, 24)];

		for (int i = 0; i < centres.Length; i++)
		{
			(byte r, byte g, byte b, byte a) = image.GetPixel(centres[i].X, centres[i].Y);
			Assert.AreEqual(r, g, $"quadrant {i} is not grey");
			Assert.AreEqual(g, b, $"quadrant {i} is not grey");
			Assert.AreEqual(255, a);
			Assert.IsTrue(Math.Abs(r - expected[i]) <= Tolerance, $"quadrant {i}: got {r}, expected about {expected[i]}");
		}
	}

	[TestMethod]
	public void Decode_ProgressiveAndBaseline_AgreeWithEachOther()
	{
		ImagePixels baseline = ImageDecoder.Decode(Convert.FromBase64String(Baseline420));
		ImagePixels progressive = ImageDecoder.Decode(Convert.FromBase64String(Progressive));

		Assert.AreEqual(baseline.ByteLength, progressive.ByteLength);

		long total = 0;
		for (int i = 0; i < baseline.ByteLength; i++)
		{
			total += Math.Abs(baseline.ReadOnlyPixels[i] - progressive.ReadOnlyPixels[i]);
		}

		double mean = (double)total / baseline.ByteLength;
		Assert.IsTrue(mean < 2.0, $"mean channel difference between the two codings was {mean:F2}");
	}

	[TestMethod]
	public void Decode_WithoutEndOfImageMarker_StillDecodes()
	{
		// Files that end the moment the entropy data does are common enough to be worth surviving:
		// the reader falls through to the same finish path the marker would have taken.
		byte[] whole = Convert.FromBase64String(Baseline420);

		ImagePixels image = ImageDecoder.Decode(whole.AsSpan(0, whole.Length - 2).ToArray());

		Assert.AreEqual(32, image.Width);
		Assert.AreEqual(32, image.Height);
		AssertQuadrant(image, 8, 8, Quadrants[0]);
	}

	[TestMethod]
	public void Decode_HeaderWithoutFrame_Throws()
	{
		byte[] soiOnly = [0xFF, 0xD8, 0xFF, 0xD9];

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(soiOnly));

		Assert.IsTrue(exception.Message.Contains("frame header", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Decode_ArithmeticCoded_ThrowsNamingTheMode()
	{
		// SOF9 is arithmetic-coded baseline, which this decoder deliberately does not implement.
		byte[] file = [0xFF, 0xD8, 0xFF, 0xC9, 0x00, 0x0B, 0x08, 0x00, 0x08, 0x00, 0x08, 0x01, 0x01, 0x11, 0x00];

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));

		Assert.IsTrue(exception.Message.Contains("C9", StringComparison.Ordinal), exception.Message);
	}

	private static void AssertQuadrant(ImagePixels image, int x, int y, (byte R, byte G, byte B) expected)
	{
		(byte r, byte g, byte b, byte a) = image.GetPixel(x, y);
		Assert.AreEqual(255, a);
		Assert.IsTrue(Math.Abs(r - expected.R) <= Tolerance, $"red at {x},{y}: got {r}, expected about {expected.R}");
		Assert.IsTrue(Math.Abs(g - expected.G) <= Tolerance, $"green at {x},{y}: got {g}, expected about {expected.G}");
		Assert.IsTrue(Math.Abs(b - expected.B) <= Tolerance, $"blue at {x},{y}: got {b}, expected about {expected.B}");
	}

	private static string Lookup(string fixture) => fixture switch
	{
		nameof(Baseline420) => Baseline420,
		nameof(Baseline444) => Baseline444,
		nameof(Progressive) => Progressive,
		nameof(RestartMarkers) => RestartMarkers,
		_ => throw new ArgumentOutOfRangeException(nameof(fixture)),
	};

	/// <summary>Baseline JPEG, 4:2:0 chroma subsampling.</summary>
	private const string Baseline420 =
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsK" +
		"CwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQU" +
		"FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAgACADASIA" +
		"AhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA" +
		"AAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3" +
		"ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm" +
		"p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA" +
		"AwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx" +
		"BhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK" +
		"U1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3" +
		"uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDwSiii" +
		"vz8/uA9oooorwj/HI8Nooor/AEyP2U/V2iiiv8DT+gz/2Q==";

	/// <summary>Baseline JPEG with no chroma subsampling.</summary>
	private const string Baseline444 =
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYF" +
		"BgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/2wBDAQICAgICAgUDAwUKBwYHCgoKCgoK" +
		"CgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgoKCgr/wAARCAAgACADAREA" +
		"AhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA" +
		"AAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3" +
		"ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm" +
		"p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA" +
		"AwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx" +
		"BhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK" +
		"U1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3" +
		"uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD5Hr8f" +
		"P9UAoA+nK+TP+asKAPmOvrD/AKVAoA+nK+TP+asKAPlOv9zD+mAoA/oMr/kTP7ECgD+fOv8ArsP4" +
		"7CgD+gyv+RM/sQKAP//Z";

	/// <summary>Progressive JPEG, exercising spectral selection and successive approximation.</summary>
	private const string Progressive =
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsK" +
		"CwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQU" +
		"FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wgARCAAgACADASIA" +
		"AhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAAAAYI/8QAFwEAAwEAAAAAAAAAAAAAAAAABgcIBP/a" +
		"AAwDAQACEAMQAAABgQPvC0GCOIYUyZ6vEDMH/8QAFBABAAAAAAAAAAAAAAAAAAAAQP/aAAgBAQAB" +
		"BQIH/8QAFBEBAAAAAAAAAAAAAAAAAAAAIP/aAAgBAwEBPwEf/8QAFBEBAAAAAAAAAAAAAAAAAAAA" +
		"IP/aAAgBAgEBPwEf/8QAFBABAAAAAAAAAAAAAAAAAAAAQP/aAAgBAQAGPwIH/8QAFBABAAAAAAAA" +
		"AAAAAAAAAAAAQP/aAAgBAQABPyEH/9oADAMBAAIAAwAAABDz/wCD/8QAFBEBAAAAAAAAAAAAAAAA" +
		"AAAAIP/aAAgBAwEBPxAf/8QAFBEBAAAAAAAAAAAAAAAAAAAAIP/aAAgBAgEBPxAf/8QAFBABAAAA" +
		"AAAAAAAAAAAAAAAAQP/aAAgBAQABPxAH/9k=";

	/// <summary>Baseline JPEG with a restart marker after every MCU.</summary>
	private const string RestartMarkers =
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsK" +
		"CwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQU" +
		"FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAgACADASIA" +
		"AhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA" +
		"AAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3" +
		"ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm" +
		"p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA" +
		"AwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx" +
		"BhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK" +
		"U1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3" +
		"uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/90ABAAB/9oADAMBAAIRAxEA" +
		"PwDwSiiivz8/uA//0MyiiivyI/lI/9H56ooor+1T83P/0vtiiiiv84j7s//Z";

	/// <summary>Greyscale baseline JPEG, a single-component frame.</summary>
	private const string Greyscale =
		"/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAIBAQEBAQIBAQECAgICAgQDAgICAgUEBAMEBgUGBgYF" +
		"BgYGBwkIBgcJBwYGCAsICQoKCgoKBggLDAsKDAkKCgr/wAALCAAgACABAREA/8QAHwAAAQUBAQEB" +
		"AQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1Fh" +
		"ByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZ" +
		"WmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXG" +
		"x8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/APkeivpyivmOivpyivlO" +
		"iv6DKK/nzor+gyiv/9k=";
}

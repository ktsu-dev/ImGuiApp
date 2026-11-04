// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using ktsu.ImGui.App.ImGuiController;
using ktsu.ImGui.App.ImGuiController.Hdr;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Silk.NET.OpenGL;

/// <summary>
/// The parts of HDR presentation that hold without a GPU: the encoding half-float textures use, what a
/// screen's description works out to, the Windows display configuration packets, and the half-float
/// textures the OpenGL renderer makes.
/// </summary>
[TestClass]
public sealed class HdrOutputTests
{
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The renderer owns the 3D renderer and disposes it with itself.")]
	private static OpenGLRenderer RendererOver(FakeGLRenderer gl) => new(gl, new NullTextureUploader(), new GLRenderer3D(new GLRenderer3DTests.FakeGL3D()));

	[TestMethod]
	[DataRow(0f)]
	[DataRow(0.002f)]
	[DataRow(0.18f)]
	[DataRow(1f)]
	[DataRow(4f)]
	[DataRow(-0.25f)]
	public void Encoding_RoundTrips(float linear) =>
		Assert.AreEqual(linear, HdrEncoding.Decode(HdrEncoding.Encode(linear)), Math.Max(1e-6f, Math.Abs(linear) * 1e-5f));

	[TestMethod]
	public void Encoding_KeepsWhiteAtOne_AndBrighterAboveIt()
	{
		Assert.AreEqual(1f, HdrEncoding.Encode(1f), 1e-6f);
		Assert.AreEqual(0.5f, HdrEncoding.Decode(0.735356983f), 1e-5f, "Below white it is the sRGB curve.");
		Assert.IsTrue(HdrEncoding.Encode(4f) > 1.5f, "Four times white encodes well above 1.");
		Assert.AreEqual(-HdrEncoding.Encode(0.3f), HdrEncoding.Encode(-0.3f), "Negative values mirror the curve, as scRGB's do.");
	}

	[TestMethod]
	public void ToRgba8_ShowsEverythingAboveWhiteAsWhite()
	{
		Half[] pixels = [(Half)0f, (Half)0.5f, (Half)1f, (Half)3.5f, (Half)(-0.2f), Half.NaN, (Half)0.25f, (Half)1f];
		byte[] bytes = new byte[pixels.Length];

		HdrEncoding.ToRgba8(pixels, bytes);

		CollectionAssert.AreEqual(new byte[] { 0, 128, 255, 255, 0, 0, 64, 255 }, bytes);
	}

	[TestMethod]
	public void ToRgba8_RefusesADestinationOfAnotherLength() =>
		Assert.ThrowsExactly<ArgumentException>(() => HdrEncoding.ToRgba8(new Half[4], new byte[3]));

	[TestMethod]
	public void DisplayInfo_FromLuminance_GivesTheHeadroom()
	{
		HdrDisplayInfo display = HdrDisplayInfo.FromLuminance(200, 1000);

		Assert.IsTrue(display.IsHdr);
		Assert.AreEqual(5, display.Headroom, 1e-12);
	}

	[TestMethod]
	public void DisplayInfo_WithNoRoomAboveWhite_IsSdr()
	{
		Assert.IsFalse(HdrDisplayInfo.FromLuminance(300, 250).IsHdr, "A peak below white leaves nothing above it.");
		Assert.AreEqual(1, HdrDisplayInfo.FromLuminance(300, 250).Headroom);
		Assert.IsFalse(HdrDisplayInfo.Sdr.IsHdr);
		Assert.AreEqual(1, HdrDisplayInfo.Sdr.Headroom);
	}

	[TestMethod]
	public void DisplayInfo_IgnoresWhatIsntANumber()
	{
		HdrDisplayInfo display = HdrDisplayInfo.FromLuminance(double.NaN, double.PositiveInfinity);

		Assert.AreEqual(HdrDisplayInfo.ReferenceWhiteNits, display.SdrWhiteNits);
		Assert.IsFalse(display.IsHdr);
	}

	[TestMethod]
	public void DisplayInfo_FromHeadroom_PlacesThePeakAboveWhite()
	{
		HdrDisplayInfo display = HdrDisplayInfo.FromHeadroom(8, 100);

		Assert.IsTrue(display.IsHdr);
		Assert.AreEqual(800, display.PeakNits, 1e-9);
		Assert.IsFalse(HdrDisplayInfo.FromHeadroom(0.5, 100).IsHdr);
	}

	[TestMethod]
	public void DisplayConfigPacket_CarriesItsHeader()
	{
		byte[] adapter = [1, 2, 3, 4, 5, 6, 7, 8];

		byte[] packet = WindowsDisplayConfig.Packet(11, 24, adapter, 77);

		Assert.HasCount(24, packet);
		Assert.AreEqual(11, BitConverter.ToInt32(packet, 0));
		Assert.AreEqual(24, BitConverter.ToInt32(packet, 4));
		CollectionAssert.AreEqual(adapter, packet[8..16]);
		Assert.AreEqual(77u, BitConverter.ToUInt32(packet, 16));
	}

	[TestMethod]
	public void DisplayConfig_ReadsTheSourceName()
	{
		byte[] packet = WindowsDisplayConfig.Packet(1, 84, new byte[8], 0);
		Encoding.Unicode.GetBytes(@"\\.\DISPLAY2").CopyTo(packet, 20);

		Assert.AreEqual(@"\\.\DISPLAY2", WindowsDisplayConfig.SourceName(packet));
	}

	[TestMethod]
	public void DisplayConfig_ReadsTheSdrWhite_InThousandthsOf80Nits()
	{
		byte[] packet = WindowsDisplayConfig.Packet(11, 24, new byte[8], 0);
		BitConverter.GetBytes(2500u).CopyTo(packet, 20);

		Assert.AreEqual(200, WindowsDisplayConfig.WhiteNits(packet), 1e-9);
	}

	[TestMethod]
	public void CreateHalfTexture_UploadsHalfFloats_AndKeepsTheCallersBinding()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		gl.Integers[GLEnum.TextureBinding2D] = 9;

		nint id = renderer.CreateTexture(new Half[4 * 3 * 2], 3, 2);

		Assert.IsTrue(gl.Calls.Any(call => call.StartsWith("CreateHalfTexture", StringComparison.Ordinal)));
		Assert.AreEqual((uint)id, gl.TextureWrites.Single().Texture);
		Assert.AreEqual((3, 2, 48), (gl.TextureWrites[0].Width, gl.TextureWrites[0].Height, gl.TextureWrites[0].Bytes));
		Assert.AreEqual(9, gl.GetInteger(GLEnum.TextureBinding2D));
	}

	[TestMethod]
	public void UpdateHalfTexture_WritesTheNamedTexture_AndKeepsTheCallersBinding()
	{
		FakeGLRenderer gl = new();
		using OpenGLRenderer renderer = RendererOver(gl);
		gl.Integers[GLEnum.TextureBinding2D] = 9;

		Assert.IsTrue(renderer.UpdateTexture(55, new Half[16], 2, 2));

		Assert.AreEqual((55u, 2, 2, 32), gl.TextureWrites.Single());
		Assert.AreEqual(9, gl.GetInteger(GLEnum.TextureBinding2D));
	}
}

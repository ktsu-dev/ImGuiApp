// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Semantics.Color;

[TestClass]
public class ColorImGuiAdapterTests
{
	[TestMethod]
	public void ToImColor_EmitsSrgbEncodedValue()
	{
		// A linear color built from sRGB 0.5 must emit the sRGB value (~0.5) to ImGui, not linear (~0.214).
		Color color = Color.FromSrgb(0.5, 0.5, 0.5, 1.0);
		ImColor im = color.ToImColor();
		Assert.AreEqual(0.5f, im.Value.X, 1e-4f);
		Assert.AreEqual(1.0f, im.Value.W, 1e-6f);
	}

	[TestMethod]
	public void ImColorRoundTrip_IsStable()
	{
		Color original = Color.FromSrgb(0.2, 0.6, 0.9, 0.8);
		Color roundTripped = original.ToImColor().FromImColor();
		Assert.AreEqual(original.R, roundTripped.R, 1e-6);
		Assert.AreEqual(original.G, roundTripped.G, 1e-6);
		Assert.AreEqual(original.B, roundTripped.B, 1e-6);
		Assert.AreEqual(original.A, roundTripped.A, 1e-6);
	}

	[TestMethod]
	public void ImGuiVector4_WidensToVector4Implicitly()
	{
		ImGuiVector4 strong = new(0.1f, 0.2f, 0.3f, 0.4f);
		Vector4 widened = strong;
		Assert.AreEqual(0.1f, widened.X, 1e-6f);
		Assert.AreEqual(0.4f, widened.W, 1e-6f);
	}

	[TestMethod]
	public void ToImGuiVector4_EmitsSrgbAndRoundTrips()
	{
		Color color = Color.FromSrgb(0.3, 0.7, 0.4, 1.0);
		ImGuiVector4 vector = color.ToImGuiVector4();
		Assert.AreEqual(0.3f, vector.X, 1e-4f);
		Color back = ColorImGuiExtensions.FromImGuiVector4(vector);
		Assert.AreEqual(color.G, back.G, 1e-6);
	}

	[TestMethod]
	public void FromImGuiVector4_RawVectorInterpretedAsSrgb()
	{
		Vector4 raw = new(0.0f, 0.0f, 0.0f, 1.0f);
		Color color = ColorImGuiExtensions.FromImGuiVector4(raw);
		Assert.AreEqual(0.0, color.R, 1e-9);
		Assert.AreEqual(1.0, color.A, 1e-9);
	}

	[TestMethod]
	public void ToImGuiU32_PacksChannelsInImGuiByteOrder()
	{
		// Opaque pure red: R=255, G=0, B=0, A=255 → default IM_COL32 layout 0xAABBGGRR = 0xFF0000FF.
		Color red = Color.FromSrgb(1.0, 0.0, 0.0, 1.0);
		Assert.AreEqual(0xFF0000FFu, red.ToImGuiU32());

		// Opaque pure blue lands in the third byte: 0xFFFF0000.
		Color blue = Color.FromSrgb(0.0, 0.0, 1.0, 1.0);
		Assert.AreEqual(0xFFFF0000u, blue.ToImGuiU32());
	}

	[TestMethod]
	public void ToImGuiU32_EmitsSrgbEncodedValue()
	{
		// sRGB 0.5 must pack to ~128, not the linear ~55.
		Color color = Color.FromSrgb(0.5, 0.5, 0.5, 1.0);
		uint packed = color.ToImGuiU32();
		Assert.AreEqual(128u, packed & 0xFF);
		Assert.AreEqual(255u, (packed >> 24) & 0xFF);
	}

	[TestMethod]
	public void ImGuiU32RoundTrip_IsStableWithinByteQuantization()
	{
		Color original = Color.FromSrgb(0.2, 0.6, 0.9, 0.8);
		Color roundTripped = ColorImGuiExtensions.FromImGuiU32(original.ToImGuiU32());
		// A single byte per channel means ~1/255 tolerance.
		Assert.AreEqual(original.R, roundTripped.R, 2.0 / 255.0);
		Assert.AreEqual(original.G, roundTripped.G, 2.0 / 255.0);
		Assert.AreEqual(original.B, roundTripped.B, 2.0 / 255.0);
		Assert.AreEqual(original.A, roundTripped.A, 2.0 / 255.0);
	}

	[TestMethod]
	public void ToImGuiU32_MatchesImGuiConversion()
	{
		Color color = Color.FromSrgb(0.3, 0.7, 0.4, 0.9);
		uint ours = color.ToImGuiU32();
		uint imgui = Hexa.NET.ImGui.ImGui.ColorConvertFloat4ToU32(color.ToSrgbVector4());
		Assert.AreEqual(imgui, ours);
	}
}

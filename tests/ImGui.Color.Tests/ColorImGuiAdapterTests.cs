// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using SemanticColor = ktsu.Semantics.Color.Color;

[TestClass]
public class ColorImGuiAdapterTests
{
	[TestMethod]
	public void ToImColor_EmitsSrgbEncodedValue()
	{
		// A linear color built from sRGB 0.5 must emit the sRGB value (~0.5) to ImGui, not linear (~0.214).
		SemanticColor color = SemanticColor.FromSrgb(0.5, 0.5, 0.5, 1.0);
		ImColor im = color.ToImColor();
		Assert.AreEqual(0.5f, im.Value.X, 1e-4f);
		Assert.AreEqual(1.0f, im.Value.W, 1e-6f);
	}

	[TestMethod]
	public void ImColorRoundTrip_IsStable()
	{
		SemanticColor original = SemanticColor.FromSrgb(0.2, 0.6, 0.9, 0.8);
		SemanticColor roundTripped = original.ToImColor().FromImColor();
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
		SemanticColor color = SemanticColor.FromSrgb(0.3, 0.7, 0.4, 1.0);
		ImGuiVector4 vector = color.ToImGuiVector4();
		Assert.AreEqual(0.3f, vector.X, 1e-4f);
		SemanticColor back = ColorImGuiExtensions.FromImGuiVector4(vector);
		Assert.AreEqual(color.G, back.G, 1e-6);
	}

	[TestMethod]
	public void FromImGuiVector4_RawVectorInterpretedAsSrgb()
	{
		Vector4 raw = new(0.0f, 0.0f, 0.0f, 1.0f);
		SemanticColor color = ColorImGuiExtensions.FromImGuiVector4(raw);
		Assert.AreEqual(0.0, color.R, 1e-9);
		Assert.AreEqual(1.0, color.A, 1e-9);
	}
}

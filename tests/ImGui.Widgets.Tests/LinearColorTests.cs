// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The conversions behind the linear colour controls: shown sRGB-encoded, stored linear. Pure, so no
/// ImGui context is required.
/// </summary>
[TestClass]
public sealed class LinearColorTests
{
	/// <summary>Linear 0.2140 is sRGB 0.5: what a person calls mid grey is about a fifth of the light.</summary>
	[TestMethod]
	public void MidGrey_IsShownAtHalfway()
	{
		Vector4 shown = ImGuiWidgets.LinearColor.ToSrgb(new Vector4(0.21404114f, 0.21404114f, 0.21404114f, 1f));
		Assert.AreEqual(0.5f, shown.X, 1e-5f);
		Assert.AreEqual(0.5f, shown.Z, 1e-5f);
	}

	[TestMethod]
	public void Alpha_PassesThroughBothWays()
	{
		Assert.AreEqual(0.25f, ImGuiWidgets.LinearColor.ToSrgb(new Vector4(0.1f, 0.2f, 0.3f, 0.25f)).W);
		Assert.AreEqual(0.25f, ImGuiWidgets.LinearColor.ToLinear(new Vector4(0.1f, 0.2f, 0.3f, 0.25f)).W);
	}

	[TestMethod]
	public void ToLinear_UndoesToSrgb()
	{
		Vector4 linear = new(0.02f, 0.5f, 0.9f, 0.75f);
		Vector4 back = ImGuiWidgets.LinearColor.ToLinear(ImGuiWidgets.LinearColor.ToSrgb(linear));
		Assert.AreEqual(linear.X, back.X, 1e-5f);
		Assert.AreEqual(linear.Y, back.Y, 1e-5f);
		Assert.AreEqual(linear.Z, back.Z, 1e-5f);

		Vector3 opaque = new(0.3f, 0.6f, 0.01f);
		Vector3 opaqueBack = ImGuiWidgets.LinearColor.ToLinear(ImGuiWidgets.LinearColor.ToSrgb(opaque));
		Assert.AreEqual(opaque.Y, opaqueBack.Y, 1e-5f);
	}

	/// <summary>A colour only displayed must stay exactly what the caller stored, not what a round trip rounds it to.</summary>
	[TestMethod]
	public void Adopt_LeavesTheStoredColourAloneWithoutAnEdit()
	{
		Vector4 stored = new(0.123456789f, 0.5f, 0.75f, 1f);
		Vector4 before = stored;

		Assert.IsFalse(ImGuiWidgets.LinearColor.Adopt(false, new Vector4(1f, 0f, 0f, 1f), ref stored));
		Assert.AreEqual(before, stored);

		Assert.IsTrue(ImGuiWidgets.LinearColor.Adopt(true, new Vector4(1f, 0f, 0f, 1f), ref stored));
		Assert.AreEqual(new Vector4(1f, 0f, 0f, 1f), stored);
	}
}

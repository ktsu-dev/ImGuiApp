// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the bezier control-point conversion. The widget itself needs a live ImGui context.
/// </summary>
[TestClass]
public sealed class BezierEditorTests
{
	[TestMethod]
	public void ToVendor_RoundTripsBothControlPoints()
	{
		BezierControlPoints points = new(new Vector2(0.1f, 0.2f), new Vector2(0.8f, 0.9f));

		BezierControlPoints result = ImGuiWidgets.FromVendorBezier(ImGuiWidgets.ToVendorBezier(points));

		Assert.AreEqual(points, result);
	}

	[TestMethod]
	public void ToVendor_KeepsControlPointOrder()
	{
		// The vendor type is an inline array; swapping the two would round-trip through an
		// equality check that only compared the set, so assert each slot individually.
		BezierControlPoints points = new(new Vector2(1f, 2f), new Vector2(3f, 4f));

		BezierControlPoints result = ImGuiWidgets.FromVendorBezier(ImGuiWidgets.ToVendorBezier(points));

		Assert.AreEqual(new Vector2(1f, 2f), result.First);
		Assert.AreEqual(new Vector2(3f, 4f), result.Second);
	}

	[TestMethod]
	public void DefaultControlPoints_AreZero()
	{
		BezierControlPoints points = default;

		Assert.AreEqual(Vector2.Zero, points.First);
		Assert.AreEqual(Vector2.Zero, points.Second);
	}
}

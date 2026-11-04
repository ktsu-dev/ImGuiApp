// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests which handle a press on the bezier editor picks up. The widget itself needs a live ImGui
/// context.
/// </summary>
[TestClass]
public sealed class BezierEditorTests
{
	private static readonly CurveFieldGeometry Square = new(new Vector2(10f, 20f), new Vector2(100f, 100f), Vector2.Zero, Vector2.One);

	[TestMethod]
	public void HandleAt_PicksTheHandleUnderThePointer()
	{
		BezierControlPoints points = new(new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

		Assert.AreEqual(0, ImGuiWidgets.BezierHandleAt(Square, points, new Vector2(35f, 95f), 6f));
		Assert.AreEqual(1, ImGuiWidgets.BezierHandleAt(Square, points, new Vector2(85f, 45f), 6f));
	}

	[TestMethod]
	public void HandleAt_EmptySpace_PicksNeither()
	{
		BezierControlPoints points = new(new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));

		Assert.AreEqual(-1, ImGuiWidgets.BezierHandleAt(Square, points, new Vector2(60f, 70f), 6f));
	}

	[TestMethod]
	public void HandleAt_OverlappingHandles_PicksTheNearer()
	{
		BezierControlPoints points = new(new Vector2(0.5f, 0.5f), new Vector2(0.52f, 0.5f));

		// The second handle sits 2 px right of the first, so a press 2 px right of that is on both.
		Assert.AreEqual(1, ImGuiWidgets.BezierHandleAt(Square, points, new Vector2(64f, 70f), 6f));
	}

	[TestMethod]
	public void DefaultControlPoints_AreZero()
	{
		BezierControlPoints points = default;

		Assert.AreEqual(Vector2.Zero, points.First);
		Assert.AreEqual(Vector2.Zero, points.Second);
	}
}

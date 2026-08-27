// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.XYPad"/> on its own.</summary>
[TestClass]
public sealed class XYPadTests : WidgetTest
{
	private const string Label = "Balance";

	private float x = 0.5f;
	private float y = 0.5f;

	private void Draw() => ImGuiWidgets.XYPad(Label, ref x, ref y, new Vector2(160f, 160f));

	[TestMethod]
	public void XYPad_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The pad marked no probe item.");
		AssertSomethingWasDrawn("the pad");
	}

	[TestMethod]
	public void XYPad_IsSquareAtTheRequestedSize()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - 160) <= 2, $"The pad reserved {rect.Width}px of width rather than 160.");
		Assert.IsTrue(Math.Abs(rect.Height - 160) <= 2, $"The pad reserved {rect.Height}px of height rather than 160.");
	}

	[TestMethod]
	public void XYPad_ClickingTheTopLeftGivesALowXAndAHighY()
	{
		Start(Draw);

		ClickFraction(Label, 0.05f, 0.05f);

		// Screen Y grows downward and the pad's axis grows upward, so the top of the pad is the
		// high end of Y, not the low one.
		Assert.IsTrue(x < 0.5f, $"X stayed at {x} after a click against the left edge.");
		Assert.IsTrue(y > 0.5f, $"Y stayed at {y} after a click against the top edge.");
	}

	[TestMethod]
	public void XYPad_ClickingRightRaisesX()
	{
		Start(Draw);

		ClickFraction(Label, 0.95f, 0.5f);

		Assert.IsTrue(x > 0.5f, $"X stayed at {x} after a click against the right edge.");
	}

	[TestMethod]
	public void XYPad_ValuesStayWithinTheUnitRange()
	{
		Start(Draw);

		// Aim outside the pad's own rectangle: the value must clamp rather than run away.
		DragAcross(Label, 0.5f, 2f);

		Assert.IsTrue(x is >= 0f and <= 1f, $"X left the unit range at {x}.");
		Assert.IsTrue(y is >= 0f and <= 1f, $"Y left the unit range at {y}.");
	}

	[TestMethod]
	public void XYPad_RedrawsTheHandleWhereItWasMoved()
	{
		Start(Draw);
		MoveAway();
		byte[] centered = Snapshot();

		x = 0.9f;
		y = 0.1f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(centered) > 0, "The handle drew in the same place after the value moved.");
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.RangeSlider"/> on its own.</summary>
[TestClass]
public sealed class RangeSliderTests : WidgetTest
{
	private const string Label = "Price";

	private float lower = 20f;
	private float upper = 80f;
	private float minGap;

	private void Draw() => ImGuiWidgets.RangeSlider(Label, ref lower, ref upper, 0f, 100f, minGap);

	[TestMethod]
	public void RangeSlider_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The range slider marked no probe item.");
		AssertSomethingWasDrawn("the range slider");
	}

	[TestMethod]
	public void RangeSlider_DraggingTheLowerHandleRaisesTheLowerBound()
	{
		Start(Draw);

		DragAcross(Label, 0.2f, 0.4f);

		Assert.IsTrue(lower > 20f, $"The lower bound stayed at {lower} after its handle was dragged right.");
		Assert.AreEqual(80f, upper, 1f, "Dragging the lower handle moved the upper bound.");
	}

	[TestMethod]
	public void RangeSlider_DraggingTheUpperHandleLowersTheUpperBound()
	{
		Start(Draw);

		DragAcross(Label, 0.8f, 0.6f);

		Assert.IsTrue(upper < 80f, $"The upper bound stayed at {upper} after its handle was dragged left.");
		Assert.AreEqual(20f, lower, 1f, "Dragging the upper handle moved the lower bound.");
	}

	[TestMethod]
	public void RangeSlider_HandlesStayInsideTheRange()
	{
		Start(Draw);

		DragAcross(Label, 0.2f, -0.5f);

		Assert.IsTrue(lower >= 0f, $"The lower bound left the range at {lower}.");
	}

	[TestMethod]
	public void RangeSlider_KeepsTheMinimumGapBetweenHandles()
	{
		minGap = 25f;
		Start(Draw);

		// Push the lower handle well past the upper one; the gap is what stops it.
		DragAcross(Label, 0.2f, 0.95f);

		Assert.IsTrue(
			upper - lower >= minGap - 0.5f,
			$"The handles closed to {upper - lower}, inside the {minGap} minimum gap.");
	}

	[TestMethod]
	public void RangeSlider_LeftAlone_KeepsItsBounds()
	{
		Start(Draw);

		Step(5);

		Assert.AreEqual(20f, lower);
		Assert.AreEqual(80f, upper);
	}
}

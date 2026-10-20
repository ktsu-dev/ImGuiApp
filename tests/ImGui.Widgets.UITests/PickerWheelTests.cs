// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PickerWheel(string, ref int, System.Collections.Generic.IReadOnlyList{string}, PickerWheelOptions?)"/> on its own.</summary>
[TestClass]
public sealed class PickerWheelTests : WidgetTest
{
	private const string Id = "fruit";
	private static readonly string[] Items = ["Apple", "Banana", "Cherry", "Date", "Elder", "Fig", "Grape", "Honeydew", "Kiwi", "Lemon"];

	private int index = 4;
	private int number = 20;
	private bool changed;
	private PickerWheelOptions options = new();

	private void Draw() => changed |= ImGuiWidgets.PickerWheel(Id, ref index, Items, options);

	private float RowHeight => RectOf(Id).Height / options.VisibleRows;

	// Drags the wheel vertically from its middle. Many small steps make a slow release.
	private void DragWheel(float pixels, int steps)
	{
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MinY + (rect.Height * 0.5f);
		Harness.Mouse.Drag(x, y, x, y + pixels, steps);
		Step(90);
	}

	[TestMethod]
	public void PickerWheel_ShowsItsRows()
	{
		Start(Draw);

		Assert.IsTrue(RectOf(Id).Height > RectOf(Id).Width * 0.5f, "The wheel is not as tall as its five rows.");
		AssertSomethingWasDrawn("The wheel drew nothing.");
	}

	[TestMethod]
	public void PickerWheel_SlowDragUpTwoRows_SelectsTwoItemsOn()
	{
		Start(Draw);

		DragWheel(-2f * RowHeight, 60);

		Assert.AreEqual(6, index);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void PickerWheel_Flick_SpinsFurtherThanTheDrag()
	{
		Start(Draw);

		DragWheel(-1f * RowHeight, 3);

		Assert.IsTrue(index > 5, $"A flick of one row moved the wheel to {index}, no further than the drag itself.");
	}

	[TestMethod]
	public void PickerWheel_ClickingTheRowBelow_SelectsIt()
	{
		Start(Draw);
		Rectangle rect = RectOf(Id);
		float rowY = (rect.Height * 0.5f) + (RectOf(Id).Height * 0.5f * System.MathF.Sin(ImGuiWidgets.PickerWheelImpl.RowAngle(5)));

		ClickWithin(Id, rect.Width * 0.5f, rowY);
		Step(60);

		Assert.AreEqual(5, index);
	}

	[TestMethod]
	public void PickerWheel_MouseWheel_MovesOneItemPerNotch()
	{
		Start(Draw);
		Rectangle rect = RectOf(Id);

		Harness.Mouse.Wheel(rect.MinX + (rect.Width * 0.5f), rect.MinY + (rect.Height * 0.5f), -1);
		Step();

		Assert.AreEqual(5, index);
	}

	[TestMethod]
	public void PickerWheel_DraggingBeforeTheFirstItem_StaysOnIt()
	{
		index = 0;
		Start(Draw);

		DragWheel(3f * RowHeight, 60);

		Assert.AreEqual(0, index);
		Assert.IsFalse(changed);
	}

	[TestMethod]
	public void PickerWheel_Wrapping_GoesRoundFromTheFirstToTheLast()
	{
		index = 0;
		options = new PickerWheelOptions { Wrap = true };
		Start(Draw);

		DragWheel(RowHeight, 60);

		Assert.AreEqual(Items.Length - 1, index);
	}

	[TestMethod]
	public void PickerWheel_ChangedByTheCaller_IsNotReported()
	{
		Start(Draw);

		index = 8;
		Step(60);

		Assert.IsFalse(changed);
		Assert.AreEqual(8, index);
	}

	[TestMethod]
	public void PickerWheel_NumericRange_StepsByItsStep()
	{
		Start(() => changed |= ImGuiWidgets.PickerWheel("count", ref number, 0, 100, 5));
		Rectangle rect = RectOf("count");

		Harness.Mouse.Wheel(rect.MinX + (rect.Width * 0.5f), rect.MinY + (rect.Height * 0.5f), -1);
		Step();

		Assert.AreEqual(25, number);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void PickerWheel_NumericValueOffItsSteps_IsMovedOntoOne()
	{
		number = 23;
		Start(() => ImGuiWidgets.PickerWheel("count", ref number, 0, 100, 5));

		Assert.AreEqual(20, number);
	}
}

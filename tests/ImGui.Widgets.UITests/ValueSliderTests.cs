// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ValueSlider(string, ref float, float, float, ValueSliderOptions?)"/> on its own.</summary>
[TestClass]
public sealed class ValueSliderTests : WidgetTest
{
	private const string Label = "##amount";
	private const float FrameWidth = 200f;

	private float value = 50f;
	private int whole = 10;
	private ValueSliderOptions options = new();

	private void Draw()
	{
		ImGui.PushItemWidth(FrameWidth);
		ImGuiWidgets.ValueSlider(Label, ref value, 0f, 100f, options);
		ImGui.PopItemWidth();
	}

	private void DrawWhole()
	{
		ImGui.PushItemWidth(FrameWidth);
		ImGuiWidgets.ValueSlider(Label, ref whole, 0, 20, options);
		ImGui.PopItemWidth();
	}

	private void DoubleClick(string name, float fractionX = 0.5f)
	{
		Rectangle rect = RectOf(name);
		float x = rect.MinX + (FrameWidth * fractionX);
		float y = rect.MinY + (rect.Height / 2f);
		Harness.Mouse.Click(x, y);
		Harness.Mouse.Click(x, y);

		// The release opens the editor and the next frame gives it the keyboard, so typing can start
		// on the one after.
		Step(2);
	}

	private void CtrlClick(string name)
	{
		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		ClickFraction(name, 0.5f);
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();
	}

	[TestMethod]
	public void ValueSlider_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The value slider marked no probe item.");
		AssertSomethingWasDrawn("the value slider");
	}

	[TestMethod]
	public void ValueSlider_DraggingRightRaisesTheValueByTheDistanceMoved()
	{
		Start(Draw);

		// A quarter of the frame's width is a quarter of the range.
		DragAcross(Label, 0.5f, 0.75f);

		Assert.AreEqual(75f, value, 1.5f, "A quarter-width drag did not move the value a quarter of the range.");
	}

	[TestMethod]
	public void ValueSlider_DraggingDoesNotJumpToWhereThePressLanded()
	{
		Start(Draw);

		// The press lands near the right end, but nothing moves until the pointer does.
		DragAcross(Label, 0.9f, 0.8f);

		Assert.AreEqual(40f, value, 1.5f, "The drag jumped to the press instead of moving by the distance dragged.");
	}

	[TestMethod]
	public void ValueSlider_StaysInsideTheRange()
	{
		Start(Draw);

		DragAcross(Label, 0.5f, -1f);

		Assert.AreEqual(0f, value, "Dragging far past the left end left the range.");
	}

	[TestMethod]
	public void ValueSlider_ShiftDragsFinely()
	{
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		DragAcross(Label, 0.5f, 0.75f);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(52.5f, value, 0.5f, "Holding Shift did not make the drag ten times finer.");
	}

	[TestMethod]
	public void ValueSlider_AClickAloneChangesNothing()
	{
		Start(Draw);

		ClickFraction(Label, 0.9f);
		Step(2);

		Harness.Keyboard.Type("9");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(50f, value, "A plain click changed the value, or opened it for typing.");
	}

	[TestMethod]
	public void ValueSlider_DoubleClickResetsWhenAResetValueIsSet()
	{
		options = new ValueSliderOptions { ResetValue = 20.0 };
		value = 80f;
		Start(Draw);

		DoubleClick(Label);

		Assert.AreEqual(20f, value, "A double-click did not return the slider to its reset value.");
	}

	[TestMethod]
	public void ValueSlider_CtrlClickOpensItForTyping()
	{
		Start(Draw);

		CtrlClick(Label);
		Harness.Keyboard.Type("33.5");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(33.5f, value, 1e-4f, "The typed value was not applied.");
	}

	[TestMethod]
	public void ValueSlider_DoubleClickOpensItForTypingWithoutAResetValue()
	{
		Start(Draw);

		DoubleClick(Label);
		Harness.Keyboard.Type("12");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(12f, value, 1e-4f, "A double-click with no reset value did not open the value for typing.");
	}

	[TestMethod]
	public void ValueSlider_EscapeLeavesTheValueAlone()
	{
		Start(Draw);

		CtrlClick(Label);
		Harness.Keyboard.Type("90");
		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step(2);

		Assert.AreEqual(50f, value, "Escape applied what was typed.");
	}

	[TestMethod]
	public void ValueSlider_TypedValuesAreClampedAndMayCarryTheUnit()
	{
		options = new ValueSliderOptions { Unit = "%" };
		Start(Draw);

		CtrlClick(Label);
		Harness.Keyboard.Type("140 %");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(100f, value, "A typed value past the maximum was not clamped, or its unit was not ignored.");
	}

	[TestMethod]
	public void ValueSlider_IntegerOverloadStepsInWholeNumbers()
	{
		Start(DrawWhole);

		// A quarter of 0..20 is five; small moves in between round rather than being lost.
		DragAcross(Label, 0.5f, 0.75f);

		Assert.AreEqual(15, whole, "A quarter-width drag did not move the whole-number slider by five.");
	}

	[TestMethod]
	public void ValueSlider_LeftAlone_KeepsItsValue()
	{
		Start(Draw);

		Step(5);

		Assert.AreEqual(50f, value);
	}

	[TestMethod]
	public void ValueSlider_ShowsAHorizontalResizeCursor()
	{
		Start(Draw);

		Hover(Label);

		Assert.AreEqual(ImGuiMouseCursor.ResizeEw, Harness.MouseCursor, "Hovering the value slider did not show a horizontal resize cursor.");
	}

	[TestMethod]
	public void ValueSlider_TheLabelIsPartOfTheItemButDoesNotDrag()
	{
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			ImGuiWidgets.ValueSlider("Amount", ref value, 0f, 100f);
			ImGui.PopItemWidth();
		});

		Rectangle rect = RectOf("Amount");
		Assert.IsTrue(rect.Width > FrameWidth, "The probe mark did not take in the label, as a slider's item rectangle does.");

		float labelX = rect.MinX + FrameWidth + ((rect.Width - FrameWidth) / 2f);
		float y = rect.MinY + (rect.Height / 2f);
		Harness.Mouse.Drag(labelX, y, labelX - 100f, y);
		Step();

		Assert.AreEqual(50f, value, "A drag starting on the label moved the value.");
	}

	[TestMethod]
	public void ValueSlider_TheFillRunsFromZeroInTheBipolarMode()
	{
		options = new ValueSliderOptions { Fill = ValueSliderFill.FromZero };
		float bipolar = 0f;
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			ImGuiWidgets.ValueSlider(Label, ref bipolar, -100f, 100f, options);
			ImGui.PopItemWidth();
		});

		byte[] atZero = Snapshot();
		bipolar = 50f;
		Step();

		Rectangle frame = RectOf(Label);
		if (BoundsOfDifference(atZero) is not Rectangle changed)
		{
			Assert.Fail("Moving the value away from zero drew nothing new.");
			return;
		}

		// The fill and the number change; neither reaches the left quarter, which the fill would cover
		// if it started from the minimum.
		Assert.IsTrue(
			changed.MinX > frame.MinX + (FrameWidth * 0.25f),
			$"The fill reached x={changed.MinX}, left of zero, so it did not start from zero.");
	}

	[TestMethod]
	public void ValueSlider_ReportsTheFrameItChangedOn()
	{
		bool reported = false;
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			reported |= ImGuiWidgets.ValueSlider(Label, ref value, 0f, 100f);
			ImGui.PopItemWidth();
		});

		DragAcross(Label, 0.5f, 0.6f);

		Assert.IsTrue(reported, "The slider changed its value without reporting it.");
	}

	[TestMethod]
	public void ValueSlider_IsActivatedForTheCallersLastItemQueries()
	{
		bool activated = false;
		bool deactivatedAfterEdit = false;
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			ImGuiWidgets.ValueSlider(Label, ref value, 0f, 100f);
			activated |= ImGui.IsItemActivated();
			deactivatedAfterEdit |= ImGui.IsItemDeactivatedAfterEdit();
			ImGui.PopItemWidth();
		});

		DragAcross(Label, 0.5f, 0.6f);

		Assert.IsTrue(activated, "IsItemActivated did not see the slider, so a caller cannot capture the value a drag starts from.");
		Assert.IsTrue(deactivatedAfterEdit, "IsItemDeactivatedAfterEdit did not see the drag's end.");
	}
}

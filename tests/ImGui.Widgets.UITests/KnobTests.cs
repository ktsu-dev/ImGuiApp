// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Knob</c> on its own.</summary>
[TestClass]
public sealed class KnobTests : WidgetTest
{
	private const string Label = "Gain";

	private float value = 0.5f;
	private int intValue = 5;
	private ImGuiKnobVariant variant = ImGuiKnobVariant.Tick;

	private void DrawFloatKnob() =>
		ImGuiWidgets.Knob(Label, ref value, 0f, 1f, variant: variant, size: 64f);

	private void DrawIntKnob() =>
		ImGuiWidgets.Knob(Label, ref intValue, 0, 10, variant: variant, size: 64f);

	// The knob is driven by a vertical drag, the same as ImGui's own drag controls: up raises the
	// value and down lowers it.
	private void DragVertically(float pixels)
	{
		Vector2 center = CenterOf(Label);
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y + pixels);
		Step();
	}

	[TestMethod]
	public void Knob_IsDrawnAndMarksItself()
	{
		Start(DrawFloatKnob);

		Assert.IsTrue(IsVisible(Label), "The knob marked no probe item.");
		AssertSomethingWasDrawn("the knob");
	}

	[TestMethod]
	public void Knob_ReservesTheRequestedDiameter()
	{
		Start(DrawFloatKnob);

		Rectangle rect = RectOf(Label);

		// The interactive area is a square of twice the radius, and the widget is asked for a
		// size of 64, so anything far from that means the size argument was ignored.
		Assert.IsTrue(rect.Width >= 32, $"The knob reserved only {rect.Width}px across.");
		Assert.AreEqual(rect.Width, rect.Height, "The knob's grab area was not square.");
	}

	[TestMethod]
	public void Knob_DraggingUpRaisesTheValue()
	{
		value = 0.5f;
		Start(DrawFloatKnob);

		DragVertically(-60f);

		Assert.IsTrue(value > 0.5f, $"Dragging up left the value at {value}.");
	}

	[TestMethod]
	public void Knob_DraggingDownLowersTheValue()
	{
		value = 0.5f;
		Start(DrawFloatKnob);

		DragVertically(60f);

		Assert.IsTrue(value < 0.5f, $"Dragging down left the value at {value}.");
	}

	[TestMethod]
	public void Knob_ClampsToItsRange()
	{
		value = 0.9f;
		Start(DrawFloatKnob);

		DragVertically(-400f);

		Assert.IsTrue(value <= 1f, $"The knob ran past its maximum to {value}.");
	}

	[TestMethod]
	public void Knob_IntegerOverload_Steps()
	{
		intValue = 5;
		Start(DrawIntKnob);

		DragVertically(-80f);

		Assert.IsTrue(intValue > 5, $"The integer knob stayed at {intValue}.");
		Assert.IsTrue(intValue <= 10, $"The integer knob ran past its maximum to {intValue}.");
	}

	[TestMethod]
	public void Knob_EveryVariantDrawsSomething()
	{
		foreach (ImGuiKnobVariant candidate in Enum.GetValues<ImGuiKnobVariant>())
		{
			variant = candidate;
			value = 0.4f;

			Start(DrawFloatKnob);
			MoveAway();

			Assert.IsTrue(IsVisible(Label), $"Knob variant {candidate} drew no item.");
			AssertSomethingWasDrawn($"knob variant {candidate}");

			DisposeHarness();
		}
	}

	[TestMethod]
	public void Knob_RedrawsAsTheValueChanges()
	{
		value = 0f;
		Start(DrawFloatKnob);
		MoveAway();
		byte[] atZero = Snapshot();

		value = 1f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(atZero) > 0, "The knob drew identically at both ends of its range.");
	}
}

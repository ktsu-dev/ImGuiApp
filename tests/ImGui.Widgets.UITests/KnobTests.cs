// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

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

	// MathF.Round accepts at most 6 digits, but a format may ask for up to 99 decimals, as Dear
	// ImGui's own drag controls allow. The drag must round without throwing.
	[TestMethod]
	[DataRow("%.7f")]
	[DataRow("%.10f")]
	public void Knob_DragsWithAFormatOfMoreThanSixDecimals(string format)
	{
		value = 0.5f;
		Start(() => ImGuiWidgets.Knob(Label, ref value, 0f, 1f, format: format, variant: variant, size: 64f));

		DragVertically(-60f);

		Assert.IsTrue(value > 0.5f, $"Dragging up with format {format} left the value at {value}.");
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

	[TestMethod]
	public void Knob_ShowsAVerticalResizeCursor()
	{
		Start(DrawFloatKnob);

		Hover(Label);

		Assert.AreEqual(ImGuiMouseCursor.ResizeNs, Harness.MouseCursor, "Hovering a knob driven by vertical drags did not show a vertical resize cursor.");
	}

	// A long ID suffix would wrap into several title rows if it were drawn, pushing that knob's dial
	// below its neighbour's. Shown as just "Gain", both titles take one row and the dials line up.
	// The first knob only starts the row: the two compared both follow a SameLine, so ImGui places
	// them alike.
	[TestMethod]
	public void Knob_TitleLeavesOutTheIdSuffix()
	{
		const string Plain = "Trim";
		const string Suffixed = "Gain##a suffix that is far wider than the dial";
		float first = 0.5f;
		float left = 0.5f;
		float right = 0.5f;
		Start(() =>
		{
			ImGuiWidgets.Knob("Pan", ref first, 0f, 1f, size: 64f);
			ImGui.SameLine();
			ImGuiWidgets.Knob(Plain, ref left, 0f, 1f, size: 64f);
			ImGui.SameLine();
			ImGuiWidgets.Knob(Suffixed, ref right, 0f, 1f, size: 64f);
		});

		Assert.AreEqual(RectOf(Plain).MinY, RectOf(Suffixed).MinY, "The ID suffix took title rows of its own.");
	}

	[TestMethod]
	public void Knob_WithOnlyAnIdHasNoTitleRow()
	{
		const string Untitled = "Gain";
		const string Hidden = "##hidden";
		float left = 0.5f;
		float right = 0.5f;
		Start(() =>
		{
			ImGuiWidgets.Knob(Untitled, ref left, 0f, 1f, size: 64f, flags: ImGuiKnobOptions.NoTitle);
			ImGui.SameLine();
			ImGuiWidgets.Knob(Hidden, ref right, 0f, 1f, size: 64f);
		});

		Assert.AreEqual(RectOf(Untitled).MinY, RectOf(Hidden).MinY, "A knob labelled only by an ID reserved a title row.");
	}

	[TestMethod]
	public void Knob_SameCaptionDifferentIdsDragIndependently()
	{
		const string Left = "Gain##left";
		const string Right = "Gain##right";
		float left = 0.5f;
		float right = 0.5f;
		Start(() =>
		{
			ImGuiWidgets.Knob(Left, ref left, 0f, 1f, size: 64f);
			ImGui.SameLine();
			ImGuiWidgets.Knob(Right, ref right, 0f, 1f, size: 64f);
		});

		Vector2 center = CenterOf(Left);
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y - 60f);
		Step();

		Assert.IsTrue(left > 0.5f, $"Dragging the left knob left it at {left}.");
		Assert.AreEqual(0.5f, right, "Dragging the left knob moved the right one.");
	}

	// The tooltip has to show the value the way the knob's own input shows it. A frame that draws the
	// expected text in a tooltip by hand, with the pointer in the same place, must match it pixel for pixel.
	[TestMethod]
	public void Knob_ValueTooltipShowsTheValue()
	{
		value = 0.5f;
		Start(() => ImGuiWidgets.Knob(Label, ref value, 0f, 1f, variant: variant, size: 64f));
		Hover(Label);
		Step(2);
		byte[] expected = TakeWithTooltip("0.500");

		Start(() => ImGuiWidgets.Knob(Label, ref value, 0f, 1f, variant: variant, size: 64f, flags: ImGuiKnobOptions.ValueTooltip));
		Hover(Label);
		Step(2);

		Assert.IsTrue(IsVisible($"{Label}/tooltip"), "Hovering a ValueTooltip knob showed no tooltip.");
		Assert.AreEqual(0, PixelsChangedSince(expected), "The value tooltip did not read \"0.500\".");
	}

	// Before #667 the int default format was "%if", which prints the value followed by a literal "f".
	[TestMethod]
	public void Knob_IntegerDefaultFormatHasNoTrailingF()
	{
		intValue = 5;
		Start(() => ImGuiWidgets.Knob(Label, ref intValue, 0, 10, format: "%d", variant: variant, size: 64f));
		MoveAway();
		byte[] plain = Snapshot();
		DisposeHarness();

		Start(DrawIntKnob);
		MoveAway();

		Assert.AreEqual(0, PixelsChangedSince(plain), "An int knob with the default format did not display its value as \"5\".");
	}

	// Draws the same frame again with a hand-made tooltip of the given text on top, then disposes the harness.
	private byte[] TakeWithTooltip(string text)
	{
		Vector2 pointer = CenterOf(Label);
		DisposeHarness();

		Start(() =>
		{
			ImGuiWidgets.Knob(Label, ref value, 0f, 1f, variant: variant, size: 64f);
			ImGui.BeginTooltip();
			ImGui.TextUnformatted(text);
			ImGui.EndTooltip();
		});
		Harness.Mouse.MoveTo(pointer.X, pointer.Y);
		Step(4);

		byte[] pixels = Snapshot();
		DisposeHarness();
		return pixels;
	}
}

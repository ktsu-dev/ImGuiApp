// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
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
	public void ValueSlider_AFillEndingUnderTheNumberStepsAroundIt()
	{
		// At 50 the fill's edge falls in the middle of the number, which would split it into two colors.
		value = 50f;
		Start(Draw);

		Rectangle frame = RectOf(Label);
		Vector4 grab = ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrab];
		CapturedFrame picture = Harness.Capture();
		int middleY = frame.MinY + (frame.Height / 2);
		int middleX = frame.MinX + (frame.Width / 2);

		Assert.IsTrue(IsFill(picture.GetPixel(frame.MinX + 4, middleY), grab), "The fill should still cover the frame's left half.");
		for (int x = middleX - 4; x < middleX; x++)
		{
			Assert.IsFalse(
				IsFill(picture.GetPixel(x, middleY), grab),
				$"The fill runs behind the number at x={x}, so the number is drawn in two colors.");
		}

		Assert.IsTrue(IsFill(picture.GetPixel(middleX - 2, frame.MinY + 1), grab), "The fill should continue above the number.");
	}

	private static bool IsFill(Rgba32 pixel, Vector4 grab) =>
		Math.Abs(pixel.R - (grab.X * 255f)) < 12f
		&& Math.Abs(pixel.G - (grab.Y * 255f)) < 12f
		&& Math.Abs(pixel.B - (grab.Z * 255f)) < 12f;

	[TestMethod]
	public void ValueSlider_LogarithmicDragsMultiplyTheValue()
	{
		options = new ValueSliderOptions { Logarithmic = true };
		float size = 10f;
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			ImGuiWidgets.ValueSlider(Label, ref size, 1f, 1000f, options);
			ImGui.PopItemWidth();
		});

		// 1..1000 is three factors of ten across the frame, so a third of its width is one of them.
		DragAcross(Label, 0.3f, 0.3f + (1f / 3f));

		Assert.AreEqual(100f, size, 8f, "A third-width drag on a logarithmic 1..1000 slider did not multiply the value by ten.");
	}

	[TestMethod]
	[DataRow(0f)]
	[DataRow(1f)]
	[DataRow(-1f)]
	public void ValueSlider_NothingIsDrawnThroughTheNumberNearZero(float near)
	{
		options = new ValueSliderOptions { Format = "0", Fill = ValueSliderFill.FromZero };
		float bipolar = near;
		Start(() =>
		{
			// The number is drawn invisibly, leaving the frame, the fill and the zero mark to look at.
			ImGui.PushItemWidth(FrameWidth);
			ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
			ImGuiWidgets.ValueSlider(Label, ref bipolar, -100f, 100f, options);
			ImGui.PopStyleColor();
			ImGui.PopItemWidth();
		});

		// Across the middle of the frame, where the number sits, the row through its centre must be the
		// frame's own colour: neither the zero mark nor a sliver of fill may run through the digits.
		byte[] pixels = Snapshot();
		Rectangle frame = RectOf(Label);
		int width = Harness.Options.Width;
		int y = frame.MinY + (frame.Height / 2);
		int centre = frame.MinX + (int)(FrameWidth / 2f);
		int reference = ((y * width) + frame.MinX + 10) * 4;
		for (int x = centre - 4; x <= centre + 4; x++)
		{
			int i = ((y * width) + x) * 4;
			bool same = pixels[i] == pixels[reference] && pixels[i + 1] == pixels[reference + 1] && pixels[i + 2] == pixels[reference + 2];
			Assert.IsTrue(same, $"At {near}, something was drawn through the number at x={x}.");
		}
	}

	[TestMethod]
	public void ValueSlider_APrefixIsDrawnButNotTyped()
	{
		options = new ValueSliderOptions { Format = "0" };
		Start(Draw);
		byte[] withoutPrefix = Snapshot();

		options = new ValueSliderOptions { Format = "0", Prefix = "Size " };
		Step();
		Assert.IsTrue(PixelsChangedSince(withoutPrefix) > 0, "The prefix drew nothing.");

		// Ctrl+click opens the value for typing; typing a number with the prefix in front still reads it.
		CtrlClick(Label);
		Harness.Keyboard.Type("Size 30");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step(2);

		Assert.AreEqual(30f, value, "A typed value with the prefix in front was not read.");
	}

	[TestMethod]
	public void ValueSlider_AHeldPointerThatStopsReportsNoChange()
	{
		// 0..1 over 200 px, so a 7 px drag lands on 0.035, which a float cannot hold exactly.
		float fraction = 0.5f;
		int changes = 0;
		Start(() =>
		{
			ImGui.PushItemWidth(FrameWidth);
			if (ImGuiWidgets.ValueSlider(Label, ref fraction, 0f, 1f))
			{
				changes++;
			}

			ImGui.PopItemWidth();
		});

		Rectangle rect = RectOf(Label);
		float x = rect.MinX + (FrameWidth / 2f);
		float y = rect.MinY + (rect.Height / 2f);
		Harness.Mouse.MoveTo(x, y);
		Step();
		HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(x + 7f, y);
		Step(2);

		Assert.AreNotEqual(0.5f, fraction, "The drag did not move the value.");
		changes = 0;
		Step(5);
		HarnessMouse.Up(0);
		Step();

		Assert.AreEqual(0, changes, "The slider reported a change on frames the held pointer did not move.");
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
	[TestMethod]
	public void ValueSlider_WritesATrueMinusSignWhenTheFontHasOne()
	{
		SignDrawn sign = DrawNegativeAndFindTheSign(withNerdFont: true);

		Assert.IsTrue(sign.FontHasMinusSign, "The bundled Nerd Font should carry U+2212.");
		Assert.IsTrue(sign.MinusSignDrawn, "A negative value should be written with U+2212.");
		Assert.IsFalse(sign.HyphenDrawn);
	}

	[TestMethod]
	public void ValueSlider_FallsBackToAHyphenWhenTheFontHasNoMinusSign()
	{
		// ImGui's default font, which a host outside ImGui.App draws with, has no U+2212, and neither
		// does an atlas built from ImGui's default ranges; drawn, it would be the missing-glyph box (#648).
		SignDrawn sign = DrawNegativeAndFindTheSign(withNerdFont: false);

		Assert.IsFalse(sign.FontHasMinusSign, "ImGui's default font was expected to lack U+2212.");
		Assert.IsTrue(sign.HyphenDrawn, "A negative value should fall back to a hyphen.");
		Assert.IsFalse(sign.FallbackDrawn, "The missing-glyph box was drawn.");
	}

	[TestCleanup]
	public void ReleaseFonts() => FontHelper.CleanupCustomFonts();

	private readonly record struct SignDrawn(bool FontHasMinusSign, bool MinusSignDrawn, bool HyphenDrawn, bool FallbackDrawn);

	private unsafe SignDrawn DrawNegativeAndFindTheSign(bool withNerdFont)
	{
		float negative = -7f;
		SignDrawn found = default;
		Start(
			() =>
			{
				ImGui.PushItemWidth(FrameWidth);
				ImGuiWidgets.ValueSlider(Label, ref negative, -10f, 10f, new ValueSliderOptions { Format = "0" });
				ImGui.PopItemWidth();

				// The face is the only text in the window, so a quad sampling a glyph's corner of the
				// atlas is that glyph being drawn. Without U+2212, FindGlyph answers with the fallback.
				ImFontBakedPtr baked = ImGui.GetFontBaked();
				bool hasMinus = ImGui.GetFont().IsGlyphInFont(0x2212);
				ImFontGlyphPtr hyphen = baked.FindGlyph('-');
				ImFontGlyphPtr minus = baked.FindGlyph(0x2212);
				Vector2 hyphenUv = new(hyphen.U0, hyphen.V0);
				Vector2 minusUv = new(minus.U0, minus.V0);
				ImDrawListPtr drawList = ImGui.GetWindowDrawList();
				bool hyphenDrawn = false;
				bool minusDrawn = false;
				for (int i = 0; i < drawList.VtxBuffer.Size; i++)
				{
					Vector2 uv = drawList.VtxBuffer.Data[i].Uv;
					hyphenDrawn |= uv == hyphenUv;
					minusDrawn |= uv == minusUv;
				}

				found = new SignDrawn(hasMinus, hasMinus && minusDrawn, hyphenDrawn, !hasMinus && minusDrawn);
			},
			onStart: withNerdFont ? UseTheNerdFont : null);
		Step();
		return found;
	}

	private static void UseTheNerdFont()
	{
		ImGuiIOPtr io = ImGui.GetIO();
		if (FontHelper.AddInterfaceFont(io, null, 14f) is ImFontPtr font)
		{
			io.FontDefault = font;
		}
	}
}

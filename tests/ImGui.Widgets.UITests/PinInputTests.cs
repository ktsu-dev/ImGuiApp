// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PinInput"/> on its own.</summary>
[TestClass]
public sealed class PinInputTests : WidgetTest
{
	private const string Id = "otp";
	private const string Span = "pin-span";
	private const int Length = 4;

	private string value = string.Empty;
	private bool masked;
	private bool digitsOnly = true;

	// The PIN input is a row of separate text boxes and marks none of them, so the test records the
	// span they occupy together and aims at a fraction across it to reach one box.
	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		ImGuiWidgets.PinInput(Id, ref value, Length, masked, digitsOnly);
		MarkSpan(Span, origin);
	}

	private void ClickBox(int index) => ClickFraction(Span, (index + 0.5f) / Length);

	/// <summary>
	/// Types one character per two frames rather than one per frame.
	/// </summary>
	/// <remarks>
	/// The widget advances focus by asking for it on the next frame, so a character delivered in
	/// the very next frame still lands in the box that was already focused and overwrites what was
	/// just typed there. A gap of one frame is what a real keystroke has and what the widget is
	/// built for.
	/// </remarks>
	private void TypeDigits(string digits)
	{
		foreach (char digit in digits)
		{
			Harness.Keyboard.Type(digit.ToString());
			Step(2);
		}
	}

	[TestMethod]
	public void PinInput_DrawsOneBoxPerDigit()
	{
		Start(Draw);

		Rectangle rect = RectOf(Span);

		Assert.IsTrue(IsVisible(Span), "The PIN input drew nothing to measure.");
		Assert.IsTrue(
			rect.Width > rect.Height * (Length - 1),
			$"The row is {rect.Width}px wide, too narrow to hold {Length} boxes of {rect.Height}px.");
	}

	[TestMethod]
	public void PinInput_TypingFillsTheBoxes()
	{
		Start(Draw);

		ClickBox(0);
		TypeDigits("1234");
		Step(2);

		Assert.AreEqual("1234", value, "Typing four digits did not fill the PIN.");
	}

	[TestMethod]
	public void PinInput_StopsAtItsLength()
	{
		Start(Draw);

		ClickBox(0);
		TypeDigits("123456789");
		Step(2);

		Assert.AreEqual(Length, value.Length, $"The PIN grew to '{value}', past its length of {Length}.");
	}

	[TestMethod]
	public void PinInput_DigitsOnly_RejectsLetters()
	{
		Start(Draw);

		ClickBox(0);
		TypeDigits("ab");
		Step(2);

		Assert.AreEqual(string.Empty, value, $"A digits-only PIN accepted letters: '{value}'.");
	}

	[TestMethod]
	public void PinInput_AllowingLetters_AcceptsThem()
	{
		digitsOnly = false;
		Start(Draw);

		ClickBox(0);
		TypeDigits("ab");
		Step(2);

		Assert.AreEqual("ab", value, $"A PIN that allows letters rejected them: '{value}'.");
	}

	[TestMethod]
	public void PinInput_Masked_DrawsSomethingOtherThanTheDigits()
	{
		value = "1234";
		masked = false;
		Start(Draw);
		MoveAway();
		byte[] plain = Snapshot();

		masked = true;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(plain) > 0, "A masked PIN drew the same glyphs as an unmasked one.");
	}

	[TestMethod]
	public void PinInput_NormalizesAnOverlongStartingValue()
	{
		value = "9876543210";
		Start(Draw);

		Assert.AreEqual("9876", value, "An overlong starting value was not trimmed to the PIN's length.");
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Keybinding.Core.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="KeyChordMatcher.TryCapture"/>, the read a rebinding control makes, through real
/// frames, because which keys went down this frame only exists inside an ImGui context.
/// </summary>
[TestClass]
public sealed class KeyChordCaptureTests : WidgetTest
{
	private Chord? captured;
	private int captures;

	private void Capture()
	{
		if (KeyChordMatcher.TryCapture(out Chord? chord))
		{
			captured = chord;
			captures++;
		}
	}

	[TestMethod]
	public void TryCapture_ReadsTheKeyWithTheModifiersHeld()
	{
		Start(Capture);

		Harness.Keyboard.Press(ImGuiKey.S, ctrl: true, shift: true);

		Assert.AreEqual(1, captures, "One press should capture once.");
		Assert.AreEqual(Chord.Parse("Ctrl+Shift+S"), captured);
		Assert.AreEqual("Ctrl+Shift+S", KeyChordMatcher.Display(captured));
	}

	[TestMethod]
	public void TryCapture_NamesASymbolKeyByItsSymbol()
	{
		Start(Capture);

		Harness.Keyboard.Press(ImGuiKey.RightBracket, ctrl: true);

		Assert.AreEqual(Chord.Parse("Ctrl+]"), captured);
	}

	/// <summary>A user holds the modifiers before the key, so the modifiers alone must not end the capture.</summary>
	[TestMethod]
	public void TryCapture_IgnoresModifiersOnTheirOwn()
	{
		Start(Capture);

		HarnessKeyboard.KeyDown(ImGuiKey.LeftCtrl);
		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		Step(2);
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		HarnessKeyboard.KeyUp(ImGuiKey.LeftCtrl);
		Step();

		Assert.AreEqual(0, captures, "Pressing Ctrl alone captured a chord.");
	}

	[TestMethod]
	public void TryCapture_CapturesOncePerPressWhileHeld()
	{
		Start(Capture);

		HarnessKeyboard.KeyDown(ImGuiKey.F5);
		Step(60);
		HarnessKeyboard.KeyUp(ImGuiKey.F5);
		Step();

		Assert.AreEqual(1, captures, "Holding a key captured it again at the repeat rate.");
		Assert.AreEqual("F5", KeyChordMatcher.Display(captured));
	}

	[TestMethod]
	public void ACapturedChord_IsPressedByTheSameKeys()
	{
		Chord chord = KeyChordMatcher.Compose(new(Ctrl: true, Alt: false, Shift: false, Meta: false), ImGuiKey.Equal);
		int presses = 0;
		Start(() =>
		{
			if (KeyChordMatcher.IsPressed(chord))
			{
				presses++;
			}
		});

		Harness.Keyboard.Press(ImGuiKey.Equal, ctrl: true);

		Assert.AreEqual(1, presses);
	}
}

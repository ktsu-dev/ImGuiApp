// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Keybinding.Core.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the shared chord matcher through real frames, because repeat and modifier state only
/// exist inside an ImGui context.
/// </summary>
[TestClass]
public sealed class KeyChordMatcherTests : WidgetTest
{
	private Chord chord = Chord.Parse("Down");
	private bool canRepeat;
	private bool isShiftIgnored;
	private int presses;

	private void CountPresses()
	{
		if (KeyChordMatcher.IsPressed(chord, canRepeat, isShiftIgnored))
		{
			presses++;
		}
	}

	private void StartCounting()
	{
		Start(CountPresses);
		presses = 0;
	}

	/// <summary>
	/// Holds the key for a full second at the harness's fixed sixty frames a second, which is well past
	/// ImGui's repeat delay.
	/// </summary>
	private void HoldDownArrow()
	{
		HarnessKeyboard.KeyDown(ImGuiKey.DownArrow);
		Step(60);
		HarnessKeyboard.KeyUp(ImGuiKey.DownArrow);
		Step();
	}

	[TestMethod]
	public void IsPressed_HeldWithoutRepeat_FiresOnce()
	{
		StartCounting();

		HoldDownArrow();

		Assert.AreEqual(1, presses, "A chord that cannot repeat fired more than once while held.");
	}

	[TestMethod]
	public void IsPressed_HeldWithRepeat_KeepsFiring()
	{
		canRepeat = true;
		StartCounting();

		HoldDownArrow();

		Assert.IsGreaterThan(1, presses, "A repeating chord fired only once in a second of holding.");
	}

	[TestMethod]
	public void IsPressed_WithShiftHeld_DoesNotMatchAChordWithoutShift()
	{
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreEqual(0, presses, "Shift+Down matched a Down chord, so modifiers are not matched exactly.");
	}

	[TestMethod]
	public void IsPressed_WithShiftIgnored_MatchesWithShiftHeld()
	{
		isShiftIgnored = true;
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreEqual(1, presses, "Shift+Down did not match a Down chord that ignores Shift.");
	}

	[TestMethod]
	public void IsPressed_WithShiftIgnored_StillRequiresOtherModifiersToMatch()
	{
		isShiftIgnored = true;
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.DownArrow, ctrl: true);

		Assert.AreEqual(0, presses, "Ignoring Shift also ignored Ctrl.");
	}

	[TestMethod]
	public void IsPressed_AChordWithShift_MatchesOnlyWithShiftHeld()
	{
		chord = Chord.Parse("Shift+Tab");
		StartCounting();

		Harness.Keyboard.Press(ImGuiKey.Tab);
		Assert.AreEqual(0, presses, "Tab alone matched Shift+Tab.");

		Harness.Keyboard.Press(ImGuiKey.Tab, shift: true);
		Assert.AreEqual(1, presses, "Shift+Tab did not match Shift+Tab.");
	}
}

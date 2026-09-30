// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the geometry and the pointer rules behind PianoKeyboard. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class PianoKeyboardStateTests
{
	private static readonly int[] BlackNotes = [61, 63, 66, 68, 70, -11];
	private static readonly int[] WhiteNotes = [60, 62, 64, 65, 67, 69, 71];

	private static ImGuiWidgets.PianoKeyboardLayout Octave() => new(60, 72);

	[TestMethod]
	public void IsBlackKey_FollowsTheChromaticPattern()
	{
		foreach (int note in BlackNotes)
		{
			Assert.IsTrue(ImGuiWidgets.PianoKeyboardLayout.IsBlackKey(note), $"{note} should be black.");
		}

		foreach (int note in WhiteNotes)
		{
			Assert.IsFalse(ImGuiWidgets.PianoKeyboardLayout.IsBlackKey(note), $"{note} should be white.");
		}
	}

	[TestMethod]
	public void Constructor_CountsKeys()
	{
		ImGuiWidgets.PianoKeyboardLayout layout = new(48, 84);

		Assert.AreEqual(37, layout.KeyCount);
		Assert.AreEqual(22, layout.WhiteKeyCount);
	}

	[TestMethod]
	public void Constructor_AcceptsTheWholeMidiRange()
	{
		ImGuiWidgets.PianoKeyboardLayout layout = new(0, 127);

		Assert.AreEqual(128, layout.KeyCount);
		Assert.AreEqual(75, layout.WhiteKeyCount);
	}

	[TestMethod]
	[DataRow(49, 84)]
	[DataRow(48, 85)]
	[DataRow(60, 60)]
	[DataRow(72, 60)]
	[DataRow(-12, 60)]
	[DataRow(60, 128)]
	public void Constructor_RejectsABlackEndOrAnEmptyRange(int lowest, int highest) =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.PianoKeyboardLayout(lowest, highest));

	[TestMethod]
	public void GetKeySpan_WhiteKeysTileTheKeyboard()
	{
		ImGuiWidgets.PianoKeyboardLayout layout = Octave();
		float previousEnd = 0f;
		bool first = true;

		for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
		{
			if (ImGuiWidgets.PianoKeyboardLayout.IsBlackKey(note))
			{
				continue;
			}

			ImGuiWidgets.PianoKeySpan span = layout.GetKeySpan(note);

			if (first)
			{
				Assert.AreEqual(0f, span.Start, "The first white key does not start at the left edge.");
				first = false;
			}
			else
			{
				Assert.AreEqual(previousEnd, span.Start, $"The white key {note} does not meet the one before it.");
			}

			previousEnd = span.End;
		}

		Assert.AreEqual(8f, previousEnd, "The last white key does not end at the right edge.");
	}

	[TestMethod]
	public void GetKeySpan_BlackKeyStraddlesTheBoundary()
	{
		ImGuiWidgets.PianoKeySpan span = Octave().GetKeySpan(61);

		Assert.AreEqual(0.7f, span.Start, 1e-6f);
		Assert.AreEqual(1.3f, span.End, 1e-6f);
	}

	[TestMethod]
	public void GetKeySpan_OutsideTheLayoutThrows() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Octave().GetKeySpan(73));

	[TestMethod]
	public void HitTest_BlackKeyWinsOverWhite()
	{
		ImGuiWidgets.PianoKeyboardLayout layout = Octave();

		Assert.AreEqual(61, layout.HitTest(1.0f, 0.3f));
		Assert.AreEqual(62, layout.HitTest(1.0f, 0.8f));
	}

	[TestMethod]
	public void HitTest_BoundaryBelongsToTheRightHandKey() =>
		Assert.AreEqual(64, Octave().HitTest(2.0f, 0.9f));

	[TestMethod]
	[DataRow(-0.01f, 0.5f)]
	[DataRow(8.0f, 0.5f)]
	[DataRow(1.5f, -0.1f)]
	[DataRow(1.5f, 1.1f)]
	[DataRow(float.NaN, 0.5f)]
	[DataRow(1.5f, float.NaN)]
	public void HitTest_OutsideReturnsMinusOne(float along, float across) =>
		Assert.AreEqual(-1, Octave().HitTest(along, across));

	[TestMethod]
	public void VelocityAt_RisesTowardsTheFrontEdge()
	{
		ImGuiWidgets.PianoKeyboardLayout layout = Octave();

		Assert.AreEqual(1, layout.VelocityAt(60, 0f));
		Assert.AreEqual(64, layout.VelocityAt(60, 0.5f));
		Assert.AreEqual(127, layout.VelocityAt(60, 1f));
	}

	[TestMethod]
	public void VelocityAt_BlackKeyScalesToItsOwnLength() =>
		Assert.AreEqual(127, Octave().VelocityAt(61, ImGuiWidgets.PianoKeyboardLayout.BlackKeyLength));

	[TestMethod]
	public void VelocityAt_NaNPlaysMidVelocity() =>
		Assert.AreEqual(64, Octave().VelocityAt(60, float.NaN));

	[TestMethod]
	[DataRow(60, "C4")]
	[DataRow(61, "C#4")]
	[DataRow(21, "A0")]
	[DataRow(0, "C-1")]
	[DataRow(127, "G9")]
	public void NoteName_UsesMiddleCAsC4(int note, string expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.PianoKeyboardLayout.NoteName(note));

	[TestMethod]
	public void Press_PlaysTheNoteWithItsVelocity()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();

		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(60, -1, 90), interaction.Press(60, 90));
		Assert.AreEqual(60, interaction.SoundingNote);
		Assert.IsTrue(interaction.IsHeld);
	}

	[TestMethod]
	public void MoveTo_AnotherKey_StopsTheOldNoteAndStartsTheNewOneTogether()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();
		interaction.Press(60, 90);

		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(62, 60, 70), interaction.MoveTo(62, 70));
		Assert.AreEqual(62, interaction.SoundingNote);
	}

	[TestMethod]
	public void MoveTo_SameKey_DoesNotRetrigger()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();
		interaction.Press(60, 90);

		Assert.AreEqual(ImGuiWidgets.PianoKeyEvent.None, interaction.MoveTo(60, 20));
	}

	[TestMethod]
	public void MoveTo_OffTheKeyboard_StopsTheNoteAndBackOnPlaysAgain()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();
		interaction.Press(60, 90);

		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(-1, 60, 0), interaction.MoveTo(-1, 0));
		Assert.IsTrue(interaction.IsHeld, "Leaving the keyboard must not release the button.");
		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(64, -1, 50), interaction.MoveTo(64, 50));
	}

	[TestMethod]
	public void MoveTo_WithoutAPress_DoesNothing()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();

		Assert.AreEqual(ImGuiWidgets.PianoKeyEvent.None, interaction.MoveTo(60, 64));
		Assert.AreEqual(-1, interaction.SoundingNote);
	}

	[TestMethod]
	public void Press_OffTheKeyboard_ThenSlidingOnPlays()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();

		Assert.AreEqual(ImGuiWidgets.PianoKeyEvent.None, interaction.Press(-1, 0));
		Assert.IsTrue(interaction.IsHeld);
		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(60, -1, 64), interaction.MoveTo(60, 64));
	}

	[TestMethod]
	public void Release_StopsTheSoundingNote()
	{
		ImGuiWidgets.PianoKeyboardInteraction interaction = new();
		interaction.Press(60, 90);

		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(-1, 60, 0), interaction.Release());
		Assert.IsFalse(interaction.IsHeld);
		Assert.AreEqual(ImGuiWidgets.PianoKeyEvent.None, interaction.Release());
	}

	[TestMethod]
	public void PianoKeyEvent_NoneHasNeitherNote()
	{
		Assert.IsFalse(ImGuiWidgets.PianoKeyEvent.None.HasNoteOn);
		Assert.IsFalse(ImGuiWidgets.PianoKeyEvent.None.HasNoteOff);
		Assert.IsTrue(new ImGuiWidgets.PianoKeyEvent(62, 60, 70).HasNoteOn);
		Assert.IsTrue(new ImGuiWidgets.PianoKeyEvent(62, 60, 70).HasNoteOff);
	}
}

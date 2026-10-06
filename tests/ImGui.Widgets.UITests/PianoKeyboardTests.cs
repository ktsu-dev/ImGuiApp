// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PianoKeyboard"/> on its own.</summary>
[TestClass]
public sealed class PianoKeyboardTests : WidgetTest
{
	private const string Label = "Keys";
	private static readonly Vector2 Size = new(400f, 100f);

	private readonly ImGuiWidgets.PianoKeyboardLayout layout = new(60, 72);
	private readonly List<ImGuiWidgets.PianoKeyEvent> events = [];
	private int[] held = [];

	private void Draw()
	{
		ImGuiWidgets.PianoKeyEvent result = ImGuiWidgets.PianoKeyboard(Label, layout, Size, held);
		if (result != ImGuiWidgets.PianoKeyEvent.None)
		{
			events.Add(result);
		}
	}

	private int[] NoteOns() => [.. events.Where(e => e.HasNoteOn).Select(e => e.NoteOn)];

	[TestMethod]
	public void PianoKeyboard_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.AreEqual((int)Size.X, rect.Width, "The keyboard reserved the wrong width.");
		Assert.AreEqual((int)Size.Y, rect.Height, "The keyboard reserved the wrong height.");
	}

	[TestMethod]
	public void PianoKeyboard_ClickingAWhiteKeyPlaysAndReleasesIt()
	{
		Start(Draw);

		Click($"{Label}/C4");

		Assert.HasCount(2, events, $"Events: {string.Join(", ", events)}");
		Assert.AreEqual(60, events[0].NoteOn);
		Assert.AreEqual(-1, events[0].NoteOff);
		Assert.IsTrue(events[0].Velocity is >= 1 and <= 127, $"Velocity {events[0].Velocity} is out of range.");
		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(-1, 60, 0), events[1]);
	}

	[TestMethod]
	public void PianoKeyboard_ClickingABlackKeyPlaysItRatherThanTheWhiteKeyBeneath()
	{
		Start(Draw);

		Click($"{Label}/C#4");

		int[] noteOns = NoteOns();
		Assert.HasCount(1, noteOns, $"Events: {string.Join(", ", events)}");
		Assert.AreEqual(61, noteOns[0]);
	}

	[TestMethod]
	public void PianoKeyboard_DraggingAcrossKeysPlaysEachInTurn()
	{
		Start(Draw);

		DragAcross(Label, 0.03f, 0.97f, 0.9f);

		string all = string.Join(", ", events);
		int[] noteOns = NoteOns();

		Assert.IsNotEmpty(noteOns, "The drag played nothing.");
		Assert.AreEqual(60, noteOns[0], all);
		Assert.AreEqual(72, noteOns[^1], all);

		for (int i = 0; i < noteOns.Length; i++)
		{
			Assert.IsFalse(ImGuiWidgets.PianoKeyboardLayout.IsBlackKey(noteOns[i]), $"The drag below the black keys played {noteOns[i]}. {all}");
			if (i > 0)
			{
				Assert.IsGreaterThan(noteOns[i - 1], noteOns[i], $"The note-ons are not strictly ascending. {all}");
			}
		}

		ImGuiWidgets.PianoKeyEvent[] ons = [.. events.Where(e => e.HasNoteOn)];
		for (int i = 1; i < ons.Length; i++)
		{
			Assert.AreEqual(ons[i - 1].NoteOn, ons[i].NoteOff, $"Sliding to {ons[i].NoteOn} did not stop {ons[i - 1].NoteOn}. {all}");
		}

		Assert.AreEqual(new ImGuiWidgets.PianoKeyEvent(-1, 72, 0), events[^1], all);
	}

	[TestMethod]
	public void PianoKeyboard_VelocityFollowsTheClickHeight()
	{
		Start(Draw);

		ClickFraction($"{Label}/C4", 0.5f, 0.1f);
		ClickFraction($"{Label}/C4", 0.5f, 0.9f);

		ImGuiWidgets.PianoKeyEvent[] ons = [.. events.Where(e => e.HasNoteOn)];
		Assert.HasCount(2, ons, $"Events: {string.Join(", ", events)}");
		Assert.IsGreaterThan(ons[0].Velocity, ons[1].Velocity, "A press nearer the front edge did not play louder.");
	}

	[TestMethod]
	public void PianoKeyboard_HeldNotesAreHighlighted()
	{
		held = [64];
		Start(Draw);
		MoveAway();

		Rectangle keys = RectOf(Label);
		int y = keys.MinY + (int)(keys.Height * 0.85f);
		CapturedFrame frame = Harness.Capture();
		Rgba32 e4 = frame.GetPixel((int)CenterOf($"{Label}/E4").X, y);
		Rgba32 d4 = frame.GetPixel((int)CenterOf($"{Label}/D4").X, y);

		Assert.AreNotEqual(d4, e4, "The held E4 is drawn like the unheld D4.");
		Assert.IsEmpty(events, "Held notes were reported as events.");
	}

	[TestMethod]
	public void PianoKeyboard_IsMarkedForProbes()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The keyboard is not marked.");
		Assert.IsTrue(IsVisible($"{Label}/C5"), "The top key is not marked.");
	}

	[TestMethod]
	public void PianoKeyboard_KeyMarksMatchTheLayout()
	{
		Start(Draw);

		Rectangle keys = RectOf(Label);
		Rectangle c4 = RectOf($"{Label}/C4");
		Rectangle cSharp4 = RectOf($"{Label}/C#4");

		Assert.AreEqual(keys.MinX, c4.MinX, "C4 does not start at the left edge.");
		Assert.AreEqual(50f, c4.Width, 1f, "A white key is not one eighth of the keyboard.");
		Assert.AreEqual(keys.Height, c4.Height, 1f, "A white key does not run the whole length.");
		Assert.AreEqual(62f, cSharp4.Height, 1f, "A black key is not 0.62 of the length.");
		Assert.IsTrue(cSharp4.MinX < c4.MaxX && cSharp4.MaxX > c4.MaxX, "C#4 does not straddle the C4/D4 boundary.");
	}
}

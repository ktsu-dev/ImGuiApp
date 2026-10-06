// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows a two-octave piano keyboard, the last note it played, and notes held from outside.</summary>
internal static class PianoKeyboardDemo
{
	private const float KeyboardHeight = 90f;

	private static readonly ImGuiWidgets.PianoKeyboardLayout Layout = new(48, 72);
	private static readonly int[] CMajorChord = [48, 52, 55];

	/// <summary>Gets the last note the keyboard started, or -1 before any.</summary>
	internal static int LastNote { get; private set; } = -1;

	/// <summary>Gets the velocity of <see cref="LastNote"/>, or 0 before any.</summary>
	internal static int LastVelocity { get; private set; }

	/// <summary>Gets a value indicating whether the C major chord is passed as held notes.</summary>
	internal static bool HoldChord => holdChord;

	private static bool holdChord;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		LastNote = -1;
		LastVelocity = 0;
		holdChord = false;
	}

	/// <summary>Draws the section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Piano Keyboard"))
		{
			return;
		}

		ImGui.TextUnformatted("Click and drag to play; press nearer the front edge of a key to play louder:");
		ImGui.Separator();

		ImGuiWidgets.PianoKeyEvent played = ImGuiWidgets.PianoKeyboard(
			"Piano",
			Layout,
			new Vector2(0f, KeyboardHeight),
			holdChord ? CMajorChord : default);

		if (played.HasNoteOn)
		{
			LastNote = played.NoteOn;
			LastVelocity = played.Velocity;
		}

		ImGui.TextUnformatted(LastNote < 0
			? "Last: none"
			: string.Create(CultureInfo.InvariantCulture, $"Last: {ImGuiWidgets.PianoKeyboardLayout.NoteName(LastNote)} ({LastNote}) vel {LastVelocity}"));

		DemoProbe.Checkbox("Hold C major chord", ref holdChord);
	}
}

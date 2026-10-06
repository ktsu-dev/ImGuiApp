// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Which note the pointer is holding on a <see cref="PianoKeyboard"/>, and the events that
	/// pressing, sliding and releasing produce. Free of ImGui, so the rules are tested without a
	/// context.
	/// </summary>
	/// <remarks>
	/// Monophonic by construction: one pointer holds one note. Sliding to another key stops the old
	/// note and starts the new one in a single event, which is a glissando. Only the key under the
	/// pointer in each frame sounds, so keys skipped between two frames are not played.
	/// </remarks>
	internal sealed class PianoKeyboardInteraction
	{
		/// <summary>Gets the MIDI note the pointer is sounding, or -1.</summary>
		public int SoundingNote { get; private set; } = -1;

		/// <summary>Gets a value indicating whether the button is down.</summary>
		public bool IsHeld { get; private set; }

		/// <summary>The button went down over <paramref name="note"/>, or off the keys when it is -1.</summary>
		/// <param name="note">The note under the pointer, or -1.</param>
		/// <param name="velocity">The velocity the press plays.</param>
		/// <returns>The note started, or <see cref="PianoKeyEvent.None"/> when the press missed the keys.</returns>
		/// <remarks>
		/// A press that misses still holds the button, so sliding onto the keyboard afterwards plays.
		/// </remarks>
		public PianoKeyEvent Press(int note, int velocity)
		{
			IsHeld = true;
			return Sound(note, velocity);
		}

		/// <summary>The pointer is over <paramref name="note"/> while the button may be held.</summary>
		/// <param name="note">The note under the pointer, or -1 off the keyboard.</param>
		/// <param name="velocity">The velocity a new note would play.</param>
		/// <returns>The notes started and stopped, or <see cref="PianoKeyEvent.None"/>.</returns>
		/// <remarks>
		/// Nothing happens while the button is up, or while the pointer stays on the sounding key:
		/// a change of velocity within a key never retriggers it.
		/// </remarks>
		public PianoKeyEvent MoveTo(int note, int velocity) =>
			IsHeld ? Sound(note, velocity) : PianoKeyEvent.None;

		/// <summary>The button came up.</summary>
		/// <returns>The note stopped, or <see cref="PianoKeyEvent.None"/> when nothing was sounding.</returns>
		public PianoKeyEvent Release()
		{
			IsHeld = false;
			return Sound(-1, 0);
		}

		private PianoKeyEvent Sound(int note, int velocity)
		{
			int previous = SoundingNote;

			if (note == previous)
			{
				return PianoKeyEvent.None;
			}

			SoundingNote = note;

			return note >= 0
				? new PianoKeyEvent(note, previous, velocity)
				: new PianoKeyEvent(-1, previous, 0);
		}
	}
}

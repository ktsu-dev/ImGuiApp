// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

public static partial class ImGuiWidgets
{
	/// <summary>Where one key sits along the keyboard, in white-key widths from the lowest key's left edge.</summary>
	/// <param name="Start">The key's left edge, inclusive.</param>
	/// <param name="End">The key's right edge, exclusive.</param>
	public readonly record struct PianoKeySpan(float Start, float End);

	/// <summary>
	/// The geometry of a keyboard from one white key to another, with no ImGui dependency, so a piano
	/// roll can lay its rows out from the same numbers the keyboard draws with.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Positions are measured in white-key widths along the keyboard and as a fraction of the key's
	/// length across it, so nothing here knows how big the keyboard is drawn. White key <c>i</c>,
	/// counted from the left, spans <c>[i, i + 1)</c>.
	/// </para>
	/// <para>
	/// Every black key is centred on the boundary between the white keys either side of it. A real
	/// piano staggers them; this does not, so the geometry is one rule and a piano roll's rows line
	/// up with it.
	/// </para>
	/// <para>Immutable: a layout can be shared between widgets and threads.</para>
	/// </remarks>
	public sealed class PianoKeyboardLayout
	{
		/// <summary>Width of a black key as a fraction of a white key's width.</summary>
		public const float BlackKeyWidth = 0.6f;

		/// <summary>Length of a black key as a fraction of a white key's length.</summary>
		public const float BlackKeyLength = 0.62f;

		private const int HighestMidiNote = 127;

		private static readonly string[] PitchNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

		// How many white keys lie below each pitch class within its octave. A black key shares the
		// count of the white key above it, which is the boundary it is centred on.
		private static readonly int[] WhitesBelowInOctave = [0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6];

		private readonly PianoKeySpan[] spans;
		private readonly int[] whiteNotes;

		/// <summary>Creates a layout. Both ends must be white keys, 0 &lt;= lowestNote &lt; highestNote &lt;= 127.</summary>
		/// <param name="lowestNote">The MIDI note of the leftmost key.</param>
		/// <param name="highestNote">The MIDI note of the rightmost key.</param>
		/// <exception cref="ArgumentOutOfRangeException">An end is out of range, is a black key, or the range is empty.</exception>
		public PianoKeyboardLayout(int lowestNote = 48, int highestNote = 84)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(lowestNote);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(highestNote, HighestMidiNote);
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lowestNote, highestNote);

			if (IsBlackKey(lowestNote))
			{
				throw new ArgumentOutOfRangeException(nameof(lowestNote), lowestNote, "The lowest note must be a white key.");
			}

			if (IsBlackKey(highestNote))
			{
				throw new ArgumentOutOfRangeException(nameof(highestNote), highestNote, "The highest note must be a white key.");
			}

			LowestNote = lowestNote;
			HighestNote = highestNote;
			KeyCount = highestNote - lowestNote + 1;
			WhiteKeyCount = WhitesBelow(highestNote) - WhitesBelow(lowestNote) + 1;

			spans = new PianoKeySpan[KeyCount];
			whiteNotes = new int[WhiteKeyCount];

			int baseline = WhitesBelow(lowestNote);
			for (int note = lowestNote; note <= highestNote; note++)
			{
				int index = WhitesBelow(note) - baseline;

				if (IsBlackKey(note))
				{
					// Index is the white key above, so the key straddles that key's left edge.
					spans[note - lowestNote] = new PianoKeySpan(index - (BlackKeyWidth / 2f), index + (BlackKeyWidth / 2f));
				}
				else
				{
					spans[note - lowestNote] = new PianoKeySpan(index, index + 1);
					whiteNotes[index] = note;
				}
			}
		}

		/// <summary>Gets the MIDI note of the leftmost key.</summary>
		public int LowestNote { get; }

		/// <summary>Gets the MIDI note of the rightmost key.</summary>
		public int HighestNote { get; }

		/// <summary>Gets the number of notes, white and black, from lowest to highest inclusive.</summary>
		public int KeyCount { get; }

		/// <summary>Gets the number of white keys; the keyboard is this many white-key widths wide.</summary>
		public int WhiteKeyCount { get; }

		/// <summary>True for C#, D#, F#, G# and A# in every octave.</summary>
		/// <param name="note">A MIDI note. Negative notes are allowed and follow the same pattern.</param>
		/// <returns>True when the note is a black key.</returns>
		public static bool IsBlackKey(int note) => PitchClass(note) is 1 or 3 or 6 or 8 or 10;

		/// <summary>Scientific pitch name with middle C (60) as C4 and sharps for black keys: 61 is "C#4", 21 is "A0", 0 is "C-1".</summary>
		/// <param name="note">A MIDI note.</param>
		/// <returns>The note's name.</returns>
		public static string NoteName(int note) =>
			PitchNames[PitchClass(note)] + (Octave(note) - 1).ToString(CultureInfo.InvariantCulture);

		/// <summary>True when the note lies in this layout's range.</summary>
		/// <param name="note">A MIDI note.</param>
		/// <returns>True when the note is drawn by this layout.</returns>
		public bool Contains(int note) => note >= LowestNote && note <= HighestNote;

		/// <summary>The note's span along the keyboard in white-key widths.</summary>
		/// <param name="note">A MIDI note in the layout's range.</param>
		/// <returns>The span the key covers.</returns>
		/// <exception cref="ArgumentOutOfRangeException">The note is outside the layout.</exception>
		public PianoKeySpan GetKeySpan(int note)
		{
			if (!Contains(note))
			{
				throw new ArgumentOutOfRangeException(nameof(note), note, "The note is outside the keyboard.");
			}

			return spans[note - LowestNote];
		}

		/// <summary>
		/// The note under a point. <paramref name="along"/> is in white-key widths from the left edge;
		/// <paramref name="across"/> is 0 at the back (top) of the keys and 1 at the front edge.
		/// Returns -1 outside the keyboard.
		/// </summary>
		/// <param name="along">Distance from the left edge, in white-key widths.</param>
		/// <param name="across">Distance from the back of the keys, as a fraction of their length.</param>
		/// <returns>The MIDI note under the point, or -1.</returns>
		/// <remarks>
		/// A black key wins over the white key beneath it. A point exactly on a white-key boundary
		/// belongs to the key on its right, and the keyboard's right edge is outside it.
		/// </remarks>
		public int HitTest(float along, float across)
		{
			// Written so a NaN fails every comparison and lands outside.
			if (!(along >= 0f && along < WhiteKeyCount && across >= 0f && across <= 1f))
			{
				return -1;
			}

			int column = (int)MathF.Floor(along);

			if (across < BlackKeyLength)
			{
				// Only the boundaries either side of the column can hold a black key reaching this far.
				for (int boundary = column; boundary <= column + 1; boundary++)
				{
					if (boundary < 1 || boundary >= WhiteKeyCount)
					{
						continue;
					}

					int black = whiteNotes[boundary - 1] + 1;
					if (IsBlackKey(black))
					{
						PianoKeySpan span = spans[black - LowestNote];
						if (along >= span.Start && along < span.End)
						{
							return black;
						}
					}
				}
			}

			return whiteNotes[column];
		}

		/// <summary>The velocity a press at <paramref name="across"/> on <paramref name="note"/> plays, 1..127.</summary>
		/// <param name="note">The MIDI note pressed.</param>
		/// <param name="across">Where along the key's length the press landed, 0 at the back and 1 at the front edge.</param>
		/// <returns>The velocity: 1 at the back of the key, 127 at its front edge, 64 in the middle.</returns>
		/// <remarks>
		/// A black key is shorter, so its own length is the scale: its front edge plays 127 too. A NaN
		/// position plays the middle velocity.
		/// </remarks>
		[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Deliberately an instance member: velocity is a question asked of a key on this keyboard, alongside HitTest and GetKeySpan, and a layout with its own key lengths would answer it from instance data without breaking callers.")]
		public int VelocityAt(int note, float across)
		{
			float fraction = IsBlackKey(note) ? across / BlackKeyLength : across;
			fraction = float.IsNaN(fraction) ? 0.5f : Math.Clamp(fraction, 0f, 1f);
			return 1 + (int)MathF.Round(126f * fraction);
		}

		private static int PitchClass(int note) => ((note % 12) + 12) % 12;

		private static int Octave(int note) => (note - PitchClass(note)) / 12;

		private static int WhitesBelow(int note) => (Octave(note) * 7) + WhitesBelowInOctave[PitchClass(note)];
	}
}

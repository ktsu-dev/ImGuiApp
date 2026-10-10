// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>What a keyboard did this frame. A glissando step carries both an off and an on.</summary>
	/// <param name="NoteOn">The MIDI note that started sounding this frame, or -1.</param>
	/// <param name="NoteOff">The MIDI note that stopped sounding this frame, or -1.</param>
	/// <param name="Velocity">Velocity of <paramref name="NoteOn"/>, 1..127; 0 when there is no note on.</param>
	public readonly record struct PianoKeyEvent(int NoteOn, int NoteOff, int Velocity)
	{
		/// <summary>Gets the event for a frame in which no note started or stopped.</summary>
		public static PianoKeyEvent None { get; } = new(-1, -1, 0);

		/// <summary>Gets a value indicating whether a note started sounding this frame.</summary>
		public bool HasNoteOn => NoteOn >= 0;

		/// <summary>Gets a value indicating whether a note stopped sounding this frame.</summary>
		public bool HasNoteOff => NoteOff >= 0;
	}

	/// <summary>
	/// Draws a keyboard and plays it with the pointer. The interaction (which note the pointer holds)
	/// is kept per <paramref name="label"/> id; the layout is caller-owned and never modified.
	/// </summary>
	/// <param name="label">ID and probe name. Nothing is drawn for the label text.</param>
	/// <param name="layout">The notes to draw.</param>
	/// <param name="size">Size in pixels. A component &lt;= 0 means: width = available content width, height = 5 text line heights.</param>
	/// <param name="heldNotes">Notes the caller reports as sounding (for example from MIDI input), highlighted like pressed keys.</param>
	/// <returns>The notes started and stopped by the pointer this frame, or <see cref="PianoKeyEvent.None"/>.</returns>
	/// <remarks>
	/// <para>
	/// A press plays the key under the pointer, and dragging slides the note from key to key. The
	/// velocity comes from how far down the key the press lands: the back of a key plays 1 and its
	/// front edge 127. Only the left button plays, and dragging off the keyboard stops the note.
	/// </para>
	/// <para>
	/// <paramref name="heldNotes"/> is only drawn. The widget never remembers it and never reports it
	/// back as events; notes outside the layout, and duplicates, are ignored.
	/// </para>
	/// <para>
	/// The whole keyboard is marked for probes as <paramref name="label"/>, and each key as
	/// <c>{label}/{note name}</c>, for example <c>Keys/C#4</c>.
	/// </para>
	/// </remarks>
	public static PianoKeyEvent PianoKeyboard(string label, PianoKeyboardLayout layout, Vector2 size = default, ReadOnlySpan<int> heldNotes = default) =>
		PianoKeyboardImpl.Draw(label, layout, size, heldNotes);

	internal static class PianoKeyboardImpl
	{
		private static readonly Dictionary<uint, PianoKeyboardInteraction> States = [];

		// Fixed rather than taken from the style: a keyboard whose white keys follow FrameBg stops
		// reading as a keyboard in a dark theme. Only the highlight and the outlines follow it.
		private static readonly Srgb WhiteKeyColor = new(0.96f, 0.96f, 0.96f);
		private static readonly Srgb BlackKeyColor = new(0.10f, 0.10f, 0.10f);

		public static PianoKeyEvent Draw(string label, PianoKeyboardLayout layout, Vector2 size, ReadOnlySpan<int> heldNotes)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(layout);

			float width = size.X > 0f ? size.X : ImGui.GetContentRegionAvail().X;
			float height = size.Y > 0f ? size.Y : 5f * ImGui.GetTextLineHeight();

			if (width < 1f || height < 1f)
			{
				// Still one item, so the probe records something where the keyboard would be.
				ImGui.Dummy(Vector2.One);
				ImGuiProbes.MarkItem(label);
				return PianoKeyEvent.None;
			}

			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 extent = new(width, height);
			ImGui.InvisibleButton(label, extent);
			ImGuiProbes.MarkItem(label);
			WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);

			Vector2 max = min + extent;
			float whiteWidth = width / layout.WhiteKeyCount;

			PianoKeyboardInteraction state = GetState(ImGui.GetID(label));
			PianoKeyEvent result = Interact(state, layout, min, whiteWidth, height, out int hovered);

			if (ImGui.IsItemHovered() && !ImGui.IsItemActive() && hovered >= 0)
			{
				ImGui.BeginTooltip();
				ImGui.TextUnformatted($"{PianoKeyboardLayout.NoteName(hovered)} ({hovered})");
				ImGui.EndTooltip();
			}

			Span<bool> lit = stackalloc bool[layout.KeyCount];
			Light(lit, layout, state.SoundingNote);
			foreach (int note in heldNotes)
			{
				Light(lit, layout, note);
			}

			DrawKeys(layout, lit, min, max, whiteWidth);
			MarkKeys(label, layout, min, max, whiteWidth);

			return result;
		}

		private static PianoKeyboardInteraction GetState(uint id)
		{
			if (!States.TryGetValue(id, out PianoKeyboardInteraction? state))
			{
				state = new PianoKeyboardInteraction();
				States[id] = state;
			}

			return state;
		}

		private static PianoKeyEvent Interact(
			PianoKeyboardInteraction state,
			PianoKeyboardLayout layout,
			Vector2 min,
			float whiteWidth,
			float height,
			out int hovered)
		{
			Vector2 pointer = ImGui.GetIO().MousePos;
			float along = (pointer.X - min.X) / whiteWidth;
			float across = (pointer.Y - min.Y) / height;

			hovered = layout.HitTest(along, across);
			int velocity = hovered >= 0 ? layout.VelocityAt(hovered, across) : 0;

			// Every frame, not only when the pointer moved: a key can move under a still pointer.
			if (ImGui.IsItemActivated())
			{
				return state.Press(hovered, velocity);
			}

			if (ImGui.IsItemActive())
			{
				return state.MoveTo(hovered, velocity);
			}

			// The deactivating frame, and also the first frame back after the widget stopped being
			// drawn mid-press, when ImGui has already dropped the item without deactivating it here.
			return state.IsHeld ? state.Release() : PianoKeyEvent.None;
		}

		private static void Light(Span<bool> lit, PianoKeyboardLayout layout, int note)
		{
			if (layout.Contains(note))
			{
				lit[note - layout.LowestNote] = true;
			}
		}

		private static (Vector2 Min, Vector2 Max) KeyRect(PianoKeyboardLayout layout, int note, Vector2 min, Vector2 max, float whiteWidth)
		{
			PianoKeySpan span = layout.GetKeySpan(note);
			float bottom = PianoKeyboardLayout.IsBlackKey(note)
				? min.Y + ((max.Y - min.Y) * PianoKeyboardLayout.BlackKeyLength)
				: max.Y;

			return (new Vector2(min.X + (span.Start * whiteWidth), min.Y), new Vector2(min.X + (span.End * whiteWidth), bottom));
		}

		private static void DrawKeys(PianoKeyboardLayout layout, ReadOnlySpan<bool> lit, Vector2 min, Vector2 max, float whiteWidth)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			uint whiteFill = WhiteKeyColor.ToImColor(1.0f).ToImGuiU32();
			uint blackFill = BlackKeyColor.ToImColor(1.0f).ToImGuiU32();
			uint highlight = ImGui.GetColorU32(colors[(int)ImGuiCol.ButtonActive]);
			uint border = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

			// White keys first, highlighted ones included, so the black keys cover the highlights.
			for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
			{
				if (!PianoKeyboardLayout.IsBlackKey(note))
				{
					(Vector2 keyMin, Vector2 keyMax) = KeyRect(layout, note, min, max, whiteWidth);
					drawList.AddRectFilled(keyMin, keyMax, lit[note - layout.LowestNote] ? highlight : whiteFill);
				}
			}

			// A separate pass, so a neighbour's fill never paints over an outline.
			for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
			{
				if (!PianoKeyboardLayout.IsBlackKey(note))
				{
					(Vector2 keyMin, Vector2 keyMax) = KeyRect(layout, note, min, max, whiteWidth);
					drawList.AddRect(keyMin, keyMax, border);
				}
			}

			DrawOctaveLabels(drawList, layout, lit, min, max, whiteWidth, colors);

			for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
			{
				if (PianoKeyboardLayout.IsBlackKey(note))
				{
					(Vector2 keyMin, Vector2 keyMax) = KeyRect(layout, note, min, max, whiteWidth);
					drawList.AddRectFilled(keyMin, keyMax, lit[note - layout.LowestNote] ? highlight : blackFill);
				}
			}

			drawList.AddRect(min, max, border);
		}

		private static void DrawOctaveLabels(ImDrawListPtr drawList, PianoKeyboardLayout layout, ReadOnlySpan<bool> lit, Vector2 min, Vector2 max, float whiteWidth, ReadOnlySpan<Vector4> colors)
		{
			// Sized against the widest name a C can have, so either every C is labelled or none is.
			if (whiteWidth < ImGui.CalcTextSize("C-1").X + 4f)
			{
				return;
			}

			// Muted on an ivory key, but the full text colour on a held one: the disabled colour is
			// a mid grey, which all but disappears against the highlight.
			uint textColor = ImGui.GetColorU32(colors[(int)ImGuiCol.TextDisabled]);
			uint litTextColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Text]);

			for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
			{
				if (note % 12 != 0)
				{
					continue;
				}

				string name = PianoKeyboardLayout.NoteName(note);
				Vector2 textSize = ImGui.CalcTextSize(name);
				(Vector2 keyMin, Vector2 keyMax) = KeyRect(layout, note, min, max, whiteWidth);
				float centre = (keyMin.X + keyMax.X) / 2f;
				drawList.AddText(new Vector2(centre - (textSize.X / 2f), keyMax.Y - 2f - textSize.Y), lit[note - layout.LowestNote] ? litTextColor : textColor, name);
			}
		}

		private static void MarkKeys(string label, PianoKeyboardLayout layout, Vector2 min, Vector2 max, float whiteWidth)
		{
			// Naming every key allocates, so skip it entirely when nothing is listening.
			if (!ImGuiProbes.IsRecording)
			{
				return;
			}

			for (int note = layout.LowestNote; note <= layout.HighestNote; note++)
			{
				(Vector2 keyMin, Vector2 keyMax) = KeyRect(layout, note, min, max, whiteWidth);
				ImGuiProbes.MarkRegion($"{label}/{PianoKeyboardLayout.NoteName(note)}", keyMin, keyMax);
			}
		}
	}
}

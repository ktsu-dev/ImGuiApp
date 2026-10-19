// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Models;

/// <summary>
/// Translates between <c>ktsu.Keybinding</c> chords and ImGui's keys: whether one was pressed this
/// frame, which one the keyboard just pressed, and how a menu names one.
/// </summary>
/// <remarks>
/// A chord matches when its modifiers are exactly the ones held, so Ctrl+Z does not fire for
/// Ctrl+Shift+Z, every other key in it is down, and at least one of them went down this frame.
/// Shared by the node editor and the data table, and public so an application binding its own
/// commands reads the same keys the same way rather than keeping a copy.
/// </remarks>
public static class KeyChordMatcher
{
	private const string Ctrl = "CTRL";
	private const string Alt = "ALT";
	private const string Shift = "SHIFT";
	private const string Meta = "META";

	// The symbol keys, named by the symbol a chord is written with. A chord written Ctrl+= or Ctrl+/
	// keeps the symbol as the note's name, ImGui only knows these keys by their spelled-out names, and
	// a menu reads better with the symbol, so the table is used both ways.
	private static readonly Dictionary<string, ImGuiKey> Symbols = new(StringComparer.Ordinal)
	{
		["'"] = ImGuiKey.Apostrophe,
		[","] = ImGuiKey.Comma,
		["-"] = ImGuiKey.Minus,
		["."] = ImGuiKey.Period,
		["/"] = ImGuiKey.Slash,
		[";"] = ImGuiKey.Semicolon,
		["="] = ImGuiKey.Equal,
		["["] = ImGuiKey.LeftBracket,
		["\\"] = ImGuiKey.Backslash,
		["]"] = ImGuiKey.RightBracket,
		["`"] = ImGuiKey.GraveAccent,
	};

	// The names keymaps give keys whose ImGui names differ.
	private static readonly Dictionary<string, ImGuiKey> Aliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["ESC"] = ImGuiKey.Escape,
		["RETURN"] = ImGuiKey.Enter,
		["DEL"] = ImGuiKey.Delete,
		["INS"] = ImGuiKey.Insert,
		["UP"] = ImGuiKey.UpArrow,
		["DOWN"] = ImGuiKey.DownArrow,
		["LEFT"] = ImGuiKey.LeftArrow,
		["RIGHT"] = ImGuiKey.RightArrow,
		["ARROWUP"] = ImGuiKey.UpArrow,
		["ARROWDOWN"] = ImGuiKey.DownArrow,
		["ARROWLEFT"] = ImGuiKey.LeftArrow,
		["ARROWRIGHT"] = ImGuiKey.RightArrow,
		["PGUP"] = ImGuiKey.PageUp,
		["PGDN"] = ImGuiKey.PageDown,
		["SPACEBAR"] = ImGuiKey.Space,
	};

	private static readonly Dictionary<ImGuiKey, string> SymbolNames = Symbols.ToDictionary(static pair => pair.Value, static pair => pair.Key);

	/// <summary>Reports whether a chord was pressed this frame.</summary>
	/// <param name="chord">The chord to test.</param>
	/// <param name="canRepeat">
	/// Whether a held chord fires again at the platform's repeat rate. Off for commands that should act
	/// once per press, such as undo, and on for movement, where holding an arrow should keep moving.
	/// </param>
	/// <param name="isShiftIgnored">
	/// Whether Shift may be held or not without affecting the match, for a chord that doesn't name Shift.
	/// Lets one movement chord serve for moving and, with Shift, for extending a selection. A chord that
	/// names Shift still needs it held, and every other modifier must still match.
	/// </param>
	/// <returns>True when the chord was pressed this frame.</returns>
	public static bool IsPressed(Chord chord, bool canRepeat = false, bool isShiftIgnored = false)
	{
		Ensure.NotNull(chord);
		if (!TrySplit(chord, out KeyChordModifiers modifiers, out List<ImGuiKey> keys))
		{
			return false;
		}

		ImGuiIOPtr io = ImGui.GetIO();
		// Shift is only optional for a chord that doesn't ask for it.
		bool isShiftMatched = io.KeyShift == modifiers.Shift || (isShiftIgnored && !modifiers.Shift);
		if (io.KeyCtrl != modifiers.Ctrl || io.KeyAlt != modifiers.Alt || io.KeySuper != modifiers.Meta || !isShiftMatched)
		{
			return false;
		}

		return keys.All(ImGui.IsKeyDown) && keys.Any(key => ImGui.IsKeyPressed(key, canRepeat));
	}

	/// <summary>Reads the chord the keyboard pressed this frame, for rebinding a command.</summary>
	/// <param name="chord">The chord: the modifiers held and the one other key that went down, or null.</param>
	/// <returns>True when a key other than a modifier went down this frame.</returns>
	/// <remarks>
	/// Modifiers alone never make a chord, so a user can hold Ctrl and Shift and then press the key
	/// without the capture ending early. Only the first key in ImGui's order is taken if two went down
	/// in one frame.
	/// </remarks>
	public static bool TryCapture(out Chord? chord)
	{
		chord = null;
		for (ImGuiKey key = ImGuiKey.NamedKeyBegin; key < ImGuiKey.GamepadStart; key++)
		{
			if (!IsBindable(key) || !ImGui.IsKeyPressed(key, false))
			{
				continue;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			chord = Compose(new KeyChordModifiers(io.KeyCtrl, io.KeyAlt, io.KeyShift, io.KeySuper), key);
			return true;
		}

		return false;
	}

	/// <summary>Builds the chord a key makes with a set of modifiers.</summary>
	/// <param name="modifiers">The modifiers held.</param>
	/// <param name="key">The key.</param>
	/// <returns>The chord, its key named so that <see cref="TryMapKey"/> maps it back to <paramref name="key"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="key"/> is a modifier, a mouse button, a gamepad input or a range marker.</exception>
	public static Chord Compose(KeyChordModifiers modifiers, ImGuiKey key)
	{
		if (!IsBindable(key))
		{
			throw new ArgumentOutOfRangeException(nameof(key), key, "Only a keyboard key other than a modifier can end a chord.");
		}

		List<Note> notes = [];
		if (modifiers.Ctrl)
		{
			notes.Add(new Note(Ctrl));
		}

		if (modifiers.Alt)
		{
			notes.Add(new Note(Alt));
		}

		if (modifiers.Shift)
		{
			notes.Add(new Note(Shift));
		}

		if (modifiers.Meta)
		{
			notes.Add(new Note(Meta));
		}

		notes.Add(new Note(NoteNameOf(key)));
		return new Chord(notes);
	}

	/// <summary>Formats a chord the way a menu shows it, such as Ctrl+Shift+S, Ctrl+] or Delete.</summary>
	/// <param name="chord">The chord, or null for none.</param>
	/// <returns>The text, or empty for no chord.</returns>
	/// <remarks>
	/// Modifiers come first, in the order Ctrl, Alt, Shift, Super, then the keys by <see cref="DisplayName"/>.
	/// A chord naming a key ImGui has no name for is shown as the keymap wrote it.
	/// </remarks>
	public static string Display(Chord? chord)
	{
		if (chord is null)
		{
			return string.Empty;
		}

		if (!TrySplit(chord, out KeyChordModifiers modifiers, out List<ImGuiKey> keys))
		{
			return chord.ToString();
		}

		List<string> parts = [];
		if (modifiers.Ctrl)
		{
			parts.Add("Ctrl");
		}

		if (modifiers.Alt)
		{
			parts.Add("Alt");
		}

		if (modifiers.Shift)
		{
			parts.Add("Shift");
		}

		if (modifiers.Meta)
		{
			parts.Add("Super");
		}

		parts.AddRange(keys.Select(DisplayName));
		return string.Join("+", parts);
	}

	/// <summary>Names a key the way a menu shows it: a letter, a digit, the symbol a symbol key types, or ImGui's own name.</summary>
	/// <param name="key">The key.</param>
	/// <returns>The name, such as A, 7, ], Up, Delete or F6.</returns>
	/// <remarks>The arrows are named for their direction alone, as menus write them, rather than ImGui's UpArrow.</remarks>
	public static string DisplayName(ImGuiKey key) => key switch
	{
		>= ImGuiKey.A and <= ImGuiKey.Z => ((char)('A' + (key - ImGuiKey.A))).ToString(),
		>= ImGuiKey.Key0 and <= ImGuiKey.Key9 => ((char)('0' + (key - ImGuiKey.Key0))).ToString(),
		_ when SymbolNames.TryGetValue(key, out string? symbol) => symbol,
		ImGuiKey.UpArrow => "Up",
		ImGuiKey.DownArrow => "Down",
		ImGuiKey.LeftArrow => "Left",
		ImGuiKey.RightArrow => "Right",
		// Tab shares its value with the NamedKeyBegin range marker, whose name ToString() returns instead.
		ImGuiKey.Tab => "Tab",
		_ => key.ToString(),
	};

	/// <summary>Splits a chord into its modifiers and the keys ImGui knows it by.</summary>
	/// <param name="chord">The chord.</param>
	/// <param name="modifiers">The modifiers it holds.</param>
	/// <param name="keys">The other keys, in the order the chord names them.</param>
	/// <returns>True when every note is a modifier or a key ImGui has, and at least one is a key.</returns>
	internal static bool TrySplit(Chord chord, out KeyChordModifiers modifiers, out List<ImGuiKey> keys)
	{
		Ensure.NotNull(chord);
		bool ctrl = false;
		bool alt = false;
		bool shift = false;
		bool meta = false;
		keys = [];
		foreach (string name in chord.Notes.Select(note => note.ToString()))
		{
			switch (name)
			{
				case Ctrl or "CONTROL":
					ctrl = true;
					break;

				case Alt:
					alt = true;
					break;

				case Shift:
					shift = true;
					break;

				case Meta or "WIN" or "WINDOWS" or "CMD" or "COMMAND" or "SUPER":
					meta = true;
					break;

				default:
					if (!TryMapKey(name, out ImGuiKey key))
					{
						// A key ImGui has no name for can never be pressed, so neither can the chord.
						modifiers = default;
						keys = [];
						return false;
					}

					keys.Add(key);
					break;
			}
		}

		modifiers = new KeyChordModifiers(ctrl, alt, shift, meta);
		return keys.Count > 0;
	}

	/// <summary>Finds the ImGui key a note names.</summary>
	/// <param name="name">The note's name, upper-cased as <c>ktsu.Keybinding</c> stores it.</param>
	/// <param name="key">The key.</param>
	/// <returns>True if ImGui has such a key.</returns>
	public static bool TryMapKey(string name, out ImGuiKey key)
	{
		Ensure.NotNull(name);
		if (name.Length == 1 && name[0] is >= 'A' and <= 'Z')
		{
			key = (ImGuiKey)((int)ImGuiKey.A + (name[0] - 'A'));
			return true;
		}

		if (name.Length == 1 && name[0] is >= '0' and <= '9')
		{
			key = (ImGuiKey)((int)ImGuiKey.Key0 + (name[0] - '0'));
			return true;
		}

		if (Symbols.TryGetValue(name, out key) || Aliases.TryGetValue(name, out key))
		{
			return true;
		}

		// Everything else by ImGui's own name, such as TAB, DELETE, BACKSPACE, F5, PAGEUP and COMMA, but
		// never a modifier, a mouse button or one of the range markers, none of which is a key to press.
		return Enum.TryParse(name, ignoreCase: true, out key) && IsBindable(key);
	}

	/// <summary>Whether a key can end a chord: a keyboard key that is not a modifier.</summary>
	/// <param name="key">The key.</param>
	/// <returns>True for a bindable key.</returns>
	/// <remarks>NamedKeyBegin aliases Tab itself, so the lower bound is inclusive.</remarks>
	public static bool IsBindable(ImGuiKey key) =>
		key is >= ImGuiKey.NamedKeyBegin and < ImGuiKey.GamepadStart
		and not (ImGuiKey.LeftCtrl or ImGuiKey.RightCtrl or ImGuiKey.LeftShift or ImGuiKey.RightShift
			or ImGuiKey.LeftAlt or ImGuiKey.RightAlt or ImGuiKey.LeftSuper or ImGuiKey.RightSuper);

	private static string NoteNameOf(ImGuiKey key) => key switch
	{
		>= ImGuiKey.A and <= ImGuiKey.Z => ((char)('A' + (key - ImGuiKey.A))).ToString(),
		>= ImGuiKey.Key0 and <= ImGuiKey.Key9 => ((char)('0' + (key - ImGuiKey.Key0))).ToString(),
		_ when SymbolNames.TryGetValue(key, out string? symbol) => symbol,
		ImGuiKey.Tab => "TAB",
		_ => key.ToString().ToUpperInvariant(),
	};
}

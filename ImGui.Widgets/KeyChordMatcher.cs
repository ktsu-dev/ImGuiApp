// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Models;

/// <summary>
/// Answers whether a <c>ktsu.Keybinding</c> chord was pressed this frame, in ImGui's terms.
/// </summary>
/// <remarks>
/// A chord matches when its modifiers are exactly the ones held, so Ctrl+Z does not fire for
/// Ctrl+Shift+Z, every other key in it is down, and at least one of them went down this frame.
/// Shared by the node editor and the data table, which is why it lives here rather than in either.
/// </remarks>
internal static class KeyChordMatcher
{
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

	/// <summary>Reports whether a chord was pressed this frame.</summary>
	/// <param name="chord">The chord to test.</param>
	/// <param name="canRepeat">
	/// Whether a held chord fires again at the platform's repeat rate. Off for commands that should act
	/// once per press, such as undo, and on for movement, where holding an arrow should keep moving.
	/// </param>
	/// <param name="isShiftIgnored">
	/// Whether Shift may be held or not without affecting the match. Lets one movement chord serve for
	/// moving and, with Shift, for extending a selection. Every other modifier must still match.
	/// </param>
	/// <returns>True when the chord was pressed this frame.</returns>
	public static bool IsPressed(Chord chord, bool canRepeat = false, bool isShiftIgnored = false)
	{
		Ensure.NotNull(chord);

		bool ctrl = false;
		bool alt = false;
		bool shift = false;
		bool meta = false;
		List<ImGuiKey> keys = [];

		foreach (string name in chord.Notes.Select(note => note.ToString()))
		{
			switch (name)
			{
				case "CTRL" or "CONTROL":
					ctrl = true;
					break;

				case "ALT":
					alt = true;
					break;

				case "SHIFT":
					shift = true;
					break;

				case "META" or "WIN" or "WINDOWS" or "CMD" or "COMMAND" or "SUPER":
					meta = true;
					break;

				default:
					if (!TryMapKey(name, out ImGuiKey key))
					{
						// A key ImGui has no name for can never be pressed, so neither can the chord.
						return false;
					}

					keys.Add(key);
					break;
			}
		}

		if (keys.Count == 0)
		{
			return false;
		}

		ImGuiIOPtr io = ImGui.GetIO();
		if (io.KeyCtrl != ctrl || io.KeyAlt != alt || io.KeySuper != meta || (!isShiftIgnored && io.KeyShift != shift))
		{
			return false;
		}

		return keys.All(ImGui.IsKeyDown) && keys.Any(key => ImGui.IsKeyPressed(key, canRepeat));
	}

	/// <summary>Finds the ImGui key a note names.</summary>
	/// <param name="name">The note's name, upper-cased as <c>ktsu.Keybinding</c> stores it.</param>
	/// <param name="key">The key.</param>
	/// <returns>True if ImGui has such a key.</returns>
	public static bool TryMapKey(string name, out ImGuiKey key)
	{
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

		if (Aliases.TryGetValue(name, out key))
		{
			return true;
		}

		// Everything else by ImGui's own name, such as TAB, DELETE, BACKSPACE, F5, PAGEUP and COMMA, but
		// never a modifier, a mouse button or one of the range markers, none of which is a key to press.
		// NamedKeyBegin aliases Tab itself, so the lower bound is inclusive.
		return Enum.TryParse(name, ignoreCase: true, out key)
			&& key >= ImGuiKey.NamedKeyBegin
			&& key < ImGuiKey.GamepadStart
			&& key is not (ImGuiKey.LeftCtrl or ImGuiKey.RightCtrl or ImGuiKey.LeftShift or ImGuiKey.RightShift
				or ImGuiKey.LeftAlt or ImGuiKey.RightAlt or ImGuiKey.LeftSuper or ImGuiKey.RightSuper);
	}
}

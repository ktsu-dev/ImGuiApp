// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using Hexa.NET.ImGui;
using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;

/// <summary>
/// The node editor's keyboard commands, in the form <c>ktsu.Keybinding</c> registers and binds.
/// </summary>
/// <remarks>
/// A <see cref="NodeEditorInputHandler"/> given an <see cref="IKeybindingService"/> reads each of
/// these commands' chords from the service's active profile, so the host's own keymap — whatever
/// profile the user picked, whatever they rebound — decides which keys drive the graph. Without a
/// service the handler uses <see cref="DefaultChords"/>, plus Backspace for delete and Ctrl+Shift+Z
/// for redo, which a profile holding one chord per command cannot express.
/// </remarks>
public static class NodeEditorCommands
{
	/// <summary>The category the commands are registered under.</summary>
	public const string Category = "Node Editor";

	/// <summary>Undo the last change to the graph.</summary>
	public const string Undo = "nodeeditor.undo";

	/// <summary>Redo the last change undone.</summary>
	public const string Redo = "nodeeditor.redo";

	/// <summary>Delete the selected nodes and links.</summary>
	public const string Delete = "nodeeditor.delete";

	/// <summary>Duplicate the selected nodes.</summary>
	public const string Duplicate = "nodeeditor.duplicate";

	/// <summary>Every command, ready to register.</summary>
	public static IReadOnlyList<Command> All { get; } =
	[
		new(Undo, "Undo", "Undo the last change to the graph", Category),
		new(Redo, "Redo", "Redo the last change undone", Category),
		new(Delete, "Delete Selection", "Delete the selected nodes and links", Category),
		new(Duplicate, "Duplicate Selection", "Duplicate the selected nodes", Category),
	];

	/// <summary>The chord each command is bound to unless the user says otherwise.</summary>
	public static IReadOnlyDictionary<string, string> DefaultChords { get; } = new Dictionary<string, string>
	{
		[Undo] = "Ctrl+Z",
		[Redo] = "Ctrl+Y",
		[Delete] = "Delete",
		[Duplicate] = "Ctrl+D",
	};

	/// <summary>
	/// Register the commands, and bind each one that has no chord yet to its default.
	/// </summary>
	/// <param name="registry">Where the host's commands are registered, such as <c>KeybindingManager.Commands</c>.</param>
	/// <param name="keybindings">The host's keybindings, such as <c>KeybindingManager.Keybindings</c>.</param>
	/// <param name="bindDefaultChords">False to register the commands and leave every chord to the host.</param>
	/// <returns>How many chords were bound.</returns>
	/// <remarks>
	/// Safe to call on every start-up: a command already registered is left as it is, and a chord the
	/// user has already bound — loaded from their saved profile — is not overwritten. Chords are
	/// bound in the active profile, so there has to be one for any to be bound.
	/// </remarks>
	public static int Register(ICommandRegistry registry, IKeybindingService keybindings, bool bindDefaultChords = true)
	{
		Ensure.NotNull(registry);
		Ensure.NotNull(keybindings);

		foreach (Command command in All.Where(c => !registry.IsCommandRegistered(c.Id)))
		{
			registry.RegisterCommand(command);
		}

		if (!bindDefaultChords)
		{
			return 0;
		}

		int bound = 0;
		foreach ((string commandId, string chord) in DefaultChords)
		{
			if (!keybindings.HasChordBinding(commandId) && keybindings.BindChord(commandId, keybindings.ParseChord(chord)))
			{
				bound++;
			}
		}

		return bound;
	}
}

/// <summary>
/// Answers whether a <c>ktsu.Keybinding</c> chord was pressed this frame, in ImGui's terms.
/// </summary>
/// <remarks>
/// A chord matches when its modifiers are exactly the ones held — so Ctrl+Z does not fire for
/// Ctrl+Shift+Z — every other key in it is down, and at least one of them went down this frame. Key
/// repeat is ignored, so holding a chord fires it once.
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

	public static bool IsPressed(Chord chord)
	{
		bool ctrl = false;
		bool alt = false;
		bool shift = false;
		bool meta = false;
		List<ImGuiKey> keys = [];

		foreach (Note note in chord.Notes)
		{
			string name = note.ToString();
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
		if (io.KeyCtrl != ctrl || io.KeyAlt != alt || io.KeyShift != shift || io.KeySuper != meta)
		{
			return false;
		}

		return keys.All(ImGui.IsKeyDown) && keys.Any(key => ImGui.IsKeyPressed(key, repeat: false));
	}

	/// <summary>Find the ImGui key a note names.</summary>
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

		// Everything else by ImGui's own name — DELETE, BACKSPACE, F5, PAGEUP, COMMA — but never a
		// modifier, a mouse button or one of the range markers, none of which is a key to press.
		return Enum.TryParse(name, ignoreCase: true, out key)
			&& key > ImGuiKey.NamedKeyBegin
			&& key < ImGuiKey.GamepadStart
			&& key is not (ImGuiKey.LeftCtrl or ImGuiKey.RightCtrl or ImGuiKey.LeftShift or ImGuiKey.RightShift
				or ImGuiKey.LeftAlt or ImGuiKey.RightAlt or ImGuiKey.LeftSuper or ImGuiKey.RightSuper);
	}
}

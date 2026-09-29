// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Collections.Generic;
using System.Linq;
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

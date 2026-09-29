// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;
using System.Linq;

using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;

public static partial class ImGuiWidgets
{
	/// <summary>The data table's keyboard commands, in the form <c>ktsu.Keybinding</c> registers and binds.</summary>
	/// <remarks>
	/// A table given an <see cref="IKeybindingService"/> through <c>DataTableOptions.Keybindings</c>
	/// reads each command's chord from the service's active profile, so the host's keymap decides which
	/// keys drive it. Without a service the table uses <see cref="DefaultChords"/>, plus Enter to begin
	/// editing, which a profile holding one chord per command can't express alongside F2.
	/// </remarks>
	public static class DataTableCommands
	{
		/// <summary>The category the commands are registered under.</summary>
		public const string Category = "Data Table";

		/// <summary>Move the active cell up one row.</summary>
		public const string MoveUp = "datatable.moveup";

		/// <summary>Move the active cell down one row.</summary>
		public const string MoveDown = "datatable.movedown";

		/// <summary>Move the active cell left one column.</summary>
		public const string MoveLeft = "datatable.moveleft";

		/// <summary>Move the active cell right one column.</summary>
		public const string MoveRight = "datatable.moveright";

		/// <summary>Move the active cell up one page.</summary>
		public const string PageUp = "datatable.pageup";

		/// <summary>Move the active cell down one page.</summary>
		public const string PageDown = "datatable.pagedown";

		/// <summary>Move to the first column of the row.</summary>
		public const string Home = "datatable.home";

		/// <summary>Move to the last column of the row.</summary>
		public const string End = "datatable.end";

		/// <summary>Move to the first cell of the table.</summary>
		public const string First = "datatable.first";

		/// <summary>Move to the last cell of the table.</summary>
		public const string Last = "datatable.last";

		/// <summary>Move to the next cell, wrapping to the next row.</summary>
		public const string Next = "datatable.next";

		/// <summary>Move to the previous cell, wrapping to the previous row.</summary>
		public const string Previous = "datatable.previous";

		/// <summary>Begin editing the active cell.</summary>
		public const string BeginEdit = "datatable.beginedit";

		/// <summary>Commit the edit and move down.</summary>
		public const string Commit = "datatable.commit";

		/// <summary>Cancel the edit.</summary>
		public const string Cancel = "datatable.cancel";

		/// <summary>Flip the active cell when it's a checkbox.</summary>
		public const string Toggle = "datatable.toggle";

		/// <summary>Copy the selected rows as tab-separated text.</summary>
		public const string Copy = "datatable.copy";

		/// <summary>Select every row shown.</summary>
		public const string SelectAll = "datatable.selectall";

		/// <summary>Gets every command, ready to register.</summary>
		public static IReadOnlyList<Command> All { get; } =
		[
			new(MoveUp, "Move Up", "Move the active cell up one row", Category),
			new(MoveDown, "Move Down", "Move the active cell down one row", Category),
			new(MoveLeft, "Move Left", "Move the active cell left one column", Category),
			new(MoveRight, "Move Right", "Move the active cell right one column", Category),
			new(PageUp, "Page Up", "Move the active cell up one page", Category),
			new(PageDown, "Page Down", "Move the active cell down one page", Category),
			new(Home, "Row Start", "Move to the first column of the row", Category),
			new(End, "Row End", "Move to the last column of the row", Category),
			new(First, "Table Start", "Move to the first cell of the table", Category),
			new(Last, "Table End", "Move to the last cell of the table", Category),
			new(Next, "Next Cell", "Move to the next cell, wrapping to the next row", Category),
			new(Previous, "Previous Cell", "Move to the previous cell, wrapping to the previous row", Category),
			new(BeginEdit, "Edit Cell", "Begin editing the active cell", Category),
			new(Commit, "Commit Edit", "Commit the edit and move down", Category),
			new(Cancel, "Cancel Edit", "Cancel the edit", Category),
			new(Toggle, "Toggle Cell", "Flip the active cell when it is a checkbox", Category),
			new(Copy, "Copy Rows", "Copy the selected rows as tab-separated text", Category),
			new(SelectAll, "Select All Rows", "Select every row shown", Category),
		];

		/// <summary>Gets the chord each command is bound to unless the user says otherwise.</summary>
		public static IReadOnlyDictionary<string, string> DefaultChords { get; } = new Dictionary<string, string>
		{
			[MoveUp] = "Up",
			[MoveDown] = "Down",
			[MoveLeft] = "Left",
			[MoveRight] = "Right",
			[PageUp] = "PageUp",
			[PageDown] = "PageDown",
			[Home] = "Home",
			[End] = "End",
			[First] = "Ctrl+Home",
			[Last] = "Ctrl+End",
			[Next] = "Tab",
			[Previous] = "Shift+Tab",
			[BeginEdit] = "F2",
			[Commit] = "Enter",
			[Cancel] = "Escape",
			[Toggle] = "Space",
			[Copy] = "Ctrl+C",
			[SelectAll] = "Ctrl+A",
		};

		// Parsed once, after DefaultChords, which static initialization order guarantees because both are
		// initialized in the order they're written.
		private static readonly Dictionary<string, Chord> ParsedDefaults =
			DefaultChords.ToDictionary(pair => pair.Key, pair => Chord.Parse(pair.Value));

		/// <summary>Registers the commands, and binds each one that has no chord yet to its default.</summary>
		/// <param name="registry">Where the host's commands are registered.</param>
		/// <param name="keybindings">The host's keybindings.</param>
		/// <param name="bindDefaultChords">False to register the commands and leave every chord to the host.</param>
		/// <returns>How many chords were bound.</returns>
		/// <remarks>
		/// Safe to call on every start-up. A command already registered is left alone, and a chord the
		/// user already bound is not overwritten. Chords are bound in the active profile, so there has to
		/// be one for any to be bound.
		/// </remarks>
		public static int Register(ICommandRegistry registry, IKeybindingService keybindings, bool bindDefaultChords = true)
		{
			Ensure.NotNull(registry);
			Ensure.NotNull(keybindings);

			foreach (Command command in All.Where(command => !registry.IsCommandRegistered(command.Id)))
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

		/// <summary>Gets a command's default chord, for a table with no keymap.</summary>
		internal static Chord? DefaultChordOf(string commandId) =>
			ParsedDefaults.TryGetValue(commandId, out Chord? chord) ? chord : null;
	}
}

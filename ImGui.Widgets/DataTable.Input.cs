// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Keybinding.Core.Contracts;
using ktsu.Keybinding.Core.Models;

public static partial class ImGuiWidgets
{
	/// <summary>A cell's rectangle on screen, in display pixels.</summary>
	internal readonly record struct CellRect(Vector2 Min, Vector2 Max);

	/// <summary>Turns the keyboard and clicks outside the editor into data table commands.</summary>
	internal static class DataTableInput
	{
		// Movements that Shift extends into a selection, which is why they're matched with Shift
		// ignored. Tab and Shift+Tab are separate commands and are not in this list.
		private static readonly (string Command, DataTableMove Move)[] ExtendableMoves =
		[
			(DataTableCommands.MoveUp, DataTableMove.Up),
			(DataTableCommands.MoveDown, DataTableMove.Down),
			(DataTableCommands.MoveLeft, DataTableMove.Left),
			(DataTableCommands.MoveRight, DataTableMove.Right),
			(DataTableCommands.PageUp, DataTableMove.PageUp),
			(DataTableCommands.PageDown, DataTableMove.PageDown),
			(DataTableCommands.Home, DataTableMove.Home),
			(DataTableCommands.End, DataTableMove.End),
			(DataTableCommands.First, DataTableMove.First),
			(DataTableCommands.Last, DataTableMove.Last),
		];

		/// <summary>Handles this frame's input. Called inside the table, after the rows are drawn.</summary>
		/// <param name="state">The table's state.</param>
		/// <param name="options">The table's options, for the keymap.</param>
		/// <param name="editorRect">Where the editor was drawn this frame, or <see langword="null"/> if it wasn't.</param>
		internal static void Handle<TRow>(DataTableState<TRow> state, DataTableOptions options, CellRect? editorRect)
		{
			// Checked before focus, because a click outside the table is exactly the case that takes focus
			// away from it.
			if (state.IsEditing && editorRect is CellRect rect && IsClickedAway(rect))
			{
				state.CommitEdit();
			}

			if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows))
			{
				return;
			}

			if (state.IsEditing)
			{
				HandleEditingKeys(state, options);
				return;
			}

			// A filter box has the keyboard. Its keys are its own.
			if (ImGui.GetIO().WantTextInput)
			{
				return;
			}

			HandleIdleKeys(state, options);
		}

		private static void HandleEditingKeys<TRow>(DataTableState<TRow> state, DataTableOptions options)
		{
			if (IsPressed(options, DataTableCommands.Cancel))
			{
				state.CancelEdit();
			}
			else if (IsPressed(options, DataTableCommands.Commit))
			{
				state.CommitEdit();
				state.Move(DataTableMove.Down);
			}
			else if (IsPressed(options, DataTableCommands.Next))
			{
				state.Move(DataTableMove.Next);
			}
			else if (IsPressed(options, DataTableCommands.Previous))
			{
				state.Move(DataTableMove.Previous);
			}
		}

		private static void HandleIdleKeys<TRow>(DataTableState<TRow> state, DataTableOptions options)
		{
			bool isShiftHeld = ImGui.GetIO().KeyShift;

			foreach ((string command, DataTableMove move) in ExtendableMoves)
			{
				if (IsPressed(options, command, canRepeat: true, isShiftIgnored: true))
				{
					state.Move(move, isExtending: isShiftHeld);
					return;
				}
			}

			if (IsPressed(options, DataTableCommands.Next, canRepeat: true))
			{
				state.Move(DataTableMove.Next);
			}
			else if (IsPressed(options, DataTableCommands.Previous, canRepeat: true))
			{
				state.Move(DataTableMove.Previous);
			}
			else if (IsPressed(options, DataTableCommands.BeginEdit) || IsEnterWithoutKeymap(options))
			{
				state.BeginEdit();
			}
			else if (IsPressed(options, DataTableCommands.Toggle))
			{
				state.Toggle();
			}
			else if (IsPressed(options, DataTableCommands.Copy))
			{
				string text = state.BuildCopyText();
				if (text.Length > 0)
				{
					ImGui.SetClipboardText(text);
				}
			}
			else if (IsPressed(options, DataTableCommands.SelectAll))
			{
				state.SelectAll();
			}
			else if (TryTakeTypedCharacter(out char typed))
			{
				state.BeginEdit(typed);
			}
		}

		private static bool IsPressed(DataTableOptions options, string command, bool canRepeat = false, bool isShiftIgnored = false)
		{
			Chord? chord = options.Keybindings is IKeybindingService keybindings
				? keybindings.GetChord(command)
				: DataTableCommands.DefaultChordOf(command);

			return chord is not null && KeyChordMatcher.IsPressed(chord, canRepeat, isShiftIgnored);
		}

		// Enter begins editing as well as F2 when there's no keymap. A profile holds one chord per
		// command, so with a keymap only the bound chord counts.
		private static bool IsEnterWithoutKeymap(DataTableOptions options) =>
			options.Keybindings is null && ImGui.IsKeyPressed(ImGuiKey.Enter, false);

		/// <summary>Takes the first printable character typed this frame, which begins an edit with it.</summary>
		private static bool TryTakeTypedCharacter(out char typed)
		{
			typed = default;
			ImGuiIOPtr io = ImGui.GetIO();

			// Space is the toggle chord, and a character typed with Ctrl or Alt held is a shortcut.
			if (io.KeyCtrl || io.KeyAlt || io.KeySuper || io.InputQueueCharacters.Size == 0)
			{
				return false;
			}

			// A codepoint above U+FFFF doesn't fit in one char, and a lone surrogate isn't a character, so
			// neither can begin an edit.
			uint codepoint = io.InputQueueCharacters[0];
			if (codepoint > char.MaxValue || char.IsSurrogate((char)codepoint))
			{
				return false;
			}

			typed = (char)codepoint;
			return !char.IsControl(typed) && !char.IsWhiteSpace(typed);
		}

		private static bool IsClickedAway(CellRect rect) =>
			ImGui.IsMouseClicked(ImGuiMouseButton.Left)
			&& !ImGui.IsMouseHoveringRect(rect.Min, rect.Max, false)
			&& !ImGui.IsPopupOpen(string.Empty, ImGuiPopupFlags.AnyPopupId | ImGuiPopupFlags.AnyPopupLevel);
	}
}

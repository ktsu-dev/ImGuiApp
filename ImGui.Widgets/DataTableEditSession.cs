// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The one cell being edited: the value it started with and the working copy. Typed underneath,
	/// so the state stays generic only over the row type and nothing is boxed.
	/// </summary>
	internal abstract class DataTableEditSession(int column, int sourceIndex)
	{
		/// <summary>Gets the index of the column being edited.</summary>
		public int Column { get; } = column;

		/// <summary>Gets the row's index in the list the caller passed, never its position on screen.</summary>
		public int SourceIndex { get; } = sourceIndex;

		/// <summary>Gets how many frames the editor has been drawn, which is how the renderer knows to focus it once.</summary>
		public int FramesDrawn { get; private set; }

		/// <summary>Draws the editor for this session's cell, filling it.</summary>
		/// <param name="id">The ImGui id to give the editor.</param>
		public void Draw(string id)
		{
			DrawEditor(id, FramesDrawn);
			FramesDrawn++;
		}

		/// <summary>Reports the edit to the caller if the value changed.</summary>
		/// <returns>True when an edit was reported.</returns>
		public abstract bool Commit();

		protected abstract void DrawEditor(string id, int framesDrawn);
	}

	/// <summary>Edits a string or a number as text, parsed when it's committed.</summary>
	internal sealed class DataTableTextSession<TRow, TValue>(
		DataTableColumn<TRow, TValue> owner,
		int column,
		int sourceIndex,
		TRow row,
		TValue original,
		string text,
		bool isCaretForcedToEnd) : DataTableEditSession(column, sourceIndex)
	{
		// ImGui selects all of an input's text when it takes the keyboard. A session started by typing
		// holds the caret after that first character for the few frames activation takes, so the next
		// character follows it instead of replacing it.
		private const int CaretFrames = 3;

		/// <summary>Gets or sets the working text, updated as the person types and read back when the session commits.</summary>
		public string Text { get; set; } = text;

		public override bool Commit()
		{
			if (!DataTableText.TryParse(Text, out TValue? parsed) || EqualityComparer<TValue>.Default.Equals(original, parsed))
			{
				return false;
			}

			// A null string opens as empty text, so empty text back is the value unchanged, not an edit.
			if (original is null && parsed is string { Length: 0 })
			{
				return false;
			}

			owner.OnEdit!(new DataTableEdit<TRow, TValue>(SourceIndex, row, original, parsed!));
			return true;
		}

		protected override void DrawEditor(string id, int framesDrawn)
		{
			string current = Text;
			DataTableEditors.Text(id, ref current, isCaretForcedToEnd && framesDrawn < CaretFrames);
			Text = current;
		}
	}

	/// <summary>Edits a value directly, through the column's own editor or the built-in enum combo.</summary>
	internal sealed class DataTableValueSession<TRow, TValue>(
		DataTableColumn<TRow, TValue> owner,
		int column,
		int sourceIndex,
		TRow row,
		TValue original,
		DataTableEditor<TValue> editor) : DataTableEditSession(column, sourceIndex)
	{
		private readonly TValue originalValue = original;
		private TValue current = original;

		/// <summary>Gets or sets the working value, updated as the person edits it and read back when the session commits.</summary>
		public TValue Value
		{
			get => current;
			set => current = value;
		}

		public override bool Commit()
		{
			if (EqualityComparer<TValue>.Default.Equals(originalValue, current))
			{
				return false;
			}

			owner.OnEdit!(new DataTableEdit<TRow, TValue>(SourceIndex, row, originalValue, current));
			return true;
		}

		protected override void DrawEditor(string id, int framesDrawn) => editor(id, ref current);
	}
}

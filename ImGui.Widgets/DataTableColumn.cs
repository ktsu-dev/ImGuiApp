// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>A cell of a data table, addressed by its row's place in the caller's list and its column.</summary>
	/// <param name="SourceIndex">The row's index in the list the caller passed, never its position on screen.</param>
	/// <param name="Column">The column's index in <see cref="DataTableState{TRow}.Columns"/>.</param>
	public readonly record struct DataTableCell(int SourceIndex, int Column);

	/// <summary>
	/// An edit to one cell, reported to the caller to apply. The table never writes to the caller's
	/// data itself.
	/// </summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <typeparam name="TValue">The type of the edited column's values.</typeparam>
	/// <param name="SourceIndex">The row's index in the list the caller passed.</param>
	/// <param name="Row">The row that was edited.</param>
	/// <param name="OldValue">The value when editing began, which is what an undo restores.</param>
	/// <param name="NewValue">The value to apply.</param>
	public readonly record struct DataTableEdit<TRow, TValue>(int SourceIndex, TRow Row, TValue OldValue, TValue NewValue);

	/// <summary>Draws an editor for one cell's value, filling the cell.</summary>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="id">The ImGui id to give the editor. Not drawn.</param>
	/// <param name="value">The working value, updated as the person edits it.</param>
	/// <returns>True when the value changed this frame.</returns>
	public delegate bool DataTableEditor<TValue>(string id, ref TValue value);

	/// <summary>
	/// One column of a data table. Create a <see cref="DataTableColumn{TRow, TValue}"/>, which knows
	/// the column's value type.
	/// </summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	public abstract class DataTableColumn<TRow>
	{
		// Only the typed column below derives from this, which is what lets the internal members be
		// abstract without anyone outside having to implement them.
		private protected DataTableColumn()
		{
		}

		/// <summary>Gets the heading. Also the column's ImGui id and probe name, so it must be unique in its table.</summary>
		public required string Label { get; init; }

		/// <summary>Gets the sizing and behavior flags passed to <c>TableSetupColumn</c>.</summary>
		public ImGuiTableColumnFlags Flags { get; init; }

		/// <summary>Gets the initial width or weight, read according to <see cref="Flags"/>. Zero leaves it to ImGui.</summary>
		public float Width { get; init; }

		/// <summary>Gets a value indicating whether clicking the heading sorts on this column. Defaults to <see langword="true"/>.</summary>
		public bool CanSort { get; init; } = true;

		/// <summary>Gets the text a row shows in this column, which is also what's filtered and copied.</summary>
		/// <param name="row">The row.</param>
		/// <returns>The cell's text.</returns>
		public abstract string GetText(TRow row);

		internal abstract bool IsEditable { get; }

		internal abstract bool IsToggle { get; }

		internal abstract int Compare(TRow left, TRow right);

		internal abstract void Validate();

		internal abstract DataTableEditSession? CreateSession(int column, int sourceIndex, TRow row, char? firstCharacter);

		internal abstract void Toggle(int sourceIndex, TRow row);

		/// <summary>Draws the cell's content when it isn't being edited.</summary>
		/// <returns>True when an inline toggle was clicked.</returns>
		internal abstract bool DrawContent(TRow row);
	}

	/// <summary>A column of a data table whose values are of one type.</summary>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <typeparam name="TValue">The type of this column's values.</typeparam>
	public sealed class DataTableColumn<TRow, TValue> : DataTableColumn<TRow>
	{
		/// <summary>Gets the function that reads this column's value from a row.</summary>
		public required Func<TRow, TValue> Value { get; init; }

		/// <summary>
		/// Gets the function that turns a value into cell text, or <see langword="null"/> to use the
		/// value's own formatting in the invariant culture.
		/// </summary>
		public Func<TValue, string>? Format { get; init; }

		/// <summary>Gets the comparer used to sort on this column, or <see langword="null"/> for the type's default.</summary>
		public IComparer<TValue>? Comparer { get; init; }

		/// <summary>
		/// Gets the callback that receives each committed edit, or <see langword="null"/> for a read-only
		/// column. The callback applies the edit. The table never writes to the row.
		/// </summary>
		public Action<DataTableEdit<TRow, TValue>>? OnEdit { get; init; }

		/// <summary>
		/// Gets the editor to use instead of the built-in one. Required for an editable column whose
		/// value type isn't <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="float"/>,
		/// <see cref="double"/>, <see cref="string"/>, or an enum.
		/// </summary>
		public DataTableEditor<TValue>? Editor { get; init; }

		/// <summary>
		/// Gets a function that draws a cell's content in place of its text, for icons or colored values,
		/// or <see langword="null"/> to draw the text.
		/// </summary>
		public Action<TRow, TValue>? DrawCell { get; init; }

		/// <inheritdoc/>
		public override string GetText(TRow row)
		{
			TValue value = Value(row);
			return Format is null ? DataTableText.FormatInvariant(value) : Format(value);
		}

		internal override bool IsEditable => OnEdit is not null;

		internal override bool IsToggle => OnEdit is not null && Editor is null && typeof(TValue) == typeof(bool);

		internal override int Compare(TRow left, TRow right) =>
			(Comparer ?? Comparer<TValue>.Default).Compare(Value(left), Value(right));

		internal override void Validate()
		{
			if (OnEdit is not null && Editor is null && !DataTableText.HasBuiltInEditor(typeof(TValue)))
			{
				throw new ArgumentException(
					$"Column '{Label}' is editable, but {typeof(TValue).Name} has no built-in editor. Supply an Editor, or remove OnEdit to make the column read-only.");
			}
		}

		internal override DataTableEditSession? CreateSession(int column, int sourceIndex, TRow row, char? firstCharacter)
		{
			if (OnEdit is null || IsToggle)
			{
				return null;
			}

			TValue current = Value(row);

			if (Editor is not null)
			{
				return new DataTableValueSession<TRow, TValue>(this, column, sourceIndex, row, current, Editor);
			}

			if (DataTableText.IsTextEditable(typeof(TValue)))
			{
				string text = firstCharacter is char typed
					? typed.ToString(System.Globalization.CultureInfo.InvariantCulture)
					: DataTableText.FormatInvariant(current);

				return new DataTableTextSession<TRow, TValue>(this, column, sourceIndex, row, current, text, isCaretForcedToEnd: firstCharacter is not null);
			}

			// Validate has already ruled out every other type, so what's left is an enum.
			return new DataTableValueSession<TRow, TValue>(this, column, sourceIndex, row, current, DataTableEditors.EnumValue);
		}

		internal override void Toggle(int sourceIndex, TRow row)
		{
			if (!IsToggle)
			{
				return;
			}

			TValue current = Value(row);
			TValue flipped = (TValue)(object)!(current is bool isOn && isOn);
			OnEdit!(new DataTableEdit<TRow, TValue>(sourceIndex, row, current, flipped));
		}

		internal override bool DrawContent(TRow row)
		{
			TValue value = Value(row);

			if (IsToggle)
			{
				bool isChecked = value is bool isOn && isOn;
				return ImGui.Checkbox("##toggle", ref isChecked);
			}

			if (DrawCell is not null)
			{
				DrawCell(row, value);
				return false;
			}

			ImGui.TextUnformatted(Format is null ? DataTableText.FormatInvariant(value) : Format(value));
			return false;
		}
	}
}

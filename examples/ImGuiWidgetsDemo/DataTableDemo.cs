// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;
using ktsu.UndoRedo;
using ktsu.UndoRedo.Core.Services;
using ktsu.UndoRedo.Models;

/// <summary>
/// Demonstrates <see cref="ImGuiWidgets.DataTable{TRow}"/> over a parts list, with every column editable
/// and every edit recorded on an undo stack.
/// </summary>
/// <remarks>
/// In its own class rather than in <c>ImGuiWidgetsDemo</c>, which is already at the class-coupling
/// limit the analyzers enforce. The undo stack is the point: the table reports edits rather than
/// writing them, so the demo applies each one as a command it can take back.
/// </remarks>
internal static class DataTableDemo
{
	private const int PartCount = 2_000;

	private static readonly List<DemoPart> Parts = [];

	private static readonly ImGuiWidgets.DataTableOptions Options = new()
	{
		RowHeight = 22.0f,
		OuterSize = new Vector2(0.0f, 300.0f),
		FrozenColumns = 1,
		OnContextMenu = ShowContextMenu,
	};

	private static UndoRedoService history = CreateHistory();
	private static ImGuiWidgets.DataTableState<DemoPart> state = CreateState();

	private enum PartCategory
	{
		Fastener,
		Bearing,
		Seal,
		Spring,
	}

	/// <summary>Gets how many edits the table has reported since the demo was reset.</summary>
	internal static int EditCount { get; private set; }

	/// <summary>Puts the demo back as it started, for the demo UI tests.</summary>
	internal static void ResetState()
	{
		Parts.Clear();
		history = CreateHistory();
		state = CreateState();
		EditCount = 0;
	}

	/// <summary>Draws the demo section.</summary>
	public static void Show()
	{
		if (!DemoProbe.Header("Data Table"))
		{
			return;
		}

		if (Parts.Count == 0)
		{
			Parts.AddRange(CreateParts());
		}

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"{Parts.Count:N0} parts, {state.SelectedRows.Count} selected, {EditCount} edits."));
		ImGui.TextDisabled("Double-click, F2 or start typing to edit. Enter commits, Escape cancels, Tab moves on.");

		ImGui.BeginDisabled(!history.CanUndo);
		if (DemoProbe.Button("Undo"))
		{
			history.Undo();
			state.Refresh();
		}

		ImGui.EndDisabled();
		ImGui.SameLine();

		ImGui.BeginDisabled(!history.CanRedo);
		if (DemoProbe.Button("Redo"))
		{
			history.Redo();
			state.Refresh();
		}

		ImGui.EndDisabled();

		ImGuiWidgets.DataTable("parts", Parts, state, Options);
	}

	private static UndoRedoService CreateHistory() =>
		new(new StackManager(), new SaveBoundaryManager(), new CommandMerger(), UndoRedoOptions.Create(maxStackSize: 200));

	private static ImGuiWidgets.DataTableState<DemoPart> CreateState() => new(
	[
		new ImGuiWidgets.DataTableColumn<DemoPart, string>
		{
			Label = "Name",
			Value = part => part.Name,
			Flags = ImGuiTableColumnFlags.WidthFixed,
			Width = 140.0f,
			OnEdit = edit => Apply("Rename part", edit, (part, value) => part.Name = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, int>
		{
			Label = "Quantity",
			Value = part => part.Quantity,
			OnEdit = edit => Apply("Change quantity", edit, (part, value) => part.Quantity = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, double>
		{
			Label = "Mass (kg)",
			Value = part => part.MassKilograms,
			Format = value => value.ToString("0.000", CultureInfo.InvariantCulture),
			OnEdit = edit => Apply("Change mass", edit, (part, value) => part.MassKilograms = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, bool>
		{
			Label = "In stock",
			Value = part => part.IsInStock,
			OnEdit = edit => Apply("Change stock", edit, (part, value) => part.IsInStock = value),
		},
		new ImGuiWidgets.DataTableColumn<DemoPart, PartCategory>
		{
			Label = "Category",
			Value = part => part.Category,
			OnEdit = edit => Apply("Change category", edit, (part, value) => part.Category = value),
		},
	]);

	/// <summary>
	/// Applies an edit as an undoable command. The row is captured rather than its index, so undo still
	/// finds the right part after the table has been sorted or filtered.
	/// </summary>
	private static void Apply<TValue>(string description, ImGuiWidgets.DataTableEdit<DemoPart, TValue> edit, System.Action<DemoPart, TValue> set)
	{
		DemoPart part = edit.Row;
		history.Execute(new DelegateCommand(description, () => set(part, edit.NewValue), () => set(part, edit.OldValue)));
		EditCount++;
	}

	private static void ShowContextMenu(ImGuiWidgets.DataTableContextMenu menu)
	{
		ImGui.TextDisabled(string.Create(CultureInfo.InvariantCulture, $"{menu.SelectedRows.Count} selected"));

		if (ImGui.MenuItem("Mark in stock"))
		{
			foreach (int sourceIndex in menu.SelectedRows)
			{
				DemoPart part = Parts[sourceIndex];
				bool wasInStock = part.IsInStock;
				history.Execute(new DelegateCommand("Mark in stock", () => part.IsInStock = true, () => part.IsInStock = wasInStock));
				EditCount++;
			}

			state.Refresh();
		}
	}

	private static IEnumerable<DemoPart> CreateParts()
	{
		for (int index = 0; index < PartCount; index++)
		{
			yield return new DemoPart
			{
				Name = string.Create(CultureInfo.InvariantCulture, $"Part {index:D4}"),
				Quantity = index * 37 % 500,
				MassKilograms = index * 13 % 1000 / 100.0,
				IsInStock = index % 3 != 0,
				Category = (PartCategory)(index % 4),
			};
		}
	}

	private sealed class DemoPart
	{
		public string Name { get; set; } = string.Empty;

		public int Quantity { get; set; }

		public double MassKilograms { get; set; }

		public bool IsInStock { get; set; }

		public PartCategory Category { get; set; }
	}
}

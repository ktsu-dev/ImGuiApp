// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;

/// <summary>
/// Four people and five columns, with every edit recorded rather than applied, which is the contract
/// the table has with its caller.
/// </summary>
/// <remarks>
/// Ages 30, 25, 35 and 25 give one tie, which is what the sorting tests need to show ties keep their
/// source order. Bob is the one inactive person, and Length is the one read-only column.
/// </remarks>
internal sealed class DataTableFixture
{
	public const int NameColumn = 0;
	public const int AgeColumn = 1;
	public const int ActiveColumn = 2;
	public const int MoodColumn = 3;
	public const int LengthColumn = 4;

	public DataTableFixture()
	{
		Name = new() { Label = "Name", Value = person => person.Name, OnEdit = NameEdits.Add };
		Age = new() { Label = "Age", Value = person => person.Age, OnEdit = AgeEdits.Add };
		Active = new() { Label = "Active", Value = person => person.IsActive, OnEdit = ActiveEdits.Add };
		Mood = new() { Label = "Mood", Value = person => person.Mood, OnEdit = MoodEdits.Add };
		Length = new() { Label = "Length", Value = person => person.Name.Length };
	}

	public List<DataTablePerson> People { get; } =
	[
		new("Alice", 30, true, DataTableMood.Calm),
		new("Bob", 25, false, DataTableMood.Grumpy),
		new("Carol", 35, true, DataTableMood.Cheerful),
		new("Dave", 25, true, DataTableMood.Calm),
	];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, string>> NameEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, int>> AgeEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, bool>> ActiveEdits { get; } = [];

	public List<ImGuiWidgets.DataTableEdit<DataTablePerson, DataTableMood>> MoodEdits { get; } = [];

	public ImGuiWidgets.DataTableColumn<DataTablePerson, string> Name { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, int> Age { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, bool> Active { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, DataTableMood> Mood { get; }

	public ImGuiWidgets.DataTableColumn<DataTablePerson, int> Length { get; }

	public ImGuiWidgets.DataTableState<DataTablePerson> CreateState()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = new([Name, Age, Active, Mood, Length]);
		state.Sync(People);
		return state;
	}
}

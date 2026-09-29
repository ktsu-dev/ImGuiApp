// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

/// <summary>A row for the data table tests. Mutable, so a test can play the caller applying an edit.</summary>
internal sealed class DataTablePerson(string name, int age, bool isActive, DataTableMood mood)
{
	public string Name { get; set; } = name;

	public int Age { get; set; } = age;

	public bool IsActive { get; set; } = isActive;

	public DataTableMood Mood { get; set; } = mood;
}

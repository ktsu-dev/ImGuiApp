// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

/// <summary>A row for the data table UI tests. Immutable, because these tests only watch edits being reported.</summary>
internal sealed record DataTablePerson(string Name, int Age, bool IsActive);

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Demonstrates <see cref="ImGuiWidgets.VirtualTable(string, int, System.ReadOnlySpan{ImGuiWidgets.VirtualTableColumn}, System.Action{int}, ImGuiWidgets.VirtualTableOptions?)"/>
/// against a catalogue large enough that the virtualization is the visible feature.
/// </summary>
/// <remarks>
/// In its own class rather than in <c>ImGuiWidgetsDemo</c>, which is already at the class-coupling
/// limit the analyzers enforce.
/// </remarks>
internal static class VirtualTableDemo
{
	/// <summary>
	/// Rows in the synthetic catalogue. Around the size of the real satellite catalogue that
	/// prompted the widget.
	/// </summary>
	private const int RowCount = 30_000;

	private static readonly ImGuiWidgets.VirtualTableColumn[] Columns =
	[
		new("Designator", ImGuiTableColumnFlags.WidthFixed, 130.0f),
		new("Inclination", ImGuiTableColumnFlags.WidthFixed, 110.0f),
		new("Apogee (km)", ImGuiTableColumnFlags.WidthStretch),
	];

	private static readonly ImGuiWidgets.VirtualTableOptions Options = new()
	{
		RowHeight = 22.0f,
		OuterSize = new Vector2(0.0f, 260.0f),
		OnSortChanged = OnSortChanged,
	};

	private static int goToRow = 15_000;
	private static string sortSummary = "unsorted";
	private static int rowsDrawnLastFrame;
	private static int rowsDrawnThisFrame;

	private static void OnSortChanged(int column, bool ascending) =>
		sortSummary = string.Create(
			CultureInfo.InvariantCulture,
			$"{Columns[column].Label}, {(ascending ? "ascending" : "descending")}");

	/// <summary>
	/// Draws one row's cells from its index alone.
	/// </summary>
	/// <remarks>
	/// There is no list of thirty thousand anything behind this. Synthesizing a row on demand is
	/// only affordable because the widget asks for a couple of dozen of them per frame, which is
	/// the property the row counter below is there to show.
	/// </remarks>
	/// <param name="row">The absolute row index.</param>
	private static void DrawRow(int row)
	{
		rowsDrawnThisFrame++;

		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"{1957 + (row % 69)}-{row % 1000:D3}A"));
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"{row % 180}.{row % 10}"));
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"{400 + (row % 35_000):N0}"));
	}

	/// <summary>Draws the demo section.</summary>
	public static void Show()
	{
		if (!DemoProbe.Header("Virtual Table"))
		{
			return;
		}

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"{RowCount:N0} rows, of which {rowsDrawnLastFrame} were drawn last frame."));

		ImGui.TextUnformatted(string.Create(
			CultureInfo.InvariantCulture,
			$"Selected row: {Options.SelectedRow}    Sort: {sortSummary}"));

		// The widget reports a sort change and leaves the reordering to whoever owns the data. This
		// demo owns none of it — the rows are computed from the index — so it shows the request and
		// does nothing else with it. That is the contract rather than an omission.
		ImGui.TextDisabled("Sorting is reported, not applied: the widget never reorders the caller's data.");

		ImGui.SetNextItemWidth(160.0f);
		DemoProbe.SliderInt("Go to row", ref goToRow, 0, RowCount - 1);
		ImGui.SameLine();

		if (DemoProbe.Button("Scroll there"))
		{
			Options.ScrollToRow = goToRow;
		}

		ImGui.Separator();

		rowsDrawnThisFrame = 0;

		ImGuiWidgets.VirtualTable("catalogue", RowCount, Columns, DrawRow, Options);

		rowsDrawnLastFrame = rowsDrawnThisFrame;
	}
}

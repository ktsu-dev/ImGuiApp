// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for layout systems and tables
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Used for dummy data purposes")]
internal sealed class LayoutDemo : IDemoTab
{
	private readonly List<DemoItem> tableData = [];
	private bool showTableHeaders = true;
	private bool showTableBorders = true;
	private readonly Random random = new();

	private sealed record DemoItem(int Id, string Name, string Category, float Value, bool Active);

	public string TabName => "Layout & Tables";

	public LayoutDemo()
	{
		// Initialize table data
		for (int i = 0; i < 20; i++)
		{
			string[] categories = ["Category A", "Category B", "Category C"];
			tableData.Add(new DemoItem(
				i,
				$"Item {i + 1}",
				categories[i % 3],
				(float)(random.NextDouble() * 100),
				random.NextDouble() > 0.5
			));
		}
	}

	public void Update(float deltaTime)
	{
		// No updates needed for layout demo
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			// Columns
			ImGui.Text("Columns Layout:");
			ImGui.Columns(3, "DemoColumns");
			ImGui.Separator();

			ImGui.Text("Column 1");
			ImGui.NextColumn();
			ImGui.Text("Column 2");
			ImGui.NextColumn();
			ImGui.Text("Column 3");
			ImGui.NextColumn();

			for (int i = 0; i < 9; i++)
			{
				ImGui.Text($"Item {i + 1}");
				ImGui.NextColumn();
			}

			ImGui.Columns(1);
			ImGui.Separator();

			// Tables
			ImGui.Text("Advanced Tables:");
			ImGui.Checkbox("Show Headers", ref showTableHeaders);
			ImGui.SameLine();
			ImGui.Checkbox("Show Borders", ref showTableBorders);

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Sortable | ImGuiTableFlags.Resizable;
			if (showTableHeaders)
			{
				tableFlags |= ImGuiTableFlags.RowBg;
			}
			if (showTableBorders)
			{
				tableFlags |= ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV;
			}

			if (ImGui.BeginTable("DemoTable", 5, tableFlags))
			{
				if (showTableHeaders)
				{
					// Test flags without width parameters
					ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.DefaultSort);
					ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None);
					ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.None);
					ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.None);
					ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.None);
					ImGui.TableHeadersRow();
				}

				for (int row = 0; row < Math.Min(tableData.Count, 10); row++)
				{
					DemoItem item = tableData[row];
					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					ImGui.Text(item.Id.ToString());

					ImGui.TableSetColumnIndex(1);
					ImGui.Text(item.Name);

					ImGui.TableSetColumnIndex(2);
					ImGui.Text(item.Category);

					ImGui.TableSetColumnIndex(3);
					ImGui.Text($"{item.Value:F2}");

					ImGui.TableSetColumnIndex(4);
					ImGui.Text(item.Active ? "✓" : "✗");
				}

				ImGui.EndTable();
			}

			ImGui.Separator();

			// Child windows
			ImGui.Text("Child Windows:");
			if (ImGui.BeginChild("ScrollableChild", new Vector2(0, 150)))
			{
				for (int i = 0; i < 50; i++)
				{
					ImGui.Text($"Scrollable line {i + 1}");
				}
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}
}

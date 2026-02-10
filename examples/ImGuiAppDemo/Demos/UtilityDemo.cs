// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Text;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for utility tools and debugging features
/// </summary>
internal sealed class UtilityDemo : IDemoTab
{
	private string filePath = "";
	private string fileContent = "";
	private bool showImGuiDemo;
	private bool showStyleEditor;
	private bool showMetrics;

	public string TabName => "Utilities & Tools";

	public void Update(float deltaTime)
	{
		if (showImGuiDemo)
		{
			ImGui.ShowDemoWindow(ref showImGuiDemo);
		}

		if (showStyleEditor)
		{
			ImGui.Begin("Style Editor", ref showStyleEditor);
			ImGui.ShowStyleEditor();
			ImGui.End();
		}

		if (showMetrics)
		{
			ImGui.ShowMetricsWindow(ref showMetrics);
		}
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			// File operations
			ImGui.SeparatorText("File Operations:");
			ImGui.InputText("File Path", ref filePath, 256);
			ImGui.SameLine();
			if (ImGui.Button("Load") && !string.IsNullOrEmpty(filePath))
			{
				try
				{
					fileContent = File.ReadAllText(filePath);
				}
				catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException)
				{
					// Handle file read errors gracefully
					fileContent = $"Error loading file: {ex.Message}";
				}
			}

			if (!string.IsNullOrEmpty(fileContent))
			{
				ImGui.SeparatorText("File Content Preview:");
				ImGui.TextWrapped(fileContent.Length > 500 ? fileContent[..500] + "..." : fileContent);
			}

			// System information
			ImGui.SeparatorText("System Information:");
			unsafe
			{
				byte* ptr = ImGui.GetVersion();
				int length = 0;
				while (ptr[length] != 0)
				{
					length++;
				}
				ImGui.Text($"ImGui Version: {Encoding.UTF8.GetString(ptr, length)}");
			}
			ImGui.Text($"Display Size: {ImGui.GetIO().DisplaySize}");

			// Debugging tools
			ImGui.SeparatorText("Debug Tools:");
			if (ImGui.Button("Show ImGui Demo"))
			{
				showImGuiDemo = !showImGuiDemo;
			}
			ImGui.SameLine();
			if (ImGui.Button("Show Style Editor"))
			{
				showStyleEditor = !showStyleEditor;
			}
			ImGui.SameLine();
			if (ImGui.Button("Show Metrics"))
			{
				showMetrics = !showMetrics;
			}

			ImGui.EndTabItem();
		}
	}
}

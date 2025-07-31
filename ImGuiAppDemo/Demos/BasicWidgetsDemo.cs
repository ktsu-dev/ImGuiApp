// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for basic ImGui widgets
/// </summary>
internal sealed class BasicWidgetsDemo : IDemoTab
{
	private float sliderValue = 0.5f;
	private int counter;
	private bool checkboxState;
	private string inputText = "Type here...";
	private int comboSelection;
	private readonly string[] comboItems = ["Item 1", "Item 2", "Item 3", "Item 4"];
	private int listboxSelection;
	private readonly string[] listboxItems = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];
	private float dragFloat = 1.0f;
	private int dragInt = 50;
	private Vector3 dragVector = new(1.0f, 2.0f, 3.0f);
	private float angle;
	private int radioSelection;

	public string TabName => "Basic Widgets";

	public void Update(float deltaTime)
	{
		// No updates needed for basic widgets
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.TextWrapped("This tab demonstrates basic ImGui widgets and controls.");
			ImGui.Separator();

			// Buttons
			ImGui.Text("Buttons:");
			if (ImGui.Button("Regular Button"))
			{
				counter++;
			}

			ImGui.SameLine();
			if (ImGui.SmallButton("Small"))
			{
				counter++;
			}

			ImGui.SameLine();
			if (ImGui.ArrowButton("##left", ImGuiDir.Left))
			{
				counter--;
			}

			ImGui.SameLine();
			if (ImGui.ArrowButton("##right", ImGuiDir.Right))
			{
				counter++;
			}

			ImGui.SameLine();
			ImGui.Text($"Counter: {counter}");

			// Checkboxes and Radio buttons
			ImGui.Separator();
			ImGui.Text("Selection Controls:");
			ImGui.Checkbox("Checkbox", ref checkboxState);

			ImGui.RadioButton("Option 1", ref radioSelection, 0);
			ImGui.SameLine();
			ImGui.RadioButton("Option 2", ref radioSelection, 1);
			ImGui.SameLine();
			ImGui.RadioButton("Option 3", ref radioSelection, 2);

			// Sliders
			ImGui.Separator();
			ImGui.Text("Sliders:");
			ImGui.SliderFloat("Float Slider", ref sliderValue, 0.0f, 1.0f);
			ImGui.SliderFloat("Angle", ref angle, 0.0f, 360.0f, "%.1f deg");
			ImGui.SliderInt("Int Slider", ref dragInt, 0, 100);

			// Input fields
			ImGui.Separator();
			ImGui.Text("Input Fields:");
			ImGui.InputText("Text Input", ref inputText, 100);
			ImGui.InputFloat("Float Input", ref dragFloat);
			ImGui.InputFloat3("Vector3 Input", ref dragVector);

			// Combo boxes
			ImGui.Separator();
			ImGui.Text("Dropdowns:");
			ImGui.Combo("Combo Box", ref comboSelection, comboItems, comboItems.Length);
			ImGui.ListBox("List Box", ref listboxSelection, listboxItems, listboxItems.Length, 4);

			ImGui.EndTabItem();
		}
	}
}

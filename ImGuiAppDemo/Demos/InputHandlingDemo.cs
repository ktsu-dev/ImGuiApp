// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;

/// <summary>
/// Demo for input handling and interaction
/// </summary>
internal sealed class InputHandlingDemo : IDemoTab
{
	private readonly StringBuilder textBuffer = new(1024);
	private bool wrapText = true;
	private float textSpeed = 50.0f;
	private bool showModal;
	private bool showPopup;
	private string modalResult = "";
	private string modalInputBuffer = "";

	public string TabName => "Input & Interaction";

	public InputHandlingDemo()
	{
		textBuffer.Append("This is a demonstration of ImGui text editing capabilities.\n");
		textBuffer.Append("You can edit this text, and it will update in real-time.\n");
		textBuffer.Append("ImGui supports multi-line text editing with syntax highlighting possibilities.");
	}

	public void Update(float deltaTime)
	{
		// Handle modals and popups
		HandleModalAndPopups();
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.Text("Mouse Information:");
			Vector2 mousePos = ImGui.GetMousePos();
			Vector2 mouseDelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left);
			ImGui.Text($"Mouse Position: ({mousePos.X:F1}, {mousePos.Y:F1})");
			ImGui.Text($"Mouse Delta: ({mouseDelta.X:F1}, {mouseDelta.Y:F1})");
			ImGui.Text($"Left Button: {(ImGui.IsMouseDown(ImGuiMouseButton.Left) ? "DOWN" : "UP")}");
			ImGui.Text($"Right Button: {(ImGui.IsMouseDown(ImGuiMouseButton.Right) ? "DOWN" : "UP")}");

			ImGui.Separator();

			// Simple drag demonstration
			ImGui.Text("Drag & Drop:");
			ImGui.Button("Drag Source", new Vector2(100, 50));
			ImGui.SameLine();
			ImGui.Button("Drop Target", new Vector2(100, 50));
			ImGui.Text("(Drag and drop functionality would require more complex implementation)");

			ImGui.Separator();

			// Text editing
			ImGui.Text("Multi-line Text Editor:");
			ImGui.Checkbox("Word Wrap", ref wrapText);
			ImGuiInputTextFlags textFlags = ImGuiInputTextFlags.AllowTabInput;
			if (!wrapText)
			{
				textFlags |= ImGuiInputTextFlags.NoHorizontalScroll;
			}

			string textContent = textBuffer.ToString();
			if (ImGui.InputTextMultiline("##TextEditor", ref textContent, 1024, new Vector2(-1, 150), textFlags))
			{
				textBuffer.Clear();
				textBuffer.Append(textContent);
			}

			ImGui.Separator();

			// Popup and modal buttons
			ImGui.Text("Popups and Modals:");
			if (ImGui.Button("Show Modal"))
			{
				showModal = true;
				modalResult = "";
			}

			ImGui.SameLine();
			if (ImGui.Button("Show Popup"))
			{
				showPopup = true;
			}

			if (!string.IsNullOrEmpty(modalResult))
			{
				ImGui.Text($"Modal Result: {modalResult}");
			}

			ImGui.EndTabItem();
		}
	}

	private void HandleModalAndPopups()
	{
		// Modal dialog
		if (showModal)
		{
			ImGui.OpenPopup("Demo Modal");
			showModal = false;
		}

		if (ImGui.BeginPopupModal("Demo Modal", ref showModal))
		{
			ImGui.Text("This is a modal dialog.");
			ImGui.Text("It blocks interaction with the main window.");
			ImGui.Separator();

			ImGui.InputText("Input", ref modalInputBuffer, 100);

			if (ImGui.Button("OK"))
			{
				modalResult = $"You entered: {modalInputBuffer}";
				ImGui.CloseCurrentPopup();
			}
			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				modalResult = "Cancelled";
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
		}

		// Context popup
		if (showPopup)
		{
			ImGui.OpenPopup("Demo Popup");
			showPopup = false;
		}

		if (ImGui.BeginPopup("Demo Popup"))
		{
			ImGui.Text("This is a popup menu");
			if (ImGui.MenuItem("Option 1"))
			{
				// Handle option 1
			}
			if (ImGui.MenuItem("Option 2"))
			{
				// Handle option 2
			}
			ImGui.Separator();
			if (ImGui.MenuItem("Close"))
			{
				// Handle close
			}
			ImGui.EndPopup();
		}
	}
}

// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.Popups;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Popups;

internal static class ImGuiPopupsDemo
{
	private static void Main()
	{
		ImGuiApp.Start(new()
		{
			Title = "ImGui Popups Demo",
			OnAppMenu = OnAppMenu,
			OnMoveOrResize = OnMoveOrResize,
			OnStart = OnStart,
			OnRender = OnRender,
			SaveIniSettings = false,
		});
	}

	// Demo state variables
	private static string stringInputValue = "Hello World";
	private static int intInputValue = 42;
	private static float floatInputValue = 3.14159f;
	private static string selectedFriend = "None";
	private static string selectedColor = "None";
	private static string lastFileOpened = "None";
	private static string lastFileSaved = "None";
	private static string lastDirectoryChosen = "None";
	private static string lastPromptResult = "None";
	private static string lastCustomModalResult = "None";

	// Sample data
	private static readonly string[] Friends = ["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack"];
	private static readonly string[] Colors = ["Red", "Green", "Blue", "Yellow", "Purple", "Orange", "Pink", "Cyan", "Magenta", "Brown"];

	// Custom modal state variables
	private static bool customCheckbox;
	private static float customSlider = 0.5f;
	private static readonly string[] advancedModalItems = ["Option 1", "Option 2", "Option 3", "Option 4"];
	private static int advancedModalSelectedItem;
	private static Vector3 advancedModalColorValue = new(1.0f, 0.5f, 0.0f);
	private static readonly bool[] advancedModalFlags = [true, false, true, false];

	// Popup instances
	private static readonly ImGuiPopups.InputString popupInputString = new();
	private static readonly ImGuiPopups.InputInt popupInputInt = new();
	private static readonly ImGuiPopups.InputFloat popupInputFloat = new();
	private static readonly ImGuiPopups.FilesystemBrowser popupFilesystemBrowser = new();
	private static readonly ImGuiPopups.MessageOK popupMessageOK = new();
	private static readonly ImGuiPopups.SearchableList<string> popupSearchableListFriends = new();
	private static readonly ImGuiPopups.SearchableList<string> popupSearchableListColors = new();
	private static readonly ImGuiPopups.Prompt popupPrompt = new();
	private static readonly ImGuiPopups.Modal popupCustomModal = new();

	private static void OnStart()
	{
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	private static void OnRender(float dt)
	{
		ImGui.Text("ImGui Popups Library - Demo");
		ImGui.Text("This demo showcases all popup types and configurations available in the library.");
		ImGui.Separator();

		RenderInputPopupsSection();
		RenderMessageAndPromptSection();
		RenderSearchableListsSection();
		RenderFileSystemBrowserSection();
		RenderCustomModalSection();
		RenderAdvancedExamplesSection();
		RenderTipsSection();

		// Show all open popups
		ShowAllPopups();
	}

	private static void RenderInputPopupsSection()
	{
		if (ImGui.CollapsingHeader("Input Popups", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Input popups allow users to enter different types of values.");
			ImGui.Spacing();

			// String Input
			ImGui.Text($"Current String Value: {stringInputValue}");
			if (ImGui.Button("Edit String"))
			{
				popupInputString.Open("Edit String Value", "Enter a new string:", stringInputValue, result => stringInputValue = result);
			}

			ImGui.SameLine();
			if (ImGui.Button("Edit String (Custom Size)"))
			{
				popupInputString.Open("Edit String Value", "Enter a new string:", stringInputValue, result => stringInputValue = result, new Vector2(400, 150));
			}

			// Integer Input
			ImGui.Text($"Current Integer Value: {intInputValue}");
			if (ImGui.Button("Edit Integer"))
			{
				popupInputInt.Open("Edit Integer Value", "Enter a new integer:", intInputValue, result => intInputValue = result);
			}

			// Float Input
			ImGui.Text($"Current Float Value: {floatInputValue:F5}");
			if (ImGui.Button("Edit Float"))
			{
				popupInputFloat.Open("Edit Float Value", "Enter a new float:", floatInputValue, result => floatInputValue = result);
			}

			ImGui.Spacing();
		}
	}

	private static void RenderMessageAndPromptSection()
	{
		if (ImGui.CollapsingHeader("Message & Prompt Popups", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Display messages and custom prompts with various configurations.");
			ImGui.Spacing();

			// Simple Message
			if (ImGui.Button("Show Simple Message"))
			{
				popupMessageOK.Open("Information", "This is a simple informational message popup.");
			}

			ImGui.SameLine();
			if (ImGui.Button("Show Long Message"))
			{
				string longMessage = @"This is a very long message that demonstrates text wrapping capabilities. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

				popupMessageOK.Open("Long Message", longMessage, ImGuiPopups.PromptTextLayoutType.Wrapped, new Vector2(500, 300));
			}

			// Custom Prompt with Multiple Buttons
			ImGui.Text($"Last Prompt Result: {lastPromptResult}");
			if (ImGui.Button("Show Custom Prompt"))
			{
				Dictionary<string, Action?> buttons = new()
				{
					{ "Yes", () => lastPromptResult = "User clicked Yes" },
					{ "No", () => lastPromptResult = "User clicked No" },
					{ "Maybe", () => lastPromptResult = "User clicked Maybe" },
					{ "Cancel", () => lastPromptResult = "User clicked Cancel" }
				};
				popupPrompt.Open("Confirmation", "Do you want to proceed with this action?", buttons);
			}

			ImGui.SameLine();
			if (ImGui.Button("Show Warning Prompt"))
			{
				Dictionary<string, Action?> buttons = new()
				{
					{ "Delete", () => lastPromptResult = "User confirmed deletion" },
					{ "Cancel", () => lastPromptResult = "User cancelled deletion" }
				};
				string warning = "⚠️ WARNING: This action cannot be undone!\n\nAre you sure you want to delete all selected files?";
				popupPrompt.Open("Confirm Deletion", warning, buttons, ImGuiPopups.PromptTextLayoutType.Wrapped, new Vector2(400, 200));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderSearchableListsSection()
	{
		if (ImGui.CollapsingHeader("Searchable Lists", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Select items from searchable and filterable lists.");
			ImGui.Spacing();

			ImGui.Text($"Selected Friend: {selectedFriend}");
			if (ImGui.Button("Choose Friend"))
			{
				popupSearchableListFriends.Open("Choose Friend", "Select your best friend:", Friends, result => selectedFriend = result);
			}

			ImGui.SameLine();
			if (ImGui.Button("Choose Friend (Custom Size)"))
			{
				popupSearchableListFriends.Open("Choose Friend", "Select your best friend:", Friends, null, null, result => selectedFriend = result, new Vector2(350, 400));
			}

			ImGui.Text($"Selected Color: {selectedColor}");
			if (ImGui.Button("Choose Color"))
			{
				popupSearchableListColors.Open("Choose Color", "Select your favorite color:", Colors,
					item => $"🎨 {item}", result => selectedColor = result);
			}

			ImGui.Spacing();
		}
	}

	private static void RenderFileSystemBrowserSection()
	{
		if (ImGui.CollapsingHeader("File System Browser", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Browse and select files or directories with filtering support.");
			ImGui.Spacing();

			// File Operations
			ImGui.Text($"Last File Opened: {lastFileOpened}");
			if (ImGui.Button("Open Any File"))
			{
				popupFilesystemBrowser.FileOpen("Open File", file => lastFileOpened = file.ToString());
			}

			ImGui.SameLine();
			if (ImGui.Button("Open C# File"))
			{
				popupFilesystemBrowser.FileOpen("Open C# File", file => lastFileOpened = file.ToString(), "*.cs");
			}

			ImGui.SameLine();
			if (ImGui.Button("Open Image File"))
			{
				popupFilesystemBrowser.FileOpen("Open Image File", file => lastFileOpened = file.ToString(), new Vector2(600, 500), "*.{png,jpg,jpeg,gif,bmp}");
			}

			ImGui.Text($"Last File Saved: {lastFileSaved}");
			if (ImGui.Button("Save Text File"))
			{
				popupFilesystemBrowser.FileSave("Save Text File", file => lastFileSaved = file.ToString(), "*.txt");
			}

			ImGui.SameLine();
			if (ImGui.Button("Save Any File"))
			{
				popupFilesystemBrowser.FileSave("Save File", file => lastFileSaved = file.ToString());
			}

			// Directory Operations
			ImGui.Text($"Last Directory Chosen: {lastDirectoryChosen}");
			if (ImGui.Button("Choose Directory"))
			{
				popupFilesystemBrowser.ChooseDirectory("Choose Directory", directory => lastDirectoryChosen = directory.ToString());
			}

			ImGui.SameLine();
			if (ImGui.Button("Choose Directory (Large)"))
			{
				popupFilesystemBrowser.ChooseDirectory("Choose Directory", directory => lastDirectoryChosen = directory.ToString(), new Vector2(700, 600));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderCustomModalSection()
	{
		if (ImGui.CollapsingHeader("Custom Modal", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Create completely custom modal content with full control.");
			ImGui.Spacing();

			ImGui.Text($"Last Custom Modal Result: {lastCustomModalResult}");
			if (ImGui.Button("Show Custom Modal"))
			{
				popupCustomModal.Open("Custom Modal Example", ShowCustomModalContent);
			}

			ImGui.SameLine();
			if (ImGui.Button("Show Custom Modal (Large)"))
			{
				popupCustomModal.Open("Advanced Custom Modal", ShowAdvancedCustomModalContent, new Vector2(600, 400));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderAdvancedExamplesSection()
	{
		if (ImGui.CollapsingHeader("Advanced Examples"))
		{
			ImGui.Text("Complex usage patterns and edge cases.");
			ImGui.Spacing();

			if (ImGui.Button("Nested Popup Example"))
			{
				popupMessageOK.Open("First Popup", "This popup will open another popup when you click OK.");
			}

			ImGui.SameLine();
			if (ImGui.Button("Validation Example"))
			{
				popupInputString.Open("Enter Email", "Please enter a valid email address:", "", result =>
				{
					if (result.Contains('@') && result.Contains('.'))
					{
						stringInputValue = result;
						popupMessageOK.Open("Success", $"Email '{result}' is valid!");
					}
					else
					{
						popupMessageOK.Open("Error", "Invalid email format! Please try again.");
					}
				});
			}

			if (ImGui.Button("Multi-Step Workflow"))
			{
				StartMultiStepWorkflow();
			}

			ImGui.Spacing();
		}
	}

	private static void RenderTipsSection()
	{
		if (ImGui.CollapsingHeader("Tips & Features"))
		{
			ImGui.TextWrapped("• Press ESC to close any popup");
			ImGui.TextWrapped("• Use TAB to navigate between input fields");
			ImGui.TextWrapped("• Enter key confirms string inputs");
			ImGui.TextWrapped("• Double-click items in file browser to open/navigate");
			ImGui.TextWrapped("• Type to search in searchable lists");
			ImGui.TextWrapped("• All popups support custom sizing");
			ImGui.TextWrapped("• Text can be wrapped or unformatted");
			ImGui.TextWrapped("• File browser supports glob patterns for filtering");
		}
	}

	private static void ShowCustomModalContent()
	{
		ImGui.Text("This is a custom modal with your own content!");
		ImGui.Separator();

		ImGui.Checkbox("Custom Checkbox", ref customCheckbox);
		ImGui.SliderFloat("Custom Slider", ref customSlider, 0.0f, 1.0f);

		ImGui.NewLine();

		if (ImGui.Button("Set Result"))
		{
			lastCustomModalResult = $"Checkbox: {customCheckbox}, Slider: {customSlider:F2}";
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine();
		if (ImGui.Button("Close"))
		{
			lastCustomModalResult = "User closed without setting result";
			ImGui.CloseCurrentPopup();
		}
	}

	private static void ShowAdvancedCustomModalContent()
	{
		ImGui.Text("Advanced Custom Modal with Multiple Controls");
		ImGui.Separator();

		ImGui.Combo("Select Option", ref advancedModalSelectedItem, advancedModalItems, advancedModalItems.Length);
		ImGui.ColorEdit3("Color Picker", ref advancedModalColorValue);

		ImGui.Text("Flags:");
		for (int i = 0; i < advancedModalFlags.Length; i++)
		{
			ImGui.Checkbox($"Flag {i + 1}", ref advancedModalFlags[i]);
			if (i < advancedModalFlags.Length - 1)
			{
				ImGui.SameLine();
			}
		}

		ImGui.Separator();

		if (ImGui.Button("Apply Settings"))
		{
			lastCustomModalResult = $"Selected: {advancedModalItems[advancedModalSelectedItem]}, Color: RGB({advancedModalColorValue.X:F2}, {advancedModalColorValue.Y:F2}, {advancedModalColorValue.Z:F2})";
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine();
		if (ImGui.Button("Reset"))
		{
			advancedModalSelectedItem = 0;
			advancedModalColorValue = new Vector3(1.0f, 0.5f, 0.0f);
			for (int i = 0; i < advancedModalFlags.Length; i++)
			{
				advancedModalFlags[i] = i % 2 == 0;
			}
		}

		ImGui.SameLine();
		if (ImGui.Button("Cancel"))
		{
			lastCustomModalResult = "User cancelled";
			ImGui.CloseCurrentPopup();
		}
	}

	private static void StartMultiStepWorkflow()
	{
		popupInputString.Open("Step 1: Enter Name", "What's your name?", "", name =>
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				popupMessageOK.Open("Error", "Name cannot be empty!");
				return;
			}

			popupSearchableListFriends.Open("Step 2: Choose Friend", $"Hi {name}! Who would you like to invite?", Friends, friend =>
			{
				Dictionary<string, Action?> buttons = new()
				{
					{ "Send Invitation", () =>
						{
							lastPromptResult = $"Invitation sent to {friend} from {name}";
							popupMessageOK.Open("Success", $"Invitation sent to {friend}!");
						}
					},
					{ "Cancel", () => lastPromptResult = "Workflow cancelled" }
				};

				popupPrompt.Open("Step 3: Confirm", $"{name}, send invitation to {friend}?", buttons);
			});
		});
	}

	private static void ShowAllPopups()
	{
		// Show all popup instances
		popupInputString.ShowIfOpen();
		popupInputInt.ShowIfOpen();
		popupInputFloat.ShowIfOpen();
		popupMessageOK.ShowIfOpen();
		popupSearchableListFriends.ShowIfOpen();
		popupSearchableListColors.ShowIfOpen();
		popupPrompt.ShowIfOpen();
		popupFilesystemBrowser.ShowIfOpen();
		popupCustomModal.ShowIfOpen();
	}

	private static void OnAppMenu()
	{
		if (ImGui.BeginMenu("Demo"))
		{
			if (ImGui.MenuItem("Reset All Values"))
			{
				stringInputValue = "Hello World";
				intInputValue = 42;
				floatInputValue = 3.14159f;
				selectedFriend = "None";
				selectedColor = "None";
				lastFileOpened = "None";
				lastFileSaved = "None";
				lastDirectoryChosen = "None";
				lastPromptResult = "None";
				lastCustomModalResult = "None";
			}

			if (ImGui.MenuItem("About"))
			{
				popupMessageOK.Open("About ImGui Popups Demo",
					"ImGui Popups Library Demo\n\nThis comprehensive demo showcases all features of the ktsu.ImGuiPopups library, including:\n\n" +
					"• Input popups for strings, integers, and floats\n" +
					"• Message and confirmation prompts\n" +
					"• Searchable list selection\n" +
					"• File and directory browsers\n" +
					"• Custom modal content\n" +
					"• Advanced usage patterns\n\n" +
					"Press ESC to close any popup, or use the provided buttons.",
					ImGuiPopups.PromptTextLayoutType.Wrapped,
					new Vector2(450, 350));
			}

			ImGui.EndMenu();
		}
	}

	private static void OnMoveOrResize()
	{
		// Method intentionally left empty.
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Popups;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Popups;
using ktsu.ImGui.Probes;

internal static class ImGuiPopupsDemo
{
	/// <summary>
	/// Builds the configuration the demo runs on. Extracted from <c>Main</c> so a UI test drives the
	/// real configuration rather than a parallel one written for testing.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui Popups Demo",
		OnAppMenu = OnAppMenu,
		OnMoveOrResize = OnMoveOrResize,
		OnStart = OnStart,
		OnRender = OnRender,
		SaveIniSettings = false,
	};

	private static void Main() => ImGuiApp.Start(BuildConfig());

	/// <summary>
	/// Returns every piece of demo state to its starting value. The demo keeps its state in statics,
	/// which outlive a harness, so a test that ran before this one would otherwise decide what this
	/// one starts from.
	/// </summary>
	internal static void ResetState()
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
		customCheckbox = false;
		customSlider = 0.5f;
		advancedModalSelectedItem = 0;
		advancedModalColorValue = new Vector3(1.0f, 0.5f, 0.0f);
		advancedModalFlags[0] = true;
		advancedModalFlags[1] = false;
		advancedModalFlags[2] = true;
		advancedModalFlags[3] = false;
	}

	/// <summary>Submits a button and records it for probing, so a test can click it by label.</summary>
	private static bool DemoButton(string label)
	{
		bool clicked = ImGui.Button(label);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Submits a collapsing header and records it, so a test can expand or collapse it.</summary>
	private static bool DemoHeader(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
	{
		bool open = ImGui.CollapsingHeader(label, flags);
		ImGuiProbes.MarkItem(label);
		return open;
	}

	/// <summary>Submits a menu item and records it.</summary>
	private static bool DemoMenuItem(string label)
	{
		bool clicked = ImGui.MenuItem(label);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	// Demo state variables
	internal static string stringInputValue = "Hello World";
	internal static int intInputValue = 42;
	internal static float floatInputValue = 3.14159f;
	internal static string selectedFriend = "None";
	internal static string selectedColor = "None";
	internal static string lastFileOpened = "None";
	internal static string lastFileSaved = "None";
	internal static string lastDirectoryChosen = "None";
	internal static string lastPromptResult = "None";
	internal static string lastCustomModalResult = "None";

	private const string CancelLabel = "Cancel";

	// Sample data
	internal static readonly string[] Friends = ["Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack"];
	internal static readonly string[] Colors = ["Red", "Green", "Blue", "Yellow", "Purple", "Orange", "Pink", "Cyan", "Magenta", "Brown"];

	// Custom modal state variables
	internal static bool customCheckbox;
	internal static float customSlider = 0.5f;
	internal static readonly string[] advancedModalItems = ["Option 1", "Option 2", "Option 3", "Option 4"];
	internal static int advancedModalSelectedItem;
	internal static Vector3 advancedModalColorValue = new(1.0f, 0.5f, 0.0f);
	internal static readonly bool[] advancedModalFlags = [true, false, true, false];

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
		// Method intentionally left empty; no startup initialization is required for this demo.
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
		if (DemoHeader("Input Popups", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Input popups allow users to enter different types of values.");
			ImGui.Spacing();

			// String Input
			ImGui.Text($"Current String Value: {stringInputValue}");
			if (DemoButton("Edit String"))
			{
				popupInputString.Open("Edit String Value", "Enter a new string:", stringInputValue, result => stringInputValue = result);
			}

			ImGui.SameLine();
			if (DemoButton("Edit String (Custom Size)"))
			{
				popupInputString.Open("Edit String Value", "Enter a new string:", stringInputValue, result => stringInputValue = result, new Vector2(400, 150));
			}

			// Integer Input
			ImGui.Text($"Current Integer Value: {intInputValue}");
			if (DemoButton("Edit Integer"))
			{
				popupInputInt.Open("Edit Integer Value", "Enter a new integer:", intInputValue, result => intInputValue = result);
			}

			// Float Input
			ImGui.Text($"Current Float Value: {floatInputValue:F5}");
			if (DemoButton("Edit Float"))
			{
				popupInputFloat.Open("Edit Float Value", "Enter a new float:", floatInputValue, result => floatInputValue = result);
			}

			ImGui.Spacing();
		}
	}

	private static void RenderMessageAndPromptSection()
	{
		if (DemoHeader("Message & Prompt Popups", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Display messages and custom prompts with various configurations.");
			ImGui.Spacing();

			// Simple Message
			if (DemoButton("Show Simple Message"))
			{
				popupMessageOK.Open("Information", "This is a simple informational message popup.");
			}

			ImGui.SameLine();
			if (DemoButton("Show Long Message"))
			{
				string longMessage = @"This is a very long message that demonstrates text wrapping capabilities. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.";

				popupMessageOK.Open("Long Message", longMessage, ImGuiPopups.PromptTextLayoutType.Wrapped, new Vector2(500, 300));
			}

			// Custom Prompt with Multiple Buttons
			ImGui.Text($"Last Prompt Result: {lastPromptResult}");
			if (DemoButton("Show Custom Prompt"))
			{
				Dictionary<string, Action?> buttons = new()
				{
					{ "Yes", () => lastPromptResult = "User clicked Yes" },
					{ "No", () => lastPromptResult = "User clicked No" },
					{ "Maybe", () => lastPromptResult = "User clicked Maybe" },
					{ CancelLabel, () => lastPromptResult = "User clicked Cancel" }
				};
				popupPrompt.Open("Confirmation", "Do you want to proceed with this action?", buttons);
			}

			ImGui.SameLine();
			if (DemoButton("Show Warning Prompt"))
			{
				Dictionary<string, Action?> buttons = new()
				{
					{ "Delete", () => lastPromptResult = "User confirmed deletion" },
					{ CancelLabel, () => lastPromptResult = "User canceled deletion" }
				};
				string warning = "⚠️ WARNING: This action cannot be undone!\n\nAre you sure you want to delete all selected files?";
				popupPrompt.Open("Confirm Deletion", warning, buttons, ImGuiPopups.PromptTextLayoutType.Wrapped, new Vector2(400, 200));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderSearchableListsSection()
	{
		if (DemoHeader("Searchable Lists", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Select items from searchable and filterable lists.");
			ImGui.Spacing();

			ImGui.Text($"Selected Friend: {selectedFriend}");
			if (DemoButton("Choose Friend"))
			{
				popupSearchableListFriends.Open("Choose Friend", "Select your best friend:", Friends, result => selectedFriend = result);
			}

			ImGui.SameLine();
			if (DemoButton("Choose Friend (Custom Size)"))
			{
				popupSearchableListFriends.Open("Choose Friend", "Select your best friend:", Friends, null, null, result => selectedFriend = result, new Vector2(350, 400));
			}

			ImGui.Text($"Selected Color: {selectedColor}");
			if (DemoButton("Choose Color"))
			{
				popupSearchableListColors.Open("Choose Color", "Select your favorite color:", Colors,
					item => $"🎨 {item}", result => selectedColor = result);
			}

			ImGui.Spacing();
		}
	}

	private static void RenderFileSystemBrowserSection()
	{
		if (DemoHeader("File System Browser", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Browse and select files or directories with filtering support.");
			ImGui.Spacing();

			// File Operations
			ImGui.Text($"Last File Opened: {lastFileOpened}");
			if (DemoButton("Open Any File"))
			{
				popupFilesystemBrowser.FileOpen("Open File", file => lastFileOpened = file.ToString());
			}

			ImGui.SameLine();
			if (DemoButton("Open C# File"))
			{
				popupFilesystemBrowser.FileOpen("Open C# File", file => lastFileOpened = file.ToString(), "*.cs");
			}

			ImGui.SameLine();
			if (DemoButton("Open Image File"))
			{
				popupFilesystemBrowser.FileOpen("Open Image File", file => lastFileOpened = file.ToString(), new Vector2(600, 500), "*.{png,jpg,jpeg,gif,bmp}");
			}

			ImGui.Text($"Last File Saved: {lastFileSaved}");
			if (DemoButton("Save Text File"))
			{
				popupFilesystemBrowser.FileSave("Save Text File", file => lastFileSaved = file.ToString(), "*.txt");
			}

			ImGui.SameLine();
			if (DemoButton("Save Any File"))
			{
				popupFilesystemBrowser.FileSave("Save File", file => lastFileSaved = file.ToString());
			}

			// Directory Operations
			ImGui.Text($"Last Directory Chosen: {lastDirectoryChosen}");
			if (DemoButton("Choose Directory"))
			{
				popupFilesystemBrowser.ChooseDirectory("Choose Directory", directory => lastDirectoryChosen = directory.ToString());
			}

			ImGui.SameLine();
			if (DemoButton("Choose Directory (Large)"))
			{
				popupFilesystemBrowser.ChooseDirectory("Choose Directory", directory => lastDirectoryChosen = directory.ToString(), new Vector2(700, 600));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderCustomModalSection()
	{
		if (DemoHeader("Custom Modal", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.Text("Create completely custom modal content with full control.");
			ImGui.Spacing();

			ImGui.Text($"Last Custom Modal Result: {lastCustomModalResult}");
			if (DemoButton("Show Custom Modal"))
			{
				popupCustomModal.Open("Custom Modal Example", ShowCustomModalContent);
			}

			ImGui.SameLine();
			if (DemoButton("Show Custom Modal (Large)"))
			{
				popupCustomModal.Open("Advanced Custom Modal", ShowAdvancedCustomModalContent, new Vector2(600, 400));
			}

			ImGui.Spacing();
		}
	}

	private static void RenderAdvancedExamplesSection()
	{
		if (DemoHeader("Advanced Examples"))
		{
			ImGui.Text("Complex usage patterns and edge cases.");
			ImGui.Spacing();

			if (DemoButton("Nested Popup Example"))
			{
				popupMessageOK.Open("First Popup", "This popup will open another popup when you click OK.");
			}

			ImGui.SameLine();
			if (DemoButton("Validation Example"))
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

			if (DemoButton("Multi-Step Workflow"))
			{
				StartMultiStepWorkflow();
			}

			ImGui.Spacing();
		}
	}

	private static void RenderTipsSection()
	{
		if (DemoHeader("Tips & Features"))
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
		ImGuiProbes.MarkItem("Custom Checkbox");
		ImGui.SliderFloat("Custom Slider", ref customSlider, 0.0f, 1.0f);
		ImGuiProbes.MarkItem("Custom Slider");

		ImGui.NewLine();

		if (DemoButton("Set Result"))
		{
			lastCustomModalResult = $"Checkbox: {customCheckbox}, Slider: {customSlider:F2}";
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine();
		if (DemoButton("Close"))
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
		ImGuiProbes.MarkItem("Select Option");
		ImGui.ColorEdit3("Color Picker", ref advancedModalColorValue);
		ImGuiProbes.MarkItem("Color Picker");

		ImGui.Text("Flags:");
		for (int i = 0; i < advancedModalFlags.Length; i++)
		{
			ImGui.Checkbox($"Flag {i + 1}", ref advancedModalFlags[i]);
			ImGuiProbes.MarkItem($"Flag {i + 1}");
			if (i < advancedModalFlags.Length - 1)
			{
				ImGui.SameLine();
			}
		}

		ImGui.Separator();

		if (DemoButton("Apply Settings"))
		{
			lastCustomModalResult = $"Selected: {advancedModalItems[advancedModalSelectedItem]}, Color: RGB({advancedModalColorValue.X:F2}, {advancedModalColorValue.Y:F2}, {advancedModalColorValue.Z:F2})";
			ImGui.CloseCurrentPopup();
		}

		ImGui.SameLine();
		if (DemoButton("Reset"))
		{
			advancedModalSelectedItem = 0;
			advancedModalColorValue = new Vector3(1.0f, 0.5f, 0.0f);
			for (int i = 0; i < advancedModalFlags.Length; i++)
			{
				advancedModalFlags[i] = i % 2 == 0;
			}
		}

		ImGui.SameLine();
		if (DemoButton(CancelLabel))
		{
			lastCustomModalResult = "User canceled";
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
					{ CancelLabel, () => lastPromptResult = "Workflow canceled" }
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
			if (DemoMenuItem("Reset All Values"))
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

			if (DemoMenuItem("About"))
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

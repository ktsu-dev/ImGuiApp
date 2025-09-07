// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.Styler;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Styler;
using ktsu.ThemeProvider;

/// <summary>
/// Comprehensive demonstration of the ImGuiStyler library capabilities.
/// </summary>
internal sealed class ImGuiStylerDemo
{
	private static bool valueBool = true;
	private static bool valueBool2;
	private static int valueInt = 42;
	private static string valueString = "Hello ImGuiStyler!";
	private static float valueFloat = 0.5f;
	private static float valueFloat2 = 0.75f;
	private static int selectedFamily;
	private static readonly float[] colorValue = [0.4f, 0.7f, 0.0f, 1.0f];
	private static readonly string[] comboItems = ["Option A", "Option B", "Option C", "Option D"];
	private static int comboSelection;
	private static int radioSelection = 1;
	private static readonly float[] plotData = [0.6f, 0.1f, 1.0f, 0.5f, 0.92f, 0.1f, 0.2f];

	// Form validation fields
	private static string formUsername = "";
	private static string formEmail = "";

	// Use ThemeProvider registry for all available themes
	private static readonly List<ThemeRegistry.ThemeInfo> availableThemes = [.. Theme.AllThemes];
	private static readonly List<string> availableFamilies = [.. Theme.Families.OrderBy(f => f)];

	// Store the currently selected theme instead of applying it globally
	private static ThemeRegistry.ThemeInfo? currentSelectedTheme;

	private static void Main()
	{
		ImGuiStylerDemo demo = new();
		ImGuiApp.Start(new()
		{
			Title = "ImGuiStyler Demo - Comprehensive Theme & Color Showcase",
			OnAppMenu = demo.OnAppMenu,
			OnMoveOrResize = demo.OnMoveOrResize,
			OnRender = demo.OnRender,
			OnStart = demo.OnStart,
			FrameWrapperFactory = () => currentSelectedTheme is null ? null : new ScopedTheme(currentSelectedTheme.CreateInstance()),
			SaveIniSettings = false,
		});
	}

	private void OnStart()
	{
		// Keep default ImGui styling - we'll use scoped themes for demonstration
		// currentSelectedTheme starts as null, so we'll show default styling
	}

	private void OnRender(float dt)
	{
		// Header with current theme info
		if (currentSelectedTheme is not null)
		{
			ImGui.Text($"🎨 Current Theme: {currentSelectedTheme.Name}");
			ImGui.SameLine();
			ImGui.Text($"({(currentSelectedTheme.IsDark ? "Dark" : "Light")})");
		}
		else
		{
			ImGui.Text("🎨 Current Theme: Default (Reset)");
		}

		ImGui.Separator();

		// Render the library's theme selection dialog if it's open
		// This now returns true if a theme was changed during modal interaction
		if (Theme.RenderThemeSelector())
		{
			// Theme was changed via the modal browser - respond to the change
			if (Theme.CurrentThemeName is null)
			{
				Console.WriteLine("Theme reset to default via modal");
				currentSelectedTheme = null; // Update our local selection
			}
			else
			{
				Console.WriteLine($"Theme changed via modal to: {Theme.CurrentThemeName}");
				// Find and store the corresponding theme info
				currentSelectedTheme = availableThemes.FirstOrDefault(t => t.Name == Theme.CurrentThemeName);
			}
		}

		if (ImGui.BeginTabBar("DemoTabs"))
		{
			if (ImGui.BeginTabItem("🎨 Theme Gallery"))
			{
				ShowThemeGallery();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("🎨 Color Palettes"))
			{
				ShowColorPalettesDemo();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("🔍 Complete Theme Palette"))
			{
				ShowCompleteThemePalette();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("🖱️ Widget Showcase"))
			{
				ShowWidgetShowcase();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("💡 Interactive Examples"))
			{
				ShowInteractiveExamples();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("📚 Documentation"))
			{
				ShowDocumentationDemo();
				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}
	}

	private static void ShowThemeGallery()
	{
		ImGui.TextUnformatted("🎨 Theme Gallery");
		ImGui.Text("Explore all available themes from the ThemeProvider registry.");
		ImGui.Separator();

		// Theme family selector
		ImGui.Text("Filter by Family:");
		if (ImGui.Combo("##Family", ref selectedFamily, ["All", .. availableFamilies], availableFamilies.Count + 1))
		{
			// selectedTheme = 0; // Reset selection when family changes
		}

		ImGui.Separator();

		// Get filtered themes
		IEnumerable<ThemeRegistry.ThemeInfo> filteredThemes = selectedFamily == 0
			? availableThemes
			: availableThemes.Where(t => t.Family == availableFamilies[selectedFamily - 1]);

		List<ThemeRegistry.ThemeInfo> themesToShow = [.. filteredThemes];

		// Theme grid
		ImGui.Text($"Available Themes ({themesToShow.Count}):");

		// Add reset button
		if (ImGui.Button("Reset to Default"))
		{
			currentSelectedTheme = null;
		}
		ImGui.SameLine();
		ImGui.Text("(or click a theme below to apply it)");

		ImGui.BeginChild("ThemeGrid", new Vector2(0, 300), ImGuiChildFlags.Borders);

		// Use the new delegate-based ThemeCard widget from the library
		ThemeCard.RenderGrid(themesToShow, selectedTheme => currentSelectedTheme = selectedTheme);

		ImGui.EndChild();

		ImGui.Separator();

		// Quick theme preview with widgets
		ImGui.Text("Theme Preview:");
		ImGui.BeginChild("PreviewArea", new Vector2(0, 200), ImGuiChildFlags.Borders);

		ImGui.Text("Sample UI Elements:");
		if (ImGui.Button("Sample Button"))
		{
			// Button clicked
		}
		ImGui.SameLine();
		if (ImGui.SmallButton("Small"))
		{
			// Small button clicked
		}

		ImGui.Checkbox("Sample Checkbox", ref valueBool);
		ImGui.SliderFloat("Sample Slider", ref valueFloat, 0.0f, 1.0f);
		ImGui.InputText("Sample Input", ref valueString, 128);

		ImGui.Text("Radio Buttons:");
		ImGui.RadioButton("Option 1", ref radioSelection, 0);
		ImGui.SameLine();
		ImGui.RadioButton("Option 2", ref radioSelection, 1);
		ImGui.SameLine();
		ImGui.RadioButton("Option 3", ref radioSelection, 2);

		ImGui.EndChild();
	}

	private static void ShowCompleteThemePalette()
	{
		ImGui.TextUnformatted("🔍 Complete Theme Palette");
		ImGui.Text("Explore every color available in the current theme using the new MakeCompletePalette API.");
		ImGui.Separator();

		// Check if a theme is active
		IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? completePalette = Theme.GetCurrentThemeCompletePalette();
		if (completePalette is null)
		{
			ImGui.TextWrapped("No theme is currently active. Select a theme from the Theme Gallery tab to see its complete palette.");
			return;
		}

		ImGui.Text($"Theme: {Theme.CurrentThemeName} - {completePalette.Count} colors available");
		ImGui.Separator();

		// Help text moved above the table
		ImGui.TextUnformatted("💡 Tip: Click on any color swatch to copy its hex value to clipboard");
		ImGui.TextUnformatted("💡 Hover over swatches to see detailed color information and usage examples");
		ImGui.TextUnformatted("⚙ Icon overlay shows contrast test using highest priority neutral color on all backgrounds");
		ImGui.TextUnformatted("📊 Table shows semantic meanings (rows) × priorities (columns, VeryLow→VeryHigh) for easy comparison");
		ImGui.Separator();

		// Get all unique semantic meanings and priorities
		HashSet<SemanticMeaning> allMeanings = [.. completePalette.Keys.Select(k => k.Meaning)];
		HashSet<Priority> allPriorities = [.. completePalette.Keys.Select(k => k.Priority)];

		// Sort meanings and priorities
		List<SemanticMeaning> sortedMeanings = [.. allMeanings.OrderBy(m => m.ToString())];
		List<Priority> sortedPriorities = [.. allPriorities.OrderBy(p => p)]; // Lowest first (inverted)

		// Find the highest priority neutral color for icon overlay
		Priority? highestNeutralPriority = completePalette.Keys
			.Where(k => k.Meaning == SemanticMeaning.Neutral)
			.Select(k => k.Priority)
			.DefaultIfEmpty()
			.Max();

		ImColor? neutralIconColor = null;
		if (highestNeutralPriority.HasValue &&
			completePalette.TryGetValue(new(SemanticMeaning.Neutral, highestNeutralPriority.Value), out PerceptualColor neutralColor))
		{
			neutralIconColor = Color.FromPerceptualColor(neutralColor);
		}

		// Create table with semantic meanings as rows and priorities as columns
		const float swatchWidth = 90.0f;
		const float swatchHeight = 45.0f;

		// Calculate exact column count to avoid mismatches
		int totalColumns = sortedPriorities.Count + 1; // +1 for semantic name column

		ImGui.BeginGroup();
		if (ImGui.BeginTable("ColorPaletteTable", totalColumns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			// Set up columns
			ImGui.TableSetupColumn("Semantic", ImGuiTableColumnFlags.WidthFixed, 120.0f);
			foreach (Priority priority in sortedPriorities)
			{
				ImGui.TableSetupColumn(priority.ToString(), ImGuiTableColumnFlags.WidthFixed, swatchWidth);
			}
			ImGui.TableHeadersRow();

			// Create rows for each semantic meaning - be explicit about row count
			for (int rowIndex = 0; rowIndex < sortedMeanings.Count; rowIndex++)
			{
				SemanticMeaning meaning = sortedMeanings[rowIndex];
				ImGui.TableNextRow();

				// Column 0: Semantic meaning name
				ImGui.TableSetColumnIndex(0);
				ImGui.Text(meaning.ToString());

				// Columns 1 to N: Color swatches for each priority
				for (int colIndex = 0; colIndex < sortedPriorities.Count; colIndex++)
				{
					Priority priority = sortedPriorities[colIndex];
					ImGui.TableSetColumnIndex(colIndex + 1); // +1 because column 0 is semantic name

					SemanticColorRequest request = new(meaning, priority);
					if (completePalette.TryGetValue(request, out PerceptualColor color))
					{
						ImColor imColor = Color.FromPerceptualColor(color);

						// Color swatch button
						Vector2 swatchButtonSize = new(swatchWidth, swatchHeight);
						if (ImGui.ColorButton($"##swatch_{meaning}_{priority}",
							imColor.Value, ImGuiColorEditFlags.None, swatchButtonSize))
						{
							// Copy to clipboard on click
							string hexColor = $"#{(int)(imColor.Value.X * 255):X2}{(int)(imColor.Value.Y * 255):X2}{(int)(imColor.Value.Z * 255):X2}";
							ImGui.SetClipboardText(hexColor);
						}

						// Add icon overlay on all colors using highest priority neutral color
						if (neutralIconColor.HasValue)
						{
							// Draw a test icon (gear/settings icon) over the color to show contrast
							Vector2 swatchMin = ImGui.GetItemRectMin();
							Vector2 swatchMax = ImGui.GetItemRectMax();
							Vector2 iconPos = new(
								swatchMin.X + ((swatchMax.X - swatchMin.X) * 0.5f),
								swatchMin.Y + ((swatchMax.Y - swatchMin.Y) * 0.5f)
							);

							ImDrawListPtr drawList = ImGui.GetWindowDrawList();

							// Draw a simple gear-like icon using text
							string iconText = "⚙";
							Vector2 textSize = ImGui.CalcTextSize(iconText);
							Vector2 textPos = new(
								iconPos.X - (textSize.X * 0.5f),
								iconPos.Y - (textSize.Y * 0.5f)
							);

							// Use the highest priority neutral color for the icon
							drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(neutralIconColor.Value.Value), iconText);
						}

						// Tooltip
						if (ImGui.IsItemHovered())
						{
							Vector4 c = imColor.Value;
							ImGui.SetTooltip($"Meaning: {meaning}\n" +
								$"Priority: {priority}\n" +
								$"RGBA: ({c.X:F3}, {c.Y:F3}, {c.Z:F3}, {c.W:F3})\n" +
								$"Hex: #{(int)(c.X * 255):X2}{(int)(c.Y * 255):X2}{(int)(c.Z * 255):X2}\n" +
								$"Usage: Theme.GetColor(new SemanticColorRequest(SemanticMeaning.{meaning}, Priority.{priority}))\n" +
								$"Click to copy to clipboard");
						}
					}
					else
					{
						// Empty cell for missing color combinations - ensure we still occupy the cell space
						ImGui.Dummy(new Vector2(swatchWidth, swatchHeight));
					}
				}
			}

			ImGui.EndTable();
		}
		ImGui.EndGroup();
	}

	private static void ShowColorPalettesDemo()
	{
		ImGui.TextUnformatted("🎨 Color Palette System");
		ImGui.Text("Comprehensive color palette with theme-aware colors.");
		ImGui.Separator();

		// Basic Colors Palette
		RenderColorPaletteSection("Basic Colors", [
			("Red", Color.Palette.Basic.Red),
			("Green", Color.Palette.Basic.Green),
			("Blue", Color.Palette.Basic.Blue),
			("Yellow", Color.Palette.Basic.Yellow),
			("Cyan", Color.Palette.Basic.Cyan),
			("Magenta", Color.Palette.Basic.Magenta),
			("Orange", Color.Palette.Basic.Orange),
			("Pink", Color.Palette.Basic.Pink),
			("Lime", Color.Palette.Basic.Lime),
			("Purple", Color.Palette.Basic.Purple),
		]);

		// Semantic Colors Palette
		RenderColorPaletteSection("Semantic Colors", [
			("Error", Color.Palette.Semantic.Error),
			("Warning", Color.Palette.Semantic.Warning),
			("Success", Color.Palette.Semantic.Success),
			("Info", Color.Palette.Semantic.Info),
			("Primary", Color.Palette.Semantic.Primary),
			("Secondary", Color.Palette.Semantic.Secondary),
		]);

		// Neutral Colors Palette
		RenderColorPaletteSection("Neutral Colors", [
			("White", Color.Palette.Neutral.White),
			("Light Gray", Color.Palette.Neutral.LightGray),
			("Gray", Color.Palette.Neutral.Gray),
			("Dark Gray", Color.Palette.Neutral.DarkGray),
			("Black", Color.Palette.Neutral.Black),
		]);

		// Natural Colors Palette
		RenderColorPaletteSection("Natural Colors", [
			("Brown", Color.Palette.Natural.Brown),
			("Olive", Color.Palette.Natural.Olive),
			("Maroon", Color.Palette.Natural.Maroon),
			("Navy", Color.Palette.Natural.Navy),
			("Teal", Color.Palette.Natural.Teal),
			("Indigo", Color.Palette.Natural.Indigo),
		]);

		// Vibrant Colors Palette
		RenderColorPaletteSection("Vibrant Colors", [
			("Coral", Color.Palette.Vibrant.Coral),
			("Salmon", Color.Palette.Vibrant.Salmon),
			("Turquoise", Color.Palette.Vibrant.Turquoise),
			("Violet", Color.Palette.Vibrant.Violet),
			("Gold", Color.Palette.Vibrant.Gold),
			("Silver", Color.Palette.Vibrant.Silver),
		]);

		// Pastel Colors Palette
		RenderColorPaletteSection("Pastel Colors", [
			("Beige", Color.Palette.Pastel.Beige),
			("Peach", Color.Palette.Pastel.Peach),
			("Mint", Color.Palette.Pastel.Mint),
			("Lavender", Color.Palette.Pastel.Lavender),
			("Khaki", Color.Palette.Pastel.Khaki),
			("Plum", Color.Palette.Pastel.Plum),
		]);

		ImGui.Separator();

		// Interactive color manipulation
		ImGui.Text("🔧 Interactive Color Tools:");
		ImGui.ColorEdit4("Custom Color", ref colorValue[0]);

		// Show color variations
		ImColor baseColor = Color.FromRGBA(colorValue[0], colorValue[1], colorValue[2], colorValue[3]);
		ImGui.Text("Color Variations:");

		ImGui.BeginGroup();
		RenderColorSwatch("Original", baseColor);
		RenderColorSwatch("Darker", baseColor.MultiplyLuminance(0.7f));
		RenderColorSwatch("Lighter", baseColor.MultiplyLuminance(1.3f));
		RenderColorSwatch("Desaturated", baseColor.MultiplySaturation(0.5f));
		RenderColorSwatch("Saturated", baseColor.MultiplySaturation(1.5f));
		ImGui.EndGroup();
	}

	private static void RenderColorPaletteSection(string title, (string Name, ImColor Color)[] colors)
	{
		ImGui.Text($"• {title}:");
		ImGui.BeginGroup();

		foreach ((string name, ImColor color) in colors)
		{
			RenderColorSwatch(name, color);
		}

		ImGui.EndGroup();
		ImGui.Separator();
	}

	private static void RenderColorSwatch(string name, ImColor color)
	{
		Vector2 swatchSize = new(30, 25);
		ImGui.ColorButton($"##{name}Swatch", color.Value, ImGuiColorEditFlags.None, swatchSize);

		if (ImGui.IsItemHovered())
		{
			Vector4 c = color.Value;
			ImGui.SetTooltip($"{name}\nRGBA: ({c.X:F2}, {c.Y:F2}, {c.Z:F2}, {c.W:F2})\nHex: #{(int)(c.X * 255):X2}{(int)(c.Y * 255):X2}{(int)(c.Z * 255):X2}");
		}

		ImGui.SameLine();
		ImGui.Text(name);
	}

	private static void ShowWidgetShowcase()
	{
		ImGui.TextUnformatted("🖱️ Comprehensive Widget Showcase");
		ImGui.Text("All ImGui widgets styled with the current theme.");
		ImGui.Separator();

		// Layout in columns for better organization
		ImGui.Columns(2, "WidgetColumns", true);

		// Column 1: Input widgets
		ImGui.Text("📝 Input Widgets:");

		ImGui.Button("Standard Button");
		ImGui.SameLine();
		ImGui.SmallButton("Small");

		if (ImGui.ArrowButton("##left", ImGuiDir.Left))
		{
			// Left arrow clicked
		}
		ImGui.SameLine();
		if (ImGui.ArrowButton("##right", ImGuiDir.Right))
		{
			// Right arrow clicked
		}

		ImGui.Checkbox("Checkbox", ref valueBool);
		ImGui.Checkbox("Checkbox 2", ref valueBool2);

		ImGui.RadioButton("Radio A", ref radioSelection, 0);
		ImGui.RadioButton("Radio B", ref radioSelection, 1);
		ImGui.RadioButton("Radio C", ref radioSelection, 2);

		ImGui.SliderFloat("Slider", ref valueFloat, 0.0f, 1.0f);
		ImGui.SliderFloat("Slider 2", ref valueFloat2, -10.0f, 10.0f);
		ImGui.SliderInt("Int Slider", ref valueInt, 0, 100);

		ImGui.InputText("Text Input", ref valueString, 128);

		if (ImGui.BeginCombo("Combo", comboItems[comboSelection]))
		{
			for (int i = 0; i < comboItems.Length; i++)
			{
				bool isSelected = comboSelection == i;
				if (ImGui.Selectable(comboItems[i], isSelected))
				{
					comboSelection = i;
				}
				if (isSelected)
				{
					ImGui.SetItemDefaultFocus();
				}
			}
			ImGui.EndCombo();
		}

		ImGui.ColorEdit3("Color", ref colorValue[0]);

		ImGui.NextColumn();

		// Column 2: Display widgets
		ImGui.Text("📊 Display Widgets:");

		ImGui.Text($"Current Values:");
		ImGui.BulletText($"Bool: {valueBool}");
		ImGui.BulletText($"Int: {valueInt}");
		ImGui.BulletText($"Float: {valueFloat:F2}");
		ImGui.BulletText($"String: {valueString}");

		unsafe
		{
			fixed (float* plotPtr = plotData)
			{
				ImGui.PlotLines("##Plot"u8, plotPtr, plotData.Length, 0, "Sample Plot"u8, 0.0f, 1.0f, new Vector2(0, 80));
			}
		}

		ImGui.ProgressBar(valueFloat, new Vector2(-1, 0), $"{valueFloat * 100:F0}%");

		// Separator
		ImGui.Separator();

		// Tree node
		if (ImGui.TreeNode("Tree Node"u8))
		{
			ImGui.Text("Tree content");
			if (ImGui.TreeNode("Nested Node"u8))
			{
				ImGui.Text("Nested content");
				ImGui.TreePop();
			}
			ImGui.TreePop();
		}

		// Collapsing header
		if (ImGui.CollapsingHeader("Collapsing Header"u8))
		{
			ImGui.Text("Header content");
			ImGui.Indent();
			ImGui.Text("Indented text");
			ImGui.Unindent();
		}

		ImGui.Columns(1);

		// Full-width section
		ImGui.Separator();
		ImGui.Text("📋 Tables & Lists:");

		if (ImGui.BeginTable("DemoTable"u8, 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg))
		{
			ImGui.TableSetupColumn("Column A"u8);
			ImGui.TableSetupColumn("Column B"u8);
			ImGui.TableSetupColumn("Column C"u8);
			ImGui.TableHeadersRow();

			for (int row = 0; row < 5; row++)
			{
				ImGui.TableNextRow();
				for (int col = 0; col < 3; col++)
				{
					ImGui.TableSetColumnIndex(col);
					ImGui.Text($"Row {row}, Col {col}");
				}
			}
			ImGui.EndTable();
		}
	}

	private static void ShowInteractiveExamples()
	{
		ImGui.TextUnformatted("💡 Interactive Theme Examples");
		ImGui.Text("See how themes affect real UI components and workflows.");
		ImGui.Separator();

		// Theme-aware text colors demonstration
		ImGui.Text("🎨 Semantic Text Colors in Action:");
		ImGui.BeginChild("TextColorDemo", new Vector2(0, 120), ImGuiChildFlags.Borders);

		using (Text.Color.Error())
		{
			ImGui.TextUnformatted("❌ Error: Connection failed!");
		}

		using (Text.Color.Warning())
		{
			ImGui.TextUnformatted("⚠️ Warning: Low disk space");
		}

		using (Text.Color.Success())
		{
			ImGui.TextUnformatted("✅ Success: File saved successfully");
		}

		using (Text.Color.Info())
		{
			ImGui.TextUnformatted("ℹ️ Info: 5 items processed");
		}

		ImGui.EndChild();

		ImGui.Separator();

		// Scoped theming demonstration
		ImGui.Text("🎯 Scoped Theme Applications:");

		ImGui.Text("Normal themed section:");
		ImGui.Button("Normal Button");
		ImGui.SliderFloat("Normal Slider", ref valueFloat, 0.0f, 1.0f);

		ImGui.Separator();

		ImGui.Text("Scoped color themes:");
		using (Theme.FromColor(Color.Palette.Semantic.Error))
		{
			ImGui.Text("Error-themed section:");
			ImGui.Button("Danger Button");
			ImGui.ProgressBar(0.8f, new Vector2(-1, 0), "80% Critical");
		}

		using (Theme.FromColor(Color.Palette.Semantic.Success))
		{
			ImGui.Text("Success-themed section:");
			ImGui.Button("Success Button");
			ImGui.ProgressBar(0.9f, new Vector2(-1, 0), "90% Complete");
		}

		ImGui.Separator();

		// Form example with validation
		ImGui.Text("📋 Form Example with Validation:");
		ImGui.BeginChild("FormExample", new Vector2(0, 150), ImGuiChildFlags.Borders);

		ImGui.Text("User Registration:");
		ImGui.InputText("Username", ref formUsername, 64);

		if (string.IsNullOrWhiteSpace(formUsername))
		{
			using ScopedColor errorText = new(Color.Palette.Basic.Red);
			ImGui.Text("❌ Required");
		}
		else if (formUsername.Length < 3)
		{
			using ScopedColor warningText = new(Color.Palette.Basic.Yellow);
			ImGui.Text("⚠ Username should be at least 3 characters");
		}
		else
		{
			using ScopedColor successText = new(Color.Palette.Basic.Green);
			ImGui.Text("✓ Username looks good");
		}

		ImGui.InputText("Email", ref formEmail, 128);

		bool validEmail = formEmail.Contains('@') && formEmail.Contains('.');
		if (!string.IsNullOrWhiteSpace(formEmail) && !validEmail)
		{
			using ScopedColor errorText = new(Color.Palette.Basic.Red);
			ImGui.Text("⚠ Invalid email format");
		}
		else if (validEmail)
		{
			using ScopedColor successText = new(Color.Palette.Basic.Green);
			ImGui.Text("✓ Email looks valid");
		}

		bool canSubmit = !string.IsNullOrWhiteSpace(formUsername) && formUsername.Length >= 3 && validEmail;

		if (!canSubmit)
		{
			using (Theme.DisabledFromColor(Color.Palette.Neutral.Gray))
			{
				ImGui.Button("Submit (Complete form first)");
			}
		}
		else
		{
			using (Theme.FromColor(Color.Palette.Semantic.Success))
			{
				if (ImGui.Button("Submit Registration"))
				{
					// Handle submission
				}
			}
		}

		ImGui.EndChild();

		// Form validation example with basic colors
		ImGui.Text("📝 Form with Validation:");
		ImGui.InputText("Username", ref formUsername, 64);
		ImGui.SameLine();
		if (string.IsNullOrWhiteSpace(formUsername))
		{
			using ScopedColor errorText = new(Color.Palette.Basic.Red);
			ImGui.Text("❌ Required");
		}
		else
		{
			using ScopedColor successText = new(Color.Palette.Basic.Green);
			ImGui.Text("✅ Valid");
		}

		ImGui.InputText("Email", ref formEmail, 64);
		ImGui.SameLine();
		if (string.IsNullOrWhiteSpace(formEmail) || !formEmail.Contains('@'))
		{
			using ScopedColor errorText = new(Color.Palette.Basic.Red);
			ImGui.Text("❌ Invalid");
		}
		else
		{
			using ScopedColor successText = new(Color.Palette.Basic.Green);
			ImGui.Text("✅ Valid");
		}

		ImGui.Separator();

		// Scoped Theme example
		ImGui.Text("🎨 Scoped Theme Example:");
		ImGui.TextWrapped("The ScopedTheme class applies a complete semantic theme temporarily within a 'using' block, then automatically reverts to the original styling when the scope ends.");

		ImGui.Separator();
		ImGui.Text("Normal styling here...");

		if (ImGui.Button("Normal Button"))
		{
			// Normal button styling
		}

		// Apply a temporary theme for this section
		if (availableThemes.Count > 0)
		{
			ThemeRegistry.ThemeInfo demoTheme = availableThemes[0]; // Use first available theme

			ImGui.Separator();
			ImGui.Text($"Section with {demoTheme.Name} theme applied using ScopedTheme:");

			// This 'using' block applies the theme temporarily
			using (new ScopedTheme(demoTheme.CreateInstance()))
			{
				if (ImGui.Button("Themed Button"))
				{
					// This button uses the scoped theme
				}

				ImGui.SameLine();
				if (ImGui.SmallButton("Small Themed"))
				{
					// This button also uses the scoped theme
				}

				ImGui.Checkbox("Themed Checkbox", ref valueBool2);
				ImGui.SliderFloat("Themed Slider", ref valueFloat2, 0.0f, 1.0f);

				ImGui.Text("All UI elements in this block use the scoped theme colors!");
			}
			// Theme automatically reverts here when the 'using' block ends
		}

		ImGui.Separator();
		ImGui.Text("Back to normal styling automatically...");
		if (ImGui.Button("Normal Button Again"))
		{
			// Back to normal styling
		}

		ImGui.TextWrapped("💡 Usage: using (new ScopedTheme(myTheme)) { /* themed UI here */ }");

		ImGui.Separator();
	}

	private void OnAppMenu()
	{
		// Use the library's improved theme selector menu
		if (Theme.RenderThemeSelectorMenu())
		{
			// Theme changed - this is where you would save the current theme to settings
			// For example: Settings.Theme = Theme.CurrentThemeName;
			if (Theme.CurrentThemeName is null)
			{
				Console.WriteLine("Theme reset to default");
				currentSelectedTheme = null; // Update our local selection
			}
			else
			{
				Console.WriteLine($"Theme changed to: {Theme.CurrentThemeName}");
				// Find and store the corresponding theme info
				currentSelectedTheme = availableThemes.FirstOrDefault(t => t.Name == Theme.CurrentThemeName);
			}
		}

		if (ImGui.BeginMenu("Help"))
		{
			if (ImGui.MenuItem("About ImGuiStyler"))
			{
				// Show about dialog - placeholder for demonstration
			}
			ImGui.EndMenu();
		}
	}

	private static void ShowDocumentationDemo()
	{
		ImGui.TextUnformatted("📚 ImGuiStyler Documentation & Examples");
		ImGui.Separator();

		ImGui.TextWrapped("ImGuiStyler provides comprehensive theming and styling capabilities for Dear ImGui applications. It integrates with ThemeProvider to offer semantic theming with consistent color meanings across different themes.");

		ImGui.Separator();
		ImGui.Text("Usage Examples:");

		ImGui.BeginChild("CodeExamples", new Vector2(0, 0), ImGuiChildFlags.Borders);

		ImGui.Text("// Apply semantic themes using ScopedTheme (recommended)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "var theme = ThemeRegistry.FindTheme(\"Dracula\");");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "using (new ScopedTheme(theme.CreateInstance()))");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // All UI rendering in this block uses the theme");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // Color mappings are cached for performance");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    ImGui.Button(\"Themed Button\");");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Clear cache if needed (rarely required)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "ScopedTheme.ClearCache();");
		ImGui.TextUnformatted("");

		ImGui.Text("// Or apply themes globally (affects all subsequent UI)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.Apply(\"Nord\");");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.Apply(\"Catppuccin Mocha\");");
		ImGui.TextUnformatted("");

		ImGui.Text("// Reset to default ImGui styling");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.ResetToDefault();");
		ImGui.Text("// or via property");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.CurrentThemeName = null;");
		ImGui.TextUnformatted("");

		ImGui.Text("// Render theme selection menu in your main menu bar");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (Theme.RenderThemeSelectorMenu())");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // Theme was changed - save current theme to settings");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    Settings.Theme = Theme.CurrentThemeName;");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Render the theme browser modal (call in main render loop)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (Theme.RenderThemeSelector()) // Returns true if theme changed");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // Theme was changed via modal - respond to change");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    Settings.Theme = Theme.CurrentThemeName;");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Programmatically open the theme browser modal");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.ShowThemeSelector(); // Opens the modal dialog");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.ShowThemeSelector(\"Custom Title\", new Vector2(900, 700));");
		ImGui.TextUnformatted("");

		ImGui.Text("// The ThemeBrowser uses ktsu.ImGuiPopups for proper modal behavior");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "// - Blocks interaction with underlying UI");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "// - ESC to close, centered positioning");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "// - Follows established ktsu.dev modal patterns");
		ImGui.TextUnformatted("");

		ImGui.Text("// Restore theme on application start");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "Theme.CurrentThemeName = Settings.Theme; // null restores default");
		ImGui.TextUnformatted("");

		ImGui.Text("// Use semantic text colors");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "using (Text.Color.Error())");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    ImGui.Text(\"Error message\");");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Render theme preview cards");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (ThemeCard.Render(theme))");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    Theme.Apply(theme.Name);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Render a grid of theme cards with delegate callback (recommended)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "ThemeCard.RenderGrid(themes, selectedTheme =>");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // Handle theme selection via delegate");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    currentSelectedTheme = selectedTheme;");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "});");
		ImGui.TextUnformatted("");

		ImGui.Text("// Or use the return value approach (still supported)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "var clicked = ThemeCard.RenderGrid(themes);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (clicked != null)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    Theme.Apply(clicked.Name);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Use color palette (theme-aware)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "ImColor primaryColor = Color.Palette.Semantic.Primary;");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "ImColor errorColor = Color.Palette.Semantic.Error;");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "ImColor customRed = Color.Palette.Basic.Red; // Adapts to theme");
		ImGui.TextUnformatted("");

		ImGui.Text("// NEW: Use complete theme palette API (powered by MakeCompletePalette)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "var palette = Theme.GetCurrentThemeCompletePalette();");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "var primaryHigh = new SemanticColorRequest(SemanticMeaning.Primary, Priority.High);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (palette?.TryGetValue(primaryHigh, out var color) == true)");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    ImColor myColor = Color.FromPerceptualColor(color);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Or use the simpler helper methods");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "var request = new SemanticColorRequest(SemanticMeaning.Error, Priority.VeryHigh);");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "if (Theme.TryGetColor(request, out var errorColor))");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    // Use the specific error color from theme");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");
		ImGui.TextUnformatted("");

		ImGui.Text("// Scoped theme colors for UI sections");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "using (Theme.FromColor(Color.Palette.Semantic.Success))");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "{");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "    ImGui.Button(\"Success Button\");");
		ImGui.TextColored(new Vector4(0.6f, 0.8f, 0.6f, 1.0f), "}");

		ImGui.EndChild();
	}

	private void OnMoveOrResize()
	{
		// Handle window resize if needed
	}
}

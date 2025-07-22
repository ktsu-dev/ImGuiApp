// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo;

using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiAppDemo.Properties;
using ktsu.StrongPaths;

internal static class ImGuiAppDemo
{
	private static bool showImGuiDemo;
	private static bool showStyleEditor;
	private static bool showMetrics;
	private static bool showAbout;

	// Demo state - Basic Widgets
	private static float sliderValue = 0.5f;
	private static int counter;
	private static bool checkboxState;
	private static string inputText = "Type here...";
	private static Vector3 colorPickerValue = new(0.4f, 0.7f, 0.2f);
	private static Vector4 color4Value = new(1.0f, 0.5f, 0.2f, 1.0f);
	private static readonly Random random = new();
	private static readonly List<float> plotValues = [];
	private static float plotRefreshTime;

	// Advanced widget states
	private static int comboSelection;
	private static readonly string[] comboItems = ["Item 1", "Item 2", "Item 3", "Item 4"];
	private static int listboxSelection;
	private static readonly string[] listboxItems = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];
	private static float dragFloat = 1.0f;
	private static int dragInt = 50;
	private static Vector3 dragVector = new(1.0f, 2.0f, 3.0f);
	private static float angle;

	// Table demo state
	private static readonly List<DemoItem> tableData = [];
	private static bool showTableHeaders = true;
	private static bool showTableBorders = true;

	// Text rendering state
	private static readonly StringBuilder textBuffer = new(1024);
	private static bool wrapText = true;
	private static float textSpeed = 50.0f;
	private static float animationTime;

	// Canvas drawing state
	private static readonly List<Vector2> canvasPoints = [];
	private static Vector4 drawColor = new(1.0f, 1.0f, 0.0f, 1.0f);
	private static float brushSize = 5.0f;

	// Modal and popup states
	private static bool showModal;
	private static bool showPopup;
	private static string modalResult = "";

	// Performance monitoring
	private static readonly Queue<float> frameTimes = new();
	private static float frameTimeSum;

	// File operations
	private static string filePath = "";
	private static string fileContent = "";

	// Animation demo
	private static float bounceOffset;
	private static float pulseScale = 1.0f;

	// Additional UI state
	private static int radioSelection;
	private static string modalInputBuffer = "";

	private record DemoItem(int Id, string Name, string Category, float Value, bool Active);

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "<Pending>")]
	static ImGuiAppDemo()
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

		textBuffer.Append("This is a demonstration of ImGui text editing capabilities.\n");
		textBuffer.Append("You can edit this text, and it will update in real-time.\n");
		textBuffer.Append("ImGui supports multi-line text editing with syntax highlighting possibilities.");
	}

	private static void Main() => ImGuiApp.Start(new()
	{
		Title = "ImGuiApp Demo",
		IconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>(),
		OnRender = OnRender,
		OnAppMenu = OnAppMenu,
		SaveIniSettings = false,
		// Note: EnableUnicodeSupport = true by default, so Unicode and emojis are automatically enabled!
		Fonts = new Dictionary<string, byte[]>
		{
			{ nameof(Resources.CARDCHAR), Resources.CARDCHAR }
		},
		// Example of configuring performance settings for throttled rendering
		PerformanceSettings = new()
		{
			EnableThrottledRendering = true,
			// Using default values: Focused=30, Unfocused=5, Idle=10 FPS
			// But with a shorter idle timeout for demo purposes
			IdleTimeoutSeconds = 5.0, // Consider idle after 5 seconds (default is 30)
		},
	});

	private static void OnRender(float dt)
	{
		UpdateAnimations(dt);
		UpdatePerformanceMetrics(dt);
		RenderMainDemoWindow();

		// Show additional windows based on menu toggles
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

		if (showAbout)
		{
			RenderAboutWindow();
		}

		// Handle modals and popups
		RenderModalAndPopups();

		// Update plot data
		UpdatePlotData(dt);
	}

	private static void RenderMainDemoWindow()
	{
		// Create tabs for different demo sections
		if (ImGui.BeginTabBar("DemoTabs", ImGuiTabBarFlags.None))
		{
			RenderBasicWidgetsTab();
			RenderAdvancedWidgetsTab();
			RenderLayoutTab();
			RenderGraphicsTab();
			RenderDataVisualizationTab();
			RenderInputHandlingTab();
			RenderAnimationTab();
			RenderUnicodeTab();
			RenderNerdFontTab();
			RenderUtilityTab();
			RenderPerformanceTab();
			ImGui.EndTabBar();
		}
	}

	private static void RenderBasicWidgetsTab()
	{
		if (ImGui.BeginTabItem("Basic Widgets"))
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

	private static void RenderAdvancedWidgetsTab()
	{
		if (ImGui.BeginTabItem("Advanced Widgets"))
		{
			// Color controls
			ImGui.Text("Color Controls:");
			ImGui.ColorEdit3("Color RGB", ref colorPickerValue);
			ImGui.ColorEdit4("Color RGBA", ref color4Value);
			ImGui.SetNextItemWidth(200.0f);
			ImGui.ColorPicker3("Color Picker", ref colorPickerValue);

			ImGui.Separator();

			// Tree view
			ImGui.Text("Tree View:");
			if (ImGui.TreeNode("Root Node"))
			{
				for (int i = 0; i < 5; i++)
				{
					string nodeName = $"Child Node {i}";
					bool nodeOpen = ImGui.TreeNode(nodeName);

					if (i == 2 && nodeOpen)
					{
						for (int j = 0; j < 3; j++)
						{
							if (ImGui.TreeNode($"Grandchild {j}"))
							{
								ImGui.Text($"Leaf item {j}");
								ImGui.TreePop();
							}
						}
					}
					else if (nodeOpen)
					{
						ImGui.Text($"Content of {nodeName}");
					}

					if (nodeOpen)
					{
						ImGui.TreePop();
					}
				}
				ImGui.TreePop();
			}

			ImGui.Separator();

			// Progress bars and loading indicators
			ImGui.Text("Progress Indicators:");
			float progress = ((float)Math.Sin(animationTime * 2.0) * 0.5f) + 0.5f;
			ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{progress * 100:F1}%");

			// Spinner-like effect
			ImGui.Text("Loading...");
			ImGui.SameLine();
			for (int i = 0; i < 8; i++)
			{
				float rotation = (animationTime * 5.0f) + (i * MathF.PI / 4.0f);
				float alpha = (MathF.Sin(rotation) + 1.0f) * 0.5f;
				ImGui.TextColored(new Vector4(1, 1, 1, alpha), "●");
				if (i < 7)
				{
					ImGui.SameLine();
				}
			}

			ImGui.EndTabItem();
		}
	}

	private static void RenderLayoutTab()
	{
		if (ImGui.BeginTabItem("Layout & Tables"))
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

	private static void RenderGraphicsTab()
	{
		if (ImGui.BeginTabItem("Graphics & Drawing"))
		{
			// Image display
			AbsoluteFilePath iconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
			ImGuiAppTextureInfo iconTexture = ImGuiApp.GetOrLoadTexture(iconPath);

			ImGui.Text("Image Display:");
			ImGui.Image(iconTexture.TextureRef, new Vector2(64, 64));
			ImGui.SameLine();
			ImGui.Image(iconTexture.TextureRef, new Vector2(32, 32));
			ImGui.SameLine();
			ImGui.Image(iconTexture.TextureRef, new Vector2(16, 16));

			ImGui.Separator();

			// Custom drawing with ImDrawList
			ImGui.Text("Custom Drawing Canvas:");
			ImGui.ColorEdit4("Draw Color", ref drawColor);
			ImGui.SliderFloat("Brush Size", ref brushSize, 1.0f, 20.0f);

			if (ImGui.Button("Clear Canvas"))
			{
				canvasPoints.Clear();
			}

			Vector2 canvasPos = ImGui.GetCursorScreenPos();
			Vector2 canvasSize = new(400, 200);

			// Draw canvas background
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));
			drawList.AddRect(canvasPos, canvasPos + canvasSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

			// Handle mouse input for drawing
			ImGui.InvisibleButton("Canvas", canvasSize);
			if (ImGui.IsItemActive() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				Vector2 mousePos = ImGui.GetMousePos() - canvasPos;
				if (mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= canvasSize.X && mousePos.Y <= canvasSize.Y)
				{
					canvasPoints.Add(mousePos);
				}
			}

			// Draw points
			uint color = ImGui.ColorConvertFloat4ToU32(drawColor);
			foreach (Vector2 point in canvasPoints)
			{
				drawList.AddCircleFilled(canvasPos + point, brushSize, color);
			}

			// Draw some simple shapes for demonstration
			ImGui.Separator();
			ImGui.Text("Shape Examples:");
			Vector2 shapeStart = ImGui.GetCursorScreenPos();

			// Simple animated circle
			float t = animationTime;
			Vector2 center = shapeStart + new Vector2(100, 50);
			float radius = 20 + (MathF.Sin(t * 2) * 5);
			drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), 16, 2.0f);

			// Moving rectangle
			Vector2 rectPos = shapeStart + new Vector2(200 + (MathF.Sin(t) * 30), 30);
			drawList.AddRectFilled(rectPos, rectPos + new Vector2(40, 40), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 1, 0, 0.7f)));

			ImGui.Dummy(new Vector2(400, 100)); // Reserve space

			ImGui.EndTabItem();
		}
	}

	private static void RenderDataVisualizationTab()
	{
		if (ImGui.BeginTabItem("Data Visualization"))
		{
			ImGui.Text("Real-time Data Plots:");

			// Line plot
			if (plotValues.Count > 0)
			{
				float[] values = [.. plotValues];
				ImGui.PlotLines("Random Values", ref values[0], values.Length, 0,
					$"Current: {values[^1]:F2}", 0.0f, 1.0f, new Vector2(ImGui.GetContentRegionAvail().X, 100));

				ImGui.PlotHistogram("Distribution", ref values[0], values.Length, 0,
					"Histogram", 0.0f, 1.0f, new Vector2(ImGui.GetContentRegionAvail().X, 100));
			}

			ImGui.Separator();

			// Performance metrics
			ImGui.Text("Performance Metrics:");
			float avgFrameTime = frameTimes.Count > 0 ? frameTimeSum / frameTimes.Count : 0;
			float fps = avgFrameTime > 0 ? 1.0f / avgFrameTime : 0;

			ImGui.Text($"FPS: {fps:F1}");
			ImGui.Text($"Frame Time: {avgFrameTime * 1000:F2}ms");

			if (frameTimes.Count > 0)
			{
				float[] frameTimeArray = [.. frameTimes];
				ImGui.PlotLines("Frame Times", ref frameTimeArray[0], frameTimeArray.Length, 0,
					$"{avgFrameTime * 1000:F2}ms", 0, 0, new Vector2(ImGui.GetContentRegionAvail().X, 80));
			}

			ImGui.Separator();

			// Font demonstrations
			ImGui.Text("Custom Font Rendering:");
			using (new FontAppearance(nameof(Resources.CARDCHAR), 16))
			{
				ImGui.Text("Small custom font text");
			}

			using (new FontAppearance(nameof(Resources.CARDCHAR), 24))
			{
				ImGui.Text("Medium custom font text");
			}

			using (new FontAppearance(nameof(Resources.CARDCHAR), 32))
			{
				ImGui.Text("Large custom font text");
			}

			// Text formatting examples
			ImGui.Separator();
			ImGui.Text("Text Formatting:");
			ImGui.TextColored(new Vector4(1, 0, 0, 1), "Red text");
			ImGui.TextColored(new Vector4(0, 1, 0, 1), "Green text");
			ImGui.TextColored(new Vector4(0, 0, 1, 1), "Blue text");
			ImGui.TextWrapped("This is a long line of text that should wrap to multiple lines when the window is not wide enough to contain it all on a single line.");

			ImGui.EndTabItem();
		}
	}

	private static void RenderInputHandlingTab()
	{
		if (ImGui.BeginTabItem("Input & Interaction"))
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

	private static void RenderAnimationTab()
	{
		if (ImGui.BeginTabItem("Animation & Effects"))
		{
			ImGui.Text("Animation Examples:");

			// Simple animations
			ImGui.Text("Bouncing Animation:");
			Vector2 ballPos = ImGui.GetCursorScreenPos();
			ballPos.Y += bounceOffset;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddCircleFilled(ballPos + new Vector2(50, 50), 20, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0, 1)));
			ImGui.Dummy(new Vector2(100, 100));

			// Pulsing element
			ImGui.Text("Pulse Animation:");
			Vector2 pulsePos = ImGui.GetCursorScreenPos();
			float pulseSize = 20 * pulseScale;
			drawList.AddCircleFilled(pulsePos + new Vector2(50, 50), pulseSize,
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0, 1, 0.7f)));
			ImGui.Dummy(new Vector2(100, 100));

			ImGui.Separator();

			// Animation controls
			ImGui.Text("Animation Controls:");
			ImGui.SliderFloat("Text Speed", ref textSpeed, 10.0f, 200.0f);

			// Animated text (simplified)
			ImGui.Text("Animated text effects:");
			for (int i = 0; i < 20; i++)
			{
				float wave = (MathF.Sin((animationTime * 3.0f) + (i * 0.5f)) * 0.5f) + 0.5f;
				ImGui.TextColored(new Vector4(wave, 1.0f - wave, 0.5f, 1.0f), i % 5 == 4 ? " " : "▓");
				if (i % 5 != 4)
				{
					ImGui.SameLine();
				}
			}

			ImGui.EndTabItem();
		}
	}

	private static void RenderUtilityTab()
	{
		if (ImGui.BeginTabItem("Utilities & Tools"))
		{
			// File operations
			ImGui.Text("File Operations:");
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
				ImGui.Text("File Content Preview:");
				ImGui.TextWrapped(fileContent.Length > 500 ? fileContent[..500] + "..." : fileContent);
			}

			ImGui.Separator();

			// System information
			ImGui.Text("System Information:");
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

			ImGui.Separator();

			// Debugging tools
			ImGui.Text("Debug Tools:");
			if (ImGui.Button("Show ImGui Demo"))
			{
				showImGuiDemo = true;
			}
			ImGui.SameLine();
			if (ImGui.Button("Show Style Editor"))
			{
				showStyleEditor = true;
			}
			ImGui.SameLine();
			if (ImGui.Button("Show Metrics"))
			{
				showMetrics = true;
			}

			ImGui.EndTabItem();
		}
	}

	private static void RenderUnicodeTab()
	{
		if (ImGui.BeginTabItem("Unicode & Emojis"))
		{
			ImGui.TextWrapped("Unicode and Emoji Support (Enabled by Default)");
			ImGui.TextWrapped("ImGuiApp automatically includes support for Unicode characters and emojis. This feature works with your configured fonts.");
			ImGui.Separator();

			ImGui.Text("Basic ASCII: Hello World!");
			ImGui.Text("Accented characters: café, naïve, résumé");
			ImGui.Text("Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑ ∏ √ ∫");
			ImGui.Text("Currency symbols: $ € £ ¥ ₹ ₿");
			ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓");
			ImGui.Text("Geometric shapes: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆");
			ImGui.Text("Miscellaneous symbols: ♠ ♣ ♥ ♦ ☀ ☁ ☂ ☃ ♪ ♫");

			ImGui.Separator();
			ImGui.Text("Full Emoji Range Support (if font supports them):");
			ImGui.Text("Faces: 😀 😃 😄 😁 😆 😅 😂 🤣 😊 😇 😍 😎 🤓 🧐 🤔 😴");
			ImGui.Text("Gestures: 👍 👎 👌 ✌️ 🤞 🤟 🤘 🤙 👈 👉 👆 👇 ☝️ ✋ 🤚 🖐");
			ImGui.Text("Objects: 🚀 💻 📱 🎸 🎨 🏆 🌟 💎 ⚡ 🔥 💡 🔧 ⚙️ 🔑 💰");
			ImGui.Text("Nature: 🌈 🌞 🌙 ⭐ 🌍 🌊 🌳 🌸 🦋 🐝 🐶 🐱 🦊 🐻 🐼");
			ImGui.Text("Food: 🍎 🍌 🍕 🍔 🍟 🍦 🎂 ☕ 🍺 🍷 🍓 🥑 🥨 🧀 🍯");
			ImGui.Text("Transport: 🚗 🚂 ✈️ 🚲 🚢 🚁 🚌 🏍️ 🛸 🚜 🏎️ 🚙 🚕 🚐");
			ImGui.Text("Activities: ⚽ 🏀 🏈 ⚾ 🎾 🏐 🏉 🎱 🏓 🏸 🥊 ⛳ 🎯 🎪");
			ImGui.Text("Weather: ☀️ ⛅ ☁️ 🌤️ ⛈️ 🌧️ ❄️ ☃️ ⛄ 🌬️ 💨 🌊 💧");
			ImGui.Text("Symbols: ❤️ 💚 💙 💜 🖤 💛 💔 ❣️ 💕 💖 💗 💘 💝 ✨");
			ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕ ↖ ↗ ↘ ↙ ⤴️ ⤵️ 🔀 🔁 🔂 🔄 🔃");
			ImGui.Text("Math: ± × ÷ = ≠ ≈ ≤ ≥ ∞ √ ∑ ∏ ∂ ∫ Ω π α β γ δ");
			ImGui.Text("Geometric: ■ □ ▲ △ ● ○ ◆ ◇ ★ ☆ ♠ ♣ ♥ ♦ ▪ ▫ ◾ ◽");
			ImGui.Text("Currency: $ € £ ¥ ₹ ₿ ¢ ₽ ₩ ₡ ₪ ₫ ₱ ₴ ₦ ₨ ₵");
			ImGui.Text("Dingbats: ✂ ✈ ☎ ⌚ ⏰ ⏳ ⌛ ⚡ ☔ ☂ ☀ ⭐ ☁ ⛅ ❄");
			ImGui.Text("Enclosed: ① ② ③ ④ ⑤ ⑥ ⑦ ⑧ ⑨ ⑩ ⓐ ⓑ ⓒ ⓓ ⓔ ⓕ");

			ImGui.Separator();
			ImGui.TextWrapped("Note: Character display depends on your configured font's Unicode support. " +
							 "If characters show as question marks, your font may not include those glyphs.");

			ImGui.Separator();
			ImGui.TextWrapped("To disable Unicode support (ASCII only), set EnableUnicodeSupport = false in your ImGuiAppConfig.");

			ImGui.EndTabItem();
		}
	}

	private static void RenderNerdFontTab()
	{
		if (ImGui.BeginTabItem("Nerd Fonts"))
		{
			ImGui.TextWrapped("Nerd Font Icons (Patched Fonts)");
			ImGui.TextWrapped("This tab demonstrates Nerd Font icons if you're using a Nerd Font (like JetBrains Mono Nerd Font, Fira Code Nerd Font, etc.).");
			ImGui.Separator();

			// Powerline symbols
			ImGui.Text("Powerline Symbols:");
			ImGui.Text("Basic: \uE0A0 \uE0A1 \uE0A2 \uE0B0 \uE0B1 \uE0B2 \uE0B3");
			ImGui.Text("Extra: \uE0A3 \uE0B4 \uE0B5 \uE0B6 \uE0B7 \uE0B8 \uE0CA \uE0CC \uE0CD \uE0D0 \uE0D1 \uE0D4");

			ImGui.Separator();

			// Font Awesome icons
			ImGui.Text("Font Awesome Icons:");
			ImGui.Text("Files & Folders: \uF07B \uF07C \uF15B \uF15C \uF016 \uF017 \uF019 \uF01A \uF093 \uF095");
			ImGui.Text("Git & Version Control: \uF1D3 \uF1D2 \uF126 \uF127 \uF128 \uF129 \uF12A \uF12B");
			ImGui.Text("Media & UI: \uF04B \uF04C \uF04D \uF050 \uF051 \uF048 \uF049 \uF067 \uF068 \uF00C \uF00D");

			ImGui.Separator();

			// Material Design icons
			ImGui.Text("Material Design Icons:");
			ImGui.Text("Navigation: \uF52A \uF52B \uF544 \uF53F \uF540 \uF541 \uF542 \uF543");
			ImGui.Text("Actions: \uF8D5 \uF8D6 \uF8D7 \uF8D8 \uF8D9 \uF8DA \uF8DB \uF8DC");
			ImGui.Text("Content: \uF1C1 \uF1C2 \uF1C3 \uF1C4 \uF1C5 \uF1C6 \uF1C7 \uF1C8");

			ImGui.Separator();

			// Weather icons
			ImGui.Text("Weather Icons:");
			ImGui.Text("Basic Weather: \uE30D \uE30E \uE30F \uE310 \uE311 \uE312 \uE313 \uE314");
			ImGui.Text("Temperature: \uE315 \uE316 \uE317 \uE318 \uE319 \uE31A \uE31B \uE31C");
			ImGui.Text("Wind & Pressure: \uE31D \uE31E \uE31F \uE320 \uE321 \uE322 \uE323 \uE324");

			ImGui.Separator();

			// Devicons
			ImGui.Text("Developer Icons (Devicons):");
			ImGui.Text("Languages: \uE73C \uE73D \uE73E \uE73F \uE740 \uE741 \uE742 \uE743"); // Various programming languages
			ImGui.Text("Frameworks: \uE744 \uE745 \uE746 \uE747 \uE748 \uE749 \uE74A \uE74B");
			ImGui.Text("Tools: \uE74C \uE74D \uE74E \uE74F \uE750 \uE751 \uE752 \uE753");

			ImGui.Separator();

			// Octicons
			ImGui.Text("Octicons (GitHub Icons):");
			ImGui.Text("Version Control: \uF418 \uF419 \uF41A \uF41B \uF41C \uF41D \uF41E \uF41F");
			ImGui.Text("Issues & PRs: \uF420 \uF421 \uF422 \uF423 \uF424 \uF425 \uF426 \uF427");
			ImGui.Text("Social: \u2665 \u26A1 \uF428 \uF429 \uF42A \uF42B \uF42C \uF42D");

			ImGui.Separator();

			// Font Logos
			ImGui.Text("Brand Logos (Font Logos):");
			ImGui.Text("Tech Brands: \uF300 \uF301 \uF302 \uF303 \uF304 \uF305 \uF306 \uF307");
			ImGui.Text("More Logos: \uF308 \uF309 \uF30A \uF30B \uF30C \uF30D \uF30E \uF30F");

			ImGui.Separator();

			// Pomicons
			ImGui.Text("Pomicons:");
			ImGui.Text("Small Icons: \uE000 \uE001 \uE002 \uE003 \uE004 \uE005 \uE006 \uE007");
			ImGui.Text("More Icons: \uE008 \uE009 \uE00A \uE00B \uE00C \uE00D");

			ImGui.Separator();
			ImGui.TextWrapped("Note: These icons will only display correctly if you're using a Nerd Font. " +
							 "If you see question marks or boxes, switch to a Nerd Font like 'JetBrains Mono Nerd Font' or 'Fira Code Nerd Font'.");

			ImGui.Separator();
			ImGui.TextWrapped("Popular Nerd Fonts: JetBrains Mono Nerd Font, Fira Code Nerd Font, Hack Nerd Font, " +
							 "Source Code Pro Nerd Font, DejaVu Sans Mono Nerd Font, and many more at nerdfonts.com");

			ImGui.EndTabItem();
		}
	}

	private static void RenderPerformanceTab()
	{
		if (ImGui.BeginTabItem("Performance & Throttling"))
		{
			ImGui.TextWrapped("This section shows the current performance state and throttling behavior.");
			ImGui.Separator();

			ImGui.Text($"Window Focused: {ImGuiApp.IsFocused}");
			ImGui.Text($"Application Idle: {ImGuiApp.IsIdle}");
			ImGui.Text($"Window Visible: {ImGuiApp.IsVisible}");

			ImGui.Separator();
			ImGui.TextWrapped("Throttling helps save system resources when the window is unfocused or idle.");
			ImGui.Text("Default rates: Focused=30 FPS, Unfocused=5 FPS, Idle=10 FPS");
			ImGui.TextWrapped("Try unfocusing the window or leaving it idle for 5 seconds to see the effect.");

			ImGui.EndTabItem();
		}
	}

	private static void RenderModalAndPopups()
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

	private static void UpdateAnimations(float dt)
	{
		animationTime += dt;

		// Bouncing animation
		bounceOffset = MathF.Abs(MathF.Sin(animationTime * 3)) * 50;

		// Pulse animation
		pulseScale = 0.8f + (0.4f * MathF.Sin(animationTime * 4));
	}

	private static void UpdatePerformanceMetrics(float dt)
	{
		frameTimes.Enqueue(dt);
		frameTimeSum += dt;

		while (frameTimes.Count > 60) // Keep last 60 frames
		{
			frameTimeSum -= frameTimes.Dequeue();
		}
	}

	private static void RenderAboutWindow()
	{
		ImGui.Begin("About ImGuiApp Demo", ref showAbout);
		ImGui.Text("ImGuiApp Demo Application");
		ImGui.Separator();
		ImGui.Text("This demo showcases extensive ImGui.NET features including:");
		ImGui.BulletText("Basic and advanced widgets");
		ImGui.BulletText("Layout systems (columns, tables, tabs)");
		ImGui.BulletText("Custom graphics and drawing");
		ImGui.BulletText("Data visualization and plotting");
		ImGui.BulletText("Input handling and interaction");
		ImGui.BulletText("Animations and effects");
		ImGui.BulletText("File operations and utilities");
		ImGui.Separator();
		ImGui.Text("Built with:");
		ImGui.BulletText("Hexa.NET.ImGui");
		ImGui.BulletText("Silk.NET");
		ImGui.BulletText("ktsu.ImGuiApp Framework");
		ImGui.End();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is a demo application")]
	private static void UpdatePlotData(float dt)
	{
		plotRefreshTime += dt;
		if (plotRefreshTime >= 0.1f) // Update every 100ms
		{
			plotRefreshTime = 0;
			plotValues.Add((float)random.NextDouble());
			if (plotValues.Count > 100) // Keep last 100 values
			{
				plotValues.RemoveAt(0);
			}
		}
	}

	private static void OnAppMenu()
	{
		if (ImGui.BeginMenu("View"))
		{
			ImGui.MenuItem("ImGui Demo", string.Empty, ref showImGuiDemo);
			ImGui.MenuItem("Style Editor", string.Empty, ref showStyleEditor);
			ImGui.MenuItem("Metrics", string.Empty, ref showMetrics);
			ImGui.EndMenu();
		}

		if (ImGui.BeginMenu("Help"))
		{
			ImGui.MenuItem("About", string.Empty, ref showAbout);
			ImGui.EndMenu();
		}
	}
}

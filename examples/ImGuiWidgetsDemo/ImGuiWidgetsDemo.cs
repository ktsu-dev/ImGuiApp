// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgetsDemo;

using System.Collections.ObjectModel;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiPopups;
using ktsu.ImGuiStyler;
using ktsu.ImGuiWidgets;
using ktsu.StrongPaths;
using ktsu.StrongStrings;
using ktsu.TextFilter;

internal sealed record class StrongStringExample : StrongStringAbstract<StrongStringExample> { }

/// <summary>
/// Demo enum values.
/// </summary>
public enum EnumValues
{
	/// <summary>
	/// First enum value.
	/// </summary>
	Value1,
	/// <summary>
	/// Second enum value.
	/// </summary>
	ValueB,
	/// <summary>
	/// Third enum value.
	/// </summary>
	ValueIII,
}

internal static class ImGuiWidgetsDemo
{
	private static void Main()
	{
		ImGuiApp.Start(new()
		{
			Title = "ImGuiWidgets - Complete Library Demo",
			OnStart = OnStart,
			OnAppMenu = OnAppMenu,
			OnMoveOrResize = OnMoveOrResize,
			OnRender = OnRender,
		});
	}

	private static float value = 0.5f;
	private static float tab2Value = 0.5f;

	private static ImGuiWidgets.DividerContainer DividerContainer { get; } = new("DemoDividerContainer");
	private static ImGuiPopups.MessageOK MessageOK { get; } = new();
	private static ImGuiWidgets.TabPanel DemoTabPanel { get; } = new("DemoTabPanel", true, true);
	private static Dictionary<string, string> TabIds { get; } = [];
	private static int NextDynamicTabId { get; set; } = 1;

	private static List<string> GridStrings { get; } = [];
	private static int InitialGridItemCount { get; } = 32;
	private static int GridItemsToShow { get; set; } = InitialGridItemCount;
	private static float GridHeight { get; set; } = 500f;
	private static ImGuiWidgets.GridOrder GridOrder { get; set; } = ImGuiWidgets.GridOrder.RowMajor;
	private static ImGuiWidgets.IconAlignment GridIconAlignment { get; set; } = ImGuiWidgets.IconAlignment.Vertical;
	private static bool GridIconSizeBig { get; set; } = true;
	private static bool GridIconCenterWithinCell { get; set; } = true;
	private static bool GridFitToContents { get; set; }
	private static EnumValues selectedEnumValue = EnumValues.Value1;
	private static string selectedStringValue = "Hello";
	private static readonly Collection<string> possibleStringValues = ["Hello", "World", "Goodbye"];
	private static StrongStringExample selectedStrongString = "Strong Hello".As<StrongStringExample>();
	private static readonly Collection<StrongStringExample> possibleStrongStringValues = ["Strong Hello".As<StrongStringExample>(),
		 "Strong World".As<StrongStringExample>(), "Strong Goodbye".As<StrongStringExample>()];

	// Static fields for SearchBox filter persistence
	private static string BasicSearchTerm = string.Empty;
	private static TextFilterType BasicFilterType = TextFilterType.Glob;
	private static TextFilterMatchOptions BasicMatchOptions = TextFilterMatchOptions.ByWholeString;

	private static string FilteredSearchTerm = string.Empty;
	private static TextFilterType FilteredFilterType = TextFilterType.Glob;
	private static TextFilterMatchOptions FilteredMatchOptions = TextFilterMatchOptions.ByWholeString;

	private static string RankedSearchTerm = string.Empty;

	private static string GlobSearchTerm = string.Empty;
	private static TextFilterType GlobFilterType = TextFilterType.Glob;
	private static TextFilterMatchOptions GlobMatchOptions = TextFilterMatchOptions.ByWholeString;

	private static string RegexSearchTerm = string.Empty;
	private static TextFilterType RegexFilterType = TextFilterType.Regex;
	private static TextFilterMatchOptions RegexMatchOptions = TextFilterMatchOptions.ByWholeString;

#pragma warning disable CA5394 //Do not use insecure randomness
	private static void OnStart()
	{
		// Create main layout with dedicated demo sections
		DividerContainer.Add(new("Widget Demos", 0.6f, ShowWidgetDemos));
		DividerContainer.Add(new("Advanced Demos", 0.4f, ShowAdvancedDemos));

		// Initialize TabPanel demo
		TabIds["tab1"] = DemoTabPanel.AddTab("tab1", "Tab 1", ShowTab1Content);
		TabIds["tab2"] = DemoTabPanel.AddTab("tab2", "Tab 2", ShowTab2Content);
		TabIds["tab3"] = DemoTabPanel.AddTab("tab3", "Tab 3", ShowTab3Content);

		// Generate test data for grid demos
		for (int i = 0; i < InitialGridItemCount; i++)
		{
			string randomString = $"{i}:";
			int randomAmount = new Random().Next(2, 32);
			for (int j = 0; j < randomAmount; j++)
			{
				randomString += (char)new Random().Next(32, 127);
			}

			GridStrings.Add(randomString);
		}
	}
#pragma warning restore CA5394 //Do not use insecure randomness

	private static void OnRender(float dt) => DividerContainer.Tick(dt);

	private static void OnAppMenu()
	{
		// Method intentionally left empty.
	}

	private static void OnMoveOrResize()
	{
		// Method intentionally left empty.
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
	private static void ShowWidgetDemos(float size)
	{
		ImGui.TextUnformatted("ImGuiWidgets Library - Comprehensive Demo");
		ImGui.Separator();

		ShowKnobDemo();
		ShowColorIndicatorDemo();
		ShowComboDemo();
		ShowTextDemo();
		ShowScopedWidgetsDemo();
		ShowTreeDemo();
	}

	private static void ShowAdvancedDemos(float size)
	{
		AbsoluteFilePath ktsuIconPath = (AbsoluteDirectoryPath)Environment.CurrentDirectory / (FileName)"ktsu.png";
		ImGuiAppTextureInfo ktsuTexture = ImGuiApp.GetOrLoadTexture(ktsuIconPath);

		ImGui.TextUnformatted("Advanced Widget Demos");
		ImGui.Separator();

		ShowImageAndIconDemo(ktsuTexture);
		ShowTabPanelDemo();
		ShowSearchBoxDemo();
		ShowGridDemo(ktsuTexture);
		ShowDividerDemo();

		MessageOK.ShowIfOpen();
	}

	private static void ShowTabPanelDemo()
	{
		if (ImGui.CollapsingHeader("TabPanel"))
		{
			ImGui.TextUnformatted("Tabbed interface with dirty state tracking:");
			ImGui.Separator();

			// Tab Panel controls
			ImGui.TextUnformatted("Tab Management:");
			if (ImGui.Button("Mark Active Tab Dirty"))
			{
				DemoTabPanel.MarkActiveTabDirty();
			}
			ImGui.SameLine();
			if (ImGui.Button("Mark Active Tab Clean"))
			{
				DemoTabPanel.MarkActiveTabClean();
			}
			ImGui.SameLine();
			if (ImGui.Button("Add New Tab"))
			{
				int tabIndex = NextDynamicTabId++;
				string tabKey = $"dynamic{tabIndex}";
				string tabId = $"dyntab_{tabIndex}";
				TabIds[tabKey] = DemoTabPanel.AddTab(tabId, $"Extra Tab {tabIndex}", () => ShowDynamicTabContent(tabIndex));
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Features demonstrated:");
			ImGui.BulletText("Closeable tabs (X button)");
			ImGui.BulletText("Dirty state indicators (*)");
			ImGui.BulletText("Dynamic tab addition");
			ImGui.BulletText("Per-tab state management");

			ImGui.Separator();

			// Display tab panel
			DemoTabPanel.Draw();
		}
	}

	private static void ShowSearchBoxDemo()
	{
		if (ImGui.CollapsingHeader("SearchBox"))
		{
			ImGui.TextUnformatted("Powerful search functionality with multiple filter types:");
			ImGui.Separator();

			ImGui.TextUnformatted("Basic SearchBox (UI only):");
			ImGuiWidgets.SearchBox("##BasicSearch", ref BasicSearchTerm, ref BasicFilterType, ref BasicMatchOptions);
			ImGui.TextUnformatted($"Search term: '{BasicSearchTerm}' | Type: {BasicFilterType} | Match: {BasicMatchOptions}");

			ImGui.Separator();
			ImGui.TextUnformatted("SearchBox with Filtering:");

			// Using the SearchBox that returns filtered results
			List<string> filteredResults = [.. ImGuiWidgets.SearchBox(
				"##FilteredSearch",
				ref FilteredSearchTerm,
				GridStrings,
				s => s,
				ref FilteredFilterType,
				ref FilteredMatchOptions)];

			if (!string.IsNullOrEmpty(FilteredSearchTerm))
			{
				ImGui.TextUnformatted($"Results: {filteredResults.Count} matches for '{FilteredSearchTerm}'");
				ImGui.BeginChild("FilteredResults", new Vector2(0, 100), ImGuiChildFlags.Borders);
				foreach (string item in filteredResults.Take(20))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				if (filteredResults.Count > 20)
				{
					ImGui.TextUnformatted($"... and {filteredResults.Count - 20} more");
				}
				ImGui.EndChild();
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Ranked SearchBox (Fuzzy Matching):");

			List<string> rankedResults = [.. ImGuiWidgets.SearchBoxRanked("##RankedSearch",
				ref RankedSearchTerm,
				GridStrings,
				s => s)];

			if (!string.IsNullOrEmpty(RankedSearchTerm))
			{
				ImGui.TextUnformatted($"Fuzzy Results: {rankedResults.Count} matches for '{RankedSearchTerm}'");
				ImGui.BeginChild("RankedResults", new Vector2(0, 100), ImGuiChildFlags.Borders);
				foreach (string item in rankedResults.Take(20))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				if (rankedResults.Count > 20)
				{
					ImGui.TextUnformatted($"... and {rankedResults.Count - 20} more");
				}
				ImGui.EndChild();
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Filter Type Comparison:");

			ImGui.Columns(2, "SearchComparison");

			ImGui.TextUnformatted("Glob Pattern (*,?):");
			List<string> globResults = [.. ImGuiWidgets.SearchBox("##GlobSearch",
				ref GlobSearchTerm,
				GridStrings,
				s => s,
				ref GlobFilterType,
				ref GlobMatchOptions)];

			if (!string.IsNullOrEmpty(GlobSearchTerm))
			{
				ImGui.TextUnformatted($"{globResults.Count} matches");
				ImGui.BeginChild("GlobResults", new Vector2(0, 80), ImGuiChildFlags.Borders);
				foreach (string item in globResults.Take(10))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				ImGui.EndChild();
			}
			else
			{
				ImGui.TextUnformatted("Try: *1*, ?:*, [0-9]*");
			}

			ImGui.NextColumn();

			ImGui.TextUnformatted("Regex Pattern:");
			List<string> regexResults = [.. ImGuiWidgets.SearchBox("##RegexSearch",
				ref RegexSearchTerm,
				GridStrings,
				s => s,
				ref RegexFilterType,
				ref RegexMatchOptions)];

			if (!string.IsNullOrEmpty(RegexSearchTerm))
			{
				ImGui.TextUnformatted($"{regexResults.Count} matches");
				ImGui.BeginChild("RegexResults", new Vector2(0, 80), ImGuiChildFlags.Borders);
				foreach (string item in regexResults.Take(10))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				ImGui.EndChild();
			}
			else
			{
				ImGui.TextUnformatted("Try: ^\\d+, [A-Z]+, .*[aeiou].*");
			}

			ImGui.Columns(1);
		}
	}

	private static void ShowGridDemo(ImGuiAppTextureInfo ktsuTexture)
	{
		if (ImGui.CollapsingHeader("Grid Layout"))
		{
			ImGui.TextUnformatted("Flexible grid layouts with automatic sizing:");
			ImGui.Separator();

			// Grid settings - inline controls
			ImGui.TextUnformatted("Grid Configuration:");

			bool showGridDebug = ImGuiWidgets.EnableGridDebugDraw;
			if (ImGui.Checkbox("Show Grid Debug Draw", ref showGridDebug))
			{
				ImGuiWidgets.EnableGridDebugDraw = showGridDebug;
			}
			ImGui.SameLine();

			bool showIconDebug = ImGuiWidgets.EnableIconDebugDraw;
			if (ImGui.Checkbox("Show Icon Debug Draw", ref showIconDebug))
			{
				ImGuiWidgets.EnableIconDebugDraw = showIconDebug;
			}

			ImGui.Columns(3, "GridSettings");

			bool gridIconSizeBig = GridIconSizeBig;
			if (ImGui.Checkbox("Big Icons", ref gridIconSizeBig))
			{
				GridIconSizeBig = gridIconSizeBig;
			}

			bool gridIconCenterWithinCell = GridIconCenterWithinCell;
			if (ImGui.Checkbox("Center in Cell", ref gridIconCenterWithinCell))
			{
				GridIconCenterWithinCell = gridIconCenterWithinCell;
			}

			bool gridFitToContents = GridFitToContents;
			if (ImGui.Checkbox("Fit to Contents", ref gridFitToContents))
			{
				GridFitToContents = gridFitToContents;
			}

			ImGui.NextColumn();

			int gridItemsToShow = GridItemsToShow;
			if (ImGui.SliderInt("Items", ref gridItemsToShow, 0, GridStrings.Count))
			{
				GridItemsToShow = gridItemsToShow;
			}

			ImGuiWidgets.GridOrder gridOrder = GridOrder;
			if (ImGuiWidgets.Combo("Order", ref gridOrder))
			{
				GridOrder = gridOrder;
			}

			ImGui.NextColumn();

			ImGuiWidgets.IconAlignment gridIconAlignment = GridIconAlignment;
			if (ImGuiWidgets.Combo("Icon Layout", ref gridIconAlignment))
			{
				GridIconAlignment = gridIconAlignment;
			}

			float gridHeight = GridHeight;
			if (ImGui.SliderFloat("Height", ref gridHeight, 100f, 800f))
			{
				GridHeight = gridHeight;
			}

			ImGui.Columns(1);
			ImGui.Separator();

			// Grid display
			float iconSizePx = ImGuiApp.EmsToPx(2.5f);
			float bigIconSizePx = iconSizePx * 2;
			float gridIconSize = GridIconSizeBig ? bigIconSizePx : iconSizePx;

			Vector2 MeasureGridSize(string item) => ImGuiWidgets.CalcIconSize(item, gridIconSize, GridIconAlignment);
			void DrawGridCell(string item, Vector2 cellSize, Vector2 itemSize)
			{
				if (GridIconCenterWithinCell)
				{
					using (new Alignment.CenterWithin(itemSize, cellSize))
					{
						ImGuiWidgets.Icon(item, ktsuTexture.TextureId, gridIconSize, GridIconAlignment);
					}
				}
				else
				{
					ImGuiWidgets.Icon(item, ktsuTexture.TextureId, gridIconSize, GridIconAlignment);
				}
			}

			ImGuiWidgets.GridOptions gridOptions = new()
			{
				GridSize = new Vector2(ImGui.GetContentRegionAvail().X, GridHeight),
				FitToContents = GridFitToContents,
			};

			ImGui.TextUnformatted($"Showing {GridItemsToShow} items in {GridOrder} order:");

			switch (GridOrder)
			{
				case ImGuiWidgets.GridOrder.RowMajor:
					ImGuiWidgets.RowMajorGrid("demoRowMajorGrid", GridStrings.Take(GridItemsToShow), MeasureGridSize, DrawGridCell, gridOptions);
					break;

				case ImGuiWidgets.GridOrder.ColumnMajor:
					ImGuiWidgets.ColumnMajorGrid("demoColumnMajorGrid", GridStrings.Take(GridItemsToShow), MeasureGridSize, DrawGridCell, gridOptions);
					break;

				default:
					throw new NotImplementedException();
			}
		}
	}

	// Individual widget demo methods
	private static void ShowKnobDemo()
	{
		if (ImGui.CollapsingHeader("Knobs"))
		{
			ImGui.TextUnformatted("All knob variants with interactive controls:");
			ImGui.Separator();

			// Show all knob variants
			ImGui.Columns(3, "KnobColumns");

			ImGuiWidgets.Knob("Wiper", ref value, 0, 1, 0, null, ImGuiKnobVariant.Wiper);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Wiper Only", ref value, 0, 1, 0, null, ImGuiKnobVariant.WiperOnly);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Wiper Dot", ref value, 0, 1, 0, null, ImGuiKnobVariant.WiperDot);
			ImGui.NextColumn();

			ImGuiWidgets.Knob("Tick", ref value, 0, 1, 0, null, ImGuiKnobVariant.Tick);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Stepped", ref value, 0, 1, 0, null, ImGuiKnobVariant.Stepped);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Space", ref value, 0, 1, 0, null, ImGuiKnobVariant.Space);

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted($"Current Value: {value:F3}");

			if (ImGui.Button("Reset to 0.5"))
			{
				value = 0.5f;
			}
		}
	}

	private static void ShowColorIndicatorDemo()
	{
		if (ImGui.CollapsingHeader("Color Indicators"))
		{
			ImGui.TextUnformatted("Color indicators show enabled/disabled states:");
			ImGui.Separator();

			ImGui.TextUnformatted("Status Lights:");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("System OK");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Warning, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Warning");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Error, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Error");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Info, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Info");

			ImGui.Separator();
			ImGui.TextUnformatted("Enabled vs Disabled:");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Enabled");
			ImGuiWidgets.ColorIndicator(Color.Palette.Semantic.Success, false);
			ImGui.SameLine();
			ImGui.TextUnformatted("Disabled");
		}
	}

	private static void ShowComboDemo()
	{
		if (ImGui.CollapsingHeader("Combo Boxes"))
		{
			ImGui.TextUnformatted("Type-safe combo boxes for enums and collections:");
			ImGui.Separator();

			ImGuiWidgets.Combo("Enum Combo", ref selectedEnumValue);
			ImGui.TextUnformatted($"Selected: {selectedEnumValue}");

			ImGui.Separator();
			ImGuiWidgets.Combo("String Combo", ref selectedStringValue, possibleStringValues);
			ImGui.TextUnformatted($"Selected: {selectedStringValue}");

			ImGui.Separator();
			ImGuiWidgets.Combo("Strong String Combo", ref selectedStrongString, possibleStrongStringValues);
			ImGui.TextUnformatted($"Selected: {selectedStrongString}");
		}
	}

	private static void ShowTextDemo()
	{
		if (ImGui.CollapsingHeader("Text Utilities"))
		{
			ImGui.TextUnformatted("Enhanced text rendering with alignment and clipping:");
			ImGui.Separator();

			// Regular text
			ImGuiWidgets.Text("Regular text");

			ImGui.Separator();

			// Centered text
			ImGui.TextUnformatted("Centered text in available space:");
			ImGuiWidgets.TextCentered("This text is centered!");

			ImGui.Separator();

			// Text centered within bounds
			ImGui.TextUnformatted("Text centered within 200px container:");
			Vector2 containerSize = new(200, 50);
			ImGui.GetWindowDrawList().AddRect(
				ImGui.GetCursorScreenPos(),
				ImGui.GetCursorScreenPos() + containerSize,
				ImGui.GetColorU32(ImGuiCol.Border)
			);
			ImGuiWidgets.TextCenteredWithin("Centered within bounds", containerSize);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + containerSize.Y);

			ImGui.Separator();

			// Clipped text
			ImGui.TextUnformatted("Text clipping demo (150px width):");
			Vector2 clipSize = new(150, 30);
			ImGui.GetWindowDrawList().AddRect(
				ImGui.GetCursorScreenPos(),
				ImGui.GetCursorScreenPos() + clipSize,
				ImGui.GetColorU32(ImGuiCol.Border)
			);
			// Demonstrate text clipping by manually truncating long text
			string longText = "This is a very long text that will be clipped with ellipsis";
			float textWidth = ImGui.CalcTextSize(longText).X;
			string displayText = longText;
			if (textWidth > clipSize.X)
			{
				// Manually clip the text for demo purposes
				while (ImGui.CalcTextSize(displayText + "...").X > clipSize.X && displayText.Length > 0)
				{
					displayText = displayText[..^1];
				}
				displayText += "...";
			}
			ImGuiWidgets.TextCenteredWithin(displayText, clipSize);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + clipSize.Y);
		}
	}

	private static void ShowScopedWidgetsDemo()
	{
		if (ImGui.CollapsingHeader("Scoped Utilities"))
		{
			ImGui.TextUnformatted("Scoped helpers for ImGui state management:");
			ImGui.Separator();

			// ScopedDisable demo
			ImGui.TextUnformatted("ScopedDisable - disables widgets within scope:");
			using (new ScopedDisable(true))
			{
				bool dummyBool = true;
				int dummyInt = 0;
				string[] items = ["Item 1", "Item 2", "Item 3"];

				ImGui.Checkbox("Disabled Checkbox", ref dummyBool);
				ImGui.Combo("Disabled Combo", ref dummyInt, items, items.Length);
				ImGui.Button("Disabled Button");
			}

			ImGui.Separator();

			// ScopedId demo
			ImGui.TextUnformatted("ScopedId - manages ImGui ID stack automatically:");
			for (int i = 0; i < 3; i++)
			{
				using (new ImGuiWidgets.ScopedId(i))
				{
					bool state = false;
					ImGui.Checkbox("Same Label", ref state);
				}
			}
			ImGui.TextUnformatted("↑ Three checkboxes with same label using ScopedId");
		}
	}

	private static void ShowTreeDemo()
	{
		if (ImGui.CollapsingHeader("Tree View"))
		{
			ImGui.TextUnformatted("Hierarchical tree structure with automatic cleanup:");
			ImGui.Separator();

			using ImGuiWidgets.Tree tree = new();
			for (int i = 0; i < 3; i++)
			{
				using (tree.Child)
				{
					ImGui.Button($"Parent Node {i + 1}");

					using ImGuiWidgets.Tree subtree = new();
					for (int j = 0; j < 2; j++)
					{
						using (subtree.Child)
						{
							ImGui.Button($"Child {j + 1}");

							if (i == 0 && j == 0) // Show deeper nesting for first item
							{
								using ImGuiWidgets.Tree deepTree = new();
								using (deepTree.Child)
								{
									ImGui.Button("Grandchild");
								}
							}
						}
					}
				}
			}
		}
	}

	private static void ShowImageAndIconDemo(ImGuiAppTextureInfo ktsuTexture)
	{
		if (ImGui.CollapsingHeader("Images & Icons"))
		{
			ImGui.TextUnformatted("Interactive images and icons with events:");
			ImGui.Separator();

			// Image demo with color tinting
			ImGui.TextUnformatted("Clickable Image (with alpha-preserved tinting):");
			Vector4 tintColor = new(1.0f, 0.8f, 0.8f, 1.0f); // Light red tint
			if (ImGuiWidgets.Image(ktsuTexture.TextureId, new Vector2(64, 64), tintColor))
			{
				MessageOK.Open("Image Clicked", "You clicked the tinted image!");
			}

			ImGui.SameLine();
			if (ImGuiWidgets.Image(ktsuTexture.TextureId, new Vector2(64, 64))) // No tint
			{
				MessageOK.Open("Image Clicked", "You clicked the normal image!");
			}

			ImGui.Separator();

			// Icon demos
			ImGui.TextUnformatted("Interactive Icons:");

			float iconSize = ImGuiApp.EmsToPx(4.0f);

			ImGuiWidgets.Icon("Click Me", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnClick = () => MessageOK.Open("Click", "Single click detected!")
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Double Click", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnDoubleClick = () => MessageOK.Open("Double Click", "Double click detected!")
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Right Click", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					OnContextMenu = () =>
					{
						if (ImGui.MenuItem("Context Item 1"))
						{
							MessageOK.Open("Menu", "Context Item 1 selected");
						}

						if (ImGui.MenuItem("Context Item 2"))
						{
							MessageOK.Open("Menu", "Context Item 2 selected");
						}

						ImGui.Separator();
						if (ImGui.MenuItem("Context Item 3"))
						{
							MessageOK.Open("Menu", "Context Item 3 selected");
						}
					},
				});

			ImGui.SameLine();
			ImGuiWidgets.Icon("Hover Me", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Vertical,
				new ImGuiWidgets.IconOptions()
				{
					Tooltip = "This is a tooltip that appears when you hover over the icon!"
				});

			ImGui.Separator();

			ImGui.TextUnformatted("Horizontal Layout Icons:");
			ImGuiWidgets.Icon("Horizontal 1", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Horizontal);
			ImGuiWidgets.Icon("Horizontal 2", ktsuTexture.TextureId, iconSize, ImGuiWidgets.IconAlignment.Horizontal);
		}
	}

	private static void ShowDividerDemo()
	{
		if (ImGui.CollapsingHeader("Divider Container"))
		{
			ImGui.TextUnformatted("This entire demo uses a DividerContainer!");
			ImGui.TextUnformatted("The resizable divider between 'Widget Demos' and 'Advanced Demos'");
			ImGui.TextUnformatted("is created using ImGuiWidgets.DividerContainer.");

			ImGui.Separator();
			ImGui.TextUnformatted("DividerContainer features:");
			ImGui.BulletText("Resizable panes with drag handle");
			ImGui.BulletText("Persistent sizing ratios");
			ImGui.BulletText("Automatic content management");
			ImGui.BulletText("Nested dividers support");
		}
	}

	// Tab content methods
	private static void ShowTab1Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 1");

		if (ImGui.Button("Edit Content"))
		{
			DemoTabPanel.MarkTabDirty(TabIds["tab1"]);
		}

		if (ImGui.Button("Save Content"))
		{
			DemoTabPanel.MarkTabClean(TabIds["tab1"]);
		}

		ImGui.TextUnformatted("Dirty State: " + (DemoTabPanel.IsTabDirty(TabIds["tab1"]) ? "Modified" : "Unchanged"));
	}

	private static void ShowTab2Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 2");

		if (ImGui.SliderFloat("Value", ref tab2Value, 0.0f, 1.0f))
		{
			// Mark tab as dirty when slider value changes
			DemoTabPanel.MarkTabDirty(TabIds["tab2"]);
		}

		if (ImGui.Button("Reset"))
		{
			tab2Value = 0.5f;
			DemoTabPanel.MarkTabClean(TabIds["tab2"]);
		}
	}

	private static void ShowTab3Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 3");
		ImGui.TextUnformatted("Try clicking 'Mark Active Tab Dirty' button above");
		ImGui.TextUnformatted("to see the dirty indicator (*) appear next to the tab name.");

		if (ImGui.Button("Toggle Dirty State"))
		{
			if (DemoTabPanel.IsTabDirty(TabIds["tab3"]))
			{
				DemoTabPanel.MarkTabClean(TabIds["tab3"]);
			}
			else
			{
				DemoTabPanel.MarkTabDirty(TabIds["tab3"]);
			}
		}
	}

	private static void ShowDynamicTabContent(int tabIndex)
	{
		string tabKey = $"dynamic{tabIndex}";
		ImGui.TextUnformatted($"This is a dynamically added tab ({tabIndex})");
		ImGui.TextUnformatted("The (*) indicator shows when content has been modified.");

		if (ImGui.Button("Toggle Dirty State"))
		{
			if (DemoTabPanel.IsTabDirty(TabIds[tabKey]))
			{
				DemoTabPanel.MarkTabClean(TabIds[tabKey]);
			}
			else
			{
				DemoTabPanel.MarkTabDirty(TabIds[tabKey]);
			}
		}
	}
}

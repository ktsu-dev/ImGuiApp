// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Color;
using ktsu.ImGui.Popups;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;
using ktsu.TextFilter;

/// <summary>
/// Demo strong string example.
/// </summary>
public sealed record class StrongStringExample : SemanticString<StrongStringExample> { }

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
	/// <summary>
	/// Builds the configuration the demo runs on. Extracted from <c>Main</c> so a UI test drives the
	/// real configuration rather than a parallel one written for testing.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGuiWidgets - Complete Library Demo",
		OnStart = OnStart,
		OnConfigureFonts = OnConfigureFonts,
		OnAppMenu = OnAppMenu,
		OnMoveOrResize = OnMoveOrResize,
		OnRender = OnRender,
	};

	private static void Main() => ImGuiApp.Start(BuildConfig());

	/// <summary>
	/// Returns the demo state a test can disturb to its starting value. The demo keeps its state in
	/// statics, which outlive a harness, so a test that ran before this one would otherwise decide
	/// what this one starts from.
	/// </summary>
	internal static void ResetState()
	{
		value = 0.5f;
		tab2Value = 0.5f;
		switchWifi = true;
		switchBluetooth = false;
		segmentSelected = 0;
		chipGroupSelected = 1;
		stepperQuantity = 3;
		rangeLower = 25.0f;
		rangeUpper = 75.0f;
		progressValue = 0.65f;
		progressAnimating = false;
		progressAnimationSpeed = 0.3f;
		countdownTime = CountdownTotal;
		countdownRunning = false;
		countupTime = 0.0f;
		countupRunning = false;
		ratingValue = 3.0f;
		halfRatingValue = 3.5f;
		notificationCount = 5;
		carouselPage = 0;
		pinValue = string.Empty;
		otpValue = string.Empty;
		skeletonLoading = true;
		selectedEnumValue = EnumValues.Value1;
		selectedStringValue = "Hello";
		selectedStrongString = "Strong Hello".As<StrongStringExample>();
		BasicSearchTerm = string.Empty;
		FilteredSearchTerm = string.Empty;
		RankedSearchTerm = string.Empty;
		GlobSearchTerm = string.Empty;
		RegexSearchTerm = string.Empty;
		GridItemsToShow = InitialGridItemCount;
		GridHeight = 500f;
		GridOrder = ImGuiWidgets.GridOrder.RowMajor;
		GridIconAlignment = ImGuiWidgets.IconAlignment.Vertical;
		GridIconSizeBig = true;
		GridFitToContents = false;
	}

	/// <summary>Gets the value of the Wi-Fi switch in the mobile form-control section.</summary>
	internal static bool SwitchWifi => switchWifi;

	/// <summary>Gets the quantity held by the stepper in the mobile form-control section.</summary>
	internal static int StepperQuantity => stepperQuantity;

	/// <summary>Gets the rating currently shown by the decorator section.</summary>
	internal static float RatingValue => ratingValue;

	/// <summary>Gets the value driven by the knob and radial progress sections.</summary>
	internal static float ProgressValue => progressValue;

	/// <summary>Gets the number of grid items the grid demo is currently showing.</summary>
	internal static int GridItemCount => GridItemsToShow;

	/// <summary>Gets the tab panel the advanced section demonstrates.</summary>
	internal static ImGuiWidgets.TabPanel TabPanel => DemoTabPanel;

	private static float value = 0.5f;
	private static float tab2Value = 0.5f;

	// Mobile form-control demo state
	private static bool switchWifi = true;
	private static bool switchBluetooth;
	private static int segmentSelected;
	private static int chipGroupSelected = 1;
	private static readonly List<string> chipTags = ["All", "Unread", "Flagged", "Archived", "Drafts", "Sent", "Spam"];
	private static int stepperQuantity = 3;
	private static float rangeLower = 25.0f;
	private static float rangeUpper = 75.0f;

	// Radial Progress Bar demo state
	private static float progressValue = 0.65f;
	private static bool progressAnimating;
	private static float progressAnimationSpeed = 0.3f;
	private static float countdownTime = 300.0f; // 5 minutes
	private const float CountdownTotal = 300.0f;
	private static bool countdownRunning;
	private static float countupTime;
	private const float CountupTotal = 180.0f; // 3 minutes
	private static bool countupRunning;

	// Mobile decorator widget demo state
	private static float ratingValue = 3.0f;
	private static float halfRatingValue = 3.5f;
	private static int notificationCount = 5;
	private static int carouselPage;

	// Mobile container / loader demo state
	private static string pinValue = string.Empty;
	private static string otpValue = string.Empty;
	private static bool skeletonLoading = true;

	// The DividerContainer is no longer this demo's top-level layout -- the main window is a tab bar
	// now -- so the widget keeps a live instance of its own, drawn inside its demo section.
	private static ImGuiWidgets.DividerContainer DividerDemoContainer { get; } = new("DividerDemoContainer");

	// DividerContainer.Tick needs a delta time, and the divider demo runs from a collapsing header
	// that has none threaded through to it, so OnRender stashes the frame's delta here.
	private static float deltaTime;

	private static ImGuiPopups.MessageOK MessageOK { get; } = new();
	private static ImGuiWidgets.TabPanel DemoTabPanel { get; } = new("DemoTabPanel", true, true);
	private static ImGuiWidgets.ImageCanvasState ImageCanvasDemoState { get; } = new();
	private static Dictionary<string, string> TabIds { get; } = [];
	private static int NextDynamicTabId { get; set; } = 1;

	private static List<string> GridStrings { get; } = [];
	private static int InitialGridItemCount { get; } = 32;
	private static int GridItemsToShow { get; set; } = InitialGridItemCount;
	private static float GridHeight { get; set; } = 500f;
	private static ImGuiWidgets.GridOrder GridOrder { get; set; } = ImGuiWidgets.GridOrder.RowMajor;
	private static ImGuiWidgets.IconAlignment GridIconAlignment { get; set; } = ImGuiWidgets.IconAlignment.Vertical;
	private static bool GridIconSizeBig { get; set; } = true;
	private static bool GridFitToContents { get; set; }
	private static EnumValues selectedEnumValue = EnumValues.Value1;
	private static string selectedStringValue = "Hello";
	private static readonly Collection<string> possibleStringValues = ["Hello", "World", "Goodbye"];
	private static StrongStringExample selectedStrongString = "Strong Hello".As<StrongStringExample>();
	private static readonly Collection<StrongStringExample> possibleStrongStringValues = ["Strong Hello".As<StrongStringExample>(),
		 "Strong World".As<StrongStringExample>(), "Strong Goodbye".As<StrongStringExample>()];

	// Static fields for SearchBox filter persistence
	private static string BasicSearchTerm = string.Empty;
	private static SearchBoxOptions BasicSearchOptions = new(Label: "##BasicSearch");

	private static string FilteredSearchTerm = string.Empty;
	private static SearchBoxOptions FilteredSearchOptions = new(Label: "##FilteredSearch");

	private static string RankedSearchTerm = string.Empty;
	private static SearchBoxRankedOptions RankedSearchOptions = new(Label: "##RankedSearch");

	private static string GlobSearchTerm = string.Empty;
	private static SearchBoxOptions GlobSearchOptions = new(Label: "##GlobSearch", FilterType: TextFilterType.Glob);

	private static string RegexSearchTerm = string.Empty;
	private static SearchBoxOptions RegexSearchOptions = new(Label: "##RegexSearch", FilterType: TextFilterType.Regex);

	// The Hexa-backed DatePicker, FileTreeView and IconTreeNode draw Material Icons glyphs, so this
	// demo needs the same font registration as ImGuiAppDemo -- otherwise the widgets the "Hexa
	// Widgets" pane exists to evaluate render as placeholder boxes. The font is not checked into the
	// repo; drop MaterialIcons-Regular.ttf next to the binary and this picks it up. OnConfigureFonts
	// is the only hook that works: it runs after the configured fonts are added but before the atlas
	// is built, whereas OnStart runs after the atlas has already been built.
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "FontHelper.GetMaterialIconRanges() returns a uint* that ImGui owns for the lifetime of the atlas; the pointer is passed straight through and never dereferenced or retained here.")]
	private static void OnConfigureFonts()
	{
		AbsoluteFilePath materialIconsPath =
			AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "MaterialIcons-Regular.ttf".As<FileName>();

		if (File.Exists(materialIconsPath.ToString()))
		{
			ImGuiIOPtr io = ImGui.GetIO();
			byte[] fontData = File.ReadAllBytes(materialIconsPath.ToString());
			unsafe
			{
				_ = FontHelper.AddCustomFont(io, fontData, 16f, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
			}
		}
	}

#pragma warning disable CA5394 //Do not use insecure randomness - Random is used only for generating visual demo data; no security or cryptographic use.
	[SuppressMessage("Security Hotspot", "S2245:Make sure that using this pseudorandom number generator is safe here", Justification = "Random is used only for generating visual demo data; no security or cryptographic use.")]
	private static void OnStart()
	{
		// Zones for the live DividerContainer demo (see ShowDividerDemo); the demo's own layout is
		// the tab bar in OnRender.
		DividerDemoContainer.Add(new("DividerDemoLeft", 0.5f, _ =>
			ImGui.TextWrapped("Drag the handle between these zones to resize them, or double-click it to reset.")));
		DividerDemoContainer.Add(new("DividerDemoRight", 0.5f, _ =>
			ImGui.TextWrapped("Each zone is its own child window, so it scrolls and clips independently.")));

		// Initialize TabPanel demo
		TabIds["tab1"] = DemoTabPanel.AddTab("tab1", "Tab 1", ShowTab1Content);
		TabIds["tab2"] = DemoTabPanel.AddTab("tab2", "Tab 2", ShowTab2Content);
		TabIds["tab3"] = DemoTabPanel.AddTab("tab3", "Tab 3", ShowTab3Content);

		// Generate test data for grid demos
		Random random = new();
		for (int i = 0; i < InitialGridItemCount; i++)
		{
			StringBuilder randomStringBuilder = new();
			randomStringBuilder.Append(i);
			randomStringBuilder.Append(':');

			int lineCount = 1 + (i % 5);
			for (int j = 0; j < lineCount; j++)
			{
				int randomAmount = random.Next(2, 32);
				for (int k = 0; k < randomAmount; k++)
				{
					randomStringBuilder.Append((char)random.Next(32, 127));
				}

				if (j != lineCount - 1)
				{
					randomStringBuilder.Append('\n');
				}
			}

			GridStrings.Add(randomStringBuilder.ToString());
		}
	}
#pragma warning restore CA5394 //Do not use insecure randomness

	private static void OnRender(float dt)
	{
		deltaTime = dt;

		if (ImGui.BeginTabBar("DemoTabs"))
		{
			ShowTab("Widget Demos", ShowWidgetDemos);
			ShowTab("Advanced Demos", ShowAdvancedDemos);
			ShowTab("Hexa vs ktsu", HexaWidgetsDemo.ShowComparison);
			ShowTab("Net New", HexaWidgetsDemo.ShowNetNew);
			ShowTab("Dialogs", HexaWidgetsDemo.ShowDialogComparison);

			ImGui.EndTabBar();
		}

		// Hexa's dialogs are stateful: Show() registers them with a static manager and they are
		// only drawn by this pump. Without it no dialog appears. It sits outside the tab bar because
		// Hexa draws its dialogs as their own windows, so it does not matter which tab is active --
		// unlike ktsu's popups, which are pumped inline by the tab that opens them.
		ImGuiWidgets.DrawDeferred();
	}

	/// <summary>
	/// Draws one top-level tab. The content goes in a child window so that it scrolls under a tab
	/// bar that stays put, rather than scrolling the tab bar off the top of the window with it.
	/// </summary>
	/// <param name="label">The tab label.</param>
	/// <param name="content">The tab's contents, drawn only while the tab is active.</param>
	private static void ShowTab(string label, Action content)
	{
		if (DemoProbe.TabItem(label))
		{
			// BeginChild must be paired with EndChild whatever it returns, so the result is ignored.
			ImGui.BeginChild($"{label}Content", Vector2.Zero, ImGuiChildFlags.None);
			content();
			ImGui.EndChild();
			ImGui.EndTabItem();
		}
	}

	private static void OnAppMenu()
	{
		// Method intentionally left empty.
	}

	private static void OnMoveOrResize()
	{
		// Method intentionally left empty.
	}

	private static void ShowWidgetDemos()
	{
		ImGui.TextUnformatted("ImGuiWidgets Library - Comprehensive Demo");
		ImGui.Separator();

		ShowMobileFormControlsDemo();
		ShowKnobDemo();
		ShowRadialProgressBarDemo();
		ShowColorIndicatorDemo();
		ShowComboDemo();
		ShowTextDemo();
		ShowScopedWidgetsDemo();
		ShowTreeDemo();
		ShowMobileDecoratorsDemo();
		ShowMobileContainersDemo();
	}

	private static void ShowAdvancedDemos()
	{
		AbsoluteFilePath ktsuIconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>();
		ImGuiAppTextureInfo ktsuTexture = ImGuiApp.GetOrLoadTexture(ktsuIconPath);

		ImGui.TextUnformatted("Advanced Widget Demos");
		ImGui.Separator();

		ShowImageAndIconDemo(ktsuTexture);
		ShowImageCanvasDemo(ktsuTexture);
		ShowTabPanelDemo();
		ShowSearchBoxDemo();
		ShowGridDemo(ktsuTexture);
		ShowDividerDemo();

		MessageOK.ShowIfOpen();
	}

	private static void ShowImageCanvasDemo(ImGuiAppTextureInfo ktsuTexture)
	{
		if (DemoProbe.Header("ImageCanvas"))
		{
			ImGui.TextUnformatted("Pannable, zoomable image canvas with a checkerboard behind transparency:");
			ImGui.BulletText("Drag to pan");
			ImGui.BulletText("Scroll to zoom toward the cursor");
			ImGui.BulletText("Double-click to fit the image to the canvas");
			ImGui.Separator();

			if (DemoProbe.Button("Fit"))
			{
				ImageCanvasDemoState.FitToViewport(new Vector2(ktsuTexture.Width, ktsuTexture.Height), new Vector2(ImGui.GetContentRegionAvail().X, 300f));
			}
			ImGui.SameLine();
			if (DemoProbe.Button("1:1"))
			{
				ImageCanvasDemoState.ResetToActualSize();
			}
			ImGui.SameLine();
			ImGui.TextUnformatted($"Zoom: {ImageCanvasDemoState.Zoom:0.##}x");

			Vector2 imageSize = new(ktsuTexture.Width, ktsuTexture.Height);
			Vector2 canvasSize = new(ImGui.GetContentRegionAvail().X, 300f);
			ImGuiWidgets.ImageCanvas("image_canvas_demo", ktsuTexture.TextureId, imageSize, ImageCanvasDemoState, canvasSize);
		}
	}

	private static void ShowTabPanelDemo()
	{
		if (DemoProbe.Header("TabPanel"))
		{
			ImGui.TextUnformatted("Tabbed interface with dirty state tracking:");
			ImGui.Separator();

			// Tab Panel controls
			ImGui.TextUnformatted("Tab Management:");
			if (DemoProbe.Button("Mark Active Tab Dirty"))
			{
				DemoTabPanel.MarkActiveTabDirty();
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Mark Active Tab Clean"))
			{
				DemoTabPanel.MarkActiveTabClean();
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Add New Tab"))
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
		if (DemoProbe.Header("SearchBox"))
		{
			ImGui.TextUnformatted("Powerful search functionality with multiple filter types:");
			ImGui.Separator();

			ImGui.TextUnformatted("Basic SearchBox (UI only):");
			ImGuiWidgets.SearchBox(ref BasicSearchOptions, ref BasicSearchTerm);
			ImGui.TextUnformatted($"Search term: '{BasicSearchTerm}' | Type: {BasicSearchOptions.FilterType} | Match: {BasicSearchOptions.MatchOptions}");

			ImGui.Separator();
			ImGui.TextUnformatted("SearchBox with Filtering:");

			// Toggle whether an empty filter returns all items or none
			bool returnAllWhenEmpty = FilteredSearchOptions.ReturnAllWhenEmpty;
			if (DemoProbe.Checkbox("Return all items when the filter is empty", ref returnAllWhenEmpty))
			{
				FilteredSearchOptions = FilteredSearchOptions with { ReturnAllWhenEmpty = returnAllWhenEmpty };
			}

			// Toggle whether the input stretches to the full available content width
			bool fullWidth = FilteredSearchOptions.FullWidth;
			if (DemoProbe.Checkbox("Stretch input to full content width", ref fullWidth))
			{
				FilteredSearchOptions = FilteredSearchOptions with { FullWidth = fullWidth };
			}

			// Using the SearchBox that returns filtered results
			List<string> filteredResults = [.. ImGuiWidgets.SearchBox(
				ref FilteredSearchOptions,
				ref FilteredSearchTerm,
				items: GridStrings,
				selector: s => s)];

			if (filteredResults.Count > 0)
			{
				string forText = string.IsNullOrEmpty(FilteredSearchTerm) ? "empty filter" : $"'{FilteredSearchTerm}'";
				ImGui.TextUnformatted($"Results: {filteredResults.Count} matches for {forText}");
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

			List<string> rankedResults = [.. ImGuiWidgets.SearchBoxRanked(
				ref RankedSearchOptions,
				ref RankedSearchTerm,
				items: GridStrings,
				selector: s => s)];

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
			List<string> globResults = [.. ImGuiWidgets.SearchBox(
				ref GlobSearchOptions,
				ref GlobSearchTerm,
				items: GridStrings,
				selector: s => s)];

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
			List<string> regexResults = [.. ImGuiWidgets.SearchBox(
				ref RegexSearchOptions,
				ref RegexSearchTerm,
				items: GridStrings,
				selector: s => s)];

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
		if (DemoProbe.Header("Grid Layout"))
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 itemSpacing = style.ItemSpacing;
			Vector2 framePadding = style.FramePadding;

			ImGui.TextUnformatted("Flexible grid layouts with automatic sizing:");
			ImGui.Separator();

			// Grid settings - inline controls
			ImGui.TextUnformatted("Grid Configuration:");

			bool showGridDebug = ImGuiWidgets.EnableGridDebugDraw;
			if (DemoProbe.Checkbox("Show Grid Debug Draw", ref showGridDebug))
			{
				ImGuiWidgets.EnableGridDebugDraw = showGridDebug;
			}
			ImGui.SameLine();

			bool showIconDebug = ImGuiWidgets.EnableIconDebugDraw;
			if (DemoProbe.Checkbox("Show Icon Debug Draw", ref showIconDebug))
			{
				ImGuiWidgets.EnableIconDebugDraw = showIconDebug;
			}

			ImGui.Columns(3, "GridSettings");

			bool gridIconSizeBig = GridIconSizeBig;
			if (DemoProbe.Checkbox("Big Icons", ref gridIconSizeBig))
			{
				GridIconSizeBig = gridIconSizeBig;
			}

			bool gridFitToContents = GridFitToContents;
			if (DemoProbe.Checkbox("Fit to Contents", ref gridFitToContents))
			{
				GridFitToContents = gridFitToContents;
			}

			ImGui.NextColumn();

			int gridItemsToShow = GridItemsToShow;
			if (DemoProbe.SliderInt("Items", ref gridItemsToShow, 0, GridStrings.Count))
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
			if (DemoProbe.SliderFloat("Height", ref gridHeight, 100f, 800f))
			{
				GridHeight = gridHeight;
			}

			ImGui.Columns(1);
			ImGui.Separator();

			// Grid display
			float iconSizePx = ImGuiApp.EmsToPx(2.5f);
			float bigIconSizePx = iconSizePx * 2;
			float gridIconSize = GridIconSizeBig ? bigIconSizePx : iconSizePx;

			Vector2 MeasureGridSize(string textBlock) => ImGuiWidgets.CalcIconSize(textBlock, gridIconSize, GridIconAlignment, itemSpacing, framePadding);
			void DrawGridCell(string textBlock, Vector2 cellSize, Vector2 itemSize)
			{
				float containerSizeX = GridIconAlignment == ImGuiWidgets.IconAlignment.Vertical ? cellSize.X : itemSize.X;
				float containerSizeY = GridIconAlignment == ImGuiWidgets.IconAlignment.Vertical ? itemSize.Y : cellSize.Y;
				using (new Alignment.CenterWithin(itemSize, new(containerSizeX, containerSizeY)))
				{
					ImGuiWidgets.Icon(textBlock, ktsuTexture.TextureId, gridIconSize, GridIconAlignment);
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
	private static void ShowMobileFormControlsDemo()
	{
		if (DemoProbe.Header("Mobile - Form Controls"))
		{
			ImGui.TextUnformatted("Mobile-style form controls (Switch, SegmentedControl, Chip, Stepper, RangeSlider):");
			ImGui.Separator();

			ImGui.TextUnformatted("Switch (iOS-style toggle with animated thumb):");
			ImGuiWidgets.Switch("Wi-Fi##switchWifi", ref switchWifi);
			ImGuiWidgets.Switch("Bluetooth##switchBluetooth", ref switchBluetooth);

			ImGui.Separator();
			ImGui.TextUnformatted("Segmented control (sliding highlight):");
			ImGuiWidgets.SegmentedControl("##viewMode", ref segmentSelected, "Day", "Week", "Month", "Year");
			ImGui.TextUnformatted($"Selected segment index: {segmentSelected}");

			ImGui.Separator();
			ImGui.TextUnformatted("Chips (single-select, wrapping group):");
			ImGuiWidgets.ChipGroup("##chipTags", chipTags, ref chipGroupSelected, allowDeselect: true);
			ImGui.TextUnformatted(chipGroupSelected >= 0 ? $"Filter: {chipTags[chipGroupSelected]}" : "Filter: (none)");

			ImGui.Separator();
			ImGui.TextUnformatted("Stepper (hold +/- to repeat):");
			ImGuiWidgets.Stepper("Quantity##stepperQuantity", ref stepperQuantity, step: 1, min: 0, max: 99);

			ImGui.Separator();
			ImGui.TextUnformatted("Range slider (dual handle, drag either grab):");
			ImGui.SetNextItemWidth(260.0f);
			ImGuiWidgets.RangeSlider("Price##rangePrice", ref rangeLower, ref rangeUpper, 0.0f, 100.0f, minGap: 5.0f);
			ImGui.TextUnformatted($"Range: {rangeLower:F0} - {rangeUpper:F0}");
		}
	}

	private static void ShowKnobDemo()
	{
		if (DemoProbe.Header("Knobs"))
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

			if (DemoProbe.Button("Reset to 0.5"))
			{
				value = 0.5f;
			}
		}
	}

	private static void ShowRadialProgressBarDemo()
	{
		if (DemoProbe.Header("Radial Progress Bar"))
		{
			ImGui.TextUnformatted("Circular progress indicators for loading and progress tracking:");
			ImGui.Separator();

			// Animation controls
			ImGui.TextUnformatted("Animation:");
			DemoProbe.Checkbox("Animate", ref progressAnimating);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(150);
			DemoProbe.SliderFloat("Speed", ref progressAnimationSpeed, 0.1f, 2.0f, "%.1fx");

			// Update animation
			if (progressAnimating)
			{
				progressValue += progressAnimationSpeed * ImGui.GetIO().DeltaTime * 0.2f;
				if (progressValue > 1.0f)
				{
					progressValue = 0.0f;
				}
			}

			ImGui.Separator();

			// Manual progress control
			ImGui.TextUnformatted("Manual Control:");
			if (DemoProbe.SliderFloat("Progress", ref progressValue, 0.0f, 1.0f, "%.2f"))
			{
				progressAnimating = false;
			}

			ImGui.Separator();

			// Show different sizes and styles
			ImGui.TextUnformatted("Different Sizes:");
			ImGui.Columns(4, "ProgressBarColumns");

			ImGuiWidgets.RadialProgressBar(progressValue, 30);
			ImGui.TextUnformatted("Small");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 50);
			ImGui.TextUnformatted("Medium");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 70);
			ImGui.TextUnformatted("Large");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 90);
			ImGui.TextUnformatted("Extra Large");

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted("Options:");

			ImGui.Columns(4, "ProgressBarOptionsColumns");

			ImGuiWidgets.RadialProgressBar(progressValue);
			ImGui.TextUnformatted("Default (CW, Top)");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.NoText);
			ImGui.TextUnformatted("No Text");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.CounterClockwise);
			ImGui.TextUnformatted("Counter-Clockwise");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(progressValue, 0, 0, 32, ImGuiRadialProgressBarOptions.StartAtBottom);
			ImGui.TextUnformatted("Start at Bottom");

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted($"Current Progress: {progressValue * 100.0f:F1}%");

			if (DemoProbe.Button("Reset to 0%"))
			{
				progressValue = 0.0f;
				progressAnimating = false;
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Set to 50%"))
			{
				progressValue = 0.5f;
				progressAnimating = false;
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Set to 100%"))
			{
				progressValue = 1.0f;
				progressAnimating = false;
			}

			ImGui.Separator();

			// Countdown Timer Demo
			ImGui.TextUnformatted("Countdown Timer:");
			DemoProbe.Checkbox("Run Countdown", ref countdownRunning);
			ImGui.SameLine();
			if (DemoProbe.Button("Reset Countdown"))
			{
				countdownTime = CountdownTotal;
				countdownRunning = false;
			}

			if (countdownRunning && countdownTime > 0.0f)
			{
				countdownTime -= ImGui.GetIO().DeltaTime;
				if (countdownTime < 0.0f)
				{
					countdownTime = 0.0f;
					countdownRunning = false;
				}
			}

			ImGui.Columns(3, "CountdownColumns");
			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50);
			ImGui.TextUnformatted("Countdown (Top)");
			ImGui.NextColumn();

			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50, 0, 32, ImGuiRadialProgressBarOptions.CounterClockwise);
			ImGui.TextUnformatted("Counter-Clockwise");
			ImGui.NextColumn();

			ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal, 50, 0, 32, ImGuiRadialProgressBarOptions.StartAtBottom);
			ImGui.TextUnformatted("Start at Bottom");
			ImGui.Columns(1);

			ImGui.Separator();

			// Count-Up Timer Demo
			ImGui.TextUnformatted("Count-Up Timer:");
			DemoProbe.Checkbox("Run Count-Up", ref countupRunning);
			ImGui.SameLine();
			if (DemoProbe.Button("Reset Count-Up"))
			{
				countupTime = 0.0f;
				countupRunning = false;
			}

			if (countupRunning && countupTime < CountupTotal)
			{
				countupTime += ImGui.GetIO().DeltaTime;
				if (countupTime > CountupTotal)
				{
					countupTime = CountupTotal;
					countupRunning = false;
				}
			}

			ImGui.Columns(2, "CountUpColumns");
			ImGuiWidgets.RadialCountUp(countupTime, CountupTotal, 60);
			ImGui.TextUnformatted("Count-Up");
			ImGui.NextColumn();

			ImGuiWidgets.RadialProgressBar(countupTime / CountupTotal, 60, 0, 32, ImGuiRadialProgressBarOptions.None, ImGuiRadialProgressBarTextMode.Custom, 0, $"{countupTime:F1}s");
			ImGui.TextUnformatted("Custom Text");
			ImGui.Columns(1);
		}
	}

	private static void ShowColorIndicatorDemo()
	{
		if (DemoProbe.Header("Color Indicators"))
		{
			ImGui.TextUnformatted("Color indicators show enabled/disabled states:");
			ImGui.Separator();

			ImGui.TextUnformatted("Status Lights:");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("System OK");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Warning, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Warning");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Error, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Error");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Info, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Info");

			ImGui.Separator();
			ImGui.TextUnformatted("Enabled vs Disabled:");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Enabled");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, false);
			ImGui.SameLine();
			ImGui.TextUnformatted("Disabled");
		}
	}

	private static void ShowMobileDecoratorsDemo()
	{
		if (DemoProbe.Header("Mobile - Decorators"))
		{
			AbsoluteFilePath ktsuIconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>();
			ImGuiAppTextureInfo ktsuTexture = ImGuiApp.GetOrLoadTexture(ktsuIconPath);

			ImGui.TextUnformatted("Avatars (circular image, initials fallback, presence dot):");
			ImGui.Separator();
			ImGuiWidgets.Avatar("AvatarImage", ktsuTexture.TextureId, status: AvatarStatus.Online);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarJD", "John Doe", status: AvatarStatus.Away);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarAB", "Ada Byron", status: AvatarStatus.Busy);
			ImGui.SameLine();
			ImGuiWidgets.Avatar("AvatarGrace", "Grace Hopper", status: AvatarStatus.Offline);

			ImGui.Separator();
			ImGui.TextUnformatted("Badges (overlay the previously drawn item):");
			DemoProbe.Button("Inbox");
			ImGuiWidgets.Badge(notificationCount);
			ImGui.SameLine(0, 30);
			DemoProbe.Button("Messages");
			ImGuiWidgets.Badge(150);
			ImGui.SameLine(0, 30);
			DemoProbe.Button("Updates");
			ImGuiWidgets.BadgeDot();
			DemoProbe.SliderInt("Count", ref notificationCount, 0, 200);

			ImGui.Separator();
			ImGui.TextUnformatted("Rating (click to set):");
			ImGuiWidgets.Rating("WholeRating", ref ratingValue);
			ImGui.SameLine();
			ImGui.TextUnformatted($"{ratingValue:0.#}");
			ImGui.TextUnformatted("Half-step, read-only:");
			ImGuiWidgets.Rating("HalfRating", ref halfRatingValue, allowHalf: true);
			ImGui.SameLine();
			ImGuiWidgets.Rating("ReadOnlyRating", ref halfRatingValue, allowHalf: true, readOnly: true);

			ImGui.Separator();
			ImGui.TextUnformatted("Page indicator (click a dot):");
			carouselPage = ImGuiWidgets.PageIndicator("Carousel", carouselPage, 5, interactive: true);
			ImGui.TextUnformatted($"Page {carouselPage + 1} of 5");
		}
	}

	private static void ShowMobileContainersDemo()
	{
		if (DemoProbe.Header("Mobile - Containers & Loaders"))
		{
			ImGui.TextUnformatted("Card (scoped, shadowed, elevated container):");
			ImGui.Separator();

			using (new ImGuiWidgets.Card(width: 280.0f))
			{
				ImGui.TextUnformatted("Card title");
				ImGui.TextWrapped("Cards group related content on an elevated surface with a soft drop shadow. Padding and rounding scale with the theme.");
				if (DemoProbe.Button("Action##cardAction"))
				{
					notificationCount++;
				}
			}

			ImGui.Separator();
			ImGui.TextUnformatted("PIN / OTP input (auto-advancing boxes):");
			ImGuiWidgets.PinInput("Pin##pin", ref pinValue, length: 4);
			ImGui.TextUnformatted($"PIN: {(pinValue.Length == 4 ? pinValue : "(incomplete)")}");

			ImGui.TextUnformatted("Masked, 6 digits:");
			ImGuiWidgets.PinInput("Otp##otp", ref otpValue, length: 6, masked: true);

			ImGui.Separator();
			DemoProbe.Checkbox("Loading##skeletonToggle", ref skeletonLoading);
			ImGui.TextUnformatted("Skeleton loaders (shimmer placeholders):");
			if (skeletonLoading)
			{
				ImGuiWidgets.SkeletonCircle("SkelAvatar");
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGuiWidgets.SkeletonLine("SkelLine1", width: 180.0f);
				ImGui.Spacing();
				ImGuiWidgets.SkeletonLine("SkelLine2", width: 120.0f);
				ImGui.EndGroup();
				ImGui.Spacing();
				ImGuiWidgets.SkeletonRect("SkelThumb", new Vector2(220.0f, 80.0f));
			}
			else
			{
				ImGui.TextUnformatted("Content loaded.");
			}
		}
	}

	private static void ShowComboDemo()
	{
		if (DemoProbe.Header("Combo Boxes"))
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
		if (DemoProbe.Header("Text Utilities"))
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
		if (DemoProbe.Header("Scoped Utilities"))
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

				DemoProbe.Checkbox("Disabled Checkbox", ref dummyBool);
				ImGui.Combo("Disabled Combo", ref dummyInt, items, items.Length);
				ImGuiProbes.MarkItem("Disabled Combo");
				DemoProbe.Button("Disabled Button");
			}

			ImGui.Separator();

			// ScopedId demo
			ImGui.TextUnformatted("ScopedId - manages ImGui ID stack automatically:");
			for (int i = 0; i < 3; i++)
			{
				using (new ImGuiWidgets.ScopedId(i))
				{
					bool state = false;
					DemoProbe.Checkbox("Same Label", ref state);
				}
			}
			ImGui.TextUnformatted("↑ Three checkboxes with same label using ScopedId");
		}
	}

	private static void ShowTreeDemo()
	{
		if (DemoProbe.Header("Tree View"))
		{
			ImGui.TextUnformatted("Hierarchical tree structure with automatic cleanup:");
			ImGui.Separator();

			using ImGuiWidgets.Tree tree = new();
			for (int i = 0; i < 3; i++)
			{
				using (tree.Child)
				{
					DemoProbe.Button($"Parent Node {i + 1}");

					using ImGuiWidgets.Tree subtree = new();
					for (int j = 0; j < 2; j++)
					{
						using (subtree.Child)
						{
							DemoProbe.Button($"Child {j + 1}");

							if (i == 0 && j == 0) // Show deeper nesting for first item
							{
								using ImGuiWidgets.Tree deepTree = new();
								using (deepTree.Child)
								{
									DemoProbe.Button("Grandchild");
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
		if (DemoProbe.Header("Images & Icons"))
		{
			ImGui.TextUnformatted("Interactive images and icons with events:");
			ImGui.Separator();

			// Image demo with color tinting
			ImGui.TextUnformatted("Clickable Image (with alpha-preserved tinting):");
			ImGuiVector4 tintColor = new(1.0f, 0.8f, 0.8f, 1.0f); // Light red tint
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
		if (DemoProbe.Header("Divider Container"))
		{
			ImGui.TextUnformatted("DividerContainer features:");
			ImGui.BulletText("Resizable panes with drag handle");
			ImGui.BulletText("Persistent sizing ratios");
			ImGui.BulletText("Automatic content management");
			ImGui.BulletText("Nested dividers support");

			ImGui.Separator();

			// The container lays itself out into the whole remaining content region, so it is given a
			// fixed-height host to draw into rather than being left to swallow the rest of the tab.
			ImGui.BeginChild("DividerDemoHost", new Vector2(0f, 120f), ImGuiChildFlags.None);
			DividerDemoContainer.Tick(deltaTime);
			ImGui.EndChild();
		}
	}

	// Tab content methods
	private static void ShowTab1Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 1");

		if (DemoProbe.Button("Edit Content"))
		{
			DemoTabPanel.MarkTabDirty(TabIds["tab1"]);
		}

		if (DemoProbe.Button("Save Content"))
		{
			DemoTabPanel.MarkTabClean(TabIds["tab1"]);
		}

		ImGui.TextUnformatted("Dirty State: " + (DemoTabPanel.IsTabDirty(TabIds["tab1"]) ? "Modified" : "Unchanged"));
	}

	private static void ShowTab2Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 2");

		if (DemoProbe.SliderFloat("Value", ref tab2Value, 0.0f, 1.0f))
		{
			// Mark tab as dirty when slider value changes
			DemoTabPanel.MarkTabDirty(TabIds["tab2"]);
		}

		if (DemoProbe.Button("Reset"))
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

		if (DemoProbe.Button("Toggle Dirty State"))
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

		if (DemoProbe.Button("Toggle Dirty State"))
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

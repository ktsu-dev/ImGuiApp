// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

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
	internal static void ResetState() => DemoSections.ResetState();

	// These forwarders predate the split into one class per section and stay for the tests that
	// already read them. A new section exposes its accessors on its own class instead.

	/// <summary>Gets the tags held by the property grid section's list row.</summary>
	internal static IReadOnlyList<string> PropertyTags => PropertyGridDemo.PropertyTags;

	/// <summary>Gets how many rows of the property grid section have been edited.</summary>
	internal static int PropertyEditCount => PropertyGridDemo.PropertyEditCount;

	/// <summary>Gets the value of the Wi-Fi switch in the mobile form-control section.</summary>
	internal static bool SwitchWifi => MobileFormControlsDemo.SwitchWifi;

	/// <summary>Gets the value driven by the knob and radial progress sections.</summary>
	internal static float ProgressValue => RadialProgressBarDemo.ProgressValue;

	/// <summary>Gets the tab panel the advanced section demonstrates.</summary>
	internal static ImGuiWidgets.TabPanel TabPanel => TabPanelDemo.TabPanel;

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

	private static void OnStart() => DemoSections.Initialize();

	private static void OnRender(float dt)
	{
		DemoContext.DeltaTime = dt;

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

		DemoSections.ShowAll(DemoSections.WidgetDemos);
	}

	private static void ShowAdvancedDemos()
	{
		DemoContext.KtsuTexture = ImGuiApp.GetOrLoadTexture(DemoContext.KtsuIconPath);

		ImGui.TextUnformatted("Advanced Widget Demos");
		ImGui.Separator();

		DemoSections.ShowAll(DemoSections.AdvancedDemos);

		ImagesAndIconsDemo.ShowPopups();
	}
}

// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

/// <summary>
/// Side-by-side comparison of the widgets that exist in both ktsu.ImGui.Widgets and
/// Hexa.NET.ImGui.Widgets, plus a gallery of the Hexa widgets that have no ktsu counterpart.
/// Each comparison row drives both implementations from the same backing field so behavioural
/// differences are visible rather than inferred.
/// </summary>
internal static class HexaWidgetsDemo
{
	// Shared state: each field feeds BOTH implementations of its row.
	private static bool sharedToggle = true;
	private static EnumValues sharedEnum = EnumValues.Value1;
	private static float sharedSplitWidth = 200f;
	private static float sharedProgress = 0.45f;
	private static float sharedImageSize = 48f;
	private static int ktsuImageClicks;

	// Net-new widget state.
	private static string breadcrumbPath = @"C:\dev\ktsu-dev\ImGuiApp\ImGui.Widgets";
	private static DateTime pickedDate = new(2026, 8, 14);
	private static DateTime pickedYear = new(2026, 1, 1);
	private static AbsoluteDirectoryPath treeFolder = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>();
	private static bool toggleButtonState;
	private static int flameSelected = -1;

	private static readonly Collection<FlameGraphSample> FlameSamples =
	[
		new(0f, 10f, 0, "Frame"),
		new(0f, 4f, 1, "Update"),
		new(4f, 9.5f, 1, "Render"),
		new(4.2f, 6f, 2, "Cull"),
		new(6f, 9.4f, 2, "Draw"),
	];

	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Signature must match ktsu.ImGui.Widgets.DividerContainer's Action<float> tick delegate (see DividerContainer.Add); this pane's content does not depend on the available width.")]
	internal static void Show(float size)
	{
		if (ImGui.BeginTabBar("HexaWidgetsTabs"))
		{
			if (ImGui.BeginTabItem("Hexa vs ktsu"))
			{
				ShowComparison();
				ImGui.EndTabItem();
			}

			if (ImGui.BeginTabItem("Net New"))
			{
				ShowNetNew();
				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}
	}

	private static void ShowComparison()
	{
		ImGui.TextWrapped("Both columns of each row are bound to the same value. Change one and the other follows.");
		ImGui.Separator();

		if (!ImGui.BeginTable("HexaComparison", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			return;
		}

		ImGui.TableSetupColumn("Widget", ImGuiTableColumnFlags.WidthFixed, 140f);
		ImGui.TableSetupColumn("ktsu");
		ImGui.TableSetupColumn("Hexa");
		ImGui.TableHeadersRow();

		BeginRow("Toggle");
		ImGuiWidgets.Switch("##ktsuSwitch", ref sharedToggle);
		ImGui.TableNextColumn();
		ImGuiWidgets.ToggleSwitch("##hexaToggle", ref sharedToggle);

		BeginRow("Enum combo");
		ImGuiWidgets.Combo("##ktsuCombo", ref sharedEnum);
		ImGui.TableNextColumn();
		ImGuiWidgets.EnumCombo("##hexaCombo", ref sharedEnum);

		BeginRow("Tree node");
		using (ImGuiWidgets.Tree ktsuTree = new())
		{
			using (ktsuTree.Child)
			{
				ImGui.TextUnformatted("ktsu tree child");
			}
		}

		ImGui.TableNextColumn();
		// U+E2C7 is Material Icons' Folder glyph; renders as a placeholder box without that font.
		if (ImGuiWidgets.IconTreeNode("Hexa tree", "\uE2C7", Color.FromHex("#e6b333")))
		{
			ImGui.TextUnformatted("Hexa tree child");
			ImGui.TreePop();
		}

		BeginRow("Progress");
		ImGuiWidgets.RadialProgressBar(sharedProgress);
		ImGui.TableNextColumn();
		ImGuiWidgets.BufferingBar(sharedProgress, new Vector2(160f, 12f), new Srgb(0.2f, 0.2f, 0.2f), new Srgb(0.2f, 0.7f, 1f));
		ImGuiWidgets.Spinner(10f, 3f, new Srgb(0.2f, 0.7f, 1f));

		BeginRow("Text centering");
		ImGuiWidgets.TextCentered("ktsu centered");
		ImGui.TableNextColumn();
		ImGuiWidgets.TextCenteredH("Hexa centered");

		BeginRow("Image centering");
		ImGuiAppTextureInfo sharedImage = ImGuiApp.GetOrLoadTexture(
			AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>());
		Vector2 imageSize = new(sharedImageSize, sharedImageSize);

		// ktsu's ImageCentered reports clicks; Hexa's ImageCenteredH does not.
		if (ImGuiWidgets.ImageCentered(sharedImage.TextureId, imageSize))
		{
			ktsuImageClicks++;
		}

		ImGui.TextUnformatted($"clicks: {ktsuImageClicks}");
		ImGui.TableNextColumn();
		ImGuiWidgets.ImageCenteredH(sharedImage.TextureId, imageSize);

		BeginRow("Splitter");
		ImGui.TextUnformatted($"DividerContainer: see the Advanced pane ({sharedSplitWidth:F0}px)");
		ImGui.TableNextColumn();
		ImGuiWidgets.VerticalSplitter("##hexaSplitter", ref sharedSplitWidth, 80f, 400f, 40f);
		ImGui.SameLine();
		ImGui.TextUnformatted($"{sharedSplitWidth:F0}px");

		ImGui.EndTable();

		ImGui.Separator();
		ImGui.SliderFloat("Shared progress", ref sharedProgress, 0f, 1f);
		ImGui.SliderFloat("Shared image size", ref sharedImageSize, 16f, 96f);
	}

	private static void BeginRow(string name)
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(name);
		ImGui.TableNextColumn();
	}

	private static void ShowNetNew()
	{
		ImGui.TextWrapped("Hexa widgets with no ktsu counterpart.");
		ImGui.Separator();

		if (ImGui.CollapsingHeader("Breadcrumb"))
		{
			ImGuiWidgets.Breadcrumb("##breadcrumb", ref breadcrumbPath);
			ImGui.TextUnformatted(breadcrumbPath);
		}

		if (ImGui.CollapsingHeader("Buttons"))
		{
			ImGuiWidgets.ToggleButton("Toggle me", ref toggleButtonState);
			ImGui.SameLine();
			if (ImGuiWidgets.TransparentButton("Transparent"))
			{
				toggleButtonState = !toggleButtonState;
			}
		}

		if (ImGui.CollapsingHeader("Date and year pickers"))
		{
			ImGui.TextWrapped("The date picker's calendar button needs a Material Icons font in the atlas (see ImGuiAppDemo); without one it shows a placeholder box. The year picker below draws no icon glyphs and needs no icon font.");
			ImGuiWidgets.DatePicker("Date", ref pickedDate);
			ImGuiWidgets.YearPicker("Year", ref pickedYear);
			ImGui.TextUnformatted($"Picked: {pickedDate:yyyy-MM-dd}, year {pickedYear:yyyy}");
		}

		if (ImGui.CollapsingHeader("Flame graph"))
		{
			ImGuiWidgets.FlameGraph("Frame timing", FlameSamples, ref flameSelected,
				new FlameGraphOptions { GraphSize = new Vector2(0f, 120f) });
			ImGui.TextUnformatted($"Selected: {flameSelected}");
		}

		if (ImGui.CollapsingHeader("File tree view"))
		{
			ImGui.TextWrapped("Needs a Material Icons font in the atlas; see ImGuiAppDemo.");
			ImGuiWidgets.FileTreeView("##fileTree", new Vector2(0f, 200f), ref treeFolder, treeFolder);
			ImGui.TextUnformatted(treeFolder.ToString());
		}
	}
}

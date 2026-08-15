// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Popups;
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

	// Shared state: the ktsu and Hexa dialogs of each row write to the same field.
	private static string sharedChosenFile = "(none)";
	private static string sharedChosenFolder = "(none)";
	private static string sharedSaveTarget = "(none)";
	private static string sharedMessageAnswer = "(none)";

	// ktsu's popups render inline wherever they are pumped (see the ShowIfOpen calls at the end of
	// Show), unlike Hexa's static deferred-draw manager, so each needs its own persistent instance
	// to survive across frames and its own per-frame pump call. Each instance must also be owned
	// exclusively by the pane that opens it: ImGui derives a popup's underlying ID from the ID
	// stack of whichever window is current when Open()/ShowIfOpen() run, so sharing one instance
	// across two panes that tick every frame in their own BeginChild (as this "Hexa Widgets" zone
	// and the "Advanced Demos" zone both do via DividerContainer) lets a later Open() from one pane
	// silently strand a popup already open under the other pane's ID.
	private static readonly ImGuiPopups.FilesystemBrowser KtsuOpenFileBrowser = new();
	private static readonly ImGuiPopups.FilesystemBrowser KtsuOpenFolderBrowser = new();
	private static readonly ImGuiPopups.FilesystemBrowser KtsuSaveFileBrowser = new();
	private static readonly ImGuiPopups.MessageOK KtsuMessageBox = new();

	// Net-new widget state.
	private static string breadcrumbPath = @"C:\dev\ktsu-dev\ImGuiApp\ImGui.Widgets";
	private static DateTime pickedDate = new(2026, 8, 14);
	private static DateTime pickedYear = new(2026, 1, 1);
	private static AbsoluteDirectoryPath treeFolder = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>();
	private static bool toggleButtonState;
	private static int flameSelected = -1;
	private static string renameResult = "(none)";

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

			if (ImGui.BeginTabItem("Dialogs"))
			{
				ShowDialogComparison();
				ImGui.EndTabItem();
			}

			ImGui.EndTabBar();
		}

		// ktsu's popups render inline wherever they're pumped, so each live dialog in the "Dialogs"
		// tab needs an unconditional per-frame ShowIfOpen() call here. This method runs every frame
		// regardless of which internal tab is active -- the same guarantee ImGuiWidgets.DrawDeferred()
		// relies on in OnRender for the Hexa side.
		KtsuMessageBox.ShowIfOpen();
		KtsuOpenFileBrowser.ShowIfOpen();
		KtsuOpenFolderBrowser.ShowIfOpen();
		KtsuSaveFileBrowser.ShowIfOpen();
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

	private static void ShowDialogComparison()
	{
		ImGui.TextWrapped("Dialogs are windows, not inline widgets, so each row is a pair of buttons writing to one shared field. Open ktsu's, then Hexa's, and compare what comes back.");
		ImGui.Separator();

		if (!ImGui.BeginTable("HexaDialogComparison", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
		{
			return;
		}

		ImGui.TableSetupColumn("Dialog", ImGuiTableColumnFlags.WidthFixed, 140f);
		ImGui.TableSetupColumn("ktsu");
		ImGui.TableSetupColumn("Hexa");
		ImGui.TableHeadersRow();

		BeginRow("Open file");
		if (ImGui.Button("Open##ktsuOpenFile"))
		{
			KtsuOpenFileBrowser.FileOpen("Open a file", file => sharedChosenFile = file.ToString());
		}

		ImGui.TableNextColumn();
		if (ImGui.Button("Open##hexaOpenFile"))
		{
			ImGuiWidgets.OpenFileDialog dialog = new();
			dialog.Show(outcome => sharedChosenFile = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Pick folder");
		if (ImGui.Button("Pick##ktsuFolder"))
		{
			KtsuOpenFolderBrowser.ChooseDirectory("Pick a folder", directory => sharedChosenFolder = directory.ToString());
		}

		ImGui.TableNextColumn();
		if (ImGui.Button("Pick##hexaFolder"))
		{
			ImGuiWidgets.OpenFolderDialog dialog = new();
			dialog.Show(outcome => sharedChosenFolder = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Save file");
		if (ImGui.Button("Save##ktsuSave"))
		{
			KtsuSaveFileBrowser.FileSave("Save a file", file => sharedSaveTarget = file.ToString());
		}

		ImGui.TableNextColumn();
		if (ImGui.Button("Save##hexaSave"))
		{
			ImGuiWidgets.SaveFileDialog dialog = new();
			dialog.Show(outcome => sharedSaveTarget = outcome.Path?.ToString() ?? $"({outcome.Outcome})");
		}

		BeginRow("Message");
		if (ImGui.Button("Ask##ktsuMessage"))
		{
			// MessageOK's convenience Open(title, message) has no outcome callback -- through that
			// path ktsu only ever offers a fixed, silent single "OK" button. Its base Prompt class
			// does support per-button actions, so that inherited Open(title, label, buttons) overload
			// is used here to give this row a real, comparable answer instead of leaving it dead.
			// The button dictionary is typed explicitly rather than via target-typed new() because
			// MessageOK also declares an Open(string, string, Vector2) overload that the compiler
			// would otherwise prefer, since both are three-argument overloads.
			Dictionary<string, Action?> ktsuMessageButtons = new() { { "OK", () => sharedMessageAnswer = "Ok" } };
			KtsuMessageBox.Open("Confirm", "Keep both implementations?", ktsuMessageButtons);
		}

		ImGui.TableNextColumn();
		if (ImGui.Button("Ask##hexaMessage"))
		{
			ImGuiWidgets.ShowMessageBox("Confirm", "Keep both implementations?", MessageBoxButtons.YesNo,
				outcome => sharedMessageAnswer = outcome.ToString());
		}

		ImGui.EndTable();

		ImGui.Separator();
		ImGui.TextUnformatted($"File:    {sharedChosenFile}");
		ImGui.TextUnformatted($"Folder:  {sharedChosenFolder}");
		ImGui.TextUnformatted($"Save to: {sharedSaveTarget}");
		ImGui.TextUnformatted($"Answer:  {sharedMessageAnswer}");

		ImGui.Separator();
		ImGui.TextWrapped("Hexa's file dialogs need a Material Icons font for their navigation bar, and block the UI thread briefly when closing while the async directory scan unwinds. Hexa's message box re-centres itself every frame and cannot be dragged. ktsu's dialogs render inline wherever they are pumped (see the ShowIfOpen calls in Show) rather than through a central deferred-draw manager, and ktsu's MessageOK has no built-in Yes/No button-set the way Hexa's ShowMessageBox does -- this row's ktsu answer is always \"Ok\".");
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
			ImGui.TextWrapped("The date picker's calendar button needs a Material Icons font in the atlas; drop MaterialIcons-Regular.ttf next to this demo's binary and it is picked up automatically. Without one it shows a placeholder box. The year picker below draws no icon glyphs and needs no icon font.");
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
			ImGui.TextWrapped("Needs a Material Icons font in the atlas; drop MaterialIcons-Regular.ttf next to this demo's binary.");
			ImGuiWidgets.FileTreeView("##fileTree", new Vector2(0f, 200f), ref treeFolder, treeFolder);
			ImGui.TextUnformatted(treeFolder.ToString());
		}

		if (ImGui.CollapsingHeader("Rename and message dialogs"))
		{
			if (ImGui.Button("Rename a file"))
			{
				ImGuiWidgets.RenameDialog dialog = new(
					AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "ktsu.png".As<FileName>())
				{
					SkipAutomaticMove = true,
				};
				dialog.Show(outcome => renameResult = outcome.Error?.Message ?? outcome.Destination?.ToString() ?? $"({outcome.Outcome})");
			}

			ImGui.SameLine();
			if (ImGui.Button("Dialog message box"))
			{
				ImGuiWidgets.DialogMessageBox box = new("Question", "Movable, unlike the modal message box.", MessageBoxButtons.YesNoCancel);
				box.Show(outcome => renameResult = outcome.ToString());
			}

			ImGui.TextUnformatted(renameResult);
		}
	}
}
